// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitAuthoringFramework not yet converted
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// An Editor-only control that displays a serialized UXML attribute in the Inspector, with override
/// indicators and binding affordances.
/// </summary>
/// <remarks>
/// Use <see cref="UxmlAttributeField"/> when you build custom drawers that derive from
/// <see cref="UxmlSerializedDataPropertyDrawer"/>, so your fields match the auto-generated Inspector.
/// The control shows the current attribute state: default, set inline, overridden in a template
/// instance, or driven by a data binding.
///
/// When you don't set <see cref="label"/>, it falls back to the display name of the bound
/// <see cref="UnityEditor.SerializedProperty"/>.
/// </remarks>
/// <example>
/// The following example creates a field bound to a property from <c>FindPropertyRelative</c> inside a drawer.
/// <code source="../../../../../Documentation/ManualDocs/com.unity.documentation-examples/UIToolkit/Scripts/UxmlAttributeFieldExamples.cs" region="example-bound"/>
/// </example>
/// <seealso cref="UxmlAttributeFieldDecorator"/>
/// <seealso cref="UxmlSerializedDataPropertyDrawer"/>
[UxmlElement]
public partial class UxmlAttributeField : VisualElement
{
    /// <summary>
    /// The USS class name for a UXML attribute field.
    /// </summary>
    /// <example>
    /// The following example finds and styles these controls by their USS class names.
    /// <code source="../../../../../Documentation/ManualDocs/com.unity.documentation-examples/UIToolkit/Scripts/UxmlAttributeFieldExamples.cs" region="example-ussclass"/>
    /// </example>
    public const string ussClassName = "unity-uxml-attribute-field";

    PropertyField m_PropertyField;
    UxmlAttributeFieldDecorator m_Decorator;

    /// <summary>
    /// The decorator that draws the override indicator bar, binding affordances, and context menu for this field.
    /// </summary>
    /// <example>
    /// The following example reaches the decorator to add an action button next to the field.
    /// <code source="../../../../../Documentation/ManualDocs/com.unity.documentation-examples/UIToolkit/Scripts/UxmlAttributeFieldExamples.cs" region="example-decorator"/>
    /// </example>
    /// <seealso cref="UxmlAttributeFieldDecorator"/>
    public UxmlAttributeFieldDecorator decorator => m_Decorator;

    /// <summary>
    /// Returns the serialized property bound to this UXML attribute field.
    /// </summary>
    internal SerializedProperty boundProperty => m_PropertyField.serializedProperty;

    /// <summary>
    /// The label shown next to the field's input control.
    /// </summary>
    /// <remarks>
    /// When you don't set a label, it falls back to the display name of the bound
    /// <see cref="UnityEditor.SerializedProperty"/>. Set a label to keep it stable regardless of the property name.
    /// </remarks>
    [UxmlAttribute]
    public string label
    {
        get => m_PropertyField.label;
        set => m_PropertyField.label = value;
    }

    /// <summary>
    /// The binding path of the serialized property this field displays and edits.
    /// </summary>
    /// <remarks>
    /// The path resolves relative to the <c>UxmlSerializedData</c> property, because the property view
    /// sets up the binding context. Set this to bind a field you created with the parameterless constructor.
    /// </remarks>
    [UxmlAttribute]
    public string bindingPath
    {
        get => m_PropertyField.bindingPath;
        set => m_PropertyField.bindingPath = value;
    }

    /// <summary>
    /// The UXML attributes authoring context associated with this field.
    /// </summary>
    internal UxmlAttributesEditingContext Context => m_Decorator.context;

    /// <summary>
    /// Creates an unbound field. Set <see cref="bindingPath"/> to bind it to a serialized property.
    /// </summary>
    /// <remarks>
    /// The field starts unbound. Set <see cref="bindingPath"/> to bind it, or use
    /// <see cref="UxmlAttributeField(UnityEditor.SerializedProperty)"/> to bind it when you create it.
    /// </remarks>
    /// <example>
    /// The following example creates an unbound field and sets <see cref="bindingPath"/> to bind it.
    /// <code source="../../../../../Documentation/ManualDocs/com.unity.documentation-examples/UIToolkit/Scripts/UxmlAttributeFieldExamples.cs" region="example-unbound"/>
    /// </example>
    public UxmlAttributeField() : this(null)
    {
    }

