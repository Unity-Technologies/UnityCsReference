// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UEditor = UnityEditor.Editor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Toolbars;

namespace UnityEditor.TerrainTools
{
    internal class CondensedSlider : VisualElement, INotifyValueChanged<float>
    {
        private Slider m_Slider;
        private string m_Label;
        private Label m_LabelField;
        private Texture2D m_Image;

        // this is a variable of type func which takes in a float, string, Slider direction and returns a string
        public Func<float, string, SliderDirection, string> labelFormatting
        {
            get;
            set;
        }

        public StyleLength contentWidth
        {
            get => m_Slider.style.width;
            set => m_Slider.style.width = value;
        }

        public StyleLength contentHeight
        {
            get => m_Slider.style.height;
            set => m_Slider.style.height = value;
        }

        public SliderDirection direction
        {
            get =>  m_Slider.direction;
            set => m_Slider.direction = value;

        }

        public float value
        {
            get => m_Slider.value;
            set => SetValue(value);
        }

        public string directionClassSuffix = "horizontal";

        public CondensedSlider(float min, float max, SliderDirection direction = SliderDirection.Horizontal)
            : this(null, null, min, max, direction)
        {
        }

        public CondensedSlider(string label, float min, float max, SliderDirection direction = SliderDirection.Horizontal)
            : this(label, null, min, max, direction)
        {
        }

        public CondensedSlider(Texture2D image, float min, float max, SliderDirection direction = SliderDirection.Horizontal)
            : this(null, image, min, max, direction)
        {
        }

        public CondensedSlider(string label, Texture2D image, float min, float max, SliderDirection direction = SliderDirection.Horizontal)
        {
            m_Label = label;
            tooltip = label;
            m_Image = image;
            CreateSlider(min, max, direction);
        }

        private void UpdateValue(float value, bool withoutNotify)
        {
            if (withoutNotify)
                m_Slider.SetValueWithoutNotify(value);
            else
                m_Slider.value = value;

            var size = (value - m_Slider.lowValue) / (m_Slider.highValue - m_Slider.lowValue);
            if (m_Slider.direction == SliderDirection.Horizontal)
                m_Slider.Q("unity-tracker").style.width = new StyleLength(new Length(size * 100, LengthUnit.Percent));
            else
            {
                m_Slider.Q("unity-tracker").style.height = new StyleLength(new Length(size * 100, LengthUnit.Percent));
                m_Slider.Q("unity-tracker").style.top = new StyleLength(new Length((1 - size) * 100, LengthUnit.Percent));
            }

            m_LabelField.text = labelFormatting(m_Slider.value, m_Label, direction);
        }

        public void SetHighValueWithoutNotify(float highValue)
        {
            m_Slider.highValue = highValue;
            UpdateValue(m_Slider.value, true);
        }

        public void SetLowValueWithoutNotify(float lowValue)
        {
            m_Slider.lowValue = lowValue;
            UpdateValue(m_Slider.value, true);
        }

        private void SetValue(float value)
        {
            UpdateValue(value, false);
        }
        public void SetValueWithoutNotify(float value)
        {
            UpdateValue(value, true);
        }

