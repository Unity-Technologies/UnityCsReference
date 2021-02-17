// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
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
