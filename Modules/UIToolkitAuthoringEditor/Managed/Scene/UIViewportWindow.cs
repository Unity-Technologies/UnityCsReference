// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitAuthoringFramework not yet converted
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Hierarchy.Editor;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.ShortcutManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[Icon("UIToolkit/Icons/UIViewportWindow.png")]
partial class UIViewportWindow : EditorWindow
{
    class ShortcutContext : IShortcutContext
    {
        // The Hierarchy only answers inside the UI Stage: its Main Stage rows are GameObjects, where the plain
        // F binding belongs to stock Frame Selected — which is why FrameAndFaceShortcut takes Alt+F.
        public bool active
        {
            get
            {
                if (focusedWindow is UIViewportWindow viewport)
                    return viewport.m_Context is { IsValid: true };

                return focusedWindow is HierarchyWindow &&
                       s_OpenWindows.Count > 0 &&
                       StageUtility.GetCurrentStage() is VisualElementEditingStage;
            }
        }
    }

    [AutoStaticsCleanupOnCodeReload]
    static readonly List<UIViewportWindow> s_OpenWindows = new();
    [AutoStaticsCleanupOnCodeReload]
    static UIViewportWindow s_LastFocusedWindow;
    [AutoStaticsCleanupOnCodeReload]
    static ShortcutContext s_ShortcutContext;

    const string k_MenuPath = "Window/UI Toolkit/UI Viewport";
    const int k_MenuPriority = 3019;

    const string k_VisualTreeAsset = "UIToolkitAuthoring/UIViewportWindow/UIViewportWindow.uxml";
    const string k_StyleSheetDark = "UIToolkitAuthoring/UIViewportWindow/UIViewportWindowDark.uss";
    const string k_StyleSheetLight = "UIToolkitAuthoring/UIViewportWindow/UIViewportWindowLight.uss";

    const string HiddenPostFix = "--hidden";

    public const string UssClass = "unity-ui-viewport";

    public const string EnterStageModeWarningContainerUssClass = UssClass + "__container__enter-stage-mode-warning";
    const string HiddenEnterStageModeWarningContainerUssClass = EnterStageModeWarningContainerUssClass + HiddenPostFix;
    const string EnterStageModeWarningLabelUssClass = UssClass + "__enter-stage-mode-warning";
    public const string ViewportWrapperContainerUssClass = UssClass + "__container__viewport-wrapper";
    const string HiddenViewportWrapperContainerUssClass = ViewportWrapperContainerUssClass + HiddenPostFix;

    const string CanvasUssClass = UssClass + "__canvas";
    const string ViewportUssClass = UssClass + "__viewport";

    VisualElement m_EnterStageModeOverlay;
    Label m_EnterStageModeLabel;
    VisualElement m_ViewportOverlay;
    Button m_OpenSettingsButton;

    [OnCodeLoaded]
    static void Initialize()
    {
        s_ShortcutContext = new ShortcutContext();
        EditorApplication.delayCall += () => ShortcutIntegration.instance.contextManager.RegisterToolContext(s_ShortcutContext);
    }

    [MenuItem(k_MenuPath, false, 3010, secondaryPriority = 3)]
    static void ShowWindow()
    {
        GetWindow<UIViewportWindow>();
    }

    [Shortcut("UI Viewport/Fit Viewport", typeof(ShortcutContext), KeyCode.F)]
    static void OnFitViewportShortcut(ShortcutArguments args)
        => RequestFramingCommand.Execute(CommandSources.Viewport, element: null, orientToFace: false);

    void OnFramingRequested(in CommandContext context)
    {
        if (context.Status != CommandExecutionStatus.Success || m_Viewport == null)
            return;
        var command = (RequestFramingCommand)context.Command;
        if (command.Element != null)
        {
            // RequestFramingCommand is broadcast — ignore elements that don't live in this viewport's
            // sub-panel, otherwise we'd read worldBound from a foreign panel and jump to bogus coords.
            var subRoot = m_Canvas?.PanelElement?.subRootVisualElement;
            if (command.Element.panel == null || command.Element.panel != subRoot?.panel)
                return;
            m_Viewport.FitViewport(command.Element);
        }
        else
        {
            m_Viewport.FitViewport();
        }
    }

