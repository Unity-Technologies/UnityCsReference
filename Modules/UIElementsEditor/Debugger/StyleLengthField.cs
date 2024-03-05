// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Debugger
{
    internal class StyleLengthField : TextValueField<StyleLength>
    {
        // This property to alleviate the fact we have to cast all the time
        LengthInput lengthInput => (LengthInput)textInputBase;

        public StyleLengthField() : this((string)null) {}

        public StyleLengthField(int maxLength)
            : this(null, maxLength) {}

        public StyleLengthField(string label, int maxLength = kMaxValueFieldLength)
            : base(label, maxLength, new LengthInput())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
            AddLabelDragger<StyleLength>();
        }

        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, StyleLength startValue)
        {
            lengthInput.ApplyInputDeviceDelta(delta, speed, startValue);
        }

        protected override StyleLength StringToValue(string str)
        {
            return ParseString(str, value);
        }

        protected override string ValueToString(StyleLength value)
        {
            return value.ToString().ToLower();
        }

        private static StyleLength ParseString(string str, StyleLength defaultValue)
        {
            return Length.ParseString(str, defaultValue.ToLength());
        }

        class LengthInput : TextValueInput
        {
            StyleLengthField parentLengthField => (StyleLengthField)parent;

            protected override string allowedCharacters
            {
                get { return "0123456789autonepx%.,-+"; }
            }

            public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, StyleLength startValue)
            {
                if (startValue.keyword != StyleKeyword.Undefined)
                    startValue = new StyleLength();

                double sensitivity = NumericFieldDraggerUtility.CalculateIntDragSensitivity((long)startValue.value.value);
                float acceleration = NumericFieldDraggerUtility.Acceleration(speed == DeltaSpeed.Fast, speed == DeltaSpeed.Slow);
                long v = (long)StringToValue(text).value.value;
                v += (long)Math.Round(NumericFieldDraggerUtility.NiceDelta(delta, acceleration) * sensitivity);
                if (parentLengthField.isDelayed)
                {
                    text = ValueToString(MathUtils.ClampToInt(v));
                }
                else
                {
                    Length l = new Length(MathUtils.ClampToInt(v), parentLengthField.value.value.unit);
                    parentLengthField.value = new StyleLength(l);
                }
            }

            protected override string ValueToString(StyleLength v)
            {
                return v.ToString().ToLower();
            }

            protected override StyleLength StringToValue(string str)
            {
                return ParseString(str, parentLengthField.value);
            }
        }
    }
}
