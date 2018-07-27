// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.StyleSheets;

namespace UnityEditor.StyleSheets
{
    internal class StyleSheetResolver
    {
        internal class ResolvingOptions
        {
            public bool ThrowIfCannotResolve { get; set; }
            public bool SortProperties { get; set; }
            public bool SortRules { get; set; }
        }

        internal class Value
        {
            public StyleValueType ValueType { get; set; }
            public object Obj { get; set; }

            public Value(StyleValueType type, object obj)
            {
                ValueType = type;
                Obj = obj;
            }

            public StyleValueKeyword AsKeyword()
            {
                return (StyleValueKeyword)Obj;
            }

            public Color AsColor()
            {
                return (Color)Obj;
            }

            public float AsFloat()
            {
                return (float)Obj;
            }

            public string AsString()
            {
                return (string)Obj;
            }

            public UnityEngine.Object AsAssetReference()
            {
                return (UnityEngine.Object)Obj;
            }

            public override string ToString()
            {
                return Obj.ToString();
            }

            public bool CompareTo(Value v)
            {
                if (ValueType != v.ValueType)
                    return false;

                switch (ValueType)
                {
                    case StyleValueType.Color:
                        return GUISkinCompare.CompareTo(v.AsColor(), AsColor());
                    case StyleValueType.Float:
                        return GUISkinCompare.CompareTo(v.AsFloat(), AsFloat());
                    case StyleValueType.Keyword:
                        return v.AsKeyword() == AsKeyword();
                    case StyleValueType.Enum:
                    case StyleValueType.ResourcePath:
                    case StyleValueType.String:
                        return v.AsString() == AsString();
                }
                return true;
            }
        }

        internal class Property
        {
            public string Name { get; set; }
            public List<Value> Values { get; set; }

            public Property(string name)
            {
                Name = name;
            }

            public void ValuesToString(StringBuilder builder)
            {
                foreach (var value in Values)
                {
                    builder.Append(value);
                    builder.Append(" ");
                }
            }

            public bool CompareTo(Property p)
            {
                if (p.Name != Name)
                    return false;

                if (Values.Count != p.Values.Count)
                    return false;

                for (int i = 0; i < Values.Count; i++)
                {
                    if (!Values[i].CompareTo(p.Values[i]))
                        return false;
                }

                return true;
            }
        }

        internal class Rule
        {
            public string SelectorName { get; private set; }
            public StyleComplexSelector Selector { get; private set; }
            public Dictionary<string, Property> Properties { get; private set; }
            public List<string> PseudoStateRules { get; private set; }

            public Rule(string selectorName, StyleComplexSelector selector)
            {
                SelectorName = selectorName;
                Selector = selector;
                Properties = new Dictionary<string, Property>();
                PseudoStateRules = new List<string>();
            }

            public List<Rule> ResolveExtendedRules(StyleSheetResolver resolver)
            {
                var rules = new List<Rule>();
                rules.Add(this);
                foreach (var pseudoStateRuleName in PseudoStateRules)
                {
                    rules.Add(resolver.Rules[pseudoStateRuleName]);
                }

                return rules;
            }
        }

        internal class ExtendData
        {
            public Rule ParentRule { get; private set; }
            public string ParentSelectorName { get; set; }
            public List<Rule> ChildrenRules { get; set; }

            public ExtendData(string selectorName)
            {
                ParentSelectorName = selectorName;
                ChildrenRules = new List<Rule>();
            }

            public void ResolveParent(StyleSheetResolver resolver)
            {
                Rule parentRule;
                if (!resolver.Rules.TryGetValue(ParentSelectorName, out parentRule) && resolver.Options.ThrowIfCannotResolve)
                {
                    throw new Exception("Cannot resolve parent: " + ParentSelectorName);
                }

                ParentRule = parentRule;
            }
        }

        public List<StyleSheet> Sheets { get; private set; }
        public Dictionary<string, Rule> Rules { get; private set; }
        public Dictionary<string, ExtendData> ParentToChildren { get; private set; }
        public Dictionary<string, string> ChildToParent { get; private set; }
        public Dictionary<string, Property> Variables { get; private set; }
        public ResolvingOptions Options { get; private set; }

        public StyleSheet ResolvedSheet
        {
            get
            {
                if (m_ResolvedSheet == null)
                {
                    ResolveSheets();
                    ResolveExtend();
                    m_ResolvedSheet = ConvertToStyleSheet();
                }
                return m_ResolvedSheet;
            }
        }

        StyleSheet m_ResolvedSheet;

