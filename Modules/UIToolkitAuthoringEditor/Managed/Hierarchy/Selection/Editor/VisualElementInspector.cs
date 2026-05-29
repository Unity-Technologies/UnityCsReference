// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using Unity.Properties;
using Unity.UIToolkit.Editor.Utilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[UxmlElement]
internal sealed partial class VisualElementInspector : UIInspector
{
    public static readonly BindingId ElementProperty = nameof(Element);
    public static readonly BindingId EditFlagsProperty = nameof(EditFlags);

    public const string UssClass = "unity-visual-element-inspector";
    public const string HeaderUssClass = UssClass + "__header";
    public const string AssetNotEditableHelpBoxUssClass = UssClass + "__not-editable-help-box";
    public const string AssetPathContainerUssClass = UssClass + "__asset-path-container";
    public const string AssetPathTypeIconUssClass = UssClass + "__asset-path-type-icon";
    public const string AssetPathFieldUssClass = UssClass + "__asset-path-field";
    public const string AssetActionsViewUssClass = UssClass + "__asset-actions-view";
    public const string BindingsSectionViewClass = UssClass + "__bindings-section";
    public const string ClassListClass = UssClass + "__class-list";
    public const string MatchingSelectorsClass = UssClass + "__matching-selectors";
    public const string StyleInspectorClass = UssClass + "__style-inspector";
    public const string AttributesInspectorClass = UssClass + "__attributes-inspector";
    public const string ReadonlyAttributesInspectorClass = AttributesInspectorClass + "--readonly";

    private const string k_VisualTreeAsset = "UIToolkitAuthoring/Inspector/VisualElementInspector.uxml";
    private const string k_StyleSheetDark = "UIToolkitAuthoring/Inspector/UIToolkitAuthoringInspectorDark.uss";
    private const string k_StyleSheetLight = "UIToolkitAuthoring/Inspector/UIToolkitAuthoringInspectorLight.uss";

    private const string k_NoAssetPath = "<none>.uxml";

    private VisualElement m_Element;
    private VisualElementEditFlags m_EditFlags;

    private readonly VisualElementHeader m_Header;
    readonly VisualElement m_AssetPathContainer;
    private readonly Image m_AssetPathTypeIcon;
    private readonly TextField m_AssetPathField;

    private readonly VisualTreeAssetInspectorActionsView m_AssetActionsView;
    private readonly VisualElement m_AssetNotEditableHelpBox;
    private readonly VisualElementBindingsInspectorElement m_BindingsInspector;
    private readonly VisualElementAttributesInspectorElement m_AttributesInspector;
    private readonly ClassListElement m_ClassListElement;
    private readonly MatchingSelectorsElement m_MatchingSelectorsElement;
    private readonly StyleInspectorElement m_StyleInspector;
    private StyleInspectorDefaultContent m_StyleInspectorDefaultContent;
    private VariablesInspector m_VariablesSection;
    private HelpBox m_RecordingBanner;

    bool m_IsRecording;

    bool IsRecording
    {
        get => m_IsRecording;
        set
        {
            if (m_IsRecording == value)
                return;
            m_IsRecording = value;
            UpdateControlsState();
        }
    }

    [CreateProperty]
    public VisualElement Element
    {
        get => m_Element;
        set
        {
            if (m_Element == value)
                return;
            m_Element = value;

            m_Header.Element = m_Element;

            if (m_Element == null)
            {
                m_AssetPathField.value = k_NoAssetPath;
                m_AssetPathField.tooltip = String.Empty;
                m_AssetActionsView.VisualTreeAsset = null;
                m_AssetActionsView.PanelSettings = null;
                m_AssetActionsView.SubDocumentPath = null;
                m_ClassListElement.Target = null;
                m_MatchingSelectorsElement.Target = null;
                m_StyleInspector.Target = new StyleInspectorTarget(null);
                m_AttributesInspector.Target = null;
                m_VariablesSection?.Refresh(null);
            }
            else
            {
                var visualTreeAsset = m_Element.visualTreeAssetSource
                    ? m_Element.visualTreeAssetSource
                    : m_Element.GetFirstAncestorWhere(ve => ve.visualTreeAssetSource)?.visualTreeAssetSource;

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

                m_AssetActionsView.SubDocumentPath = templateAssetPath.ToArray();
                m_AssetActionsView.PanelSettings = m_Element.GetPanelSettings();

                m_ClassListElement.Target = m_Element;
                m_MatchingSelectorsElement.Target = m_Element;
                m_StyleInspector.Target = new StyleInspectorTarget(m_Element);
                m_AttributesInspector.Target = m_Element;
                m_VariablesSection?.Refresh(GetInlineStyleRule());
            }
            MarkSearchDirty();
            NotifyPropertyChanged(ElementProperty);
        }
    }

