// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using Unity.Properties;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal static class FieldAffordanceController
{
    public static void UpdateFieldAffordanceData<TInline, TComputedValue>(in FieldAffordanceData fieldAffordanceData, VisualElement element, StyleDiff.ContextType contextType, StylePropertyData<TInline, TComputedValue> value)
    {
        fieldAffordanceData.type = FieldAffordanceDataType.USSProperty;

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
        else if (value.uxmlValue.isInlined && contextType == StyleDiff.ContextType.VisualElement)
        {
            fieldAffordanceData.sourceTypeInfo = FieldAffordanceSourceInfoType.Inline;
        }
        else if (value.uxmlValue.isInlined && contextType == StyleDiff.ContextType.StyleSheet)
        {
            fieldAffordanceData.sourceTypeInfo = FieldAffordanceSourceInfoType.LocalUSSSelector;
        }
        else if (value.uxmlValue.requireVariableResolve)
        {
            fieldAffordanceData.sourceTypeInfo = FieldAffordanceSourceInfoType.USSVariable;
            fieldAffordanceData.inlineValue = value.inlineValue;
            fieldAffordanceData.selector = value.selector;
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
