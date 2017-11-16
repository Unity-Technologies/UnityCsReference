// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.Experimental.UIElements
{
    public class DoubleField : TextValueField<double>
    {
        internal static string allowedCharacters
        {
            get { return EditorGUI.s_AllowedCharactersForFloat; }
        }

        public DoubleField()
            : this(kMaxLengthNone) {}

        public DoubleField(int maxLength)
            : base(maxLength) {}

        internal override bool AcceptCharacter(char c)
        {
            return c != 0 && allowedCharacters.IndexOf(c) != -1;
        }

        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, double startValue)
        {
            double sensitivity = NumericFieldDraggerUtility.CalculateFloatDragSensitivity(startValue);
            float acceleration = NumericFieldDraggerUtility.Acceleration(speed == DeltaSpeed.Fast, speed == DeltaSpeed.Slow);
            double v = value;
            v += NumericFieldDraggerUtility.NiceDelta(delta, acceleration) * sensitivity;
            v = MathUtils.RoundBasedOnMinimumDifference(v, sensitivity);
            SetValueAndNotify(v);
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
