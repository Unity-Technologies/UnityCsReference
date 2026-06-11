// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Implements <see cref="IVariableEditingContext"/> for the UIToolkitAuthoring inspector.
/// Uses <see cref="StyleInspectorElement"/> and its <see cref="StyleInspectorElement.AuthoringContext"/>
/// to provide the variable editing context.
/// </summary>
internal class AuthoringVariableEditingContext : IVariableEditingContext
{
    readonly StyleInspectorElement m_Inspector;

    public AuthoringVariableEditingContext(StyleInspectorElement inspector)
    {
        m_Inspector = inspector;
    }

    public VisualElement CurrentVisualElement => m_Inspector.Target.Element;
    public StyleRule CurrentRule
    {
        get
        {
            if (m_Inspector.Target.Rule != null)
                return m_Inspector.Target.Rule;

            var vea = m_Inspector.Target.Element.visualElementAsset;
            var vta = m_Inspector.Target.Element.visualTreeAssetSource;
            return vta != null && vea != null ? vta.GetInlineStyleRule(vea) : null;
        }
    }

    public StyleSheet CurrentStyleSheet => m_Inspector.Target.Sheet ??
        m_Inspector.Target.Element.visualTreeAssetSource?.inlineSheet;

    public bool EditorExtensionMode => false;
    public bool IsSelectorElement => m_Inspector.Target.Type == StyleDiff.ContextType.StyleSheet;
    public VisualElement TooltipRoot => m_Inspector;

    public void SetVariable(string variableName, BindableElement field, string styleName, int index)
    {
        if (CurrentStyleSheet == null)
            return;

        var rule = CurrentRule;
        if (rule == null)
        {
            var vea = m_Inspector.Target.Element.visualElementAsset;
            var vta = m_Inspector.Target.Element.visualTreeAssetSource;
            if (vta == null || vea == null)
                return;
            rule = vta.GetOrCreateInlineStyleRule(vea);
        }

        StylePropertyUtil.propertyNameToStylePropertyId.TryGetValue(styleName, out var id);
        SetVariableCommand.Execute(CommandSources.Inspector, CurrentStyleSheet, rule, id, variableName);

        // Update selector element
        var element = CurrentVisualElement;
        element?.UpdateInlineRule(CurrentStyleSheet, rule, element.variableContext);
        element?.IncrementVersion(VersionChangeType.StyleSheet | VersionChangeType.Styles);
    }

    public void UnsetVariable(BindableElement field, string styleName)
    {
        if (CurrentRule == null || string.IsNullOrEmpty(styleName))
            return;

        StylePropertyUtil.propertyNameToStylePropertyId.TryGetValue(styleName, out var id);
        RemoveVariableCommand.Execute(CommandSources.Inspector, CurrentStyleSheet, CurrentRule, id);

        // Update selector element
        var element = CurrentVisualElement;
        element?.UpdateInlineRule(CurrentStyleSheet, CurrentRule, element.variableContext);
        element?.IncrementVersion(VersionChangeType.StyleSheet | VersionChangeType.Styles);
    }

    public void RefreshUI()
    {
        m_Inspector.Refresh();
    }

    public string GetBoundVariableNameFromCurrentRule(string styleName, int index)
    {
        if (CurrentStyleSheet == null)
            return null;

        var property = CurrentRule?.FindLastProperty(styleName);
        if (property == null)
            return null;

        if (index == 0 && property.TryGetVariableReference(CurrentStyleSheet, out var variableName))
            return variableName;

        return GetVariableNameAtIndex(property, CurrentStyleSheet, index);
    }

    public string GetBoundVariableNameFromMatchedRules(string styleName, int index)
    {
        // TODO: implement using matching rules information from style diff
        return string.Empty;
    }

    /// <summary>
    /// Resolves a variable name from a multi-value style property at the given logical index.
    /// </summary>
    static string GetVariableNameAtIndex(StyleProperty property, StyleSheet styleSheet, int index)
    {
        using var _ = ListPool<int>.Get(out var offsets);
        StyleSheetUtility.GetValueOffsets(styleSheet, property.values, offsets);

        if (index < 0 || index >= offsets.Count)
            return null;

        var offset = offsets[index];
        var values = property.values;

        if (offset >= values.Length || values[offset].valueType != StyleValueType.Function)
            return null;

        // var() is encoded as: Function, Float (arg count), Variable (name)
        if (offset + 2 < values.Length && values[offset + 2].valueType == StyleValueType.Variable)
            return styleSheet.ReadVariable(values[offset + 2]);

        return null;
    }
}
