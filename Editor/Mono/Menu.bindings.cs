// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor
{
    [UsedByNativeCode]
    struct ScriptingMenuItem
    {
        string m_Path;
        bool m_IsSeparator;
        int m_Priority;

        public string path => m_Path;
        public bool isSeparator => m_IsSeparator;
        public int priority => m_Priority;

        public ScriptingMenuItem(string path, int priority = -1, bool isSeparator = false)
        {
            m_Path = path;
            m_Priority = priority;
            m_IsSeparator = isSeparator;
        }
    }

    [NativeHeader("Editor/Src/MenuController.h")]
    public sealed class Menu
    {
        internal static event Action menuChanged;

        [NativeMethod("MenuController::SetChecked", true)]
        public static extern void SetChecked(string menuPath, bool isChecked);

        [NativeMethod("MenuController::GetChecked", true)]
        public static extern bool GetChecked(string menuPath);

        [NativeMethod("MenuController::GetEnabled", true)]
        public static extern bool GetEnabled(string menuPath);

        [NativeMethod("MenuController::UpdateContextMenu", true)]
        internal static extern void UpdateContextMenu(UnityEngine.Object[] context, int userData);

        [NativeMethod("MenuController::CreateActionMenuBegin", true)]
        internal static extern void CreateActionMenuBegin();

        [NativeMethod("MenuController::CreateActionMenuEnd", true)]
        internal static extern void CreateActionMenuEnd();

        [NativeMethod("MenuController::GetEnabledWithContext", true)]
        internal static extern bool GetEnabledWithContext(string menuPath, UnityEngine.Object[] context);

        [FreeFunction("MenuController::GetMenuItemDefaultShortcuts")]
        internal static extern void GetMenuItemDefaultShortcuts(List<string> outItemNames, List<string> outItemDefaultShortcuts);

        [FreeFunction("MenuController::SetMenuItemHotkey")]
        internal static extern void SetHotkey(string menuPath, string hotkey);

        [FreeFunction("MenuController::GetMenuItemHotkey")]
        internal static extern string GetHotkey(string menuPath);

        [FreeFunction("MenuController::ExtractSubmenus")]
        internal static extern string[] ExtractSubmenus(string menuPath);

        // "separators" in this context means submenu roots, ex "GameObject/" in "GameObject/Cube".
        [FreeFunction("MenuController::ExtractMenuItems")]
        internal static extern ScriptingMenuItem[] GetMenuItems(string menuPath, bool includeSeparators, bool localized);

        [FreeFunction("MenuController::AddMenuItem")]
        internal static extern void AddMenuItem(string name, string shortcut, bool @checked, int priority, System.Action execute, System.Func<bool> validate);

        [FreeFunction("MenuController::RemoveMenuItem")]
        internal static extern void RemoveMenuItem(string name);

        [FreeFunction("MenuController::AddSeparator")]
        internal static extern void AddSeparator(string name, int priority);

        [FreeFunction("MenuController::RebuildAllMenus")]
        internal static extern void RebuildAllMenus();

        [NativeMethod("MenuController::FindHotkeyStartIndex", true)]
        internal static extern int FindHotkeyStartIndex(string menuPath);

        [NativeMethod("MenuController::MenuItemExists", true)]
        internal static extern bool MenuItemExists(string menuPath);

        [RequiredByNativeCode]
        private static void OnMenuChanged()
        {
            menuChanged?.Invoke();
        }
    }
}
