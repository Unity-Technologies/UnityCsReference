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
            var lowAspectRatiosContentSize = EditorStyles.toggle.CalcSize(GameView.Styles.lowResAspectRatiosContextMenuContent);
            var size = CalcSize();
            size.x = Mathf.Max(size.x, lowAspectRatiosContentSize.x + kMargin * 2);
            size.y += frameHeight + EditorGUI.kControlVerticalSpacing;
            return size;
        }

        public override void OnGUI(Rect rect)
        {
            var frameRect = new Rect(rect.x, rect.y, rect.width, frameHeight);
            GUI.Label(frameRect, "", EditorStyles.viewBackground);

            GUI.enabled = !m_GameView.forceLowResolutionAspectRatios;

            var toggleRect = new Rect(kMargin, kTopMargin, rect.width, EditorGUI.kSingleLineHeight);
            m_GameView.lowResolutionForAspectRatios = GUI.Toggle(toggleRect, m_GameView.forceLowResolutionAspectRatios || m_GameView.lowResolutionForAspectRatios, GameView.Styles.lowResAspectRatiosContextMenuContent);

            GUI.enabled = true;


            rect.height = rect.height - contentOffset;
            rect.y = rect.y + contentOffset;
            base.OnGUI(rect);
        }
    }
} // namespace
