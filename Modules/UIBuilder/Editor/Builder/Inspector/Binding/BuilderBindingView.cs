// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    /// <summary>
    /// The content view of the binding window.
    /// </summary>
    internal class BuilderBindingView : VisualElement, IBuilderSelectionNotifier
    {
        private const string k_UssClassName = "unity-builder-binding-view";

        private List<string> m_UxmlBindingTypeNames = new();
        private BuilderInspector m_Inspector;
        private bool m_IsCreatingBinding;

        // Make it internal for tests
        internal TextField m_PropertyField;
        internal Label m_TypeLabel;
        internal DropdownField m_BindingTypeField;
        internal VisualElement m_FieldsContainer;
        internal BuilderBindingUxmlAttributesView m_AttributesView;
        internal Button m_OkButton;
        internal Button m_CancelButton;

        /// <summary>
        /// The view that displays attributes of the target binding.
        /// </summary>
        public BuilderBindingUxmlAttributesView attributesView => m_AttributesView;

        /// <summary>
        /// Indicates whether the view is used to either create or edit a binding.
        /// </summary>
        public bool isCreatingBinding
        {
            get => m_IsCreatingBinding;
            private set
            {
                m_IsCreatingBinding = value;
                m_AttributesView.callInitOnValueChange = !value;
            }
        }

        /// <summary>
        /// The property of the binding to create or to edit.
        /// </summary>
        public string bindingPropertyName { get; private set; }

        /// <summary>
        /// The property of the binding to create or to edit.
        /// </summary>
        public IProperty bindableProperty => PropertyContainer.GetProperty(currentVisualElement, new PropertyPath(bindingPropertyName));

        /// <summary>
        /// The VisualElement currently selected.
        /// </summary>
        VisualElement selectedVisualElement => m_Inspector.selectedVisualElement;

        /// <summary>
        /// The VisualElement currently selected or cached if none are selected.
        /// </summary>
        VisualElement currentVisualElement => m_Inspector.currentVisualElement;

        public Action closing;
        public Action closeRequested;

        /// <summary>
        /// Constructs the view.
        /// </summary>
        public BuilderBindingView()
        {
            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(
                BuilderConstants.UIBuilderPackagePath + "/Inspector/BindingWindow.uxml");
            template.CloneTree(this);

            AddToClassList(k_UssClassName);

            m_PropertyField = this.Q<TextField>("propertyField");
            m_TypeLabel = this.Q<Label>("typeLabel");
            m_BindingTypeField = this.Q<DropdownField>("bindingTypeField");
            m_BindingTypeField.RegisterValueChangedCallback((e) =>
            {
                if (!isCreatingBinding)
                    return;

                UpdateBindingBeingCreatedFromBindingClass();
                Refresh();
            });
            m_FieldsContainer = this.Q("fieldsContainer");
            m_OkButton = this.Q<Button>("okButton");
            m_OkButton.clicked += () =>
            {
                OnCreateBindingAccepted();
                closeRequested?.Invoke();
            };
            m_CancelButton = this.Q<Button>("cancelButton");
            m_CancelButton.clicked += CloseView;
            m_AttributesView = new BuilderBindingUxmlAttributesView(m_Inspector,this) {fieldsContainer = m_FieldsContainer};

            LoadAllAvailableBindingClasses();
            RegisterCallback<DetachFromPanelEvent>((e) =>
            {
                if (m_Inspector != null && m_Inspector.selection != null)
                    m_Inspector.selection.RemoveNotifier(this);
            });
        }

        /// <summary>
        /// Sets up the view based on whether the view is used to create or edit a binding and the target property.
        /// </summary>
        /// <param name="propertyName">The name of the property to binding</param>
        /// <param name="isCreating">Indicates whether the view is used to create or edit a binding</param>
        /// <param name="inspector">The inspector of the UI Builder</param>
        private void StartCreatingOrEditingBinding(string propertyName, bool isCreating, BuilderInspector inspector)
        {
            m_Inspector = inspector;
            m_AttributesView.inspector = inspector;
            bindingPropertyName = propertyName;
            isCreatingBinding = isCreating;
            UpdateBinding();
            inspector.selection.AddNotifier(this);
            m_AttributesView.selection = inspector.selection;
            UpdateControls();
        }

        /// <summary>
        /// Starts creating a binding on the specified property.
        /// </summary>
        /// <param name="inspector">The inspector of the UI Builder</param>
        /// <param name="propertyName">The name of the property to bind.</param>
        public void StartCreatingBinding(string propertyName, BuilderInspector inspector)
        {
            StartCreatingOrEditingBinding(propertyName,true,  inspector);
        }

        void OnCreateBindingAccepted()
        {
            m_AttributesView.TransferBindingInstance(m_AttributesView.serializedRootPath + "bindings", m_Inspector.attributesSection, bindingPropertyName);
        }

        /// <summary>
        /// Starts editing the binding on the specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property bound by the binding to edit.</param>
        public void StartEditingBinding(string propertyName, BuilderInspector inspector)
        {
            StartCreatingOrEditingBinding(propertyName, false, inspector);
        }

        /// <summary>
        /// Updates the binding to create based on the selected item in the Binding Type dropdown. Or updates the binding
        /// to edit from the visual element currently selected.
        /// </summary>
        /// <exception cref="ArgumentException">Exception thrown if attempting to create a binding that already exists or to edit binding not found</exception>
        void UpdateBinding()
        {
            string GetErrorMessage()
            {
                return isCreatingBinding ? $"Cannot create binding on {bindingPropertyName}" : $"Cannot edit binding on {bindingPropertyName}";
            }

            BuilderBindingUtility.TryGetBinding(bindingPropertyName, out var binding, out var uxmlBinding);

            // If we are creating a binding on a target property of the selected VisualElement then...
            if (isCreatingBinding)
            {
                // if there is already a binding on the property then throw an error
                if (binding != null)
                    throw new ArgumentException(GetErrorMessage() + ": The property is already bound.");

                UpdateBindingBeingCreatedFromBindingClass();
                Refresh();
            }
            // If we are editing a binding on a target property of the selected VisualElement then ...
            else
            {
                // If there is no binding on the target property then throw an error
                if (binding == null)
                    throw new ArgumentException(GetErrorMessage() + ": The property is not bound yet.");

                ReadViewDataFromBinding(binding, uxmlBinding);
                Refresh();
            }
        }

        /// <summary>
        /// Refreshes the view.
        /// </summary>
        void Refresh()
        {
            m_AttributesView.Refresh();
        }

        private void UpdateControls()
        {
            m_BindingTypeField.SetEnabled(isCreatingBinding);
            m_OkButton.style.display = isCreatingBinding ? DisplayStyle.Flex : DisplayStyle.None;
            m_CancelButton.text = isCreatingBinding ? "Cancel" : "Close";

            var propertyType = bindableProperty.DeclaredValueType();

            m_PropertyField.value = bindingPropertyName;
            m_TypeLabel.text = TypeUtility.GetTypeDisplayName(propertyType);
            m_TypeLabel.tooltip = propertyType?.GetDisplayFullName();
        }

        private void LoadAllAvailableBindingClasses()
        {
            var bindingTypes = new List<string>();

            m_UxmlBindingTypeNames.Clear();

            foreach (var t in TypeCache.GetTypesDerivedFrom<Binding>())
            {
                if (t.IsAbstract)
                    continue;

                if (t.IsGenericType)
                    continue;

                var attributes = t.GetCustomAttributes(typeof(UxmlObjectAttribute), false);
                if (attributes.Length == 0)
                    continue;

                // Probably need to do some mapping to a Uxml type name.
                var description = UxmlSerializedDataRegistry.GetDescription(t.FullName);
                if (null == description)
                    continue;

                string itemText = null;
                // Display 'Default' for DataBinding class and place it first.
                if (t == typeof(DataBinding))
                {
                    itemText = "Default";
                    bindingTypes.Insert(0, itemText);
                    m_UxmlBindingTypeNames.Insert(0, description.uxmlFullName);
                }
                else if (t.IsSubclassOf(typeof(DataBinding)) ||
                         t.IsSubclassOf(typeof(CustomBinding)))
                {
                    itemText = t.Name;

                    if (bindingTypes.Contains(itemText))
                        itemText = t.FullName;

                    bindingTypes.Add(itemText);
                    m_UxmlBindingTypeNames.Add(description.uxmlFullName);
                }
            }

            m_BindingTypeField.choices = bindingTypes;
            m_BindingTypeField.index = 0;
        }

        void UpdateBindingBeingCreatedFromBindingClass()
        {
            if (m_BindingTypeField.index == -1)
                return;

            var uxmlBindingTypeName = m_UxmlBindingTypeNames[m_BindingTypeField.index];
            var description = UxmlSerializedDataRegistry.GetDescription(uxmlBindingTypeName);

            // Create UxmlSerializedData.
            var data = (Binding.UxmlSerializedData)description.CreateDefaultSerializedData();

            var propertyAttribute = description.FindAttributeWithPropertyName(nameof(Binding.property));
            propertyAttribute.SetSerializedValue(data, bindingPropertyName);
            propertyAttribute.SetSerializedValueAttributeFlags(data, UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml);

            var bindingModeAttribute = description.FindAttributeWithPropertyName(nameof(DataBinding.bindingMode));
            if (bindingModeAttribute != null)
            {
                bindingModeAttribute.SetSerializedValue(data, BindingMode.ToTarget);
                bindingModeAttribute.SetSerializedValueAttributeFlags(data, UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml);
            }

            m_AttributesView.SetAttributesOwnerFromCopy(m_Inspector.document.visualTreeAsset, currentVisualElement);

            // Add the Binding serialized data to the current element's binding list.
            var property = m_AttributesView.m_CurrentElementSerializedObject.FindProperty(m_AttributesView.serializedRootPath + "bindings");

            var undoMessage = $"Modified {property.name}";
            if (property.m_SerializedObject.targetObject.name != string.Empty)
                undoMessage += $" in {property.m_SerializedObject.targetObject.name}";

            Undo.RegisterCompleteObjectUndo(property.m_SerializedObject.targetObject, undoMessage);

            property.InsertArrayElementAtIndex(property.arraySize);
            var item = property.GetArrayElementAtIndex(property.arraySize - 1);
            item.managedReferenceValue = data;
            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();

            var uxmlObjectPropertyPath = item.propertyPath;
            m_AttributesView.bindingSerializedPropertyPathRoot = uxmlObjectPropertyPath;
            m_AttributesView.bindingUxmlSerializedDataDescription = description;

            // Update the uxml asset
            using (new BuilderUxmlAttributesView.DisableUndoScope(m_AttributesView))
            {
                if (bindingModeAttribute != null)
                {
                    // need to update the uxmlAsset with the binding mode change
                    var result = m_AttributesView.SynchronizePath(uxmlObjectPropertyPath, true);
                    result.uxmlAsset?.SetAttribute("binding-mode", BindingMode.ToTarget.ToString());
                }
            }
        }

        /// <summary>
        /// Initializes the view from the specified binding.
        /// </summary>
        /// <param name="binding">The binding instance</param>
        /// <param name="uxmlBinding">The binding uxml element</param>
        private void ReadViewDataFromBinding(Binding binding, UxmlObjectAsset uxmlBinding)
        {
            var fullType = uxmlBinding.fullTypeName;
            var description = UxmlSerializedDataRegistry.GetDescription(fullType);
            if (null == description)
                return;

            // Find the binding serializedData
            bindingPropertyName = binding.property;
            m_BindingTypeField.index = m_UxmlBindingTypeNames.IndexOf(description.uxmlFullName);
            m_AttributesView.SetAttributesOwner(m_Inspector.document.visualTreeAsset, currentVisualElement);

            // Find the binding
            var bindingsPath = m_Inspector.attributesSection.serializedRootPath + "bindings";
            var bindingsProperty = m_Inspector.attributesSection.m_CurrentElementSerializedObject.FindProperty(bindingsPath);
            for (var i = 0; i < bindingsProperty.arraySize; i++)
            {
                var item = bindingsProperty.GetArrayElementAtIndex(i);
                var propertyName = item.FindPropertyRelative("property");
                if (propertyName.stringValue == bindingPropertyName)
                {
                    m_AttributesView.bindingSerializedPropertyPathRoot = item.propertyPath;
                    m_AttributesView.bindingUxmlSerializedDataDescription = description;
                    break;
                }
            }
        }

        IVisualElementScheduledItem m_ValidateSelectionScheduledItem;

        /// <inheritdoc/>
        public void SelectionChanged()
        {
            // if we're undoing the creation of the binding window, we need to close the view
            // we also need to wait for the selection to be restored before refreshing the view
            var validBinding = m_AttributesView.m_CurrentElementSerializedObject.FindProperty(m_AttributesView.bindingSerializedPropertyPathRoot)?.managedReferenceValue != null;
            if (Builder.ActiveWindow.isInUndoRedo && validBinding && selectedVisualElement != null)
            {
                m_AttributesView.RefreshAllAttributeOverrideStyles();
                m_ValidateSelectionScheduledItem?.Pause();
                return;
            }

            // Delay the validation of the selection because the selection is cleared and restored on Undo/Redo, which triggers many notifications.
            if (m_ValidateSelectionScheduledItem != null)
            {
                m_ValidateSelectionScheduledItem.Pause();
                m_ValidateSelectionScheduledItem.Resume();
            }
            else
            {
                m_ValidateSelectionScheduledItem = schedule.Execute(ScheduledValidateSelection);
            }
        }

        /// <summary>
        /// Invoked whenever the selection changes to ensure that the current selection is still valid and matches the context in
        /// which this view was prompted.
        /// </summary>
        void ScheduledValidateSelection()
        {
            // Ignore if the window has been closed already
            if (panel == null)
                return;

            try
            {
                var validBinding = m_AttributesView.m_CurrentElementSerializedObject.FindProperty(m_AttributesView.bindingSerializedPropertyPathRoot)?.managedReferenceValue != null;
                if (currentVisualElement != null && validBinding)
                {
                    var currentVea = currentVisualElement.GetVisualElementAsset();

                    // Check if inspector.currentVisualElement has been recreated from its related uxml asset
                    // while the Binding window was opened.
                    if (currentVea == attributesView.currentElement.GetVisualElementAsset())
                    {
                        UpdateBinding();
                    }
                    else
                    {
                        CloseView();
                    }
                }
                else
                {
                    CloseView();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(e.Message);
                CloseView();
            }
        }

        /// <summary>
        /// Closes the window containing this view.
        /// </summary>
        void CloseView()
        {
            closeRequested?.Invoke();
        }

        /// <inheritdoc/>
        public void HierarchyChanged(VisualElement element, BuilderHierarchyChangeType changeType)
        {
        }

        /// <inheritdoc/>
        public void StylingChanged(List<string> styles, BuilderStylingChangeType changeType)
        {
        }

        internal void NotifyAttributesChanged()
        {
            if (isCreatingBinding || m_Inspector == null)
            {
                return;
            }

            if (bindingPropertyName.StartsWith(BuilderConstants.StylePropertyPathPrefix))
            {
                var styleName = BuilderNameUtilities.ConvertStyleCSharpNameToUssName(bindingPropertyName.Substring(BuilderConstants.StylePropertyPathPrefix.Length));
                var modifiedProperties = StringObjectListPool.Get();

                modifiedProperties.Add(styleName);
                try
                {
                    m_Inspector.selection.NotifyOfStylingChange(this, modifiedProperties);
                }
                finally
                {
                    StringObjectListPool.Release(modifiedProperties);
                }
            }
            // needed to refresh the UXML preview with any changes
            m_Inspector.selection.NotifyOfHierarchyChange();
        }
    }
}
