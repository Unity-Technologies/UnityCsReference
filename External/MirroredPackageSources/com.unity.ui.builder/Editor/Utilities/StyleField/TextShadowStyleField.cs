using JetBrains.Annotations;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    internal struct BuilderTextShadow
    {
        public Dimension offsetX;
        public Dimension offsetY;
        public Dimension blurRadius;
        public Color color;

        public BuilderTextShadow(StyleTextShadow styleTextShadow)
        {
            var textShadow = styleTextShadow.value;
            offsetX = new Dimension(textShadow.offset.x, Dimension.Unit.Pixel);
            offsetY = new Dimension(textShadow.offset.y, Dimension.Unit.Pixel);
            blurRadius = new Dimension(textShadow.blurRadius, Dimension.Unit.Pixel);
            color = textShadow.color;
        }

        public BuilderTextShadow(TextShadow textShadow)
        {
            offsetX = new Dimension(textShadow.offset.x, Dimension.Unit.Pixel);
            offsetY = new Dimension(textShadow.offset.y, Dimension.Unit.Pixel);
            blurRadius = new Dimension(textShadow.blurRadius, Dimension.Unit.Pixel);
            color = textShadow.color;
        }
    }

    [UsedImplicitly]
    class TextShadowStyleField : BaseField<BuilderTextShadow>
    {
        [UsedImplicitly]
        public new class UxmlFactory : UxmlFactory<TextShadowStyleField, UxmlTraits> {}

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

        public override void SetValueWithoutNotify(BuilderTextShadow newValue)
        {
            base.SetValueWithoutNotify(newValue);
            RefreshSubFields();
        }

        void RefreshSubFields()
        {
            m_OffsetXField.SetValueWithoutNotify(value.offsetX.ToString());
            m_OffsetYField.SetValueWithoutNotify(value.offsetY.ToString());
            m_BlurRadiusField.SetValueWithoutNotify(value.blurRadius.ToString());
            m_ColorField.SetValueWithoutNotify(value.color);
        }

        void UpdateTextShadowField()
        {
            // Rebuild value from sub fields
            BuilderTextShadow builderTextShadow;
            builderTextShadow.offsetX = new Dimension { value = m_OffsetXField.length, unit = m_OffsetXField.unit };
            builderTextShadow.offsetY = new Dimension { value = m_OffsetYField.length, unit = m_OffsetYField.unit };
            builderTextShadow.blurRadius = new Dimension { value = m_BlurRadiusField.length, unit = m_BlurRadiusField.unit };
            builderTextShadow.color = m_ColorField.value;
            value = builderTextShadow;
        }

        public bool OnFieldValueChange(StyleProperty styleProperty, StyleSheet styleSheet)
        {
            var stylePropertyValueCount = styleProperty.values.Length;
            var isNewValue = stylePropertyValueCount == 0;

            // Assume that TextShadow style property is made of 2+ values at all times at this point
            // If the current style property is saved as a different type than the new style type,
            // we need to re-save it here as the new type. We do this by just removing the current value.
            if (!isNewValue && stylePropertyValueCount != 4 ||
                !isNewValue && styleProperty.values[0].valueType != StyleValueType.Dimension ||
                !isNewValue && styleProperty.values[1].valueType != StyleValueType.Dimension ||
                !isNewValue && styleProperty.values[2].valueType != StyleValueType.Dimension ||
                !isNewValue && styleProperty.values[3].valueType != StyleValueType.Color)
            {
                Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

                styleProperty.values = new StyleValueHandle[0];
                isNewValue = true;
            }

            var offsetX = new Dimension {value = m_OffsetXField.length, unit = m_OffsetXField.unit};
            var offsetY = new Dimension {value = m_OffsetYField.length, unit = m_OffsetYField.unit};
            var blurRadius = new Dimension {value = m_BlurRadiusField.length, unit = m_BlurRadiusField.unit};

            if (isNewValue)
            {
                styleSheet.AddValue(styleProperty, offsetX);
                styleSheet.AddValue(styleProperty, offsetY);
                styleSheet.AddValue(styleProperty, blurRadius);
                styleSheet.AddValue(styleProperty, m_ColorField.value);
            }
            else
            {
                styleSheet.SetValue(styleProperty.values[0], offsetX);
                styleSheet.SetValue(styleProperty.values[1], offsetY);
                styleSheet.SetValue(styleProperty.values[2], blurRadius);
                styleSheet.SetValue(styleProperty.values[3], m_ColorField.value);
            }

            return isNewValue;
        }
    }
}
