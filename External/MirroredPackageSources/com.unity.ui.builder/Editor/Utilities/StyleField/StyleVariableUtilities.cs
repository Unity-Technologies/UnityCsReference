using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements.Debugger;
using UnityEngine;
using UnityEngine.UIElements;

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

        private static List<VariableInfo> s_AllVariables = new List<VariableInfo>();

        public static IEnumerable<VariableInfo> GetAllAvailableVariables(VisualElement currentVisualElement, StyleValueType[] compatibleTypes, bool editorExtensionMode)
        {
            s_AllVariables.Clear();
            HashSet<string> names = new HashSet<string>();

            // Traverse the element's parent hierarchy
            var current = currentVisualElement;

            while (current != null)
            {
                var customStyles = current.computedStyle.customProperties;

                if (customStyles != null)
                {
                    foreach (var varPair in customStyles)
                    {
                        var varName = varPair.Key;

                        if (varName == BuilderConstants.SelectedStyleRulePropertyName)
                            continue;
                        var propValue = varPair.Value;

                        if (!editorExtensionMode && propValue.sheet.IsUnityEditorStyleSheet())
                            continue;
                        if ((compatibleTypes == null || compatibleTypes.Contains(propValue.handle.valueType)) && !varName.StartsWith("--unity-theme") && !names.Contains(varName))
                        {
                            names.Add(varName);
                            string descr = null;
                            if (propValue.sheet.IsUnityEditorStyleSheet())
                            {
                                editorVariableDescriptions.TryGetValue(varName, out descr);
                            }
                            s_AllVariables.Add(new VariableInfo()
                            {
                                name = varName,
                                value = propValue,
                                description = descr
                            });
                        }
                    }
                }
                current = current.parent;
            }

            return s_AllVariables;
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

        static public VariableEditingHandler GetVarHandler(BindableElement field)
        {
            if (field == null)
                return null;

            return field?.GetProperty(BuilderConstants.ElementLinkedVariableHandlerVEPropertyName) as VariableEditingHandler;
        }

        public static VariableInfo FindVariable(VisualElement currentVisualElement, string variableName, bool editorExtensionMode)
        {
            // Traverse the element's parent hierarchy
            var current = currentVisualElement;

            while (current != null)
            {
                var customStyles = current.computedStyle.customProperties;
                if (customStyles != null)
                {
                    foreach (var varPair in customStyles)
                    {
                        var varName = varPair.Key;
                        var propValue = varPair.Value;

                        if (!editorExtensionMode && propValue.sheet.isDefaultStyleSheet)
                            continue;
                        if (varName == variableName)
                        {
                            string descr = null;
                            if (propValue.sheet.isDefaultStyleSheet)
                            {
                                editorVariableDescriptions.TryGetValue(varName, out descr);
                            }
                            return new VariableInfo()
                            {
                                name = varName,
                                value = propValue,
                                description = descr
                            };
                        }
                    }
                }
                current = current.parent;
            }
            return null;
        }

        public static bool FindVariableOrigin(VisualElement currentVisualElement, string variableName, out StyleSheet outStyleSheet, out StyleComplexSelector outSelector)
        {
            outSelector = null;
            outStyleSheet = null;

            if (string.IsNullOrEmpty(variableName))
                return false;

            // Traverse the element's parent hierarchy to find best matching selector that define the variable
            var extractor = new MatchedRulesExtractor();
            var current = currentVisualElement;

            while (current != null)
            {
                extractor.selectedElementRules.Clear();
                extractor.selectedElementStylesheets.Clear();
                extractor.FindMatchingRules(current);

                var matchedRules = extractor.selectedElementRules;

                for (var i = matchedRules.Count - 1; i >= 0; --i)
                {
                    var matchRecord = matchedRules.ElementAt(i).matchRecord;
                    var ruleProperty = matchRecord.sheet.FindProperty(matchRecord.complexSelector.rule, variableName);

                    if (ruleProperty != null)
                    {
                        outSelector = matchRecord.complexSelector;
                        outStyleSheet = matchRecord.sheet;
                        break;
                    }
                }

                if (outSelector != null)
                    break;

                current = current.parent;
            }

            // If the current visual element is a fake element created for the selector being edited and that the selector found is a fake selector then return the effective selector associated to the fake visual element
            if (outSelector != null && current == currentVisualElement && StyleSheetToUss.ToUssSelector(outSelector).Contains(BuilderConstants.StyleSelectorElementName))
            {
                outSelector = currentVisualElement.GetStyleComplexSelector();
            }

            return outSelector != null;
        }
    }
}
