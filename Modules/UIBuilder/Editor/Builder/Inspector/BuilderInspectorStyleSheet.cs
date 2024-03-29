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
        TextField m_NewSelectorNameNameField;
        Button m_AddNewSelectorButton;
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
            m_NewSelectorNameNameField = m_Inspector.Q<TextField>("add-new-selector-field");
            m_AddNewSelectorButton = m_Inspector.Q<Button>("add-new-selector-button");
            m_NewSelectorHelpTipsContainer = m_Inspector.Q<VisualElement>(kNewSelectorHelpTipsContainerName);

            m_AddNewSelectorButton.clickable.clicked += CreateNewSelector;
            m_NewSelectorNameNameField.RegisterValueChangedCallback(OnCreateNewSelector);
            m_NewSelectorNameNameField.isDelayed = true;
            var helpTooltipTemplate = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(kHelpTooltipPath);
            var helpTooltipContainer = helpTooltipTemplate.CloneTree();
            m_NewSelectorHelpTipsContainer.Add(helpTooltipContainer);
        }

        void OnCreateNewSelector(ChangeEvent<string> evt)
        {
            CreateNewSelector(evt.newValue);
        }

        void CreateNewSelector()
        {
            if (string.IsNullOrEmpty(m_NewSelectorNameNameField.value))
                return;

            CreateNewSelector(m_NewSelectorNameNameField.value);
        }

        void CreateNewSelector(string newSelectorString)
        {
            m_NewSelectorNameNameField.SetValueWithoutNotify(string.Empty);

            Undo.RegisterCompleteObjectUndo(
                styleSheet, BuilderConstants.AddNewSelectorUndoMessage);

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
