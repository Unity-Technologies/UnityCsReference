// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Makes a text field for entering unsigned long integers. For more information, refer to [[wiki:UIE-uxml-element-UnsignedLongField|UXML element UnsignedLongField]].
    /// </summary>
    public class UnsignedLongField : TextValueField<ulong>
    {
        // This property to alleviate the fact we have to cast all the time
        UnsignedLongInput unsignedLongInput => (UnsignedLongInput)textInputBase;

        /// <summary>
        /// Instantiates a <see cref="UnsignedLongField"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<UnsignedLongField, UxmlTraits> {}
        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="UnsignedLongField"/>.
        /// </summary>
        public new class UxmlTraits : TextValueFieldTraits<ulong, UxmlUnsignedLongAttributeDescription> {}

        /// <summary>
        /// Converts the given unsigned long integer to a string.
        /// </summary>
        /// <param name="v">The unsigned long integer to be converted to string.</param>
        /// <returns>The ulong integer as string.</returns>
        protected override string ValueToString(ulong v)
        {
            return v.ToString(formatString, CultureInfo.InvariantCulture.NumberFormat);
        }

        /// <summary>
        /// Converts a string to a unsigned long integer.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <returns>The unsigned long integer parsed from the string.</returns>
        protected override ulong StringToValue(string str)
        {
            var success = UINumericFieldsUtils.TryConvertStringToULong(str, textInputBase.originalText, out var v, out var expression);
            expressionEvaluated?.Invoke(expression);
            return success ? v : rawValue;
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-unsigned-long-field";
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
        public UnsignedLongField()
            : this((string)null) {}

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="maxLength">Maximum number of characters the field can take.</param>
        public UnsignedLongField(int maxLength)
            : this(null, maxLength) {}

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="maxLength">Maximum number of characters the field can take.</param>
        public UnsignedLongField(string label, int maxLength = kMaxValueFieldLength)
            : base(label, maxLength, new UnsignedLongInput())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
            AddLabelDragger<ulong>();
        }

        internal override bool CanTryParse(string textString) => ulong.TryParse(textString, out _);

        /// <summary>
        /// Applies the values of a 3D delta and a speed from an input device.
        /// </summary>
        /// <param name="delta">A vector used to compute the value change.</param>
        /// <param name="speed">A multiplier for the value change.</param>
        /// <param name="startValue">The start value.</param>
        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, ulong startValue)
        {
            unsignedLongInput.ApplyInputDeviceDelta(delta, speed, startValue);
        }

        class UnsignedLongInput : TextValueInput
        {
            UnsignedLongField parentUnsignedLongField => (UnsignedLongField)parent;

            internal UnsignedLongInput()
            {
                formatString = UINumericFieldsUtils.k_IntFieldFormatString;
            }

            protected override string allowedCharacters => UINumericFieldsUtils.k_AllowedCharactersForInt;

            public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, ulong startValue)
            {
                double sensitivity = NumericFieldDraggerUtility.CalculateIntDragSensitivity(startValue);
                var acceleration = NumericFieldDraggerUtility.Acceleration(speed == DeltaSpeed.Fast, speed == DeltaSpeed.Slow);
                var v = StringToValue(text);
                var niceDelta = (long)Math.Round(NumericFieldDraggerUtility.NiceDelta(delta, acceleration) * sensitivity);

                v = ClampToMinMaxULongValue(niceDelta, v);

                if (parentUnsignedLongField.isDelayed)
                {
                    text = ValueToString(v);
                }
                else
                {
                    parentUnsignedLongField.value = v;
                }
            }

            private ulong ClampToMinMaxULongValue(long niceDelta, ulong value)
            {
                var niceDeltaAbs = (ulong)Math.Abs(niceDelta);

                if (niceDelta > 0)
                {
                    if (niceDeltaAbs > ulong.MaxValue - value)
                    {
                        return ulong.MaxValue;
                    }

                    return value + niceDeltaAbs;
                }

                if (niceDeltaAbs > value)
                {
                    return ulong.MinValue;
                }

                return value - niceDeltaAbs;
            }

            protected override string ValueToString(ulong v)
            {
                return v.ToString(formatString);
            }

            protected override ulong StringToValue(string str)
            {
                return parentUnsignedLongField.StringToValue(str);
            }
        }
    }
}