        public StyleSheetResolver(ResolvingOptions options = null)
        {
            Options = options ?? new ResolvingOptions();
            Sheets = new List<UnityEngine.StyleSheets.StyleSheet>();
            Rules = new Dictionary<string, Rule>();
            m_ResolvedSheet = null;
            Variables = new Dictionary<string, Property>();
            ParentToChildren = new Dictionary<string, ExtendData>();
            ChildToParent = new Dictionary<string, string>();
        }

        public void AddStyleSheet(string sheetPath)
        {
            var sheet = ConverterUtils.LoadResourceRequired<StyleSheet>(sheetPath);
            if (sheet != null)
            {
                Sheets.Add(sheet);
            }
        }

        public void AddStyleSheets(params string[] sheetPaths)
        {
            foreach (var sheetPath in sheetPaths)
            {
                AddStyleSheet(sheetPath);
            }
        }

        public void AddStyleSheets(params StyleSheet[] sheets)
        {
            foreach (var sheet in sheets)
            {
                Sheets.Add(sheet);
            }
        }

        public StyleSheet Refresh()
        {
            Rules.Clear();
            m_ResolvedSheet = null;
            Variables.Clear();
            ParentToChildren.Clear();
            ChildToParent.Clear();
            return ResolvedSheet;
        }

        public void ResolveSheets()
        {
            foreach (var sheet in Sheets)
            {
                ResolveRules(sheet);
            }

            // Link all pseudo state to their main rule:
            foreach (var rule in Rules.Values)
            {
                RecordIfPseudoStateRule(rule);
            }
        }

        public List<Rule> CleanRedundantProperties()
        {
            ResolveSheets();
            ResolveExtendData();

            // Ensure we process derivations reverse topological order so we begin by trimming the "grand children" before anything
            var derivations = TopologicalSort(ParentToChildren.Values.ToList());
            derivations.Reverse();
            var cleanedUpRules = new HashSet<Rule>();

            foreach (var derivationData in derivations)
            {
                foreach (var mainChildrenRule in derivationData.ChildrenRules)
                {
                    var childrenExtendedRules = mainChildrenRule.ResolveExtendedRules(this);
                    foreach (var extRule in childrenExtendedRules)
                    {
                        // Create the parent chain in which we will be looking for properties duplication:
                        var parentChain = GetParentChain(extRule);
                        foreach (var childProperty in extRule.Properties.Values.ToList())
                        {
                            // Navigate all parent rule (extended or not) to see if we are overriding an existing values:
                            foreach (var parentRule in parentChain)
                            {
                                Property parentProperty;
                                if (parentRule.Properties.TryGetValue(childProperty.Name, out parentProperty))
                                {
                                    if (parentProperty.CompareTo(childProperty))
                                    {
                                        // Same property in parentchain and child: remove it.
                                        extRule.Properties.Remove(parentProperty.Name);
                                        cleanedUpRules.Add(extRule);
                                    }
                                    else
                                    {
                                        // This is a property that is overridden in the children. Stop the parentChain:
                                        break;
                                    }
                                }
                                // else: property not in parent, continue to see if it exists further in parent chain.
                            }
                        }
                    }
                }
            }

            return cleanedUpRules.ToList();
        }

        public void ResolveExtend()
        {
            ResolveExtendData();
            // Ensure we process derivations in right order (topologic) so no lingering extend stays:
            var derivations = TopologicalSort(ParentToChildren.Values.ToList());

            // Apply Derivations:
            foreach (var derivation in derivations)
            {
                if (derivation.ParentRule == null)
                {
                    Debug.LogWarning("Cannot resolve parent rule: " + derivation.ParentSelectorName);
                    continue;
                }

                var extendedRules = derivation.ParentRule.ResolveExtendedRules(this);
                foreach (var childrenRule in derivation.ChildrenRules)
                {
                    foreach (var replacementRule in extendedRules)
                    {
                        var childrenReplacementName = replacementRule.SelectorName.Replace(derivation.ParentSelectorName, childrenRule.SelectorName);
                        if (childrenRule.SelectorName == childrenReplacementName)
                        {
                            childrenRule.Properties.Remove(ConverterUtils.k_Extend);
                            Extend(replacementRule, childrenRule);
                        }
                        else
                        {
                            Rule pseudoChildrenRule;
                            if (!Rules.TryGetValue(childrenReplacementName, out pseudoChildrenRule))
                            {
                                var childrenSelector = ConverterUtils.CreateSelectorFromSource(replacementRule.Selector, childrenRule.SelectorName);
                                pseudoChildrenRule = new Rule(childrenReplacementName, childrenSelector);
                                Rules.Add(childrenReplacementName, pseudoChildrenRule);
                                RecordIfPseudoStateRule(pseudoChildrenRule);
                            }

                            Extend(replacementRule, pseudoChildrenRule);
                        }
                    }
                }
            }
        }

        public StyleSheet ConvertToStyleSheet()
        {
            return ConvertToStyleSheet(Rules.Values, Variables, Options);
        }

