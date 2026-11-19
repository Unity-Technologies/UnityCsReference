// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// The UI Stage: edits a document in the scene's place, through a <see cref="UIDocumentSession"/> owned by
/// <see cref="AuthoringContext.Global"/>.
/// </summary>
/// <remarks>
/// The session owns the panel, the clone, undo, the clipboard and the saved state. The stage adds what only a
/// stage has: navigation and its breadcrumbs, when the document is shown, save prompts when leaving, the state
/// hash, and serialization of what it edits.
/// </remarks>
internal class VisualElementEditingStage : PreviewSceneStage, ISerializationCallbackReceiver
{
    /// <summary>
    /// How far the stage has come towards showing the document it edits.
    /// </summary>
    internal enum DisplayState
    {
        /// <summary>Created, and expected to show its document as soon as it is opened.</summary>
        None,

        /// <summary>
        /// Opened by a navigation that has already decided to move on from it, so it skips building a panel and
        /// cloning its document until it is reached for itself.
        /// </summary>
        Deferred,

        /// <summary>Showing its document in a panel of its own.</summary>
        Settled,

        /// <summary>Closed. Its panel is gone for good and it must not build another.</summary>
        Disposed,
    }

    private GlobalObjectId m_MainAsset;
    private int[] m_SerializedPath;
    private SubDocumentOptions m_Options;
    private GlobalObjectId m_PanelSettings;
    private DisplayState m_DisplayState;

    private GUIContent m_HeaderContent;

    private UIDocumentSession m_Session;

    public event Action<VisualElementEditingStage> MainDocumentWasCloned;
    public event Action<PanelElement> PanelWasRepainted;

    /// <summary>
    /// Raised once the authoring panel exists and the edited document has been cloned into it.
    /// </summary>
    /// <remarks>
    /// A domain reload recreates the stage and the Hierarchy independently, in an order neither controls, so a
    /// consumer that resolves the panel while building itself can come up empty. This lets it wait instead.
    /// </remarks>
    public event Action<VisualElementEditingStage> AuthoringPanelWasCreated;

    public override string assetPath => AssetDatabase.GetAssetPath(EditedVisualTreeAsset);

    /// <summary>
    /// The session the stage edits its document through, created and registered if the stage has none yet.
    /// </summary>
    /// <remarks>
    /// Created on first use rather than in <see cref="OnEnable"/>: after a domain reload, a Hierarchy can resolve
    /// the stage before the stage is enabled, and must find the session it will show.
    /// </remarks>
    internal UIDocumentSession GetOrCreateSession() => m_Session ??= new UIDocumentSession(AuthoringContext.Global);

    internal Panel GetAuthoringPanel() => m_Session?.Panel;

    /// <inheritdoc cref="UIDocumentSession.ResolveLocalRoot"/>
    internal VisualElement ResolveLocalRoot() => m_Session?.ResolveLocalRoot();

    internal override bool isValid => ValidateContext();

    internal PanelElement PanelElement => m_Session?.PanelElement;

    public VisualTreeAssetEditingContext Context
    {
        get => m_Session?.Document ?? default;
        private set => GetOrCreateSession().Document = value;
    }

    public BreadcrumbBar.SeparatorStyle SeparatorStyle { get; set; }

    public VisualTreeAsset EditedVisualTreeAsset => m_Session?.EditedVisualTreeAsset;

    public Clipboard Clipboard => m_Session?.Clipboard;

    public void SetContext(VisualTreeAssetEditingContext context)
    {
        Context = context;
        if (EditedVisualTreeAsset != null)
            m_HeaderContent.text = EditedVisualTreeAsset.name;
        m_HeaderContent.image = EditorGUIUtility.Load("VisualTreeAsset Icon") as Texture2D;
    }

    internal DisplayState State => m_DisplayState;

