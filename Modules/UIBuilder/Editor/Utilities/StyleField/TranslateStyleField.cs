// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using JetBrains.Annotations;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements.StyleSheets;


namespace Unity.UI.Builder
{
    internal struct BuilderTranslate : IEquatable<BuilderTranslate>
    {
        public Dimension x;
        public Dimension y;
        public BuilderTranslate(StyleTranslate styleTranslate)
            : this(styleTranslate.value)
        {
        }

        public BuilderTranslate(Translate translate)
        {
            x = new Dimension(translate.x.value, StyleSheetUtilities.ConvertToDimensionUnit(translate.x.unit));
            y = new Dimension(translate.y.value, StyleSheetUtilities.ConvertToDimensionUnit(translate.y.unit));
        }

        public BuilderTranslate(float xValue, Dimension.Unit xUnit, float yValue, Dimension.Unit yUnit)
        {
            x = new Dimension(xValue, xUnit);
            y = new Dimension(yValue, yUnit);
        }

        public BuilderTranslate(float xValue, float yValue, Dimension.Unit unit = Dimension.Unit.Percent)
        {
            x = new Dimension(xValue, unit);
            y = new Dimension(yValue, unit);
        }

        public static bool operator ==(BuilderTranslate lhs, BuilderTranslate rhs)
        {
            return lhs.x == rhs.x && lhs.y == rhs.y;
        }

        public static bool operator !=(BuilderTranslate lhs, BuilderTranslate rhs)
        {
            return !(lhs == rhs);
        }

        public bool Equals(BuilderTranslate other)
        {
            return other == this;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is BuilderTranslate))
            {
                return false;
            }

            var v = (BuilderTranslate)obj;
            return v == this;
        }

        public override int GetHashCode()
        {
            var hashCode = -799583767;
            hashCode = hashCode * -1521134295 + x.GetHashCode();
            hashCode = hashCode * -1521134295 + y.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            return $"(x:{x}, y:{y})";
        }
    }

    [UsedImplicitly]
    class TranslateStyleField : BaseField<BuilderTranslate>
    {
        [UsedImplicitly]
        public new class UxmlFactory : UxmlFactory<TranslateStyleField, UxmlTraits> { }

        static readonly string s_FieldClassName = "unity-translate-style-field";
        static readonly string s_UxmlPath = BuilderConstants.UtilitiesPath + "/StyleField/TranslateStyleField.uxml";
        static readonly string s_VisualInputName = "unity-visual-input";
        public static readonly string s_TranslateXFieldName = "x-field";
        public static readonly string s_TranslateYFieldName = "y-field";

        DimensionStyleField m_TranslateXField;
        DimensionStyleField m_TranslateYField;

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

            m_TranslateXField.units.Add(StyleFieldConstants.UnitPercent);
            m_TranslateXField.populatesOptionsMenuFromParentRow = false;
            m_TranslateYField.units.Add(StyleFieldConstants.UnitPercent);
            m_TranslateYField.populatesOptionsMenuFromParentRow = false;

            m_TranslateXField.UpdateOptionsMenu();
            m_TranslateYField.UpdateOptionsMenu();

            value = new BuilderTranslate()
            {
                x = new Dimension { value = 0, unit = Dimension.Unit.Pixel },
                y = new Dimension { value = 0, unit = Dimension.Unit.Pixel }
            };
        }

        public override void SetValueWithoutNotify(BuilderTranslate newValue)
        {
            base.SetValueWithoutNotify(newValue);
            RefreshSubFields();
        }

        void RefreshSubFields()
        {
            m_TranslateXField.SetValueWithoutNotify(value.x.ToString());
            m_TranslateYField.SetValueWithoutNotify(value.y.ToString());
        }

        void UpdateTranslateField()
        {
            // Rebuild value from sub fields
            value = new BuilderTranslate()
            {
                x = new Dimension { value = m_TranslateXField.length, unit = m_TranslateXField.unit },
                y = new Dimension { value = m_TranslateYField.length, unit = m_TranslateYField.unit }
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

            // if the translate property was already defined then ...
            if (!isNewValue)
            {
                styleSheet.SetValue(styleProperty.values[0], value.x);
                styleSheet.SetValue(styleProperty.values[1], value.y);
            }
            else
            {
                styleSheet.AddValue(styleProperty, value.x);
                styleSheet.AddValue(styleProperty, value.y);
            }
            return isNewValue;
        }
    }
}
