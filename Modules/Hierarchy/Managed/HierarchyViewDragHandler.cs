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
        public int insertAtIndex;
        public int targetIndex;
        public int parentIndex;
        public int childIndex;
        public DragAndDropPosition dropPosition;
        public DragVisualMode dragVisualMode;

        [NoAutoStaticsCleanup]
        public static readonly HierarchyViewDragAndDropTargets Rejected = new(k_InvalidIndex, k_InvalidIndex, k_InvalidIndex, k_InvalidIndex, DragAndDropPosition.OverItem) { dragVisualMode = DragVisualMode.Rejected };

        public HierarchyViewDragAndDropTargets(int insertAtIndex, int targetIndex, int parentIndex, int childIndex, DragAndDropPosition dropPosition)
        {
            this.insertAtIndex = insertAtIndex;
            this.targetIndex = targetIndex;
            this.parentIndex = parentIndex;
            this.childIndex = childIndex;
            this.dropPosition = dropPosition;
            this.dragVisualMode = DragVisualMode.Move;
        }

        public bool Equals(HierarchyViewDragAndDropTargets other)
        {
            return parentIndex == other.parentIndex && childIndex == other.childIndex && dropPosition == other.dropPosition && targetIndex == other.targetIndex;
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

    internal const string DragHoverBarStyleName = "hierarchy__container__drag-hover-bar";
    internal const string DragHoverBarItemName = "HierarchyHoverBar";
    internal const string DragHoverItemMarkerItemName = "HierarchyHoverItemMarker";
    internal const string DragHoverSiblingMarkerItemName = "HierarchyHoverSiblingMarker";

    readonly HierarchyView m_HierarchyView;
    readonly CollectionView m_CollectionView;

    HierarchyViewDragAndDropTargets m_LastDragPosition;
    AutoExpansionData m_AutoExpansionData;
    IVisualElementScheduledItem m_ExpandItemScheduledItem;

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
    const float k_DefaultIndentWidth = 14f; // Keep in sync with uss.
    const float k_DragHoverBarPositionOffset = 4f;

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

        using var draggedNodes = new RentSpanUnmanaged<HierarchyNode>(ViewModel.HasFlagsCount(HierarchyNodeFlags.Selected));
        ViewModel.GetNodesWithFlags(HierarchyNodeFlags.Selected, draggedNodes);

        foreach (var handler in Source.EnumerateNodeTypeHandlers())
        {
            if (handler is IHierarchyEditorNodeTypeHandler editorHandler && !editorHandler.CanStartDrag(m_HierarchyView, draggedNodes))
                return false;
        }

        return true;
    }

    StartDragArgs SetupDragAndDrop(SetupDragAndDropArgs args)
    {
        var count = ViewModel.HasFlagsCount(HierarchyNodeFlags.Selected);
        var draggedNodes = new RentSpanUnmanaged<HierarchyNode>(count);
        ViewModel.GetNodesWithFlags(HierarchyNodeFlags.Selected, draggedNodes);

        var allEntityIds = new List<EntityId>();
        using var _ = ListPool<string>.Get(out var paths);
        using var _2 = DictionaryPool<string, object>.Get(out var genericData);
        var setupData = new HierarchyViewDragAndDropSetupData(draggedNodes, allEntityIds, paths, m_HierarchyView, genericData);
        foreach (var handler in Source.EnumerateNodeTypeHandlers())
        {
            if (handler is IHierarchyEditorNodeTypeHandler editorHandler)
                editorHandler.OnStartDrag(setupData);
        }

        var startDragArgs = new StartDragArgs(args.startDragArgs.title, args.startDragArgs.visualMode);

        startDragArgs.SetEntityIds(allEntityIds);
        startDragArgs.SetPaths(paths.ToArray());
        foreach (var kvp in genericData)
            startDragArgs.SetGenericData(kvp.Key, kvp.Value);

        return startDragArgs;
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
            ApplyDragAndDropUI(dropTargets);
        }

        return dropTargets.dragVisualMode;
    }

    HierarchyViewDragAndDropTargets GetVisualMode(in HandleDragAndDropArgs args)
    {
        if (args.insertAtIndex < 0)
            return HierarchyViewDragAndDropTargets.Rejected;

        var dragAndDropTargets = GetDragAndDropTargets(in args);
        var parentNode = dragAndDropTargets.parentIndex == k_InvalidIndex ? Source.Root : ViewModel[dragAndDropTargets.parentIndex];

        dragAndDropTargets = HandleNodeHandlersDrop(dragAndDropTargets, args.dragAndDropData, in parentNode, false);
        if (dragAndDropTargets.dragVisualMode != DragVisualMode.None)
            return dragAndDropTargets;

        return HandleDefaultCanDrop(in args, dragAndDropTargets, in parentNode);
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

    HierarchyViewDragAndDropTargets HandleNodeHandlersDrop(HierarchyViewDragAndDropTargets dragAndDropTargets, DragAndDropData dragAndDropData, in HierarchyNode parentNode, bool perform)
    {
        var targetNode = (dragAndDropTargets.targetIndex == k_InvalidIndex) || (dragAndDropTargets.targetIndex >= ViewModel.Count) ? HierarchyNode.Null : ViewModel[dragAndDropTargets.targetIndex];
        var handlingData = new HierarchyViewDragAndDropHandlingData(parentNode, targetNode, dragAndDropTargets.insertAtIndex, dragAndDropTargets.childIndex, dragAndDropTargets.dropPosition, dragAndDropData, m_HierarchyView, m_CurrentEventModifiers);

        foreach (var handler in Source.EnumerateNodeTypeHandlers())
        {
            if (handler is IHierarchyEditorNodeTypeHandler editorHandler)
            {
                var visualMode = perform ? editorHandler.OnDrop(handlingData) : editorHandler.CanDrop(handlingData);
                if (visualMode != DragVisualMode.None)
                {
                    dragAndDropTargets.dragVisualMode = visualMode;
                    return dragAndDropTargets;
                }
            }
        }

        // Set value to None to signal that no handler accepted the drop.
        dragAndDropTargets.dragVisualMode = DragVisualMode.None;
        return dragAndDropTargets;
    }

    DragVisualMode HandleDrop(HandleDragAndDropArgs args)
    {
        ClearDragAndDropUI();

        if (args.insertAtIndex < 0)
            return DragVisualMode.Rejected;

        var dragAndDropTargets = GetDragAndDropTargets(in args);

        var parentNode = dragAndDropTargets.parentIndex == k_InvalidIndex ? Source.Root : ViewModel[dragAndDropTargets.parentIndex];
        var viewModelVersion = ViewModel.Version;
        dragAndDropTargets = HandleNodeHandlersDrop(dragAndDropTargets, args.dragAndDropData, in parentNode, true);
        if (ViewModel.Version != viewModelVersion)
            return DragVisualMode.Rejected;
        var result = dragAndDropTargets.dragVisualMode;
        if (result == DragVisualMode.None)
            result = HandleDefaultDrop(in args, dragAndDropTargets, in parentNode);
        if (parentNode != Source.Root)
            ViewModel.SetFlags(in parentNode, HierarchyNodeFlags.Expanded);
        ClearAutoExpansionData(false);

        // When dropping new nodes, handlers can use the global selection to select their new nodes,
        // and in that case the GlobalSelectionHandler will take care of doing the framing. But in all other cases,
        // i.e. if the nodes cannot be mapped to global selection, reordering existing nodes or it's a runtime view,
        // then it needs to be handled here. The cost of framing the nodes twice should be low since framing a node
        // that is already in view does nothing.
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

    DragVisualMode HandleDefaultDrop(in HandleDragAndDropArgs args, HierarchyViewDragAndDropTargets dragAndDropTargets, in HierarchyNode parentNode)
    {
        // If the source is outside the view, reject by default since we need nodes to do the default HandleDrop.
        if (!DragSourceIsCurrentListView(args))
            return DragVisualMode.Rejected;

        // Because we do Source.GetChildren we first need to check if the source hierarchy changed while dragging
        // If it did change we can properly handle the drop and need to reject it.
        if (!Source.Exists(parentNode))
            return DragVisualMode.Rejected;

        using var rentedSpan = new RentSpanUnmanaged<HierarchyNode>(ViewModel.HasFlagsCount(HierarchyNodeFlags.Selected));
        var draggedNodes = rentedSpan.Span;
        ViewModel.GetNodesWithFlags(HierarchyNodeFlags.Selected, draggedNodes);

        var existingChildrenCount = Source.GetChildrenCount(parentNode);
        using var existingChildren = new RentSpanUnmanaged<HierarchyNode>(existingChildrenCount);
        Source.GetChildren(parentNode, existingChildren);
        var insertIndex = dragAndDropTargets.childIndex;

        // If dragging from inside the view, it is possible that the parent target is pointing to a node that is being dragged.
        // In that case, we need to remove it from the list of dragged nodes.
        for (var i = 0; i < draggedNodes.Length; ++i)
        {
            if (draggedNodes[i] == parentNode)
            {
                // Move remaining elements and reduce size by one.
                for (var j = i; j < draggedNodes.Length - 1; ++j)
                    draggedNodes[j] = draggedNodes[j + 1];
                draggedNodes = draggedNodes.Slice(0, draggedNodes.Length - 1);
                break;
            }
        }

        // Update the sorting indexes of the old siblings of the dragged nodes.
        var oldParents = new List<HierarchyNode>(draggedNodes.Length);
        var oldParentChildrenCount = new List<int>(draggedNodes.Length);
        var maxChildrenCount = 0;
        for (var i = 0; i < draggedNodes.Length; ++i)
        {
            var oldParentNode = ViewModel.GetParent(draggedNodes[i]);
            if (oldParentNode == parentNode || oldParents.Contains(oldParentNode))
                continue;
            oldParents.Add(oldParentNode);
            var childrenCount = ViewModel.GetChildrenCount(oldParentNode);
            oldParentChildrenCount.Add(childrenCount);
            maxChildrenCount = Math.Max(maxChildrenCount, childrenCount);
        }

        int currentSortIndex;
        if (oldParents.Count > 0)
        {
            var oldSiblings = new HierarchyNode[maxChildrenCount];
            for (var i = 0; i < oldParents.Count; ++i)
            {
                var oldParentNode = oldParents[i];
                Source.GetChildren(oldParentNode, oldSiblings);
                var currentChildrenCount = oldParentChildrenCount[i];

                currentSortIndex = 0;
                for (var j = 0; j < currentChildrenCount; ++j)
                {
                    var node = oldSiblings[j];
                    if (ViewModel.HasFlags(in node, HierarchyNodeFlags.Selected))
                        continue;

                    Source.SetSortIndex(in node, currentSortIndex++);
                }
            }
        }

        // todo: optimize this with a command list, probably for the whole method
        for (var i = 0; i < draggedNodes.Length; ++i)
            Source.SetParent(in draggedNodes[i], parentNode);

        // Update the sorting indexes of the children of the new parent.
        if (insertIndex == k_InvalidIndex)
            insertIndex = existingChildrenCount;

        // If dragging from inside the view, it is possible that the dragged nodes are already children of the parent node.
        // In that case, we need to skip them.
        currentSortIndex = 0;

        for (var i = 0; i < insertIndex && i < existingChildrenCount; ++i)
        {
            var node = existingChildren.Span[i];
            if (ViewModel.HasFlags(in node, HierarchyNodeFlags.Selected))
                continue;

            Source.SetSortIndex(in node, currentSortIndex++);
        }

        for (var i = 0; i < draggedNodes.Length; ++i)
        {
            Source.SetSortIndex(draggedNodes[i], currentSortIndex++);
        }

        for (var i = insertIndex; i < existingChildrenCount; ++i)
        {
            var node = existingChildren.Span[i];
            if (ViewModel.HasFlags(in node, HierarchyNodeFlags.Selected))
                continue;

            Source.SetSortIndex(in node, currentSortIndex++);
        }

        Source.SortChildren(parentNode);

        return DragVisualMode.Move;
    }

    HierarchyViewDragAndDropTargets GetDragAndDropTargets(in HandleDragAndDropArgs args)
    {
        if (DragSourceIsCurrentListView(args))
        {
            var count = ViewModel.HasFlagsCount(HierarchyNodeFlags.Selected);
            using var indices = new RentSpanUnmanaged<int>(count);
            ViewModel.GetIndicesWithFlags(HierarchyNodeFlags.Selected, indices);
            return HandleTreePosition(args, indices);
        }
        else
        {
            return HandleTreePosition(args, Array.Empty<int>());
        }
    }

    bool DragSourceIsCurrentListView(in HandleDragAndDropArgs args)
    {
        return args.dragAndDropData.source == m_CollectionView;
    }

    HierarchyViewDragAndDropTargets HandleTreePosition(in HandleDragAndDropArgs dnDArgs, in ReadOnlySpan<int> draggedIndices)
    {
        m_LeftIndentation = -1f;
        m_SiblingBottom = -1f;

        // Already handled.
        if (dnDArgs.insertAtIndex < 0)
            return new HierarchyViewDragAndDropTargets(dnDArgs.insertAtIndex, k_InvalidIndex, k_InvalidIndex, k_InvalidIndex, DragAndDropPosition.OutsideItems);

        // Insert inside an item, as the last child.
        if (dnDArgs.dropPosition == DragAndDropPosition.OverItem)
            return new HierarchyViewDragAndDropTargets(dnDArgs.insertAtIndex, dnDArgs.insertAtIndex, dnDArgs.insertAtIndex, k_InvalidIndex, DragAndDropPosition.OverItem);

        // Above first row.
        if (dnDArgs.insertAtIndex <= 0)
            return new HierarchyViewDragAndDropTargets(dnDArgs.insertAtIndex, 0, k_InvalidIndex, 0, DragAndDropPosition.BetweenItems);

        var indexFromPosition = m_HierarchyView.GetIndexFromWorldPosition(dnDArgs.position, k_HalfDropBetweenHeight);
        if (indexFromPosition >= ViewModel.Count)
            return new HierarchyViewDragAndDropTargets(dnDArgs.insertAtIndex, 0, k_InvalidIndex, k_InvalidIndex, DragAndDropPosition.OutsideItems);

        return HandleSiblingInsertionAtAvailableDepthsAndChangeTargetIfNeeded(dnDArgs, dnDArgs.position, draggedIndices);
    }

    HierarchyViewDragAndDropTargets HandleSiblingInsertionAtAvailableDepthsAndChangeTargetIfNeeded(in HandleDragAndDropArgs dnDArgs, in Vector2 pointerPosition, in ReadOnlySpan<int> draggedIndices)
    {
        var initialTargetIndex = dnDArgs.insertAtIndex;
        GetPreviousAndNextIndexesIgnoringDraggedItems(dnDArgs.insertAtIndex, out var previousNodeIndex, out var nextNodeIndex, draggedIndices);
        var dragAndDropTargets = new HierarchyViewDragAndDropTargets(dnDArgs.insertAtIndex, dnDArgs.insertAtIndex, k_InvalidIndex, k_InvalidIndex, DragAndDropPosition.BetweenItems);

        if (previousNodeIndex == k_InvalidIndex)
            return dragAndDropTargets; // Above first row so keep targetItem

        var previousNode = ViewModel[previousNodeIndex];
        var nextNode = nextNodeIndex == k_InvalidIndex ? HierarchyNode.Null : ViewModel[nextNodeIndex];

        var hoveringBetweenExpandedParentAndFirstChild = ViewModel.GetChildrenCount(in previousNode) > 0 && ViewModel.HasFlags(in previousNode, HierarchyNodeFlags.Expanded);
        var previousNodeDepth = ViewModel.GetDepth(previousNode);
        var nextNodeDepth = nextNodeIndex == k_InvalidIndex ? 0 : ViewModel.GetDepth(nextNode);
        var minDepth = nextNodeDepth;
        var maxDepth = previousNodeDepth + (hoveringBetweenExpandedParentAndFirstChild ? 1 : 0);

        // Change target item.
        var targetIndex = previousNodeIndex;
        dragAndDropTargets.targetIndex = previousNodeIndex;
        var targetNode = previousNode;
        var targetDepth = previousNodeDepth;

        // Get the indent width
        var toggleWidth = 0f;
        var indentWidth = k_DefaultIndentWidth;
        VisualElement rootElement = null;
        if (previousNodeDepth > 0)
        {
            rootElement = m_CollectionView.GetRootElementForIndex(previousNodeIndex);
        }
        else
        {
            var initialTargetNode = (dnDArgs.insertAtIndex == k_InvalidIndex || dnDArgs.insertAtIndex >= ViewModel.Count) ? HierarchyNode.Null : ViewModel[dnDArgs.insertAtIndex];
            var initialItemDepth = initialTargetNode == HierarchyNode.Null ? 0 : ViewModel.GetDepth(initialTargetNode);
            if (initialItemDepth > 0)
            {
                rootElement = m_CollectionView.GetRootElementForIndex(dnDArgs.insertAtIndex);
            }
        }

        var hierarchyViewItem = rootElement?.Q<HierarchyViewItem>();
        if (hierarchyViewItem != null)
        {
            toggleWidth = hierarchyViewItem.Toggle.layout.width;
            indentWidth = previousNodeDepth > 0 ? (hierarchyViewItem.LeftContainer.style.translate.value.x.value + k_DragHoverBarPositionOffset) / previousNodeDepth : indentWidth;
        }

        var nameColumn = GetNameColumn();
        var isMouseContainedInNameColumn = false;
        var localMousePosition = Vector2.zero;
        if (nameColumn != null)
        {
            localMousePosition = nameColumn.WorldToLocal(pointerPosition);
            isMouseContainedInNameColumn = (localMousePosition.x >= 0) && (localMousePosition.x < nameColumn.layout.width);
        }

        if (maxDepth <= minDepth)
        {
            m_LeftIndentation = toggleWidth + indentWidth * minDepth;
            if (hoveringBetweenExpandedParentAndFirstChild)
            {
                dragAndDropTargets.parentIndex = previousNodeIndex;
                dragAndDropTargets.childIndex = 0;
            }
            else
            {
                var previousNodeParent = ViewModel.GetParent(previousNode);
                dragAndDropTargets.parentIndex = ViewModel.IndexOf(previousNodeParent);
                dragAndDropTargets.childIndex = nextNodeIndex == k_InvalidIndex ? ViewModel.GetChildrenCount(previousNodeParent) : ViewModel.GetChildIndex(nextNode);
            }
            return dragAndDropTargets; // The nextItem is a descendant of previous item so keep targetItem
        }

        var cursorDepth = isMouseContainedInNameColumn ? Mathf.FloorToInt((localMousePosition.x - toggleWidth) / indentWidth) : maxDepth;
        if (cursorDepth >= maxDepth)
        {
            m_LeftIndentation = toggleWidth + indentWidth * maxDepth;
            if (hoveringBetweenExpandedParentAndFirstChild)
            {
                dragAndDropTargets.parentIndex = previousNodeIndex;
                dragAndDropTargets.childIndex = 0;
            }
            else
            {
                var previousNodeParent = ViewModel.GetParent(previousNode);
                dragAndDropTargets.parentIndex = ViewModel.IndexOf(previousNodeParent);
                dragAndDropTargets.childIndex = ViewModel.GetChildIndex(previousNode) + 1;
            }
            return dragAndDropTargets; // No need to change targetItem if same or higher depth
        }

        // Search through parents for a new target that matches the cursor
        while (targetDepth > minDepth)
        {
            if (targetDepth == cursorDepth)
                break;

            targetNode = ViewModel.GetParent(targetNode);
            targetIndex = ViewModel.IndexOf(targetNode);
            targetDepth--;
        }

        var didChangeTargetToAncestor = targetIndex != initialTargetIndex;
        if (didChangeTargetToAncestor)
        {
            var siblingRoot = m_CollectionView.GetRootElementForIndex(targetIndex);
            if (siblingRoot != null)
            {
                var contentViewport = m_CollectionView.scrollView.viewport;
                var elementBounds = contentViewport.WorldToLocal(siblingRoot.worldBound);
                if (contentViewport.localBound.yMin < elementBounds.yMax && elementBounds.yMax < contentViewport.localBound.yMax)
                {
                    m_SiblingBottom = elementBounds.yMax;
                }
            }
        }

        // Change to new target item
        var targetParentNode = ViewModel.GetParent(targetNode);
        dragAndDropTargets.parentIndex = ViewModel.IndexOf(targetParentNode);
        dragAndDropTargets.targetIndex = targetIndex;
        dragAndDropTargets.childIndex = ViewModel.GetChildIndex(targetNode) + 1;
        m_LeftIndentation = toggleWidth + indentWidth * targetDepth;
        return dragAndDropTargets;
    }

    void GetPreviousAndNextIndexesIgnoringDraggedItems(int insertAtIndex, out int previousNodeIndex, out int nextNodeIndex, in ReadOnlySpan<int> draggedIndices)
    {
        previousNodeIndex = nextNodeIndex = k_InvalidIndex;
        var possiblePreviousItemIndex = insertAtIndex - 1;
        var possibleNextItemIndex = insertAtIndex;

        while (possiblePreviousItemIndex >= 0)
        {
            if (!draggedIndices.Contains(possiblePreviousItemIndex))
            {
                previousNodeIndex = possiblePreviousItemIndex;
                break;
            }

            possiblePreviousItemIndex--;
        }

        var count = ViewModel.Count;
        while (possibleNextItemIndex < count)
        {
            if (!draggedIndices.Contains(possibleNextItemIndex))
            {
                nextNodeIndex = possibleNextItemIndex;
                break;
            }

            possibleNextItemIndex++;
        }
    }

    void ApplyDragAndDropUI(HierarchyViewDragAndDropTargets dragTargets)
    {
        if (m_LastDragPosition.Equals(dragTargets))
            return;

        var scrollView = m_CollectionView.scrollView;

        if (m_DragHoverBar == null)
        {
            m_DragHoverBar = new VisualElement() { name = DragHoverBarItemName };
            m_DragHoverBar.AddToClassList(BaseVerticalCollectionView.dragHoverBarUssClassName);
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
            m_DragHoverItemMarker = new VisualElement() { name = DragHoverItemMarkerItemName };
            m_DragHoverItemMarker.AddToClassList(BaseVerticalCollectionView.dragHoverMarkerUssClassName);
            m_DragHoverItemMarker.style.visibility = Visibility.Hidden;
            m_DragHoverItemMarker.pickingMode = PickingMode.Ignore;
            m_DragHoverBar.Add(m_DragHoverItemMarker);

            m_DragHoverSiblingMarker = new VisualElement() { name = DragHoverSiblingMarkerItemName };
            m_DragHoverSiblingMarker.AddToClassList(BaseVerticalCollectionView.dragHoverMarkerUssClassName);
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
                if (dragTargets.insertAtIndex == 0)
                {
                    PlaceHoverBarAt(0);
                }
                else
                {
                    var beforeItem = m_CollectionView.GetRootElementForIndex(dragTargets.insertAtIndex - 1);
                    var afterItem = m_CollectionView.GetRootElementForIndex(dragTargets.insertAtIndex);
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
                    PlaceHoverBarAt(0);
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

            ViewModel.SetFlags(in node, HierarchyNodeFlags.Expanded);
        }
    }
}

static class SpanExtensions
{
    public static bool Contains<T>(this in ReadOnlySpan<T> span, T value) where T : IEquatable<T>
    {
        for (var i = 0; i < span.Length; ++i)
        {
            if (span[i].Equals(value))
                return true;
        }
        return false;
    }
}
