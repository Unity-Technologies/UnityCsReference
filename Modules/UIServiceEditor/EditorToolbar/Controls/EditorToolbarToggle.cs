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

        Texture2D m_OnIcon;
        Texture2D m_OffIcon;

        readonly TextElement m_TextElement;
        readonly Image m_IconElement;

        public new string text
        {
            get => m_TextElement.text;
            set => m_TextElement.text = value;
        }

        public Texture2D icon
        {
            get => offIcon;
            set => offIcon = onIcon = value;
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

        public EditorToolbarToggle(string text, Texture2D onIcon, Texture2D offIcon)
        {
            AddToClassList(ussClassName);
            var input = this.Q<VisualElement>(className: Toggle.inputUssClassName);

            m_IconElement = new Image { scaleMode = ScaleMode.ScaleToFit};
            m_IconElement.AddToClassList(EditorToolbar.elementIconClassName);
            input.Add(m_IconElement);

            m_TextElement = new TextElement();
            m_TextElement.AddToClassList(EditorToolbar.elementLabelClassName);
            input.Add(m_TextElement);

            this.text = text;
            m_OnIcon = onIcon;
            m_OffIcon = offIcon;

            UpdateIcon();
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
    }
}
