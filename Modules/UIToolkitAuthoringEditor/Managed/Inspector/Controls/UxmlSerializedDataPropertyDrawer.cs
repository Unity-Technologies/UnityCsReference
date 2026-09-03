// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitAuthoringFramework not yet converted
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// View that displays serialized properties of a UXMLSerializedData instance.
/// </summary>
[UxmlElement]
internal partial class UxmlSerializedDataPropertyView : BindableElement
{
    public const string ussClassName = "unity-uxml-serialized-data-property-view";
    public const string AddUxmlObjectMenuPropertyKey = "UxmlSerializedDataPropertyView_AddUxmlObjectMenu";

    UxmlAttributeFieldDecorator m_FieldDecoratorForListItem;
    UxmlAttributesEditingContext m_Context;
    List<UxmlAttributeFieldDecorator> m_RegisteredDecorators = new();
    bool m_IsUxmlObject = false;

    public override VisualElement contentContainer =>
        m_FieldDecoratorForListItem != null ? m_FieldDecoratorForListItem : this;

    public UxmlAttributesEditingContext context
    {
        get => m_Context;
        set
        {
            if (m_Context == value)
                return;

            SetContext(value);

            // Propagate the context to any child UxmlSerializedDataPropertyView
            var childPropertyViews = this.Query<UxmlSerializedDataPropertyView>().ToList();
            foreach (var childPropertyView in childPropertyViews)
            {
                childPropertyView.SetContext(value);
            }
        }
    }

    void SetContext(UxmlAttributesEditingContext context)
    {
        if (m_Context != null)
            m_Context.contextChanged -= OnContextChanged;

        m_Context = context;

        if (m_Context != null)
            m_Context.contextChanged += OnContextChanged;

        // Propagate the context to any registered UxmlAttributeFieldDecorator
        foreach (var decorator in m_RegisteredDecorators)
        {
            decorator.context = context;
        }
        UpdateEnableState();
    }

    /// <summary>
    /// Constructs a UxmlSerializedDataPropertyView with no bound property.
    /// </summary>
    public UxmlSerializedDataPropertyView() : this(null)
    {
    }

    /// <summary>
    /// Constructs a UxmlSerializedDataPropertyView.
    /// </summary>
    /// <param name="property">The serialized property created from the UXMLSerializedData instance to display</param>
    public UxmlSerializedDataPropertyView(SerializedProperty property)
    {
        AddToClassList(ussClassName);

        RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
        RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);

        m_IsUxmlObject = property?.managedReferenceValue != null && property.managedReferenceValue is not VisualElement.UxmlSerializedData;

        // Find the parent serialized property and check if it is a list or array.
        // If so then this UxmlSerializedDataPropertyView represents a list item. Therefore, we add a field decorator
        // for the list item itself because list items are created by ListViewSerializedObjectBinding.MakeItem,
        // which only creates instances of PropertyField.
        var parentProperty = property?.GetParentProperty();

