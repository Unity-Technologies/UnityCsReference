// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Globalization;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Makes a text field for entering a float. For more information, refer to [[wiki:UIE-uxml-element-FloatField|UXML element FloatField]].
    /// </summary>
    [MovedFrom(true, UpgradeConstants.EditorNamespace, UpgradeConstants.EditorAssembly)]
    public class FloatField : TextValueField<float>
    {
        // This property to alleviate the fact we have to cast all the time
        FloatInput floatInput => (FloatInput)textInputBase;

        /// <summary>
        /// Instantiates a <see cref="FloatField"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<FloatField, UxmlTraits> {}
        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="FloatField"/>.
        /// </summary>
        public new class UxmlTraits : TextValueFieldTraits<float, UxmlFloatAttributeDescription> {}

        /// <summary>
        /// Converts the given float to a string.
        /// </summary>
        /// <param name="v">The float to be converted to string.</param>
        /// <returns>The float as string.</returns>
        protected override string ValueToString(float v)
        {
            return v.ToString(formatString, CultureInfo.InvariantCulture.NumberFormat);
        }

        /// <summary>
        /// Converts a string to a float.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <returns>The float parsed from the string.</returns>
        protected override float StringToValue(string str)
        {
            var success = UINumericFieldsUtils.TryConvertStringToFloat(str, textInputBase.originalText, out var v, out var expression);
            expressionEvaluated?.Invoke(expression);
            return success ? v : rawValue;
        }

        internal override void UpdateValueFromText()
        {
            // When working with floats, the text representation is generated using the format string "G7",
            // which applies the general format with a precision of 7 significant digits.
            // Due to the limitations of floating-point precision, converting the float to a string
            // and then parsing it back may not yield the exact original value.
            // Therefore, before converting the string back to a float, we compare the current generated
            // text representation against the current text value to determine if the value has changed.
            // This helps avoid losing precision and incorrect change events when the value was not actually changed. (UUM-100527)
            // For example, the float value 112.038414 is converted to the string "112.0384" using "G7",
            // parsing this string back into a float results in a slightly different value, as the original
            // precision is lost during the conversion process.
            // So if the text is "112.0384" then we know nothing changed and we can avoid replacing 112.038414 with 112.0484.
            var currentValueAsString = ValueToString(rawValue);
            if (currentValueAsString != text)
            {
                base.UpdateValueFromText();
            }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-float-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        /// <summary>
        /// Constructor.
        /// </summary>
        public FloatField() : this((string)null) {}

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="maxLength">Maximum number of characters the field can take.</param>
        public FloatField(int maxLength)
            : this(null, maxLength) {}

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="maxLength">Maximum number of characters the field can take.</param>
        public FloatField(string label, int maxLength = kMaxValueFieldLength)
            : base(label, maxLength, new FloatInput())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
            AddLabelDragger<float>();
        }

        internal override bool CanTryParse(string textString) => float.TryParse(textString, out _);

        /// <summary>
        /// Applies the values of a 3D delta and a speed from an input device.
        /// </summary>
        /// <param name="delta">A vector used to compute the value change.</param>
        /// <param name="speed">A multiplier for the value change.</param>
        /// <param name="startValue">The start value.</param>
        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, float startValue)
        {
            floatInput.ApplyInputDeviceDelta(delta, speed, startValue);
        }

        class FloatInput : TextValueInput
        {
            FloatField parentFloatField => (FloatField)parent;

            internal FloatInput()
            {
                formatString = UINumericFieldsUtils.k_FloatFieldFormatString;
            }

            protected override string allowedCharacters => UINumericFieldsUtils.k_AllowedCharactersForFloat;

            public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, float startValue)
            {
                double sensitivity = NumericFieldDraggerUtility.CalculateFloatDragSensitivity(startValue);
                float acceleration = NumericFieldDraggerUtility.Acceleration(speed == DeltaSpeed.Fast, speed == DeltaSpeed.Slow);
                double v = StringToValue(text);
                v += NumericFieldDraggerUtility.NiceDelta(delta, acceleration) * sensitivity;
                v = Mathf.RoundBasedOnMinimumDifference(v, sensitivity);
                if (parentFloatField.isDelayed)
                {
                    text = ValueToString(Mathf.ClampToFloat(v));
                }
                else
                {
                    parentFloatField.value = Mathf.ClampToFloat(v);
                }
            }

            protected override string ValueToString(float v)
            {
                return v.ToString(formatString);
            }

            protected override float StringToValue(string str)
            {
                return parentFloatField.StringToValue(str);
            }
        }
    }
}
