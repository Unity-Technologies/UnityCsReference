using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Makes a text field for entering long integers.
    /// </summary>
    public class LongField : TextValueField<long>
    {
        // This property to alleviate the fact we have to cast all the time
        LongInput longInput => (LongInput)textInputBase;

        /// <summary>
        /// Instantiates a <see cref="LongField"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<LongField, UxmlTraits> {}
        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="LongField"/>.
        /// </summary>
        public new class UxmlTraits : TextValueFieldTraits<long, UxmlLongAttributeDescription> {}

        /// <summary>
        /// Converts the given long integer to a string.
        /// </summary>
        /// <param name="v">The long integer to be converted to string.</param>
        /// <returns>The long integer as string.</returns>
        protected override string ValueToString(long v)
        {
            return v.ToString(formatString, CultureInfo.InvariantCulture.NumberFormat);
        }

        /// <summary>
        /// Converts a string to a long integer.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <returns>The long integer parsed from the string.</returns>
        protected override long StringToValue(string str)
        {
            long v;
            EditorGUI.StringToLong(str, out v);
            return v;
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-long-field";
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
        public LongField()
            : this((string)null) {}

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="maxLength">Maximum number of characters the field can take.</param>
        public LongField(int maxLength)
            : this(null, maxLength) {}

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="maxLength">Maximum number of characters the field can take.</param>
        public LongField(string label, int maxLength = kMaxLengthNone)
            : base(label, maxLength, new LongInput())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
            AddLabelDragger<long>();
        }

        internal override bool CanTryParse(string textString) => long.TryParse(textString, out _);

        /// <summary>
        /// Modify the value using a 3D delta and a speed, typically coming from an input device.
        /// </summary>
        /// <param name="delta">A vector used to compute the value change.</param>
        /// <param name="speed">A multiplier for the value change.</param>
        /// <param name="startValue">The start value.</param>
        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, long startValue)
        {
            longInput.ApplyInputDeviceDelta(delta, speed, startValue);
        }

        class LongInput : TextValueInput
        {
            LongField parentLongField => (LongField)parent;

            internal LongInput()
            {
                formatString = EditorGUI.kIntFieldFormatString;
            }

            protected override string allowedCharacters
            {
                get { return EditorGUI.s_AllowedCharactersForInt; }
            }

            public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, long startValue)
            {
                double sensitivity = NumericFieldDraggerUtility.CalculateIntDragSensitivity(startValue);
                float acceleration = NumericFieldDraggerUtility.Acceleration(speed == DeltaSpeed.Fast, speed == DeltaSpeed.Slow);
                long v = StringToValue(text);
                v += (long)Math.Round(NumericFieldDraggerUtility.NiceDelta(delta, acceleration) * sensitivity);
                if (parentLongField.isDelayed)
                {
                    text = ValueToString(v);
                }
                else
                {
                    parentLongField.value = v;
                }
            }

            protected override string ValueToString(long v)
            {
                return v.ToString(formatString);
            }

            protected override long StringToValue(string str)
            {
                long v;
                EditorGUI.StringToLong(str, out v);
                return v;
            }
        }
    }
}
