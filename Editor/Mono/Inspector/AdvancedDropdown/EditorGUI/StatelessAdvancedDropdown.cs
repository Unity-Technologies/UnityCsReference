// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.AdvancedDropdown;
using UnityEngine;

namespace UnityEditor
{
    internal static partial class StatelessAdvancedDropdown
    {
        private static AdvancedDropdownWindow s_Instance;
        private static EditorWindow s_ParentWindow;
        private static bool m_WindowClosed;
        private static bool m_ShouldReturnValue;
        private static int m_Result;
        private static int s_CurrentControl;
        private static MultiselectDataSource s_DataSource;

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

        private static void InitSearchableWindow(Rect rect, string label, int selectedIndex, string[] displayedOptions)
        {
            var dataSource = new MultiLevelDataSource();

            dataSource.displayedOptions = displayedOptions;
            dataSource.selectedIndex = selectedIndex;
            dataSource.label = label;

            s_Instance.dataSource = dataSource;

            s_Instance.windowClosed += (w) =>
            {
                if (s_ParentWindow != null)
                    s_ParentWindow.Repaint();
            };

            s_Instance.Init(rect);
        }

        private static void InitPopupWindow(Rect rect, int selectedIndex, GUIContent[] displayedOptions)
        {
            var dataSource = new SimpleDataSource();

            dataSource.displayedOptions = displayedOptions;
            dataSource.selectedIndex = selectedIndex;

            s_Instance.dataSource = dataSource;
            s_Instance.searchable = false;
            s_Instance.showHeader = false;

            s_Instance.selectionChanged += (w) =>
            {
                if (s_ParentWindow != null)
                    s_ParentWindow.Repaint();
            };

            s_Instance.Init(rect);
        }

        private static void InitMultiselectPopupWindow(Rect rect, MultiselectDataSource dataSource)
        {
            s_DataSource = dataSource;
            s_Instance.dataSource = dataSource;
            s_Instance.showHeader = false;
            s_Instance.searchable = false;
            s_Instance.closeOnSelection = false;
            s_Instance.selectionChanged += (w) =>
            {
                if (s_ParentWindow != null)
                    s_ParentWindow.Repaint();
            };

            s_Instance.Init(rect);
        }

