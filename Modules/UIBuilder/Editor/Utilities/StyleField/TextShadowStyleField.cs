// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using JetBrains.Annotations;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements.StyleSheets;
using System;
using System.Diagnostics;

namespace Unity.UI.Builder
{
    [UsedImplicitly]
    class TextShadowStyleField : BaseField<TextShadow>
    {
        public class TextShadowStyleFieldConverter : UxmlAttributeConverter<TextShadow>
        {
            public override TextShadow FromString(string value) => throw new NotImplementedException();
            public override string ToString(TextShadow value) => throw new NotImplementedException();
        }

        [Serializable]
        public new class UxmlSerializedData : BaseField<TextShadow>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseField<TextShadow>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new TextShadowStyleField();
        }

        static readonly string s_FieldClassName = "unity-text-shadow-style-field";
        static readonly string s_UxmlPath = BuilderConstants.UtilitiesPath + "/StyleField/TextShadowStyleField.uxml";
        static readonly string s_UssPath = BuilderConstants.UtilitiesPath + "/StyleField/TextFoldoutStyleField.uss";

        private static readonly string s_TextShadowFieldName = "text-shadow-field";
        static readonly string s_OffsetXFieldName = s_TextShadowFieldName + "-offset-x";
        static readonly string s_OffsetYFieldName = s_TextShadowFieldName + "-offset-y";
        static readonly string s_BlurRadiusFieldName = s_TextShadowFieldName + "-blur-radius";
        private static readonly string s_ColorFieldName = s_TextShadowFieldName + "-color";

        DimensionStyleField m_OffsetXField;
        DimensionStyleField m_OffsetYField;
        DimensionStyleField m_BlurRadiusField;
        ColorField m_ColorField;

        public TextShadowStyleField() : this(null) {}

        public TextShadowStyleField(string label) : base(label)
        {
            AddToClassList(s_FieldClassName);

            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(s_UssPath));

            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(s_UxmlPath);
            template.CloneTree(this);

            m_OffsetXField = this.Q<DimensionStyleField>(s_OffsetXFieldName);
            m_OffsetYField = this.Q<DimensionStyleField>(s_OffsetYFieldName);
            m_BlurRadiusField = this.Q<DimensionStyleField>(s_BlurRadiusFieldName);

            m_OffsetXField.dragStep = BuilderConstants.DimensionStyleFieldReducedDragStep;
            m_OffsetYField.dragStep = BuilderConstants.DimensionStyleFieldReducedDragStep;
            m_BlurRadiusField.dragStep = BuilderConstants.DimensionStyleFieldReducedDragStep;

            m_ColorField = this.Q<ColorField>(s_ColorFieldName);

            m_OffsetXField.RegisterValueChangedCallback(e =>
            {
                UpdateTextShadowField();
                e.StopPropagation();
            });
            m_OffsetYField.RegisterValueChangedCallback(e =>
            {
                UpdateTextShadowField();
                e.StopPropagation();
            });
            m_BlurRadiusField.RegisterValueChangedCallback(e =>
            {
                UpdateTextShadowField();
                e.StopPropagation();
            });
            m_ColorField.RegisterValueChangedCallback(e =>
            {
                UpdateTextShadowField();
                e.StopPropagation();
            });
        }

        public override void SetValueWithoutNotify(TextShadow newValue)
        {
            base.SetValueWithoutNotify(newValue);
            RefreshSubFields();
        }

        void RefreshSubFields()
        {
            m_OffsetXField.SetValueWithoutNotify(value.offset.x.ToString());
            m_OffsetYField.SetValueWithoutNotify(value.offset.y.ToString());
            m_BlurRadiusField.SetValueWithoutNotify(value.blurRadius.ToString());
            m_ColorField.SetValueWithoutNotify(value.color);
        }

        void UpdateTextShadowField()
        {
            value = new TextShadow
            {
                offset = new Vector2(m_OffsetXField.length, m_OffsetYField.length),
                blurRadius = m_BlurRadiusField.length,
                color = m_ColorField.value
            };
        }

        internal void UpdateSubFieldVisualInputTooltips(string offsetXTooltip, string offsetYTooltip, string blurRadiusTooltip, string colorTooltip)
        {
            m_OffsetXField.visualInput.tooltip = offsetXTooltip;
            m_OffsetYField.visualInput.tooltip = offsetYTooltip;
            m_BlurRadiusField.visualInput.tooltip = blurRadiusTooltip;
            m_ColorField.visualInput.tooltip = colorTooltip;
        }
    }
}
