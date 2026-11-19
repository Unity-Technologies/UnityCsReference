// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// The viewport pane: the canvas a document is previewed and edited on, its code previews and its theme menu.
/// </summary>
/// <remarks>
/// The view shows whatever <see cref="IUIViewportContext"/> its host hands it, and answers the canvas queries and
/// framing requests of the <see cref="AuthoringContext"/> it was created for. Deciding what to show is the host's
/// job: the UI Viewport window follows the UI Stage and the selection, a window with a local context shows its own
/// session.
/// </remarks>
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
sealed partial class UIViewportView : VisualElement
{
    const string k_VisualTreeAsset = "UIToolkitAuthoring/UIViewportWindow/UIViewportWindow.uxml";
    const string k_StyleSheetDark = "UIToolkitAuthoring/UIViewportWindow/UIViewportWindowDark.uss";
    const string k_StyleSheetLight = "UIToolkitAuthoring/UIViewportWindow/UIViewportWindowLight.uss";

    const string k_HiddenPostFix = "--hidden";

    public const string UssClass = "unity-ui-viewport";

    public const string EnterStageModeWarningContainerUssClass = UssClass + "__container__enter-stage-mode-warning";
    const string k_HiddenEnterStageModeWarningContainerUssClass = EnterStageModeWarningContainerUssClass + k_HiddenPostFix;
    const string k_EnterStageModeWarningLabelUssClass = UssClass + "__enter-stage-mode-warning";
    public const string ViewportWrapperContainerUssClass = UssClass + "__container__viewport-wrapper";
    const string k_HiddenViewportWrapperContainerUssClass = ViewportWrapperContainerUssClass + k_HiddenPostFix;

    const string k_CanvasUssClass = UssClass + "__canvas";
    const string k_ViewportUssClass = UssClass + "__viewport";

    static readonly string k_DefaultEmptyStateText = L10n.Tr("Select a UI document in the Hierarchy to preview it.", null);

    /// <summary>
    /// Raised after any view lets go of its context or starts showing another one.
    /// </summary>
    [AutoStaticsCleanupOnCodeReload] // subscribers re-subscribe per instance after a reload
    internal static event Action<UIViewportView> ContextChanged;

    readonly AuthoringContext m_AuthoringContext;

    readonly VisualElement m_EmptyStateOverlay;
    readonly Label m_EmptyStateLabel;
    readonly VisualElement m_ViewportOverlay;
    readonly UICanvas m_Canvas;
    readonly UIViewport m_Viewport;
    readonly UxmlCodePreview m_UxmlPreview;
    readonly UssCodePreview m_UssPreview;

    IUIViewportContext m_Context;
    PreviewThemeState m_ThemeState;
    PanelSettings m_PanelSettings;
    StyleSheet m_DefaultTheme;
    string m_EmptyStateText = k_DefaultEmptyStateText;

    public UIViewportView(AuthoringContext authoringContext)
    {
        m_AuthoringContext = authoringContext;
        style.flexGrow = 1;

        var vta = EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;
        if (vta)
            vta.CloneTree(this);

        var styleSheetPath = EditorGUIUtility.isProSkin ? k_StyleSheetDark : k_StyleSheetLight;
        var styleSheet = EditorGUIUtility.Load(styleSheetPath) as StyleSheet;
        if (styleSheet)
            styleSheets.Add(styleSheet);

        m_EmptyStateOverlay = this.Q(className: EnterStageModeWarningContainerUssClass);
        m_EmptyStateLabel = this.Q<Label>(className: k_EnterStageModeWarningLabelUssClass);
        m_ViewportOverlay = this.Q(className: ViewportWrapperContainerUssClass);
        m_Canvas = this.Q<UICanvas>(k_CanvasUssClass);
        m_Viewport = this.Q<UIViewport>(k_ViewportUssClass);
        m_UxmlPreview = this.Q<UxmlCodePreview>();
        m_UssPreview = this.Q<UssCodePreview>();

        RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

        UpdateOverlays();
    }

    public AuthoringContext AuthoringContext => m_AuthoringContext;

    /// <summary>What the view shows, or null when it shows nothing.</summary>
    public IUIViewportContext Context => m_Context;

    public UICanvas Canvas => m_Canvas;
    public UIViewport Viewport => m_Viewport;
    public UxmlCodePreview UxmlPreview => m_UxmlPreview;
    public UssCodePreview UssPreview => m_UssPreview;

    /// <summary>The message shown in place of the canvas while the view has nothing to show.</summary>
    public string EmptyStateText
    {
        get => m_EmptyStateText;
        set
        {
            m_EmptyStateText = value;
            UpdateOverlays();
        }
    }

