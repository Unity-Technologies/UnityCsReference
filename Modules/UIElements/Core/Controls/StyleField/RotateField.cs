// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Makes a field for entering Rotate.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    internal class RotateField : BaseField<Rotate>, IValueField<Rotate>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BaseField<Rotate>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseField<Rotate>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new RotateField();
        }

        public static readonly string styleFieldUssClassName = "unity-style-field";

        AngleField m_AngleField;
        Vector3Field m_AxisField;

        public AngleField angleField => m_AngleField;
        public Vector3Field axisField => m_AxisField;

        private BaseFieldMouseDragger m_Dragger;

        /// <summary>
        /// Constructor.
        /// </summary>
        public RotateField() : this(null) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="label">The Label text.</param>
        public RotateField(string label)
            : base(label, null)
        {
            m_AngleField = new AngleField();
            m_AxisField = new Vector3Field();

            visualInput.Add(m_AngleField);
            visualInput.Add(m_AxisField);

            m_AngleField.AddToClassList(styleFieldUssClassName);

            m_AngleField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue != value.angle)
                {
                    var newVal = value;
                    newVal.angle = e.newValue;
                    value = newVal;
                }
            });

            m_AxisField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue != value.axis)
                {
                    var newVal = value;
                    newVal.axis = e.newValue;
                    value = newVal;
                }
            });

            AddLabelDragger();
            SetValueWithoutNotify(Rotate.Initial());
        }

        public override void SetValueWithoutNotify(Rotate rotate)
        {
            base.SetValueWithoutNotify(rotate);
            m_AngleField.SetValueWithoutNotify(value.angle);
            m_AxisField.SetValueWithoutNotify(value.axis);
        }

        protected void AddLabelDragger()
        {
            m_Dragger = new FieldMouseDragger<Rotate>(this);
            EnableLabelDragger(!m_AngleField.isReadOnly);
        }

        void EnableLabelDragger(bool enable)
        {
            if (m_Dragger != null)
            {
                m_Dragger.SetDragZone(enable ? labelElement : null);

                labelElement.EnableInClassList(labelDraggerVariantUssClassName, enable);
            }
        }


        public void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, Rotate startValue)
        {
            m_AngleField.ApplyInputDeviceDelta(delta, speed, startValue.angle);
        }

        public void StartDragging()
        {
            m_AngleField.StartDragging();
        }

        public void StopDragging()
        {
            m_AngleField.StopDragging();
        }
    }
}
