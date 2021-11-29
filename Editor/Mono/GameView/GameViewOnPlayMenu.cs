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

        public GameViewOnPlayMenu(IFlexibleMenuItemProvider itemProvider, int selectionIndex, FlexibleMenuModifyItemUI modifyItemUi, IGameViewOnPlayMenuUser gameView, bool showFullscreenOptions = true)
           : base(itemProvider, selectionIndex, modifyItemUi, gameView.OnPlayPopupSelection)
        {
            m_GameView = gameView;
            m_ShowFullscreenOptions = showFullscreenOptions;
        }

        public override Vector2 GetWindowSize()
        {
            var playFocusedToggleSize = EditorStyles.toggle.CalcSize(Styles.playFocusedToggleContent);
            var size = CalcSize();

            size.x = Mathf.Max(size.x, playFocusedToggleSize.x + Styles.kMargin * 2);
            size.y += Styles.frameHeight + EditorGUI.kControlVerticalSpacing;
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

        private void DoVSyncToggle()
        {
            if (IsVSyncToggleVisible())
            {
                m_GameView.vSyncEnabled = GUILayout.Toggle(m_GameView.vSyncEnabled, Styles.vSyncToggleContent);
            }
            else
            {
                m_GameView.vSyncEnabled = false;
                GUILayout.Label(Styles.vSyncUnsupportedContent, EditorStyles.miniLabel);
            }
        }

        private void OnPlayFocusedToggleChanged(bool newValue)
        {
            List<PlayModeView> playViewList;
            WindowLayout.ShowAppropriateViewOnEnterExitPlaymodeList(true, out playViewList);

            foreach (PlayModeView playView in playViewList)
            {
                if (playView != (m_GameView as PlayModeView))
                {
                    ((IGameViewOnPlayMenuUser)playView).playFocused = false;
                }
            }

            m_GameView.playFocused = newValue;
        }

        public override void OnGUI(Rect rect)
        {
            var frameRect = new Rect(rect.x, rect.y, rect.width, rect.height);
            GUI.Label(frameRect, "", EditorStyles.viewBackground);

            GUILayout.BeginHorizontal();
            GUILayout.Space(15); // Move everything slightly right so it doesn't overlap with our "repaint indicator"
            bool playFocuedToggle = GUILayout.Toggle(m_GameView.playFocused, Styles.playFocusedToggleContent);
            if (playFocuedToggle != m_GameView.playFocused)
            {
                OnPlayFocusedToggleChanged(playFocuedToggle);
            }
            DoVSyncToggle();
            GUILayout.EndHorizontal();

            GUILayout.Label(Styles.gamePlayModeBehaviorLabelContent, EditorStyles.boldLabel);

            rect.height = rect.height - Styles.contentOffset;
            rect.y = rect.y + Styles.contentOffset;

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
