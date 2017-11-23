// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    public sealed partial class EditorGUILayout
    {
        internal static void SliderWithTexture(
            GUIContent label,
            SerializedProperty property,
            float sliderMin, float sliderMax,
            float power,
            Texture2D sliderBackground,
            params GUILayoutOption[] options
            )
        {
            var rect = s_LastRect = GetSliderRect(false, options);
            EditorGUI.SliderWithTexture(rect, label, property, sliderMin, sliderMax, power, sliderBackground);
        }

        internal static float SliderWithTexture(
            GUIContent label,
            float sliderValue, float sliderMin, float sliderMax,
            string formatString,
            Texture2D sliderBackground,
            params GUILayoutOption[] options
            )
        {
            return SliderWithTexture(
                label, sliderValue, sliderMin, sliderMax, formatString, sliderMin, sliderMax, sliderBackground
                );
        }

        internal static float SliderWithTexture(
            GUIContent label,
            float sliderValue, float sliderMin, float sliderMax,
            string formatString, float textFieldMin, float textFieldMax,
            Texture2D sliderBackground,
            params GUILayoutOption[] options
            )
        {
            var rect = s_LastRect = GetSliderRect(false, options);
            return EditorGUI.SliderWithTexture(
                rect, label, sliderValue, sliderMin, sliderMax, formatString, textFieldMin, textFieldMax, 1f, sliderBackground
                );
        }
    }

    public sealed partial class EditorGUI
    {
        internal static void SliderWithTexture(
            Rect position,
            GUIContent label,
            SerializedProperty property,
            float sliderMin, float sliderMax,
            float power,
            Texture2D sliderBackground
            )
        {
            label = BeginProperty(position, label, property);

            BeginChangeCheck();

            var formatString = property.propertyType == SerializedPropertyType.Integer ?
                kIntFieldFormatString : kFloatFieldFormatString;
            var newValue = SliderWithTexture(
                    position, label, property.floatValue, sliderMin, sliderMax, formatString, sliderMin, sliderMax, power, sliderBackground
                    );

            if (EndChangeCheck())
                property.floatValue = newValue;

            EndProperty();
        }

        internal static float SliderWithTexture(
            Rect rect,
            GUIContent label,
            float sliderValue, float sliderMin, float sliderMax,
            string formatString,
            Texture2D sliderBackground,
            params GUILayoutOption[] options
            )
        {
            return SliderWithTexture(
                rect, label, sliderValue, sliderMin, sliderMax, formatString, sliderMin, sliderMax, 1f, sliderBackground
                );
        }

        internal static float SliderWithTexture(
            Rect position,
            GUIContent label,
            float sliderValue, float sliderMin, float sliderMax,
            string formatString, float textFieldMin, float textFieldMax,
            float power,
            Texture2D sliderBackground
            )
        {
            int id = GUIUtility.GetControlID(s_SliderHash, FocusType.Keyboard, position);
            var controlRect = PrefixLabel(position, id, label);
            var dragZone =
                LabelHasContent(label)
                ? EditorGUIUtility.DragZoneRect(position)
                : default(Rect);         // Ensure dragzone is empty when we have no label
            return DoSlider(
                controlRect, dragZone, id, sliderValue, sliderMin, sliderMax, formatString, textFieldMin, textFieldMax, power, "ColorPickerSliderBackground", "ColorPickerHorizThumb", sliderBackground
                );
        }
    }
}
