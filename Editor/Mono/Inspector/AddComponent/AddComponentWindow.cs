// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditorInternal;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;

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
                var child = CurrentlyRenderedTree.GetSelectedChild();
                if (child != null)
                    return child is NewScriptDropdownElement;
                return false;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            s_AddComponentWindow = this;
            m_Search = EditorPrefs.GetString(kComponentSearch, "");
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
            var createScriptMenu = CurrentlyRenderedTree.GetSelectedChild();
            if (createScriptMenu is NewScriptDropdownElement)
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

        protected override DropdownElement RebuildTree()
        {
            var root = new DropdownElement("ROOT");
            var menus = Unsupported.GetSubmenus("Component");
            var commands = Unsupported.GetSubmenusCommands("Component");
            for (var i = 0; i < menus.Length; i++)
            {
                if (commands[i] == "ADD")
                {
                    continue;
                }

                var menuPath = menus[i];
                var paths = menuPath.Split('/');

                var parent = root;
                for (var j = 0; j < paths.Length; j++)
                {
                    var path = paths[j];
                    if (j == paths.Length - 1)
                    {
                        var element = new ComponentDropdownElement(LocalizationDatabase.GetLocalizedString(path), menuPath, commands[i]);
                        element.SetParent(parent);
                        parent.AddChild(element);
                        continue;
                    }
                    var group = parent.children.SingleOrDefault(c => c.name == path);
                    if (group == null)
                    {
                        group = new GroupDropdownElement(path);
                        group.SetParent(parent);
                        parent.AddChild(group);
                    }
                    parent = group;
                }
            }
            root = root.children.Single();
            root.SetParent(null);
            var newScript = new GroupDropdownElement("New script");
            newScript.AddChild(new NewScriptDropdownElement());
            newScript.SetParent(root);
            root.AddChild(newScript);
            return root;
        }

        protected override DropdownElement RebuildSearch()
        {
            var searchTree = base.RebuildSearch();

            if (searchTree != null)
            {
                var addNewScriptGroup = new GroupDropdownElement("New script");
                var addNewScript = new NewScriptDropdownElement();
                addNewScript.className = m_Search;
                addNewScriptGroup.AddChild(addNewScript);
                addNewScript.SetParent(addNewScriptGroup);
                addNewScriptGroup.SetParent(searchTree);
                searchTree.AddChild(addNewScriptGroup);
            }
            return searchTree;
        }

        internal static void SendUsabilityAnalyticsEvent(AnalyticsEventData eventData)
        {
            var openTime = s_AddComponentWindow.m_ComponentOpenTime;
            UsabilityAnalytics.SendEvent("executeAddComponentWindow", openTime, DateTime.UtcNow - openTime, false, eventData);
        }

        private static void CloseAllOpenWindows<T>()
        {
            var windows = Resources.FindObjectsOfTypeAll(typeof(T));
            foreach (var window in windows)
            {
                try
                {
                    ((EditorWindow)window).Close();
                }
                catch
                {
                    DestroyImmediate(window);
                }
            }
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
