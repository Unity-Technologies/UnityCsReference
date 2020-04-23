using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    public class Scroller : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<Scroller, UxmlTraits> {}

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlFloatAttributeDescription m_LowValue = new UxmlFloatAttributeDescription { name = "low-value", obsoleteNames = new[] { "lowValue" } };
            UxmlFloatAttributeDescription m_HighValue = new UxmlFloatAttributeDescription { name = "high-value", obsoleteNames = new[] { "highValue" } };
            UxmlEnumAttributeDescription<SliderDirection> m_Direction = new UxmlEnumAttributeDescription<SliderDirection> { name = "direction", defaultValue = SliderDirection.Vertical};
            UxmlFloatAttributeDescription m_Value = new UxmlFloatAttributeDescription { name = "value" };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                Scroller scroller = ((Scroller)ve);
                scroller.slider.lowValue = m_LowValue.GetValueFromBag(bag, cc);
                scroller.slider.highValue = m_HighValue.GetValueFromBag(bag, cc);
                scroller.direction = m_Direction.GetValueFromBag(bag, cc);
                scroller.value = m_Value.GetValueFromBag(bag, cc);
            }
        }

        // Usually set by the owner of the scroller
        public event System.Action<float> valueChanged;

        public Slider slider { get; private set; }
        public RepeatButton lowButton { get; private set; }
        public RepeatButton highButton { get; private set; }

        public float value
        {
            get { return slider.value; }
            set { slider.value = value; }
        }

        public float lowValue
        {
            get { return slider.lowValue; }
            set { slider.lowValue = value; }
        }

        public float highValue
        {
            get { return slider.highValue; }
            set { slider.highValue = value; }
        }

        public SliderDirection direction
        {
            get { return resolvedStyle.flexDirection == FlexDirection.Row ? SliderDirection.Horizontal : SliderDirection.Vertical; }
            set
            {
                slider.direction = value;
                if (value == SliderDirection.Horizontal)
                {
                    style.flexDirection = FlexDirection.Row;
                    AddToClassList(horizontalVariantUssClassName);
                    RemoveFromClassList(verticalVariantUssClassName);
                }
                else
                {
                    style.flexDirection = FlexDirection.Column;
                    AddToClassList(verticalVariantUssClassName);
                    RemoveFromClassList(horizontalVariantUssClassName);
                }
            }
        }

        internal const float kDefaultPageSize = 20.0f;

        public static readonly string ussClassName = "unity-scroller";
        public static readonly string horizontalVariantUssClassName = ussClassName + "--horizontal";
        public static readonly string verticalVariantUssClassName = ussClassName + "--vertical";
        public static readonly string sliderUssClassName = ussClassName + "__slider";
        public static readonly string lowButtonUssClassName = ussClassName + "__low-button";
        public static readonly string highButtonUssClassName = ussClassName + "__high-button";

        public Scroller()
            : this(0, 0, null) {}

        public Scroller(float lowValue, float highValue, System.Action<float> valueChanged, SliderDirection direction = SliderDirection.Vertical)
        {
            AddToClassList(ussClassName);

            // Add children in correct order
            slider = new Slider(lowValue, highValue, direction, kDefaultPageSize) {name = "unity-slider", viewDataKey = "Slider"};
            slider.AddToClassList(sliderUssClassName);
            slider.RegisterValueChangedCallback(OnSliderValueChange);

            lowButton = new RepeatButton(ScrollPageUp, ScrollWaitDefinitions.firstWait, ScrollWaitDefinitions.regularWait) { name = "unity-low-button" };
            lowButton.AddToClassList(lowButtonUssClassName);
            Add(lowButton);
            highButton = new RepeatButton(ScrollPageDown, ScrollWaitDefinitions.firstWait, ScrollWaitDefinitions.regularWait) { name = "unity-high-button" };
            highButton.AddToClassList(highButtonUssClassName);
            Add(highButton);
            Add(slider);

            this.direction = direction;
            this.valueChanged = valueChanged;
        }

        public void Adjust(float factor)
        {
            // Any factor smaller than 1f will enable the scroller (and its children)
            SetEnabled(factor < 1f);
            slider.AdjustDragElement(factor);
        }

        void OnSliderValueChange(ChangeEvent<float> evt)
        {
            value = evt.newValue;

            valueChanged?.Invoke(slider.value);
            this.IncrementVersion(VersionChangeType.Repaint);
        }

        public void ScrollPageUp()
        {
            ScrollPageUp(1.0f);
        }

        public void ScrollPageDown()
        {
            ScrollPageDown(1.0f);
        }

        public void ScrollPageUp(float factor)
        {
            value -= factor * (slider.pageSize * (slider.lowValue < slider.highValue ? 1f : -1f));
        }

        public void ScrollPageDown(float factor)
        {
            value += factor * (slider.pageSize * (slider.lowValue < slider.highValue ? 1f : -1f));
        }
    }
}
