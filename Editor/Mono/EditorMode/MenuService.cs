// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine.Scripting;
using UnityEngine;

using JSONObject = System.Collections.IDictionary;

using static UnityEditor.ModeService;
using UnityEditor.Scripting.ScriptCompilation;

namespace UnityEditor
{
    static class MenuService
    {
        private const string k_WindowMenuName = "Window";
        private const string k_HelpMenuName = "Help";

        // Contains the final ordered menus that can come either from the .mode file or from attributes
        private static readonly Dictionary<string, MenuItemsTree<MenuItemOrderingNative>> s_MenusFromModeFile = new Dictionary<string, MenuItemsTree<MenuItemOrderingNative>>();
        // Contains menu from attributes for modes other than default
        // Used to add menu items from attributes when iterating through the .mode menus
        private static Dictionary<string, MenuItemsTree<MenuItemScriptCommand>> s_MenuItemsPerMode = null;
        // Contains menu from attributes for the default mode
        // The default mode menus are in a separate dictionary for performance reasons, in that case we don't need the costly MenuItemsTree structure because it won't be used when iterating through the .mode menus
        private static Dictionary<string, MenuItemScriptCommand> s_MenuItemsDefaultMode = null;

        [UsedImplicitly, RequiredByNativeCode]
        internal static bool UseDefaultModeMenus()
        {
            if (currentId == k_DefaultModeId)
                return true;

            var menus = GetMenusFromModeFile(currentIndex);
            if (menus == null)
                return true;

            return false;
        }

        // Used to filter the C# menu attributes with need in the current mode
        [UsedImplicitly, RequiredByNativeCode]
        internal static MenuItemScriptCommand[] GetMenuItemsFromAttributes()
        {
            return GetMenuItemsFromAttributesById(currentId);
        }

        // Find menu item to add
        [UsedImplicitly, RequiredByNativeCode]
        internal static MenuItemOrderingNative FindMenuItem(string fullMenuName)
        {
            var menus = GetMenusItemsFromModeFile(currentId);
            if (menus != null)
                return menus.FindClosestItem(fullMenuName);
            return null;
        }

        internal static bool IsShortcutAvailableInMode(string shortcutId)
        {
            if (shortcutId.StartsWith(ShortcutManagement.Discovery.k_MainMenuShortcutPrefix))
            {
                string menuName = shortcutId.Replace(ShortcutManagement.Discovery.k_MainMenuShortcutPrefix, "");

                var menus = GetMenusItemsFromModeFile(currentId);
                if (menus != null)
                    return menus.FindItem(menuName) != null;
                return false;
            }
            return true;
        }

        private static MenuItemsTree<MenuItemOrderingNative> GetModeMenuTree(string modeName)
        {
            var mit = new MenuItemsTree<MenuItemOrderingNative>(new MenuItemOrderingNative());
            mit.onlyLeafHaveValue = false;

            var menuData = GetMenusFromModeFile(GetModeIndexById(modeName));
            if (menuData == null)
            {
                if (modeName == k_DefaultModeId)
                {
                    // if there are no menus in the mode file and we are on the default mode, we use the default modes menus directly
                    foreach (var menuItem in s_MenuItemsDefaultMode.Values)
                        mit.AddChildSearch(new MenuItemOrderingNative(menuItem.name, menuItem.name, menuItem.priority, menuItem.priority));
                    return mit;
                }
                else
                    menuData = new List<JSONObject>();
            }
            s_MenuItemsPerMode.TryGetValue(modeName, out var menuItems);
            GetModeMenuTreeRecursive(mit, menuData, menuItems);

            return mit;
        }

