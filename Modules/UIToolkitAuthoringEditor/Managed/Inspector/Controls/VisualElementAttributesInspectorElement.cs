// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Internal;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Represents an inspector element that displays and allows editing of the attributes of a selected VisualElement.
/// </summary>
[UxmlElement]
sealed class VisualElementAttributesInspectorElement : VisualElement
{
    static readonly string ussClassName = "unity-attributes-inspector";
    internal static readonly string rootPropertyFieldUssClassName = "unity-uxml-serialized-data-root-property-field";
    static readonly string linkToCustomControlMigrationDoc = "https://docs.unity3d.com/Manual/ui-systems/migrate-custom-control.html";
    internal static readonly string usingUxmlTraitsOrUxmlSerializedDataNotDefinedWarning = L10n.Tr("Attributes for this control failed to load because it uses UxmlTraits, a deprecated API; or did not define its UxmlSerializedData class." +
                                                                          $" To make attributes readable and editable, update the control to use UxmlElement. <a href=\"{linkToCustomControlMigrationDoc}\">Learn more</a>.");
    [Serializable]
    public new class UxmlSerializedData : VisualElement.UxmlSerializedData
    {
        /// <summary>
        /// This is used by the code generator when a custom control is using the <see cref="UxmlElementAttribute"/>. You should not need to call it.
        /// </summary>
        [Conditional("UNITY_EDITOR"), RegisterUxmlCache]
        public new static void Register()
        {
            UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[] { }, true);
        }

        [ExcludeFromDocs]
        public override object CreateInstance()
        {
            return new VisualElementAttributesInspectorElement();
        }
    }

    PropertyField m_RootPropertyField;
    HelpBox m_DeprecatedApiWarningBox;

    public UxmlAttributesEditingContext context { get; private set; }

    /// <summary>
    /// Constructor for the VisualElementAttributesInspectorElement.
    /// </summary>
    public VisualElementAttributesInspectorElement()
    {
        AddToClassList(ussClassName);
        AddToClassList(InspectorElement.ussClassName);
        AddToClassList(InspectorElement.uIEInspectorVariantUssClassName);
        AddToClassList(InspectorElement.uIECustomVariantUssClassName);
        AddToClassList(InspectorElement.customInspectorUssClassName);
        context = new UxmlAttributesEditingContext(new UxmlAttributesEditingController());
        context.contextChanged += OnContextChanged;
    }

    void OnContextChanged(object sender, UxmlAttributesEditingContext.ContextChangedEventArgs args)
    {
        ReleaseSelection(args.oldElement);
        if (args.newElement != null)
        {
            AcquireSelection(args.newElement);
        }
    }

    private void AcquireSelection(VisualElement element)
    {
        if (context.uxmlSerializedDataDescription == null)
        {
            ShowUxmlTraitsUsageWarningBox();
        }
        else
        {
            var serializedField = context.rootSerializedObject.FindProperty(context.serializedBasePath);

            m_RootPropertyField = new PropertyField(serializedField);
            m_RootPropertyField.AddToClassList(rootPropertyFieldUssClassName);
            m_RootPropertyField.reset += OnPropertyFieldReset;
            Add(m_RootPropertyField);
            m_RootPropertyField.Bind(context.rootSerializedObject);
            context.rootSerializedObject.ApplyModifiedProperties();
        }
    }

    private void ReleaseSelection(VisualElement element)
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
            propertyView.context = context;
        }
    }

    void ShowUxmlTraitsUsageWarningBox()
    {
        if (m_DeprecatedApiWarningBox == null)
        {
            m_DeprecatedApiWarningBox = new HelpBox(usingUxmlTraitsOrUxmlSerializedDataNotDefinedWarning, HelpBoxMessageType.Warning);
            Add(m_DeprecatedApiWarningBox);
        }
        m_DeprecatedApiWarningBox.style.display = DisplayStyle.Flex;
    }
}