        public List<Value> ResolveValues(Property property)
        {
            return ResolveValues(property, Variables, Options);
        }

        public static List<Value> ResolveValues(Property property, Dictionary<string, Property> variables, ResolvingOptions options)
        {
            if (property.Values.Count == 1 &&
                property.Values[0].ValueType == StyleValueType.Enum &&
                property.Values[0].AsString().StartsWith("--"))
            {
                Property varProperty;
                if (variables.TryGetValue(property.Values[0].AsString(), out varProperty))
                {
                    return varProperty.Values;
                }
                else if (options.ThrowIfCannotResolve)
                {
                    throw new Exception("Cannot resolve variable: " + property.Values[0].AsString());
                    // Debug.Log("Cannot resolve variable: " + property.Values[0].AsString());
                }
            }

            return property.Values;
        }

        public static StyleSheet ConvertToStyleSheet(IEnumerable<Rule> rules, Dictionary<string, Property> variables = null, ResolvingOptions options = null)
        {
            options = options ?? new ResolvingOptions();
            variables = variables ?? new Dictionary<string, Property>();
            var helper = new StyleSheetBuilderHelper();
            if (options.SortRules)
                rules = rules.OrderBy(rule => rule.SelectorName);
            foreach (var rule in rules)
            {
                helper.BeginRule();
                StyleSheetBuilderHelper.BuildSelector(rule.Selector, helper);

                var propertyValues = rule.Properties.Values.ToList();
                if (options.SortProperties)
                    propertyValues.Sort((p1, p2) => p1.Name.CompareTo(p2.Name));
                foreach (var property in propertyValues)
                {
                    helper.builder.BeginProperty(property.Name);
                    // Try to resolve variable
                    var values = ResolveValues(property, variables, options);
                    AddValues(helper, values);
                    helper.builder.EndProperty();
                }
                helper.EndRule();
            }
            helper.PopulateSheet();
            return helper.sheet;
        }

        private Rule GetParentRule(string childSelectorName)
        {
            Rule parentRule = null;
            if (ChildToParent.ContainsKey(childSelectorName))
            {
                Rules.TryGetValue(ChildToParent[childSelectorName], out parentRule);
            }
            return parentRule;
        }

        private List<Rule> GetParentChain(Rule childRule)
        {
            var isPseudoState = ConverterUtils.IsPseudoSelector(childRule.SelectorName);
            var childMainRuleName = ConverterUtils.GetNoPseudoSelector(childRule.SelectorName);
            var parentChain = new List<Rule>();
            var parentRule = GetParentRule(childMainRuleName);
            while (parentRule != null)
            {
                if (isPseudoState)
                {
                    // Fetch the corresponding pseudo state rule in the parent:
                    var extendedParentRuleName = childRule.SelectorName.Replace(childMainRuleName, parentRule.SelectorName);
                    Rule extentedParentRule;
                    if (Rules.TryGetValue(extendedParentRuleName, out extentedParentRule))
                    {
                        parentChain.Add(extentedParentRule);
                    }
                }
                else
                {
                    parentChain.Add(parentRule);
                }
                parentRule = GetParentRule(parentRule.SelectorName);
            }

            return parentChain;
        }

        private void ResolveExtendData()
        {
            foreach (var derivationData in ParentToChildren.Values)
            {
                derivationData.ResolveParent(this);
            }
        }

        private void RecordIfPseudoStateRule(Rule rule)
        {
            if (ConverterUtils.IsPseudoSelector(rule.SelectorName))
            {
                var mainRuleName = ConverterUtils.GetNoPseudoSelector(rule.SelectorName);
                Rule mainRule;
                if (Rules.TryGetValue(mainRuleName, out mainRule))
                {
                    mainRule.PseudoStateRules.Add(rule.SelectorName);
                }
                else if (Options.ThrowIfCannotResolve)
                {
                    throw new Exception("Cannot resolve main rule: " + mainRuleName);
                }
            }
        }

