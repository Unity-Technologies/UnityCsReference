// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// A document opened for editing: the panel it is cloned into, its clipboard, and its unsaved changes.
/// </summary>
/// <remarks>
/// <para>
/// The UI Stage edits its document through a session, and a view outside any stage can open one of its own. The
/// session belongs to an <see cref="AuthoringContext"/>: the stage's to <see cref="AuthoringContext.Global"/>, any
/// other to the local context that owns it, which then claims the session's panel.
/// </para>
/// <para>
/// A document can be open in several sessions at once. Changes to its assets go through
/// <see cref="UICommandQueue"/>, and dirty state is kept per asset by <see cref="UIAssetRegistry"/>, so every
/// session showing the document sees every change.
/// </para>
/// </remarks>
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
sealed partial class UIDocumentSession : IDisposable
{
    const CommandCategory k_ExternalChangesTrackedCategories = ~CommandCategory.Save;

    // Before the canvas sizes the panel from its settings.
    static readonly Vector2Int k_InitialPanelSize = new(480, 640);

    [AutoStaticsCleanupOnCodeReload]
    [IgnoreForUAL0015("Registry of open sessions; cleared on code reload and refilled as the authoring views are rebuilt")]
    static readonly List<UIDocumentSession> s_Sessions = new();

    readonly Clipboard m_Clipboard = new();

    VisualTreeAssetEditingContext m_Document;
    VisualTreeAsset m_RootAsset;
    GlobalObjectId m_RootAssetId;
    PanelElement m_PanelElement;
    bool m_DocumentCloned;
    bool m_FrameUpdateRequested;
    bool m_ExternalChanges;
    bool m_ExternalSave;
    bool m_InsideGroup;
    // Set while we drive our own discard reload, so the registry's AssetReloaded event (which we also
    // subscribe to for other tools' changes) does not make us re-clone a second time.
    bool m_SelfReloading;

    /// <summary>
    /// Every session that is not disposed.
    /// </summary>
    public static IReadOnlyList<UIDocumentSession> Sessions => s_Sessions;

    public AuthoringContext Owner { get; }

    public bool IsDisposed { get; private set; }

    /// <summary>
    /// The document being edited. Setting it does not re-clone the panel; call <see cref="RequestRefresh"/>.
    /// </summary>
    public VisualTreeAssetEditingContext Document
    {
        get => m_Document;
        set
        {
            if (m_Document == value)
                return;

            m_Document = value;
            EditedVisualTreeAsset = m_Document.SubDocumentOptions != SubDocumentOptions.None
                ? m_Document.SubDocumentPath[^1].ResolveTemplate()
                : m_Document.RootVisualTreeAsset;

            if (!ReferenceEquals(m_RootAsset, m_Document.RootVisualTreeAsset))
            {
                m_RootAsset = m_Document.RootVisualTreeAsset;
                m_RootAssetId = m_RootAsset ? GlobalObjectId.GetGlobalObjectIdSlow(m_RootAsset) : default;
            }
        }
    }

    public VisualTreeAsset EditedVisualTreeAsset { get; private set; }

    /// <summary>
    /// The style sheet the session edits on its own, over a read-only preview of <see cref="Document"/>. Null when
    /// the session edits the document.
    /// </summary>
    public StyleSheet StandaloneStyleSheet { get; }

    /// <summary>
    /// Whether the document is only shown: nothing in it can be edited, as when a style sheet is edited on its own.
    /// </summary>
    public bool IsDocumentReadOnly => StandaloneStyleSheet != null;

    // What Save and Discard settle.
    UnityEngine.Object EditedAsset => StandaloneStyleSheet != null ? StandaloneStyleSheet : EditedVisualTreeAsset;

    /// <summary>
    /// The element the document is rendered by. It outlives its sub-panel: <see cref="Panel"/> is only there once
    /// the session is open.
    /// </summary>
    public PanelElement PanelElement => m_PanelElement;

    public Panel Panel => m_PanelElement?.SubPanel;

