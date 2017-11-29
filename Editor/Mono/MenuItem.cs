// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditor
{
    // The MenuItem attribute allows you to add menu items to the main menu and inspector context menus.
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    [RequiredByNativeCode]
    public sealed class MenuItem : Attribute
    {
        // Creates a menu item and invokes the static function following it, when the menu item is selected.
        public MenuItem(string itemName) : this(itemName, false) {}

        // Creates a menu item and invokes the static function following it, when the menu item is selected.
        public MenuItem(string itemName, bool isValidateFunction) : this(itemName, isValidateFunction, itemName.StartsWith("GameObject/Create Other") ? 10 : 1000) {}
        // The special treatment of "GameObject/Other" is to ensure that legacy scripts that don't set a priority don't create a
        // "Create Other" menu at the very bottom of the GameObject menu (thus preventing the items from being propagated to the
        // scene hierarchy dropdown and context menu).

        // Creates a menu item and invokes the static function following it, when the menu item is selected.
        public MenuItem(string itemName, bool isValidateFunction, int priority) : this(itemName, isValidateFunction, priority, false) {}

        // Creates a menu item and invokes the static function following it, when the menu item is selected.
        internal MenuItem(string itemName, bool isValidateFunction, int priority, bool internalMenu)
        {
            if (internalMenu)
                menuItem = "internal:" + itemName;
            else
                menuItem = itemName;
            validate = isValidateFunction;
            this.priority = priority;
        }

        public string menuItem;
        public bool validate;
        public int priority;
    }
}