    /// <summary>
    /// Creates a field bound to the given serialized property.
    /// </summary>
    /// <param name="property">The property to display and edit. Pass null to create an unbound field.</param>
    /// <remarks>
    /// When <paramref name="property"/> is null, the field starts unbound. Set <see cref="bindingPath"/>
    /// after you create it, or use the parameterless constructor.
    /// </remarks>
    /// <example>
    /// The following example creates a field bound to a property from <c>FindPropertyRelative</c>.
    /// <code source="../../../../../Documentation/ManualDocs/com.unity.documentation-examples/UIToolkit/Scripts/UxmlAttributeFieldExamples.cs" region="example-bound"/>
    /// </example>
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
/// An Editor-only container for bindable fields. It adds an override indicator bar, binding state
/// indicators, and a context menu for use in a custom drawer.
/// </summary>
/// <remarks>
/// Use <see cref="UxmlAttributeFieldDecorator"/> to wrap a <see cref="UnityEngine.UIElements.TextField"/>
/// or another <see cref="UnityEngine.UIElements.BaseField{T}"/> in a custom layout or next to action buttons,
/// while it still takes part in the override and binding system. For a plain property display, use
/// <see cref="UxmlAttributeField"/> instead.
///
/// The decorator can hold several children, but it binds only its first direct
/// <see cref="UnityEngine.UIElements.IBindable"/> child, and detects the binding path when that child
/// binds to a <see cref="UnityEditor.SerializedProperty"/>.
///
/// Right-click the decorator to open a context menu whose actions depend on the attribute state:
/// add a binding, edit or remove a binding, unset the attribute, or unset all attributes on the element.
/// </remarks>
/// <example>
/// The following example wraps a text field in a decorator next to action buttons.
/// <code source="../../../../../Documentation/ManualDocs/com.unity.documentation-examples/UIToolkit/Scripts/UxmlSerializedDataPropertyDrawerExamples.cs" region="example3"/>
/// </example>
/// <seealso cref="UxmlAttributeField"/>
/// <seealso cref="UxmlSerializedDataPropertyDrawer"/>
[UxmlElement]
public partial class UxmlAttributeFieldDecorator : VisualElement, ITrackablePropertyProvider
{
    /// <summary>
    /// The USS class name for a UXML attribute field decorator.
    /// </summary>
    public const string ussClassName = "unity-uxml-attribute-field-decorator";
    internal const string contentContainerUssClassName = ussClassName + "__content-container";
    internal const string affordanceElementName = "affordance-element";
    internal const string affordanceElementUssClassName = ussClassName + "__affordance-element";
    internal static readonly string s_InlineFieldUssClassName = "property-field__inline-value";
    internal static readonly UniqueStyleString s_BoundFieldUssClassName = new("property-field__bound");

    internal static readonly string k_AddBindingText = L10n.Tr("Add Binding", null);
    internal static readonly string k_RemoveBindingText = L10n.Tr("Remove Binding", null);
    internal static readonly string k_EditBindingText = L10n.Tr("Edit Binding", null);
    internal static readonly string k_ViewBindingText = L10n.Tr("View Binding", null);
    internal static readonly string k_UnsetText = L10n.Tr("Unset", null);
    internal static readonly string k_UnsetAllText = L10n.Tr("Unset all", null);

    readonly List<string> s_BindingIgnoredAttributeNames = ["property"];

    const string k_ArraySizeRelativePath = "Array.size";

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
    SerializedProperty m_BoundPropertyArraySize;
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
    /// The element that holds this decorator's children.
    /// </summary>
    /// <remarks>
    /// Add a bindable child here. The decorator tracks the first direct
    /// <see cref="UnityEngine.UIElements.IBindable"/> child it receives and listens for that child's bind event,
    /// which drives the override indicator, the binding affordances, and the context menu. It ignores any bindable
    /// children added after the first, so add the field you want the decorator to follow before the others.
    /// </remarks>
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
            m_BoundPropertyArraySize = m_BoundProperty is { isArray: true }
                ? m_BoundProperty.FindPropertyRelative(k_ArraySizeRelativePath)
                : null;
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
    internal event EventHandler boundPropertyChanged;

