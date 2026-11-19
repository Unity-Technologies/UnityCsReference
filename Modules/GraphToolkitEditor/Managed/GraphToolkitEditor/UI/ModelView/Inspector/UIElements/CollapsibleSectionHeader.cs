// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// The header of a collapsible section. Has a title and reacts to clicks.
    /// </summary>
    [UnityRestricted]
    internal class CollapsibleSectionHeader : VisualElement, INotifyValueChanged<bool>
    {
        /// <summary>
        /// The USS class name added to a <see cref="CollapsibleSectionHeader"/>.
        /// </summary>
        public static readonly string ussClassName = "ge-collapsible-section-header";

        /// <summary>
        /// The USS class name added to the icon of a <see cref="CollapsibleSectionHeader"/>.
        /// </summary>
        public static readonly string iconElementUssClassName = ussClassName.WithUssElement(GraphElementHelper.iconName);

        /// <summary>
        /// The USS class name added to the label of a <see cref="CollapsibleSectionHeader"/>.
        /// </summary>
        public static readonly string labelElementUssClassName = ussClassName.WithUssElement(GraphElementHelper.labelName);

        /// <summary>
        /// The USS class name added to a <see cref="CollapsibleSectionHeader"/> when it is collapsed.
        /// </summary>
        public static readonly string collapsedUssClassName = ussClassName.WithUssModifier(GraphElementHelper.collapsedUssModifier);

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
        public string Text
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

            this.AddPackageStylesheet("CollapsibleSectionHeader.uss");
            AddToClassList(ussClassName);

            var icon = new VisualElement { name = GraphElementHelper.iconName };
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
