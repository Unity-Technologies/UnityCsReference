// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using JetBrains.Annotations;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    [UsedImplicitly]
    [UxmlElement]
    partial class BoxShadowStyleField : BaseField<BoxShadow>
    {
        static readonly string s_FieldClassName = "unity-box-shadow-style-field";
        static readonly string s_UxmlPath = BuilderConstants.UtilitiesPath + "/StyleField/BoxShadowStyleField.uxml";
        static readonly string s_UssPath = BuilderConstants.UtilitiesPath + "/StyleField/TextFoldoutStyleField.uss";

        static readonly string s_BoxShadowFieldName = "box-shadow-field";
        static readonly string s_ColorFieldName = s_BoxShadowFieldName + "-color";
        static readonly string s_OffsetXFieldName = s_BoxShadowFieldName + "-offset-x";
        static readonly string s_OffsetYFieldName = s_BoxShadowFieldName + "-offset-y";
        static readonly string s_BlurRadiusFieldName = s_BoxShadowFieldName + "-blur-radius";
        static readonly string s_SpreadRadiusFieldName = s_BoxShadowFieldName + "-spread-radius";
        static readonly string s_InsetFieldName = s_BoxShadowFieldName + "-inset";
        static readonly string s_OffsetLabelName = s_BoxShadowFieldName + "-offset-label";

        ColorField m_ColorField;
        DimensionStyleField m_OffsetXField;
        DimensionStyleField m_OffsetYField;
        DimensionStyleField m_BlurRadiusField;
        DimensionStyleField m_SpreadRadiusField;
        Toggle m_InsetField;

        public ColorField colorField => m_ColorField;

        public BoxShadowStyleField() : this(null) {}

        public BoxShadowStyleField(string label) : base(label)
        {
            AddToClassList(s_FieldClassName);

            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(s_UssPath));

            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(s_UxmlPath);
            template.CloneTree(this);

            m_ColorField = this.Q<ColorField>(s_ColorFieldName);
            m_OffsetXField = this.Q<DimensionStyleField>(s_OffsetXFieldName);
            m_OffsetYField = this.Q<DimensionStyleField>(s_OffsetYFieldName);
            m_BlurRadiusField = this.Q<DimensionStyleField>(s_BlurRadiusFieldName);
            m_SpreadRadiusField = this.Q<DimensionStyleField>(s_SpreadRadiusFieldName);
            m_InsetField = this.Q<Toggle>(s_InsetFieldName);

            m_OffsetXField.dragStep = BuilderConstants.DimensionStyleFieldReducedDragStep;
            m_OffsetYField.dragStep = BuilderConstants.DimensionStyleFieldReducedDragStep;
            m_BlurRadiusField.dragStep = BuilderConstants.DimensionStyleFieldReducedDragStep;
            m_SpreadRadiusField.dragStep = BuilderConstants.DimensionStyleFieldReducedDragStep;

            m_ColorField.RegisterValueChangedCallback(OnSubFieldValueChanged);
            m_OffsetXField.RegisterValueChangedCallback(OnSubFieldValueChanged);
            m_OffsetYField.RegisterValueChangedCallback(OnSubFieldValueChanged);
            m_BlurRadiusField.RegisterValueChangedCallback(OnSubFieldValueChanged);
            m_SpreadRadiusField.RegisterValueChangedCallback(OnSubFieldValueChanged);
            m_InsetField.RegisterValueChangedCallback(OnSubFieldValueChanged);
        }

        public override void SetValueWithoutNotify(BoxShadow newValue)
        {
            base.SetValueWithoutNotify(newValue);
            RefreshSubFields();
        }

        void RefreshSubFields()
        {
            m_ColorField.SetValueWithoutNotify(value.color);
            m_OffsetXField.SetValueWithoutNotify(value.offset.x.ToString());
            m_OffsetYField.SetValueWithoutNotify(value.offset.y.ToString());
            m_BlurRadiusField.SetValueWithoutNotify(value.blurRadius.ToString());
            m_SpreadRadiusField.SetValueWithoutNotify(value.spreadRadius.ToString());
            m_InsetField.SetValueWithoutNotify(value.inset);
        }

        void OnSubFieldValueChanged<T>(ChangeEvent<T> e)
        {
            value = new BoxShadow
            {
                color = m_ColorField.value,
                offset = new Vector2(m_OffsetXField.length, m_OffsetYField.length),
                // The blur radius can't be negative
                blurRadius = Mathf.Max(0, m_BlurRadiusField.length),
                spreadRadius = m_SpreadRadiusField.length,
                inset = m_InsetField.value
            };

            // The field is reset even when the clamped value doesn't change
            if (m_BlurRadiusField.length < 0)
                m_BlurRadiusField.length = value.blurRadius;
            e.StopPropagation();
        }

        // The descriptions are on the labels too, like the labels of top-level style fields
        internal void UpdateSubFieldVisualInputTooltips(string colorTooltip, string offsetTooltip, string offsetXTooltip, string offsetYTooltip, string blurRadiusTooltip, string spreadRadiusTooltip, string insetTooltip)
        {
            this.Q<Label>(s_OffsetLabelName).tooltip = offsetTooltip;
            m_ColorField.visualInput.tooltip = m_ColorField.labelElement.tooltip = colorTooltip;
            m_OffsetXField.visualInput.tooltip = m_OffsetXField.labelElement.tooltip = offsetXTooltip;
            m_OffsetYField.visualInput.tooltip = m_OffsetYField.labelElement.tooltip = offsetYTooltip;
            m_BlurRadiusField.visualInput.tooltip = m_BlurRadiusField.labelElement.tooltip = blurRadiusTooltip;
            m_SpreadRadiusField.visualInput.tooltip = m_SpreadRadiusField.labelElement.tooltip = spreadRadiusTooltip;
            m_InsetField.tooltip = insetTooltip;
        }
    }
}
