// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A slider containing floating point values. 
    /// </summary>
    /// <remarks>
    /// The Slider control is a horizontal or vertical bar with a handle that can be moved to select a value from a range.
    /// The range is defined by the @@lowValue@@ and @@highValue@@ properties. 
    ///\\
    ///\\
    /// The slider manages navigation events in a customized manner. When it detects <see cref="NavigationMoveEvent"/>.
    /// that align with the slider's direction, it adjusts the slider's value. If it detects a 
    /// <see cref="NavigationSubmitEvent"/>, it removes the BaseSlider_1::ref::movableUssClassName
    /// class from the dragger, causing all Navigation events to revert to their default behavior.
    /// A second navigation submit event re-enables the movableUssClassName class on the dragger and
    /// restores the previous customized behavior.
    ///\\
    ///\\
    /// For more information and code examples, refer to the [[wiki:UIE-uxml-element-Slider|UXML element Slider]] manual page.
    /// </remarks>
    /// <remarks>
    /// SA: [[MinMaxSlider]]
    /// </remarks>
    public class Slider : BaseSlider<float>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BaseSlider<float>.UxmlSerializedData
        {
            #pragma warning disable 649
            [SerializeField] float lowValue;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags lowValue_UxmlAttributeFlags;
            [SerializeField] float highValue;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags highValue_UxmlAttributeFlags;
            [SerializeField] float pageSize;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags pageSize_UxmlAttributeFlags;
            [SerializeField] bool showInputField;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags showInputField_UxmlAttributeFlags;
            [SerializeField] SliderDirection direction;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags direction_UxmlAttributeFlags;
            [SerializeField] bool inverted;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags inverted_UxmlAttributeFlags;
            #pragma warning restore 649

            public override object CreateInstance() => new Slider();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (Slider)obj;
                if (ShouldWriteAttributeValue(lowValue_UxmlAttributeFlags))
                    e.lowValue = lowValue;
                if (ShouldWriteAttributeValue(highValue_UxmlAttributeFlags))
                    e.highValue = highValue;
                if (ShouldWriteAttributeValue(direction_UxmlAttributeFlags))
                    e.direction = direction;
                if (ShouldWriteAttributeValue(pageSize_UxmlAttributeFlags))
                    e.pageSize = pageSize;
                if (ShouldWriteAttributeValue(showInputField_UxmlAttributeFlags))
                    e.showInputField = showInputField;
                if (ShouldWriteAttributeValue(inverted_UxmlAttributeFlags))
                    e.inverted = inverted;
            }
        }

        /// <summary>
        /// Instantiates a <see cref="Slider"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlFactory : UxmlFactory<Slider, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="Slider"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlTraits : UxmlTraits<UxmlFloatAttributeDescription>
        {
            UxmlFloatAttributeDescription m_LowValue = new UxmlFloatAttributeDescription { name = "low-value" };
            UxmlFloatAttributeDescription m_HighValue = new UxmlFloatAttributeDescription { name = "high-value", defaultValue = kDefaultHighValue };
            UxmlFloatAttributeDescription m_PageSize = new UxmlFloatAttributeDescription { name = "page-size", defaultValue = kDefaultPageSize };
            UxmlBoolAttributeDescription m_ShowInputField = new UxmlBoolAttributeDescription { name = "show-input-field", defaultValue = kDefaultShowInputField };
            UxmlEnumAttributeDescription<SliderDirection> m_Direction = new UxmlEnumAttributeDescription<SliderDirection> { name = "direction", defaultValue = SliderDirection.Horizontal };
            UxmlBoolAttributeDescription m_Inverted = new UxmlBoolAttributeDescription { name = "inverted", defaultValue = kDefaultInverted };

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
                f.inverted = m_Inverted.GetValueFromBag(bag, cc);

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
        /// Creates a new instance of a Slider.
        /// </summary>
        public Slider()
            : this((string)null, 0, kDefaultHighValue) {}

        /// <summary>
        /// Creates a new instance of a Slider.
        /// </summary>
        /// <param name="start">The minimum value that the slider encodes.</param>
        /// <param name="end">The maximum value that the slider encodes.</param>
        /// <param name="direction">The direction of the slider (Horizontal or Vertical).</param>
        /// <param name="pageSize">A generic page size used to change the value when clicking in the slider.</param>
        public Slider(float start, float end, SliderDirection direction = SliderDirection.Horizontal, float pageSize = kDefaultPageSize)
            : this(null, start, end, direction, pageSize) {}

        /// <summary>
        /// Creates a new instance of a Slider.
        /// </summary>
        /// <param name="label">The string representing the label that will appear beside the field.</param>
        /// <param name="start">The minimum value that the slider encodes.</param>
        /// <param name="end">The maximum value that the slider encodes.</param>
        /// <param name="direction">The direction of the slider (Horizontal or Vertical).</param>
        /// <param name="pageSize">A generic page size used to change the value when clicking in the slider.</param>
        public Slider(string label, float start = 0, float end = kDefaultHighValue, SliderDirection direction = SliderDirection.Horizontal, float pageSize = kDefaultPageSize)
            : base(label, start, end, direction, pageSize)
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }

        /// <inheritdoc />
        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, float startValue)
        {
            double sensitivity = NumericFieldDraggerUtility.CalculateFloatDragSensitivity(startValue, lowValue, highValue);
            float acceleration = NumericFieldDraggerUtility.Acceleration(speed == DeltaSpeed.Fast, speed == DeltaSpeed.Slow);
            double v = value;

            v += NumericFieldDraggerUtility.NiceDelta(delta, acceleration) * sensitivity;
            value = (float)v;
        }

        internal override float SliderLerpUnclamped(float a, float b, float interpolant)
        {
            var newValue = Mathf.LerpUnclamped(a, b, interpolant);

            // The purpose of this code is to reproduce the same rounding as IMGUI, based on min/max values and container size.
            // Equivalent of UnityEditor.MathUtils.RoundBasedOnMinimumDifference
            var minDifference = Mathf.Abs((highValue - lowValue) / (dragContainer.resolvedStyle.width - dragElement.resolvedStyle.width));
            var numOfDecimalsForMinDifference = minDifference == 0.0f ?
                Mathf.Clamp((int)(5.0 - Mathf.Log10(Mathf.Abs(minDifference))), 0, 15) :
                Mathf.Clamp(-Mathf.FloorToInt(Mathf.Log10(Mathf.Abs(minDifference))), 0, 15);
            var valueRoundedBasedOnMinimumDifference = (float)Math.Round(newValue, numOfDecimalsForMinDifference, MidpointRounding.AwayFromZero);
            return valueRoundedBasedOnMinimumDifference;
        }

        internal override float SliderNormalizeValue(float currentValue, float lowerValue, float higherValue)
        {
            var range = higherValue - lowerValue;

            // Avoid divide by zero
            if (Mathf.Approximately(range, 0.0f))
                return 1.0f;
            return (currentValue - lowerValue) / range;
        }

        internal override float SliderRange()
        {
            return Math.Abs(highValue - lowValue);
        }

        internal override float ParseStringToValue(string previousValue, string newValue)
        {
            if (UINumericFieldsUtils.TryConvertStringToFloat(newValue, previousValue, out var value))
                return value;
            return 0;
        }

        internal override void ComputeValueFromKey(SliderKey sliderKey, bool isShift)
        {
            switch (sliderKey)
            {
                case SliderKey.None:
                    return;
                case SliderKey.Lowest:
                    value = lowValue;
                    return;
                case SliderKey.Highest:
                    value = highValue;
                    return;
            }

            bool isPageSize = sliderKey == SliderKey.LowerPage || sliderKey == SliderKey.HigherPage;

            // Change by approximately 1/100 of entire range, or 1/10 if holding down shift
            // But round to nearest power of ten to get nice resulting numbers.
            var delta = GetClosestPowerOfTen(Mathf.Abs((highValue - lowValue) * 0.01f));
            if (isPageSize)
                delta *= pageSize;
            else if (isShift)
                delta *= 10;

            // Increment or decrement by just over half the delta.
            // This means that e.g. if delta is 1, incrementing from 1.0 will go to 2.0,
            // but incrementing from 0.9 is going to 1.0 rather than 2.0.
            // This feels more right since 1.0 is the "next" one.
            if (sliderKey == SliderKey.Lower || sliderKey == SliderKey.LowerPage)
                delta = -delta;

            // Now round to a multiple of our delta value so we get a round end result instead of just a round delta.
            value = RoundToMultipleOf(value + (delta * 0.5001f), Mathf.Abs(delta));
        }
    }
}
