// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Makes a text field for entering an unsigned integer. For more information, refer to [[wiki:UIE-uxml-element-UnsignedIntegerField|UXML element UnsignedIntegerField]].
    /// </summary>
    public class UnsignedIntegerField : TextValueField<uint>
    {
        // This property to alleviate the fact we have to cast all the time
        UnsignedIntegerInput integerInput => (UnsignedIntegerInput)textInputBase;

        /// <summary>
        /// Instantiates an <see cref="UnsignedIntegerField"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<UnsignedIntegerField, UxmlTraits> {}
        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="UnsignedIntegerField"/>.
        /// </summary>
        public new class UxmlTraits : TextValueFieldTraits<uint, UxmlUnsignedIntAttributeDescription> {}

        /// <summary>
        /// Converts the given unsigned integer to a string.
        /// </summary>
        /// <param name="v">The unsigned integer to be converted to string.</param>
        /// <returns>The unsigned integer as string.</returns>
        protected override string ValueToString(uint v)
        {
            return v.ToString(formatString, CultureInfo.InvariantCulture.NumberFormat);
        }

        /// <summary>
        /// Converts a string to an unsigned integer.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <returns>The unsigned integer parsed from the string.</returns>
        protected override uint StringToValue(string str)
        {
            var success = UINumericFieldsUtils.TryConvertStringToUInt(str, textInputBase.originalText, out var v, out var expression);
            expressionEvaluated?.Invoke(expression);
            return success ? v : rawValue;
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-unsigned-integer-field";
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
        public UnsignedIntegerField()
            : this((string)null) {}

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="maxLength">Maximum number of characters the field can take.</param>
        public UnsignedIntegerField(int maxLength)
            : this(null, maxLength) {}

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="maxLength">Maximum number of characters the field can take.</param>
        public UnsignedIntegerField(string label, int maxLength = kMaxValueFieldLength)
            : base(label, maxLength, new UnsignedIntegerInput())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
            AddLabelDragger<uint>();
        }

        internal override bool CanTryParse(string textString) => uint.TryParse(textString, out _);

        /// <summary>
        /// Applies the values of a 3D delta and a speed from an input device.
        /// </summary>
        /// <param name="delta">A vector used to compute the value change.</param>
        /// <param name="speed">A multiplier for the value change.</param>
        /// <param name="startValue">The start value.</param>
        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, uint startValue)
        {
            integerInput.ApplyInputDeviceDelta(delta, speed, startValue);
        }

        class UnsignedIntegerInput : TextValueInput
        {
            UnsignedIntegerField parentUnsignedIntegerField => (UnsignedIntegerField)parent;

            internal UnsignedIntegerInput()
            {
                formatString = UINumericFieldsUtils.k_IntFieldFormatString;
            }

            protected override string allowedCharacters => UINumericFieldsUtils.k_AllowedCharactersForInt;

            public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, uint startValue)
            {
                double sensitivity = NumericFieldDraggerUtility.CalculateIntDragSensitivity(startValue);
                var acceleration = NumericFieldDraggerUtility.Acceleration(speed == DeltaSpeed.Fast, speed == DeltaSpeed.Slow);
                long v = StringToValue(text);
                v += (long)Math.Round(NumericFieldDraggerUtility.NiceDelta(delta, acceleration) * sensitivity);

                if (parentUnsignedIntegerField.isDelayed)
                {
                    text = ValueToString(Mathf.ClampToUInt(v));
                }
                else
                {
                    parentUnsignedIntegerField.value = Mathf.ClampToUInt(v);
                }
            }

            protected override string ValueToString(uint v)
            {
                return v.ToString(formatString);
            }

            protected override uint StringToValue(string str)
            {
                return parentUnsignedIntegerField.StringToValue(str);
            }
        }
    }
}
