// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Properties;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements.HierarchyV2
{
    internal class ScrollerSlider : BaseSlider<double>
    {
        public ScrollerSlider(double start, double end, SliderDirection direction, float pageSize) : base(null,  start, end, direction, pageSize)
        {
        }

        internal override double SliderLerpUnclamped(double a, double b, float interpolant)
        {
            var newValue =  a + (b - a) * interpolant;

            // The purpose of this code is to reproduce the same rounding as IMGUI, based on min/max values and container size.
            // Equivalent of UnityEditor.MathUtils.RoundBasedOnMinimumDifference
            var minDifference = Math.Abs((highValue - lowValue) / (dragContainer.resolvedStyle.width - dragElement.resolvedStyle.width));
            var numOfDecimalsForMinDifference = minDifference == 0.0f ?
                Math.Clamp((int)(5.0 - Math.Log10(Math.Abs(minDifference))), 0, 15) :
                (int)Math.Clamp(-Math.Floor(Math.Log10(Math.Abs(minDifference))), 0, 15);
            var valueRoundedBasedOnMinimumDifference = Math.Round(newValue, numOfDecimalsForMinDifference, MidpointRounding.AwayFromZero);
            return valueRoundedBasedOnMinimumDifference;
        }

        internal override float SliderNormalizeValue(double currentValue, double lowerValue, double higherValue)
        {
            var range = higherValue - lowerValue;

            // Avoid divide by zero
            if (Math.Abs(range) < 0.00001)
                return 1.0f;

            var ratio = (currentValue - lowerValue) / range;

            // Ensure that the dragElement of the scrollbar never goes beyond the limits even when mouse wheel scrolling with
            // elastic animation.
            return (float)Math.Clamp(ratio, 0, 1);
        }

        internal override double SliderRange()
        {
            return Math.Abs(highValue - lowValue);
        }

        internal override double ParseStringToValue(string previousValue, string newValue)
        {
            if (UINumericFieldsUtils.TryConvertStringToDouble(newValue, previousValue, out var convertedValue, out _))
                return convertedValue;
            return 0;
        }

        static double GetClosestPowerOfTen(double positiveNumber)
        {
            if (positiveNumber <= 0)
                return 1;
            return Math.Pow(10, Math.Round(Math.Log10(positiveNumber)));
        }

        static double RoundToMultipleOf(double value, double roundingValue)
        {
            if (roundingValue == 0)
                return value;
            return Math.Round(value / roundingValue) * roundingValue;
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

            var isPageSize = sliderKey == SliderKey.LowerPage || sliderKey == SliderKey.HigherPage;

            // Change by approximately 1/100 of entire range, or 1/10 if holding down shift
            // But round to nearest power of ten to get nice resulting numbers.
            var delta = GetClosestPowerOfTen(Math.Abs((highValue - lowValue) * 0.01f));

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
            value = RoundToMultipleOf(value + (delta * 0.5001), Math.Abs(delta));
        }
    }

    /// <summary>
    /// A vertical or horizontal scrollbar.
    /// </summary>
    [VisibleToOtherModules("UnityEngine.HierarchyModule")]
    internal class CollectionViewScroller : VisualElement, INotifyValueChanged<double>
    {
        internal static readonly BindingId valueProperty = nameof(value);
        internal static readonly BindingId lowValueProperty = nameof(lowValue);
        internal static readonly BindingId highValueProperty = nameof(highValue);
        internal static readonly BindingId directionProperty = nameof(direction);

        const float k_DefaultPageSize = 20.0f;
        const double k_closeEnoughEpsilon = double.Epsilon * 16;
        float m_Factor = 1;

        /// <summary>
        /// The slider used by this scroller.
        /// </summary>
        ScrollerSlider slider { get; }

        /// <summary>
        /// Bottom or left scroll button.
        /// </summary>
        RepeatButton lowButton { get; }

        /// <summary>
        /// Top or right scroll button.
        /// </summary>
        RepeatButton highButton { get; }

        public void SetValueWithoutNotify(double newValue)
        {
            slider.SetValueWithoutNotify(newValue);
        }

        double INotifyValueChanged<double>.value
        {
            get => value;
            set => this.value = value;
        }

        /// <summary>
        /// Value that defines the slider position. It lies between <see cref="lowValue"/> and <see cref="highValue"/>.
        /// </summary>
        [CreateProperty]
        public double value
        {
            get => slider.value;
            set
            {
                if (!enabledSelf)
                    return;

                var previous = slider.value;
                slider.value = value;

                if (!Approximately(previous, slider.value))
                    NotifyPropertyChanged(valueProperty);
            }
        }

        /// <summary>
        /// Minimum value.
        /// </summary>
        [CreateProperty]
        public double lowValue
        {
            get => slider.lowValue;
            set
            {
                var previous = slider.lowValue;
                slider.lowValue = value;

                if (!Approximately(previous, slider.lowValue))
                    NotifyPropertyChanged(lowValueProperty);
            }
        }

        /// <summary>
        /// Maximum value.
        /// </summary>
        [CreateProperty]
        public double highValue
        {
            get => slider.highValue;
            set
            {
                var previous = slider.highValue;
                slider.highValue = value;

                if (!Approximately(previous, slider.highValue))
                    NotifyPropertyChanged(highValueProperty);
            }
        }

        /// <summary>
        /// Direction of this scrollbar.
        /// </summary>
        [CreateProperty]
        public SliderDirection direction
        {
            get => resolvedStyle.flexDirection == FlexDirection.Row ? SliderDirection.Horizontal : SliderDirection.Vertical;
            set
            {
                var previous = slider.direction;
                slider.direction = value;
                // We want default behavior for vertical scrollers to be lowValue at the top and highValue at the bottom,
                // instead of the default Slider behavior.
                slider.inverted = value == SliderDirection.Vertical;
                if (value == SliderDirection.Horizontal)
                {
                    style.flexDirection = FlexDirection.Row;
                    AddToClassList(Scroller.horizontalVariantUssClassNameUnique);
                    RemoveFromClassList(Scroller.verticalVariantUssClassNameUnique);
                }
                else
                {
                    style.flexDirection = FlexDirection.Column;
                    AddToClassList(Scroller.verticalVariantUssClassNameUnique);
                    RemoveFromClassList(Scroller.horizontalVariantUssClassNameUnique);
                }
                if (previous != slider.direction)
                    NotifyPropertyChanged(directionProperty);
            }
        }

        // Caches the last factor in cases where we need it in a later frame
        internal float factor
        {
            get { return m_Factor; }
            set
            {
                if (Mathf.Approximately(m_Factor, value))
                    return;

                m_Factor = value;
                Adjust();
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public CollectionViewScroller() : this(0, 0) {}

        /// <summary>
        /// Constructor.
        /// </summary>
        public CollectionViewScroller(double lowValue, double highValue, SliderDirection direction = SliderDirection.Vertical)
        {
            scrollSize = k_DefaultPageSize;

            AddToClassList(Scroller.ussClassNameUnique);

            // Add children in correct order
            slider = new ScrollerSlider(lowValue, highValue, direction, k_DefaultPageSize)
            {
                name = "unity-slider",
                viewDataKey = "Slider"
            };
            slider.AddToClassList(Scroller.sliderUssClassNameUnique);

            lowButton = new RepeatButton(ScrollPageUp, ScrollWaitDefinitions.firstWait, ScrollWaitDefinitions.regularWait) { name = "unity-low-button" };
            lowButton.AddToClassList(Scroller.lowButtonUssClassNameUnique);
            Add(lowButton);
            highButton = new RepeatButton(ScrollPageDown, ScrollWaitDefinitions.firstWait, ScrollWaitDefinitions.regularWait) { name = "unity-high-button" };
            highButton.AddToClassList(Scroller.highButtonUssClassNameUnique);
            Add(highButton);
            Add(slider);

            this.direction = direction;
        }

        /// <summary>
        /// Updates the slider element size as a ratio of total range. A value greater than or equal to 1 will disable the Scroller.
        /// </summary>
        public void Adjust()
        {
            // Any factor smaller than 1f will enable the scroller (and its children)
            SetEnabled(factor < 1f);
            slider.AdjustDragElement(factor);
        }

        // value change for a button click or repeat action
        public double scrollSize { get; set; }

        /// <summary>
        /// Will change the value according to the current slider pageSize.
        /// </summary>
        public void ScrollPageUp()
        {
            ScrollPage(-1.0f);
        }

        /// <summary>
        /// Will change the value according to the current slider pageSize.
        /// </summary>
        public void ScrollPageDown()
        {
            ScrollPage(1.0f);
        }

        /// <summary>
        /// Will change the value according to the current slider pageSize.
        /// </summary>
        public void ScrollPage(double factor)
        {
            value += factor * (scrollSize * (slider.lowValue < slider.highValue ? 1f : -1f));
        }

        /// <summary>
        /// Will return true if the value's difference are below the epsilon.
        /// </summary>
        public bool Approximately(double a, double b)
        {
            var diff = Math.Abs(a - b);
            return diff < k_closeEnoughEpsilon;
        }
    }
}
