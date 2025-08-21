// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    public abstract class MainToolbarElement
    {
        public MainToolbarContent content { get; set; }
        public bool displayed { get; set; } = true;
        public bool enabled { get; set; } = true;

        public Action<DropdownMenu> populateContextMenu { get; set; } = null;
        internal virtual Action<DropdownMenu> populateContextMenuInternal => null;

        internal VisualElement Rebuild()
        {
            var element = CreateElement();
            if (element != null)
            {
                element.style.display = displayed ? DisplayStyle.Flex : DisplayStyle.None;
                element.SetEnabled(enabled);
            }
            return element;
        }

        internal abstract VisualElement CreateElement();
    }
}
