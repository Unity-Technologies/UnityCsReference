// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using Unity.Hierarchy;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal class StyleSheetEditingNodeTypeHandler : StyleSheetNodeTypeHandler, IHierarchyEditorNodeTypeHandler
{
    internal static readonly string DraggedRulesKey = "StyleSheetEditingNodeTypeHandler.DraggedRules";
    const string DraggedStyleSheetsKey = "StyleSheetEditingNodeTypeHandler.DraggedStyleSheets";

    bool m_IsEnteringStagingMode;

    public StyleSheetEditingNodeTypeHandler()
    {
        isReadonly = false;
    }

    public void SetEnteringStagingMode()
    {
        m_IsEnteringStagingMode = true;
    }

    void FilterSelection(HierarchyView view, in SelectionContext selection, List<Node> styleNodes)
    {
        for (var i = 0; i < selection.SelectionCount; ++i)
        {
            if (!Mappings.TryGetValue(selection.Selection[i], out var styleNode))
                continue;

            if (styleNode.Rule != null)
                styleNodes.Add(styleNode);
        }
    }

    bool CanDoHierarchyOperation(HierarchyView view, in SelectionContext selection)
    {
        if (selection.Type != SelectionContext.SelectionType.All)
            return false;

        for (var i = 0; i < selection.SelectionCount; ++i)
        {
            if (!Mappings.TryGetValue(selection.Selection[i], out var styleNode))
                continue;

            if (styleNode.Rule != null)
                return true;
        }

        return false;
    }

    protected override void ViewModelPostUpdate(HierarchyViewModel viewModel)
    {
        base.ViewModelPostUpdate(viewModel);

        // Expand all stylesheet nodes when first entering staging mode
        if (m_IsEnteringStagingMode)
        {
            // Expand all stylesheets by iterating through hierarchy root children
            viewModel.SetFlags(HierarchyNodeFlags.Expanded);
            m_IsEnteringStagingMode = false;
        }
        // Expand only newly added stylesheet nodes
        else if (NewlyAddedStyleSheets.Count > 0)
        {
            foreach (var stylesheet in NewlyAddedStyleSheets)
            {
                if (Mappings.TryGetValue(stylesheet, out var hierarchyNode))
                {
                    viewModel.SetFlags(hierarchyNode, HierarchyNodeFlags.Expanded);
                }
            }
        }
        NewlyAddedStyleSheets.Clear();
    }

    IMemoryOwner<HierarchyNode> GetSelection(HierarchyView view, out SelectionContext selectionContext)
    {
        var selectionCount = view.ViewModel.HasFlagsCount(HierarchyNodeFlags.Selected);
        var owner = MemoryPool<HierarchyNode>.Shared.Rent(selectionCount);
        var selection = owner.Memory.Span[..selectionCount];
        view.ViewModel.GetNodesWithFlags(HierarchyNodeFlags.Selected, selection);

        var containsStyleSheets = false;
        foreach (var node in selection)
        {
            if (!IsStyleSheet(node))
                continue;

            containsStyleSheets = true;
            break;
        }

        SelectionContext.SelectionType type;
        if (selectionCount == 0)
            type = SelectionContext.SelectionType.None;
        else if (containsStyleSheets)
            type = SelectionContext.SelectionType.Mixed;
        else
            type = SelectionContext.SelectionType.All;

        selectionContext = new SelectionContext(selection, selectionCount, type);
        return owner;
    }

    #region IHierarchyEditorNodeTypeHandler

    // IHierarchyEditorNodeTypeHandler - Drag and Drop implementation
    bool IHierarchyEditorNodeTypeHandler.CanStartDrag(HierarchyView view, ReadOnlySpan<HierarchyNode> nodes)
    {
        if (nodes.Length == 0)
            return false;

        // Check if all nodes are stylesheet root nodes
        bool allStyleSheets = true;
        bool allSelectors = true;

        foreach (var node in nodes)
        {
            if (!Mappings.TryGetValue(node, out var styleNode))
                return false;

            if (styleNode.Rule != null)
                allStyleSheets = false; // Has a rule, so it's a selector
            else
                allSelectors = false; // No rule, so it's a stylesheet
        }

        // Allow dragging if all nodes are of the same type (all stylesheets OR all selectors)
        return allStyleSheets || allSelectors;
    }

    void IHierarchyEditorNodeTypeHandler.OnStartDrag(in HierarchyViewDragAndDropSetupData data)
    {
        // Check if we're dragging stylesheet nodes or selector nodes
        if (data.Nodes.Length == 0)
            return;

        var firstNode = data.Nodes[0];
        if (!Mappings.TryGetValue(firstNode, out var firstStyleNode))
            return;

        if (firstStyleNode.Rule == null)
        {
            // Dragging stylesheet nodes
            var draggedStyleSheets = new List<StyleSheet>();
            foreach (var node in data.Nodes)
            {
                if (Mappings.TryGetValue(node, out var styleNode) && styleNode.StyleSheet != null)
                {
                    draggedStyleSheets.Add(styleNode.StyleSheet);
                }
            }
            data.SetGenericData(DraggedStyleSheetsKey, draggedStyleSheets);
        }
        else
        {
            // Dragging rule nodes
            var draggedRules = new List<StyleRule>();
            foreach (var node in data.Nodes)
            {
                if (Mappings.TryGetValue(node, out var styleNode) && styleNode.Rule != null)
                {
                    draggedRules.Add(styleNode.Rule);
                }
            }
            data.SetGenericData(DraggedRulesKey, draggedRules);
        }
    }

    DragVisualMode IHierarchyEditorNodeTypeHandler.CanDrop(in HierarchyViewDragAndDropHandlingData data)
    {
        return HandleDrop(in data, performDrop: false);
    }

    DragVisualMode IHierarchyEditorNodeTypeHandler.OnDrop(in HierarchyViewDragAndDropHandlingData data)
    {
        return HandleDrop(in data, performDrop: true);
    }

    DragVisualMode HandleDrop(in HierarchyViewDragAndDropHandlingData data, bool performDrop)
    {
        var visualMode = DragVisualMode.None;
        visualMode = HandleStyleSheetDrop(in data, performDrop);
        if (visualMode != DragVisualMode.None)
            return visualMode;

        visualMode = HandleStyleRuleDrop(in data, performDrop);
        if (visualMode != DragVisualMode.None)
            return visualMode;

        visualMode = HandleExternalAssetDrop(in data, performDrop);
        if (visualMode != DragVisualMode.None)
            return visualMode;

        return DragVisualMode.None;
    }

    DragVisualMode HandleStyleSheetDrop(in HierarchyViewDragAndDropHandlingData data, bool performDrop)
    {
        var draggedStyleSheets = data.GetGenericData(DraggedStyleSheetsKey) as List<StyleSheet>;

        if (draggedStyleSheets == null || draggedStyleSheets.Count == 0)
            return DragVisualMode.None;

        // Stylesheets can only be dropped on Root, not on other stylesheets or selectors
        if (data.Parent != Hierarchy.Root)
            return DragVisualMode.Rejected;

        // Get the current VTA
        var stage = UnityEditor.SceneManagement.StageUtility.GetCurrentStage();
        if (stage is not VisualElementEditingStage uiStage)
            return DragVisualMode.Rejected;

        var vta = uiStage.EditedVisualTreeAsset;
        if (vta == null)
            return DragVisualMode.Rejected;

        var visualTree = vta.visualTreeNoAlloc;

        if (!performDrop)
            return DragVisualMode.Move;

        // Build the reordered list using pooled collections
        using var _allSheets = ListPool<StyleSheet>.Get(out var allStyleSheets);
        using var _draggedSheets = HashSetPool<StyleSheet>.Get(out var draggedSheets);
        using var _sheetsToInsert = ListPool<StyleSheet>.Get(out var sheetsToInsert);

        allStyleSheets.AddRange(visualTree.stylesheets);

        // Collect dragged stylesheets
        foreach (var styleSheet in draggedStyleSheets)
        {
            draggedSheets.Add(styleSheet);
            sheetsToInsert.Add(styleSheet);
        }

        // Calculate adjusted insertion index
        var adjustedInsertIndex = data.ChildIndex;
        for (var i = 0; i < data.ChildIndex && i < allStyleSheets.Count; i++)
        {
            if (draggedSheets.Contains(allStyleSheets[i]))
                adjustedInsertIndex--;
        }

        // Remove dragged stylesheets from their current positions
        allStyleSheets.RemoveAll(sheet => draggedSheets.Contains(sheet));

        // Insert at new position
        adjustedInsertIndex = Math.Clamp(adjustedInsertIndex, 0, allStyleSheets.Count);
        allStyleSheets.InsertRange(adjustedInsertIndex, sheetsToInsert);

        // Execute the set stylesheets command
        var command = new SetStyleSheetsCommand(vta, allStyleSheets);
        return command.Execute() ? DragVisualMode.Move : DragVisualMode.Rejected;
    }

    DragVisualMode HandleStyleRuleDrop(in HierarchyViewDragAndDropHandlingData data, bool performDrop)
    {
        var draggedRules = data.GetGenericData(DraggedRulesKey) as List<StyleRule>;

        if (draggedRules == null || draggedRules.Count == 0)
            return DragVisualMode.None;

        if (!Mappings.TryGetValue(data.Parent, out var parentStyleNode))
            return DragVisualMode.Rejected;

        // Selectors can only be dropped on stylesheet root nodes
        if (parentStyleNode.Rule != null)
            return DragVisualMode.Rejected;

        if (!performDrop)
            return DragVisualMode.Move;

        // Calculate adjusted insertion index to account for items being removed first
        var rules = parentStyleNode.StyleSheet.rules;
        var adjustedIndex = data.ChildIndex;

        using var _draggedSet = HashSetPool<StyleRule>.Get(out var draggedSet);
        foreach (var rule in draggedRules)
            draggedSet.Add(rule);

        for (var i = 0; i < data.ChildIndex && i < rules.Length; i++)
        {
            if (draggedSet.Contains(rules[i]))
                adjustedIndex--;
        }

        var command = new ReorderStyleRulesCommand(parentStyleNode.StyleSheet, draggedRules, adjustedIndex);
        if (!command.Execute())
            return DragVisualMode.Rejected;

        // Get the refreshed rules from the stylesheet from the target position
        using var _ = ListPool<StyleRule>.Get(out var freshRules);
        rules = parentStyleNode.StyleSheet.rules;
        var startIndex = adjustedIndex == -1 || adjustedIndex >= rules.Length ? rules.Length - draggedRules.Count : adjustedIndex;

        for (var i = 0; i < draggedRules.Count; i++)
        {
            var index = startIndex + i;
            if (index >= 0 && index < rules.Length)
                freshRules.Add(rules[index]);
        }

        Window?.SelectAndScrollTo(freshRules);
        return DragVisualMode.Move;
    }

    DragVisualMode HandleExternalAssetDrop(in HierarchyViewDragAndDropHandlingData data, bool performDrop)
    {
        // Stylesheets can only be dropped on Root, not on other stylesheets or selectors
        if (data.Parent != Hierarchy.Root)
            return DragVisualMode.Rejected;

        // Filter for USS files only using pooled list
        using var _ussFiles = ListPool<string>.Get(out var ussFiles);

        foreach (var path in data.Paths)
        {
            if (string.IsNullOrEmpty(path))
                continue;

            var assetType = UnityEditor.AssetDatabase.GetMainAssetTypeAtPath(path);
            if (typeof(StyleSheet).IsAssignableFrom(assetType))
            {
                var styleSheet = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                if (styleSheet != null && !styleSheet.importedWithErrors)
                {
                    ussFiles.Add(path);
                }
            }
        }

        if (ussFiles.Count == 0)
            return DragVisualMode.None;

        // Add USS files to the current VTA
        var stage = UnityEditor.SceneManagement.StageUtility.GetCurrentStage();
        if (stage is not VisualElementEditingStage uiStage)
            return DragVisualMode.Rejected;

        var vta = uiStage.EditedVisualTreeAsset;
        if (vta == null)
            return DragVisualMode.Rejected;

        if (!performDrop)
            return DragVisualMode.Copy;

        var existingStyleSheets = vta.visualTreeNoAlloc?.stylesheets;

        // Calculate insertion index based on drop position
        var insertIndex = data.ChildIndex;

        // Add each USS file to the VTA at the specified position
        foreach (var ussPath in ussFiles)
        {
            var styleSheet = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);

            // Skip if this stylesheet is already referenced
            if (existingStyleSheets != null && existingStyleSheets.Contains(styleSheet))
                continue;

            new AddStyleSheetCommand(vta, ussPath, insertIndex).Execute();

            var wasAdded = false;
            foreach (var stylesheet in vta.stylesheets)
            {
                if (stylesheet != styleSheet)
                    continue;

                wasAdded = true;
                break;
            }

            if (wasAdded && insertIndex > 0)
                insertIndex++;
        }

        return DragVisualMode.Copy;
    }

    bool IHierarchyEditorNodeTypeHandler.CanCopy(HierarchyView view)
    {
        using var memoryOwner = GetSelection(view, out var selection);
        return CanDoHierarchyOperation(view, in selection);
    }

    bool IHierarchyEditorNodeTypeHandler.OnCopy(HierarchyView view)
    {
        if (isReadonly)
            return false;

        Clipboard.SystemCopyBuffer = null;

        using var memoryOwner = GetSelection(view, out var selection);
        using var _ = ListPool<Node>.Get(out var nodes);

        FilterSelection(view, in selection, nodes);

        if (nodes.Count == 0)
            return false;

        for (var i = 0; i < nodes.Count; ++i)
        {
            Clipboard.SystemCopyBuffer += m_Exporter.ToUssString(nodes[i].StyleSheet, nodes[i].Rule);
        }

        return true;
    }

    bool IHierarchyEditorNodeTypeHandler.CanPaste(HierarchyView view)
    {
        return Clipboard.IsSystemCopyBufferUss();
    }

    bool IHierarchyEditorNodeTypeHandler.OnPaste(HierarchyView view)
    {
        if (isReadonly)
            return false;

        if (!Clipboard.IsSystemCopyBufferUss())
            return false;

        try
        {
            var styleSheet = Window.ActiveStyleSheet;
            var originalRuleCount = styleSheet.rules.Length;

            new PasteStyleRuleCommand(Clipboard.SystemCopyBuffer, styleSheet).Execute();

            // Collect all newly pasted rule(s)
            using var _ = ListPool<StyleRule>.Get(out var pastedRules);
            for (var i = originalRuleCount; i < styleSheet.rules.Length; i++)
            {
                pastedRules.Add(styleSheet.rules[i]);
            }

            Window?.SelectAndScrollTo(pastedRules);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    bool IHierarchyEditorNodeTypeHandler.CanSetName(HierarchyView view, in HierarchyNode hierarchyNode)
    {
        if (isReadonly)
            return false;

        return Mappings.TryGetValue(hierarchyNode, out var styleNode) && styleNode.Rule != null;
    }

    bool IHierarchyEditorNodeTypeHandler.OnSetName(HierarchyView view, in HierarchyNode hierarchyNode, string name)
    {
        if (!Mappings.TryGetValue(hierarchyNode, out var node))
        {
            CommandList.SetDirty();
            return false;
        }

        var currentName = ((IHierarchyEditorNodeTypeHandler)this).GetDisplayName(view, hierarchyNode);
        if (string.CompareOrdinal(currentName, name) == 0)
            return true;

        var selectorStrings = name.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var selectorString in selectorStrings)
        {
            if (StyleSheetExtensions.ValidateSelector(selectorString, out var error))
                continue;

            Debug.LogError( $"Invalid selector string '{selectorString}': {error}.");
            CommandList.SetDirty();
            return false;
        }

        new RenameStyleRuleCommand(selectorStrings, node.Rule).Execute();

        // Force the selection object to refresh (even though reference didn't change)
        SelectionHandler.Remap([new StyleRuleRemap(node.Rule, node.Rule)]);
        return true;
    }

    string IHierarchyEditorNodeTypeHandler.GetDisplayName(HierarchyView view, in HierarchyNode hierarchyNode)
    {
        if (!Mappings.TryGetValue(hierarchyNode, out var styleNode) || !styleNode.StyleSheet)
            return "<null>";

        if (string.IsNullOrEmpty(styleNode.StyleSheet.name))
            return string.Empty;

        return styleNode.Rule != null ? m_Exporter.ToUssString(styleNode.StyleSheet, styleNode.Rule.complexSelectors, s_ExportOptions) : $"{styleNode.StyleSheet.name}.uss";
    }

    bool IHierarchyEditorNodeTypeHandler.CanDuplicate(HierarchyView view)
    {
        if (isReadonly)
            return false;

        using var memoryOwner = GetSelection(view, out var selection);
        return CanDoHierarchyOperation(view, in selection);
    }

    bool IHierarchyEditorNodeTypeHandler.OnDuplicate(HierarchyView view)
    {
        if (isReadonly)
            return false;

        using var memoryOwner = GetSelection(view, out var selection);
        using var _ = ListPool<Node>.Get(out var nodes);

        FilterSelection(view, in selection, nodes);

        if (nodes.Count == 0)
            return false;

        using var indicesAndSheets = ListPool<(StyleSheet, int)>.Get(out var targetPositions);
        var toDuplicate = new StyleRule[nodes.Count];
        for (var i = 0; i < nodes.Count; i++)
        {
            toDuplicate[i] = nodes[i].Rule;
        }

        new DuplicateStyleRuleCommand(toDuplicate).Execute();

        // Need to get the updated rules in order to apply the proper selection (and scroll)
        using var freshRules = ListPool<StyleRule>.Get(out var newRules);
        foreach (var node in nodes)
        {
            var styleSheet = node.StyleSheet;
            var originalIndex = Array.IndexOf(styleSheet.rules, node.Rule);

            if (originalIndex != -1 && originalIndex + 1 < styleSheet.rules.Length)
            {
                newRules.Add(styleSheet.rules[originalIndex + 1]);
            }
        }

        if (newRules.Count > 0)
            Window?.SelectAndScrollTo(newRules);

        return true;
    }

    bool IHierarchyEditorNodeTypeHandler.CanDelete(HierarchyView view)
    {
        if (isReadonly)
            return false;

        using var memoryOwner = GetSelection(view, out var selection);
        return CanDoHierarchyOperation(view, in selection);
    }

    bool IHierarchyEditorNodeTypeHandler.OnDelete(HierarchyView view)
    {
        if (isReadonly)
            return false;

        using var memoryOwner = GetSelection(view, out var selection);
        using var _ = ListPool<Node>.Get(out var nodes);

        FilterSelection(view, in selection, nodes);

        if (nodes.Count == 0)
            return false;

        var toRemove = new StyleRule[nodes.Count];
        for (var i = 0; i < nodes.Count; ++i)
        {
            toRemove[i] = nodes[i].Rule;
        }

        new RemoveStyleRulesCommand(toRemove).Execute();
        return true;
    }

    bool IHierarchyEditorNodeTypeHandler.CanDoubleClick(HierarchyView view, in HierarchyNode node) => true;

    bool IHierarchyEditorNodeTypeHandler.OnDoubleClick(HierarchyView view, in HierarchyNode node)
    {
        if (!Mappings.TryGetValue(node, out var styleNode) || styleNode.Rule == null)
            return true;

        view.BeginRename(in node);
        return true;
    }

    void IHierarchyEditorNodeTypeHandler.PopulateContextMenu(HierarchyView view, HierarchyViewItem item, DropdownMenu menu)
    {
        if (item == null)
            return;

        if (Mappings.TryGetValue(item.Node, out _))
            StyleSheetContextMenuUtility.PopulateMenu(view, in item.Node, menu, this);
    }

    bool IHierarchyEditorNodeTypeHandler.CanCut(HierarchyView view) => false;
    bool IHierarchyEditorNodeTypeHandler.OnCut(HierarchyView view) => false;
    bool IHierarchyEditorNodeTypeHandler.CanPasteAsChild(HierarchyView view) => false;
    bool IHierarchyEditorNodeTypeHandler.OnPasteAsChild(HierarchyView view, bool keepWorldPos) => false;
    bool IHierarchyEditorNodeTypeHandler.CanFindReferences(HierarchyView view) => false;
    bool IHierarchyEditorNodeTypeHandler.OnFindReferences(HierarchyView view) => false;
    void IHierarchyEditorNodeTypeHandler.GetTooltip(HierarchyViewItem item, bool isFiltering, StringBuilder tooltip) { }
    bool IHierarchyEditorNodeTypeHandler.AcceptParent(HierarchyView view, in HierarchyNode parent) => false;
    bool IHierarchyEditorNodeTypeHandler.AcceptChild (HierarchyView view, in HierarchyNode child) => false;

    #endregion
}
