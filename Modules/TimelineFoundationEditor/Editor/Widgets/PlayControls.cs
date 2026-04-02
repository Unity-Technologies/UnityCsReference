// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Time;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.Widgets
{
    partial class PlayControls : VisualElement
    {
        // [UxmlElement] does no codegen in trunk (6000.2); we have to provide the generated UxmlSerializedData manually.
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new PlayControls();
        }

        const string k_ClassName = "playControl";
        protected const string k_FirstFrameElement = "firstFrame";
        protected const string k_PreviousFrameElement = "previousFrame";
        protected const string k_PlayElement = "play";
        protected const string k_NextFrameElement = "nextFrame";
        protected const string k_LastFrameElement = "lastFrame";
        protected const string k_PlayRangeElement = "playRange";
        protected const string k_TimeInputElement = "timeInput";

        static readonly TemplateResource k_Template = UIResources.TemplateFactory.Get<PlayControls>();
        static readonly StylesheetResource k_Stylesheet = UIResources.StylesheetFactory.Get<PlayControls>();

        ToolbarToggle m_PlayToggle;
        ToolbarToggle m_PlayRangeToggle;
        TimeField m_PlayTimeField;

        public ToolbarToggle playRangeToggle => m_PlayRangeToggle;
        protected ToolbarToggle playToggle => m_PlayToggle;

        public PlayControls()
        {
            k_Template.CloneInto(this);
            UIResources.CommonStylesheet.ApplyTo(this);
            k_Stylesheet.ApplyTo(this);
            this.AddToTimelineClassList(k_ClassName);

            var gotoBeginButton = this.Q<Button>(k_FirstFrameElement);
            gotoBeginButton.clickable.clicked += () => FirstFrameClicked?.Invoke();
            var previousFrameButton = this.Q<Button>(k_PreviousFrameElement);
            previousFrameButton.clickable.clicked += () => StepBackClicked?.Invoke();
            m_PlayToggle = this.Q<ToolbarToggle>(k_PlayElement);
            m_PlayToggle.RegisterValueChangedCallback(evt => PlayClicked?.Invoke(evt.newValue));
            var nextFrameButton = this.Q<Button>(k_NextFrameElement);
            nextFrameButton.clickable.clicked += () => StepForwardClicked?.Invoke();
            var gotoEndButton = this.Q<Button>(k_LastFrameElement);
            gotoEndButton.clickable.clicked += () => LastFrameClicked?.Invoke();

            m_PlayRangeToggle = this.Q<ToolbarToggle>(k_PlayRangeElement);
            m_PlayRangeToggle.RegisterValueChangedCallback(evt => TogglePlayRangeMarkersClicked?.Invoke(evt.newValue));

            m_PlayTimeField = this.Q<TimeField>(k_TimeInputElement);
            m_PlayTimeField.TimeChanged += newVal => PlayTimeChanged?.Invoke(newVal);
        }

        public bool Play
        {
            set => m_PlayToggle.SetValueWithoutNotify(value);
        }

        public float Time
        {
            set => m_PlayTimeField.SetValueWithoutNotify(value);
        }

        public TimeField PlayTimeField => m_PlayTimeField;

        public event Action<bool> PlayClicked;
        public event Action StepBackClicked;
        public event Action StepForwardClicked;
        public event Action FirstFrameClicked;
        public event Action LastFrameClicked;
        public event Action<bool> TogglePlayRangeMarkersClicked;
        public event Action<DiscreteTime> PlayTimeChanged;

        public TimeFormat TimeFormat
        {
            get => m_PlayTimeField.TimeFormat;
            set => m_PlayTimeField.TimeFormat = value;
        }
    }
}