    /// <summary>
    /// Whether the session has created the panel its document is cloned into.
    /// </summary>
    public bool IsOpen => m_PanelElement is { IsCreated: true };

    /// <summary>
    /// Whether the panel holds a clone of the document.
    /// </summary>
    public bool IsDocumentShown => m_DocumentCloned;

    public Clipboard Clipboard => m_Clipboard;

    /// <summary>
    /// Raised after the document is cloned into the panel. Every element of the previous clone is gone.
    /// </summary>
    public event Action<UIDocumentSession> DocumentCloned;

    /// <summary>
    /// Raised when the session creates its panel, before the document is cloned into it.
    /// </summary>
    public event Action<UIDocumentSession> PanelCreated;

    public event Action<PanelElement> PanelRepainted;

    public UIDocumentSession(AuthoringContext owner)
    {
        if (owner == null)
            throw new ArgumentNullException(nameof(owner));
        if (owner.IsDisposed)
            throw new ObjectDisposedException(nameof(AuthoringContext));

        Owner = owner;

        m_PanelElement = new PanelElement();
        m_PanelElement.OnAfterRepaint += OnPanelRepainted;
        m_PanelElement.SubPanelReleasing += OnSubPanelReleasing;
        m_PanelElement.SubPanelRecreated += OnSubPanelRecreated;

        // Claimed before anything resolves a context from the document's elements, and after the session is set,
        // which throws when the context already has one.
        if (!owner.IsGlobal)
        {
            owner.SetSession(this);
            owner.Claim(m_PanelElement);
        }

        Undo.undoRedoPerformed += OnUndoRedoPerformed;
        UICommandQueue.RegisterHandlerForCategory(CommandCategory.Styling, OnStylingChanged);
        UICommandQueue.GroupBegan += OnGroupBegan;
        UICommandQueue.GroupEnded += OnGroupEnded;
        UICommandQueue.RegisterHandlerForCategory(k_ExternalChangesTrackedCategories, CheckForBuilderChanges);
        UICommandQueue.RegisterHandlerForCategory(CommandCategory.Save, OnBuilderSave);
        UIAssetRegistry.instance.AssetReloaded += OnRegistryAssetReloaded;

        s_Sessions.Add(this);
    }

    public UIDocumentSession(VisualTreeAssetEditingContext document, AuthoringContext owner)
        : this(owner)
    {
        Document = document;
    }

    /// <summary>
    /// Opens <paramref name="styleSheet"/> to be edited on its own, previewed on <paramref name="previewDocument"/>,
    /// which the session never changes.
    /// </summary>
    public UIDocumentSession(VisualTreeAssetEditingContext previewDocument, StyleSheet styleSheet, AuthoringContext owner)
        : this(previewDocument, RequireStyleSheet(styleSheet, owner))
    {
        StandaloneStyleSheet = styleSheet;
    }

    // Checked before the session registers anything: without the style sheet it would edit the preview.
    static AuthoringContext RequireStyleSheet(StyleSheet styleSheet, AuthoringContext owner)
        => styleSheet != null ? owner : throw new ArgumentNullException(nameof(styleSheet));

    /// <summary>
    /// What can be changed on <paramref name="element"/>: nothing when the document is read-only, else what its
    /// document allows.
    /// </summary>
    public VisualElementEditFlags GetElementEditFlags(VisualElement element)
        => IsDocumentReadOnly ? VisualElementEditFlags.None : m_Document.GetElementEditFlags(element);

    /// <summary>
    /// Creates the panel, starts tracking it, and clones the document into it. Does nothing when already open.
    /// </summary>
    public void Open()
    {
        if (IsDisposed || IsOpen)
            return;

        CreatePanel();
        Track();
        ReloadAssets();
        AttachToRegistry();
        RequestRefresh();
    }

    /// <summary>
    /// Creates the panel the document is cloned into, without cloning it.
    /// </summary>
    internal void CreatePanel()
    {
        if (IsDisposed || IsOpen)
            return;

        m_PanelElement.CreateSubPanel();
        Binding.SetPanelLogLevel(m_PanelElement.SubPanel, BindingLogLevel.None);
        m_PanelElement.SetPanelSize(k_InitialPanelSize);
        m_PanelElement.PanelSettings = m_Document.PanelSettings;
        PanelCreated?.Invoke(this);
    }

