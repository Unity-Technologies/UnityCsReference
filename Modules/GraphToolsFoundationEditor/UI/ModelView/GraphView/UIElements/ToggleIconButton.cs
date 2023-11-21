// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A toggle button with an icon that shows the state of the button.
    /// </summary>
    class ToggleIconButton : VisualElement, INotifyValueChanged<bool>
    {
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            #pragma warning disable 649
            [SerializeField] bool value;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags value_UxmlAttributeFlags;
            #pragma warning restore 649

            public override object CreateInstance() => new ToggleIconButton();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                if (ShouldWriteAttributeValue(value_UxmlAttributeFlags))
                {
                    var e = (ToggleIconButton)obj;
                    e.value = value;
                }
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
