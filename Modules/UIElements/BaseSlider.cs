// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    public enum SliderDirection
    {
        Horizontal,
        Vertical
    }

    public abstract class BaseSlider<T> : BaseField<T>
        where T : System.IComparable<T>
    {
        internal VisualElement dragElement { get; private set; }

        private T m_LowValue;
        public T lowValue
        {
            get { return m_LowValue; }
            set
            {
                if (!EqualityComparer<T>.Default.Equals(m_LowValue, value))
                {
                    m_LowValue = value;
                    ClampValue();
                    UpdateDragElementPosition();
                }
            }
        }

        private T m_HighValue;
        public T highValue
        {
            get { return m_HighValue; }
            set
            {
                if (!EqualityComparer<T>.Default.Equals(m_HighValue, value))
                {
                    m_HighValue = value;
                    ClampValue();
                    UpdateDragElementPosition();
                }
            }
        }

        public T range { get { return SliderRange(); } }

        private float m_PageSize;
        public virtual float pageSize
        {
            get { return m_PageSize; }
            set { m_PageSize = value; }
        }

        internal ClampedDragger<T> clampedDragger { get; private set; }
        Rect m_DragElementStartPos;

        T Clamp(T value, T lowBound, T highBound)
        {
            T result = value;
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

        public override T value
        {
            get { return base.value; }
            set
            {
                // Clamp the value around the real lowest and highest range values.
                T lowest = lowValue, highest = highValue;
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
                    RemoveFromClassList("vertical");
                    AddToClassList("horizontal");
                }
                else
                {
                    RemoveFromClassList("horizontal");
                    AddToClassList("vertical");
                }
            }
        }
        internal const float kDefaultPageSize = 0.0f;
        public BaseSlider(T start, T end, SliderDirection direction, float pageSize = kDefaultPageSize)
        {
            this.direction = direction;
            this.pageSize = pageSize;
            lowValue = start;
            highValue = end;

            Add(new VisualElement() { name = "TrackElement" });

            dragElement = new VisualElement() { name = "DragElement" };
            dragElement.RegisterCallback<GeometryChangedEvent>(UpdateDragElementPosition);

            Add(dragElement);

            clampedDragger = new ClampedDragger<T>(this, SetSliderValueFromClick, SetSliderValueFromDrag);
            this.AddManipulator(clampedDragger);
        }

        private void ClampValue()
        {
            // The property setter takes care of this
            value = m_Value;
        }

        internal abstract T SliderLerpUnclamped(T a, T b, float interpolant);
        internal abstract float SliderNormalizeValue(T currentValue, T lowerValue, T higherValue);
        internal abstract T SliderRange();

        // Handles slider drags
        void SetSliderValueFromDrag()
        {
            if (clampedDragger.dragDirection != ClampedDragger<T>.DragDirection.Free)
                return;

            var delta = clampedDragger.delta;

            if (direction == SliderDirection.Horizontal)
                ComputeValueAndDirectionFromDrag(layout.width, dragElement.style.width, m_DragElementStartPos.x + delta.x);
            else
                ComputeValueAndDirectionFromDrag(layout.height, dragElement.style.height, m_DragElementStartPos.y + delta.y);
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
            if (clampedDragger.dragDirection == ClampedDragger<T>.DragDirection.Free)
                return;

            if (clampedDragger.dragDirection == ClampedDragger<T>.DragDirection.None)
            {
                if (Mathf.Approximately(pageSize, 0.0f))
                {
                    // Jump drag element to current mouse position when user clicks on slider and pageSize == 0
                    var x = (direction == SliderDirection.Horizontal) ?
                        clampedDragger.startMousePosition.x - (dragElement.style.width / 2f) : dragElement.style.positionLeft.value;
                    var y = (direction == SliderDirection.Horizontal) ?
                        dragElement.style.positionTop.value : clampedDragger.startMousePosition.y - (dragElement.style.height / 2f);

                    dragElement.style.positionLeft = x;
                    dragElement.style.positionTop = y;
                    m_DragElementStartPos = new Rect(x, y, dragElement.style.width, dragElement.style.height);

                    // Manipulation becomes a free form drag
                    clampedDragger.dragDirection = ClampedDragger<T>.DragDirection.Free;
                    if (direction == SliderDirection.Horizontal)
                        ComputeValueAndDirectionFromDrag(layout.width, dragElement.style.width, m_DragElementStartPos.x);
                    else
                        ComputeValueAndDirectionFromDrag(layout.height, dragElement.style.height, m_DragElementStartPos.y);
                    return;
                }

                m_DragElementStartPos = new Rect(dragElement.style.positionLeft, dragElement.style.positionTop, dragElement.style.width, dragElement.style.height);
            }

            if (direction == SliderDirection.Horizontal)
                ComputeValueAndDirectionFromClick(layout.width, dragElement.style.width, dragElement.style.positionLeft, clampedDragger.lastMousePosition.x);
            else
                ComputeValueAndDirectionFromClick(layout.height, dragElement.style.height, dragElement.style.positionTop, clampedDragger.lastMousePosition.y);
        }

        internal virtual void ComputeValueAndDirectionFromClick(float sliderLength, float dragElementLength, float dragElementPos, float dragElementLastPos)
        {
            var totalRange = sliderLength - dragElementLength;
            if (Mathf.Abs(totalRange) < Mathf.Epsilon)
                return;

            if ((dragElementLastPos < dragElementPos) &&
                (clampedDragger.dragDirection != ClampedDragger<T>.DragDirection.LowToHigh))
            {
                clampedDragger.dragDirection = ClampedDragger<T>.DragDirection.HighToLow;
                float normalizedDragElementPosition = Mathf.Max(0f, Mathf.Min(dragElementPos - pageSize, totalRange)) / totalRange;
                value = SliderLerpUnclamped(lowValue, highValue, normalizedDragElementPosition);
            }
            else if ((dragElementLastPos > (dragElementPos + dragElementLength)) &&
                     (clampedDragger.dragDirection != ClampedDragger<T>.DragDirection.HighToLow))
            {
                clampedDragger.dragDirection = ClampedDragger<T>.DragDirection.LowToHigh;
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
                IStyle elemStyles = dragElement.style;
                dragElement.visible = true;

                // Any factor smaller than 1f will necessitate a drag element
                if (direction == SliderDirection.Horizontal)
                {
                    // Make sure the minimum width of drag element is honoured
                    float elemMinWidth = elemStyles.minWidth.GetSpecifiedValueOrDefault(0.0f);
                    elemStyles.width = Mathf.Max(layout.width * factor, elemMinWidth);
                }
                else
                {
                    // Make sure the minimum height of drag element is honoured
                    float elemMinHeight = elemStyles.minHeight.GetSpecifiedValueOrDefault(0.0f);
                    elemStyles.height = Mathf.Max(layout.height * factor, elemMinHeight);
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

        public override void OnPersistentDataReady()
        {
            base.OnPersistentDataReady();
            UpdateDragElementPosition();
        }

        void UpdateDragElementPosition()
        {
            // UpdateDragElementPosition() might be called at times where we have no panel
            // we must skip the position calculation and wait for a layout pass
            if (panel == null)
                return;

            float normalizedPosition = SliderNormalizeValue(value, lowValue, highValue);
            float dragElementWidth = dragElement.style.width;
            float dragElementHeight = dragElement.style.height;

            if (direction == SliderDirection.Horizontal)
            {
                float totalWidth = layout.width - dragElementWidth;
                dragElement.style.positionLeft = normalizedPosition * totalWidth;
            }
            else
            {
                float totalHeight = layout.height - dragElementHeight;
                dragElement.style.positionTop = normalizedPosition * totalHeight;
            }
        }

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt.GetEventTypeId() == GeometryChangedEvent.TypeId())
            {
                UpdateDragElementPosition((GeometryChangedEvent)evt);
            }
        }
    }
}
