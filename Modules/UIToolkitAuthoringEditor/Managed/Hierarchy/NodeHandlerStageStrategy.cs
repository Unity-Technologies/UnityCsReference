// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Hierarchy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

abstract class NodeHandlerStageStrategy(Stage stage)
{
    protected Stage Stage => stage;

    /// <summary>
    /// The handler this strategy was wired to, or <see langword="null"/> outside the
    /// <see cref="Initialize"/>/<see cref="Dispose"/> window.
    /// </summary>
    protected VisualElementNodeHandler Handler { get; private set; }

    /// <summary>
    /// Raised when the stage re-clones the document it edits. Every live element of that document is replaced,
    /// so anything resolved from the previous tree has to be resolved again.
    /// </summary>
    public event Action DocumentCloned;

    protected void RaiseDocumentCloned() => DocumentCloned?.Invoke();

    /// <summary>
    /// The context the handler's hierarchy belongs to: its selection is the one the handler writes, and its
    /// messages are the ones the handler exchanges highlights and framing requests on.
    /// </summary>
    public virtual AuthoringContext Context => AuthoringContext.Global;

    /// <summary>
    /// Subscribes <paramref name="handler"/> to the panels the stage shows. Called by the handler this strategy
    /// was created for while it initializes, and undone by <see cref="Dispose"/> when that handler is disposed.
    /// </summary>
    /// <param name="handler">The handler being wired to the stage.</param>
    public virtual void Initialize(VisualElementNodeHandler handler)
    {
        Handler = handler;
    }

    /// <summary>Undoes <see cref="Initialize"/>.</summary>
    public virtual void Dispose()
    {
        Handler = null;
    }

    public bool IsFullyEditable(VisualElement element) => GetEditFlags(element).IsFullyEditable();

    public virtual bool IsReadOnly => true;

    public virtual VisualElementEditFlags GetEditFlags(VisualElement element) => VisualElementEditFlags.None;

    /// <summary>
    /// Whether <paramref name="element"/> is panel plumbing rather than authored content. Such an element gets
    /// no node of its own, but its children do.
    /// </summary>
    public virtual bool IsPanelRoot(VisualElement element) => false;

    /// <summary>
    /// Whether <paramref name="element"/> is a root element of the document the stage edits. Such an element
    /// has no parent element to hang from, so its node is a child of the hierarchy root.
    /// </summary>
    public virtual bool IsEditedDocumentRootElement(VisualElement element) => false;

    /// <summary>
    /// The GameObject hosting the document <paramref name="element"/> is the root of, or <see langword="null"/>
    /// when <paramref name="element"/> is not a document root. A stage showing several documents at once
    /// anchors each of them under the GameObject that renders it.
    /// </summary>
    public virtual GameObject GetDocumentHostGameObject(VisualElement element) => null;

    /// <summary>
    /// The document whose row <paramref name="element"/> is, when the stage shows a document as a node of its own
    /// and <paramref name="element"/> is the one standing for it; <see langword="null"/> otherwise. Scene documents
    /// are not asked: their root element already names its document.
    /// </summary>
    public virtual VisualTreeAsset GetDocumentNodeAsset(VisualElement element) => null;

    /// <summary>
    /// The object identifying the document scope <paramref name="element"/> belongs to. Its
    /// <see cref="GlobalObjectId"/>, together with <see cref="GetScopeKeySuffix"/>, forms the scope half of a
    /// node's UID, so it must resolve to the same object across domain reloads.
    /// </summary>
    public virtual UnityEngine.Object GetScopeObject(VisualElement element) => null;

    /// <summary>
    /// Distinguishes two scopes that share the same <see cref="GetScopeObject"/>, or <see langword="null"/>
    /// when the object alone identifies the scope.
    /// </summary>
    public virtual string GetScopeKeySuffix() => null;

    /// <summary>
    /// Whether the navigation arrow is only offered inside the document being edited. A stage editing a single
    /// document shows the rest of the tree as read-only context, which cannot be navigated into.
    /// </summary>
    public virtual bool LimitsNavigationToEditedDocument => false;

