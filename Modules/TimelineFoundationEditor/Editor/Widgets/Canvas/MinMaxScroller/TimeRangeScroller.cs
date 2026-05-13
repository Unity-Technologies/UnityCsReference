// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.IntegerTime;
using Unity.Timeline.Foundation.Time;
using Unity.Timeline.Foundation.Widgets.Properties;
using Unity.Timeline.Foundation.Widgets.Internals;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.Widgets
{
    [UxmlElement]
    internal partial class TimeRangeScroller : VisualElement
    {
        const float k_ContentDurationPaddingInPixels = 30f;
        const string k_UnityScroller = "unity-scroller";
        const string k_UnityScrollerHorizontal = k_UnityScroller + "--horizontal";
        const string k_StyleClassName = "timeRangeScroller";
        const string k_TimeRangeStepButton = "timeRangeStepButton";
        const string k_DurationIndicator = k_StyleClassName + "--durationIndicator";
        const string k_StepButtonLeft = k_TimeRangeStepButton + "__left";
        const string k_StepButtonRight = k_TimeRangeStepButton + "__right";

        public static readonly StylesheetResource stylesheetResource = UIResources.StylesheetFactory.Get(
            $"{nameof(TimeRangeScroller)}/{nameof(TimeRangeScroller)}");

        readonly RangeSlider m_Slider;
        readonly TimeIndicator m_CurrentTimeIndicator;
        readonly TimeIndicator m_DurationIndicator;
        readonly TimeMinimap m_Minimap;

        public TimeRangeScroller()
        {
            stylesheetResource.ApplyTo(this);
            AddToClassList(k_UnityScroller);
            AddToClassList(k_UnityScrollerHorizontal);
            this.AddToTimelineClassList(k_StyleClassName);
            m_Slider = new RangeSlider();
            m_Slider.RegisterValueChangedCallback(SliderValueChanged);
            Add(CreateStepButton(true));
            // Removed temporarily until UUM-84683 is fixed
            //Add(m_Slider);
            Add(CreateStepButton(false));

            m_Minimap = new TimeMinimap();

            m_DurationIndicator = new TimeIndicator();
            stylesheetResource.ApplyTo(m_DurationIndicator);
            m_DurationIndicator.AddToTimelineClassList(k_DurationIndicator);
            m_Minimap.Add(m_DurationIndicator);

            m_CurrentTimeIndicator = new TimeIndicator();
            m_Minimap.Add(m_CurrentTimeIndicator);

            m_Slider.Add(m_Minimap);

            RegisterCallback<PointerCaptureEvent>(PointerCaptured);
            RegisterCallback<PointerCaptureOutEvent>(PointerCaptureOut);
        }

        bool m_PointerCaptured;
        void PointerCaptured(PointerCaptureEvent evt)
        {
            if (GetContentDurationWithPadding_Internal() < m_Slider.highLimit)
                m_PointerCaptured = true;
        }

        void PointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (m_PointerCaptured)
            {
                m_PointerCaptured = false;
                CommitLimits();
            }
        }

        VisualElement CreateStepButton(bool left)
        {
            var stepButton = new RepeatButton();
            stepButton.AddToTimelineClassList(k_TimeRangeStepButton);
            stepButton.AddToTimelineClassList(CommonStyles.flatClickable);
            stepButton.AddToTimelineClassList(CommonStyles.toolbarButton);
            string stepButtonStyle = left ? k_StepButtonLeft : k_StepButtonRight;
            stepButton.AddToTimelineClassList(stepButtonStyle);
            stepButton.SetAction(() => StepButtonClicked(left), 250, 30);
            return stepButton;
        }

        TimeRange DisplayRange => m_Slider.value;
        public TimeRange Limits => new TimeRange(m_Slider.lowLimit, m_Slider.highLimit);

        void SliderValueChanged(ChangeEvent<Vector2> evt)
        {
            DisplayRangeChangeEvent.Send(this, evt.previousValue, evt.newValue);
        }

        void StepButtonClicked(bool left)
        {
            float scrollStepSize = (float)DisplayRange.duration * 10 / layout.width * (left ? -1 : 1);

            var newValue = new Vector2(m_Slider.value.x + scrollStepSize, m_Slider.value.y + scrollStepSize);
            if (newValue.x < 0)
                newValue = new Vector2(0, m_Slider.value.magnitude);

            if (newValue.y > m_Slider.highLimit)
                m_Slider.highLimit = newValue.y;
            m_Slider.value = newValue;
        }

        public void SetRange(TimeRange displayRange)
        {
            m_Slider.SetRange((float)displayRange.start, (float)displayRange.end);
            SetLimits(m_Slider.lowLimit, Mathf.Max((float)displayRange.end, GetContentDurationWithPadding_Internal()));
        }

        // This overload is only used in Tests
        internal void SetLimits_Internal(TimeRange limits)
        {
            SetLimits((float)limits.start, (float)limits.end);
        }

        float m_QueuedLowLimit;
        float m_QueuedHighLimit;
        void SetLimits(float lowLimit, float highLimit)
        {
            if (m_PointerCaptured)
            {
                m_QueuedLowLimit = lowLimit;
                m_QueuedHighLimit = highLimit;
            }
            else
            {
                m_Slider.SetLimits(lowLimit, highLimit);
                m_Minimap.SetMaxValue(m_Slider.highLimit);
            }
        }

        void CommitLimits()
        {
            if (m_QueuedLowLimit >= 0 && m_QueuedHighLimit >= 0)
            {
                SetLimits(m_QueuedLowLimit, Mathf.Max(m_QueuedHighLimit, GetContentDurationWithPadding_Internal()));
                m_QueuedLowLimit = -1;
                m_QueuedHighLimit = -1;
            }
        }

        public void SetCurrentTime(DiscreteTime time)
        {
            m_CurrentTimeIndicator.time = time;
        }

        public void SetShowCurrentTime(bool value)
        {
            if (value)
                m_CurrentTimeIndicator.Show();
            else
                m_CurrentTimeIndicator.Hide();
        }

        public void SetContentDuration(DiscreteTime duration)
        {
            m_DurationIndicator.time = duration;
        }

        internal float GetContentDurationWithPadding_Internal()
        {
            float pixelsPerSecond = TimeViewUtility.DurationToPixelWidth(1, m_Slider.layout.width, DisplayRange, CanvasTransform.foundationCanvasPixelsBeforeZero);
            if (Mathf.Approximately(0f, pixelsPerSecond))
                return (float)m_DurationIndicator.time;
            float paddingInSeconds = k_ContentDurationPaddingInPixels / pixelsPerSecond;
            return (float)m_DurationIndicator.time + paddingInSeconds;
        }
    }
}
