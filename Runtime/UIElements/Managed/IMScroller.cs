// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Experimental.UIElements
{
    class IMScroller : IMElement
    {
        private const float ScrollStepSize = 10f;

        private readonly IMSlider m_Slider;
        private readonly IMRepeatButton m_LeftButton;
        private readonly IMRepeatButton m_RightButton;
        private float m_PageSize;
        private float m_LeftValue;
        private float m_RightValue;

        public DateTime m_NextScrollStepTime = DateTime.Now;
        private static int s_ScrollControlId = 0;

        public float value { get; private set; }

        public IMScroller()
        {
            m_NextScrollStepTime = DateTime.Now;
            m_Slider = new IMSlider();
            m_LeftButton = new IMRepeatButton();
            m_RightButton = new IMRepeatButton();
        }

        public void SetProperties(Rect pos, float val, float size, float leftValue, float rightValue, GUIStyle slider, GUIStyle thumb, GUIStyle leftButton, GUIStyle rightButton, bool horiz)
        {
            position = pos;

            m_PageSize = size;
            m_LeftValue = leftValue;
            m_RightValue = rightValue;

            value = val;

            Rect sliderRect, minRect, maxRect;
            GetRects(horiz, position, leftButton, rightButton, out sliderRect, out minRect, out maxRect);

            m_Slider.SetProperties(sliderRect, value, size, leftValue, rightValue, slider, thumb, horiz);
            m_LeftButton.position = minRect;
            m_LeftButton.guiStyle = leftButton;
            m_RightButton.position = maxRect;
            m_RightButton.guiStyle = rightButton;
        }

        public override void OnReuse()
        {
            base.OnReuse();
            m_Slider.OnReuse();
            m_LeftButton.OnReuse();
            m_RightButton.OnReuse();
        }

        public override bool OnGUI(Event evt)
        {
            bool used = m_Slider.OnGUI(evt);
            value = m_Slider.value;

            bool wasMouseUpEvent = (evt.type == EventType.MouseUp);

            used |= m_LeftButton.OnGUI(evt);
            if (m_LeftButton.isPressed && OnScrollerButton(evt))
            {
                value -= ScrollStepSize * (m_LeftValue < m_RightValue ? 1f : -1f);
            }

            used |= m_RightButton.OnGUI(evt);
            if (m_RightButton.isPressed && OnScrollerButton(evt))
            {
                value += ScrollStepSize * (m_LeftValue < m_RightValue ? 1f : -1f);
            }

            if (wasMouseUpEvent && evt.type == EventType.Used) // repeat buttons ate mouse up event - release scrolling
            {
                s_ScrollControlId = 0;
            }

            if (m_LeftValue < m_RightValue)
                value = Mathf.Clamp(value, m_LeftValue, m_RightValue - m_PageSize);
            else
                value = Mathf.Clamp(value, m_RightValue, m_LeftValue - m_PageSize);

            if (used)
            {
                evt.Use();
            }

            // sync slider value
            m_Slider.value = value;

            return used;
        }

        protected override int DoGenerateControlID()
        {
            m_Slider.GenerateControlID();
            m_LeftButton.GenerateControlID();
            m_RightButton.GenerateControlID();
            return GUIUtility.GetControlID("IMScroller".GetHashCode(), focusType, position);
        }

        private void GetRects(bool horiz, Rect pos, GUIStyle leftButton, GUIStyle rightButton, out Rect sliderRect, out Rect minRect, out Rect maxRect)
        {
            if (horiz)
            {
                sliderRect = new Rect(
                        pos.x + leftButton.fixedWidth, pos.y,
                        pos.width - leftButton.fixedWidth - rightButton.fixedWidth, pos.height
                        );
                minRect = new Rect(pos.x, pos.y, leftButton.fixedWidth, pos.height);
                maxRect = new Rect(pos.xMax - rightButton.fixedWidth, pos.y, rightButton.fixedWidth, pos.height);
            }
            else
            {
                sliderRect = new Rect(
                        pos.x, pos.y + leftButton.fixedHeight,
                        pos.width, pos.height - leftButton.fixedHeight - rightButton.fixedHeight
                        );
                minRect = new Rect(pos.x, pos.y, pos.width, leftButton.fixedHeight);
                maxRect = new Rect(pos.x, pos.yMax - rightButton.fixedHeight, pos.width, rightButton.fixedHeight);
            }
        }

        private bool OnScrollerButton(Event evt)
        {
            bool firstClick = s_ScrollControlId != m_Slider.id;
            s_ScrollControlId = m_Slider.id;
            bool changed = false;

            if (firstClick)
            {
                changed = true;
                m_NextScrollStepTime = DateTime.Now.AddMilliseconds(ScrollWaitDefinitions.firstWait);
            }
            else
            {
                if (DateTime.Now >= m_NextScrollStepTime)
                {
                    changed = true;
                    m_NextScrollStepTime = DateTime.Now.AddMilliseconds(ScrollWaitDefinitions.regularWait);
                }
            }

            if (evt.type == EventType.Repaint)
            {
                GUI.InternalRepaintEditorWindow();
            }

            return changed;
        }
    }
}
