// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine;

namespace UnityEditor.Toolbars
{
    public class EditorToolbarButton : ToolbarButton
    {
        EditorToolbarContent m_Content;

        public new string text
        {
            get => m_Content.text;
            set => m_Content.text = value;
        }

        internal string textIcon
        {
            get => m_Content.icon.textIcon;
            set => m_Content.icon = new EditorToolbarIcon(value);
        }

        public Texture2D icon
        {
            get => m_Content.icon.textureIcon;
            set => m_Content.icon = new EditorToolbarIcon(value);
        }

        public EditorToolbarButton() : this(string.Empty, null, null) {}
        public EditorToolbarButton(Action clickEvent) : this(string.Empty, null, clickEvent) {}
        public EditorToolbarButton(string text, Action clickEvent) : this(text, null, clickEvent) {}
        public EditorToolbarButton(Texture2D icon, Action clickEvent) : this(string.Empty, icon, clickEvent) {}

        public EditorToolbarButton(string text, Texture2D icon, Action clickEvent) : base(clickEvent)
        {
            AddToClassList(EditorToolbar.elementClassName);
            m_Content = new EditorToolbarContent(this, text, new EditorToolbarIcon(icon));
        }
    }
}