        internal static int DoSearchablePopup(Rect rect, int selectedIndex, string[] displayedOptions, GUIStyle style)
        {
            string contentLabel = null;
            if (selectedIndex >= 0)
            {
                contentLabel = displayedOptions[selectedIndex];
            }

            var content = new GUIContent(contentLabel);

            int id =  EditorGUIUtility.GetControlID("AdvancedDropdown".GetHashCode(), FocusType.Keyboard, rect);
            if (EditorGUI.DropdownButton(id, rect, content, style))
            {
                s_CurrentControl = id;
                ResetAndCreateWindow();
                InitSearchableWindow(rect, content.text, selectedIndex, displayedOptions);

                s_Instance.windowClosed += w =>
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

        public static int DoPopup(Rect rect, int selectedIndex, GUIContent[] displayedOptions)
        {
            GUIContent content = GUIContent.none;
            if (selectedIndex >= 0 && selectedIndex < displayedOptions.Length)
                content = displayedOptions[selectedIndex];

            int id = EditorGUIUtility.GetControlID("AdvancedDropdown".GetHashCode(), FocusType.Keyboard, rect);
            if (EditorGUI.DropdownButton(id, rect, content, EditorStyles.popup))
            {
                s_CurrentControl = id;
                ResetAndCreateWindow();
                InitPopupWindow(rect, selectedIndex, displayedOptions);

                s_Instance.windowClosed += w =>
                {
                    m_Result = w.GetSelectedIndex();
                    m_WindowClosed = true;
                };
                GUIUtility.ExitGUI();
            }
            if (m_WindowClosed && s_CurrentControl == id)
            {
                s_CurrentControl = 0;
                m_WindowClosed = false;
                return m_Result;
            }

            return selectedIndex;
        }

        public static Enum DoEnumMaskPopup(Rect rect, Enum options, GUIStyle style)
        {
            var enumData = EditorGUI.GetCachedEnumData(options.GetType());
            var optionValue = EditorGUI.EnumFlagsToInt(enumData, options);
            string buttonText;
            string[] optionNames;
            int[] optionMaskValues;
            int[] selectedOptions;
            MaskFieldGUI.GetMenuOptions(optionValue, enumData.displayNames, enumData.flagValues, out buttonText, out optionNames, out optionMaskValues, out selectedOptions);

            var id = EditorGUIUtility.GetControlID("AdvancedDropdown".GetHashCode(), FocusType.Keyboard, rect);

            if (EditorGUI.DropdownButton(id, rect, GUIContent.Temp(buttonText), EditorStyles.popup))
            {
                s_CurrentControl = id;
                ResetAndCreateWindow();
                InitMultiselectPopupWindow(rect, new MultiselectDataSource(options));

                s_Instance.selectionChanged += i =>
                {
                    m_ShouldReturnValue = true;
                };
                s_Instance.windowClosed += w =>
                {
                    m_WindowClosed = true;
                };
            }

            if (m_ShouldReturnValue && s_CurrentControl == id)
            {
                m_ShouldReturnValue = false;
                return s_DataSource.enumFlags;
            }
            if (m_WindowClosed && s_CurrentControl == id)
            {
                s_CurrentControl = 0;
                m_WindowClosed = false;
                var result = s_DataSource.enumFlags;
                s_DataSource = null;
                return result;
            }
            return options;
        }

        private static Enum DoEnumPopup(Rect rect, Enum selected, GUIStyle style, params GUILayoutOption[] options)
        {
            var enumType = selected.GetType();
            bool localize = EditorUtility.IsUnityAssembly(enumType);
            var enumData = EditorGUI.GetCachedEnumData(enumType);
            var i = Array.IndexOf(enumData.values, selected);
            i = DoPopup(rect, i, localize ? EditorGUIUtility.TrTempContent(enumData.displayNames, enumData.tooltip) : EditorGUIUtility.TempContent(enumData.displayNames, enumData.tooltip));
            return (i < 0 || i >= enumData.flagValues.Length) ? selected : enumData.values[i];
        }

        private static int DoIntPopup(Rect rect, int selectedValue, string[] displayedOptions, int[] optionValues)
        {
            var idx = Array.IndexOf(optionValues, selectedValue);
            var returnedValue = DoPopup(rect, idx, EditorGUIUtility.TempContent(displayedOptions));
            return returnedValue >= 0 ? optionValues[returnedValue] : returnedValue;
        }

        static int DoMaskField(Rect rect, int mask, string[] displayedOptions, GUIStyle popup)
        {
            var flagValues = new int[displayedOptions.Length];
            for (int i = 0; i < flagValues.Length; ++i)
                flagValues[i] = (1 << i);

            string buttonText;
            string[] optionNames;
            int[] optionMaskValues;
            int[] selectedOptions;
            MaskFieldGUI.GetMenuOptions(mask, displayedOptions, flagValues, out buttonText, out optionNames, out optionMaskValues, out selectedOptions);

            var id = EditorGUIUtility.GetControlID("AdvancedDropdown".GetHashCode(), FocusType.Keyboard, rect);

            if (EditorGUI.DropdownButton(id, rect, GUIContent.Temp(buttonText), EditorStyles.popup))
            {
                s_CurrentControl = id;
                ResetAndCreateWindow();
                InitMultiselectPopupWindow(rect, new MultiselectDataSource(mask, displayedOptions, flagValues));

                s_Instance.selectionChanged += i =>
                {
                    m_ShouldReturnValue = true;
                };
                s_Instance.windowClosed += w =>
                {
                    m_WindowClosed = true;
                };
            }

            if (m_ShouldReturnValue && s_CurrentControl == id)
            {
                m_ShouldReturnValue = false;
                return s_DataSource.mask;
            }
            if (m_WindowClosed && s_CurrentControl == id)
            {
                s_CurrentControl = 0;
                m_WindowClosed = false;
                var result = s_DataSource.mask;
                s_DataSource = null;
                return result;
            }
            return mask;
        }
    }
}
