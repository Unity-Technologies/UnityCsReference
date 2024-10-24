// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
        internal static readonly string toggleNoIconClassName = ussClassName + "-noicon";

        Texture2D m_OnIcon;
        Texture2D m_OffIcon;

        TextElement m_TextElement;
        TextElement m_TextIconElement;
        Image m_IconElement;

        public new string text
        {
            get => m_TextElement?.text;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    m_TextElement?.RemoveFromHierarchy();
                    m_TextElement = null;
                    return;
                }

                if (m_TextElement == null)
                {
                    m_TextElement = new TextElement();
                    m_TextElement.name = k_TextElementName;
                    m_TextElement.AddToClassList(textClassName);

                    var input = this.Q<VisualElement>(className: Toggle.inputUssClassName);
                    if (m_IconElement != null)
                        input.Insert(input.IndexOf(m_IconElement) + 1, m_TextElement);
                    else if (m_TextIconElement != null)
                        input.Insert(input.IndexOf(m_TextIconElement) + 1, m_TextElement);
                    else
                        input.Add(m_TextElement);
                }

                m_TextElement.text = value;
                UpdateIconState();
                UpdateTextIconState();
            }
        }

        public string textIcon
        {
            get => m_TextIconElement?.text;
            set
            {
                if (m_TextIconElement == null)
                {
                    m_IconElement?.RemoveFromHierarchy();
                    m_IconElement = null;
                    m_OffIcon = null;
                    m_OnIcon = null;

                    m_TextIconElement = new TextElement();
                    m_TextIconElement.name = k_TextIconElementName;
                    m_TextIconElement.AddToClassList(textIconClassName);

                    var input = this.Q<VisualElement>(className: Toggle.inputUssClassName);
                    if (m_TextElement != null)
                    {
                        m_TextElement.RemoveFromHierarchy();
                        input.Add(m_TextIconElement);
                        input.Add(m_TextElement);
                    }
                    else
                    {
                        input.Add(m_TextIconElement);
                    }
                }

                m_TextIconElement.text = OverlayUtilities.GetSignificantLettersForIcon(value);
                UpdateTextIconState();
            }
        }

        public Texture2D icon
        {
            get => offIcon;
            set
            {
                CreateIconElement();

                offIcon = onIcon = value;
                UpdateIconState();
            }
        }

        public Texture2D onIcon
        {
            get => m_OnIcon;
            set
            {
                CreateIconElement();

                m_OnIcon = value;
                UpdateIconState();
            }
        }

        public Texture2D offIcon
        {
            get => m_OffIcon;
            set
            {
                CreateIconElement();

                m_OffIcon = value;
                UpdateIconState();
            }
        }

        public EditorToolbarToggle() : this(string.Empty, null, null) {}
        public EditorToolbarToggle(string text) : this(text, null, null) {}

        public EditorToolbarToggle(Texture2D icon) : this(string.Empty, icon, icon) {}
        public EditorToolbarToggle(Texture2D onIcon, Texture2D offIcon) : this(string.Empty, onIcon, offIcon) {}

        public EditorToolbarToggle(string text, Texture2D onIcon, Texture2D offIcon)
        {
            AddToClassList(ussClassName);

            this.onIcon = onIcon;
            this.offIcon = offIcon;
            this.text = text;

            UpdateIconState();
        }

        public EditorToolbarToggle(string textIcon, string label)
        {
            AddToClassList(ussClassName);

            this.textIcon = textIcon;
            this.text = label;

            UpdateTextIconState();
        }

        public override void SetValueWithoutNotify(bool newValue)
        {
            var changed = newValue != value;
            base.SetValueWithoutNotify(newValue);
            if (changed)
                UpdateIcon();
        }

        void UpdateIcon()
        {
            if (m_IconElement == null)
                return;

            m_IconElement.image = value ? onIcon : offIcon;
        }

        void UpdateIconState()
        {
            if (m_IconElement == null)
                return;

            if (icon == null && (text != null && text != string.Empty))
            {
                if (!m_IconElement.ClassListContains(toggleNoIconClassName))
                    m_IconElement.AddToClassList(toggleNoIconClassName);
            }
            else if (icon && m_IconElement.ClassListContains(toggleNoIconClassName))
            {
                m_IconElement.RemoveFromClassList(toggleNoIconClassName);
            }

            UpdateIcon();
        }

        void UpdateTextIconState()
        {
            if (m_TextIconElement == null)
                return;

            if ((m_TextIconElement == null || string.IsNullOrEmpty(textIcon)) && (text != null && text != string.Empty))
            {
                if (!m_TextIconElement.ClassListContains(toggleNoIconClassName))
                    m_TextIconElement.AddToClassList(toggleNoIconClassName);
            }
            else if (m_TextIconElement != null && m_TextIconElement.ClassListContains(toggleNoIconClassName))
            {
                m_TextIconElement.RemoveFromClassList(toggleNoIconClassName);
            }
        }

        void CreateIconElement()
        {
            if (m_IconElement == null)
            {
                m_TextIconElement?.RemoveFromHierarchy();
                m_TextIconElement = null;

                var input = this.Q<VisualElement>(className: Toggle.inputUssClassName);
                m_IconElement = new Image { scaleMode = ScaleMode.ScaleToFit };
                m_IconElement.AddToClassList(iconClassName);

                if (m_TextElement != null)
                {
                    m_TextElement.RemoveFromHierarchy();
                    input.Add(m_IconElement);
                    input.Add(m_TextElement);
                }
                else
                {
                    input.Add(m_IconElement);
                }
            }
        }
    }
}
