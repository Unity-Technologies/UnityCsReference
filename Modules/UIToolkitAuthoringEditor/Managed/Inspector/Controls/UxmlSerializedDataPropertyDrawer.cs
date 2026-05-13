// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Bindings;
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
            m_FieldDecoratorForListItem = new UxmlAttributeFieldDecorator();
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
/// Custom property drawer for UxmlSerializedData.
/// </summary>
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
[CustomPropertyDrawer(typeof(UxmlSerializedData), true)]
internal class UxmlSerializedDataPropertyDrawer : PropertyDrawer
{
    public static readonly string k_CreateOrRemoveDataButtonUssName = UxmlSerializedDataPropertyView.ussClassName + "__create-or-remove-data-button";
    public static readonly string k_CreateDataButtonUssName = k_CreateOrRemoveDataButtonUssName + "--add";
    public static readonly string k_CreateDataButtonWithMenuUssName = k_CreateOrRemoveDataButtonUssName + "--add-with-menu";
    public static readonly string k_RemoveDataButtonUssName = k_CreateOrRemoveDataButtonUssName + "--remove";

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

    void UpdateAddOrRemoveButtonState(SerializedProperty property, Button addOrRemoveButton)
    {
        var flagsPath = property.propertyPath + "_UxmlAttributeFlags";
        var flagProperty = property.serializedObject.FindProperty(flagsPath);
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
    /// Creates the property GUI for a UxmlSerializedData instance.
    /// </summary>
    /// <param name="container">The VisualElement parent of the content to create</param>
    /// <param name="property">The serialized property on the UxmlSerializedData to display</param>
    protected virtual void CreatePropertyGUI(VisualElement container, SerializedProperty property)
    {
        var foldout = new Foldout() { text = property.displayName, value = true };

        container.Add(foldout);

        if (property.managedReferenceValue == null)
            return;

        CreateChildPropertiesGUI(foldout, property);
    }

    /// <summary>
    /// Creates the GUI for a property of the UxmlSerializedData.
    /// </summary>
    /// <param name="container">The VisualElement parent of the content to create</param>
    /// <param name="property">The serialized property on the UxmlSerializedData to display</param>
    /// <param name="childProperty">The serialized property on one of the properties of the UxmlSerializedData to display</param>
    protected virtual void CreateChildPropertyGUI(VisualElement container, SerializedProperty property,
        SerializedProperty childProperty)
    {
        container.Add(new UxmlAttributeField(childProperty));
    }

    /// <summary>
    /// Creates property fields for each visible serialized property in the specified UxmlSerializedData.
    /// </summary>
    /// <param name="container">The VisualElement parent where to add the created fields</param>
    /// <param name="property">A property of the UxmlSerializedData to view</param>
    protected virtual void CreateChildPropertiesGUI(VisualElement container, SerializedProperty property)
    {
        var uxmlSerializedData = property.managedReferenceValue as UxmlSerializedData;

        // Use the UxmlSerializedDataDescription to determine which properties to show and in which order.
        var dataAttribute =
            UxmlSerializedDataRegistry.GetDescription(uxmlSerializedData.GetType().DeclaringType.FullName);

        foreach (var attribute in dataAttribute.serializedAttributes)
        {
            if (attribute.serializedField.GetCustomAttribute<UnityEngine.HideInInspector>() == null)
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
