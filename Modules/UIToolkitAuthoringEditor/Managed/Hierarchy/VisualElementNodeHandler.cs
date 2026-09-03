// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitAuthoringFramework not yet converted
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using Unity.Properties;
using Unity.Scripting.LifecycleManagement;
using Unity.UIToolkit.Editor.Utilities;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal partial class VisualElementNodeHandler :
    HierarchyNodeTypeHandler,
    IVisualElementChangeProcessor,
    IHierarchySearchPropositionProvider,
    IHierarchyExtendCreateMenu
{
    public const string NodeTypeName = "VisualElementNodeHandler";

    [OnCodeLoaded, UsedImplicitly]
    private static void RegisterStageHandlers()
    {
        HierarchyWindow.RegisterNodeTypeHandler<VisualElementNodeHandler>();
    }

    [OnCodeUnloading, UsedImplicitly]
    private static void UnregisterHierarchyHandlers()
    {
        HierarchyWindow.UnregisterNodeTypeHandler<VisualElementNodeHandler>();
    }

    protected const string VisualElementDisabledUssClass = "unity-disabled";

    public static Regex elementNameRegex { get; } = new (@"^[a-zA-Z0-9\-_]+$", RegexOptions.Compiled);
    public const string DraggedVisualElementKey = "VisualElementHandler__DraggedVisualElements";

    // Used for tests
    internal MappingsAccess GetMappings() => new(this);

    internal readonly struct MappingsAccess
    {
        private readonly VisualElementNodeHandler m_Handler;

        internal MappingsAccess(VisualElementNodeHandler handler)
        {
            m_Handler = handler;
        }

        public bool TryGetElement(in HierarchyNode node, out VisualElement element)
            => m_Handler.TryGetElementFromNode(in node, out element);

        public bool TryGetNode(VisualElement element, out HierarchyNode node)
            => m_Handler.TryGetNodeFromElement(element, out node);

        public bool TryGetScopeKey(VisualElement element, out string scopeKey)
            => m_Handler.TryGetScopeKey(element, out scopeKey);
        public bool TryComputeStableHash(VisualElement element, out Hash128 hash)
            => m_Handler.TryComputeStableHash(element, out hash);
    }

    public const string HierarchyItemClassName = "ui-hierarchy-item";
    public const string HierarchyItemElementNameClassName = HierarchyItemClassName + "__element-name";
    public const string HierarchyItemElementTypeNameClassName = HierarchyItemClassName + "__element-type-name";
    public const string HierarchyItemUssClassName = HierarchyItemClassName + "__element-uss-class";
    public const string HierarchyItemTemplatePath = HierarchyItemClassName + "__template-path";
    public const string HierarchyItemDisabledClassName = HierarchyItemClassName + "__disabled";

    private const string k_StyleSheetPath = "UIToolkitAuthoring/Hierarchy/VisualElementNodeTypeHandler.uss";

    static readonly ManipulatorActivationFilter k_StageAltActivationFilter = new() { button = MouseButton.LeftMouse, modifiers = EventModifiers.Alt };

    private class Mappings
    {
        private readonly Dictionary<HierarchyNode, VisualElement> m_Map = new();
        private readonly Dictionary<VisualElement, HierarchyNode> m_ReversedMap = new();
        private readonly Dictionary<HierarchyNode, EntityId> m_SelectionHandles = new();
        private readonly Dictionary<EntityId, HierarchyNode> m_ReversedSelectionHandles = new();

        public int Count => m_Map.Count;

        public Dictionary<VisualElement, HierarchyNode>.KeyCollection MappedElements => m_ReversedMap.Keys;

        public bool TryAdd(in HierarchyNode node, VisualElement element, in EntityId selectionHandle)
        {
            if (node == HierarchyNode.Null || element == null)
                return false;

            return m_Map.TryAdd(node, element) &&
                   m_ReversedMap.TryAdd(element, node) &&
                   m_SelectionHandles.TryAdd(node, selectionHandle) &&
                   m_ReversedSelectionHandles.TryAdd(selectionHandle, node);
        }

        public bool TryGetValue(in HierarchyNode node, out VisualElement element)
        {
            if (node != HierarchyNode.Null)
                return m_Map.TryGetValue(node, out element);

            element = null;
            return false;
        }

        public bool TryGetSelectionHandle(in HierarchyNode node, out EntityId selectionHandle)
        {
            if (node != HierarchyNode.Null)
                return m_SelectionHandles.TryGetValue(node, out selectionHandle);

            selectionHandle = EntityId.None;
            return false;
        }

        public bool TryGetNodeFromSelectionHandle(in EntityId selectionHandle, out HierarchyNode node)
        {
            if (selectionHandle != EntityId.None)
                return m_ReversedSelectionHandles.TryGetValue(selectionHandle, out node);

            node = HierarchyNode.Null;
            return false;
        }

        public bool TryGetValue(VisualElement element, out HierarchyNode node)
        {
            if (element != null)
                return m_ReversedMap.TryGetValue(element, out node);

            node = HierarchyNode.Null;
            return false;
        }

        public bool TryRemove(in HierarchyNode node)
        {
            if (node == HierarchyNode.Null)
                return false;

            return m_Map.Remove(node, out var element) &&
                   m_ReversedMap.Remove(element) &&
                   m_SelectionHandles.Remove(node, out var selectionHandle) &&
                   m_ReversedSelectionHandles.Remove(selectionHandle);
        }

        public bool TryRemove(VisualElement element)
        {
            if (element != null)
                return m_ReversedMap.Remove(element, out var node)
                       && m_Map.Remove(node) &&
                       m_SelectionHandles.Remove(node, out var selectionHandle) &&
                       m_ReversedSelectionHandles.Remove(selectionHandle);

            return false;
        }

        public bool RemoveSelection(VisualElement element)
        {
            if (m_ReversedMap.TryGetValue(element, out var node))
            {
                return m_SelectionHandles.Remove(node, out var selectionHandle) &&
                       m_ReversedSelectionHandles.Remove(selectionHandle);
            }

            return false;
        }

        public void Remap(List<VisualElementRemap> remappings)
        {
            foreach (var remap in remappings)
            {
                if (TryGetValue(remap.Previous, out var node))
                {
                    m_Map[node] = remap.Remapped;
                    m_ReversedMap[remap.Remapped] = node;
                    m_ReversedMap.Remove(remap.Previous);
                    // Intentionally not remapping selection, because it's based on the node.
                }
            }
        }
    }

    /// <summary>
    /// Indicates if and how a <see cref="HierarchyNode"/> should be created from a  <see cref="VisualElement"/>é
    /// </summary>
    protected enum NodeCreationType
    {
        /// <summary>
        /// Do not create a node for the current VisualElement or its children.
        /// </summary>
        DontCreate,

        /// <summary>
        /// Create a node for the current VisualElement and its children.
        /// </summary>
        Create,

        /// <summary>
        /// do not create a node for the current VisualElement, but create nodes for its children.
        /// </summary>
        CreateChildren,
    }

    private readonly Mappings m_Mappings = new();
    private readonly List<Panel> m_RegisteredPanels = new();

    // Panels whose initial tree walk (IVisualElementChangeProcessor.BeginProcessing) has run. Until
    // then, UpdateBegin forces the panel's authoring update to trigger it synchronously (see UpdateBegin).
    private readonly HashSet<Panel> m_BegunPanels = new();

    private readonly QueryEngine<VisualElement> m_QueryEngine;
    readonly HashSet<HierarchyNode> m_HighlightedNodes = new();
    VisualElement m_HoveredElement;

    NodeHandlerStageStrategy m_StageStrategy;

    // The live root of the document the stage edits, when it edits a single one. Resolved on initialization and
    // on every re-clone of that document.
    VisualElement m_DocumentRoot;
    bool m_ExpandDocumentRootOnNextUpdate;

    HierarchyGameObjectHandler m_GameObjectHandler;

    private StyleSheet m_StyleSheet;
    private StyleSheet m_ThemeStyleSheet;
    private ParsedQuery<VisualElement> m_ParsedQuery;

    private UIHierarchyDisplayOptions m_DisplayOptions;

    internal List<SelectionRequest> m_NodesToSelect;

    /// <summary>
    /// How many updates a pending request is given to resolve before what is left of it is dropped.
    /// </summary>
    /// <remarks>
    /// Counted in updates that could have resolved it rather than in elapsed time: the view model only posts an
    /// update when the hierarchy actually changed, so this is a budget of real chances, and an editor that is
    /// idle or busy elsewhere cannot spend it. The legitimate path needs a handful — the re-clone lands on the
    /// next editor tick — so this is headroom over that rather than a deadline anything normally approaches.
    /// </remarks>
    internal const int MaxSelectionRequestAttempts = 50;

    /// <summary>Updates the requests in <see cref="m_NodesToSelect"/> have failed to fully resolve on.</summary>
    int m_SelectionRequestAttempts;

    /// <summary>
    /// What to select once the documents a command changed have been cloned again.
    /// </summary>
    /// <remarks>
    /// An asset on its own does not name an element: a document instantiated twice clones every one of its
    /// assets twice, and both clones are equally backed by it. What tells them apart is the chain of template
    /// instances they hang from, and the panel component rendering the outermost document — the same identity
    /// <see cref="VisualElementSelectionRegistry"/> files its selection objects under.
    /// </remarks>
    internal readonly struct SelectionRequest
    {
        /// <summary>The asset the element to select is authored by.</summary>
        public readonly VisualElementAsset Asset;

        /// <summary>
        /// The in-memory authoring id path the element must have, or <see langword="null"/> when the caller had
        /// no live context to resolve one from and any clone of <see cref="Asset"/> will do.
        /// </summary>
        public readonly int[] InstancePath;

        /// <summary>
        /// The panel component whose document the element has to be in, or <see langword="null"/> for any. Only
        /// a scene document has one; the UI Stage shows a single document, so its path alone is unambiguous.
        /// </summary>
        public readonly IPanelComponent Scope;

        public SelectionRequest(VisualElementAsset asset, int[] instancePath = null, IPanelComponent scope = null)
        {
            Asset = asset;
            InstancePath = instancePath;
            Scope = scope;
        }

        public SelectionRequest ScopedTo(int[] instancePath, IPanelComponent scope)
            => new(Asset, instancePath, scope);
    }

    protected internal NodeHandlerStageStrategy StageStrategy => m_StageStrategy;

    static NodeHandlerStageStrategy CreateStrategyFromStage()
    {
        var stage = StageUtility.GetCurrentStage();
        return stage switch
        {
            MainStage or PrefabStage => new MainStageNodeHandlerStrategy(stage),
            VisualElementEditingStage mainStage => new UIStageNodeHandlerStrategy(mainStage),
            _ => new UnsupportedNodeHandlerStrategy(stage)
        };
    }

    /// <summary>
    /// The stage integration this handler runs with. Defaults to the strategy matching the current stage;
    /// a handler that registers its own panels can supply an inert strategy instead.
    /// </summary>
    protected virtual NodeHandlerStageStrategy CreateStageStrategy() => CreateStrategyFromStage();

    private StyleSheet StyleSheet
    {
        get
        {
            if (!m_StyleSheet)
                m_StyleSheet = EditorGUIUtility.Load(k_StyleSheetPath) as StyleSheet;
            return m_StyleSheet;
        }
    }

    private StyleSheet ThemeStyleSheet
    {
        get
        {
            if (!m_ThemeStyleSheet)
            {
                var path = k_StyleSheetPath;
                var index = path.LastIndexOf(".uss", StringComparison.OrdinalIgnoreCase);
                if (EditorGUIUtility.isProSkin)
                    path = path.Insert(index, "Dark");
                else
                    path = path.Insert(index, "Light");
                m_ThemeStyleSheet = EditorGUIUtility.Load(path) as StyleSheet;
            }

            return m_ThemeStyleSheet;
        }
    }

    public VisualElement HoveredElement
    {
        get => m_HoveredElement;
        set
        {
            if (m_HoveredElement == value)
                return;
            m_HoveredElement = value;
            if (m_HoveredElement != null)
                HighlightUtility.RequestHighlights(m_HoveredElement, CommandSources.Hierarchy);
            else
                HighlightUtility.ClearHighlights();
        }
    }

    protected VisualElementNodeHandler()
    {
        m_QueryEngine = CreateQueryEngine();
        UIToolkitAuthoringSettings.DisplayOptionsChanged += OnDisplayOptionsChanged;
        m_DisplayOptions = UIToolkitAuthoringSettings.DisplayOptions;
    }

    protected override void Initialize()
    {
        UICommandQueue.RegisterHandler<HighlightCommand>(ProcessHighlightElementsCommand);
        UICommandQueue.RegisterHandler<AddClassCommand>(OnClassAddedToElement);
        UICommandQueue.RegisterHandler<CutElementsCommand>(OnElementsCut);
        UICommandQueue.RegisterHandler<CopyElementsCommand>(ClearCutFlagsOnSuccess);
        UICommandQueue.RegisterHandler<PasteElementsCommand>(ClearCutFlagsOnSuccess);
        UICommandQueue.RegisterHandler<ReparentElementsCommand>(ClearCutFlagsOnSuccess);
        UICommandQueue.RegisterHandler<DuplicateElementsCommand>(OnElementsDuplicated);

        UIToolkitAuthoringSettings.MainStageAuthoringChanged += OnMainStageAuthoringChanged;

        m_StageStrategy = CreateStageStrategy();
        m_StageStrategy.DocumentCloned += OnDocumentCloned;
        m_StageStrategy.Initialize(this);

        RefreshDocumentRoot();
        m_ExpandDocumentRootOnNextUpdate = m_StageStrategy.ExpandsDocumentRootWhenOpened;
    }

    /// <inheritdoc cref="HierarchyNodeTypeHandlerBase.Dispose(bool)"/>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (m_StageStrategy != null)
        {
            m_StageStrategy.DocumentCloned -= OnDocumentCloned;
            m_StageStrategy.Dispose();
            m_StageStrategy = null;
        }

        m_DocumentRoot = null;
        HoveredElement = null;
        UnregisterAllPanels();
        UIToolkitAuthoringSettings.DisplayOptionsChanged -= OnDisplayOptionsChanged;
        UICommandQueue.UnregisterHandler<HighlightCommand>(ProcessHighlightElementsCommand);
        UICommandQueue.UnregisterHandler<AddClassCommand>(OnClassAddedToElement);
        UICommandQueue.UnregisterHandler<CutElementsCommand>(OnElementsCut);
        UICommandQueue.UnregisterHandler<CopyElementsCommand>(ClearCutFlagsOnSuccess);
        UICommandQueue.UnregisterHandler<PasteElementsCommand>(ClearCutFlagsOnSuccess);
        UICommandQueue.UnregisterHandler<ReparentElementsCommand>(ClearCutFlagsOnSuccess);
        UICommandQueue.UnregisterHandler<DuplicateElementsCommand>(OnElementsDuplicated);
        UIToolkitAuthoringSettings.MainStageAuthoringChanged -= OnMainStageAuthoringChanged;
        m_GlobalObjectIdKeyCache.Clear();
    }

    /// <inheritdoc cref="HierarchyNodeTypeHandlerBase.GetNodeTypeName"/>
    public override string GetNodeTypeName()
    {
        return NodeTypeName;
    }

    void OnDocumentCloned() => RefreshDocumentRoot();

    void RefreshDocumentRoot() => m_DocumentRoot = m_StageStrategy?.ValidateDocumentRoot();

    void OnMainStageAuthoringChanged(bool enabled)
    {
        if (Hierarchy.IsCreated)
            CommandList.SetDirty();
    }

    /// <summary>
    /// Requests a redraw of the hierarchy rows, for changes that alter what a row displays without changing
    /// the tree itself.
    /// </summary>
    internal void MarkHierarchyDirty()
    {
        if (Hierarchy.IsCreated)
            CommandList.SetDirty();
    }

    /// <summary>
    /// The handler owning the GameObject nodes of this hierarchy, or <see langword="null"/> in a stage that has
    /// none. Resolved on demand: only a stage that anchors its documents to GameObjects needs it.
    /// </summary>
    HierarchyGameObjectHandler GameObjectHandler
        => m_GameObjectHandler ??= Hierarchy.GetNodeTypeHandler<HierarchyGameObjectHandler>();

    #region HierarchyNodeTypeHandler

    /// <inheritdoc cref="HierarchyNodeTypeHandlerBase.SearchBegin"/>
    protected sealed override void SearchBegin(HierarchySearchQueryDescriptor query)
    {
        m_ParsedQuery = m_QueryEngine.ParseQuery(query.ToString());
    }

    /// <inheritdoc cref="HierarchyNodeTypeHandlerBase.SearchMatch"/>
    protected sealed override bool SearchMatch(in HierarchyNode node)
    {
        return m_Mappings.TryGetValue(node, out var element) && m_ParsedQuery.Test(element);
    }

    /// <inheritdoc cref="HierarchyNodeTypeHandlerBase.SearchEnd"/>
    protected sealed override void SearchEnd()
    {
        m_ParsedQuery = null;
    }

    protected override void OnBindItem(HierarchyViewItem item)
    {
        if (m_Mappings.TryGetValue(item.Node, out var element))
        {
            if (m_HighlightedNodes.Contains(item.Node))
            {
                var highlightColor = EditorGUIUtility.isProSkin ? 0.1888f : 0.6980f;
                item.RowContainer.style.backgroundColor = new Color(highlightColor, highlightColor, highlightColor, 1.0f);
            }

            item.Icon.style.backgroundImage = GetIcon(element);
            Bind(item, element);
            BindNavigation(item, element);
            item.RowContainer.RegisterCallback<PointerEnterEvent, VisualElement>(OnStartHover, element);
            item.RowContainer.RegisterCallback<PointerLeaveEvent>(OnEndHover);
        }
        else
        {
            item.Icon.style.backgroundImage = null;
            item.EnableInClassList(HierarchyItemDisabledClassName, true);
        }
    }

    /// <inheritdoc cref="HierarchyView.UnbindViewItem"/>
    protected override void OnUnbindItem(HierarchyViewItem item)
    {
        item.RowContainer.style.backgroundColor = StyleKeyword.Null;

        if (m_Mappings.TryGetValue(item.Node, out var element))
        {
            Unbind(item, element);
            UnbindNavigation(item, element);
            item.RowContainer.UnregisterCallback<PointerEnterEvent, VisualElement>(OnStartHover);
            item.RowContainer.UnregisterCallback<PointerLeaveEvent>(OnEndHover);
        }
        else
        {
            item.Icon.style.backgroundImage = null;
            item.RemoveFromClassList(HierarchyItemElementNameClassName);
            item.RemoveFromClassList(HierarchyItemElementTypeNameClassName);
            item.RemoveFromClassList(HierarchyItemUssClassName);
            item.EnableInClassList(HierarchyItemDisabledClassName, false);
            item.LeftCustomContainer.Clear();
            item.parent.Q(className: HierarchyItemElementTypeNameClassName)?.RemoveFromHierarchy();
            item.Q(className: HierarchyItemTemplatePath)?.RemoveFromHierarchy();
            UnsetStageNodeNavigation(item);
        }
    }

    protected override void ViewModelPostUpdate(HierarchyViewModel viewModel)
    {
        base.ViewModelPostUpdate(viewModel);

        if (m_ExpandDocumentRootOnNextUpdate)
            ExpandDocumentRoot(viewModel);

        ApplyDeferredSelectionRequests(viewModel);
    }

    /// <summary>
    /// Expands the branch leading to the document being edited. Editing a sub-document in context opens the
    /// hierarchy on it, inside the otherwise collapsed parent document.
    /// </summary>
    void ExpandDocumentRoot(HierarchyViewModel viewModel)
    {
        if (m_DocumentRoot == null)
            return;

        if (TryGetNodeFromElement(m_DocumentRoot, out var node) && Hierarchy.Exists(node))
            viewModel.SetFlagsRecursive(node, HierarchyNodeFlags.Expanded, HierarchyTraversalDirection.Parents);

        m_ExpandDocumentRootOnNextUpdate = false;
    }

    /// <summary>
    /// Selects the elements the editing commands authored, once the panels have been re-cloned and the nodes for
    /// those elements exist.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A request outlives the update it was made in. The elements it names only come into being when the document
    /// they were authored into is cloned again, and in the Main Stage that re-clone is deferred to an editor tick
    /// (see <see cref="UIAssetRegistrySceneTracking"/>) — so the updates in between cannot resolve any of it yet.
    /// Consuming the request there would clear the selection and put nothing in its place, which is why nothing is
    /// cleared until the whole request resolves.
    /// </para>
    /// <para>
    /// All of it, rather than what happens to be resolvable now: the elements of one paste or duplicate can be
    /// cloned over more than one update, and applying them as they arrive would leave the selection holding only
    /// the last batch.
    /// </para>
    /// <para>
    /// That wait is bounded by <see cref="MaxSelectionRequestAttempts"/>. An element that is never cloned — a
    /// request naming a document no tracked panel renders, or an instance that does not exist — would otherwise
    /// hold its batch for the rest of the session, re-scanning every panel on every update and leaving the
    /// selection frozen wherever it stood. Once the budget is spent, whatever did resolve is selected and the
    /// rest is dropped: a partial selection beats none, and the next authoring action starts from a clean slate.
    /// </para>
    /// </remarks>
    void ApplyDeferredSelectionRequests(HierarchyViewModel viewModel)
    {
        var toSelects = GetDelayedSelectionRequests();
        if (toSelects is not { Count: > 0 })
            return;

        // An asset that has left its document — deleted, or undone — has no element left to wait for, and would
        // otherwise hold the rest of the request back forever.
        toSelects.RemoveAll(static request => request.Asset == null || request.Asset.visualTreeAsset == null);

        // Every request is resolved, rather than stopping at the first one that is not ready: on the update the
        // budget runs out, what has resolved so far is what gets selected, so it has to be in hand by then.
        using var _ = ListPool<HierarchyNode>.Get(out var nodes);
        var pending = false;
        foreach (var request in toSelects)
        {
            if (TryGetNodeFromElement(FindElementFor(in request), out var node))
                nodes.Add(node);
            else
                pending = true;
        }

        // Not cloned yet: leave the whole request pending and try again on a later update, for as long as a
        // later update can still be what brings it.
        if (pending && ++m_SelectionRequestAttempts < MaxSelectionRequestAttempts)
            return;

        toSelects.Clear();
        m_SelectionRequestAttempts = 0;

        if (nodes.Count == 0)
            return;

        viewModel.ClearFlags(HierarchyNodeFlags.Selected);
        var selectionSet = false;
        foreach (var node in nodes)
        {
            var parentNode = Hierarchy.GetParent(node);

            if (parentNode != Hierarchy.Root && parentNode != HierarchyNode.Null)
                viewModel.SetFlagsRecursive(parentNode, HierarchyNodeFlags.Expanded,
                    HierarchyTraversalDirection.Parents);
            viewModel.SetFlags(node, HierarchyNodeFlags.Selected);

            if (!TryGetSelectionObject(node, out var entityId))
                continue;

            if (selectionSet)
            {
                Selection.Add(entityId);
            }
            else
            {
                Selection.activeEntityId = entityId;
                selectionSet = true;
            }
        }
    }

    /// <summary>
    /// The live element <paramref name="request"/> names: the one cloned from its asset by the document that
    /// currently owns that asset, in one of the panels this handler tracks, and — when the request carries an
    /// instance identity — in that instance rather than any other clone of the same document.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Matched on the asset itself rather than on its id: ids are only unique within one
    /// <see cref="VisualTreeAsset"/>, so an id comparison can pick an unrelated element out of another
    /// document — exactly what happens right after an element is moved across two of them.
    /// </para>
    /// <para>
    /// The document the element was cloned from has to match the one the asset now belongs to. An edit only
    /// rewrites the authoring assets, and the live trees catch up on a later re-clone, so until then the element
    /// an asset was moved <em>away from</em> is still there and still backed by it. Matching that one hands back
    /// an element that is about to be released — its node and its selection object with it — which for a move
    /// across documents is not reclaimed by anything, so the selection would land on it just in time to be
    /// destroyed.
    /// </para>
    /// </remarks>
    VisualElement FindElementFor(in SelectionRequest request)
    {
        var asset = request.Asset;
        var document = asset.visualTreeAsset;

        using var candidatesHandle = ListPool<VisualElement>.Get(out var candidates);
        using var pathHandle = ListPool<int>.Get(out var path);

        for (var i = 0; i < m_RegisteredPanels.Count; ++i)
        {
            candidates.Clear();
            m_RegisteredPanels[i].visualTree.Query()
                .Where(e => e.visualElementAsset == asset && e.visualTreeAssetSource == document)
                .ToList(candidates);

            foreach (var candidate in candidates)
            {
                if (Matches(in request, candidate, path))
                    return candidate;
            }
        }

        return null;
    }

    /// <summary>
    /// Whether <paramref name="element"/> is the instance <paramref name="request"/> names, for an element
    /// already known to be authored by its asset.
    /// </summary>
    static bool Matches(in SelectionRequest request, VisualElement element, List<int> pathBuffer)
    {
        if ((Component)request.Scope
            && !ReferenceEquals(element.GetFirstOfType<IPanelComponentRootElement>()?.panelComponent, request.Scope))
            return false;

        if (request.InstancePath == null)
            return true;

        if (!VisualElementReferenceTools.TryGetInMemoryPath(element, pathBuffer)
            || pathBuffer.Count != request.InstancePath.Length)
            return false;

        for (var i = 0; i < pathBuffer.Count; ++i)
        {
            if (pathBuffer[i] != request.InstancePath[i])
                return false;
        }

        return true;
    }

    /// <inheritdoc cref="HierarchyNodeTypeHandler.OnBindView"/>
    protected override void OnBindView(HierarchyView view)
    {
        view.StyleContainer.styleSheets.Add(StyleSheet);
        view.StyleContainer.styleSheets.Add(ThemeStyleSheet);
        m_Views.Add(new WeakReference<HierarchyView>(view));
        RefreshCutFlagsForView(view);
    }

    /// <inheritdoc cref="HierarchyNodeTypeHandler.OnUnbindView"/>
    protected override void OnUnbindView(HierarchyView view)
    {
        for (var i = m_Views.Count - 1; i >= 0; i--)
        {
            if (m_Views[i].TryGetTarget(out var stored) && ReferenceEquals(stored, view))
            {
                m_Views.RemoveAt(i);
                break;
            }
        }
    }

    // UID serialization -------------------------------------------------------------------------
    //
    // Persists a stable, domain-reload-safe identity per node so the hierarchy view state
    // (expansion/selection) can be restored after the hierarchy is recreated. Per-node layout (k_UIDSize):
    //   [0, k_UIDHashSize)  Hash128 of (document scope key, in-memory authoring-id path)
    //   [k_UIDHashSize, ..)  EntityId of the node's selection object
    //
    // The hash is the durable identifier: the scope key (see TryGetScopeKey) and the in-memory path
    // (VisualElementAsset ids, which are serialized on the asset) are both stable across a domain
    // reload, so it re-resolves against the freshly rebuilt element tree. The EntityId is an exact
    // within-session fallback (its selection object survives the frequent clear+re-clone cycles but
    // not a domain reload) used for undo/redo and cases where the hash cannot be built.
    private const int k_UIDVersion = 1;
    private const int k_UIDHashSize = 16;      // sizeof(Hash128)
    private const int k_UIDEntityIdSize = 8;   // sizeof(EntityId)
    private const int k_UIDSize = k_UIDHashSize + k_UIDEntityIdSize;

    // Separates the authoring-id path of the nearest VEA-backed ancestor from the structural
    // child-index chain used for elements that have no VisualElementAsset (see TryGetStableUidPath).
    private const int k_NonAssetPathMarker = int.MinValue;

    private readonly List<int> m_UIDPathBuffer = new();
    private readonly Dictionary<UnityEngine.Object, string> m_GlobalObjectIdKeyCache = new();

    protected override void GetUIDInfo(out HierarchyUIDInfo info) => info = new HierarchyUIDInfo(k_UIDVersion, k_UIDSize);

    protected override void WriteUIDs(ReadOnlySpan<HierarchyNode> nodes, Span<byte> outUIDs)
    {
        for (var i = 0; i < nodes.Length; ++i)
        {
            var node = nodes[i];
            var slot = outUIDs.Slice(i * k_UIDSize, k_UIDSize);

            if (m_Mappings.TryGetValue(node, out var element) && TryComputeStableHash(element, out var hash))
                WriteBlittable(slot[..k_UIDHashSize], ref hash);

            if (m_Mappings.TryGetSelectionHandle(node, out var entityId))
                WriteBlittable(slot[k_UIDHashSize..], ref entityId);
        }
    }

    protected override void ReadUIDs(in HierarchyUIDInfo info, ReadOnlySpan<byte> uids, Span<HierarchyNode> outNodes)
    {
        if (info.Version != k_UIDVersion || info.Size != k_UIDSize)
            return;

        // Build a transient hash -> node index over the currently mapped elements. The window
        // restores state only after UpdateData() has repopulated the mappings, so everything that
        // can be resolved is present here; rebuilding per call is fine as restores are infrequent.
        using var _ = DictionaryPool<Hash128, HierarchyNode>.Get(out var nodesByHash);
        foreach (var element in m_Mappings.MappedElements)
        {
            if (TryComputeStableHash(element, out var hash) && m_Mappings.TryGetValue(element, out var node))
                nodesByHash[hash] = node;
        }

        for (var i = 0; i < outNodes.Length; ++i)
        {
            var slot = uids.Slice(i * k_UIDSize, k_UIDSize);

            // Preferred path: the hash re-resolves across domain reloads.
            var hash = ReadBlittable<Hash128>(slot[..k_UIDHashSize]);
            if (hash.isValid && nodesByHash.TryGetValue(hash, out var node))
            {
                outNodes[i] = node;
                continue;
            }

            // Within-session fallback: the selection object's EntityId is still valid (no reload).
            var entityId = ReadBlittable<EntityId>(slot[k_UIDHashSize..]);
            if (entityId != EntityId.None && m_Mappings.TryGetNodeFromSelectionHandle(entityId, out node))
                outNodes[i] = node;
        }
    }

    /// <summary>
    /// Computes the stable, domain-reload-safe hash for <paramref name="element"/> from its document
    /// scope key (see <see cref="TryGetScopeKey"/>) and its in-memory authoring-id path.
    /// </summary>
    private bool TryComputeStableHash(VisualElement element, out Hash128 hash)
    {
        hash = default;

        if (element == null || !TryGetStableUidPath(element, m_UIDPathBuffer))
            return false;

        if (!TryGetScopeKey(element, out var scopeKey) || string.IsNullOrEmpty(scopeKey))
            return false;

        var builder = new Hash128();
        builder.Append(scopeKey);
        for (var i = 0; i < m_UIDPathBuffer.Count; ++i)
            builder.Append(m_UIDPathBuffer[i]);

        hash = builder;
        return hash.isValid;
    }

    /// <summary>
    /// Builds the identity path used for hashing. Authored elements use the authoring-id path
    /// (<see cref="VisualElementReferenceTools.TryGetInMemoryPath"/>), which is stable across reloads and
    /// robust to edits. Elements with no <see cref="VisualElement.visualElementAsset"/> — control-internal
    /// elements such as Foldout/ScrollView/ListView children — have no authoring id, so we anchor to their
    /// nearest VEA-backed (or panel-component-root) ancestor and append the physical child-index chain,
    /// which the control rebuilds identically across a domain reload. A marker separates the two segments
    /// so an index chain can never collide with an authoring-id path.
    /// </summary>
    private bool TryGetStableUidPath(VisualElement element, List<int> pathBuffer)
    {
        if (VisualElementReferenceTools.TryGetInMemoryPath(element, pathBuffer))
            return true;

        using var _ = ListPool<int>.Get(out var indexChain);

        var node = element;
        var parent = node.hierarchy.parent;
        while (parent != null && parent.visualElementAsset == null && parent is not IPanelComponentRootElement)
        {
            indexChain.Add(parent.hierarchy.IndexOf(node));
            node = parent;
            parent = node.hierarchy.parent;
        }

        // TryGetInMemoryPath clears pathBuffer and succeeds for a VEA-backed element or a panel root.
        if (parent == null || !VisualElementReferenceTools.TryGetInMemoryPath(parent, pathBuffer))
            return false;

        indexChain.Add(parent.hierarchy.IndexOf(node));

        pathBuffer.Add(k_NonAssetPathMarker);
        for (var i = indexChain.Count - 1; i >= 0; --i)
            pathBuffer.Add(indexChain[i]);

        return true;
    }

    /// <summary>
    /// Returns a stable, serializable identifier for the document scope that <paramref name="element"/>
    /// belongs to. Combined with the element's in-memory authoring-id path it forms the node's UID, so
    /// the key must be identical across domain reloads for the same logical document (for example a
    /// <see cref="GlobalObjectId"/> for a scene panel component, or the edited asset for a stage).
    /// </summary>
    protected bool TryGetScopeKey(VisualElement element, out string scopeKey)
    {
        scopeKey = null;

        var scopeObject = StageStrategy.GetScopeObject(element);
        if (scopeObject == null)
            return false;

        var objectKey = GetGlobalObjectIdScopeKey(scopeObject);
        if (string.IsNullOrEmpty(objectKey))
            return false;

        var suffix = StageStrategy.GetScopeKeySuffix();
        scopeKey = string.IsNullOrEmpty(suffix) ? objectKey : objectKey + suffix;
        return true;
    }

    /// <summary>
    /// Returns a cached <see cref="GlobalObjectId"/> string for <paramref name="obj"/>, suitable as a
    /// <see cref="TryGetScopeKey"/> result.
    /// </summary>
    protected string GetGlobalObjectIdScopeKey(UnityEngine.Object obj)
    {
        if (obj == null)
            return null;

        if (!m_GlobalObjectIdKeyCache.TryGetValue(obj, out var key))
            m_GlobalObjectIdKeyCache[obj] = key = GlobalObjectId.GetGlobalObjectIdSlow(obj).ToString();

        return key;
    }

    private static void WriteBlittable<T>(Span<byte> destination, ref T value) where T : unmanaged
        => MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref value, 1)).CopyTo(destination);

    private static T ReadBlittable<T>(ReadOnlySpan<byte> source) where T : unmanaged
    {
        T value = default;
        source.CopyTo(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1)));
        return value;
    }

    #endregion // HierarchyNodeTypeHandler

    #region IHierarchySearchPropositionProvider

    IEnumerable<SearchProposition> IHierarchySearchPropositionProvider.FetchPropositions(HierarchyViewModel viewModel,
        SearchContext context, SearchPropositionOptions options)
    {
        yield return new SearchProposition(
            category: null,
            label: "UI",
            priority: -1,
            icon: UIResources.GetIconForType(typeof(VisualElement), UIResources.RequestSize.Px16).texture);

        foreach (var proposition in m_QueryEngine.GetPropositions())
            yield return proposition;

        foreach (var t in QueryListBlockAttribute.GetPropositions(typeof(UIQueryTypeListBlock)))
            yield return t;
    }

    #endregion // IHierarchySearchPropositionProvider

    protected virtual bool CanSetEnabled(HierarchyView view, in HierarchyNode node, VisualElement element) => false;

    protected virtual bool OnSetEnabled(HierarchyView view, in HierarchyNode node, VisualElement element) => false;

    /// <summary>
    /// Called when a hierarchy view item is bound to a hierarchy view, allowing customization of the view item.
    /// </summary>
    /// <param name="item">The hierarchy view item.</param>
    /// <param name="element">The <see cref="VisualElement"/> to bind.</param>
    protected virtual void Bind(HierarchyViewItem item, VisualElement element)
    {
        if (element is IPanelComponentRootElement)
        {
            // We just want to display the name of the file.
            return;
        }

        var nameElement = item.Q(className: "hierarchy-item__name");
        nameElement.AddToClassList(HierarchyItemElementNameClassName);
        var index = nameElement.parent.IndexOf(nameElement);

        if (string.IsNullOrEmpty(element.name) ||
            (m_DisplayOptions & UIHierarchyDisplayOptions.Typename) != 0)
        {
            var typeNameLabel = new Label(element.GetType().Name) { pickingMode = PickingMode.Ignore };
            typeNameLabel.AddToClassList(HierarchyItemElementTypeNameClassName);
            nameElement.parent.Insert(index, typeNameLabel);
            typeNameLabel.EnableInClassList(HierarchyItemDisabledClassName, StageStrategy.IsReadOnly || !element.enabledSelf);
        }

        nameElement.EnableInClassList(HierarchyItemDisabledClassName, StageStrategy.IsReadOnly || !element.enabledSelf);

        item.EnableInClassList(VisualElementDisabledUssClass, !StageStrategy.IsFullyEditable(element));

        if ((m_DisplayOptions & UIHierarchyDisplayOptions.UssClasses) != 0)
        {
            foreach (var ussClass in element.GetClasses())
            {
                var ussClassLabel = new Label($".{ussClass}") { pickingMode = PickingMode.Ignore };
                ussClassLabel.AddToClassList(HierarchyItemUssClassName);
                item.LeftCustomContainer.Add(ussClassLabel);
            }
        }
    }

    /// <summary>
    /// Called when a hierarchy view item is unbound from a hierarchy view, allowing cleanup of the view item.
    /// </summary>
    /// <param name="item">The hierarchy view item.</param>
    /// <param name="element">The <see cref="VisualElement"/> to unbind.</param>
    protected virtual void Unbind(HierarchyViewItem item, VisualElement element)
    {
        var nameElement = item.Q(className: "hierarchy-item__name");
        nameElement.RemoveFromClassList(HierarchyItemElementNameClassName);
        nameElement.EnableInClassList("unity-disabled", false);
        item.EnableInClassList("unity-disabled", false);
        item.LeftCustomContainer.Clear();
        var typenameElement = item.parent.Q(className: HierarchyItemElementTypeNameClassName);
        typenameElement.EnableInClassList("unity-disabled", false);
        typenameElement?.RemoveFromHierarchy();
    }

    protected virtual void BindNavigation(HierarchyViewItem item, VisualElement container)
    {
        if (container.visualElementAsset is TemplateAsset subDocument)
        {
            var nameElement = item.Q(className: "hierarchy-item__name");
            var index = nameElement.parent.IndexOf(nameElement);
            var template = subDocument.ResolveTemplate();
            var path = AssetDatabase.GetAssetPath(template);
            var filename = Path.GetFileName(path);

            // The same unsaved-changes marker the document rows carry (see GetDisplayNameOverride). An edit on
            // an element inside an instance lands in the template's own document, not in the one hosting the
            // instance, so this row is the only place that change shows up. The tooltip stays the bare path.
            if (UIAssetRegistry.LiveInstance?.IsDirty(template) == true)
                filename += "*";

            var label = new Label(filename){ tooltip = path };
            label.AddToClassList(HierarchyItemTemplatePath);
            nameElement.parent.Insert(index + 1, label);
        }

        if (!StageStrategy.LimitsNavigationToEditedDocument || IsInsideEditedDocument(container))
            SetStageNodeNavigation(item, container);
        else
            UnsetStageNodeNavigation(item);
    }

    bool IsInsideEditedDocument(VisualElement element)
        => element.GetFirstAncestorWhere(e => e == m_DocumentRoot) != null;

    protected virtual void UnbindNavigation(HierarchyViewItem item, VisualElement container)
    {
        item.Q(className:HierarchyItemTemplatePath)?.RemoveFromHierarchy();
        UnsetStageNodeNavigation(item);
    }

    protected void SetStageNodeNavigation(HierarchyViewItem item, VisualElement container)
    {
        switch (container)
        {
            case IPanelComponentRootElement panelComponentRoot when panelComponentRoot.panelComponent != null && panelComponentRoot.panelComponent.visualTreeAsset != null:
            {
                var panelComponent = panelComponentRoot.panelComponent;
                var context = new VisualTreeAssetEditingContext(panelComponent.visualTreeAsset, panelComponent.panelSettings);
                SetStageNodeNavigation(item, context);
                break;
            }
            case { visualElementAsset: TemplateAsset subDocument }:
            {
                using var _ = ListPool<TemplateAsset>.Get(out var subDocumentPath);
                container.GenerateSubDocumentPath(subDocumentPath);

                var rootVisualTreeAsset = GetRootVisualTreeAsset(container);
                if (!VisualTreeAssetEditingContext.ValidateSubDocumentIsPartOrMainAssetHierarchy(rootVisualTreeAsset, NoAllocHelpers.CreateSpan(subDocumentPath)))
                {
                    UnsetStageNodeNavigation(item);
                    break;
                }

                var panelSettings = GetPanelSettings(container);
                var context = new VisualTreeAssetEditingContext(
                    rootVisualTreeAsset,
                    subDocumentPath.ToArray(),
                    SubDocumentOptions.InContext,
                    panelSettings
                );
                SetStageNodeNavigation(item, context);
                break;
            }
            default:
                UnsetStageNodeNavigation(item);
                break;
        }
    }

    private VisualTreeAsset GetRootVisualTreeAsset(VisualElement element)
    {
        var vta = default(VisualTreeAsset);
        while (element != null)
        {
            if (element.visualTreeAssetSource != null)
                vta = element.visualTreeAssetSource;
            element = element.parent;
        }

        return vta;
    }

    protected PanelSettings GetPanelSettings(VisualElement element)
    {
        return element.GetPanelSettings();
    }

    protected void SetStageNodeNavigation(HierarchyViewItem item, VisualTreeAssetEditingContext context)
    {
        var modifierKey = Application.platform == RuntimePlatform.OSXEditor ? "Option" : "Alt";
        var navigationTooltip = $"Open Visual Tree Asset in context.\nPress the {modifierKey} modifier key to open in isolation.";
        var navigateButton = item.NavigateIntoButton;

        if (navigateButton == null)
            return;

        navigateButton.style.display = DisplayStyle.Flex;
        navigateButton.tooltip = navigationTooltip;
        if (navigateButton.userData == null)
        {
            navigateButton.clickable.activators.Add(k_StageAltActivationFilter);
            navigateButton.clickable.clickedWithEventInfo += OpenStageMode;
        }

        navigateButton.userData = context;
    }

    protected void UnsetStageNodeNavigation(HierarchyViewItem item)
    {
        var navigateButton = item.NavigateIntoButton;
        if (navigateButton == null)
            return;

        navigateButton.style.display = DisplayStyle.None;
        navigateButton.tooltip = null;
        navigateButton.clickable.activators.Remove(k_StageAltActivationFilter);
        navigateButton.clickable.clickedWithEventInfo -= OpenStageMode;
        navigateButton.userData = null;
    }

    void OpenStageMode(EventBase obj)
    {
        if (obj.target is not Button button)
            return;

        if (button.userData is not VisualTreeAssetEditingContext context)
            return;

        var isolationRequested = obj is PointerUpEvent { altKey: true };
        if (isolationRequested && context.SubDocumentPath != null)
        {
            GoToStage(new VisualTreeAssetEditingContext(
                context.RootVisualTreeAsset,
                context.SubDocumentPath,
                SubDocumentOptions.Isolation,
                context.PanelSettings
            ), BreadcrumbBar.SeparatorStyle.Line);
        }
        else
        {
            GoToStage(context, isolationRequested ? BreadcrumbBar.SeparatorStyle.Line : BreadcrumbBar.SeparatorStyle.Arrow);
        }
    }

    internal void GoToStage(VisualTreeAssetEditingContext context, BreadcrumbBar.SeparatorStyle separatorStyle)
    {
        VisualElementEditingStage.GoToStage(context, separatorStyle);
    }





    /// <summary>
    /// Returns the icon to use for a given <see cref="VisualElement"/> instance.
    /// </summary>
    /// <param name="element">The <see cref="VisualElement"/>.</param>
    /// <returns>The icon.</returns>
    protected virtual Background GetIcon(VisualElement element)
        => UIResources.GetIconForElement(element, UIResources.RequestSize.Px16);

    /// <summary>
    /// Queries the handler to figure out if a node should be created for the request <see cref="VisualElement"/>.
    /// </summary>
    /// <param name="element">The requested <see cref="VisualElement"/></param>
    /// <returns>The <see cref="NodeCreationType"/> indicating if a node should be created or not.</returns>
    protected virtual NodeCreationType ShouldCreateNode(VisualElement element)
        => StageStrategy.IsPanelRoot(element) ? NodeCreationType.CreateChildren : NodeCreationType.Create;

    /// <summary>
    /// Attempts to find the parent <see cref="HierarchyNode"/> for a given <see cref="VisualElement"/>.
    /// </summary>
    /// <param name="element">The requested <see cref="VisualElement"/>.</param>
    /// <param name="parentNode">The parent <see cref="HierarchyNode"/> of the <see cref="VisualElement"/>.</param>
    /// <returns>The <see cref="HierarchyNode"/> of the parent of the <paramref name="element"/> or the root node.</returns>
    /// <exception cref="ArgumentNullException">If the <paramref name="element"/> is null.</exception>
    protected virtual bool TryGetParentNode(VisualElement element, out HierarchyNode parentNode)
    {
        if (null == element)
            throw new ArgumentNullException(nameof(element));

        // A document the stage shows is anchored by the stage itself: at the hierarchy root when the stage
        // edits a single document, under the GameObject that renders it when it shows several at once.
        if (StageStrategy.IsEditedDocumentRootElement(element))
        {
            parentNode = Hierarchy.Root;
            return true;
        }

        var hostGameObject = StageStrategy.GetDocumentHostGameObject(element);
        if (hostGameObject != null)
        {
            var gameObjectHandler = GameObjectHandler;
            parentNode = gameObjectHandler != null ? gameObjectHandler.GetOrCreateNode(hostGameObject) : HierarchyNode.Null;
            return parentNode != HierarchyNode.Null;
        }

        var parent = element.hierarchy.parent;
        if (null == parent)
        {
            parentNode = Hierarchy.Root;
            return true;
        }

        while (true)
        {
            switch (ShouldCreateNode(parent))
            {
                case NodeCreationType.DontCreate:
                    parentNode = HierarchyNode.Null;
                    break;
                case NodeCreationType.Create:
                    if (m_Mappings.TryGetValue(parent, out parentNode))
                        return true;
                    parentNode = HierarchyNode.Null;
                    return false;
                case NodeCreationType.CreateChildren:
                    parent = parent.hierarchy.parent;
                    if (parent == null)
                    {
                        parentNode = Hierarchy.Root;
                        return true;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    /// <summary>
    /// Register a panel to start getting changes.
    /// </summary>
    /// <param name="panel">The requested panel.</param>
    internal void RegisterPanel(Panel panel)
    {
        if (panel == null || m_RegisteredPanels.Contains(panel))
            return;

        panel.RegisterChangeProcessor(this);
        m_RegisteredPanels.Add(panel);
        UnregisterOrphanedElements();
    }

    /// <summary>
    /// Unregister a panel to stop getting changes.
    /// </summary>
    /// <param name="panel">The requested panel.</param>
    internal void UnregisterPanel(Panel panel)
    {
        panel.UnregisterChangeProcessor(this);
        m_RegisteredPanels.Remove(panel);
        m_BegunPanels.Remove(panel);
    }

    /// <summary>
    /// Queries the associated <see cref="VisualElement"/> of a given <see cref="HierarchyNode"/>.
    /// </summary>
    /// <param name="node">The <see cref="HierarchyNode"/>.</param>
    /// <param name="element">The associated <see cref="VisualElement"/>.</param>
    /// <returns><see langword="true"/> if the element was found; <see langword="false"/> otherwise.</returns>
    protected bool TryGetElementFromNode(in HierarchyNode node, out VisualElement element)
    {
        return m_Mappings.TryGetValue(node, out element);
    }

    /// <summary>
    /// Queries the associated <see cref="HierarchyNode"/> of a given <see cref="VisualElement"/>.
    /// </summary>
    /// <param name="element">The <see cref="VisualElement"/>.</param>
    /// <param name="node">The associated <see cref="HierarchyNode"/>.</param>
    /// <returns><see langword="true"/> if the element was found; <see langword="false"/> otherwise.</returns>
    protected bool TryGetNodeFromElement(VisualElement element, out HierarchyNode node)
    {
        return m_Mappings.TryGetValue(element, out node);
    }

    protected bool TryGetSelectionObject(HierarchyNode node, out EntityId entityId)
    {
        return m_Mappings.TryGetSelectionHandle(node, out entityId);
    }


    public bool GetEnabled(HierarchyView view, in HierarchyNode node)
    {
        return TryGetElementFromNode(node, out var element) && element.enabledSelf;
    }

    public bool SetEnabled(HierarchyView view, in HierarchyNode node, bool value)
    {
        try
        {
            if (TryGetElementFromNode(node, out var element) &&
                CanSetEnabled(view, in node, element))
                return OnSetEnabled(view, in node, element);
            return false;
        }
        finally
        {
            Hierarchy.SetDirty();
        }
    }

    /// <summary>
    /// Asks for the elements of <paramref name="assets"/> to be selected on the first update that can resolve
    /// every one of them (see <see cref="ApplyDeferredSelectionRequests"/>).
    /// </summary>
    /// <remarks>
    /// Replaces whatever is still pending instead of adding to it. Each authoring action makes exactly one
    /// request, and the one it made supersedes any older one that has not resolved — a request for a document no
    /// tracked panel renders never will, and appending to it would hold up every request made after it.
    /// </remarks>
    internal void RequestSelectionOnNextUpdate(IList<VisualElementAsset> assets)
    {
        m_NodesToSelect ??= new List<SelectionRequest>();
        m_NodesToSelect.Clear();

        // The budget belongs to the batch, not to the handler: this request has had no chance to resolve yet,
        // whatever the one it supersedes spent.
        m_SelectionRequestAttempts = 0;

        for (var i = 0; i < assets.Count; ++i)
            m_NodesToSelect.Add(new SelectionRequest(assets[i]));
    }

    /// <summary>
    /// Asks for the clone of <paramref name="originElement"/> to be selected in a stage rooted at
    /// <paramref name="stageDocument"/>: the request carries the origin's instance path relative to the
    /// enclosing instance of that document, which is the path its stage clone has.
    /// </summary>
    internal void RequestSelectionOnNextUpdate(VisualElement originElement, VisualTreeAsset stageDocument)
    {
        var asset = originElement?.visualElementAsset;
        if (asset == null)
            return;

        using var _ = ListPool<VisualElementAsset>.Get(out var assets);
        assets.Add(asset);
        RequestSelectionOnNextUpdate(assets);

        // No panel-component scope: the stage panel has none, and the relative path is unambiguous there.
        using var __ = ListPool<int>.Get(out var prefix);
        BuildInstancePrefix(originElement.parent, prefix, stageDocument);
        ScopeRequestAt(0, prefix, scope: null);

        MarkHierarchyDirty();
    }

    /// <summary>
    /// Narrows every pending request to the document instance <paramref name="parent"/> belongs to, for a caller
    /// that knows which one it just edited.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The commands only ever see assets, so what they ask for is "any clone of this asset". That is the wrong
    /// element as soon as the document holding it is instantiated more than once — dropping into the second
    /// instance of a template would select the copy in the first. The caller driving the command is the one that
    /// knows the instance, so it hands it over here, after the command has run — which is also when the requests
    /// it narrows exist, since the commands are what file them.
    /// </para>
    /// <para>
    /// Every pending request is assumed to be a child of <paramref name="parent"/>, which is what a drop, a
    /// paste and an add all produce. An action that spreads its results over several parents — a duplicate —
    /// has one of its own to name for each, and uses <see cref="ScopePendingSelectionRequestsTo(IReadOnlyList{VisualElement})"/>.
    /// </para>
    /// </remarks>
    internal void ScopePendingSelectionRequestsTo(VisualElement parent)
    {
        if (m_NodesToSelect is not { Count: > 0 } || parent == null)
            return;

        using var _ = ListPool<int>.Get(out var prefix);
        BuildInstancePrefix(parent, prefix);

        var scope = GetInstanceScope(parent);

        for (var i = 0; i < m_NodesToSelect.Count; ++i)
            ScopeRequestAt(i, prefix, scope);
    }

   internal void ScopePendingSelectionRequestsTo(IReadOnlyList<VisualElement> parents)
    {
        if (m_NodesToSelect is not { Count: > 0 } || parents == null || parents.Count != m_NodesToSelect.Count)
            return;

        using var _ = ListPool<int>.Get(out var prefix);

        for (var i = 0; i < m_NodesToSelect.Count; ++i)
        {
            var parent = parents[i];
            if (parent == null)
                continue;

            BuildInstancePrefix(parent, prefix);
            ScopeRequestAt(i, prefix, GetInstanceScope(parent));
        }
    }

    /// <summary>
    /// Points the pending request at <paramref name="index"/> at the one clone of its asset that hangs from the
    /// template instances of <paramref name="prefix"/>, inside <paramref name="scope"/>.
    /// </summary>
    void ScopeRequestAt(int index, List<int> prefix, IPanelComponent scope)
    {
        var request = m_NodesToSelect[index];
        if (request.Asset == null)
            return;

        var path = new int[prefix.Count + 1];
        for (var j = 0; j < prefix.Count; ++j)
            path[j] = prefix[j];
        path[^1] = request.Asset.id;

        m_NodesToSelect[index] = request.ScopedTo(path, scope);
    }

    /// <summary>The panel component rendering the outermost document <paramref name="element"/> sits in.</summary>
    static IPanelComponent GetInstanceScope(VisualElement element)
        => element.GetFirstOfType<IPanelComponentRootElement>()?.panelComponent;

    /// <summary>
    /// The ids of the template instances <paramref name="parent"/> sits in, outermost first — the prefix every
    /// child of it carries in its in-memory authoring id path.
    /// </summary>
    /// <remarks>
    /// Mirrors what <see cref="VisualElementReferenceTools.TryGetInMemoryPath"/> collects walking up from an
    /// element: only template instances scope the id space, and the clone root — a panel component's root, which
    /// is a <see cref="TemplateContainer"/> with no backing asset — is left out of the path.
    /// </remarks>
    static void BuildInstancePrefix(VisualElement parent, List<int> prefix, VisualTreeAsset stopAtDocument = null)
    {
        prefix.Clear();

        for (var current = parent; current != null; current = current.parent)
        {
            // The enclosing instance of the stop document stays out of the path, like the clone root.
            if (stopAtDocument != null
                && current is TemplateContainer { templateSource: { } templateSource }
                && templateSource == stopAtDocument)
                break;

            if (current is TemplateContainer && current is not IPanelComponentRootElement && current.visualElementAsset != null)
                prefix.Add(current.visualElementAsset.id);
        }

        prefix.Reverse();
    }

    protected List<SelectionRequest> GetDelayedSelectionRequests()
    {
        return m_NodesToSelect;
    }

    protected override void UpdateBegin()
    {
        if (!Hierarchy.IsCreated)
            return;

        var registry = VisualElementSelectionRegistry.Instance;
        if (registry == null)
            return;

        registry.EnsureInitialized();

        for (var i = 0; i < m_RegisteredPanels.Count; ++i)
        {
            var panel = m_RegisteredPanels[i];
            if (m_BegunPanels.Contains(panel))
                continue;

            if (registry.IsTracked(panel))
                panel.UpdateAuthoring();
        }
    }

    #region IVisualElementChangeProcessor

    void IVisualElementChangeProcessor.BeginProcessing(BaseVisualElementPanel panel)
    {
        if (!Hierarchy.IsCreated)
            return;

        if (panel is Panel p)
            m_BegunPanels.Add(p);

        Rebuild(panel);
    }

    void IVisualElementChangeProcessor.ProcessChanges(BaseVisualElementPanel panel, AuthoringChanges changes)
    {
        if (!Hierarchy.IsCreated)
            return;

        // The selection registry (registered as a change processor before us) has already computed
        // the old-instance -> new-instance remaps for this batch and transferred the selection
        // objects. We only need to keep our node map stable by reusing nodes for reclaimed elements.
        var remappings = VisualElementSelectionRegistry.Instance?.GetFrameRemaps(panel as Panel);
        if (remappings is { Count: > 0 })
            m_Mappings.Remap(remappings);

        // We process the elements that were added or moved before the elements that were removed
        // because when the Hierarchy will remove a node, it will also remove its children, which
        // would invalidate the node of children moved to a different parent.
        if (changes.addedOrMovedElements.Count > 0)
        {
            using var parentHashHandler = HashSetPool<HierarchyNode>.Get(out var parentsToSort);
            using var processedElementsHandler = HashSetPool<VisualElement>.Get(out var processedElements);

            foreach (var element in changes.addedOrMovedElements)
            {
                if (processedElements.Contains(element))
                    continue;

                var elementParent = element.hierarchy.parent;

                // Already tracked, the element must have been moved.
                if (m_Mappings.TryGetValue(element, out var elementNode))
                {
                    // Parent is already mapped
                    if (TryGetParentNode(element, out var parentNode))
                    {
                        CommandList.SetParent(elementNode, parentNode);
                        parentsToSort.Add(parentNode);
                    }
                    else
                    {
                        // Wait until the parent is processed.
                    }
                }
                // Element was not known previously
                else
                {
                    // If the element does not have a parent, it is the single root of the panel.
                    var index = elementParent?.IndexOf(element) ?? 0;
                    Rebuild(element, index);

                    if (m_Mappings.TryGetValue(elementParent, out var elementParentNode))
                        parentsToSort.Add(elementParentNode);
                }
            }

            // Recompute sorting index
            foreach (var parentNode in parentsToSort)
            {
                RefreshChildrenSortingIndices(parentNode);
            }
        }

        foreach (var removed in changes.removedFromPanel)
        {
            ClearSingle(removed);
        }

        foreach (var element in changes.stylingContextChanged)
        {
            if (m_Mappings.TryGetValue(element, out var elementNode))
                CommandList.SetName(elementNode, element.name);
        }
    }

    void IVisualElementChangeProcessor.EndProcessing(BaseVisualElementPanel panel)
    {
        if (!Hierarchy.IsCreated)
            return;

        if (panel is Panel p)
            m_BegunPanels.Remove(p);

        Clear(panel);
    }

    #endregion IVisualElementChangeProcessor

    protected void UnregisterAllPanels()
    {
        while (m_RegisteredPanels.Count > 0)
        {
            var panel = m_RegisteredPanels[^1];
            Clear(panel);
            UnregisterPanel(panel);
        }

        UnregisterOrphanedElements();
    }

    void UnregisterOrphanedElements()
    {
        using var _ = ListPool<VisualElement>.Get(out var mappedChildren);
        mappedChildren.AddRange(m_Mappings.MappedElements);
        foreach (var child in mappedChildren)
        {
            if (child.resourcesReleased || child.panel == null)
                Clear(child);
        }
    }

    private void Rebuild(BaseVisualElementPanel panel)
    {
        var root = panel.visualTree;
        Rebuild(root, 0);
    }

    private void Rebuild(VisualElement element, int siblingIndex)
    {
        var kind = ShouldCreateNode(element);
        switch (kind)
        {
            case NodeCreationType.DontCreate:
                return;
            case NodeCreationType.Create:
                RebuildSingle(element, siblingIndex);
                break;
            case NodeCreationType.CreateChildren:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        PartialRebuild(element.hierarchy);
    }

    private void RebuildSingle(VisualElement element, int siblingIndex)
    {
        if (!TryGetParentNode(element, out var parentNode))
            return;

        if (!m_Mappings.TryGetValue(element, out var elementNode))
        {
            CommandList.Add(in parentNode, out elementNode);
            var instanceID = VisualElementSelectionRegistry.Instance?.GetOrCreateEntityId(element) ?? EntityId.None;
            m_Mappings.TryAdd(in elementNode, element, instanceID);
        }
        else
        {
            CommandList.SetParent(in elementNode, in parentNode);
        }

        CommandList.SetName(in elementNode, element.name);
        CommandList.SetSortIndex(in elementNode, siblingIndex);
    }

    private void PartialRebuild(VisualElement.Hierarchy hierarchy)
    {
        for (var i = 0; i < hierarchy.childCount; ++i)
        {
            var child = hierarchy[i];
            Rebuild(child, i);
        }
    }

    private void Clear(BaseVisualElementPanel panel)
    {
        var root = panel.visualTree;
        Clear(root);
    }

    private void Clear(VisualElement element)
    {
        ClearSingle(element);

        for (var i = 0; i < element.hierarchy.childCount; ++i)
        {
            var child = element.hierarchy[i];
            Clear(child);
        }
    }

    private void ClearSingle(VisualElement element)
    {
        // PointerLeaveEvent is not fired for virtualized items, so clear the hover state here.
        if (element == m_HoveredElement)
            HoveredElement = null;

        if (!m_Mappings.TryGetValue(element, out var removedNode))
            return;

        CommandList.Remove(removedNode);
        m_Mappings.TryRemove(removedNode);
    }

    private void RefreshChildrenSortingIndices(HierarchyNode node)
    {
        if (node == Hierarchy.Root)
        {
            var children = Hierarchy.GetChildren(node);
            VisualElement rootParent = null;
            for (var i = 0; i < children.Length; ++i)
            {
                if (!TryGetElementFromNode(children[i], out var element) || element.panel == null)
                {
                    continue;
                }

                if (rootParent == null)
                    rootParent = element.hierarchy.parent;
            }

            if (rootParent == null)
                return;
            for (var i = 0; i < rootParent.hierarchy.childCount; ++i)
            {
                if (!TryGetNodeFromElement(rootParent.hierarchy[i], out var elementNode))
                    continue;
                CommandList.SetSortIndex(elementNode, i);
            }
            CommandList.SortChildren(node);
            return;
        }

        if (!m_Mappings.TryGetValue(node, out var parent))
            return;

        for (var i = 0; i < parent.hierarchy.childCount; ++i)
        {
            var child = parent.hierarchy[i];
            if (m_Mappings.TryGetValue(child, out var childNode))
                CommandList.SetSortIndex(childNode, i);
            else
                Debug.Log($"Trying to set sort index for element `{GetDebugName(child)}`");
        }

        CommandList.SortChildren(node);
    }

    private static string GetDebugName(VisualElement element)
    {
        if (element == null)
            return "<null>";
        var name = string.IsNullOrEmpty(element.name) ? "" : $"#{element.name}";
        var typeName = TypeUtility.GetTypeDisplayName(element.GetType());
        return $"{typeName}{name}, id={element.controlid}";
    }

    private IMemoryOwner<HierarchyNode> GetSelection(HierarchyView view, out SelectionContext selectionContext)
    {
        var selectionCount = view.ViewModel.HasFlagsCount(HierarchyNodeFlags.Selected);
        var owner = MemoryPool<HierarchyNode>.Shared.Rent(selectionCount);
        var selection = owner.Memory.Span[..selectionCount];
        view.ViewModel.GetNodesWithFlags(HierarchyNodeFlags.Selected, selection);

        var containsElements = false;
        var onlyContainsElements = true;
        foreach (var node in selection)
        {
            if (view.ViewModel.GetNodeTypeHandlerBase(node) == this)
                containsElements = true;
            else
                onlyContainsElements = false;
        }

        SelectionContext.SelectionType type;
        if (onlyContainsElements)
            type = SelectionContext.SelectionType.All;
        else if (containsElements)
            type = SelectionContext.SelectionType.Mixed;
        else
            type = SelectionContext.SelectionType.None;

        selectionContext = new SelectionContext(selection, selectionCount, type);
        return owner;
    }

    private static QueryEngine<VisualElement> CreateQueryEngine()
    {
        var engine = new QueryEngine<VisualElement>();

        // A plain word without a search-data callback is a parse error that invalidates the whole
        // query, taking any t:/c: filters beside it down too. Names are word-matched natively before
        // handlers run, so this only has to agree with that verdict.
        engine.SetSearchDataCallback(GetSearchData, StringComparison.OrdinalIgnoreCase);

        engine.AddFilter<List<string>>(UISearchTokens.UssClassesSearchToken, CompareClasses);
        if (engine.TryGetFilter(UISearchTokens.UssClassesSearchToken, out var classesFilter))
        {
            classesFilter.AddTypeParser(ExtractUssClassesFromInputString);
            classesFilter.AddOrUpdatePropositionData(
                label: "classes",
                category: "UI",
                help: "Search by USS classes",
                replacement: UISearchTokens.UssClassesSearchToken + ":",
                type: typeof(string),
                priority: -2,
                icon: UIResources.GetIconForType(typeof(StyleSheet), UIResources.RequestSize.Px16).texture
            );
        }

        engine.AddFilter(UISearchTokens.TypeSearchToken, ExtractTypeName);
        if (engine.TryGetFilter(UISearchTokens.TypeSearchToken, out var typeFilter))
        {
            typeFilter.AddOrUpdatePropositionData(
                label: "type",
                category: "UI",
                help: "Search by UI Toolkit types",
                replacement: UISearchTokens.TypeSearchToken + ":",
                type: typeof(string),
                priority: -1,
                icon: UIResources.GetIconForType(typeof(VisualTreeAsset), UIResources.RequestSize.Px16).texture
            );
        }

        return engine;
    }

    private static ParseResult<List<string>> ExtractUssClassesFromInputString(string classesString)
    {
        if (string.IsNullOrEmpty(classesString))
            return new ParseResult<List<string>>(false, null);

        var classes = new List<string>();
        // [TODO] MP: Change to add TrimEntries when we get access to it.
        var classesArray = classesString.Split(", ", StringSplitOptions.RemoveEmptyEntries);
        foreach (var classString in classesArray)
        {
            classes.Add(classString.Trim());
        }

        return new ParseResult<List<string>>(true, classes);
    }

    private static string ExtractTypeName(VisualElement element)
    {
        return element.typeName;
    }

    private static IEnumerable<string> GetSearchData(VisualElement element)
    {
        yield return element.name;
    }

    private static bool CompareClasses(VisualElement element, string _, List<string> classes)
    {
        if (classes == null || classes.Count == 0)
            return false;

        for (var i = 0; i < classes.Count; ++i)
        {
            var found = false;
            // While this comparison is not particularly efficient, it allows users
            // to search for partial uss classes.
            foreach (var ussClass in element.GetClasses())
            {
                if (!ussClass.Contains(classes[i], StringComparison.InvariantCultureIgnoreCase))
                    continue;

                found = true;
                break;
            }

            if (!found)
                return false;
        }

        return true;
    }

    public override HierarchyNode GetNodeFromEntityId(EntityId entityId)
    {
        return m_Mappings.TryGetNodeFromSelectionHandle(entityId, out var node)
            ? node
            : HierarchyNode.Null;
    }

    public override int GetNodesFromEntityIds(ReadOnlySpan<EntityId> entityIds, Span<HierarchyNode> outNodes)
    {
        var resolved = 0;
        for (var i = 0; i < entityIds.Length; ++i)
        {
            ref var outNode = ref outNodes[i];
            if (outNode != HierarchyNode.Null)
            {
                ++resolved;
                continue;
            }

            if (m_Mappings.TryGetNodeFromSelectionHandle(entityIds[i], out var node))
            {
                outNode = node;
                ++resolved;
            }
        }
        return entityIds.Length - resolved;
    }

    public override EntityId GetEntityIdFromNode(in HierarchyNode node)
    {
        return m_Mappings.TryGetSelectionHandle(node, out var selectionHandle)
            ? selectionHandle
            : EntityId.None;
    }

    public override int GetEntityIdsFromNodes(ReadOnlySpan<HierarchyNode> nodes,
        Span<EntityId> outEntityIds)
    {
        var resolved = 0;
        for (var i = 0; i < nodes.Length; ++i)
        {
            ref var outEntityId = ref outEntityIds[i];
            if (outEntityId != EntityId.None)
            {
                ++resolved;
                continue;
            }

            if (m_Mappings.TryGetSelectionHandle(nodes[i], out var selectionHandle))
            {
                outEntityId = selectionHandle;
                ++resolved;
            }
        }
        return nodes.Length - resolved;
    }

    private void OnDisplayOptionsChanged(UIHierarchyDisplayOptions options)
    {
        m_DisplayOptions = options;
        if (Hierarchy.IsCreated)
            CommandList.SetDirty();
    }

    void OnStartHover(PointerEnterEvent evt, VisualElement element)
    {
        HoveredElement = element;
    }

    void OnEndHover(PointerLeaveEvent evt)
    {
        HoveredElement = null;
    }

    void ProcessHighlightElementsCommand(in CommandContext context)
    {
        m_HighlightedNodes.Clear();
        if (Hierarchy.IsCreated)
            CommandList.SetDirty();
        if (context.Status != CommandExecutionStatus.Success)
            return;

        if (context.Source == CommandSources.Hierarchy)
            return;

        if (context.Command is not HighlightCommand command)
            return;

        if (command.Elements == null)
            return;

        foreach (var element in command.Elements)
        {
            if (m_Mappings.TryGetValue(element, out var node))
            {
                m_HighlightedNodes.Add(node);
            }
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