    [CreateProperty]
    public VisualElementEditFlags EditFlags
    {
        get => m_EditFlags;
        set
        {
            if (m_EditFlags == value)
                return;
            SetEditFlags(value);
            NotifyPropertyChanged(EditFlagsProperty);
        }
    }

    public VisualElementInspector()
    {
        AddToClassList(UssClass);

        var vta = EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;
        vta.CloneTree(this);

        var styleSheetPath = EditorGUIUtility.isProSkin ? k_StyleSheetDark : k_StyleSheetLight;
        var styleSheet = EditorGUIUtility.Load(styleSheetPath) as StyleSheet;
        styleSheets.Add(styleSheet);

        m_AssetNotEditableHelpBox = this.Q(className:AssetNotEditableHelpBoxUssClass);

        m_Header = this.Q<VisualElementHeader>(className:HeaderUssClass);
        m_AssetPathContainer = this.Q(className:AssetPathContainerUssClass);
        m_AssetPathTypeIcon = this.Q<Image>(className:AssetPathTypeIconUssClass);
        m_AssetPathTypeIcon.image = EditorGUIUtility.Load("VisualTreeAsset Icon") as Texture2D;
        m_AssetPathField = this.Q<TextField>(className:AssetPathFieldUssClass);
        m_AssetActionsView = this.Q<VisualTreeAssetInspectorActionsView>(className:AssetActionsViewUssClass);
        InitializeSearchField();

        // Attributes
        m_AttributesInspector = this.Q<VisualElementAttributesInspectorElement>();

        // Bindings
        m_BindingsInspector = this.Q<VisualElementBindingsInspectorElement>();

        // Context Sharing
        m_Header.AttributesView.ShareContext(m_AttributesInspector.AttributesView);
        m_BindingsInspector.AttributesView.ShareContext(m_AttributesInspector.AttributesView);

        // StyleSheet
        m_ClassListElement = this.Q<ClassListElement>(className: ClassListClass);
        m_MatchingSelectorsElement = this.Q<MatchingSelectorsElement>(className:MatchingSelectorsClass);

        // Styles
        m_StyleInspector = this.Q<StyleInspectorElement>(className:StyleInspectorClass);
        EnsureRecordingBanner();

        SetEditFlags(VisualElementEditFlags.None);
    }

    void EnsureRecordingBanner()
    {
        if (m_RecordingBanner != null)
            return;
        m_RecordingBanner = new HelpBox("", HelpBoxMessageType.Warning) { name = "RecordingBanner" };
        m_RecordingBanner.style.display = DisplayStyle.None;
        var assetActionsViewIndex = this.IndexOf(m_AssetActionsView);
        if (assetActionsViewIndex >= 0)
            this.Insert(assetActionsViewIndex , m_RecordingBanner);
    }

