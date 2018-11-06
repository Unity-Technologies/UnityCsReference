// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    public class DoubleField : TextValueField<double>
    {
        // This property to alleviate the fact we have to cast all the time
        DoubleInput doubleInput => (DoubleInput)textInputBase;

        public new class UxmlFactory : UxmlFactory<DoubleField, UxmlTraits> {}
        public new class UxmlTraits : BaseFieldTraits<double, UxmlDoubleAttributeDescription> {}

        protected override string ValueToString(double v)
        {
            return v.ToString(formatString, CultureInfo.InvariantCulture.NumberFormat);
        }

        protected override double StringToValue(string str)
        {
            double v;
            EditorGUI.StringToDouble(str, out v);
            return v;
        }

        public new static readonly string ussClassName = "unity-double-field";

        public DoubleField() : this((string)null) {}

        public DoubleField(int maxLength)
            : this(null, maxLength) {}

        public DoubleField(string label, int maxLength = kMaxLengthNone)
            : base(label, maxLength, new DoubleInput() { name = "unity-text-input" })
        {
            AddToClassList(ussClassName);
            AddLabelDragger<double>();
        }

        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, double startValue)
        {
            doubleInput.ApplyInputDeviceDelta(delta, speed, startValue);
        }

        class DoubleInput : TextValueInput
        {
            DoubleField parentDoubleField => (DoubleField)parentField;

            internal DoubleInput()
            {
                formatString = EditorGUI.kDoubleFieldFormatString;
            }

            protected override string allowedCharacters
            {
                get { return EditorGUI.s_AllowedCharactersForFloat; }
            }

            public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, double startValue)
            {
                double sensitivity = NumericFieldDraggerUtility.CalculateFloatDragSensitivity(startValue);
                float acceleration = NumericFieldDraggerUtility.Acceleration(speed == DeltaSpeed.Fast, speed == DeltaSpeed.Slow);
                double v = parentDoubleField.value;
                v += NumericFieldDraggerUtility.NiceDelta(delta, acceleration) * sensitivity;
                v = MathUtils.RoundBasedOnMinimumDifference(v, sensitivity);
                parentDoubleField.value = v;
            }

            protected override string ValueToString(double v)
            {
                return v.ToString(formatString);
            }

            protected override double StringToValue(string str)
            {
                double v;
                EditorGUI.StringToDouble(str, out v);
                return v;
            }
        }
    }
}
