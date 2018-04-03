// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    public class FloatField : TextValueField<float>
    {
        public class FloatFieldFactory : UxmlFactory<FloatField, FloatFieldUxmlTraits> {}

        public class FloatFieldUxmlTraits : TextValueFieldUxmlTraits
        {
            UxmlFloatAttributeDescription m_Value;

            public FloatFieldUxmlTraits()
            {
                m_Value = new UxmlFloatAttributeDescription { name = "value" };
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

                ((FloatField)ve).value = m_Value.GetValueFromBag(bag);
            }
        }

        public FloatField()
            : this(kMaxLengthNone) {}

        public FloatField(int maxLength)
            : base(maxLength)
        {
            formatString = EditorGUI.kFloatFieldFormatString;
        }

        protected override string allowedCharacters
        {
            get { return EditorGUI.s_AllowedCharactersForFloat; }
        }

        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, float startValue)
        {
            double sensitivity = NumericFieldDraggerUtility.CalculateFloatDragSensitivity(startValue);
            float acceleration = NumericFieldDraggerUtility.Acceleration(speed == DeltaSpeed.Fast, speed == DeltaSpeed.Slow);
            double v = value;
            v += NumericFieldDraggerUtility.NiceDelta(delta, acceleration) * sensitivity;
            v = MathUtils.RoundBasedOnMinimumDifference(v, sensitivity);
            SetValueAndNotify(MathUtils.ClampToFloat(v));
        }

        protected override string ValueToString(float v)
        {
            return v.ToString(formatString);
        }

        protected override float StringToValue(string str)
        {
            double v;
            EditorGUI.StringToDouble(str, out v);
            return MathUtils.ClampToFloat(v);
        }
    }
}
