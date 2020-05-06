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

        public StyleLengthField(string label, int maxLength = kMaxLengthNone)
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
            if (string.IsNullOrEmpty(str))
                return defaultValue;

            str = str.ToLower();

            StyleLength result = defaultValue;
            if (char.IsLetter(str[0]))
            {
                if (str == "auto")
                    result = new StyleLength(StyleKeyword.Auto);
                else if (str == "none")
                    result = new StyleLength(StyleKeyword.None);
            }
            else
            {
                Length length = defaultValue.value;
                float value = length.value;
                LengthUnit unit = length.unit;

                // Find unit index
                int digitEndIndex = 0;
                int unitIndex = -1;
                for (int i = 0; i < str.Length; i++)
                {
                    var c = str[i];
                    if (char.IsLetter(c) || c == '%')
                    {
                        unitIndex = i;
                        break;
                    }

                    ++digitEndIndex;
                }

                var floatStr = str.Substring(0, digitEndIndex);
                var unitStr = string.Empty;
                if (unitIndex > 0)
                    unitStr = str.Substring(unitIndex, str.Length - unitIndex).ToLower();

                float v;
                if (float.TryParse(floatStr, out v))
                    value = v;

                switch (unitStr)
                {
                    case "px":
                        unit = LengthUnit.Pixel;
                        break;
                    case "%":
                        unit = LengthUnit.Percent;
                        break;
                    default:
                        break;
                }
                result = new Length(value, unit);
            }

            return result;
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
