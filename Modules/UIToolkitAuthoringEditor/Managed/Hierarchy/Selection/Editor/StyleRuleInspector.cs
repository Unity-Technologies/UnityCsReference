// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Properties;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[UxmlElement]
internal sealed partial class StyleRuleInspector : UIInspector
{
    class __SelectorElement : VisualElement;

    public static readonly BindingId StyleRuleProperty = nameof(StyleRule);
    public static readonly BindingId IsReadOnlyProperty = nameof(IsReadOnly);

    public const string UssClass = "unity-style-rule-inspector";
    public const string StyleInspectorClass = UssClass + "__style-inspector";
    public const string ReadOnlyStyleInspectorClass = "unity-visual-element-inspector__style-inspector--readonly";
    private const string k_VisualTreeAsset = "UIToolkitAuthoring/Inspector/StyleRuleInspector.uxml";
    private const string k_StyleSheetDark = "UIToolkitAuthoring/Inspector/UIToolkitAuthoringInspectorDark.uss";
    private const string k_StyleSheetLight = "UIToolkitAuthoring/Inspector/UIToolkitAuthoringInspectorLight.uss";

    internal const string VariablesFoldoutName = "style-inspector__style-section__variables";
    internal const string AnimationFoldoutName = "style-inspector__style-section__animation";
    internal const string GridFoldoutName = "style-inspector__style-section__grid";

    private readonly StyleInspectorElement m_StyleInspector;
    private StyleInspectorDefaultContent m_StyleInspectorDefaultContent;
    private VariablesInspector m_VariablesSection;

    private StyleRule m_StyleRule;
    private bool m_IsReadOnly;

    private PanelElement m_PanelElement;
    private __SelectorElement m_Element;
    private bool m_IsUpdating;
    private ThemeStyleSheet m_ThemeStyleSheet;

    [CreateProperty]
    public StyleRule StyleRule
    {
        get => m_StyleRule;
        set
        {
            m_StyleRule = value;

            if (m_StyleRule == null)
            {
                m_StyleInspector.Target = default;
                m_VariablesSection?.Refresh(null);
            }
            else
            {
                if (m_Element != null)
                {
                    SetSelectorElementInlineStyles();
                }
                else
                {
                    // If the element is still null, don't set the inline styles yet because the panel is still null.
                    // Wait until AttachToPanel.
                    m_Element = new __SelectorElement();
                }

                m_StyleInspector.Target = new StyleInspectorTarget(m_Element, m_StyleRule.styleSheet, m_StyleRule);
                m_VariablesSection?.Refresh(m_StyleRule);
            }

            MarkSearchDirty();
            NotifyPropertyChanged(StyleRuleProperty);
        }
    }

    [CreateProperty]
    public bool IsReadOnly
    {
        get => m_IsReadOnly;
        set
        {
            if (m_IsReadOnly == value && m_StyleInspector.IsReadOnly == value)
                return;
            m_IsReadOnly = value;
            m_StyleInspector.IsReadOnly = value;
            m_StyleInspector.EnableInClassList(ReadOnlyStyleInspectorClass, value);
            NotifyPropertyChanged(IsReadOnlyProperty);
        }
    }

    // Pushes the animation recording context onto the style fields so a selected rule's edits
    // route into its clip while the Animation Window records (mirrors VisualElementInspector).
    internal override void RefreshRecordingState(StyleInspectorAnimationRecordingContext controller)
    {
        m_StyleInspector.SetAnimationController(controller);
    }

    public StyleRuleInspector()
    {
        AddToClassList(UssClass);

        var vta = EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;
        vta.CloneTree(this);

        var styleSheetPath = EditorGUIUtility.isProSkin ? k_StyleSheetDark : k_StyleSheetLight;
        var styleSheet = EditorGUIUtility.Load(styleSheetPath) as StyleSheet;
        styleSheets.Add(styleSheet);
        m_StyleInspector = this.Q<StyleInspectorElement>(className:StyleInspectorClass);
        IsReadOnly = false;
    }

    void OnPreviewThemeChanged(in CommandContext context)
    {
        m_ThemeStyleSheet = ((SetPreviewThemeCommand)context.Command).Theme;
        SetSelectorElementInlineStyles();
    }

    void RefreshRulePreviewAndVariables()
    {
        SetSelectorElementInlineStyles();
        m_VariablesSection?.Refresh(m_StyleRule);
    }

