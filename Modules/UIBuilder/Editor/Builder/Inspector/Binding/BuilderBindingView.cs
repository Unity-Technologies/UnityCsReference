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
        class BindingTypeComparer : IComparer<BindingType>
        {
            public int Compare(BindingType x, BindingType y)
            {
                // Ensure that DataBinding is the first entry.
                if (x.type == typeof(DataBinding))
                    return -1;
                return y.type == typeof(DataBinding)
                    ? 1
                    : string.Compare(x.displayName, y.displayName, StringComparison.Ordinal);
            }
        }

        struct BindingType
        {
            public Type type;
            public string uxmlFullName;
            public string displayName;
        }

        private const string k_UssClassName = "unity-builder-binding-view";

        private static readonly BindingType[] k_UxmlBindingTypes;
        private static readonly List<string> k_UxmlBindingTypeDisplayNames = new();

        static BuilderBindingView()
        {
            k_UxmlBindingTypes = LoadAllAvailableBindingClasses();
        }

        static int IndexOfBindingType(string uxmlFullName)
        {
            for (var i = 0; i < k_UxmlBindingTypes.Length; ++i)
            {
                if (string.CompareOrdinal(k_UxmlBindingTypes[i].uxmlFullName, uxmlFullName) == 0)
                    return i;
            }

            return -1;
        }


        private BuilderInspector m_Inspector;
        private bool m_IsCreatingBinding;

        // Make it internal for tests
        internal TextField m_BindingIdField;
        internal Label m_TargetPropertyTypeName;
        internal DropdownField m_BindingTypeDropdown;
        internal VisualElement m_BindingAttributesContainer;
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
            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(BuilderConstants.UIBuilderPackagePath + "/Inspector/BindingWindow.uxml");
            template.CloneTree(this);

            AddToClassList(k_UssClassName);

            m_BindingIdField = this.Q<TextField>("bindingId__field");
            m_TargetPropertyTypeName = this.Q<Label>("target-property-type-name__label");
            m_BindingTypeDropdown = this.Q<DropdownField>("binding-type__dropdown-field");
            m_BindingTypeDropdown.RegisterValueChangedCallback((e) =>
            {
                UpdateBindingBeingCreatedFromBindingClass();
                Refresh();
            });
            m_BindingAttributesContainer = this.Q("binding-attributes__container");
            m_OkButton = this.Q<Button>("button--ok");
            m_OkButton.clicked += () =>
            {
                OnCreateBindingAccepted();
                closeRequested?.Invoke();
            };
            m_CancelButton = this.Q<Button>("button--cancel");
            m_CancelButton.clicked += CloseView;
            m_AttributesView = new BuilderBindingUxmlAttributesView(m_Inspector,this) {attributesContainer = m_BindingAttributesContainer};

            m_BindingTypeDropdown.choices = k_UxmlBindingTypeDisplayNames;
            m_BindingTypeDropdown.index = 0;

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
        public void StartCreatingOrEditingBinding(string propertyName, bool isCreating, BuilderInspector inspector)
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

        void OnCreateBindingAccepted()
        {
            m_AttributesView.TransferBindingInstance($"{m_AttributesView.serializedRootPath}.bindings", m_Inspector.attributesSection, bindingPropertyName);
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
            m_BindingTypeDropdown.SetEnabled(isCreatingBinding);
            m_OkButton.style.display = isCreatingBinding ? DisplayStyle.Flex : DisplayStyle.None;
            m_CancelButton.text = isCreatingBinding ? "Cancel" : "Close";

            var propertyType = bindableProperty.DeclaredValueType();

            m_BindingIdField.value = bindingPropertyName;
            m_TargetPropertyTypeName.text = TypeUtility.GetTypeDisplayName(propertyType);
            m_TargetPropertyTypeName.tooltip = propertyType?.GetDisplayFullName();
        }

        private static BindingType[] LoadAllAvailableBindingClasses()
        {
            var bindingTypes = new List<BindingType>();
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
                    bindingTypes.Add(new BindingType
                    {
                        type = t,
                        uxmlFullName = description.uxmlFullName,
                        displayName = "Default"
                    });
                }
                else
                {
                    itemText = ObjectNames.NicifyVariableName(description.uxmlName);

                    if (!string.IsNullOrEmpty(t.Namespace))
                        itemText = $"{t.Namespace}/{itemText}";

                    bindingTypes.Add(new BindingType
                    {
                        type = t,
                        uxmlFullName = description.uxmlFullName,
                        displayName = itemText
                    });
                }
            }

            bindingTypes.Sort(new BindingTypeComparer());
            foreach (var bindingType in bindingTypes)
            {
                k_UxmlBindingTypeDisplayNames.Add(bindingType.displayName);
            }
            return bindingTypes.ToArray();
        }

        void UpdateBindingBeingCreatedFromBindingClass()
        {
            if (m_BindingTypeDropdown.index == -1)
                return;

            var uxmlBindingTypeName = k_UxmlBindingTypes[m_BindingTypeDropdown.index];
            var description = UxmlSerializedDataRegistry.GetDescription(uxmlBindingTypeName.uxmlFullName);

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
            var property = m_AttributesView.m_CurrentElementSerializedObject.FindProperty($"{m_AttributesView.serializedRootPath}.bindings");

            var undoMessage = $"Modified {property.name}";
            if (property.m_SerializedObject.targetObject.name != string.Empty)
                undoMessage += $" in {property.m_SerializedObject.targetObject.name}";

            Undo.RegisterCompleteObjectUndo(property.m_SerializedObject.targetObject, undoMessage);

            property.InsertArrayElementAtIndex(property.arraySize);
            var item = property.GetArrayElementAtIndex(property.arraySize - 1);
            item.managedReferenceValue = data;
            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();

            var uxmlObjectPropertyPath = item.propertyPath;
            m_AttributesView.bindingSerializedPropertyRootPath = uxmlObjectPropertyPath;
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

            var bindingTypeIndex = IndexOfBindingType(description.uxmlFullName);
            var choice = k_UxmlBindingTypes[bindingTypeIndex].displayName;
            m_BindingTypeDropdown.SetValueWithoutNotify(choice);

            m_AttributesView.SetAttributesOwner(m_Inspector.document.visualTreeAsset, currentVisualElement);

            // Find the binding
            var bindingsPath = $"{m_Inspector.attributesSection.serializedRootPath}.bindings";
            var bindingsProperty = m_Inspector.attributesSection.m_CurrentElementSerializedObject.FindProperty(bindingsPath);
            for (var i = 0; i < bindingsProperty.arraySize; i++)
            {
                var item = bindingsProperty.GetArrayElementAtIndex(i);
                var propertyName = item.FindPropertyRelative("property");
                if (propertyName.stringValue == bindingPropertyName)
                {
                    m_AttributesView.bindingSerializedPropertyRootPath = item.propertyPath;
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
            var validBinding = m_AttributesView.m_CurrentElementSerializedObject.FindProperty(m_AttributesView.bindingSerializedPropertyRootPath)?.managedReferenceValue != null;
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
                var validBinding = m_AttributesView.m_CurrentElementSerializedObject.FindProperty(m_AttributesView.bindingSerializedPropertyRootPath)?.managedReferenceValue != null;
                if (selectedVisualElement != null && validBinding)
                {
                    var currentVea = selectedVisualElement.GetVisualElementAsset();

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
