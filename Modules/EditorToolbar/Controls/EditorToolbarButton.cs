// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Overlays;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    public class EditorToolbarButton : ToolbarButton
    {
        internal const string textClassName = EditorToolbar.elementLabelClassName;
        internal const string textIconClassName = EditorToolbar.elementTextIconClassName;
        internal const string iconClassName = EditorToolbar.elementIconClassName;

        internal const string k_TextElementName = "EditorToolbarButtonText";
        internal const string k_TextIconElementName = "EditorToolbarButtonTextIcon";

        Image m_IconElement;
        TextElement m_TextIconElement;
        TextElement m_TextElement;

        Texture2D m_Icon;
        string m_TextIcon;
        string m_Text;

        public new string text
        {
            get => m_Text;

            set
            {
                if (m_Text == value)
                    return;
                m_Text = value;
                if (m_TextElement != null)
                    m_TextElement.text = m_Text;
                UpdateIcon();
            }
        }

        internal string textIcon
        {
            get => m_TextIcon;
            set
            {
                if (m_TextIcon == value)
                    return;
                m_TextIcon = value;
                UpdateIcon();
            }
        }

        public Texture2D icon
        {
            get => m_Icon as Texture2D;
            set
            {
                m_Icon = value;
                UpdateIcon();
            }
        }

        public EditorToolbarButton() : this(string.Empty, null, null) {}
        public EditorToolbarButton(Action clickEvent) : this(string.Empty, null, clickEvent) {}
        public EditorToolbarButton(string text, Action clickEvent) : this(text, null, clickEvent) {}
        public EditorToolbarButton(Texture2D icon, Action clickEvent) : this(string.Empty, icon, clickEvent) {}

        public EditorToolbarButton(string text, Texture2D icon, Action clickEvent) : base(clickEvent)
        {
            InitializeButton(text, icon);
        }

        void InitializeButton(string text, Texture2D icon, string textIcon = null)
        {
            m_IconElement = new Image { scaleMode = ScaleMode.ScaleToFit };
            m_IconElement.AddToClassList(iconClassName);

            m_TextIconElement = new TextElement();
            m_TextIconElement.name = k_TextIconElementName;
            m_TextIconElement.AddToClassList(textIconClassName);

            m_TextElement = new TextElement();
            m_TextElement.name = k_TextElementName;
            m_TextElement.AddToClassList(textClassName);


            Add(m_IconElement);
            Add(m_TextIconElement);
            Add(m_TextElement);

            this.text = text;
            this.icon = icon;

            UpdateIcon();
            RegisterCallback<GeometryChangedEvent>(GeometryChanged);
        }

        // Since it is possible that a button icon is set through styling, we want to update again when layout/styling is done.
        void GeometryChanged(GeometryChangedEvent evt)
        {
            UpdateIcon();
        }

        void UpdateIcon()
        {
            EditorToolbarUtility.UpdateIconContent(
                m_Text,
                m_TextIcon,
                m_Icon,
                m_TextElement,
                m_TextIconElement,
                m_IconElement
            );
        }
    }
}
