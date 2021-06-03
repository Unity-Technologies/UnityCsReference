// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    public class EditorToolbarButton : ToolbarButton
    {
        internal const string textClassName = EditorToolbar.elementLabelClassName;
        internal const string iconClassName = EditorToolbar.elementIconClassName;

        readonly TextElement m_TextElement;
        readonly Image m_IconElement;

        public new string text
        {
            get => m_TextElement.text;
            set => m_TextElement.text = value;
        }

        public Texture2D icon
        {
            get => m_IconElement.image as Texture2D;
            set
            {
                m_IconElement.image = value;
                m_IconElement.style.display = value != null ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public EditorToolbarButton() : this(string.Empty, null, null) {}
        public EditorToolbarButton(Action clickEvent) : this(string.Empty, null, clickEvent) {}
        public EditorToolbarButton(string text, Action clickEvent) : this(text, null, clickEvent) {}
        public EditorToolbarButton(Texture2D icon, Action clickEvent) : this(string.Empty, icon, clickEvent) {}

        public EditorToolbarButton(string text, Texture2D icon, Action clickEvent) : base(clickEvent)
        {
            m_IconElement = new Image { scaleMode = ScaleMode.ScaleToFit };
            m_IconElement.AddToClassList(iconClassName);
            this.icon = icon;
            Add(m_IconElement);

            m_TextElement = new TextElement();
            m_TextElement.AddToClassList(textClassName);
            m_TextElement.text = text;
            Add(m_TextElement);
        }
    }
}
