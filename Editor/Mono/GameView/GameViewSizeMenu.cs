// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;

namespace UnityEditor
{
    // Resolution/Aspect ratio menu for the GameView, with an optional toggle for low-resolution aspect ratios
    internal class GameViewSizeMenu : FlexibleMenu
    {
        static class Styles
        {
            public static GUIContent vSyncToggleContent = EditorGUIUtility.TrTextContent("VSync (Game view only)", "Enable VSync only for the game view while in playmode.");
        }
        const float kTopMargin = 7f;
        const float kMargin = 9f;
        IGameViewSizeMenuUser m_GameView;

        float frameHeight { get { return kTopMargin * 2 + EditorGUI.kSingleLineHeight * (IsVSyncToggleVisible() ? 2 : 1);}}
        float contentOffset { get { return frameHeight + EditorGUI.kControlVerticalSpacing; } }

        public GameViewSizeMenu(IFlexibleMenuItemProvider itemProvider, int selectionIndex, FlexibleMenuModifyItemUI modifyItemUi, IGameViewSizeMenuUser gameView)
            : base(itemProvider, selectionIndex, modifyItemUi, gameView.SizeSelectionCallback)
        {
            m_GameView = gameView;
        }

        public override Vector2 GetWindowSize()
        {
            var lowAspectRatiosContentSize = EditorStyles.toggle.CalcSize(GameView.Styles.lowResAspectRatiosContextMenuContent);
            var size = CalcSize();
            size.x = Mathf.Max(size.x, lowAspectRatiosContentSize.x + kMargin * 2);
            size.y += frameHeight + EditorGUI.kControlVerticalSpacing;
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
            if (!IsVSyncToggleVisible())
                return;
            var toggleRect = new Rect(rect.xMin, rect.yMax + 2, rect.width, EditorGUI.kSingleLineHeight);
            m_GameView.vSyncEnabled = GUI.Toggle(toggleRect, m_GameView.vSyncEnabled, Styles.vSyncToggleContent);
        }

        public override void OnGUI(Rect rect)
        {
            var frameRect = new Rect(rect.x, rect.y, rect.width, frameHeight);
            GUI.Label(frameRect, "", EditorStyles.viewBackground);

            GUI.enabled = !m_GameView.forceLowResolutionAspectRatios;

            var toggleRect = new Rect(kMargin, kTopMargin, rect.width, EditorGUI.kSingleLineHeight);
            m_GameView.lowResolutionForAspectRatios = GUI.Toggle(toggleRect, m_GameView.forceLowResolutionAspectRatios || m_GameView.lowResolutionForAspectRatios, GameView.Styles.lowResAspectRatiosContextMenuContent);

            GUI.enabled = true;
            DoVSyncToggle(toggleRect);

            rect.height = rect.height - contentOffset;
            rect.y = rect.y + contentOffset;
            base.OnGUI(rect);
        }
    }
} // namespace