    /// <summary>
    /// Creates a decorator that has no bound property.
    /// </summary>
    /// <remarks>
    /// Add a bindable child element after you construct the decorator. The decorator stays unbound until that child
    /// binds to a <see cref="UnityEditor.SerializedProperty"/>, and only then does it show the override indicator and
    /// the context menu. Use this constructor when you build the hierarchy yourself rather than letting a drawer
    /// create it for you.
    /// </remarks>
    /// <example>
    /// <code source="../../../../../Documentation/ManualDocs/com.unity.documentation-examples/UIToolkit/Scripts/UxmlSerializedDataPropertyDrawerExamples.cs" region="example-decorator-constructor"/>
    /// </example>
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

    internal void RebindProperty(SerializedProperty property)
    {
        boundProperty = property;
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

            // Add a separator in case then menu is already filled with items (e.g: TextField's input)
            menu.AppendSeparator();

            // The binding section is shown only for contexts that support attribute binding, and only when
            // the path addresses a bindable target. The context validates the path: an element checks its
            // own property bag, a component checks the ${component:<Type>}.<field> selector against its data.
            // The Unset / Unset All actions below always apply.
            BindingId bindingPath = default;
            var isBindableProperty = false;
            if (context.supportsAttributeBindings)
            {
                bindingPath = GetFullBindingPath();
                isBindableProperty = context.IsBindablePath(context.element, bindingPath);
            }

            // The element path additionally requires a resolved attribute description (rows without one,
            // like composite sub-fields, get no binding menu). A component attribute has no element-side
            // description by design — its attributes live on the component serialized data — so the
            // component context qualifies on the bindable-path check alone.
            if (isBindableProperty && (boundAttributeDescription != null || context is ComponentUxmlAttributesEditingContext))
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

            if (context.showsAncestorOverrides && boundAttributeDescription != null)
            {
                menu.AppendSeparator();
                AttributeOverridesMenu.Append(menu, context, boundAttributeDescription);
            }
        };
    }

    bool CanUnsetAttribute()
    {
        if (context?.element == null || boundProperty is not { isValid: true })
            return false;

        var result = UxmlAssetUtilities.SynchronizePath(context, boundProperty.propertyPath, false);

        // Disable Unset for the "property" property of binding.
        if (result is { success: true, serializedData: Binding.UxmlSerializedData } &&
            boundProperty.name == nameof(Binding.UxmlSerializedData.property))
        {
            return false;
        }

        // Asked of the element rather than read off the affordance, whose single source-type channel an
        // ancestor override takes over from an unsuccessful binding.
        var hasBinding = context.element.GetBinding(GetFullBindingPath()) != null;

        return !context.isReadOnly && (hasBinding || IsAttributeOverridden(this, in result));
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

        context.rootSerializedObject.UpdateIfRequiredOrScript();
        ScheduleRefresh();
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

        context.rootSerializedObject.UpdateIfRequiredOrScript();
        context.editingController.RefreshAllDecorators();
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
    internal delegate void CustomFieldAffordanceDataUpdate(UxmlAttributeFieldDecorator decorator, in FieldAffordanceData data,  VisualElement element,  Binding binding,  bool isInline);

    /// <summary>
    /// Allows custom update logic for the field affordance data.
    /// </summary>
    internal CustomFieldAffordanceDataUpdate customFieldAffordanceDataUpdate;

    static bool IsAttributeOverridden(UxmlAttributeFieldDecorator fieldDecorator)
    {
        var result = UxmlAssetUtilities.SynchronizePath(fieldDecorator.context, fieldDecorator.boundProperty.propertyPath, false);
        return IsAttributeOverridden(fieldDecorator, in result);
    }

    static bool IsAttributeOverridden(UxmlAttributeFieldDecorator fieldDecorator, in SynchronizePathResult result)
    {
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

            // A component attribute at a template instance stores its override in the template's component
            // overrides, which the shared element path below cannot see; let the context answer instead.
            if (fieldDecorator.context.isInTemplateInstance &&
                fieldDecorator.context.IsTemplateAttributeOverridden(fieldDecorator.boundAttributeDescription) is { } overridden)
                return overridden;

            return UxmlAssetUtilities.IsAttributeOverridden(
                fieldDecorator.context.editedVisualTreeAsset,
                result.uxmlAsset == fieldDecorator.context.elementAsset ? fieldDecorator.context.element : null,
                result.uxmlAsset, serializedData, fieldDecorator.boundAttributeDescription
                , fieldDecorator.context.isInTemplateInstance);
        }
        return false;
    }

    internal void ScheduleRefresh()
    {
        m_RefreshBinding.MarkDirty();
    }

