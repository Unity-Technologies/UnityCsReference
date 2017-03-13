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
        const float kTopMargin = 7f;
        const float kMargin = 9f;
        IGameViewSizeMenuUser m_GameView;

        float frameHeight { get { return kTopMargin * 2 + EditorGUI.kSingleLineHeight; } }
        float contentOffset { get { return frameHeight + EditorGUI.kControlVerticalSpacing; } }

        public GameViewSizeMenu(IFlexibleMenuItemProvider itemProvider, int selectionIndex, FlexibleMenuModifyItemUI modifyItemUi, IGameViewSizeMenuUser gameView)
            : base(itemProvider, selectionIndex, modifyItemUi, gameView.SizeSelectionCallback)
        {
            m_GameView = gameView;
        }

        public override Vector2 GetWindowSize()
        {
            var size = CalcSize();
            if (!m_GameView.showLowResolutionToggle)
                return size;
            size.y += frameHeight + EditorGUI.kControlVerticalSpacing;
            return size;
        }

        public override void OnGUI(Rect rect)
        {
            if (!m_GameView.showLowResolutionToggle)
            {
                base.OnGUI(rect);
                return;
            }

            var frameRect = new Rect(rect.x, rect.y, rect.width, frameHeight);
            GUI.Label(frameRect, "", EditorStyles.inspectorBig);

            GUI.enabled = !m_GameView.forceLowResolutionAspectRatios;

            var toggleRect = new Rect(kMargin, kTopMargin, rect.width, EditorGUI.kSingleLineHeight);
            m_GameView.lowResolutionForAspectRatios = GUI.Toggle(toggleRect, m_GameView.forceLowResolutionAspectRatios ? true : m_GameView.lowResolutionForAspectRatios, GameView.Styles.lowResAspectRatiosContextMenuContent);

            GUI.enabled = true;

            rect.height = rect.height - contentOffset;
            rect.y = rect.y + contentOffset;
            base.OnGUI(rect);
        }
    }
} // namespace
