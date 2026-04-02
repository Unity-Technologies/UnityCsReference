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
using Object = System.Object;

namespace Unity.UIToolkit.Editor
{
    sealed class BindingView : UxmlAttributesView
    {
        public enum BindingViewMode
        {
            Create,
            Edit,
            View
        }

        private const string k_InspectorStyleSheet = "UIToolkitAuthoring/Inspector/UIToolkitAuthoringInspector.uss";
        private const string k_InspectorStyleSheetDark = "UIToolkitAuthoring/Inspector/UIToolkitAuthoringInspectorDark.uss";
        private const string k_InspectorStyleSheetLight = "UIToolkitAuthoring/Inspector/UIToolkitAuthoringInspectorLight.uss";

        private const string k_StyleSheet = "UIToolkitAuthoring/Inspector/Binding/BindingView.uss";

        class BindingTypeComparer : IComparer<BindingType>
        {
            public int Compare(BindingType x, BindingType y)
            {
                // Ensure that DataBinding is the first entry.
                if (x.type.IsAssignableFrom(typeof(DataBinding)))
                    return -1;
                return y.type.IsAssignableFrom(typeof(DataBinding))
                    ? 1
                    : string.Compare(x.displayName, y.displayName, StringComparison.Ordinal);
            }
        }

        internal struct BindingType
        {
            public Type type;
            public string uxmlFullName;
            public string displayName;
        }

        public new const string UssClassName = "unity-binding-view";
        private const string k_VisualTreeAssetPath = "UIToolkitAuthoring/Inspector/Binding/BindingView.uxml";
        public const string RootPropertyViewName = "RootPropertyView";
        public const string RootPropertyFieldName = "RootPropertyField";
        public const string BindingPropertyFieldName = "BindingPropertyField";
        public const string ConverterGroupWarningBoxName = "ConverterGroupWarningBox";
        public const string BindingModeFieldName = "BindingModeField";
        public const string AdvancedSettingsContainerName = "AdvancedSettings";
        public const string BindingConvertersToUiFieldName = "BindingConvertersToUiField";
        public const string BindingConvertersToSourceFieldName = "BindingConvertersToSourceField";
        public const string BindingUpdateTriggerFieldName = "BindingUpdateTriggerField";

        private static readonly BindingType[] k_UxmlBindingTypes;
        private static readonly List<string> k_UxmlBindingTypeDisplayNames = new();

        VisualElement m_Element;

        VisualTreeAsset m_TempVisualTreeAsset;
        VisualElement m_TempElement;

        int m_UndoGroupBeforeStartId;
        int m_UndoGroupId;
        bool m_Accepted;

        UxmlSerializedDataPropertyView m_RootPropertyView;
        PropertyField m_RootPropertyField;
        UxmlAttributeField m_BindingPropertyField;
        internal Label m_TargetPropertyTypeName;
        internal DropdownField m_BindingTypeDropdown;
        internal VisualElement m_BindingAttributesContainer;
        internal UxmlSerializedDataPropertyView m_AttributesView;
        VisualElement m_ContentContainer;
        internal Button m_OkButton;
        internal Button m_CancelButton;

        UxmlSerializedDataDescription m_BindingUxmlSerializedDataDescription;
        Binding.UxmlSerializedData m_BindingUxmlSerializedData;

        public Action closing;
        public Action closeRequested;

        /// <summary>
        /// The Content container for the view.
        /// </summary>
        public override VisualElement contentContainer => m_ContentContainer ?? this;

        /// <summary>
        /// Indicates whether the view is used to either create, edit or view a binding.
        /// </summary>
        public BindingViewMode Mode { get; set; }

        /// <summary>
        /// The property of the binding to create or to edit.
        /// </summary>
        public string BindingPropertyName { get; private set; }

        /// <summary>
        /// The property of the binding to create or to edit.
        /// </summary>
        public IProperty BindableProperty => Context?.element != null ? PropertyContainer.GetProperty(Context.element, new PropertyPath(BindingPropertyName)) : null;