    /// <summary>
    /// Shows the edited document to whoever just started watching the panel, for a caller that needs the document
    /// to be there rather than to be up to date. The tree is only cloned when the panel does not already hold it,
    /// but the panel is ticked either way: a change processor registered on it gets its first look at the tree
    /// from that tick.
    /// </summary>
    public void EnsureDocumentShown()
    {
        if (!m_DocumentCloned)
        {
            RequestRefresh();
            return;
        }

        if (IsOpen)
            m_PanelElement.FrameUpdate();
    }

    public void RequestRefresh()
    {
        if (!IsOpen)
            return;

        // Process whatever changes we previously add before cloning to ensure everything is up to date.
        m_PanelElement.FrameUpdate();
        CloneTree();
        m_PanelElement.FrameUpdate();
    }

    public void RequestCanvasSize(Vector2 viewportSize, Vector2 canvasSize, Vector2 offset, float zoomFactor)
    {
        if (!IsOpen)
            return;

        m_PanelElement.ResizeRenderTexture(viewportSize);
        m_PanelElement.Offset = offset;
        m_PanelElement.ScaleFactor = zoomFactor;
        m_PanelElement.Size = canvasSize;

        if (viewportSize.x == 0 || viewportSize.y == 0)
            return;

        m_PanelElement.FrameUpdate();
    }

