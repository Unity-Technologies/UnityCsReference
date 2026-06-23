// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Represents a field for editing UXML attributes in the UXML Serialized Data Property View.
/// </summary>
[UxmlElement]
internal partial class UxmlAttributeField : VisualElement
{
    public const string ussClassName = "unity-uxml-attribute-field";

    PropertyField m_PropertyField;
    UxmlAttributeFieldDecorator m_Decorator;

    /// <summary>
    /// The decorator element that wraps the property field.
    /// </summary>
    public UxmlAttributeFieldDecorator decorator => m_Decorator;

    /// <summary>
    /// Returns the serialized property bound to this UXML attribute field.
    /// </summary>
    internal SerializedProperty boundProperty => m_PropertyField.serializedProperty;

    /// <summary>
    /// Optionally overwrite the label of the generate property field. If no label is provided the string will be taken from the SerializedProperty.
    /// </summary>
    [UxmlAttribute]
    public string label
    {
        get => m_PropertyField.label;
        set => m_PropertyField.label = value;
    }

    /// <summary>
    /// The binding path of the UXML attribute field.
    /// </summary>
    [UxmlAttribute]
    public string bindingPath
    {
        get => m_PropertyField.bindingPath;
        set => m_PropertyField.bindingPath = value;
    }

    /// <summary>
    /// The UXML attributes authoring context associated with this field.
    /// </summary>
    public UxmlAttributesEditingContext Context => m_Decorator.context;

    /// <summary>
    /// Constructor for UxmlAttributeField.
    /// </summary>
    public UxmlAttributeField() : this(null)
    {
    }

    /// <summary>
    /// Constructor for UxmlAttributeField.
    /// </summary>
    /// <param name="property">The property that represents the UXML attribute edited by this field</param>
    public UxmlAttributeField(SerializedProperty property)
    {
        AddToClassList(ussClassName);

        m_Decorator = new UxmlAttributeFieldDecorator();
        m_PropertyField = new PropertyField(property);
        m_Decorator.Add(m_PropertyField);
        Add(m_Decorator);
    }
}

/// <summary>
/// Decorated fields to UXML attributes adding override and affordance functionality.
/// </summary>
[UxmlElement]
internal partial class UxmlAttributeFieldDecorator : VisualElement, ITrackablePropertyProvider
{
    public const string ussClassName = "unity-uxml-attribute-field-decorator";
    public const string contentContainerUssClassName = ussClassName + "__content-container";
    public const string affordanceElementName = "affordance-element";
    public const string affordanceElementUssClassName = ussClassName + "__affordance-element";
    public static readonly string s_InlineFieldUssClassName = "property-field__inline-value";
    public static readonly UniqueStyleString s_BoundFieldUssClassName = new("property-field__bound");

    public static readonly string k_AddBindingText = L10n.Tr("Add Binding");
    public static readonly string k_RemoveBindingText = L10n.Tr("Remove Binding");
    public static readonly string k_EditBindingText = L10n.Tr("Edit Binding");
    public static readonly string k_ViewBindingText = L10n.Tr("View Binding");
    public static readonly string k_UnsetText = L10n.Tr("Unset");
    public static readonly string k_UnsetAllText = L10n.Tr("Unset all");

    readonly List<string> s_BindingIgnoredAttributeNames = ["property"];

    class ContentContainer : VisualElement
    {
        IBindable m_Bindable;
        bool m_IsPropertyBoundToBindable;
        UxmlAttributeFieldDecorator m_Decorator;

        public IBindable bindable => m_Bindable;

        public ContentContainer(UxmlAttributeFieldDecorator decorator)
        {
            m_Decorator = decorator;
            AddToClassList(contentContainerUssClassName);
        }

