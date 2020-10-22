using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Makes a text field for entering a float.
    /// </summary>
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
            double v;
            EditorGUI.StringToDouble(str, out v);
            return MathUtils.ClampToFloat(v);
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
        public FloatField(string label, int maxLength = kMaxLengthNone)
            : base(label, maxLength, new FloatInput())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
            AddLabelDragger<float>();
        }

        internal override bool CanTryParse(string textString) => float.TryParse(textString, out _);

        /// <summary>
        /// Modify the value using a 3D delta and a speed, typically coming from an input device.
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
                formatString = EditorGUI.kFloatFieldFormatString;
            }

            protected override string allowedCharacters => EditorGUI.s_AllowedCharactersForFloat;

            public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, float startValue)
            {
                double sensitivity = NumericFieldDraggerUtility.CalculateFloatDragSensitivity(startValue);
                float acceleration = NumericFieldDraggerUtility.Acceleration(speed == DeltaSpeed.Fast, speed == DeltaSpeed.Slow);
                double v = StringToValue(text);
                v += NumericFieldDraggerUtility.NiceDelta(delta, acceleration) * sensitivity;
                v = MathUtils.RoundBasedOnMinimumDifference(v, sensitivity);
                if (parentFloatField.isDelayed)
                {
                    text = ValueToString(MathUtils.ClampToFloat(v));
                }
                else
                {
                    parentFloatField.value = MathUtils.ClampToFloat(v);
                }
            }

            protected override string ValueToString(float v)
            {
                return v.ToString(formatString);
            }

            protected override float StringToValue(string str)
            {
                double v;
                EditorGUI.StringToDouble(str, out v);
                return MathUtils.ClampToFloat(v);
            }
        }
    }
}
