// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitAuthoringFramework not yet converted
using System;
using Unity.Properties;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using Unity.Scripting.LifecycleManagement;

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
                                                                          $" To make attributes readable and editable, update the control to use UxmlElement. <a href=\"{k_LinkToCustomControlMigrationDoc}\">Learn more</a>.", null);
    [NoAutoStaticsCleanup] // immutable binding id, safe to persist
    public static BindingId TargetProperty = nameof(Target);
    [NoAutoStaticsCleanup] // immutable binding id, safe to persist
    public static BindingId IsReadOnlyProperty = nameof(IsReadOnly);

    internal const string k_NoNameHelpBoxName = "no-name-help-box";
    static readonly string k_NoNameMessage = L10n.Tr("A name is required in order to override attributes.", null);

    internal const string k_NotEditableHelpBoxName = "not-editable-help-box";
    internal const string k_SelectAncestorLinkId = "select-ancestor";
    public const string LinkCursorUssClassName = "unity-attributes-inspector__link-cursor";
    static readonly string k_SelectAncestorMessageFormat = L10n.Tr("This element was created by a control, so its attributes cannot be set here. Select {0} to see the attributes that can be changed.", null);
    static readonly string k_TemplateInstanceMessageFormat = L10n.Tr("This element was created by a control, so its attributes cannot be set here. Select the {0} instance and open it in context to edit the control.", null);
    internal static readonly string k_CreatedByControlMessage = L10n.Tr("This element was created by a control, so its attributes cannot be set here.", null);
    internal static readonly string k_CreatedInScriptMessage = L10n.Tr("Attributes can only be edited for elements authored in UXML. This element was created in a C# script, so its properties must be set in code instead.", null);

    readonly UxmlAttributesView m_AttributesView;
    readonly HelpBox m_NoNameHelpBox;
    readonly HelpBox m_NotEditableHelpBox;
    readonly Label m_NotEditableLabel;
    Action m_SelectAncestorAction;
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

        m_NotEditableHelpBox = new HelpBox(k_CreatedInScriptMessage, HelpBoxMessageType.Info) { name = k_NotEditableHelpBoxName };
        m_NotEditableHelpBox.style.display = DisplayStyle.None;
        Add(m_NotEditableHelpBox);

        m_NotEditableLabel = m_NotEditableHelpBox.Q<Label>();

        m_AttributesView = new UxmlAttributesView();
        m_AttributesView.ContextChanged += OnContextChanged;
        Add(m_AttributesView);
    }

    [EventInterest(typeof(AttachToPanelEvent), typeof(DetachFromPanelEvent))]
    protected override void HandleEventBubbleUp(EventBase evt)
    {
        switch (evt)
        {
            case AttachToPanelEvent { destinationPanel: not null }:
                OnAttachToPanel();
                break;
            case DetachFromPanelEvent { originPanel: not null }:
                OnDetachFromPanel();
                break;
        }

        base.HandleEventBubbleUp(evt);
    }

    void OnAttachToPanel()
    {
        BindingsStyleHelpers.HandleRightClickMenu += HandleRightClickMenu;

        if (m_NotEditableLabel == null)
            return;

        m_NotEditableLabel.RegisterCallback<PointerUpLinkTagEvent>(OnNotEditableLinkClicked);
        m_NotEditableLabel.RegisterCallback<PointerOverLinkTagEvent>(OnNotEditableLinkOver);
        m_NotEditableLabel.RegisterCallback<PointerOutLinkTagEvent>(OnNotEditableLinkOut);
    }

    void OnDetachFromPanel()
    {
        BindingsStyleHelpers.HandleRightClickMenu -= HandleRightClickMenu;

        if (m_NotEditableLabel == null)
            return;

        m_NotEditableLabel.UnregisterCallback<PointerUpLinkTagEvent>(OnNotEditableLinkClicked);
        m_NotEditableLabel.UnregisterCallback<PointerOverLinkTagEvent>(OnNotEditableLinkOver);
        m_NotEditableLabel.UnregisterCallback<PointerOutLinkTagEvent>(OnNotEditableLinkOut);
        m_NotEditableLabel.RemoveFromClassList(LinkCursorUssClassName);
    }

    void OnNotEditableLinkOver(PointerOverLinkTagEvent evt) => m_NotEditableLabel.AddToClassList(LinkCursorUssClassName);

    void OnNotEditableLinkOut(PointerOutLinkTagEvent evt) => m_NotEditableLabel.RemoveFromClassList(LinkCursorUssClassName);

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

    internal enum NotEditableHint
    {
        CreatedInScript,      // no authored ancestor anywhere above
        CreatedByControl,     // an authored ancestor exists, but there is nothing useful to offer
        SelectAncestor,       // the ancestor takes attribute overrides, so it is worth selecting
        SelectTemplateInstance // the ancestor cannot take overrides; offer the instance holding it
    }

    internal void SetNotEditableHelpboxVisible(bool visible, NotEditableHint hint = NotEditableHint.CreatedInScript,
        string linkText = null, Action onLinkClicked = null)
    {
        m_SelectAncestorAction = visible ? onLinkClicked : null;

        if (visible)
        {
            var link = FormatAncestorLink(linkText, onLinkClicked != null);
            m_NotEditableHelpBox.text = hint switch
            {
                NotEditableHint.SelectAncestor => string.Format(k_SelectAncestorMessageFormat, link),
                NotEditableHint.SelectTemplateInstance => string.Format(k_TemplateInstanceMessageFormat, link),
                NotEditableHint.CreatedByControl => k_CreatedByControlMessage,
                _ => k_CreatedInScriptMessage
            };
        }
        else
        {
            m_NotEditableLabel?.RemoveFromClassList(LinkCursorUssClassName);
        }

        m_NotEditableHelpBox.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    static string FormatAncestorLink(string ancestorName, bool clickable)
    {
        if (!clickable)
            return $"<noparse>{ancestorName}</noparse>";

        return $"<link=\"{k_SelectAncestorLinkId}\"><color=#{GetLinkColorHex()}><u><noparse>{ancestorName}</noparse></u></color></link>";
    }

    static string GetLinkColorHex() => EditorGUIUtility.isProSkin ? "7BA6FF" : "2C5FD8";

    void OnNotEditableLinkClicked(PointerUpLinkTagEvent evt)
    {
        if (evt.linkID != k_SelectAncestorLinkId)
            return;

        evt.StopPropagation();
        m_SelectAncestorAction?.Invoke();
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
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
