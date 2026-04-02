// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.UIToolkit.Editor;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class BuilderInspectorVariables : VariablesInspector
    {
        BuilderInspector m_Inspector;
        BuilderSelection m_Selection;
        VisualElement m_SectionFoldout;
        VisualElement currentVisualElement => m_Inspector.currentVisualElement;

        public VisualElement root => m_SectionFoldout;

        public BuilderInspectorVariables(BuilderInspector inspector)
        {
            m_Inspector = inspector;
            m_SectionFoldout = m_Inspector.Q(k_VariablesSectionClassName);
            m_SectionFoldout.Add(this);
            m_SectionFoldout.styleSheets.Add(
                BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(BuilderConstants.UssPath_InspectorVariable));
            m_Selection = inspector.selection;
            styleRule = m_Inspector.currentRule;
        }

        protected override VariablesListItem CreateListItem() => new BuilderInspectorVariablesListItem();

        protected override void OnStyleSheetModified()
        {
            m_Inspector.panel.visualTree.IncrementVersion(VersionChangeType.StyleSheet);
            m_Selection.NotifyOfStylingChange();
        }

        protected override void AfterAddVariable()
        {
            var props = new List<StyleProperty>(styleRule.properties);
            var index = props.FindIndex(p => p.name == BuilderConstants.SelectedStyleRulePropertyName);
            if (index > 0)
            {
                var selectedStyleVar = props[index];
                props.RemoveAt(index);
                props.Add(selectedStyleVar);
                for (int i = 0; i < props.Count; i++)
                {
                    styleRule.properties[i] = props[i];
                }
            }

            OnStyleSheetModified();
            variablesListView.RefreshItems();
        }

        protected override StyleComplexSelector GetRootRule(StyleRule rule)
        {
            if (rule == null) return null;

            foreach (var complexSelector in rule.complexSelectors)
            {
                if (!complexSelector.isSimple) continue;
                var parts = complexSelector.selectors[0].parts;
                if (parts.Length == 1 &&
                    parts[0].type == StyleSelectorType.PseudoClass &&
                    parts[0].value == "root")
                    return complexSelector;
            }

            return null;
        }

        public override void ExtractToGlobalVariable()
        {
            RegisterStyleSheetUndo();

            var selectedIndices = variablesListView.selectedIndicesList;
            if (selectedIndices.Count == 0)
                return;

            var rootSelector = m_Inspector.styleSheet.FindSelector(":root");
            if (currentVisualElement.GetStyleComplexSelector().Equals(rootSelector))
                return;

            if (rootSelector == null)
            {
                rootSelector = BuilderSharedStyles.CreateNewSelector(currentVisualElement.parent, styleSheet, ":root");
                m_Selection.NotifyOfHierarchyChange(m_Inspector);
                m_Selection.NotifyOfStylingChange(m_Inspector);
            }

            foreach (var selectedIndex in selectedIndices)
            {
                if (selectedIndex < 0 || selectedIndex >= variablesItemsSource.Count)
                    continue;
                var styleProperty = variablesItemsSource[selectedIndex];
                styleSheet.TransferPropertyToSelector(rootSelector, styleRule, styleProperty);
            }

            DeleteVariable(variablesListView);
            variablesListView.RefreshItems();
        }
    }
}
