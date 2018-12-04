// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    public enum SliderDirection
    {
        Horizontal,
        Vertical
    }

    public abstract class BaseSlider<TValueType> : BaseField<TValueType>
        where TValueType : System.IComparable<TValueType>
    {
        internal VisualElement dragElement { get; private set; }

        private TValueType m_LowValue;
        public TValueType lowValue
        {
            get { return m_LowValue; }
            set
            {
                if (!EqualityComparer<TValueType>.Default.Equals(m_LowValue, value))
                {
                    m_LowValue = value;
                    ClampValue();
                    UpdateDragElementPosition();
                }
            }
        }

        private TValueType m_HighValue;
        public TValueType highValue
        {
            get { return m_HighValue; }
            set
            {
                if (!EqualityComparer<TValueType>.Default.Equals(m_HighValue, value))
                {
                    m_HighValue = value;
                    ClampValue();
                    UpdateDragElementPosition();
                }
            }
        }

        public TValueType range { get { return SliderRange(); } }

        private float m_PageSize;
        public virtual float pageSize
        {
            get { return m_PageSize; }
            set { m_PageSize = value; }
        }


        internal ClampedDragger<TValueType> clampedDragger { get; private set; }
        Rect m_DragElementStartPos;

        TValueType Clamp(TValueType value, TValueType lowBound, TValueType highBound)
        {
            TValueType result = value;
            if (lowBound.CompareTo(value) > 0)
            {
                result = lowBound;
            }
            else if (highBound.CompareTo(value) < 0)
            {
                result = highBound;
            }

            return result;
        }

        public override TValueType value
        {
            get { return base.value; }
            set
            {
                // Clamp the value around the real lowest and highest range values.
                TValueType lowest = lowValue, highest = highValue;
                if (lowest.CompareTo(highest) > 0)
                {
                    var t = lowest;
                    lowest = highest;
                    highest = t;
                }

                var newValue = Clamp(value, lowest, highest);
                base.value = newValue;

                UpdateDragElementPosition();
            }
        }

        private SliderDirection m_Direction;
        public SliderDirection direction
        {
            get { return m_Direction; }
            set
            {
                m_Direction = value;
                if (m_Direction == SliderDirection.Horizontal)
                {
                    RemoveFromClassList(verticalVariantUssClassName);
                    AddToClassList(horizontalVariantUssClassName);
                }
                else
                {
                    RemoveFromClassList(horizontalVariantUssClassName);
                    AddToClassList(verticalVariantUssClassName);
                }
            }
        }


        internal const float kDefaultPageSize = 0.0f;

        public new static readonly string ussClassName = "unity-base-slider";
        public new static readonly string labelUssClassName = ussClassName + "__label";
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public static readonly string horizontalVariantUssClassName = ussClassName + "--horizontal";
        public static readonly string verticalVariantUssClassName = ussClassName + "--vertical";
        public static readonly string trackerUssClassName = ussClassName + "__tracker";
        public static readonly string draggerUssClassName = ussClassName + "__dragger";

        internal BaseSlider(string label, TValueType start, TValueType end, SliderDirection direction = SliderDirection.Horizontal, float pageSize = kDefaultPageSize)
            : base(label, null)
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);

            this.direction = direction;
            this.pageSize = pageSize;
            lowValue = start;
            highValue = end;
            pickingMode = PickingMode.Ignore;

            visualInput.pickingMode = PickingMode.Position;
            var trackElement = new VisualElement() { name = "unity-tracker" };
            trackElement.AddToClassList(trackerUssClassName);
            visualInput.Add(trackElement);

            dragElement = new VisualElement() { name = "unity-dragger" };
            dragElement.RegisterCallback<GeometryChangedEvent>(UpdateDragElementPosition);
            dragElement.AddToClassList(draggerUssClassName);

            visualInput.Add(dragElement);

            clampedDragger = new ClampedDragger<TValueType>(this, SetSliderValueFromClick, SetSliderValueFromDrag);
            visualInput.AddManipulator(clampedDragger);
        }

        private void ClampValue()
        {
            // The property setter takes care of this
            value = rawValue;
        }

        internal abstract TValueType SliderLerpUnclamped(TValueType a, TValueType b, float interpolant);
        internal abstract float SliderNormalizeValue(TValueType currentValue, TValueType lowerValue, TValueType higherValue);
        internal abstract TValueType SliderRange();

        // Handles slider drags
        void SetSliderValueFromDrag()
        {
            if (clampedDragger.dragDirection != ClampedDragger<TValueType>.DragDirection.Free)
                return;

            var delta = clampedDragger.delta;

            if (direction == SliderDirection.Horizontal)
                ComputeValueAndDirectionFromDrag(visualInput.layout.width, dragElement.computedStyle.width.value.value, m_DragElementStartPos.x + delta.x);
            else
                ComputeValueAndDirectionFromDrag(visualInput.layout.height, dragElement.computedStyle.height.value.value, m_DragElementStartPos.y + delta.y);
        }

        void ComputeValueAndDirectionFromDrag(float sliderLength, float dragElementLength, float dragElementPos)
        {
            var totalRange = sliderLength - dragElementLength;
            if (Mathf.Abs(totalRange) < Mathf.Epsilon)
                return;

            float normalizedDragElementPosition = Mathf.Max(0f, Mathf.Min(dragElementPos, totalRange)) / totalRange;
            value = SliderLerpUnclamped(lowValue, highValue, normalizedDragElementPosition);
        }

        // Handles slider clicks and page scrolls
        void SetSliderValueFromClick()
        {
            if (clampedDragger.dragDirection == ClampedDragger<TValueType>.DragDirection.Free)
                return;

            if (clampedDragger.dragDirection == ClampedDragger<TValueType>.DragDirection.None)
            {
                if (Mathf.Approximately(pageSize, 0.0f))
                {
                    // Jump drag element to current mouse position when user clicks on slider and pageSize == 0
                    var x = (direction == SliderDirection.Horizontal) ?
                        clampedDragger.startMousePosition.x - (dragElement.computedStyle.width.value.value / 2f) : (float)dragElement.computedStyle.left.value.value;
                    var y = (direction == SliderDirection.Horizontal) ?
                        (float)dragElement.computedStyle.top.value.value : clampedDragger.startMousePosition.y - (dragElement.computedStyle.height.value.value / 2f);

                    dragElement.style.left = x;
                    dragElement.style.top = y;
                    m_DragElementStartPos = new Rect(x, y, dragElement.computedStyle.width.value.value, dragElement.computedStyle.height.value.value);

                    // Manipulation becomes a free form drag
                    clampedDragger.dragDirection = ClampedDragger<TValueType>.DragDirection.Free;
                    if (direction == SliderDirection.Horizontal)
                        ComputeValueAndDirectionFromDrag(visualInput.layout.width, dragElement.computedStyle.width.value.value, m_DragElementStartPos.x);
                    else
                        ComputeValueAndDirectionFromDrag(visualInput.layout.height, dragElement.computedStyle.height.value.value, m_DragElementStartPos.y);
                    return;
                }

                m_DragElementStartPos = new Rect(dragElement.computedStyle.left.value.value, dragElement.computedStyle.top.value.value, dragElement.computedStyle.width.value.value, dragElement.computedStyle.height.value.value);
            }

            if (direction == SliderDirection.Horizontal)
                ComputeValueAndDirectionFromClick(visualInput.layout.width, dragElement.computedStyle.width.value.value, dragElement.computedStyle.left.value.value, clampedDragger.lastMousePosition.x);
            else
                ComputeValueAndDirectionFromClick(visualInput.layout.height, dragElement.computedStyle.height.value.value, dragElement.computedStyle.top.value.value, clampedDragger.lastMousePosition.y);
        }

        internal virtual void ComputeValueAndDirectionFromClick(float sliderLength, float dragElementLength, float dragElementPos, float dragElementLastPos)
        {
            var totalRange = sliderLength - dragElementLength;
            if (Mathf.Abs(totalRange) < Mathf.Epsilon)
                return;

            if ((dragElementLastPos < dragElementPos) &&
                (clampedDragger.dragDirection != ClampedDragger<TValueType>.DragDirection.LowToHigh))
            {
                clampedDragger.dragDirection = ClampedDragger<TValueType>.DragDirection.HighToLow;
                float normalizedDragElementPosition = Mathf.Max(0f, Mathf.Min(dragElementPos - pageSize, totalRange)) / totalRange;
                value = SliderLerpUnclamped(lowValue, highValue, normalizedDragElementPosition);
            }
            else if ((dragElementLastPos > (dragElementPos + dragElementLength)) &&
                     (clampedDragger.dragDirection != ClampedDragger<TValueType>.DragDirection.HighToLow))
            {
                clampedDragger.dragDirection = ClampedDragger<TValueType>.DragDirection.LowToHigh;
                float normalizedDragElementPosition = Mathf.Max(0f, Mathf.Min(dragElementPos + pageSize, totalRange)) / totalRange;
                value = SliderLerpUnclamped(lowValue, highValue, normalizedDragElementPosition);
            }
        }

        public void AdjustDragElement(float factor)
        {
            // Any factor greater or equal to 1f eliminates the need for a drag element
            bool needsElement = factor < 1f;
            dragElement.visible = needsElement;

            if (needsElement)
            {
                IStyle inlineStyles = dragElement.style;
                dragElement.visible = true;

                // Any factor smaller than 1f will necessitate a drag element
                if (direction == SliderDirection.Horizontal)
                {
                    // Make sure the minimum width of drag element is honoured
                    float elemMinWidth = resolvedStyle.minWidth == StyleKeyword.Auto ? 0 : resolvedStyle.minWidth.value;
                    inlineStyles.width = Mathf.Max(visualInput.layout.width * factor, elemMinWidth);
                }
                else
                {
                    // Make sure the minimum height of drag element is honoured
                    float elemMinHeight = resolvedStyle.minHeight == StyleKeyword.Auto ? 0 : resolvedStyle.minHeight.value;
                    inlineStyles.height = Mathf.Max(visualInput.layout.height * factor, elemMinHeight);
                }
            }
        }

        void UpdateDragElementPosition(GeometryChangedEvent evt)
        {
            // Only affected by dimension changes
            if (evt.oldRect.size == evt.newRect.size)
            {
                return;
            }

            UpdateDragElementPosition();
        }

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();
            UpdateDragElementPosition();
        }

        void UpdateDragElementPosition()
        {
            // UpdateDragElementPosition() might be called at times where we have no panel
            // we must skip the position calculation and wait for a layout pass
            if (panel == null)
                return;

            float normalizedPosition = SliderNormalizeValue(value, lowValue, highValue);
            float dragElementWidth = dragElement.computedStyle.width.value.value;
            float dragElementHeight = dragElement.computedStyle.height.value.value;

            if (direction == SliderDirection.Horizontal)
            {
                // This is the main calculation for the location of the thumbs / dragging element
                float offsetForThumbFullWidth = -dragElement.computedStyle.marginLeft.value.value - dragElement.computedStyle.marginRight.value.value;
                float totalWidth = visualInput.layout.width - dragElementWidth + offsetForThumbFullWidth;
                dragElement.style.left = normalizedPosition * totalWidth;
            }
            else
            {
                float totalHeight = visualInput.layout.height - dragElementHeight;
                dragElement.style.top = normalizedPosition * totalHeight;
            }
        }

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt == null)
            {
                return;
            }

            if (evt.eventTypeId == GeometryChangedEvent.TypeId())
            {
                UpdateDragElementPosition((GeometryChangedEvent)evt);
            }
        }
    }
}
