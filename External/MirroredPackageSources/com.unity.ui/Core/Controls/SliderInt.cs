using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    public class SliderInt : BaseSlider<int>
    {
        public new class UxmlFactory : UxmlFactory<SliderInt, UxmlTraits> {}

        public new class UxmlTraits : BaseFieldTraits<int, UxmlIntAttributeDescription>
        {
            UxmlIntAttributeDescription m_LowValue = new UxmlIntAttributeDescription { name = "low-value" };
            UxmlIntAttributeDescription m_HighValue = new UxmlIntAttributeDescription { name = "high-value", defaultValue = kDefaultHighValue };
            UxmlIntAttributeDescription m_PageSize = new UxmlIntAttributeDescription { name = "page-size", defaultValue = (int)kDefaultPageSize };
            UxmlEnumAttributeDescription<SliderDirection> m_Direction = new UxmlEnumAttributeDescription<SliderDirection> { name = "direction", defaultValue = SliderDirection.Horizontal };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                var f = (SliderInt)ve;

                f.lowValue = m_LowValue.GetValueFromBag(bag, cc);
                f.highValue = m_HighValue.GetValueFromBag(bag, cc);
                f.direction = m_Direction.GetValueFromBag(bag, cc);
                f.pageSize = m_PageSize.GetValueFromBag(bag, cc);

                base.Init(ve, bag, cc);
            }
        }

        internal const int kDefaultHighValue = 10;

        public new static readonly string ussClassName = "unity-slider-int";
        public new static readonly string labelUssClassName = ussClassName + "__label";
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public SliderInt()
            : this(null, 0, kDefaultHighValue) {}

        public SliderInt(int start, int end, SliderDirection direction = SliderDirection.Horizontal, float pageSize = kDefaultPageSize)
            : this(null, start, end, direction, pageSize) {}

        public SliderInt(string label, int start = 0, int end = kDefaultHighValue, SliderDirection direction = SliderDirection.Horizontal, float pageSize = kDefaultPageSize)
            : base(label, start, end, direction, pageSize)
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }

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
