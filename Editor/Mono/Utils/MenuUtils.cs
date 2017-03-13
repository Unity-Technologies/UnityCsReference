// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal class MenuUtils
    {
        public static void MenuCallback(object callbackObject)
        {
            MenuCallbackObject menuCallBackObject = callbackObject as MenuCallbackObject;

            if (menuCallBackObject.onBeforeExecuteCallback != null)
                menuCallBackObject.onBeforeExecuteCallback(menuCallBackObject.menuItemPath, menuCallBackObject.temporaryContext, menuCallBackObject.userData);

            if (menuCallBackObject.temporaryContext != null)
            {
                EditorApplication.ExecuteMenuItemWithTemporaryContext(menuCallBackObject.menuItemPath, menuCallBackObject.temporaryContext);
            }
            else
            {
                EditorApplication.ExecuteMenuItem(menuCallBackObject.menuItemPath);
            }

            if (menuCallBackObject.onAfterExecuteCallback != null)
                menuCallBackObject.onAfterExecuteCallback(menuCallBackObject.menuItemPath, menuCallBackObject.temporaryContext, menuCallBackObject.userData);
        }

        public static void ExtractSubMenuWithPath(string path, GenericMenu menu, string replacementPath, Object[] temporaryContext)
        {
            HashSet<string> menusWithCommands = new HashSet<string>(Unsupported.GetSubmenus(path));
            string[] menus = Unsupported.GetSubmenusIncludingSeparators(path);
            for (int i = 0; i < menus.Length; i++)
            {
                string menuString = menus[i];
                string replacedMenuString = replacementPath + menuString.Substring(path.Length);
                if (menusWithCommands.Contains(menuString))
                {
                    ExtractMenuItemWithPath(menuString, menu, replacedMenuString, temporaryContext, -1, null, null);
                }
                //else // Comment back in when GenericMenu can handle separators
                //  menu.AddSeparator(replacedMenuString);
            }
        }

        public static void ExtractMenuItemWithPath(string menuString, GenericMenu menu, string replacementMenuString, Object[] temporaryContext, int userData, Action<string, Object[], int> onBeforeExecuteCallback, Action<string, Object[], int> onAfterExecuteCallback)
        {
            MenuCallbackObject callbackObject = new MenuCallbackObject();
            callbackObject.menuItemPath = menuString;
            callbackObject.temporaryContext = temporaryContext;
            callbackObject.onBeforeExecuteCallback = onBeforeExecuteCallback;
            callbackObject.onAfterExecuteCallback = onAfterExecuteCallback;
            callbackObject.userData = userData;
            menu.AddItem(new GUIContent(replacementMenuString), false, MenuCallback, callbackObject);
        }

        private class MenuCallbackObject
        {
            public string menuItemPath;
            public Object[] temporaryContext;
            public Action<string, Object[], int> onBeforeExecuteCallback; // <menuItemPath, temporaryContext, userData>
            public Action<string, Object[], int> onAfterExecuteCallback;  // <menuItemPath, temporaryContext, userData>
            public int userData;
        }
    }
}