        if (parentProperty is { isArray: true })
        {
            m_FieldDecoratorForListItem = new UxmlAttributeFieldDecorator(property);
            hierarchy.Add(m_FieldDecoratorForListItem);
        }
    }

    public void RegisterUxmlAttributeFieldDecorator(UxmlAttributeFieldDecorator decorator)
    {
        decorator.context = context;
        m_RegisteredDecorators.Add(decorator);
    }

    public void UnregisterUxmlAttributeFieldDecorator(UxmlAttributeFieldDecorator decorator)
    {
        m_RegisteredDecorators.Remove(decorator);
        decorator.context = null;
    }

    void OnAttachedToPanel(AttachToPanelEvent evt)
    {
        // Do nothing if we already have a context
        if (context != null)
            return;

        var parentView = GetFirstAncestorOfType<UxmlSerializedDataPropertyView>();

        if (parentView != null)
            context = parentView.context;
    }

    void OnDetachedFromPanel(DetachFromPanelEvent evt)
    {
        context = null;
    }

    void OnContextChanged(object obj, UxmlAttributesEditingContext.ContextChangedEventArgs args)
    {
        UpdateEnableState();
    }

    void UpdateEnableState()
    {
        if (context != null)
        {
            var readOnly = context.isReadOnly || (context.isInTemplateInstance && m_IsUxmlObject);

            SetEnabled(!readOnly);
        }
        else
        {
            SetEnabled(false);
        }
    }

    [EventInterest(typeof(SerializedPropertyBindEvent))]
    protected override void HandleEventBubbleUp(EventBase evt)
    {
        base.HandleEventBubbleUp(evt);

        // Stop propagation of SerializedPropertyBindEvent as binding to UxmlSerializedData is actually not supported.
        // It is mainly used to bind a UxmlSerializedDataPropertyView to a UxmlSerializedData property so that
        // children of this view can use binding path relative to the bound UxmlSerializedData.
        if (evt is SerializedPropertyBindEvent bindEvent)
        {
            // Update m_IsUxmlObject when the view is rebound to handle view recycling in lists
            var property = bindEvent.bindProperty;
            m_IsUxmlObject = property?.managedReferenceValue != null && property.managedReferenceValue is not VisualElement.UxmlSerializedData;

            // Forward the rebind to the list-item decorator so its boundProperty stays in sync when
            // a virtualized row is recycled to a different array index.
            m_FieldDecoratorForListItem?.RebindProperty(property);

            UpdateEnableState();

            evt.StopPropagation();
        }
    }

    public static void ShowAddUxmlObjectMenu(VisualElement element, UxmlSerializedAttributeDescription attribute,
        Action<Type> action)
    {
        if (attribute.uxmlObjectAcceptedTypes.Count == 0)
            return;

        if (attribute.uxmlObjectAcceptedTypes.Count == 1)
        {
            action(attribute.uxmlObjectAcceptedTypes[0]);
            return;
        }

        var menu = new GenericDropdownMenu();

        foreach (var type in attribute.uxmlObjectAcceptedTypes)
        {
            var name = ObjectNames.NicifyVariableName(type.DeclaringType.Name);

            menu.AddItem(name, false, () => action(type));
        }

        element.SetProperty(AddUxmlObjectMenuPropertyKey, menu);
        menu.onClose += () => element.ClearProperty(AddUxmlObjectMenuPropertyKey);
        menu.DropDown(element.worldBound, element, DropdownMenuSizeMode.Auto);
    }
}

/// <summary>
/// A base property drawer for UXML serialized data that renders attribute fields in the Inspector.
/// </summary>
/// <remarks>
/// Derive from <see cref="UxmlSerializedDataPropertyDrawer"/> and apply
/// <see cref="UnityEditor.CustomPropertyDrawer"/> for a <c>UxmlSerializedData</c> type to customize how
/// its attributes appear in the authoring Inspector. The base class generates a field for each visible
/// attribute. Override <see cref="CreateChildPropertiesGUI"/> to control which attributes appear and in
/// what order, and <see cref="CreateChildPropertyGUI"/> to control how each one is drawn.
/// </remarks>
/// <example>
/// The following example defines a minimal drawer that uses the default field generation.
/// <code source="../../../../../Documentation/ManualDocs/com.unity.documentation-examples/UIToolkit/Scripts/UxmlSerializedDataPropertyDrawerExamples.cs" region="example1"/>
/// </example>
/// <seealso cref="UxmlAttributeField"/>
/// <seealso cref="UxmlAttributeFieldDecorator"/>
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
[CustomPropertyDrawer(typeof(UxmlSerializedData), true)]
public class UxmlSerializedDataPropertyDrawer : PropertyDrawer
{
    internal static readonly string k_CreateOrRemoveDataButtonUssName = UxmlSerializedDataPropertyView.ussClassName + "__create-or-remove-data-button";
    internal static readonly string k_CreateDataButtonUssName = k_CreateOrRemoveDataButtonUssName + "--add";
    internal static readonly string k_CreateDataButtonWithMenuUssName = k_CreateOrRemoveDataButtonUssName + "--add-with-menu";
    internal static readonly string k_RemoveDataButtonUssName = k_CreateOrRemoveDataButtonUssName + "--remove";

