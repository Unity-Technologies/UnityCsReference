// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using JetBrains.Annotations;
using System;
using System.Diagnostics;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    [UsedImplicitly]
    class TranslateStyleField : BaseField<Translate>
    {
        public class BuilderTranslateConverter : UxmlAttributeConverter<Translate>
        {
            public override Translate FromString(string value) => throw new NotImplementedException();
            public override string ToString(Translate value) => throw new NotImplementedException();
        }

        [Serializable]
        public new class UxmlSerializedData : BaseField<Translate>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseField<Translate>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new TranslateStyleField();
        }

        static readonly string s_FieldClassName = "unity-translate-style-field";
        static readonly string s_UxmlPath = BuilderConstants.UtilitiesPath + "/StyleField/TranslateStyleField.uxml";
        static readonly string s_VisualInputName = "unity-visual-input";
        public static readonly string s_TranslateXFieldName = "x-field";
        public static readonly string s_TranslateYFieldName = "y-field";
        public static readonly string s_TranslateZFieldName = "z-field";

        DimensionStyleField m_TranslateXField;
        DimensionStyleField m_TranslateYField;
        DimensionStyleField m_TranslateZField;

        public TranslateStyleField() : this(null) { }

        public TranslateStyleField(string label) : base(label)
        {
            AddToClassList(BuilderConstants.InspectorContainerClassName);
            AddToClassList(s_FieldClassName);

            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(s_UxmlPath);

            template.CloneTree(this);

            visualInput = this.Q(s_VisualInputName);

            m_TranslateXField = this.Q<DimensionStyleField>(s_TranslateXFieldName);
            m_TranslateYField = this.Q<DimensionStyleField>(s_TranslateYFieldName);
            m_TranslateZField = this.Q<DimensionStyleField>(s_TranslateZFieldName);

            m_TranslateXField.RegisterValueChangedCallback(e =>
            {
                UpdateTranslateField();
                e.StopPropagation();
            });
            m_TranslateYField.RegisterValueChangedCallback(e =>
            {
                UpdateTranslateField();
                e.StopPropagation();
            });
            m_TranslateZField.RegisterValueChangedCallback(e =>
            {
                UpdateTranslateField();
                e.StopPropagation();
            });

            m_TranslateXField.units.Add(StyleFieldConstants.UnitPercent);
            m_TranslateXField.populatesOptionsMenuFromParentRow = false;
            m_TranslateYField.units.Add(StyleFieldConstants.UnitPercent);
            m_TranslateYField.populatesOptionsMenuFromParentRow = false;
            m_TranslateZField.populatesOptionsMenuFromParentRow = false;

            m_TranslateXField.UpdateOptionsMenu();
            m_TranslateYField.UpdateOptionsMenu();
            m_TranslateZField.UpdateOptionsMenu();

            value = new Translate(Length.Pixels(0), Length.Pixels(0));
        }

        public override void SetValueWithoutNotify(Translate newValue)
        {
            base.SetValueWithoutNotify(newValue);
            RefreshSubFields();
        }

        void RefreshSubFields()
        {
            m_TranslateXField.SetValueWithoutNotify(value.x.ToString());
            m_TranslateYField.SetValueWithoutNotify(value.y.ToString());
            m_TranslateZField.SetValueWithoutNotify(value.z.ToString());
        }

        void UpdateTranslateField()
        {
            // Rebuild value from sub fields
            value = new Translate()
            {
                x = new Dimension { value = m_TranslateXField.length, unit = m_TranslateXField.unit }.ToLength(),
                y = new Dimension { value = m_TranslateYField.length, unit = m_TranslateYField.unit }.ToLength(),
                z = m_TranslateZField.length
            };
        }
    }
}
