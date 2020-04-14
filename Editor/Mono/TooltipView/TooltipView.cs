// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEditor
{
    class TooltipView : GUIView
    {
        private const float MAX_WIDTH = 300.0f;

        private GUIContent m_tooltip = new GUIContent();
        private Vector2 m_optimalSize;
        private GUIStyle m_Style;
        private Rect m_hoverRect;

        static TooltipView s_guiView;

        protected override void OnEnable()
        {
            base.OnEnable();
            s_guiView = this;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            s_guiView = null;
        }

        protected override void OldOnGUI()
        {
            if (window != null)
            {
                Color prevColor = GUI.color;
                GUI.color = Color.white;
                GUI.Box(new Rect(0, 0, m_optimalSize.x, m_optimalSize.y) , m_tooltip, m_Style);
                GUI.color = prevColor;
            }
        }

        void Setup(string tooltip, Rect rect, GUIView hostView)
        {
            m_hoverRect = rect;
            m_tooltip.text = tooltip;

            // Calculate size and position tooltip view
            m_Style = EditorStyles.tooltip;

            m_Style.wordWrap = false;
            m_optimalSize = m_Style.CalcSize(m_tooltip);

            if (m_optimalSize.x > MAX_WIDTH)
            {
                m_Style.wordWrap = true;
                m_optimalSize.x = MAX_WIDTH;
                m_optimalSize.y = m_Style.CalcHeight(m_tooltip, MAX_WIDTH);
            }

            var popupPosition = new Rect(
                Mathf.Floor(m_hoverRect.x + (m_hoverRect.width / 2) - (m_optimalSize.x / 2)),
                Mathf.Floor(m_hoverRect.y + (m_hoverRect.height) + 10.0f),
                m_optimalSize.x, m_optimalSize.y);

            if (hostView != null)
            {
                var viewRect = hostView.screenPosition;
                if (popupPosition.x < viewRect.x)
                    popupPosition.x = viewRect.x;
                if (popupPosition.xMax > viewRect.xMax)
                    popupPosition.x -= popupPosition.xMax - viewRect.xMax;
                if (popupPosition.y < viewRect.y)
                    popupPosition.y = viewRect.y;
                if (popupPosition.yMax > viewRect.yMax)
                    popupPosition.y -= popupPosition.yMax - viewRect.yMax;

                popupPosition.y = Mathf.Max(popupPosition.y, Mathf.Floor(m_hoverRect.y + (m_hoverRect.height) + 10.0f));
            }

            // If when fitted to screen, the tooltip would overlap the hover area
            // (and thus potentially mouse) -- for example when the control is near
            // the bottom of screen, place it atop of the hover area instead.
            var fittedToScreen = ContainerWindow.FitRectToScreen(popupPosition, true, true);
            if (fittedToScreen.Overlaps(m_hoverRect))
            {
                popupPosition.y = m_hoverRect.y - m_optimalSize.y - 10.0f;
            }

            window.position = popupPosition;
            position = new Rect(0, 0, m_optimalSize.x, m_optimalSize.y);

            window.ShowTooltip();
            window.SetAlpha(1.0f);
            s_guiView.mouseRayInvisible = true;

            Repaint(); // Flag for repaint but allow time for updates
        }

        public static void Show(string tooltip, Rect rect, GUIView hostView = null)
        {
            if (s_guiView == null)
            {
                s_guiView = ScriptableObject.CreateInstance<TooltipView>();
            }

            if (s_guiView.window == null)
            {
                var newWindow = ScriptableObject.CreateInstance<ContainerWindow>();
                newWindow.m_DontSaveToLayout = true;
                newWindow.rootView = s_guiView;
                newWindow.SetMinMaxSizes(new Vector2(10.0f, 10.0f), new Vector2(2000.0f, 2000.0f));
                s_guiView.SetWindow(newWindow);
            }

            if (s_guiView.m_tooltip.text == tooltip && rect == s_guiView.m_hoverRect)
                return;

            s_guiView.Setup(tooltip, rect, hostView);
        }

        public static void Close()
        {
            if (s_guiView != null)
                s_guiView.window.Close();
        }

        public static void SetAlpha(float percent)
        {
            if (s_guiView != null)
                s_guiView.window.SetAlpha(percent);
        }
    }
}
