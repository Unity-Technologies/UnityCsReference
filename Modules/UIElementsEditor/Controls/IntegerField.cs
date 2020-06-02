// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    public class IntegerField : TextValueField<int>
    {
        // This property to alleviate the fact we have to cast all the time
        IntegerInput integerInput => (IntegerInput)textInputBase;

        public new class UxmlFactory : UxmlFactory<IntegerField, UxmlTraits> {}
        public new class UxmlTraits : TextValueFieldTraits<int, UxmlIntAttributeDescription> {}

        protected override string ValueToString(int v)
        {
            return v.ToString(formatString, CultureInfo.InvariantCulture.NumberFormat);
        }

        protected override int StringToValue(string str)
        {
            long v;
            EditorGUI.StringToLong(str, out v);
            return MathUtils.ClampToInt(v);
        }

        public new static readonly string ussClassName = "unity-integer-field";
        public new static readonly string labelUssClassName = ussClassName + "__label";
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public IntegerField()
            : this((string)null) {}

        public IntegerField(int maxLength)
            : this(null, maxLength) {}

        public IntegerField(string label, int maxLength = kMaxLengthNone)
            : base(label, maxLength, new IntegerInput())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
            AddLabelDragger<int>();
        }

        internal override bool CanTryParse(string textString) => int.TryParse(textString, out _);

        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, int startValue)
        {
            integerInput.ApplyInputDeviceDelta(delta, speed, startValue);
        }

        class IntegerInput : TextValueInput
        {
            IntegerField parentIntegerField => (IntegerField)parent;

            internal IntegerInput()
            {
                formatString = EditorGUI.kIntFieldFormatString;
            }

            protected override string allowedCharacters => EditorGUI.s_AllowedCharactersForInt;

            public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, int startValue)
            {
                double sensitivity = NumericFieldDraggerUtility.CalculateIntDragSensitivity(startValue);
                float acceleration = NumericFieldDraggerUtility.Acceleration(speed == DeltaSpeed.Fast, speed == DeltaSpeed.Slow);
                long v = StringToValue(text);
                v += (long)Math.Round(NumericFieldDraggerUtility.NiceDelta(delta, acceleration) * sensitivity);
                if (parentIntegerField.isDelayed)
                {
                    text = ValueToString(MathUtils.ClampToInt(v));
                }
                else
                {
                    parentIntegerField.value = MathUtils.ClampToInt(v);
                }
            }

            protected override string ValueToString(int v)
            {
                return v.ToString(formatString);
            }

            protected override int StringToValue(string str)
            {
                long v;
                EditorGUI.StringToLong(str, out v);
                return MathUtils.ClampToInt(v);
            }
        }
    }
}