        /// <summary>
        /// Constructor for the BindingAttributesView.
        /// </summary>
        public BindingView()
        {
            // Load assets.
            var mainInspectorUSS = EditorGUIUtility.Load(k_InspectorStyleSheet) as StyleSheet;
            var themeInspectorUSSPath = EditorGUIUtility.isProSkin ? k_InspectorStyleSheetDark : k_InspectorStyleSheetLight;
            var themeInspectorUSS = EditorGUIUtility.Load(themeInspectorUSSPath) as StyleSheet;
            var mainUSS = EditorGUIUtility.LoadRequired(k_StyleSheet) as StyleSheet;

            styleSheets.Add(mainInspectorUSS);
            styleSheets.Add(themeInspectorUSS);
            styleSheets.Add(mainUSS);

            AddToClassList(UssClassName);

            var asset = EditorGUIUtility.LoadRequired(k_VisualTreeAssetPath) as VisualTreeAsset;
            var container = asset.CloneTree();

            container.style.flexGrow = 1;
            hierarchy.Add(container);

            m_BindingTypeDropdown = this.Q<DropdownField>("BindingTypeDropdownField");
            m_ContentContainer = container.Q("ContentContainer");
            m_OkButton = this.Q<Button>("ConfirmButton");
            m_OkButton.clicked += () =>
            {
                OnCreateBindingAccepted();
                closeRequested?.Invoke();
            };
            m_CancelButton = this.Q<Button>("CancelButton");
            m_CancelButton.clicked += CloseView;

            Context = new UxmlAttributesEditingContext(new UxmlAttributesEditingController());

            m_BindingTypeDropdown.choices = k_UxmlBindingTypeDisplayNames;
            m_BindingTypeDropdown.index = 0;
            m_BindingTypeDropdown.RegisterValueChangedCallback((e) =>
            {
                UpdateBindingBeingCreatedFromBindingClass();
            });
        }

        static BindingView()
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

