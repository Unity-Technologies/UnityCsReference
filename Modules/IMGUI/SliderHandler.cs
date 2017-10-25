// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // State for when we're dragging a slider.
    internal class SliderState
    {
        [RequiredByNativeCode] // Created by reflection from SliderHandler.SliderState
        public SliderState() {}

        public float dragStartPos;
        public float dragStartValue;
        public bool isDragging;
    }

    // TODO: Make the thumb positioning / sizing be right
    internal struct SliderHandler
    {
        readonly Rect position;
        readonly float currentValue;
        readonly float size;
        readonly float start;
        readonly float end;
        readonly GUIStyle slider;
        readonly GUIStyle thumb;
        readonly bool horiz;
        readonly int id;

        public SliderHandler(Rect position, float currentValue, float size, float start, float end, GUIStyle slider, GUIStyle thumb, bool horiz, int id)
        {
            this.position = position;
            this.currentValue = currentValue;
            this.size = size;
            this.start = start;
            this.end = end;
            this.slider = slider;
            this.thumb = thumb;
            this.horiz = horiz;
            this.id = id;
        }

        public float Handle()
        {
            if (slider == null || thumb == null)
                return currentValue;

            switch (CurrentEventType())
            {
                case EventType.MouseDown:
                    return OnMouseDown();

                case EventType.MouseDrag:
                    return OnMouseDrag();

                case EventType.MouseUp:
                    return OnMouseUp();

                case EventType.Repaint:
                    return OnRepaint();
            }
            return currentValue;
        }

        private float OnMouseDown()
        {
            // if the click is outside this control, just bail out...
            if (!position.Contains(CurrentEvent().mousePosition) || IsEmptySlider())
                return currentValue;

            GUI.scrollTroughSide = 0;
            GUIUtility.hotControl = id;
            CurrentEvent().Use();

            if (ThumbSelectionRect().Contains(CurrentEvent().mousePosition))
            {
                // We have a mousedown on the thumb
                // Record where we're draging from, so the user can get back.
                StartDraggingWithValue(ClampedCurrentValue());
                return currentValue;
            }

            GUI.changed = true;

            // We're outside the thumb, but inside the trough.
            // If we have a scrollSize, we do pgup/pgdn style movements
            // if not, we just snap to the current position and begin tracking
            if (SupportsPageMovements())
            {
                SliderState().isDragging = false;
                GUI.nextScrollStepTime = SystemClock.now.AddMilliseconds(ScrollWaitDefinitions.firstWait);
                GUI.scrollTroughSide = CurrentScrollTroughSide();
                return PageMovementValue();
            }

            float newValue = ValueForCurrentMousePosition();
            StartDraggingWithValue(newValue);
            return Clamp(newValue);
        }

        private float OnMouseDrag()
        {
            if (GUIUtility.hotControl != id)
                return currentValue;

            var sliderState = SliderState();
            if (!sliderState.isDragging)
                return currentValue;

            GUI.changed = true;
            CurrentEvent().Use();

            // Recalculate the value from the mouse position. this has the side effect that values are relative to the
            // click point - no matter where inside the trough the original value was. Also means user can get back original value
            // if he drags back to start position.
            float deltaPos = MousePosition() - sliderState.dragStartPos;
            var newValue = sliderState.dragStartValue + deltaPos / ValuesPerPixel();
            return Clamp(newValue);
        }

        private float OnMouseUp()
        {
            if (GUIUtility.hotControl == id)
            {
                CurrentEvent().Use();
                GUIUtility.hotControl = 0;
            }
            return currentValue;
        }

        private float OnRepaint()
        {
            slider.Draw(position, GUIContent.none, id);
            if (!IsEmptySlider())
                thumb.Draw(ThumbRect(), GUIContent.none, id);

            if (GUIUtility.hotControl != id || !position.Contains(CurrentEvent().mousePosition) || IsEmptySlider())
                return currentValue;

            if (ThumbRect().Contains(CurrentEvent().mousePosition))
            {
                if (GUI.scrollTroughSide != 0) // if was scrolling with "trough" and the thumb reached mouse - sliding action over
                {
                    GUIUtility.hotControl = 0;
                }

                return currentValue;
            }

            GUI.InternalRepaintEditorWindow();

            if (SystemClock.now < GUI.nextScrollStepTime)
                return currentValue;

            if (CurrentScrollTroughSide() != GUI.scrollTroughSide)
                return currentValue;

            GUI.nextScrollStepTime = SystemClock.now.AddMilliseconds(ScrollWaitDefinitions.regularWait);

            if (SupportsPageMovements())
            {
                SliderState().isDragging = false;
                GUI.changed = true;
                return PageMovementValue();
            }
            return ClampedCurrentValue();
        }

        private EventType CurrentEventType()
        {
            return CurrentEvent().GetTypeForControl(id);
        }

        private int CurrentScrollTroughSide()
        {
            float mousePos = horiz ? CurrentEvent().mousePosition.x : CurrentEvent().mousePosition.y;
            float thumbPos = horiz ? ThumbRect().x : ThumbRect().y;

            return mousePos > thumbPos ? 1 : -1;
        }

        private bool IsEmptySlider()
        {
            return start == end;
        }

        private bool SupportsPageMovements()
        {
            return size != 0 && GUI.usePageScrollbars;
        }

        private float PageMovementValue()
        {
            var newValue = currentValue;
            var sign = start > end ? -1 : 1;
            if (MousePosition() > PageUpMovementBound())
                newValue += size * sign * .9f;
            else
                newValue -= size * sign * .9f;
            return Clamp(newValue);
        }

        private float PageUpMovementBound()
        {
            if (horiz)
                return ThumbRect().xMax - position.x;
            return ThumbRect().yMax - position.y;
        }

        private Event CurrentEvent()
        {
            return Event.current;
        }

        private float ValueForCurrentMousePosition()
        {
            if (horiz)
                return (MousePosition() - ThumbRect().width * .5f) / ValuesPerPixel() + start - size * .5f;
            return (MousePosition() - ThumbRect().height * .5f) / ValuesPerPixel() + start - size * .5f;
        }

        private float Clamp(float value)
        {
            return Mathf.Clamp(value, MinValue(), MaxValue());
        }

        private Rect ThumbSelectionRect()
        {
            var selectionRect = ThumbRect();
            return selectionRect;
        }

        private void StartDraggingWithValue(float dragStartValue)
        {
            var state = SliderState();
            state.dragStartPos = MousePosition();
            state.dragStartValue = dragStartValue;
            state.isDragging = true;
        }

        private SliderState SliderState()
        {
            return (SliderState)GUIUtility.GetStateObject(typeof(SliderState), id);
        }

        private Rect ThumbRect()
        {
            return horiz ? HorizontalThumbRect() : VerticalThumbRect();
        }

        private Rect VerticalThumbRect()
        {
            var valuesPerPixel = ValuesPerPixel();
            if (start < end)
                return new Rect(
                    position.x + slider.padding.left,
                    (ClampedCurrentValue() - start) * valuesPerPixel + position.y + slider.padding.top,
                    position.width - slider.padding.horizontal,
                    size * valuesPerPixel + ThumbSize());

            return new Rect(
                position.x + slider.padding.left,
                (ClampedCurrentValue() + size - start) * valuesPerPixel + position.y + slider.padding.top,
                position.width - slider.padding.horizontal,
                size * -valuesPerPixel + ThumbSize());
        }

        private Rect HorizontalThumbRect()
        {
            var valuesPerPixel = ValuesPerPixel();
            if (start < end)
                return new Rect(
                    (ClampedCurrentValue() - start) * valuesPerPixel + position.x + slider.padding.left,
                    position.y + slider.padding.top,
                    size * valuesPerPixel + ThumbSize(),
                    position.height - slider.padding.vertical);

            return new Rect(
                (ClampedCurrentValue() + size - start) * valuesPerPixel + position.x + slider.padding.left,
                position.y,
                size * -valuesPerPixel + ThumbSize(),
                position.height);
        }

        private float ClampedCurrentValue()
        {
            return Clamp(currentValue);
        }

        private float MousePosition()
        {
            if (horiz)
                return CurrentEvent().mousePosition.x - position.x;
            return CurrentEvent().mousePosition.y - position.y;
        }

        private float ValuesPerPixel()
        {
            if (horiz)
                return (position.width - slider.padding.horizontal - ThumbSize()) / (end - start);
            return (position.height - slider.padding.vertical - ThumbSize()) / (end - start);
        }

        private float ThumbSize()
        {
            if (horiz)
                return thumb.fixedWidth != 0 ? thumb.fixedWidth : thumb.padding.horizontal;
            return thumb.fixedHeight != 0 ? thumb.fixedHeight : thumb.padding.vertical;
        }

        private float MaxValue()
        {
            return Mathf.Max(start, end) - size;
        }

        private float MinValue()
        {
            return Mathf.Min(start, end);
        }
    }
}