    /// <summary>
    /// Enters a stage on <paramref name="context"/>.
    /// </summary>
    /// <param name="deferRealization">
    /// Whether the caller is only passing through this stage on its way to a deeper one, so the stage can skip
    /// building a panel and cloning its document. What the stage landed on shows has to exist by the time its own
    /// switch completes: the Hierarchy registers the stage's authoring panel once, while the switch runs, and
    /// nothing asks it for one again afterwards.
    /// </param>
    internal static VisualElementEditingStage GoToStage(VisualTreeAssetEditingContext context, BreadcrumbBar.SeparatorStyle separatorStyle, bool setAsFirstItemAfterMainStage = false, bool deferRealization = false)
    {
        var stage = ScriptableObject.CreateInstance<VisualElementEditingStage>();
        stage.SeparatorStyle = separatorStyle;
        stage.m_DisplayState = deferRealization ? DisplayState.Deferred : DisplayState.None;
        stage.SetContext(context);
        StageUtility.GoToStage(stage, setAsFirstItemAfterMainStage);
        return stage;
    }

    public VisualElementEditingStage()
    {
        m_HeaderContent = new GUIContent();
    }

    /// <inheritdoc cref="UIDocumentSession.EnsureDocumentShown"/>
    internal void EnsureDocumentShown() => m_Session?.EnsureDocumentShown();

    internal void RequestRefresh() => m_Session?.RequestRefresh();

    public void RequestCanvasSize(Vector2 viewportSize, Vector2 canvasSize, Vector2 offset, float zoomFactor)
        => m_Session?.RequestCanvasSize(viewportSize, canvasSize, offset, zoomFactor);

    internal override Scene GetSceneAt(int index)
    {
        // Don't want no scene.
        return default;
    }

    internal override void SyncSceneViewToStage(SceneView sceneView)
    {
        // A non-default bit is required here for native gizmo culling to honor the focused scene set below.
        sceneView.overrideSceneCullingMask = SceneCullingMasks.MainStageSceneViewObjects | GetSceneCullingMask();
    }

    internal override void OnPreSceneViewRender(SceneView sceneView)
    {
        // Nothing staged here lives in a scene, so every real GameObject counts as out of context.
        StageUtility.EnableHidingForInContextEditingInSceneView(true);
        StageUtility.SetFocusedScene(scene);
        StageUtility.SetFocusedSceneContextRenderMode(StageUtility.ContextRenderMode.GreyedOut);
    }

    internal override void OnPostSceneViewRender(SceneView sceneView)
    {
        StageUtility.EnableHidingForInContextEditingInSceneView(false);
        StageUtility.SetFocusedScene(default);
        StageUtility.SetFocusedSceneContextRenderMode(StageUtility.ContextRenderMode.Normal);
    }

    internal override Stage GetContextStage()
    {
        // Share camera state with the main stage so SceneView zoom/offset
        // is not independently saved and restored when entering this stage.
        var history = StageNavigationManager.instance.stageHistory;
        return history.Count > 0 ? history[0] : this;
    }

    /// <summary>
    /// Creates the authoring panel the stage shows its document in. A stage that is only navigated through on
    /// the way to a deeper one never gets here, so it costs no more than its own existence.
    /// </summary>
    private void EnsureRealized()
    {
        m_DisplayState = DisplayState.Settled;
        GetOrCreateSession().CreatePanel();
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        // Enabled again without a domain reload: the disable disposed the session, but the stage still edits its
        // document.
        if (m_Session is { IsDisposed: true } disposed)
            m_Session = new UIDocumentSession(disposed.Document, AuthoringContext.Global);

        var session = GetOrCreateSession();
        session.DocumentCloned += OnDocumentCloned;
        session.PanelRepainted += OnPanelRepainted;

        RestoreSerializedContext();
        StageNavigationManager.instance.beforeSwitchingAwayFromStage += BeforeLeavingStage;

        // This is temporary fix for domain issues that are very specific to timings.
        // TODO: [MP] Remove once we have the proper reload attributes for managed objects.
        if (StageUtility.GetCurrentStage() == this)
        {
            EnsureRealized();
            session.CloneTree();
            session.Track();
            session.AttachToRegistry();
        }

        // A stage whose document could not be resolved is torn down by the next stage tick, so nothing should
        // start resolving live elements against it.
        if (EditedVisualTreeAsset != null)
            AuthoringPanelWasCreated?.Invoke(this);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        StageNavigationManager.instance.beforeSwitchingAwayFromStage -= BeforeLeavingStage;

        if (m_Session == null)
            return;

        m_Session.DocumentCloned -= OnDocumentCloned;
        m_Session.PanelRepainted -= OnPanelRepainted;
        m_Session.DisposeKeepingPanelOwner();
    }

