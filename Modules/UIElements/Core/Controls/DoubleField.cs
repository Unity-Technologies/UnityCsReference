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
    /// Makes a text field for entering doubles.
    /// </summary>
    [MovedFrom(true, UpgradeConstants.EditorNamespace, UpgradeConstants.EditorAssembly)]
    public class DoubleField : TextValueField<double>
    {
        // This property to alleviate the fact we have to cast all the time
        DoubleInput doubleInput => (DoubleInput)textInputBase;

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : TextValueField<double>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                TextValueField<double>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new DoubleField();
        }

        /// <summary>
        /// Instantiates a <see cref="DoubleField"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlFactory : UxmlFactory<DoubleField, UxmlTraits> {}
        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="DoubleField"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlTraits : TextValueFieldTraits<double, UxmlDoubleAttributeDescription> {}

        /// <summary>
        /// Converts the given double to a string.
        /// </summary>
        /// <param name="v">The double to be converted to string.</param>
        /// <returns>The double as string.</returns>
        protected override string ValueToString(double v)
        {
            return v.ToString(formatString, CultureInfo.InvariantCulture.NumberFormat);
        }

        /// <summary>
        /// Converts a string to a double.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <returns>The double parsed from the string.</returns>
        protected override double StringToValue(string str)
        {
            var success = UINumericFieldsUtils.TryConvertStringToDouble(str, textInputBase.originalText, out var v, out var expression);
            expressionEvaluated?.Invoke(expression);
            return success ? v : rawValue;
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-double-field";
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
        public DoubleField() : this((string)null) {}

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="maxLength">Maximum number of characters the field can take.</param>
        public DoubleField(int maxLength)
            : this(null, maxLength) {}

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="maxLength">Maximum number of characters the field can take.</param>
        public DoubleField(string label, int maxLength = kMaxValueFieldLength)
            : base(label, maxLength, new DoubleInput())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
            AddLabelDragger<double>();
        }

        internal override bool CanTryParse(string textString) => double.TryParse(textString, out _);

        /// <summary>
        /// Applies the values of a 3D delta and a speed from an input device.
        /// </summary>
        /// <param name="delta">A vector used to compute the value change.</param>
        /// <param name="speed">A multiplier for the value change.</param>
        /// <param name="startValue">The start value.</param>
        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, double startValue)
        {
            doubleInput.ApplyInputDeviceDelta(delta, speed, startValue);
        }

        class DoubleInput : TextValueInput
        {
            DoubleField parentDoubleField => (DoubleField)parent;

            internal DoubleInput()
            {
                formatString = UINumericFieldsUtils.k_DoubleFieldFormatString;
            }

            protected override string allowedCharacters => UINumericFieldsUtils.k_AllowedCharactersForFloat;

            public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, double startValue)
            {
                double sensitivity = NumericFieldDraggerUtility.CalculateFloatDragSensitivity(startValue);
                float acceleration = NumericFieldDraggerUtility.Acceleration(speed == DeltaSpeed.Fast, speed == DeltaSpeed.Slow);
                double v = StringToValue(text);
                v += NumericFieldDraggerUtility.NiceDelta(delta, acceleration) * sensitivity;
                v = Mathf.RoundBasedOnMinimumDifference(v, sensitivity);
                if (parentDoubleField.isDelayed)
                {
                    text = ValueToString(v);
                }
                else
                {
                    parentDoubleField.value = v;
                }
            }

            protected override string ValueToString(double v)
            {
                return v.ToString(formatString);
            }

            protected override double StringToValue(string str)
            {
                return parentDoubleField.StringToValue(str);
            }
        }
    }
}
