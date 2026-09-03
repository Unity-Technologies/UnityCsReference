// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Suspends the document live reload of the tracked scene panels while
/// <see cref="UIToolkitAuthoringSettings.EnableMainStageAuthoring"/> is on.
/// </summary>
/// <remarks>
/// <para>
/// The document tracker re-clones a panel's whole live tree whenever a <see cref="VisualTreeAsset"/> it shows
/// changes its dirty count, which while authoring in the Main Stage happens on every edit — once per keystroke
/// for an attribute. The re-clone throws away every live element, and with it the selection, the scene view
/// tools' element references and the hierarchy nodes, to show changes the authoring handlers have already
/// applied to the live elements themselves; the ones they cannot apply ask for a re-clone explicitly (see
/// <see cref="NodeHandlerStageStrategy.RequestRefresh"/>).
/// </para>
/// <para>
/// Only <see cref="LiveReloadTrackers.Document"/> is suspended, so style changes keep being applied in place
/// by the style sheet tracker. Rebuilding a document's live tree is the one thing that stops.
/// </para>
/// <para>
/// What the dual-writes do <em>not</em> cover is every <em>other</em> view of the document that changed: a
/// second scene component rendering the same <see cref="VisualTreeAsset"/>, or a scene component rendering a
/// document being edited in the UI Stage or the UI Builder. Those, and undo/redo, are re-cloned from here —
/// see <see cref="ReloadScenePanelsAfterChanges"/>.
/// </para>
/// </remarks>
static partial class UIAssetRegistrySceneTracking
{
    // One suspension per suspended panel; its presence is also what marks the panel as suspended.
    [NoAutoStaticsCleanup]
    static readonly Dictionary<Panel, LiveReloadSuspension> s_LiveReloadSuspensions = new();

    // The re-clone requested by the changes seen so far, pending until the next editor tick.
    [NoAutoStaticsCleanup]
    static readonly HashSet<UnityEngine.Object> s_PendingChangedAssets = new();
    [NoAutoStaticsCleanup]
    static CommandCategory s_PendingChanges = CommandCategory.None;
    [NoAutoStaticsCleanup]
    static bool s_PendingUnknownAssets;

    static void SubscribeToLiveReload()
    {
        Undo.undoRedoPerformed += OnUndoRedoPerformed;
        UICommandQueue.RegisterHandler<PostSaveCommand>(OnAssetSaved);
    }

    // The registry only exists once something is tracked, so its reimport signal is subscribed separately from
    // the load-time hooks (see Init).
    static void SubscribeToAssetReloads()
    {
        var registry = UIAssetRegistry.instance;

        // Init retries itself until the selection registry has bootstrapped, so this can run more than once.
        registry.AssetReloaded -= OnAssetReloaded;
        registry.AssetReloaded += OnAssetReloaded;
    }

    static void UnsubscribeFromLiveReload()
    {
        Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        UICommandQueue.UnregisterHandler<PostSaveCommand>(OnAssetSaved);
        EditorApplication.delayCall -= FlushPendingReload;

        var registry = UIAssetRegistry.LiveInstance;
        if (registry != null)
            registry.AssetReloaded -= OnAssetReloaded;

        s_PendingChangedAssets.Clear();
        s_PendingChanges = CommandCategory.None;
        s_PendingUnknownAssets = false;

        // The panels outlive this module's code, so hand their live reload back before letting go of them.
        foreach (var suspension in s_LiveReloadSuspensions.Values)
            suspension.Restore();
        s_LiveReloadSuspensions.Clear();
    }

    /// <summary>
    /// Brings <paramref name="panel"/> in line with the current settings: its documents stop live reloading
    /// while Main Stage authoring is enabled, and reload again as soon as it is not.
    /// </summary>
    /// <remarks>
    /// Only call this once the panel's assets are tracked: the registry's authoring trackers are what keep the
    /// live-reload system polling (and so consuming) the dirty counts of a suspended panel, which is why
    /// restoring the tracker later does not fire for the changes made in the meantime.
    /// </remarks>
    static void ApplyLiveReloadPolicy(Panel panel)
    {
        if (panel == null)
            return;

        if (!UIToolkitStageUtility.IsAuthoringEnabledInMainStage)
        {
            RestoreLiveReload(panel);
            return;
        }

        if (s_LiveReloadSuspensions.ContainsKey(panel))
            return;

        var suspension = new LiveReloadSuspension();
        suspension.Suspend(panel);
        s_LiveReloadSuspensions[panel] = suspension;
    }

    static void RestoreLiveReload(Panel panel)
    {
        if (panel == null || !s_LiveReloadSuspensions.TryGetValue(panel, out var suspension))
            return;

        s_LiveReloadSuspensions.Remove(panel);
        suspension.Restore();
    }

