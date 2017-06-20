// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Experimental.UIElements
{
    public class Slider : VisualContainer
    {
        public enum Direction
        {
            Horizontal,
            Vertical
        }

        public VisualElement dragElement { get; private set; }

        public float lowValue { get; set; }
        public float highValue { get; set; }
        public float range { get { return highValue - lowValue; } }
        public float pageSize { get; set; }

        public event System.Action<float> valueChanged;
        // TODO refactor Slider to be UIElements-ish. Styles should be applied externally
        internal ClampedDragger clampedDragger { get; private set; }
        Rect m_DragElementStartPos;

        float m_Value;
        public float value
        {
            get { return m_Value; }
            set
            {
                var newValue = Mathf.Clamp(value, lowValue, highValue);

                if (Mathf.Approximately(m_Value, newValue))
                    return;

                m_Value = newValue;

                UpdateDragElementPosition();

                if (valueChanged != null)
                    valueChanged(m_Value);

                this.Dirty(ChangeType.Repaint);
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

        public Slider(float start, float end, System.Action<float> valueChanged,
                      Direction direction = Direction.Horizontal, float pageSize = 10f)
        {
            this.valueChanged = valueChanged;
            this.direction = direction;
            this.pageSize = pageSize;
            lowValue = start;
            highValue = end;

            AddChild(new VisualElement() { name = "TrackElement" });

            // Explicitly set m_Value
            m_Value = lowValue;

            dragElement = new VisualElement() { name = "DragElement" };

            AddChild(dragElement);

            clampedDragger = new ClampedDragger(this, SetSliderValueFromClick, SetSliderValueFromDrag);
            AddManipulator(clampedDragger);
        }

        // Handles slider drags
        void SetSliderValueFromDrag()
        {
            if (clampedDragger.dragDirection != ClampedDragger.DragDirection.Free)
                return;

            var delta = clampedDragger.delta;

            if (direction == Direction.Horizontal)
                ComputeValueAndDirectionFromDrag(layout.width, dragElement.width, m_DragElementStartPos.x + delta.x);
            else
                ComputeValueAndDirectionFromDrag(layout.height, dragElement.height, m_DragElementStartPos.y + delta.y);
        }

        void ComputeValueAndDirectionFromDrag(float sliderLength, float dragElementLength, float dragElementPos)
        {
            var totalRange = sliderLength - dragElementLength;
            if (Mathf.Abs(totalRange) < Mathf.Epsilon)
                return;

            value = (Mathf.Max(0f, Mathf.Min(dragElementPos, totalRange)) / totalRange * range) + lowValue;
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
                        clampedDragger.startMousePosition.x - (dragElement.width / 2f) : dragElement.positionLeft;
                    var y = (direction == Direction.Horizontal) ?
                        dragElement.positionTop : clampedDragger.startMousePosition.y - (dragElement.height / 2f);

                    dragElement.positionLeft = x;
                    dragElement.positionTop = y;
                    m_DragElementStartPos = new Rect(x, y, dragElement.width, dragElement.height);

                    // Manipulation becomes a free form drag
                    clampedDragger.dragDirection = ClampedDragger.DragDirection.Free;
                    if (direction == Direction.Horizontal)
                        ComputeValueAndDirectionFromDrag(layout.width, dragElement.width, m_DragElementStartPos.x);
                    else
                        ComputeValueAndDirectionFromDrag(layout.height, dragElement.height, m_DragElementStartPos.y);
                    return;
                }

                m_DragElementStartPos = new Rect(dragElement.positionLeft, dragElement.positionTop, dragElement.width, dragElement.height);
            }

            if (direction == Direction.Horizontal)
                ComputeValueAndDirectionFromClick(layout.width, dragElement.width, dragElement.positionLeft, clampedDragger.lastMousePosition.x);
            else
                ComputeValueAndDirectionFromClick(layout.height, dragElement.height, dragElement.positionTop, clampedDragger.lastMousePosition.y);
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
                value = (Mathf.Max(0f, Mathf.Min(dragElementPos - pageSize, totalRange)) / totalRange * range) + lowValue;
            }
            else if ((dragElementLastPos > (dragElementPos + dragElementLength)) &&
                     (clampedDragger.dragDirection != ClampedDragger.DragDirection.HighToLow))
            {
                clampedDragger.dragDirection = ClampedDragger.DragDirection.LowToHigh;
                value = (Mathf.Max(0f, Mathf.Min(dragElementPos + pageSize, totalRange)) / totalRange * range) + lowValue;
            }
        }

        public void AdjustDragElement(float factor)
        {
            // Any factor greater or equal to 1f eliminates the need for a drag element
            bool needsElement = factor < 1f;
            dragElement.visible = needsElement;

            if (needsElement)
            {
                StyleSheets.VisualElementStyles elemStyles = dragElement.styles;
                dragElement.visible = true;

                // Any factor smaller than 1f will necessitate a drag element
                if (direction == Direction.Horizontal)
                {
                    // Make sure the minimum width of drag element is honoured
                    float elemMinWidth = elemStyles.minWidth.GetSpecifiedValueOrDefault(0.0f);
                    dragElement.width = Mathf.Max(layout.width * factor, elemMinWidth);
                }
                else
                {
                    // Make sure the minimum height of drag element is honoured
                    float elemMinHeight = elemStyles.minHeight.GetSpecifiedValueOrDefault(0.0f);
                    dragElement.height = Mathf.Max(layout.height * factor, elemMinHeight);
                }
            }
        }

        void UpdateDragElementPosition()
        {
            // UpdateDragElementPosition() might be called at times where we have no panel
            // we must skip the position calculation and wait for a layout pass
            if (panel == null)
                return;

            float pos = m_Value - lowValue;
            float dragElementWidth = dragElement.width;
            float dragElementHeight = dragElement.height;

            if (direction == Direction.Horizontal)
            {
                float totalWidth = layout.width - dragElementWidth;
                dragElement.positionLeft = ((pos / range) * totalWidth);
            }
            else
            {
                float totalHeight = layout.height - dragElementHeight;
                dragElement.positionTop = ((pos / range) * totalHeight);
            }
        }

        protected internal override void OnPostLayout(bool hasNewLayout)
        {
            if (!hasNewLayout)
                return;

            UpdateDragElementPosition();
        }
    }
}
