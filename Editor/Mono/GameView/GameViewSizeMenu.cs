// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;

namespace UnityEditor
{
    // Resolution/Aspect ratio menu for the GameView, with a pixel size control in its header
    internal class GameViewSizeMenu : FlexibleMenu
    {
        static class Styles
        {
            public static readonly GUIContent vSyncToggleContent = L10n.TextContent("VSync (Game view only)", "Enable VSync only for the game view while in playmode.", null, null);
            public static readonly GUIContent[] pixelSizeSegments =
            {
                L10n.TextContent("1x", "One screen pixel per Game view pixel.", null, null),
                L10n.TextContent("2x", "A 2 by 2 block of screen pixels per Game view pixel.", null, null),
                L10n.TextContent("3x", "A 3 by 3 block of screen pixels per Game view pixel.", null, null),
            };
        }
        const float kPixelSizeSegmentMinWidth = 28f;
        const float kPixelSizeLabelSpacing = 8f;
        readonly Vector3 kMargins = new (9f, 7f);
        IGameViewSizeMenuUser m_GameView;

        float frameHeight { get { return kMargins.y * 2 + EditorGUI.kSingleLineHeight * (IsVSyncToggleVisible() ? 2 : 1);}}
        float contentOffset { get { return frameHeight + EditorGUI.kControlVerticalSpacing; } }

        public GameViewSizeMenu(IFlexibleMenuItemProvider itemProvider, int selectionIndex, FlexibleMenuModifyItemUI modifyItemUi, IGameViewSizeMenuUser gameView)
            : base(itemProvider, selectionIndex, modifyItemUi, gameView.SizeSelectionCallback)
        {
            m_GameView = gameView;
        }

        public override Vector2 GetWindowSize()
        {
            var labelWidth = EditorStyles.label.CalcSize(GameView.Styles.pixelSizeContent).x;
            var headerWidth = labelWidth + kPixelSizeLabelSpacing + pixelSizeSegmentWidth * Styles.pixelSizeSegments.Length;
            var size = CalcSize();
            size.x = Mathf.Max(size.x, headerWidth + kMargins.x * 2);
            size.y += frameHeight + EditorGUI.kControlVerticalSpacing + kMargins.y;
            return size;
        }

        static float pixelSizeSegmentWidth
        {
            get
            {
                var width = kPixelSizeSegmentMinWidth;
                foreach (var segment in Styles.pixelSizeSegments)
                    width = Mathf.Max(width, EditorStyles.miniButtonMid.CalcSize(segment).x);
                return width;
            }
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

        static GUIStyle GetPixelSizeSegmentStyle(int index, int count)
        {
            if (index == 0)
                return EditorStyles.miniButtonLeft;
            return index == count - 1 ? EditorStyles.miniButtonRight : EditorStyles.miniButtonMid;
        }

        void DoPixelSizeControl(Rect rowRect)
        {
            GUI.Label(rowRect, GameView.Styles.pixelSizeContent);

            var segments = Styles.pixelSizeSegments;
            var segmentWidth = pixelSizeSegmentWidth;
            var segmentRect = new Rect(rowRect.xMax - segmentWidth * segments.Length, rowRect.y, segmentWidth, rowRect.height);
            var currentPixelSize = m_GameView.pixelSize;
            for (var i = 0; i < segments.Length; ++i)
            {
                var segmentPixelSize = i + 1;
                EditorGUI.BeginChangeCheck();
                GUI.Toggle(segmentRect, currentPixelSize == segmentPixelSize, segments[i], GetPixelSizeSegmentStyle(i, segments.Length));
                if (EditorGUI.EndChangeCheck())
                    m_GameView.pixelSize = segmentPixelSize;
                segmentRect.x += segmentWidth;
            }
        }

        public override void OnGUI(Rect rect)
        {
            var frameRect = new Rect(rect.x, rect.y, rect.width, frameHeight);
            GUI.Label(frameRect, "", EditorStyles.viewBackground);

            var pixelSizeRect = new Rect(kMargins.x, kMargins.y, rect.width - kMargins.x * 2, EditorGUI.kSingleLineHeight);
            DoPixelSizeControl(pixelSizeRect);
            DoVSyncToggle(pixelSizeRect);

            rect.height = rect.height - contentOffset;
            rect.y = rect.y + contentOffset;
            base.OnGUI(rect);
        }
    }
} // namespace