        private void CreateSlider(float min, float max, SliderDirection direction)
        {
            labelFormatting = (value, label, direction) =>
            {
                if (direction == SliderDirection.Horizontal)
                    return $"{label} {value:F0}";
                return $"{value:F0}";
            };

            EditorToolbarUtility.LoadStyleSheets("CondensedSlider", this);
            directionClassSuffix = direction == SliderDirection.Horizontal ? "horizontal" : "vertical";

            AddToClassList("condensed-slider");
            AddToClassList("condensed-slider--"+directionClassSuffix);
            m_Slider = new Slider("", min, max, direction);
            m_Slider.AddToClassList("condensed-slider__slider");
            m_Slider.AddToClassList("condensed-slider__slider--"+directionClassSuffix);
            Add(m_Slider);
            var slider = m_Slider.Q("unity-tracker");
            slider.ClearClassList();
            slider.AddToClassList("condensed-slider__slider-tracker");

            m_Slider.Q("unity-dragger").style.display = DisplayStyle.None;
            m_Slider.Q("unity-dragger-border").style.display = DisplayStyle.None;

            var content = new VisualElement();
            content.name = "content";
            content.AddToClassList("condensed-slider__content--"+directionClassSuffix);
            content.pickingMode = PickingMode.Ignore;
            m_Slider.Add(content);
            var imageField = new VisualElement();
            imageField.AddToClassList("condensed-slider__image");
            imageField.AddToClassList("condensed-slider__image--"+directionClassSuffix);
            imageField.pickingMode = PickingMode.Ignore;
            if (m_Image == null)
                imageField.style.display = DisplayStyle.None;
            else
                imageField.style.backgroundImage = m_Image;
            content.Add(imageField);
            m_LabelField = new Label(labelFormatting(m_Slider.value, m_Label, direction));
            m_LabelField.AddToClassList("condensed-slider__label");
            m_LabelField.AddToClassList("condensed-slider__label--"+directionClassSuffix);
            m_LabelField.pickingMode = PickingMode.Ignore;
            content.Add(m_LabelField);

            var contentTextField = new VisualElement();
            contentTextField.name = "contentTextField";
            contentTextField.AddToClassList("condensed-slider__content-textfield--"+directionClassSuffix);
            contentTextField.pickingMode = PickingMode.Ignore;
            m_Slider.Add(contentTextField);
            var textField = new FloatField();
            textField.name = "textField";
            textField.AddToClassList("condensed-slider__label");
            textField.AddToClassList("condensed-slider__textfield--"+directionClassSuffix);
            textField.style.display = DisplayStyle.None;
            if (m_Image != null)
            {
                if (direction == SliderDirection.Horizontal)
                    textField.style.marginLeft = 23;
                else
                    textField.style.marginTop = 34;
            }

            contentTextField.Add(textField);

            RegisterCallback<AttachToPanelEvent>(RegisterCallbacks);
            RegisterCallback<DetachFromPanelEvent>(UnregisterCallbacks);
        }

        private void RegisterCallbacks(AttachToPanelEvent e)
        {
            var textField = m_Slider.Q("textField") as FloatField;
            var contentTextField = m_Slider.Q("contentTextField"); ;
            m_Slider.RegisterCallback<GeometryChangedEvent>(OnSliderRectChange);
            m_Slider.RegisterCallback<GeometryChangedEvent>(OnSliderWidthChange);
            textField.RegisterValueChangedCallback(TextFieldValueChange);
            m_Slider.RegisterValueChangedCallback(SliderSetValue);
            m_Slider.RegisterCallback<MouseDownEvent>(SliderMouseDownEvent);
            m_Slider.RegisterCallback<MouseUpEvent>(SliderMouseUpEvent);
            contentTextField.RegisterCallback<MouseDownEvent>(TextFieldMouseDownEvent);
            textField.RegisterCallback<KeyDownEvent>(TextFieldKeyDownEvent);
        }

        private void UnregisterCallbacks(DetachFromPanelEvent e)
        {
            var textField = m_Slider.Q("textField") as FloatField;
            var contentTextField = m_Slider.Q("contentTextField");
            m_Slider.UnregisterCallback<GeometryChangedEvent>(OnSliderRectChange);
            m_Slider.UnregisterCallback<GeometryChangedEvent>(OnSliderWidthChange);
            textField.UnregisterValueChangedCallback(TextFieldValueChange);
            m_Slider.UnregisterValueChangedCallback(SliderSetValue);
            m_Slider.UnregisterCallback<MouseDownEvent>(SliderMouseDownEvent);
            m_Slider.UnregisterCallback<MouseUpEvent>(SliderMouseUpEvent);
            contentTextField.UnregisterCallback<MouseDownEvent>(TextFieldMouseDownEvent);
            textField.UnregisterCallback<KeyDownEvent>(TextFieldKeyDownEvent);
        }

        private void OnSliderRectChange(GeometryChangedEvent e)
        {
            var slider = m_Slider.Q("unity-tracker");
            if (direction == SliderDirection.Horizontal)
                slider.style.height = e.newRect.height;
            else
                slider.style.width = e.newRect.width;
        }

