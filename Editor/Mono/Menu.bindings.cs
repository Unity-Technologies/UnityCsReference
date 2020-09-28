// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using System.Collections.Generic;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/MenuController.h")]
    public sealed class Menu
    {
        [NativeMethod("MenuController::SetChecked", true)]
        public static extern void SetChecked(string menuPath, bool isChecked);

        [NativeMethod("MenuController::GetChecked", true)]
        public static extern bool GetChecked(string menuPath);

        [NativeMethod("MenuController::GetEnabled", true)]
        public static extern bool GetEnabled(string menuPath);

        [FreeFunction("MenuController::GetMenuItemDefaultShortcuts")]
        internal static extern void GetMenuItemDefaultShortcuts(List<string> outItemNames, List<string> outItemDefaultShortcuts);

        [FreeFunction("MenuController::SetMenuItemHotkey")]
        internal static extern void SetHotkey(string menuPath, string hotkey);

        [FreeFunction("MenuController::ExtractSubmenus")]
        internal static extern string[] ExtractSubmenus(string menuPath);

        [FreeFunction("MenuController::ResetMenus")]
        internal static extern void ResetMenus(bool resetToDefault);

        [FreeFunction("MenuController::AddMenuItem")]
        internal static extern void AddExistingMenuItem(string name, string existingMenuItemId, int priority, int parentPriority);

        [FreeFunction("MenuController::AddMenuItem")]
        internal static extern void AddMenuItem(string name, string shortcut, bool @checked, int priority, System.Action execute, System.Func<bool> validate);

        [FreeFunction("MenuController::RemoveMenuItem")]
        internal static extern void RemoveMenuItem(string name);

        [FreeFunction("MenuController::AddSeparator")]
        internal static extern void AddSeparator(string name, int priority);
    }
}
