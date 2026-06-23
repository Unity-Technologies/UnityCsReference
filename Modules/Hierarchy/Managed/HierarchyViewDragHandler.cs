// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using UnityEngine.UIElements.HierarchyV2;

namespace Unity.Hierarchy;

sealed class HierarchyViewDragHandler
{
    struct HierarchyViewDragAndDropTargets : IEquatable<HierarchyViewDragAndDropTargets>
    {
        /// <summary>
        /// The index of the parent node in the hierarchy view model. Can be k_InvalidIndex if inserting at root.
        /// </summary>
        public int parentIndex;

        /// <summary>
        /// The index in the parent's children list where the nodes would be inserted.
        /// </summary>
        public int childIndex;

        /// <summary>
        /// The drop position relative to the target item (or completely outside items).
        /// </summary>
        public DragAndDropPosition dropPosition;

        /// <summary>
        /// The visual mode to display for the drag-and-drop operation.
        /// </summary>
        public DragVisualMode dragVisualMode;

        [NoAutoStaticsCleanup]
        public static readonly HierarchyViewDragAndDropTargets Rejected = new(parentIndex: k_InvalidIndex, childIndex: k_InvalidIndex, dropPosition: DragAndDropPosition.OverItem) { dragVisualMode = DragVisualMode.Rejected };

        public HierarchyViewDragAndDropTargets(int parentIndex, int childIndex, DragAndDropPosition dropPosition)
        {
            this.parentIndex = parentIndex;
            this.childIndex = childIndex;
            this.dropPosition = dropPosition;
            this.dragVisualMode = DragVisualMode.Move;
        }

        public bool Equals(HierarchyViewDragAndDropTargets other)
        {
            return parentIndex == other.parentIndex && childIndex == other.childIndex && dropPosition == other.dropPosition;
        }

        public override bool Equals(object obj)
        {
            return obj is HierarchyViewDragAndDropTargets other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(parentIndex, childIndex, (int)dropPosition);
        }
    }

    sealed class AutoExpansionData
    {
        public HierarchyNode[] expandedNodesBeforeDrag;
        public int lastItemIndex = -1;
        public float expandItemBeginTimerMs;
        public Vector2 expandItemBeginPosition;
    }

    const string k_MultipleDragTitle = "<Multiple>";
    internal static readonly UniqueStyleString DragHoverBarStyleName = new("hierarchy__container__drag-hover-bar");
    internal static readonly UniqueStyleString DragHoverBarItemName = new("HierarchyHoverBar");
    internal static readonly UniqueStyleString DragHoverItemMarkerItemName = new("HierarchyHoverItemMarker");
    internal static readonly UniqueStyleString DragHoverSiblingMarkerItemName = new("HierarchyHoverSiblingMarker");

    readonly HierarchyView m_HierarchyView;
    readonly CollectionView m_CollectionView;

    HierarchyViewDragAndDropTargets m_LastDragPosition;
    AutoExpansionData m_AutoExpansionData;
    IVisualElementScheduledItem m_ExpandItemScheduledItem;

    // Handlers whose node type appears in the current drag's selection. Populated by SetupDragAndDrop
    // and consumed by HandleNodeHandlersCanReorder/OnReorder so the per-frame CanReorder calls don't
    // re-classify the dragged set every time. Cleared on drag exit and at the end of HandleDrop.
    readonly List<IHierarchyEditorNodeTypeHandler> m_ParticipatingHandlers = new();

    VisualElement m_DragHoverBar;
    VisualElement m_DragHoverItemMarker;
    VisualElement m_DragHoverSiblingMarker;
    EventModifiers m_CurrentEventModifiers; // Temporary workaround until they are available in the ListView drag events

    float m_LeftIndentation = -1f;
    float m_SiblingBottom = -1f;
    const int k_DragHoverBarHeight = 2;
    const int k_InvalidIndex = -1;
    const long k_ExpandUpdateIntervalMs = 10;
    const float k_DropExpandTimeoutMs = 700f;
    const float k_DropDeltaPosition = 100f;
    const float k_HalfDropBetweenHeight = 4f; // Value taken from TreeViewGUI -> k_HalfDropBetweenHeight.

    Hierarchy Source => m_HierarchyView.Source;
    HierarchyViewModel ViewModel => m_HierarchyView.ViewModel;

    public HierarchyViewDragHandler(HierarchyView hierarchyView)
    {
        m_HierarchyView = hierarchyView;
        m_CollectionView = m_HierarchyView.ListView;
        m_AutoExpansionData = new AutoExpansionData();

        m_CollectionView.canStartDrag += CanStartDrag;
        m_CollectionView.setupDragAndDrop += SetupDragAndDrop;
        m_CollectionView.dragAndDropUpdate += DragAndDropUpdate;
        m_CollectionView.handleDrop += HandleDrop;

#pragma warning disable UAL0015 // Auto cleaned up symbol assigned by constructor
        // this is ok, as it is used as part of HierarchyView Visual element which is re-created on code reload
        m_CollectionView.RegisterCallback<DragUpdatedEvent>(OnDragUpdated, TrickleDown.TrickleDown);
        m_CollectionView.RegisterCallback<DragExitedEvent>(OnDragExited, TrickleDown.TrickleDown);
        m_CollectionView.RegisterCallback<DragPerformEvent>(OnDragPerform, TrickleDown.TrickleDown);
        m_CollectionView.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);