    // What the window is pointed at when no UI Stage is open. Serialized so the preview survives a domain
    // reload, and sticky: only a selection that names a panel component replaces it.
    [SerializeField]
    Component m_PanelComponentSource;
    [SerializeField]
    VisualTreeAsset m_DocumentSource;
    [SerializeField]
    PanelSettings m_PanelSettingsSource;

    [NonSerialized]
    IUIViewportContext m_Context;

    UICanvas m_Canvas;
    UIViewport m_Viewport;

    PreviewThemeState m_ThemeState;
    PanelSettings m_PanelSettings;
    UxmlCodePreview m_UxmlPreview;
    UssCodePreview m_UssPreview;

    internal IUIViewportContext Context => m_Context;

    void OnEnable()
    {
        titleContent.text = "UI Viewport";
        titleContent.image = UIResources.GetIconForType(typeof(UIViewportWindow), UIResources.RequestSize.Px16, GetPixelsPerPoint(rootVisualElement)).texture;
        StageNavigationManager.instance.afterSuccessfullySwitchedToStage += OnStageChanged;
        UIToolkitAuthoringSettings.EnableInSceneAuthoringChanged += OnAuthoringSettingChanged;
        UIToolkitAuthoringSettings.MainStageAuthoringChanged += OnAuthoringSettingChanged;
        EditorApplication.projectChanged += OnProjectChanged;
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
        ObjectChangeEvents.changesPublished += OnObjectChangesPublished;
        Selection.selectionChanged += OnSelectionChanged;
        s_OpenWindows.Add(this);
        UICommandQueue.RegisterHandler<RequestFramingCommand>(OnFramingRequested);
    }

    void OnDisable()
    {
        StageNavigationManager.instance.afterSuccessfullySwitchedToStage -= OnStageChanged;
        EditorApplication.projectChanged -= OnProjectChanged;
        EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        ObjectChangeEvents.changesPublished -= OnObjectChangesPublished;
        Selection.selectionChanged -= OnSelectionChanged;
        UICommandQueue.UnregisterHandler<RequestFramingCommand>(OnFramingRequested);
        UIToolkitAuthoringSettings.EnableInSceneAuthoringChanged -= OnAuthoringSettingChanged;
        UIToolkitAuthoringSettings.MainStageAuthoringChanged -= OnAuthoringSettingChanged;
        s_OpenWindows.Remove(this);
        if (s_LastFocusedWindow == this)
            s_LastFocusedWindow = null;

        // The canvas holding the panel is rebuilt after a domain reload, so a panel the context owns has to go
        // with it. The canvas settings are meant to survive one, and only go when the window itself does.
        ReleaseContext(destroyCanvasSettings: false);
    }

    void OnFocus()
    {
        s_LastFocusedWindow = this;
    }

    [UsedImplicitly]
    void CreateGUI()
    {
        titleContent.image = UIResources.GetIconForType(typeof(UIViewportWindow), UIResources.RequestSize.Px16, GetPixelsPerPoint(rootVisualElement)).texture;

        var vta = EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;
        if (vta)
            vta.CloneTree(rootVisualElement);

        var styleSheetPath = EditorGUIUtility.isProSkin ? k_StyleSheetDark : k_StyleSheetLight;
        var styleSheet = EditorGUIUtility.Load(styleSheetPath) as StyleSheet;
        if (styleSheet)
            rootVisualElement.styleSheets.Add(styleSheet);

        m_EnterStageModeOverlay = rootVisualElement.Q(className: EnterStageModeWarningContainerUssClass);
        m_EnterStageModeLabel = rootVisualElement.Q<Label>(className: EnterStageModeWarningLabelUssClass);
        m_ViewportOverlay = rootVisualElement.Q(className: ViewportWrapperContainerUssClass);
        m_OpenSettingsButton = rootVisualElement.Q<Button>("unity-ui-viewport__open-settings-button");
        m_OpenSettingsButton.clicked += UIToolkitAuthoringSettingsProvider.OpenSettings;
        UpdateOpenSettingsButton();
        m_Canvas = rootVisualElement.Q<UICanvas>(CanvasUssClass);
        m_Viewport = rootVisualElement.Q<UIViewport>(ViewportUssClass);
        m_UxmlPreview = rootVisualElement.Q<UxmlCodePreview>();
        m_UssPreview = rootVisualElement.Q<UssCodePreview>();

        rootVisualElement.RegisterCallback<CanvasManipulatorMessageEvent>(OnCanvasManipulatorMessage);

        AdoptSelectionWhenSourceless();
        RefreshContext();
    }

