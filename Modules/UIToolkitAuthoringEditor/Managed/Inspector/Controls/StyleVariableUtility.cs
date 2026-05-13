// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor;

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
internal static class StyleVariableUtility
{
    public static readonly string ElementLinkedVariableHandlerVEPropertyName = "__unity-ui-builder-linked-variable-handler";
    public static readonly string VariableDescriptionsCouldNotBeLoadedMessage = "Could not load the variable descriptions file.";
    public static readonly string USSVariableUIBuilderPrefix = "--unity_builder";
    public static readonly string SelectedStyleRulePropertyName = "--ui-builder-selected-style-property";

    static readonly Dictionary<string, string> s_EmptyEditorVarDescriptions = new();
    static Dictionary<string, string> s_EditorVarDescriptions;

    [Serializable]
    public class DescriptionInfo
    {
        public string name;
        public string description;
    }

    [Serializable]
    public class DescriptionInfoList
    {
        public List<DescriptionInfo> infos;
    }

    static void InitEditorVarDescriptions()
    {
        if (s_EditorVarDescriptions != null)
            return;

        var textAsset = UIElementsEditorUtility.LoadVariableDescriptionsAsset();

        if (textAsset)
        {
            s_EditorVarDescriptions = new Dictionary<string, string>();
            var descriptionsContent = textAsset.text;
            var descriptionInfoList = JsonUtility.FromJson<DescriptionInfoList>(descriptionsContent);

            foreach (var descriptionInfo in descriptionInfoList.infos)
            {
                s_EditorVarDescriptions[descriptionInfo.name] = descriptionInfo.description;
            }
        }
        else
        {
            s_EditorVarDescriptions = s_EmptyEditorVarDescriptions;
            Debug.LogWarning(VariableDescriptionsCouldNotBeLoadedMessage);
        }
    }

    public static Dictionary<string, string> editorVariableDescriptions
    {
        get
        {
            InitEditorVarDescriptions();
            return s_EditorVarDescriptions ?? s_EmptyEditorVarDescriptions;
        }
    }

    public static IEnumerable<VariableInfo> GetAllAvailableVariables(VisualElement currentVisualElement, ReadOnlySpan<StyleValueType> compatibleTypes, bool editorExtensionMode)
    {
        var result = new List<VariableInfo>();
        var seenVariables = new HashSet<StyleVariable>();

        var variables = currentVisualElement.variableContext.variables;
        for (int i = 0; i < variables.Count; i++)
        {
            var variable = variables[i];

            if (variable.name == SelectedStyleRulePropertyName)
                continue;

            if (variable.name.StartsWith(USSVariableUIBuilderPrefix))
                continue;

            var isUnityEditorStyleSheet = variable.sheet.IsUnityEditorStyleSheet();
            if (!editorExtensionMode && isUnityEditorStyleSheet)
                continue;

            var valueHandle = variable.handles[0];
            var valueType = valueHandle.valueType;
            if (valueType == StyleValueType.Enum)
            {
                var colorName = variable.sheet.ReadAsString(valueHandle);
                if (StyleSheetColor.TryGetColor(colorName.ToLowerInvariant(), out var color))
                    valueType = StyleValueType.Color;
            }

            if (valueType == StyleValueType.Function)
            {
                var function = (StyleValueFunction)valueHandle.valueIndex;
                if (function == StyleValueFunction.Var)
                {
                    var varName = variable.sheet.ReadVariable(variable.handles[2]);
                    var varInfo = FindVariable(currentVisualElement, varName, editorExtensionMode);
                    if (varInfo.StyleVariable.handles != null)
                        valueType = varInfo.StyleVariable.handles[0].valueType;
                }
            }

            var isCompatibleType = compatibleTypes == null;
            if (!isCompatibleType)
            {
                for (int j = 0; j < compatibleTypes.Length; j++)
                {
                    if (compatibleTypes[j] == valueType)
                    {
                        isCompatibleType = true;
                        break;
                    }
                }
            }

            if (!isCompatibleType || variable.name.StartsWith("--unity-theme"))
                continue;

            if (!seenVariables.Add(variable))
                continue;

            string descr = null;
            if (isUnityEditorStyleSheet)
            {
                editorVariableDescriptions.TryGetValue(variable.name, out descr);
            }
            result.Add(new VariableInfo(variable, descr));
        }

        return result;
    }

    public static VariableEditingHandler GetOrCreateVarHandler(BindableElement field, IVariableEditingContext context, VisualElement styleRow = null, bool attachCompleterOnTarget = true)
    {
        if (field == null)
            return null;

        VariableEditingHandler handler = GetVarHandler(field);

        if (handler == null)
        {
            handler = new VariableEditingHandler(field, context, styleRow, attachCompleterOnTarget);
            field.SetProperty(ElementLinkedVariableHandlerVEPropertyName, handler);
        }
        else
        {
            handler.SetContext(context, styleRow);
        }

        return handler;
    }

    public static VariableEditingHandler GetVarHandler(BindableElement field)
    {
        if (field == null)
            return null;

        return field.GetProperty(ElementLinkedVariableHandlerVEPropertyName) as VariableEditingHandler;
    }

    public static VariableInfo FindVariable(VisualElement currentVisualElement, string variableName, bool editorExtensionMode)
    {
        var variables = currentVisualElement.variableContext.variables;

        for (int i = variables.Count - 1; i >= 0; --i)
        {
            var variable = variables[i];

            if (!editorExtensionMode && variable.sheet.IsUnityEditorStyleSheet())
                continue;

            if (variables[i].name == variableName)
            {
                string descr = null;
                if (variable.sheet.isDefaultStyleSheet)
                {
                    editorVariableDescriptions.TryGetValue(variableName, out descr);
                }
                return new VariableInfo(variable, descr);
            }
        }

        return default;
    }

    public static bool IsUnityEditorStyleSheet(this StyleSheet styleSheet)
    {
        return (UIElementsEditorUtility.IsCommonDarkStyleSheetLoaded() && styleSheet == UIElementsEditorUtility.GetCommonDarkStyleSheet())
            || (UIElementsEditorUtility.IsCommonLightStyleSheetLoaded() && styleSheet == UIElementsEditorUtility.GetCommonLightStyleSheet());
    }
}
