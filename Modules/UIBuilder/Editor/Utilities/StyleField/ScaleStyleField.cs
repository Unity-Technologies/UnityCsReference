// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using JetBrains.Annotations;
using System;
using System.Diagnostics;
using System.Globalization;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    [UsedImplicitly]
    class ScaleStyleField : BaseField<Scale>
    {
        public class BuilderScaleConverter : UxmlAttributeConverter<Scale>
        {
            public override Scale FromString(string value) => throw new NotImplementedException();
            public override string ToString(Scale value) => throw new NotImplementedException();
        }

        [Serializable]
        public new class UxmlSerializedData : BaseField<Scale>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseField<Scale>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new ScaleStyleField();
        }

        static readonly string s_FieldClassName = "unity-scale-style-field";
        static readonly string s_UxmlPath = BuilderConstants.UtilitiesPath + "/StyleField/ScaleStyleField.uxml";
        static readonly string s_VisualInputName = "unity-visual-input";
        public static readonly string s_ScaleXFieldName = "x-field";
        public static readonly string s_ScaleYFieldName = "y-field";
        public static readonly string s_ScaleZFieldName = "z-field";

        DimensionStyleField m_ScaleXField;
        DimensionStyleField m_ScaleYField;
        DimensionStyleField m_ScaleZField;

        public ScaleStyleField() : this(null) { }

        public ScaleStyleField(string label) : base(label)
        {
            AddToClassList(BuilderConstants.InspectorContainerClassName);
            AddToClassList(s_FieldClassName);

            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(s_UxmlPath);

            template.CloneTree(this);

            visualInput = this.Q(s_VisualInputName);

            m_ScaleXField = this.Q<DimensionStyleField>(s_ScaleXFieldName);
            m_ScaleYField = this.Q<DimensionStyleField>(s_ScaleYFieldName);
            m_ScaleZField = this.Q<DimensionStyleField>(s_ScaleZFieldName);

            m_ScaleXField.RegisterValueChangedCallback(e =>
            {
                UpdateScaleField();
                e.StopPropagation();
            });
            m_ScaleYField.RegisterValueChangedCallback(e =>
            {
                UpdateScaleField();
                e.StopPropagation();
            });
            m_ScaleZField.RegisterValueChangedCallback(e =>
            {
                UpdateScaleField();
                e.StopPropagation();
            });

            value = Vector3.one;
        }

        public override void SetValueWithoutNotify(Scale newValue)
        {
            base.SetValueWithoutNotify(newValue);
            RefreshSubFields();
        }

        void RefreshSubFields()
        {
            m_ScaleXField.SetValueWithoutNotify(value.value.x.ToString(CultureInfo.InvariantCulture.NumberFormat));
            m_ScaleYField.SetValueWithoutNotify(value.value.y.ToString(CultureInfo.InvariantCulture.NumberFormat));
            m_ScaleZField.SetValueWithoutNotify(value.value.z.ToString(CultureInfo.InvariantCulture.NumberFormat));
        }

        void UpdateScaleField()
        {
            // Rebuild value from sub fields
            value = new Scale()
            {
                value = new Vector3(m_ScaleXField.length, m_ScaleYField.length, m_ScaleZField.length)
            };
        }
    }
}