    void OnCanvasManipulatorMessage(CanvasManipulatorMessageEvent e) =>
        ShowNotification(new GUIContent(e.Message, EditorGUIUtility.FindTexture("console.warnicon")), 4);

    void OnDestroy()
    {
        ReleaseContext(destroyCanvasSettings: true);
    }

    void OnStageChanged(Stage stage) => RefreshContext();

    // A deleted panel component is only reported here, and it is what the preview was resolved from.
    void OnHierarchyChanged() => RefreshContext();

    // What the viewport may author into follows these, without the document on screen changing.
    void OnAuthoringSettingChanged(bool enabled) => RefreshContext();

    void OnProjectChanged()
    {
        RefreshContext();
        RefreshContextMetadata();
    }

    void OnObjectChangesPublished(ref ObjectChangeEventStream stream)
    {
        if (m_Canvas == null)
            return;

        // Removing the component leaves the context stale rather than changed, and the event that reports it
        // names an object that no longer resolves to anything, so validity is re-checked on every batch.
        if (m_Context is { IsValid: false })
        {
            RefreshContext();
            return;
        }

        if (!m_PanelComponentSource)
            return;

        // The component can be pointed at another document, or its GameObject renamed, from the inspector —
        // neither a selection, a stage nor a scene change.
        var componentId = m_PanelComponentSource.GetEntityId();
        var gameObjectId = m_PanelComponentSource.gameObject.GetEntityId();

        for (var i = 0; i < stream.length; ++i)
        {
            if (stream.GetEventType(i) != ObjectChangeKind.ChangeGameObjectOrComponentProperties)
                continue;

            stream.GetChangeGameObjectOrComponentPropertiesEvent(i, out var args);
            if (args.entityId != componentId && args.entityId != gameObjectId)
                continue;

            RefreshContext();
            RefreshContextMetadata();
            return;
        }
    }

    void OnSelectionChanged()
    {
        // The UI Stage decides what the viewport shows; selecting inside it must not retarget the window.
        if (m_Canvas == null || StageUtility.GetCurrentStage() is VisualElementEditingStage)
            return;

        // Sticky: a selection naming no panel component leaves the viewport on the document it shows. The
        // refresh runs either way, which is what drops a source that was just deleted.
        var panelComponent = MainStageViewportSelection.ResolveFromSelection();
        if (panelComponent != null)
            SetPanelComponentSource(panelComponent);
        else
            RefreshContext();
    }

    /// <summary>Points the window at the document <paramref name="panelComponent"/> renders.</summary>
    internal void SetPanelComponentSource(IPanelComponent panelComponent)
    {
        m_PanelComponentSource = panelComponent as Component;
        m_DocumentSource = null;
        m_PanelSettingsSource = null;
        RefreshContext();
    }

    /// <summary>
    /// Points the window at <paramref name="document"/>, for callers with no panel component to point at. A
    /// null document empties the window.
    /// </summary>
    internal void SetDocumentSource(VisualTreeAsset document, PanelSettings panelSettings)
    {
        m_PanelComponentSource = null;
        m_DocumentSource = document;
        m_PanelSettingsSource = panelSettings;
        RefreshContext();
    }

    /// <summary>Points every open UI Viewport at <paramref name="document"/>.</summary>
    internal static void SetDocumentSourceForAll(VisualTreeAsset document, PanelSettings panelSettings)
    {
        for (var i = s_OpenWindows.Count - 1; i >= 0; --i)
            s_OpenWindows[i].SetDocumentSource(document, panelSettings);
    }

    /// <summary>
    /// Re-resolves what the window should show, swapping the context when that is something else. Everything
    /// that can change the answer ends up here.
    /// </summary>
    void RefreshContext()
    {
        // The window is enabled long before its GUI exists, for example while a window layout is loaded.
        if (m_Canvas == null)
            return;

        DropDeletedSources();

        var context = ResolveContext();
        if (!IsSameContext(m_Context, context))
            SetContext(context);

        UpdateDropManipulator();
        UpdateOverlays();
    }

