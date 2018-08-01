// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    public class MinMaxSlider : BaseField<Vector2>
    {
        public new class UxmlFactory : UxmlFactory<MinMaxSlider, UxmlTraits> {}

        public new class UxmlTraits : BaseField<Vector2>.UxmlTraits
        {
            UxmlFloatAttributeDescription m_MinValue = new UxmlFloatAttributeDescription { name = "min-value", defaultValue = 0 };
            UxmlFloatAttributeDescription m_MaxValue = new UxmlFloatAttributeDescription { name = "max-value", defaultValue = kDefaultHighValue };
            UxmlFloatAttributeDescription m_LowLimit = new UxmlFloatAttributeDescription { name = "low-limit", defaultValue = float.MinValue };
            UxmlFloatAttributeDescription m_HighLimit = new UxmlFloatAttributeDescription { name = "high-limit", defaultValue = float.MaxValue };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var slider = ((MinMaxSlider)ve);
                slider.SetValueWithoutNotify(new Vector2(m_MinValue.GetValueFromBag(bag, cc), m_MaxValue.GetValueFromBag(bag, cc)));
                slider.lowLimit = m_LowLimit.GetValueFromBag(bag, cc);
                slider.highLimit = m_HighLimit.GetValueFromBag(bag, cc);
            }
        }

        private enum DragState
        {
            NoThumb,
            MinThumb,
            MiddleThumb,
            MaxThumb
        }

        internal VisualElement dragElement { get; private set; }
        private VisualElement dragMinThumb { get; set; }
        private VisualElement dragMaxThumb { get; set; }
        internal ClampedDragger<float> clampedDragger { get; private set; }

        // For dragging purpose
        Vector2 m_DragElementStartPos;
        Vector2 m_ValueStartPos;

        Rect m_DragMinThumbRect;
        Rect m_DragMaxThumbRect;
        DragState m_DragState;

        public float minValue
        {
            get { return value.x; }
            set
            {
                base.value = ClampValues(new Vector2(value, m_Value.y));
            }
        }

        public float maxValue
        {
            get { return value.y; }
            set
            {
                base.value = ClampValues(new Vector2(m_Value.x, value));
            }
        }

        public override Vector2 value
        {
            get { return base.value; }
            set
            {
                base.value = ClampValues(value);
            }
        }

        public override void SetValueWithoutNotify(Vector2 newValue)
        {
            base.SetValueWithoutNotify(ClampValues(newValue));
            UpdateDragElementPosition();
        }

        public float range
        {
            get { return Math.Abs(highLimit - lowLimit); }
        }

        float m_MinLimit;
        float m_MaxLimit;
        public float lowLimit
        {
            get { return m_MinLimit; }
            set
            {
                if (!Mathf.Approximately(m_MinLimit, value))
                {
                    if (value > m_MaxLimit)
                    {
                        throw new ArgumentException("lowLimit is greater than highLimit");
                    }

                    m_MinLimit = value;
                    this.value = m_Value;
                    UpdateDragElementPosition();

                    if (!string.IsNullOrEmpty(persistenceKey))
                        SavePersistentData();
                }
            }
        }

        public float highLimit
        {
            get { return m_MaxLimit; }
            set
            {
                if (!Mathf.Approximately(m_MaxLimit, value))
                {
                    if (value < m_MinLimit)
                    {
                        throw new ArgumentException("highLimit is smaller than lowLimit");
                    }

                    m_MaxLimit = value;
                    this.value = m_Value;
                    UpdateDragElementPosition();

                    if (!string.IsNullOrEmpty(persistenceKey))
                        SavePersistentData();
                }
            }
        }

        internal const float kDefaultHighValue = 10;

        public MinMaxSlider()
            : this(0, kDefaultHighValue, float.MinValue, float.MaxValue) {}

        public MinMaxSlider(float minValue, float maxValue, float minLimit, float maxLimit)
        {
            m_DragState = DragState.NoThumb;

            Add(new VisualElement() { name = "TrackElement" });

            dragElement = new VisualElement() { name = "DragElement" };
            dragElement.RegisterCallback<GeometryChangedEvent>(UpdateDragElementPosition);
            Add(dragElement);

            // For a better handling of the cursor style, children elements are created so that the style is automatic with the uss.
            dragMinThumb = new VisualElement();
            dragMaxThumb = new VisualElement();
            dragMinThumb.AddToClassList("thumbelement");
            dragMaxThumb.AddToClassList("thumbelement");
            dragElement.Add(dragMinThumb);
            dragElement.Add(dragMaxThumb);

            clampedDragger = new ClampedDragger<float>(null, SetSliderValueFromClick, SetSliderValueFromDrag);
            this.AddManipulator(clampedDragger);


            m_MinLimit = minLimit;
            m_MaxLimit = maxLimit;
            m_Value = ClampValues(new Vector2(minValue, maxValue));
            UpdateDragElementPosition();
        }

        Vector2 ClampValues(Vector2 valueToClamp)
        {
            // Make sure the limits are ok...
            if (m_MinLimit > m_MaxLimit)
            {
                m_MinLimit = m_MaxLimit;
            }

            Vector2 clampedValue = new Vector2();

            // Make sure the value max is not bigger than the max limit...
            if (valueToClamp.y > m_MaxLimit)
            {
                valueToClamp.y = m_MaxLimit;
            }

            // Clamp both values
            clampedValue.x = Mathf.Clamp(valueToClamp.x, m_MinLimit, valueToClamp.y);
            clampedValue.y = Mathf.Clamp(valueToClamp.y, valueToClamp.x, m_MaxLimit);
            return clampedValue;
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

            var sliceSpan = dragElement.style.sliceLeft + dragElement.style.sliceRight;
            var newPositionLeft = Mathf.Round(SliderLerpUnclamped(dragElement.style.sliceLeft, layout.width - dragElement.style.sliceRight, SliderNormalizeValue(minValue, lowLimit, highLimit)) - dragElement.style.sliceLeft);
            var newPositionRight = Mathf.Round(SliderLerpUnclamped(dragElement.style.sliceLeft, layout.width - dragElement.style.sliceRight, SliderNormalizeValue(maxValue, lowLimit, highLimit)) + dragElement.style.sliceRight);
            dragElement.style.width = Mathf.Max(sliceSpan, newPositionRight - newPositionLeft);
            dragElement.style.positionLeft = newPositionLeft;

            // Calculate the rect for the mouse selection, in the parent coordinate (MinMaxSlider world) ...
            m_DragMinThumbRect = new Rect(dragElement.style.positionLeft, dragElement.layout.yMin, dragElement.style.sliceLeft, dragElement.style.height);
            m_DragMaxThumbRect = new Rect(dragElement.style.positionLeft + (dragElement.style.width - dragElement.style.sliceRight), dragElement.layout.yMin, dragElement.style.sliceRight, dragElement.style.height);

            // The child elements are positioned based on the parent (drag element) coordinate...
            // Set up the Max Thumb for the horizontal slider...
            dragMaxThumb.style.positionLeft = dragElement.style.width - dragElement.style.sliceRight;
            dragMaxThumb.style.positionTop = 0;
            // The child elements are positioned based on the parent (drag element) coordinate...
            // Same location for both Horizontal / Vertical slider
            dragMinThumb.style.width = m_DragMinThumbRect.width;
            dragMinThumb.style.height = m_DragMinThumbRect.height;
            dragMinThumb.style.positionLeft = 0;
            dragMinThumb.style.positionTop = 0;

            dragMaxThumb.style.width = m_DragMaxThumbRect.width;
            dragMaxThumb.style.height = m_DragMaxThumbRect.height;
        }

        internal float SliderLerpUnclamped(float a, float b, float interpolant)
        {
            return Mathf.LerpUnclamped(a, b, interpolant);
        }

        internal float SliderNormalizeValue(float currentValue, float lowerValue, float higherValue)
        {
            return (currentValue - lowerValue) / (higherValue - lowerValue);
        }

        float ComputeValueFromPosition(float positionToConvert)
        {
            var interpolant = 0.0f;
            interpolant = SliderNormalizeValue(positionToConvert, dragElement.style.sliceLeft, (layout.width - dragElement.style.sliceRight));
            return SliderLerpUnclamped(lowLimit, highLimit, interpolant);
        }

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt.GetEventTypeId() == GeometryChangedEvent.TypeId())
            {
                UpdateDragElementPosition((GeometryChangedEvent)evt);
            }
        }

        // Handles slider drags
        void SetSliderValueFromDrag()
        {
            if (clampedDragger.dragDirection != ClampedDragger<float>.DragDirection.Free)
                return;

            var originalPosition = m_DragElementStartPos.x;
            var newPosition = originalPosition + clampedDragger.delta.x;
            ComputeValueFromDraggingThumb(originalPosition, newPosition);
        }

        // Handles slider clicks and page scrolls
        void SetSliderValueFromClick()
        {
            if (clampedDragger.dragDirection == ClampedDragger<float>.DragDirection.Free)
                return;

            // Detection of the thumb click (min or max)
            if (m_DragMinThumbRect.Contains(clampedDragger.startMousePosition))
            {
                m_DragState = DragState.MinThumb;
            }
            else if (m_DragMaxThumbRect.Contains(clampedDragger.startMousePosition))
            {
                m_DragState = DragState.MaxThumb;
            }
            else if (dragElement.layout.Contains(clampedDragger.startMousePosition))
            {
                m_DragState = DragState.MiddleThumb;
            }
            else
            {
                m_DragState = DragState.NoThumb;
            }

            // No thumb we want to move the dragger with a click
            if (m_DragState == DragState.NoThumb)
            {
                // Jump drag element to current mouse position : the new MAX value is simply where the mouse click is done
                m_DragElementStartPos = new Vector2(clampedDragger.startMousePosition.x, dragElement.style.positionTop.value);

                // Manipulation becomes a free form drag
                clampedDragger.dragDirection = ClampedDragger<float>.DragDirection.Free;
                ComputeValueDragStateNoThumb(dragElement.style.sliceLeft, (layout.width - dragElement.style.sliceRight), m_DragElementStartPos.x);
                // For any future dragging after the initial click, this is a Middle Thumb like drag...
                m_DragState = DragState.MiddleThumb;
                m_ValueStartPos = value;
            }
            else
            {
                // This is probably a drag move
                m_ValueStartPos = value;
                clampedDragger.dragDirection = ClampedDragger<float>.DragDirection.Free;
                m_DragElementStartPos = clampedDragger.startMousePosition;
            }
        }

        void ComputeValueDragStateNoThumb(float lowLimitPosition, float highLimitPosition, float dragElementPos)
        {
            float newPosition;

            // Clamp the dragElementPos
            if (dragElementPos < lowLimitPosition)
            {
                newPosition = lowLimit;
            }
            else if (dragElementPos > highLimitPosition)
            {
                newPosition = highLimit;
            }
            else
            {
                newPosition = ComputeValueFromPosition(dragElementPos);
            }

            // The new position is the MAX value... here, we must keep the distance between min and max the same as it was, since this is just a drag...
            var actualDifference = (maxValue - minValue);
            var newMinValue = (newPosition - actualDifference);
            var newMaxValue = newPosition;

            if (newMinValue < lowLimit)
            {
                newMinValue = lowLimit;
                newMaxValue = newMinValue + actualDifference;
            }

            value = new Vector2(newMinValue, newMaxValue);
        }

        void ComputeValueFromDraggingThumb(float dragElementStartPos, float dragElementEndPos)
        {
            var startPosInValue = ComputeValueFromPosition(dragElementStartPos);
            var endPosInValue = ComputeValueFromPosition(dragElementEndPos);
            var deltaInValueWorld = endPosInValue - startPosInValue;

            switch (m_DragState)
            {
                case DragState.MiddleThumb:
                {
                    var newValue = value;
                    newValue.x = m_ValueStartPos.x + deltaInValueWorld;
                    newValue.y = m_ValueStartPos.y + deltaInValueWorld;
                    var actualDifference = (m_ValueStartPos.y - m_ValueStartPos.x);

                    if (newValue.x < lowLimit)
                    {
                        newValue.x = lowLimit;
                        newValue.y = lowLimit + actualDifference;
                    }
                    else if (newValue.y > highLimit)
                    {
                        newValue.y = highLimit;
                        newValue.x = highLimit - actualDifference;
                    }

                    value = newValue;
                    break;
                }
                case DragState.MinThumb:
                {
                    var newPosition = m_ValueStartPos.x + deltaInValueWorld;
                    if (newPosition > maxValue)
                    {
                        newPosition = maxValue;
                    }
                    else if (newPosition < lowLimit)
                    {
                        newPosition = lowLimit;
                    }

                    value = new Vector2(newPosition, maxValue);
                    break;
                }
                case DragState.MaxThumb:
                {
                    var newPosition = m_ValueStartPos.y + deltaInValueWorld;
                    if (newPosition < minValue)
                    {
                        newPosition = minValue;
                    }
                    else if (newPosition > highLimit)
                    {
                        newPosition = highLimit;
                    }

                    value = new Vector2(minValue, newPosition);
                    break;
                }
            }
        }
    }
}