        // Get the next menu item to add (in the right order)
        private static void GetModeMenuTreeRecursive(MenuItemsTree<MenuItemOrderingNative> menuTree, IList menus, MenuItemsTree<MenuItemScriptCommand> menuItemsFromAttributes,
            HashSet<string> addedMenuItemAttributes = null, string currentPrefix = "", string currentOriginalPrefix = "", int parentPriority = 0, int priority = 100)
        {
            if (menus == null)
                return;
            if (addedMenuItemAttributes == null)
                addedMenuItemAttributes = new HashSet<string>();

            for (int index = 0; index < menus.Count; ++index)
            {
                var menuData = menus[index];
                // separator
                if (menuData == null)
                {
                    priority += 100;
                    continue;
                }
                var menu = menuData as JSONObject;
                var isInternal = JsonUtils.JsonReadBoolean(menu, k_MenuKeyInternal);
                if (isInternal && !Unsupported.IsDeveloperMode())
                    continue;

                var platform = JsonUtils.JsonReadString(menu, k_MenuKeyPlatform);
                // Check the menu item platform
                if (!String.IsNullOrEmpty(platform) && !Application.platform.ToString().ToLowerInvariant().StartsWith(platform.ToLowerInvariant()))
                    continue;

                var menuName = JsonUtils.JsonReadString(menu, k_MenuKeyName);
                var fullMenuName = currentPrefix + menuName;
                priority = JsonUtils.JsonReadInt(menu, k_MenuKeyPriority, priority);

                // if there is an original full name (complete path) then use it, else if there is an original name (path following currentOriginalPrefix) then use that, else get the menu name
                var originalName = JsonUtils.JsonReadString(menu, k_MenuKeyOriginalFullName, currentOriginalPrefix + JsonUtils.JsonReadString(menu, k_MenuKeyOriginalName, menuName));

                if (menuItemsFromAttributes != null)
                    InsertMenuFromAttributeIfNecessary(menuTree, addedMenuItemAttributes, menuItemsFromAttributes, menuName, currentPrefix, priority, parentPriority);

                // Check if we are a submenu
                if (menu.Contains(k_MenuKeyChildren))
                {
                    if (menu[k_MenuKeyChildren] is IList children)
                    {
                        // we go deeper
                        menuTree.AddChildSearch(new MenuItemOrderingNative(fullMenuName, originalName, priority, parentPriority));
                        GetModeMenuTreeRecursive(menuTree, children, menuItemsFromAttributes, addedMenuItemAttributes, fullMenuName + "/", originalName + "/", priority);
                        continue;
                    }
                    else if (menu[k_MenuKeyChildren] is string wildCard && wildCard == "*")
                    {
                        var menuToAdd = new MenuItemOrderingNative(fullMenuName, originalName, priority, parentPriority, addChildren: true);
                        if (menu.Contains(k_MenuKeyExclude))
                        {
                            if (menu[k_MenuKeyExclude] is IList excludedMenus)
                            {
                                menuToAdd.childrenToExclude = new string[excludedMenus.Count];
                                for (int excludedMenusIndex = 0; excludedMenusIndex < excludedMenus.Count; ++excludedMenusIndex)
                                {
                                    if (excludedMenus[excludedMenusIndex] is JSONObject excludeMenu)
                                        menuToAdd.childrenToExclude[excludedMenusIndex] = originalName + "/" + JsonUtils.JsonReadString(excludeMenu, k_MenuKeyName);
                                }
                            }
                        }
                        if (menu.Contains(k_MenuKeyNotExclude))
                        {
                            IList notExcludedMenus = menu[k_MenuKeyNotExclude] as IList;
                            if (notExcludedMenus != null)
                            {
                                menuToAdd.childrenToNotExclude = new string[notExcludedMenus.Count];
                                for (int notExcludedMenusIndex = 0; notExcludedMenusIndex < notExcludedMenus.Count; ++notExcludedMenusIndex)
                                {
                                    var notExcludeMenu = notExcludedMenus[notExcludedMenusIndex] as JSONObject;
                                    if (notExcludeMenu != null)
                                        menuToAdd.childrenToNotExclude[notExcludedMenusIndex] = originalName + "/" + JsonUtils.JsonReadString(notExcludeMenu, k_MenuKeyName);
                                }
                            }
                        }
                        menuTree.AddChildSearch(menuToAdd);
                    }
                }
                else
                    menuTree.AddChildSearch(new MenuItemOrderingNative(fullMenuName, originalName, priority, parentPriority));
                priority++;
            }
            if (menuItemsFromAttributes != null)
                InsertMenuFromAttributeIfNecessary(menuTree, addedMenuItemAttributes, menuItemsFromAttributes, null, currentPrefix, priority, parentPriority);

            // no more menu at this level
        }

