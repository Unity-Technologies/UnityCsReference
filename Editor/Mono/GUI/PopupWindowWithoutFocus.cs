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
    class PopupWindowWithoutFocus : EditorWindow
    {
        static PopupWindowWithoutFocus s_PopupWindowWithoutFocus;
        static double s_LastClosedTime;
        static Rect s_LastActivatorRect;

        PopupWindowContent m_WindowContent;
        PopupLocationHelper.PopupLocation[] m_LocationPriorityOrder;
        Vector2 m_LastWantedSize = Vector2.zero;
        Rect m_ActivatorRect;
        float m_BorderWidth = 1f;

        public static void Show(Rect activatorRect, PopupWindowContent windowContent)
        {
            Show(activatorRect, windowContent, null);
        }

        public static bool IsVisible()
        {
            return s_PopupWindowWithoutFocus != null;
        }

        internal static void Show(Rect activatorRect, PopupWindowContent windowContent, PopupLocationHelper.PopupLocation[] locationPriorityOrder)
        {
            if (ShouldShowWindow(activatorRect))
            {
                if (s_PopupWindowWithoutFocus == null)
                    s_PopupWindowWithoutFocus = CreateInstance<PopupWindowWithoutFocus>();

                s_PopupWindowWithoutFocus.Init(activatorRect, windowContent, locationPriorityOrder);
            }
        }

        public static void Hide()
        {
            if (s_PopupWindowWithoutFocus != null)
                s_PopupWindowWithoutFocus.Close();
        }

        void Init(Rect activatorRect, PopupWindowContent windowContent, PopupLocationHelper.PopupLocation[] locationPriorityOrder)
        {
            m_WindowContent = windowContent;
            m_WindowContent.editorWindow = this;
            m_ActivatorRect = GUIUtility.GUIToScreenRect(activatorRect);
            m_LastWantedSize = windowContent.GetWindowSize();
            m_LocationPriorityOrder = locationPriorityOrder;

            Vector2 windowSize = windowContent.GetWindowSize() + new Vector2(m_BorderWidth * 2, m_BorderWidth * 2);
            position = PopupLocationHelper.GetDropDownRect(m_ActivatorRect, windowSize, windowSize, null, m_LocationPriorityOrder);
            ShowPopup();
            Repaint();
        }

        void OnEnable()
        {
            hideFlags = HideFlags.DontSave;
            s_PopupWindowWithoutFocus = this;
        }

        void OnDisable()
        {
            s_LastClosedTime = EditorApplication.timeSinceStartup;
            if (m_WindowContent != null)
                m_WindowContent.OnClose();
            s_PopupWindowWithoutFocus = null;
        }

        // Invoked from C++
        static bool OnGlobalMouseOrKeyEvent(EventType type, KeyCode keyCode, Vector2 mousePosition)
        {
            if (s_PopupWindowWithoutFocus == null)
                return false;

            if (type == EventType.KeyDown && keyCode == KeyCode.Escape)
            {
                s_PopupWindowWithoutFocus.Close();
                return true;
            }

            if (type == EventType.MouseDown && !s_PopupWindowWithoutFocus.position.Contains(mousePosition))
            {
                s_PopupWindowWithoutFocus.Close();
                return true;
            }

            return false;
        }

        static bool ShouldShowWindow(Rect activatorRect)
        {
            const double kJustClickedTime = 0.2;
            bool justClosed = (EditorApplication.timeSinceStartup - s_LastClosedTime) < kJustClickedTime;
            if (!justClosed || activatorRect != s_LastActivatorRect)
            {
                s_LastActivatorRect = activatorRect;
                return true;
            }
            return false;
        }

        internal void OnGUI()
        {
            FitWindowToContent();
            Rect windowRect = new Rect(m_BorderWidth, m_BorderWidth, position.width - 2 * m_BorderWidth, position.height - 2 * m_BorderWidth);
            m_WindowContent.OnGUI(windowRect);
            GUI.Label(new Rect(0, 0, position.width, position.height), GUIContent.none, "grey_border");
        }

        private void FitWindowToContent()
        {
            Vector2 wantedSize = m_WindowContent.GetWindowSize();
            if (m_LastWantedSize != wantedSize)
            {
                m_LastWantedSize = wantedSize;
                Vector2 windowSize = wantedSize + new Vector2(2 * m_BorderWidth, 2 * m_BorderWidth);
                Rect screenRect = PopupLocationHelper.GetDropDownRect(m_ActivatorRect, windowSize, windowSize, null, m_LocationPriorityOrder);
                m_Pos = screenRect;
                minSize = maxSize = new Vector2(screenRect.width, screenRect.height);
            }
        }
    }
}
