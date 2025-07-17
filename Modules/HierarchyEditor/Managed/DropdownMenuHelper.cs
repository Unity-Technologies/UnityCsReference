// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Hierarchy.Editor
{
    static class DropdownMenuHelper
    {
        public static void AppendFromGenericMenu(this DropdownMenu @this, GenericMenu genericMenu)
        {
            var menuItems = genericMenu.menuItems;

            for (var i = 0; i < menuItems.Count; i++)
            {
                var menuItem = menuItems[i];

                if (menuItem.separator)
                {
                    if (i < menuItems.Count - 1)
                        @this.AppendSeparator(menuItem.content.text);
                }
                else if (menuItem.userData != null)
                    @this.AppendAction(menuItem.content.text, a => menuItem.func2?.Invoke(a.userData), _ => GetStatus(menuItem.func2 == null, menuItem.on), menuItem.userData);
                else
                    @this.AppendAction(menuItem.content.text, a => menuItem.func?.Invoke(), _ => GetStatus(menuItem.func == null, menuItem.on));
            }
        }

        static DropdownMenuAction.Status GetStatus(bool isCallbackNull, bool isCheckOn) => (isCallbackNull, isCheckOn) switch
        {
            (false, false) => DropdownMenuAction.Status.Normal,
            (false, true) => DropdownMenuAction.Status.Checked,
            (true, false) => DropdownMenuAction.Status.Disabled,
            (true, true) => DropdownMenuAction.Status.Disabled | DropdownMenuAction.Status.Checked
        };
    }
}
