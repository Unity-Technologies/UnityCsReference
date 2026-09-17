// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

// Viewport-specific shared context menu helpers, used by UIViewport and VisualElementToolContext.PopulateMenu.
// For the Hierarchy path see StageContextMenuUtility.
internal static class UIViewportContextMenuUtility
{
    public static void FilterSelection(List<VisualElementAsset> assets, bool requireFullyEditable = true)
        => FilterSelection(assets, null, requireFullyEditable);

    public static void FilterSelection(
        List<VisualElementAsset> assets,
        List<VisualElement> liveElements,
        bool requireFullyEditable = true)
    {
        using var elementsHandle = ListPool<VisualElement>.Get(out var elements);
        foreach (var selectedId in Selection.entityIds)
        {
            if (EditorUtility.EntityIdToObject(selectedId) is VisualElementSelection { Element: { } element } &&
                element.panel != null && element.visualElementAsset != null)
                elements.Add(element);
        }

        using var seenAssetsHandle = HashSetPool<VisualElementAsset>.Get(out var seenAssets);
        using var selectionSetHandle = HashSetPool<VisualElement>.Get(out var selectionSet);
        foreach (var e in elements)
            selectionSet.Add(e);

        foreach (var element in elements)
        {
            if (requireFullyEditable && !UIToolkitStageUtility.GetEditFlags(element).IsFullyEditable())
                continue;

            var parent = element.hierarchy.parent;
            while (parent != null && !selectionSet.Contains(parent))
                parent = parent.hierarchy.parent;

            if (parent == null && seenAssets.Add(element.visualElementAsset))
            {
                assets?.Add(element.visualElementAsset);
                liveElements?.Add(element);
            }
        }
    }

    public static VisualElement GetFirstSelectedElement(IPanel scopePanel = null)
    {
        foreach (var selectedId in Selection.entityIds)
            if (EditorUtility.EntityIdToObject(selectedId) is VisualElementSelection { Element: { } element } && element.panel != null
                && (scopePanel == null || element.panel == scopePanel))
                return element;
        return null;
    }

    public static bool CanPaste(IPanel scopePanel = null)
    {
        if (!VisualElementEditingUtility.TryResolvePasteParent(GetFirstSelectedElement(scopePanel), out _, out _))
            return false;
        return VisualElementEditingUtility.CanPasteContent();
    }

    public static bool DoPaste(CommandSources.CommandSource source, IPanel scopePanel = null)
    {
        var selected = GetFirstSelectedElement(scopePanel);
        if (!VisualElementEditingUtility.TryResolvePasteParent(selected, out var parentElement, out var parentAsset))
            return false;

        var targetDocument = parentAsset.visualTreeAsset;
        var clipboard = Clipboard.GetClipboardForStage();
        var cutElements = clipboard?.GetCutElements();

        if (cutElements != null && cutElements.Count > 0)
        {
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
                if (cutElements[i] != null) toPasteList.Add(cutElements[i]);
            if (toPasteList.Count == 0)
                return false;

            var arr = toPasteList.ToArray();
            UIToolkitStageUtility.RequestSelectionOnNextUpdate(arr);
            ReparentElementsCommand.Execute(source, parentAsset, -1, arr);
            UIToolkitStageUtility.ScopePendingSelectionRequestsTo(parentElement);
            clipboard.Clear();
            return true;
        }

        return VisualElementEditingUtility.TryPasteCopied(source, parentElement, parentAsset);
    }

    // Appends the complete viewport context menu: edit actions, optional in-place editor entry,
    // open actions, template operations, and generate selector.
    // Paste behavior is caller-supplied. inPlaceEditor is only consulted when source == Viewport.
    public static void PopulateMenu(
        DropdownMenu menu,
        CommandSources.CommandSource source,
        UICanvasInPlaceEditor inPlaceEditor = null,
        IPanel scopePanel = null)
    {
        using var selectionHandle = ListPool<VisualElementAsset>.Get(out var filteredSelection);
        FilterSelection(filteredSelection);
        var selection = filteredSelection.ToArray();

        using var liveElementsHandle = ListPool<VisualElement>.Get(out var liveElements);
        using var liveElementsAssetsHandle = ListPool<VisualElementAsset>.Get(out var liveElementsAssets);
        FilterSelection(liveElementsAssets, liveElements, requireFullyEditable: false);
        var allAssets = liveElementsAssets.ToArray();

        PopulateEditActions(menu, selection, allAssets, CanPaste(scopePanel), () => DoPaste(source, scopePanel));

        if (source == CommandSources.Viewport && inPlaceEditor != null)
        {
            var editElement = liveElements.Count == 1 ? liveElements[0] : null;
            if (inPlaceEditor.HasEditor(editElement))
            {
                menu.AppendSeparator();
                var canEdit = inPlaceEditor.CanOpenEditor(editElement);
                var menuText = canEdit
                    ? StageContextMenuUtility.EditInPlace
                    : $"{StageContextMenuUtility.EditInPlace} (bound)";
                StageContextMenuUtility.AppendActionWithHotKey(menu, menuText,
                    () => inPlaceEditor.TryOpenEditor(editElement),
                    enabled: canEdit);
            }
        }

        if (liveElements.Count > 0)
        {
            menu.AppendSeparator();
            StageContextMenuUtility.PopulateOpenActions(liveElements[0], menu, source);
            StageContextMenuUtility.AppendMainStageActions(liveElements[0], menu);
        }
        menu.AppendSeparator();
        StageContextMenuUtility.PopulateTemplateOperations(menu, liveElements, CommandSources.Menus, includeShowInProject: false);

        using var rawHandle = ListPool<VisualElement>.Get(out var rawSelected);
        CollectPanelElements(rawSelected);
        StageContextMenuUtility.AppendGenerateSelectorAction(menu,
            rawSelected.Count == 1 ? rawSelected[0] : null, rawSelected.Count);
    }

    static void CollectPanelElements(List<VisualElement> elements)
    {
        foreach (var selectedId in Selection.entityIds)
            if (EditorUtility.EntityIdToObject(selectedId) is VisualElementSelection { Element: { } element } && element.panel != null)
                elements.Add(element);
    }

    static void PopulateEditActions(
        DropdownMenu menu,
        VisualElementAsset[] selection,
        VisualElementAsset[] allAssets,
        bool canPaste,
        Action doPaste)
    {
        StageContextMenuUtility.AppendActionWithHotKey(menu, StageContextMenuUtility.Cut,
            () => CutElementsCommand.Execute(CommandSources.Menus, selection),
            CutElementsCommand.Validate(CommandSources.Menus, selection));
        StageContextMenuUtility.AppendActionWithHotKey(menu, StageContextMenuUtility.Copy,
            () => CopyElementsCommand.Execute(CommandSources.Menus, allAssets),
            CopyElementsCommand.Validate(CommandSources.Menus, allAssets));
        StageContextMenuUtility.AppendActionWithHotKey(menu, StageContextMenuUtility.Paste,
            doPaste, canPaste);
        StageContextMenuUtility.AppendActionWithHotKey(menu, StageContextMenuUtility.Duplicate,
            () => DuplicateElementsCommand.Execute(CommandSources.Menus, selection),
            DuplicateElementsCommand.Validate(CommandSources.Menus, selection));
        StageContextMenuUtility.AppendActionWithHotKey(menu, StageContextMenuUtility.Delete,
            () => RemoveElementsCommand.Execute(CommandSources.Menus, selection),
            RemoveElementsCommand.Validate(CommandSources.Menus, selection));
    }
}
