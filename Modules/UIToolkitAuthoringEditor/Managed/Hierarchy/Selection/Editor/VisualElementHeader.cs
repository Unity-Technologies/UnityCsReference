// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitAuthoringFramework not yet converted
using System;
using System.IO;
using Unity.Properties;
using Unity.UIToolkit.Editor.Utilities;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using Unity.Scripting.LifecycleManagement;

namespace Unity.UIToolkit.Editor;

[UxmlElement]
partial class VisualElementHeader : UISelectionObjectHeader
{
    public static readonly BindingId ElementProperty = nameof(Element);

    public new const string UssClass = "unity-visual-element-header";
    public const string AssetPathContainerUssClass = "unity-visual-element-inspector__asset-path-container";
    public const string AssetPathTypeIconUssClass = "unity-visual-element-inspector__asset-path-type-icon";
    public const string AssetPathFieldUssClass = "unity-visual-element-inspector__asset-path-field";
    public const string AssetActionsViewUssClass = "unity-visual-element-inspector__asset-actions-view";
    public const string StackingIndicatorUssClass = "unity-visual-element-header__stacking-context-indicator";

    private const string k_VisualTreeAsset = "UIToolkitAuthoring/Inspector/VisualElementHeader.uxml";
    private const string k_StyleSheet = "UIToolkitAuthoring/Inspector/UIToolkitAuthoringInspector.uss";
    private const string k_StyleSheetDark = "UIToolkitAuthoring/Inspector/UIToolkitAuthoringInspectorDark.uss";
    private const string k_StyleSheetLight = "UIToolkitAuthoring/Inspector/UIToolkitAuthoringInspectorLight.uss";
    private const string k_NoAssetPath = "<none>.uxml";

    [AutoStaticsCleanupOnCodeReload]
    static StyleSheet s_StyleSheet;
    [AutoStaticsCleanupOnCodeReload]
    static StyleSheet s_ThemedStyleSheet;
    [AutoStaticsCleanupOnCodeReload]
    static bool s_ThemedStyleSheetIsProSkin;

    private VisualElement m_Element;
    readonly UxmlAttributesView m_AttributesView;

    readonly VisualElement m_AssetPathContainer;
    readonly Image m_AssetPathTypeIcon;
    readonly TextField m_AssetPathField;
    readonly VisualTreeAssetInspectorActionsView m_AssetActionsView;

    readonly Button m_StackingIndicator;
    VisualElement m_StackingContextRoot;

    public UxmlAttributesView AttributesView => m_AttributesView;
    public InspectorSearchField SearchField { get; }

    protected override VisualTreeAsset IdentifierDetails => EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;

    [CreateProperty]
    public VisualElement Element
    {
        get => m_Element;
        set
        {
            if (m_Element == value)
                return;
            m_Element = value;
            if (m_Element == null)
            {
                TypeIcon = UIResources.GetIconForType(typeof(VisualElement), UIResources.RequestSize.Px32);
                TypeName = nameof(VisualElement);
                m_AssetPathField.value = k_NoAssetPath;
                m_AssetPathField.tooltip = string.Empty;
                m_AssetActionsView.VisualTreeAsset = null;
                m_AssetActionsView.PanelSettings = null;
                m_AssetActionsView.SubDocumentPath = null;
            }
            else
            {
                TypeIcon = UIResources.GetIconForElement(m_Element, UIResources.RequestSize.Px32);
                TypeName = TypeUtility.GetTypeDisplayName(m_Element.GetType());

                VisualTreeAsset visualTreeAsset;
                if (m_Element is TemplateContainer templateContainer)
                    visualTreeAsset = templateContainer.templateSource;
                else
                {
                    visualTreeAsset = m_Element.visualTreeAssetSource
                        ? m_Element.visualTreeAssetSource
                        : m_Element.GetFirstAncestorWhere(ve => ve.visualTreeAssetSource)?.visualTreeAssetSource;
                }

                var assetPath = k_NoAssetPath;
                var assetPathToolTip = string.Empty;

                if (visualTreeAsset)
                {
                    var fullPath = AssetDatabase.GetAssetPath(visualTreeAsset.GetEntityId());
                    assetPath = Path.GetFileName(fullPath);
                    assetPathToolTip = fullPath;
                }

                m_AssetPathField.value = assetPath;
                m_AssetPathField.tooltip = assetPathToolTip;
                m_AssetActionsView.VisualTreeAsset = visualTreeAsset;
                using var _ = ListPool<TemplateAsset>.Get(out var templateAssetPath);
                m_Element.GenerateSubDocumentPath(templateAssetPath);
                m_AssetActionsView.SubDocumentPath = templateAssetPath.Count > 0
                    ? templateAssetPath.ToArray()
                    : Array.Empty<TemplateAsset>();
                m_AssetActionsView.PanelSettings = m_Element.GetPanelSettings();
            }
            NotifyPropertyChanged(ElementProperty);
            RefreshStackingIndicator();
        }
    }

    public void SetEditState(VisualElementEditFlags editFlags)
    {
        var enabled = editFlags == VisualElementEditFlags.FullyEditable;
        m_AttributesView?.SetEnabled(enabled);
        m_AssetPathContainer?.SetEnabled(enabled);
    }

