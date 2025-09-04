// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    sealed class EditorToolbarContent
    {
        enum IconMode
        {
            Texture,
            TextIcon,
            StylesheetDefined,
            SignificantLetters,
            None
        }

        internal const string textClassName = EditorToolbar.elementLabelClassName;
        internal const string textIconClassName = EditorToolbar.elementTextIconClassName;
        internal const string iconClassName = EditorToolbar.elementIconClassName;
        internal const string textElementName = "EditorToolbarButtonText";
        internal const string textIconElementName = "EditorToolbarButtonTextIcon";

        EditorToolbarIcon m_Icon;
        string m_Text;
        Image m_IconElement;
        TextElement m_TextIconElement;
        TextElement m_TextElement;

        internal TextElement textIconElement => m_TextIconElement;
        internal TextElement textElement => m_TextElement;
        internal Image iconElement => m_IconElement;

        public EditorToolbarIcon icon
        {
            get => m_Icon;
            set
            {
                if (m_Icon.Equals(value))
                    return;

                m_Icon = value;
                UpdateIcon();
            }
        }

        public string text
        {
            get => m_Text;
            set
            {
                if (m_Text == value)
                    return;

                m_Text = value;
                UpdateText();
                UpdateIcon();
            }
        }

        public EditorToolbarContent(VisualElement root, string text) : this(root, text, default) {}
        public EditorToolbarContent(VisualElement root, EditorToolbarIcon icon) : this(root, default, icon) { }

        public EditorToolbarContent(VisualElement root, string text, EditorToolbarIcon icon)
        {
            m_IconElement = new Image { scaleMode = ScaleMode.ScaleToFit };
            m_IconElement.AddToClassList(iconClassName);

            m_TextIconElement = new TextElement();
            m_TextIconElement.name = textIconElementName;
            m_TextIconElement.AddToClassList(textIconClassName);

            m_TextElement = new TextElement();
            m_TextElement.name = textElementName;
            m_TextElement.AddToClassList(textClassName);

            m_Text = text;
            m_Icon = icon;

            root.Add(m_IconElement);
            root.Add(m_TextIconElement);
            root.Add(m_TextElement);

            root.RegisterCallback<GeometryChangedEvent>(GeometryChanged);
            UpdateText();
            UpdateIcon();
        }

        // Since it is possible that a button icon is set through styling, we want to update again when layout/styling is done.
        void GeometryChanged(GeometryChangedEvent evt)
        {
            UpdateIcon();
        }

        IconMode GetIconMode()
        {
            if (icon.textureIcon != null)
                return IconMode.Texture;

            if (!string.IsNullOrEmpty(icon.textIcon))
                return IconMode.TextIcon;

            if (m_IconElement.resolvedStyle.backgroundImage != null)
                return IconMode.StylesheetDefined;

            if (m_TextElement.resolvedStyle.display == DisplayStyle.None && !string.IsNullOrEmpty(text))
                return IconMode.SignificantLetters;

            return IconMode.None;
        }

        void UpdateText()
        {
            m_TextElement.text = m_Text;
            m_TextElement.style.display = !string.IsNullOrEmpty(m_Text) ? StyleKeyword.Null : DisplayStyle.None;
        }

        void UpdateIcon()
        {
            var mode = GetIconMode();
            var showIconElement = false;
            var showIconTextElement = false;

            switch (mode)
            {
                case IconMode.Texture:
                    showIconElement = true;
                    m_IconElement.image = icon.textureIcon;
                    m_TextIconElement.text = "";
                    break;

                case IconMode.TextIcon:
                    showIconTextElement = true;
                    m_TextIconElement.text = OverlayUtilities.GetSignificantLettersForIcon(icon.textIcon);
                    m_IconElement.image = null;
                    break;

                case IconMode.StylesheetDefined:
                    showIconElement = true;
                    m_IconElement.image = null;
                    m_TextIconElement.text = "";
                    break;

                case IconMode.SignificantLetters:
                    showIconTextElement = true;
                    m_TextIconElement.text = OverlayUtilities.GetSignificantLettersForIcon(text);
                    m_IconElement.image = null;
                    break;

                case IconMode.None:
                    m_IconElement.image = null;
                    m_TextIconElement.text = "";
                    break;
            }

            m_IconElement.style.display = showIconElement ? StyleKeyword.Null : DisplayStyle.None;
            m_TextIconElement.style.display = showIconTextElement ? StyleKeyword.Null : DisplayStyle.None;
        }
    }
}
