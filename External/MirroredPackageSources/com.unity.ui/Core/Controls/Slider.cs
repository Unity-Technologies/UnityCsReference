using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A slider containing floating point values.
    /// </summary>
    public class Slider : BaseSlider<float>
    {
        /// <summary>
        /// Instantiates a <see cref="Slider"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<Slider, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="Slider"/>.
        /// </summary>
        public new class UxmlTraits : BaseFieldTraits<float, UxmlFloatAttributeDescription>
        {
            UxmlFloatAttributeDescription m_LowValue = new UxmlFloatAttributeDescription { name = "low-value" };
            UxmlFloatAttributeDescription m_HighValue = new UxmlFloatAttributeDescription { name = "high-value", defaultValue = kDefaultHighValue };
            UxmlFloatAttributeDescription m_PageSize = new UxmlFloatAttributeDescription { name = "page-size", defaultValue = kDefaultPageSize };
            UxmlBoolAttributeDescription m_ShowInputField = new UxmlBoolAttributeDescription { name = "show-input-field", defaultValue = kDefaultShowInputField };
            UxmlEnumAttributeDescription<SliderDirection> m_Direction = new UxmlEnumAttributeDescription<SliderDirection> { name = "direction", defaultValue = SliderDirection.Horizontal };

            /// <summary>
            /// Initialize <see cref="Slider"/> properties using values from the attribute bag.
            /// </summary>
            /// <param name="ve">The object to initialize.</param>
            /// <param name="bag">The attribute bag.</param>
            /// <param name="cc">The creation context; unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                var f = (Slider)ve;

                f.lowValue = m_LowValue.GetValueFromBag(bag, cc);
                f.highValue = m_HighValue.GetValueFromBag(bag, cc);
                f.direction = m_Direction.GetValueFromBag(bag, cc);
                f.pageSize = m_PageSize.GetValueFromBag(bag, cc);
                f.showInputField = m_ShowInputField.GetValueFromBag(bag, cc);

                base.Init(ve, bag, cc);
            }
        }

        internal const float kDefaultHighValue = 10.0f;

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-slider";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        /// <summary>
        /// Constructor.
        /// </summary>
        public Slider()
            : this((string)null, 0, kDefaultHighValue) {}

        /// <summary>
        /// Constructor.
        /// </summary>
        public Slider(float start, float end, SliderDirection direction = SliderDirection.Horizontal, float pageSize = kDefaultPageSize)
            : this(null, start, end, direction, pageSize) {}

        /// <summary>
        /// Constructor.
        /// </summary>
        public Slider(string label, float start = 0, float end = kDefaultHighValue, SliderDirection direction = SliderDirection.Horizontal, float pageSize = kDefaultPageSize)
            : base(label, start, end, direction, pageSize)
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }

        internal override float SliderLerpUnclamped(float a, float b, float interpolant)
        {
            return Mathf.RoundToInt(Mathf.LerpUnclamped(a, b, interpolant) * 100f) / 100f;
        }

        internal override float SliderNormalizeValue(float currentValue, float lowerValue, float higherValue)
        {
            return (currentValue - lowerValue) / (higherValue - lowerValue);
        }

        internal override float SliderRange()
        {
            return Math.Abs(highValue - lowValue);
        }

        internal override float ParseStringToValue(string stringValue)
        {
            float result;
            if (float.TryParse(stringValue, out result))
            {
                return result;
            }
            else
            {
                return 0f;
            }
        }
    }
}