    /// <summary>
    /// Whether the branch leading to the document root has to be expanded when the stage opens. Editing a
    /// sub-document in context opens the hierarchy on it, inside the collapsed parent document.
    /// </summary>
    public virtual bool ExpandsDocumentRootWhenOpened => false;

    /// <summary>
    /// Resolves the live root of the document being edited, reporting when the stage can no longer be
    /// recreated. Unlike <see cref="ResolveDocumentRoot"/> this is only called when the document is (re-)cloned.
    /// </summary>
    public virtual VisualElement ValidateDocumentRoot() => ResolveDocumentRoot(null);

    /// <summary>
    /// Appends the stage's context menu for <paramref name="element"/>.
    /// </summary>
    public virtual void PopulateContextMenu(HierarchyView view, in HierarchyNode node, VisualElement element,
        DropdownMenu menu, VisualElementNodeHandler handler)
    {
    }

    /// <summary>
    /// Appends the stage's context menu for a right-click that missed every row.
    /// </summary>
    public virtual void PopulateStageContextMenu(HierarchyView view, DropdownMenu menu, VisualElementNodeHandler handler)
    {
    }

    /// <summary>
    /// Appends the stage's entries to the Hierarchy toolbar's create menu.
    /// </summary>
    public virtual void PopulateCreateMenu(DropdownMenu menu)
    {
    }

    /// <summary>
    /// Resolves where a paste operation should land in <see cref="Context"/>.
    /// </summary>
    /// <param name="selected">The first selected element, or <see langword="null"/> when nothing is selected.</param>
    /// <param name="parentElement">The live element the pasted content should be added to.</param>
    /// <param name="parentAsset">The asset the pasted assets should be reparented onto.</param>
    /// <returns><see langword="true"/> when a paste target was resolved; <see langword="false"/> otherwise.</returns>
    public virtual bool TryResolvePasteParent(VisualElement selected, out VisualElement parentElement, out VisualElementAsset parentAsset)
        => VisualElementEditingUtility.TryResolvePasteParent(Context, selected, out parentElement, out parentAsset);

    /// <summary>
    /// Whether the hierarchy root can receive visual elements. Only a stage whose hierarchy root <em>is</em> the
    /// document can; elsewhere (the Main Stage root holds GameObjects) it cannot.
    /// </summary>
    public virtual bool AcceptRootAsParent => false;

    /// <summary>
    /// Whether a UI Library element dropped on empty Hierarchy space should create a new panel to hold it. Only
    /// a stage whose root holds GameObjects rather than a document has nowhere else for such a drop to land.
    /// </summary>
    public virtual bool CreatesPanelForRootLibraryDrop => false;

    /// <summary>
    /// The live root element of the document <paramref name="element"/> belongs to. Passing
    /// <see langword="null"/> asks for the ambient document, which only a stage editing a single document
    /// has; <see langword="null"/> then means there is no document to drop into.
    /// </summary>
    public virtual VisualElement ResolveDocumentRoot(VisualElement element) => null;

    /// <summary>
    /// The <see cref="VisualTreeAsset"/> cloned into the root returned by <see cref="ResolveDocumentRoot"/>.
    /// </summary>
    public virtual VisualTreeAsset ResolveDocumentAsset(VisualElement element) => null;

    /// <summary>
    /// Whether <paramref name="element"/> can be the target of a drop. Document roots are only valid targets
    /// in stages that show them as nodes, hence the hook.
    /// </summary>
    public virtual bool CanHostDrop(VisualElement element) => IsFullyEditable(element);

    /// <summary>
    /// Resolves the <see cref="VisualElementAsset"/> a drop on <paramref name="element"/> must mutate: its own
    /// asset, or — for a document root, which has none — the root asset of the document it hosts.
    /// </summary>
    public bool TryResolveDropAsset(VisualElement element, out VisualElementAsset asset)
    {
        if (element != null && element == ResolveDocumentRoot(element))
        {
            var document = ResolveDocumentAsset(element);
            asset = document != null ? document.visualTree : null;
            return asset != null;
        }

        asset = element?.visualElementAsset;
        return asset != null;
    }

