// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.UIToolkit.Editor;
using UnityEngine.UIElements;
using UnityEditor;

namespace Unity.UI.Builder
{
    internal class BuilderInspectorStyleSheet : IBuilderInspectorSection
    {
        BuilderInspector m_Inspector;
        BuilderSelection m_Selection;

        VisualElement m_StyleSheetSection;
        private NewSelectorField m_NewSelectorField;

        public VisualElement root => m_StyleSheetSection;

        StyleSheet styleSheet => m_Inspector.styleSheet;
        VisualElement currentVisualElement => m_Inspector.currentVisualElement;

        public BuilderInspectorStyleSheet(BuilderInspector inspector)
        {
            m_Inspector = inspector;
            m_Selection = inspector.selection;

            m_StyleSheetSection = m_Inspector.Q("shared-styles-controls");
            m_NewSelectorField = m_Inspector.Q<NewSelectorField>("new-selector-field");
            m_NewSelectorField.RegisterCallback<NewSelectorSubmitEvent>(OnCreateNewSelector);
        }

        void OnCreateNewSelector(NewSelectorSubmitEvent evt)
        {
            CreateNewSelector(evt.selectorStr);
        }

        void CreateNewSelector(string newSelectorString)
        {
            Undo.RegisterCompleteObjectUndo(
                styleSheet, BuilderConstants.AddNewSelectorUndoMessage);

            BuilderSharedStyles.CreateNewSelector(currentVisualElement.parent, styleSheet, newSelectorString);

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
