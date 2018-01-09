// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditor
{
    [InitializeOnLoad]
    internal class AddComponentWindow : AdvancedDropdownWindow
    {
        [Serializable]
        internal class AnalyticsEventData
        {
            public string name;
            public string filter;
            public bool isNewScript;
        }

        internal static AddComponentWindow s_AddComponentWindow = null;

        internal GameObject[] m_GameObjects;
        internal string searchString => m_Search;

        private DateTime m_ComponentOpenTime;
        private const string kComponentSearch = "ComponentSearchString";

        public const string OpenAddComponentDropdown = "OpenAddComponentDropdown";

        protected override bool isSearchFieldDisabled
        {
            get
            {
                var child = m_CurrentlyRenderedTree.GetSelectedChild();
                if (child != null)
                    return child is NewScriptDropdownItem;
                return false;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            gui = new AddComponentGUI();
            dataSource = new AddComponentDataSource();
            s_AddComponentWindow = this;
            m_Search = EditorPrefs.GetString(kComponentSearch, "");
            showHeader = true;
        }

        protected override void OnDisable()
        {
            s_AddComponentWindow = null;
            EditorPrefs.SetString(kComponentSearch, m_Search);
        }

        internal static bool Show(Rect rect, GameObject[] gos)
        {
            CloseAllOpenWindows<AddComponentWindow>();

            Event.current.Use();
            s_AddComponentWindow = CreateAndInit<AddComponentWindow>(rect);
            s_AddComponentWindow.m_GameObjects = gos;
            s_AddComponentWindow.m_ComponentOpenTime = DateTime.UtcNow;
            return true;
        }

        protected override bool SpecialKeyboardHandling(Event evt)
        {
            var createScriptMenu = m_CurrentlyRenderedTree.GetSelectedChild();
            if (createScriptMenu is NewScriptDropdownItem)
            {
                // When creating new script name we want to dedicate both left/right arrow and backspace
                // to editing the script name so they can't be used for navigating the menus.
                // The only way to get back using the keyboard is pressing Esc.
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    createScriptMenu.OnAction();
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

        internal static void SendUsabilityAnalyticsEvent(AnalyticsEventData eventData)
        {
            var openTime = s_AddComponentWindow.m_ComponentOpenTime;
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
                insp.SendEvent(EditorGUIUtility.CommandEvent(OpenAddComponentDropdown));
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