    void SetEditFlags(VisualElementEditFlags editFlags)
    {
        m_EditFlags = editFlags;

        switch (m_EditFlags)
        {
            // Complete read-only.
            case VisualElementEditFlags.None:
                m_Header.SetEnabled(false);
                m_AttributesInspector.IsReadOnly = true;
                m_AttributesInspector.EnableInClassList(ReadonlyAttributesInspectorClass, true);
                m_ClassListElement.IsReadOnly = true;
                m_StyleInspector.IsReadOnly = true;
                if (m_VariablesSection != null)
                    m_VariablesSection.enabledSelf = false;
                break;
            // Attribute overrides
            case VisualElementEditFlags.Attributes:
                m_Header.SetEnabled(false);
                m_AttributesInspector.IsReadOnly = false;
                m_AttributesInspector.EnableInClassList(ReadonlyAttributesInspectorClass, false);
                m_ClassListElement.IsReadOnly = true;
                m_StyleInspector.IsReadOnly = true;
                if (m_VariablesSection != null)
                    m_VariablesSection.enabledSelf = false;
                break;
            // Should almost never get here.
            case VisualElementEditFlags.Styles:
                m_Header.SetEnabled(false);
                m_AttributesInspector.IsReadOnly = true;
                m_AttributesInspector.EnableInClassList(ReadonlyAttributesInspectorClass, true);
                m_ClassListElement.IsReadOnly = false;
                m_StyleInspector.IsReadOnly = false;
                if (m_VariablesSection != null)
                    m_VariablesSection.enabledSelf = true;
                break;
            // Full editing
            case VisualElementEditFlags.FullyEditable:
                m_Header.SetEnabled(true);
                m_AttributesInspector.IsReadOnly = false;
                m_AttributesInspector.EnableInClassList(ReadonlyAttributesInspectorClass, false);
                m_ClassListElement.IsReadOnly = false;
                m_StyleInspector.IsReadOnly = false;
                if (m_VariablesSection != null)
                    m_VariablesSection.enabledSelf = true;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        UpdateControlsState();
        m_StyleInspector.Refresh();
    }


    void UpdateControlsState()
    {
        // Banner: show a blocking-reason message when recording but element is not fully animatable
        if (m_RecordingBanner != null && m_Element != null)
        {
            var message = m_IsRecording ? VisualElementRecordability.ProbeElement(m_Element).GetBlockedMessage() : null;
            if (message != null && m_EditFlags != VisualElementEditFlags.Styles && m_EditFlags != VisualElementEditFlags.FullyEditable)
            {
                m_RecordingBanner.text = message;
                m_RecordingBanner.style.display = DisplayStyle.Flex;
            }
            else
            {
                m_RecordingBanner.style.display = DisplayStyle.None;
            }
        }

        var inStaging = StageUtility.GetCurrentStage() is VisualElementEditingStage;
        var assetActionsVisible = DisplayStyle.None;
        var editableHelpBoxAndAssetPathFieldVisible = DisplayStyle.None;

        if (m_IsRecording && !inStaging)
        {
            assetActionsVisible = DisplayStyle.Flex;
        }
        else
        {
            if (m_EditFlags == VisualElementEditFlags.None && !inStaging)
                assetActionsVisible = editableHelpBoxAndAssetPathFieldVisible = DisplayStyle.Flex;
        }

        m_AssetActionsView.style.display = assetActionsVisible;
        m_AssetNotEditableHelpBox.style.display = editableHelpBoxAndAssetPathFieldVisible;
        m_AssetPathContainer.style.display = editableHelpBoxAndAssetPathFieldVisible;
    }

    /// <summary>
    /// Called by <see cref="VisualElementSelectionEditor"/> when recording starts, stops,
    /// or the selection changes while recording. Updates the recording banner, OpenInBuilder
    /// visibility, and passes the controller to the style inspector.
    /// </summary>
    internal void RefreshRecordingState(StyleInspectorAnimationRecordingContext controller)
    {
        IsRecording = controller != null;
        m_StyleInspector.SetAnimationController(controller);
        m_StyleInspector.Refresh();
    }

    protected override void HandleEventBubbleUp(EventBase evt)
    {
        switch (evt)
        {
            case AttachToPanelEvent attachToPanelEvent:
            {
                if (attachToPanelEvent.destinationPanel == null)
                    return;
                m_StyleInspector.contentContainer.Add(m_StyleInspectorDefaultContent = StyleInspectorDefaultContent.Get());
                InitializeVariablesSection();
                InitializeAnimationSectionVisibility();
                break;
            }
            case DetachFromPanelEvent detachFromPanelEvent:
            {
                if (detachFromPanelEvent.originPanel == null)
                    return;
                if (m_StyleInspectorDefaultContent != null)
                {
                    m_StyleInspectorDefaultContent.contentWasGenerated -= OnDefaultContentGeneratedForVariables;
                    m_StyleInspectorDefaultContent.contentWasGenerated -= OnDefaultContentGeneratedForAnimation;
                }
                m_StyleInspectorDefaultContent?.RemoveFromHierarchy();
                StyleInspectorDefaultContent.Release(m_StyleInspectorDefaultContent);
                m_StyleInspectorDefaultContent = null;
                m_VariablesSection = null;
                break;
            }
        }
        base.HandleEventBubbleUp(evt);
    }

    void InitializeVariablesSection()
    {
        m_VariablesSection = m_StyleInspectorDefaultContent.Q<VariablesInspector>();
        if (m_VariablesSection != null)
        {
            m_VariablesSection.enabledSelf = (EditFlags & VisualElementEditFlags.Styles) == VisualElementEditFlags.Styles;
            m_VariablesSection.Refresh(GetInlineStyleRule(), GetOrCreateInlineStyleRule);
            return;
        }

        m_StyleInspectorDefaultContent.contentWasGenerated += OnDefaultContentGeneratedForVariables;
    }

    void OnDefaultContentGeneratedForVariables(StyleInspectorDefaultContent content)
    {
        content.contentWasGenerated -= OnDefaultContentGeneratedForVariables;

        m_VariablesSection = content.Q<VariablesInspector>();
        if (m_VariablesSection != null)
        {
            m_VariablesSection.enabledSelf = (EditFlags & VisualElementEditFlags.Styles) == VisualElementEditFlags.Styles;
            m_VariablesSection.Refresh(GetInlineStyleRule(), GetOrCreateInlineStyleRule);
        }
    }

    void InitializeAnimationSectionVisibility()
    {
        if (m_StyleInspectorDefaultContent.Q(StyleRuleInspector.AnimationFoldoutName) != null)
        {
            UpdateAnimationSectionVisibility(m_StyleInspectorDefaultContent);
            AnimationClipNewButtonController.ConnectButton(m_StyleInspectorDefaultContent, GetAnimationClipDialogSubject);
            return;
        }

        m_StyleInspectorDefaultContent.contentWasGenerated += OnDefaultContentGeneratedForAnimation;
    }

    void OnDefaultContentGeneratedForAnimation(StyleInspectorDefaultContent content)
    {
        content.contentWasGenerated -= OnDefaultContentGeneratedForAnimation;
        UpdateAnimationSectionVisibility(content);
        AnimationClipNewButtonController.ConnectButton(content, GetAnimationClipDialogSubject);
    }

    string GetAnimationClipDialogSubject()
    {
        if (m_Element == null)
            return null;
        // Use the element name when available; fall back to the type for unnamed elements.
        return string.IsNullOrEmpty(m_Element.name) ? m_Element.GetType().Name : m_Element.name;
    }

    static void UpdateAnimationSectionVisibility(VisualElement content)
    {
        var animationSection = content.Q(StyleRuleInspector.AnimationFoldoutName);
        if (animationSection != null)
            // Use StyleKeyword.Null (not DisplayStyle.Flex) to avoid setting an inline style when enabling.
            // It breaks the behavior of inspector search otherwise.
            animationSection.style.display = UIToolkitProjectSettings.s_EnablePanelRendererAnimationAtBoot ? StyleKeyword.Null : DisplayStyle.None;
    }

    StyleRule GetInlineStyleRule()
    {
        if (m_Element?.visualTreeAssetSource?.inlineSheet == null)
            return null;

        var index = m_Element?.visualElementAsset?.ruleIndex;
        if (index == null || index < 0)
            return null;

        var styleSheet = m_Element.visualTreeAssetSource.inlineSheet;
        if (styleSheet.rules.Length <= index)
            return null;

        return styleSheet.rules[index.Value];
    }

    StyleRule GetOrCreateInlineStyleRule()
    {
        if (m_Element?.visualElementAsset == null || m_Element.visualTreeAssetSource == null)
            return null;

        var vta = m_Element.visualTreeAssetSource;
        var vea = m_Element.visualElementAsset;
        return vta.GetOrCreateInlineStyleRule(vea);
    }

    public override void Dispose()
    {
        base.Dispose();
        m_AttributesInspector.AttributesView.Context.Dispose();
        m_MatchingSelectorsElement.Target = null;
    }
}