    IUIViewportContext ResolveContext()
    {
        if (StageUtility.GetCurrentStage() is VisualElementEditingStage stage)
            return new StageViewportContext(stage);

        // Previewing a scene document is part of in-scene authoring: it is picked from the Hierarchy, which
        // that switch is what turns on.
        if (!UIToolkitAuthoringSettings.EnableInSceneUIAuthoring)
            return null;

        if (m_PanelComponentSource is IPanelComponent panelComponent && panelComponent.visualTreeAsset != null)
            return new DocumentViewportContext(panelComponent);

        if (m_DocumentSource != null)
            return new DocumentViewportContext(m_DocumentSource, m_PanelSettingsSource);

        return null;
    }

    /// <summary>
    /// Adopts what the selection names when the window has no source of its own, so opening it on a selected
    /// document shows that document at once. Called on window creation only, so a window pointed at nothing
    /// stays empty and a sticky source is never replaced behind the user's back.
    /// </summary>
    void AdoptSelectionWhenSourceless()
    {
        if (m_PanelComponentSource != null || m_DocumentSource != null)
            return;
        if (StageUtility.GetCurrentStage() is VisualElementEditingStage)
            return;

        m_PanelComponentSource = MainStageViewportSelection.ResolveFromSelection() as Component;
    }

    // A destroyed object is still a non-null C# reference, which would keep the window pointed at nothing.
    void DropDeletedSources()
    {
        if (!m_PanelComponentSource)
            m_PanelComponentSource = null;
        if (!m_DocumentSource)
            m_DocumentSource = null;
    }

    // Same means swapping one for the other would show the same thing, so the live panel can be kept. A stale
    // context never is, which is what gets it replaced rather than left on screen.
    static bool IsSameContext(IUIViewportContext current, IUIViewportContext candidate)
    {
        if (current == null || candidate == null)
            return current == null && candidate == null;

        return current.IsValid
               && current.GetType() == candidate.GetType()
               && ReferenceEquals(current.Source, candidate.Source)
               && current.EditedVisualTreeAsset == candidate.EditedVisualTreeAsset
               && current.PanelSettings == candidate.PanelSettings;
    }

    void SetContext(IUIViewportContext context)
    {
        ReleaseContext(destroyCanvasSettings: true);

        m_Context = context is { IsValid: true } ? context : null;
        if (m_Context != null)
            AcquireContext();

        UpdateDropManipulator();
        UpdateOverlays();
    }

    void AcquireContext()
    {
        m_Context.Acquire();

        var document = m_Context.EditedVisualTreeAsset;

        m_Canvas.HeaderTitle = m_Context.HeaderTitle;
        m_Canvas.RequestRefresh = m_Context.RequestRefresh;
        m_Canvas.SetContext(m_Context.PanelElement, m_Context.CanvasStorageKey);

        m_ThemeState = PreviewThemeState.ForDocument(m_Context.RootVisualTreeAsset);
        SetupThemeMenu(m_Context.PanelSettings, m_ThemeState.SelectedTheme);

        m_UxmlPreview.Asset = document;
        m_UssPreview.Asset = GetActiveStyleSheetQuery.Get() ?? document.GetAllReferencedStyleSheets().FirstOrDefault();
        UICommandQueue.RegisterHandler<ActiveStyleSheetChangedMessage>(ActiveStyleSheetChanged);
        UICommandQueue.RegisterHandler<GetCanvasThemeQuery>(GetCanvasThemeRequest);

        m_Context.PopulateBreadcrumbs(m_Viewport);
    }