        internal override void OnChildAdded(VisualElement child)
        {
            // If the decorator is already bound, do nothing.
            if (m_Decorator.boundProperty != null)
                return;

            if (m_Bindable == null && child is IBindable bindable)
            {
                m_Bindable = bindable;
                child.RegisterCallback<SerializedPropertyBindEvent>(OnSerializedPropertyBindEvent);
                if (child is PropertyField propertyField)
                    propertyField.reset += m_Decorator.OnPropertyFieldReset;
            }
        }

        internal override void OnChildRemoved(VisualElement child)
        {
            if (m_Bindable == child)
            {
                child.UnregisterCallback<SerializedPropertyBindEvent>(OnSerializedPropertyBindEvent);
                if (child is PropertyField propertyField)
                    propertyField.reset -= m_Decorator.OnPropertyFieldReset;
                m_Bindable = null;
                if (m_IsPropertyBoundToBindable)
                {
                    m_Decorator.boundProperty = null;
                    m_Decorator.boundField = null;
                    m_IsPropertyBoundToBindable = false;
                }
            }
        }

        void OnSerializedPropertyBindEvent(SerializedPropertyBindEvent evt)
        {
            m_Decorator.boundProperty = evt.bindProperty;
            m_Decorator.boundField = evt.elementTarget;
            m_IsPropertyBoundToBindable = true;

            // boundProperty is assigned after PropertyField.reset fires during the initial bind,
            // so OnPropertyFieldReset silently aborts on first dispatch. Call it here to ensure
            // the ListView handlers are always wired up.
            m_Decorator.OnPropertyFieldReset();
        }
    }

    class RefreshBinding : CustomBinding
    {
        UxmlAttributeFieldDecorator m_Decorator;

        public RefreshBinding(UxmlAttributeFieldDecorator decorator)
        {
            m_Decorator = decorator;
            updateTrigger = BindingUpdateTrigger.WhenDirty;
        }

        protected internal override BindingResult Update(in BindingContext context)
        {
            m_Decorator.Refresh();
            return new BindingResult(BindingStatus.Success);
        }
    }

    static readonly BindingId k_RefreshBindingId = "attribute-field__refresh";

    FieldAffordanceElement m_AffordanceElement;
    ContentContainer m_ContentContainer;
    VisualElement m_BoundField;
    UxmlSerializedDataPropertyView m_PropertyView;
    SerializedProperty m_BoundProperty;
    SerializedProperty m_BoundPropertyFlags;
    UxmlSerializedAttributeDescription m_BoundAttributeDescription;
    UxmlAttributesEditingContext m_Context;
    OverrideRow m_OverrideRow;
    BindingId? m_CachedFullBindingPath;
    RefreshBinding m_RefreshBinding;

    event Action<ITrackablePropertyProvider, string, TrackedPropertyType> OnTrackedPropertyChanged;
    event Action<ITrackablePropertyProvider, string, bool, bool, bool> OnTrackedPropertySourceChanged;

    event Action<ITrackablePropertyProvider, string, TrackedPropertyType> ITrackablePropertyProvider.OnTrackedPropertyChanged
    {
        add => OnTrackedPropertyChanged += value;
        remove => OnTrackedPropertyChanged -= value;
    }

    event Action<ITrackablePropertyProvider, string, bool, bool, bool> ITrackablePropertyProvider.OnTrackedPropertySourceChanged
    {
        add => OnTrackedPropertySourceChanged += value;
        remove => OnTrackedPropertySourceChanged -= value;
    }

    /// <summary>
    /// The content container for this decorator.
    /// </summary>
    public override VisualElement contentContainer => m_ContentContainer;

    /// <summary>
    /// The field affordance element for this decorator.
    /// </summary>
    internal FieldAffordanceElement affordanceElement => m_AffordanceElement;

    /// <summary>
    /// The UXML attributes authoring context associated with this decorator.
    /// </summary>
    internal UxmlAttributesEditingContext context
    {
        get => m_Context;
        set
        {
            if (m_Context == value)
                return;

            m_Context?.editingController.UnregisterUxmlAttributeFieldDecorator(this);

            m_Context = value;

            m_Context?.editingController.RegisterUxmlAttributeFieldDecorator(this);
        }
    }

