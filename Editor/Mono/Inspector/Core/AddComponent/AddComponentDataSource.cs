// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine.Pool;
using Unity.Collections;

namespace UnityEditor.AddComponent
{
    internal class AddComponentDataSource : AdvancedDropdownDataSource
    {
        private static readonly string kSearchHeader = L10n.Tr("Search", null);
        AdvancedDropdownState m_State;
        UnityEngine.GameObject[] m_Targets;

        internal static readonly string kScriptHeader = "Component/Scripts/";
        internal static readonly string kNewScriptGroupName = "New script";

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
            public int priority;
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
                var paths = menu.path.Split('/');

                var parent = root;
                for (var j = 0; j < paths.Length; j++)
                {
                    var path = paths[j];

                    if (j == paths.Length - 1)
                    {
                        var element = new ComponentDropdownItem(path, L10n.Tr(path, null), menu.path, menu.command, menu.isLegacy);
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

#pragma warning disable UAC2001 // Avoid Linq
                    var group = (ComponentDropdownItem)parent.childList.SingleOrDefault(c => c.id == pathHashCode);
#pragma warning restore UAC2001
                    if (group == null)
                    {
                        group = new ComponentDropdownItem(path, L10n.Tr(path, null));
                        parent.AddChild(group);
                    }
                    parent = group;
                }
            }
            root = root.childList.Single();
            AddNewScriptGroup(root);
            DictionaryPool<KeyValuePair<string, int>, int>.Release(pathHashCodeMap);
            return root;
        }

        void AddNewScriptGroup(AdvancedDropdownItem parent, string className = null)
        {
            var newScriptGroup = new ComponentDropdownItem(kNewScriptGroupName, L10n.Tr("New script", null));
            var newScript = new NewScriptDropdownItem();
            if (className != null)
                newScript.className = className;
            newScriptGroup.AddChild(newScript);
            parent.AddChild(newScriptGroup);
            // Seed after AddChild: AddChild assigns the group's final id and the state is keyed by id.
            m_State.SetSelectedIndex(newScriptGroup, 0);
        }

        static List<MenuItemData> GetSortedMenuItems(UnityEngine.GameObject[] targets)
        {
            var menus = Menu.GetMenuItems("Component", false, false);
            var commands = Unsupported.GetSubmenusCommands("Component");

            var menuItems = new List<MenuItemData>(menus.Length);
            var legacyMenuItems = new List<MenuItemData>(menus.Length);
            const string kLegacyString = "legacy";

            var hasFilterOverride = ModeService.HasExecuteHandler("inspector_filter_component");

            for (var i = 0; i < menus.Length; i++)
            {
                // "Component/Add..." opens this dropdown, so it is not listed.
                if (commands[i] == "ADD")
                    continue;

                var menuPath = menus[i].path;
                bool isLegacy = menuPath.ToLower().Contains(kLegacyString);
                var item = new MenuItemData
                {
                    path = menuPath,
                    command = commands[i],
                    isLegacy = isLegacy,
                    priority = menus[i].priority
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

            menuItems.Sort(CompareMenuItems);
            legacyMenuItems.Sort(CompareMenuItems);

            menuItems.AddRange(legacyMenuItems);

            return menuItems;
        }

        // Orders the items of each menu level with the submenus first, sorted alphabetically, followed by the components,
        // sorted by menu priority (AddComponentMenu.componentOrder) and then alphabetically.
        // Paths are compared at their first differing segment, which keeps the items of a submenu together.
        static int CompareMenuItems(MenuItemData x, MenuItemData y)
        {
            var xPath = x.path;
            var yPath = y.path;
            var xStart = 0;
            var yStart = 0;
            while (true)
            {
                var xEnd = xPath.IndexOf('/', xStart);
                var xIsComponent = xEnd < 0;
                if (xIsComponent)
                    xEnd = xPath.Length;

                var yEnd = yPath.IndexOf('/', yStart);
                var yIsComponent = yEnd < 0;
                if (yIsComponent)
                    yEnd = yPath.Length;

                if (xIsComponent != yIsComponent)
                    return xIsComponent ? 1 : -1;

                if (xIsComponent)
                {
                    var priorityComparison = x.priority.CompareTo(y.priority);
                    if (priorityComparison != 0)
                        return priorityComparison;
                }

                var xLength = xEnd - xStart;
                var yLength = yEnd - yStart;
                var nameComparison = string.CompareOrdinal(xPath, xStart, yPath, yStart, Math.Min(xLength, yLength));
                if (nameComparison == 0)
                    nameComparison = xLength.CompareTo(yLength);

                if (nameComparison != 0)
                    return nameComparison;

                // Components with the same name and priority, compared by command to keep the order deterministic.
                if (xIsComponent)
                    return string.CompareOrdinal(x.command, y.command);

                xStart = xEnd + 1;
                yStart = yEnd + 1;
            }
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
            AddNewScriptGroup(searchTree, searchString);
            return searchTree;
        }
    }
}
