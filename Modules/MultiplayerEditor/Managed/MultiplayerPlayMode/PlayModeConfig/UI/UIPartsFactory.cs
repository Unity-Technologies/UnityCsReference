// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor
{
    internal static class UIFactory
    {
        public static SliderInt CreateStepSlider(SerializedProperty prop, string SliderLabel, int min, int max)
        {
            var slider = new SliderInt(min, max, SliderDirection.Horizontal, 100);
            slider.label = SliderLabel;
            slider.tooltip = prop.tooltip;
            var sliderProp = prop.Copy();
            slider.SetValueWithoutNotify(sliderProp.intValue);

            slider.RegisterValueChangedCallback(evt =>
            {
                var rounded = evt.newValue / 100 * 100;
                slider.SetValueWithoutNotify(rounded);
                sliderProp.serializedObject.Update();
                sliderProp.intValue = rounded;
                sliderProp.serializedObject.ApplyModifiedProperties();

            });
            slider.showInputField = true;
            return slider;
        }

        public static TextField CreateStandardTextField(SerializedProperty prop, string label)
        {
            var textField = new TextField(label);
            textField.BindProperty(prop);
            textField.Bind(prop.serializedObject);
            return textField;
        }

        public static TextField CreateTextfieldWithDefault(SerializedProperty prop, string label, bool isEditable = true, string defaultText = "Default set by the Unity Editor")
        {
            var textField = new TextField(label);
            textField.isReadOnly = !isEditable;
            var textInput = textField.Q<VisualElement>("unity-text-input");
            textField.BindProperty(prop);
            textField.Bind(prop.serializedObject);

            textField.RegisterCallback<FocusOutEvent>(evt =>
            {
                if (string.IsNullOrEmpty(textField.value))
                {
                    textField.SetValueWithoutNotify(defaultText);
                    textInput.style.opacity = 0.5f;
                }

                if (textField.value == defaultText)
                {
                    textInput.style.opacity = 0.5f;
                }
            });

            textField.RegisterCallback<FocusInEvent>(evt =>
            {
                textInput.style.opacity = 1f;
            });

            textField.RegisterValueChangedCallback(evt =>
            {
                // this condition is met when the binding system sets the initial value and the string is empty
                if (string.IsNullOrEmpty(evt.newValue) && string.IsNullOrEmpty(evt.previousValue))
                {
                    textField.SetValueWithoutNotify(defaultText);
                    textInput.style.opacity = 0.5f;
                }
            });

            return textField;
        }
    }
}
