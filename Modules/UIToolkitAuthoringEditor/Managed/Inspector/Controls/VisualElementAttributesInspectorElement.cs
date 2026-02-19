// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Properties;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Internal;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Represents an inspector element that displays and allows editing of the attributes of a selected VisualElement.
/// </summary>
[UxmlElement]
sealed class VisualElementAttributesInspectorElement : UxmlAttributesView
{
    new const string UssClassName = "unity-attributes-inspector";
    internal const string k_RootPropertyFieldUssClassName = "unity-uxml-serialized-data-root-property-field";
    const string k_LinkToCustomControlMigrationDoc = "https://docs.unity3d.com/Manual/ui-systems/migrate-custom-control.html";
    internal static readonly string k_UsingUxmlTraitsOrUxmlSerializedDataNotDefinedWarning = L10n.Tr("Attributes for this control failed to load because it uses UxmlTraits, a deprecated API; or did not define its UxmlSerializedData class." +
                                                                          $" To make attributes readable and editable, update the control to use UxmlElement. <a href=\"{k_LinkToCustomControlMigrationDoc}\">Learn more</a>.");
    public static BindingId TargetProperty = nameof(Target);
    public static BindingId IsReadOnlyProperty = nameof(IsReadOnly);


    [Serializable]
    public new class UxmlSerializedData : UxmlAttributesView.UxmlSerializedData
    {
        /// <summary>
        /// This is used by the code generator when a custom control is using the <see cref="UxmlElementAttribute"/>. You should not need to call it.
        /// </summary>
        [Conditional("UNITY_EDITOR"), RegisterUxmlCache]
        public new static void Register()
        {
            UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), Array.Empty<UxmlAttributeNames>(), true);
        }

        [ExcludeFromDocs]
        public override object CreateInstance()
        {
            return new VisualElementAttributesInspectorElement();
        }
    }

    PropertyField m_RootPropertyField;
    HelpBox m_DeprecatedApiWarningBox;

    private bool m_IsReadOnly;

    [CreateProperty]
    public VisualElement Target
    {
        get => Context.element;
        set
        {
            if (Context.element == value)
                return;

            if (value == null)
                Context.Clear();
            else
                Context.Set(value, IsReadOnly);
            NotifyPropertyChanged(TargetProperty);
        }
    }

    [CreateProperty]
    public bool IsReadOnly
    {
        get => m_IsReadOnly;
        set
        {
            if (m_IsReadOnly == value)
                return;
            m_IsReadOnly = value;
            if (Target != null)
            {
                Context.Set(Target, m_IsReadOnly);
            }
            NotifyPropertyChanged(IsReadOnlyProperty);
        }
    }

    /// <summary>
    /// Constructor for the VisualElementAttributesInspectorElement.
    /// </summary>
    public VisualElementAttributesInspectorElement()
    {
        AddToClassList(UssClassName);
        AddToClassList(InspectorElement.ussClassName);
        AddToClassList(InspectorElement.uIEInspectorVariantUssClassName);
        AddToClassList(InspectorElement.uIECustomVariantUssClassName);
        AddToClassList(InspectorElement.customInspectorUssClassName);

        Context = new UxmlAttributesEditingContext(new UxmlAttributesEditingController());
    }

    void UpdateContext()
    {
        if (Target != null)
        {
            Context.Set(Target, IsReadOnly);
        }
        else
        {
            Context.Clear();
        }
    }

    protected override void CreateViewContent(UxmlAttributesEditingContext context)
    {
        if (context.uxmlSerializedDataDescription == null)
        {
            ShowUxmlTraitsUsageWarningBox();
        }
        else
        {
            var serializedField = context.rootSerializedObject.FindProperty(context.serializedBasePath);

            m_RootPropertyField = new PropertyField(serializedField);
            m_RootPropertyField.AddToClassList(k_RootPropertyFieldUssClassName);
            m_RootPropertyField.reset += OnPropertyFieldReset;
            Add(m_RootPropertyField);
            m_RootPropertyField.Bind(context.rootSerializedObject);
            context.rootSerializedObject.ApplyModifiedProperties();
        }
    }

    protected override void ReleaseViewContent(UxmlAttributesEditingContext context)
    {
        if (m_RootPropertyField != null)
            m_RootPropertyField.reset -= OnPropertyFieldReset;
        m_RootPropertyField?.RemoveFromHierarchy();
        m_RootPropertyField = null;
        if (m_DeprecatedApiWarningBox != null)
            m_DeprecatedApiWarningBox.style.display = DisplayStyle.None;
        dataSource = null;
    }

    void OnPropertyFieldReset()
    {
        var propertyView = m_RootPropertyField.Q<UxmlSerializedDataPropertyView>();

        // Set the context of the root property view.
        if (propertyView != null)
        {
            propertyView.context = Context;
        }
    }

    void ShowUxmlTraitsUsageWarningBox()
    {
        if (m_DeprecatedApiWarningBox == null)
        {
            m_DeprecatedApiWarningBox = new HelpBox(k_UsingUxmlTraitsOrUxmlSerializedDataNotDefinedWarning, HelpBoxMessageType.Warning);
            Add(m_DeprecatedApiWarningBox);
        }
        m_DeprecatedApiWarningBox.style.display = DisplayStyle.Flex;
    }
}
