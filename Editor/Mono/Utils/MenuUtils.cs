// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    class MenuUtils
    {
        internal enum ContextMenuOrigin
        {
            GameObject,
            Scene,
            Subscene,
            Toolbar,
            None
        }

        public static void MenuCallback(object callbackObject)
        {
            MenuCallbackObject menuCallBackObject = callbackObject as MenuCallbackObject;

            if (menuCallBackObject.onBeforeExecuteCallback != null)
                menuCallBackObject.onBeforeExecuteCallback(menuCallBackObject.menuItemPath, menuCallBackObject.temporaryContext, menuCallBackObject.origin, menuCallBackObject.userData);

            if (menuCallBackObject.temporaryContext != null)
            {
                EditorApplication.ExecuteMenuItemWithTemporaryContext(menuCallBackObject.menuItemPath, menuCallBackObject.temporaryContext);
            }
            else
            {
                EditorApplication.ExecuteMenuItem(menuCallBackObject.menuItemPath);
            }

            if (menuCallBackObject.onAfterExecuteCallback != null)
                menuCallBackObject.onAfterExecuteCallback(menuCallBackObject.menuItemPath, menuCallBackObject.temporaryContext, menuCallBackObject.origin, menuCallBackObject.userData);
        }

        public static void ExtractOnlyEnabledMenuItem(
            ScriptingMenuItem menuItem,
            GenericMenu menu,
            string replacementMenuString,
            Object[] temporaryContext,
            int userData,
            Action<string, Object[], ContextMenuOrigin, int> onBeforeExecuteCallback,
            Action<string, Object[], ContextMenuOrigin, int> onAfterExecuteCallback,
            ContextMenuOrigin origin,
            int previousMenuItemPriority = -1)
        {
            MenuCallbackObject callbackObject = new MenuCallbackObject();
            callbackObject.menuItemPath = menuItem.path;
            callbackObject.temporaryContext = temporaryContext;
            callbackObject.onBeforeExecuteCallback = onBeforeExecuteCallback;
            callbackObject.onAfterExecuteCallback = onAfterExecuteCallback;
            callbackObject.userData = userData;
            callbackObject.origin = origin;

            // logic should match CocoaMenuController.mm and MenuControllerWin.cpp
            if (menuItem.priority != -1 && menuItem.priority > previousMenuItemPriority + 10)
            {
                var separator = Path.GetDirectoryName(replacementMenuString);
                menu.AddSeparator($"{separator}/");
            }

            if (!menuItem.isSeparator && EditorApplication.ValidateMenuItem(menuItem.path))
                menu.AddItem(new GUIContent(L10n.TrPath(replacementMenuString)), false, MenuCallback, callbackObject);
        }

        static string GetSubmenuPath(GenericMenu.MenuItem item)
        {
            int index = item.content.text.LastIndexOf('/');
            return item.content.text.Substring(0, index + 1);
        }

        static int GetChildCount(List<GenericMenu.MenuItem> items, GenericMenu.MenuItem item)
        {
            if (!item.separator)
                return 0;

            string level = GetSubmenuPath(item);
            int count = 0;

            for (int i = 0, c = items.Count; i < c; ++i)
            {
                if (items[i].separator)
                    continue;
                var dir = GetSubmenuPath(items[i]);
                if (dir.StartsWith(level))
                    count++;
            }

            return count;
        }

        struct SeparatorInfo
        {
            public bool hasTitle;
            public bool hasItemAbove;
            public bool hasItemBelow;
        }

        static bool GetSeparatorInfo(List<GenericMenu.MenuItem> items, int index, out SeparatorInfo info)
        {
            if (!items[index].separator)
            {
                info = default;
                return false;
            }

            bool above = false, below = false;
            string submenu = GetSubmenuPath(items[index]);

            // check up and down for adjacent menu item on same submenu level
            for (int i = index - 1; i > -1 && !above; i--)
                above = GetSubmenuPath(items[i]).StartsWith(submenu);

            for (int i = index + 1, c = items.Count; i < c && !below; i++)
                below = GetSubmenuPath(items[i]).StartsWith(submenu);

            info = new SeparatorInfo()
            {
                hasTitle = !items[index].content.text.EndsWith("/"),
                hasItemAbove = above,
                hasItemBelow = below
            };

            return true;
        }

        // ExtractOnlyEnabledMenuItem can leave orphaned submenus. Ex, a separator in a submenu of all disabled items.
        static void RemoveEmptySubmenus(GenericMenu menu)
        {
            var items = menu.menuItems;

            for (int i = items.Count - 1; i > -1; i--)
            {
                var entry = items[i];
                if (entry.separator && GetChildCount(items, entry) < 1)
                    items.RemoveAt(i);
            }
        }

        // Remove separators with no other menu items in the same submenu, or without valid adjacent items.
        // Valid adjacent items are different for titled vs. empty separators. Empty separators are only valid between two
        // menu items on the same submenu. Titled separators are valid if there is a menu item below. This method does
        // not handle removing empty submenus, use RemoveEmptySubmenus prior to this method if that is a concern.
        static void RemoveInvalidSeparators(GenericMenu menu)
        {
            var items = menu.menuItems;
            var itemsCount = items.Count - 1;

            for (int i = itemsCount; i > -1; --i)
            {
                if (GetSeparatorInfo(items, i, out var info))
                {
                    if (info.hasTitle && info.hasItemBelow)
                        continue;

                    if (info.hasItemAbove && i < itemsCount)
                        continue;

                    items.RemoveAt(i);
                }
            }
        }

        public static void RemoveInvalidMenuItems(GenericMenu menu)
        {
            RemoveEmptySubmenus(menu);
            RemoveInvalidSeparators(menu);
        }

        class MenuCallbackObject
        {
            public string menuItemPath;
            public Object[] temporaryContext;
            public Action<string, Object[], ContextMenuOrigin, int> onBeforeExecuteCallback; // <menuItemPath, temporaryContext, userData>
            public Action<string, Object[], ContextMenuOrigin, int> onAfterExecuteCallback;  // <menuItemPath, temporaryContext, userData>
            public int userData;
            public ContextMenuOrigin origin;
        }
    }
}
