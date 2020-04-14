// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    public class FloatField : TextValueField<float>
    {
        // This property to alleviate the fact we have to cast all the time
        FloatInput floatInput => (FloatInput)textInputBase;

        public new class UxmlFactory : UxmlFactory<FloatField, UxmlTraits> {}
        public new class UxmlTraits : TextValueFieldTraits<float, UxmlFloatAttributeDescription> {}

        protected override string ValueToString(float v)
        {
            return v.ToString(formatString, CultureInfo.InvariantCulture.NumberFormat);
        }

        protected override float StringToValue(string str)
        {
            double v;
            EditorGUI.StringToDouble(str, out v);
            return MathUtils.ClampToFloat(v);
        }

        public new static readonly string ussClassName = "unity-float-field";
        public new static readonly string labelUssClassName = ussClassName + "__label";
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public FloatField() : this((string)null) {}

        public FloatField(int maxLength)
            : this(null, maxLength) {}

        public FloatField(string label, int maxLength = kMaxLengthNone)
            : base(label, maxLength, new FloatInput())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
            AddLabelDragger<float>();
        }

        internal override bool CanTryParse(string textString) => float.TryParse(textString, out _);

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
