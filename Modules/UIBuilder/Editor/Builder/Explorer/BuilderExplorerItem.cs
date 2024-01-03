// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderExplorerItem : VisualElement
    {
        VisualElement m_Container;
        VisualElement m_ReorderZoneAbove;
        VisualElement m_ReorderZoneBelow;
        TextField m_RenameTextField;
        internal List<Label> elidableLabels = new();

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
        }

        public void ActivateRenameElementMode()
        {
            var documentElement = GetProperty(BuilderConstants.ElementLinkedDocumentVisualElementVEPropertyName) as VisualElement;

            if ((!documentElement.IsPartOfCurrentDocument() || BuilderSharedStyles.IsDocumentElement(documentElement)) &&
                !BuilderSharedStyles.IsSelectorElement(documentElement))
                return;

            SetReorderingZonesEnabled(false);

            FocusOnRenameTextField();
        }

        internal void SetReorderingZonesEnabled(bool value)
        {
            m_ReorderZoneAbove.pickingMode = value ? PickingMode.Position : PickingMode.Ignore;
            m_ReorderZoneBelow.pickingMode = value ? PickingMode.Position : PickingMode.Ignore;
        }

        public bool IsRenamingActive()
        {
            if (m_RenameTextField == null)
                return false;

            return !m_RenameTextField.ClassListContains(BuilderConstants.HiddenStyleClassName);
        }

        void FocusOnRenameTextField()
        {
            var nameLabel = this.Q<Label>(classes: BuilderConstants.ExplorerItemNameLabelClassName);
            var labelContainer = this.Q(classes: BuilderConstants.ExplorerItemSelectorLabelContClassName);

            m_RenameTextField.RemoveFromClassList(BuilderConstants.HiddenStyleClassName);

            nameLabel?.AddToClassList(BuilderConstants.HiddenStyleClassName);
            labelContainer?.AddToClassList(BuilderConstants.HiddenStyleClassName);

            var baseInput = m_RenameTextField.Q(TextField.textInputUssName).Q<TextElement>();
            if (baseInput.focusController != null)
            {
                // Since renameTextfield isn't attached to a panel yet, we are using DoFocusChange() to bypass canGrabFocus.
                baseInput.focusController.DoFocusChange(baseInput);
                baseInput.selectingManipulator.m_SelectingUtilities.OnFocus();
            }
        }

        public TextField CreateRenamingTextField(VisualElement documentElement, Label nameLabel, BuilderSelection selection)
        {
            m_RenameTextField = new TextField()
            {
                name = BuilderConstants.ExplorerItemRenameTextfieldName,
                isDelayed = true
            };
            m_RenameTextField.AddToClassList(BuilderConstants.ExplorerItemRenameTextfieldClassName);

            if (BuilderSharedStyles.IsSelectorElement(documentElement))
            {
                m_RenameTextField.SetValueWithoutNotify(BuilderSharedStyles.GetSelectorString(documentElement));
            }
            else
            {
                m_RenameTextField.SetValueWithoutNotify(
                    string.IsNullOrEmpty(documentElement.name)
                    ? documentElement.typeName
                    : documentElement.name);
            }
            m_RenameTextField.AddToClassList(BuilderConstants.HiddenStyleClassName);

            m_RenameTextField.RegisterCallback<KeyDownEvent>((e) =>
            {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || e.keyCode == KeyCode.Escape)
                {
                    Focus();
                    return;
                }

                e.StopImmediatePropagation();
            });

            m_RenameTextField.RegisterCallback<FocusOutEvent>(e =>
            {
                OnEditTextFinished(documentElement, nameLabel, selection);
            });

            m_RenameTextField.RegisterCallback<MouseUpEvent>(e =>
            {
                // Stop propagation when clicking on the text field so we don't get back focus to the TreeView
                e.StopImmediatePropagation();
            });

            return m_RenameTextField;
        }

        void OnEditTextFinished(VisualElement documentElement, Label nameLabel,
            BuilderSelection selection)
        {
            var vea = documentElement.GetVisualElementAsset();
            var value = m_RenameTextField.text ?? documentElement.name;

            if (documentElement.IsSelector())
            {
                value = value.Trim();

                var stylesheet = documentElement.GetStyleSheet();

                if (!string.IsNullOrEmpty(m_RenameTextField.text))
                {
                    if (!BuilderNameUtilities.styleSelectorRegex.IsMatch(value))
                    {
                        Builder.ShowWarning(string.Format(BuilderConstants.StyleSelectorValidationSpacialCharacters, "Name"));
                        m_RenameTextField.schedule.Execute(() =>
                        {
                            FocusOnRenameTextField();
                            m_RenameTextField.SetValueWithoutNotify(value);
                        });
                        return;
                    }

                    BuilderSharedStyles.SetSelectorString(documentElement, stylesheet, value);
                }

                selection.NotifyOfStylingChange();
            }
            else
            {
                if (!string.IsNullOrEmpty(m_RenameTextField.text))
                {
                    value = value.Trim();
                    value = value.TrimStart('#');
                    if (!BuilderNameUtilities.attributeRegex.IsMatch(value))
                    {
                        Builder.ShowWarning(string.Format(BuilderConstants.AttributeValidationSpacialCharacters, "Name"));
                        m_RenameTextField.schedule.Execute(() =>
                        {
                            FocusOnRenameTextField();
                            m_RenameTextField.SetValueWithoutNotify(value);
                        });
                        return;
                    }

                    nameLabel.text = BuilderConstants.UssSelectorNameSymbol + value;
                }
                else
                {
                    nameLabel.text = m_RenameTextField.text;
                }

                // Update Uxml asset
                documentElement.name = value;
                vea.SetAttribute("name", value);

                // Update SerializedData
                var desc = UxmlSerializedDataRegistry.GetDescription(vea.fullTypeName);
                if (desc != null)
                {
                    if (vea.serializedData == null)
                        vea .serializedData = desc.CreateDefaultSerializedData();

                    var attribute = desc.FindAttributeWithPropertyName("name");
                    attribute.SetSerializedValue(vea.serializedData, value);
                    attribute.SetSerializedValueAttributeFlags(vea.serializedData, UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml);
                }
            }

            selection.NotifyOfHierarchyChange();
        }

        public VisualElement row()
        {
            return parent.parent;
        }
    }
}
