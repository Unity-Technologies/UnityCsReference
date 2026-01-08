// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Hierarchy;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal class VisualElementEditingNodeHandler : VisualElementNodeTypeHandler, IVisualElementEditingManager
{
    private const string k_DraggedVisualElementKey = "VisualElementHandler__DraggedVisualElements";

    private VisualElementEditingStage m_Stage;
    private VisualTreeAssetEditingContext m_Context;
    private Panel m_Panel;
    private VisualElement m_LocalRoot;
    private bool m_ExpandOnInitialize = false;

    public VisualElementEditingNodeHandler()
        : base(new HierarchySelectionHandler())
    {
        isReadonly = false;
        SelectionHandler.SetEditingManager(this);
        UIElementsRuntimeUtility.onCreateAuthoringPanel += RegisterPanel;
        UIElementsRuntimeUtility.onWillDestroyAuthoringPanel += UnregisterPanel;
    }

    protected override void Dispose(bool disposing)
    {
        UIElementsRuntimeUtility.onCreateAuthoringPanel -= RegisterPanel;
        UIElementsRuntimeUtility.onWillDestroyAuthoringPanel -= UnregisterPanel;
        if (m_Stage)
            m_Stage.MainDocumentWasCloned -= StageOnMainDocumentWasCloned;
        m_Stage = null;
        base.Dispose(disposing);
    }

    public void SetContextForEditing(VisualTreeAssetEditingContext context, Panel panel)
    {
        m_Context = context;

        if (m_Panel == panel)
            return;

        if (m_Panel != null)
        {
            UnregisterPanel(m_Panel);
        }

        m_Panel = panel;

        if (m_Panel != null)
        {
            RegisterPanel(m_Panel);
            CacheLocalRoot();
            m_ExpandOnInitialize = context.SubDocumentOptions == SubDocumentOptions.InContext;
        }
    }

    protected override void Initialize()
    {
        base.Initialize();

        if (StageNavigationManager.instance.currentStage is not VisualElementEditingStage stage)
            return;

        m_Stage = stage;
        if (m_Stage)
            m_Stage.MainDocumentWasCloned += StageOnMainDocumentWasCloned;

        SetContextForEditing(stage.Context, stage.GetAuthoringPanel());
    }

    void StageOnMainDocumentWasCloned(VisualElementEditingStage obj)
    {
        CacheLocalRoot();
    }

    protected override bool AcceptRootAsParent() => m_Context.SubDocumentOptions != SubDocumentOptions.InContext;

    static bool CanReceiveChildren(VisualElement element) => element.contentContainer != null;

    protected override bool AcceptParent(HierarchyView view, in HierarchyNode parentNode, VisualElement parent)
    {
        return CanReceiveChildren(parent) && (IsEditable(parent) || parent == m_LocalRoot);
    }

    protected override bool AcceptChild(HierarchyView view, in HierarchyNode childNode, VisualElement child)
    {
        return IsEditable(child);
    }

    protected override void InitializeDrag(in HierarchyViewDragAndDropSetupData data)
    {
        var list = new List<VisualElement>();
        foreach (var node in data.Nodes)
        {
            if (TryGetElementFromNode(in node, out var element))
            {
                list.Add(element);
            }
        }
        data.SetGenericData(k_DraggedVisualElementKey, list);
    }

    protected override DragVisualMode HandleDrop(in HierarchyViewDragAndDropHandlingData data, bool performDrop)
    {
        var visualMode = DragVisualMode.None;
        visualMode = HandleVisualElementBeingDragged(in data, performDrop);
        if (visualMode != DragVisualMode.None)
            return visualMode;

        return DragVisualMode.None;
    }

    DragVisualMode HandleVisualElementBeingDragged(in HierarchyViewDragAndDropHandlingData data, bool performDrop)
    {
        var draggedVisualElements = (List<VisualElement>)data.GetGenericData(k_DraggedVisualElementKey);
        if (draggedVisualElements == null)
            return DragVisualMode.None;

        foreach (var visualElement in draggedVisualElements)
        {
            if (!IsEditable(visualElement))
                return DragVisualMode.Rejected;

            if (data.Parent == Hierarchy.Root)
                continue;

            if (!TryGetElementFromNode(data.Parent, out var parentElement))
                return DragVisualMode.Rejected;

            if (parentElement == visualElement)
                return DragVisualMode.Rejected;

            if (visualElement.Contains(parentElement))
                return DragVisualMode.Rejected;
        }

        switch (data.DropPosition)
        {
            case DragAndDropPosition.OverItem:
            {
                // Here, we return `None` because the default handle drop behaviour will already handle it for us
                // using AcceptParent/AcceptChild.
                if (!performDrop)
                    return DragVisualMode.None;
            }
                break;
            case DragAndDropPosition.BetweenItems:
            {
                var accept = CheckIfElementCanBeInsertedAtIndex(data.View, data.Parent, data.ChildIndex, data.InsertAtIndex);
                if (!accept)
                    return DragVisualMode.Rejected;
                if (!performDrop)
                    return DragVisualMode.Move;
            }
                break;
            case DragAndDropPosition.OutsideItems:
            {
                // Here, we return `None` because the default handle drop behaviour will already handle it for us
                // using AcceptParent/AcceptChild.
                if (!performDrop)
                    return DragVisualMode.None;
            }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        return DoPerformDrop(in data, draggedVisualElements);
    }

    bool CheckIfElementCanBeInsertedAtIndex(HierarchyView view, in HierarchyNode parent, int childIndex, int insertIndex)
    {
        // Try to detect a case where we are trying to drag a parent as it's first
        if (childIndex == 0)
        {
            var parentIndex = view.ViewModel.IndexOf(in parent);
            if (parentIndex != insertIndex - 1)
                return false;
        }

        // Root level elements are either all editable or all non-editable, depending on the context.
        var droppingOnRoot = parent == Hierarchy.Root;
        var inContextEditing = m_Context.SubDocumentOptions == SubDocumentOptions.InContext;
        if (droppingOnRoot)
            return !inContextEditing;

        // Can't reparent if we can't find the parent or if the parent won't accept nodes.
        if (!TryGetElementFromNode(in parent, out var physicalParentElement))
            return false;

        var logicalParent = GetLogicalParentFromPhysicalParent(physicalParentElement);
        // If we're dragging inside the `.hierarchy`, don't accept the drag operation.
        if (logicalParent == physicalParentElement && logicalParent.contentContainer != logicalParent)
        {
            return false;
        }
        if (!TryGetNodeFromElement(logicalParent, out var logicalParentNode))
            return false;

        if (!AcceptParent(view, in logicalParentNode, logicalParent))
            return false;

        var childCount = Hierarchy.GetChildrenCount(in parent);
        var children = Hierarchy.GetChildren(in parent);

        if (childIndex < 0)
            childIndex = childCount;

        if (childIndex == 0)
        {
            if (childCount == 0)
                return true;

            return TryGetElementFromNode(in children[0], out var current) && IsEditable(current);
        }

        if (childIndex < childCount)
        {
            // We only want to allow drag and dropping at a place where the index will visually remain the same, so we
            // need to check if either the current element or the element before it is editable.
            return TryGetElementFromNode(in children[childIndex - 1], out var previous) && IsEditable(previous) ||
                   TryGetElementFromNode(in children[childIndex], out var current) && IsEditable(current);
        }
        // childIndex == childCount
        return true;
    }

    private DragVisualMode DoPerformDrop(in HierarchyViewDragAndDropHandlingData data, List<VisualElement> draggedVisualElements)
    {
        var parentElement = m_LocalRoot;

        if (data.Parent != Hierarchy.Root)
            if (!TryGetElementFromNode(data.Parent, out parentElement))
                return DragVisualMode.Rejected;

        if (parentElement == null)
            return DragVisualMode.Rejected;

        var childIndex = data.ChildIndex < 0 ? Hierarchy.GetChildrenCount(data.Parent) : data.ChildIndex;

        var logicalParent = GetLogicalParentFromPhysicalParent(parentElement);
        if (logicalParent != m_LocalRoot)
            Assert.IsNotNull(logicalParent.visualElementAsset);

        var parentAsset = logicalParent != m_LocalRoot
            ? logicalParent.visualElementAsset
            : m_Stage.EditedVisualTreeAsset.visualTree;
        Assert.IsNotNull(parentAsset);

        var childrenAssets = new VisualElementAsset[draggedVisualElements.Count];
        for (var i = 0; i < draggedVisualElements.Count; ++i)
            childrenAssets[i] = draggedVisualElements[i].visualElementAsset;

        if (data.DropPosition == DragAndDropPosition.OverItem)
        {
            Assert.IsTrue(data.ChildIndex == -1);
            foreach (var element in draggedVisualElements)
                logicalParent.Add(element);

            new ReparentElementsCommand(parentAsset, -1, childrenAssets).Execute();
            return DragVisualMode.Move;
        }

        var index = childIndex;
        for (var i = 0; i < draggedVisualElements.Count; ++i)
        {
            var element = draggedVisualElements[i];
            // We might need to adjust the new index if the element is already a children of the target parent.
            if (element.parent == logicalParent)
            {
                if (logicalParent.IndexOf(element) < index)
                    --index;

            }
            logicalParent.Insert(index++, element);
        }

        var adjustedIndex = logicalParent.IndexOf(draggedVisualElements[0]);
        for (var i = 0; i < adjustedIndex; ++i)
        {
            if (logicalParent[i].visualElementAsset == null)
                --adjustedIndex;
        }
        new ReparentElementsCommand(parentAsset, adjustedIndex, childrenAssets).Execute();

        return DragVisualMode.Move;
    }

    protected override bool CanStartDrag(HierarchyView view, in SelectionContext selection) => true;

    protected override void OnBindView(HierarchyView view)
    {
        if (StageNavigationManager.instance.currentStage is not VisualElementEditingStage)
            return;
        base.OnBindView(view);
    }

    protected override void ViewModelPostUpdate(HierarchyViewModel viewModel)
    {
        if (StageNavigationManager.instance.currentStage is not VisualElementEditingStage)
            return;

        base.ViewModelPostUpdate(viewModel);

        if (m_ExpandOnInitialize)
            ForceDisplayInContextPanel(viewModel);
    }

    private void ForceDisplayInContextPanel(HierarchyViewModel viewModel)
    {
        if (m_Context.SubDocumentPath == null)
            return;

        if (m_LocalRoot == null)
            return;

        if (TryGetNodeFromElement(m_LocalRoot, out var node) && Hierarchy.Exists(node))
        {
            viewModel.SetFlagsRecursive(node, HierarchyNodeFlags.Expanded, HierarchyTraversalDirection.Parents);
        }

        m_ExpandOnInitialize = false;
    }

    protected override NodeCreationType ShouldCreateNode(VisualElement element)
    {
        return element is PanelRootElement
            ? NodeCreationType.CreateChildren
            : base.ShouldCreateNode(element);
    }

    protected override bool TryGetParentNode(VisualElement element, out HierarchyNode parentNode)
    {
        if (element.parent is PanelRootElement)
        {
            parentNode = Hierarchy.Root;
            return true;
        }

        return base.TryGetParentNode(element, out parentNode);
    }

    protected override void Bind(HierarchyViewItem item, VisualElement element)
    {
        item.EnableInClassList(VisualElementDisabledUssClass, !IsEditable(element));
        base.Bind(item, element);
    }

    protected override void Unbind(HierarchyViewItem item, VisualElement element)
    {
        item.style.opacity = StyleKeyword.Null;
        base.Unbind(item, element);
    }

    protected override void BindNavigation(HierarchyViewItem item, VisualElement container)
    {
        if (IsNavigable(container))
            SetStageNodeNavigation(item, container);
        else
            UnsetStageNodeNavigation(item);
    }

    protected override void UnbindNavigation(HierarchyViewItem item, VisualElement container)
    {
        UnsetStageNodeNavigation(item);
    }

    private bool IsNavigable(VisualElement element)
    {
        return element.GetFirstAncestorWhere(e => e == m_LocalRoot) != null;
    }

    private bool IsEditable(VisualElement element)
    {
        return GetEditFlags(element) == (VisualElementEditFlags.FullyEditable);
    }

    public VisualElementEditFlags GetEditFlags(VisualElement element)
    {
        return m_Context.GetElementEditFlags(element);
    }

    internal void CacheLocalRoot()
    {
        m_LocalRoot = m_Panel.visualTree;

        if (m_Context.SubDocumentOptions is SubDocumentOptions.None or SubDocumentOptions.Isolation)
            return;

        if (m_Context.SubDocumentPath == null)
            return;

        for (var i = 0; i < m_Context.SubDocumentPath.Length; ++i)
        {
            var template = m_Context.SubDocumentPath[i];
            m_LocalRoot = m_LocalRoot?.Query<TemplateContainer>().Where(tc => tc.visualElementAsset.id == template.id).First();
        }

        // We couldn't find the correct local root, so we go out of the current staging mode.
        if (m_LocalRoot == null)
        {
            Debug.LogWarning("Current stage could not be recreated correctly.");
            StageUtility.GoBackToPreviousStage();
        }
    }
}
