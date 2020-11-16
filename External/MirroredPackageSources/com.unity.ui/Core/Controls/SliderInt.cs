using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A slider containing Integer discrete values.
    /// </summary>
    public class SliderInt : BaseSlider<int>
    {
        /// <summary>
        /// Instantiates a <see cref="SliderInt"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<SliderInt, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="SliderInt"/>.
        /// </summary>
        public new class UxmlTraits : BaseFieldTraits<int, UxmlIntAttributeDescription>
        {
            UxmlIntAttributeDescription m_LowValue = new UxmlIntAttributeDescription { name = "low-value" };
            UxmlIntAttributeDescription m_HighValue = new UxmlIntAttributeDescription { name = "high-value", defaultValue = kDefaultHighValue };
            UxmlIntAttributeDescription m_PageSize = new UxmlIntAttributeDescription { name = "page-size", defaultValue = (int)kDefaultPageSize };
            UxmlBoolAttributeDescription m_ShowInputField = new UxmlBoolAttributeDescription { name = "show-input-field", defaultValue = kDefaultShowInputField };
            UxmlEnumAttributeDescription<SliderDirection> m_Direction = new UxmlEnumAttributeDescription<SliderDirection> { name = "direction", defaultValue = SliderDirection.Horizontal };
            UxmlBoolAttributeDescription m_Inverted = new UxmlBoolAttributeDescription { name = "inverted", defaultValue = kDefaultInverted };

            /// <summary>
            /// Initialize <see cref="SliderInt"/> properties using values from the attribute bag.
            /// </summary>
            /// <param name="ve">The object to initialize.</param>
            /// <param name="bag">The bag of attributes.</param>
            /// <param name="cc">The creation context; unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                var f = (SliderInt)ve;

                f.lowValue = m_LowValue.GetValueFromBag(bag, cc);
                f.highValue = m_HighValue.GetValueFromBag(bag, cc);
                f.direction = m_Direction.GetValueFromBag(bag, cc);
                f.pageSize = m_PageSize.GetValueFromBag(bag, cc);
                f.showInputField = m_ShowInputField.GetValueFromBag(bag, cc);
                f.inverted = m_Inverted.GetValueFromBag(bag, cc);

                base.Init(ve, bag, cc);
            }
        }

        internal const int kDefaultHighValue = 10;

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-slider-int";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        /// <summary>
        /// Constructors for the <see cref="SliderInt"/>.
        /// </summary>
        public SliderInt()
            : this(null, 0, kDefaultHighValue) {}

        /// <summary>
        /// Constructors for the <see cref="SliderInt"/>.
        /// </summary>
        /// <param name="start">This is the low value of the slider.</param>
        /// <param name="end">This is the high value of the slider.</param>
        /// <param name="direction">This is the slider direction, horizontal or vertical.</param>
        /// <param name="pageSize">This is the number of values to change when the slider is clicked.</param>
        public SliderInt(int start, int end, SliderDirection direction = SliderDirection.Horizontal, float pageSize = kDefaultPageSize)
            : this(null, start, end, direction, pageSize) {}

        /// <summary>
        /// Constructors for the <see cref="SliderInt"/>.
        /// </summary>
        /// <param name="start">This is the low value of the slider.</param>
        /// <param name="end">This is the high value of the slider.</param>
        /// <param name="direction">This is the slider direction, horizontal or vertical.</param>
        /// <param name="pageSize">This is the number of values to change when the slider is clicked.</param>
        public SliderInt(string label, int start = 0, int end = kDefaultHighValue, SliderDirection direction = SliderDirection.Horizontal, float pageSize = kDefaultPageSize)
            : base(label, start, end, direction, pageSize)
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }

        /// <summary>
        /// The value to add or remove to the SliderInt.value when it is clicked.
        /// </summary>
        /// <remarks>
        /// This is casted to int.
        /// </remarks>
        public override float pageSize
        {
            get { return base.pageSize; }
            set { base.pageSize = Mathf.RoundToInt(value); }
        }

        internal override int SliderLerpUnclamped(int a, int b, float interpolant)
        {
            return Mathf.RoundToInt(Mathf.LerpUnclamped((float)a, (float)b, interpolant));
        }

        internal override float SliderNormalizeValue(int currentValue, int lowerValue, int higherValue)
        {
            return ((float)currentValue - (float)lowerValue) / ((float)higherValue - (float)lowerValue);
        }

        internal override int SliderRange()
        {
            return Math.Abs(highValue - lowValue);
        }

        internal override int ParseStringToValue(string stringValue)
        {
            int result;
            if (int.TryParse(stringValue, out result))
            {
                return result;
            }
            else
            {
                return 0;
            }
        }

        internal override void ComputeValueAndDirectionFromClick(float sliderLength, float dragElementLength, float dragElementPos, float dragElementLastPos)
        {
            if (Mathf.Approximately(pageSize, 0.0f))
            {
                base.ComputeValueAndDirectionFromClick(sliderLength, dragElementLength, dragElementPos, dragElementLastPos);
            }
            else
            {
                var totalRange = sliderLength - dragElementLength;
                if (Mathf.Abs(totalRange) < Mathf.Epsilon)
                    return;

                var adjustedPageDirection = (int)pageSize;
                if ((lowValue > highValue && !inverted) ||
                    (lowValue < highValue && inverted) ||
                    (direction == SliderDirection.Vertical && !inverted))
                {
                    adjustedPageDirection = -adjustedPageDirection;
                }

                var isPositionDecreasing = dragElementLastPos < dragElementPos;
                var isPositionIncreasing = dragElementLastPos > (dragElementPos + dragElementLength);
                var isDraggingHighToLow = inverted ? isPositionIncreasing : isPositionDecreasing;
                var isDraggingLowToHigh = inverted ? isPositionDecreasing : isPositionIncreasing;

                if (isDraggingHighToLow && (clampedDragger.dragDirection != ClampedDragger<int>.DragDirection.LowToHigh))
                {
                    clampedDragger.dragDirection = ClampedDragger<int>.DragDirection.HighToLow;
                    // Compute the next value based on the page size.
                    value = value - adjustedPageDirection;
                }
                else if (isDraggingLowToHigh && (clampedDragger.dragDirection != ClampedDragger<int>.DragDirection.HighToLow))
                {
                    clampedDragger.dragDirection = ClampedDragger<int>.DragDirection.LowToHigh;
                    // Compute the next value based on the page size.
                    value = value + adjustedPageDirection;
                }
            }
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
            if (delta < 1)
                delta = 1;
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
            value = Mathf.RoundToInt(RoundToMultipleOf(value + (delta * 0.5001f), Mathf.Abs(delta)));
        }
    }
}