        private static void InsertMenuFromAttributeIfNecessary(MenuItemsTree<MenuItemOrderingNative> menuTree, HashSet<string> addedMenuItemAttributes, MenuItemsTree<MenuItemScriptCommand> menuItems, string menuName, string currentPrefix, int priority, int parentPriority)
        {
            if (currentPrefix == "")
            {
                bool mustAddNow = false;
                if (menuName == null)
                    mustAddNow = true;
                else
                {
                    mustAddNow = menuName == k_WindowMenuName || menuName == k_HelpMenuName;
                }
                if (mustAddNow)
                {
                    foreach (var menuPerMode in menuItems.menuItemChildrenSorted)
                    {
                        if (!addedMenuItemAttributes.Contains(menuPerMode.name)
                            && (menuName == null    // if there are no menus left we should add every menu item attributes that were not added
                                || (!menuPerMode.name.StartsWith(k_HelpMenuName + "/")      // Creating a help menu from attribute should only happen if there are no menus left since Help is the last
                                    && !(menuName == k_WindowMenuName && menuPerMode.name.StartsWith(k_WindowMenuName + "/")))))    //Creating a window menu from attribute should happen when there are only Help or no menu left, so if the next is Window then we should not add window menu item
                        {
                            addedMenuItemAttributes.Add(menuPerMode.name);
                            menuTree.AddChildSearch(new MenuItemOrderingNative(menuPerMode.name, menuPerMode.name, menuPerMode.priority, menuPerMode.priority));
                        }
                    }
                }
            }
            else
            {
                // find if there is a mode specific menu to insert at this position
                var menuItemTree = menuItems.FindTree(currentPrefix.Substring(0, currentPrefix.Length - 1));
                if (menuItemTree != null)
                {
                    foreach (var menuPerMode in menuItemTree.menuItemChildrenSorted)
                    {
                        if (!addedMenuItemAttributes.Contains(menuPerMode.name)
                            && (menuPerMode.priority < priority || menuName == null))
                        {
                            addedMenuItemAttributes.Add(menuPerMode.name);
                            if (menuPerMode.name.Substring(currentPrefix.Length).Contains("/"))
                                menuTree.AddChildSearch(new MenuItemOrderingNative(menuPerMode.name, menuPerMode.name, menuPerMode.priority, menuPerMode.priority));
                            else
                                menuTree.AddChildSearch(new MenuItemOrderingNative(menuPerMode.name, menuPerMode.name, menuPerMode.priority, parentPriority));
                        }
                    }
                }
            }
        }

        // Find menus from command id
        private static void LoadMenuFromCommandId(IList menus, Dictionary<string, MenuItemScriptCommand> menuItems, string prefix = "")
        {
            if (menus == null || menuItems == null)
                return;

            foreach (var menuData in menus)
            {
                if (menuData != null)
                {
                    var menu = menuData as JSONObject;
                    if (menu == null)
                        continue;
                    var isInternal = JsonUtils.JsonReadBoolean(menu, k_MenuKeyInternal);
                    if (isInternal && !Unsupported.IsDeveloperMode())
                        continue;
                    var menuName = JsonUtils.JsonReadString(menu, k_MenuKeyName);
                    var fullMenuName = prefix + menuName;

                    var platform = JsonUtils.JsonReadString(menu, k_MenuKeyPlatform);

                    // Check the menu item platform
                    if (!String.IsNullOrEmpty(platform) && !Application.platform.ToString().ToLowerInvariant().StartsWith(platform.ToLowerInvariant()))
                        continue;

                    // Check if we are a submenu
                    if (menu.Contains(k_MenuKeyChildren))
                    {
                        if (menu[k_MenuKeyChildren] is IList children)
                            LoadMenuFromCommandId(children, menuItems, fullMenuName + "/");
                    }
                    else
                    {
                        var commandId = JsonUtils.JsonReadString(menu, k_MenuKeyCommandId);
                        if (!String.IsNullOrEmpty(commandId) && CommandService.Exists(commandId))
                        {
                            // Create a new menu item pointing to a command handler
                            var shortcut = JsonUtils.JsonReadString(menu, k_MenuKeyShortcut);
                            var @checked = JsonUtils.JsonReadBoolean(menu, k_MenuKeyChecked);

                            var validateCommandId = JsonUtils.JsonReadString(menu, k_MenuKeyValidateCommandId);
                            var commandMenuItem = MenuItemScriptCommand.InitializeFromCommand(fullMenuName, 100, commandId, validateCommandId);
                            menuItems[fullMenuName] = commandMenuItem;
                        }
                    }
                }
            }
        }