    void OnVariableChange(in CommandContext context)
    {
        if (m_IsUpdating)
            return;
        RefreshRulePreviewAndVariables();
    }

    void OnUndoRedoPerformed()
    {
        if (m_PanelElement == null || m_StyleRule?.styleSheet == null)
            return;

        if (m_IsUpdating)
            return;

        m_IsUpdating = true;
        try
        {
            RefreshRulePreviewAndVariables();
        }
        finally
        {
            m_IsUpdating = false;
        }
    }

    [EventInterest(typeof(AttachToPanelEvent), typeof(DetachFromPanelEvent))]
    protected override void HandleEventBubbleUp(EventBase evt)
    {
        switch (evt)
        {
            case AttachToPanelEvent attachToPanelEvent:
            {
                if (attachToPanelEvent.destinationPanel == null)
                    return;
                m_PanelElement = new PanelElement();
                m_PanelElement.CreateSubPanel();
                m_Element ??= new __SelectorElement();
                m_PanelElement.SubPanel.visualTree.Add(m_Element);

                m_ThemeStyleSheet = GetCanvasThemeQuery.Get();
                SetSelectorElementInlineStyles();

                m_StyleInspector.contentContainer.Add(m_StyleInspectorDefaultContent = StyleInspectorDefaultContent.Get());
                InitializeVariablesSection();
                InitializeAnimationSectionVisibility();
                InitializeZIndexFieldVisibility();

                m_StyleInspector.Target = new StyleInspectorTarget(m_Element, m_StyleRule?.styleSheet, m_StyleRule);

                Undo.undoRedoPerformed += OnUndoRedoPerformed;
                UICommandQueue.RegisterHandler<SetPreviewThemeCommand>(OnPreviewThemeChanged);
                UICommandQueue.RegisterHandlerForCategory(CommandCategory.Variables, OnVariableChange);
                break;
            }
            case DetachFromPanelEvent detachFromPanelEvent:
            {
                if (detachFromPanelEvent.originPanel == null)
                    return;

                if (m_StyleInspectorDefaultContent != null)
                {
                    m_StyleInspectorDefaultContent.contentWasGenerated -= OnDefaultContentGenerated;
                    m_StyleInspectorDefaultContent.contentWasGenerated -= OnContentGeneratedForAnimation;
                    m_StyleInspectorDefaultContent.contentWasGenerated -= OnContentGeneratedForZIndex;
                }
                UIToolkitProjectSettings.onEnableZIndexChanged -= OnEnableZIndexChangedForStyleRule;
                m_StyleInspectorDefaultContent?.RemoveFromHierarchy();
                StyleInspectorDefaultContent.Release(m_StyleInspectorDefaultContent);
                m_StyleInspectorDefaultContent = null;

                m_VariablesSection = null;

                m_StyleInspector.Target = default;

                m_PanelElement.DestroySubPanel();
                m_Element = null;
                m_PanelElement = null;
                m_ThemeStyleSheet = null;

                Undo.undoRedoPerformed -= OnUndoRedoPerformed;
                UICommandQueue.UnregisterHandler<SetPreviewThemeCommand>(OnPreviewThemeChanged);
                UICommandQueue.UnregisterHandlerForCategory(CommandCategory.Variables, OnVariableChange);
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
            m_VariablesSection.Refresh(m_StyleRule);
            return;
        }

        m_StyleInspectorDefaultContent.contentWasGenerated += OnDefaultContentGenerated;
    }

    void OnDefaultContentGenerated(StyleInspectorDefaultContent content)
    {
        content.contentWasGenerated -= OnDefaultContentGenerated;

        m_VariablesSection = content.Q<VariablesInspector>();
        m_VariablesSection?.Refresh(m_StyleRule);
    }

    void InitializeAnimationSectionVisibility()
    {
        if (m_StyleInspectorDefaultContent.Q(AnimationFoldoutName) != null)
        {
            UpdateAnimationSectionVisibility(m_StyleInspectorDefaultContent);
            HookGridSectionVisibility(m_StyleInspectorDefaultContent);
            // Style rules have no specific element to name; dialog uses its default subject.
            AnimationClipNewButtonController.ConnectButton(m_StyleInspectorDefaultContent, getDialogSubject: null);
            return;
        }

        m_StyleInspectorDefaultContent.contentWasGenerated += OnContentGeneratedForAnimation;
    }

    void OnContentGeneratedForAnimation(StyleInspectorDefaultContent content)
    {
        content.contentWasGenerated -= OnContentGeneratedForAnimation;
        UpdateAnimationSectionVisibility(content);
        HookGridSectionVisibility(content);
        AnimationClipNewButtonController.ConnectButton(content, getDialogSubject: null);
    }

    static void UpdateAnimationSectionVisibility(VisualElement content)
    {
        var animationSection = content.Q(AnimationFoldoutName);
        if (animationSection != null)
            // Use StyleKeyword.Null (not DisplayStyle.Flex) to avoid setting an inline style when enabling.
            // It breaks the behavior of inspector search otherwise.
            animationSection.style.display = UIToolkitProjectSettings.s_EnablePanelRendererAnimationAtBoot ? StyleKeyword.Null : DisplayStyle.None;
    }

    // Unlike the animation setting, the CSS Grid flag applies live, so the section tracks
    // onEnableGridLayoutChanged for as long as it is attached rather than reading a boot snapshot.
    internal static void HookGridSectionVisibility(VisualElement content)
    {
        var gridSection = content.Q(GridFoldoutName);
        if (gridSection == null)
            return;

        void Apply(bool enabled) => gridSection.style.display = enabled ? StyleKeyword.Null : DisplayStyle.None;
        Apply(UIToolkitProjectSettings.enableGridLayout);
        gridSection.RegisterCallback<AttachToPanelEvent>(_ =>
        {
            UIToolkitProjectSettings.onEnableGridLayoutChanged -= Apply;
            UIToolkitProjectSettings.onEnableGridLayoutChanged += Apply;
            Apply(UIToolkitProjectSettings.enableGridLayout);
        });
        gridSection.RegisterCallback<DetachFromPanelEvent>(_ => UIToolkitProjectSettings.onEnableGridLayoutChanged -= Apply);
    }

    void InitializeZIndexFieldVisibility()
    {
        if (m_StyleInspectorDefaultContent.Q<ZIndexStyleIntField>() != null)
        {
            UpdateZIndexFieldVisibility(m_StyleInspectorDefaultContent);
            UIToolkitProjectSettings.onEnableZIndexChanged += OnEnableZIndexChangedForStyleRule;
            return;
        }

        m_StyleInspectorDefaultContent.contentWasGenerated += OnContentGeneratedForZIndex;
    }

    void OnContentGeneratedForZIndex(StyleInspectorDefaultContent content)
    {
        content.contentWasGenerated -= OnContentGeneratedForZIndex;
        UpdateZIndexFieldVisibility(content);
        UIToolkitProjectSettings.onEnableZIndexChanged += OnEnableZIndexChangedForStyleRule;
    }

    void OnEnableZIndexChangedForStyleRule()
    {
        if (m_StyleInspectorDefaultContent != null)
            UpdateZIndexFieldVisibility(m_StyleInspectorDefaultContent);
    }

    static void UpdateZIndexFieldVisibility(VisualElement content)
    {
        var zIndexField = content.Q<ZIndexStyleIntField>();
        if (zIndexField == null)
            return;
        var row = zIndexField.GetFirstAncestorOfType<OverrideRow>();
        if (row != null)
            row.style.display = UIToolkitProjectSettings.enableZIndex ? StyleKeyword.Null : DisplayStyle.None;
    }

    void SetSelectorElementInlineStyles()
    {
        if (m_StyleRule == null || m_StyleRule.styleSheet == null || m_Element == null)
            return;

        m_Element.styleSheets.Clear();

        // Inject theme so theme variables appear in variableContext.
        var effectiveTheme = m_ThemeStyleSheet
            ?? (StageUtility.GetCurrentStage() as VisualElementEditingStage)?.Context.PanelSettings?.themeStyleSheet;
        if (effectiveTheme != null)
            m_Element.styleSheets.Add(effectiveTheme);

        m_Element.styleSheets.Add(m_StyleRule.styleSheet);

        m_Element.UpdateInlineRule(m_StyleRule.styleSheet, m_StyleRule);
        m_Element.IncrementVersion(VersionChangeType.StyleSheet | VersionChangeType.Styles);

        // Force immediate style resolution to update the element's variableContext
        m_PanelElement.SubPanel.ApplyStyles();
    }
}
