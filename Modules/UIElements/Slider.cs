// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    public class Slider : BaseSlider<float>
    {
        public new class UxmlFactory : UxmlFactory<Slider, UxmlTraits> {}

        public new class UxmlTraits : BaseSlider<float>.UxmlTraits
        {
            UxmlFloatAttributeDescription m_LowValue = new UxmlFloatAttributeDescription { name = "low-value", obsoleteNames = new[] { "lowValue" } };
            UxmlFloatAttributeDescription m_HighValue = new UxmlFloatAttributeDescription { name = "high-value", obsoleteNames = new[] { "highValue" }, defaultValue = kDefaultHighValue };
            UxmlFloatAttributeDescription m_PageSize = new UxmlFloatAttributeDescription { name = "page-size", obsoleteNames = new[] { "pageSize" }, defaultValue = kDefaultPageSize };
            UxmlEnumAttributeDescription<SliderDirection> m_Direction = new UxmlEnumAttributeDescription<SliderDirection> { name = "direction", defaultValue = SliderDirection.Vertical };
            UxmlFloatAttributeDescription m_Value = new UxmlFloatAttributeDescription { name = "value" };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                Slider slider = ((Slider)ve);
                slider.lowValue = m_LowValue.GetValueFromBag(bag, cc);
                slider.highValue = m_HighValue.GetValueFromBag(bag, cc);
                slider.direction = m_Direction.GetValueFromBag(bag, cc);
                slider.pageSize = m_PageSize.GetValueFromBag(bag, cc);
                slider.SetValueWithoutNotify(m_Value.GetValueFromBag(bag, cc));
            }
        }

        Action<float> m_ValueChanged;
        public Action<float> valueChanged
        {
            get { return m_ValueChanged; }
            set
            {
                if ((value != null) && (m_ValueChanged == null))
                {
                    // Forward the clickEvent by the InternalOnValueChanged notification
                    OnValueChanged(InternalOnValueChanged);
                }
                else if ((value == null) && (m_ValueChanged != null))
                {
                    // Don't need to keep being notified...
                    UnregisterCallback<ChangeEvent<float>>(InternalOnValueChanged);
                }

                this.m_ValueChanged = value;
            }
        }

        internal const float kDefaultHighValue = 10.0f;

        public Slider()
            : this(0, kDefaultHighValue)
        {
        }

        public Slider(float start, float end, System.Action<float> valueChanged,
                      SliderDirection direction = SliderDirection.Horizontal, float pageSize = kDefaultPageSize) :
            this(start, end, direction, pageSize)
        {
            // We set this event last, so that the construction does not call it.
            this.valueChanged = valueChanged;
        }

        void InternalOnValueChanged(ChangeEvent<float> evt)
        {
            m_ValueChanged?.Invoke(value);
        }

        public Slider(float start, float end, SliderDirection direction = SliderDirection.Horizontal, float pageSize = kDefaultPageSize) :
            base(start, end, direction, pageSize)
        {
        }

        internal override float SliderLerpUnclamped(float a, float b, float interpolant)
        {
            return Mathf.LerpUnclamped(a, b, interpolant);
        }

        internal override float SliderNormalizeValue(float currentValue, float lowerValue, float higherValue)
        {
            return (currentValue - lowerValue) / (higherValue - lowerValue);
        }

        internal override float SliderRange()
        {
            return Math.Abs(highValue - lowValue);
        }
    }
}
