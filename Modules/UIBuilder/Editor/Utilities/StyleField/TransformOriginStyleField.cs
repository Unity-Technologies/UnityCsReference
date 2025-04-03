// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using JetBrains.Annotations;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    [UsedImplicitly]
    class TransformOriginStyleField : BaseField<TransformOrigin>
    {
        public class BuilderTransformOriginConverter : UxmlAttributeConverter<TransformOrigin>
        {
            public override TransformOrigin FromString(string value) => throw new NotImplementedException();
            public override string ToString(TransformOrigin value) => throw new NotImplementedException();
        }

        [Serializable]
        public new class UxmlSerializedData : BaseField<TransformOrigin>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseField<TransformOrigin>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new TransformOriginStyleField();
        }

        static readonly string s_FieldClassName = "unity-transform-origin-style-field";
        static readonly string s_UxmlPath = BuilderConstants.UtilitiesPath + "/StyleField/TransformOriginStyleField.uxml";
        static readonly string s_UssPathNoExt = BuilderConstants.UtilitiesPath + "/StyleField/TransformOriginStyleField";
        static readonly string s_VisualInputName = "unity-visual-input";
        public static readonly string s_TransformOriginXFieldName = "x-field";
        public static readonly string s_TransformOriginYFieldName = "y-field";
        public static readonly string s_TransformOriginZFieldName = "z-field";
        public static readonly string s_TransformOriginSelectorFieldName = "selector";
        const TransformOriginOffset k_TransformOriginOffset_None = 0;

        DimensionStyleField m_TransformOriginXField;
        DimensionStyleField m_TransformOriginYField;
        DimensionStyleField m_TransformOriginZField;
        TransformOriginSelector m_Selector;

        public TransformOriginStyleField() : this(null) { }

        public TransformOriginStyleField(string label) : base(label)
        {
            AddToClassList(BuilderConstants.InspectorContainerClassName);
            AddToClassList(s_FieldClassName);
            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(s_UssPathNoExt + (EditorGUIUtility.isProSkin ? "Dark" : "Light") + ".uss"));
            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(s_UssPathNoExt + ".uss"));

            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(s_UxmlPath);

            template.CloneTree(this);

            visualInput = this.Q(s_VisualInputName);

            m_TransformOriginXField = this.Q<DimensionStyleField>(s_TransformOriginXFieldName);
            m_TransformOriginYField = this.Q<DimensionStyleField>(s_TransformOriginYFieldName);
            m_TransformOriginZField = this.Q<DimensionStyleField>(s_TransformOriginZFieldName);
            m_Selector = this.Q<TransformOriginSelector>(s_TransformOriginSelectorFieldName);

            m_TransformOriginXField.RegisterValueChangedCallback(e =>
            {
                UpdateTransformOriginField();
                e.StopPropagation();
            });
            m_TransformOriginYField.RegisterValueChangedCallback(e =>
            {
                UpdateTransformOriginField();
                e.StopPropagation();
            });
            m_TransformOriginZField.RegisterValueChangedCallback(e =>
            {
                UpdateTransformOriginField();
                e.StopPropagation();
            });

            m_Selector.pointSelected = OnPointClicked;

            m_TransformOriginXField.units.Add(StyleFieldConstants.UnitPercent);
            m_TransformOriginXField.populatesOptionsMenuFromParentRow = false;
            m_TransformOriginYField.units.Add(StyleFieldConstants.UnitPercent);
            m_TransformOriginYField.populatesOptionsMenuFromParentRow = false;
            m_TransformOriginZField.populatesOptionsMenuFromParentRow = false;

            m_TransformOriginXField.UpdateOptionsMenu();
            m_TransformOriginYField.UpdateOptionsMenu();
            m_TransformOriginZField.UpdateOptionsMenu();

            value = new TransformOrigin(Length.Pixels(0), Length.Pixels(0), 0);
        }

        public override void SetValueWithoutNotify(TransformOrigin newValue)
        {
            base.SetValueWithoutNotify(newValue);
            RefreshSubFields();
        }

        void RefreshSubFields()
        {
            // Converting to dimension here to ensure we write the unit.
            m_TransformOriginXField.SetValueWithoutNotify(value.x.ToDimension().ToString());
            m_TransformOriginYField.SetValueWithoutNotify(value.y.ToDimension().ToString());
            m_TransformOriginZField.SetValueWithoutNotify(value.z.ToString());
            UpdateSelector();
        }

        void UpdateTransformOriginField()
        {
            // Rebuild value from sub fields
            value = new TransformOrigin()
            {
                x = new Dimension { value = m_TransformOriginXField.length, unit = m_TransformOriginXField.unit }.ToLength(),
                y = new Dimension { value = m_TransformOriginYField.length, unit = m_TransformOriginYField.unit }.ToLength(),
                z = m_TransformOriginZField.length
            };
        }

        void OnPointClicked(float x, float y)
        {
            value = new TransformOrigin()
            {
                x = Length.Percent(x * 100),
                y = Length.Percent(y * 100),
                z = value.z
            };
        }

        void UpdateSelector()
        {
            float posX = float.NaN;
            float posY = float.NaN;

            // Show the indicator if x and y are 0
            if (value.x.unit == LengthUnit.Pixel)
            {
                if (Mathf.Approximately(value.x.value, 0))
                    posX = 0;
            }
            else
            {
                posX = value.x.value / 100;
            }

            if (value.y.unit == LengthUnit.Pixel)
            {
                if (Mathf.Approximately(value.y.value, 0))
                    posY = 0;
            }
            else
            {
                posY = value.y.value / 100;
            }

            m_Selector.originX = posX;
            m_Selector.originY = posY;
        }
    }
}
