// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor
{
    // The MenuItem attribute allows you to add menu items to the main menu and inspector context menus.
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    [RequiredByNativeCode]
    public sealed class MenuItem : Attribute
    {
        private static readonly string[] kMenuItemSeparators = {"/"};

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
        internal MenuItem(string itemName, bool isValidateFunction, int priority, bool internalMenu) : this(itemName, isValidateFunction, priority, internalMenu, new string[] { "default" }) {}

        // Creates a menu item and invokes the static function following it, when the menu item is selected.
        internal MenuItem(string itemName, bool isValidateFunction, int priority, bool internalMenu, string[] editorModes)
        {
            itemName = NormalizeMenuItemName(itemName);
            if (internalMenu)
                menuItem = "internal:" + itemName;
            else
                menuItem = itemName;
            validate = isValidateFunction;
            this.priority = priority;
            this.editorModes = editorModes;
        }

        private static string NormalizeMenuItemName(string rawName)
        {
            return string.Join(kMenuItemSeparators[0], rawName.Split(kMenuItemSeparators, StringSplitOptions.None).Select(token => token.Trim()).ToArray());
        }

        public string menuItem;
        public bool validate;
        public int priority;
        public string[] editorModes;
    }

    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    class MenuItemScriptCommand : IMenuItem
    {
        public string name;
        public int priority;
        public MethodInfo execute;
        public MethodInfo validate;
        public Delegate commandExecute;
        public Delegate commandValidate;

        public string Name => name;

        public int Priority => priority;
        internal bool IsNotValid => validate != null && execute == null;

        public static MenuItemScriptCommand Initialize(string menuName, MenuItem menuItemAttribute, MethodInfo methodInfo)
        {
            if (!menuItemAttribute.validate)
                return InitializeFromExecute(menuName, menuItemAttribute.priority, methodInfo);
            else
                return InitializeFromValidate(menuName, methodInfo);
        }

        private static MenuItemScriptCommand InitializeFromValidate(string menuName, MethodInfo validate)
        {
            return new MenuItemScriptCommand()
            {
                name = menuName,
                validate = validate
            };
        }

        private static MenuItemScriptCommand InitializeFromExecute(string menuName, int priority, MethodInfo execute)
        {
            return new MenuItemScriptCommand()
            {
                name = menuName,
                priority = priority,
                execute = execute
            };
        }

        internal static MenuItemScriptCommand InitializeFromCommand(string fullMenuName, int priority, string commandId, string validateCommandId)
        {
            var menuItem = new MenuItemScriptCommand()
            {
                name = fullMenuName,
                priority = priority,
                commandExecute = new Action(() => CommandService.Execute(commandId, CommandHint.Menu))
            };
            if (!string.IsNullOrEmpty(validateCommandId))
                menuItem.commandValidate = new Func<bool>(() => (bool)CommandService.Execute(commandId, CommandHint.Menu | CommandHint.Validate));

            return menuItem;
        }

        internal void Update(MenuItem menuItemAttribute, MethodInfo methodInfo)
        {
            if (!menuItemAttribute.validate)
            {
                if (execute != null)
                {
                    if (!(name == "GameObject/3D Object/Mirror" || name == "GameObject/Light/Planar Reflection Probe")) //TODO: remove when HDRP removes the duplicate menus
                        Debug.LogWarning($"Cannot add menu item '{name}' for method '{methodInfo.DeclaringType}.{methodInfo.Name}' because a menu item with the same name already exists.");
                    return;
                }
                priority = menuItemAttribute.priority;
                execute = methodInfo;
            }
            else
            {
                if (validate != null)
                {
                    Debug.LogWarning($"Cannot add validate method '{methodInfo.DeclaringType}.{methodInfo.Name}' for menu item '{name}' because a menu item with the same name already has a validate method.");
                    return;
                }
                validate  = methodInfo;
            }
        }
    }

    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    class MenuItemOrderingNative : IMenuItem
    {
        public int position = -1;
        public int parentPosition = -1;
        public string currentModeFullMenuName; // name of the menu to show
        public string defaultModeFullMenuName; // name to find the default menu
        public bool addChildren; // if true then native should add all children menu
        public string[] childrenToExclude; // exclude those menu (path) when adding children
        public string[] childrenToNotExclude; // if excluding, those menu (path) will not be excluded

        public MenuItemOrderingNative()
        {
            defaultModeFullMenuName = string.Empty;
        }

        public MenuItemOrderingNative(string currentModeFullMenuName, string defaultModeFullMenuName, int position, int parentPosition, bool addChildren = false)
        {
            this.position = position;
            this.parentPosition = parentPosition;
            this.currentModeFullMenuName = currentModeFullMenuName;
            this.defaultModeFullMenuName = defaultModeFullMenuName;
            this.addChildren = addChildren;
        }

        public string Name => defaultModeFullMenuName;

        public int Priority => position;
    }

    interface IMenuItem
    {
        string Name { get; }
        int Priority { get; }
    }
}
