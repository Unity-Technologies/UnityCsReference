// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    public class EditorToolbarToggle : ToolbarToggle
    {
        internal const string textClassName = EditorToolbar.elementLabelClassName;
        internal const string iconClassName = EditorToolbar.elementIconClassName;
        public new const string ussClassName = "unity-editor-toolbar-toggle";
        internal static readonly string toggleNoIconClassName = ussClassName + "-noicon";

        Texture2D m_OnIcon;
        Texture2D m_OffIcon;

        TextElement m_TextElement;
        readonly Image m_IconElement;

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
                    Insert(IndexOf(m_IconElement)+1, m_TextElement = new TextElement());
                    m_TextElement.AddToClassList(textClassName);
                }

                m_TextElement.text = value;
                UpdateIconState();
            }
        }

        public Texture2D icon
        {
            get => offIcon;
            set
            {
                offIcon = onIcon = value;
                UpdateIconState();
            }
        }

        public Texture2D onIcon
        {
            get => m_OnIcon;
            set
            {
                m_OnIcon = value;
                UpdateIconState();
            }
        }

        public Texture2D offIcon
        {
            get => m_OffIcon;
            set
            {
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
            var input = this.Q<VisualElement>(className: Toggle.inputUssClassName);

            m_IconElement = new Image { scaleMode = ScaleMode.ScaleToFit};
            m_IconElement.AddToClassList(iconClassName);
            input.Add(m_IconElement);

            this.text = text;
            m_OnIcon = onIcon;
            m_OffIcon = offIcon;

            UpdateIconState();
        }

        public override void SetValueWithoutNotify(bool newValue)
        {
            base.SetValueWithoutNotify(newValue);
            UpdateIcon();
        }

        void UpdateIcon()
        {
            m_IconElement.image = value ? onIcon : offIcon;
        }

        void UpdateIconState()
        {
            if (icon == null && (text != null && text != string.Empty))
            {
                if (!m_IconElement.ClassListContains(toggleNoIconClassName))
                    m_IconElement.AddToClassList(toggleNoIconClassName);
            }
            else if (icon && m_IconElement.ClassListContains(toggleNoIconClassName))
                m_IconElement.RemoveFromClassList(toggleNoIconClassName);

            UpdateIcon();
        }
    }
}
