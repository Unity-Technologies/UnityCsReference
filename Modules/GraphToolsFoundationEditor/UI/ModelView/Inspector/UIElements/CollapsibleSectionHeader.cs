// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// The header of a collapsible section. Has a title and reacts to clicks.
    /// </summary>
    class CollapsibleSectionHeader : VisualElement, INotifyValueChanged<bool>
    {
        public static readonly string ussClassName = "ge-collapsible-section-header";
        public static readonly string iconElementUssClassName = ussClassName.WithUssElement("icon");
        public static readonly string labelElementUssClassName = ussClassName.WithUssElement("label");
        public static readonly string collapsedUssClassName = ussClassName.WithUssModifier("collapsed");

        bool m_Collapsed;

        Clickable m_Clickable;
        protected Label m_Label;

        /// <summary>
        /// The <see cref="Clickable"/> used to respond to clicks.
        /// </summary>
        protected Clickable Clickable
        {
            get => m_Clickable;
            set => this.ReplaceManipulator(ref m_Clickable, value);
        }

        /// <inheritdoc />
        public bool value
        {
            get => m_Collapsed;
            set
            {
                if (m_Collapsed != value)
                {
                    using (var e = ChangeEvent<bool>.GetPooled(m_Collapsed, value))
                    {
                        e.target = this;
                        SetValueWithoutNotify(value);
                        SendEvent(e);
                    }
                }
            }
        }

        /// <summary>
        /// The text displayed in the header.
        /// </summary>
        public string text
        {
            get => m_Label.text;
            set => m_Label.text = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CollapsibleSectionHeader"/> class.
        /// </summary>
        /// <param name="labelText">The text to display in the header.</param>
        public CollapsibleSectionHeader(string labelText = null)
        {
            m_Collapsed = false;

            this.AddStylesheet_Internal("CollapsibleSectionHeader.uss");
            AddToClassList(ussClassName);

            var icon = new VisualElement { name = "icon" };
            icon.AddToClassList(iconElementUssClassName);
            Add(icon);

            m_Label = new Label(labelText ?? "");
            m_Label.AddToClassList(labelElementUssClassName);
            Add(m_Label);

            Clickable = new Clickable(OnClick);
        }

        /// <summary>
        /// Callback for the click event.
        /// </summary>
        protected void OnClick()
        {
            value = !m_Collapsed;
        }

        /// <inheritdoc />
        public void SetValueWithoutNotify(bool newValue)
        {
            m_Collapsed = newValue;
            EnableInClassList(collapsedUssClassName, m_Collapsed);
        }
    }
}