        // Extract in another method for tests
        private static MenuItemScriptCommand[] GetMenuItemsFromAttributesById(string id)
        {
            ExtractMenuItemsFromAttributes();
            var menus = GetMenusFromModeFile(GetModeIndexById(id));

            if (menus == null) // If there is no mode menus in the mode file
                return CombineMenuItemsFromAttributes(id, false).Values.ToArray();

            var menuItems = CombineMenuItemsFromAttributes(id, true);
            LoadMenuFromCommandId(menus, menuItems);

            return menuItems.Values.ToArray(); //In that case there is a .mode so menus will be filtered by the iterator
        }

        private static MenuItemsTree<MenuItemOrderingNative> GetMenusItemsFromModeFile(string modeName)
        {
            if (s_MenusFromModeFile.TryGetValue(modeName, out var menus))
                return menus;

            if (s_MenuItemsPerMode == null)
                ExtractMenuItemsFromAttributes();

            var mit = GetModeMenuTree(modeName);
            s_MenusFromModeFile.Add(modeName, mit);
            return mit;
        }

        private static IList GetMenusFromModeFile(int index)
        {
            object items = GetModeDataSection(index, ModeDescriptor.MenusKey);
            if (items == null || (items is string wildCard && wildCard == "*"))
                return null;

            return items as IList;
        }

        private static void ExtractMenuItemsFromAttributes()
        {
            s_MenuItemsPerMode = new Dictionary<string, MenuItemsTree<MenuItemScriptCommand>>();
            s_MenuItemsDefaultMode = new Dictionary<string, MenuItemScriptCommand>();

            var menuItems = TypeCache.GetMethodsWithAttribute<MenuItem>();

            // Order the menu items to start with Unity menus before projects menus. That way if there is a duplicate, the project one is flagged as duplicate
            foreach (var methodInfo in menuItems.OrderBy(m => !Utility.FastStartsWith(m.DeclaringType.Assembly.FullName, "UnityEditor", "unityeditor")))
            {
                if (!ValidateMethodForMenuCommand(methodInfo))
                    continue;
                foreach (var attribute in methodInfo.GetCustomAttributes(typeof(MenuItem), false))
                {
                    string menuName = SanitizeMenuItemName(((MenuItem)attribute).menuItem);
                    string[] editorModes = ((MenuItem)attribute).editorModes;
                    foreach (var editorMode in editorModes)
                    {
                        if (editorMode == k_DefaultModeId)
                        {
                            if (s_MenuItemsDefaultMode.TryGetValue(menuName, out var menuItem))
                                menuItem.Update((MenuItem)attribute, methodInfo);
                            else
                                s_MenuItemsDefaultMode.Add(menuName, MenuItemScriptCommand.Initialize(menuName, (MenuItem)attribute, methodInfo));
                        }
                        else
                        {
                            if (s_MenuItemsPerMode.TryGetValue(editorMode, out var menuItemsPerMode))
                            {
                                MenuItemScriptCommand menuItem = menuItemsPerMode.FindItem(menuName);
                                if (menuItem == null)
                                    menuItemsPerMode.AddChildSearch(MenuItemScriptCommand.Initialize(menuName, (MenuItem)attribute, methodInfo));
                                else
                                    menuItem.Update((MenuItem)attribute, methodInfo);
                            }
                            else
                            {
                                var newMenusPerMode = new MenuItemsTree<MenuItemScriptCommand>();
                                newMenusPerMode.AddChildSearch(MenuItemScriptCommand.Initialize(menuName, (MenuItem)attribute, methodInfo));
                                s_MenuItemsPerMode.Add(editorMode, newMenusPerMode);
                            }
                        }
                    }
                }
            }
            CleanUpInvalidMenuItems();
        }

        private static void CleanUpInvalidMenuItems()
        {
            foreach (var menuItemPerMode in s_MenuItemsPerMode.Values)
            {
                menuItemPerMode.CleanUp();
            }
            // Default mode items
            var itemsToDelete = new List<string>();
            foreach (var menu in s_MenuItemsDefaultMode)
            {
                if (menu.Value.IsNotValid)
                    itemsToDelete.Add(menu.Key);
            }
            foreach (var itemToDelete in itemsToDelete)
            {
                s_MenuItemsDefaultMode.Remove(itemToDelete);
            }
        }

