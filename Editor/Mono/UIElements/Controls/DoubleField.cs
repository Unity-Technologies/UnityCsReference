// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    public class DoubleField : TextValueField<double>
    {
        public class DoubleFieldFactory : UxmlFactory<DoubleField, DoubleFieldUxmlTraits> {}

        public class DoubleFieldUxmlTraits : TextValueFieldUxmlTraits
        {
            UxmlDoubleAttributeDescription m_Value;

            public DoubleFieldUxmlTraits()
            {
                m_Value = new UxmlDoubleAttributeDescription { name = "value" };
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

                ((DoubleField)ve).value = m_Value.GetValueFromBag(bag);
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
