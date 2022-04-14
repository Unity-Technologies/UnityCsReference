// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnityEditor
{
    internal class GameViewOnPlayMenu : FlexibleMenu
    {
        private static class Styles
        {
            public static readonly GUIContent gamePlayNormalContent = EditorGUIUtility.TrTextContent("Normally", "Play inside a game view docked inside unity");
            public static readonly GUIContent gamePlayMaximizedContent = EditorGUIUtility.TrTextContent("Maximized", "Maximize the game view before entering play mode.");
            public static readonly GUIContent gamePlayFullscreenContent = EditorGUIUtility.TrTextContent("Fullscreen on ", "Play the game view on a fullscreen monitor");
            public static readonly GUIContent playFocusedToggleContent = EditorGUIUtility.TrTextContent("Focused", "Forcilby focus the game view when entering play mode.");
            public static readonly GUIContent playFocusedToggleDisabledContent = EditorGUIUtility.TrTextContent("Focused", "Focus toggle is currently disabled because a view will play maximized.");
            public static GUIContent vSyncToggleContent = EditorGUIUtility.TrTextContent("VSync", "Enable VSync only for the game view while in playmode.");
            public static GUIContent vSyncUnsupportedContent = EditorGUIUtility.TrTextContent("No VSync", "VSync is not available because it is not supported by this device");
            public static GUIContent gamePlayModeBehaviorLabelContent = EditorGUIUtility.TrTextContent("Enter Play Mode:");

            public const float kMargin = 9f;
            public const float kTopMargin = 7f;
            public const int kNumberOfTogglesOnTop = 2;
            public static float frameHeight => (kTopMargin * kNumberOfTogglesOnTop) + EditorGUI.kSingleLineHeight;
            public static float contentOffset => frameHeight + EditorGUI.kControlVerticalSpacing;
        }

        // Number of on play behaviors that should always be visible. Fullscreen may or may not be supported and there may be multiple monitors.
        // The base is 2, "Play Normally" (with focus as a checkbox) and "Play Maximized"
        public const int kPlayModeBaseOptionCount = 2;
        private readonly IGameViewOnPlayMenuUser m_GameView;
        private bool m_ShowFullscreenOptions = true;
        private IFlexibleMenuItemProvider m_ItemProvider;

        public GameViewOnPlayMenu(IFlexibleMenuItemProvider itemProvider, int selectionIndex, FlexibleMenuModifyItemUI modifyItemUi, IGameViewOnPlayMenuUser gameView, bool showFullscreenOptions = true)
           : base(itemProvider, selectionIndex, modifyItemUi, gameView.OnPlayPopupSelection)
        {
            m_GameView = gameView;
            m_ShowFullscreenOptions = showFullscreenOptions;
            m_ItemProvider = itemProvider;
        }

        public override Vector2 GetWindowSize()
        {
            var playFocusedToggleSize = EditorStyles.toggle.CalcSize(Styles.playFocusedToggleContent);
            var size = CalcSize();

            size.x = Mathf.Max(size.x, playFocusedToggleSize.x + Styles.kMargin * 2);
            size.y += Styles.frameHeight + EditorGUI.kControlVerticalSpacing + EditorGUI.kSingleLineHeight;
            return size;
        }

        private bool IsVSyncToggleVisible()
        {
            // Only show the vsync toggle for editor supported gfx device backend.
            var gfxDeviceType = SystemInfo.graphicsDeviceType;
            return gfxDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Metal ||
                gfxDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Vulkan ||
                gfxDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Direct3D11 ||
                gfxDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Direct3D12 ||
                gfxDeviceType == UnityEngine.Rendering.GraphicsDeviceType.OpenGLCore;
        }

        private void DoVSyncToggle(Rect rect)
        {
            if (IsVSyncToggleVisible())
            {
                m_GameView.vSyncEnabled = GUI.Toggle(rect, m_GameView.vSyncEnabled, Styles.vSyncToggleContent);
            }
            else
            {
                m_GameView.vSyncEnabled = false;
                GUI.Label(rect, Styles.vSyncUnsupportedContent, EditorStyles.miniLabel);
            }
        }

        private static void UncheckFocusToggleOnAllViews()
        {
            List<PlayModeView> playViewList;
            WindowLayout.ShowAppropriateViewOnEnterExitPlaymodeList(true, out playViewList);
            foreach (PlayModeView playView in playViewList)
            {
                if (playView is IGameViewOnPlayMenuUser)
                {
                    ((IGameViewOnPlayMenuUser)playView).playFocused = false;
                }
            }
        }

        public static void SetFocusedToggle(IGameViewOnPlayMenuUser view, bool newValue)
        {
            UncheckFocusToggleOnAllViews();
            view.playFocused = newValue;
        }

        private bool IsAnyViewInMaximizeMode()
        {
            List<PlayModeView> playViewList;
            WindowLayout.ShowAppropriateViewOnEnterExitPlaymodeList(true, out playViewList);
            foreach (PlayModeView playView in playViewList)
            {
                if (playView.enterPlayModeBehavior == PlayModeView.EnterPlayModeBehavior.PlayMaximized)
                {
                    SetFocusedToggle(playView as IGameViewOnPlayMenuUser, true);
                    return true;
                }
            }
            return false;
        }

        public override void OnGUI(Rect rect)
        {
            var frameRect = new Rect(rect.x, rect.y, rect.width, rect.height);
            GUI.Label(frameRect, "", EditorStyles.viewBackground);

            var focusTextSize = EditorStyles.label.CalcSize(Styles.playFocusedToggleContent);
            GUI.enabled = !IsAnyViewInMaximizeMode();
            var focusToggleRect = new Rect(Styles.kMargin, Styles.kTopMargin, focusTextSize.x, EditorGUI.kSingleLineHeight);
            bool playFocusedToggle = GUI.Toggle(focusToggleRect, m_GameView.playFocused, GUI.enabled ? Styles.playFocusedToggleContent : Styles.playFocusedToggleDisabledContent);
            GUI.enabled = true;
            if (playFocusedToggle != m_GameView.playFocused)
            {
                SetFocusedToggle(m_GameView, playFocusedToggle);
            }
            var vsyncToggleRect = new Rect(focusTextSize.x + Styles.kMargin*2, Styles.kTopMargin, rect.width, EditorGUI.kSingleLineHeight);
            DoVSyncToggle(vsyncToggleRect);

            var labelSize = EditorStyles.boldLabel.CalcSize(Styles.gamePlayModeBehaviorLabelContent);
            var labelRect = new Rect(Styles.kMargin, Styles.kTopMargin + EditorGUI.kSingleLineHeight, labelSize.x, labelSize.y + EditorGUI.kSingleLineHeight);
            GUI.Label(labelRect, Styles.gamePlayModeBehaviorLabelContent, EditorStyles.boldLabel);

            rect.y = rect.y + Styles.contentOffset + EditorGUI.kSingleLineHeight;

            base.OnGUI(rect);
        }

        public static string GetOnPlayBehaviorName(int selectedIndex)
        {
            if (selectedIndex == 0)
                return Styles.gamePlayNormalContent.text;
            if (selectedIndex == 1)
                return Styles.gamePlayMaximizedContent.text;

            int displayIdx = SelectedIndexToDisplayIndex(selectedIndex);
            var connectedDisplay = EditorFullscreenController.GetConnectedDisplayNames();

            var displayName = (displayIdx >= connectedDisplay.Length)
                ? "Invalid monitor"
                : connectedDisplay[displayIdx];

            return Styles.gamePlayFullscreenContent.text + displayIdx + ":" + displayName;
        }

        public static int SelectedIndexToDisplayIndex(int selectedIndex)
        {
            if (selectedIndex <= 1)
                return -1; //Invalid fullscreen selection
            return selectedIndex - kPlayModeBaseOptionCount;
        }
    }
}
