// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Properties;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A vertical or horizontal scrollbar. For more information, refer to [[wiki:UIE-uxml-element-scroller|UXML element Scroller]].
    /// </summary>
    /// <remarks>
    /// Each ScrollView has a Scroller for each axis, controlled by <see cref="ScrollerVisibility"/>. For more information, refer to <see cref="ScrollView"/>.
    /// If a Scroller is used in ScrollView, its <see cref="highValue"/> and <see cref="lowValue"/> are automatically overridden to match the ScrollView content size on <see cref="GeometryChangedEvent"/>.
    /// A Scroller contains a <see cref="Slider"/> and two <see cref="RepeatButton"/>s for scrolling.
    /// </remarks>
    /// <example>
    /// The following example creates a scroller independently from a ScrollView:
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/Scroller_Example.cs"/>
    /// </example>
    [UxmlElement(libraryPath = "Controls")]
    [Icon("UIToolkit/Icons/Scroller.png")]
    public partial class Scroller : VisualElement
    {
        internal static readonly BindingId valueProperty = nameof(value);
        internal static readonly BindingId lowValueProperty = nameof(lowValue);
        internal static readonly BindingId highValueProperty = nameof(highValue);
        internal static readonly BindingId directionProperty = nameof(direction);

        class ScrollerSlider : Slider
        {
            public ScrollerSlider(float start, float end,
                SliderDirection direction, float pageSize)
            : base(start, end, direction, pageSize)
            {
            }

            internal override float SliderNormalizeValue(float currentValue, float lowerValue, float higherValue)
            {
                // Ensure that the dragElement of the scrollbar never goes beyond the limits even when mouse wheel scrolling with
                // elastic animation.
                return Mathf.Clamp(base.SliderNormalizeValue(currentValue, lowerValue, higherValue), 0, 1);
            }
        }

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new(nameof(lowValue), "low-value", null, "lowValue"),
                    new(nameof(highValue), "high-value", null, "highValue"),
                    new(nameof(direction), "direction"),
                    new(nameof(value), "value"),
                }, false);
            }

            #pragma warning disable 649
            [UxmlAttribute("low-value", "lowValue")]
            [SerializeField] float lowValue;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags lowValue_UxmlAttributeFlags;
            [UxmlAttribute("high-value", "highValue")]
            [SerializeField] float highValue;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags highValue_UxmlAttributeFlags;
            [SerializeField] SliderDirection direction;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags direction_UxmlAttributeFlags;
            [SerializeField] float value;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags value_UxmlAttributeFlags;
            #pragma warning restore 649

            public override object CreateInstance() => new Scroller();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (Scroller)obj;
                if (ShouldWriteAttributeValue(lowValue_UxmlAttributeFlags))
                    e.slider.lowValue = lowValue;
                if (ShouldWriteAttributeValue(highValue_UxmlAttributeFlags))
                    e.slider.highValue = highValue;
                if (ShouldWriteAttributeValue(direction_UxmlAttributeFlags))
                    e.direction = direction;
                if (ShouldWriteAttributeValue(value_UxmlAttributeFlags))
                    e.value = value;
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
        public Slider slider { get; }

        /// <summary>
        /// Bottom or left scroll button.
        /// </summary>
        public RepeatButton lowButton { get;  }

        /// <summary>
        /// Top or right scroll button.
        /// </summary>
        public RepeatButton highButton { get; }

        /// <summary>
        /// Value that defines the slider position. It lies between <see cref="lowValue"/> and <see cref="highValue"/>.
        /// </summary>
        [CreateProperty]
        public float value
        {
            get { return slider.value; }
            set
            {
                var previous = slider.value;
                slider.value = value;
                if (!Mathf.Approximately(previous, slider.value))
                    NotifyPropertyChanged(valueProperty);
            }
        }

        /// <summary>
        /// Minimum value.
        /// </summary>
        [CreateProperty]
        public float lowValue
        {
            get { return slider.lowValue; }
            set
            {
                var previous = slider.lowValue;
                slider.lowValue = value;

                if (!Mathf.Approximately(previous, slider.lowValue))
                    NotifyPropertyChanged(lowValueProperty);
            }
        }

        /// <summary>
        /// Maximum value.
        /// </summary>
        [CreateProperty]
        public float highValue
        {
            get { return slider.highValue; }
            set
            {
                var previous = slider.highValue;
                slider.highValue = value;

                if (!Mathf.Approximately(previous, slider.highValue))
                    NotifyPropertyChanged(highValueProperty);
            }
        }

        /// <summary>
        /// Direction of this scrollbar.
        /// </summary>
        [CreateProperty]
        public SliderDirection direction
        {
            get { return resolvedStyle.flexDirection == FlexDirection.Row ? SliderDirection.Horizontal : SliderDirection.Vertical; }
            set
            {
                var previous = slider.direction;
                slider.direction = value;
                // We want default behavior for vertical scrollers to be lowValue at the top and highValue at the bottom,
                // instead of the default Slider behavior.
                slider.inverted = value == SliderDirection.Vertical;
                if (value == SliderDirection.Horizontal)
                {
                    style.flexDirection = FlexDirection.Row;
                    AddToClassList(horizontalVariantUssClassNameUnique);
                    RemoveFromClassList(verticalVariantUssClassNameUnique);
                }
                else
                {
                    style.flexDirection = FlexDirection.Column;
                    AddToClassList(verticalVariantUssClassNameUnique);
                    RemoveFromClassList(horizontalVariantUssClassNameUnique);
                }
                if (previous != slider.direction)
                    NotifyPropertyChanged(directionProperty);
            }
        }

        internal const float kDefaultPageSize = 20.0f;

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string ussClassName = "unity-scroller";
        internal static readonly UniqueStyleString ussClassNameUnique = new(ussClassName);

        /// <summary>
        /// USS class name of elements of this type, when they are displayed horizontally.
        /// </summary>
        public static readonly string horizontalVariantUssClassName = ussClassName + "--horizontal";
        internal static readonly UniqueStyleString horizontalVariantUssClassNameUnique = new(horizontalVariantUssClassName);

        /// <summary>
        /// USS class name of elements of this type, when they are displayed vertically.
        /// </summary>
        public static readonly string verticalVariantUssClassName = ussClassName + "--vertical";
        internal static readonly UniqueStyleString verticalVariantUssClassNameUnique = new(verticalVariantUssClassName);

        /// <summary>
        /// USS class name of slider elements in elements of this type.
        /// </summary>
        public static readonly string sliderUssClassName = ussClassName + "__slider";
        internal static readonly UniqueStyleString sliderUssClassNameUnique = new(sliderUssClassName);

        /// <summary>
        /// USS class name of low buttons in elements of this type.
        /// </summary>
        public static readonly string lowButtonUssClassName = ussClassName + "__low-button";
        internal static readonly UniqueStyleString lowButtonUssClassNameUnique = new(lowButtonUssClassName);

        /// <summary>
        /// USS class name of high buttons in elements of this type.
        /// </summary>
        public static readonly string highButtonUssClassName = ussClassName + "__high-button";
        internal static readonly UniqueStyleString highButtonUssClassNameUnique = new(highButtonUssClassName);

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
            AddToClassList(ussClassNameUnique);

            // Add children in correct order
            slider = new ScrollerSlider(lowValue, highValue, direction, kDefaultPageSize) {name = "unity-slider", viewDataKey = "Slider"};
            slider.AddToClassList(sliderUssClassNameUnique);
            slider.RegisterValueChangedCallback(OnSliderValueChange);

            lowButton = new RepeatButton(ScrollPageUp, ScrollWaitDefinitions.firstWait, ScrollWaitDefinitions.regularWait) { name = "unity-low-button" };
            lowButton.AddToClassList(lowButtonUssClassNameUnique);
            Add(lowButton);
            highButton = new RepeatButton(ScrollPageDown, ScrollWaitDefinitions.firstWait, ScrollWaitDefinitions.regularWait) { name = "unity-high-button" };
            highButton.AddToClassList(highButtonUssClassNameUnique);
            Add(highButton);
            Add(slider);

            this.direction = direction;
            this.valueChanged = valueChanged;
        }

        /// <summary>
        /// Updates the slider element size as a ratio of total range. A value greater than or equal to 1 will disable the Scroller.
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
