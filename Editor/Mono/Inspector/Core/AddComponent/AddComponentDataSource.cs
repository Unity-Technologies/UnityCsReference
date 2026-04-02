// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine.Pool;
using Unity.Collections;

namespace UnityEditor.AddComponent
{
    internal class AddComponentDataSource : AdvancedDropdownDataSource
    {
        private static readonly string kSearchHeader = L10n.Tr("Search");
        AdvancedDropdownState m_State;
        UnityEngine.GameObject[] m_Targets;

        internal static readonly string kScriptHeader = "Component/Scripts/";

        public AddComponentDataSource(AdvancedDropdownState state, UnityEngine.GameObject[] targets)
        {
            m_State = state;
            m_Targets = targets;
        }

        protected override AdvancedDropdownItem FetchData()
        {
            return RebuildTree();
        }

        struct MenuItemData
        {
            public string path;
            public string command;
            public bool isLegacy;
        }

        protected AdvancedDropdownItem RebuildTree()
        {
            m_SearchableElements = new List<AdvancedDropdownItem>();
            AdvancedDropdownItem root = new ComponentDropdownItem("ROOT");
            List<MenuItemData> menuItems = GetSortedMenuItems(m_Targets);

            var pathHashCodeMap = DictionaryPool<KeyValuePair<string, int>, int>.Get();

            for (var i = 0; i < menuItems.Count; i++)
            {
                var menu = menuItems[i];
                if (menu.command == "ADD")
                {
                    continue;
                }

                var paths = menu.path.Split('/');

                var parent = root;
                for (var j = 0; j < paths.Length; j++)
                {
                    var path = paths[j];

                    if (j == paths.Length - 1)
                    {
                        var element = new ComponentDropdownItem(path, L10n.Tr(path), menu.path, menu.command, menu.isLegacy);
                        parent.AddChild(element);
                        m_SearchableElements.Add(element);
                        continue;
                    }

                    var pathSectionWithId = new KeyValuePair<string, int>(path, parent.id);
                    if (!pathHashCodeMap.TryGetValue(pathSectionWithId, out int pathHashCode))
                    {
                        pathHashCode = AdvancedDropdownItem.GenerateChildId(parent.id, path);
                        pathHashCodeMap[pathSectionWithId] = pathHashCode;
                    }

#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    var group = (ComponentDropdownItem)parent.childList.SingleOrDefault(c => c.id == pathHashCode);
#pragma warning restore UA2001
                    if (group == null)
                    {
                        group = new ComponentDropdownItem(path, L10n.Tr(path));
                        parent.AddChild(group);
                    }
                    parent = group;
                }
            }
            root = root.childList.Single();
            var newScript = new ComponentDropdownItem("New script", L10n.Tr("New script"));
            newScript.AddChild(new NewScriptDropdownItem());
            root.AddChild(newScript);
            DictionaryPool<KeyValuePair<string, int>, int>.Release(pathHashCodeMap);
            return root;
        }

        static List<MenuItemData> GetSortedMenuItems(UnityEngine.GameObject[] targets)
        {
            var menus = Unsupported.GetSubmenus("Component");
            var commands = Unsupported.GetSubmenusCommands("Component");

            var menuItems = new List<MenuItemData>(menus.Length);
            var legacyMenuItems = new List<MenuItemData>(menus.Length);
            const string kLegacyString = "legacy";

            var hasFilterOverride = ModeService.HasExecuteHandler("inspector_filter_component");

            for (var i = 0; i < menus.Length; i++)
            {
                var menuPath = menus[i];
                bool isLegacy = menuPath.ToLower().Contains(kLegacyString);
                var item = new MenuItemData
                {
                    path = menuPath,
                    command = commands[i],
                    isLegacy = isLegacy
                };

                if (!hasFilterOverride || ModeService.Execute("inspector_filter_component", targets, menuPath))
                {
                    if (isLegacy)
                    {
                        legacyMenuItems.Add(item);
                    }
                    else
                    {
                        menuItems.Add(item);
                    }
                }
            }

            int comparison(MenuItemData x, MenuItemData y) => string.CompareOrdinal(x.path, y.path);

            menuItems.Sort(comparison);
            legacyMenuItems.Sort(comparison);

            menuItems.AddRange(legacyMenuItems);

            return menuItems;
        }

        protected override AdvancedDropdownItem Search(string searchString)
        {
            if (string.IsNullOrEmpty(searchString) || m_SearchableElements == null)
                return null;

            // Support multiple search words separated by spaces.
            var searchWords = searchString.ToLower().Split(' ');

            // We keep two lists. Matches that matches the start of an item always get first priority.
            var matchesStart = new List<AdvancedDropdownItem>();
            var matchesWithin = new List<AdvancedDropdownItem>();

            bool found = false;
            foreach (var e in m_SearchableElements)
            {
                var addComponentItem = (ComponentDropdownItem)e;
                string name;
                if (addComponentItem.menuPath.StartsWith(kScriptHeader))
                    name = addComponentItem.menuPath.Remove(0, kScriptHeader.Length).ToLower().Replace(" ", "");
                else
                    name = addComponentItem.searchableName.ToLower().Replace(" ", "");

                if (AddMatchItem(e, name, searchWords, matchesStart, matchesWithin))
                    found = true;
            }
            if (!found)
            {
                foreach (var e in m_SearchableElements)
                {
                    var addComponentItem = (ComponentDropdownItem)e;
                    var name = addComponentItem.searchableNameLocalized.Replace(" ", "");
                    AddMatchItem(e, name, searchWords, matchesStart, matchesWithin);
                }
            }

            var searchTree = new AdvancedDropdownItem(kSearchHeader);
            matchesStart.Sort();
            foreach (var element in matchesStart)
            {
                searchTree.AddChild(element);
            }
            matchesWithin.Sort();
            foreach (var element in matchesWithin)
            {
                searchTree.AddChild(element);
            }
            if (searchTree != null)
            {
                var addNewScriptGroup = new ComponentDropdownItem("New script", L10n.Tr("New script"));
                m_State.SetSelectedIndex(addNewScriptGroup, 0);
                var addNewScript = new NewScriptDropdownItem();
                addNewScript.className = searchString;
                addNewScriptGroup.AddChild(addNewScript);
                searchTree.AddChild(addNewScriptGroup);
            }
            return searchTree;
        }
    }
}
