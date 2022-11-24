// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    class EditorToolbarIcon : VisualElement
    {
        const string m_IconClassName = "unity-editor-toolbar-icon";
        const string m_TextIconClassName = EditorToolbar.elementTextIconClassName;
        const string m_ImageIconClassName = EditorToolbar.elementIconClassName;

        TextElement m_TextIconElement;
        Image m_IconElement;

        public string textIcon
        {
            get => m_TextIconElement?.text;

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    m_TextIconElement?.RemoveFromHierarchy();
                    m_TextIconElement = null;
                    return;
                }

                if (m_TextIconElement == null)
                {
                    m_IconElement?.RemoveFromHierarchy();
                    m_IconElement = null;

                    m_TextIconElement = new TextElement();
                    m_TextIconElement.AddToClassList(m_TextIconClassName);
                    Add(m_TextIconElement);
                }

                m_TextIconElement.text = OverlayUtilities.GetSignificantLettersForIcon(value);
            }
        }

        public Texture2D icon
        {
            get => (Texture2D)m_IconElement?.image;
            set
            {
                if (value == null)
                {
                    m_IconElement?.RemoveFromHierarchy();
                    m_IconElement = null;
                    return;
                }

                if (m_IconElement == null)
                {
                    m_TextIconElement?.RemoveFromHierarchy();
                    m_TextIconElement = null;

                    m_IconElement = new Image { scaleMode = ScaleMode.ScaleToFit };
                    m_IconElement.AddToClassList(m_ImageIconClassName);
                    Add(m_IconElement);
                }

                m_IconElement.image = value;
            }
        }

        public EditorToolbarIcon() : this(string.Empty, null) {}
        public EditorToolbarIcon(string text) : this(text, null) {}

        public EditorToolbarIcon(Texture2D icon) : this(string.Empty, icon) {}

        private EditorToolbarIcon(string text, Texture2D icon)
        {
            AddToClassList(m_IconClassName);

            if (icon != null)
                this.icon = icon;
            else
                this.textIcon = text;
        }
    }
}
