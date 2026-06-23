// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Represents an inspector element that displays and allows editing of the attributes of a selected VisualElement.
/// </summary>
[UxmlElement]
sealed partial class VisualElementAttributesInspectorElement : VisualElement
{
    const string UssClassName = "unity-attributes-inspector";
    internal const string k_RootPropertyFieldUssClassName = "unity-uxml-serialized-data-root-property-field";
    const string k_LinkToCustomControlMigrationDoc = "https://docs.unity3d.com/Manual/ui-systems/migrate-custom-control.html";
    internal static readonly string k_UsingUxmlTraitsOrUxmlSerializedDataNotDefinedWarning = L10n.Tr("Attributes for this control failed to load because it uses UxmlTraits, a deprecated API; or did not define its UxmlSerializedData class." +
                                                                          $" To make attributes readable and editable, update the control to use UxmlElement. <a href=\"{k_LinkToCustomControlMigrationDoc}\">Learn more</a>.");
    public static BindingId TargetProperty = nameof(Target);
    public static BindingId IsReadOnlyProperty = nameof(IsReadOnly);

    internal const string k_NoNameHelpBoxName = "no-name-help-box";
    static readonly string k_NoNameMessage = L10n.Tr("A name is required in order to override attributes.");

    readonly UxmlAttributesView m_AttributesView;
    readonly HelpBox m_NoNameHelpBox;
    PropertyField m_RootPropertyField;

    private bool m_IsReadOnly;

    public UxmlAttributesView AttributesView => m_AttributesView;

    [CreateProperty]
    public VisualElement Target
    {
        get => m_AttributesView.Context.element;
        set
        {
            if (m_AttributesView.Context.element == value)
                return;

            if (value == null)
                m_AttributesView.Context.Clear();
            else
                m_AttributesView.Context.Set(value, IsReadOnly);
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
                m_AttributesView.Context.Set(Target, m_IsReadOnly);
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

        m_NoNameHelpBox = new HelpBox(k_NoNameMessage, HelpBoxMessageType.Info) { name = k_NoNameHelpBoxName };
        m_NoNameHelpBox.style.display = DisplayStyle.None;
        Add(m_NoNameHelpBox);

        m_AttributesView = new UxmlAttributesView();
        m_AttributesView.ContextChanged += OnContextChanged;
        Add(m_AttributesView);

        RegisterCallback<AttachToPanelEvent>(_=> BindingsStyleHelpers.HandleRightClickMenu += HandleRightClickMenu);
        RegisterCallback<DetachFromPanelEvent>(_=> BindingsStyleHelpers.HandleRightClickMenu -= HandleRightClickMenu);
    }

    static void HandleRightClickMenu(VisualElement ve, ref bool handled)
    {
        while (ve != null)
        {
            if (ve is UxmlAttributeFieldDecorator)
            {
                handled =  true;
                return;
            }
            ve = ve.parent;
        }
    }

    internal void SetAttributeOverrideHelpboxVisible(bool visible)
    {
        m_NoNameHelpBox.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    void OnContextChanged(object sender, UxmlAttributesEditingContext.ContextChangedEventArgs args)
    {
        var view = sender as UxmlAttributesView;

        if (view?.Context == null)
        {
            return;
        }

        if (view.Context.uxmlSerializedDataDescription == null)
        {
            m_RootPropertyField?.RemoveFromHierarchy();
            m_RootPropertyField = null;
            return;
        }

        var bindingPath = view.Context.serializedBasePath;

        if (m_RootPropertyField == null)
        {
            m_RootPropertyField = new PropertyField();
            m_RootPropertyField.AddToClassList(k_RootPropertyFieldUssClassName);
            m_AttributesView.Add(m_RootPropertyField);
        }

        m_RootPropertyField.bindingPath = bindingPath;
    }
}
