// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Experimental.UIElements
{
    class IMSlider : IMElement
    {
        public float value { get; set; }

        private float m_PageSize;
        private float m_Start;
        private float m_End;
        private GUIStyle m_SliderStyle = GUIStyle.none;
        private GUIStyle m_ThumbStyle = GUIStyle.none;
        private bool m_Horiz;

        public DateTime nextScrollStepTime { get; set; }
        int scrollTroughSide { get; set; }

        public IMSlider()
        {
            nextScrollStepTime = DateTime.Now; // whatever but null
            scrollTroughSide = 0;
        }

        public void SetProperties(Rect pos, float val, float pageSize, float start,
            float end, GUIStyle sliderStyle, GUIStyle thumbStyle, bool horiz)
        {
            position = pos;
            value = val;
            m_PageSize = pageSize;
            m_Start = start;
            m_End = end;
            m_SliderStyle = sliderStyle;
            m_ThumbStyle = thumbStyle;
            m_Horiz = horiz;
        }

        public override bool OnGUI(Event evt)
        {
            if (m_SliderStyle == null || m_ThumbStyle == null)
                return false;

            return base.OnGUI(evt);
        }

        protected override int DoGenerateControlID()
        {
            return GUIUtility.GetControlID("IMSlider".GetHashCode(), focusType, position);
        }

        protected override bool DoMouseDown(MouseEventArgs args)
        {
            // if the click is outside this control, just bail out...
            if (!position.Contains(args.mousePosition) || IsEmptySlider())
                return false;

            scrollTroughSide = 0;
            GUIUtility.hotControl = id;

            if (ThumbSelectionRect().Contains(args.mousePosition))
            {
                // We have a mousedown on the thumb
                // Record where we're draging from, so the user can get back.
                StartDraggingWithValue(Clampedvalue(), args.mousePosition);
                return true;
            }

            GUI.changed = true;

            // We're outside the thumb, but inside the trough.
            // If we have a scrollSize, we do pgup/pgdn style movements
            // if not, we just snap to the current position and begin tracking
            if (SupportsPageMovements())
            {
                SliderState().isDragging = false;
                nextScrollStepTime = SystemClock.now.AddMilliseconds(ScrollWaitDefinitions.firstWait);
                scrollTroughSide = CurrentScrollTroughSide(args.mousePosition);
                value = PageMovementValue(args.mousePosition);
                return true;
            }

            float newValue = ValueForCurrentMousePosition(args.mousePosition);
            StartDraggingWithValue(newValue, args.mousePosition);
            value = Clamp(newValue);
            return true;
        }

        protected override bool DoMouseDrag(MouseEventArgs args)
        {
            if (GUIUtility.hotControl != id)
                return false;

            var sliderState = SliderState();
            if (!sliderState.isDragging)
                return false;

            GUI.changed = true;

            // Recalculate the value from the mouse position. This has the side effect that values are relative to the
            // click point - no matter where inside the trough the original value was. Also means user can get back original scrollerValue
            // if he drags back to start position.
            float deltaPos = MousePosition(args.mousePosition) - sliderState.dragStartPos;
            var newValue = sliderState.dragStartValue + deltaPos / ValuesPerPixel();
            value = Clamp(newValue);
            return true;
        }

        protected override bool DoMouseUp(MouseEventArgs args)
        {
            if (GUIUtility.hotControl == id)
            {
                GUIUtility.hotControl = 0;
                return true;
            }
            return false;
        }

        internal override void DoRepaint(IStylePainter args)
        {
            m_SliderStyle.Draw(position, GUIContent.none, id);
            if (!IsEmptySlider())
                m_ThumbStyle.Draw(ThumbRect(), GUIContent.none, id);

            if (GUIUtility.hotControl != id
                || !position.Contains(args.mousePosition)
                || IsEmptySlider())
                return;

            if (ThumbRect().Contains(args.mousePosition))
            {
                if (scrollTroughSide != 0) // if was scrolling with "trough" and the thumb reached mouse - sliding action over
                    GUIUtility.hotControl = 0;
                return;
            }

            GUI.InternalRepaintEditorWindow();

            if (SystemClock.now < nextScrollStepTime)
                return;

            if (CurrentScrollTroughSide(args.mousePosition) != scrollTroughSide)
                return;

            nextScrollStepTime = SystemClock.now.AddMilliseconds(ScrollWaitDefinitions.regularWait);

            if (SupportsPageMovements())
            {
                SliderState().isDragging = false;
                GUI.changed = true;
                value = PageMovementValue(args.mousePosition);
                return;
            }

            value = Clampedvalue();
        }

        private int CurrentScrollTroughSide(Vector2 mousePosition)
        {
            float mousePos = m_Horiz ? mousePosition.x : mousePosition.y;
            float thumbPos = m_Horiz ? ThumbRect().x : ThumbRect().y;

            return mousePos > thumbPos ? 1 : -1;
        }

        private bool IsEmptySlider()
        {
            return m_Start == m_End;
        }

        private bool SupportsPageMovements()
        {
            return m_PageSize != 0 && GUI.usePageScrollbars;
        }

        private float PageMovementValue(Vector2 currentMousePos)
        {
            var newValue = value;
            var sign = m_Start > m_End ? -1 : 1;
            if (MousePosition(currentMousePos) > PageUpMovementBound())
                newValue += m_PageSize * sign * .9f;
            else
                newValue -= m_PageSize * sign * .9f;
            return Clamp(newValue);
        }

        private float PageUpMovementBound()
        {
            if (m_Horiz)
                return ThumbRect().xMax - position.x;
            return ThumbRect().yMax - position.y;
        }

        private float ValueForCurrentMousePosition(Vector2 currentMousePos)
        {
            if (m_Horiz)
                return (MousePosition(currentMousePos) - ThumbRect().width * .5f) / ValuesPerPixel() + m_Start - m_PageSize * .5f;
            return (MousePosition(currentMousePos) - ThumbRect().height * .5f) / ValuesPerPixel() + m_Start - m_PageSize * .5f;
        }

        private float Clamp(float val)
        {
            return Mathf.Clamp(val, MinValue(), MaxValue());
        }

        private Rect ThumbSelectionRect()
        {
            var selectionRect = ThumbRect();
            return selectionRect;
        }

        private void StartDraggingWithValue(float dragStartValue, Vector2 currentMousePos)
        {
            var state = SliderState();
            state.dragStartPos = MousePosition(currentMousePos);
            state.dragStartValue = dragStartValue;
            state.isDragging = true;
        }

        private SliderState SliderState()
        {
            return (SliderState)GUIUtility.GetStateObject(typeof(SliderState), id);
        }

        private Rect ThumbRect()
        {
            return m_Horiz ? HorizontalThumbRect() : VerticalThumbRect();
        }

        private Rect VerticalThumbRect()
        {
            var valuesPerPixel = ValuesPerPixel();
            if (m_Start < m_End)
                return new Rect(
                    position.x + m_SliderStyle.padding.left,
                    (Clampedvalue() - m_Start) * valuesPerPixel + position.y + m_SliderStyle.padding.top,
                    position.width - m_SliderStyle.padding.horizontal,
                    m_PageSize * valuesPerPixel + ThumbSize());

            return new Rect(
                position.x + m_SliderStyle.padding.left,
                (Clampedvalue() + m_PageSize - m_Start) * valuesPerPixel + position.y + m_SliderStyle.padding.top,
                position.width - m_SliderStyle.padding.horizontal,
                m_PageSize * -valuesPerPixel + ThumbSize());
        }

        private Rect HorizontalThumbRect()
        {
            var valuesPerPixel = ValuesPerPixel();
            if (m_Start < m_End)
                return new Rect(
                    (Clampedvalue() - m_Start) * valuesPerPixel + position.x + m_SliderStyle.padding.left,
                    position.y + m_SliderStyle.padding.top,
                    m_PageSize * valuesPerPixel + ThumbSize(),
                    position.height - m_SliderStyle.padding.vertical);

            return new Rect(
                (Clampedvalue() + m_PageSize - m_Start) * valuesPerPixel + position.x + m_SliderStyle.padding.left,
                position.y,
                m_PageSize * -valuesPerPixel + ThumbSize(),
                position.height);
        }

        private float Clampedvalue()
        {
            return Clamp(value);
        }

        private float MousePosition(Vector2 currentMousePos)
        {
            if (m_Horiz)
                return currentMousePos.x - position.x;
            return currentMousePos.y - position.y;
        }

        private float ValuesPerPixel()
        {
            if (m_Horiz)
                return (position.width - m_SliderStyle.padding.horizontal - ThumbSize()) / (m_End - m_Start);
            return (position.height - m_SliderStyle.padding.vertical - ThumbSize()) / (m_End - m_Start);
        }

        private float ThumbSize()
        {
            if (m_Horiz)
                return m_ThumbStyle.fixedWidth != 0 ? m_ThumbStyle.fixedWidth : m_ThumbStyle.padding.horizontal;
            return m_ThumbStyle.fixedHeight != 0 ? m_ThumbStyle.fixedHeight : m_ThumbStyle.padding.vertical;
        }

        private float MaxValue()
        {
            return Mathf.Max(m_Start, m_End) - m_PageSize;
        }

        private float MinValue()
        {
            return Mathf.Min(m_Start, m_End);
        }
    }
}