        // Temporary until event modifiers are available in the ListView drag events.
        m_CollectionView.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
        m_CollectionView.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
        m_HierarchyView.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
#pragma warning restore UAL0015 // Auto cleaned up symbol assigned by constructor
    }

    void OnDragUpdated(DragUpdatedEvent evt)
    {
        m_CurrentEventModifiers = evt.modifiers;
        if (!m_CollectionView.worldBound.Contains(evt.mousePosition))
            ClearDragAndDropUI();
    }

    void OnDragExited(DragExitedEvent evt)
    {
        ClearDragAndDrop();
    }

    void OnDragPerform(DragPerformEvent evt)
    {
        m_CurrentEventModifiers = evt.modifiers;
    }

    void OnPointerLeave(PointerLeaveEvent evt)
    {
        ClearDragAndDropUI();
    }

    void OnPointerDown(PointerDownEvent evt)
    {
        m_CurrentEventModifiers = evt.modifiers;
    }

    void OnPointerMove(PointerMoveEvent evt)
    {
        m_CurrentEventModifiers = evt.modifiers;
    }

    void OnPointerUp(PointerUpEvent evt)
    {
        m_CurrentEventModifiers = evt.modifiers;
    }

    bool IsSearchActive()
    {
        return m_HierarchyView.Filtering;
    }

    bool CanStartDrag(CanStartDragArgs args)
    {
        if (IsSearchActive())
            return false;

        using var _ = DictionaryPool<IHierarchyEditorNodeTypeHandler, List<HierarchyNode>>.Get(out var nodesByType);
        GetDraggedNodesByType(nodesByType);
        foreach (var (editorHandler, nodes) in nodesByType)
        {
            if (!editorHandler.CanStartDrag(m_HierarchyView, NoAllocHelpers.CreateReadOnlySpan(nodes)))
            {
                ReleasePooledNodeBuckets(nodesByType);
                return false;
            }
        }
        ReleasePooledNodeBuckets(nodesByType);

        return true;
    }

    void GetDraggedNodesByType(Dictionary<IHierarchyEditorNodeTypeHandler, List<HierarchyNode>> outNodesByType)
    {
        foreach (ref readonly var node in ViewModel.EnumerateNodesWithFlags(HierarchyNodeFlags.Selected))
        {
            var nodeTypeHandler = ViewModel.GetNodeTypeHandlerBase(in node);
            if (nodeTypeHandler is not IHierarchyEditorNodeTypeHandler editorHandler)
                continue;
            if (!outNodesByType.TryGetValue(editorHandler, out var list))
            {
                list = ListPool<HierarchyNode>.Get();
                outNodesByType[editorHandler] = list;
            }
            list.Add(node);
        }
    }

    static void ReleasePooledNodeBuckets(Dictionary<IHierarchyEditorNodeTypeHandler, List<HierarchyNode>> nodesByType)
    {
        foreach (var list in nodesByType.Values)
            ListPool<HierarchyNode>.Release(list);
    }

    StartDragArgs SetupDragAndDrop(SetupDragAndDropArgs args)
    {
        var allEntityIds = new List<EntityId>();
        using var _ = ListPool<string>.Get(out var paths);
        using var _2 = DictionaryPool<string, object>.Get(out var genericData);
        using var _3 = DictionaryPool<IHierarchyEditorNodeTypeHandler, List<HierarchyNode>>.Get(out var nodesByType);
        GetDraggedNodesByType(nodesByType);

        m_ParticipatingHandlers.Clear();
        foreach (var (editorHandler, nodes) in nodesByType)
        {
            var setupData = new HierarchyViewDragAndDropSetupData(NoAllocHelpers.CreateReadOnlySpan(nodes), allEntityIds, paths, m_HierarchyView, genericData);
            editorHandler.OnStartDrag(setupData);
            m_ParticipatingHandlers.Add(editorHandler);
        }

        var title = GetDragTitle(nodesByType, args.startDragArgs.title);
        ReleasePooledNodeBuckets(nodesByType);
        var startDragArgs = new StartDragArgs(title, args.startDragArgs.visualMode);

        startDragArgs.SetEntityIds(allEntityIds);
        startDragArgs.SetPaths(paths.ToArray());
        foreach (var kvp in genericData)
            startDragArgs.SetGenericData(kvp.Key, kvp.Value);

        return startDragArgs;
    }

    string GetDragTitle(Dictionary<IHierarchyEditorNodeTypeHandler, List<HierarchyNode>> draggedNodesByType, string fallbackTitle)
    {
        if (draggedNodesByType.Count > 1)
            return k_MultipleDragTitle;

        if (draggedNodesByType.Count == 1)
        {
            List<HierarchyNode> draggedNodes = null;
            using var enumerator = draggedNodesByType.Values.GetEnumerator();
            if (enumerator.MoveNext())
                draggedNodes = enumerator.Current;
            if (draggedNodes == null || draggedNodes.Count == 0)
                return fallbackTitle;
            if (draggedNodes.Count > 1)
                return k_MultipleDragTitle;

            var node = draggedNodes[0];
            if (node != HierarchyNode.Null && ViewModel.GetNodeTypeHandler(in node) is IHierarchyEditorNodeTypeHandler editorHandler)
                return editorHandler.GetDragTitle(m_HierarchyView, in node) ?? fallbackTitle;
        }

        return fallbackTitle;
    }

    DragVisualMode DragAndDropUpdate(HandleDragAndDropArgs args)
    {
        var dropTargets = GetVisualMode(in args);
        if (dropTargets.dragVisualMode == DragVisualMode.Rejected)
        {
            ClearDragAndDropUI();
        }
        else
        {
            HandleAutoExpansion(dropTargets, args.position);
            ApplyDragAndDropUI(dropTargets, args.insertAtIndex);
        }

        return dropTargets.dragVisualMode;
    }

    HierarchyViewDragAndDropTargets GetVisualMode(in HandleDragAndDropArgs args)
    {
        if (args.insertAtIndex < 0)
            return HierarchyViewDragAndDropTargets.Rejected;

        var dragAndDropTargets = GetDragAndDropTargets(in args);
        var parentNode = dragAndDropTargets.parentIndex == k_InvalidIndex ? Source.Root : ViewModel[dragAndDropTargets.parentIndex];

        if (DragSourceIsCurrentListView(args) && HandleDefaultCanDrop(in args, dragAndDropTargets, in parentNode).dragVisualMode != DragVisualMode.Rejected)
        {
            // Internal drag: default validation passes (AcceptParent/AcceptChild), do handler-specific validation.
            return HandleNodeHandlersCanReorder(dragAndDropTargets, args.insertAtIndex, args.dragAndDropData, in parentNode);
        }
        else
        {
            // External drag: handlers get first chance, then default validation.
            // Or fallback: some handlers (e.g. StyleSheetEditingNodeTypeHandler) own their internal
            // drag logic entirely and have AcceptParent=false to prevent generic reparenting.
            // Give them a chance to claim the drag via CanAcceptDrop.
            var visualMode = HandleNodeHandlersAcceptDrop(dragAndDropTargets, args.insertAtIndex, args.dragAndDropData, in parentNode, false);
            dragAndDropTargets.dragVisualMode = visualMode;
            if (dragAndDropTargets.dragVisualMode == DragVisualMode.None)
                return HierarchyViewDragAndDropTargets.Rejected;

            return dragAndDropTargets;
        }
    }

    HierarchyViewDragAndDropTargets HandleDefaultCanDrop(in HandleDragAndDropArgs args, HierarchyViewDragAndDropTargets dragAndDropTargets, in HierarchyNode parentNode)
    {
        // If the source is outside the view, reject by default since we need nodes to do the default CanDrop.
        if (!DragSourceIsCurrentListView(args))
            return HierarchyViewDragAndDropTargets.Rejected;

        using var draggedNodes = new RentSpanUnmanaged<HierarchyNode>(ViewModel.HasFlagsCount(HierarchyNodeFlags.Selected));

        ViewModel.GetNodesWithFlags(HierarchyNodeFlags.Selected, draggedNodes);

        var parentNodeTypeHandler = parentNode == Source.Root ? null : ViewModel.GetNodeTypeHandler(in parentNode) as IHierarchyEditorNodeTypeHandler;
        for (var i = 0; i < draggedNodes.Span.Length; ++i)
        {
            var draggedNode = draggedNodes.Span[i];
            if (IsDescendant(parentNode, draggedNode))
                return HierarchyViewDragAndDropTargets.Rejected;

            var draggedNodeTypeHandler = ViewModel.GetNodeTypeHandler(in draggedNode) as IHierarchyEditorNodeTypeHandler;
            if (!parentNodeTypeHandler?.AcceptChild(m_HierarchyView, in draggedNode) ?? false)
                return HierarchyViewDragAndDropTargets.Rejected;
            if (!draggedNodeTypeHandler?.AcceptParent(m_HierarchyView, in parentNode) ?? false)
                return HierarchyViewDragAndDropTargets.Rejected;
        }

        // If all handlers accepts the parent and children, then we can accept the drop.
        dragAndDropTargets.dragVisualMode = DragVisualMode.Move;
        return dragAndDropTargets;
    }

    bool IsDescendant(in HierarchyNode possibleDescendant, in HierarchyNode target)
    {
        // We want to check if the possibleDescendant is a descendant of the node.
        if (ViewModel.GetDepth(possibleDescendant) <= ViewModel.GetDepth(target))
            return false;

        var n = ViewModel.GetParent(possibleDescendant);
        while (n != HierarchyNode.Null)
        {
            if (n == target)
                return true;
            n = ViewModel.GetParent(n);
        }

        return false;
    }

    HierarchyViewDragAndDropHandlingData BuildHandlingData(HierarchyViewDragAndDropTargets dragAndDropTargets, int insertAtIndex, DragAndDropData dragAndDropData, in HierarchyNode parentNode)
    {
        var targetNode = (insertAtIndex == k_InvalidIndex) || (insertAtIndex >= ViewModel.Count) ? HierarchyNode.Null : ViewModel[insertAtIndex];
        return new HierarchyViewDragAndDropHandlingData(parentNode, targetNode, insertAtIndex, dragAndDropTargets.childIndex, dragAndDropTargets.dropPosition, dragAndDropData, m_HierarchyView, m_CurrentEventModifiers);
    }

    // Asks participating handlers whether the current internal drag (reorder/reparent) is allowed.
    // Returns Rejected if any handler vetoes; otherwise the last non-None visual mode returned across
    // handlers wins (default Move when no handler expresses an opinion - None would otherwise be
    // interpreted by the framework as "no drop possible").
    HierarchyViewDragAndDropTargets HandleNodeHandlersCanReorder(HierarchyViewDragAndDropTargets dragAndDropTargets, int insertAtIndex, DragAndDropData dragAndDropData, in HierarchyNode parentNode)
    {
        var resultMode = DragVisualMode.Move;
        var handlingData = BuildHandlingData(dragAndDropTargets, insertAtIndex, dragAndDropData, in parentNode);
        foreach (var editorHandler in m_ParticipatingHandlers)
        {
            var visualMode = editorHandler.CanReorder(handlingData);
            if (visualMode == DragVisualMode.Rejected)
            {
                dragAndDropTargets.dragVisualMode = DragVisualMode.Rejected;
                return dragAndDropTargets;
            }
            if (visualMode != DragVisualMode.None)
                resultMode = visualMode;
        }

        dragAndDropTargets.dragVisualMode = resultMode;
        return dragAndDropTargets;
    }

    // Calls OnReorder on participating handlers after SetParentOfSelection has moved the hierarchy
    // nodes. Each handler syncs its underlying objects (Transform, Scene, etc.). Sort index is
    // handler-owned; handlers update it during their own ViewModelPostUpdate cycle.
    void HandleNodeHandlersOnReorder(HierarchyViewDragAndDropTargets dragAndDropTargets, int insertAtIndex, DragAndDropData dragAndDropData, in HierarchyNode parentNode)
    {
        var handlingData = BuildHandlingData(dragAndDropTargets, insertAtIndex, dragAndDropData, in parentNode);
        foreach (var editorHandler in m_ParticipatingHandlers)
        {
            editorHandler.OnReorder(handlingData);
        }
    }

    // Asks (perform=false) or executes (perform=true) the accept-drop flow for an external drop.
    // All handlers iterate so each can claim its own slice of the payload. Rejected from any handler
    // short-circuits as a hard veto. Otherwise the last non-None visual mode wins.
    DragVisualMode HandleNodeHandlersAcceptDrop(HierarchyViewDragAndDropTargets dragAndDropTargets, int insertAtIndex, DragAndDropData dragAndDropData, in HierarchyNode parentNode, bool perform)
    {
        var handlingData = BuildHandlingData(dragAndDropTargets, insertAtIndex, dragAndDropData, in parentNode);
        var resultMode = DragVisualMode.None;
        foreach (var handler in Source.EnumerateNodeTypeHandlers())
        {
            if (handler is not IHierarchyEditorNodeTypeHandler editorHandler)
                continue;

            var visualMode = perform ? editorHandler.OnAcceptDrop(handlingData) : editorHandler.CanAcceptDrop(handlingData);
            if (visualMode == DragVisualMode.Rejected)
            {
                return DragVisualMode.Rejected;
            }
            if (visualMode != DragVisualMode.None)
                resultMode = visualMode;
        }

        return resultMode;
    }

    DragVisualMode HandleDrop(HandleDragAndDropArgs args)
    {
        ClearDragAndDropUI();

        var dragAndDropTargets = GetDragAndDropTargets(in args);
        var result = ExecuteDrop(in args, in dragAndDropTargets, out var didInternalReorder);

        m_ParticipatingHandlers.Clear();
        if (result == DragVisualMode.Rejected || result == DragVisualMode.None)
        {
            // Drop was not accepted: revert any nodes that auto-expanded during hover.
            ClearAutoExpansionData(restoreState: true);
        }
        else
        {
            // Keep auto-expanded ancestors. Only auto expand the parent when doing an internal
            // reorder, because we know there is going to be nodes under the parent. For external
            // drops or fallbacks, if the drop produced a selected node under it, Frame() in the post-update action below will
            // expand the necessary ancestors. Otherwise, we cannot be sure that what is dropped actually added children
            // under the parent, so we don't want to expand it and risk an unwanted expansion on a successful drop.
            if (didInternalReorder)
            {
                var parentNode = dragAndDropTargets.parentIndex == k_InvalidIndex ? HierarchyNode.Null : ViewModel[dragAndDropTargets.parentIndex];
                if (parentNode != HierarchyNode.Null)
                    ViewModel.SetFlags(in parentNode, HierarchyNodeFlags.Expanded);
            }
            ClearAutoExpansionData(restoreState: false);
        }

        // Frame the first selected node so ancestors are expanded and the node is scrolled into view.
        // Runs after all deferred post-update actions (e.g. handler-enqueued SetSelection), which is
        // required for external OverItem drops where selection is deferred via EnqueuePostUpdateAction.
        if (result != DragVisualMode.Rejected && result != DragVisualMode.None)
        {
            m_HierarchyView.EnqueuePostUpdateAction(() =>
            {
                foreach (ref readonly var node in ViewModel.EnumerateNodesWithFlags(HierarchyNodeFlags.Selected))
                {
                    if (node == HierarchyNode.Null || node == Source.Root)
                        continue;

                    m_HierarchyView.Frame(in node);
                    break;
                }
            });
        }

        return result;
    }

    DragVisualMode ExecuteDrop(in HandleDragAndDropArgs args, in HierarchyViewDragAndDropTargets dragAndDropTargets, out bool didInternalReorder)
    {
        didInternalReorder = false;
        if (args.insertAtIndex < 0)
            return DragVisualMode.Rejected;

        // If the drag and drop operation was already rejected during DragAndDropUpdate, return Rejected immediately.
        if (args.dragAndDropData.visualMode == DragVisualMode.Rejected)
            return DragVisualMode.Rejected;

        var parentNode = dragAndDropTargets.parentIndex == k_InvalidIndex ? Source.Root : ViewModel[dragAndDropTargets.parentIndex];

        DragVisualMode result;

        if (DragSourceIsCurrentListView(args) && HandleDefaultCanDrop(in args, dragAndDropTargets, in parentNode).dragVisualMode != DragVisualMode.Rejected)
        {
            // Internal drag: default validation passes (AcceptParent/AcceptChild), do handler-specific validation.
            var reorderCanDrop = HandleNodeHandlersCanReorder(dragAndDropTargets, args.insertAtIndex, args.dragAndDropData, in parentNode);
            if (reorderCanDrop.dragVisualMode == DragVisualMode.Rejected)
                return DragVisualMode.Rejected;

            // Move nodes to the target position in the hierarchy.
            if (parentNode == HierarchyNode.Null)
                return DragVisualMode.Rejected;

            if (HierarchyUndoManager.IsUndoRedoSupported())
            {
                using var _draggedNodes = ListPool<HierarchyNode>.Get(out var draggedNodes);
                using var _preParents = ListPool<HierarchyNode>.Get(out var preParents);
                using var _preChildIndices = ListPool<int>.Get(out var preChildIndices);
                foreach (ref readonly var node in ViewModel.EnumerateNodesWithFlags(HierarchyNodeFlags.Selected))
                {
                    draggedNodes.Add(node);
                    preParents.Add(ViewModel.GetParent(in node));
                    preChildIndices.Add(ViewModel.GetChildIndex(in node));
                }

                ViewModel.SetParentOfSelection(in parentNode, dragAndDropTargets.childIndex);

                if (draggedNodes.Count > 0)
                {
                    // We get the child indices from the Source since SetParentOfSelection only changes the Hierarchy.
                    using var _postChildIndices = ListPool<int>.Get(out var postChildIndices);
                    for (int i = 0; i < draggedNodes.Count; ++i)
                        postChildIndices.Add(Source.GetChildIndex(draggedNodes[i]));

                    HierarchyUndoManager.RegisterChildIndexUndo(
                        Source,
                        NoAllocHelpers.CreateReadOnlySpan(draggedNodes),
                        NoAllocHelpers.CreateReadOnlySpan(preParents),
                        NoAllocHelpers.CreateReadOnlySpan(preChildIndices),
                        in parentNode,
                        NoAllocHelpers.CreateReadOnlySpan(postChildIndices),
                        "Reorder Hierarchy");
                }
            }
            else
            {
                ViewModel.SetParentOfSelection(in parentNode, dragAndDropTargets.childIndex);
            }

            // Notify handlers so they can sync their underlying objects (Transform, Scene, etc.).
            HandleNodeHandlersOnReorder(dragAndDropTargets, args.insertAtIndex, args.dragAndDropData, in parentNode);

            result = reorderCanDrop.dragVisualMode;
            didInternalReorder = true;
        }
        else
        {
            // External drag or handler-owned internal drag (AcceptParent=false): delegate entirely to handlers.

            // Let handlers perform the drop (e.g. instantiate a prefab into the scene).
            var viewModelVersion = ViewModel.Version;
            var visualMode = HandleNodeHandlersAcceptDrop(dragAndDropTargets, args.insertAtIndex, args.dragAndDropData, in parentNode, true);
            var vmVersionUnchanged = ViewModel.Version == viewModelVersion;

            // Run the hierarchy update; handlers write Add commands to the stream, which is executed then cleared.
            var cmdList = Source.GetCommandList();
            var startOffset = cmdList.ReadPosition;
            Source.Update();

            if (visualMode == DragVisualMode.Rejected || visualMode == DragVisualMode.None || !vmVersionUnchanged)
                return DragVisualMode.Rejected;

            // Recover newly added nodes by scanning the buffer bytes written during the update.
            // Filter to nodes placed directly under parentNode — handlers may add nodes elsewhere.
            var newNodes = cmdList.ScanAddedNodes(startOffset, cmdList.LastWritePosition);
            using var _newNodes = ListPool<HierarchyNode>.Get(out var newNodesUnderParent);
            foreach (var node in newNodes)
                if (Source.GetParent(in node) == parentNode)
                    newNodesUnderParent.Add(node);

            // Reposition the new nodes to the exact drop point requested by the user.
            if (newNodesUnderParent.Count > 0 && dragAndDropTargets.dropPosition == DragAndDropPosition.BetweenItems)
            {
                var newNodesSpan = NoAllocHelpers.CreateReadOnlySpan(newNodesUnderParent);
                Source.SetParent(newNodesSpan, in parentNode, dragAndDropTargets.childIndex);

                if (HierarchyUndoManager.IsUndoRedoSupported())
                {
                    // Register an undo operation for the new nodes' child indices under the parent,
                    // so that they are restored to the new positions in case of a redo (an undo would delete those nodes).
                    // We get the child indices from the Source since SetParent is done on the Hierarchy.
                    using var _postChildIndices = ListPool<int>.Get(out var postChildIndices);
                    for (int i = 0; i < newNodesUnderParent.Count; ++i)
                        postChildIndices.Add(Source.GetChildIndex(newNodesUnderParent[i]));
                    HierarchyUndoManager.RegisterChildIndexUndo(
                        Source,
                        newNodesSpan,
                        Array.Empty<HierarchyNode>(),
                        Array.Empty<int>(),
                        in parentNode,
                        NoAllocHelpers.CreateReadOnlySpan(postChildIndices),
                        "Reorder Hierarchy");
                }

                var firstRoot = newNodesUnderParent[0];
                m_HierarchyView.EnqueuePostUpdateAction(() => m_HierarchyView.Frame(in firstRoot));
            }

            result = visualMode;
        }

        return result;
    }

    HierarchyViewDragAndDropTargets GetDragAndDropTargets(in HandleDragAndDropArgs args)
    {
        return HandleTreePosition(args);
    }

    bool DragSourceIsCurrentListView(in HandleDragAndDropArgs args)
    {
        return args.dragAndDropData.source == m_CollectionView;
    }

    HierarchyViewDragAndDropTargets HandleTreePosition(in HandleDragAndDropArgs dnDArgs)
    {
        m_LeftIndentation = -1f;
        m_SiblingBottom = -1f;

        // Already handled. Same behavior as ListViewDragger.
        if (dnDArgs.insertAtIndex < 0)
            return new HierarchyViewDragAndDropTargets(k_InvalidIndex, k_InvalidIndex, DragAndDropPosition.OutsideItems);

        // Insert inside an item, as the last child.
        if (dnDArgs.dropPosition == DragAndDropPosition.OverItem)
        {
            var overParentNode = ViewModel[dnDArgs.insertAtIndex];
            return new HierarchyViewDragAndDropTargets(dnDArgs.insertAtIndex, ViewModel.GetChildrenCount(in overParentNode), DragAndDropPosition.OverItem);
        }

        // Above first row.
        if (dnDArgs.insertAtIndex == 0)
        {
            m_LeftIndentation = GetFirstRowIndentOffset();
            return new HierarchyViewDragAndDropTargets(k_InvalidIndex, 0, DragAndDropPosition.BetweenItems);
        }

        // Below last row.
        var indexFromPosition = m_HierarchyView.GetIndexFromWorldPosition(dnDArgs.position, k_HalfDropBetweenHeight);
        if (indexFromPosition >= ViewModel.Count)
        {
            m_LeftIndentation = GetFirstRowIndentOffset();
            return new HierarchyViewDragAndDropTargets(k_InvalidIndex, ViewModel.GetChildrenCount(Source.Root), DragAndDropPosition.OutsideItems);
        }

        return HandleSiblingInsertionAtAvailableDepthsAndChangeTargetIfNeeded(dnDArgs);
    }

    float GetFirstRowIndentOffset() => m_CollectionView.GetRootElementForIndex(0)?.Q<HierarchyViewItem>()?.IndentOffset ?? -1f;

    HierarchyViewDragAndDropTargets HandleSiblingInsertionAtAvailableDepthsAndChangeTargetIfNeeded(in HandleDragAndDropArgs dnDArgs)
    {
        var targetIndex = dnDArgs.insertAtIndex;
        if (targetIndex == k_InvalidIndex)
            return HierarchyViewDragAndDropTargets.Rejected;

        // Right below the last node, but not considered as outside items
        bool belowLastNode = targetIndex >= ViewModel.Count;
        if (belowLastNode)
            targetIndex = ViewModel.Count - 1;

        var targetRootElement = m_CollectionView.GetRootElementForIndex(targetIndex);
        if (targetRootElement == null)
            return HierarchyViewDragAndDropTargets.Rejected;

        var targetViewItem = targetRootElement.Q<HierarchyViewItem>();
        if (targetViewItem == null)
            return HierarchyViewDragAndDropTargets.Rejected;

        var targetNode = targetViewItem.Node;
        if (targetNode == HierarchyNode.Null)
            return HierarchyViewDragAndDropTargets.Rejected;

        var parentNode = ViewModel.GetParent(in targetNode);
        if (parentNode == HierarchyNode.Null)
            return HierarchyViewDragAndDropTargets.Rejected;

        var localPosition = targetViewItem.WorldToLocal(dnDArgs.position);
        if (localPosition.x < 0 || localPosition.x > targetViewItem.layout.width)
            return HierarchyViewDragAndDropTargets.Rejected;

        var parentIndex = ViewModel.IndexOf(in parentNode);
        var childIndex = ViewModel.GetChildIndex(in targetNode);

        var targetDepth = ViewModel.GetDepth(in targetNode);
        var indentOffset = targetViewItem.IndentOffset;
        var indentWidth = targetViewItem.IndentWidth;

        var draggingFromHierarchy = DragSourceIsCurrentListView(in dnDArgs); // Ignore selection when dragging from outside the hierarchy.

        // Reset target index if below last node, so we can use it to compute the depth of the pointer and the surrounding nodes.
        if (belowLastNode)
            targetIndex = ViewModel.Count;

        // Get the depth of the pointer and the surrounding nodes.
        var pointerDepth = Mathf.FloorToInt((localPosition.x - indentOffset) / HierarchyViewItem.k_IndentWidth);
        var previousNode = targetIndex > 0 ? ViewModel[targetIndex - 1] : Source.Root;
        var previousNodeDepth = ViewModel.GetDepth(in previousNode);
        var nextNodeDepth = 0;
        if (!belowLastNode)
            nextNodeDepth = ViewModel.GetDepth(in targetNode);
        var maxDepth = Math.Max(previousNodeDepth, nextNodeDepth);

        // Handle horizontal drag to the left (at least one depth level shallower than the max depth)
        if (pointerDepth < maxDepth && (!belowLastNode || pointerDepth < targetDepth))
        {
            // If the previous node has a greater depth than the target node, assume the target node is the previous node
            // for the purpose of computing the parent node. Otherwise, we get the wrong "lastNonSelectedChild".
            if (previousNodeDepth > targetDepth && previousNode != HierarchyNode.Null)
            {
                parentNode = ViewModel.GetParent(in previousNode);
            }

            var previousNonSelectedNode = Source.Root;
            if (targetIndex > 0)
                previousNonSelectedNode = draggingFromHierarchy ? FindFirsNonSelectedNodeBackward(targetIndex - 1) : ViewModel[targetIndex - 1];
            var lastNonSelectedChild = draggingFromHierarchy ? GetLastNonSelectedChild(in parentNode) : GetLastChild(in parentNode);
            // Allow only if the previous non-selected node is the last child of its parent, or the parent itself if and only if
            // it has children and they are all selected.
            if (lastNonSelectedChild == previousNonSelectedNode ||
                (parentNode == previousNonSelectedNode && lastNonSelectedChild == HierarchyNode.Null))
            {
                var nextNonSelectedNodeDepth = 0;
                if (!belowLastNode)
                {
                    var nextNonSelectedNode = draggingFromHierarchy ? FindFirsNonSelectedNodeForward(targetIndex) : ViewModel[targetIndex];
                    if (nextNonSelectedNode != HierarchyNode.Null)
                        nextNonSelectedNodeDepth = ViewModel.GetDepth(in nextNonSelectedNode);
                }

                // We cannot drag to a depth smaller than the next non selected node, which acts as the next sibling.
                if (pointerDepth < nextNonSelectedNodeDepth)
                    pointerDepth = nextNonSelectedNodeDepth;

                parentNode = FindAncestorAtDepth(parentNode, pointerDepth - 1);
                if (parentNode == HierarchyNode.Null)
                    return HierarchyViewDragAndDropTargets.Rejected;

                var firstChildNode = FindFirstChildBackward(in parentNode, targetIndex - 1);
                if (firstChildNode == HierarchyNode.Null)
                    return HierarchyViewDragAndDropTargets.Rejected;

                parentIndex = ViewModel.IndexOf(in parentNode);
                childIndex = ViewModel.GetChildIndex(in firstChildNode) + 1;
                indentWidth = HierarchyViewItem.k_IndentWidth * pointerDepth;
                m_SiblingBottom = GetNodeViewItemBottom(in firstChildNode);
            }
        }

        // Handle normal drag or horizontal drag to the right (at least one depth level deeper than the max depth)
        else
        {
            var previousNonSelectedNode = Source.Root;
            if (targetIndex > 0)
                previousNonSelectedNode = draggingFromHierarchy ? FindFirsNonSelectedNodeBackward(targetIndex - 1) : ViewModel[targetIndex - 1];

            // Find the real parent that receives the drop based on the pointer depth
            var dropParent = previousNonSelectedNode;
            var dropParentDepth = ViewModel.GetDepth(in dropParent);
            if (dropParentDepth >= pointerDepth ||
                ViewModel.GetChildrenCount(in dropParent) == 0 ||
                ViewModel.DoesNotHaveFlags(in dropParent, HierarchyNodeFlags.Expanded))
            {
                dropParent = FindAncestorAtDepth(dropParent, pointerDepth - 1);
                dropParentDepth = ViewModel.GetDepth(in dropParent);
            }

            var lastChild = GetLastChild(in dropParent);
            if (ViewModel.IndexOf(in lastChild) >= targetIndex && !belowLastNode)
            {
                // Find the child index of the next node under dropParent, which might be different that the target node
                // if the drop parent is an ancestor of the parent of the target node (i.e. the parent of the target node is selected)
                var childTarget = FindFirstChildForward(in dropParent, targetIndex);
                childIndex = ViewModel.GetChildIndex(in childTarget); // Insert at child target position
            }
            else
            {
                childIndex = ViewModel.GetChildrenCount(in dropParent); // Insert as last child of parent
            }

            parentIndex = ViewModel.IndexOf(in dropParent);
            indentWidth = HierarchyViewItem.k_IndentWidth * (dropParentDepth + 1);
        }

        m_LeftIndentation = indentOffset + indentWidth;
        return new HierarchyViewDragAndDropTargets(parentIndex, childIndex, DragAndDropPosition.BetweenItems);
    }

    /// <summary>
    /// Gets the last child of the specified parent node.
    /// </summary>
    HierarchyNode GetLastChild(in HierarchyNode parentNode)
    {
        var childrenCount = ViewModel.GetChildrenCount(in parentNode);
        return childrenCount > 0 ? ViewModel.GetChild(in parentNode, childrenCount - 1) : HierarchyNode.Null;
    }

    /// <summary>
    /// Gets the last non-selected child of the specified parent node. If all children are selected or there is no child, returns null.
    /// </summary>
    /// <param name="parentNode">The parent node.</param>
    /// <returns>The last non selected child, if any.</returns>
    HierarchyNode GetLastNonSelectedChild(in HierarchyNode parentNode)
    {
        var childrenCount = ViewModel.GetChildrenCount(in parentNode);
        if (childrenCount == 0)
            return HierarchyNode.Null;

        for (var i = childrenCount - 1; i >= 0; --i)
        {
            var child = ViewModel.GetChild(in parentNode, i);
            if (ViewModel.DoesNotHaveFlags(in child, HierarchyNodeFlags.Selected))
                return child;
        }

        return HierarchyNode.Null;
    }

    /// <summary>
    /// Finds the ancestor of the specified node at the specified depth.
    /// </summary>
    HierarchyNode FindAncestorAtDepth(HierarchyNode parentNode, int depth)
    {
        while (parentNode != HierarchyNode.Null && parentNode != Source.Root)
        {
            parentNode = ViewModel.GetParent(in parentNode);
            if (ViewModel.GetDepth(in parentNode) <= depth)
                break;
        }
        return parentNode;
    }

    /// <summary>
    /// Finds the first node with the specified parent node, starting from the specified index and going backwards.
    /// </summary>
    HierarchyNode FindFirstChildBackward(in HierarchyNode parentNode, int index)
    {
        while (index >= 0)
        {
            var node = ViewModel[index--];
            if (ViewModel.GetParent(node) == parentNode)
                return node;
        }
        return HierarchyNode.Null;
    }

    /// <summary>
    /// Finds the first node with the specified parent node, starting from the specified index and going forwards.
    /// </summary>
    HierarchyNode FindFirstChildForward(in HierarchyNode parentNode, int index)
    {
        while (index < ViewModel.Count)
        {
            var node = ViewModel[index++];
            if (ViewModel.GetParent(node) == parentNode)
                return node;
        }
        return HierarchyNode.Null;
    }

    /// <summary>
    /// Finds the first non-selected node, starting from the specified index and going backwards.
    /// </summary>
    /// <param name="index">Starting index.</param>
    /// <returns>The first non-selected node.</returns>
    HierarchyNode FindFirsNonSelectedNodeBackward(int index)
    {
        while (index >= 0)
        {
            var node = ViewModel[index];
            if (ViewModel.DoesNotHaveFlags(in node, HierarchyNodeFlags.Selected))
                return node;
            --index;
        }
        return Source.Root;
    }

    /// <summary>
    /// Finds the first non-selected node, starting from the specified index and going forwards.
    /// </summary>
    /// <param name="index">Starting index.</param>
    /// <returns>The first non-selected node.</returns>
    HierarchyNode FindFirsNonSelectedNodeForward(int index)
    {
        while (index < ViewModel.Count)
        {
            var node = ViewModel[index];
            if (ViewModel.DoesNotHaveFlags(in node, HierarchyNodeFlags.Selected))
                return node;
            ++index;
        }
        return HierarchyNode.Null;
    }

    /// <summary>
    /// Gets the bottom position of the specified node in the view.
    /// </summary>
    float GetNodeViewItemBottom(in HierarchyNode node)
    {
        var nodeIndex = ViewModel.IndexOf(in node);
        if (nodeIndex < 0)
            return -1f;

        var nodeElement = m_CollectionView.GetRootElementForIndex(nodeIndex);
        if (nodeElement == null)
            return -1f;

        var contentViewport = m_CollectionView.scrollView.viewport;
        var elementBounds = contentViewport.WorldToLocal(nodeElement.worldBound);
        return contentViewport.localBound.yMin < elementBounds.yMax && elementBounds.yMax < contentViewport.localBound.yMax ? elementBounds.yMax : -1f;
    }

    void ApplyDragAndDropUI(HierarchyViewDragAndDropTargets dragTargets, int insertAtIndex)
    {
        if (m_LastDragPosition.Equals(dragTargets))
            return;

        var scrollView = m_CollectionView.scrollView;

        if (m_DragHoverBar == null)
        {
            m_DragHoverBar = new VisualElement();
            m_DragHoverBar.SetName(DragHoverBarItemName);
            m_DragHoverBar.AddToClassList(BaseVerticalCollectionView.dragHoverBarUssClassNameUnique);
            m_DragHoverBar.AddToClassList(DragHoverBarStyleName);
            m_DragHoverBar.style.width = m_CollectionView.localBound.width;
            m_DragHoverBar.style.visibility = Visibility.Hidden;
            m_DragHoverBar.pickingMode = PickingMode.Ignore;

            void GeometryChangedCallback(GeometryChangedEvent e)
            {
                m_DragHoverBar.style.width = m_CollectionView.localBound.width;
            }

            m_CollectionView.RegisterCallback<GeometryChangedEvent>(GeometryChangedCallback);
            scrollView.viewport.Add(m_DragHoverBar);
        }

        if (m_DragHoverItemMarker == null)
        {
            m_DragHoverItemMarker = new VisualElement();
            m_DragHoverItemMarker.SetName(DragHoverItemMarkerItemName);
            m_DragHoverItemMarker.AddToClassList(BaseVerticalCollectionView.dragHoverMarkerUssClassNameUnique);
            m_DragHoverItemMarker.style.visibility = Visibility.Hidden;
            m_DragHoverItemMarker.pickingMode = PickingMode.Ignore;
            m_DragHoverBar.Add(m_DragHoverItemMarker);

            m_DragHoverSiblingMarker = new VisualElement();
            m_DragHoverSiblingMarker.SetName(DragHoverSiblingMarkerItemName);
            m_DragHoverSiblingMarker.AddToClassList(BaseVerticalCollectionView.dragHoverMarkerUssClassNameUnique);
            m_DragHoverSiblingMarker.style.visibility = Visibility.Hidden;
            m_DragHoverSiblingMarker.pickingMode = PickingMode.Ignore;
            scrollView.viewport.Add(m_DragHoverSiblingMarker);
        }

        ClearDragAndDropUI();
        m_LastDragPosition = dragTargets;
        switch (dragTargets.dropPosition)
        {
            case DragAndDropPosition.OverItem:
                break; // Do nothing
            case DragAndDropPosition.BetweenItems:
                if (insertAtIndex == 0)
                {
                    PlaceHoverBarAt(0, m_LeftIndentation);
                }
                else
                {
                    var beforeItem = m_CollectionView.GetRootElementForIndex(insertAtIndex - 1);
                    var afterItem = m_CollectionView.GetRootElementForIndex(insertAtIndex);
                    var item = beforeItem ?? afterItem;
                    if (item != null)
                        PlaceHoverBarAtElement(item);
                    else
                        PlaceHoverBarAt(0);
                }

                break;
            case DragAndDropPosition.OutsideItems:
                var recycledItem = m_CollectionView.GetRootElementForIndex(m_CollectionView.itemsSource.Count - 1);
                if (recycledItem != null)
                    PlaceHoverBarAtElement(recycledItem);
                else
                    PlaceHoverBarAt(0, m_LeftIndentation);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(dragTargets.dropPosition),
                    dragTargets.dropPosition,
                    $"Unsupported {nameof(dragTargets.dropPosition)} value");
        }
    }

    void ClearDragAndDropUI()
    {
        m_LastDragPosition = new HierarchyViewDragAndDropTargets();

        if (m_DragHoverBar != null)
            m_DragHoverBar.style.visibility = Visibility.Hidden;
        if (m_DragHoverItemMarker != null)
            m_DragHoverItemMarker.style.visibility = Visibility.Hidden;
        if (m_DragHoverSiblingMarker != null)
            m_DragHoverSiblingMarker.style.visibility = Visibility.Hidden;
    }

    void ClearDragAndDrop()
    {
        m_CurrentEventModifiers = EventModifiers.None;
        ClearDragAndDropUI();
        ClearAutoExpansionData();
        // m_ParticipatingHandlers is intentionally NOT cleared here: DragExitedEvent fires
        // when the pointer leaves the window mid-drag, but the drag may resume on re-entry.
        // SetupDragAndDrop clears+repopulates at the start of every new drag, and HandleDrop
        // clears at the end, so the cache cannot leak across drags.
    }

    void ClearAutoExpansionData(bool restoreState = true)
    {
        if (restoreState && m_AutoExpansionData?.expandedNodesBeforeDrag != null)
        {
            RestoreExpanded(m_AutoExpansionData.expandedNodesBeforeDrag);
        }
        m_AutoExpansionData = new AutoExpansionData();
        m_ExpandItemScheduledItem?.Pause();
    }

    void RestoreExpanded(ReadOnlySpan<HierarchyNode> expandedNodes)
    {
        using (var _ = new HierarchyViewModelFlagsChangeScope(ViewModel))
        {
            ViewModel.ClearFlags(HierarchyNodeFlags.Expanded);
            ViewModel.SetFlags(expandedNodes, HierarchyNodeFlags.Expanded);
        }
    }

    float GetHoverBarTopPosition(VisualElement item)
    {
        var contentViewport = m_CollectionView.scrollView.viewport;
        var elementBounds = contentViewport.WorldToLocal(item.worldBound);
        var top = Mathf.Min(elementBounds.yMax, contentViewport.localBound.yMax - k_DragHoverBarHeight);
        return top;
    }

    void PlaceHoverBarAtElement(VisualElement item)
    {
        PlaceHoverBarAt(GetHoverBarTopPosition(item), m_LeftIndentation, m_SiblingBottom);
    }

    void PlaceHoverBarAt(float top, float indentationPadding = -1f, float siblingBottom = -1f)
    {
        m_DragHoverBar.style.top = top;
        m_DragHoverBar.style.visibility = Visibility.Visible;

        var nameColumnLayout = GetNameColumnLayout();

        var baseColumnOffset = nameColumnLayout.xMin;
        var width = m_CollectionView.localBound.width;
        if (nameColumnLayout.width > 0f)
            width = nameColumnLayout.width;
        else
            indentationPadding = -1f;

        if (m_DragHoverItemMarker != null)
        {
            m_DragHoverItemMarker.style.visibility = Visibility.Visible;
        }

        if (indentationPadding >= 0)
        {
            m_DragHoverBar.style.marginLeft = baseColumnOffset + indentationPadding;
            m_DragHoverBar.style.width = width - indentationPadding;

            if (siblingBottom > 0 && m_DragHoverSiblingMarker != null)
            {
                m_DragHoverSiblingMarker.style.top = siblingBottom;
                m_DragHoverSiblingMarker.style.visibility = Visibility.Visible;
                m_DragHoverSiblingMarker.style.marginLeft = baseColumnOffset + indentationPadding;
            }
        }
        else
        {
            m_DragHoverBar.style.marginLeft = baseColumnOffset;
            m_DragHoverBar.style.width = width;
        }
    }

    VisualElement GetNameColumn()
    {
        return m_CollectionView.Q(HierarchyViewColumnName.k_HierarchyNameColumnName);
    }

    Rect GetNameColumnLayout()
    {
        var nameColumn = GetNameColumn();
        if (nameColumn == null)
            return Rect.zero;
        return nameColumn.layout;
    }

    void HandleAutoExpansion(HierarchyViewDragAndDropTargets dropTargets, Vector2 pointerPosition)
    {
        if (dropTargets.dropPosition != DragAndDropPosition.OverItem)
            return;

        var itemIndex = dropTargets.parentIndex;
        var item = m_CollectionView.GetRootElementForIndex(itemIndex);
        if (item == null)
            return;

        HandleAutoExpansion(item, itemIndex, pointerPosition);
    }

    void HandleAutoExpansion(VisualElement item, int itemIndex, Vector2 pointerPosition)
    {
        var targetItemRect = item.worldBound;

        // Handle auto expansion
        var indentedContentRect = new Rect(targetItemRect.x, targetItemRect.y + k_HalfDropBetweenHeight, targetItemRect.width, targetItemRect.height - k_HalfDropBetweenHeight * 2);
        var hoveringOverIndentedContent = indentedContentRect.Contains(pointerPosition);
        var deltaPosition = m_AutoExpansionData.expandItemBeginPosition - pointerPosition;

        if (itemIndex != m_AutoExpansionData.lastItemIndex || !hoveringOverIndentedContent || deltaPosition.sqrMagnitude >= k_DropDeltaPosition)
        {
            m_AutoExpansionData.lastItemIndex = itemIndex;
            m_AutoExpansionData.expandItemBeginTimerMs = 0;
            m_AutoExpansionData.expandItemBeginPosition = pointerPosition;
            DelayExpandItem();
        }
    }

    void DelayExpandItem()
    {
        if (m_ExpandItemScheduledItem == null)
        {
            m_ExpandItemScheduledItem = m_CollectionView.schedule.Execute(ExpandItem).Every(k_ExpandUpdateIntervalMs);
        }
        else
        {
            m_ExpandItemScheduledItem.Pause();
            m_ExpandItemScheduledItem.Resume();
        }
    }

    internal void ExpandItem(TimerState state)
    {
        m_AutoExpansionData.expandItemBeginTimerMs = state.deltaTime + m_AutoExpansionData.expandItemBeginTimerMs;
        var expandTimerExpired = m_AutoExpansionData.expandItemBeginTimerMs > k_DropExpandTimeoutMs;
        var itemIndex = m_AutoExpansionData.lastItemIndex;

        // Auto open folders we are about to drag into
        if (expandTimerExpired && itemIndex >= 0 && itemIndex < ViewModel.Count)
        {
            var node = ViewModel[itemIndex];
            var hasChildren = ViewModel.GetChildrenCount(in node) > 0;
            var isExpanded = ViewModel.HasFlags(in node, HierarchyNodeFlags.Expanded);

            if (!hasChildren || isExpanded)
                return;

            // todo: fix this so we don't allocate every time
            var nodes = ViewModel.GetNodesWithFlags(HierarchyNodeFlags.Expanded);

            // Store the expanded array prior to drag so we can revert it with a delay later
            m_AutoExpansionData.expandedNodesBeforeDrag ??= nodes;
            m_AutoExpansionData.expandItemBeginTimerMs = 0;
            m_AutoExpansionData.lastItemIndex = -1;

            m_HierarchyView.SetExpandedState(in node, true, false);
        }
    }
}
