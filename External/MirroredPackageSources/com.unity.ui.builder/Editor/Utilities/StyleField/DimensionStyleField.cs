using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using UnityEngine.UIElements.StyleSheets.Syntax;

namespace Unity.UI.Builder
{
    internal class DimensionStyleField : StyleField<float>
    {
        static readonly string k_DraggerFieldUssClassName = "unity-style-field__dragger-field";

        List<string> m_Units = new List<string>() { StyleFieldConstants.UnitPixel };
        static readonly string s_DefaultUnit = StyleFieldConstants.UnitPixel;

        public new class UxmlFactory : UxmlFactory<DimensionStyleField, UxmlTraits> {}

        public new class UxmlTraits : StyleField<float>.UxmlTraits {}

        static public string defaultUnit => s_DefaultUnit;

        IntegerField m_DraggerIntegerField;

        float m_DragStep = 1;

        public float dragStep
        {
            get
            {
                return m_DragStep;
            }

            set
            {
                if (m_DragStep == value)
                    return;
                m_DragStep = value;
                UpdateDragger();
            }
        }

        public List<string> units => m_Units;

        public float length
        {
            get => innerValue;
            set
            {
                innerValue = value;
                SetValueWithoutNotify(innerValue.ToString());
            }
        }

        public Dimension.Unit unit
        {
            get
            {
                var found = StyleFieldConstants.StringToDimensionUnitMap.TryGetValue(option, out var outUnit);
                if (found)
                    return outUnit;

                return Dimension.Unit.Unitless;
            }
            set
            {
                var found = StyleFieldConstants.DimensionUnitToStringMap.TryGetValue(value, out var opt);
                if (found)
                    option = opt;
                else
                    option = s_DefaultUnit;

                SetValueWithoutNotify(option);
            }
        }

        public DimensionStyleField() : this(string.Empty) { }

        public DimensionStyleField(string label) : base(label)
        {
            m_DraggerIntegerField = new IntegerField(" ");
            m_DraggerIntegerField.name = "dragger-integer-field";
            m_DraggerIntegerField.AddToClassList(k_DraggerFieldUssClassName);
            m_DraggerIntegerField.RegisterValueChangedCallback(OnDraggerFieldUpdate);
            Insert(0, m_DraggerIntegerField);
            option = defaultUnit;

            RefreshChildFields();
        }

        bool FindUnitInExpression(Expression expression, DataType dataType)
        {
            if (expression.type == ExpressionType.Data && expression.dataType == dataType)
                return true;

            if (expression.subExpressions == null)
                return false;

            foreach (var subExp in expression.subExpressions)
                if (FindUnitInExpression(subExp, dataType))
                    return true;

            return false;
        }

        protected override List<string> GenerateAdditionalOptions(string binding)
        {
            if (string.IsNullOrEmpty(binding))
                return m_Units;

            var syntaxParser = new StyleSyntaxParser();

            var syntaxFound = StylePropertyCache.TryGetSyntax(binding, out var syntax);
            if (!syntaxFound)
                return StyleFieldConstants.KLDefault;

            var expression = syntaxParser.Parse(syntax);
            if (expression == null)
                return StyleFieldConstants.KLDefault;

            var hasLength = FindUnitInExpression(expression, DataType.Length);
            var hasPercent = FindUnitInExpression(expression, DataType.Percentage);

            m_Units.Clear();
            if (hasLength)
                m_Units.Add(StyleFieldConstants.UnitPixel);
            if (hasPercent)
                m_Units.Add(StyleFieldConstants.UnitPercent);

            return m_Units;
        }

        protected override bool SetInnerValueFromValue(string val)
        {
            if (styleKeywords.Contains(val))
                return false;

            var num = new string(val.Where((c) => Char.IsDigit(c) || c == '.' || c == '-').ToArray());
            float length;
            var result = float.TryParse(num, out length);
            if (!result)
                return false;

            if (isKeyword)
                option = defaultUnit;

            innerValue = length;
            return true;
        }

        protected override bool SetOptionFromValue(string val)
        {
            if (base.SetOptionFromValue(val))
                return true;

            var unit = new string(val.Where((c) => !Char.IsDigit(c) && c != '.' && c != '-').ToArray());
            if (string.IsNullOrEmpty(unit) || !m_Units.Contains(unit))
                return false;

            option = unit;
            return true;
        }

        protected override string ComposeValue()
        {
            if (styleKeywords.Contains(option))
                return option;

            return innerValue.ToString() + option;
        }

        protected override void RefreshChildFields()
        {
            textField.SetValueWithoutNotify(GetTextFromValue());
            UpdateDragger();
            optionsPopup.SetValueWithoutNotify(GetOptionFromValue());
        }

        void UpdateDragger()
        {
            m_DraggerIntegerField?.SetValueWithoutNotify(Mathf.RoundToInt(innerValue / m_DragStep));
        }

        void OnDraggerFieldUpdate(ChangeEvent<int> evt)
        {
            if (isKeyword)
                option = defaultUnit;

            value = (evt.newValue * m_DragStep).ToString();

            evt.StopImmediatePropagation();
            evt.PreventDefault();
        }
    }
}
