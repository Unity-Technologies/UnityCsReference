// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    public class Slider : BaseSlider<float>
    {
        public new class UxmlFactory : UxmlFactory<Slider, UxmlTraits> {}

        public new class UxmlTraits : BaseFieldTraits<float, UxmlFloatAttributeDescription>
        {
            UxmlFloatAttributeDescription m_LowValue = new UxmlFloatAttributeDescription { name = "low-value" };
            UxmlFloatAttributeDescription m_HighValue = new UxmlFloatAttributeDescription { name = "high-value", defaultValue = kDefaultHighValue };
            UxmlFloatAttributeDescription m_PageSize = new UxmlFloatAttributeDescription { name = "page-size", defaultValue = kDefaultPageSize };
            UxmlEnumAttributeDescription<SliderDirection> m_Direction = new UxmlEnumAttributeDescription<SliderDirection> { name = "direction", defaultValue = SliderDirection.Horizontal };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                var f = (Slider)ve;

                f.lowValue = m_LowValue.GetValueFromBag(bag, cc);
                f.highValue = m_HighValue.GetValueFromBag(bag, cc);
                f.direction = m_Direction.GetValueFromBag(bag, cc);
                f.pageSize = m_PageSize.GetValueFromBag(bag, cc);

                base.Init(ve, bag, cc);
            }
        }

        internal const float kDefaultHighValue = 10.0f;

        public new static readonly string ussClassName = "unity-slider";

        public Slider()
            : this((string)null, 0, kDefaultHighValue) {}

        public Slider(float start, float end, SliderDirection direction = SliderDirection.Horizontal, float pageSize = kDefaultPageSize)
            : this(null, start, end, direction, pageSize) {}

        public Slider(string label, float start = 0, float end = kDefaultHighValue, SliderDirection direction = SliderDirection.Horizontal, float pageSize = kDefaultPageSize)
            : base(label, start, end, direction, pageSize)
        {
            AddToClassList(ussClassName);
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