    /// <summary>
    /// The serialized property bound to this decorator's content.
    /// </summary>
    internal SerializedProperty boundProperty {
        get => m_BoundProperty;
        private set
        {
            if (m_BoundProperty == value)
                return;

            UntrackPropertyValueChange();

            m_BoundProperty = value;
            m_BoundPropertyFlags = m_BoundProperty?.GetUxmlAttributeFlags();
            m_CachedFullBindingPath = null;

            TrackPropertyValueChange();
            UpdateBoundAttribute();
            boundPropertyChanged?.Invoke(this, EventArgs.Empty);
            ScheduleRefresh();
        }
    }

    /// <summary>
    /// The UXML serialized attribute description bound to this decorator's content.
    /// </summary>
    internal UxmlSerializedAttributeDescription boundAttributeDescription
    {
        get => m_BoundAttributeDescription;
        private set
        {
            if (m_BoundAttributeDescription == value)
                return;
            m_BoundAttributeDescription = value;
            UpdateFieldFromBoundAttribute();
        }
    }

    /// <summary>
    /// The field bound to this decorator's content.
    /// </summary>
    internal VisualElement boundField
    {
        get => m_BoundField;
        set
        {
            if (m_BoundField == value)
                return;

            m_BoundField?.UnregisterCallback<DetachFromPanelEvent>(OnFieldDetachedFromPanel);

            m_BoundField = value;

            if (m_BoundField == null)
            {
                return;
            }

            if (boundProperty is { isValid: true })
                SendTrackPropertyEvent(this, m_BoundField, boundProperty.propertyPath, PropertyTrackingType.Register);
            m_BoundField.RegisterCallback<DetachFromPanelEvent>(OnFieldDetachedFromPanel);
            ScheduleRefresh();
        }
    }

    /// <summary>
    /// Event sent when the bound property of this decorator has changed.
    /// </summary>
    public event EventHandler boundPropertyChanged;

    /// <summary>
    /// Constructor for UxmlAttributeFieldDecorator.
    /// </summary>
    public UxmlAttributeFieldDecorator() : this(null)
    {
    }

    /// <summary>
    /// Constructor for UxmlAttributeFieldDecorator with a bound property.
    /// </summary>
    internal UxmlAttributeFieldDecorator(SerializedProperty property)
    {
        AddToClassList(ussClassName);

        m_OverrideRow = new OverrideRow() { style = { flexGrow = 1 } };
        m_AffordanceElement = new FieldAffordanceElement() { name = affordanceElementName };
        m_AffordanceElement.AddToClassList(affordanceElementUssClassName);
        m_OverrideRow.Add(m_AffordanceElement);
        hierarchy.Add(m_OverrideRow);

        m_ContentContainer = new ContentContainer(this);
        m_OverrideRow.Add(m_ContentContainer);
        RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
        RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);

        m_RefreshBinding = new RefreshBinding(this);
        this.SetBinding(k_RefreshBindingId, m_RefreshBinding);