    /// <summary>
    /// Updates the visibility of the asset path field and asset actions buttons (asset views), and the enabled state of the
    /// "Open In Context" button, based on the current editing context.
    /// </summary>
    public void UpdateAssetVisibility(VisualElementEditFlags editFlags, bool isRecording = false, bool inStagingMode = false)
    {
        if (m_Element == null)
            return;

        bool showTemplateOptions = false;
        bool canOpenInContext = true;

        if (inStagingMode)
        {
            StageContextMenuUtility.GetOpenOptions(m_Element, out showTemplateOptions, out canOpenInContext);
        }

        // Outside the editing stage (Main Stage included) the document link and its actions are
        // always available; inside the stage they only appear for elements of template instances.
        bool showAssetViews = !inStagingMode || showTemplateOptions;
        m_AssetActionsView.style.display = showAssetViews ? DisplayStyle.Flex : DisplayStyle.None;
        m_AssetPathContainer.style.display = showAssetViews ? DisplayStyle.Flex : DisplayStyle.None;
        if (showAssetViews)
        {
            var openInContextButton = m_AssetActionsView.OpenInContextButton;

            openInContextButton.SetEnabled(canOpenInContext);
            openInContextButton.tooltip = canOpenInContext
                ? string.Empty
                : L10n.Tr("Not available: this element is the currently edited template container.", null);
        }
    }

    public VisualElementHeader()
    {
        AddToClassList(UssClass);
        if (s_StyleSheet == null)
            s_StyleSheet = EditorGUIUtility.Load(k_StyleSheet) as StyleSheet;
        if (s_ThemedStyleSheet == null || s_ThemedStyleSheetIsProSkin != EditorGUIUtility.isProSkin)
        {
            s_ThemedStyleSheet = EditorGUIUtility.Load(EditorGUIUtility.isProSkin ? k_StyleSheetDark : k_StyleSheetLight) as StyleSheet;
            s_ThemedStyleSheetIsProSkin = EditorGUIUtility.isProSkin;
        }
        styleSheets.Add(s_StyleSheet);
        styleSheets.Add(s_ThemedStyleSheet);

        TypeIcon = UIResources.GetIconForType(typeof(VisualElement), UIResources.RequestSize.Px32);
        TypeName = nameof(VisualElement);

        m_AttributesView = this.Q<UxmlAttributesView>();

        m_AssetPathContainer = new VisualElement();
        m_AssetPathContainer.AddToClassList(AssetPathContainerUssClass);

        var assetPathLabel = new Label("UXML");
        assetPathLabel.AddToClassList("unity-visual-element-inspector__asset-path-label");
        m_AssetPathContainer.Add(assetPathLabel);

        m_AssetPathTypeIcon = new Image();
        m_AssetPathTypeIcon.AddToClassList(AssetPathTypeIconUssClass);
        m_AssetPathTypeIcon.image = EditorGUIUtility.Load("VisualTreeAsset Icon") as Texture2D;
        m_AssetPathContainer.Add(m_AssetPathTypeIcon);

        m_AssetPathField = new TextField { value = k_NoAssetPath };
        m_AssetPathField.isReadOnly = true;
        m_AssetPathField.AddToClassList(AssetPathFieldUssClass);
        m_AssetPathContainer.Add(m_AssetPathField);

        Add(m_AssetPathContainer);

        m_AssetActionsView = new VisualTreeAssetInspectorActionsView();
        m_AssetActionsView.AddToClassList(AssetActionsViewUssClass);
        Add(m_AssetActionsView);

        SearchField = new InspectorSearchField();
        Add(SearchField);

        m_StackingIndicator = new Button(OnStackingIndicatorClicked);
        m_StackingIndicator.AddToClassList(StackingIndicatorUssClass);
        m_StackingIndicator.style.backgroundImage = EditorGUIUtility.IconContent("UnityEditor.SceneHierarchyWindow").image as Texture2D;
        this.Q(className: UISelectionObjectHeader.ObjectIdentifierRowUssClass)?.Add(m_StackingIndicator);

        RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
    }

    void OnAttachToPanel(AttachToPanelEvent evt)
    {
        UIToolkitProjectSettings.onEnableZIndexChanged += RefreshStackingIndicator;
        UICommandQueue.RegisterHandlerForCategory(CommandCategory.Styling, OnStylingChange);
    }

    void OnDetachFromPanel(DetachFromPanelEvent evt)
    {
        UIToolkitProjectSettings.onEnableZIndexChanged -= RefreshStackingIndicator;
        UICommandQueue.UnregisterHandlerForCategory(CommandCategory.Styling, OnStylingChange);
    }

    void OnStylingChange(in CommandContext context) => RefreshStackingIndicator();

    void RefreshStackingIndicator()
    {
        m_StackingContextRoot = null;

        if (!UIToolkitProjectSettings.enableZIndex || m_Element == null)
        {
            m_StackingIndicator.style.display = DisplayStyle.None;
            return;
        }

        var boundary = m_Element.GetFirstAncestorOfType<IPanelComponentRootElement>() as VisualElement
            ?? m_Element.panel?.visualTree;
        var root = VisualElement.FindStackingContextRootElement(m_Element, boundary);

        if (root == null || root.GetSelectionObject<VisualElementSelection>()?.GetEntityId() == null)
        {
            m_StackingIndicator.style.display = DisplayStyle.None;
            return;
        }

        m_StackingContextRoot = root;
        var rootName = !string.IsNullOrEmpty(root.name) ? root.name : TypeUtility.GetTypeDisplayName(root.GetType());
        m_StackingIndicator.tooltip = string.Format(L10n.Tr("Inside a stacking context established by {0}. Click to select it.", null), rootName);
        m_StackingIndicator.style.display = DisplayStyle.Flex;
    }

    void OnStackingIndicatorClicked()
    {
        var entityId = m_StackingContextRoot?.GetSelectionObject<VisualElementSelection>()?.GetEntityId();
        if (entityId.HasValue)
            Selection.activeEntityId = entityId.Value;
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