    /// <summary>
    /// Creates the root element for the property GUI. This method is sealed.
    /// </summary>
    /// <param name="property">The property that represents the <c>UxmlSerializedData</c> instance.</param>
    /// <returns>The root element for the Inspector.</returns>
    /// <remarks>
    /// This method is sealed so the binding context stays in place. Override
    /// <see cref="CreatePropertyGUI(UnityEngine.UIElements.VisualElement,UnityEditor.SerializedProperty)"/>,
    /// <see cref="CreateChildPropertiesGUI"/>, or <see cref="CreateChildPropertyGUI"/> to customize the result.
    /// </remarks>
    public sealed override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var container = new UxmlSerializedDataPropertyView(property);

        CreatePropertyGUI(container, property);

        var isPropertyToUxmlObjectSerializedData = property.managedReferenceValue is not VisualElement.UxmlSerializedData;

        if (isPropertyToUxmlObjectSerializedData)
        {
            var parent = property.Copy();

            if (parent.Parent() && !parent.isArray)
            {
                var addOrRemoveButton = new Button().WithClassList(k_CreateOrRemoveDataButtonUssName);

                UpdateAddOrRemoveButtonState(property, addOrRemoveButton);
                addOrRemoveButton.clicked += () => OnAddOrRemoveButtonClicked(addOrRemoveButton);
                addOrRemoveButton.RegisterCallback<AttachToPanelEvent>((e) =>
                {
                    var fieldDecorator = addOrRemoveButton.GetFirstAncestorOfType<UxmlAttributeFieldDecorator>();

                    // If the button is not in a field decorator then hide it
                    if (fieldDecorator == null)
                    {
                        addOrRemoveButton.style.display = DisplayStyle.None; // Hide the button;
                        return;
                    }
                    fieldDecorator.boundPropertyChanged += (_, _) => UpdateAddOrRemoveButtonState(property, addOrRemoveButton);

                    // The object can also be created without a rebind, for example when a nested list's bound
                    // size field creates it. Follow the flags so the icon does not stay on add.
                    var flagProperty = property.serializedObject.FindProperty(GetAttributeFlagsPath(property));

                    if (flagProperty != null)
                        addOrRemoveButton.TrackPropertyValue(flagProperty, _ => UpdateAddOrRemoveButtonState(property, addOrRemoveButton));
                });

                var foldout = container.childCount > 0 ? container[0] as Foldout : null;

                if (foldout != null)
                {
                    foldout.Q<Toggle>().Add(addOrRemoveButton);
                }
                else
                {
                    container.Add(addOrRemoveButton);
                }
            }
        }

