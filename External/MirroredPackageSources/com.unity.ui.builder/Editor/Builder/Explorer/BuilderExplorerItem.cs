using System;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;

namespace Unity.UI.Builder
{
    internal class BuilderExplorerItem : VisualElement
    {
        VisualElement m_Container;
        VisualElement m_ReorderZoneAbove;
        VisualElement m_ReorderZoneBelow;

        public override VisualElement contentContainer => m_Container == null ? this : m_Container;

        public VisualElement reorderZoneAbove => m_ReorderZoneAbove;
        public VisualElement reorderZoneBelow => m_ReorderZoneBelow;

        public BuilderExplorerItem()
        {
            // Load Template
            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(
                BuilderConstants.UIBuilderPackagePath + "/Explorer/BuilderExplorerItem.uxml");
            template.CloneTree(this);

            m_Container = this.Q("content-container");

            m_ReorderZoneAbove = this.Q("reorder-zone-above");
            m_ReorderZoneBelow = this.Q("reorder-zone-below");

            m_ReorderZoneAbove.userData = this;
            m_ReorderZoneBelow.userData = this;

            this.RegisterCallback<MouseDownEvent>(e => OnMouseDownEventForRename(e));
        }

        void OnMouseDownEventForRename(MouseDownEvent e)
        {
            if (e.clickCount != 2 || e.button != (int)MouseButton.LeftMouse || e.target == null)
                return;

            ActivateRenameElementMode();

            e.PreventDefault();
        }

        public void ActivateRenameElementMode()
        {
            var documentElement = GetProperty(BuilderConstants.ElementLinkedDocumentVisualElementVEPropertyName) as VisualElement;
            if (!documentElement.IsPartOfCurrentDocument() || BuilderSharedStyles.IsDocumentElement(documentElement))
                return;

            FocusOnRenameTextField();
        }

        void FocusOnRenameTextField()
        {
            var renameTextfield = this.Q<TextField>(BuilderConstants.ExplorerItemRenameTextfieldName);
            var nameLabel = this.Q<Label>(classes: BuilderConstants.ExplorerItemNameLabelClassName);

            renameTextfield.RemoveFromClassList(BuilderConstants.HiddenStyleClassName);

            if (nameLabel != null)
                nameLabel.AddToClassList(BuilderConstants.HiddenStyleClassName);

            var baseInput = renameTextfield.Q(TextField.textInputUssName);
            if (baseInput.focusController != null)
                baseInput.focusController.DoFocusChange(baseInput);

            renameTextfield.SelectAll();
        }

        public TextField CreateRenamingTextField(VisualElement documentElement, Label nameLabel, BuilderSelection selection)
        {
            var renameTextfield = new TextField()
            {
                name = BuilderConstants.ExplorerItemRenameTextfieldName,
                isDelayed = true
            };
            renameTextfield.AddToClassList(BuilderConstants.ExplorerItemRenameTextfieldClassName);

            renameTextfield.SetValueWithoutNotify(
                string.IsNullOrEmpty(documentElement.name)
                    ? documentElement.typeName
                    : documentElement.name);
            renameTextfield.AddToClassList(BuilderConstants.HiddenStyleClassName);

            renameTextfield.RegisterCallback<KeyUpEvent>((e) =>
            {
                e.StopImmediatePropagation();
            });

            renameTextfield.RegisterCallback<FocusOutEvent>(e =>
            {
                OnEditTextFinished(documentElement, renameTextfield, nameLabel, selection);
            });

            return renameTextfield;
        }

        void OnEditTextFinished(VisualElement documentElement, TextField renameTextfield, Label nameLabel,
            BuilderSelection selection)
        {
            var vea = documentElement.GetVisualElementAsset();
            var value = renameTextfield.text ?? documentElement.name;

            if (!string.IsNullOrEmpty(renameTextfield.text))
            {
                value = value.Trim();
                value = value.TrimStart('#');
                if (!BuilderNameUtilities.attributeRegex.IsMatch(value))
                {
                    Builder.ShowWarning(string.Format(BuilderConstants.AttributeValidationSpacialCharacters, "Name"));
                    renameTextfield.schedule.Execute(() =>
                    {
                        FocusOnRenameTextField();
                        renameTextfield.SetValueWithoutNotify(value);
                    });
                    return;
                }

                nameLabel.text = BuilderConstants.UssSelectorNameSymbol + value;
            }
            else
            {
                nameLabel.text = renameTextfield.text;
            }

            documentElement.name = value;
            vea.SetAttributeValue("name", value);

            selection.NotifyOfHierarchyChange();
        }

        public VisualElement row()
        {
            return parent.parent;
        }
    }
}