        boundProperty = property;
        SetupContextMenu();
    }

    void SetupContextMenu()
    {
        var contextMenuManipulator = new ContextualMenuManipulator((evt) =>
        {
            m_AffordanceElement.OnContextualMenuPopulate(evt);
        });
        contextMenuManipulator.acceptClicksIfDisabled = true;
        this.AddManipulator(contextMenuManipulator);

        m_AffordanceElement.populateMenuItems = menu =>
        {
            var vea = context.element.visualElementAsset;

            if (vea == null)
                return;

            var container = context.element;

            var bindingPath = GetFullBindingPath();
            var isBindableProperty = PropertyContainer.IsPathValid(ref container, bindingPath);

            // Add a separator in case then menu is already filled with items (e.g: TextField's input)
            menu.AppendSeparator();

            if (isBindableProperty)
            {
                var hasDataBinding = false;

                if (vea != null)
                {
                    hasDataBinding = context.element.TryGetBinding(bindingPath, out _);
                }

                if (hasDataBinding)
                {
                    if (context.isInTemplateInstance || context.isReadOnly)
                    {
                        menu.AppendAction(k_ViewBindingText,
                            (a) => BindingWindow.OpenToView(context.element, bindingPath, this),
                            (a) => DropdownMenuAction.Status.Normal,
                            this);
                    }
                    else
                    {
                        menu.AppendAction(k_EditBindingText,
                            (a) => BindingWindow.OpenToEdit(context.element, bindingPath, this),
                            (a) => DropdownMenuAction.Status.Normal,
                            this);


                        menu.AppendAction(k_RemoveBindingText, (a) =>
                        {
                            RemoveBindingCommand.Execute(CommandSources.Inspector, context.element, bindingPath);
                            context.rootSerializedObject.UpdateIfRequiredOrScript();
                            ScheduleRefresh();
                        }, (a) => DropdownMenuAction.Status.Normal, this);
                    }
                }
                else
                {
                    if (!context.isInTemplateInstance && !context.isReadOnly && vea != null)
                    {
                        menu.AppendAction(k_AddBindingText,
                            _ => { BindingWindow.OpenToCreate(context.element, bindingPath, this); });
                    }
                }
            }
            menu.AppendSeparator();

            menu.AppendAction(k_UnsetText, (_) => UnsetAttribute(), (_) => CanUnsetAttribute() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            menu.AppendAction(k_UnsetAllText, (_) => UnsetAllAttributes(), (_) => CanUnsetAllAttributes() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        };
    }

    bool CanUnsetAttribute()
    {
        bool isInline = m_AffordanceElement.fieldAffordanceData.sourceTypeInfo ==
                        FieldAffordanceSourceInfoType.Inline;
        bool hasBinding = m_AffordanceElement.fieldAffordanceData.sourceTypeInfo is
            FieldAffordanceSourceInfoType.ResolvedBinding or
            FieldAffordanceSourceInfoType.UnhandledBinding or
            FieldAffordanceSourceInfoType.UnresolvedBinding;

        var result = UxmlAssetUtilities.SynchronizePath(context, boundProperty.propertyPath, false);

        // Disable Unset for the "property" property of binding.
        if (result is { success: true, serializedData: Binding.UxmlSerializedData } &&
            boundProperty.name == nameof(Binding.UxmlSerializedData.property))
        {
            return false;
        }

        return !context.isReadOnly && (hasBinding || isInline);
    }

    void UnsetAttribute()
    {
        var result = UxmlAssetUtilities.SynchronizePath(context, boundProperty.propertyPath, false);

        if (!result.success)
            return;

        UnsetAttributeCommand.Execute(CommandSources.Inspector, context.editedVisualTreeAsset,
            result.uxmlAsset,
            result.serializedData as UnityEngine.UIElements.UxmlSerializedData,
            boundAttributeDescription,
            context.element,
            GetFullBindingPath(),
            context.isInTemplateInstance,
            true);
    }

    readonly record struct UnsetAllAttributesContext(
        object attributesOwner,
        UxmlAsset attributesUxmlOwner,
        UnityEngine.UIElements.UxmlSerializedData attributesSerializedData,
        UxmlSerializedDataDescription description,
        List<string> ignoredAttributeNames,
        bool success)
    {
        public readonly object attributesOwner = attributesOwner;
        public readonly UxmlAsset attributesUxmlOwner = attributesUxmlOwner;
        public readonly UnityEngine.UIElements.UxmlSerializedData attributesSerializedData = attributesSerializedData;
        public readonly UxmlSerializedDataDescription description = description;
        public readonly List<string> ignoredAttributeNames = ignoredAttributeNames;
        public readonly bool success = success;
    }

    UnsetAllAttributesContext ResolveUnsetAllAttributesContext()
    {
        var bindingsPath = $"{context.serializedBasePath}.bindings.Array.data";

        // Simple solution to handle BindingView
        if (boundProperty.propertyPath.Contains(bindingsPath))
        {
            var startingIndex = bindingsPath.Length;
            var closingBracketIndex = boundProperty.propertyPath.IndexOf(']', startingIndex);

            if (closingBracketIndex == -1)
            {
                // Invalid path format, fall back to default behavior
                return new UnsetAllAttributesContext(
                    context.element,
                    context.elementAsset,
                    context.uxmlSerializedData,
                    context.uxmlSerializedDataDescription,
                    null,
                    true);
            }
            else
            {
                var bindingRootProperty = boundProperty.propertyPath.Substring(0, closingBracketIndex + 1) +
                                          $".{nameof(Binding.property)}";
                var syncResult = UxmlAssetUtilities.SynchronizePath(context, bindingRootProperty, false);

                if (!syncResult.success)
                {
                    // Path synchronization failed, fall back to default behavior
                    return new UnsetAllAttributesContext(
                        context.element,
                        context.elementAsset,
                        context.uxmlSerializedData,
                        context.uxmlSerializedDataDescription,
                        null,
                        false);
                }
                else
                {
                    return new UnsetAllAttributesContext(
                        syncResult.attributeOwner,
                        syncResult.uxmlAsset,
                        syncResult.serializedData as UnityEngine.UIElements.UxmlSerializedData,
                        syncResult.dataDescription,
                        s_BindingIgnoredAttributeNames,
                        true);
                }
            }
        }
        else
        {
            return new UnsetAllAttributesContext(
                context.element,
                context.elementAsset,
                context.uxmlSerializedData,
                context.uxmlSerializedDataDescription,
                null,
                true);
        }
    }

    bool CanUnsetAllAttributes()
    {
        var resolvedContext = ResolveUnsetAllAttributesContext();

        return !context.isReadOnly && UxmlAssetUtilities.IsAnyAttributeSet(
            context.editedVisualTreeAsset,
            resolvedContext.attributesOwner,
            resolvedContext.attributesUxmlOwner,
            resolvedContext.attributesSerializedData,
            resolvedContext.description,
            context.isInTemplateInstance,
            resolvedContext.ignoredAttributeNames);
    }

    void UnsetAllAttributes()
    {
        var resolvedContext = ResolveUnsetAllAttributesContext();

        if (!resolvedContext.success)
            return;

        UnsetAllAttributesCommand.Execute(CommandSources.Inspector, context.editedVisualTreeAsset,
            resolvedContext.attributesUxmlOwner,
            resolvedContext.attributesSerializedData,
            resolvedContext.description,
            context.element,
            context.isInTemplateInstance,
            resolvedContext.ignoredAttributeNames);
    }

    void UpdateBoundAttribute()
    {
        UxmlSerializedAttributeDescription desc = null;

        m_OverrideRow.trackedProperties.Clear();

        if (boundProperty != null)
        {
            var parentProperty = boundProperty.GetParentProperty();

            if (parentProperty is { propertyType: SerializedPropertyType.ManagedReference, managedReferenceValue: UnityEngine.UIElements.UxmlSerializedData parentData })
            {
                var dataDesc = UxmlSerializedDataRegistry.GetDescription(parentData.GetType().DeclaringType?.FullName);

                if (dataDesc != null)
                {
                    desc = dataDesc.FindAttributeWithPropertyName(boundProperty.name);
                }
            }

            m_OverrideRow.AddTrackedProperty(boundProperty.name);
            m_OverrideRow.AddTrackedProperty(boundProperty.displayName);

            if (desc == null && parentProperty is not { isArray: true })
                Debug.LogError($"Property '{boundProperty.name}' is not associated with a valid UXML attribute description.");
        }

        boundAttributeDescription = desc;
    }

    void UpdateFieldFromBoundAttribute()
    {
        if (m_ContentContainer.bindable is PropertyField propertyField)
        {
            if (boundAttributeDescription != null)
            {
                if (string.IsNullOrEmpty(propertyField.label))
                    propertyField.label = StyleSheetUtility.ConvertDashToHuman(boundAttributeDescription.name);
            }
            else
            {
                propertyField.label = string.Empty;
            }
        }
    }

    private static void SendTrackPropertyEvent(ITrackablePropertyProvider provider, VisualElement target, string property, PropertyTrackingType type)
    {
        using var evt = TrackPropertyEvent.GetPooled(provider, property);
        evt.target = target;
        target.SendEvent(evt);
    }

    private void OnPropertyChanged(object obj, SerializedProperty property)
    {
        ScheduleRefresh();
    }

    /// <summary>
    /// Custom update delegate for field affordance data.
    /// </summary>
    public delegate void CustomFieldAffordanceDataUpdate(UxmlAttributeFieldDecorator decorator, in FieldAffordanceData data,  VisualElement element,  Binding binding,  bool isInline);

    /// <summary>
    /// Allows custom update logic for the field affordance data.
    /// </summary>
    public CustomFieldAffordanceDataUpdate customFieldAffordanceDataUpdate;

    static bool IsAttributeOverridden(UxmlAttributeFieldDecorator fieldDecorator)
    {
        var result = UxmlAssetUtilities.SynchronizePath(fieldDecorator.context, fieldDecorator.boundProperty.propertyPath, false);

        if (result.success)
        {
            var serializedData = result.serializedData as UnityEngine.UIElements.UxmlSerializedData;

            // The boundAttributeDescription may be stale when RefreshBinding fires before SerializedPropertyBindEvent
            // has had a chance to update it. In that case the field info stored on the description belongs to the old
            // type and would throw an ArgumentException when accessed on the new serializedData type.
            // Skip the override check until the decorator is fully in sync.
            if (serializedData != null &&
                fieldDecorator.boundAttributeDescription?.serializedFieldAttributeFlags is { DeclaringType: { } declaringType } &&
                !declaringType.IsAssignableFrom(serializedData.GetType()))
                return false;

            return UxmlAssetUtilities.IsAttributeOverridden(
                fieldDecorator.context.editedVisualTreeAsset,
                result.uxmlAsset == fieldDecorator.context.elementAsset ? fieldDecorator.context.element : null,
                result.uxmlAsset, serializedData, fieldDecorator.boundAttributeDescription
                , fieldDecorator.context.isInTemplateInstance);
        }
        return false;
    }

    public void ScheduleRefresh()
    {
        m_RefreshBinding.MarkDirty();
    }

    internal void Refresh()
    {
        // Ensure that the boundProperty is not null and sync with its parent serializedObject (isValid will ensure that it is sync)
        if (context?.element == null || boundField == null || boundProperty is not { isValid: true } || boundAttributeDescription == null)
            return;

        var isInline = IsAttributeOverridden(this);

        Binding binding = null;
        bool isBindingSuccessful = false;
        if (context is { element: not null })
        {
            binding = context.element.GetBinding(GetFullBindingPath());
            if (binding != null)
            {
                // Check if binding is actually successful
                isBindingSuccessful = context.element.TryGetLastBindingToUIResult(
                    GetFullBindingPath(),
                    out var bindingResult) &&
                    bindingResult.status == BindingStatus.Success;
            }
            FieldAffordanceController.UpdateFieldAffordanceData(m_AffordanceElement.fieldAffordanceData, context.element, binding, isInline);
            customFieldAffordanceDataUpdate?.Invoke(this, m_AffordanceElement.fieldAffordanceData, context.element, binding, isInline);
        }

        var isOverridden = isInline || binding != null;
        OnTrackedPropertyChanged?.Invoke(this, boundProperty.propertyPath,
            isOverridden ? TrackedPropertyType.MarkOverride : TrackedPropertyType.ClearOverride);

        EnableInClassList(s_InlineFieldUssClassName, isInline);
        EnableInClassList(s_BoundFieldUssClassName, binding != null);
        OnTrackedPropertySourceChanged?.Invoke(this, boundProperty.propertyPath, false, binding != null, false);

        if (m_ContentContainer.bindable is VisualElement bindableElement)
        {
            bindableElement.SetEnabled(!isBindingSuccessful);
        }
    }

    public BindingId GetFullBindingPath() =>
        m_CachedFullBindingPath ??= m_BoundProperty?.GetFullBindingPath() ?? string.Empty;

    void OnAttachedToPanel(AttachToPanelEvent evt)
    {
        m_PropertyView = GetFirstAncestorOfType<UxmlSerializedDataPropertyView>();
        m_PropertyView?.RegisterUxmlAttributeFieldDecorator(this);
        m_Context?.editingController?.RegisterUxmlAttributeFieldDecorator(this);
    }

    void OnDetachedFromPanel(DetachFromPanelEvent evt)
    {
        m_PropertyView?.UnregisterUxmlAttributeFieldDecorator(this);
    }

    void OnFieldDetachedFromPanel(DetachFromPanelEvent evt)
    {
        // Ensure that the boundProperty is not null and sync with its parent serializedObject (isValid will ensure that it is sync)
        if (boundProperty is not { isValid: true })
            return;
        OnTrackedPropertyChanged?.Invoke(this, boundProperty.propertyPath, TrackedPropertyType.StopTracking);
    }

    void TrackPropertyValueChange()
    {
        if (m_BoundProperty is not { isValid: true })
            return;

        this.TrackPropertyValue(m_BoundProperty, OnPropertyChanged);


        if (m_BoundPropertyFlags is not { isValid: true })
            return;
        this.TrackPropertyValue(m_BoundPropertyFlags, OnPropertyChanged);
    }

    void UntrackPropertyValueChange()
    {
        if (m_BoundProperty != null)
        {
            var _ = m_BoundProperty.isValid; // isValid tries to sync the property with is parent object in case it was not the case.
            // Untrack the property even if it may be invalid
            this.UntrackPropertyValue(m_BoundProperty, OnPropertyChanged);
        }

        if (m_BoundPropertyFlags != null)
        {
            var _ = m_BoundPropertyFlags.isValid; // isValid tries to sync the property with is parent object  in case it was not the case.
            // Untrack the property even if it may be invalid
            this.UntrackPropertyValue(m_BoundPropertyFlags, OnPropertyChanged);
        }
    }

    void OnPropertyFieldReset()
    {
        if (boundProperty == null || boundAttributeDescription == null)
            return;

        if (boundAttributeDescription.isList && boundAttributeDescription.isUxmlObject)
        {
            HandleUxmlObjectListProperty();
        }
    }

    void HandleUxmlObjectListProperty()
    {
        var propertyField = m_ContentContainer.bindable as PropertyField;

        if (propertyField == null || propertyField.childCount == 0)
            return;

        var listView = propertyField.Q<ListView>(classes: PropertyField.listViewUssClassName);
        if (listView == null)
            return;

        listView.Q(ListView.footerAddButtonName).EnableInClassList(ListView.footerAddButtonWithMenuNameUnique, boundAttributeDescription.uxmlObjectAcceptedTypes.Count > 1);
        listView.overridingAddButtonBehavior = OnListViewAddButtonClicked;
        listView.onRemove = OnListViewRemoveButtonClicked;
    }

    void OnListViewAddButtonClicked(BaseListView listView, Button button)
    {
        UxmlSerializedDataPropertyView.ShowAddUxmlObjectMenu(button, boundAttributeDescription, t =>
        {
            UxmlAssetUtilities.AddUxmlObjectToSerializedData(context, boundProperty, t);
        });
    }

    void OnListViewRemoveButtonClicked(BaseListView listView)
    {
        var index = listView.selectedIndex >= 0 ? listView.selectedIndex : boundProperty.arraySize - 1;

        UxmlAssetUtilities.RemoveArrayItemFromSerializedData(context, boundProperty, index);
    }
}
