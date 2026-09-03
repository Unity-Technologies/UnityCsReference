// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitAuthoringFramework not yet converted
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal partial class VisualElementNodeHandler : IHierarchyEditorNodeTypeHandler
{
    readonly List<WeakReference<HierarchyView>> m_Views = new();

    bool IHierarchyEditorNodeTypeHandler.CanCut(HierarchyView view)
    {
        if (StageStrategy.IsReadOnly)
            return false;

        using var memoryOwner = GetSelection(view, out var selection);
        using var assetsHandle = ListPool<VisualElementAsset>.Get(out var assets);
        FilterSelection(view, in selection, assets);
        if (assets.Count == 0)
            return false;

        return CutElementsCommand.Validate(CommandSources.Hierarchy, NoAllocHelpers.CreateSpan(assets));
    }

    bool IHierarchyEditorNodeTypeHandler.OnCut(HierarchyView view)
    {
        if (StageStrategy.IsReadOnly)
            return false;

        using var memoryOwner = GetSelection(view, out var selection);
        Clipboard.GetClipboardForStage().ClearCutElements();

        using var assetsHandle = ListPool<VisualElementAsset>.Get(out var assets);
        FilterSelection(view, in selection, assets);

        if (assets.Count == 0)
            return false;

        CutElementsCommand.Execute(CommandSources.Hierarchy, assets.ToArray());
        return true;
    }

    void OnElementsCut(in CommandContext context)
    {
        if (context.Status != CommandExecutionStatus.Success)
            return;

        var cmd = (CutElementsCommand)context.Command;
        using var nodesHandle = ListPool<HierarchyNode>.Get(out var nodes);
        using var elementsHandle = ListPool<VisualElement>.Get(out var elements);
        foreach (var asset in cmd.ElementsToCut)
        {
            if (asset != null)
                FindAllElementsFromAsset(asset, elements);
        }

        foreach (var element in elements)
        {
            if (TryGetNodeFromElement(element, out var node))
                nodes.Add(node);
        }

        ClearCutFlags();

        if (nodes.Count == 0)
            return;

        foreach (var weakRef in m_Views)
        {
            if (!weakRef.TryGetTarget(out var view))
                continue;

            using (new HierarchyViewModelFlagsChangeScope(view.ViewModel))
                view.ViewModel.SetFlagsRecursive(NoAllocHelpers.CreateSpan(nodes), HierarchyNodeFlags.Cut, HierarchyTraversalDirection.Children);
        }
    }

    bool IHierarchyEditorNodeTypeHandler.CanCopy(HierarchyView view)
    {
        if (StageStrategy.IsReadOnly)
            return false;

        using var memoryOwner = GetSelection(view, out var selection);
        using var assetsHandle = ListPool<VisualElementAsset>.Get(out var assets);
        FilterSelection(view, in selection, assets, requireFullyEditable: false);
        if (assets.Count == 0)
            return false;

        return CopyElementsCommand.Validate(NoAllocHelpers.CreateSpan(assets));
    }

    bool IHierarchyEditorNodeTypeHandler.OnCopy(HierarchyView view)
    {
        if (StageStrategy.IsReadOnly)
            return false;

        using var memoryOwner = GetSelection(view, out var selection);
        using var assetsHandle = ListPool<VisualElementAsset>.Get(out var assets);
        FilterSelection(view, in selection, assets, requireFullyEditable: false);

        if (assets.Count == 0)
            return false;

        CopyElementsCommand.Execute(CommandSources.Hierarchy, assets.ToArray());
        return true;
    }

    void ClearCutFlags()
    {
        foreach (var weakRef in m_Views)
        {
            if (!weakRef.TryGetTarget(out var view))
                continue;

            using (new HierarchyViewModelFlagsChangeScope(view.ViewModel))
                view.ViewModel.ClearFlags(HierarchyNodeFlags.Cut);
        }
    }

    void RefreshCutFlagsForView(HierarchyView view)
    {
        var cutElements = Clipboard.GetClipboardForStage()?.GetCutElements();
        if (cutElements == null || cutElements.Count == 0)
            return;

        using var nodesHandle = ListPool<HierarchyNode>.Get(out var nodes);
        using var elementsHandle = ListPool<VisualElement>.Get(out var elements);
        foreach (var asset in cutElements)
        {
            if (asset != null)
                FindAllElementsFromAsset(asset, elements);
        }

        foreach (var element in elements)
        {
            if (TryGetNodeFromElement(element, out var node))
                nodes.Add(node);
        }

        if (nodes.Count == 0)
            return;

        using (new HierarchyViewModelFlagsChangeScope(view.ViewModel))
            view.ViewModel.SetFlagsRecursive(NoAllocHelpers.CreateSpan(nodes), HierarchyNodeFlags.Cut, HierarchyTraversalDirection.Children);
    }

    void ClearCutFlagsOnSuccess(in CommandContext context)
    {
        if (context.Status != CommandExecutionStatus.Success)
            return;

        ClearCutFlags();
    }

    void OnElementsDuplicated(in CommandContext context)
    {
        if (context.Status != CommandExecutionStatus.Success)
            return;

        var cmd = (DuplicateElementsCommand)context.Command;

        // TODO: ScopePendingSelectionRequestsTo relies on the live element mapping maintained by
        // this handler, so it only works when the hierarchy is open. Duplicating from UIViewport
        // while the hierarchy is closed will still create the element but won't auto-select it.
        // The selection request issued by the command itself (RequestSelectionOnNextUpdate) should
        // eventually be made viewport-aware so it resolves without the hierarchy being present.
        using var parentsHandle = ListPool<VisualElement>.Get(out var parents);
        foreach (var asset in cmd.ElementsToDuplicate)
        {
            var element = FindElementFromAsset(asset);
            parents.Add(element?.parent);
        }
        ScopePendingSelectionRequestsTo(parents);

        ClearCutFlags();
    }

    bool IHierarchyEditorNodeTypeHandler.CanPaste(HierarchyView view)
    {
        if (StageStrategy.IsReadOnly)
            return false;

        using var memoryOwner = GetSelection(view, out var selection);
        if (!CanResolvePasteTarget(view, in selection))
            return false;

        return VisualElementEditingUtility.CanPasteContent();
    }

    /// <summary>
    /// Whether the selection names somewhere to paste into.
    /// </summary>
    /// <remarks>
    /// <para>
    /// With no selection the target is the stage's own fallback: the document being edited in the UI Stage, and
    /// nothing in the Main Stage, which shows several documents at once and cannot pick one.
    /// </para>
    /// <para>
    /// Looser than what <c>OnPaste</c> goes on to resolve: <see cref="CanDoHierarchyOperation"/> is satisfied by
    /// any editable row of the selection, while the paste itself only ever uses the first. A selection whose
    /// first row cannot take the content is still offered the menu item, and pasting then does nothing.
    /// </para>
    /// </remarks>
    bool CanResolvePasteTarget(HierarchyView view, in SelectionContext selection)
    {
        if (selection.SelectionCount == 0)
            return StageStrategy.TryResolvePasteParent(null, out _, out _);

        if (selection.Type != SelectionContext.SelectionType.All)
            return false;

        // The fallback is resolved against the first selected row, the one OnPaste pastes into.
        // TryResolvePasteParent mirrors OnPaste exactly: for a non-editable element (e.g. a template
        // instance) it walks to the logical parent, so Paste is correctly enabled in those cases too.
        return CanDoHierarchyOperation(view, in selection)
               || (TryGetElementFromNode(in selection.Selection[0], out var selected)
                   && StageStrategy.TryResolvePasteParent(selected, out _, out _));
    }

    bool IHierarchyEditorNodeTypeHandler.OnPaste(HierarchyView view)
    {
        if (StageStrategy.IsReadOnly)
            return false;

        using var memoryOwner = GetSelection(view, out var selection);

        // Resolve the paste target through the stage strategy: as a sibling of the first selected element, or —
        // when nothing is selected — a stage-specific fallback (the main stage has none and declines; the UI
        // stage falls back to the local root of the document being edited).
        VisualElement selected = null;
        if (selection.SelectionCount > 0)
            TryGetElementFromNode(in selection.Selection[0], out selected);

        if (!StageStrategy.TryResolvePasteParent(selected, out var parentElement, out var parentAsset))
            return false;

        var targetDocument = parentAsset.visualTreeAsset;

        // Cut (move) branch: move the live elements into the new parent and reparent their assets in place.
        var cutElements = Clipboard.GetClipboardForStage().GetCutElements();
        if (cutElements.Count > 0)
        {
            // Pasting a cut element into another document re-homes it, template instances and all, exactly like a
            // drag does — and can close the same cycle.
            for (var i = 0; i < cutElements.Count; ++i)
            {
                var cutAsset = cutElements[i];
                if (cutAsset == null || cutAsset.visualTreeAsset == targetDocument)
                    continue;

                if (VisualElementEditingUtility.WouldCauseCircularDependency(targetDocument, parentElement, cutAsset))
                    return false;
            }

            using var toPasteHandle = ListPool<VisualElementAsset>.Get(out var toPasteList);
            for (var i = 0; i < cutElements.Count; ++i)
            {
                var cutAsset = cutElements[i];
                if (cutAsset == null) continue;
                var ve = FindElementFromAsset(cutAsset);
                if (ve == null)
                {
                    cutAsset.TryGetAttributeValue("name", out var elementName);
                    var displayName = string.IsNullOrEmpty(elementName) ? cutAsset.fullTypeName : elementName;
                    Debug.LogWarning($"Cut element '{displayName}' could not be found in the current document and will be skipped.");
                    continue;
                }
                parentElement.Add(ve);
                toPasteList.Add(cutAsset);
            }

            if (toPasteList.Count == 0)
                return false;

            var toPaste = toPasteList.ToArray();
            RequestSelectionOnNextUpdate(toPaste);
            ReparentElementsCommand.Execute(CommandSources.Hierarchy, parentAsset, -1, toPaste);
            ScopePendingSelectionRequestsTo(parentElement);
            Clipboard.GetClipboardForStage().ClearCutElements();
            return true;
        }

        // Copy branch: there is nothing live to move, so the content is recreated from the UXML in the buffer.
        return VisualElementEditingUtility.TryPasteCopied(CommandSources.Hierarchy, parentElement, parentAsset);
    }

    bool IHierarchyEditorNodeTypeHandler.CanPasteAsChild(HierarchyView view)
    {
        if (StageStrategy.IsReadOnly)
            return false;

        using var memoryOwner = GetSelection(view, out var selection);
        return CanPasteAsChild(view, in selection);
    }

    /// <summary>
    /// Determines if copied nodes can be pasted as child.
    /// </summary>
    /// <param name="view">The <see cref="HierarchyView"/>.</param>
    /// <param name="selection">The selection.</param>
    /// <returns><see langword="true"/> if action is supported, <see langword="false"/> otherwise.</returns>
    protected virtual bool CanPasteAsChild(HierarchyView view, in SelectionContext selection) => false;

    bool IHierarchyEditorNodeTypeHandler.OnPasteAsChild(HierarchyView view, bool keepWorldPos)
    {
        if (StageStrategy.IsReadOnly)
            return false;

        using var memoryOwner = GetSelection(view, out var selection);
        return OnPasteAsChild(view, in selection);
    }

    /// <summary>
    /// Executes the paste on child operation on the selected nodes.
    /// </summary>
    /// <param name="view">The <see cref="HierarchyView"/>.</param>
    /// <param name="selection">The selection.</param>
    /// <returns><see langword="true"/> if action is executed, <see langword="false"/> otherwise.</returns>
    protected virtual bool OnPasteAsChild(HierarchyView view, in SelectionContext selection) => false;

    bool IHierarchyEditorNodeTypeHandler.CanSetName(HierarchyView view, in HierarchyNode node)
    {
        if (StageStrategy.IsReadOnly)
            return false;

        if (!m_Mappings.TryGetValue(in node, out var element))
            return false;

        if (element is IPanelComponentRootElement rootElement)
            return !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(rootElement.panelComponent?.visualTreeAsset));

        return StageStrategy.GetEditFlags(element).IsFullyEditable();
    }

    bool IHierarchyEditorNodeTypeHandler.OnSetName(HierarchyView view, in HierarchyNode node, string name)
    {
        if (!m_Mappings.TryGetValue(node, out var element))
        {
            CommandList.SetDirty();
            return false;
        }

        if (element is IPanelComponentRootElement rootElement)
            return TryRenameDocumentAsset(rootElement, name);

        if (!ValidateName(name))
        {
            CommandList.SetDirty();
            return false;
        }

        var elementVea = element.visualElementAsset;
        if (elementVea == null)
        {
            CommandList.SetDirty();
            return false;
        }
        SetElementNameCommand.Execute(CommandSources.Hierarchy, elementVea, name);
        element.name = name;
        return true;
    }

    // The rename field is seeded with the row label, so the file extension and the unsaved marker it shows are
    // tolerated and stripped before the asset file is renamed. ObjectNames.SetNameSmart renames through the same
    // undoable path as the Project window, unlike AssetDatabase.RenameAsset which is not on the Undo stack; it
    // keeps the extension the asset already has and warns on failure itself.
    bool TryRenameDocumentAsset(IPanelComponentRootElement rootElement, string name)
    {
        var document = rootElement.panelComponent?.visualTreeAsset;
        var path = document != null ? AssetDatabase.GetAssetPath(document) : null;
        if (string.IsNullOrEmpty(path))
        {
            CommandList.SetDirty();
            return false;
        }

        name = name.Trim();
        if (name.EndsWith('*'))
            name = name[..^1].TrimEnd();

        var extension = Path.GetExtension(path);
        if (extension.Length > 0 && name.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            name = name[..^extension.Length];

        if (string.IsNullOrEmpty(name)
            || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || name == Path.GetFileNameWithoutExtension(path))
        {
            CommandList.SetDirty();
            return false;
        }

        ObjectNames.SetNameSmart(document, name);
        CommandList.SetDirty();

        return Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(document)) == name;
    }

    /// <summary>
    /// Called when setting a new name for a <see cref="VisualElement"/> to determine if the name is valid or not.
    /// </summary>
    /// <param name="name">The new name.</param>
    /// <returns><see langword="true"/> if the name is valid; <see langword="false"/> otherwise.</returns>
    protected virtual bool ValidateName(string name)
    {
        return elementNameRegex.IsMatch(name);
    }

    string IHierarchyEditorNodeTypeHandler.GetDisplayNameOverride(HierarchyView view, in HierarchyNode node)
    {
        if (!m_Mappings.TryGetValue(node, out var element) || element == null)
            return "<null>";

        switch (element)
        {
            case IPanelComponentRootElement rootElement:
                var vta = rootElement.panelComponent.visualTreeAsset;
                if (!vta)
                    return "<none>.uxml";

                var path = AssetDatabase.GetAssetPath(vta);
                var fileName = !string.IsNullOrEmpty(path)
                    ? Path.GetFileName(path)
                    : string.IsNullOrEmpty(vta.name)
                        ? "<unsaved file>.uxml"
                        : $"{vta.name}.uxml";

                return UIAssetRegistry.LiveInstance?.IsDirty(vta) == true
                    ? fileName + "*"
                    : fileName;

            default:
                return string.IsNullOrEmpty(element.name)
                    ? string.Empty
                    : $"#{element.name}";
        }
    }

    bool IHierarchyEditorNodeTypeHandler.CanDuplicate(HierarchyView view)
    {
        if (StageStrategy.IsReadOnly)
            return false;

        using var memoryOwner = GetSelection(view, out var selection);
        using var assetsHandle = ListPool<VisualElementAsset>.Get(out var assets);
        FilterSelection(view, in selection, assets);
        if (assets.Count == 0)
            return false;

        return DuplicateElementsCommand.Validate(NoAllocHelpers.CreateSpan(assets));
    }

    bool IHierarchyEditorNodeTypeHandler.OnDuplicate(HierarchyView view)
    {
        if (StageStrategy.IsReadOnly)
            return false;

        using var memoryOwner = GetSelection(view, out var selection);
        using var nodesHandle = ListPool<HierarchyNode>.Get(out var nodes);
        using var elementsHandle = ListPool<VisualElement>.Get(out var elements);

        FilterSelection(view, in selection, elements, nodes);

        if (elements.Count == 0)
            return false;

        var toDuplicate = new VisualElementAsset[elements.Count];
        for (var i = 0; i < elements.Count; ++i)
            toDuplicate[i] = elements[i].visualElementAsset;

        DuplicateElementsCommand.Execute(CommandSources.Hierarchy, toDuplicate);

        return true;
    }

    bool IHierarchyEditorNodeTypeHandler.CanDelete(HierarchyView view)
    {
        if (StageStrategy.IsReadOnly)
            return false;

        using var memoryOwner = GetSelection(view, out var selection);
        using var assetsHandle = ListPool<VisualElementAsset>.Get(out var assets);
        FilterSelection(view, in selection, assets);
        if (assets.Count == 0)
            return false;

        return RemoveElementsCommand.Validate(NoAllocHelpers.CreateSpan(assets));
    }

    bool IHierarchyEditorNodeTypeHandler.OnDelete(HierarchyView view)
    {
        if (StageStrategy.IsReadOnly)
            return false;

        using var memoryOwner = GetSelection(view, out var selection);
        using var assetsHandle = ListPool<VisualElementAsset>.Get(out var assets);
        FilterSelection(view, in selection, assets);

        if (assets.Count == 0)
            return false;

        RemoveElementsCommand.Execute(CommandSources.Hierarchy, assets.ToArray());
        return true;
    }

    bool IHierarchyEditorNodeTypeHandler.CanFindReferences(HierarchyView view)
    {
        // We currently don't support visual element references.
        return false;
    }

    bool IHierarchyEditorNodeTypeHandler.OnFindReferences(HierarchyView view)
    {
        // We currently don't support visual element references.
        return false;
    }

    bool IHierarchyEditorNodeTypeHandler.CanDoubleClick(HierarchyView view, in HierarchyNode node)
    {
        return m_Mappings.TryGetValue(in node, out var _);
    }

    bool IHierarchyEditorNodeTypeHandler.OnDoubleClick(HierarchyView view, in HierarchyNode node)
    {
        if (!m_Mappings.TryGetValue(node, out var element))
            return false;
        RequestFramingCommand.Execute(CommandSources.Hierarchy, element, orientToFace: false);
        return true;
    }

    void IHierarchyEditorNodeTypeHandler.GetTooltip(HierarchyViewItem item, bool isFiltering, StringBuilder tooltip)
    {
        if (item == null)
            return;

        if (m_Mappings.TryGetValue(item.Node, out var element))
        {
            tooltip.Clear();
            tooltip.Append(isFiltering ? Hierarchy.GetPath(in item.Node) : element.tooltip);
        }
    }


    void IHierarchyEditorNodeTypeHandler.PopulateContextMenu(HierarchyView view, HierarchyViewItem item, DropdownMenu menu)
    {
        if (item == null)
        {
            StageStrategy.PopulateStageContextMenu(view, menu, this);
            return;
        }

        if (m_Mappings.TryGetValue(item.Node, out var element))
            PopulateContextMenu(view, in item.Node, element, menu);
    }

    void IHierarchyExtendCreateMenu.PopulateCreateMenu(DropdownMenu menu) => StageStrategy.PopulateCreateMenu(menu);

    /// <summary>
    /// Append context menu for a given hierarchy node.
    /// </summary>
    /// <param name="view">The selected <see cref="HierarchyView"/>.</param>
    /// <param name="node">The hierarchy node.</param>
    /// <param name="element">The <see cref="VisualElement"/>.</param>
    /// <param name="menu">The <see cref="DropdownMenu"/> to populate with.</param>
    protected virtual void PopulateContextMenu(HierarchyView view, in HierarchyNode node, VisualElement element, DropdownMenu menu)
    {
        StageStrategy.PopulateContextMenu(view, in node, element, menu, this);
    }

    bool IHierarchyEditorNodeTypeHandler.AcceptParent(HierarchyView view, in HierarchyNode parent)
    {
        if (StageStrategy.IsReadOnly)
            return false;

        if (parent == Hierarchy.Root)
            return StageStrategy.AcceptRootAsParent;

        var handler = view.ViewModel.GetNodeTypeHandlerBase(in parent);
        if (handler != this)
            return false;

        if (TryGetElementFromNode(parent, out var physicalParentElement))
        {
            var logicalParent = GetLogicalParentFromPhysicalParent(physicalParentElement);
            if (TryGetNodeFromElement(logicalParent, out var logicalParentNode))
            {
                return AcceptParent(view, in logicalParentNode, logicalParent);
            }
        }

        return false;
    }

    protected static VisualElement GetLogicalParentFromPhysicalParent(VisualElement physicalParent)
    {
        return physicalParent.GetFirstAncestorWhere(ve => ve.contentContainer == physicalParent) ?? physicalParent;
    }

    bool IHierarchyEditorNodeTypeHandler.AcceptChild(HierarchyView view, in HierarchyNode child)
    {
        if (StageStrategy.IsReadOnly)
            return false;

        var handler = view.ViewModel.GetNodeTypeHandlerBase(in child);
        if (handler != this)
            return false;

        return TryGetElementFromNode(child, out var childElement) && AcceptChild(view, in child, childElement);
    }

    bool IHierarchyEditorNodeTypeHandler.CanStartDrag(HierarchyView view, ReadOnlySpan<HierarchyNode> nodes)
    {
        // This requires custom handling compared to the other end points because returning false here
        // will prevent drag and drop of other handlers too. This is an issue that will be fixed on the hierarchy's side.
        using var memoryOwner = GetSelection(view, out var selection);
        return selection.Type switch
        {
            // No elements selected, revert to default handling.
            SelectionContext.SelectionType.None => true,
            // Mixed selection, disallow dragging.
            SelectionContext.SelectionType.Mixed => false,
            // We're only dragging visual element here.
            SelectionContext.SelectionType.All => CanStartDrag(view, in selection),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    void IHierarchyEditorNodeTypeHandler.OnStartDrag(in HierarchyViewDragAndDropSetupData data) =>
        InitializeDrag(in data);

    DragVisualMode IHierarchyEditorNodeTypeHandler.CanReorder(in HierarchyViewDragAndDropHandlingData data) => HandleDrop(in data, false);

    DragVisualMode IHierarchyEditorNodeTypeHandler.OnReorder(in HierarchyViewDragAndDropHandlingData data) => HandleDrop(in data, true);

    DragVisualMode IHierarchyEditorNodeTypeHandler.CanAcceptDrop(in HierarchyViewDragAndDropHandlingData data) => HandleDrop(in data, false);

    DragVisualMode IHierarchyEditorNodeTypeHandler.OnAcceptDrop(in HierarchyViewDragAndDropHandlingData data) => HandleDrop(in data, true);

    protected bool CanDoHierarchyOperation(HierarchyView view, in SelectionContext selection)
    {
        if (selection.Type != SelectionContext.SelectionType.All)
            return false;

        for (var i = 0; i < selection.SelectionCount; ++i)
        {
            if (!TryGetElementFromNode(selection.Selection[i], out var element))
                continue;

            if (StageStrategy.IsFullyEditable(element))
                return true;
        }

        return false;
    }

    void FilterSelection(HierarchyView view, in SelectionContext selection, List<VisualElementAsset> assets,
        bool requireFullyEditable = true)
    {
        using var elementsHandle = ListPool<VisualElement>.Get(out var elements);
        using var nodesHandle = ListPool<HierarchyNode>.Get(out var nodes);
        FilterSelection(view, in selection, elements, nodes, requireFullyEditable);
        foreach (var element in elements)
            assets.Add(element.visualElementAsset);
    }

    protected void FilterSelection(HierarchyView view, in SelectionContext selection, List<VisualElement> elements,
        List<HierarchyNode> nodes, bool requireFullyEditable = true)
    {
        using var seenAssetsHandle = HashSetPool<VisualElementAsset>.Get(out var seenAssets);
        for (var i = 0; i < selection.SelectionCount; ++i)
        {
            if (!TryGetElementFromNode(selection.Selection[i], out var element))
                continue;

            if (requireFullyEditable && !StageStrategy.IsFullyEditable(element))
                continue;

            var vea = element.visualElementAsset;
            if (vea == null || !seenAssets.Add(vea))
                continue;

            if (elements.Count > 0 && elements[^1].FindCommonAncestor(element) == elements[^1])
                continue;

            nodes.Add(selection.Selection[i]);
            elements.Add(element);
        }
    }

    VisualElement FindElementFromAsset(VisualElementAsset asset)
    {
        foreach (var element in m_Mappings.MappedElements)
        {
            if (element.visualElementAsset == asset)
                return element;
        }
        return null;
    }

    void FindAllElementsFromAsset(VisualElementAsset asset, List<VisualElement> results)
    {
        foreach (var element in m_Mappings.MappedElements)
        {
            if (element.visualElementAsset == asset)
                results.Add(element);
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
