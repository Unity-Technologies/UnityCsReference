// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    public class IntegerField : TextValueField<int>
    {
        public new class UxmlFactory : UxmlFactory<IntegerField, UxmlTraits> {}

        public new class UxmlTraits : TextValueField<int>.UxmlTraits
        {
            UxmlIntAttributeDescription m_Value = new UxmlIntAttributeDescription { name = "value" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                ((IntegerField)ve).SetValueWithoutNotify(m_Value.GetValueFromBag(bag, cc));
            }
        }

        public IntegerField()
            : this(kMaxLengthNone) {}

        public IntegerField(int maxLength)
            : base(maxLength)
        {
            formatString = EditorGUI.kIntFieldFormatString;
        }

        protected override string allowedCharacters
        {
            get { return EditorGUI.s_AllowedCharactersForInt; }
        }

        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, int startValue)
        {
            double sensitivity = NumericFieldDraggerUtility.CalculateIntDragSensitivity(startValue);
            float acceleration = NumericFieldDraggerUtility.Acceleration(speed == DeltaSpeed.Fast, speed == DeltaSpeed.Slow);
            long v = value;
            v += (long)Math.Round(NumericFieldDraggerUtility.NiceDelta(delta, acceleration) * sensitivity);
            value = MathUtils.ClampToInt(v);
        }

        protected override string ValueToString(int v)
        {
            return v.ToString(formatString);
        }

        protected override int StringToValue(string str)
        {
            long v;
            EditorGUI.StringToLong(text, out v);
            return MathUtils.ClampToInt(v);
        }
    }
}