    /// <summary>
    /// Re-clones the live tree of the document holding <paramref name="element"/>, for drops that only mutate
    /// authoring assets (template and library-item drops) and therefore have nothing to dual-write.
    /// </summary>
    public virtual void RequestRefresh(VisualElement element)
    {
    }

}

sealed class UnsupportedNodeHandlerStrategy(Stage stage) : NodeHandlerStageStrategy(stage);

sealed class MainStageNodeHandlerStrategy(Stage stage) : NodeHandlerStageStrategy(stage)
{
    // Scene documents come and go with the panels of the scene, which the selection registry tracks for us.
    public override void Initialize(VisualElementNodeHandler handler)
    {
        base.Initialize(handler);

        // Observe the asset registry without forcing the singleton alive on every reload; subscribe
        // as soon as it exists.
        var assetRegistry = UIAssetRegistry.LiveInstance;
        if (assetRegistry != null)
            assetRegistry.AssetDirtyStateChanged += OnAssetDirtyStateChanged;
        else
            UIAssetRegistry.LiveInstanceCreated += OnAssetRegistryCreated;

        var registry = VisualElementSelectionRegistry.Instance;
        if (registry == null)
            return;

        registry.PanelTracked += handler.RegisterPanel;
        registry.PanelUntracked += handler.UnregisterPanel;

        // Register already tracked panels in case the registry was already initialized. That way, we won't
        // process the same panel more than once.
        var trackedPanels = registry.TrackedScenePanels;
        for (var i = 0; i < trackedPanels.Count; ++i)
            handler.RegisterPanel(trackedPanels[i]);
        registry.EnsureInitialized();
    }

    public override void Dispose()
    {
        UIAssetRegistry.LiveInstanceCreated -= OnAssetRegistryCreated;
        var assetRegistry = UIAssetRegistry.LiveInstance;
        if (assetRegistry != null)
            assetRegistry.AssetDirtyStateChanged -= OnAssetDirtyStateChanged;

        var registry = VisualElementSelectionRegistry.Instance;
        if (registry != null && Handler != null)
        {
            registry.PanelTracked -= Handler.RegisterPanel;
            registry.PanelUntracked -= Handler.UnregisterPanel;
        }

        base.Dispose();
    }

    void OnAssetRegistryCreated(UIAssetRegistry registry)
    {
        UIAssetRegistry.LiveInstanceCreated -= OnAssetRegistryCreated;
        registry.AssetDirtyStateChanged += OnAssetDirtyStateChanged;
    }

    // The `Foo.uxml` rows carry the unsaved-changes marker — both the document rows and the template path
    // shown on every instance row — so they have to be redrawn when a document is dirtied or saved.
    void OnAssetDirtyStateChanged(UnityEngine.Object asset)
    {
        if (asset is VisualTreeAsset)
            Handler?.MarkHierarchyDirty();
    }

    public override bool IsReadOnly => false;

    public override VisualElementEditFlags GetEditFlags(VisualElement element)
        => UIToolkitStageUtility.GetMainStageEditFlags(element);

    public override bool IsPanelRoot(VisualElement element) => element is PanelRootElement;

    // Each scene document hangs under the GameObject of the panel component that renders it.
    public override GameObject GetDocumentHostGameObject(VisualElement element)
    {
        var panelComponent = (element as IPanelComponentRootElement)?.panelComponent;
        return panelComponent.IsAlive() ? panelComponent.gameObject : null;
    }

    // Scene documents are scoped by their owning panel component: multiple documents in one panel (or two
    // instances of the same UXML) must not collide on identical in-document paths.
    public override UnityEngine.Object GetScopeObject(VisualElement element)
    {
        var rootElement = element as IPanelComponentRootElement
                          ?? element?.GetFirstAncestorOfType<IPanelComponentRootElement>();
        return rootElement?.panelComponent.AliveOrNull() as UnityEngine.Object;
    }

    public override void PopulateContextMenu(HierarchyView view, in HierarchyNode node, VisualElement element,
        DropdownMenu menu, VisualElementNodeHandler handler)
        => StageContextMenuUtility.PopulateMainStageMenu(view, in node, element, menu, handler);

