// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor.Search
{
    class SimpleListSelectionWindow : DropdownWindow<SimpleListSelectionWindow>
    {
        public struct Config
        {
            public Action<TreeView, TreeViewItem, object, string, Rect, int, bool, bool> drawRow;
            public Func<string, string, string> drawSearchField;
            public bool showSearchField;
            public string initialSearchFieldValue;
            public Action<TreeView> listInit;
            public float rowWidth;
            public float rowHeight;
            public float windowHeight;
            public IList models;
            public int initialSelectionIndex;
            public object initialSelectedModel;
            public Action<int> elementSelectedHandler;

            public Config(IList models, Action<int> elementSelectedHandler)
            {
                showSearchField = true;
                this.models = models;
                this.elementSelectedHandler = elementSelectedHandler;
                initialSearchFieldValue = null;
                drawRow = null;
                drawSearchField = null;
                initialSelectedModel = null;
                listInit = null;
                rowHeight = 0;
                initialSelectionIndex = -1;
                windowHeight = 200;
                rowWidth = 200;
            }
        }

        const string k_SearchField = "ItemSelectionWindowSearchField";

        private Action<int> m_ElementSelectedHandler;
        private SimpleListView m_ListView;
        private bool m_InitialDisplay;
        private string m_SearchValue;
        private Config m_Config;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_InitialDisplay = true;
        }

        public static void SelectionButton(Rect rect, GUIContent content, GUIStyle style, Config config)
        {
            DropDownButton(rect, content, style, () => SetupWindow(config));
        }

        public static void SelectionButton(Rect rect, GUIContent content, GUIStyle style, Func<Config> getConfig)
        {
            DropDownButton(rect, content, style, () => SetupWindow(getConfig()));
        }

        public static void CheckShowWindow(Rect rect, Config config)
        {
            CheckShowWindow(rect, () => SetupWindow(config));
        }

        public static void CheckShowWindow(Rect rect, Func<Config> getConfig)
        {
            CheckShowWindow(rect, () => SetupWindow(getConfig()));
        }

        internal void OnGUI()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Escape)
                {
                    Event.current.Use();
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

            if (m_InitialDisplay && (m_Config.initialSelectionIndex != -1 || m_Config.initialSelectedModel != null))
            {
                if (m_Config.initialSelectedModel != null)
                {
                    m_Config.initialSelectionIndex = m_Config.models.IndexOf(m_Config.initialSelectedModel);
                }

                if (m_Config.initialSelectionIndex != -1)
                {
                    var selection = new List<int>() { m_Config.initialSelectionIndex + 1 };
                    m_ListView.FrameItem(m_Config.initialSelectionIndex + 1);
                    m_ListView.SetSelection(selection);
                    m_ListView.SetFocusAndEnsureSelectedItem();
                }
            }

            if (m_Config.showSearchField)
            {
                EditorGUI.BeginChangeCheck();
                if (m_Config.drawSearchField != null)
                {
                    m_SearchValue = m_Config.drawSearchField(k_SearchField, m_SearchValue);
                }
                else
                {
                    GUI.SetNextControlName(k_SearchField);
                    m_SearchValue = SearchField(m_SearchValue);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    m_ListView.searchString = m_SearchValue;
                }
                if (m_InitialDisplay)
                {
                    m_InitialDisplay = false;
                    GUI.FocusControl(k_SearchField);
                }
            }
            else if (m_InitialDisplay)
            {
                m_ListView.SetFocusAndEnsureSelectedItem();
            }

            var rect = EditorGUILayout.GetControlRect(false, GUILayout.ExpandHeight(true));
            m_ListView.OnGUI(rect);

            m_InitialDisplay = false;
        }

        private static SimpleListSelectionWindow SetupWindow(Config config)
        {
            var window = CreateInstance<SimpleListSelectionWindow>();
            window.position = new Rect(window.position.x, window.position.y, config.rowWidth, config.windowHeight);
            window.InitWindow(config);
            return window;
        }

        private void InitWindow(Config config)
        {
            m_Config = config;
            m_SearchValue = m_Config.initialSearchFieldValue;
            m_ElementSelectedHandler = config.elementSelectedHandler;
            m_ListView = new SimpleListView(config.models, config.rowHeight, config.drawRow);
            if (!string.IsNullOrEmpty(m_Config.initialSearchFieldValue))
            {
                m_ListView.searchString = m_Config.initialSearchFieldValue;
            }

            config.listInit?.Invoke(m_ListView);
            m_ListView.elementActivated += OnElementActivated;
        }

        private void OnElementActivated(int indexSelected)
        {
            Close();
            m_ElementSelectedHandler?.Invoke(indexSelected);
        }

        static MethodInfo ToolbarSearchField;
        private static string SearchField(string value, params GUILayoutOption[] options)
        {
            if (ToolbarSearchField == null)
            {
                ToolbarSearchField = typeof(EditorGUILayout).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).First(mi => mi.Name == "ToolbarSearchField" && mi.GetParameters().Length == 2);
            }

            return ToolbarSearchField.Invoke(null, new[] { value, (object)options }) as string;
        }
    }
}