    // The changes that only reach a document's live elements through a re-clone. Styling and Variables are left
    // out on purpose: they are carried by the style sheet tracker, which stays enabled, and they ride on
    // high-frequency commands (a style value drag) that must not re-clone anything.
    const CommandCategory k_NeedsReclone =
        CommandCategory.Hierarchy | CommandCategory.StylingContext | CommandCategory.Attributes;

    /// <summary>
    /// Re-clones the scene panel documents a just-finished command group changed, for the views of those
    /// documents nothing wrote to directly.
    /// </summary>
    /// <param name="changes">The categories of the commands that ran, deciding whether a re-clone is needed.</param>
    /// <param name="changedAssets">
    /// The assets the group modified, so unrelated documents are left alone. An empty set means the tool did not
    /// name them (the UI Builder records nothing for undo), and every scene document is re-cloned rather than
    /// risk missing the one that changed.
    /// </param>
    internal static void ReloadScenePanelsAfterChanges(CommandCategory changes,
        IReadOnlyCollection<UnityEngine.Object> changedAssets)
    {
        // Nothing is suspended, so the panels' own trackers still do this.
        if (s_LiveReloadSuspensions.Count == 0)
            return;

        if ((changes & k_NeedsReclone) == CommandCategory.None)
            return;

        s_PendingChanges |= changes;

        if (changedAssets is { Count: > 0 })
        {
            foreach (var asset in changedAssets)
                s_PendingChangedAssets.Add(asset);
        }
        else
        {
            s_PendingUnknownAssets = true;
        }

        ScheduleReload();
    }

    // Undo and redo restore the authoring assets without touching the live elements, and the suspended tracker
    // no longer notices the dirty count moving back. Neither reports which asset moved, so every view of every
    // scene document is re-cloned — hence Hierarchy, the category that skips nothing.
    static void OnUndoRedoPerformed()
    {
        if (s_LiveReloadSuspensions.Count == 0)
            return;

        s_PendingChanges |= CommandCategory.Hierarchy;
        s_PendingUnknownAssets = true;
        ScheduleReload();
    }

    /// <summary>
    /// A document was just saved. Saving writes the <c>.uxml</c> and reimports it, and that reimport rebuilds
    /// the document's whole asset tree, so every live element cloned from it now points at a
    /// <see cref="VisualElementAsset"/> that has left the document. Re-clone so they are backed by the assets
    /// the document actually holds.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The reimport cannot be picked up as an external change: it is the registry's own write, and the save
    /// boundary deliberately suppresses it so it is not reported as a conflict. The panels' own document
    /// tracker would have caught it through the dirty count, which is the path the suspension turns off — so,
    /// like every other change that only reaches the scene panels through a re-clone, it has to be pushed from
    /// here.
    /// </para>
    /// <para>
    /// Left unpushed, the next edit on one of those elements writes to the detached tree: the document it is
    /// supposed to change never sees it, while the tree it does change is rendered by nobody. A drop across two
    /// documents then removes the element from a tree nothing renders and adds it to another, leaving it
    /// visible in both.
    /// </para>
    /// </remarks>
    static void OnAssetSaved(in CommandContext context)
    {
        if (s_LiveReloadSuspensions.Count == 0 || context.Status != CommandExecutionStatus.Success)
            return;

        var command = (PostSaveCommand)context.Command;

        // Only a document rebuilds an asset tree on save. A style sheet's reimport is carried to the panels by
        // the style sheet tracker, which is never suspended.
        if (!command.Succeeded || command.Asset is not VisualTreeAsset)
            return;

        s_PendingChanges |= CommandCategory.Hierarchy;
        s_PendingChangedAssets.Add(command.Asset);
        ScheduleReload();
    }

    /// <summary>
    /// An asset was replaced by its on-disk version — reimported behind the editor's back, or discarded — so
    /// every document rendering it has to be cloned again.
    /// </summary>
    /// <remarks>
    /// The panels' own live reload cannot be relied on to carry this while it is suspended, so — like every
    /// other change that only reaches the scene panels through a re-clone — it is pushed from here.
    /// </remarks>
    static void OnAssetReloaded(UnityEngine.Object asset)
    {
        if (s_LiveReloadSuspensions.Count == 0 || asset == null)
            return;

        s_PendingChanges |= CommandCategory.Hierarchy;
        s_PendingChangedAssets.Add(asset);
        ScheduleReload();
    }

