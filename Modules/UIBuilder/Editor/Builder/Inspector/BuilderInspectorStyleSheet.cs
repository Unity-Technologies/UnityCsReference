// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;
using UnityEditor;

namespace Unity.UI.Builder
{
    internal class BuilderInspectorStyleSheet : IBuilderInspectorSection
    {
        BuilderInspector m_Inspector;
        BuilderSelection m_Selection;

        VisualElement m_StyleSheetSection;
        private BuilderNewSelectorField m_NewSelectorField;
        private VisualElement m_NewSelectorHelpTipsContainer;

        static readonly string kHelpTooltipPath = BuilderConstants.UIBuilderPackagePath + "/Explorer/BuilderStyleSheetsNewSelectorHelpTips.uxml";
        private static readonly string kNewSelectorHelpTipsContainerName = "new-selector-help-tips-container";

        public VisualElement root => m_StyleSheetSection;

        StyleSheet styleSheet => m_Inspector.styleSheet;
        VisualElement currentVisualElement => m_Inspector.currentVisualElement;

        public BuilderInspectorStyleSheet(BuilderInspector inspector)
        {
            m_Inspector = inspector;
            m_Selection = inspector.selection;

            m_StyleSheetSection = m_Inspector.Q("shared-styles-controls");
            m_NewSelectorField = m_Inspector.Q<BuilderNewSelectorField>("new-selector-field");
            m_NewSelectorHelpTipsContainer = m_Inspector.Q<VisualElement>(kNewSelectorHelpTipsContainerName);

            m_NewSelectorField.RegisterCallback<NewSelectorSubmitEvent>(OnCreateNewSelector);
            var helpTooltipTemplate = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(kHelpTooltipPath);
            var helpTooltipContainer = helpTooltipTemplate.CloneTree();
            m_NewSelectorHelpTipsContainer.Add(helpTooltipContainer);
        }

        void OnCreateNewSelector(NewSelectorSubmitEvent evt)
        {
            CreateNewSelector(evt.selectorStr);
        }

        void CreateNewSelector(string newSelectorString)
        {
            Undo.RegisterCompleteObjectUndo(
                styleSheet, BuilderConstants.AddNewSelectorUndoMessage);

            if (!SelectorUtility.TryCreateSelector(newSelectorString, out var complexSelector, out var error))
            {
                Builder.ShowWarning(error);
                return;
            }

            BuilderSharedStyles.CreateNewSelector(
                currentVisualElement.parent, styleSheet, newSelectorString);

            m_Selection.NotifyOfHierarchyChange(m_Inspector);
            m_Selection.NotifyOfStylingChange(m_Inspector);
        }

        public void Refresh()
        {
            // Do nothing.
        }

        public void Enable()
        {
            // Do nothing.
        }

        public void Disable()
        {
            // Do nothing.
        }
    }
}