    // Every scene document is its own drop scope, so the document is resolved from the element rather than
    // from the stage. The hierarchy root belongs to the GameObjects, never to a document.
    public override VisualElement ResolveDocumentRoot(VisualElement element)
        => element?.GetFirstOfType<IPanelComponentRootElement>() as VisualElement;

    public override VisualTreeAsset ResolveDocumentAsset(VisualElement element)
    {
        var panelComponent = element?.GetFirstOfType<IPanelComponentRootElement>()?.panelComponent;
        return panelComponent.IsAlive() ? panelComponent.visualTreeAsset : null;
    }

    // A document root has no VisualElementAsset of its own, but the document behind it can still receive
    // children and style sheets, so the `Foo.uxml` row is a valid drop target.
    public override bool CanHostDrop(VisualElement element)
        => IsFullyEditable(element) || (element is IPanelComponentRootElement && ResolveDocumentAsset(element) != null);

    // Scene panels do not reflect in-memory VisualTreeAsset edits incrementally; only a full re-clone does.
    public override void RequestRefresh(VisualElement element)
        => UIAssetRegistrySceneTracking.ReloadDocumentOf(element);

    // The Main Stage root holds GameObjects, so an element dropped on empty space has no document to land in
    // and gets a new panel of its own.
    public override bool CreatesPanelForRootLibraryDrop => true;
}

