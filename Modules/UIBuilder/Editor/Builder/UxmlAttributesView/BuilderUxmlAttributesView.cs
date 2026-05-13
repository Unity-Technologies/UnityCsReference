// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Unity.Profiling;
using Unity.Properties;
using Unity.UIToolkit.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.UIElements.Bindings;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace Unity.UI.Builder
{
    /// <summary>
    /// This view displays and edits the list of uxml attributes of an object in a uxml document.
    /// </summary>
    internal class BuilderUxmlAttributesView : IDisposable, IBatchedUxmlChangesListener
    {
        static readonly string s_AttributeFieldRowUssClassName = "unity-builder-attribute-field-row";
        static readonly string s_UxmlButtonUssClassName = "unity-builder-uxml-object-button";
        static readonly string s_UxmlMenuUssClassName = "unity-builder-uxml-object-menu";
        public static readonly string builderSerializedPropertyFieldName = "unity-builder-serialized-property-field";
        internal static readonly PropertyName UndoGroupPropertyKey = "__UnityUndoGroup";

        // Used in tests.
        // ReSharper disable MemberCanBePrivate.Global
        internal const string attributeOverrideMarkerName = "BuilderUxmlAttributesView.UpdateAttributeOverrideStyle";
        internal const string updateFieldStatusMarkerName = "BuilderUxmlAttributesView.UpdateFieldStatus";
        internal const string postAttributeValueChangedMarkerName = "BuilderUxmlAttributesView.PostAttributeValueChange";
        // ReSharper restore MemberCanBePrivate.Global

        public static readonly string ArraySizeRelativePath = "Array.size";
        const string k_ArraySizePart = "size";

        static readonly ProfilerMarker k_UpdateAttributeOverrideStyleMarker = new (attributeOverrideMarkerName);
        static readonly ProfilerMarker k_UpdateFieldStatusMarker = new (updateFieldStatusMarkerName);
        static readonly ProfilerMarker k_PostAttributeValueChangedMarker = new (postAttributeValueChangedMarkerName);

        protected internal BuilderInspector inspector;
        public BuilderUxmlAttributesEditingContext context { get; }  = new ();

        public string serializedRootPath => context.serializedBasePath;

        public IVisualElementScheduledItem refreshScheduledItem;

        private bool m_HasUxmlChangeFlag;

        internal class UxmlAssetSerializedDataRoot : VisualElement
        {
            public UxmlSerializedDataDescription dataDescription;
            public string rootPath;
            public override string ToString() => $"{rootPath} ({dataDescription})";
        }

        // Makes it easier to identify the root when dealing with nested classes
        internal class UxmlSerializedDataAttributeField : VisualElement
        {
        }

        class CustomPropertyDrawerField : VisualElement
        {
        }

        public UxmlSerializedData uxmlSerializedData => context.uxmlSerializedData;

        /// <summary>
        /// Are we able to edit the element or just view its data?
        /// </summary>
        internal bool readOnly => context.readOnly;

        /// <summary>
        /// The container of fields generated from uxml attributes.
        /// </summary>
        public VisualElement attributesContainer { get; set; }

        /// <summary>
        /// The visual element being edited.
        /// </summary>
        public VisualElement currentElement => context.element;

        /// <summary>
        /// The uxml document being edited.
        /// </summary>
        public VisualTreeAsset uxmlDocument => context.visualTree;

        /// <summary>
        /// Returns the uxml element of which attributes are being edited by the view.
        /// </summary>
        public UxmlAsset attributesUxmlOwner => context.elementAsset;

        public BuilderUxmlAttributesView(BuilderInspector inspector = null)
        {
            this.inspector = inspector;

            if (inspector?.batchedChangesController == null)
                return;

            context.notifyAttributesChanged += NotifyAttributesChanged;
            this.inspector.batchedChangesController.deserializeElement += DeserializeElement;
            this.inspector.batchedChangesController.notifyAllChangesProcessed += NotifyAllChangesProcessed;
            this.inspector.batchedChangesController.onUndoRedoPerformedByController += CallDeserializeOnElementActionWrapper;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            if (inspector?.batchedChangesController == null)
                return;

            inspector.batchedChangesController.deserializeElement -= DeserializeElement;
            inspector.batchedChangesController.notifyAllChangesProcessed -= NotifyAllChangesProcessed;
            inspector.batchedChangesController.onUndoRedoPerformedByController -= CallDeserializeOnElementActionWrapper;
        }

        /// <summary>
        /// Sets the specified VisualElement as the owner of attributes to be edited.
        /// </summary>
        /// <param name="uxmlDocument">The uxml document being edited</param>
        /// <param name="visualElement">The VisualElement that provides attributes to be edited</param>
        /// <param name="isInTemplate">Indicates whether the VisualElement is in a template instance</param>
        public void SetAttributesOwner(VisualTreeAsset uxmlDocument, VisualElement visualElement, bool isInTemplate = false)
        {
            context.Set(inspector.document, uxmlDocument, visualElement, inspector.batchedChangesController, isInTemplate);
            attributesContainer?.Clear();
        }

        public virtual void SetInlineValue(VisualElement fieldElement, string property)
        {
            if (serializedRootPath == null)
                return;

            var bindableElement = fieldElement?.Q<BindableElement>();
            var binding = bindableElement?.GetBinding(BindingExtensions.s_SerializedBindingId);
            if (binding is not SerializedObjectBindingBase bindingBase)
                return;

            var path = $"{serializedRootPath}.{property}";
            var result = BuilderAssetUtilities.SynchronizePath(context, path, false);

            object value;
            if (result.attributeOwner == null && result.attributeDescription.isUxmlObject)
            {
                value = null;
            }
            else
            {

                var dataDescription = UxmlSerializedDataRegistry.GetDescription(result.attributeOwner.GetType().FullName);
                var attribute = dataDescription.FindAttributeWithPropertyName(property);
                if (attribute == null)
                    return;

                if (result.uxmlAsset.TryGetAttributeValue(attribute.name, out var uxmlValueString))
                {
                    var tryConvertFromStringResult = UxmlAttributeConverter.TryConvertFromString(attribute.type, uxmlValueString, new CreationContext(context.visualTree), out var uxmlValue);
                    tryConvertFromStringResult.DefaultErrorAction();
                    value = uxmlValue;
                }
                else
                {
                    value = attribute.defaultValueClone;
                }
            }

            var serializedProperty = context.rootSerializedObject.FindProperty(path);
            var handler = ScriptAttributeUtility.GetHandler(serializedProperty);
            if (handler.hasPropertyDrawer && handler.propertyDrawer is not UxmlSerializedDataPropertyDrawer)
            {
                serializedProperty.boxedValue = value;
                serializedProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();

                var propField = fieldElement.Q<PropertyField>() ?? fieldElement.GetFirstAncestorOfType<PropertyField>();
                var context = SerializedObjectBindingContext.GetBindingContextFromElement(propField);
                context?.UpdateRevision();
            }
            else
            {
                bindingBase.SyncValueWithoutNotify(value);
            }
        }

        public void SetBoundValue(VisualElement fieldElement, object value)
        {
            var bindableElement = fieldElement.Q<BindableElement>();
            var binding = bindableElement?.GetBinding(BindingExtensions.s_SerializedBindingId);
            if (binding is not SerializedObjectBindingBase bindingBase)
                return;

            bindingBase.SyncValueWithoutNotify(value);
        }

        internal void RemoveBindingFromSerializedData(VisualElement fieldElement, string property)
        {
            BuilderBindingUtility.RemoveBindingFromSerializedData(context, property, fieldElement);
        }

        internal static string GetSerializedDataRoot(string path)
        {
            // Assuming the "path" here is the full property path
            var endIndex = path.LastIndexOf(BuilderConstants.UxmlSerializedDataFieldName, StringComparison.Ordinal) + BuilderConstants.UxmlSerializedDataFieldName.Length;
            return path.Substring(0, endIndex);
        }

        /// <summary>
        /// Clears the the attributes owner.
        /// </summary>
        public void ResetAttributesOwner()
        {
            SetAttributesOwner(null, null);

            // For tests, update loops run in inversed order. Clear propertyFields to avoid tracking.
            inspector.attributesSection.attributesContainer.Clear();
        }

        /// <summary>
        /// Refreshes the view.
        /// </summary>
        public virtual void Refresh()
        {
            if (attributesContainer == null)
                return;

            attributesContainer.Clear();

            if (context.element == null || context.uxmlSerializedDataDescription == null)
                return;

            GenerateSerializedAttributeFields();
        }

        /// <summary>
        /// Generates fields from the uxml attributes.
        /// </summary>
        protected virtual void GenerateSerializedAttributeFields()
        {
            // UxmlSerializedData
            var root = new UxmlAssetSerializedDataRoot { dataDescription = context.uxmlSerializedDataDescription, rootPath = serializedRootPath };
            attributesContainer.Add(root);
            GenerateSerializedAttributeFields(context.uxmlSerializedDataDescription, root);
        }

        protected void GenerateSerializedAttributeFields(UxmlSerializedDataDescription dataDescription, UxmlAssetSerializedDataRoot parent)
        {
            // We need to show a BindableElement if there are no attributes, just add a label. (UUM-71735)
            if (dataDescription.serializedAttributes.Count == 0)
            {
                parent.Add(new Label(dataDescription.uxmlName) { tooltip = dataDescription.uxmlFullName });
                return;
            }

            foreach (var desc in dataDescription.serializedAttributes)
            {
                var propertyPath = $"{parent.rootPath}.{desc.serializedField.Name}";
                attributesContainer.AddToClassList(InspectorElement.ussClassName);
                if (desc.serializedField.GetCustomAttribute<HideInInspector>() == null)
                {
                    CreateSerializedAttributeRow(desc, propertyPath, parent);
                }
                else
                {
                    var itemRoot = new UxmlAssetSerializedDataRoot
                    {
                        dataDescription = dataDescription,
                        rootPath = propertyPath,
                        name = propertyPath
                    };
                    parent.Add(itemRoot);

                    var fieldElement = new UxmlSerializedDataAttributeField { name = desc.serializedField.Name };
                    fieldElement.SetLinkedAttributeDescription(desc);
                    TrackElementPropertyValue(fieldElement, propertyPath);
                    itemRoot.Add(fieldElement);
                }
            }
        }

        public void RefreshAllAttributeOverrideStyles()
        {
            var fields = GetAttributeFields();
            foreach (var fieldElement in fields)
            {
                UpdateAttributeOverrideStyle(fieldElement);
                UpdateFieldStatus(fieldElement);
            }
        }

        internal static VisualElement GetRootFieldElement(VisualElement visualElement)
        {
            if (visualElement == null)
                return null;

            var dataField = visualElement as UxmlSerializedDataAttributeField ?? visualElement.GetFirstAncestorOfType<UxmlSerializedDataAttributeField>();
            return dataField ?? visualElement;
        }

        protected static string GetAttributeName(VisualElement visualElement)
        {
            var desc = visualElement.GetLinkedAttributeDescription();
            return desc != null ? desc.name : ((IBindable)visualElement).bindingPath;
        }

        string GetBindingPropertyName(VisualElement visualElement)
        {
            // UxmlSerializedFields have a UxmlSerializedDataAttributeField as the parent
            var dataField = visualElement as UxmlSerializedDataAttributeField ?? visualElement.GetFirstAncestorOfType<UxmlSerializedDataAttributeField>();
            if (dataField != null)
            {
                var serializedAttribute = dataField.GetLinkedAttributeDescription() as UxmlSerializedAttributeDescription;
                return serializedAttribute.serializedField.Name;
            }

            var name = visualElement.GetProperty(BuilderConstants.InspectorAttributeBindingPropertyNameVEPropertyName) as string;
            return name;
        }

        internal static BuilderStyleRow GetLinkedStyleRow(VisualElement visualElement)
        {
            return GetRootFieldElement(visualElement).GetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName) as BuilderStyleRow;
        }

        IEnumerable<VisualElement> GetAttributeFields() => attributesContainer.Query<UxmlSerializedDataAttributeField>().Where(ve => ve.HasLinkedAttributeDescription()).Build();

        protected VisualElement CreateUxmlObjectAttributeRow(UxmlSerializedAttributeDescription attribute, string propertyPath)
        {
            var property = context.rootSerializedObject.FindProperty(propertyPath);
            var labelText = StyleSheetUtility.ConvertDashToHuman(attribute.name);

            if (attribute.isList)
            {
                var listView = new ListView
                {
                    bindingPath = propertyPath,
                    virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                    headerTitle = labelText,
                    showAddRemoveFooter = true,
                    showFoldoutHeader = true,
                    showBorder = true,
                    showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                    showBoundCollectionSize = false,
                    reorderable = true,
                    reorderMode = ListViewReorderMode.Animated,
                    bindItem = (ve, i) =>
                    {
                        property.serializedObject.UpdateIfRequiredOrScript();

                        ve.Clear();
                        var item = property.GetArrayElementAtIndex(i);
                        var instance = item.boxedValue;
                        if (instance != null)
                        {
                            var desc = UxmlSerializedDataRegistry.GetDescription(instance.GetType().DeclaringType.FullName);
                            var root = new UxmlAssetSerializedDataRoot { dataDescription = desc, rootPath = item.propertyPath };
                            ve.Add(root);

                            CreateUxmlObjectField(item, desc, root);
                            ve.Bind(context.rootSerializedObject);
                        }
                    },
                    makeItem = () => new VisualElement(),
                    overridingAddButtonBehavior = (bv, btn) =>
                    {
                        ShowAddUxmlObjectMenu(btn, attribute, t =>
                        {
                            BuilderAssetUtilities.AddUxmlObjectToSerializedData(context, property, t);
                        });
                    },
                    onRemove = l =>
                    {
                        if (property.arraySize > 0)
                        {
                            Undo.RegisterCompleteObjectUndo(property.m_SerializedObject.targetObject, BuilderAssetUtilities.GetUndoMessage(property));

                            int index = l.selectedIndex >= 0 ? l.selectedIndex : property.arraySize - 1;
                            property.DeleteArrayElementAtIndex(index);
                            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                            BuilderAssetUtilities.SyncUxmlObjectChanges(context, property.propertyPath);
                        }
                    },
                };
                listView.bindingPath = propertyPath;
                listView.itemIndexChanged += (_, _) =>
                {
                    BuilderAssetUtilities.SyncUxmlObjectChanges(context, property.propertyPath);
                };
                return listView;
            }

            var foldout = new Foldout { text = labelText };
            foldout.TrackPropertyValue(property, p => UpdateUxmlObjectReferenceFieldAddRemoveButtons(p, attribute, foldout, true));
            UpdateUxmlObjectReferenceFieldAddRemoveButtons(property, attribute, foldout, false);
            return foldout;
        }

        protected void CreateUxmlObjectField(SerializedProperty serializedProperty, UxmlSerializedDataDescription dataDescription, UxmlAssetSerializedDataRoot root)
        {
            var handler = ScriptAttributeUtility.GetHandler(serializedProperty);
            if (handler.hasPropertyDrawer && handler.propertyDrawer is not UxmlSerializedDataPropertyDrawer)
            {
                CreateCustomPropertyDrawerField(serializedProperty, root);
            }
            else
            {
                GenerateSerializedAttributeFields(dataDescription, root);
            }
        }

        protected void CreateCustomPropertyDrawerField(SerializedProperty serializedProperty, VisualElement root)
        {
            var drawerRoot = new CustomPropertyDrawerField();
            drawerRoot.AddManipulator(new ContextualMenuManipulator(BuildCustomPropertyDrawerMenu));
            root.Add(drawerRoot);

            var propertyField = new PropertyField { bindingPath = serializedProperty.propertyPath };
            drawerRoot.Add(propertyField);
            inspector.batchedChangesController.TrackCustomPropertyDrawerFields(drawerRoot, serializedProperty, this, context.visualTree, context.element != null);

            // The hierarchy is not complete yet so we need to defer the update
            root.schedule.Execute(() => UpdateCustomPropertyDrawerAttributeOverrideStyle(drawerRoot));
        }

        void UpdateUxmlObjectReferenceFieldAddRemoveButtons(SerializedProperty property, UxmlSerializedAttributeDescription attribute, Foldout field, bool bind = false)
        {
            property.serializedObject.UpdateIfRequiredOrScript();
            const string buttonName = "uxml-button";

            var previousType = field.GetProperty("previousType") as string;

            // Only update if the actual instance type changed
            if (previousType == property.managedReferenceFullTypename)
                return;

            field.SetProperty("previousType", property.managedReferenceFullTypename);

            property = property.Copy();
            field.Clear();
            field.Q(buttonName)?.RemoveFromHierarchy();
            var serializedInstanced = property.managedReferenceValue;

            if (serializedInstanced != null)
            {
                var removeButton = new Button { name = buttonName, text = "Delete" }.WithClassList(s_UxmlButtonUssClassName);
                removeButton.clicked += () =>
                {
                    BuilderAssetUtilities.AddUxmlObjectToSerializedData(context, property, null);
                };
                field.Q<Toggle>().Add(removeButton);

                var desc = UxmlSerializedDataRegistry.GetDescription(serializedInstanced.GetType().DeclaringType.FullName);
                var root = new UxmlAssetSerializedDataRoot { dataDescription = desc, rootPath = property.propertyPath };
                field.Add(root);

                CreateUxmlObjectField(property, desc, root);
                if (bind)
                    field.Bind(context.rootSerializedObject);
            }
            else
            {
                var addButton = new Button { name = buttonName, text = "Add" }.WithClassList(s_UxmlButtonUssClassName);
                addButton.clicked += () =>
                {
                    ShowAddUxmlObjectMenu(addButton, attribute, t =>
                    {
                        BuilderAssetUtilities.AddUxmlObjectToSerializedData(context, property, t);
                    });
                };
                field.Q<Toggle>().Add(addButton);
            }
        }

        void ShowAddUxmlObjectMenu(VisualElement element, UxmlSerializedAttributeDescription attribute, Action<Type> action)
        {
            if (attribute.uxmlObjectAcceptedTypes.Count == 1)
            {
                action(attribute.uxmlObjectAcceptedTypes[0]);
            }
            else if (attribute.uxmlObjectAcceptedTypes.Count > 1)
            {
                var menu = new GenericDropdownMenu();
                menu.contentContainer.AddToClassList(s_UxmlMenuUssClassName);
                foreach (var type in attribute.uxmlObjectAcceptedTypes)
                {
                    var name = ObjectNames.NicifyVariableName(type.DeclaringType.Name);

                    menu.AddItem(name, false, () =>
                    {
                        action(type);
                    });
                }
                menu.DropDown(element.parent.worldBound, element, DropdownMenuSizeMode.Auto);
            }
        }

        /// <summary>
        /// Creates a row in the fields container for the specified UxmlSerializedData attribute.
        /// </summary>
        protected virtual BuilderStyleRow CreateSerializedAttributeRow(UxmlSerializedAttributeDescription attribute, string propertyPath, VisualElement parent = null)
        {
            parent ??= attributesContainer;
            var fieldElement = new UxmlSerializedDataAttributeField();

            if (attribute.isUxmlObject)
            {
                var uxmlObjectField = CreateUxmlObjectAttributeRow(attribute, propertyPath);
                uxmlObjectField.Bind(context.rootSerializedObject);
                fieldElement.Add(uxmlObjectField);

                // Disable template override support for UxmlObjects. (UUM-72789)
                uxmlObjectField.SetEnabled(!context.isInTemplateInstance);
            }
            else
            {
                var property = context.rootSerializedObject.FindProperty(propertyPath);
                if (property == null)
                {
                    var label = new UnityEngine.UIElements.HelpBox { text = $"Attribute <b>{attribute.name}</b> is not serializable. Attribute type does not follow the <a href=\"https://docs.unity3d.com/Manual/script-serialization-rules.html\">Unity Serialization rules</a>.", messageType = HelpBoxMessageType.Warning };
                    fieldElement.Add(label);
                }
                else
                {
                    var serializedReferencesFound = TrackPropertySerializedReferences(property, fieldElement);

                    fieldElement.name = property.name;
                    var propertyField = new PropertyField
                    {
                        name = builderSerializedPropertyFieldName,
                        bindingPath = propertyPath,
                        label = StyleSheetUtility.ConvertDashToHuman(attribute.name)
                    };

                    void TooltipCallback(TooltipEvent e) => OnTooltipEvent(e, propertyField, attribute);
                    propertyField.RegisterCallback<TooltipEvent>(TooltipCallback, TrickleDown.TrickleDown);

                    // We only care about changes when not in readOnly mode.
                    if (!readOnly)
                    {
                        TrackElementPropertyValue(propertyField, property);
                    }

                    // if a serialized reference was found, we need to add the property field the CustomPropertyDrawerField,
                    // which is the child of the UxmlSerializedDataAttributeField.
                    // This is to ensure that the CustomPropertyDrawerField override can be updated correctly.
                    if (serializedReferencesFound)
                    {
                        var customPropertyDrawerField = fieldElement.Q<CustomPropertyDrawerField>();
                        customPropertyDrawerField.Insert(0, propertyField);
                    }
                    else
                        fieldElement.Add(propertyField);

                    propertyField.Bind(context.rootSerializedObject);

                    // Special case for ToggleButtonGroup
                    if (context.element is ToggleButtonGroup && attribute.name == nameof(ToggleButtonGroup.value))
                    {
                        propertyField.RegisterCallback<SerializedPropertyBindEvent>(OnToggleButtonGroupValuePropertyFieldBound);
                    }
                }
            }

            // Create row.
            var styleRow = new BuilderStyleRow();

            styleRow.AddToClassList($"{s_AttributeFieldRowUssClassName}-{propertyPath}");
            styleRow.Add(fieldElement);

            // Ensure the row is added to the inspector hierarchy before refreshing
            parent.Add(styleRow);

            SetupStyleRow(styleRow, fieldElement, attribute);

            return styleRow;
        }

        bool TrackPropertySerializedReferences(SerializedProperty property,
            UxmlSerializedDataAttributeField fieldElement)
        {
            var drawerRoot = new CustomPropertyDrawerField();
            var attributeDescription = context.uxmlSerializedDataDescription.FindAttributeWithPropertyName(property.name);
            if (attributeDescription == null || !inspector.batchedChangesController.FindSerializedReferenceAndTrackChildProperties((property), p =>
            {
                var attributeField = new UxmlSerializedDataAttributeField { name = p.name };
                attributeField.SetLinkedAttributeDescription(attributeDescription);
                drawerRoot.Add(attributeField);
                inspector.batchedChangesController.TrackPropertyValue(attributeField, p, this, uxmlDocument);
            }))
                return false;

            // We found a serialized reference, so we need to add the item root to the FieldElement.
            drawerRoot.AddManipulator(new ContextualMenuManipulator(BuildCustomPropertyDrawerMenu));
            fieldElement.Add(drawerRoot);
            return true;
        }

        void OnToggleButtonGroupValuePropertyFieldBound(SerializedPropertyBindEvent evt)
        {
            if (context.element is not ToggleButtonGroup groupElement)
                return;

            var propertyField = evt.elementTarget;
            var groupField = propertyField.Q<ToggleButtonGroup>();
            if (groupField == null)
                return;

            // Special case for toggle button groups.
            // We want to sync the length of the value with the number of buttons in the hierarchy, and we want to match the
            // allowMultipleSelection and allowEmptySelection attributes so that the value matches the state of the group.
            var obj = context.rootSerializedObject;
            var valueProperty = obj.FindProperty($"{serializedRootPath}.valueUXML");
            var multipleProperty = obj.FindProperty($"{serializedRootPath}.{nameof(ToggleButtonGroup.isMultipleSelection)}");
            var allowEmptyProperty = obj.FindProperty($"{serializedRootPath}.{nameof(ToggleButtonGroup.allowEmptySelection)}");

            groupField.isMultipleSelection = multipleProperty.boolValue;
            groupField.allowEmptySelection = allowEmptyProperty.boolValue;

            var fieldElement = GetRootFieldElement(propertyField);
            fieldElement.TrackPropertyValue(multipleProperty, p =>
            {
                var multiplePropertyFlagsField = context.rootSerializedObject.FindProperty(p.propertyPath + UxmlSerializedData.AttributeFlagSuffix);
                multiplePropertyFlagsField.intValue = (int)UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml;
                var valueFlagsField = context.rootSerializedObject.FindProperty(valueProperty.propertyPath + UxmlSerializedData.AttributeFlagSuffix);
                valueFlagsField.intValue = (int)UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml;

                groupField.isMultipleSelection = p.boolValue;
                groupElement.isMultipleSelection = p.boolValue;
                valueProperty.structValue = groupField.value;
                p.serializedObject.ApplyModifiedProperties();

                attributesUxmlOwner.SetAttribute("is-multiple-selection", p.boolValue.ToString().ToLowerInvariant());
                attributesUxmlOwner.SetAttribute("value", groupField.value.ToString());
                PostAttributeValueChange(fieldElement, groupField.value.ToString(), attributesUxmlOwner);
            });
            fieldElement.TrackPropertyValue(allowEmptyProperty, p =>
            {
                var allowEmptyPropertyFlagsField = context.rootSerializedObject.FindProperty(p.propertyPath + UxmlSerializedData.AttributeFlagSuffix);
                allowEmptyPropertyFlagsField.intValue = (int)UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml;
                var valueFlagsField = context.rootSerializedObject.FindProperty(valueProperty.propertyPath + UxmlSerializedData.AttributeFlagSuffix);
                valueFlagsField.intValue = (int)UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml;

                groupField.allowEmptySelection = p.boolValue;
                groupElement.allowEmptySelection = p.boolValue;
                valueProperty.structValue = groupField.value;
                p.serializedObject.ApplyModifiedProperties();

                attributesUxmlOwner.SetAttribute("allow-empty-selection", p.boolValue.ToString().ToLowerInvariant());
                attributesUxmlOwner.SetAttribute("value", groupField.value.ToString());
                PostAttributeValueChange(fieldElement, groupField.value.ToString(), attributesUxmlOwner);
            });
        }

        protected void SetupStyleRow(BuilderStyleRow styleRow, VisualElement fieldElement, UxmlSerializedAttributeDescription  attribute)
        {
            // Link the PropertyField to the BuilderStyleRow.
            fieldElement.SetContainingRow(styleRow);
            styleRow.AddLinkedFieldElement(fieldElement);

            // Link the PropertyField to the UxmlSerializedAttributeDescription.
            fieldElement.SetLinkedAttributeDescription(attribute);

            // Save the property name.
            fieldElement.SetProperty(BuilderConstants.InspectorAttributeBindingPropertyNameVEPropertyName, attribute.bindingPath);

            // Set initial value.
            UpdateAttributeField(fieldElement);

            // Context menu.
            styleRow.AddManipulator(new ContextualMenuManipulator(BuildAttributeFieldContextualMenu));

            if (fieldElement.GetFieldStatusIndicator() != null)
            {
                fieldElement.GetFieldStatusIndicator().populateMenuItems =
                    (menu) => BuildAttributeFieldContextualMenu(menu, styleRow);
            }

            if (fieldElement is IEditableElement editableElement)
            {
                // used to group undo operations (UUM-32599)
                editableElement.editingStarted += () => SetUndoGroup(fieldElement);
                editableElement.editingEnded += () => UnsetUndoGroup(fieldElement);
            }
        }

        void OnTooltipEvent(TooltipEvent e, PropertyField propertyField, UxmlSerializedAttributeDescription attribute)
        {
            // Only show tooltip on labels
            if (e.target is Label)
            {
                var tooltip = attribute.serializedField.GetCustomAttribute<TooltipAttribute>();
                var valueInfo = GetValueInfo(propertyField);
                var description = tooltip?.tooltip;

                if (inspector.bindingsCache?.TryGetCachedData(inspector.currentVisualElement, attribute.serializedField.Name, out _) == true)
                {
                    // Extract value as a string and add it to the description.
                    var result = BuilderAssetUtilities.SynchronizePath(context, propertyField.bindingPath, true);
                    var currentUxmlSerializedData = result.serializedData as UxmlSerializedData;
                    var newValue = attribute.GetSerializedValue(currentUxmlSerializedData);
                    if (newValue == null || !UxmlAttributeConverter.TryConvertToString(newValue, context.visualTree, out var stringValue))
                        stringValue = "";

                    const int maxValueStringLength = 64;
                    if (stringValue.Length > maxValueStringLength)
                        stringValue = stringValue[..maxValueStringLength] + BuilderConstants.EllipsisText;

                    if (!string.IsNullOrEmpty(description))
                        description += "\n\n";
                    description = $"{description}Inline Value: {stringValue}";
                }

                e.tooltip = BuilderInspector.GetFieldTooltip(propertyField, valueInfo, description, false);
                e.rect = e.elementTarget.worldBound;
            }
            else
            {
                e.tooltip = null;
            }

            e.StopPropagation();
        }

        /// <summary>
        /// Gets the value of the specified attribute.
        /// </summary>
        /// <param name="attribute">The target attribute.</param>
        /// <returns></returns>
        protected virtual object GetAttributeValue(UxmlSerializedAttributeDescription attribute)
        {
            var attributesOwner = context.element;
            if (attribute is UxmlSerializedAttributeDescription uxmlSerializedAttribute)
            {
                uxmlSerializedAttribute.TryGetValueFromObject(attributesOwner, out var value);
                return value;
            }
            return null;
        }

        public void ToggleUxmlChangeFlagForView(bool enabled)
        {
            m_HasUxmlChangeFlag = enabled;
        }

        public void DeserializeElement()
        {
            if (!m_HasUxmlChangeFlag)
                return;
            // Apply changes to the whole element
            context.rootSerializedObject.ApplyModifiedPropertiesWithoutUndo();

            BuilderAssetUtilities.CallDeserializeOnElement(context);

            // Now resync as its possible that the setters made changes during Deserialize, e.g clamping values.
            context.uxmlSerializedDataDescription.SyncSerializedData(context.element, uxmlSerializedData);
        }

        public void NotifyAllChangesProcessed()
        {
            if (!m_HasUxmlChangeFlag)
                return;
            // Update the serialized object to reflect the changes made by PostAttributeValueChange.
            context.rootSerializedObject.UpdateIfRequiredOrScript();
            m_HasUxmlChangeFlag = false;
        }

        /// <summary>
        /// Synchronizes the UXML serialized data to the current UXML asset and sub-UXML objects that are part of the path.
        /// __Note__: To synchronize the attribute owner when extracting, call <see cref="BuilderAssetUtilities.CallDeserializeOnElement"/>.
        /// </summary>
        /// <param name="propertyPath">The full serialized property path.</param>
        /// <param name="changeUxmlAssets">Whether to add missing UXML assets in the path.</param>
        /// <returns></returns>
        public SynchronizePathResult SynchronizePath(string propertyPath, bool changeUxmlAssets)
        {
            return BuilderAssetUtilities.SynchronizePath(context, propertyPath, changeUxmlAssets);
        }

        protected void TrackElementPropertyValue(VisualElement target, SerializedProperty property)
        {
            if (property == null)
            {
                Debug.LogWarning("Property is null, cannot track property value.");
                return;
            }

            // We use TrackPropertyValue because it does not send a change event when it is bound and its safer
            // than relying on change events which may not always be sent, such as when using a custom drawer.
            if (inspector.batchedChangesController.isInsideUndoRedoUpdate || context.element == null)
                return;

            inspector.batchedChangesController.TrackPropertyValue(target, property, this, uxmlDocument);
        }

        protected void TrackElementPropertyValue(VisualElement target, string path)
        {
            var property = context.rootSerializedObject.FindProperty(path);
            TrackElementPropertyValue(target, property);
        }


        /// <summary>
        /// Refreshes the value and status of the specified field.
        /// </summary>
        /// <param name="fieldElement">The field to refresh</param>
        void UpdateAttributeField(VisualElement fieldElement)
        {
            UpdateAttributeOverrideStyle(fieldElement);
            UpdateFieldStatus(fieldElement);
        }

        void UpdateCustomPropertyDrawerAttributeOverrideStyle(CustomPropertyDrawerField fieldElement)
        {
            // When an assembly reload occurs this may be called before the view is fully initialized.
            if (context.rootSerializedObject == null)
                return;
            var overridde = IsAttributeOverriden(fieldElement.Q<PropertyField>().bindingPath);
            var style = fieldElement.GetFirstAncestorOfType<BuilderStyleRow>();
            style?.EnableInClassList(BuilderConstants.InspectorLocalStyleOverrideClassName, overridde);
        }

        internal virtual void UpdateAttributeOverrideStyle(VisualElement fieldElement)
        {
            using var marker = k_UpdateAttributeOverrideStyleMarker.Auto();

            var attribute = fieldElement.GetLinkedAttributeDescription();
            var row = fieldElement.GetContainingRow();

            if (attribute == null || row == null)
                return;

            var attributeIsOverriden = false;
            var fieldElements = row.GetLinkedFieldElements();
            foreach (var field in fieldElements)
            {
                attributeIsOverriden |= IsAttributeOverriden(field);
            }

            row.EnableInClassList(BuilderConstants.InspectorLocalStyleOverrideClassName, attributeIsOverriden);
        }

        /// <summary>
        /// Gets details about the value of the specified field.
        /// </summary>
        /// <param name="fieldElement">The target field.</param>
        protected virtual FieldValueInfo GetValueInfo(VisualElement fieldElement)
        {
            var attribute = fieldElement.GetLinkedAttributeDescription();
            var attributeIsOverriden = IsAttributeOverriden(fieldElement);
            var valueSourceType = attributeIsOverriden ? FieldValueSourceInfoType.Inline : FieldValueSourceInfoType.Default;

            return new FieldValueInfo()
            {
                type = FieldValueInfoType.UXMLAttribute,
                name = attribute.name,
                valueBinding = new FieldValueBindingInfo(FieldValueBindingInfoType.Constant),
                valueSource = new FieldValueSourceInfo(valueSourceType)
            };
        }

        /// <summary>
        /// Updates the status of the specified field.
        /// </summary>
        /// <param name="fieldElement">The field to update.</param>
        protected virtual void UpdateFieldStatus(VisualElement fieldElement)
        {
            using var marker = k_UpdateFieldStatusMarker.Auto();

            fieldElement = GetRootFieldElement(fieldElement);
            var valueInfo = GetValueInfo(fieldElement);

            fieldElement.SetProperty(BuilderConstants.InspectorFieldValueInfoVEPropertyName, valueInfo);
            BuilderInspector.UpdateFieldStatusIconAndStyling(inspector?.currentVisualElement, fieldElement, valueInfo, false);
        }

        public void SendNotifyAttributesChanged() => NotifyAttributesChanged();

        /// <summary>
        /// Notifies that the list of attributes has changed.
        /// </summary>
        protected virtual void NotifyAttributesChanged(string attributeName = null)
        {
        }

        protected bool IsAttributeOverriden(VisualElement fieldElement)
        {
            if (readOnly)
                return false;

            var attribute = fieldElement.GetLinkedAttributeDescription();
            var rootElement = fieldElement?.GetFirstAncestorOfType<UxmlAssetSerializedDataRoot>();
            if (rootElement == null)
            {
                Debug.LogError("[UI Builder] Serialization error. Root can't be found.");
                return false;
            }

            var result = BuilderAssetUtilities.SynchronizePath(context, rootElement.rootPath, false);
            if (result.success)
            {
                return IsAttributeOverriden(result.uxmlAsset == context.elementAsset ? context.element : null, result.uxmlAsset, attribute);
            }

            return false;
        }

        /// <summary>
        /// Indicates whether the specified uxml attribute is defined in the uxml element related to the specified instance.
        /// </summary>
        /// <param name="attributesOwner">An instance of the uxml element that owns the uxml attribute</param>
        /// <param name="attribute">The uxml attribute</param>
        public static bool IsAttributeOverriden(VisualElement attributesOwner, UxmlSerializedAttributeDescription attribute)
        {
            return IsAttributeOverriden(attributesOwner, attributesOwner.GetVisualElementAsset(), attribute);
        }

        /// <summary>
        /// Indicates whether the specified attribute is defined in the specified UMXL element.
        /// </summary>
        /// <param name="attributeOwner">An instance created from the uxml element that owns the related xml attribute.</param>
        /// <param name="attributeUxmlOwner">The uxml element that owns the uxml attribute to evaluate.</param>
        /// <param name="attribute">The uxml attribute.</param>
        /// <returns></returns>
        public static bool IsAttributeOverriden(object attributeOwner, UxmlAsset attributeUxmlOwner, UxmlSerializedAttributeDescription attribute)
        {
            if (attribute is UxmlSerializedAttributeDescription { isUxmlObject: true })
                return false;

            if (attributeOwner is VisualElement ve)
            {
                if (attributeUxmlOwner != null && attribute.name == "picking-mode")
                {
                    var veaAttributeValue = attributeUxmlOwner.GetAttributeValue(attribute.name);
                    var isBound = DataBindingUtility.TryGetBinding(ve, new PropertyPath(attribute.name), out _);
                    if (isBound || veaAttributeValue != null &&
                        veaAttributeValue.ToLower() != attribute.defaultValueAsString.ToLower())
                        return true;
                }
                else if (attribute.name == "name")
                {
                    if (!string.IsNullOrEmpty(ve.name))
                        return true;
                }
                else if (BuilderAssetUtilities.HasAttributeOverrideInRootTemplate(ve, attribute.name))
                {
                    return true;
                }
                else
                {
                    var template = BuilderAssetUtilities.GetVisualElementRootTemplate(ve);
                    var templateVta = template?.GetProperty(BuilderConstants.ElementLinkedInstancedVisualTreeAssetVEPropertyName) as VisualTreeAsset;
                    var linkedOpenVta = ve.GetProperty(BuilderConstants.ElementLinkedBelongingVisualTreeAssetVEPropertyName) as VisualTreeAsset;
                    if ((templateVta == null || templateVta == linkedOpenVta) && DataBindingUtility.TryGetBinding(ve, new PropertyPath(attribute.name), out _))
                    {
                        return true;
                    }
                }
            }

            if (attributeUxmlOwner != null && attributeUxmlOwner.HasAttribute(attribute.name))
            {
                return true;
            }

            return false;
        }

        public bool IsAttributeOverriden(string propertyPath)
        {
            var result = BuilderAssetUtilities.SynchronizePath(context, propertyPath, false);
            if (result.success)
            {
                return result.uxmlAsset.HasAttribute(result.attributeDescription.name) ||
                    (result.attributeDescription.isUxmlObject && result.uxmlAsset != null);
            }
            return false;
        }

        /// <summary>
        /// Resets the value of the specified attribute field to its default value.
        /// </summary>
        /// <param name="fieldElement">The field to reset</param>
        /// <para name="attribute">The attribute related to the field</para>
        protected virtual void ResetAttributeFieldToDefault(VisualElement fieldElement, UxmlSerializedAttributeDescription attribute)
        {
            attribute.SyncDefaultValue(uxmlSerializedData, true);
            context.rootSerializedObject.UpdateIfRequiredOrScript();
            BuilderAssetUtilities.CallDeserializeOnElement(context);
            // Rebind to the new default value
            fieldElement.Bind(context.rootSerializedObject);
        }

        void BuildAttributeFieldContextualMenu(ContextualMenuPopulateEvent evt) => BuildAttributeFieldContextualMenu(evt.menu, evt.currentTarget as BuilderStyleRow);

        void BuildCustomPropertyDrawerMenu(ContextualMenuPopulateEvent evt)
        {
            var target = evt.triggerEvent.target as VisualElement;
            var property = target.userData as SerializedProperty ?? target.parent.userData as SerializedProperty;
            if (property == null)
                return;

            evt.menu.AppendAction(
                BuilderConstants.ContextMenuUnsetMessage,
                (a) => UnsetAttributeProperty(a.userData as SerializedProperty, true),
                action =>
                {
                    if (action.userData is not SerializedProperty property)
                        return DropdownMenuAction.Status.Disabled;

                    if (IsAttributeOverriden(property.propertyPath))
                        return DropdownMenuAction.Status.Normal;
                    return DropdownMenuAction.Status.Disabled;
                },
                property);

            evt.menu.AppendAction(
                BuilderConstants.ContextMenuUnsetAllMessage,
                (action) => UnsetAllAttributes(),
                action =>
                {
                    if (IsAnyAttributeSet())
                        return DropdownMenuAction.Status.Normal;
                    return DropdownMenuAction.Status.Disabled;
                });
        }

        protected virtual void BuildAttributeFieldContextualMenu(DropdownMenu menu, BuilderStyleRow styleRow)
        {
            var fields = styleRow.GetLinkedFieldElements();
            var fieldElement = fields[0]; // Assume there's only one field for default case.

            // Dont add menu items to the root of UxmlObjects, they conflict with the field menu items, e.g multiple "Unset" menu items would be added.
            if (fieldElement.GetLinkedAttributeDescription() is { } desc && desc.isUxmlObject)
                return;

            menu.AppendAction(
                BuilderConstants.ContextMenuUnsetMessage,
                (a) => UnsetAttributeProperty(a.userData as VisualElement, true),
                action =>
                {
                    if (action.userData is not VisualElement field)
                        return DropdownMenuAction.Status.Disabled;

                    var attributeName = GetAttributeName(field);
                    var bindingProperty = GetBindingPropertyName(field);
                    var isAttributeOverrideAttribute =
                        context.isInTemplateInstance
                        && BuilderAssetUtilities.HasAttributeOverrideInRootTemplate(context.element, attributeName);
                    var canUnsetBinding = !context.isInTemplateInstance && DataBindingUtility.TryGetBinding(context.element, new PropertyPath(bindingProperty), out _);

                    // Check UxmlObjects
                    bool hasAttributeOverride = false;
                    var root = field.GetFirstAncestorOfType<UxmlAssetSerializedDataRoot>();

                    var result = BuilderAssetUtilities.SynchronizePath(context, root.rootPath, false);
                    if (result.success)
                        hasAttributeOverride = IsAttributeOverriden(result.uxmlAsset == context.elementAsset ? context.element : null, result.uxmlAsset, field.GetLinkedAttributeDescription());

                    return hasAttributeOverride || isAttributeOverrideAttribute || canUnsetBinding
                        ? DropdownMenuAction.Status.Normal
                        : DropdownMenuAction.Status.Disabled;
                },
                fieldElement);

            menu.AppendAction(
                BuilderConstants.ContextMenuUnsetAllMessage,
                (action) => UnsetAllAttributes(),
                action =>
                {
                    if (IsAnyAttributeSet())
                        return DropdownMenuAction.Status.Normal;
                    return DropdownMenuAction.Status.Disabled;
                });
        }

        internal bool IsAnyAttributeSet()
        {
            foreach (var attribute in context.uxmlSerializedDataDescription.serializedAttributes)
            {
                if (attribute?.name == null)
                    continue;

                if (context.isInTemplateInstance
                    && attribute.name == "name")
                {
                    continue;
                }

                if (IsAttributeOverriden(context.element, attribute))
                    return true;
            }

            if (attributesUxmlOwner != null)
            {
                // Do we have any UxmlObjects?
                return attributesUxmlOwner != null && attributesUxmlOwner.HasAnyUxmlObjectAsset();
            }

            return false;
        }

        internal virtual void UnsetAllAttributes()
        {
            var undoGroup = Undo.GetCurrentGroup();
            BuilderAssetUtilities.UndoRecordDocument(context, (BuilderConstants.ChangeAttributeValueUndoMessage));
            var builder = Builder.ActiveWindow;

            if (context.isInTemplateInstance)
            {
                var parentTemplate = BuilderAssetUtilities.GetVisualElementRootTemplate(context.element);
                var parentTemplateAsset = parentTemplate.GetVisualElementAsset() as TemplateAsset;
                var attributeOverrides = new List<TemplateAsset.AttributeOverride>(parentTemplateAsset.attributeOverrides);
                var pathToTemplateAsset = parentTemplateAsset.GetPathToTemplateAsset(context.element);

                foreach (var attributeOverride in attributeOverrides)
                {
                    if (attributeOverride.NamesPathMatchesElementNamesPath(pathToTemplateAsset))
                    {
                        parentTemplateAsset.RemoveAttributeOverride(attributeOverride.m_AttributeName, pathToTemplateAsset);
                    }
                }

                // Re-sync serializedDataOverrides since attribute overrides have changed.
                parentTemplateAsset.serializedDataOverrides.Clear();
                UxmlSerializer.CreateSerializedDataOverrides(context.visualTree);

                var hierarchyView = builder.hierarchy.elementHierarchyView;
                var selectionId = hierarchyView.GetSelectedItemId();

                builder.OnEnableAfterAllSerialization();

                hierarchyView.SelectItemById(selectionId);
                NotifyAttributesChanged();
            }
            else
            {
                // Clear UxmlObjects
                attributesUxmlOwner.RemoveUxmlObjectAssetChildren();

                // Clear attribute overrides
                foreach (var attribute in context.uxmlSerializedDataDescription.serializedAttributes)
                {
                    if (attribute.isUxmlObject)
                        continue;

                    context.elementAsset.RemoveAttribute(attribute.name);
                }

                // Reset the whole UxmlSerializedData but keep the id.
                var uxmlAssetId = context.elementAsset.serializedData.uxmlAssetId;
                context.elementAsset.serializedData = context.uxmlSerializedDataDescription.CreateDefaultSerializedData();
                context.elementAsset.serializedData.uxmlAssetId = uxmlAssetId;
                BuilderAssetUtilities.CallDeserializeOnElement(context);

                // Notify of changes.
                NotifyAttributesChanged();
                Refresh();
                inspector.headerSection.Refresh();
            }

            Undo.CollapseUndoOperations(undoGroup);
        }

        protected void CallDeserializeOnElementActionWrapper()
        {
            BuilderAssetUtilities.CallDeserializeOnElement(context);
        }

        public void UnsetAttributeProperty(SerializedProperty property, bool removeBinding)
        {
            var result = BuilderAssetUtilities.SynchronizePath(context, property.propertyPath, false);
            if (!result.success)
                return;

            BuilderAssetUtilities.UndoRecordDocument(context, (BuilderConstants.ChangeAttributeValueUndoMessage));

            // Unset value in asset.
            if (context.isInTemplateInstance)
            {
                UnsetTemplateAttribute(result.attributeDescription.name, removeBinding);
                NotifyAttributesChanged(result.attributeDescription.name);
            }
            else
            {
                result.uxmlAsset.RemoveAttribute(result.attributeDescription.name);
                result.attributeDescription.SyncDefaultValue(result.serializedData, true);
                BuilderAssetUtilities.CallDeserializeOnElement(context);

                // TODO: Can we remove this?
                UnsetEnumValue(result.attributeDescription.name, removeBinding);

                NotifyAttributesChanged(result.attributeDescription.name);
                Refresh();
            }
        }

        public void UnsetAttributeProperty(VisualElement fieldElement, bool removeBinding)
        {
            var attributeName = GetAttributeName(fieldElement);

            BuilderAssetUtilities.UndoRecordDocument(context, (BuilderConstants.ChangeAttributeValueUndoMessage));

            // Unset value in asset.
            if (context.isInTemplateInstance)
            {
                UnsetTemplateAttribute(attributeName, removeBinding);
                NotifyAttributesChanged(attributeName);
            }
            else
            {
                var currentAttributesUxmlOwner = attributesUxmlOwner;
                var currentSerializedData = uxmlSerializedData;

                if (fieldElement.GetFirstAncestorOfType<UxmlAssetSerializedDataRoot>() is { } dataRoot && dataRoot.dataDescription.isUxmlObject)
                {
                    var result = BuilderAssetUtilities.SynchronizePath(context, dataRoot.rootPath, false);
                    currentAttributesUxmlOwner = result.uxmlAsset;
                    currentSerializedData = result.serializedData as UxmlSerializedData;
                }

                if (removeBinding)
                {
                    var bindingProperty = GetBindingPropertyName(fieldElement);
                    RemoveBindingFromSerializedData(fieldElement, bindingProperty);
                }

                currentAttributesUxmlOwner.RemoveAttribute(attributeName);
                var description = fieldElement.GetLinkedAttributeDescription() as UxmlSerializedAttributeDescription;
                description.SyncDefaultValue(currentSerializedData, true);
                BuilderAssetUtilities.CallDeserializeOnElement(context);

                UnsetEnumValue(attributeName, removeBinding);

                NotifyAttributesChanged(attributeName);
                Refresh();
            }
        }

        void UnsetTemplateAttribute(string attributeName, bool removeBinding)
        {
            var templateContainer = BuilderAssetUtilities.GetVisualElementRootTemplate(context.element);
            var templateAsset = templateContainer.GetVisualElementAsset() as TemplateAsset;

            if (templateAsset != null)
            {
                var builder = Builder.ActiveWindow;
                var hierarchyView = builder.hierarchy.elementHierarchyView;
                var selectionId = hierarchyView.GetSelectedItemId();
                var pathToTemplateAsset = templateAsset.GetPathToTemplateAsset(context.element);

                templateAsset.RemoveAttributeOverride(attributeName, pathToTemplateAsset);

                // Re-sync serializedDataOverrides since attribute overrides have changed.
                templateAsset.serializedDataOverrides.Clear();
                UxmlSerializer.CreateSerializedDataOverrides(context.visualTree);

                builder.OnEnableAfterAllSerialization();

                hierarchyView.SelectItemById(selectionId);
            }

            UnsetEnumValue(attributeName, removeBinding);
        }

        protected void UnsetEnumValue(string attributeName, bool removeBinding)
        {
            if (attributeName != "type")
                return;

            // When unsetting the type value for an enum field, we also need to clear the value field as well.
            if (context.element is EnumField)
            {
                // If the current value is not defined in the new enum type, we need to clear the property because
                // it will otherwise throw an exception.
                var valueField = attributesContainer.Query<PropertyField>()
                    .Where(f => f.serializedProperty?.name == nameof(EnumField.valueAsString))
                    .First()?.Q<EnumStringValueField>();
                if (valueField != null)
                    UnsetAttributeProperty(valueField, removeBinding);
            }
            else if (context.element is EnumFlagsField)
            {
                // If the current value is not defined in the new enum type, we need to clear the property because
                // it will otherwise throw an exception.
                var valueField = attributesContainer.Query<PropertyField>()
                    .Where(f => f.serializedProperty?.name == nameof(EnumFlagsField.valueAsString))
                    .First()?.Q<EnumFlagsStringValueField>();
                if (valueField != null)
                    UnsetAttributeProperty(valueField, removeBinding);
            }
        }

        public void AttributeValueChanged(VisualElement target, string propertyPath, UxmlAsset uxmlOwner)
        {
            PostAttributeValueChange(target, propertyPath, uxmlOwner);
        }

        public void UxmlObjectChanged(VisualElement fieldElement)
        {
            if (fieldElement.GetFirstAncestorOfType<CustomPropertyDrawerField>() is { } customPropertyDrawer)
                UpdateCustomPropertyDrawerAttributeOverrideStyle(customPropertyDrawer);
        }

        internal void OnValidatedAttributeValueChange(ChangeEvent<string> evt, Regex regex, string message)
        {
            var field = evt.elementTarget as TextField;
            if (!string.IsNullOrEmpty(evt.newValue) && !regex.IsMatch(evt.newValue))
            {
                Builder.ShowWarning(string.Format(message, field.label));
                field.SetValueWithoutNotify(evt.previousValue);
                evt.StopPropagation();
                return;
            }

            // Sync with serialized property
            var prop = context.rootSerializedObject.FindProperty($"{serializedRootPath}.{field.bindingPath}");

            Undo.RegisterCompleteObjectUndo(prop.m_SerializedObject.targetObject, BuilderAssetUtilities.GetUndoMessage(prop));

            prop.stringValue = evt.newValue;
            context.rootSerializedObject.ApplyModifiedPropertiesWithoutUndo();

            // Change is added manually because input validation takes priority over tracking update.
            inspector.batchedChangesController.AddBatchedChange(field, prop, this, uxmlDocument);
        }

        void PostAttributeValueChange(VisualElement field, string value, UxmlAsset uxmlAsset = null)
        {
            if (field == null)
                return;

            using var marker = k_PostAttributeValueChangedMarker.Auto();
            var attributeName = GetAttributeName(field);
            UxmlAssetUtilities.PostAttributeValueChange(attributeName, value, context.visualTree, uxmlAsset, context.isInTemplateInstance, context.element,
                () => BuilderAssetUtilities.UndoRecordDocument(context, BuilderConstants.ChangeAttributeValueUndoMessage),
                (vta, vea) => BuilderAssetUtilities.CallDeserializeOnElement(context, vea), VisualElementExtensions.GetVisualElementAsset);

            // Mark field as overridden.
            if (field.GetFirstAncestorOfType<CustomPropertyDrawerField>() is { } customPropertyDrawer)
                UpdateCustomPropertyDrawerAttributeOverrideStyle(customPropertyDrawer);
            else
                UpdateAttributeOverrideStyle(field);

            // Notify of changes.
            NotifyAttributesChanged(attributeName);

            var styleRow = GetLinkedStyleRow(field);
            if (styleRow != null)
                UpdateFieldStatus(field);
        }

        void SetUndoGroup(VisualElement field)
        {
            Undo.IncrementCurrentGroup();
            field?.SetProperty(UndoGroupPropertyKey, Undo.GetCurrentGroup());
        }

        void UnsetUndoGroup(VisualElement field)
        {
            Undo.IncrementCurrentGroup();
            field?.SetProperty(UndoGroupPropertyKey, null);
        }
    }
}
