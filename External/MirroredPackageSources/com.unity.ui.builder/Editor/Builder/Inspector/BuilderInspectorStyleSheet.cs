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

        public VisualElement root => m_StyleSheetSection;

        StyleSheet styleSheet => m_Inspector.styleSheet;
        VisualElement currentVisualElement => m_Inspector.currentVisualElement;

        public BuilderInspectorStyleSheet(BuilderInspector inspector)
        {
            m_Inspector = inspector;
            m_Selection = inspector.selection;

            m_StyleSheetSection = m_Inspector.Q("shared-styles-controls");
            m_NewSelectorNameNameField = m_Inspector.Q<TextField>("add-new-selector-field");
            m_NewSelectorNameNameField.RegisterValueChangedCallback(OnCreateNewSelector);
            m_NewSelectorNameNameField.isDelayed = true;
        }

        void OnCreateNewSelector(ChangeEvent<string> evt)
        {
            CreateNewSelector(evt.newValue);
        }

        void CreateNewSelector(string newSelectorString)
        {
            m_NewSelectorNameNameField.SetValueWithoutNotify(string.Empty);

            Undo.RegisterCompleteObjectUndo(
                styleSheet, BuilderConstants.AddNewSelectorUndoMessage);

            if (!SelectorUtility.TryCreateSelector(newSelectorString, out var complexSelector, out var error))
            {
                Builder.ShowWarning(error);
                return;
            }
            
            BuilderSharedStyles.CreateNewSelector(
                currentVisualElement.parent, styleSheet, complexSelector);

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