         private static BindingType[] LoadAllAvailableBindingClasses()
        {
            var bindingTypes = new List<BindingType>();
            foreach (var t in TypeCache.GetTypesDerivedFrom<Binding>())
            {
                if (t.IsAbstract)
                    continue;

                if (t.IsGenericType)
                    continue;

                var attributes = t.IsDefined(typeof(UxmlObjectAttribute), false);
                if (attributes == false)
                    continue;

                // Probably need to do some mapping to a Uxml type name.
                var description = UxmlSerializedDataRegistry.GetDescription(t.FullName);
                if (null == description)
                    continue;

                string itemText = null;
                // Display 'Default' for DataBinding class and place it first.
                if (t.IsAssignableFrom(typeof(DataBinding)))
                {
                    bindingTypes.Add(new BindingType
                    {
                        type = t,
                        uxmlFullName = description.uxmlFullName,
                        displayName = "Default"
                    });
                }
                // Skip this type, as it requires context to work and should be used by users.
                else if (t.FullName == "Unity.UIToolkit.Editor.StylePropertyBinding")
                {
                    continue;
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
            var m_BindingTypeDropdownIndex = m_BindingTypeDropdown.index;

            if (m_BindingTypeDropdownIndex == -1)
                return;

            ResetContext();

            var uxmlBindingTypeName = k_UxmlBindingTypes[m_BindingTypeDropdownIndex];
            var description = UxmlSerializedDataRegistry.GetDescription(uxmlBindingTypeName.uxmlFullName);

            // Create UxmlSerializedData.
            m_BindingUxmlSerializedData = (Binding.UxmlSerializedData)description.CreateDefaultSerializedData();

            var propertyAttribute = description.FindAttributeWithPropertyName(nameof(Binding.property));
            propertyAttribute.SetSerializedValue(m_BindingUxmlSerializedData, BindingPropertyName, UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml);

            var bindingModeAttribute = description.FindAttributeWithPropertyName(nameof(DataBinding.bindingMode));
            if (bindingModeAttribute != null)
            {
                bindingModeAttribute.SetSerializedValue(m_BindingUxmlSerializedData, BindingMode.ToTarget, UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml);
            }

            // Add the Binding serialized data to the current element's binding list.
            var property = Context.rootSerializedObject.FindProperty($"{Context.serializedBasePath}.bindings");

            var undoMessage = $"Modified {property.name}";
            if (property.serializedObject.targetObject.name != string.Empty)
                undoMessage += $" in {property.serializedObject.targetObject.name}";

            Undo.RegisterCompleteObjectUndo(property.serializedObject.targetObject, undoMessage);

            property.InsertArrayElementAtIndex(property.arraySize);
            var item = property.GetArrayElementAtIndex(property.arraySize - 1);
            item.managedReferenceValue = m_BindingUxmlSerializedData;
            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();

            var uxmlObjectPropertyPath = item.propertyPath;
            m_RootPropertyField.bindingPath = uxmlObjectPropertyPath;
            m_BindingUxmlSerializedDataDescription = description;

            // Recreate the actual bindings
            Context.editingController.DeserializeElement();
        }

        /// <summary>
        /// Closes the window containing this view.
        /// </summary>
        void CloseView()
        {
            closeRequested?.Invoke();
        }

        /// <summary>
        /// Called when the window containing this view is closed. Reverts the changes made to the serialized data if creating a new binding and the creation was not accepted.
        /// </summary>
        public void OnClose()
        {
            if (Mode == BindingViewMode.Create && !m_Accepted)
                Undo.RevertAllDownToGroup(m_UndoGroupBeforeStartId);
            DestroyTempContext();
        }

        public bool Start(VisualElement element, string bindingPath, BindingViewMode mode)
        {
            var type = element.GetType();
            var fullTypeName = type.FullName;

            // Get the UxmlSerializedDataDescription for this type
            var description = UxmlSerializedDataRegistry.GetDescription(fullTypeName);
            if (description == null)
            {
                Debug.LogError("Binding cannot be added or edited for unregistered VisualElement types. Ensure UxmlElement attribute is applied to the type.");
                return false;
            }

            Mode = mode;
            BindingPropertyName = bindingPath;
            m_Element = element;

            // Use the default binding class (DataBinding) if creating a new binding.
            if (mode == BindingViewMode.Create)
            {
                CreateTempContext(m_Element.visualTreeAssetSource, m_Element);
                m_BindingTypeDropdown.index = 0;
                m_UndoGroupBeforeStartId = Undo.GetCurrentGroup();
                Undo.IncrementCurrentGroup();
                m_UndoGroupId = Undo.GetCurrentGroup();
            }

            UpdateBinding();
            UpdateControls();
            m_Accepted = false;
            return true;
        }

        void CreateTempContext(VisualTreeAsset asset, VisualElement visualElement)
        {
            var type = visualElement.GetType();
            var fullTypeName = type.FullName;

            // Destroy the previous context if it exists
            DestroyTempContext();

            // Get the UxmlSerializedDataDescription for this type
            var description = UxmlSerializedDataRegistry.GetDescription(fullTypeName);

            // Work on a copy of the VisualTreeAsset so that we can discard or apply the changes later.
            m_TempVisualTreeAsset = ScriptableObject.CreateInstance<VisualTreeAsset>();
            m_TempVisualTreeAsset.name += "(Binding Copy)";

            // Create a new VisualElementAsset with the same type as the source VisualElement
            var newVisualElementAsset = new VisualElementAsset(fullTypeName);

            // Create default serialized data
            newVisualElementAsset.serializedData = description.CreateDefaultSerializedData();

            // First, copy UXML attributes from the original VisualElementAsset if it exists
            if (visualElement.visualElementAsset != null)
            {
                var originalAsset = visualElement.visualElementAsset;

                // Copy all UXML attribute properties from the original asset
                if (originalAsset.properties != null)
                {
                    foreach (var property in originalAsset.properties)
                    {
                        newVisualElementAsset.SetAttribute(property.name, property.value);
                    }
                }

                // Copy the original serialized data if it exists
                if (originalAsset.serializedData != null)
                {
                    newVisualElementAsset.serializedData = UxmlUtility.CloneObject(originalAsset.serializedData) as UnityEngine.UIElements.UxmlSerializedData;
                }
            }

            // Then sync the serialized data from the VisualElement's runtime state
            // This captures any programmatic changes made at runtime
            description.SyncSerializedData(visualElement, newVisualElementAsset.serializedData);

            // Add the VisualElementAsset to the VisualTreeAsset
            m_TempVisualTreeAsset.visualTree.Add(newVisualElementAsset);

            m_TempElement = newVisualElementAsset.Instantiate(new CreationContext(m_TempVisualTreeAsset));
            m_TempElement.visualTreeAssetSource = m_TempVisualTreeAsset;
            m_TempElement.visualElementAsset = newVisualElementAsset;
        }

        void DestroyTempContext()
        {
            if (m_TempVisualTreeAsset != null)
            {
                ScriptableObject.DestroyImmediate(m_TempVisualTreeAsset);
                m_TempElement = null;
                m_TempVisualTreeAsset = null;
            }
        }

        // Reset the context to prepare for creating a new binding or editing an existing binding.
        void ResetContext()
        {
            Context.Clear();

            if (Mode == BindingViewMode.Create)
            {
                Context.Set(m_TempVisualTreeAsset, m_TempElement);
                Context.rootSerializedObject.Update();
            }
            else
            {
                Context.Set(m_Element, Mode == BindingViewMode.View);
            }
        }

        void OnCreateBindingAccepted()
        {
            Undo.SetCurrentGroupName("Add binding");

            // Set the context to the real serialized data of the element being edited so that the changes made in the view are applied to the real serialized data when deserializing.
            Context.Set(m_Element);

            var attrDesc =
                Context.uxmlSerializedDataDescription.FindAttributeWithPropertyName("bindings") as
                    UxmlSerializedUxmlObjectAttributeDescription;

            var bindingsProperty = Context.rootSerializedObject.FindProperty($"{Context.serializedBasePath}.bindings");

            UxmlAssetUtilities.AddUxmlObjectToSerializedData(Context,
                bindingsProperty, typeof(DataBinding.UxmlSerializedData), m_BindingUxmlSerializedData);

            Undo.CollapseUndoOperations(m_UndoGroupId);
            m_Accepted = true;
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
                return Mode == BindingViewMode.Create ? $"Cannot create binding on {BindingPropertyName}" : $"Cannot edit binding on {BindingPropertyName}";
            }

            m_Element.TryGetBinding(BindingPropertyName, out var binding, out var uxmlBinding);

            // If we are creating a binding on a target property of the selected VisualElement then...
            if (Mode == BindingViewMode.Create)
            {
                // if there is already a binding on the property then throw an error
                if (binding != null)
                    throw new ArgumentException(GetErrorMessage() + ": The property is already bound.");

                UpdateBindingBeingCreatedFromBindingClass();
            }
            // If we are editing a binding on a target property of the selected VisualElement then ...
            else
            {
                // If there is no binding on the target property then throw an error
                if (binding == null)
                    throw new ArgumentException(GetErrorMessage() + ": The property is not bound yet.");

                ReadViewDataFromBinding(binding, uxmlBinding);
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

            ResetContext();

            // Find the binding serializedData
            BindingPropertyName = binding.property;

            // Find the binding
            var bindingsPath = $"{Context.serializedBasePath}.bindings";
            var bindingsProperty = Context.rootSerializedObject.FindProperty(bindingsPath);
            for (var i = 0; i < bindingsProperty.arraySize; i++)
            {
                var item = bindingsProperty.GetArrayElementAtIndex(i);
                var propertyName = item.FindPropertyRelative("property");
                if (propertyName.stringValue == BindingPropertyName)
                {
                    m_RootPropertyField.bindingPath = item.propertyPath;
                    m_BindingUxmlSerializedDataDescription = description;
                    break;
                }
            }
        }

        private void UpdateControls()
        {
            m_BindingTypeDropdown.SetEnabled(Mode == BindingViewMode.Create);
            m_OkButton.style.display = Mode == BindingViewMode.Create ? DisplayStyle.Flex : DisplayStyle.None;
            m_CancelButton.text = Mode == BindingViewMode.Create ? "Cancel" : "Close";

            if (m_TargetPropertyTypeName != null)
            {
                var propertyType = BindableProperty.DeclaredValueType();

                m_TargetPropertyTypeName.text = TypeUtility.GetTypeDisplayName(propertyType);
                m_TargetPropertyTypeName.tooltip = propertyType?.GetDisplayFullName();
            }
        }

        protected override void CreateViewContent(UxmlAttributesEditingContext context)
        {
            // If there is no UxmlSerializedData, there are no bindings to show.
            if (context.uxmlSerializedDataDescription == null)
            {
                return;
            }

            m_RootPropertyView = new UxmlSerializedDataPropertyView();
            m_RootPropertyView.name = RootPropertyViewName;
            m_RootPropertyView.AddToClassList(InspectorElement.ussClassName);
            m_RootPropertyView.context = context;
            m_RootPropertyView.Bind(context.rootSerializedObject);
            m_RootPropertyField = new PropertyField();
            m_RootPropertyField.name = RootPropertyFieldName;
            m_RootPropertyField.reset += OnPropertyFieldReset;
            m_RootPropertyView.Add(m_RootPropertyField);
            Add(m_RootPropertyView);
        }

        void OnPropertyFieldReset()
        {
            m_BindingPropertyField = m_RootPropertyField.Q<UxmlAttributeField>(BindingPropertyFieldName);
            // insert the binding class field after the binding property field
            var bindingPropertyIndex = m_BindingPropertyField.parent.IndexOf(m_BindingPropertyField);
            if (bindingPropertyIndex != -1)
            {
                m_BindingPropertyField.parent.Insert(bindingPropertyIndex + 1,  m_BindingTypeDropdown);
                var seperator = new VisualElement();
                seperator.AddToClassList("unity-inspector__divider");
                m_BindingPropertyField.parent.Insert(bindingPropertyIndex + 2, seperator);
            }

            m_TargetPropertyTypeName = m_BindingPropertyField.Q<Label>("BindingPropertyTypeNameLabel");

            if (Mode != BindingViewMode.Create)
            {
                var bindingTypeIndex = IndexOfBindingType(m_BindingUxmlSerializedDataDescription.uxmlFullName);
                var choice = k_UxmlBindingTypes[bindingTypeIndex].displayName;
                m_BindingTypeDropdown.SetValueWithoutNotify(choice);
            }

            var sourceToUiConvertersField = m_RootPropertyField.Q<UxmlAttributeField>(BindingConvertersToUiFieldName).Q<PropertyField>();
            sourceToUiConvertersField.reset += () =>
            {
                sourceToUiConvertersField.Q<BindingConvertersField>().EditedElement = Context.element;
            };

            var uiToSourceConvertersField = m_RootPropertyField.Q<UxmlAttributeField>(BindingConvertersToSourceFieldName).Q<PropertyField>();
            uiToSourceConvertersField.reset += () =>
            {
                uiToSourceConvertersField.Q<BindingConvertersField>().EditedElement = Context.element;
            };

            UpdateControls();
        }

        protected override void ReleaseViewContent(UxmlAttributesEditingContext context)
        {
            if (m_RootPropertyView == null)
                return;

            m_RootPropertyView?.Unbind();
            m_RootPropertyView.context = null;
            m_RootPropertyView = null;
            m_RootPropertyField = null;
            m_TargetPropertyTypeName = null;
        }
    }
}
