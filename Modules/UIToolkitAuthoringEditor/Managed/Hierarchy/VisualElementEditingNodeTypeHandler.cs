// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Hierarchy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal class VisualElementEditingNodeHandler : VisualElementNodeTypeHandler, IVisualElementEditingManager
{
    private VisualElementEditingStage m_Stage;
    private Panel m_Panel;
    private VisualElement m_LocalRoot;
    private bool m_ExpandOnInitialize = false;

    VisualTreeAssetEditingContext Context => m_Stage?.Context ?? default;

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

    protected override bool AcceptRootAsParent() => Context.SubDocumentOptions != SubDocumentOptions.InContext;

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
        if (StageUtility.GetCurrentStage() != m_Stage)
            return;
        base.InitializeDrag(data);
    }

    protected override DragVisualMode HandleDrop(in HierarchyViewDragAndDropHandlingData data, bool performDrop)
    {
        var visualMode = DragVisualMode.None;
        visualMode = HandleVisualElementBeingDragged(in data, performDrop);
        if (visualMode != DragVisualMode.None)
            return visualMode;

        visualMode = HandleUIAssetsBeingDragged(in data, performDrop);
        if (visualMode != DragVisualMode.None)
            return visualMode;

        visualMode = HandleLibraryItemBeingDragged(in data, performDrop);
        if (visualMode != DragVisualMode.None)
            return visualMode;

        return DragVisualMode.None;
    }

    DragVisualMode HandleVisualElementBeingDragged(in HierarchyViewDragAndDropHandlingData data, bool performDrop)
    {
        var draggedVisualElements = (List<VisualElement>)data.GetGenericData(DraggedVisualElementKey);
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
        return DoPerformVisualElementsDrop(in data, draggedVisualElements);
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
        var inContextEditing = Context.SubDocumentOptions == SubDocumentOptions.InContext;
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

    private DragVisualMode DoPerformVisualElementsDrop(in HierarchyViewDragAndDropHandlingData data, List<VisualElement> draggedVisualElements)
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

    DragVisualMode HandleUIAssetsBeingDragged(in HierarchyViewDragAndDropHandlingData data, bool performDrop)
    {
        if (data.Paths is null or { Length: 0 })
            return DragVisualMode.None;

        var totalDragCount = data.Paths.Length;

        using var ssHandle = ListPool<StyleSheet>.Get(out var styleSheets);
        using var vtaHandle = ListPool<VisualTreeAsset>.Get(out var visualTreeAssets);

        foreach (var path in data.Paths)
        {
            if (string.IsNullOrEmpty(path))
                continue;
            var assetType = AssetDatabase.GetMainAssetTypeAtPath(path);

            if (typeof(StyleSheet).IsAssignableFrom(assetType))
            {
                var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                if (styleSheet)
                    styleSheets.Add(styleSheet);
            }
            else if (typeof(VisualTreeAsset).IsAssignableFrom(assetType))
            {
                var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
                if (visualTreeAsset)
                    visualTreeAssets.Add(visualTreeAsset);
            }
        }

        // Only dragging style sheets.
        if (styleSheets.Count == totalDragCount)
            return HandleStyleSheetsBeingDropped(in data, styleSheets, performDrop);

        // Only dragging visual tree assets.
        if (visualTreeAssets.Count == totalDragCount)
            return HandleVisualTreeAssetsBeingDropped(in data, visualTreeAssets, performDrop);

        // A mix of different asset types
        if (styleSheets.Count > 0 || visualTreeAssets.Count > 0)
        {
            return DragVisualMode.Rejected;
        }
        // Contains assets we do not care about here.
        return DragVisualMode.None;
    }

    private DragVisualMode HandleStyleSheetsBeingDropped(in HierarchyViewDragAndDropHandlingData data, List<StyleSheet> styleSheets, bool performDrop)
    {
        if (data.DropPosition != DragAndDropPosition.OverItem)
            return DragVisualMode.Rejected;

        if (data.Parent != data.Target)
            return DragVisualMode.Rejected;

        if (!TryGetElementFromNode(data.Target, out var element))
            return DragVisualMode.Rejected;

        if (!IsEditable(element))
            return DragVisualMode.Rejected;

        return performDrop
            ? DoPerformStyleSheetsDrop(in data, styleSheets)
            : DragVisualMode.Copy;
    }

    private DragVisualMode DoPerformStyleSheetsDrop(in HierarchyViewDragAndDropHandlingData data, List<StyleSheet> styleSheets)
    {
        // Only support "OverItem" for now, so the target and parent should be the same.
        Assert.IsTrue(data.Parent == data.Target);

        if (!TryGetElementFromNode(data.Target, out var element))
            return DragVisualMode.Rejected;

        var visualElementAsset = element.visualElementAsset;
        new AddStyleSheetsToElementCommand(visualElementAsset, styleSheets.ToArray()).Execute();

        foreach (var styleSheet in styleSheets)
            element.styleSheets.Remove(styleSheet);
        foreach (var styleSheet in styleSheets)
            element.styleSheets.Add(styleSheet);
        return DragVisualMode.Copy;
    }

    private DragVisualMode HandleVisualTreeAssetsBeingDropped(in HierarchyViewDragAndDropHandlingData data, List<VisualTreeAsset> visualTreeAssets, bool performDrop)
    {
        foreach (var visualTreeAsset in visualTreeAssets)
        {
            if (!visualTreeAsset)
                return DragVisualMode.Rejected;

            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(visualTreeAsset)))
                return DragVisualMode.Rejected;

            // Check for cyclic dependencies
            if (Context.WillCauseCircularDependency(visualTreeAsset))
                return DragVisualMode.Rejected;
        }

        switch (data.DropPosition)
        {
            case DragAndDropPosition.OverItem:
            {
                if (!TryGetElementFromNode(data.Target, out var parent)
                    || !IsEditable(parent))
                    return DragVisualMode.Rejected;

                if (!performDrop)
                    return DragVisualMode.Copy;
            }
                break;
            case DragAndDropPosition.BetweenItems:
            {
                var accept = CheckIfElementCanBeInsertedAtIndex(data.View, data.Parent, data.ChildIndex, data.InsertAtIndex);
                if (!accept)
                    return DragVisualMode.Rejected;
                if (!performDrop)
                    return DragVisualMode.Copy;
            }
                break;
            case DragAndDropPosition.OutsideItems:
            {
                // Here, we return `None` because the default handle drop behaviour will already handle it for us
                // using AcceptParent/AcceptChild. We're using the same rules as dropping elements here, because we'll
                // create elements.
                if (!performDrop)
                    return DragVisualMode.None;
            }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        return DoPerformVisualTreeAssetDrop(in data, visualTreeAssets);
    }

    private DragVisualMode DoPerformVisualTreeAssetDrop(in HierarchyViewDragAndDropHandlingData data, List<VisualTreeAsset> visualTreeAssets)
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

        if (data.DropPosition == DragAndDropPosition.OverItem)
        {
            Assert.IsTrue(data.ChildIndex == -1);
            new AddTemplatesToElementCommand(parentAsset, -1, visualTreeAssets.ToArray()).Execute();
            m_Stage.RequestRefresh();
            return DragVisualMode.Move;
        }

        var adjustedIndex = logicalParent.IndexOf(logicalParent[childIndex]);
        for (var i = 0; i < adjustedIndex; ++i)
        {
            if (logicalParent[i].visualElementAsset == null)
                --adjustedIndex;
        }
        new AddTemplatesToElementCommand(parentAsset, adjustedIndex, visualTreeAssets.ToArray()).Execute();
        m_Stage.RequestRefresh();

        return DragVisualMode.Copy;
    }

    DragVisualMode HandleLibraryItemBeingDragged(in HierarchyViewDragAndDropHandlingData data, bool performDrop)
    {
        if (DragAndDrop.GetGenericData("LibraryItem") is not LibraryItem libraryItem)
            return DragVisualMode.None;

        var elementType = libraryItem.libraryType.type;
        switch (data.DropPosition)
        {
            case DragAndDropPosition.OverItem:
            {
                if (!TryGetElementFromNode(data.Target, out var parent) || !IsEditable(parent))
                    return DragVisualMode.Rejected;

                if (!performDrop)
                    return DragVisualMode.Copy;
            }
            break;

            case DragAndDropPosition.BetweenItems:
            {
                var accept = CheckIfElementCanBeInsertedAtIndex(data.View, data.Parent, data.ChildIndex, data.InsertAtIndex);
                if (!accept)
                    return DragVisualMode.Rejected;
                if (!performDrop)
                    return DragVisualMode.Copy;
            }
            break;

            case DragAndDropPosition.OutsideItems:
            {
                if (Context.SubDocumentOptions == SubDocumentOptions.InContext)
                    return DragVisualMode.Rejected;

                if (!performDrop)
                    return DragVisualMode.Copy;
            }
            break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        return DoPerformLibraryItemDrop(in data, elementType);
    }

    DragVisualMode DoPerformLibraryItemDrop(in HierarchyViewDragAndDropHandlingData data, Type elementType)
    {
        var parentElement = m_LocalRoot;

        if (data.Parent != Hierarchy.Root)
        {
            if (!TryGetElementFromNode(data.Parent, out parentElement))
                return DragVisualMode.Rejected;
        }

        if (parentElement == null)
            return DragVisualMode.Rejected;

        var logicalParent = GetLogicalParentFromPhysicalParent(parentElement);
        var parentAsset = logicalParent != m_LocalRoot ? logicalParent.visualElementAsset : m_Stage.EditedVisualTreeAsset.visualTree;
        var adjustedIndex = -1;
        if (data.DropPosition == DragAndDropPosition.BetweenItems)
        {
            var childIndex = data.ChildIndex < 0 ? Hierarchy.GetChildrenCount(data.Parent) : data.ChildIndex;

            // Check if we're inserting at the end
            if (childIndex < logicalParent.childCount)
            {
                adjustedIndex = logicalParent.IndexOf(logicalParent[childIndex]);
                for (var i = 0; i < adjustedIndex; ++i)
                {
                    if (logicalParent[i].visualElementAsset == null)
                        --adjustedIndex;
                }
            }
        }

        var command = new AddElementCommand(elementType, m_Stage.EditedVisualTreeAsset, parentAsset, adjustedIndex);
        command.Execute();

        m_Stage.RequestRefresh();

        return DragVisualMode.Copy;
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
        if (Context.SubDocumentPath == null)
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
        return Context.GetElementEditFlags(element);
    }

    internal void CacheLocalRoot()
    {
        m_LocalRoot = m_Panel.visualTree;

        if (Context.SubDocumentOptions is SubDocumentOptions.None or SubDocumentOptions.Isolation)
            return;

        if (Context.SubDocumentPath == null)
            return;

        for (var i = 0; i < Context.SubDocumentPath.Length; ++i)
        {
            var template = Context.SubDocumentPath[i];
            m_LocalRoot = m_LocalRoot?.Query<TemplateContainer>()
                .Where(tc =>
                {
                    var templateAsset = (TemplateAsset)tc.visualElementAsset;
                    return templateAsset == template && templateAsset.id == template.id;
                }).First();
        }

        // We couldn't find the correct local root, so we go out of the current staging mode.
        if (m_LocalRoot == null)
        {
            Debug.LogWarning("Current stage could not be recreated correctly.");
            StageUtility.GoBackToPreviousStage();
        }
    }

    protected override void PopulateContextMenu(in HierarchyNode node, VisualElement element, DropdownMenu menu)
    {
        StageContextMenuUtility.PopulateMenu(menu);
    }
}
