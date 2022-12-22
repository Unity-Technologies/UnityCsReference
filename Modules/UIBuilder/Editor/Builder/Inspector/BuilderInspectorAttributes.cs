// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderInspectorAttributes : BuilderUxmlAttributesView, IBuilderInspectorSection
    {
        BuilderInspector m_Inspector;

        public VisualElement root => fieldsContainer;

        public BuilderInspectorAttributes(BuilderInspector inspector)
        {
            m_Inspector = inspector;
            fieldsContainer = m_Inspector.Q<PersistedFoldout>("inspector-attributes-foldout");
        }

        public override void Refresh()
        {
            base.Refresh();

            if (currentElement != null && m_Inspector.selection.selectionType == BuilderSelectionType.ElementInTemplateInstance &&
                string.IsNullOrEmpty(currentElement.name))
            {
                var helpBox = new HelpBox();
                helpBox.AddToClassList(BuilderConstants.InspectorClassHelpBox);
                helpBox.text = BuilderConstants.NoNameElementAttributes;
                fieldsContainer.Insert(0, helpBox);
            }

            // Forward focus to the panel header.
            fieldsContainer
                .Query()
                .Where(e => e.focusable)
                .ForEach((e) => m_Inspector.AddFocusable(e));
        }

        public void Enable()
        {
            fieldsContainer.contentContainer.SetEnabled(true);
        }

        public void Disable()
        {
            fieldsContainer.contentContainer.SetEnabled(false);
        }

        protected override void UpdateFieldStatus(BindableElement fieldElement)
        {
            m_Inspector.UpdateFieldStatus(fieldElement, null);

            var hasOverriddenField = BuilderInspectorUtilities.HasOverriddenField(fieldsContainer);
            fieldsContainer.EnableInClassList(BuilderConstants.InspectorCategoryFoldoutOverrideClassName, hasOverriddenField);
        }

        protected override void NotifyAttributesChanged()
        {
            m_Inspector.selection.NotifyOfHierarchyChange(m_Inspector);
        }
    }
}