    // The re-clone is deferred, never run from the change that caused it. It releases every element of the
    // documents it rebuilds, and the code that made the change is still using them: a drop in the Main Stage
    // hierarchy holds the elements it just reparented, and the hierarchy framework goes on working on the
    // dropped nodes after the command returns. This is also the timing the panels' own live reload had, since
    // it re-cloned on the panel's next update rather than on the change.
    static void ScheduleReload()
    {
        // Requests coalesce through the pending state, not through a "already scheduled" latch: a delayCall can
        // be dropped without ever running in some editor states, and a latch would then stay set and swallow
        // every later request for the rest of the session. Queuing the flush again instead is harmless — it
        // finds nothing pending and returns.
        EditorApplication.delayCall += FlushPendingReload;
    }

    static void FlushPendingReload()
    {
        var changes = s_PendingChanges;
        s_PendingChanges = CommandCategory.None;

        // Detached before reloading anything: re-cloning runs the panels' change processors, which can execute
        // commands and so schedule another reload while this one is still going.
        using var _assets = ListPool<UnityEngine.Object>.Get(out var changedAssets);
        if (!s_PendingUnknownAssets)
            changedAssets.AddRange(s_PendingChangedAssets);
        s_PendingChangedAssets.Clear();
        s_PendingUnknownAssets = false;

        if (s_LiveReloadSuspensions.Count == 0 || (changes & k_NeedsReclone) == CommandCategory.None)
            return;

        // An attribute edit runs on every keystroke and is dual-written onto the element being inspected, so
        // re-cloning the document holding it is exactly what the suspension exists to prevent. A hierarchy or
        // styling-context change is a discrete action instead, and only partially dual-written, so it re-clones
        // every view, the edited one included.
        using var _skipped = ListPool<IPanelComponent>.Get(out var skipped);
        if ((changes & (CommandCategory.Hierarchy | CommandCategory.StylingContext)) == CommandCategory.None)
            CollectSelectedPanelComponents(skipped);

        // Reloading rebuilds the trees these panels are keyed on, so snapshot before touching any of them.
        using var _panels = ListPool<Panel>.Get(out var panels);
        panels.AddRange(s_LiveReloadSuspensions.Keys);
        foreach (var panel in panels)
            ReloadPanelDocuments(panel, changedAssets, skipped);
    }

    static void ReloadPanelDocuments(Panel panel, List<UnityEngine.Object> changedAssets,
        List<IPanelComponent> skipped)
    {
        if (panel?.visualTree == null)
            return;

        // Reloading a component rebuilds its part of the tree, so all of them are collected before the first
        // one runs.
        using var _ = ListPool<IPanelComponent>.Get(out var components);
        panel.visualTree.Query<VisualElement>().ForEach(element =>
        {
            if (element is IPanelComponentRootElement { panelComponent: { } component })
                components.Add(component);
        });

        var reloaded = false;
        foreach (var component in components)
        {
            if (skipped != null && skipped.Contains(component))
                continue;

            // An empty change set means the tool did not name what it touched, so nothing can be ruled out.
            if (changedAssets is { Count: > 0 } && !PanelDependencyTracker.DependsOnAny(component, changedAssets))
                continue;

            component.HandleLiveReload();
            reloaded = true;
        }

        if (reloaded)
            ReconcileAfterReload(panel);
    }

    /// <summary>
    /// Re-clones the scene document <paramref name="element"/> belongs to, then lets its panel reconcile the new
    /// instances. For the callers that already know which document a change landed in, and that are not holding
    /// on to the elements it replaces.
    /// </summary>
    internal static void ReloadDocumentOf(VisualElement element)
    {
        // Resolved first: the re-clone releases element, and with it the way back to its panel.
        var panel = element?.panel as BaseVisualElementPanel;
        var component = element?.GetFirstOfType<IPanelComponentRootElement>()?.panelComponent;
        if (component == null)
            return;

        component.HandleLiveReload();
        ReconcileAfterReload(panel);
    }

    /// <summary>
    /// Runs the panel's authoring update phase, where the change processors holding on to live elements — the
    /// selection registry, the hierarchy node handlers, the inspector's editing controllers — reconcile the
    /// instances a re-clone replaced. Without it they keep pointing at released elements until the panel next
    /// repaints; the UI Stage gets the same thing from the frame update around its own re-clone.
    /// </summary>
    static void ReconcileAfterReload(BaseVisualElementPanel panel) => panel?.UpdateAuthoring();

    // The scene components holding the current selection: what the inspector edits, and therefore the only
    // documents the authoring handlers dual-write to.
    static void CollectSelectedPanelComponents(List<IPanelComponent> components)
    {
        foreach (var selected in Selection.objects)
        {
            if (selected is not VisualElementSelection { Element: { } element })
                continue;

            var component = element.GetFirstOfType<IPanelComponentRootElement>()?.panelComponent;
            if (component != null && !components.Contains(component))
                components.Add(component);
        }
    }
}
