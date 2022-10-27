// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A toggle button with an arrow image that rotates with the state of the button.
    /// </summary>
    class CollapseButton : VisualElement, INotifyValueChanged<bool>
    {
        /// <summary>
        /// Instantiates a <see cref="CollapseButton"/> using data from a UXML file.
        /// </summary>
        /// <remarks>
        /// This class is added to every <see cref="VisualElement"/> that is created from UXML.
        /// </remarks>
        public new class UxmlFactory : UxmlFactory<CollapseButton, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="CollapseButton"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the properties of a CollapseButton element that you can
        /// use in a UXML asset.
        /// </remarks>
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlBoolAttributeDescription m_Collapsed;

            /// <summary>
            /// Constructor.
            /// </summary>
            public UxmlTraits()
            {
                m_Collapsed = new UxmlBoolAttributeDescription { name = "collapsed" };
            }

            /// <summary>
            /// Initialize <see cref="CollapseButton"/> properties using values from the attribute bag.
            /// </summary>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var collapsed = m_Collapsed.GetValueFromBag(bag, cc);
                ((CollapseButton)ve).value = collapsed;
            }
        }

        public static readonly string ussClassName = "ge-collapse-button";
        public static readonly string collapsedUssClassName = ussClassName.WithUssModifier("collapsed");

        public static readonly string iconElementName = "icon";
        public static readonly string iconElementUssClassName = ussClassName.WithUssElement(iconElementName);

        bool m_Collapsed;

        Clickable m_Clickable;

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
        /// Initializes a new instance of the <see cref="CollapseButton"/> class.
        /// </summary>
        public CollapseButton()
        {
            m_Collapsed = false;

            this.AddStylesheet_Internal("CollapseButton.uss");
            AddToClassList(ussClassName);

            var icon = new VisualElement { name = iconElementName };
            icon.AddToClassList(iconElementUssClassName);
            Add(icon);

            Clickable = new Clickable(OnClick);
        }

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
