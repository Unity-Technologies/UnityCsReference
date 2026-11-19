// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
    public static void FilterSelection(AuthoringContext context, List<VisualElementAsset> assets, bool requireFullyEditable = true)
        => FilterSelection(context, assets, null, requireFullyEditable);

    public static void FilterSelection(
        AuthoringContext context,
        List<VisualElementAsset> assets,
        List<VisualElement> liveElements,
        bool requireFullyEditable = true)
    {
        using var elementsHandle = ListPool<VisualElement>.Get(out var elements);
        foreach (var selectedId in context.Selection.EntityIds)
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
            if (requireFullyEditable && !context.GetEditFlags(element).IsFullyEditable())
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

    public static VisualElement GetFirstSelectedElement(AuthoringContext context, Func<VisualElement, bool> isInScope = null)
    {
        foreach (var selectedId in context.Selection.EntityIds)
            if (EditorUtility.EntityIdToObject(selectedId) is VisualElementSelection { Element: { } element } && element.panel != null
                && (isInScope == null || isInScope(element)))
                return element;
        return null;
    }

    public static bool CanPaste(AuthoringContext context, Func<VisualElement, bool> isInScope = null)
    {
        if (!VisualElementEditingUtility.TryResolvePasteParent(context, GetFirstSelectedElement(context, isInScope), out var parentElement, out var parentAsset))
            return false;
        return VisualElementEditingUtility.CanPasteContent(context, parentElement, parentAsset);
    }

    public static bool DoPaste(AuthoringContext context, CommandSources.CommandSource source, Func<VisualElement, bool> isInScope = null)
    {
        var selected = GetFirstSelectedElement(context, isInScope);
        if (!VisualElementEditingUtility.TryResolvePasteParent(context, selected, out var parentElement, out var parentAsset))
            return false;

        var clipboard = Clipboard.For(context);
        var cutElements = clipboard?.GetCutElements();

        if (cutElements != null && cutElements.Count > 0)
        {
            if (!VisualElementEditingUtility.CanMoveCutElements(cutElements, parentElement, parentAsset))
                return false;

            using var toPasteHandle = ListPool<VisualElementAsset>.Get(out var toPasteList);
            for (var i = 0; i < cutElements.Count; ++i)
                if (cutElements[i] != null && cutElements[i].visualTreeAsset != null)
                    toPasteList.Add(cutElements[i]);
            if (toPasteList.Count == 0)
                return false;

            var arr = toPasteList.ToArray();
            if (ReparentElementsCommand.Execute(source, parentAsset, -1, arr) != CommandExecutionStatus.Success)
                return false;
            UIToolkitStageUtility.RequestSelectionOnNextUpdate(context, arr);
            UIToolkitStageUtility.ScopePendingSelectionRequestsTo(context, parentElement);
            clipboard.Clear();
            return true;
        }

        return VisualElementEditingUtility.TryPasteCopied(context, source, parentElement, parentAsset);
    }

    // Duplicates the selection and points the selection request at the instance each copy was made in.
    public static bool DoDuplicate(AuthoringContext context, CommandSources.CommandSource source)
    {
        using var assetsHandle = ListPool<VisualElementAsset>.Get(out var assets);
        using var elementsHandle = ListPool<VisualElement>.Get(out var elements);
        FilterSelection(context, assets, elements);

        // Captured before Execute, which can replace the live elements.
        using var parentsHandle = ListPool<VisualElement>.Get(out var parents);
        foreach (var element in elements)
            parents.Add(element.parent);

        if (DuplicateElementsCommand.Execute(source, assets.ToArray(), context) != CommandExecutionStatus.Success)
            return false;

        UIToolkitStageUtility.ScopePendingSelectionRequestsTo(context, parents);
        return true;
    }

    // Appends the complete viewport context menu: edit actions, hierarchy editing actions, optional in-place
    // editor entry, open actions, template operations, and generate selector.
    // Paste behavior is caller-supplied. inPlaceEditor is only consulted when source == Viewport.
    public static void PopulateMenu(
        AuthoringContext context,
        DropdownMenu menu,
        CommandSources.CommandSource source,
        UICanvasInPlaceEditor inPlaceEditor = null,
        Func<VisualElement, bool> isInScope = null)
    {
        using var selectionHandle = ListPool<VisualElementAsset>.Get(out var filteredSelection);
        FilterSelection(context, filteredSelection);
        var selection = filteredSelection.ToArray();

        using var liveElementsHandle = ListPool<VisualElement>.Get(out var liveElements);
        using var liveElementsAssetsHandle = ListPool<VisualElementAsset>.Get(out var liveElementsAssets);
        FilterSelection(context, liveElementsAssets, liveElements, requireFullyEditable: false);
        var allAssets = liveElementsAssets.ToArray();

        PopulateEditActions(context, menu, selection, allAssets, CanPaste(context, isInScope),
            () => DoPaste(context, source, isInScope));

        // The Scene View also builds this menu, but the hierarchy editing shortcuts are not bound there.
        if (source == CommandSources.Viewport)
        {
            menu.AppendSeparator();
            KeyboardAuthoringShortcuts.AppendMenuActions(context, menu);
        }

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
        CollectPanelElements(context, rawSelected);
        StageContextMenuUtility.AppendGenerateSelectorAction(menu,
            rawSelected.Count == 1 ? rawSelected[0] : null, rawSelected.Count);
    }

    static void CollectPanelElements(AuthoringContext context, List<VisualElement> elements)
    {
        foreach (var selectedId in context.Selection.EntityIds)
            if (EditorUtility.EntityIdToObject(selectedId) is VisualElementSelection { Element: { } element } && element.panel != null)
                elements.Add(element);
    }

    static void PopulateEditActions(
        AuthoringContext context,
        DropdownMenu menu,
        VisualElementAsset[] selection,
        VisualElementAsset[] allAssets,
        bool canPaste,
        Action doPaste)
    {
        StageContextMenuUtility.AppendActionWithHotKey(menu, StageContextMenuUtility.Cut,
            () => CutElementsCommand.Execute(CommandSources.Menus, selection, context),
            CutElementsCommand.Validate(CommandSources.Menus, selection));
        StageContextMenuUtility.AppendActionWithHotKey(menu, StageContextMenuUtility.Copy,
            () => CopyElementsCommand.Execute(CommandSources.Menus, allAssets, context),
            CopyElementsCommand.Validate(CommandSources.Menus, allAssets));
        StageContextMenuUtility.AppendActionWithHotKey(menu, StageContextMenuUtility.Paste,
            doPaste, canPaste);
        StageContextMenuUtility.AppendActionWithHotKey(menu, StageContextMenuUtility.Duplicate,
            () => DoDuplicate(context, CommandSources.Menus),
            DuplicateElementsCommand.Validate(CommandSources.Menus, selection));
        StageContextMenuUtility.AppendActionWithHotKey(menu, StageContextMenuUtility.Delete,
            () => RemoveElementsCommand.Execute(CommandSources.Menus, selection),
            RemoveElementsCommand.Validate(CommandSources.Menus, selection));
    }
}
