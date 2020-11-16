using UnityEngine.UIElements;
using UnityEditor;

namespace Unity.UI.Builder
{
    internal class BuilderInspectorSelector : IBuilderInspectorSection
    {
        BuilderInspector m_Inspector;
        BuilderSelection m_Selection;

        PersistedFoldout m_StyleSelectorSection;
        TextField m_StyleSelectorNameField;

        public VisualElement root => m_StyleSelectorSection;

        StyleSheet styleSheet => m_Inspector.styleSheet;
        VisualElement currentVisualElement => m_Inspector.currentVisualElement;

        public BuilderInspectorSelector(BuilderInspector inspector)
        {
            m_Inspector = inspector;
            m_Selection = inspector.selection;

            m_StyleSelectorSection = m_Inspector.Q<PersistedFoldout>("shared-style-selector-controls");
            m_StyleSelectorNameField = m_StyleSelectorSection.Q<TextField>("rename-selector-field");
            m_StyleSelectorNameField.isDelayed = true;
            m_StyleSelectorNameField.RegisterValueChangedCallback(OnStyleSelectorNameChange);
        }

        void OnStyleSelectorNameChange(ChangeEvent<string> evt)
        {
            if (m_Selection.selectionType != BuilderSelectionType.StyleSelector)
                return;

            if (evt.newValue == evt.previousValue)
                return;

            Undo.RegisterCompleteObjectUndo(
                styleSheet, BuilderConstants.RenameSelectorUndoMessage);

            BuilderSharedStyles.SetSelectorString(currentVisualElement, styleSheet, evt.newValue);

            m_Selection.NotifyOfHierarchyChange(m_Inspector);
            m_Selection.NotifyOfStylingChange(m_Inspector);
        }

        public void Refresh()
        {
            // Bind the style selector controls.
            if (m_Selection.selectionType == BuilderSelectionType.StyleSelector || m_Selection.selectionType == BuilderSelectionType.ParentStyleSelector)
                m_StyleSelectorNameField.SetValueWithoutNotify(BuilderSharedStyles.GetSelectorString(currentVisualElement));
        }

        public void Enable()
        {
            m_StyleSelectorNameField.SetEnabled(true);
        }

        public void Disable()
        {
            m_StyleSelectorNameField.SetEnabled(false);
        }
    }
}
