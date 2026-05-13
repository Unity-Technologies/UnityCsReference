// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A slider containing Integer discrete values. For more information, refer to [[wiki:UIE-uxml-element-sliderInt|UXML element SliderInt]].
    /// </summary>
    [UxmlElement(libraryPath = "Controls")]
    [Icon("UIToolkit/Icons/SliderInt.png")]
    public partial class SliderInt : BaseSlider<int>
    {
        internal const int kDefaultHighValue = 10;

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-slider-int";
        internal new static readonly UniqueStyleString ussClassNameUnique = new(ussClassName);

        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        internal new static readonly UniqueStyleString labelUssClassNameUnique = new(labelUssClassName);

        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";
        internal new static readonly UniqueStyleString inputUssClassNameUnique = new(inputUssClassName);

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
            AddToClassList(ussClassNameUnique);
            labelElement.AddToClassList(labelUssClassNameUnique);
            visualInput.AddToClassList(inputUssClassNameUnique);
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

        /// <inheritdoc />
        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, int startValue)
        {
            double sensitivity = NumericFieldDraggerUtility.CalculateIntDragSensitivity(startValue, lowValue, highValue);
            float acceleration = NumericFieldDraggerUtility.Acceleration(speed == DeltaSpeed.Fast, speed == DeltaSpeed.Slow);
            long v = value;

            v += (long)Math.Round(NumericFieldDraggerUtility.NiceDelta(delta, acceleration) * sensitivity);
            value = (int)v;
        }

        internal override int SliderLerpUnclamped(int a, int b, float interpolant)
        {
            return Mathf.RoundToInt(Mathf.LerpUnclamped((float)a, (float)b, interpolant));
        }

        internal override float SliderNormalizeValue(int currentValue, int lowerValue, int higherValue)
        {
            // Avoid divide by zero
            if (higherValue - lowerValue == 0)
                return 1.0f;
            return ((float)currentValue - (float)lowerValue) / ((float)higherValue - (float)lowerValue);
        }

        internal override int SliderRange()
        {
            return Math.Abs(highValue - lowValue);
        }

        internal override int ParseStringToValue(string previousValue, string newValue)
        {
            var success = UINumericFieldsUtils.TryConvertStringToInt(newValue, previousValue, out var value, out var expression);
            expressionEvaluated?.Invoke(expression);
            return success ? value : 0;
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
                if (Mathf.Abs(totalRange) < UIRUtility.k_Epsilon)
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
