// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Experimental.UIElements.StyleEnums;

namespace UnityEngine.Experimental.UIElements
{
    // TODO: Using ScrollerButton class for now, as Button class uses Skin styles by default because of the GUISkinStyle attribute
    // ScrollerButton is a repeat button without any skin styles
    public class ScrollerButton : VisualElement
    {
        public Clickable clickable;

        public ScrollerButton(System.Action clickEvent, long delay, long interval)
        {
            clickable = new Clickable(clickEvent, delay, interval);
            this.AddManipulator(clickable);
        }
    }

    public class Scroller : VisualElement
    {
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

        public Slider.Direction direction
        {
            get { return style.flexDirection == FlexDirection.Row ? Slider.Direction.Horizontal : Slider.Direction.Vertical; }
            set
            {
                if (value == Slider.Direction.Horizontal)
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

        public Scroller(float lowValue, float highValue, System.Action<float> valueChanged, Slider.Direction direction = Slider.Direction.Vertical)
        {
            this.direction = direction;
            this.valueChanged = valueChanged;

            // Add children in correct order
            slider = new Slider(lowValue, highValue, OnSliderValueChange, direction) {name = "Slider", persistenceKey = "Slider"};
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

        void OnSliderValueChange(float newValue)
        {
            value = newValue;

            if (valueChanged != null)
                valueChanged(slider.value);
            this.Dirty(ChangeType.Repaint);
        }

        public void ScrollPageUp()
        {
            value -= (slider.pageSize * (slider.lowValue < slider.highValue ? 1f : -1f));
        }

        public void ScrollPageDown()
        {
            value += (slider.pageSize * (slider.lowValue < slider.highValue ? 1f : -1f));
        }
    }
}
