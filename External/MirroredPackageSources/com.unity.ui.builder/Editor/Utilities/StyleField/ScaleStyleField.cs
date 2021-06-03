using System;
using JetBrains.Annotations;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements.StyleSheets;
using UnityEditor.UIElements;

namespace Unity.UI.Builder
{
    internal struct BuilderScale : IEquatable<BuilderScale>
    {
        public Vector3 value;
        public BuilderScale(StyleScale styleScale)
            : this(styleScale.value)
        {
        }

        public BuilderScale(Scale scaleValue)
        {
            value = scaleValue.value;
        }

        public BuilderScale(float xValue, float yValue)
        {
            value = new Vector3(xValue, yValue, 1);
        }

        public static bool operator ==(BuilderScale lhs, BuilderScale rhs)
        {
            return lhs.value == rhs.value;
        }

        public static bool operator !=(BuilderScale lhs, BuilderScale rhs)
        {
            return !(lhs == rhs);
        }

        public bool Equals(BuilderScale other)
        {
            return other == this;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is BuilderScale))
            {
                return false;
            }

            var v = (BuilderScale)obj;
            return v == this;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override string ToString()
        {
            return $"(x:{value.x}, y:{value.y})";
        }
    }

    [UsedImplicitly]
    class ScaleStyleField : BaseField<BuilderScale>
    {
        [UsedImplicitly]
        public new class UxmlFactory : UxmlFactory<ScaleStyleField, UxmlTraits> { }

        static readonly string s_FieldClassName = "unity-scale-style-field";
        static readonly string s_UxmlPath = BuilderConstants.UtilitiesPath + "/StyleField/ScaleStyleField.uxml";
        static readonly string s_VisualInputName = "unity-visual-input";
        public static readonly string s_ScaleXFieldName = "x-field";
        public static readonly string s_ScaleYFieldName = "y-field";

        FloatField m_ScaleXField;
        FloatField m_ScaleYField;

        public ScaleStyleField() : this(null) { }

        public ScaleStyleField(string label) : base(label)
        {
            AddToClassList(BuilderConstants.InspectorContainerClassName);
            AddToClassList(s_FieldClassName);

            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(s_UxmlPath);

            template.CloneTree(this);

            visualInput = this.Q(s_VisualInputName);

            m_ScaleXField = this.Q<FloatField>(s_ScaleXFieldName);
            m_ScaleYField = this.Q<FloatField>(s_ScaleYFieldName);

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

            value = new BuilderScale()
            {
                value = Vector3.zero
            };
        }

        public override void SetValueWithoutNotify(BuilderScale newValue)
        {
            base.SetValueWithoutNotify(newValue);
            RefreshSubFields();
        }

        void RefreshSubFields()
        {
            m_ScaleXField.SetValueWithoutNotify(value.value.x);
            m_ScaleYField.SetValueWithoutNotify(value.value.y);
        }

        void UpdateScaleField()
        {
            // Rebuild value from sub fields
            value = new BuilderScale()
            {
                value = new Vector3(m_ScaleXField.value, m_ScaleYField.value, 1)
            };
        }

        public bool OnFieldValueChange(StyleProperty styleProperty, StyleSheet styleSheet)
        {
            var stylePropertyValueCount = styleProperty.values.Length;
            var isNewValue = stylePropertyValueCount == 0;

            if (!isNewValue && (
                stylePropertyValueCount < 2 ||
                styleProperty.values[0].valueType != StyleValueType.Dimension || 
                styleProperty.values[1].valueType != StyleValueType.Dimension))
            {
                Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
                styleProperty.values = new StyleValueHandle[0];
                isNewValue = true;
            }

            // if the scale property was already defined then ...
            if (!isNewValue)
            {
                styleSheet.SetValue(styleProperty.values[0], value.value.x);
                styleSheet.SetValue(styleProperty.values[1], value.value.y);
            }
            else
            {
                styleSheet.AddValue(styleProperty, value.value.x);
                styleSheet.AddValue(styleProperty, value.value.y);
            }
            return isNewValue;
        }
    }
}
