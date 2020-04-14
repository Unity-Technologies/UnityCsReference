// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    public class LongField : TextValueField<long>
    {
        // This property to alleviate the fact we have to cast all the time
        LongInput longInput => (LongInput)textInputBase;

        public new class UxmlFactory : UxmlFactory<LongField, UxmlTraits> {}
        public new class UxmlTraits : TextValueFieldTraits<long, UxmlLongAttributeDescription> {}

        protected override string ValueToString(long v)
        {
            return v.ToString(formatString, CultureInfo.InvariantCulture.NumberFormat);
        }

        protected override long StringToValue(string str)
        {
            long v;
            EditorGUI.StringToLong(str, out v);
            return v;
        }

        public new static readonly string ussClassName = "unity-long-field";
        public new static readonly string labelUssClassName = ussClassName + "__label";
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public LongField()
            : this((string)null) {}

        public LongField(int maxLength)
            : this(null, maxLength) {}

        public LongField(string label, int maxLength = kMaxLengthNone)
            : base(label, maxLength, new LongInput())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
            AddLabelDragger<long>();
        }

        internal override bool CanTryParse(string textString) => long.TryParse(textString, out _);

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
