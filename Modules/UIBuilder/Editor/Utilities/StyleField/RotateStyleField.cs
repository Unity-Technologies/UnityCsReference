// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using JetBrains.Annotations;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEngine.UIElements.StyleSheets;
using UnityEditor.UIElements;

namespace Unity.UI.Builder
{
    [UsedImplicitly]
    class RotateStyleField : BaseField<Rotate>
    {
        public class BuilderRotateConverter : UxmlAttributeConverter<Rotate>
        {
            public override Rotate FromString(string value) => throw new NotImplementedException();
            public override string ToString(Rotate value) => throw new NotImplementedException();
        }

        [Serializable]
        public new class UxmlSerializedData : BaseField<Rotate>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseField<Rotate>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new RotateStyleField();
        }

        static readonly string s_FieldClassName = "unity-rotate-style-field";
        static readonly string s_UxmlPath = BuilderConstants.UtilitiesPath + "/StyleField/RotateStyleField.uxml";
        static readonly string s_UssPath = BuilderConstants.UtilitiesPath + "/StyleField/RotateStyleField.uss";
        public static readonly string s_AngleFieldName = "angle-field";
        public static readonly string s_AxisFieldName = "axis-field";

        AngleStyleField m_AngleField;
        Vector3Field m_AxisField;

        public RotateStyleField() : this(null) { }

        public RotateStyleField(string label) : base(label)
        {
            AddToClassList(s_FieldClassName);

            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(s_UssPath));

            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(s_UxmlPath);

            template.CloneTree(this);

            m_AngleField = this.Q<AngleStyleField>(s_AngleFieldName);
            m_AxisField = this.Q<Vector3Field>(s_AxisFieldName);

            m_AngleField.RegisterValueChangedCallback(e =>
            {
                UpdateRotateField();
                e.StopPropagation();
            });

            m_AxisField.RegisterValueChangedCallback(e =>
            {
                UpdateRotateField();
                e.StopPropagation();
            });

            value = new Rotate()
            {
                angle = Angle.Degrees(0),
                axis = Vector3.forward
            };
        }

        public override void SetValueWithoutNotify(Rotate newValue)
        {
            base.SetValueWithoutNotify(newValue);
            RefreshSubFields();
        }

        void RefreshSubFields()
        {
            m_AngleField.SetValueWithoutNotify(value.angle.ToString());
            m_AxisField.SetValueWithoutNotify(value.axis);

            float step = 1;

            switch (value.angle.unit)
            {
                case AngleUnit.Turn:
                    step = 0.05f;
                    break;
                case AngleUnit.Radian:
                    step = Mathf.Deg2Rad;
                    break;
                default:
                    break;
            }

            m_AngleField.dragStep = step;
        }

        void UpdateRotateField()
        {
            // Rebuild value from sub fields
            value = new Rotate()
            {
                angle = new Dimension { value = m_AngleField.length, unit = m_AngleField.unit }.ToAngle(),
                axis = m_AxisField.value
            };
        }
    }
}
