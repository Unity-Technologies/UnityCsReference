// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Profiling;
using Unity.Properties;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderInspectorAttributes : BuilderUxmlAttributesView, IBuilderInspectorSection
    {
        BuilderSelection m_Selection;

        public VisualElement root => fieldsContainer;

        // ReSharper disable MemberCanBePrivate.Global
        internal const string inspectorAttributeRefreshMarkerName = "BuilderInspectorAttributes.Refresh";
        // ReSharper restore MemberCanBePrivate.Global

        static readonly ProfilerMarker k_RefreshMarker = new (inspectorAttributeRefreshMarkerName);

        public BuilderInspectorAttributes(BuilderInspector inspector) : base(inspector)
        {
            this.inspector = inspector;
            m_Selection = inspector.selection;
            fieldsContainer = inspector.Q<PersistedFoldout>("inspector-attributes-foldout");
        }

        /// <inheritdoc/>
        protected override bool IsAttributeIgnored(UxmlAttributeDescription attribute)
        {
            return base.IsAttributeIgnored(attribute) || (attribute.name is "data-source" or "data-source-type" or "data-source-path");
        }

        public override void Refresh()
        {
            using var marker = k_RefreshMarker.Auto();

            var currentVisualElement = inspector.currentVisualElement;

            base.Refresh();

            if (currentVisualElement != null && m_Selection.selectionType == BuilderSelectionType.ElementInTemplateInstance &&
                string.IsNullOrEmpty(currentVisualElement.name))
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
                .ForEach((e) => inspector.AddFocusable(e));
        }

        internal override void UpdateAttributeOverrideStyle(VisualElement fieldElement)
        {
            base.UpdateAttributeOverrideStyle(fieldElement);

            var hasAnyBoundField = fieldsContainer.Q(className: BuilderConstants.InspectorLocalStyleBindingClassName) != null
                                   || fieldsContainer.Q(className: BuilderConstants.InspectorLocalStyleUnresolvedBindingClassName) != null;

            fieldsContainer.EnableInClassList(BuilderConstants.InspectorCategoryFoldoutBindingClassName, hasAnyBoundField);

            var hasOverriddenField = BuilderInspectorUtilities.HasOverriddenField(fieldsContainer);
            fieldsContainer.EnableInClassList(BuilderConstants.InspectorCategoryFoldoutOverrideClassName, hasOverriddenField);
        }

        public void Enable()
        {
            fieldsContainer.contentContainer.SetEnabled(true);
        }

        public void Disable()
        {
            fieldsContainer.contentContainer.SetEnabled(false);
        }

        protected override void UpdateFieldStatus(VisualElement fieldElement)
        {
            inspector.UpdateFieldStatus(fieldElement, null);

            var hasOverriddenField = BuilderInspectorUtilities.HasOverriddenField(fieldsContainer);
            fieldsContainer.EnableInClassList(BuilderConstants.InspectorCategoryFoldoutOverrideClassName, hasOverriddenField);
        }

        protected override void NotifyAttributesChanged(string attributeName = null)
        {
            var changeType = attributeName == BuilderConstants.UxmlNameAttr ? BuilderHierarchyChangeType.ElementName : BuilderHierarchyChangeType.Attributes;
            m_Selection.NotifyOfHierarchyChange(inspector, inspector.currentVisualElement, changeType);
        }

        protected override void BuildAttributeFieldContextualMenu(DropdownMenu menu, BuilderStyleRow styleRow)
        {
            if (styleRow != null)
            {
                if (m_Selection.selectionType != BuilderSelectionType.ElementInTemplateInstance)
                {
                    var fieldElement = styleRow.GetLinkedFieldElements()[0]; // Assume default case of only 1 field per row.
                    var csPropertyName = fieldElement.GetProperty(BuilderConstants.InspectorAttributeBindingPropertyNameVEPropertyName) as string;
                    var container = currentElement;
                    var isBindableElement = UxmlSerializedDataRegistry.GetDescription(attributesUxmlOwner.fullTypeName) != null;
                    var isBindableProperty = PropertyContainer.IsPathValid(ref container, csPropertyName);

                    // Do show binding related actions if the underlying property is not bindable or if the element is
                    // not using the new serialization system to define attributes.
                    if (isBindableElement && isBindableProperty)
                    {
                        var hasDataBinding = false;
                        var vea = inspector.currentVisualElement.GetVisualElementAsset();

                        if (vea != null)
                        {
                            hasDataBinding = BuilderBindingUtility.TryGetBinding(csPropertyName, out _, out _);
                        }

                        if (hasDataBinding)
                        {
                            menu.AppendAction(BuilderConstants.ContextMenuEditBindingMessage,
                                (a) => BuilderBindingUtility.OpenBindingWindowToEdit(csPropertyName, inspector),
                                (a) => DropdownMenuAction.Status.Normal,
                                fieldElement);
                            menu.AppendAction(BuilderConstants.ContextMenuRemoveBindingMessage,
                                (a) => { BuilderBindingUtility.DeleteBinding(fieldElement, csPropertyName); },
                                (a) => DropdownMenuAction.Status.Normal,
                                fieldElement);

                            DataBindingUtility.TryGetLastUIBindingResult(new BindingId(csPropertyName), inspector.currentVisualElement,
                                out var bindingResult);

                            if (bindingResult.status == BindingStatus.Success)
                            {
                                menu.AppendAction(BuilderConstants.ContextMenuEditInlineValueMessage,
                                    (a) => { inspector.EnableInlineValueEditing(fieldElement); },
                                    (a) => DropdownMenuAction.Status.Normal,
                                    fieldElement);
                            }

                            menu.AppendAction(
                                BuilderConstants.ContextMenuUnsetInlineValueMessage,
                                (a) =>
                                {
                                    inspector.UnsetBoundFieldInlineValue(a);
                                },
                                action =>
                                {
                                    var attributeDescription = fieldElement.GetLinkedAttributeDescription();
                                    var attributeName = attributeDescription.name;

                                    return (attributesUxmlOwner != null && attributesUxmlOwner.HasAttribute(attributeName))
                                        ? DropdownMenuAction.Status.Normal
                                        : DropdownMenuAction.Status.Disabled;
                                },
                                fieldElement);
                        }
                        else
                        {
                            menu.AppendAction(BuilderConstants.ContextMenuAddBindingMessage,
                                (a) => BuilderBindingUtility.OpenBindingWindowToCreate(csPropertyName, inspector),
                                (a) => DropdownMenuAction.Status.Normal,
                                fieldElement);
                        }

                        menu.AppendSeparator();
                    }
                }

                base.BuildAttributeFieldContextualMenu(menu, styleRow);
            }
        }

        protected override BuilderStyleRow CreateSerializedAttributeRow(UxmlSerializedAttributeDescription attribute, string propertyPath,
            VisualElement parent = null)
        {
            var row = base.CreateSerializedAttributeRow(attribute, propertyPath, parent);
            var propertyField = row.Q<PropertyField>();
            propertyField?.RegisterCallback<SerializedPropertyBindEvent>(OnSerializedPropertyBindCallback);

            return row;
        }

        private void OnSerializedPropertyBindCallback(SerializedPropertyBindEvent e)
        {
            var targetVE = e.target as VisualElement;
            var attributeField = targetVE.GetFirstAncestorOfType<UxmlSerializedDataAttributeField>();

            inspector.RegisterFieldToInlineEditingEvents(attributeField);
            targetVE.UnregisterCallback<SerializedPropertyBindEvent>(OnSerializedPropertyBindCallback);
        }
    }
}
