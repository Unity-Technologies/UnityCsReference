// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;

namespace UnityEditor
{
    internal class AddComponentDataSource : AdvancedDropdownDataSource
    {
        protected override AdvancedDropdownItem FetchData()
        {
            return RebuildTree();
        }

        protected AdvancedDropdownItem RebuildTree()
        {
            AdvancedDropdownItem root = new ComponentDropdownItem();
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
                        var element = new ComponentDropdownItem(LocalizationDatabase.GetLocalizedString(path), menuPath, menu.Value);
                        element.searchable = true;
                        element.SetParent(parent);
                        parent.AddChild(element);
                        continue;
                    }
                    var group = parent.children.SingleOrDefault(c => c.name == path);
                    if (group == null)
                    {
                        group = new ComponentDropdownItem(path, -1);
                        group.SetParent(parent);
                        parent.AddChild(group);
                    }
                    parent = group;
                }
            }
            root = root.children.Single();
            root.SetParent(null);
            var newScript = new ComponentDropdownItem("New script", -1);
            newScript.AddChild(new NewScriptDropdownItem());
            newScript.SetParent(root);
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
            var searchTree = base.Search(searchString);
            if (searchTree != null)
            {
                var addNewScriptGroup = new ComponentDropdownItem("New script", -1);
                var addNewScript = new NewScriptDropdownItem();
                addNewScript.className = searchString;
                addNewScriptGroup.AddChild(addNewScript);
                addNewScript.SetParent(addNewScriptGroup);
                addNewScriptGroup.SetParent(searchTree);
                searchTree.AddChild(addNewScriptGroup);
            }
            return searchTree;
        }
    }
}
