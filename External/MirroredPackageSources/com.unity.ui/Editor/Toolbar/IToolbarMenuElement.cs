using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// An interface for toolbar items that display drop-down menus.
    /// </summary>
    public interface IToolbarMenuElement
    {
        /// <summary>
        /// The drop-down menu for the element.
        /// </summary>
        DropdownMenu menu { get; }
    }

    /// <summary>
    /// An extension class that handles menu management for elements that are implemented with the IToolbarMenuElement interface, but are identical to DropdownMenu.
    /// </summary>
    public static class ToolbarMenuElementExtensions
    {
        /// <summary>
        /// Display the menu for the element.
        /// </summary>
        /// <param name="tbe">The element that is part of the menu to be displayed.</param>
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
