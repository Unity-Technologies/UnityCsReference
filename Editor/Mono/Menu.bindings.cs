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

        [FreeFunction("MenuController::GetMenuItemDefaultShortcuts")]
        internal static extern void GetMenuItemDefaultShortcuts(List<string> outItemNames, List<string> outItemDefaultShortcuts);

        [FreeFunction("MenuController::SetMenuItemHotkey")]
        internal static extern void SetHotkey(string menuPath, string hotkey);

        [FreeFunction("MenuController::ExtractSubmenus")]
        internal static extern string[] ExtractSubmenus(string menuPath);
    }
}
