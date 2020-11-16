using System.Linq;
using UnityEngine.UIElements;
using UnityEditor.UIElements.Debugger;

namespace Unity.UI.Builder
{
    static class StyleVariableUtilities
    {
        static public VariableEditingHandler GetOrCreateVarHandler(BindableElement field)
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

            // If the current visual element is a fake elemennt created for the selector being edited and that the selector found is a fake selector then return the effective selector associated to the fake visual element
            if (outSelector != null && current == currentVisualElement && StyleSheetToUss.ToUssSelector(outSelector).Contains(BuilderConstants.StyleSelectorElementName))
            {
                outSelector = currentVisualElement.GetStyleComplexSelector();
            }

            return outSelector != null;
        }
    }
}
