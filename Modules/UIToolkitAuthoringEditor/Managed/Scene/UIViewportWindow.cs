// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Hierarchy.Editor;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.ShortcutManagement;
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
                    return viewport.Context is { IsValid: true };

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
        => RequestFramingCommand.Execute(AuthoringContext.Global, CommandSources.Viewport, element: null, orientToFace: false);

    // What the window is pointed at when no UI Stage is open. Serialized so the preview survives a domain
    // reload, and sticky: only a selection that names a panel component replaces it.
    [SerializeField]
    Component m_PanelComponentSource;
    [SerializeField]
    VisualTreeAsset m_DocumentSource;
    [SerializeField]
    PanelSettings m_PanelSettingsSource;

    // The window is a view of the in-scene workflow, which runs on the global context.
    readonly AuthoringContext m_AuthoringContext = AuthoringContext.Global;

    UIViewportView m_View;

    internal UIViewportView View => m_View;
    internal IUIViewportContext Context => m_View?.Context;

    /// <summary>
    /// The settings of the canvas an open window shows <paramref name="document"/> on, preferring the window that
    /// shows it from <paramref name="source"/>, or <see langword="null"/> when no window shows it.
    /// </summary>
    internal static CanvasSettings FindCanvasSettings(VisualTreeAsset document, object source)
    {
        if (document == null)
            return null;

        CanvasSettings match = null;
        foreach (var window in s_OpenWindows)
        {
            var context = window.Context;
            if (context == null || context.RootVisualTreeAsset != document)
                continue;

            var settings = window.m_View.Canvas.Settings;
            if (settings == null)
                continue;

            if (ReferenceEquals(context.Source, source))
                return settings;

            if (match == null)
                match = settings;
        }

        return match;
    }

    void OnEnable()
    {
        titleContent.text = "UI Viewport";
        titleContent.image = UIResources.GetIconForType(typeof(UIViewportWindow), UIResources.RequestSize.Px16, GetPixelsPerPoint(rootVisualElement)).texture;
        UIStageNavigation.StageSettled += OnStageChanged;
        EditorApplication.projectChanged += OnProjectChanged;
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
        ObjectChangeEvents.changesPublished += OnObjectChangesPublished;
        m_AuthoringContext.Selection.Changed += OnSelectionChanged;
        s_OpenWindows.Add(this);
        UIAssetRegistry.instance.AssetDirtyStateChanged += OnAssetDirtyStateChanged;
    }

    void OnDisable()
    {
        UIStageNavigation.StageSettled -= OnStageChanged;
        EditorApplication.projectChanged -= OnProjectChanged;
        EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        ObjectChangeEvents.changesPublished -= OnObjectChangesPublished;
        m_AuthoringContext.Selection.Changed -= OnSelectionChanged;
        UIAssetRegistry.LiveInstance?.AssetDirtyStateChanged -= OnAssetDirtyStateChanged;
        s_OpenWindows.Remove(this);
        if (s_LastFocusedWindow == this)
            s_LastFocusedWindow = null;

        // The canvas holding the panel is rebuilt after a domain reload, so a panel the context owns has to go
        // with it. The canvas settings are meant to survive one, and only go when the window itself does.
        m_View?.ReleaseContext(destroyCanvasSettings: false);
    }

    void OnFocus()
    {
        s_LastFocusedWindow = this;
    }

    [UsedImplicitly]
    void CreateGUI()
    {
        titleContent.image = UIResources.GetIconForType(typeof(UIViewportWindow), UIResources.RequestSize.Px16, GetPixelsPerPoint(rootVisualElement)).texture;

        m_View = new UIViewportView(m_AuthoringContext);
        rootVisualElement.Add(m_View);

        rootVisualElement.RegisterCallback<CanvasManipulatorMessageEvent>(OnCanvasManipulatorMessage);

        AdoptSelectionWhenSourceless();
        RefreshContext();
    }

    void OnCanvasManipulatorMessage(CanvasManipulatorMessageEvent e) =>
        ShowNotification(new GUIContent(e.Message, EditorGUIUtility.FindTexture("console.warnicon")), 4);

    void OnDestroy()
    {
        m_View?.ReleaseContext(destroyCanvasSettings: true);
    }

    void OnStageChanged(Stage stage) => RefreshContext();

    // Keeps the breadcrumb "*" and the Save button in step with unsaved edits.
    void OnAssetDirtyStateChanged(UnityEngine.Object asset) => m_View?.RefreshMetadata();

    // A deleted panel component is only reported here, and it is what the preview was resolved from.
    void OnHierarchyChanged() => RefreshContext();

    // What the viewport may author into follows these, without the document on screen changing.
    void OnProjectChanged()
    {
        RefreshContext();
        m_View?.RefreshMetadata();
    }

    void OnObjectChangesPublished(ref ObjectChangeEventStream stream)
    {
        if (m_View == null)
            return;

        // Removing the component leaves the context stale rather than changed, and the event that reports it
        // names an object that no longer resolves to anything, so validity is re-checked on every batch.
        if (Context is { IsValid: false })
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
            m_View.RefreshMetadata();
            return;
        }
    }

    void OnSelectionChanged()
    {
        // The UI Stage decides what the viewport shows; selecting inside it must not retarget the window.
        if (m_View == null || StageUtility.GetCurrentStage() is VisualElementEditingStage)
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
        if (m_View == null)
            return;

        DropDeletedSources();

        var context = ResolveContext();
        if (!IsSameContext(m_View.Context, context))
            m_View.SetContext(context);

        m_View.RefreshAuthoringState();
    }

    IUIViewportContext ResolveContext()
    {
        if (StageUtility.GetCurrentStage() is VisualElementEditingStage stage)
            return SessionViewportContext.ForStage(stage);

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

    static float GetPixelsPerPoint(VisualElement element)
    {
        return element?.panel == null
            ? EditorGUIUtility.pixelsPerPoint : element.scaledPixelsPerPoint;
    }
}
