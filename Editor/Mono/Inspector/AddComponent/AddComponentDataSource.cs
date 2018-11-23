// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;

namespace UnityEditor.AddComponent
{
    internal class AddComponentDataSource : AdvancedDropdownDataSource
    {
        private static readonly string kSearchHeader = L10n.Tr("Search");
        AdvancedDropdownState m_State;

        public AddComponentDataSource(AdvancedDropdownState state)
        {
            m_State = state;
        }

        protected override AdvancedDropdownItem FetchData()
        {
            return RebuildTree();
        }

        protected AdvancedDropdownItem RebuildTree()
        {
            m_SearchableElements = new List<AdvancedDropdownItem>();
            AdvancedDropdownItem root = new ComponentDropdownItem("ROOT");
            var menuDictionary = GetMenuDictionary();
            menuDictionary.Sort(CompareItems);
            for (var i = 0; i < menuDictionary.Count; i++)
            {
                var menu = menuDictionary[i];
                if (menu.Value == "ADD")
                {
                    continue;
                }

                var menuPath = menu.Key;
                var paths = menuPath.Split('/');

                var parent = root;
                for (var j = 0; j < paths.Length; j++)
                {
                    var path = paths[j];
                    if (j == paths.Length - 1)
                    {
                        var element = new ComponentDropdownItem(path, menuPath, menu.Value);
                        element.localizedName = L10n.Tr(path);
                        parent.AddChild(element);
                        m_SearchableElements.Add(element);
                        continue;
                    }
                    var group = (ComponentDropdownItem)parent.children.SingleOrDefault(c => c.name == path);
                    if (group == null)
                    {
                        group = new ComponentDropdownItem(path);
                        group.localizedName = L10n.Tr(path);
                        parent.AddChild(group);
                    }
                    parent = group;
                }
            }
            root = root.children.Single();
            var newScript = new ComponentDropdownItem("New script");
            newScript.localizedName = L10n.Tr("New script");
            newScript.AddChild(new NewScriptDropdownItem());
            root.AddChild(newScript);
            return root;
        }

        private static List<KeyValuePair<string, string>> GetMenuDictionary()
        {
            var menus = Unsupported.GetSubmenus("Component");
            var commands = Unsupported.GetSubmenusCommands("Component");

            var menuDictionary = new Dictionary<string, string>(menus.Length);
            for (var i = 0; i < menus.Length; i++)
            {
                menuDictionary.Add(menus[i], commands[i]);
            }
            return menuDictionary.ToList();
        }

        private int CompareItems(KeyValuePair<string, string> x, KeyValuePair<string, string> y)
        {
            var legacyString = "legacy";
            var isStr1Legacy = x.Key.ToLower().Contains(legacyString);
            var isStr2Legacy = y.Key.ToLower().Contains(legacyString);
            if (isStr1Legacy && isStr2Legacy)
                return x.Key.CompareTo(y.Key);
            if (isStr1Legacy)
                return 1;
            if (isStr2Legacy)
                return -1;
            return x.Key.CompareTo(y.Key);
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
                var name = addComponentItem.searchableName.ToLower().Replace(" ", "");
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
                var addNewScriptGroup = new ComponentDropdownItem("New script");
                addNewScriptGroup.name = L10n.Tr("New script");
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
