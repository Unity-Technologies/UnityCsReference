// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Experimental.UIElements.StyleEnums;

namespace UnityEngine.Experimental.UIElements
{
    // TODO: Using ScrollerButton class for now, as Button class uses Skin styles by default because of the GUISkinStyle attribute
    // ScrollerButton is a repeat button without any skin styles
    public class ScrollerButton : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<ScrollerButton, UxmlTraits> {}

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlLongAttributeDescription m_Delay = new UxmlLongAttributeDescription { name = "delay" };
            UxmlLongAttributeDescription m_Interval = new UxmlLongAttributeDescription { name = "interval" };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                ((ScrollerButton)ve).clickable = new Clickable(null, m_Delay.GetValueFromBag(bag, cc), m_Interval.GetValueFromBag(bag, cc));
            }
        }

        public Clickable clickable;

        public ScrollerButton() {}

        public ScrollerButton(System.Action clickEvent, long delay, long interval)
        {
            clickable = new Clickable(clickEvent, delay, interval);
            this.AddManipulator(clickable);
        }
    }

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
        public ScrollerButton lowButton { get; private set; }
        public ScrollerButton highButton { get; private set; }

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
            get { return style.flexDirection == FlexDirection.Row ? SliderDirection.Horizontal : SliderDirection.Vertical; }
            set
            {
                if (value == SliderDirection.Horizontal)
                {
                    style.flexDirection = FlexDirection.Row;
                    AddToClassList("horizontal");
                }
                else
                {
                    style.flexDirection = FlexDirection.Column;
                    AddToClassList("vertical");
                }
            }
        }

        internal const float kDefaultPageSize = 20.0f;

        public Scroller()
            : this(0, 0, null) {}

        public Scroller(float lowValue, float highValue, System.Action<float> valueChanged, SliderDirection direction = SliderDirection.Vertical)
        {
            this.direction = direction;
            this.valueChanged = valueChanged;

            // Add children in correct order
            slider = new Slider(lowValue, highValue, direction, kDefaultPageSize) {name = "Slider", persistenceKey = "Slider"};
            slider.OnValueChanged(OnSliderValueChange);

            Add(slider);
            lowButton = new ScrollerButton(ScrollPageUp, ScrollWaitDefinitions.firstWait, ScrollWaitDefinitions.regularWait) {name = "LowButton"};
            Add(lowButton);
            highButton = new ScrollerButton(ScrollPageDown, ScrollWaitDefinitions.firstWait, ScrollWaitDefinitions.regularWait) {name = "HighButton"};
            Add(highButton);
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

            if (valueChanged != null)
                valueChanged(slider.value);
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
