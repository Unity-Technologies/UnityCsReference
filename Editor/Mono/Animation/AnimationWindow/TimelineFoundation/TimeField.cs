// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using Unity.IntegerTime;
using UnityEngine.UIElements;

using FrameRate = UnityEngine.Playables.FrameRate;

namespace UnityEditor.Animations.AnimationWindow.TimelineFoundation
{
    class TimeField : VisualElement
    {
        [global::System.Runtime.CompilerServices.CompilerGenerated]
        [global::System.Serializable]
        internal new class UxmlSerializedData : global::UnityEngine.UIElements.VisualElement.UxmlSerializedData
        {
            [global::UnityEngine.UIElements.RegisterUxmlCacheAttribute]
            [global::System.Diagnostics.Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                global::UnityEngine.UIElements.UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new global::UnityEngine.UIElements.UxmlAttributeNames[]
                {
                    new (nameof(Label), "label", null, global::System.Array.Empty<string>()),
                    new (nameof(ShowLabels), "showLabels", null, global::System.Array.Empty<string>()),
                }
                , true);
            }

            #pragma warning disable 649
            [global::UnityEngine.UIElements.UxmlAttributeAttribute("label")]
            [global::UnityEngine.SerializeField] string Label;
            [global::UnityEngine.UIElements.UxmlAttributeAttribute("showLabels")]
            [global::UnityEngine.SerializeField] bool ShowLabels;
            [global::UnityEngine.SerializeField, global::UnityEngine.UIElements.UxmlIgnore, global::UnityEngine.HideInInspector] global::UnityEngine.UIElements.UxmlSerializedData.UxmlAttributeFlags Label_UxmlAttributeFlags;
            [global::UnityEngine.SerializeField, global::UnityEngine.UIElements.UxmlIgnore, global::UnityEngine.HideInInspector] global::UnityEngine.UIElements.UxmlSerializedData.UxmlAttributeFlags ShowLabels_UxmlAttributeFlags;
            #pragma warning restore 649

            public override object CreateInstance() => new TimeField();
            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);
                var e = (TimeField)obj;
                if (ShouldWriteAttributeValue(Label_UxmlAttributeFlags))
                {
                    e.Label = this.Label;
                }
                if (ShouldWriteAttributeValue(ShowLabels_UxmlAttributeFlags))
                {
                    e.ShowLabels = this.ShowLabels;
                }
            }
        }


        [UxmlAttribute("label")]
        public string Label
        {
            get => m_FieldName.text;
            set => m_FieldName.text = value;
        }

        [UxmlAttribute("showLabels")]
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
