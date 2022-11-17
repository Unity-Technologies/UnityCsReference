// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditor.AddComponent
{
    [InitializeOnLoad]
    internal class AddComponentWindow : AdvancedDropdownWindow
    {
        internal const string OpenAddComponentDropdown = "OpenAddComponentDropdown";
        [Serializable]
        internal class AnalyticsEventData
        {
            public string name;
            public string filter;
            public bool isNewScript;
        }

        private GameObject[] m_GameObjects;
        private DateTime m_ComponentOpenTime;
        private const string kComponentSearch = "ComponentSearchString";
        private const int kMaxWindowHeight = 395 - 80;
        private static AdvancedDropdownState s_State = new AdvancedDropdownState();

        protected override bool setInitialSelectionPosition { get; } = false;

        protected override bool isSearchFieldDisabled
        {
            get
            {
                var child = state.GetSelectedChild(renderedTreeItem);
                if (child != null)
                    return child is NewScriptDropdownItem;
                return false;
            }
        }

        internal static bool Show(Rect rect, GameObject[] gos)
        {
            CloseAllOpenWindows<AddComponentWindow>();
            var window = CreateInstance<AddComponentWindow>();
            window.dataSource = new AddComponentDataSource(s_State);
            window.gui = new AddComponentGUI(window.dataSource, window.OnCreateNewScript);
            window.state = s_State;
            window.m_GameObjects = gos;
            window.m_ComponentOpenTime = DateTime.UtcNow;
            window.Init(rect);
            window.searchString = EditorPrefs.GetString(kComponentSearch, "");
            return true;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            showHeader = true;
            selectionChanged += OnItemSelected;
        }

        private void OnItemSelected(AdvancedDropdownItem item)
        {
            if (item is ComponentDropdownItem)
            {
                SendUsabilityAnalyticsEvent(new AnalyticsEventData
                {
                    name = item.name,
                    filter = searchString,
                    isNewScript = false
                });

                var gos = m_GameObjects;
                EditorApplication.ExecuteMenuItemOnGameObjects(((ComponentDropdownItem)item).menuPath, gos);
            }
        }

        protected override void OnDisable()
        {
            EditorPrefs.SetString(kComponentSearch, searchString);
        }

        protected override Vector2 CalculateWindowSize(Rect buttonRect)
        {
            return new Vector2(buttonRect.width, kMaxWindowHeight);
        }

        protected override bool SpecialKeyboardHandling(Event evt)
        {
            var createScriptMenu = state.GetSelectedChild(renderedTreeItem);
            if (createScriptMenu is NewScriptDropdownItem)
            {
                // When creating new script name we want to dedicate both left/right arrow and backspace
                // to editing the script name so they can't be used for navigating the menus.
                // The only way to get back using the keyboard is pressing Esc.
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    OnCreateNewScript((NewScriptDropdownItem)createScriptMenu);
                    evt.Use();
                    GUIUtility.ExitGUI();
                }

                if (evt.keyCode == KeyCode.Escape)
                {
                    GoToParent();
                    evt.Use();
                }

                return true;
            }
            return false;
        }

        void OnCreateNewScript(NewScriptDropdownItem item)
        {
            item.Create(m_GameObjects, searchString);
            SendUsabilityAnalyticsEvent(new AnalyticsEventData
            {
                name = item.className,
                filter = searchString,
                isNewScript = true
            });
            Close();
        }

        internal void SendUsabilityAnalyticsEvent(AnalyticsEventData eventData)
        {
            var openTime = m_ComponentOpenTime;
            UsabilityAnalytics.SendEvent("executeAddComponentWindow", openTime, DateTime.UtcNow - openTime, false, eventData);
        }

        [UsedByNativeCode]
        internal static bool ValidateAddComponentMenuItem()
        {
            if (FirstInspectorWithGameObject() != null)
                return true;
            return false;
        }

        [UsedByNativeCode]
        internal static void ExecuteAddComponentMenuItem()
        {
            var insp = FirstInspectorWithGameObject();
            if (insp != null)
            {
                insp.ShowTab();
                insp.m_OpenAddComponentMenu = true;
            }
        }

        private static InspectorWindow FirstInspectorWithGameObject()
        {
            foreach (var insp in InspectorWindow.GetInspectors())
                if (insp.GetInspectedObject() is GameObject)
                    return insp;
            return null;
        }
    }
}
