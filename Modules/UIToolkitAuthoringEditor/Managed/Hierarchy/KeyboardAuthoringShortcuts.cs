// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using JetBrains.Annotations;
using Unity.Hierarchy.Editor;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

// A tool context keeps these bindings from shadowing GameObject and Scene View shortcuts.
static partial class KeyboardAuthoringShortcuts
{
    const string k_Prefix = "UI Toolkit Authoring/";
    internal const string AddAfterId = k_Prefix + "Add Element After";
    internal const string AddChildId = k_Prefix + "Add Child Element";
    internal const string ReplaceId = k_Prefix + "Replace Element Type";
    internal const string WrapId = k_Prefix + "Wrap in New Parent";
    internal const string UnwrapId = k_Prefix + "Unwrap Element";

    internal static readonly string AddAfterLabel = L10n.Tr("Add Element After...", null);
    internal static readonly string AddChildLabel = L10n.Tr("Add Child Element...", null);
    internal static readonly string ReplaceLabel = L10n.Tr("Replace Element Type...", null);
    internal static readonly string WrapLabel = L10n.Tr("Wrap in New Parent...", null);
    internal static readonly string UnwrapLabel = L10n.Tr("Unwrap Element", null);

    [AutoStaticsCleanupOnCodeReload]
    static ShortcutContext s_Context;

    [OnCodeLoaded, UsedImplicitly]
    static void Register()
    {
        s_Context = new ShortcutContext();
        EditorApplication.delayCall += () =>
            ShortcutIntegration.instance.contextManager.RegisterToolContext(s_Context);
    }

    static bool HasUISelection(AuthoringContext context)
        => context.Selection.ActiveObject is VisualElementSelection or VisualTreeAssetSelection;

    // With nothing selected, an add in the UI Stage lands at the end of the edited document's root.
    static bool CanAddAfter(AuthoringContext context) => HasUISelection(context) || context.IsEditingDocument;

    // A window with a context of its own, like the UI Builder, edits its own document; any other window edits the
    // scene's.
    static AuthoringContext ResolveContext()
        => TryGetFocusedWindowContext(out var context) ? context : AuthoringContext.Global;

    static bool TryGetFocusedWindowContext(out AuthoringContext context)
    {
        context = EditorWindow.focusedWindow is { } window ? AuthoringContext.For(window.rootVisualElement) : null;
        return context is { IsGlobal: false, IsDisposed: false };
    }

    class ShortcutContext : IShortcutContext
    {
        public bool active
        {
            get
            {
                if (TryGetFocusedWindowContext(out var context))
                    return CanAddAfter(context);

                return EditorWindow.focusedWindow is HierarchyWindow or UIViewportWindow &&
                       CanAddAfter(AuthoringContext.Global) &&
                       StageUtility.GetCurrentStage() is MainStage or VisualElementEditingStage;
            }
        }
    }

    /// <summary>
    /// Appends the hierarchy editing operations, each labeled with its current shortcut binding.
    /// </summary>
    internal static void AppendMenuActions(AuthoringContext context, DropdownMenu menu)
    {
        TryCaptureSelection(context, out var assets, out _);

        AppendMenuAction(menu, AddAfterLabel, AddAfterId, () => AddAfter(context), CanAddAfter(context));
        AppendMenuAction(menu, AddChildLabel, AddChildId, () => AddChild(context), HasUISelection(context));
        AppendMenuAction(menu, ReplaceLabel, ReplaceId, () => Replace(context), ReplaceElementTypeCommand.Validate(assets));
        AppendMenuAction(menu, WrapLabel, WrapId, () => Wrap(context), WrapElementsCommand.Validate(assets));
        AppendMenuAction(menu, UnwrapLabel, UnwrapId, () => Unwrap(context), UnwrapElementsCommand.Validate(assets));
    }

    static void AppendMenuAction(DropdownMenu menu, string label, string shortcutId, Action action, bool enabled)
        => StageContextMenuUtility.AppendAction(menu, label, StageContextMenuUtility.GetShortcutHotkey(shortcutId),
            action, enabled);

    [Shortcut(AddAfterId, typeof(ShortcutContext), KeyCode.N, ShortcutModifiers.Action | ShortcutModifiers.Shift)]
    static void AddAfter() => AddAfter(ResolveContext());

    static void AddAfter(AuthoringContext context)
        => UIElementsProvider.ShowLibraryPicker(context, "Add Element", includeDocuments: true,
            item => MenuUtility.InsertNextToSelection(context, item, SiblingPlacement.After));

