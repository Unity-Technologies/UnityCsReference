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
        public new const string ussClassName = "unity-editor-toolbar-toggle";

        EditorToolbarContent m_Content;
        Texture2D m_OnIcon;
        Texture2D m_OffIcon;

        public new string text
        {
            get => m_Content.text;
            set => m_Content.text = value;
        }

        public string textIcon
        {
            get => m_Content.icon.textIcon;
            set
            {
                m_Content.icon = new EditorToolbarIcon(value);
                m_OnIcon = m_OffIcon = null;
            }
        }

        public Texture2D icon
        {
            get => value ? m_OnIcon : m_OffIcon;
            set
            {
                m_OnIcon = m_OffIcon = value;
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

        public EditorToolbarToggle() : this(string.Empty, null, null) { }
        public EditorToolbarToggle(string text) : this(text, null, null) { }

        public EditorToolbarToggle(Texture2D icon) : this(string.Empty, icon, icon) { }
        public EditorToolbarToggle(Texture2D onIcon, Texture2D offIcon) : this(string.Empty, onIcon, offIcon) { }

        public EditorToolbarToggle(string textIcon, string label) : this(label, null, null)
        {
            this.textIcon = textIcon;
        }

        public EditorToolbarToggle(string text, Texture2D onIcon, Texture2D offIcon)
        {
            AddToClassList(ussClassName);
            AddToClassList(EditorToolbar.elementClassName);


            m_OnIcon = onIcon;
            m_OffIcon = offIcon;

            m_Content = new EditorToolbarContent(this.Q<VisualElement>(className: inputUssClassName), text);
            UpdateIcon();
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
            m_Content.icon = new EditorToolbarIcon(textIcon, value ? onIcon : offIcon);
        }
    }
}
