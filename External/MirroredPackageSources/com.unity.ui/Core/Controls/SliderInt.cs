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
                if (lowValue > highValue)
                {
                    adjustedPageDirection = -adjustedPageDirection;
                }

                if ((dragElementLastPos < dragElementPos) &&
                    (clampedDragger.dragDirection != ClampedDragger<int>.DragDirection.LowToHigh))
                {
                    clampedDragger.dragDirection = ClampedDragger<int>.DragDirection.HighToLow;
                    // Compute the next value based on the page size.
                    value = value - adjustedPageDirection;
                }
                else if ((dragElementLastPos > (dragElementPos + dragElementLength)) &&
                         (clampedDragger.dragDirection != ClampedDragger<int>.DragDirection.HighToLow))
                {
                    clampedDragger.dragDirection = ClampedDragger<int>.DragDirection.LowToHigh;
                    // Compute the next value based on the page size.
                    value = value + adjustedPageDirection;
                }
            }
        }
    }
}
