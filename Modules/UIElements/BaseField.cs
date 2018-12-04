// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    public abstract class BaseField<TValueType> : BindableElement, INotifyValueChanged<TValueType>
    {
        public new class UxmlFactory : UxmlFactory<BaseField<TValueType>, UxmlTraits> {}
        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Label = new UxmlStringAttributeDescription { name = "label" };

            public UxmlTraits()
            {
                focusIndex.defaultValue = 0;
                focusable.defaultValue = true;
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                ((BaseField<TValueType>)ve).label = m_Label.GetValueFromBag(bag, cc);
            }
        }

        public static readonly string ussClassName = "unity-base-field";
        public static readonly string labelUssClassName = ussClassName + "__label";
        public static readonly string inputUssClassName = ussClassName + "__input";
        public static readonly string noLabelVariantUssClassName = ussClassName + "--no-label";
        public static readonly string labelDraggerVariantUssClassName = labelUssClassName + "--with-dragger";


        private VisualElement m_VisualInput;

        internal VisualElement visualInput
        {
            get { return m_VisualInput; }
            set
            {
                // Get rid of the older value...
                if (m_VisualInput != null)
                {
                    if (m_VisualInput.parent == this)
                    {
                        m_VisualInput.RemoveFromHierarchy();
                    }

                    m_VisualInput = null;
                }

                // Handle the new value...
                if (value != null)
                {
                    m_VisualInput = value;
                }
                else
                {
                    m_VisualInput = new VisualElement() { pickingMode = PickingMode.Ignore };
                }
                m_VisualInput.focusable = true;
                m_VisualInput.AddToClassList(inputUssClassName);
                Add(m_VisualInput);
            }
        }

        [SerializeField]
        TValueType m_Value;

        protected TValueType rawValue
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        public virtual TValueType value
        {
            get
            {
                return m_Value;
            }
            set
            {
                if (!EqualityComparer<TValueType>.Default.Equals(m_Value, value))
                {
                    if (panel != null)
                    {
                        using (ChangeEvent<TValueType> evt = ChangeEvent<TValueType>.GetPooled(m_Value, value))
                        {
                            evt.target = this;
                            SetValueWithoutNotify(value);
                            SendEvent(evt);
                        }
                    }
                    else
                    {
                        SetValueWithoutNotify(value);
                    }
                }
            }
        }

        public Label labelElement { get; private set; }
        public string label
        {
            get
            {
                return labelElement.text;
            }
            set
            {
                if (labelElement.text != value)
                {
                    labelElement.text = value;

                    if (string.IsNullOrEmpty(labelElement.text))
                    {
                        AddToClassList(noLabelVariantUssClassName);
                        labelElement.RemoveFromHierarchy();
                    }
                    else
                    {
                        if (!Contains(labelElement))
                        {
                            Insert(0, labelElement);
                            RemoveFromClassList(noLabelVariantUssClassName);
                        }
                    }
                }
            }
        }

        internal BaseField(string label)
        {
            isCompositeRoot = true;
            focusable = true;
            tabIndex = 0;
            excludeFromFocusRing = true;
            delegatesFocus = true;

            AddToClassList(ussClassName);

            labelElement = new Label() { focusable = true, tabIndex = -1 };
            labelElement.AddToClassList(labelUssClassName);
            if (label != null)
            {
                this.label = label;
            }
            else
            {
                AddToClassList(noLabelVariantUssClassName);
            }

            m_VisualInput = null;
        }

        protected BaseField(string label, VisualElement visualInput)
            : this(label)
        {
            this.visualInput = visualInput;
        }

        public virtual void SetValueWithoutNotify(TValueType newValue)
        {
            m_Value = newValue;

            if (!string.IsNullOrEmpty(viewDataKey))
                SaveViewData();
            MarkDirtyRepaint();
        }

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();

            if (m_VisualInput != null)
            {
                var key = GetFullHierarchicalViewDataKey();
                var oldValue = m_Value;
                OverwriteFromViewData(this, key);

                if (!EqualityComparer<TValueType>.Default.Equals(oldValue, m_Value))
                {
                    using (ChangeEvent<TValueType> evt = ChangeEvent<TValueType>.GetPooled(oldValue, m_Value))
                    {
                        evt.target = this;
                        SendEvent(evt);
                    }
                }
            }
        }
    }

    public class BaseFieldTraits<TValueType, TValueUxmlAttributeType> : BaseField<TValueType>.UxmlTraits
        where TValueUxmlAttributeType : TypedUxmlAttributeDescription<TValueType>, new()
    {
        TValueUxmlAttributeType m_Value = new TValueUxmlAttributeType { name = "value" };

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            ((INotifyValueChanged<TValueType>)ve).SetValueWithoutNotify(m_Value.GetValueFromBag(bag, cc));
        }
    }
}