    /// <summary>
    /// Whether the toolbar offers to switch the shown document between runtime UI and editor UI, which it stores
    /// as its editor extension setting (<see cref="EditorExtensionAuthoring"/>). Editor UI previews in an
    /// editor-context panel, which only a host whose context follows that switch can offer. Without the menu,
    /// every document previews as runtime UI.
    /// </summary>
    public bool ShowContextTypeMenu
    {
        get => m_Viewport?.ThemeMenu?.ShowContextTypeMenu ?? false;
        set
        {
            if (m_Viewport?.ThemeMenu == null || m_Viewport.ThemeMenu.ShowContextTypeMenu == value)
                return;

            m_Viewport.ThemeMenu.ShowContextTypeMenu = value;
            RefreshContextType();
        }
    }

    /// <summary>Whether the shown document previews as runtime UI or as editor UI.</summary>
    public ContextType ContextType => m_Viewport.ThemeMenu.ContextType;

    /// <summary>
    /// Where the preview theme of a document is kept. By default it is shared by every view showing the document;
    /// a host that offers themes the others do not keeps its own. Read when a context is shown.
    /// </summary>
    public Func<VisualTreeAsset, PreviewThemeState> ThemeStateProvider { get; set; } = PreviewThemeState.ForDocument;

    /// <summary>
    /// The theme to preview with when neither the theme menu nor the panel settings name one. By default there is
    /// none, and the content previews unstyled. Read when a context is shown.
    /// </summary>
    public Func<StyleSheet> DefaultThemeProvider { get; set; }

    /// <summary>
    /// The theme chosen in the theme menu, or null for the default of the context type: the theme of the panel
    /// settings for runtime UI, the active editor theme for editor UI.
    /// </summary>
    public StyleSheet SelectedTheme => m_Context != null ? m_Viewport.ThemeMenu.SelectedTheme : null;

    /// <summary>
    /// Previews the shown document with <paramref name="theme"/>, as if it was chosen in the theme menu. Null
    /// goes back to the default of the context type.
    /// </summary>
    public void SelectTheme(StyleSheet theme)
    {
        if (m_Context == null)
            return;

        m_Viewport.ThemeMenu.SelectedTheme = theme;
        OnThemeMenuThemeSelected(theme);
    }

    /// <summary>
    /// Switches the shown document to <paramref name="contextType"/>, as if it was chosen in the context type
    /// menu: the change is written to the document and can be undone.
    /// </summary>
    public void SelectContextType(ContextType contextType)
    {
        if (m_Context != null)
            m_Viewport.ThemeMenu.SelectContextType(contextType);
    }

    /// <summary>
    /// Follows the editor extension setting of the shown document, which undo, a reload of the document or the
    /// project's default can change: the canvas previews in the matching context type, with the themes of it.
    /// </summary>
    public void RefreshContextType()
    {
        if (m_Context == null)
            return;

        var contextType = ResolveContextType();
        var themeMenu = m_Viewport.ThemeMenu;
        themeMenu.CanChangeContextType = m_Context.AllowsAuthoring;
        if (themeMenu.ContextType == contextType)
            return;

        themeMenu.ContextType = contextType;
        #pragma warning disable UAL0018 // the theme menu belongs to this view, which is rebuilt on code reload, so it never keeps a stale default theme
        themeMenu.SelectedTheme = m_ThemeState.GetSelectedTheme(contextType);
        #pragma warning restore UAL0018
        ApplyPreviewTheme();
    }

    ContextType ResolveContextType()
        => ShowContextTypeMenu && EditorExtensionAuthoring.IsEnabled(m_Context.EditedVisualTreeAsset) ? ContextType.Editor : ContextType.Player;

    // Framing requests are answered only while the view is on screen, which is also when it has a viewport to
    // frame. Registration is idempotent, so a re-attach after docking is harmless.
    void OnAttachToPanel(AttachToPanelEvent evt)
        => m_AuthoringContext.Messages.RegisterHandler<RequestFramingCommand>(OnFramingRequested);

    void OnDetachFromPanel(DetachFromPanelEvent evt)
        => m_AuthoringContext.Messages.UnregisterHandler<RequestFramingCommand>(OnFramingRequested);

    void OnFramingRequested(in CommandContext context)
    {
        if (context.Status != CommandExecutionStatus.Success || m_Viewport == null)
            return;
        var command = (RequestFramingCommand)context.Command;
        if (command.Element != null)
        {
            // RequestFramingCommand is broadcast, and outside the UI Stage it names the live element while this
            // canvas shows a clone of it — so it is resolved onto our own panel rather than matched against it.
            // One that maps onto nothing here is still ignored: reading worldBound from a foreign panel would
            // frame bogus coords.
            var target = m_Canvas?.DocumentRoot?.ResolveCanvasElement(command.Element);
            if (target == null)
                return;
            m_Viewport.FitViewport(target);
        }
        else
        {
            m_Viewport.FitViewport();
        }
    }

