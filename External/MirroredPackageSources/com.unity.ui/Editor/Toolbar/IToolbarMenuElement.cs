using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    public interface IToolbarMenuElement
    {
        DropdownMenu menu { get; }
    }

    public static class ToolbarMenuElementExtensions
    {
        public static void ShowMenu(this IToolbarMenuElement tbe)
        {
            if (tbe == null || !tbe.menu.MenuItems().Any())
                return;

            var ve = tbe as VisualElement;
            if (ve == null)
                return;

            var worldBound = ve.worldBound;
            if (worldBound.x <= 0f)
            {
                // If the toolbar menu element is going over its allowed left edge, the menu won't be drawn.
                // (IMGUI seems to to the same as toolbar menus appear less attached to the left edge when the
                // windows are stuck to the left side of the screen as well as our menus)
                worldBound.x = 1f;
            }
            tbe.menu.DoDisplayEditorMenu(worldBound);
        }
    }
}
