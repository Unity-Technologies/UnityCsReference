// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Experimental.RMGUI
{
    internal class Slider : VisualElement
    {
        public delegate void ValueChanged(float value);

        public event ValueChanged onValueChanged;

        internal GUIStyle horizontalSlider = GUIStyle.none;

        internal GUIStyle verticalSlider = GUIStyle.none;

        internal GUIStyle horizontalSliderThumb = GUIStyle.none;

        internal GUIStyle verticalSliderThumb = GUIStyle.none;

        public float dragStartPos;
        public float dragStartValue;
        public bool isDragging;

        public float value { get; set; }

        // TODO refactor Slider to be RMGUIish. Styles should be applied externally
        public new GUIStyle style
        {
            get { return m_GUIStyle; }
            set
            {
                m_GUIStyle = value;
            }
        }

        private float m_PageSize;
        private float m_Start;
        private float m_End;
        private bool m_Horiz;

        private GUIStyle m_ThumbStyle;

        public DateTime nextScrollStepTime { get; set; }
        int scrollTroughSide { get; set; }

        public Slider(ValueChanged onValueChanged)
        {
            this.onValueChanged = onValueChanged;
            nextScrollStepTime = DateTime.Now; // whatever but null
            scrollTroughSide = 0;
        }

        void ChangeValue(float value)
        {
            if (!Mathf.Approximately(this.value, value))
            {
                this.value = value;
                if (onValueChanged != null)
                {
                    onValueChanged(value);
                }
            }
        }

        public void SetProperties(Rect pos, float val, float pageSize, float start, float end, bool horiz)
        {
            position = pos;
            value = val;
            m_PageSize = pageSize;
            m_Start = start;
            m_End = end;
            m_Horiz = horiz;

            if (m_Horiz)
            {
                style = horizontalSlider;
                m_ThumbStyle = horizontalSliderThumb;
            }
            else
            {
                style = verticalSlider;
                m_ThumbStyle = verticalSliderThumb;
            }
        }

        public override EventPropagation HandleEvent(Event evt, VisualElement finalTarget)
        {
            if (style == GUIStyle.none || m_ThumbStyle == GUIStyle.none)
            {
                return EventPropagation.Continue;
            }

            var mouseEvtArgs = new MouseEventArgs(globalTransform.inverse.MultiplyPoint3x4(evt.mousePosition), evt.clickCount, evt.modifiers);

            switch (evt.type)
            {
                case EventType.MouseDown:
                    if (DoMouseDown(mouseEvtArgs))
                        return EventPropagation.Stop;
                    break;

                case EventType.MouseDrag:
                    if (DoMouseDrag(mouseEvtArgs))
                        return EventPropagation.Stop;
                    break;

                case EventType.MouseUp:
                    if (DoMouseUp(mouseEvtArgs))
                        return EventPropagation.Stop;
                    break;
            }

            return EventPropagation.Continue;
        }

        internal bool DoMouseDown(MouseEventArgs args)
        {
            // if the click is outside this control, just bail out...
            if (!ContainsPoint(args.mousePosition) || IsEmptySlider())
                return false;

            scrollTroughSide = 0;

            this.TakeCapture();

            if (ThumbSelectionRect().Contains(args.mousePosition))
            {
                // We have a mousedown on the thumb
                // Record where we're draging from, so the user can get back.
                StartDraggingWithValue(Clampedvalue(), args.mousePosition);
                return true;
            }

            // We're outside the thumb, but inside the trough.
            // If we have a scrollSize, we do pgup/pgdn style movements
            // if not, we just snap to the current position and begin tracking
            if (SupportsPageMovements())
            {
                isDragging = false;
                nextScrollStepTime = SystemClock.now.AddMilliseconds(ScrollWaitDefinitions.firstWait);
                scrollTroughSide = CurrentScrollTroughSide(args.mousePosition);
                ChangeValue(PageMovementValue(args.mousePosition));
                return true;
            }

            float newValue = ValueForCurrentMousePosition(args.mousePosition);
            StartDraggingWithValue(newValue, args.mousePosition);
            ChangeValue(Clamp(newValue));
            return true;
        }

        internal bool DoMouseDrag(MouseEventArgs args)
        {
            if (!this.HasCapture())
                return false;

            if (!isDragging)
                return false;

            // Recalculate the value from the mouse position. This has the side effect that values are relative to the
            // click point - no matter where inside the trough the original value was. Also means user can get back original scrollerValue
            // if he drags back to start position.
            float deltaPos = MousePosition(args.mousePosition) - dragStartPos;
            var newValue = dragStartValue + deltaPos / ValuesPerPixel();
            ChangeValue(Clamp(newValue));
            return true;
        }

        internal bool DoMouseUp(MouseEventArgs args)
        {
            if (this.HasCapture())
            {
                this.ReleaseCapture();
                return true;
            }
            return false;
        }

        public override void DoRepaint(IStylePainter args)
        {
            style.Draw(position, enabled && ContainsPoint(args.mousePosition), isDragging, enabled, false);
            if (!IsEmptySlider())
                m_ThumbStyle.Draw(ThumbRect(), ContainsPoint(args.mousePosition), isDragging, enabled, false);
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
                return ThumbRect().xMax;
            return ThumbRect().yMax;
        }

        private float ValueForCurrentMousePosition(Vector2 currentMousePos)
        {
            var r = ThumbRect();
            r.x -= position.x;
            r.y -= position.y;

            return (MousePosition(currentMousePos) - (m_Horiz ? r.width : r.height) * .5f) / ValuesPerPixel() + m_Start - m_PageSize * .5f;
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
            this.dragStartPos = MousePosition(currentMousePos);
            this.dragStartValue = dragStartValue;
            this.isDragging = true;
        }

        private Rect ThumbRect()
        {
            var r = m_Horiz ? HorizontalThumbRect() : VerticalThumbRect();
            r.x += position.x;
            r.y += position.y;
            return r;
        }

        private Rect VerticalThumbRect()
        {
            var valuesPerPixel = ValuesPerPixel();
            if (m_Start < m_End)
                return new Rect(
                    style.padding.left,
                    (Clampedvalue() - m_Start) * valuesPerPixel + style.padding.top,
                    position.width - style.padding.horizontal,
                    m_PageSize * valuesPerPixel + ThumbSize());

            return new Rect(
                style.padding.left,
                (Clampedvalue() + m_PageSize - m_Start) * valuesPerPixel + style.padding.top,
                position.width - style.padding.horizontal,
                m_PageSize * -valuesPerPixel + ThumbSize());
        }

        private Rect HorizontalThumbRect()
        {
            var valuesPerPixel = ValuesPerPixel();
            if (m_Start < m_End)
                return new Rect(
                    (Clampedvalue() - m_Start) * valuesPerPixel + style.padding.left,
                    style.padding.top,
                    m_PageSize * valuesPerPixel + ThumbSize(),
                    position.height - style.padding.vertical);

            return new Rect(
                (Clampedvalue() + m_PageSize - m_Start) * valuesPerPixel + style.padding.left,
                0,
                m_PageSize * -valuesPerPixel + ThumbSize(),
                position.height);
        }

        private float Clampedvalue()
        {
            return Clamp(value);
        }

        private float MousePosition(Vector2 currentMousePos)
        {
            var m = currentMousePos;
            m.x -= position.x;
            m.y -= position.y;
            if (m_Horiz)
                return m.x;
            return m.y;
        }

        private float ValuesPerPixel()
        {
            if (m_Horiz)
                return (position.width - style.padding.horizontal - ThumbSize()) / (m_End - m_Start);
            return (position.height - style.padding.vertical - ThumbSize()) / (m_End - m_Start);
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
