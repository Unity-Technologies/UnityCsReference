// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    public interface IToolbarMenuElement
    {
        DropdownMenu menu { get; }
    }

    public static class ToolbarMenuElementExtensions
    {
        public static void ShowMenu(this IToolbarMenuElement tbe)
        {
            if (!tbe.menu.MenuItems().Any())
                return;

            var ve = tbe as VisualElement;
            if (ve == null)
                return;

            Vector2 pos = new Vector2(ve.layout.xMin, ve.layout.yMax);
            pos = ve.parent.LocalToWorld(pos);

            tbe.menu.DoDisplayEditorMenu(pos);
        }
    }
}
