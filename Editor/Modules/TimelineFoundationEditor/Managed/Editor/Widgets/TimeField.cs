// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Time;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.Widgets
{
    [UxmlElement]
    internal partial class TimeField : VisualElement
    {
        [UxmlAttribute]
        public string Label
        {
            get => m_FieldName.text;
            set => m_FieldName.text = value;
        }

        [UxmlAttribute("show-labels")]
        public bool ShowLabels
        {
            get => m_FieldName.style.display == DisplayStyle.Flex;
            set => SetDisplayLabels(value);
        }

        Label m_FieldName;
        TextField m_Field;
        TimeFormat m_TimeFormat;
        FrameRate m_FrameRate;
        DiscreteTime m_Time;

        public TimeField()
        {
            AddToClassList("timeField");
            m_FieldName = new Label();
            m_FieldName.AddToTimelineClassList("timeFieldNameLabel");
            Add(m_FieldName);
            var inputContainer = new VisualElement();

            inputContainer.AddToTimelineClassList("timeFieldInputContainer");
            Add(inputContainer);

            m_Field = new TextField("TimeCode:");
            m_Field.AddToTimelineClassList("timeInput");
            m_Field.RegisterValueChangedCallback(TimeValueChanged);
            m_Field.isDelayed = true;
            inputContainer.Add(m_Field);

            FrameRate = FrameRate.k_60Fps;
            Time = DiscreteTime.Zero;
        }

        void SetDisplayLabels(bool display)
        {
            DisplayStyle displayStyle = display ? DisplayStyle.Flex : DisplayStyle.None;
            m_FieldName.style.display = displayStyle;
            m_Field.labelElement.style.display = displayStyle;
        }

        public event Action<DiscreteTime> TimeChanged;

        public DiscreteTime Time
        {
            get => m_Time;
            set
            {
                m_Time = value;
                TimeChanged?.Invoke(m_Time);
                RefreshDisplay();
            }
        }

        public TimeFormat TimeFormat
        {
            get => m_TimeFormat;
            set
            {
                if (m_TimeFormat != value)
                {
                    m_TimeFormat = value;
                    RefreshDisplay();
                }
            }
        }

        public FrameRate FrameRate
        {
            get => m_FrameRate;
            set
            {
                m_FrameRate = value;
                RefreshDisplay();
            }
        }

        public void SetValueWithoutNotify(float value)
        {
            m_Time = new DiscreteTime(value);
            RefreshDisplay();
        }

        public void SetValueWithoutNotify(DiscreteTime value)
        {
            m_Time = value;
            RefreshDisplay();
        }

        public void DisableTabIndex()
        {
            tabIndex = -1;
            m_Field.tabIndex = -1;
        }

        void TimeValueChanged(ChangeEvent<string> evt)
        {
            DiscreteTime newTime = new DiscreteTime(TimeFormat.FromTimeString(evt.newValue, FrameRate, -1));
            if (newTime >= DiscreteTime.Zero)
                Time = newTime;
            else
                Time = Time;
        }

        void RefreshDisplay()
        {
            //TODO - ATL-1510 - Improve TimeUtility to accept DiscreteTime values
            //     - If we are able to modify the TimeUtility.OnFrameBoundary to compare DiscreteTime values we can probably remove the format conditional below and always use f4
            string currentTime = TimeFormat.ToTimeString((double)Time, FrameRate, TimeFormat == TimeFormat.Frames ? "f0" : "f4");
            m_Field.SetValueWithoutNotify(currentTime);
        }
    }
}
