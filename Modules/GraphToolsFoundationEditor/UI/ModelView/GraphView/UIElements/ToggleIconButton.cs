// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A toggle button with an icon that shows the state of the button.
    /// </summary>
    class ToggleIconButton : VisualElement, INotifyValueChanged<bool>
    {
        /// <summary>
        /// Instantiates a <see cref="ToggleIconButton"/> using data from a UXML file.
        /// </summary>
        /// <remarks>
        /// This class is added to every <see cref="VisualElement"/> that is created from UXML.
        /// </remarks>
        public new class UxmlFactory : UxmlFactory<ToggleIconButton, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="ToggleIconButton"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the properties of a <see cref="ToggleIconButton"/> element that you can
        /// use in a UXML asset.
        /// </remarks>
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlBoolAttributeDescription m_Value;

            /// <summary>
            /// Constructor.
            /// </summary>
            public UxmlTraits()
            {
                m_Value = new UxmlBoolAttributeDescription { name = "value" };
            }

            /// <summary>
            /// Initialize <see cref="ToggleIconButton"/> properties using values from the attribute bag.
            /// </summary>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var value = m_Value.GetValueFromBag(bag, cc);
                ((ToggleIconButton)ve).value = value;
            }
        }

        public static readonly string ussClassName = "ge-toggle-icon-button";
        public static readonly string visibleUssClassName = ussClassName.WithUssModifier("visible");

        public static readonly string iconElementName = "icon";
        public static readonly string iconElementUssClassName = ussClassName.WithUssElement(iconElementName);

        protected bool m_Value;
        protected VisualElement m_Icon;
        Clickable m_Clickable;

        Clickable Clickable
        {
            get => m_Clickable;
            set => this.ReplaceManipulator(ref m_Clickable, value);
        }

        /// <inheritdoc />
        public bool value
        {
            get => m_Value;
            set
            {
                if (m_Value != value)
                {
                    using (var e = ChangeEvent<bool>.GetPooled(m_Value, value))
                    {
                        e.target = this;
                        SetValueWithoutNotify(value);
                        SendEvent(e);
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ToggleIconButton"/> class.
        /// </summary>
        public ToggleIconButton()
        {
            m_Value = false;

            this.AddStylesheet_Internal("ToggleIconButton.uss");
            AddToClassList(ussClassName);

            m_Icon = new VisualElement { name = iconElementName };
            m_Icon.AddToClassList(iconElementUssClassName);
            Add(m_Icon);

            Clickable = new Clickable(OnClick);
        }

        void OnClick()
        {
            value = !m_Value;
        }

        /// <inheritdoc />
        public virtual void SetValueWithoutNotify(bool newValue)
        {
            m_Value = newValue;
        }
    }
}