        return container;
    }

    static string GetAttributeFlagsPath(SerializedProperty property) => property.propertyPath + "_UxmlAttributeFlags";

    void UpdateAddOrRemoveButtonState(SerializedProperty property, Button addOrRemoveButton)
    {
        if (property is not { isValid: true })
            return;

        var flagProperty = property.serializedObject.FindProperty(GetAttributeFlagsPath(property));
        var isInline = false;

        if (flagProperty != null)
        {
            var uxmlFlagsValue = (UxmlSerializedData.UxmlAttributeFlags)flagProperty.enumValueIndex;
            isInline = UxmlSerializedData.ShouldWriteAttributeValue(uxmlFlagsValue);
        }

        bool shouldRemove = isInline && property.managedReferenceValue != null;

        addOrRemoveButton.EnableInClassList(k_CreateDataButtonUssName, !shouldRemove);
        addOrRemoveButton.EnableInClassList(k_RemoveDataButtonUssName, shouldRemove);

        var fieldDecorator = addOrRemoveButton.GetFirstAncestorOfType<UxmlAttributeFieldDecorator>();

        if (fieldDecorator is { boundAttributeDescription: not null })
        {
            addOrRemoveButton.EnableInClassList(k_CreateDataButtonWithMenuUssName, !shouldRemove && fieldDecorator.boundAttributeDescription.uxmlObjectAcceptedTypes.Count > 1);
        }
    }

    void OnAddOrRemoveButtonClicked(Button addOrRemoveButton)
    {
        bool shouldAdd = addOrRemoveButton.ClassListContains(k_CreateDataButtonUssName);

        if (shouldAdd)
        {
            var fieldDecorator = addOrRemoveButton.GetFirstAncestorOfType<UxmlAttributeFieldDecorator>();

            UxmlSerializedDataPropertyView.ShowAddUxmlObjectMenu(addOrRemoveButton,
                fieldDecorator.boundAttributeDescription, t =>
                {
                    UxmlAssetUtilities.AddUxmlObjectToSerializedData(fieldDecorator.context,
                        fieldDecorator.boundProperty, t);
                });
        }
        else
        {
            var fieldDecorator = addOrRemoveButton.GetFirstAncestorOfType<UxmlAttributeFieldDecorator>();

            UxmlAssetUtilities.AddUxmlObjectToSerializedData(fieldDecorator.context, fieldDecorator.boundProperty, (UxmlSerializedData)null);
        }
    }

    /// <summary>
    /// Creates the property GUI for a UXML serialized data instance.
    /// </summary>
    /// <param name="container">The parent element to add the content to.</param>
    /// <param name="property">The serialized property that represents the UXML serialized data instance.</param>
    /// <remarks>
    /// The default implementation wraps the child properties in a <see cref="UnityEngine.UIElements.Foldout"/>
    /// with the property display name as its label. Override this method to use a flat layout or a different
    /// container.
    /// </remarks>
    /// <example>
    /// The following example replaces the default foldout with a flat layout.
    /// <code source="../../../../../Documentation/ManualDocs/com.unity.documentation-examples/UIToolkit/Scripts/UxmlSerializedDataPropertyDrawerExamples.cs" region="example-create-property-gui"/>
    /// </example>
    protected virtual void CreatePropertyGUI(VisualElement container, SerializedProperty property)
    {
        var foldout = new Foldout() { text = property.displayName, value = true };

        container.Add(foldout);

        if (property.managedReferenceValue == null)
            return;

        CreateChildPropertiesGUI(foldout, property);
    }

    /// <summary>
    /// Creates the GUI for a single attribute of the UXML serialized data.
    /// </summary>
    /// <param name="container">The parent element to add the content to.</param>
    /// <param name="property">The serialized property that represents the UXML serialized data instance.</param>
    /// <param name="childProperty">The serialized property for the attribute to draw.</param>
    /// <remarks>
    /// The default implementation adds a <see cref="UxmlAttributeField"/> bound to
    /// <paramref name="childProperty"/>. Override this method to draw specific attributes with a custom
    /// layout, and call the base method for the rest.
    /// </remarks>
    /// <example>
    /// The following example draws specific attributes with custom layouts and action buttons.
    /// <code source="../../../../../Documentation/ManualDocs/com.unity.documentation-examples/UIToolkit/Scripts/UxmlSerializedDataPropertyDrawerExamples.cs" region="example3"/>
    /// </example>
    protected virtual void CreateChildPropertyGUI(VisualElement container, SerializedProperty property, SerializedProperty childProperty)
    {
        container.Add(new UxmlAttributeField(childProperty));
    }

    /// <summary>
    /// Creates a field for each visible attribute of the UXML serialized data, excluding the specified attributes.
    /// </summary>
    /// <param name="container">The parent element to add the fields to.</param>
    /// <param name="property">The serialized property that represents the UXML serialized data instance.</param>
    /// <param name="excludedPropertyNames">The UXML names of the properties to exclude.</param>
    protected void CreateChildPropertiesExcluding(VisualElement container, SerializedProperty property, params string[] excludedPropertyNames)
    {
        using (HashSetPool<UxmlSerializedAttributeDescription>.Get(out var excludedProperties))
        {
            var description = GetDataDescription(property);
            PopulatePropertyHashSet(excludedProperties, description, excludedPropertyNames);
            CreateChildPropertiesFiltered(container, property, description, (field) => IsVisibleAttribute(field) && !excludedProperties.Contains(field));
        }
    }

    /// <summary>
    /// Creates a field only for the specified attributes of the UXML serialized data.
    /// </summary>
    /// <param name="container">The parent element to add the fields to.</param>
    /// <param name="property">The serialized property that represents the UXML serialized data instance.</param>
    /// <param name="includedPropertyNames">The UXML names of the properties to include.</param>
    protected void CreateChildPropertiesIncluding(VisualElement container, SerializedProperty property, params string[] includedPropertyNames)
    {
        using (HashSetPool<UxmlSerializedAttributeDescription>.Get(out var excludedProperties))
        {
            // We use UXML names because serialized names can change when an attribute is overridden in a derived class.
            var description = GetDataDescription(property);
            PopulatePropertyHashSet(excludedProperties, description, includedPropertyNames);

            // We allow HideInInspector attributes to be included when they are explicitly specified in the includedPropertyNames list.
            CreateChildPropertiesFiltered(container, property, description, field => excludedProperties.Contains(field));
        }
    }

    /// <summary>
    /// Creates a field for each visible attribute of the UXML serialized data.
    /// </summary>
    /// <param name="container">The parent element to add the fields to.</param>
    /// <param name="property">The serialized property that represents the UXML serialized data instance.</param>
    /// <remarks>
    /// The default implementation calls <see cref="CreateChildPropertyGUI"/> for each visible attribute,
    /// in declaration order. Override this method to show only specific attributes or to change their order.
    /// Call <see cref="CreateChildPropertiesExcluding"/> to exclude specific attributes by name whilst keeping the default order and property drawers for the rest.
    /// </remarks>
    /// <example>
    /// The following example shows only specific attributes by name.
    /// <code source="../../../../../Documentation/ManualDocs/com.unity.documentation-examples/UIToolkit/Scripts/UxmlSerializedDataPropertyDrawerExamples.cs" region="example2"/>
    /// </example>
    protected virtual void CreateChildPropertiesGUI(VisualElement container, SerializedProperty property)
    {
        CreateChildPropertiesFiltered(container, property, null, IsVisibleAttribute);
    }

    // Returns null when the property does not hold a registered UxmlSerializedData, which a custom
    // drawer can reach by applying itself to an unexpected type.
    static UxmlSerializedDataDescription GetDataDescription(SerializedProperty property)
    {
        var declaringType = (property?.managedReferenceValue as UxmlSerializedData)?.GetType().DeclaringType;
        return declaringType == null ? null : UxmlSerializedDataRegistry.GetDescription(declaringType.FullName);
    }

    void PopulatePropertyHashSet(HashSet<UxmlSerializedAttributeDescription> hashSet, UxmlSerializedDataDescription description, string[] propertyNames)
    {
        if (description == null || propertyNames == null)
            return;

        foreach (var property in propertyNames)
        {
            var attribute = description.FindAttributeWithUxmlName(property);
            if (attribute != null)
            {
                hashSet.Add(attribute);
                continue;
            }

            // An obsolete name can map to more than one attribute, so take them all.
            foreach (var obsoleteAttribute in description.FindAttributesWithObsoleteUxmlName(property))
            {
                hashSet.Add(obsoleteAttribute);
            }
        }
    }

    static bool IsVisibleAttribute(UxmlSerializedAttributeDescription attribute)
    {
        return attribute.serializedField.GetCustomAttribute<UnityEngine.HideInInspector>() == null;
    }

    void CreateChildPropertiesFiltered(VisualElement container, SerializedProperty property, UxmlSerializedDataDescription dataDescription = null, Func<UxmlSerializedAttributeDescription, bool> filter = null)
    {
        dataDescription ??= GetDataDescription(property);
        if (dataDescription == null)
            return;

        foreach (var attribute in dataDescription.serializedAttributes)
        {
            if (filter == null || filter(attribute))
            {
                var childProperty = property.FindPropertyRelative(attribute.serializedField.Name);
                if (childProperty != null)
                {
                    CreateChildPropertyGUI(container, property, childProperty);
                }
            }
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