    [Shortcut(AddChildId, typeof(ShortcutContext), KeyCode.N, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
    static void AddChild() => AddChild(ResolveContext());

    static void AddChild(AuthoringContext context)
    {
        // The shortcut context also stays active without a selection, which only Add Element After can use.
        if (!HasUISelection(context))
            return;

        UIElementsProvider.ShowLibraryPicker(context, "Add Child Element", includeDocuments: true,
            item => MenuUtility.AddAsLastChild(context, item));
    }

    [Shortcut(ReplaceId, typeof(ShortcutContext), KeyCode.R, ShortcutModifiers.Action)]
    static void Replace() => Replace(ResolveContext());

    static void Replace(AuthoringContext context)
    {
        if (!TryCaptureSelection(context, out var assets, out var parents) || !ReplaceElementTypeCommand.Validate(assets))
            return;

        UIElementsProvider.ShowLibraryPicker(context, "Replace With", includeDocuments: false, item =>
        {
            var status = ReplaceElementTypeCommand.Execute(CommandSources.Hierarchy, assets, item.libraryType.type,
                item.libraryType.variantName, context);
            if (status != CommandExecutionStatus.Success)
                return;

            UIToolkitStageUtility.ScopePendingSelectionRequestsTo(context, parents);
            ReloadDocumentsOf(parents);
        });
    }

    [Shortcut(WrapId, typeof(ShortcutContext), KeyCode.G, ShortcutModifiers.Action | ShortcutModifiers.Shift)]
    static void Wrap() => Wrap(ResolveContext());

    static void Wrap(AuthoringContext context)
    {
        if (!TryCaptureSelection(context, out var assets, out var parents))
            return;

        if (!WrapElementsCommand.Validate(assets))
        {
            Debug.LogWarning("Only contiguous siblings sharing the same parent can be wrapped.");
            return;
        }

        UIElementsProvider.ShowLibraryPicker(context, "Wrap In", includeDocuments: false,
            item => Wrap(context, assets, parents, item.libraryType.type, item.libraryType.variantName));
    }

    internal static void Wrap(AuthoringContext context, VisualElementAsset[] assets, VisualElement[] parents,
        Type wrapperType, string variantName)
    {
        var status = WrapElementsCommand.Execute(CommandSources.Hierarchy, assets, wrapperType, variantName, context);
        if (status != CommandExecutionStatus.Success)
            return;

        // The wrapped siblings share one parent, which the single new wrapper hangs from.
        UIToolkitStageUtility.ScopePendingSelectionRequestsTo(context, parents[0]);
        ReloadDocumentsOf(parents);
        UIToolkitStageUtility.RequestRenameOfPendingSelection(context);
    }

    [Shortcut(UnwrapId, typeof(ShortcutContext), KeyCode.G, ShortcutModifiers.Action)]
    internal static void Unwrap() => Unwrap(ResolveContext());

    internal static void Unwrap(AuthoringContext context)
    {
        if (!TryCaptureSelection(context, out var assets, out var parents))
            return;

        // One parent per promoted element child, in the order the command requests their selection.
        using var _ = ListPool<VisualElement>.Get(out var promotedParents);
        for (var i = 0; i < assets.Length; ++i)
        {
            for (var j = 0; j < assets[i].childCount; ++j)
            {
                if (assets[i][j] is VisualElementAsset)
                    promotedParents.Add(parents[i]);
            }
        }

        if (UnwrapElementsCommand.Execute(CommandSources.Hierarchy, assets, context) != CommandExecutionStatus.Success)
            return;

        UIToolkitStageUtility.ScopePendingSelectionRequestsTo(context, promotedParents);
        ReloadDocumentsOf(parents);
    }

    // Captured before the picker opens so the edit targets what was selected when the shortcut fired.
    internal static bool TryCaptureSelection(AuthoringContext context, out VisualElementAsset[] assets,
        out VisualElement[] parents)
    {
        using var assetsHandle = ListPool<VisualElementAsset>.Get(out var assetList);
        using var elementsHandle = ListPool<VisualElement>.Get(out var elementList);
        UIViewportContextMenuUtility.FilterSelection(context, assetList, elementList);

        assets = assetList.ToArray();
        parents = new VisualElement[elementList.Count];
        for (var i = 0; i < elementList.Count; ++i)
            parents[i] = elementList[i].parent;

        return assets.Length > 0;
    }

    // Scene panels only pick up in-memory document edits on a re-clone.
    static void ReloadDocumentsOf(VisualElement[] parents)
    {
        foreach (var parent in parents)
            UIAssetRegistrySceneTracking.ReloadDocumentOf(parent);
    }
}