    /// <summary>
    /// Shows <paramref name="context"/>, releasing the one shown before. An invalid context shows nothing.
    /// </summary>
    public void SetContext(IUIViewportContext context)
    {
        ReleaseContextSilently(destroyCanvasSettings: true);

        m_Context = context is { IsValid: true } ? context : null;
        if (m_Context != null)
            AcquireContext();

        RefreshAuthoringState();
        ContextChanged?.Invoke(this);
    }

    void AcquireContext()
    {
        m_Context.Acquire();

        var document = m_Context.EditedVisualTreeAsset;

        m_Canvas.HeaderTitle = m_Context.HeaderTitle;
        m_Canvas.RequestRefresh = m_Context.RequestRefresh;
        // Before SetContext: acquiring the panel rebuilds the handles from the current selection, which already
        // needs to know whether what it holds is a live element this canvas only previews.
        m_Canvas.DocumentRoot.AuthoritativeRootProvider = () => m_Context?.AuthoritativeRoot;
        m_Canvas.SetContext(m_Context.PanelElement, m_Context.CanvasStorageKey);

        m_ThemeState = (ThemeStateProvider ?? PreviewThemeState.ForDocument)(m_Context.RootVisualTreeAsset);
        SetupThemeMenu(m_Context.PanelSettings);

        m_UxmlPreview.Asset = document;
        m_UssPreview.Asset = GetActiveStyleSheetQuery.Get(m_AuthoringContext) ?? document.GetAllReferencedStyleSheets().FirstOrDefault();
        m_AuthoringContext.Messages.RegisterHandler<ActiveStyleSheetChangedMessage>(ActiveStyleSheetChanged);
        m_AuthoringContext.Messages.RegisterHandler<GetCanvasThemeQuery>(GetCanvasThemeRequest);

        m_Viewport.SetSaveTarget(m_Context);

        m_Context.PopulateBreadcrumbs(m_Viewport);
    }

    /// <summary>
    /// Lets go of the context the view shows, if any.
    /// </summary>
    /// <param name="destroyCanvasSettings">
    /// False keeps the canvas framing of the context, for a release that a domain reload will undo.
    /// </param>
    public void ReleaseContext(bool destroyCanvasSettings)
    {
        ReleaseContextSilently(destroyCanvasSettings);
        ContextChanged?.Invoke(this);
    }

    // Without raising ContextChanged, for SetContext to raise it once the new context is shown.
    void ReleaseContextSilently(bool destroyCanvasSettings)
    {
        if (destroyCanvasSettings)
            m_Canvas.DestroySettingsPermanently();

        // The canvas has to let go of the panel before the context destroys it.
        m_Canvas.PanelElement = null;
        m_Canvas.RequestRefresh = null;
        m_Canvas.DocumentRoot.AuthoritativeRootProvider = null;
        ClearThemeMenu();

        m_UxmlPreview.Asset = null;
        m_UssPreview.Asset = null;
        m_AuthoringContext.Messages.UnregisterHandler<ActiveStyleSheetChangedMessage>(ActiveStyleSheetChanged);
        m_AuthoringContext.Messages.UnregisterHandler<GetCanvasThemeQuery>(GetCanvasThemeRequest);

        m_Viewport.ClearBreadcrumbs();
        m_Viewport.SetSaveTarget(null);

        if (m_Context != null)
        {
            m_Context.Release();
            m_Context = null;
        }

        UpdateDropManipulator();
    }

    /// <summary>
    /// Re-applies what can change while the same context stays on screen: whether the view may author into it,
    /// and which overlay is visible.
    /// </summary>
    public void RefreshAuthoringState()
    {
        UpdateDropManipulator();
        UpdateOverlays();
    }

    /// <summary>
    /// Re-reads what can change without the context itself changing: a document rename, a GameObject rename,
    /// the stage history, the unsaved state. A context that went stale is dropped.
    /// </summary>
    public void RefreshMetadata()
    {
        if (m_Context == null)
            return;

        if (!m_Context.IsValid)
        {
            SetContext(null);
            return;
        }

        m_Canvas.HeaderTitle = m_Context.HeaderTitle;
        m_Viewport.SetSaveTarget(m_Context);
        m_Context.PopulateBreadcrumbs(m_Viewport);
    }

    // Rewired on every refresh, not only when the context changes: what the viewport may author into follows a
    // setting that can be toggled while the same document stays on screen. A null document leaves it inert.
    void UpdateDropManipulator()
    {
        var context = m_Context is { IsValid: true, AllowsAuthoring: true } ? m_Context : null;

        m_Viewport.DropManipulator.EditedVisualTreeAsset = context?.EditedVisualTreeAsset;
        m_Viewport.DropManipulator.RequestRefresh = context != null ? context.RequestRefresh : null;
        m_Viewport.DropManipulator.WouldCauseCircularDependency = context != null ? context.WillCauseCircularDependency : null;
    }

