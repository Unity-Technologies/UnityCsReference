// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    public sealed class MainToolbarDropdown : MainToolbarElement
    {
        readonly Action<Rect> m_OpenDropdown;

        public MainToolbarDropdown(MainToolbarContent content, Action<Rect> openDropdown)
        {
            this.content = content;
            m_OpenDropdown = openDropdown;
        }

        internal override VisualElement CreateElement()
        {
            var dropdown = new EditorToolbarDropdown(content.text, content.image, null);
            dropdown.AddToClassList(EditorToolbar.elementClassName);
            dropdown.clicked += () => m_OpenDropdown?.Invoke(dropdown.worldBound);
            dropdown.text = content.text;
            dropdown.icon = content.image;
            dropdown.tooltip = content.tooltip;
            return dropdown;
        }
    }
}
