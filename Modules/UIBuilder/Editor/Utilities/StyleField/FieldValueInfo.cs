// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.UIElements.Debugger;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using UnityEngine.Pool;

namespace Unity.UI.Builder
{
    /// <summary>
    /// Represents the different ways the value of a field can be bound.
    /// </summary>
    internal enum FieldValueBindingInfoType
    {
        /// <summary>
        /// No binding.
        /// </summary>
        None,
        /// <summary>
        /// Set to a constant value.
        /// </summary>
        Constant,
        /// <summary>
        /// Bound to a USS variable
        /// </summary>
        USSVariable,
        /// <summary>
        /// Bound to data binding
        /// </summary>
        Binding
    }

    /// <summary>
    /// Provides extension methods for <see cref="FieldValueBindingInfoType"/> enum.
    /// </summary>
    internal static class FieldValueBindingInfoTypeExtensions
    {
        /// <summary>
        /// Returns the text displayed in the inspector for a <see cref="FieldValueBindingInfoType"/> value.
        /// </summary>
        /// <param name="type">The target type</param>
        /// <returns></returns>
        public static string ToDisplayString(this FieldValueBindingInfoType type)
        {
            return type switch
            {
                FieldValueBindingInfoType.USSVariable => BuilderConstants.FieldValueBindingInfoTypeEnumUSSVariableDisplayString,
                _ => type.ToString()
            };
        }
    }

    /// <summary>
    /// Provides information about how the value of a field is bound.
    /// </summary>
    internal struct FieldValueBindingInfo
    {
        /// <summary>
        /// Data that provides details about the value binding. It is a <see cref="VariableInfo"/> object if the value is bound to a variable.
        /// </summary>
        object m_Data;

        /// <summary>
        /// The type of value binding.
        /// </summary>
        public FieldValueBindingInfoType type { get; }

