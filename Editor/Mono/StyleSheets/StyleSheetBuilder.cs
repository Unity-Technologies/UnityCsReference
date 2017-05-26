// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define ENABLE_STYLE_SHEET_BUILDER_LOGS
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.StyleSheets;

namespace UnityEditor.StyleSheets
{
    class StyleSheetBuilder
    {
        public struct ComplexSelectorScope : System.IDisposable
        {
            StyleSheetBuilder m_Builder;

            public ComplexSelectorScope(StyleSheetBuilder builder)
            {
                m_Builder = builder;
            }

            public void Dispose()
            {
                m_Builder.EndComplexSelector();
            }
        }

        enum BuilderState
        {
            Init,
            Rule,
            ComplexSelector,
            Property
        }
        BuilderState m_BuilderState;

        List<float> m_Floats = new List<float>();
        List<Color> m_Colors = new List<Color>();
        List<string> m_Strings = new List<string>();
        List<StyleRule> m_Rules = new List<StyleRule>();
        List<StyleComplexSelector> m_ComplexSelectors = new List<StyleComplexSelector>();

        List<StyleProperty> m_CurrentProperties = new List<StyleProperty>();
        List<StyleValueHandle> m_CurrentValues = new List<StyleValueHandle>();
        StyleComplexSelector m_CurrentComplexSelector;
        List<StyleSelector> m_CurrentSelectors = new List<StyleSelector>();
        string m_CurrentPropertyName;
        StyleRule m_CurrentRule;

        public void BeginRule(int ruleLine)
        {
            Log("Beginning rule");
            Debug.Assert(m_BuilderState == BuilderState.Init);
            m_BuilderState = BuilderState.Rule;

            m_CurrentRule = new StyleRule { line = ruleLine };
        }

        public ComplexSelectorScope BeginComplexSelector(int specificity)
        {
            Log("Begin complex selector with specificity " + specificity);
            Debug.Assert(m_BuilderState == BuilderState.Rule);
            m_BuilderState = BuilderState.ComplexSelector;

            m_CurrentComplexSelector = new StyleComplexSelector();
            m_CurrentComplexSelector.specificity = specificity;
            m_CurrentComplexSelector.ruleIndex = m_Rules.Count;

            return new ComplexSelectorScope(this);
        }

        public void AddSimpleSelector(StyleSelectorPart[] parts, StyleSelectorRelationship previousRelationsip)
        {
            Debug.Assert(m_BuilderState == BuilderState.ComplexSelector);
            var selector = new StyleSelector();
            selector.parts = parts;
            selector.previousRelationship = previousRelationsip;

            Log("Add simple selector " + selector);

            m_CurrentSelectors.Add(selector);
        }

        public void EndComplexSelector()
        {
            Log("Ending complex selector");

            Debug.Assert(m_BuilderState == BuilderState.ComplexSelector);
            m_BuilderState = BuilderState.Rule;

            if (m_CurrentSelectors.Count > 0)
            {
                m_CurrentComplexSelector.selectors = m_CurrentSelectors.ToArray();
                m_ComplexSelectors.Add(m_CurrentComplexSelector);
                m_CurrentSelectors.Clear();
            }
            m_CurrentComplexSelector = null;
        }

        public void BeginProperty(string name)
        {
            Log("Begin property named " + name);

            Debug.Assert(m_BuilderState == BuilderState.Rule);
            m_BuilderState = BuilderState.Property;

            m_CurrentPropertyName = name;
        }

        public void AddValue(float value)
        {
            RegisterValue(m_Floats, StyleValueType.Float, value);
        }

        public void AddValue(StyleValueKeyword keyword)
        {
            // for keyword we use the index to store the enum value
            m_CurrentValues.Add(new StyleValueHandle((int)keyword, StyleValueType.Keyword));
        }

        public void AddValue(string value, StyleValueType type)
        {
            RegisterValue(m_Strings, type, value);
        }

        public void AddValue(Color value)
        {
            RegisterValue(m_Colors, StyleValueType.Color, value);
        }

        public void EndProperty()
        {
            Log("Ending property");

            Debug.Assert(m_BuilderState == BuilderState.Property);
            m_BuilderState = BuilderState.Rule;

            StyleProperty property = new StyleProperty();
            property.name = m_CurrentPropertyName;
            property.values = m_CurrentValues.ToArray();
            m_CurrentProperties.Add(property);
            m_CurrentValues.Clear();
        }

        public int EndRule()
        {
            Log("Ending rule");

            Debug.Assert(m_BuilderState == BuilderState.Rule);
            m_BuilderState = BuilderState.Init;

            m_CurrentRule.properties = m_CurrentProperties.ToArray();
            m_Rules.Add(m_CurrentRule);
            m_CurrentRule = null;
            m_CurrentProperties.Clear();
            return m_Rules.Count - 1;
        }

        public void BuildTo(StyleSheet writeTo)
        {
            Debug.Assert(m_BuilderState == BuilderState.Init);

            writeTo.floats = m_Floats.ToArray();
            writeTo.colors = m_Colors.ToArray();
            writeTo.strings = m_Strings.ToArray();
            writeTo.rules = m_Rules.ToArray();
            writeTo.complexSelectors = m_ComplexSelectors.ToArray();
        }

        void RegisterValue<T>(List<T> list, StyleValueType type, T value)
        {
            Log("Add value of type " + type + " : " + value);
            Debug.Assert(m_BuilderState == BuilderState.Property);
            list.Add(value);
            m_CurrentValues.Add(new StyleValueHandle(list.Count - 1, type));
        }

        static void Log(string msg)
        {
        }
    }
}
