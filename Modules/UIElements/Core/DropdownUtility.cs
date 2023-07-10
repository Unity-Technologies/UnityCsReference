// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    internal static class DropdownUtility
    {
        internal static Func<bool, GenericDropdownMenu> MakeDropdownFunc;
        internal static Action<GenericDropdownMenu, Rect, VisualElement, bool, bool> ShowDropdownFunc;

        internal static GenericDropdownMenu CreateDropdown(bool allowSubmenus = false)
        {
            return MakeDropdownFunc != null ? MakeDropdownFunc.Invoke(allowSubmenus) : new GenericDropdownMenu();
        }

        internal static void ShowDropdown(GenericDropdownMenu menu, Vector2 position, VisualElement target = null, bool anchored = false, bool parseShortcuts = false, bool autoClose = true)
        {
            var positionRect = new Rect(position, Vector2.zero);

            if (ShowDropdownFunc != null)
                ShowDropdownFunc.Invoke(menu, positionRect, target, parseShortcuts, autoClose);
            else
                menu.DropDown(positionRect, target, anchored);
        }
    }
}

