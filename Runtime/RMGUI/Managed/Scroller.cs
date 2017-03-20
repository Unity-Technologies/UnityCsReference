// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.RMGUI
{
    class Scroller : VisualContainer
    {
        public delegate void OnScrollerValueChanged(float newValue);

        // TODO: Don't we want to be able to adjust this value?
        private const float ScrollStepSize = 10f;

        public Slider slider
        {
            get { return this.m_Slider; }
        }

        public RepeatButton leftButton
        {
            get { return this.m_LeftButton; }
        }

        public RepeatButton rightButton
        {
            get { return this.m_RightButton; }
        }

        private readonly Slider m_Slider;
        private readonly RepeatButton m_LeftButton;
        private readonly RepeatButton m_RightButton;

        private float m_PageSize;
        private float m_LeftValue;
        private float m_RightValue;

        public float value { get; private set; }

        public event OnScrollerValueChanged onChange;

        public Scroller()
        {
            m_Slider = new Slider(OnSliderValueChange);
            AddChild(m_Slider);
            m_LeftButton = new RepeatButton(OnClickLeftButton, ScrollWaitDefinitions.firstWait, ScrollWaitDefinitions.regularWait);
            AddChild(m_LeftButton);
            m_RightButton = new RepeatButton(OnClickRightButton, ScrollWaitDefinitions.firstWait, ScrollWaitDefinitions.regularWait);
            AddChild(m_RightButton);
        }

        void OnSliderValueChange(float value)
        {
            ValidateSliderValue(value);
            if (onChange != null)
                onChange(value);
        }

        void OnClickLeftButton()
        {
            ValidateSliderValue(value - (ScrollStepSize * (m_LeftValue < m_RightValue ? 1f : -1f)));
            if (onChange != null)
                onChange(value);
            // sync slider value
            m_Slider.value = value;
        }

        void OnClickRightButton()
        {
            ValidateSliderValue(value + (ScrollStepSize * (m_LeftValue < m_RightValue ? 1f : -1f)));
            if (onChange != null)
                onChange(value);
            // sync slider value
            m_Slider.value = value;
        }

        public void SetProperties(Rect pos, float val, float size, float leftValue, float rightValue, bool horiz)
        {
            position = pos;

            m_PageSize = size;
            m_LeftValue = leftValue;
            m_RightValue = rightValue;

            value = val;

            Rect sliderRect, minRect, maxRect;

            GetRects(horiz, position, m_LeftButton.style, m_RightButton.style, out sliderRect, out minRect, out maxRect);
            m_Slider.SetProperties(sliderRect, value, size, leftValue, rightValue, horiz);

            // TODO default style settings should do flexbox layout
            // Or we need a hook into the layout system
            m_LeftButton.position = minRect;
            m_RightButton.position = maxRect;
        }

        public void ValidateSliderValue(float newValue)
        {
            value = newValue;
            if (m_LeftValue < m_RightValue)
                value = Mathf.Clamp(value, m_LeftValue, m_RightValue - m_PageSize);
            else
                value = Mathf.Clamp(value, m_RightValue, m_LeftValue - m_PageSize);
        }

        private void GetRects(bool horiz, Rect pos, GUIStyle leftButton, GUIStyle rightButton, out Rect sliderRect, out Rect minRect, out Rect maxRect)
        {
            if (horiz)
            {
                sliderRect = new Rect(
                        leftButton.fixedWidth, 0,
                        pos.width - leftButton.fixedWidth - rightButton.fixedWidth, pos.height
                        );
                minRect = new Rect(0, 0, leftButton.fixedWidth, pos.height);
                maxRect = new Rect(pos.width - rightButton.fixedWidth, 0, rightButton.fixedWidth, pos.height);
            }
            else
            {
                sliderRect = new Rect(
                        0, leftButton.fixedHeight,
                        pos.width, pos.height - leftButton.fixedHeight - rightButton.fixedHeight
                        );
                minRect = new Rect(0, 0, pos.width, leftButton.fixedHeight);
                maxRect = new Rect(0, pos.height - rightButton.fixedHeight, pos.width, rightButton.fixedHeight);
            }
        }
    }
}
