// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitAuthoringFramework not yet converted
using System;
using System.Collections.Generic;
using Unity.Hierarchy;
using Unity.UIToolkit.Editor.Utilities;
using UnityEditor;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal partial class VisualElementNodeHandler
{
    /// <summary>
    /// Determines if a drag operation can be started with the specified nodes.
    /// </summary>
    /// <param name="view">The <see cref="HierarchyView"/>.</param>
    /// <param name="selection">The selection.</param>
    /// <returns><see langword="true"/> if the dragging operation can be started, <see langword="false"/> otherwise.</returns>
    protected virtual bool CanStartDrag(HierarchyView view, in SelectionContext selection) => true;

    protected virtual void InitializeDrag(in HierarchyViewDragAndDropSetupData data)
    {
        var list = new List<VisualElement>();
        foreach (var node in data.Nodes)
        {
            if (TryGetElementFromNode(in node, out var element))
            {
                list.Add(element);
            }
        }
        data.SetGenericData(DraggedVisualElementKey, list);
    }

    /// <summary>
    /// Determines if <paramref name="parent"/> can receive the elements being dragged.
    /// </summary>
    protected virtual bool AcceptParent(HierarchyView view, in HierarchyNode parentNode, VisualElement parent)
        => VisualElementUtility.CanReceiveChildren(parent)
           && (StageStrategy.IsFullyEditable(parent) || parent == StageStrategy.ResolveDocumentRoot(parent));

    /// <summary>
    /// Determines if <paramref name="child"/> can be moved by a drag operation.
    /// </summary>
    protected virtual bool AcceptChild(HierarchyView view, in HierarchyNode childNode, VisualElement child)
        => StageStrategy.IsFullyEditable(child);

    protected virtual DragVisualMode HandleDrop(in HierarchyViewDragAndDropHandlingData data, bool performDrop)
    {
        // Unlike the rest of the IHierarchyEditorNodeTypeHandler dispatch, the drop endpoints are broadcast to
        // every handler without a read-only check, so gate here.
        if (StageStrategy.IsReadOnly)
            return DragVisualMode.None;

        // Everything below needs the element that would receive the drop; not being able to resolve one means
        // the drop is not aimed at a visual element we own — except a library item dropped on empty space,
        // which creates the panel to hold it.
        if (!TryResolveDropTarget(in data, out var dropParent, out var dropIndex))
            return HandleLibraryItemDroppedOnEmptySpace(in data, performDrop);

        var visualMode = HandleVisualElementBeingDragged(in data, dropParent, dropIndex, performDrop);
        if (visualMode != DragVisualMode.None)
            return visualMode;

        visualMode = HandleUIAssetsBeingDragged(in data, dropParent, dropIndex, performDrop);
        if (visualMode != DragVisualMode.None)
            return visualMode;

        visualMode = HandleLibraryItemBeingDragged(in data, dropParent, dropIndex, performDrop);
        if (visualMode != DragVisualMode.None)
            return visualMode;

        visualMode = HandleClassNameBeingDragged(in data, performDrop);
        if (visualMode != DragVisualMode.None)
            return visualMode;

        return DragVisualMode.None;
    }

    /// <summary>
    /// Resolves the live element that a drop described by <paramref name="data"/> would be parented to.
    /// </summary>
    bool TryResolveDropParentElement(in HierarchyViewDragAndDropHandlingData data, out VisualElement parentElement)
    {
        if (data.Parent == Hierarchy.Root)
        {
            // Root-level drops land in the ambient document, which only a stage editing a single document has.
            parentElement = StageStrategy.ResolveDocumentRoot(null);
            return parentElement != null;
        }

        // Only nodes we own are mapped, so this also filters out drops aimed at another node type.
        return TryGetElementFromNode(data.Parent, out parentElement);
    }

    /// <summary>
    /// Resolves the element a drop described by <paramref name="data"/> parents its content to, and the index
    /// the drop lands at among that element's own children — <c>-1</c> to append.
    /// </summary>
    /// <remarks>
    /// The view reports a drop against a <em>physical</em> parent node. For a control holding its content in a
    /// nested container — a <see cref="ScrollView"/>, a <see cref="Foldout"/> — that node is either the content
    /// container, whose children are the control's content one for one, or the control itself, whose children
    /// are its internals. Authoring only ever adds to the control, so the position has to be restated as an
    /// index among the control's own children before it means anything: taken as-is off the control it counts
    /// internals instead of content and lands past the end of it, appending instead of inserting.
    /// </remarks>
    bool TryResolveDropTarget(in HierarchyViewDragAndDropHandlingData data, out VisualElement parentElement,
        out int childIndex)
    {
        childIndex = -1;

        if (!TryResolveDropParentElement(in data, out var physicalParent))
        {
            parentElement = null;
            return false;
        }

        parentElement = GetLogicalParentFromPhysicalParent(physicalParent);

        // Only a drop landing between two rows carries a position; the others append.
        if (data.DropPosition != DragAndDropPosition.BetweenItems || data.ChildIndex < 0)
            return true;

        var contentContainer = parentElement.contentContainer;
        if (contentContainer == null)
            return true;

        // The reported parent is the content container itself, or an element that is its own: either way its
        // children are the logical children, in order.
        if (contentContainer == physicalParent)
        {
            childIndex = Math.Min(data.ChildIndex, parentElement.childCount);
            return true;
        }

        // The reported parent is the control, so the index counts its internals: all it can tell us is whether
        // the drop landed before or after the branch the content lives in.
        childIndex = data.ChildIndex <= IndexOfContentBranch(parentElement) ? 0 : parentElement.childCount;
        return true;
    }

    /// <summary>
    /// The index, among <paramref name="element"/>'s physical children, of the one whose branch holds its
    /// content container.
    /// </summary>
    static int IndexOfContentBranch(VisualElement element)
    {
        var branch = element.contentContainer;
        while (branch != null && branch.hierarchy.parent != element)
            branch = branch.hierarchy.parent;

        return branch != null ? element.hierarchy.IndexOf(branch) : 0;
    }

    DragVisualMode HandleVisualElementBeingDragged(in HierarchyViewDragAndDropHandlingData data,
        VisualElement dropParent, int dropIndex, bool performDrop)
    {
        var draggedVisualElements = (List<VisualElement>)data.GetGenericData(DraggedVisualElementKey);
        if (draggedVisualElements == null)
            return DragVisualMode.None;

        foreach (var visualElement in draggedVisualElements)
        {
            if (!StageStrategy.IsFullyEditable(visualElement))
                return DragVisualMode.Rejected;

            if (MoveWouldCauseCircularDependency(dropParent, visualElement))
                return DragVisualMode.Rejected;

            if (data.Parent == Hierarchy.Root)
                continue;

            if (dropParent == visualElement)
                return DragVisualMode.Rejected;

            if (visualElement.Contains(dropParent))
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
                var accept = CheckIfElementCanBeInsertedAtIndex(in data, dropParent, dropIndex);
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

        return DoPerformVisualElementsDrop(dropParent, dropIndex, draggedVisualElements);
    }

    /// <summary>
    /// Whether moving <paramref name="child"/> onto <paramref name="parentElement"/> would leave a document
    /// (transitively) instantiating itself.
    /// </summary>
    /// <remarks>
    /// A moved element takes the template instances authored below it along, so a move into another document makes
    /// that document instantiate their templates — the same dependency a template dropped from the Project
    /// browser creates, and the same cycle it can close. Refused per dragged element but on behalf of the whole
    /// drag, so a selection holding one offending element is turned away rather than half applied.
    /// </remarks>
    bool MoveWouldCauseCircularDependency(VisualElement parentElement, VisualElement child)
    {
        var childAsset = child.visualElementAsset;
        if (childAsset == null || !StageStrategy.TryResolveDropAsset(parentElement, out var dropAsset))
            return false;

        // Reordering, or moving within one document, leaves what that document instantiates exactly as it was.
        var targetDocument = ResolveTargetAsset(parentElement, child, dropAsset)?.visualTreeAsset;
        if (targetDocument == childAsset.visualTreeAsset)
            return false;

        return VisualElementEditingUtility.WouldCauseCircularDependency(targetDocument, parentElement, childAsset);
    }

    /// <summary>
    /// Whether the drop described by <paramref name="data"/> can insert at <paramref name="childIndex"/> among
    /// <paramref name="parentElement"/>'s own children, both resolved by <see cref="TryResolveDropTarget"/>.
    /// </summary>
    bool CheckIfElementCanBeInsertedAtIndex(in HierarchyViewDragAndDropHandlingData data,
        VisualElement parentElement, int childIndex)
    {
        // The node the view reported the drop against, which is not always the one backing parentElement.
        var reportedParentNode = data.Parent;

        // Try to detect a case where we are trying to drag a parent as it's first
        if (data.ChildIndex == 0)
        {
            var parentIndex = data.View.ViewModel.IndexOf(in reportedParentNode);
            if (parentIndex != data.InsertAtIndex - 1)
                return false;
        }

        if (reportedParentNode == Hierarchy.Root)
            return StageStrategy.AcceptRootAsParent;

        // Can't reparent if we can't find the parent or if the parent won't accept nodes.
        if (!TryGetNodeFromElement(parentElement, out var parentNode))
            return false;

        if (!AcceptParent(data.View, in parentNode, parentElement))
            return false;

        // The children of the resolved parent, not the rows under the drop parent: for a control holding its
        // content in a nested container the two differ, and only the content can be reordered.
        var childCount = parentElement.childCount;

        if (childIndex < 0 || childIndex > childCount)
            childIndex = childCount;

        if (childIndex == 0)
            return childCount == 0 || StageStrategy.IsFullyEditable(parentElement[0]);

        if (childIndex < childCount)
        {
            // We only want to allow drag and dropping at a place where the index will visually remain the same, so we
            // need to check if either the current element or the element before it is editable.
            return StageStrategy.IsFullyEditable(parentElement[childIndex - 1]) ||
                   StageStrategy.IsFullyEditable(parentElement[childIndex]);
        }

        // childIndex == childCount
        return true;
    }

    // The drop only writes to the authoring assets; every document it changed is re-cloned once the command
    // group closes, and that re-clone is what brings the live trees in line. Moving the live elements here as
    // well would only pre-empt it by a tick, and could not reach the views the assets are shared with anyway
    // (a second component rendering the same document, the other instances of an edited template), so the two
    // would disagree until the re-clone landed.
    DragVisualMode DoPerformVisualElementsDrop(VisualElement parentElement, int childIndex,
        List<VisualElement> draggedVisualElements)
    {
        if (!StageStrategy.TryResolveDropAsset(parentElement, out var dropAsset))
            return DragVisualMode.Rejected;

        var parentAsset = dropAsset;
        var resolvedParentAsset = false;

        var childrenAssets = new VisualElementAsset[draggedVisualElements.Count];
        for (var i = 0; i < draggedVisualElements.Count; ++i)
        {
            var child = draggedVisualElements[i];
            var childAsset = child.visualElementAsset;
            if (childAsset == null)
                return DragVisualMode.Rejected;
            childrenAssets[i] = childAsset;

            var target = ResolveTargetAsset(parentElement, child, dropAsset);

            // The dragged elements share a parent but are authored in two different documents, so no single
            // asset can take them all: one of them would have to be re-homed to satisfy the other.
            if (resolvedParentAsset && parentAsset != target)
                return DragVisualMode.Rejected;

            parentAsset = target;
            resolvedParentAsset = true;
        }

        var index = ResolveAssetInsertIndex(parentElement, childIndex, parentAsset, childrenAssets);

        var status = ReparentElementsCommand.Execute(CommandSources.Hierarchy, parentAsset, index, childrenAssets);
        if (status != CommandExecutionStatus.Success)
            return DragVisualMode.Rejected;

        // The re-clone that follows the command group replaces every live element the drag moved, and an element
        // that changed parent no longer matches the identity its selection was filed under, so asking for it back
        // is what keeps the dropped elements selected — narrowed to the instance they were dropped into, which is
        // the one the user is looking at.
        RequestSelectionOnNextUpdate(childrenAssets);
        ScopePendingSelectionRequestsTo(parentElement);

        return DragVisualMode.Move;
    }

    /// <summary>
    /// The asset <paramref name="child"/> has to end up under, for a drop onto <paramref name="parentElement"/>
    /// whose own asset is <paramref name="dropAsset"/>.
    /// </summary>
    /// <remarks>
    /// Dropping an element onto a parent it does not already sit under moves it, and reparenting its asset onto
    /// that parent's asset re-homes it into the target document — which is how a drag across two scene
    /// documents works in the Main Stage. Dropping it under the parent it is already in only reorders it, and
    /// must re-home nothing, because the children of one element are not always authored in the same document:
    /// a <see cref="TemplateContainer"/> holds the roots cloned from the template it instantiates followed by
    /// the children the <c>&lt;Instance&gt;</c> declares in the host document. Re-homing there would tear a
    /// root out of the template — losing it for every other instance — and graft it onto this one instead of
    /// moving it among its siblings. A reorder therefore keeps the asset where it is authored.
    /// </remarks>
    static VisualElementAsset ResolveTargetAsset(VisualElement parentElement, VisualElement child,
        VisualElementAsset dropAsset)
    {
        if (child.parent != parentElement)
            return dropAsset;

        return child.visualElementAsset.parentAsset as VisualElementAsset ?? dropAsset;
    }

    /// <summary>
    /// The index the dropped content has to end up at among <paramref name="parentAsset"/>'s own children, for
    /// a drop landing before the <paramref name="childIndex"/>-th child of <paramref name="parentElement"/>, or
    /// <c>-1</c> when the drop appends.
    /// </summary>
    /// <remarks>
    /// A child index and an asset index are not interchangeable. The children of the drop parent also cover
    /// elements that have no slot in <paramref name="parentAsset"/> at all: control internals with no backing
    /// asset, and content cloned from another document — a nested template's, or the root of a nested panel
    /// component, whose own rows hang from its GameObject instead. Only the siblings actually authored in
    /// <paramref name="parentAsset"/> count. <paramref name="movedAssets"/>, when given, is skipped as well:
    /// those assets leave their current slot before being re-inserted, so counting them would push the drop one
    /// slot too far.
    /// </remarks>
    static int ResolveAssetInsertIndex(VisualElement parentElement, int childIndex, VisualElementAsset parentAsset,
        VisualElementAsset[] movedAssets = null)
    {
        if (childIndex < 0)
            return -1;

        var upTo = Math.Min(childIndex, parentElement.childCount);

        var index = 0;
        for (var i = 0; i < upTo; ++i)
        {
            var siblingAsset = parentElement[i].visualElementAsset;
            if (siblingAsset == null || siblingAsset.parentAsset != parentAsset)
                continue;

            if (movedAssets != null && Array.IndexOf(movedAssets, siblingAsset) >= 0)
                continue;

            ++index;
        }

        return index;
    }

    DragVisualMode HandleUIAssetsBeingDragged(in HierarchyViewDragAndDropHandlingData data,
        VisualElement dropParent, int dropIndex, bool performDrop)
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
            return HandleVisualTreeAssetsBeingDropped(in data, dropParent, dropIndex, visualTreeAssets, performDrop);

        // A mix of different asset types
        if (styleSheets.Count > 0 || visualTreeAssets.Count > 0)
        {
            return DragVisualMode.Rejected;
        }

        // Contains assets we do not care about here.
        return DragVisualMode.None;
    }

    DragVisualMode HandleStyleSheetsBeingDropped(in HierarchyViewDragAndDropHandlingData data,
        List<StyleSheet> styleSheets, bool performDrop)
    {
        if (data.DropPosition != DragAndDropPosition.OverItem)
            return DragVisualMode.Rejected;

        if (data.Parent != data.Target)
            return DragVisualMode.Rejected;

        if (!TryGetElementFromNode(data.Target, out var element))
            return DragVisualMode.Rejected;

        // On a document root the target asset is the document's own root asset, which is where a
        // document-level `<Style src="..."/>` lives.
        if (!StageStrategy.CanHostDrop(element) || !StageStrategy.TryResolveDropAsset(element, out var targetAsset))
            return DragVisualMode.Rejected;

        if (!performDrop)
            return DragVisualMode.Copy;

        AddStyleSheetsToElementCommand.Execute(CommandSources.Hierarchy, targetAsset, styleSheets.ToArray());

        foreach (var styleSheet in styleSheets)
            element.styleSheets.Remove(styleSheet);
        foreach (var styleSheet in styleSheets)
            element.styleSheets.Add(styleSheet);

        return DragVisualMode.Copy;
    }

    DragVisualMode HandleVisualTreeAssetsBeingDropped(in HierarchyViewDragAndDropHandlingData data,
        VisualElement dropParent, int dropIndex, List<VisualTreeAsset> visualTreeAssets, bool performDrop)
    {
        // The cycle would be created in the document the drop mutates, which is not always the one the drop
        // parent belongs to: dropping onto a document root adds to the document it hosts.
        StageStrategy.TryResolveDropAsset(dropParent, out var dropAsset);

        foreach (var visualTreeAsset in visualTreeAssets)
        {
            if (!visualTreeAsset)
                return DragVisualMode.Rejected;

            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(visualTreeAsset)))
                return DragVisualMode.Rejected;

            // Check for cyclic dependencies
            if (VisualElementEditingUtility.WillCauseCircularDependency(dropAsset?.visualTreeAsset, dropParent, visualTreeAsset))
                return DragVisualMode.Rejected;
        }

        switch (data.DropPosition)
        {
            case DragAndDropPosition.OverItem:
            {
                if (!StageStrategy.CanHostDrop(dropParent) || !VisualElementUtility.CanReceiveChildren(dropParent))
                    return DragVisualMode.Rejected;

                if (!performDrop)
                    return DragVisualMode.Copy;
            }
                break;
            case DragAndDropPosition.BetweenItems:
            {
                var accept = CheckIfElementCanBeInsertedAtIndex(in data, dropParent, dropIndex);
                if (!accept)
                    return DragVisualMode.Rejected;
                if (!performDrop)
                    return DragVisualMode.Copy;
            }
                break;
            case DragAndDropPosition.OutsideItems:
            {
                if (!performDrop)
                    return DragVisualMode.Copy;
            }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return DoPerformVisualTreeAssetDrop(dropParent, dropIndex, visualTreeAssets);
    }

    DragVisualMode DoPerformVisualTreeAssetDrop(VisualElement parentElement, int childIndex,
        List<VisualTreeAsset> visualTreeAssets)
    {
        if (!StageStrategy.TryResolveDropAsset(parentElement, out var parentAsset))
            return DragVisualMode.Rejected;

        var index = ResolveAssetInsertIndex(parentElement, childIndex, parentAsset);

        AddTemplatesToElementCommand.Execute(CommandSources.Hierarchy, parentAsset, index,
            visualTreeAssets.ToArray());

        // The command asked for the instances it created by asset alone; point that at the one dropped into.
        ScopePendingSelectionRequestsTo(parentElement);

        StageStrategy.RequestRefresh(parentElement);
        return DragVisualMode.Copy;
    }

    // An element dropped on empty Hierarchy space, where no drop target resolves because the root holds no
    // document, creates a GameObject with a PanelRenderer, a new document and the element.
    DragVisualMode HandleLibraryItemDroppedOnEmptySpace(in HierarchyViewDragAndDropHandlingData data, bool performDrop)
    {
        if (data.DropPosition != DragAndDropPosition.OutsideItems || !StageStrategy.CreatesPanelForRootLibraryDrop)
            return DragVisualMode.None;

        if (DragAndDrop.GetGenericData(LibraryItem.DragDataKey) is not LibraryItem libraryItem)
            return DragVisualMode.None;

        if (!performDrop)
            return DragVisualMode.Copy;

        return MenuUtility.CreatePanelWithElement(libraryItem.libraryType.type, libraryItem.libraryType.variantName)
            ? DragVisualMode.Copy
            : DragVisualMode.Rejected;
    }

    DragVisualMode HandleLibraryItemBeingDragged(in HierarchyViewDragAndDropHandlingData data,
        VisualElement dropParent, int dropIndex, bool performDrop)
    {
        if (DragAndDrop.GetGenericData(LibraryItem.DragDataKey) is not LibraryItem libraryItem)
            return DragVisualMode.None;

        var elementType = libraryItem.libraryType.type;
        var variantName = libraryItem.libraryType.variantName;
        switch (data.DropPosition)
        {
            case DragAndDropPosition.OverItem:
            {
                if (!StageStrategy.CanHostDrop(dropParent) || !VisualElementUtility.CanReceiveChildren(dropParent))
                    return DragVisualMode.Rejected;

                if (!performDrop)
                    return DragVisualMode.Copy;
            }
                break;

            case DragAndDropPosition.BetweenItems:
            {
                var accept = CheckIfElementCanBeInsertedAtIndex(in data, dropParent, dropIndex);
                if (!accept)
                    return DragVisualMode.Rejected;
                if (!performDrop)
                    return DragVisualMode.Copy;
            }
                break;

            case DragAndDropPosition.OutsideItems:
            {
                if (!StageStrategy.AcceptRootAsParent)
                    return DragVisualMode.Rejected;

                if (!performDrop)
                    return DragVisualMode.Copy;
            }
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        return DoPerformLibraryItemDrop(dropParent, dropIndex, elementType, variantName);
    }

    DragVisualMode DoPerformLibraryItemDrop(VisualElement parentElement, int childIndex, Type elementType,
        string variantName)
    {
        if (!StageStrategy.TryResolveDropAsset(parentElement, out var parentAsset))
            return DragVisualMode.Rejected;

        var index = ResolveAssetInsertIndex(parentElement, childIndex, parentAsset);

        // The new element is authored in the document that owns the parent asset, which in the Main Stage is
        // whichever scene document (or nested template) the drop landed in.
        AddElementCommand.Execute(CommandSources.Hierarchy, elementType, parentAsset.visualTreeAsset, parentAsset,
            index, variantName);

        // The command asked for what it created by asset alone; point that at the instance dropped into.
        ScopePendingSelectionRequestsTo(parentElement);

        StageStrategy.RequestRefresh(parentElement);
        return DragVisualMode.Copy;
    }

    DragVisualMode HandleClassNameBeingDragged(in HierarchyViewDragAndDropHandlingData data, bool performDrop)
    {
        var className = DragAndDrop.GetGenericData(StyleSheetNodeTypeHandler.DraggedSelectorKey) as string;
        if (string.IsNullOrEmpty(className))
            return DragVisualMode.None;

        if (data.DropPosition != DragAndDropPosition.OverItem)
            return DragVisualMode.Rejected;

        // Unlike style sheets, a class only means something on an authored element: a document root has no
        // element of its own to carry it.
        if (!TryGetElementFromNode(data.Target, out var element) || !StageStrategy.IsFullyEditable(element))
            return DragVisualMode.Rejected;

        if (performDrop)
            AddClassCommand.Execute(CommandSources.Hierarchy, element.visualElementAsset, className);

        return DragVisualMode.Copy;
    }

    // AddClassCommand only writes to the asset. The UI Stage does not re-clone for a styling change, so
    // without this the class would not show up until something else rebuilt the live tree.
    void OnClassAddedToElement(in CommandContext context)
    {
        if (context.Status != CommandExecutionStatus.Success)
            return;

        var command = (AddClassCommand)context.Command;
        var vea = command.ElementAsset;

        for (var i = 0; i < m_RegisteredPanels.Count; ++i)
        {
            m_RegisteredPanels[i].visualTree.Query().Where(e => e.visualElementAsset == vea).ForEach(e =>
            {
                e.AddToClassList(command.ClassName);
            });
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