/// <summary>
/// The strategy for a hierarchy that shows one <see cref="UIDocumentSession"/>: the UI Stage's, or that of a view
/// editing a document outside any stage.
/// </summary>
/// <remarks>
/// With <paramref name="showsDocumentNode"/>, the document the panel holds gets a node of its own, parent of its root
/// elements: the panel root element stands for it. Otherwise its root elements are the hierarchy roots, under the
/// stage header that names the document.
/// </remarks>
sealed class UIDocumentSessionStrategy(UIDocumentSession session, Stage stage = null, bool showsDocumentNode = false)
    : NodeHandlerStageStrategy(stage)
{
    public UIDocumentSession Session => session;

    public override AuthoringContext Context => session.Owner;

    // The session owns the single authoring panel its document is cloned into.
    public override void Initialize(VisualElementNodeHandler handler)
    {
        base.Initialize(handler);

        session.DocumentCloned += OnDocumentCloned;

        // The document row carries the unsaved-changes marker.
        if (showsDocumentNode)
            UIAssetRegistry.instance.AssetDirtyStateChanged += OnAssetDirtyStateChanged;

        AttachPanel();

        // A handler only needs the document to be there to populate its nodes; the session that clones it on its
        // way open would pay for a second clone of the same tree.
        session.EnsureDocumentShown();
    }

    public override void Dispose()
    {
        session.DocumentCloned -= OnDocumentCloned;
        if (UIAssetRegistry.LiveInstance is { } registry)
            registry.AssetDirtyStateChanged -= OnAssetDirtyStateChanged;

        base.Dispose();
    }

    // A domain reload recreates the session's panel and the hierarchy in an order neither controls, so the panel
    // can first appear with a clone. Attaching is idempotent.
    void OnDocumentCloned(UIDocumentSession clonedSession)
    {
        AttachPanel();
        RaiseDocumentCloned();
    }

    void OnAssetDirtyStateChanged(UnityEngine.Object asset)
    {
        if (asset is VisualTreeAsset)
            Handler?.MarkHierarchyDirty();
    }

    void AttachPanel()
    {
        var panel = session.Panel;
        if (Handler == null || panel == null)
            return;

        // The selection registry has to see a panel's changes before the hierarchy does: it computes the
        // old-instance to new-instance remaps the hierarchy consumes to keep its node map stable.
        session.Track();
        Handler.RegisterPanel(panel);
    }

    public override bool IsReadOnly => false;

    public override VisualElementEditFlags GetEditFlags(VisualElement element)
     => session.GetElementEditFlags(element);

    public override bool IsPanelRoot(VisualElement element)
        => element is PanelRootElement || (!showsDocumentNode && element is PanelElement.PanelElementRootVisualElement);

    // The session edits one document, cloned right under the panel: the node standing for it is the hierarchy root,
    // or else its root elements are.
    public override bool IsEditedDocumentRootElement(VisualElement element)
        => showsDocumentNode
            ? element is PanelElement.PanelElementRootVisualElement
            : element.parent is PanelRootElement or PanelElement.PanelElementRootVisualElement;

    // The document cloned into the panel: the edited one, or the one it is edited in the context of.
    public override VisualTreeAsset GetDocumentNodeAsset(VisualElement element)
    {
        if (!showsDocumentNode || session.IsDisposed || element is not PanelElement.PanelElementRootVisualElement)
            return null;

        return session.Document.SubDocumentOptions == SubDocumentOptions.Isolation
            ? session.EditedVisualTreeAsset
            : session.Document.RootVisualTreeAsset;
    }

    // The session edits a single asset; scope by that asset (stable across reloads) plus the in-context
    // sub-document chain so identical elements edited in different sub-document contexts of the same asset do
    // not collide.
    public override UnityEngine.Object GetScopeObject(VisualElement element)
        => session.IsDisposed ? null : session.Document.RootVisualTreeAsset;

    public override string GetScopeKeySuffix()
    {
        var subDocumentPath = session.Document.SubDocumentPath;
        if (subDocumentPath is not { Length: > 0 })
            return null;

        using var _ = StringBuilderPool.Get(out var builder);
        for (var i = 0; i < subDocumentPath.Length; ++i)
        {
            builder.Append(':');
            builder.Append(subDocumentPath[i]?.id ?? 0);
        }

        return builder.ToString();
    }

    public override bool LimitsNavigationToEditedDocument => true;

    public override bool ExpandsDocumentRootWhenOpened
        => session.Panel != null && (showsDocumentNode || session.Document.SubDocumentOptions == SubDocumentOptions.InContext);

    public override VisualElement ValidateDocumentRoot()
    {
        var localRoot = session.ResolveLocalRoot();

        // We couldn't find the correct local root, so we go out of the current staging mode.
        if (localRoot == null && session.Panel != null)
        {
            Debug.LogWarning("Current stage could not be recreated correctly.");

            // Wrong: this runs from the handler's Initialize, and switching stages there re-enters the stage switch that is creating the handler; it must move out of Initialize
            if (Stage is VisualElementEditingStage)
                StageUtility.GoBackToPreviousStage();
        }

        return localRoot;
    }

    // The document row acts on the document, like a click on empty space does.
    public override void PopulateContextMenu(HierarchyView view, in HierarchyNode node, VisualElement element,
        DropdownMenu menu, VisualElementNodeHandler handler)
    {
        if (GetDocumentNodeAsset(element) != null)
            StageContextMenuUtility.PopulateStageMenu(view, menu, handler);
        else
            StageContextMenuUtility.PopulateMenu(view, in node, element, menu, handler);
    }

    public override void PopulateStageContextMenu(HierarchyView view, DropdownMenu menu, VisualElementNodeHandler handler)
        => StageContextMenuUtility.PopulateStageMenu(view, menu, handler);

    public override void PopulateCreateMenu(DropdownMenu menu)
        => StageContextMenuUtility.PopulateElementOperations(menu, Context);

    public override bool TryResolvePasteParent(VisualElement selected, out VisualElement parentElement,
        out VisualElementAsset parentAsset)
        => VisualElementEditingUtility.TryResolvePasteParent(session, selected, out parentElement, out parentAsset);

    // Root-level elements are either all editable or all non-editable: when editing a sub-document in context
    // the hierarchy root belongs to the parent document, which is read-only. A drop next to the document row
    // lands in the document.
    public override bool AcceptRootAsParent => session.Document.SubDocumentOptions != SubDocumentOptions.InContext;

    // The session edits a single document, so every element resolves to the same root.
    public override VisualElement ResolveDocumentRoot(VisualElement element) => session.ResolveLocalRoot();

    public override VisualTreeAsset ResolveDocumentAsset(VisualElement element) => session.EditedVisualTreeAsset;

    // The document row has no asset of its own, but receives what is dropped on it as root elements.
    public override bool CanHostDrop(VisualElement element)
        => base.CanHostDrop(element) ||
            (GetDocumentNodeAsset(element) != null && !session.IsDocumentReadOnly && element == ResolveDocumentRoot(element));
}