    void ReleaseContext(bool destroyCanvasSettings)
    {
        // The window can be destroyed before CreateGUI runs, for example while a window layout is loaded.
        if (m_Canvas == null)
        {
            m_Context = null;
            return;
        }

        if (destroyCanvasSettings)
            m_Canvas.DestroySettingsPermanently();

        // The canvas has to let go of the panel before the context destroys it.
        m_Canvas.PanelElement = null;
        m_Canvas.RequestRefresh = null;
        ClearThemeMenu();

        m_UxmlPreview.Asset = null;
        m_UssPreview.Asset = null;
        UICommandQueue.UnregisterHandler<ActiveStyleSheetChangedMessage>(ActiveStyleSheetChanged);
        UICommandQueue.UnregisterHandler<GetCanvasThemeQuery>(GetCanvasThemeRequest);

        m_Viewport.ClearBreadcrumbs();

        if (m_Context != null)
        {
            m_Context.Release();
            m_Context = null;
        }

        UpdateDropManipulator();
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

    // What can change without the context itself changing: a document rename, a GameObject rename, the stage
    // history.
    void RefreshContextMetadata()
    {
        if (m_Canvas == null || m_Context == null)
            return;

        if (!m_Context.IsValid)
        {
            SetContext(null);
            return;
        }

        m_Canvas.HeaderTitle = m_Context.HeaderTitle;
        m_Context.PopulateBreadcrumbs(m_Viewport);
    }

    void UpdateOverlays()
    {
        if (m_EnterStageModeOverlay == null)
            return;

        var hasContext = m_Context is { IsValid: true };
        m_EnterStageModeOverlay.EnableInClassList(HiddenEnterStageModeWarningContainerUssClass, hasContext);
        m_ViewportOverlay.EnableInClassList(HiddenViewportWrapperContainerUssClass, !hasContext);

        if (!hasContext && m_EnterStageModeLabel != null)
        {
            // Without in-scene authoring there is nothing to pick a document from, so the stage is the only
            // way in — which is what the button below the message offers to change.
            m_EnterStageModeLabel.text = UIToolkitAuthoringSettings.EnableInSceneUIAuthoring
                ? L10n.Tr("Select a UI document in the Hierarchy to preview it.", null)
                : L10n.Tr("Enter visual element editing stage to have access to this feature.", null);
        }

        UpdateOpenSettingsButton();
    }

    void UpdateOpenSettingsButton()
    {
        if (m_OpenSettingsButton == null)
            return;

        m_OpenSettingsButton.style.display = UIToolkitAuthoringSettings.EnableInSceneUIAuthoring
            ? DisplayStyle.None
            : DisplayStyle.Flex;
    }

    void SetupThemeMenu(PanelSettings panelSettings, ThemeStyleSheet selectedTheme)
    {
        m_PanelSettings = panelSettings;
        m_Viewport.ThemeMenu.ThemeSelected += OnThemeMenuThemeSelected;
        m_Viewport.ThemeMenu.SelectedTheme = selectedTheme;
        m_Viewport.ThemeMenu.PanelSettings = panelSettings;

        if (m_Canvas.PanelElement != null)
            m_Canvas.PanelElement.ThemeStyleSheet = selectedTheme;
    }

    void OnThemeMenuThemeSelected(ThemeStyleSheet theme)
    {
        SetPreviewThemeCommand.Execute(CommandSources.Viewport, m_ThemeState, theme);

        if (m_Canvas.PanelElement != null)
            m_Canvas.PanelElement.ThemeStyleSheet = theme;
    }

    internal void ClearThemeMenu()
    {
        m_PanelSettings = null;

        if (m_Viewport?.ThemeMenu != null)
        {
            m_Viewport.ThemeMenu.ThemeSelected -= OnThemeMenuThemeSelected;
            m_Viewport.ThemeMenu.ClearItems();
        }

        if (m_Canvas?.PanelElement != null)
            m_Canvas.PanelElement.ThemeStyleSheet = null;
    }

    static float GetPixelsPerPoint(VisualElement element)
    {
        return element?.panel == null
            ? EditorGUIUtility.pixelsPerPoint : element.scaledPixelsPerPoint;
    }

    void ActiveStyleSheetChanged(in CommandContext context)
    {
        m_UssPreview.Asset = ((ActiveStyleSheetChangedMessage)context.Command).StyleSheet;
    }

    void GetCanvasThemeRequest(in CommandContext context)
    {
        var theme = m_Canvas?.PanelElement?.ThemeStyleSheet ?? m_PanelSettings?.themeStyleSheet;
        GetCanvasThemeQuery.QueryPayload.Execute(CommandSources.Viewport, theme);
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