    public void CloneTree()
    {
        var subRoot = m_PanelElement?.subRootVisualElement;
        if (subRoot == null)
            return;

        subRoot.Clear();

        switch (m_Document.SubDocumentOptions)
        {
            case SubDocumentOptions.None:
            case SubDocumentOptions.InContext:
                m_Document.RootVisualTreeAsset.CloneTree(subRoot);
                break;
            case SubDocumentOptions.Isolation:
                EditedVisualTreeAsset?.CloneTree(subRoot);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        // Kept on the root rather than added to the document, which the session does not change.
        if (StandaloneStyleSheet != null && !subRoot.styleSheets.Contains(StandaloneStyleSheet))
            subRoot.styleSheets.Add(StandaloneStyleSheet);

        m_DocumentCloned = true;
        DocumentCloned?.Invoke(this);
        UIAssetRegistry.instance.RefreshPanel(m_PanelElement?.SubPanel);
    }

    /// <summary>
    /// Empties the panel, keeping it open. The next <see cref="EnsureDocumentShown"/> clones the document again.
    /// </summary>
    public void ClearDocumentTree()
    {
        m_PanelElement?.subRootVisualElement?.Clear();
        m_DocumentCloned = false;
    }

    /// <summary>
    /// The live element the edited (sub-)document hangs from: the panel's root, or — when a sub-document is
    /// edited in context — the template instance along <see cref="VisualTreeAssetEditingContext.SubDocumentPath"/>
    /// that is being edited.
    /// </summary>
    /// <remarks>
    /// A session has no panel component, so this instance is the only thing that tells apart two clones of one
    /// template in the same document: they are cloned from the same assets and differ only in the chain of
    /// instances they hang from. A caller that authors without a live parent of its own — an add with nothing
    /// selected — names this one, so its result is selected where the user is editing rather than in whichever
    /// clone comes first.
    /// </remarks>
    public VisualElement ResolveLocalRoot()
    {
        var panel = Panel;
        if (panel == null)
            return null;

        var localRoot = panel is PanelElement.IPanelElementSubPanel subPanel ? subPanel.Root : panel.visualTree;

        if (m_Document.SubDocumentOptions is SubDocumentOptions.None or SubDocumentOptions.Isolation)
            return localRoot;

        var subDocumentPath = m_Document.SubDocumentPath;
        if (subDocumentPath == null)
            return localRoot;

        for (var i = 0; i < subDocumentPath.Length && localRoot != null; ++i)
        {
            var template = subDocumentPath[i];
            localRoot = localRoot.Query<TemplateContainer>()
                .Where(tc => (tc.visualElementAsset as TemplateAsset)?.id == template.id)
                .First();
        }

        return localRoot;
    }

    public bool WillCauseCircularDependency(VisualTreeAsset visualTreeAsset)
        => m_Document.WillCauseCircularDependency(visualTreeAsset);

    /// <summary>
    /// Identifies the edited document, and the chain of instances it is edited through when edited in context.
    /// </summary>
    public Hash128 GetStateStorageHash()
    {
        // Not the preview document's: the same document can also be edited, and keeps its own state then.
        if (StandaloneStyleSheet != null)
        {
            var styleSheetPath = AssetDatabase.GetAssetPath(StandaloneStyleSheet);
            return string.IsNullOrEmpty(styleSheetPath)
                ? new Hash128()
                : Hash128.Compute("StyleSheet:" + AssetDatabase.AssetPathToGUID(styleSheetPath));
        }

        switch (m_Document.SubDocumentOptions)
        {
            // When editing the root VisualTreeAsset or editing a VisualTreeAsset in isolation,
            // use the hash for the edited VisualTreeAsset
            case SubDocumentOptions.None:
            case SubDocumentOptions.Isolation:
            {
                var assetPath = AssetDatabase.GetAssetPath(EditedVisualTreeAsset);
                return string.IsNullOrEmpty(assetPath)
                    ? new Hash128()
                    : Hash128.Compute(AssetDatabase.AssetPathToGUID(assetPath));
            }
            // When editing a VisualTreeAsset in context, use a hash calculated from the all
            // the VisualTreeAssets from the root to the edited one.
            case SubDocumentOptions.InContext:
            {
                using var _ = StringBuilderPool.Get(out var sb);
                sb.Append(AssetDatabase.GetAssetPath(m_Document.RootVisualTreeAsset));

                for (var i = 0; i < m_Document.SubDocumentPath.Length; ++i)
                {
                    sb.Append('|');
                    var templateAsset = m_Document.SubDocumentPath[i];
                    var vta = templateAsset.ResolveTemplate();
                    sb.Append(AssetDatabase.GetAssetPath(vta));
                    sb.Append(templateAsset.id);
                }
                return Hash128.Compute(sb.ToString());
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>
    /// Whether the edited document, or a style sheet it references, has unsaved changes. For a style sheet edited
    /// on its own, whether that style sheet has.
    /// </summary>
    public bool HasUnsavedChanges
    {
        get
        {
            if (StandaloneStyleSheet != null)
                return UIAssetRegistry.instance.IsDirty(StandaloneStyleSheet);

            var vta = EditedVisualTreeAsset;
            if (vta == null)
                return false;

            var registry = UIAssetRegistry.instance;
            if (registry.IsDirty(vta))
                return true;

            using var _ = ListPool<StyleSheet>.Get(out var styleSheets);
            UIAssetRegistry.CollectDocumentStyleSheets(vta, styleSheets);
            foreach (var styleSheet in styleSheets)
                if (registry.IsDirty(styleSheet))
                    return true;
            return false;
        }
    }

    public bool Save()
    {
        var succeeded = UIAssetRegistry.instance.SaveAsset(EditedAsset, CommandSources.Stage);
        ReloadAssets();
        CloneTree();
        return succeeded;
    }

    public void DiscardChanges()
    {
        m_SelfReloading = true;
        try
        {
            UIAssetRegistry.instance.DiscardAsset(EditedAsset, CommandSources.Stage);
        }
        finally
        {
            m_SelfReloading = false;
        }

        ReloadAssets();
        CloneTree();
    }

    public void ReloadAssets()
    {
        if (VisualTreeAssetEditingContext.TryReload(m_Document, out var reloaded))
            Document = reloaded;
    }

    /// <summary>
    /// Brings the document back to its on-disk state, clean and with no undo history.
    /// </summary>
    public void ReimportAssets()
    {
        Document = VisualTreeAssetEditingContext.Reimport(m_Document);
        // A force reimport brings the assets back to their on-disk state, so re-baseline them clean.
        MarkTrackedAssetsClean();
        ClearUndoForEditedAssets();
    }

    /// <summary>
    /// Starts tracking the panel with <see cref="VisualElementSelectionRegistry"/>, so its elements can be selected.
    /// </summary>
    internal void Track()
    {
        if (Panel == null)
            return;

        VisualElementSelectionRegistry.Instance?.TrackSessionPanel(this);
    }

    internal void Untrack()
    {
        if (Panel == null)
            return;

        VisualElementSelectionRegistry.Instance?.UntrackSessionPanel(this);
    }

    /// <summary>
    /// Lets <see cref="UIAssetRegistry"/> watch the panel's assets, which is what reloads it when they change.
    /// </summary>
    internal void AttachToRegistry()
    {
        var panel = Panel;
        if (panel != null)
            UIAssetRegistry.instance.AttachPanel(panel, this, CollectRoots, ResolveAccess, CollectStyleSheetRoots);
    }

    internal void DetachFromRegistry()
    {
        var panel = Panel;
        if (panel != null)
            UIAssetRegistry.instance.DetachPanel(panel);
    }

    /// <summary>
    /// Closes the session for good: the panel and its owner are destroyed, and the session stops listening.
    /// </summary>
    public void Dispose()
    {
        if (!IsDisposed)
        {
            Untrack();
            DetachFromRegistry();
            Teardown();
        }

        // Also after DisposeKeepingPanelOwner: a stage is disabled before it is closed.
        m_PanelElement?.DestroyPanelPermanently();
        m_PanelElement = null;
    }

    /// <summary>
    /// Disposes the session but keeps its panel owner registered, for a stage that is being disabled rather than
    /// closed. A later <see cref="Dispose"/> still destroys the owner.
    /// </summary>
    internal void DisposeKeepingPanelOwner()
    {
        if (IsDisposed)
            return;

        Untrack();
        // Not UIAssetRegistry.instance: a disable that runs during teardown must not bring the singleton back.
        UIAssetRegistry.LiveInstance?.DetachPanel(Panel);
        m_PanelElement?.subRootVisualElement?.Clear();
        m_PanelElement?.DestroySubPanel();
        Teardown();
    }

    void Teardown()
    {
        IsDisposed = true;
        m_DocumentCloned = false;
        s_Sessions.Remove(this);

        if (!Owner.IsGlobal)
        {
            Owner.Release(m_PanelElement);
            Owner.ClearSession(this);
        }

        m_PanelElement.OnAfterRepaint -= OnPanelRepainted;
        m_PanelElement.SubPanelReleasing -= OnSubPanelReleasing;
        m_PanelElement.SubPanelRecreated -= OnSubPanelRecreated;
        m_Clipboard.Dispose();

        Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        EditorApplication.delayCall -= DoProcessBuilderChanges;
        EditorApplication.delayCall -= DoProcessBuilderSave;
        UICommandQueue.UnregisterHandlerForCategory(CommandCategory.Styling, OnStylingChanged);
        UICommandQueue.UnregisterHandlerForCategory(k_ExternalChangesTrackedCategories, CheckForBuilderChanges);
        UICommandQueue.UnregisterHandlerForCategory(CommandCategory.Save, OnBuilderSave);
        UICommandQueue.GroupBegan -= OnGroupBegan;
        UICommandQueue.GroupEnded -= OnGroupEnded;
        UIAssetRegistry.LiveInstance?.AssetReloaded -= OnRegistryAssetReloaded;
    }

    void OnSubPanelReleasing(PanelElement panelElement)
    {
        Untrack();
        DetachFromRegistry();
    }

    void OnSubPanelRecreated(PanelElement panelElement)
    {
        Binding.SetPanelLogLevel(panelElement.SubPanel, BindingLogLevel.None);
        Track();
        AttachToRegistry();
        if (m_DocumentCloned)
            RequestRefresh();
    }

    void OnPanelRepainted(PanelElement panel) => PanelRepainted?.Invoke(panel);

    void OnUndoRedoPerformed()
    {
        // A session whose document is not shown has nothing to re-clone; it re-clones when it is shown again.
        if (!m_DocumentCloned)
            return;

        if (EditedVisualTreeAsset != null)
        {
            foreach (var styleSheet in EditedVisualTreeAsset.GetAllReferencedStyleSheets())
            {
                styleSheet.RequestRebuild(StyleSheet.RebuildOptions.Synchronous);
            }
        }
        if (StandaloneStyleSheet != null)
            StandaloneStyleSheet.RequestRebuild(StyleSheet.RebuildOptions.Synchronous);
        UIElementsEditorUtility.ClearStyleCacheAfterUndoIfTracked(default);
        ReloadAssets();
        CloneTree();
    }

    void CollectRoots(List<VisualTreeAsset> roots)
    {
        if (m_Document.RootVisualTreeAsset != null)
            roots.Add(m_Document.RootVisualTreeAsset);
        if (EditedVisualTreeAsset != null)
            roots.Add(EditedVisualTreeAsset);
    }

    void CollectStyleSheetRoots(List<StyleSheet> roots)
    {
        if (StandaloneStyleSheet != null)
            roots.Add(StandaloneStyleSheet);
    }

    // The edited document and the stylesheets it references are writable; everything else in the hierarchy
    // (enclosing/nested templates, imported sheets) is tracked read-only. A style sheet edited on its own is the
    // only writable asset.
    UIAssetAccess ResolveAccess(UnityEngine.Object asset)
    {
        if (StandaloneStyleSheet != null)
            return ReferenceEquals(asset, StandaloneStyleSheet) ? UIAssetAccess.ReadWrite : UIAssetAccess.ReadOnly;

        var edited = EditedVisualTreeAsset;
        if (ReferenceEquals(asset, edited))
            return UIAssetAccess.ReadWrite;

        if (asset is StyleSheet styleSheet && edited != null)
        {
            using var _ = ListPool<StyleSheet>.Get(out var sheets);
            edited.GetAllReferencedStyleSheets(sheets);
            if (sheets.Contains(styleSheet))
                return UIAssetAccess.ReadWrite;
        }
        return UIAssetAccess.ReadOnly;
    }

    void MarkTrackedAssetsClean()
    {
        // The preview document is another session's to settle.
        var registry = UIAssetRegistry.instance;
        if (StandaloneStyleSheet != null)
        {
            registry.MarkClean(StandaloneStyleSheet);
            return;
        }

        var vta = EditedVisualTreeAsset;
        if (vta == null)
            return;

        registry.MarkClean(vta);
        using var _ = ListPool<StyleSheet>.Get(out var styleSheets);
        UIAssetRegistry.CollectDocumentStyleSheets(vta, styleSheets);
        foreach (var styleSheet in styleSheets)
            if (styleSheet != null)
                registry.MarkClean(styleSheet);
    }

    void ClearUndoForEditedAssets()
    {
        if (StandaloneStyleSheet != null)
        {
            Undo.ClearUndo(StandaloneStyleSheet);
            return;
        }

        var vta = EditedVisualTreeAsset;
        if (vta == null)
            return;

        Undo.ClearUndo(vta);
        if (vta.inlineSheet != null)
            Undo.ClearUndo(vta.inlineSheet);

        foreach (var styleSheet in vta.GetAllReferencedStyleSheets())
            Undo.ClearUndo(styleSheet);
    }

    void OnStylingChanged(in CommandContext context)
    {
        m_FrameUpdateRequested = true;
        if (m_InsideGroup)
            return;

        ProcessDelayedCommands();
    }

    void OnGroupBegan(string undoGroup)
    {
        m_InsideGroup = true;
    }

    void OnGroupEnded(in GroupEndedContext context)
    {
        m_InsideGroup = false;
        ProcessDelayedCommands();
    }

    void ProcessDelayedCommands()
    {
        if (m_FrameUpdateRequested)
        {
            m_PanelElement?.FrameUpdate();
            m_FrameUpdateRequested = false;
        }

        if (m_ExternalChanges)
        {
            ProcessBuilderChanges();
        }
    }

    void CheckForBuilderChanges(in CommandContext context)
    {
        if (context.Status != CommandExecutionStatus.Success || context.Source != CommandSources.Builder)
            return;

        m_ExternalChanges = true;
        if (m_InsideGroup)
            return;

        ProcessBuilderChanges();
    }

    void ProcessBuilderChanges()
    {
        // Here, we take for granted that the UI Builder made a change. Go nuclear.
        EditorApplication.delayCall += DoProcessBuilderChanges;
    }

    void DoProcessBuilderChanges()
    {
        if (!m_ExternalChanges)
            return;
        RequestRefresh();
        m_ExternalChanges = false;
    }

    void OnBuilderSave(in CommandContext context)
    {
        if (context.Status != CommandExecutionStatus.Success || context.Source != CommandSources.Builder)
            return;

        if (context.Command is not PostSaveCommand postSaveCommand)
            return;

        // A save that failed to write leaves the document unsaved, so adopting it as clean would drop the "*"
        // while the edits are still only in memory.
        if (!postSaveCommand.Succeeded)
            return;

        // Ignore saves of documents we are not editing so a save of an unrelated document never discards our
        // own in-memory edits.
        if (postSaveCommand.Context.EditedVisualTreeAsset != EditedVisualTreeAsset)
            return;

        m_ExternalSave = true;
        // Defer so the reload runs after the Builder's save has fully settled.
        EditorApplication.delayCall += DoProcessBuilderSave;
    }

    void DoProcessBuilderSave()
    {
        if (!m_ExternalSave)
            return;
        m_ExternalSave = false;

        AdoptReloadedContext(markClean: true);
    }

    void OnRegistryAssetReloaded(UnityEngine.Object asset)
    {
        if (m_SelfReloading || m_PanelElement == null || !IsPartOfEditedDocument(asset))
            return;

        AdoptReloadedContext(markClean: false);
    }

    bool IsPartOfEditedDocument(UnityEngine.Object asset)
    {
        if (asset == null)
            return false;

        // Fast path: the common reimport reuses the managed instance, so a reference match settles it.
        if (ReferenceEquals(asset, EditedVisualTreeAsset) || ReferenceEquals(asset, m_Document.RootVisualTreeAsset))
            return true;

        if (StandaloneStyleSheet != null && ReferenceEquals(asset, StandaloneStyleSheet))
            return true;

        var path = AssetDatabase.GetAssetPath(asset);
        if (string.IsNullOrEmpty(path))
            return false;

        if (StandaloneStyleSheet != null && AssetDatabase.GetAssetPath(StandaloneStyleSheet) == path)
            return true;

        var root = (VisualTreeAsset)GlobalObjectId.GlobalObjectIdentifierToObjectSlow(m_RootAssetId);
        if (root != null && AssetDatabase.GetAssetPath(root) == path)
            return true;

        var edited = EditedVisualTreeAsset;
        if (edited == null)
            return false;
        if (AssetDatabase.GetAssetPath(edited) == path)
            return true;

        using var _ = ListPool<StyleSheet>.Get(out var sheets);
        edited.GetAllReferencedStyleSheets(sheets);
        foreach (var sheet in sheets)
            if (sheet != null && AssetDatabase.GetAssetPath(sheet) == path)
                return true;
        return false;
    }

    void AdoptReloadedContext(bool markClean)
    {
        if (m_PanelElement == null)
            return;

        var freshRoot = (VisualTreeAsset)GlobalObjectId.GlobalObjectIdentifierToObjectSlow(m_RootAssetId);
        if (freshRoot && (m_Document.SubDocumentPath == null || m_Document.SubDocumentPath.Length == 0))
            Document = new VisualTreeAssetEditingContext(freshRoot, m_Document.PanelSettings);
        else
            ReloadAssets();

        if (markClean)
            MarkTrackedAssetsClean();
        CloneTree();
        ClearUndoForEditedAssets();
    }
}
