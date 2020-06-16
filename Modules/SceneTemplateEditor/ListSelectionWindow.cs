// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;

namespace UnityEditor.SceneTemplate
{
    internal class ListSelectionWindow : DropdownWindow<ListSelectionWindow>
    {
        private string[] m_Models;
        private Action<int> m_ElementSelectedHandler;
        private StringListView m_ListView;
        private string m_SearchValue;
        private bool m_SearchFieldGiveFocus;
        const string k_SearchField = "ListSelectionWindow_SearchField";

        public static void SelectionButton(Rect rect, Vector2 windowSize, GUIContent content, GUIStyle style, string[] models, Action<int> elementSelectedHandler)
        {
            DropDownButton(rect, windowSize, content, style, () =>
            {
                var window = CreateInstance<ListSelectionWindow>();
                window.InitWindow(models, elementSelectedHandler);
                return window;
            });
        }

        [UsedImplicitly]
        void OnEnable()
        {
            m_SearchFieldGiveFocus = true;
        }

        [UsedImplicitly]
        protected void OnDisable()
        {
        }

        [UsedImplicitly]
        internal void OnGUI()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Escape)
                {
                    Close();
                    m_ElementSelectedHandler(-1);
                }
                else if (Event.current.keyCode == KeyCode.DownArrow &&
                         GUI.GetNameOfFocusedControl() == k_SearchField)
                {
                    m_ListView.SetFocusAndEnsureSelectedItem();
                    Event.current.Use();
                }
                else if (Event.current.keyCode == KeyCode.UpArrow &&
                         m_ListView.HasFocus() &&
                         m_ListView.IsFirstItemSelected())
                {
                    EditorGUI.FocusTextInControl(k_SearchField);
                    Event.current.Use();
                }
            }

            EditorGUI.BeginChangeCheck();
            GUI.SetNextControlName(k_SearchField);
            m_SearchValue = EditorGUILayout.ToolbarSearchField(m_SearchValue);
            if (EditorGUI.EndChangeCheck())
            {
                m_ListView.searchString = m_SearchValue;
            }
            if (m_SearchFieldGiveFocus)
            {
                m_SearchFieldGiveFocus = false;
                GUI.FocusControl(k_SearchField);
            }

            var rect = EditorGUILayout.GetControlRect(false, GUILayout.ExpandHeight(true));
            m_ListView.OnGUI(rect);
        }

        private void InitWindow(string[] models, Action<int> elementSelectedHandler)
        {
            m_Models = models;
            m_ElementSelectedHandler = elementSelectedHandler;
            m_ListView = new StringListView(models);
            m_ListView.elementActivated += OnElementActivated;
        }

        private void OnElementActivated(int indexSelected)
        {
            Close();
            m_ElementSelectedHandler.Invoke(indexSelected);
        }
    }
}