        private void ResolveRules(StyleSheet sheet)
        {
            foreach (var complexSelector in sheet.complexSelectors)
            {
                var selectorName = StyleSheetToUss.ToUssSelector(complexSelector);
                Rule aggregateRule;
                if (!Rules.TryGetValue(selectorName, out aggregateRule))
                {
                    aggregateRule = new Rule(selectorName, complexSelector);
                    Rules.Add(selectorName, aggregateRule);
                }

                // Override existing properties and append new ones:
                foreach (var property in complexSelector.rule.properties)
                {
                    Property dstProp;
                    if (aggregateRule.Properties.TryGetValue(property.name, out dstProp))
                    {
                        dstProp.Values = ToValues(property, sheet);
                    }
                    else
                    {
                        dstProp = AddProperty(aggregateRule, property, sheet);
                    }
                    if (dstProp.Name.StartsWith("--"))
                    {
                        Variables.Set(dstProp.Name, dstProp);
                    }
                }

                Property derivationProperty;
                if (aggregateRule.Properties.TryGetValue(ConverterUtils.k_Extend, out derivationProperty))
                {
                    var parentName = derivationProperty.Values[0].AsString();
                    ExtendData derivationData;
                    if (!ParentToChildren.TryGetValue(parentName, out derivationData))
                    {
                        derivationData = new ExtendData(parentName);
                        ParentToChildren.Add(parentName, derivationData);
                    }
                    derivationData.ChildrenRules.Add(aggregateRule);
                    if (!ChildToParent.ContainsKey(aggregateRule.SelectorName))
                    {
                        ChildToParent.Add(aggregateRule.SelectorName, parentName);
                    }
                }
            }
        }

        private static List<Value> ToValues(StyleProperty srcProp, StyleSheet srcSheet)
        {
            return srcProp.values.Select(v => new Value(v.valueType, GetPropertyValue(v, srcSheet))).ToList();
        }

        private static Property AddProperty(Rule aggregate, StyleProperty property, StyleSheet srcSheet)
        {
            var dstProperty = new Property(property.name) { Values = ToValues(property, srcSheet) };
            if (!aggregate.Properties.ContainsKey(property.name))
            {
                aggregate.Properties.Add(property.name, dstProperty);
            }
            else
            {
                Debug.LogWarning("Duplicate property");
            }

            return dstProperty;
        }

        private static object GetPropertyValue(StyleValueHandle valueHandle, StyleSheet srcSheet)
        {
            object value = null;
            switch (valueHandle.valueType)
            {
                case StyleValueType.Keyword:
                    value = srcSheet.ReadKeyword(valueHandle);
                    break;
                case StyleValueType.Color:
                    value = srcSheet.ReadColor(valueHandle);
                    break;
                case StyleValueType.ResourcePath:
                    value = srcSheet.ReadResourcePath(valueHandle);
                    break;
                case StyleValueType.Enum:
                    value = srcSheet.ReadEnum(valueHandle);
                    break;
                case StyleValueType.String:
                    value = srcSheet.ReadString(valueHandle);
                    break;
                case StyleValueType.Float:
                    value = srcSheet.ReadFloat(valueHandle);
                    break;
                case StyleValueType.AssetReference:
                    value = srcSheet.ReadAssetReference(valueHandle);
                    break;
                default:
                    throw new Exception("Unhandled value type: " + valueHandle.valueType);
            }
            return value;
        }

        private static void AddValues(StyleSheetBuilderHelper helper, List<Value> values)
        {
            foreach (var value in values)
            {
                switch (value.ValueType)
                {
                    case StyleValueType.Keyword:
                        helper.builder.AddValue(value.AsKeyword());
                        break;
                    case StyleValueType.Color:
                        helper.builder.AddValue(value.AsColor());
                        break;
                    case StyleValueType.Float:
                        helper.builder.AddValue(value.AsFloat());
                        break;
                    case StyleValueType.Enum:
                    case StyleValueType.String:
                    case StyleValueType.ResourcePath:
                        helper.builder.AddValue(value.AsString(), value.ValueType);
                        break;
                    default:
                        throw new Exception("Unhandled value type: " + value.ValueType);
                }
            }
        }

        private static List<ExtendData> TopologicalSort(List<ExtendData> derivations)
        {
            var graph = new Graph(derivations.Count);
            for (var i = 0; i < derivations.Count; ++i)
            {
                var derivation = derivations[i];
                foreach (var childrenRule in derivation.ChildrenRules)
                {
                    var childIndex = derivations.FindIndex(d => d.ParentSelectorName == childrenRule.SelectorName);
                    if (childIndex != -1)
                    {
                        graph.AddEdge(i, childIndex);
                    }
                }
            }

            var sortedEdges = graph.DepthFirstTraversal();
            var sortedDerivations = sortedEdges.Select(index => derivations[index]).ToList();

            return sortedDerivations;
        }

        private void Extend(Rule parentRule, Rule childrenRule)
        {
            foreach (var parentProperty in parentRule.Properties.Values)
            {
                if (Options.ThrowIfCannotResolve && parentProperty.Name == ConverterUtils.k_Extend)
                {
                    throw new Exception("Rule derivation not resolved: " + parentRule.SelectorName);
                    // Debug.Log("Rule derivation not resolved: " + parentRule.SelectorName);
                }
                if (!childrenRule.Properties.ContainsKey(parentProperty.Name))
                {
                    childrenRule.Properties.Add(parentProperty.Name, parentProperty);
                }
            }
        }
    }
}
