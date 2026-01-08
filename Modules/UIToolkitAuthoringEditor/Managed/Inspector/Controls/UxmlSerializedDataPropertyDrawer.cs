// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
class UxmlSerializedDataPropertyView : VisualElement
{
    public const string ussClassName = "unity-uxml-serialized-data-property-view";

    UxmlAttributeFieldDecorator m_FieldDecoratorForListItem;
    UxmlAttributesEditingContext m_Context;
    List<UxmlAttributeFieldDecorator> m_RegisteredDecorators = new();

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
        m_Context = context;
        // Propagate the context to any registered UxmlAttributeFieldDecorator
        foreach (var decorator in m_RegisteredDecorators)
        {
            decorator.context = context;
        }
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

        // Find the parent serialized property and check if it is a list or array.
        // If so then this UxmlSerializedDataPropertyView represents a list item. Therefore, we add a field decorator
        // for the list item itself because list items are created by ListViewSerializedObjectBinding.MakeItem,
        // which only creates instances of PropertyField.
        var parentProperty = property.GetParentProperty();

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
}

/// <summary>
/// Custom property drawer for UxmlSerializedData.
/// </summary>
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
[CustomPropertyDrawer(typeof(UxmlSerializedData), true)]
internal class UxmlSerializedDataPropertyDrawer : PropertyDrawer
{
    public sealed override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var container = new UxmlSerializedDataPropertyView(property);

        CreatePropertyGUI(container, property);
        return container;
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

        CreatePropertyFields(foldout, property);
    }

    /// <summary>
    /// Creates the GUI for a property of the UxmlSerializedData.
    /// </summary>
    /// <param name="container">The VisualElement parent of the content to create</param>
    /// <param name="property">The serialized property on the UxmlSerializedData to display</param>
    /// <param name="childProperty">The serialized property on one of the properties of the UxmlSerializedData to display</param>
    protected virtual void CreateChildPropertyGUI(VisualElement container, SerializedProperty property, SerializedProperty childProperty)
    {
        container.Add(new UxmlAttributeField(childProperty));
    }

    /// <summary>
    /// Creates property fields for each serialized property in the UxmlSerializedData.
    /// </summary>
    /// <param name="container">The VisualElement parent where to add the created fields</param>
    /// <param name="property">A property of the UxmlSerializedData to view</param>
    void CreatePropertyFields(VisualElement container, SerializedProperty property)
    {
        var uxmlSerializedData = property.managedReferenceValue as UxmlSerializedData;

        if (uxmlSerializedData == null)
            return;

        // Use the UxmlSerializedDataDescription to determine which properties to show and in which order.
        var dataAttribute = UxmlSerializedDataRegistry.GetDescription(uxmlSerializedData.GetType().DeclaringType.FullName);

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