    protected internal override bool OnOpenStage()
    {
        // Never shown (GetSceneAt keeps returning default): only its unique culling-mask bit is used.
        base.OnOpenStage();

        if (m_DisplayState != DisplayState.Deferred)
            OpenForDisplay();
        return true;
    }

    /// <summary>
    /// Shows the edited document, unless the stage is already showing it or has been closed. Safe to call again
    /// on a stage that has settled, for a caller that cannot tell whether the stage it lands on was opened as a
    /// destination or passed through.
    /// </summary>
    internal void OpenForDisplay()
    {
        if (m_DisplayState == DisplayState.Disposed || GetOrCreateSession().IsOpen)
            return;

        m_DisplayState = DisplayState.Settled;
        GetOrCreateSession().Open();
    }

    void BeforeLeavingStage(Stage stage)
    {
        if (stage != this)
            return;

        // Untracking here would re-register the selection registry behind the hierarchy handler on return.
        m_Session?.ClearDocumentTree();
    }

    protected override void OnCloseStage()
    {
        m_Session?.Dispose();
        m_DisplayState = DisplayState.Disposed;
        base.OnCloseStage();
    }

    protected internal override void OnReturnToStage()
    {
        base.OnReturnToStage();

        // A stage that was only passed through has never shown its document, so reaching it now is a first
        // display, not a return to one: it has no on-disk changes to pick back up, and the forced reimport that
        // does so would adopt whatever the document was left dirty with as its saved state.
        if (m_DisplayState is DisplayState.None or DisplayState.Deferred)
        {
            OpenForDisplay();
            AuthoringPanelWasCreated?.Invoke(this);
            return;
        }

        EnsureRealized();
        var session = GetOrCreateSession();
        session.Track();
        session.AttachToRegistry();
        session.ReimportAssets();
        session.RequestRefresh();
    }

    protected internal override GUIContent CreateHeaderContent()
    {
        if (EditedVisualTreeAsset != null)
            m_HeaderContent.text = EditedVisualTreeAsset.name;

        return m_HeaderContent;
    }

    internal override bool SupportsSaving()
    {
        return true;
    }

    // Dirtiness is tracked per asset, not per stage, so a stage only passed through must not answer for it: the
    // walk settles what it opens up front, but a caller that opted out of modals leaves that settling undone.
    internal override bool hasUnsavedChanges
        => m_DisplayState != DisplayState.Deferred && m_Session is { HasUnsavedChanges: true };

    internal override bool Save() => m_Session != null && m_Session.Save();

    internal override void DiscardChanges() => m_Session?.DiscardChanges();

    internal bool AskUserToSaveModifiedStage()
    {
        return AskUserToSaveModifiedStageBeforeSwitchingStage();
    }

    internal override bool AskUserToSaveModifiedStageBeforeSwitchingStage()
    {
        if (!hasUnsavedChanges)
            return true;

        var result = EditorDialog.DisplayComplexDecisionDialog(
            "UI Stage - Unsaved Changes Detected",
            "Do you want to save changes you made?",
            "Save",
            "Discard Changes",
            "Cancel",
            DialogIconType.Info
            );
        switch (result)
        {
            case DialogResult.Cancel:
                return false;
            case DialogResult.DefaultAction:
                if (Save())
                    return true;
                // TODO: Display error message.
                break;
            case DialogResult.AlternateAction:
                DiscardChanges();
                return true;
            default:
                throw new ArgumentOutOfRangeException();
        }

        // We'll see what happens here.
        return false;
    }

    internal override BreadcrumbBar.Item CreateBreadcrumbItem()
    {
        GUIContent content = CreateHeaderContent();

        var history = StageNavigationManager.instance.stageHistory;
        bool isLastCrumb = this == history[^1];
        var style = isLastCrumb ? BreadcrumbBar.DefaultStyles.labelBold : BreadcrumbBar.DefaultStyles.label;
        if (isAssetMissing)
        {
            style = isLastCrumb ? BreadcrumbBar.DefaultStyles.labelBoldMissing : BreadcrumbBar.DefaultStyles.labelMissing;
            content.tooltip = L10n.Tr("VisualTreeAsset Asset has been deleted.", null);
        }

        return new BreadcrumbBar.Item
        {
            content = content,
            guistyle = style,
            userdata = this,
            separatorstyle = SeparatorStyle
        };
    }

