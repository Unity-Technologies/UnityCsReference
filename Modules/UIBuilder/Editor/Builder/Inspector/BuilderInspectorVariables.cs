// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIBuilder not yet converted
using System;
using System.Collections.Generic;
using System.IO;
using Unity.UIToolkit.Editor;
using UnityEditor;
using UnityEditor.UIElements;
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
            // Force immediate style resolution to update the element's variableContext
            currentVisualElement.SetInlineRule(styleSheet, styleRule);

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

        protected override bool supportsCreateNewStyleSheet => true;

        protected override IReadOnlyList<(StyleSheet sheet, string label)> GetAvailableStyleSheets()
        {
            var vta = m_Inspector.document.activeOpenUXMLFile.visualTreeAsset;
            if (vta == null)
                return Array.Empty<(StyleSheet, string)>();

            var sheets = vta.GetAllReferencedStyleSheets();
            var result = new List<(StyleSheet, string)>();
            var labelCounts = new Dictionary<string, int>();
            foreach (var sheet in sheets)
            {
                if (sheet == null) continue;
                var path = AssetDatabase.GetAssetPath((UnityEngine.Object)sheet);
                var label = string.IsNullOrEmpty(path) ? sheet.name : Path.GetFileName(path);
                labelCounts.TryGetValue(label, out var count);
                labelCounts[label] = count + 1;
                result.Add((sheet, label));
            }

            for (var i = 0; i < result.Count; i++)
            {
                var (sheet, label) = result[i];
                if (labelCounts[label] <= 1)
                    continue;

                var path = AssetDatabase.GetAssetPath((UnityEngine.Object)sheet);
                if (string.IsNullOrEmpty(path))
                    continue;

                var parent = Path.GetFileName(Path.GetDirectoryName(path));
                result[i] = (sheet, string.IsNullOrEmpty(parent) ? label : $"{label} ({parent})");
            }

            return result;
        }

        public override void ExtractVariableToRootSelector(StyleSheet targetStyleSheet = null)
        {
            var selectedIndices = variablesListView.selectedIndicesList;
            if (selectedIndices.Count == 0)
                return;

            // Capture source data before any rebuild invalidates references
            var capturedSourceSheet = styleSheet;
            var capturedSourceRule  = styleRule;
            var capturedProps = new List<StyleProperty>();
            foreach (var i in selectedIndices)
            {
                if (i >= 0 && i < variablesItemsSource.Count)
                    capturedProps.Add(variablesItemsSource[i]);
            }

            string newUssPath = null;
            if (targetStyleSheet == null)
            {
                newUssPath = BuilderStyleSheetsUtilities.s_SaveFileDialogCallback();
                if (string.IsNullOrEmpty(newUssPath))
                    return;

                BuilderStyleSheetsUtilities.CreateNewUSSAsset(m_Inspector.paneWindow, newUssPath);
                targetStyleSheet = BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(newUssPath);
                if (targetStyleSheet == null)
                    return;

                Undo.RegisterCompleteObjectUndo(capturedSourceSheet, "Extract Variable to New StyleSheet");
            }

            var rootSelector = targetStyleSheet.FindSelector(":root");

            // Don't extract if we are already editing the :root rule of the target stylesheet
            if (rootSelector != null && capturedSourceRule == rootSelector.rule)
                return;

            if (rootSelector == null)
            {
                // CreateNewSelector adds the :root rule to the StyleSheet; the change is applied by the notifications below.
                var selectorsRoot = BuilderSharedStyles.GetSelectorContainerElement(m_Selection.documentRootElement);
                rootSelector = BuilderSharedStyles.CreateNewSelector(selectorsRoot, targetStyleSheet, ":root");
                if (newUssPath == null)
                {
                    m_Selection.NotifyOfHierarchyChange(m_Inspector);
                    m_Selection.NotifyOfStylingChange(m_Inspector);
                }
            }

            var isSameSheet = targetStyleSheet == capturedSourceSheet;
            foreach (var prop in capturedProps)
            {
                if (isSameSheet)
                    capturedSourceSheet.TransferPropertyToSelector(rootSelector, capturedSourceRule, prop);
                else
                    targetStyleSheet.TransferPropertyToSelector(rootSelector, capturedSourceSheet, capturedSourceRule, prop);
            }

            if (newUssPath != null)
            {
                m_Selection.NotifyOfHierarchyChange(m_Inspector);
                m_Selection.NotifyOfStylingChange(m_Inspector);
            }

            DeleteVariable(variablesListView);
            variablesListView.RefreshItems();
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
