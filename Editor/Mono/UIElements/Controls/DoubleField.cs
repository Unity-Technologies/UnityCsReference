// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    public class DoubleField : TextValueField<double>
    {
        public new class UxmlFactory : UxmlFactory<DoubleField, UxmlTraits> {}

        public new class UxmlTraits : TextValueField<double>.UxmlTraits
        {
            UxmlDoubleAttributeDescription m_Value = new UxmlDoubleAttributeDescription { name = "value" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                ((DoubleField)ve).SetValueWithoutNotify(m_Value.GetValueFromBag(bag, cc));
            }
        }

        public DoubleField()
            : this(kMaxLengthNone) {}

        public DoubleField(int maxLength)
            : base(maxLength)
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
            double v = value;
            v += NumericFieldDraggerUtility.NiceDelta(delta, acceleration) * sensitivity;
            v = MathUtils.RoundBasedOnMinimumDifference(v, sensitivity);
            value = v;
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
