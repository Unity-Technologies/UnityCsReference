// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    public class Slider : VisualElement
    {
        public class SliderFactory : UxmlFactory<Slider, SliderUxmlTraits> {}

        public class SliderUxmlTraits : VisualElementUxmlTraits
        {
            UxmlFloatAttributeDescription m_LowValue;
            UxmlFloatAttributeDescription m_HighValue;
            UxmlFloatAttributeDescription m_PageSize;
            UxmlEnumAttributeDescription<Direction> m_Direction;
            UxmlFloatAttributeDescription m_Value;

            public SliderUxmlTraits()
            {
                m_LowValue = new UxmlFloatAttributeDescription { name = "lowValue" };
                m_HighValue = new UxmlFloatAttributeDescription { name = "highValue", defaultValue = kDefaultHighValue };
                m_PageSize = new UxmlFloatAttributeDescription { name = "pageSize", defaultValue = kDefaultPageSize};
                m_Direction = new UxmlEnumAttributeDescription<Slider.Direction> { name = "direction", defaultValue = Direction.Vertical };
                m_Value = new UxmlFloatAttributeDescription { name = "value" };
            }

            public override IEnumerable<UxmlAttributeDescription> uxmlAttributesDescription
            {
                get
                {
                    foreach (var attr in base.uxmlAttributesDescription)
                    {
                        yield return attr;
                    }

                    yield return m_LowValue;
                    yield return m_HighValue;
                    yield return m_PageSize;
                    yield return m_Direction;
                    yield return m_Value;
                }
            }

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                Slider slider = ((Slider)ve);
                slider.lowValue = m_LowValue.GetValueFromBag(bag);
                slider.highValue = m_HighValue.GetValueFromBag(bag);
                slider.direction = m_Direction.GetValueFromBag(bag);
                slider.pageSize = m_PageSize.GetValueFromBag(bag);
                slider.value = m_Value.GetValueFromBag(bag);
            }
        }

        public enum Direction
        {
            Horizontal,
            Vertical
        }

        public VisualElement dragElement { get; private set; }

        private float m_LowValue;
        public float lowValue
        {
            get { return m_LowValue; }
            set
            {
                if (!Mathf.Approximately(m_LowValue, value))
                {
                    m_LowValue = value;
                    ClampValue();
                    UpdateDragElementPosition();
                }
            }
        }

        private float m_HighValue;
        public float highValue
        {
            get { return m_HighValue; }
            set
            {
                if (!Mathf.Approximately(m_HighValue, value))
                {
                    m_HighValue = value;
                    ClampValue();
                    UpdateDragElementPosition();
                }
            }
        }

        public float range { get { return Math.Abs(highValue - lowValue); } }

        public float pageSize { get; set; }

        public event System.Action<float> valueChanged;
        // TODO refactor Slider to be UIElements-ish. Styles should be applied externally
        internal ClampedDragger clampedDragger { get; private set; }
        Rect m_DragElementStartPos;

        [Serializable]
        class SliderValue
        {
            public float m_Value = 0.0f;
        }
        SliderValue m_SliderValue;

        public float value
        {
            get { return m_SliderValue == null ? 0.0f : m_SliderValue.m_Value; }
            set
            {
                if (m_SliderValue == null)
                    m_SliderValue = new SliderValue() { m_Value = lowValue };

                // Clamp the value around the real lowest and highest range values.
                float lowest = lowValue, highest = highValue;
                if (lowest > highest)
                {
                    float t = lowest;
                    lowest = highest;
                    highest = t;
                }
                var newValue = Mathf.Clamp(value, lowest, highest);

                if (Mathf.Approximately(m_SliderValue.m_Value, newValue))
                    return;

                m_SliderValue.m_Value = newValue;

                UpdateDragElementPosition();

                if (valueChanged != null)
                    valueChanged(m_SliderValue.m_Value);

                this.Dirty(ChangeType.Repaint);

                SavePersistentData();
            }
        }

        private Direction m_Direction;
        public Direction direction
        {
            get { return m_Direction; }
            set
            {
                m_Direction = value;
                if (m_Direction == Direction.Horizontal)
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

        internal const float kDefaultHighValue = 10.0f;
        internal const float kDefaultPageSize = 10.0f;

        public Slider()
            : this(0, kDefaultHighValue, null) {}

        public Slider(float start, float end, System.Action<float> valueChanged,
                      Direction direction = Direction.Horizontal, float pageSize = kDefaultPageSize)
        {
            this.direction = direction;
            this.pageSize = pageSize;
            lowValue = start;
            highValue = end;

            Add(new VisualElement() { name = "TrackElement" });

            dragElement = new VisualElement() { name = "DragElement" };
            dragElement.RegisterCallback<GeometryChangedEvent>(UpdateDragElementPosition);

            Add(dragElement);

            clampedDragger = new ClampedDragger(this, SetSliderValueFromClick, SetSliderValueFromDrag);
            this.AddManipulator(clampedDragger);

            // We set this event last, so that the construction does not call it.
            this.valueChanged = valueChanged;
        }

        private void ClampValue()
        {
            //the property setter takes care of this
            value = value;
        }

        public override void OnPersistentDataReady()
        {
            base.OnPersistentDataReady();

            var key = GetFullHierarchicalPersistenceKey();

            m_SliderValue = GetOrCreatePersistentData<SliderValue>(m_SliderValue, key);
        }

        // Handles slider drags
        void SetSliderValueFromDrag()
        {
            if (clampedDragger.dragDirection != ClampedDragger.DragDirection.Free)
                return;

            var delta = clampedDragger.delta;

            if (direction == Direction.Horizontal)
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
            value = Mathf.LerpUnclamped(lowValue, highValue, normalizedDragElementPosition);
        }

        // Handles slider clicks and page scrolls
        void SetSliderValueFromClick()
        {
            if (clampedDragger.dragDirection == ClampedDragger.DragDirection.Free)
                return;

            if (clampedDragger.dragDirection == ClampedDragger.DragDirection.None)
            {
                if (pageSize == 0f)
                {
                    // Jump drag element to current mouse position when user clicks on slider and pageSize == 0
                    var x = (direction == Direction.Horizontal) ?
                        clampedDragger.startMousePosition.x - (dragElement.style.width / 2f) : dragElement.style.positionLeft.value;
                    var y = (direction == Direction.Horizontal) ?
                        dragElement.style.positionTop.value : clampedDragger.startMousePosition.y - (dragElement.style.height / 2f);

                    dragElement.style.positionLeft = x;
                    dragElement.style.positionTop = y;
                    m_DragElementStartPos = new Rect(x, y, dragElement.style.width, dragElement.style.height);

                    // Manipulation becomes a free form drag
                    clampedDragger.dragDirection = ClampedDragger.DragDirection.Free;
                    if (direction == Direction.Horizontal)
                        ComputeValueAndDirectionFromDrag(layout.width, dragElement.style.width, m_DragElementStartPos.x);
                    else
                        ComputeValueAndDirectionFromDrag(layout.height, dragElement.style.height, m_DragElementStartPos.y);
                    return;
                }

                m_DragElementStartPos = new Rect(dragElement.style.positionLeft, dragElement.style.positionTop, dragElement.style.width, dragElement.style.height);
            }

            if (direction == Direction.Horizontal)
                ComputeValueAndDirectionFromClick(layout.width, dragElement.style.width, dragElement.style.positionLeft, clampedDragger.lastMousePosition.x);
            else
                ComputeValueAndDirectionFromClick(layout.height, dragElement.style.height, dragElement.style.positionTop, clampedDragger.lastMousePosition.y);
        }

        void ComputeValueAndDirectionFromClick(float sliderLength, float dragElementLength, float dragElementPos, float dragElementLastPos)
        {
            var totalRange = sliderLength - dragElementLength;
            if (Mathf.Abs(totalRange) < Mathf.Epsilon)
                return;

            if ((dragElementLastPos < dragElementPos) &&
                (clampedDragger.dragDirection != ClampedDragger.DragDirection.LowToHigh))
            {
                clampedDragger.dragDirection = ClampedDragger.DragDirection.HighToLow;
                float normalizedDragElementPosition = Mathf.Max(0f, Mathf.Min(dragElementPos - pageSize, totalRange)) / totalRange;
                value = Mathf.LerpUnclamped(lowValue, highValue, normalizedDragElementPosition);
            }
            else if ((dragElementLastPos > (dragElementPos + dragElementLength)) &&
                     (clampedDragger.dragDirection != ClampedDragger.DragDirection.HighToLow))
            {
                clampedDragger.dragDirection = ClampedDragger.DragDirection.LowToHigh;
                float normalizedDragElementPosition = Mathf.Max(0f, Mathf.Min(dragElementPos + pageSize, totalRange)) / totalRange;
                value = Mathf.LerpUnclamped(lowValue, highValue, normalizedDragElementPosition);
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
                if (direction == Direction.Horizontal)
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

        void UpdateDragElementPosition()
        {
            // UpdateDragElementPosition() might be called at times where we have no panel
            // we must skip the position calculation and wait for a layout pass
            if (panel == null)
                return;

            float normalizedPosition = (value - lowValue) / (highValue - lowValue);
            float dragElementWidth = dragElement.style.width;
            float dragElementHeight = dragElement.style.height;

            if (direction == Direction.Horizontal)
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
