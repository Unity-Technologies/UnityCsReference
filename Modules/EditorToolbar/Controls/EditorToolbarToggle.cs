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
    public class EditorToolbarToggle : ToolbarToggle
    {
        internal const string textIconClassName = EditorToolbar.elementTextIconClassName;
        internal const string textClassName = EditorToolbar.elementLabelClassName;
        internal const string iconClassName = EditorToolbar.elementIconClassName;

        internal const string k_TextIconElementName = "EditorToolbarToggleTextIcon";
        internal const string k_TextElementName = "EditorToolbarToggleText";

        public new const string ussClassName = "unity-editor-toolbar-toggle";

        Image m_IconElement;
        TextElement m_TextIconElement;
        TextElement m_TextElement;

        Texture2D m_OnIcon;
        Texture2D m_OffIcon;

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

        public string textIcon
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
            get => offIcon;
            set
            {
                offIcon = onIcon = value;
                UpdateIcon();
            }
        }

        public Texture2D onIcon
        {
            get => m_OnIcon;
            set
            {
                m_OnIcon = value;
                UpdateIcon();
            }
        }

        public Texture2D offIcon
        {
            get => m_OffIcon;
            set
            {
                m_OffIcon = value;
                UpdateIcon();
            }
        }

        public EditorToolbarToggle() : this(string.Empty, null, null) {}
        public EditorToolbarToggle(string text) : this(text, null, null) {}

        public EditorToolbarToggle(Texture2D icon) : this(string.Empty, icon, icon) {}
        public EditorToolbarToggle(Texture2D onIcon, Texture2D offIcon) : this(string.Empty, onIcon, offIcon) {}

        public EditorToolbarToggle(string textIcon, string label) : this(label, null, null)
        {
            this.textIcon = textIcon;
        }

        public EditorToolbarToggle(string text, Texture2D onIcon, Texture2D offIcon)
        {
            InitializeToggle(text, onIcon, offIcon, textIcon);
        }


        public override void SetValueWithoutNotify(bool newValue)
        {
            var changed = newValue != value;
            base.SetValueWithoutNotify(newValue);
            if (changed)
                UpdateIcon();
        }

        void InitializeToggle(string text, Texture2D onIcon, Texture2D offIcon, string textIcon)
        {
            AddToClassList(ussClassName);

            m_IconElement = new Image { scaleMode = ScaleMode.ScaleToFit };
            m_IconElement.AddToClassList(iconClassName);

            m_TextIconElement = new TextElement { name = k_TextIconElementName };
            m_TextIconElement.AddToClassList(textIconClassName);

            m_TextElement = new TextElement { name = k_TextElementName };
            m_TextElement.AddToClassList(textClassName);

            var container = this.Q<VisualElement>(className: Toggle.inputUssClassName);
            container.Add(m_IconElement);
            container.Add(m_TextIconElement);
            container.Add(m_TextElement);

            this.onIcon = onIcon;
            this.offIcon = offIcon;
            this.text = text;
            this.textIcon = textIcon;

            UpdateIcon();
            RegisterCallback<GeometryChangedEvent>(GeometryChanged);
        }

        // Since it is possible that a toggle icon is set through styling, we want to update again when layout/styling is done.
        void GeometryChanged(GeometryChangedEvent evt)
        {
            UpdateIcon();
        }

        void UpdateIcon()
        {
            EditorToolbarUtility.UpdateIconContent(
                m_Text,
                m_TextIcon,
                value ? onIcon : offIcon,
                m_TextElement,
                m_TextIconElement,
                m_IconElement
            );
        }
    }
}