        private static Dictionary<string, MenuItemScriptCommand> CombineMenuItemsFromAttributes(string id, bool menusDependOnModeFile)
        {
            // Create the initial menu items collection for the mode id
            var menuItemsResult = new Dictionary<string, MenuItemScriptCommand>();
            // We add the menus from the mode id
            if (id != k_DefaultModeId && s_MenuItemsPerMode.ContainsKey(id))
            {
                foreach (var menuItem in s_MenuItemsPerMode[id].menuItemChildren)
                    menuItemsResult.Add(menuItem.name, menuItem);
            }
            // We always add the default mode menus (if not present)
            AddMenuItemsFromMode(menuItemsResult, s_MenuItemsDefaultMode.Values);

            // If the menus depend on the .mode file, we also add the other modes menus because the mode file can reference them
            if (menusDependOnModeFile)
            {
                foreach (var menuItemPerMode in s_MenuItemsPerMode)
                {
                    if (menuItemPerMode.Key != id)
                    {
                        AddMenuItemsFromMode(menuItemsResult, menuItemPerMode.Value.menuItemChildren);
                    }
                }
            }
            return menuItemsResult;
        }

        private static void AddMenuItemsFromMode(Dictionary<string, MenuItemScriptCommand> menuItemsResult, IEnumerable<MenuItemScriptCommand> menuItems)
        {
            foreach (var menuItem in menuItems)
            {
                if (!menuItemsResult.ContainsKey(menuItem.name)) // there can be multiple methods that have the same attribute name for different modes, we don't add it in that case
                {
                    menuItemsResult.Add(menuItem.name, menuItem);
                }
            }
        }

        private static string SanitizeMenuItemName(string menuName)
        {
            while (menuName.StartsWith("/"))
            {
                menuName = menuName.Substring(1);
            }
            // removing trailing "/" is already done when building the tree because we're removing empty entries when splitting the menu name with /
            return menuName;
        }

        private static bool ValidateMethodForMenuCommand(MethodInfo methodInfo)
        {
            if (methodInfo.DeclaringType.IsGenericType)
            {
                Debug.LogWarningFormat("Method {0}.{1} cannot be used for menu commands because class {0} is an open generic type.", methodInfo.DeclaringType, methodInfo.Name);
                return false;
            }
            // Skip non-static methods for regular menus
            if (!methodInfo.IsStatic)
            {
                Debug.LogWarningFormat("Method {0}.{1} is not static and cannot be used for menu commands.", methodInfo.DeclaringType, methodInfo.Name);
                return false;
            }
            // Skip generic methods
            if (methodInfo.IsGenericMethod)
            {
                Debug.LogWarningFormat("Method {0}.{1} is generic and cannot be used for menu commands.", methodInfo.DeclaringType, methodInfo.Name);
                return false;
            }
            // Skip invalid methods
            ParameterInfo[] parameters = methodInfo.GetParameters();
            if (parameters.Length > 1)
            {
                Debug.LogWarningFormat("Method {0}.{1} has invalid parameters. MenuCommand is the only optional supported parameter.", methodInfo.DeclaringType, methodInfo.Name);
                return false;
            }
            if (parameters.Length == 1)
            {
                if (parameters[0].ParameterType != typeof(MenuCommand))
                {
                    Debug.LogWarningFormat("Method {0}.{1} has invalid parameters. MenuCommand is the only optional supported parameter.", methodInfo.DeclaringType, methodInfo.Name);
                    return false;
                }
            }
            return true;
        }

