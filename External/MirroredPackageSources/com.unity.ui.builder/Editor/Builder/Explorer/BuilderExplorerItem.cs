using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor;

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

            if ((!documentElement.IsPartOfCurrentDocument() ||
                 BuilderSharedStyles.IsDocumentElement(documentElement)) &&
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
            if (IsRenamingActive())
            {
                m_RenameTextField.Focus();
                return;
            }

            var nameLabel = this.Q<Label>(classes: BuilderConstants.ExplorerItemNameLabelClassName);
            var labelContainer = this.Q(classes: BuilderConstants.ExplorerItemSelectorLabelContClassName);

            m_RenameTextField.RemoveFromClassList(BuilderConstants.HiddenStyleClassName);

            nameLabel?.AddToClassList(BuilderConstants.HiddenStyleClassName);
            labelContainer?.AddToClassList(BuilderConstants.HiddenStyleClassName);

            m_RenameTextField.RegisterCallback<GeometryChangedEvent>(OnRenameTextFieldGeometryChanged);
        }

        private void OnRenameTextFieldGeometryChanged(GeometryChangedEvent evt)
        {
            m_RenameTextField.UnregisterCallback<GeometryChangedEvent>(OnRenameTextFieldGeometryChanged);
            m_RenameTextField.delegatesFocus = true;
            m_RenameTextField.Focus();

            var typeLabel = this.Q<Label>(classes: BuilderConstants.ElementTypeClassName);
            if (m_RenameTextField.text == string.Empty && typeLabel != null)
            {
                m_RenameTextField.text = typeLabel.text;
            }
        }

        public void ResetRenamingField()
        {
            var documentElement =
                GetProperty(BuilderConstants.ElementLinkedDocumentVisualElementVEPropertyName) as VisualElement;
            SetRenameTextFieldValueFromDocumentElement(documentElement);
            m_RenameTextField.textEdition.SaveValueAndText();
        }

        private void SetRenameTextFieldValueFromDocumentElement(VisualElement documentElement)
        {
            if (BuilderSharedStyles.IsSelectorElement(documentElement))
            {
                m_RenameTextField.SetValueWithoutNotify(BuilderSharedStyles.GetSelectorString(documentElement));
            }
            else
            {
                m_RenameTextField.SetValueWithoutNotify(documentElement.name);
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

            SetRenameTextFieldValueFromDocumentElement(documentElement);

            m_RenameTextField.AddToClassList(BuilderConstants.HiddenStyleClassName);

            m_RenameTextField.RegisterCallback<KeyDownEvent>((e) =>
            {
                if (e.character == '\n')
                {
                    // Ignoring the second keydown evt sent because it will cause the textfield
                    // to lose focus when it just received it.
                    e.StopPropagation();
                }
            }, TrickleDown.TrickleDown);

            m_RenameTextField.RegisterCallback<FocusOutEvent>(e =>
            {
                if (!IsRenamingActive())
                    return;

                OnEditTextFinished(documentElement, nameLabel, selection);
            });

            m_RenameTextField.RegisterCallback<MouseUpEvent>(e =>
            {
                // Stop propagation when clicking on the text field so we don't get back focus to the TreeView
                e.StopImmediatePropagation();
            });

            // When escaping to cancel rename, we don't want to refocus on parent element, we want to refocus on the TreeView.
            m_RenameTextField.textEdition.MoveFocusToCompositeRoot = null;

            return m_RenameTextField;
        }

        internal bool IsRenameTextValid()
        {
            var documentElement = GetProperty(BuilderConstants.ElementLinkedDocumentVisualElementVEPropertyName) as VisualElement;
            var value = m_RenameTextField.text ?? documentElement.name;

            if (documentElement.IsSelector())
            {
                value = value.Trim();
                return BuilderNameUtilities.styleSelectorRegex.IsMatch(value);
            }

            value = value.Trim();
            value = value.TrimStart('#');
            return BuilderNameUtilities.attributeRegex.IsMatch(value);
        }

        void OnEditTextFinished(VisualElement documentElement, Label nameLabel,
            BuilderSelection selection)
        {
            var vea = documentElement.GetVisualElementAsset();
            var value = m_RenameTextField.text ?? documentElement.name;

            if (!IsRenameTextValid() && (selection.isEmpty || selection.selectionCount > 1 || selection.GetFirstSelectedElement() != documentElement))
            {
                // Selection changed while renaming and renaming is invalid. Cancel renaming.
                m_RenameTextField.AddToClassList(BuilderConstants.HiddenStyleClassName);
                if (documentElement.IsSelector())
                {
                    Builder.ShowWarning(string.Format(BuilderConstants.StyleSelectorValidationSpacialCharacters, "Name"));
                    selection.NotifyOfStylingChange();
                }
                else
                {
                    Builder.ShowWarning(string.Format(BuilderConstants.AttributeValidationSpacialCharacters, "Name"));
                }
                selection.NotifyOfHierarchyChange();
                return;
            }

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

                    if (!BuilderSharedStyles.SetSelectorString(documentElement, stylesheet, value, out var error))
                    {
                        Builder.ShowWarning(error);
                        m_RenameTextField.schedule.Execute(() =>
                        {
                            FocusOnRenameTextField();
                            m_RenameTextField.SetValueWithoutNotify(value);
                        });
                        return;
                    }
                    
                    var styleSheet = documentElement.GetClosestStyleSheet();
                    Undo.RegisterCompleteObjectUndo(
                        styleSheet, BuilderConstants.RenameSelectorUndoMessage);
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
                
                // We get the VTA from the documentRootElement.
                Undo.RegisterCompleteObjectUndo(
                    selection.documentRootElement.GetVisualTreeAsset(), BuilderConstants.RenameUIElementUndoMessage);

                documentElement.name = value;
                vea.SetAttribute("name", value);
            }

            m_RenameTextField.AddToClassList(BuilderConstants.HiddenStyleClassName);
            selection.NotifyOfHierarchyChange();
        }

        public VisualElement row()
        {
            return parent.parent;
        }
    }
}
