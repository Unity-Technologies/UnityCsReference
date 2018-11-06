// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    public class LongField : TextValueField<long>
    {
        public new class UxmlFactory : UxmlFactory<LongField, UxmlTraits> {}

        public new class UxmlTraits : TextValueField<long>.UxmlTraits
        {
            UxmlLongAttributeDescription m_Value = new UxmlLongAttributeDescription { name = "value" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                ((LongField)ve).SetValueWithoutNotify(m_Value.GetValueFromBag(bag, cc));
            }
        }

        public LongField()
            : this(kMaxLengthNone) {}

        public LongField(int maxLength)
            : base(maxLength)
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
            long v = value;
            v += (long)Math.Round(NumericFieldDraggerUtility.NiceDelta(delta, acceleration) * sensitivity);
            value = v;
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