    void UpdateOverlays()
    {
        if (m_EmptyStateOverlay == null)
            return;

        var hasContext = m_Context is { IsValid: true };
        m_EmptyStateOverlay.EnableInClassList(k_HiddenEnterStageModeWarningContainerUssClass, hasContext);
        m_ViewportOverlay.EnableInClassList(k_HiddenViewportWrapperContainerUssClass, !hasContext);

        if (!hasContext && m_EmptyStateLabel != null)
            m_EmptyStateLabel.text = m_EmptyStateText;
    }

    /// <summary>
    /// Re-reads the panel settings of the shown context, which its host can change while the context stays on
    /// screen: the theme menu names their theme, and the preview uses it when no other theme is chosen.
    /// </summary>
    public void RefreshPanelSettings()
    {
        if (m_Context == null)
            return;

        m_PanelSettings = m_Context.PanelSettings;
        m_Viewport.ThemeMenu.PanelSettings = m_PanelSettings;
        ApplyPreviewTheme();
    }

    void SetupThemeMenu(PanelSettings panelSettings)
    {
        var themeMenu = m_Viewport.ThemeMenu;
        var contextType = ResolveContextType();

        m_PanelSettings = panelSettings;
        m_DefaultTheme = DefaultThemeProvider?.Invoke();
        themeMenu.ThemeSelected += OnThemeMenuThemeSelected;
        themeMenu.ContextTypeSelected += OnContextTypeSelected;
        themeMenu.CanChangeContextType = m_Context.AllowsAuthoring;
        themeMenu.ContextType = contextType;
        #pragma warning disable UAL0018 // the theme menu belongs to this view, which is rebuilt on code reload, so it never keeps a stale default theme
        themeMenu.SelectedTheme = m_ThemeState.GetSelectedTheme(contextType);
        #pragma warning restore UAL0018
        themeMenu.DefaultTheme = m_DefaultTheme;
        themeMenu.PanelSettings = panelSettings;
        ApplyPreviewTheme();
    }

    void OnThemeMenuThemeSelected(StyleSheet theme)
    {
        SetPreviewThemeCommand.Execute(m_AuthoringContext, CommandSources.Viewport, m_ThemeState, theme);
        ApplyPreviewTheme();
    }

    void OnContextTypeSelected(ContextType contextType)
    {
        var document = m_Context?.EditedVisualTreeAsset;
        if (document == null || !m_Context.AllowsAuthoring)
            return;

        EditorExtensionAuthoring.SetEnabled(CommandSources.Viewport, document, contextType == ContextType.Editor);
        RefreshContextType();
    }

    void ApplyPreviewTheme()
    {
        var themeMenu = m_Viewport.ThemeMenu;
        m_Canvas.PanelElement?.SetContextTypeAndTheme(themeMenu.ContextType, ResolvePreviewTheme(themeMenu.SelectedTheme));
    }

    // Checked on every change: the panel settings can be given a theme while they are shown, and the editor skin
    // can change.
    StyleSheet ResolvePreviewTheme(StyleSheet selectedTheme)
    {
        if (selectedTheme != null)
            return selectedTheme;

        if (m_Viewport.ThemeMenu.ContextType == ContextType.Editor)
            return ThemeUtility.GetStyleSheetForTheme(CanvasTheme.Default, null);

        return m_PanelSettings != null && m_PanelSettings.themeStyleSheet != null ? null : m_DefaultTheme;
    }

    internal void ClearThemeMenu()
    {
        m_PanelSettings = null;
        m_DefaultTheme = null;

        if (m_Viewport?.ThemeMenu != null)
        {
            m_Viewport.ThemeMenu.ThemeSelected -= OnThemeMenuThemeSelected;
            m_Viewport.ThemeMenu.ContextTypeSelected -= OnContextTypeSelected;
            m_Viewport.ThemeMenu.ClearItems();
        }

        if (m_Canvas?.PanelElement != null)
            m_Canvas.PanelElement.Theme = null;
    }

    void ActiveStyleSheetChanged(in CommandContext context)
    {
        m_UssPreview.Asset = ((ActiveStyleSheetChangedMessage)context.Command).StyleSheet;
    }

    void GetCanvasThemeRequest(in CommandContext context)
    {
        var theme = m_Canvas?.PanelElement?.Theme ?? m_PanelSettings?.themeStyleSheet;
        GetCanvasThemeQuery.QueryPayload.Execute(m_AuthoringContext, CommandSources.Viewport, theme);
    }
}