    internal void Refresh()
    {
        // Ensure that the boundProperty is not null and sync with its parent serializedObject (isValid will ensure that it is sync)
        if (context?.element == null || boundField == null || boundProperty is not { isValid: true } || boundAttributeDescription == null)
            return;

        var isInline = IsAttributeOverridden(this);

        var binding = context.element.GetBinding(GetFullBindingPath());
        bool isBindingSuccessful = false;
        if (binding != null)
        {
            // Check if binding is actually successful
            isBindingSuccessful = context.element.TryGetLastBindingToUIResult(
                GetFullBindingPath(),
                out var bindingResult) &&
                bindingResult.status == BindingStatus.Success;
        }

        // A live binding drives the runtime value, so it wins over the override state.
        var isDrivenByAncestorOverride = false;
        VisualTreeAsset ancestorOverrideDocument = null;
        if (context.showsAncestorOverrides && !isBindingSuccessful)
        {
            isDrivenByAncestorOverride = UxmlAssetUtilities.TryGetDrivingAttributeOverride(context.element,
                boundAttributeDescription, out ancestorOverrideDocument);
        }

        FieldAffordanceController.UpdateFieldAffordanceData(m_AffordanceElement.fieldAffordanceData, context.element, binding, isInline);
        customFieldAffordanceDataUpdate?.Invoke(this, m_AffordanceElement.fieldAffordanceData, context.element, binding, isInline);

        // After the custom hook, so no other update can replace the override state it knows nothing about.
        // A binding keeps the source type it needs to report, even a failing one; the lock below is all
        // the override contributes there.
        var reportsAncestorOverride = isDrivenByAncestorOverride && binding == null;
        m_AffordanceElement.fieldAffordanceData.definingDocument = reportsAncestorOverride ? ancestorOverrideDocument : null;
        if (reportsAncestorOverride)
            m_AffordanceElement.fieldAffordanceData.sourceTypeInfo = FieldAffordanceSourceInfoType.AncestorAttributeOverride;

        var isOverridden = isInline || binding != null;
        OnTrackedPropertyChanged?.Invoke(this, boundProperty.propertyPath,
            isOverridden ? TrackedPropertyType.MarkOverride : TrackedPropertyType.ClearOverride);

        EnableInClassList(s_InlineFieldUssClassName, isInline);
        EnableInClassList(s_BoundFieldUssClassName, binding != null);
        OnTrackedPropertySourceChanged?.Invoke(this, boundProperty.propertyPath, false, binding != null, false);

        if (m_ContentContainer.bindable is VisualElement bindableElement)
        {
            bindableElement.SetEnabled(!isBindingSuccessful && !isDrivenByAncestorOverride);
        }
    }

    internal BindingId GetFullBindingPath() =>
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

        if (m_BoundPropertyArraySize is { isValid: true })
            this.TrackPropertyValue(m_BoundPropertyArraySize, OnArraySizeChanged);

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

        if (m_BoundPropertyArraySize != null)
        {
            var _ = m_BoundPropertyArraySize.isValid;
            this.UntrackPropertyValue(m_BoundPropertyArraySize, OnArraySizeChanged);
        }
    }

    void OnArraySizeChanged(object obj, SerializedProperty property)
    {
        if (context == null
            || boundProperty is not { isValid: true, isArray: true }
            || boundAttributeDescription is not { isList: true, isUxmlObject: true })
            return;

        var acceptedTypes = boundAttributeDescription.uxmlObjectAcceptedTypes;

        // With more than one accepted type there is no unambiguous choice, so leave the items null.
        if (acceptedTypes.Count != 1 || !RejectsNullItems(acceptedTypes[0]))
            return;

        if (!UxmlAssetUtilities.FillNullArrayItemsInSerializedData(context, boundProperty, acceptedTypes[0]))
            return;

        FindUxmlObjectListView()?.Rebuild();
    }

    static bool RejectsNullItems(Type uxmlSerializedDataType)
    {
        var itemType = uxmlSerializedDataType?.DeclaringType;

        return itemType == typeof(Column) || itemType == typeof(SortColumnDescription);
    }

    ListView FindUxmlObjectListView()
    {
        var propertyField = m_ContentContainer.bindable as PropertyField;

        return propertyField?.Q<ListView>(classes: PropertyField.listViewUssClassName);
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
        var listView = FindUxmlObjectListView();
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
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