        /// <summary>
        /// Provides details about the USS variable to which the field is bound.
        /// </summary>
        public VariableInfo variable
        {
            get
            {
                if (m_Data is VariableInfo varInfo)
                    return varInfo;
                return default;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="type">The type of value binding</param>
        public FieldValueBindingInfo(FieldValueBindingInfoType type)
        {
            this.type = type;
            m_Data = default;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="type">The type of value binding</param>
        /// <param name="variable">The details about the variable to which the field is bound</param>
        public FieldValueBindingInfo(FieldValueBindingInfoType type, VariableInfo variable)
        {
            this.type = type;
            this.m_Data = variable;
        }
    }

    /// <summary>
    /// Represents the different ways the value of a field has been resolved.
    /// </summary>
    internal enum FieldValueSourceInfoType
    {
        /// <summary>
        /// Indicates that the value of the underlying VisualElement property is set as inline in its source UXML file.
        /// </summary>
        Inline,
        /// <summary>
        /// Indicates that the value of the underlying Selector property is set in the selector's definition in its
        /// source StyleSheet file.
        /// </summary>
        LocalUSSSelector,
        /// <summary>
        /// Indicates that the value of the underlying VisualElement property is resolved from one of its matching
        /// USS selectors.
        /// </summary>
        MatchingUSSSelector,
        /// <summary>
        /// Indicates that the value of the underlying VisualElement property is inherited from one of its parents
        /// (ancestors).
        /// </summary>
        Inherited,
        /// <summary>
        /// Indicates that the value of the underlying property is default.
        /// </summary>
        Default,
        /// <summary>
        /// Indicates that the value of the underlying VisualElement property is resolved from a Binding
        /// </summary>
        ResolvedBinding,
        /// <summary>
        /// Indicates that the value of the underlying VisualElement property is resolved from a Binding but the binding
        /// is unresolved
        /// </summary>
        UnresolvedBinding,
        /// <summary>
        /// Indicates that the value of the underlying VisualElement property is resolved from a Binding but the binding
        /// is unhandled
        /// </summary>
        UnhandledBinding
    }

    /// <summary>
    /// Provides extension methods for <see cref="FieldValueSourceInfoType"/> enum.
    /// </summary>
    internal static class FieldValueSourceInfoTypeExtensions
    {
        /// <summary>
        /// Indicates whether the source is the currently selected USS selector or a USS selector matching the currently selected VisualElement.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsFromUSSSelector(this FieldValueSourceInfoType type) => type is FieldValueSourceInfoType.LocalUSSSelector
            or FieldValueSourceInfoType.MatchingUSSSelector;

        /// <summary>
        /// Returns the text displayed in the inspector for a <see cref="FieldValueSourceInfoType"/> value.
        /// </summary>
        /// <param name="type">The target type</param>
        /// <returns></returns>
        public static string ToDisplayString(this FieldValueSourceInfoType type)
        {
            return type switch
            {
                FieldValueSourceInfoType.Inherited => BuilderConstants.FieldValueSourceInfoTypeEnumInheritedDisplayString,
                FieldValueSourceInfoType.MatchingUSSSelector => BuilderConstants.FieldValueSourceInfoTypeEnumUSSSelectorDisplayString,
                FieldValueSourceInfoType.LocalUSSSelector => BuilderConstants.FieldValueSourceInfoTypeEnumLocalUSSSelectorDisplayString,
                FieldValueSourceInfoType.ResolvedBinding => BuilderConstants.FieldStatusIndicatorResolvedBindingTooltip,
                FieldValueSourceInfoType.UnresolvedBinding => BuilderConstants.FieldStatusIndicatorUnresolvedBindingTooltip,
                FieldValueSourceInfoType.UnhandledBinding => BuilderConstants.FieldStatusIndicatorUnhandledBindingTooltip,
                _ => type.ToString()
            };
        }
    }

    /// <summary>
    /// Provides information about how the value of a field has been resolved.
    /// </summary>
    internal struct FieldValueSourceInfo
    {
        /// <summary>
        /// Data that provides details about the value source. It is a <see cref="MatchedRule"/> object if the value is from a USS selector.
        /// </summary>
        object m_Data;

        /// <summary>
        /// The type of value source.
        /// </summary>
        public FieldValueSourceInfoType type { get; }

        /// <summary>
        /// Provides details about the matching USS rule from which the value is resolved source.
        /// </summary>
        public MatchedRule matchedRule
        {
            get
            {
                if (m_Data is MatchedRule rule)
                    return rule;
                return default;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="type">The type of value source.</param>
        public FieldValueSourceInfo(FieldValueSourceInfoType type)
        {
            this.type = type;
            this.m_Data = default;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="type">The type of value source.</param>
        /// <param name="rule">The data that provides about the value source.</param>
        public FieldValueSourceInfo(FieldValueSourceInfoType type, MatchedRule rule)
        {
            this.type = type;
            m_Data = rule;
        }
    }

    /// <summary>
    /// The type of the underlying property.
    /// </summary>
    internal enum FieldValueInfoType
    {
        /// <summary>
        /// None
        /// </summary>
        None,
        /// <summary>
        /// UXML attribute.
        /// </summary>
        UXMLAttribute,
        /// <summary>
        /// USS property.
        /// </summary>
        USSProperty
    }

    /// <summary>
    /// Provides extension methods for <see cref="FieldValueInfoType"/> enum.
    /// </summary>
    internal static class FieldValueInfoTypeExtensions
    {
        /// <summary>
        /// Returns the text displayed in the inspector for a <see cref="FieldValueInfoType"/> value.
        /// </summary>
        /// <param name="type">The target type</param>
        /// <returns></returns>
       public static string ToDisplayString(this FieldValueInfoType type)
        {
            return type switch
            {
                FieldValueInfoType.USSProperty => BuilderConstants.FieldValueInfoTypeEnumUSSPropertyDisplayString,
                FieldValueInfoType.UXMLAttribute => BuilderConstants.FieldValueInfoTypeEnumUXMLAttributeDisplayString,
                _ => null
            };
        }
    }

    /// <summary>
    /// Provides information about how the value of a UXML attribute or USS property is bound and resolved.
    /// </summary>
    internal struct FieldValueInfo
    {
        /// <summary>
        /// Type of the underlying property.
        /// </summary>
        public FieldValueInfoType type;
        /// <summary>
        /// The name of the underlying property.
        /// </summary>
        public string name;
        /// <summary>
        /// Information about how the value is bound.
        /// </summary>
        public FieldValueBindingInfo valueBinding;
        /// <summary>
        /// Information about how the value has been resolved.
        /// </summary>
        public FieldValueSourceInfo valueSource;

        /// <summary>
        /// Gets the value binding and value source information of the specified field in the specified inspector.
        /// </summary>
        /// <param name="inspector">The inspector</param>
        /// <param name="field">The field</param>
        /// <param name="property">The overridden USS property</param>
        /// <returns></returns>
        public static FieldValueInfo Get(BuilderInspector inspector, VisualElement field, StyleProperty property)
        {
            return FieldValueInfoGetter.Get(inspector, field, property);
        }
    }

    /// <summary>
    /// Helper class to get the value binding and value source information of a given field.
    /// </summary>
    static class FieldValueInfoGetter
    {
        /// <summary>
        /// Gets the value binding and value source information of the specified field in the specified inspector.
        /// </summary>
        /// <param name="inspector">The inspector</param>
        /// <param name="field">The field</param>
        /// <param name="property">The overridden USS property</param>
        /// <returns></returns>
        public static FieldValueInfo Get(BuilderInspector inspector, VisualElement field, StyleProperty property)
        {
            // If the field is not contained in the Builder inspector then ignore
            if (inspector == null)
                return default;

            var uxmlAttr = field.GetLinkedAttributeDescription();
            FieldValueBindingInfo valueBinding = default;
            FieldValueSourceInfo valueSource = default;
            var propName = "";

            // If the field is a UXML attribute of a visual element then...
            if (uxmlAttr != null)
            {
                propName = uxmlAttr.name;
                valueBinding = new FieldValueBindingInfo(FieldValueBindingInfoType.Constant);
                bool isInline =
                    BuilderInspectorAttributes.IsAttributeOverriden(inspector.currentVisualElement, uxmlAttr);
                valueSource = new FieldValueSourceInfo(isInline ? FieldValueSourceInfoType.Inline : FieldValueSourceInfoType.Default);
            }
            // .. otherwise, if the field is a USS property then...
            else
            {
                // 1. Determine the value binding type
                // Look for the USS variable bound to the field
                var selectionIsSelector = BuilderSharedStyles.IsSelectorElement(inspector.currentVisualElement);
                var varHandler = StyleVariableUtilities.GetOrCreateVarHandler(field as BindableElement);
                var varName = VariableEditingHandler.GetBoundVariableName(varHandler);

                if (!string.IsNullOrEmpty(varName))
                {
                    var varInfo = StyleVariableUtilities.FindVariable(inspector.currentVisualElement, varName, inspector.document.fileSettings.editorExtensionMode);

                    // If the variable cannot be resolved then at least set the name
                    if (!varInfo.IsValid())
                        varInfo = new VariableInfo(varName);

                    valueBinding = new FieldValueBindingInfo(FieldValueBindingInfoType.USSVariable, varInfo);
                }
                else
                {
                    valueBinding = new FieldValueBindingInfo(FieldValueBindingInfoType.Constant);
                }

                // 2. Determine the value source
                // Check whether there is a style property specified
                propName = field.GetProperty(BuilderConstants.InspectorStylePropertyNameVEPropertyName) as string;

                // If the style property is set in the file then...
                if (property != null)
                {
                    if (selectionIsSelector)
                    {
                        var selectorMatchRecord =
                            new SelectorMatchRecord(inspector.currentVisualElement.GetClosestStyleSheet(), 0)
                            {
                                complexSelector = inspector.currentVisualElement.GetStyleComplexSelector()
                            };
                        valueSource = new FieldValueSourceInfo(FieldValueSourceInfoType.LocalUSSSelector, new MatchedRule(selectorMatchRecord));
                    }
                    else
                    {
                        valueSource = new FieldValueSourceInfo(FieldValueSourceInfoType.Inline);
                    }
                }
                // otherwise ...
                else
                {
                    var sourceIsSet = false;

                    if (!selectionIsSelector)
                    {
                        // Check if the value is from a matching selector
                        var matchedRules = inspector.matchingSelectors.matchedRulesExtractor.selectedElementRules;

                        for (var i = matchedRules.Count - 1; i >= 0; --i)
                        {
                            var matchedRule = matchedRules.ElementAt(i);
                            var matchRecord = matchedRule.matchRecord;
                            var ruleProperty = matchRecord.sheet.FindProperty(matchRecord.complexSelector, propName);

                            // If propName has a short hand then try to find the matching selector using the shorthand
                            if (ruleProperty == null)
                            {
                                var id = StylePropertyName.StylePropertyIdFromString(propName);
                                var shorthandId = id.GetShorthandProperty();

                                if (shorthandId != StylePropertyId.Unknown)
                                {
                                    if (StylePropertyUtil.s_IdToName.TryGetValue(shorthandId, out var shorthandName))
                                    {
                                        ruleProperty = matchRecord.sheet.FindProperty(matchRecord.complexSelector, shorthandName);
                                    }
                                }
                            }

                            if (ruleProperty != null)
                            {
                                valueSource = new FieldValueSourceInfo(FieldValueSourceInfoType.MatchingUSSSelector,
                                    matchedRule);
                                sourceIsSet = true;
                                break;
                            }
                        }

                        // If the value does not come from a matching stylesheet selector then check if it comes from inheritance
                        if (!sourceIsSet)
                        {
                            var result = DictionaryPool<StylePropertyId, int>.Get();
                            StyleDebug.FindSpecifiedStyles(inspector.currentVisualElement.computedStyle,
                                inspector.matchingSelectors.matchedRulesExtractor.matchRecords, result);

                            StylePropertyId id;
                            if (StylePropertyUtil.s_NameToId.TryGetValue(propName, out id))
                            {
                                if (result.TryGetValue(id, out var spec))
                                {
                                    if (spec == StyleDebug.InheritedSpecificity)
                                    {
                                        valueBinding = default;
                                        valueSource = new FieldValueSourceInfo(FieldValueSourceInfoType.Inherited);
                                        sourceIsSet = true;
                                    }
                                }
                            }

                            DictionaryPool<StylePropertyId, int>.Release(result);
                        }
                    }

                    // If no source is set so far then we assume the source is default
                    if (!sourceIsSet)
                    {
                        valueSource = new FieldValueSourceInfo(FieldValueSourceInfoType.Default);
                    }
                }
            }

            var bindingProperty = BuilderInspectorUtilities.GetBindingProperty(inspector, field);

            if (inspector.bindingsCache != null
                && inspector.bindingsCache.TryGetCachedData(inspector.currentVisualElement, bindingProperty, out var cacheData))
            {
                valueBinding = new FieldValueBindingInfo(FieldValueBindingInfoType.Binding);

                if (cacheData.isStatusResultValid && cacheData.status == BindingStatus.Success)
                {
                    valueSource = new FieldValueSourceInfo(FieldValueSourceInfoType.ResolvedBinding);
                }
                // Check if Status Result valid for unhandled
                else if (cacheData.isStatusResultValid && cacheData.status == BindingStatus.Pending)
                {
                    valueSource = new FieldValueSourceInfo(FieldValueSourceInfoType.UnhandledBinding);
                }
                else
                {
                    valueSource = new FieldValueSourceInfo(FieldValueSourceInfoType.UnresolvedBinding);
                }
            }

            return new FieldValueInfo()
            {
                type = uxmlAttr != null ? FieldValueInfoType.UXMLAttribute : FieldValueInfoType.USSProperty,
                name = propName,
                valueBinding = valueBinding,
                valueSource = valueSource
            };
        }
    }
}
