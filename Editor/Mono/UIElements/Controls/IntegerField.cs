// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    public class IntegerField : TextValueField<int>
    {
        public class IntegerFieldFactory : UxmlFactory<IntegerField, IntegerFieldUxmlTraits> {}

        public class IntegerFieldUxmlTraits : TextValueFieldUxmlTraits
        {
            UxmlIntAttributeDescription m_Value;

            public IntegerFieldUxmlTraits()
            {
                m_Value = new UxmlIntAttributeDescription { name = "value" };
            }

            public override IEnumerable<UxmlAttributeDescription> uxmlAttributesDescription
            {
                get
                {
                    foreach (var attr in base.uxmlAttributesDescription)
                    {
                        yield return attr;
                    }

                    yield return m_Value;
                }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                ((IntegerField)ve).value = m_Value.GetValueFromBag(bag);
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
            SetValueAndNotify(MathUtils.ClampToInt(v));
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
