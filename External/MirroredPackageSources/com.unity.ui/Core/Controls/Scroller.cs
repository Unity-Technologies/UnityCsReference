using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A vertical or horizontal scrollbar.
    /// </summary>
    public class Scroller : VisualElement
    {
        /// <summary>
        /// Instantiates a <see cref="Scroller"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<Scroller, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="Scroller"/>.
        /// </summary>
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlFloatAttributeDescription m_LowValue = new UxmlFloatAttributeDescription { name = "low-value", obsoleteNames = new[] { "lowValue" } };
            UxmlFloatAttributeDescription m_HighValue = new UxmlFloatAttributeDescription { name = "high-value", obsoleteNames = new[] { "highValue" } };
            UxmlEnumAttributeDescription<SliderDirection> m_Direction = new UxmlEnumAttributeDescription<SliderDirection> { name = "direction", defaultValue = SliderDirection.Vertical};
            UxmlFloatAttributeDescription m_Value = new UxmlFloatAttributeDescription { name = "value" };

            /// <summary>
            /// Returns an empty enumerable, as scrollers do not have children.
            /// </summary>
            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            /// <summary>
            /// Initialize <see cref="Scroller"/> properties using values from the attribute bag.
            /// </summary>
            /// <param name="ve">The object to initialize.</param>
            /// <param name="bag">The attribute bag.</param>
            /// <param name="cc">The creation context; unused.</param>
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
        /// <summary>
        /// Event sent when the slider value has changed.
        /// </summary>
        public event System.Action<float> valueChanged;

        /// <summary>
        /// The slider used by this scroller.
        /// </summary>
        public Slider slider { get; private set; }
        /// <summary>
        /// Bottom or left scroll button.
        /// </summary>
        public RepeatButton lowButton { get; private set; }
        /// <summary>
        /// Top or right scroll button.
        /// </summary>
        public RepeatButton highButton { get; private set; }

        /// <summary>
        /// Value that defines the slider position. It lies between <see cref="lowValue"/> and <see cref="highValue"/>.
        /// </summary>
        public float value
        {
            get { return slider.value; }
            set { slider.value = value; }
        }

        /// <summary>
        /// Minimum value.
        /// </summary>
        public float lowValue
        {
            get { return slider.lowValue; }
            set { slider.lowValue = value; }
        }

        /// <summary>
        /// Maximum value.
        /// </summary>
        public float highValue
        {
            get { return slider.highValue; }
            set { slider.highValue = value; }
        }

        /// <summary>
        /// Direction of this scrollbar.
        /// </summary>
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

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string ussClassName = "unity-scroller";
        /// <summary>
        /// USS class name of elements of this type, when they are displayed horizontally.
        /// </summary>
        public static readonly string horizontalVariantUssClassName = ussClassName + "--horizontal";
        /// <summary>
        /// USS class name of elements of this type, when they are displayed vertically.
        /// </summary>
        public static readonly string verticalVariantUssClassName = ussClassName + "--vertical";
        /// <summary>
        /// USS class name of slider elements in elements of this type.
        /// </summary>
        public static readonly string sliderUssClassName = ussClassName + "__slider";
        /// <summary>
        /// USS class name of low buttons in elements of this type.
        /// </summary>
        public static readonly string lowButtonUssClassName = ussClassName + "__low-button";
        /// <summary>
        /// USS class name of high buttons in elements of this type.
        /// </summary>
        public static readonly string highButtonUssClassName = ussClassName + "__high-button";

        /// <summary>
        /// Constructor.
        /// </summary>
        public Scroller()
            : this(0, 0, null) {}

        /// <summary>
        /// Constructor.
        /// </summary>
        public Scroller(float lowValue, float highValue, System.Action<float> valueChanged, SliderDirection direction = SliderDirection.Vertical)
        {
            AddToClassList(ussClassName);

            // Add children in correct order
            slider = new Slider(lowValue, highValue, direction, kDefaultPageSize) {name = "unity-slider", viewDataKey = "Slider"};
            slider.AddToClassList(sliderUssClassName);
            slider.RegisterValueChangedCallback(OnSliderValueChange);

            // We want default behavior for vertical scrollers to be lowValue at the top and highValue at the bottom, instead of the default Slider behavior.
            slider.inverted = direction == SliderDirection.Vertical;

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

        /// <summary>
        /// Updates the slider element size as a ratio of total range. A value greater than 1 will disable the Scroller.
        /// </summary>
        /// <param name="factor">Slider size ratio.</param>
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

        /// <summary>
        /// Will change the value according to the current slider pageSize.
        /// </summary>
        public void ScrollPageUp()
        {
            ScrollPageUp(1.0f);
        }

        /// <summary>
        /// Will change the value according to the current slider pageSize.
        /// </summary>
        public void ScrollPageDown()
        {
            ScrollPageDown(1.0f);
        }

        /// <summary>
        /// Will change the value according to the current slider pageSize.
        /// </summary>
        public void ScrollPageUp(float factor)
        {
            value -= factor * (slider.pageSize * (slider.lowValue < slider.highValue ? 1f : -1f));
        }

        /// <summary>
        /// Will change the value according to the current slider pageSize.
        /// </summary>
        public void ScrollPageDown(float factor)
        {
            value += factor * (slider.pageSize * (slider.lowValue < slider.highValue ? 1f : -1f));
        }
    }
}
