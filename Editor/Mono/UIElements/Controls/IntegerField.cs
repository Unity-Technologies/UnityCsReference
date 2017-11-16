// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.Experimental.UIElements
{
    public class IntegerField : TextValueField<long>
    {
        internal static string allowedCharacters
        {
            get { return EditorGUI.s_AllowedCharactersForInt; }
        }

        public IntegerField()
            : this(kMaxLengthNone) {}

        public IntegerField(int maxLength)
            : base(maxLength)
        {
        }

        internal override bool AcceptCharacter(char c)
        {
            return c != 0 && allowedCharacters.IndexOf(c) != -1;
        }

        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, long startValue)
        {
            double sensitivity = NumericFieldDraggerUtility.CalculateIntDragSensitivity(startValue);
            float acceleration = NumericFieldDraggerUtility.Acceleration(speed == DeltaSpeed.Fast, speed == DeltaSpeed.Slow);
            long v = value;
            v += (long)Math.Round(NumericFieldDraggerUtility.NiceDelta(delta, acceleration) * sensitivity);
            SetValueAndNotify(v);
        }

        protected override string ValueToString(long v)
        {
            return v.ToString(formatString);
        }

        protected override long StringToValue(string str)
        {
            long v;
            EditorGUI.StringToLong(text, out v);
            return v;
        }
    }
}
