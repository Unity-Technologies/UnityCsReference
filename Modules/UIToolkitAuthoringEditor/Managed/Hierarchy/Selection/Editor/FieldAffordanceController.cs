// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor;

internal static class FieldAffordanceController
{
    public static void UpdateFieldAffordanceData<TInline, TComputedValue>(in FieldAffordanceData fieldAffordanceData, VisualElement element, StyleDiff.ContextType contextType, StylePropertyData<TInline, TComputedValue> value, StyleSheet owningSheet = null)
    {
        fieldAffordanceData.Reset();
        fieldAffordanceData.type = FieldAffordanceDataType.USSProperty;

        // Animation takes precedence over binding/variable/inline/selector when active.
        if (TryProbeAnimationDriven(element, value.id, out var animationSubState))
        {
            fieldAffordanceData.sourceTypeInfo = animationSubState;
            fieldAffordanceData.targetElement = element;
            return;
        }

        if (value.binding != null)
        {
            if (element.TryGetLastBindingToUIResult(value.binding.property, out var bindingResult))
            {
                fieldAffordanceData.sourceTypeInfo = bindingResult.status switch
                {
                    BindingStatus.Success => FieldAffordanceSourceInfoType.ResolvedBinding,
                    BindingStatus.Pending => FieldAffordanceSourceInfoType.UnhandledBinding,
                    _ => FieldAffordanceSourceInfoType.UnresolvedBinding
                };
                fieldAffordanceData.targetElement = element;
                fieldAffordanceData.binding = value.binding;
            }
            else
            {
                fieldAffordanceData.sourceTypeInfo = FieldAffordanceSourceInfoType.UnresolvedBinding;
            }
        }
        else if (value.uxmlValue.requireVariableResolve)
        {
            fieldAffordanceData.sourceTypeInfo = FieldAffordanceSourceInfoType.USSVariable;

            if (contextType == StyleDiff.ContextType.VisualElement && element.visualTreeAssetSource == null)
                return;

            // If a variable needs resolution when selecting a visual element, the style comes from the inline sheet.
            // We use the inline sheet to resolve the variable reference, but keep the variable sheet as the selector stylesheet.
            if (owningSheet != null && value.uxmlValue.inlineProperty.TryGetVariableReference(owningSheet, out var variableName))
            {
                var varInfo = StyleVariableUtility.FindVariable(element, variableName, false);
                if (varInfo.IsValid())
                {
                    fieldAffordanceData.inlineValue = variableName;
                    fieldAffordanceData.variableSheet = varInfo.Sheet;
                }
            }
        }
        else if (value.uxmlValue.isInlined && contextType == StyleDiff.ContextType.VisualElement)
        {
            fieldAffordanceData.sourceTypeInfo = FieldAffordanceSourceInfoType.Inline;
        }
        else if (value.uxmlValue.isInlined && contextType == StyleDiff.ContextType.StyleSheet)
        {
            fieldAffordanceData.sourceTypeInfo = FieldAffordanceSourceInfoType.LocalUSSSelector;
        }
        else if (value.selector.complexSelector is { rule: not null })
        {
            fieldAffordanceData.sourceTypeInfo = FieldAffordanceSourceInfoType.MatchingUSSSelector;
            fieldAffordanceData.selector = value.selector;
        }
        else
        {
            fieldAffordanceData.sourceTypeInfo = FieldAffordanceSourceInfoType.Default;
        }
    }

    // True when the property is currently driven
    static bool TryProbeAnimationDriven(VisualElement element, StylePropertyId stylePropertyId, out FieldAffordanceSourceInfoType subState)
    {
        subState = default;
        if (element == null || stylePropertyId == StylePropertyId.Unknown)
            return false;
        if (!AnimationRecordingStyleBridge.TryGetBindingPropertyName(stylePropertyId, out var propName))
            return false;

        // Per-element UIAnimationClip:
        if (PerElementAnimationContext.TryResolveForElementForProbe(element, out var uiClip, out var perElementBinder, out var perElementPath)
            && uiClip != null)
        {
            var perElementKey = AnimationRecordingStyleBridge.BuildStyleKeyPropertyName(perElementPath, propName, null);
            if (TryClassify(uiClip, perElementKey, perElementBinder, perElementPath, (int)stylePropertyId, out subState))
                return true;
        }

        // Panel-wide animation:
        var panelRoot = element.GetFirstAncestorOfType<IPanelComponentRootElement>();
        var panelComponent = panelRoot?.panelComponent;
        var panelGo = panelComponent?.gameObject;
        if (panelGo == null)
            return false;
        var panelRenderer = panelGo.GetComponent<UnityEngine.UIElements.PanelRenderer>();
        if (panelRenderer == null)
            return false;
        var recordability = VisualElementRecordability.ProbeElement(element);
        if (!recordability.CanRecord)
            return false;
        var panelPath = recordability.Path ?? string.Empty;
        var panelKey = AnimationRecordingStyleBridge.BuildStyleKeyPropertyName(panelPath, propName, null);

        var panelBinder = panelRenderer.GetAnimationBinder();
        if (panelBinder != null)
            panelBinder.UpdateElementNamesIfNeeded();
        return TryClassify(panelRenderer, panelKey, panelBinder, panelPath, (int)stylePropertyId, out subState);
    }

    static bool TryClassify(UnityEngine.Object target, string key, UIAnimationBinder binder, string elementPath, int propertyId, out FieldAffordanceSourceInfoType subState)
    {
        subState = default;

        bool animated = AnimationMode.IsPropertyAnimated(target, key);
        bool candidate = AnimationMode.IsPropertyCandidate(target, key);

        bool runtimeBound = binder != null && elementPath != null && binder.IsBound(elementPath, propertyId);

        if (!animated && !candidate && !runtimeBound)
            return false;

        // (Recording > Candidate > Animated).
        // unkeyed edit during record only registers on the candidate driver,
        // so IsPropertyAnimated is false until the edit becomes a key.
        if (AnimationMode.InAnimationRecording())
            subState = FieldAffordanceSourceInfoType.AnimationRecording;
        else if (candidate)
            subState = FieldAffordanceSourceInfoType.AnimationCandidate;
        else
            subState = FieldAffordanceSourceInfoType.AnimationAnimated;
        return true;
    }
    public static void UpdateFieldAffordanceData(in FieldAffordanceData fieldAffordanceData, VisualElement element, Binding binding, bool isInline)
    {
        fieldAffordanceData.type = FieldAffordanceDataType.UXMLAttribute;

        if (binding != null)
        {
            if (element.TryGetLastBindingToUIResult(binding.property, out var bindingResult))
            {
                fieldAffordanceData.sourceTypeInfo = bindingResult.status switch
                {
                    BindingStatus.Success => FieldAffordanceSourceInfoType.ResolvedBinding,
                    BindingStatus.Pending => FieldAffordanceSourceInfoType.UnhandledBinding,
                    _ => FieldAffordanceSourceInfoType.UnresolvedBinding
                };
                fieldAffordanceData.targetElement = element;
                fieldAffordanceData.binding = binding;
            }
            else
            {
                fieldAffordanceData.sourceTypeInfo = FieldAffordanceSourceInfoType.UnresolvedBinding;
            }
        }
        else if (isInline)
        {
            fieldAffordanceData.sourceTypeInfo = FieldAffordanceSourceInfoType.Inline;
        }
        else
        {
            fieldAffordanceData.sourceTypeInfo = FieldAffordanceSourceInfoType.Default;
        }
    }
}
