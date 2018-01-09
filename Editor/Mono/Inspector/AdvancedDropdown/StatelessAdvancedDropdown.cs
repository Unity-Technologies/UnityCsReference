// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    internal static class StatelessAdvancedDropdown
    {
        private static AdvancedDropdownWindow s_Instance;
        private static EditorWindow s_ParentWindow;
        private static bool m_WindowClosed;
        private static int m_Result;
        private static int s_CurrentControl;

        internal static void SearchablePopup(Rect rect, string label, int selectedIndex, string[] displayedOptions,
            Action<string> onSelected)
        {
            ResetAndCreateWindow();
            InitWindow(rect, label, selectedIndex, displayedOptions);

            s_Instance.onSelected += (w) => onSelected(w.GetIdOfSelectedItem());
        }

        public static int SearchablePopup(Rect rect, int selectedIndex, string[] displayedOptions)
        {
            return SearchablePopup(rect, selectedIndex, displayedOptions, "MiniPullDown");
        }

        public static int SearchablePopup(Rect rect, int selectedIndex, string[] displayedOptions, GUIStyle style)
        {
            string contentLabel = null;
            if (selectedIndex >= 0)
            {
                contentLabel = displayedOptions[selectedIndex];
            }

            var content = new GUIContent(contentLabel);

            int id =  EditorGUIUtility.GetControlID("AdvancedDropdown".GetHashCode(), FocusType.Keyboard, rect);
            if (EditorGUI.DropdownButton(id, rect, content, "MiniPullDown"))
            {
                s_CurrentControl = id;
                ResetAndCreateWindow();
                InitWindow(rect, content.text, selectedIndex, displayedOptions);

                s_Instance.onSelected += w =>
                    {
                        m_Result = w.GetSelectedIndex();
                        m_WindowClosed = true;
                    };
            }
            if (m_WindowClosed && s_CurrentControl == id)
            {
                s_CurrentControl = 0;
                m_WindowClosed = false;
                return m_Result;
            }

            return selectedIndex;
        }

        private static void ResetAndCreateWindow()
        {
            if (s_Instance != null)
            {
                s_Instance.Close();
                s_Instance = null;
            }
            s_ParentWindow = EditorWindow.focusedWindow;
            s_Instance = ScriptableObject.CreateInstance<AdvancedDropdownWindow>();
            m_WindowClosed = false;
        }

        private static void InitWindow(Rect rect, string label, int selectedIndex, string[] displayedOptions)
        {
            var dataSource = new AdvancedDropdownSimpleDataSource();

            dataSource.DisplayedOptions = displayedOptions;
            dataSource.SelectedIndex = selectedIndex;
            dataSource.Label = label;

            s_Instance.dataSource = dataSource;

            s_Instance.onSelected += (w) =>
                {
                    if (s_ParentWindow != null)
                        s_ParentWindow.Repaint();
                };

            s_Instance.Init(rect);
        }
    }
}