        internal class MenuItemsTree<T>
            where T : class, IMenuItem
        {
            private string key;
            private T value;
            private readonly int m_Priority;

            private readonly List<MenuItemsTree<T>> m_Children;
            public List<T> menuItemChildren => GetChildrenRecursively();
            public List<T> menuItemChildrenSorted => GetChildrenRecursively(true);

            public bool onlyLeafHaveValue { get; set; } = true;

            private List<T> GetChildrenRecursively(bool sorted = false, List<T> result = null)
            {
                if (result == null)
                    result = new List<T>();
                if (m_Children.Any())
                {
                    var children = sorted ? (IEnumerable<MenuItemsTree<T>>)m_Children.OrderBy(c => c.key).OrderBy(c => c.m_Priority) : m_Children;
                    foreach (var child in children)
                        child.GetChildrenRecursively(sorted, result);
                }
                else if (value != null)
                    result.Add(value);
                return result;
            }

            public MenuItemsTree(string key = "", int priority = 100)
            {
                this.key = EditorUtility.ParseMenuName(key);
                m_Priority = priority;
                m_Children = new List<MenuItemsTree<T>>();
            }

            public MenuItemsTree(T value) : this(value.Name, value.Priority)
            {
                this.value = value;
            }

            // In case of renaming it's risky to use this method because the key is the original name (to be able to find it on MenuController::AddMenuItem) but the tree we might want to add this item to can have a totally different name
            private MenuItemsTree<T> AddChildDirectly(T menuItem)
            {
                var child = new MenuItemsTree<T>(menuItem);
                m_Children.Add(child);
                return child;
            }

            private MenuItemsTree<T> AddIntermediateMenuItem(string pathPart, int priority)
            {
                string name = string.IsNullOrEmpty(key) ? pathPart : key + "/" + pathPart;


                var child = new MenuItemsTree<T>(name, priority);
                m_Children.Add(child);
                return child;
            }

            public bool AddChildSearch(T menuItem)
            {
                if (key == menuItem.Name)
                {
                    if (onlyLeafHaveValue)
                        Debug.LogWarning($"MenuItem {key} was added twice");
                    else if (value == null)
                        value = menuItem;
                    return true;
                }
                if (IsParentMenu(menuItem.Name))
                {
                    foreach (var child in m_Children)
                    {
                        if (child.AddChildSearch(menuItem))
                            return true;
                    }
                    // create hierarchy from name and /
                    string rightSide = menuItem.Name.Substring(key.Length);
                    string[] pathSplit = rightSide.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    // create intermediate menus first
                    var currentMenu = this;
                    for (int i = 0; i < pathSplit.Length - 1; ++i)
                    {
                        currentMenu = currentMenu.AddIntermediateMenuItem(pathSplit[i], menuItem.Priority);
                    }
                    // then add the menuItem
                    currentMenu.AddChildDirectly(menuItem);
                    return true;
                }
                return false;
            }

            public bool Contains(string key)
            {
                return FindTree(key) != null;
            }

            public T FindItem(string key)
            {
                return FindTree(key)?.value;
            }

            public T FindClosestItem(string key)
            {
                return FindTreePrivate(key, true)?.value;
            }

            public MenuItemsTree<T> FindTree(string key)
            {
                return FindTreePrivate(key);
            }

            private MenuItemsTree<T> FindTreePrivate(string key, bool findClosest = false)
            {
                return Find(EditorUtility.ParseMenuName(key), findClosest);
            }

            private MenuItemsTree<T> Find(string key, bool findClosest = false)
            {
                if (this.key == key)
                    return this;
                if (IsParentMenu(key))
                {
                    foreach (var menuItem in m_Children)
                    {
                        var foundItem = menuItem.Find(key, findClosest);
                        if (foundItem != null)
                            return foundItem;
                    }
                    if (findClosest)
                        return this;
                }
                return null;
            }

            private bool IsParentMenu(string menuName)
            {
                return key.Length == 0 || (key.Length < menuName.Length && menuName.StartsWith(key) && menuName.Substring(key.Length).StartsWith("/"));
            }

            internal void CleanUp()
            {
                for (int i = 0; i < m_Children.Count;)
                {
                    if (m_Children[i].m_Children.Count > 0 || (m_Children[i].m_Children.Count == 0 && m_Children[i].value == null)) // CleanUp intermediate menu
                    {
                        m_Children[i].CleanUp();
                        if (m_Children[i].m_Children.Count == 0) // If clean up removed every children of that child we need to remove that child
                        {
                            m_Children.RemoveAt(i);
                            continue;
                        }
                    }
                    else
                    {
                        MenuItemScriptCommand menuItem = m_Children[i].value as MenuItemScriptCommand;
                        if (menuItem != null && menuItem.IsNotValid)
                        {
                            m_Children.RemoveAt(i);
                            continue;
                        }
                    }
                    ++i;
                }
            }
        }
    }
}
