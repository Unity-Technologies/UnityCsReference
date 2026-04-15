// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Text.RegularExpressions;
using Unity.UIToolkit.Editor;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    internal class BuilderVariableEditingContext : IVariableEditingContext
    {
        readonly Builder m_Builder;
        readonly BuilderInspector m_Inspector;

        public BuilderVariableEditingContext(BuilderInspector inspector)
        {
            m_Inspector = inspector;
            m_Builder = inspector.paneWindow as Builder;
        }

        public VisualElement CurrentVisualElement => m_Inspector.currentVisualElement;
        public StyleRule CurrentRule => m_Inspector.currentRule;
        public StyleSheet CurrentStyleSheet => m_Inspector.styleSheet;
        public bool EditorExtensionMode => m_Inspector.document.fileSettings.editorExtensionMode;
        public bool IsSelectorElement => BuilderSharedStyles.IsSelectorElement(m_Inspector.currentVisualElement);
        public VisualElement TooltipRoot => m_Builder?.rootVisualElement;

        public void SetVariable(string variableName, BindableElement field, string styleName, int index)
        {
            m_Inspector.styleFields.OnFieldVariableChange(variableName, field, styleName, index);
        }

        public void UnsetVariable(BindableElement field)
        {
            m_Inspector.styleFields.UnsetStylePropertyForElement(field, true);
        }

        public void RefreshUI()
        {
            m_Inspector.RefreshUI(false);
        }

        public string GetBoundVariableNameFromCurrentRule(string styleName, int index)
        {
            var property = CurrentRule?.FindLastProperty(styleName);
            if (property == null)
                return null;

            using (var manipulator = CurrentStyleSheet.GetStylePropertyManipulator(
                CurrentVisualElement, CurrentRule, property.name, EditorExtensionMode))
            {
                var displayIndex = index;
                displayIndex = AdjustDisplayIndexForFilter(displayIndex, manipulator);
                displayIndex = AdjustDisplayIndexForTransitions(displayIndex, manipulator);

                if (displayIndex >= 0 && displayIndex < manipulator.GetValuesCount() && manipulator.IsVariableAtIndex(displayIndex))
                    return manipulator.GetVariableNameAtIndex(displayIndex);
            }

            return null;
        }

        public string GetBoundVariableNameFromMatchedRules(string styleName, int index)
        {
            var matchedRules = m_Inspector.matchingSelectors.matchedRulesExtractor.selectedElementRules;
            var matchedRulesList = new System.Collections.Generic.List<MatchedRule>(matchedRules);

            for (var i = matchedRulesList.Count - 1; i >= 0; --i)
            {
                var matchRecord = matchedRulesList[i].matchRecord;
                var ruleProperty = matchRecord.complexSelector.rule?.FindLastProperty(styleName);

                if (ruleProperty == null)
                    continue;

                using (var manipulator = matchRecord.sheet.GetStylePropertyManipulator(
                    m_Inspector.currentVisualElement, matchRecord.complexSelector.rule, ruleProperty.name,
                    m_Inspector.document.fileSettings.editorExtensionMode))
                {
                    var displayIndex = index;
                    displayIndex = AdjustDisplayIndexForFilter(displayIndex, manipulator);
                    displayIndex = AdjustDisplayIndexForTransitions(displayIndex, manipulator);

                    if (displayIndex >= 0 && displayIndex < manipulator.GetValuesCount() && manipulator.IsVariableAtIndex(displayIndex))
                        return manipulator.GetVariableNameAtIndex(displayIndex);
                }
            }

            return null;
        }

        static int AdjustDisplayIndexForFilter(int index, StylePropertyManipulator manipulator)
        {
            if (StylePropertyUtil.propertyNameToStylePropertyId.TryGetValue(manipulator.propertyName, out var id) &&
                id == StylePropertyId.Filter)
                return -1;

            return index;
        }

        static int AdjustDisplayIndexForTransitions(int index, StylePropertyManipulator manipulator)
        {
            var valueCount = manipulator.GetValuesCount();
            if (index < valueCount ||
                !StylePropertyUtil.propertyNameToStylePropertyId.TryGetValue(manipulator.propertyName, out var id) ||
                !id.IsTransitionId())
                return index;

            switch (id)
            {
                case StylePropertyId.TransitionProperty:
                    index = -1;
                    break;
                case StylePropertyId.TransitionDuration:
                    index %= valueCount;
                    break;
                case StylePropertyId.TransitionTimingFunction:
                    index %= valueCount;
                    break;
                case StylePropertyId.TransitionDelay:
                    index %= valueCount;
                    break;
            }

            return index;
        }
    }
}
