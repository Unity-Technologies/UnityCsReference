// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using Unity.Hierarchy;
using UnityEngine.UIElements;
using UnityEngine.Pool;

namespace Unity.UIToolkit.Editor;

internal class StyleSheetEditingNodeTypeHandler : StyleSheetNodeTypeHandler, IHierarchyEditorNodeTypeHandler
{
    const string DraggedRulesKey = "StyleSheetEditingNodeTypeHandler.DraggedRules";
    const string DraggedStyleSheetsKey = "StyleSheetEditingNodeTypeHandler.DraggedStyleSheets";

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

        var command = new ReorderStyleRulesCommand(parentStyleNode.StyleSheet, draggedRules, data.ChildIndex);
        return command.Execute() ? DragVisualMode.Move : DragVisualMode.Rejected;
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

            var command = new AddStyleSheetCommand(vta, ussPath, insertIndex);
            if (command.Execute())
            {
                // Increment index for next insertion to maintain order
                if (insertIndex > 0)
                    insertIndex++;
            }
        }

        return DragVisualMode.Copy;
    }

    // IHierarchyEditorNodeTypeHandler - Stub implementations for other required methods
    bool IHierarchyEditorNodeTypeHandler.CanCut(HierarchyView view) => false;
    bool IHierarchyEditorNodeTypeHandler.OnCut(HierarchyView view) => false;
    bool IHierarchyEditorNodeTypeHandler.CanCopy(HierarchyView view) => false;
    bool IHierarchyEditorNodeTypeHandler.OnCopy(HierarchyView view) => false;
    bool IHierarchyEditorNodeTypeHandler.CanPaste(HierarchyView view) => false;
    bool IHierarchyEditorNodeTypeHandler.OnPaste(HierarchyView view) => false;
    bool IHierarchyEditorNodeTypeHandler.CanPasteAsChild(HierarchyView view) => false;
    bool IHierarchyEditorNodeTypeHandler.OnPasteAsChild(HierarchyView view, bool keepWorldPos) => false;
    bool IHierarchyEditorNodeTypeHandler.CanSetName(HierarchyView view, in HierarchyNode node) => false;
    bool IHierarchyEditorNodeTypeHandler.OnSetName(HierarchyView view, in HierarchyNode node, string name) => false;
    string IHierarchyEditorNodeTypeHandler.GetDisplayName(HierarchyView view, in HierarchyNode node)
    {
        if (!Mappings.TryGetValue(node, out var styleNode))
            return string.Empty;

        // For stylesheet root nodes, return the stylesheet name
        if (styleNode.StyleSheet != null && styleNode.Rule == null)
            return styleNode.StyleSheet.name;

        return Hierarchy.GetName(node);
    }
    bool IHierarchyEditorNodeTypeHandler.CanDuplicate(HierarchyView view) => false;
    bool IHierarchyEditorNodeTypeHandler.OnDuplicate(HierarchyView view) => false;
    bool IHierarchyEditorNodeTypeHandler.CanDelete(HierarchyView view) => false;
    bool IHierarchyEditorNodeTypeHandler.OnDelete(HierarchyView view) => false;
    bool IHierarchyEditorNodeTypeHandler.CanFindReferences(HierarchyView view) => false;
    bool IHierarchyEditorNodeTypeHandler.OnFindReferences(HierarchyView view) => false;
    bool IHierarchyEditorNodeTypeHandler.CanDoubleClick(HierarchyView view, in HierarchyNode node) => false;
    bool IHierarchyEditorNodeTypeHandler.OnDoubleClick(HierarchyView view, in HierarchyNode node) => false;
    void IHierarchyEditorNodeTypeHandler.GetTooltip(HierarchyViewItem item, bool isFiltering, StringBuilder tooltip) { }
    void IHierarchyEditorNodeTypeHandler.PopulateContextMenu(HierarchyView view, HierarchyViewItem item, DropdownMenu menu) { }
    bool IHierarchyEditorNodeTypeHandler.AcceptParent(HierarchyView view, in HierarchyNode parent) => false;
    bool IHierarchyEditorNodeTypeHandler.AcceptChild (HierarchyView view, in HierarchyNode child) => false;
}