    /// <summary>
    /// Fills the viewport's breadcrumbs with the stage history, the path to what is shown: every crumb but the
    /// last navigates back to its stage.
    /// </summary>
    internal static void PopulateHistoryBreadcrumbs(UIViewport viewport)
    {
        viewport.ClearBreadcrumbs();

        var history = StageNavigationManager.instance.stageHistory;
        for (var i = 0; i < history.Count; i++)
        {
            var stage = history[i];
            var content = stage.CreateHeaderContent();
            var icon = content.image as Texture2D;
            var label = stage.hasUnsavedChanges ? content.text + "*" : content.text;

            var isCurrentStage = i == history.Count - 1;
            if (isCurrentStage)
            {
                viewport.PushBreadcrumb(label, icon);
            }
            else
            {
                viewport.PushBreadcrumb(label, icon, () => StageUtility.GoToStage(stage, false));
            }
        }
    }

    protected internal override Hash128 GetHashForStateStorage() => GetOrCreateSession().GetStateStorageHash();

    public void OnBeforeSerialize()
    {
        var context = Context;
        m_MainAsset = GlobalObjectId.GetGlobalObjectIdSlow(context.RootVisualTreeAsset);

        if (context.SubDocumentPath != null)
        {
            m_SerializedPath = new int[context.SubDocumentPath.Length];
            for (var i = 0; i < context.SubDocumentPath.Length; ++i)
                m_SerializedPath[i] = context.SubDocumentPath[i].id;
        }
        else
        {
            m_SerializedPath = null;
        }

        m_PanelSettings = GlobalObjectId.GetGlobalObjectIdSlow(context.PanelSettings);
        m_Options = context.SubDocumentOptions;
    }

    public void OnAfterDeserialize()
    {
        // Sadly, here, we can't deserialize GlobalObjectIds.
    }

    private void RestoreSerializedContext()
    {
        var main = (VisualTreeAsset)GlobalObjectId.GlobalObjectIdentifierToObjectSlow(m_MainAsset);
        if (!main)
            return;

        var path = new TemplateAsset[m_SerializedPath?.Length ?? 0];
        var vta = main;
        for (var i = 0; i < m_SerializedPath?.Length; ++i)
        {
            var templates = vta.DepthFirstTraversalOfType<TemplateAsset>();
            var found = false;
            foreach (var template in templates)
            {
                if (template.id == m_SerializedPath[i])
                {
                    path[i] = template;
                    vta = template.ResolveTemplate();
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                break;
            }
        }
        var options = m_Options;
        var settings = (PanelSettings)GlobalObjectId.GlobalObjectIdentifierToObjectSlow(m_PanelSettings);
        Context = new VisualTreeAssetEditingContext(main, path, options, settings);
    }

    private bool ValidateContext()
    {
        var context = Context;
        if (!context.RootVisualTreeAsset)
            return false;

        if (context.SubDocumentPath != null)
        {
            for (var i = context.SubDocumentPath.Length - 1; i >= 1; --i)
            {
                var template = context.SubDocumentPath[i];
                if (template.visualTreeAsset != context.SubDocumentPath[i - 1].ResolveTemplate())
                    return false;
            }

            if (context.SubDocumentPath.Length > 0 && !VisualTreeAssetEditingContext.CanResolveSubDocumentPath(context))
                return false;
        }
        return true;
    }

    void OnDocumentCloned(UIDocumentSession session) => MainDocumentWasCloned?.Invoke(this);

    void OnPanelRepainted(PanelElement panel) => PanelWasRepainted?.Invoke(panel);

    internal override string GetErrorMessage()
    {
        if (Context.SubDocumentPath is { Length: > 0 } && !VisualTreeAssetEditingContext.CanResolveSubDocumentPath(Context))
        {
            return $"The document '{EditedVisualTreeAsset?.name}' is no longer part of '{Context.RootVisualTreeAsset?.name}'.\n\nReturning to the main stage.";
        }

        return "The UI document being edited is no longer valid.\n\nReturning to the main stage.";
    }
}