        private void OnSliderWidthChange(GeometryChangedEvent e)
        {
            var content = m_Slider.Q("content");
            content.style.width = e.newRect.width;
        }

        private void TextFieldValueChange(ChangeEvent<float> e)
        {
            UpdateValue(e.newValue, false);
        }

        private void SliderSetValue(ChangeEvent<float> e)
        {
            SetValueWithoutNotify(e.newValue);
        }

        private void SliderMouseDownEvent(MouseDownEvent e)
        {
            if (e.button == (int)MouseButton.RightMouse)
            {
                e.StopPropagation();
                e.PreventDefault();

                var textField = m_Slider.Q("textField") as FloatField;
                var contentTextField = m_Slider.Q("contentTextField");

                if (textField != null)
                {
                    textField.value = m_Slider.value;
                    textField.style.display = DisplayStyle.Flex;
                }

                m_LabelField.style.display = DisplayStyle.None;
                contentTextField.pickingMode = PickingMode.Position;
            }
        }

        private void SliderMouseUpEvent(MouseUpEvent e)
        {
            if (e.button == (int)MouseButton.RightMouse)
            {
                e.StopPropagation();
                e.PreventDefault();
            }
        }

        private void TextFieldMouseDownEvent(MouseDownEvent e)
        {
            var textField = m_Slider.Q("textField") as FloatField;
            var contentTextField = m_Slider.Q("contentTextField");

            contentTextField.pickingMode = PickingMode.Ignore;
            if (textField != null)
            {
                if (textField.style.display == DisplayStyle.Flex)
                {
                    textField.style.display = DisplayStyle.None;
                    m_LabelField.style.display = DisplayStyle.Flex;
                }
            }
        }

        private void TextFieldKeyDownEvent(KeyDownEvent e)
        {
            var textField = m_Slider.Q("textField") as FloatField;
            var contentTextField = m_Slider.Q("contentTextField");

            if (e.keyCode == KeyCode.Escape || e.keyCode == KeyCode.Return)
            {
                contentTextField.pickingMode = PickingMode.Ignore;

                if (textField != null)
                {
                    if (textField.style.display == DisplayStyle.Flex)
                    {
                        textField.style.display = DisplayStyle.None;
                        m_LabelField.style.display = DisplayStyle.Flex;
                    }
                }
            }
        }

        public void UpdateDirection(SliderDirection newDirection, float min, float max)
        {
            if (direction == newDirection) return; // no new direction
            direction = newDirection;
            Clear();
            ClearClassList();
            CreateSlider(min, max, newDirection);
        }

    }

    internal class CondensedSliderDropdown : CondensedSlider
    {
        private readonly Button m_Dropdown;

        public event Action clicked
        {
            add => m_Dropdown.clicked += value;
            remove => m_Dropdown.clicked -= value;
        }

        public CondensedSliderDropdown(float min, float max, Action clicked, SliderDirection direction = SliderDirection.Horizontal)
            : this(null, null, min, max, clicked, direction)
        { }

        public CondensedSliderDropdown(string label, float min, float max, Action clicked, SliderDirection direction = SliderDirection.Horizontal)
            : this(label, null, min, max, clicked, direction)
        { }
        public CondensedSliderDropdown(Texture2D image, float min, float max, Action clicked, SliderDirection direction = SliderDirection.Horizontal)
            : this(null, image, min, max, clicked, direction)
        { }
        public CondensedSliderDropdown(string label, Texture2D image, float min, float max, Action clicked, SliderDirection direction = SliderDirection.Horizontal)
            : base(label, image, min, max, direction)
        {
            directionClassSuffix = direction == SliderDirection.Horizontal ? "horizontal" : "vertical";

            m_Dropdown = new Button(clicked);
            Add(m_Dropdown);
            m_Dropdown.ClearClassList();
            m_Dropdown.AddToClassList("unity-base-popup-field__arrow");
            m_Dropdown.AddToClassList("condensed-slider__dropdown--"+directionClassSuffix);
        }

    }
}
