// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Globalization;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Makes a text field for entering long integers. For more information, refer to [[wiki:UIE-uxml-element-LongField|UXML element LongField]].
    /// </summary>
    [MovedFrom(true, UpgradeConstants.EditorNamespace, UpgradeConstants.EditorAssembly)]
    public class LongField : TextValueField<long>
    {
        // This property to alleviate the fact we have to cast all the time
        LongInput longInput => (LongInput)textInputBase;

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : TextValueField<long>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                TextValueField<long>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new LongField();
        }

        /// <summary>
        /// Instantiates a <see cref="LongField"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlFactory : UxmlFactory<LongField, UxmlTraits> {}
        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="LongField"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
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
            var success = UINumericFieldsUtils.TryConvertStringToLong(str, textInputBase.originalText, out var v);
            return success ? v : rawValue;
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
        public LongField(string label, int maxLength = kMaxValueFieldLength)
            : base(label, maxLength, new LongInput())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
            AddLabelDragger<long>();
        }

        internal override bool CanTryParse(string textString) => long.TryParse(textString, out _);

        /// <summary>
        /// Applies the values of a 3D delta and a speed from an input device.
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
                formatString = UINumericFieldsUtils.k_IntFieldFormatString;
            }

            protected override string allowedCharacters
            {
                get { return UINumericFieldsUtils.k_AllowedCharactersForInt; }
            }

            public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, long startValue)
            {
                double sensitivity = NumericFieldDraggerUtility.CalculateIntDragSensitivity(startValue);
                var acceleration = NumericFieldDraggerUtility.Acceleration(speed == DeltaSpeed.Fast, speed == DeltaSpeed.Slow);
                var v = StringToValue(text);
                var niceDelta = (long)Math.Round(NumericFieldDraggerUtility.NiceDelta(delta, acceleration) * sensitivity);

                v = ClampMinMaxLongValue(niceDelta, v);

                if (parentLongField.isDelayed)
                {
                    text = ValueToString(v);
                }
                else
                {
                    parentLongField.value = v;
                }
            }

            private long ClampMinMaxLongValue(long niceDelta, long value)
            {
                var niceDeltaAbs = Math.Abs(niceDelta);
                if (niceDelta > 0)
                {
                    if (value > 0 && niceDeltaAbs > long.MaxValue - value)
                    {
                        return long.MaxValue;
                    }

                    return value + niceDelta;
                }

                if (value < 0 && value < long.MinValue + niceDeltaAbs)
                {
                    return long.MinValue;
                }

                return value - niceDeltaAbs;
            }

            protected override string ValueToString(long v)
            {
                return v.ToString(formatString);
            }

            protected override long StringToValue(string str)
            {
                UINumericFieldsUtils.TryConvertStringToLong(str, originalText, out var v);
                return v;
            }
        }
    }
}
