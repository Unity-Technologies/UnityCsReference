// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements.Debugger;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    static class StyleVariableUtilities
    {
        private static Dictionary<string, string> s_EmptyEditorVarDescriptions = new Dictionary<string, string>();
        private static Dictionary<string, string> s_EditorVarDescriptions;

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

            var textAsset = EditorGUIUtility.Load("UIPackageResources/StyleSheets/Default/Variables/Public/descriptions.json") as TextAsset;

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
                Builder.ShowWarning(BuilderConstants.VariableDescriptionsCouldNotBeLoadedMessage);
            }
        }

        static public Dictionary<string, string> editorVariableDescriptions
        {
            get
            {
                InitEditorVarDescriptions();
                return s_EditorVarDescriptions ?? s_EmptyEditorVarDescriptions;
            }
        }

        public static IEnumerable<VariableInfo> GetAllAvailableVariables(VisualElement currentVisualElement, StyleValueType[] compatibleTypes, bool editorExtensionMode)
        {
            return currentVisualElement.variableContext.variables
                .Where(variable =>
                {
                    if (variable.name == BuilderConstants.SelectedStyleRulePropertyName)
                        return false;

                    if (variable.name.StartsWith(BuilderConstants.USSVariableUIBuilderPrefix))
                        return false;

                    if (!editorExtensionMode && variable.sheet.IsUnityEditorStyleSheet())
                        return false;

                    var valueHandle = variable.handles[0];
                    var valueType = valueHandle.valueType;
                    if (valueType == StyleValueType.Enum)
                    {
                        var colorName = variable.sheet.ReadAsString(valueHandle);
                        if (StyleSheetColor.TryGetColor(colorName.ToLower(), out var color))
                            valueType = StyleValueType.Color;
                    }

                    if (valueType == StyleValueType.Function)
                    {
                        var function = (StyleValueFunction)valueHandle.valueIndex;
                        if (function == StyleValueFunction.Var)
                        {
                            // resolve to find the true value type
                            var varName = variable.sheet.ReadVariable(variable.handles[2]);
                            var varInfo = FindVariable(currentVisualElement, varName, editorExtensionMode);
                            if (varInfo.styleVariable.handles != null)
                                valueType = varInfo.styleVariable.handles[0].valueType;
                        }
                    }

                    return (compatibleTypes == null || compatibleTypes.Contains(valueType)) && !variable.name.StartsWith("--unity-theme");
                })
                .Distinct()
                .Select(variable =>
                {
                    string descr = null;
                    if (variable.sheet.IsUnityEditorStyleSheet())
                    {
                        editorVariableDescriptions.TryGetValue(variable.name, out descr);
                    }
                    return new VariableInfo(variable, descr);
                });
        }

        public static VariableEditingHandler GetOrCreateVarHandler(BindableElement field)
        {
            if (field == null)
                return null;

            VariableEditingHandler handler = GetVarHandler(field);

            if (handler == null)
            {
                handler = new VariableEditingHandler(field);
                field.SetProperty(BuilderConstants.ElementLinkedVariableHandlerVEPropertyName, handler);
            }
            return handler;
        }

        public static VariableEditingHandler GetVarHandler(BindableElement field)
        {
            if (field == null)
                return null;

            return field?.GetProperty(BuilderConstants.ElementLinkedVariableHandlerVEPropertyName) as VariableEditingHandler;
        }

        public static VariableInfo FindVariable(VisualElement currentVisualElement, string variableName, bool editorExtensionMode)
        {
            var variables = currentVisualElement.variableContext.variables;

            for (int i = variables.Count - 1; i >= 0; --i)
            {
                var variable = variables[i];

                if (!editorExtensionMode && variable.sheet.isDefaultStyleSheet)
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
    }
}
