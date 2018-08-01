// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    public class SliderInt : BaseSlider<int>
    {
        public new class UxmlFactory : UxmlFactory<SliderInt, UxmlTraits> {}

        public new class UxmlTraits : BaseSlider<int>.UxmlTraits
        {
            UxmlIntAttributeDescription m_LowValue = new UxmlIntAttributeDescription { name = "low-value" };
            UxmlIntAttributeDescription m_HighValue = new UxmlIntAttributeDescription { name = "high-value", defaultValue = kDefaultHighValue };
            UxmlIntAttributeDescription m_PageSize = new UxmlIntAttributeDescription { name = "page-size", defaultValue = (int)kDefaultPageSize };
            UxmlEnumAttributeDescription<SliderDirection> m_Direction = new UxmlEnumAttributeDescription<SliderDirection> { name = "direction", defaultValue = SliderDirection.Vertical };
            UxmlIntAttributeDescription m_Value = new UxmlIntAttributeDescription { name = "value" };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                SliderInt slider = ((SliderInt)ve);
                slider.lowValue = m_LowValue.GetValueFromBag(bag, cc);
                slider.highValue = m_HighValue.GetValueFromBag(bag, cc);
                slider.direction = m_Direction.GetValueFromBag(bag, cc);
                slider.pageSize = m_PageSize.GetValueFromBag(bag, cc);
                slider.SetValueWithoutNotify(m_Value.GetValueFromBag(bag, cc));
            }
        }

        internal const int kDefaultHighValue = 10;

        public SliderInt()
            : this(0, kDefaultHighValue) {}

        public SliderInt(int start, int end, SliderDirection direction = SliderDirection.Horizontal, float pageSize = kDefaultPageSize) :
            base(start, end, direction, pageSize)
        {
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
