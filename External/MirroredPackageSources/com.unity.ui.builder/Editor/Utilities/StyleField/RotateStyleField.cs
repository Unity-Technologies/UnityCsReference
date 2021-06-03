using System;
using JetBrains.Annotations;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements.StyleSheets;


namespace Unity.UI.Builder
{
    internal struct BuilderRotate : IEquatable<BuilderRotate>
    {
        public Dimension x;
        public BuilderRotate(StyleRotate styleRotate)
            : this(styleRotate.value)
        {
        }

        public BuilderRotate(Rotate rotate)
        {
            x = new Dimension(rotate.angle.value, StyleSheetUtilities.ConvertToDimensionUnit(rotate.angle.unit));
        }

        public BuilderRotate(float xValue, Dimension.Unit xUnit)
        {
            x = new Dimension(xValue, xUnit);
        }

        public static bool operator ==(BuilderRotate lhs, BuilderRotate rhs)
        {
            return lhs.x == rhs.x;
        }

        public static bool operator !=(BuilderRotate lhs, BuilderRotate rhs)
        {
            return !(lhs == rhs);
        }

        public bool Equals(BuilderRotate other)
        {
            return other == this;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is BuilderRotate))
            {
                return false;
            }

            var v = (BuilderRotate)obj;
            return v == this;
        }

        public override int GetHashCode()
        {
            var hashCode = -799583767;
            hashCode = hashCode * -1521134295 + x.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            return x.ToString();
        }
    }

    [UsedImplicitly]
    class RotateStyleField : BaseField<BuilderRotate>
    {
        [UsedImplicitly]
        public new class UxmlFactory : UxmlFactory<RotateStyleField, UxmlTraits> { }

        static readonly string s_FieldClassName = "unity-rotate-style-field";
        static readonly string s_UxmlPath = BuilderConstants.UtilitiesPath + "/StyleField/RotateStyleField.uxml";
        static readonly string s_UssPath = BuilderConstants.UtilitiesPath + "/StyleField/RotateStyleField.uss";
        static readonly string s_VisualInputName = "unity-visual-input";
        public static readonly string s_RotateXFieldName = "x-field";

        DimensionStyleField m_RotateXField;

        public RotateStyleField() : this(null) { }

        public RotateStyleField(string label) : base(label)
        {
            AddToClassList(BuilderConstants.InspectorContainerClassName);
            AddToClassList(s_FieldClassName);

            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(s_UssPath));

            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(s_UxmlPath);

            template.CloneTree(this);

            visualInput = this.Q(s_VisualInputName);

            m_RotateXField = this.Q<DimensionStyleField>(s_RotateXFieldName);

            m_RotateXField.RegisterValueChangedCallback(e =>
            {
                UpdateRotateField();
                e.StopPropagation();
            });

            m_RotateXField.option = StyleFieldConstants.UnitDegree;
            m_RotateXField.units.Clear();
            m_RotateXField.units.Add(StyleFieldConstants.UnitDegree);
            m_RotateXField.units.Add(StyleFieldConstants.UnitGrad);
            m_RotateXField.units.Add(StyleFieldConstants.UnitRad);
            m_RotateXField.units.Add(StyleFieldConstants.UnitTurn);
            m_RotateXField.populatesOptionsMenuFromParentRow = false;
           
            m_RotateXField.UpdateOptionsMenu();

            value = new BuilderRotate()
            {
                x = new Dimension { value = 0, unit = Dimension.Unit.Degree }
            };
        }

        public override void SetValueWithoutNotify(BuilderRotate newValue)
        {
            base.SetValueWithoutNotify(newValue);
            RefreshSubFields();
        }

        void RefreshSubFields()
        {
            m_RotateXField.SetValueWithoutNotify(value.x.ToString());
            m_RotateXField.dragStep = value.x.unit == Dimension.Unit.Turn ? 0.1f : 1;
        }

        void UpdateRotateField()
        {
            // Rebuild value from sub fields
            value = new BuilderRotate()
            {
                x = new Dimension { value = m_RotateXField.length, unit = m_RotateXField.unit } 
            };
        }

        public bool OnFieldValueChange(StyleProperty styleProperty, StyleSheet styleSheet)
        {
            var stylePropertyValueCount = styleProperty.values.Length;
            var isNewValue = stylePropertyValueCount == 0;

            if (!isNewValue && styleProperty.values[0].valueType != StyleValueType.Dimension)
            {
                Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
                styleProperty.values = new StyleValueHandle[0];
                isNewValue = true;
            }

            // if the rotate property was already defined then ...
            if (!isNewValue)
            {
                styleSheet.SetValue(styleProperty.values[0], value.x);
            }
            else
            {
                styleSheet.AddValue(styleProperty, value.x);
            }
            return isNewValue;
        }
    }
}
