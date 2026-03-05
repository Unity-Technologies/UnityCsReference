// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Represents a field for editing UXML attributes in the UXML Serialized Data Property View.
/// </summary>
internal class UxmlAttributeField : VisualElement
{
    public const string ussClassName = "unity-uxml-attribute-field";

    [UnityEngine.Internal.ExcludeFromDocs, Serializable]
    public new class UxmlSerializedData : VisualElement.UxmlSerializedData
    {
        [RegisterUxmlCache]
        [Conditional("UNITY_EDITOR")]
        public new static void Register()
        {
            UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
            {
                new (nameof(label), "label"),
                new (nameof(bindingPath), "binding-path"),
            }, true);
        }

#pragma warning disable 649
        [SerializeField] string label;
        [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags label_UxmlAttributeFlags;
        [SerializeField] string bindingPath;
        [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags bindingPath_UxmlAttributeFlags;
#pragma warning restore 649

        public override object CreateInstance() => new UxmlAttributeField();

        public override void Deserialize(object obj)
        {
            base.Deserialize(obj);

            var e = (UxmlAttributeField)obj;

            if (ShouldWriteAttributeValue(label_UxmlAttributeFlags))
                e.label = label;

            if (ShouldWriteAttributeValue(bindingPath_UxmlAttributeFlags))
                e.bindingPath = bindingPath;
        }
    }

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
    public string label
    {
        get => m_PropertyField.label;
        set => m_PropertyField.label = value;
    }

    /// <summary>
    /// The binding path of the UXML attribute field.
    /// </summary>
    public string bindingPath
    {
        get => m_PropertyField.bindingPath;
        set => m_PropertyField.bindingPath = value;
    }

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
internal class UxmlAttributeFieldDecorator : VisualElement, ITrackablePropertyProvider
{
    public const string ussClassName = "unity-uxml-attribute-field-decorator";
    public const string contentContainerUssClassName = ussClassName + "__content-container";
    public const string affordanceElementName = "affordance-element";
    public const string affordanceElementUssClassName = ussClassName + "__affordance-element";
    public static readonly string s_InlineFieldUssClassName = "property-field__inline-value";
    public static readonly string s_BoundFieldUssClassName = "property-field__bound";

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
            }
        }

        internal override void OnChildRemoved(VisualElement child)
        {
            if (m_Bindable == child)
            {
                child.UnregisterCallback<SerializedPropertyBindEvent>(OnSerializedPropertyBindEvent);
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
        }
    }

    [UnityEngine.Internal.ExcludeFromDocs, Serializable]
    public new class UxmlSerializedData : VisualElement.UxmlSerializedData
    {
        [RegisterUxmlCache]
        [Conditional("UNITY_EDITOR")]
        public new static void Register()
        {
            UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), Array.Empty<UxmlAttributeNames>(), true);
        }
        public override object CreateInstance() => new UxmlAttributeFieldDecorator();
    }

    FieldAffordanceElement m_AffordanceElement;
    ContentContainer m_ContentContainer;
    VisualElement m_BoundField;
    UxmlSerializedDataPropertyView m_PropertyView;
    SerializedProperty m_BoundProperty;
    UxmlSerializedAttributeDescription m_BoundAttributeDescription;
    UxmlAttributesEditingContext m_Context;

    event Action<ITrackablePropertyProvider, string, TrackedPropertyType> OnTrackedPropertyChanged;
    event Action<ITrackablePropertyProvider, string, TrackedPropertyType> ITrackablePropertyProvider.OnTrackedPropertyChanged
    {
        add => OnTrackedPropertyChanged += value;
        remove => OnTrackedPropertyChanged -= value;
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
            m_BoundProperty = value;
            UpdateBoundAttribute();
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
            m_BoundField?.UnregisterCallback<DetachFromPanelEvent>(OnFieldDetachedFromPanel);

            m_BoundField = value;

            if (m_BoundField == null)
            {
                return;
            }

            SendTrackPropertyEvent(this, m_BoundField, GetBindingPath(), PropertyTrackingType.Register);
            Refresh();

            m_BoundField.RegisterCallback<DetachFromPanelEvent>(OnFieldDetachedFromPanel);

            m_BoundField.TrackPropertyValue(boundProperty, OnPropertyChanged);
            var attributeFlagsProperty = boundProperty.serializedObject.FindProperty(boundProperty.propertyPath + UxmlSerializedData.AttributeFlagSuffix);
            if (attributeFlagsProperty != null)
            {
                m_BoundField.TrackPropertyValue(attributeFlagsProperty, OnPropertyChanged);
            }
        }
    }

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

        var overrideRow = new OverrideRow() { style = { flexGrow = 1 } };
        m_AffordanceElement = new FieldAffordanceElement() { name = affordanceElementName };
        m_AffordanceElement.AddToClassList(affordanceElementUssClassName);
        overrideRow.Add(m_AffordanceElement);
        hierarchy.Add(overrideRow);

        m_ContentContainer = new ContentContainer(this);
        overrideRow.Add(m_ContentContainer);
        RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
        RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);

        boundProperty = property;
    }

    void UpdateBoundAttribute()
    {
        UxmlSerializedAttributeDescription desc = null;

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

    private void OnPropertyChanged(SerializedProperty property)
    {
        Refresh();
    }

    /// <summary>
    /// Custom update delegate for field affordance data.
    /// </summary>
    public delegate void CustomFieldAffordanceDataUpdate(UxmlAttributeFieldDecorator decorator, in FieldAffordanceData data,  VisualElement element,  Binding binding,  bool isInline);

    /// <summary>
    /// Allows custom update logic for the field affordance data.
    /// </summary>
    public CustomFieldAffordanceDataUpdate customFieldAffordanceDataUpdate;

    internal void Refresh()
    {
        if (boundProperty == null)
            return;

        var isInline = false;

        var flagsPath = boundProperty.propertyPath + "_UxmlAttributeFlags";
        var flagProperty = boundProperty.serializedObject.FindProperty(flagsPath);
        if (flagProperty != null)
        {
            var uxmlFlagsValue = (UnityEngine.UIElements.UxmlSerializedData.UxmlAttributeFlags)flagProperty.enumValueIndex;
            isInline = UnityEngine.UIElements.UxmlSerializedData.ShouldWriteAttributeValue(uxmlFlagsValue);
        }

        Binding binding = null;
        if (context is { element: not null })
        {
            binding = context.element.GetBinding(GetBindingPath());
            FieldAffordanceController.UpdateFieldAffordanceData(m_AffordanceElement.fieldAffordanceData, context.element, binding, isInline);
            customFieldAffordanceDataUpdate?.Invoke(this, m_AffordanceElement.fieldAffordanceData, context.element, binding, isInline);
        }

        var isOverridden = isInline || binding != null;
        OnTrackedPropertyChanged?.Invoke(this, GetBindingPath(),
            isOverridden ? TrackedPropertyType.MarkOverride : TrackedPropertyType.ClearOverride);

        EnableInClassList(s_InlineFieldUssClassName, isInline);
        EnableInClassList(s_BoundFieldUssClassName, binding != null);
    }

    public BindingId GetBindingPath()
    {
        if (boundProperty == null)
            return string.Empty;
        return boundProperty.GetBindingPath();
    }

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
        if (boundProperty.isValid)
            OnTrackedPropertyChanged?.Invoke(this, GetBindingPath(), TrackedPropertyType.StopTracking);
    }
}
