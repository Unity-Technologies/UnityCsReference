// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Registry of current <see cref="VisualTreeAsset"/> / <see cref="StyleSheet"/> assets opened or changed across
/// UI Toolkit authoring tools, tracking how dirty each is and how to save or discard them.
/// </summary>
/// <remarks>
/// Assets are reference-counted per owner and per access mode (<see cref="UIAssetAccess"/>). A save prompt is
/// only warranted when the last read-write holder of a dirty asset releases its reference. Dirtiness is
/// decided by <see cref="UIAssetDirtyTracker"/> (re-export and diff). The clean baseline hashes and each
/// dirty asset's unsaved edits persist across a domain reload; the open set rebuilds as the tools re-report
/// their assets.
/// </remarks>
[FilePath("Library/UIAssetRegistry.asset", FilePathAttribute.Location.ProjectFolder)]
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
internal sealed class UIAssetRegistry : ScriptableSingleton<UIAssetRegistry>, ISerializationCallbackReceiver
{
    [SerializeField] List<AssetBaseline> m_SerializedBaselines = new();

    // The unsaved (edited) text of each dirty tracked asset, persisted alongside the baselines (keyed by the
    // durable GlobalObjectId) so a "Keep my changes" after a domain reload can still restore the user's work.
    // The live m_UnsavedSnapshots is rebuilt from this on restore.
    [SerializeField] List<AssetSnapshot> m_SerializedSnapshots = new();

    [NonSerialized] readonly Dictionary<EntityId, AssetEntry> m_Entries = new();
    [NonSerialized] readonly UIAssetDirtyTracker m_Dirty = new();
    [NonSerialized] bool m_BaselinesRestored;

    [NonSerialized] readonly Dictionary<StyleSheet, VisualTreeAsset> m_InlineSheetOwners = new();

    // The inline sheet instance each owner was registered WITH, so an owner whose inline sheet was swapped
    // (a content restore recreates it) unregisters the entry it actually created instead of leaking the old
    // key and never re-registering the new one.
    [NonSerialized] readonly Dictionary<VisualTreeAsset, StyleSheet> m_RegisteredInlineSheets = new();

    // Asset paths we are writing ourselves, so our own reimport does not look like an external change.
    [NonSerialized] readonly HashSet<string> m_SuppressedReimportPaths = new();

    // Reimports that a move (rename) triggered, which reconcile as a silent "keep" rather than as a conflict.
    [NonSerialized] readonly HashSet<EntityId> m_MovedReimports = new();

    // The exact paths suppressed for each in-flight save/discard, so the release uses the same set the suppress
    // added. Recomputing them from the asset at release time can yield a different (or empty) set when the
    // operation's own reimport replaced the managed instance, which would strand the suppression and make us
    // ignore every later external change to those files.
    [NonSerialized] readonly Dictionary<EntityId, List<string>> m_SuppressedPathsByAsset = new();

    // The current exported (unsaved) text of each dirty tracked asset, kept current as it changes, so an
    // external reimport that clobbers the in-memory asset can still offer to keep the user's edits.
    [NonSerialized] readonly Dictionary<EntityId, string> m_UnsavedSnapshots = new();

    [NonSerialized] readonly Dictionary<Panel, PanelDependencyTracker> m_PanelTrackers = new();

    // External reimports observed in the current import batch (asset id -> was-dirty-before-import), drained on
    // a deferred flush, because reconciling/prompting is not safe inside the import callback.
    [NonSerialized] readonly Dictionary<EntityId, bool> m_PendingReimports = new();

    // Reimports a tool has claimed (see ClaimExternalChange): the tool drives its own reload and pulls the
    // resolution via ResolveExternalChange, so the registry's own deferred handling defers to it for these.
    [NonSerialized] readonly HashSet<EntityId> m_ClaimedReimports = new();

    // One decision per external change: several holders of the same asset can each pull the resolution while
    // reloading (e.g. two Builder windows sharing a document). The first ask resolves and the decision sticks
    // until the asset's next reimport voids it.
    [NonSerialized] readonly Dictionary<EntityId, UIAssetConflictChoice> m_ResolvedConflicts = new();

    [NoAutoStaticsCleanup]
    static UIAssetRegistry s_Live;

    // Owner token for assets adopted purely because a command edited them while no authoring tool had them
    // explicitly open (i.e. code-driven clones).
    [NoAutoStaticsCleanup]
    static readonly object s_AutoTrackOwner = new();

    /// <summary>
    /// The registry instance if one is already alive without creating it.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="ScriptableSingleton{T}.instance"/>, this never triggers a file load, so it is safe to call
    /// from a ScriptableObject constructor/field initializer (e.g. a teardown path that only needs to release
    /// references when the registry actually exists).
    /// </remarks>
    public static UIAssetRegistry LiveInstance => s_Live;

    /// <summary>Raised when an asset first becomes tracked (its first reference is opened).</summary>
    public event Action<UnityEngine.Object> AssetTracked;

    /// <summary>Raised when an asset stops being tracked (its last reference is closed, or it was deleted).</summary>
    public event Action<UnityEngine.Object> AssetUntracked;

    /// <summary>Raised when a tracked asset transitions between clean and dirty.</summary>
    public event Action<UnityEngine.Object> AssetDirtyStateChanged;

    /// <summary>Raised when the last read-write holder of a dirty asset releases its reference.</summary>
    public event Action<UIAssetSaveRequest> SaveRequested;

    /// <summary>
    /// Raised when a tracked asset was reimported externally while it had unsaved edits, after the registry
    /// resolved the conflict. Carries the resolution (<see cref="UIAssetConflict.Choice"/>) so a tool can
    /// react — notably preserve its work-in-progress on <see cref="UIAssetConflictChoice.SaveBackupAndUseImported"/>.
    /// Fires just before <see cref="AssetReloaded"/>.
    /// </summary>
    public event Action<UIAssetConflict> AssetConflictDetected;

    /// <summary>
    /// Raised after a tracked asset's in-memory content was replaced — it adopted a fresh external reimport,
    /// or had the user's unsaved edits restored over one — so consumers (the Stage, the Builder) can re-clone
    /// or re-bind their live views to the current content.
    /// </summary>
    public event Action<UnityEngine.Object> AssetReloaded;

    /// <summary>
    /// Overrides how an external-change conflict on a dirty asset is resolved. Default is a modal
    /// keep-vs-use-imported dialog; tests set this to run headless and a tool may set custom UI.
    /// </summary>
    internal Func<UnityEngine.Object, UIAssetConflictChoice> ConflictResolver;

    /// <summary>
    /// Suppresses the built-in external-change dialog, so a tool or test run that has opted out of modals is
    /// never blocked by one. Unlike <see cref="ConflictResolver"/> this does not change the decision: the
    /// dialog's own safe default (keep the user's work) is taken.
    /// </summary>
    /// <remarks>
    /// Batch mode is deliberately NOT folded in here: <see cref="EditorUtility.DisplayDialogComplex"/> already
    /// answers itself there, and tests that script a specific response through an interaction context rely on
    /// the call still being made.
    /// </remarks>
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    [NoAutoStaticsCleanup]
    internal static bool PreventDialogsFromOpening { get; set; }

    void OnEnable()
    {
        s_Live = this;

        UICommandQueue.GroupEnded += OnGroupEnded;
        UICommandQueue.RegisterHandlerForCategory(CommandCategory.Save, OnToolSaveBoundary);
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        // GlobalObjectId cannot be resolved inside a deserialize callback, so restore baselines on the next
        // editor tick. Any earlier access resolves them on demand via EnsureBaselinesRestored.
        EditorApplication.delayCall += EnsureBaselinesRestored;
    }

    void OnDisable()
    {
        UICommandQueue.GroupEnded -= OnGroupEnded;
        UICommandQueue.UnregisterHandlerForCategory(CommandCategory.Save, OnToolSaveBoundary);
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        if (s_Live == this)
            s_Live = null;
    }

    void ISerializationCallbackReceiver.OnBeforeSerialize()
    {
        if (m_Dirty != null && m_BaselinesRestored)
        {
            m_Dirty.Serialize(m_SerializedBaselines);
            SerializeSnapshots();
        }
    }

    void ISerializationCallbackReceiver.OnAfterDeserialize()
    {
        // Defer the actual restore; GlobalObjectId resolution is not allowed here.
        m_BaselinesRestored = false;
    }

    void EnsureBaselinesRestored()
    {
        if (m_BaselinesRestored)
            return;
        m_BaselinesRestored = true;

        if (m_SerializedBaselines != null)
            foreach (var baseline in m_SerializedBaselines)
                m_Dirty.Restore(baseline);

        RestoreSnapshots();
    }

    // Captures the live unsaved snapshots into the serialized list, keyed by the durable GlobalObjectId, so
    // "Keep my changes" survives a domain reload.
    void SerializeSnapshots()
    {
        m_SerializedSnapshots.Clear();
        foreach (var kvp in m_UnsavedSnapshots)
        {
            if (string.IsNullOrEmpty(kvp.Value))
                continue;

            // Prefer the live entry's id, but fall back to the baseline's: Untrack deliberately KEEPS the
            // baseline and the snapshot of an asset that is still dirty when its last holder closes, and
            // m_Entries is not serialized. Without the fallback, any domain reload while such an asset is
            // closed-and-dirty silently drops the text and "Keep My Changes" degrades into "Use Imported".
            GlobalObjectId globalId;
            if (m_Entries.TryGetValue(kvp.Key, out var entry))
                globalId = entry.GlobalId;
            else if (!m_Dirty.TryGetGlobalId(kvp.Key, out globalId))
                continue;

            m_SerializedSnapshots.Add(new AssetSnapshot { AssetId = globalId, Text = kvp.Value });
        }
    }

    // Rebuilds the live unsaved snapshots (keyed by the session-local entity id) from the serialized list after
    // a domain reload. Must not run inside a deserialize callback — GlobalObjectId cannot resolve there.
    void RestoreSnapshots()
    {
        if (m_SerializedSnapshots == null)
            return;

        foreach (var snapshot in m_SerializedSnapshots)
        {
            var asset = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(snapshot.AssetId);
            if (asset != null && !string.IsNullOrEmpty(snapshot.Text))
                m_UnsavedSnapshots[asset.GetEntityId()] = snapshot.Text;
        }
    }

    /// <summary>
    /// Reports that <paramref name="owner"/> now has <paramref name="asset"/> open with the given access. The
    /// same owner opening the same asset again updates its access mode in place.
    /// </summary>
    public void Open(UnityEngine.Object asset, object owner, UIAssetAccess access)
    {
        if (!IsAuthoringAsset(asset) || owner == null)
            return;

        EnsureBaselinesRestored();

        var id = asset.GetEntityId();
        if (!m_Entries.TryGetValue(id, out var entry))
        {
            entry = new AssetEntry
            {
                Id = id,
                GlobalId = GlobalObjectId.GetGlobalObjectIdSlow(asset),
                Kind = asset is VisualTreeAsset ? UIAssetKind.VisualTreeAsset : UIAssetKind.StyleSheet,
                Asset = asset,
                AssetPath = AssetDatabase.GetAssetPath(asset),
            };
            m_Entries[id] = entry;

            // The first time we see an asset, establish a clean baseline unless one was already restored from
            // a previous session (a re-open of an asset that is open elsewhere reuses the shared baseline).
            if (!m_Dirty.HasBaseline(id))
                m_Dirty.Capture(asset);

            RegisterInlineSheet(asset);
            entry.WasDirty = m_Dirty.IsModified(id, asset);
            AssetTracked?.Invoke(asset);
        }

        if (entry.ReferencesByOwner.TryGetValue(owner, out var existing))
        {
            if (existing != access)
                ApplyAccessChange(entry, existing, access);
            entry.ReferencesByOwner[owner] = access;
        }
        else
        {
            entry.ReferencesByOwner[owner] = access;
            if (access == UIAssetAccess.ReadWrite)
                entry.ReadWriteCount++;

            // A real tool has now adopted an asset we were only holding via the synthetic auto-track reference
            // (a code-driven edit with no tool open). Hand ownership over and drop the synthetic ref, so it no
            // longer inflates ReadWriteCount — otherwise closing this real holder would not register as the last
            // read-write release and would skip the save prompt for the still-unsaved edit.
            if (owner != s_AutoTrackOwner && access == UIAssetAccess.ReadWrite &&
                entry.ReferencesByOwner.ContainsKey(s_AutoTrackOwner))
                Close(entry.Id, entry.Asset, s_AutoTrackOwner);
        }
    }

    /// <summary>Releases <paramref name="owner"/>'s reference to <paramref name="asset"/>.</summary>
    public void Close(UnityEngine.Object asset, object owner)
    {
        // Deliberately ReferenceEquals and not `asset == null`: fake-null makes a DESTROYED asset
        // compare equal to null, and a reference taken on an instance that a reimport later replaced must
        // still be releasable — otherwise the entry keeps that owner forever, is never untracked, and its
        // phantom AssetPath can capture the next external reimport. GetEntityId() is valid on a destroyed
        // wrapper, which is all the id-keyed overload below needs.
        if (ReferenceEquals(asset, null) || owner == null)
            return;

        Close(asset.GetEntityId(), asset, owner);
    }

    void Close(EntityId id, UnityEngine.Object asset, object owner)
    {
        if (!m_Entries.TryGetValue(id, out var entry) || !entry.ReferencesByOwner.Remove(owner, out var access))
            return;

        var wasLastReadWrite = false;
        if (access == UIAssetAccess.ReadWrite)
        {
            entry.ReadWriteCount--;
            wasLastReadWrite = entry.ReadWriteCount == 0;
        }

        if (wasLastReadWrite && m_Dirty.IsModified(id, asset))
        {
            var captured = asset;
            SaveRequested?.Invoke(new UIAssetSaveRequest(captured, () => SaveAsset(captured), () => DiscardAsset(captured)));
        }

        // A real owner leaving may leave only the synthetic auto-track reference behind; drop it too if the
        // asset is now clean, so an adopted asset does not outlive the tools that kept it around. Guarded
        // against re-entry when the synthetic owner is itself the one being closed.
        if (owner != s_AutoTrackOwner)
            ReleaseAutoTrackIfClean(id, asset);

        if (entry.ReferencesByOwner.Count == 0)
            Untrack(entry);
    }

    /// <summary>Releases every reference held by <paramref name="owner"/> (e.g. a closed window or stage).</summary>
    public void CloseAll(object owner)
    {
        if (owner == null)
            return;

        using var _ = ListPool<AssetEntry>.Get(out var affected);
        foreach (var entry in m_Entries.Values)
            if (entry.ReferencesByOwner.ContainsKey(owner))
                affected.Add(entry);

        foreach (var entry in affected)
            Close(entry.Id, entry.Asset, owner);
    }

    static void ApplyAccessChange(AssetEntry entry, UIAssetAccess from, UIAssetAccess to)
    {
        if (from == UIAssetAccess.ReadWrite && to == UIAssetAccess.ReadOnly)
            entry.ReadWriteCount--;
        else if (from == UIAssetAccess.ReadOnly && to == UIAssetAccess.ReadWrite)
            entry.ReadWriteCount++;
    }

    void Untrack(AssetEntry entry)
    {
        // Idempotent: a re-entrant Close (ReleaseAutoTrackIfClean releasing the synthetic owner) can already
        // have untracked this entry. Bail if so, otherwise AssetUntracked would fire twice for one asset.
        if (!m_Entries.Remove(entry.Id))
            return;

        UnregisterInlineSheet(entry.Asset);

        // Keep the baseline (and snapshot) while the asset is still dirty so a later re-open resumes the
        // correct dirty state; drop them once clean to avoid unbounded growth.
        if (!m_Dirty.IsModified(entry.Id, entry.Asset))
        {
            m_Dirty.Remove(entry.Id);
            m_UnsavedSnapshots.Remove(entry.Id);
        }

        AssetUntracked?.Invoke(entry.Asset);
    }

    /// <summary>
    /// Starts discovering and live-tracking the full asset dependency graph of <paramref name="panel"/>. All
    /// discovered assets are opened under <paramref name="owner"/> with the access
    /// <paramref name="resolveAccess"/> reports for each (read-only when it is not supplied), re-resolved on
    /// every walk. <paramref name="collectRoots"/> supplies the panel's root VisualTreeAssets (e.g. its scene
    /// panel components, or a stage's edited document).
    /// </summary>
    internal void AttachPanel(Panel panel, object owner, Action<List<VisualTreeAsset>> collectRoots,
        Func<UnityEngine.Object, UIAssetAccess> resolveAccess = null)
    {
        if (panel == null)
            return;

        if (m_PanelTrackers.ContainsKey(panel))
            return;

        var tracker = new PanelDependencyTracker(this, panel, owner, collectRoots, resolveAccess);
        m_PanelTrackers[panel] = tracker;
        tracker.Rewalk();
    }

    /// <summary>Stops tracking a panel and releases every read-only reference its dependency tracker held.</summary>
    internal void DetachPanel(Panel panel)
    {
        if (panel == null || m_PanelTrackers == null)
            return;

        if (m_PanelTrackers.TryGetValue(panel, out var tracker))
        {
            m_PanelTrackers.Remove(panel);
            tracker.Dispose();
        }
    }

    /// <summary>
    /// Re-walks a panel's dependency graph now (e.g. after a stage reloaded its document into new instances),
    /// picking up added/removed assets and re-resolving each one's access.
    /// </summary>
    internal void RefreshPanel(Panel panel)
    {
        if (panel != null && m_PanelTrackers != null && m_PanelTrackers.TryGetValue(panel, out var tracker))
            tracker.Rewalk();
    }

    /// <summary>Re-evaluates the dirty state of every tracked asset, raising the change event on transitions.</summary>
    public void RefreshDirtyState()
    {
        EnsureBaselinesRestored();
        using var _ = ListPool<AssetEntry>.Get(out var entries);
        entries.AddRange(m_Entries.Values);
        foreach (var entry in entries)
            NotifyDirtyStateMaybeChanged(entry.Id, entry.Asset);
    }

    public bool IsTracked(UnityEngine.Object asset)
        => asset != null && m_Entries != null && m_Entries.ContainsKey(asset.GetEntityId());

    /// <summary>
    /// Whether a tool other than <paramref name="owner"/> currently holds <paramref name="asset"/> open for
    /// writing — so the shared in-memory instance may carry unsaved edits that are not the caller's.
    /// </summary>
    /// <remarks>
    /// The question a tool has to answer before reverting a shared asset to its OWN backup. Neither its own
    /// unsaved-changes flag nor <see cref="IsDirty"/> can distinguish whose edits are in the instance; the set
    /// of read-write holders can at least say whether anyone else's could be.
    /// </remarks>
    public bool IsOpenForWritingByOther(UnityEngine.Object asset, object owner)
    {
        if (ReferenceEquals(asset, null) || m_Entries == null)
            return false;

        if (!m_Entries.TryGetValue(asset.GetEntityId(), out var entry))
            return false;

        foreach (var reference in entry.ReferencesByOwner)
            if (reference.Value == UIAssetAccess.ReadWrite && !ReferenceEquals(reference.Key, owner))
                return true;

        return false;
    }

    internal void CollectDirtyAssetsHeldOnlyBy(Func<object, bool> isReleasingOwner, List<UnityEngine.Object> results)
    {
        if (m_Entries == null || results == null || isReleasingOwner == null)
            return;

        EnsureBaselinesRestored();

        var documentCount = 0;
        foreach (var entry in m_Entries.Values)
        {
            if (entry.ReadWriteCount == 0 || !m_Dirty.IsModified(entry.Id, entry.Asset))
                continue;

            var heldOnlyByReleasingOwners = true;
            foreach (var reference in entry.ReferencesByOwner)
            {
                if (reference.Value != UIAssetAccess.ReadWrite || isReleasingOwner(reference.Key))
                    continue;
                heldOnlyByReleasingOwners = false;
                break;
            }

            if (!heldOnlyByReleasingOwners)
                continue;

            if (entry.Kind == UIAssetKind.VisualTreeAsset)
                results.Insert(documentCount++, entry.Asset);
            else
                results.Add(entry.Asset);
        }
    }

    /// <summary>Whether the tracked asset has unsaved changes (its exported content differs from disk).</summary>
    public bool IsDirty(UnityEngine.Object asset)
    {
        if (!IsTracked(asset))
            return false;

        EnsureBaselinesRestored();
        return m_Dirty.IsModified(asset);
    }

    /// <summary>
    /// Records the asset's current content as the clean baseline. Tools call this after loading/reloading an
    /// asset from disk so it is not reported dirty by a subsequent no-op re-serialization.
    /// </summary>
    public void MarkClean(UnityEngine.Object asset)
    {
        if (!IsAuthoringAsset(asset))
            return;

        EnsureBaselinesRestored();
        var id = asset.GetEntityId();

        // Only maintain a baseline for an asset some holder is actually tracking. Capturing one for an asset no
        // entry references (e.g. a saved document's referenced stylesheet that no tool opened) would leak — no
        // Untrack ever removes it — and go stale, because a later Open reuses it instead of re-capturing.
        if (m_Entries.ContainsKey(id))
            m_Dirty.Capture(asset);
        else
            m_Dirty.Remove(id);

        m_UnsavedSnapshots.Remove(id);
        NotifyDirtyStateMaybeChanged(id, asset);
        ReleaseAutoTrackIfClean(id, asset);
    }

    /// <summary>Saves a single tracked asset (and, for a VisualTreeAsset, its dirty referenced stylesheets).</summary>
    public bool SaveAsset(UnityEngine.Object asset, object source = null)
    {
        source ??= CommandSources.Registry;
        return asset switch
        {
            VisualTreeAsset vta => SaveDocument(vta, source),
            StyleSheet styleSheet => SaveStyleSheet(styleSheet, source),
            _ => false
        };
    }

    /// <summary>Saves every tracked asset that has unsaved changes.</summary>
    public bool SaveAll(object source = null)
    {
        source ??= CommandSources.Registry;
        if (m_Entries.Count == 0)
            return true;

        EnsureBaselinesRestored();

        using var _ = ListPool<VisualTreeAsset>.Get(out var vtas);
        using var __ = ListPool<StyleSheet>.Get(out var sheets);
        foreach (var entry in m_Entries.Values)
        {
            if (!m_Dirty.IsModified(entry.Id, entry.Asset))
                continue;
            if (entry.Asset is VisualTreeAsset vta)
                vtas.Add(vta);
            else if (entry.Asset is StyleSheet styleSheet)
                sheets.Add(styleSheet);
        }

        var succeeded = true;
        using var group = UICommandQueue.BeginGroup("Save All UI Assets");
        using var saved = HashSetPool<UnityEngine.Object>.Get(out var savedSet);

        using (new AssetDatabase.AssetEditingScope())
        {
            foreach (var vta in vtas)
            {
                succeeded &= SaveDocument(vta, source, savedSet);
            }

            // Standalone stylesheets not already written as part of a document save above.
            foreach (var styleSheet in sheets)
            {
                if (savedSet.Contains(styleSheet))
                    continue;
                succeeded &= SaveStyleSheet(styleSheet, source);
            }
        }

        return succeeded;
    }

    bool SaveDocument(VisualTreeAsset vta, object source) => SaveDocument(vta, source, null);

    bool SaveDocument(VisualTreeAsset vta, object source, HashSet<UnityEngine.Object> saved)
    {
        if (vta == null)
            return false;

        var vtaPath = AssetDatabase.GetAssetPath(vta);
        if (string.IsNullOrEmpty(vtaPath))
            // [TODO] Save As for a never-saved document is not supported yet.
            return false;

        EnsureBaselinesRestored();

        using var group = UICommandQueue.BeginGroup("Save Document");
        var context = new VisualTreeAssetEditingContext(vta);
        PreSaveCommand.Execute(source, context);

        var succeeded = true;
        try
        {
            using var _ = ListPool<StyleSheet>.Get(out var sheets);
            CollectDocumentStyleSheets(vta, sheets);

            using (new AssetDatabase.AssetEditingScope())
            {
                foreach (var styleSheet in sheets)
                {
                    if (styleSheet == null)
                        continue;
                    if (m_Dirty.IsModified(styleSheet))
                        succeeded &= WriteAsset(styleSheet, AssetDatabase.GetAssetPath(styleSheet));
                    else
                        EditorUtility.ClearDirty(styleSheet);
                }

                if (m_Dirty.IsModified(vta))
                {
                    // HarmonizeIds renumbers the document's element ids. Re-file the selection registry's id-path
                    // caches for every live panel before anything re-clones, otherwise the live selection (and its
                    // EntityId) is lost against the renumbered elements.
                    VisualTreeAsset.HarmonizeIds(vta);
                    VisualElementSelectionRegistry.Instance?.ResyncStablePathsForAllPanels();
                    succeeded &= WriteAsset(vta, vtaPath);
                }
                else
                {
                    EditorUtility.ClearDirty(vta);
                }
            }

            if (succeeded)
            {
                ClearUndoForDocument(vta, sheets);
                MarkClean(vta);
                foreach (var styleSheet in sheets)
                    if (styleSheet != null)
                        MarkClean(styleSheet);
            }

            if (saved != null)
            {
                saved.Add(vta);
                foreach (var styleSheet in sheets)
                    if (styleSheet != null)
                        saved.Add(styleSheet);
            }
        }
        finally
        {
            // Always close the save boundary so the reimport suppression the pre-save command set is released
            // even if the export/import threw; otherwise external-change detection stays dead for these assets.
            // Report whether the write actually landed: every observer (this registry's own re-baseline, the
            // Builder's "*", the Stage's reload) must skip anything that assumes the asset now matches disk,
            // or a save that failed — a read-only/unchecked-out file — would silently be declared clean.
            PostSaveCommand.Execute(source, context, succeeded);
        }

        return succeeded;
    }

    bool SaveStyleSheet(StyleSheet styleSheet, object source)
    {
        if (styleSheet == null)
            return false;

        var path = AssetDatabase.GetAssetPath(styleSheet);
        if (string.IsNullOrEmpty(path))
            // [TODO] Save As for a never-saved stylesheet is not supported yet.
            return false;

        EnsureBaselinesRestored();

        using var group = UICommandQueue.BeginGroup("Save StyleSheet");
        PreSaveCommand.Execute(source, styleSheet);

        var succeeded = true;
        try
        {
            using (new AssetDatabase.AssetEditingScope())
            {
                if (m_Dirty.IsModified(styleSheet))
                    succeeded &= WriteAsset(styleSheet, path);
                else
                    EditorUtility.ClearDirty(styleSheet);
            }

            if (succeeded)
            {
                Undo.ClearUndo(styleSheet);
                MarkClean(styleSheet);
            }
        }
        finally
        {
            // Always close the save boundary so the reimport suppression is released even on an export failure,
            // but report the outcome so observers do not re-baseline an asset that was never written.
            PostSaveCommand.Execute(source, styleSheet, succeeded);
        }

        return succeeded;
    }

    /// <summary>Reverts a single tracked asset to its on-disk state, dropping unsaved changes.</summary>
    public void DiscardAsset(UnityEngine.Object asset, object source = null)
    {
        source ??= CommandSources.Registry;
        switch (asset)
        {
            case VisualTreeAsset vta:
                DiscardDocument(vta, source);
                break;
            case StyleSheet styleSheet:
                DiscardStandalone(styleSheet, source);
                break;
        }
    }

    void DiscardDocument(VisualTreeAsset vta, object source)
    {
        if (vta == null)
            return;

        using var group = UICommandQueue.BeginGroup("Discard Changes");
        var context = new VisualTreeAssetEditingContext(vta);
        PreDiscardCommand.Execute(source, context);

        try
        {
            using var _ = ListPool<StyleSheet>.Get(out var sheets);
            CollectDocumentStyleSheets(vta, sheets);

            // A ForceUpdate reimport can replace the managed instances, orphaning the ones we hold. Capture the
            // tracked ids now so we reconcile by id afterwards — adopting whatever fresh instance the
            // AssetDatabase produced — instead of re-baselining (and leaving consumers cloned from) the stale,
            // still-edited instances.
            using var __ = ListPool<EntityId>.Get(out var ids);
            ids.Add(vta.GetEntityId());
            foreach (var styleSheet in sheets)
                if (styleSheet != null)
                    ids.Add(styleSheet.GetEntityId());

            using (new AssetDatabase.AssetEditingScope())
            {
                foreach (var styleSheet in sheets)
                    if (styleSheet != null)
                        ReimportFromDisk(AssetDatabase.GetAssetPath(styleSheet));
                ReimportFromDisk(AssetDatabase.GetAssetPath(vta));
            }

            ClearUndoForDocument(vta, sheets);

            // Adopt the freshly reimported instances, re-baseline them clean, and notify every holder (the UI
            // Stage directly; the UI Builder via the discard command below) to re-clone against the reverted
            // content.
            foreach (var id in ids)
                ReconcileAfterReimportToDisk(id);
        }
        finally
        {
            // Always close the discard boundary so the reimport suppression is released even if a reimport threw.
            PostDiscardCommand.Execute(source, context);
        }
    }

    void DiscardStandalone(StyleSheet styleSheet, object source)
    {
        if (styleSheet == null)
            return;

        using var group = UICommandQueue.BeginGroup("Discard Changes");
        PreDiscardCommand.Execute(source, styleSheet);

        try
        {
            var id = styleSheet.GetEntityId();

            using (new AssetDatabase.AssetEditingScope())
                ReimportFromDisk(AssetDatabase.GetAssetPath(styleSheet));

            Undo.ClearUndo(styleSheet);
            ReconcileAfterReimportToDisk(id);
        }
        finally
        {
            // Always close the discard boundary so the reimport suppression is released even if the reimport threw.
            PostDiscardCommand.Execute(source, styleSheet);
        }
    }

    // Adopts the freshly reimported instance of an asset just reverted to disk (a discard), re-baselines it
    // clean, drops any stale unsaved snapshot, and notifies holders to re-clone. Mirrors the clean branch of
    // ProcessExternalReimport, but a discard always takes the on-disk version — there is no conflict to
    // resolve.
    void ReconcileAfterReimportToDisk(EntityId id)
    {
        if (!m_Entries.TryGetValue(id, out var entry))
            return;

        entry = RebindFreshInstance(entry);
        m_UnsavedSnapshots.Remove(entry.Id);
        m_Dirty.Capture(entry.Asset);
        entry.WasDirty = false;
        AssetReloaded?.Invoke(entry.Asset);

        // The asset is now clean; if it was only held via the synthetic auto-track reference (a code-driven
        // edit that was then discarded), release it so it does not linger tracked forever.
        ReleaseAutoTrackIfClean(entry.Id, entry.Asset);
    }

    /// <summary>
    /// Notifies the registry that a tool reverted a tracked asset — and, for a <see cref="VisualTreeAsset"/>,
    /// its referenced stylesheets — to its on-disk state <em>in place</em>, without a reimport (the UI
    /// Builder's backup-restore discard). Re-baselines them clean and notifies other holders (e.g. the UI
    /// Stage) to re-clone against the reverted content. Use <see cref="DiscardAsset"/> instead when the caller
    /// needs the registry to re-import the on-disk version itself.
    /// </summary>
    public void NotifyReverted(UnityEngine.Object asset)
    {
        if (!IsAuthoringAsset(asset))
            return;

        EnsureBaselinesRestored();

        ReconcileRevertedInPlace(asset);
        if (asset is VisualTreeAsset vta)
        {
            using var _ = ListPool<StyleSheet>.Get(out var sheets);
            CollectDocumentStyleSheets(vta, sheets);
            foreach (var sheet in sheets)
                if (sheet != null)
                    ReconcileRevertedInPlace(sheet);
        }
    }

    // Re-baselines an asset whose in-memory content a tool already reverted in place, and notifies holders to
    // re-clone. Unlike ReconcileAfterReimportToDisk it does not adopt a fresh instance: the caller reverted
    // the live instance itself, so the tracked instance is already current.
    void ReconcileRevertedInPlace(UnityEngine.Object asset)
    {
        var id = asset.GetEntityId();

        // Only reconcile what an entry actually references. NotifyReverted walks the caller's whole stylesheet
        // graph, which can include sheets no tool holds open: capturing a baseline for one of those would leak
        // (no Untrack ever removes it) and go stale, because a later Open reuses it instead of re-capturing —
        // and dropping its snapshot would destroy the unsaved edits Untrack deliberately kept.
        if (!m_Entries.ContainsKey(id))
            return;

        m_UnsavedSnapshots.Remove(id);
        m_Dirty.Capture(asset);
        NotifyDirtyStateMaybeChanged(id, asset);
        AssetReloaded?.Invoke(asset);
        ReleaseAutoTrackIfClean(id, asset);
    }

    bool WriteAsset(UnityEngine.Object asset, string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        // Never write an asset's text over a file of a different kind. A sub-object (notably a VisualTreeAsset's
        // inline stylesheet) reports its OWNER's path, so without this a stray StyleSheet entry pointing at a
        // .uxml would silently replace the whole document with USS text.
        if (!PathMatchesKind(asset, path))
        {
            Debug.LogError($"Refusing to save '{asset?.name}' ({asset?.GetType().Name}) over '{path}': the file " +
                           "does not belong to that asset kind.");
            return false;
        }

        // Export via the same path the dirty tracker hashes for its baseline, so the bytes we write and the
        // bytes we diff against can never drift out of sync.
        var content = UIAssetDirtyTracker.ComputeContentText(asset);
        if (content == null)
            return false;

        if (!WriteTextFileToDisk(path, content))
            return false;

        // The surrounding Pre/PostSaveCommand (see OnToolSaveBoundary) suppresses this reimport so it is not
        // mistaken for an external change.
        AssetDatabase.ImportAsset(path);
        return true;
    }

    void ReimportFromDisk(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
    }

    static bool WriteTextFileToDisk(string path, string content)
    {
        var folder = Path.GetDirectoryName(path);
        if (folder != null && !Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        var success = FileUtil.WriteTextFileToDisk(path, content, out var message);
        if (!success)
            Debug.LogError(message);
        return success;
    }

    // Writes an asset's unsaved (edited) text to a backup file outside the Assets folder (so it is not itself
    // reimported) when the user chose "Save Backup & Use Imported" on the registry's own push path, where no
    // tool is present to write it. The location is logged so the work can be recovered.
    static void WriteConflictBackup(UnityEngine.Object asset, string content)
    {
        var extension = asset is StyleSheet ? "uss" : "uxml";
        var name = string.IsNullOrEmpty(asset.name) ? "UIAsset" : asset.name;
        var backupPath = $"{FileUtil.GetUniqueTempPathInProject()}_{name}.{extension}.backup";
        if (WriteTextFileToDisk(backupPath, content))
            Debug.LogWarning(
                $"'{name}' was changed outside the editor while you had unsaved changes; your changes were " +
                $"backed up to '{backupPath}'.");
    }

    static void ClearUndoForDocument(VisualTreeAsset vta, List<StyleSheet> referencedSheets)
    {
        if (vta == null)
            return;

        Undo.ClearUndo(vta);
        if (vta.inlineSheet != null)
            Undo.ClearUndo(vta.inlineSheet);
        foreach (var styleSheet in referencedSheets)
            if (styleSheet != null)
                Undo.ClearUndo(styleSheet);
    }

    /// <summary>
    /// The stylesheets that belong to a document's save/dirty scope: the ones it references directly, plus
    /// everything those <c>@import</c>, stopping at theme stylesheets.
    /// </summary>
    /// <remarks>
    /// <see cref="PanelDependencyTracker"/> registers exactly this recursive closure, so an <c>@import</c>ed
    /// sheet is tracked and can be reported dirty; querying and saving only the direct sheets would leave such
    /// an edit visible in the "*" of nothing and silently dropped on save. A <see cref="ThemeStyleSheet"/> is a
    /// project-wide asset every document imports, so it is never part of a single document's scope.
    /// </remarks>
    public static void CollectDocumentStyleSheets(VisualTreeAsset vta, List<StyleSheet> results)
    {
        if (vta == null || results == null)
            return;

        using var _ = HashSetPool<StyleSheet>.Get(out var visited);
        using var __ = ListPool<StyleSheet>.Get(out var direct);
        vta.GetAllReferencedStyleSheets(direct);

        foreach (var sheet in direct)
        {
            if (sheet == null || !visited.Add(sheet))
                continue;
            results.Add(sheet);
            CollectImportedStyleSheets(sheet, visited, results);
        }
    }

    static void CollectImportedStyleSheets(StyleSheet sheet, HashSet<StyleSheet> visited, List<StyleSheet> results)
    {
        var imports = sheet.imports;
        if (imports == null)
            return;

        foreach (var import in imports)
        {
            var imported = import.styleSheet;
            if (imported == null || imported is ThemeStyleSheet || !visited.Add(imported))
                continue;
            results.Add(imported);
            CollectImportedStyleSheets(imported, visited, results);
        }
    }

    void OnGroupEnded(in GroupEndedContext context)
    {
        // GroupEndedContext is a ref struct and its UndoObjects are only valid for the duration of this call,
        // so copy them out before touching anything else. Save/discard groups record no undo objects, so they
        // arrive here empty and are naturally ignored.
        // Note: we do NOT early-out on an empty entry set — a command can edit an authoring asset that no tool
        // has open, which we adopt via auto-tracking (see OnCommandChangedAsset).
        using var _ = HashSetPool<UnityEngine.Object>.Get(out var changed);
        foreach (var obj in context.UndoObjects)
            if (obj != null)
                changed.Add(obj);

        if (changed.Count == 0)
            return;

        EnsureBaselinesRestored();
        foreach (var obj in changed)
            MarkChangedByCommand(obj);
    }

    void MarkChangedByCommand(UnityEngine.Object obj)
    {
        switch (obj)
        {
            case VisualTreeAsset vta:
                OnCommandChangedAsset(vta.GetEntityId(), vta);
                break;
            case StyleSheet styleSheet:
                if (m_InlineSheetOwners.TryGetValue(styleSheet, out var ownerVta) && ownerVta != null)
                {
                    // An inline stylesheet is a sub-object of its document, so editing it changes the .uxml.
                    // The document itself may not have been recorded for undo (and so never dirtied), and the
                    // dirty tracker gates on EditorUtility.IsDirty: without this the document is reported clean,
                    // its unsaved snapshot is dropped, and the save writes the .uss but not the .uxml.
                    EditorUtility.SetDirty(ownerVta);
                    OnCommandChangedAsset(ownerVta.GetEntityId(), ownerVta);
                }
                else
                {
                    OnCommandChangedAsset(styleSheet.GetEntityId(), styleSheet);
                }
                break;
        }
    }

    void OnCommandChangedAsset(EntityId id, UnityEngine.Object asset)
    {
        // If no tool has this asset open, adopt it so its edit is not silently lost by the save operation.
        if (!m_Entries.ContainsKey(id) && !TryAutoTrackChangedAsset(id, asset))
            return;

        // A command just changed this asset's content; keep its unsaved snapshot current so an external
        // reimport can still offer to restore these edits.
        RefreshSnapshot(id, asset);
        NotifyDirtyStateMaybeChanged(id, asset);
    }

    /// <summary>
    /// Adopts an asset that a command dirtied while no tool had it open, so the edit participates in the save
    /// operation. Declines assets the command did not actually leave dirty, and assets that have no file of
    /// their own — a never-saved asset, or a sub-object such as a <see cref="VisualTreeAsset"/>'s inline
    /// stylesheet, which reports its OWNER's path and so cannot be saved standalone.
    /// </summary>
    bool TryAutoTrackChangedAsset(EntityId id, UnityEngine.Object asset)
    {
        if (!IsAuthoringAsset(asset) || !EditorUtility.IsDirty(asset))
            return false;

        var path = AssetDatabase.GetAssetPath(asset);
        if (string.IsNullOrEmpty(path))
            return false;

        // A sub-object's path is its owner's file, so an inline stylesheet reports "…/Doc.uxml". Adopting it
        // would create a second entry claiming the document's path — which the first-match-wins path lookup can
        // then hand the document's external reimports — and a save of that entry would write USS text over the
        // .uxml itself.
        if (!PathMatchesKind(asset, path))
            return false;

        var entry = new AssetEntry
        {
            Id = id,
            GlobalId = GlobalObjectId.GetGlobalObjectIdSlow(asset),
            Kind = asset is VisualTreeAsset ? UIAssetKind.VisualTreeAsset : UIAssetKind.StyleSheet,
            Asset = asset,
            AssetPath = path,
            // Start clean so NotifyDirtyStateMaybeChanged raises the clean->dirty transition below.
            WasDirty = false,
        };
        m_Entries[id] = entry;

        // Deliberately capture NO baseline: the edit already happened, so the on-disk file is the clean
        // reference. With no baseline and the dirty flag set, UIAssetDirtyTracker.IsModified reports the asset
        // dirty until a save captures a fresh clean baseline.
        RegisterInlineSheet(asset);

        // Hold read-write under the synthetic owner: keeping ReadWriteCount > 0 protects the pending edit from
        // the enter-edit-mode re-baseline, which only re-baselines read-only (scene-only) assets.
        entry.ReferencesByOwner[s_AutoTrackOwner] = UIAssetAccess.ReadWrite;
        entry.ReadWriteCount++;

        AssetTracked?.Invoke(asset);
        return true;
    }

    /// <summary>
    /// Releases the synthetic auto-track reference once it is the only thing keeping a now-clean asset tracked,
    /// so command-adopted assets do not linger after they are saved, discarded, or otherwise settled.
    /// </summary>
    void ReleaseAutoTrackIfClean(EntityId id, UnityEngine.Object asset)
    {
        if (!m_Entries.TryGetValue(id, out var entry))
            return;
        if (entry.ReferencesByOwner.Count != 1 || !entry.ReferencesByOwner.ContainsKey(s_AutoTrackOwner))
            return;
        if (m_Dirty.IsModified(id, asset))
            return;

        Close(id, asset, s_AutoTrackOwner);
    }

    void RefreshSnapshot(EntityId id, UnityEngine.Object asset)
    {
        // One export answers both questions: whether the content differs from the baseline, and what to snapshot.
        if (m_Dirty.TryGetModifiedContent(id, asset, out var content))
            m_UnsavedSnapshots[id] = content;
        else
            m_UnsavedSnapshots.Remove(id);
    }

    /// <summary>
    /// Reports the current unsaved (edited) text of a tracked asset. Used by tools that edit outside the
    /// <see cref="UICommandQueue"/> (the UI Builder) so the registry can offer "keep my changes" on an
    /// external reimport.
    /// </summary>
    public void SetUnsavedSnapshot(UnityEngine.Object asset, string text)
    {
        if (!IsAuthoringAsset(asset))
            return;

        var id = asset.GetEntityId();
        if (string.IsNullOrEmpty(text))
            m_UnsavedSnapshots.Remove(id);
        else
            m_UnsavedSnapshots[id] = text;

        // Tools that edit outside the command queue (the Builder) report edits only through this snapshot, so
        // re-evaluate dirtiness here to keep the registry's view current for external-conflict detection.
        NotifyDirtyStateMaybeChanged(id, asset);
    }

    void NotifyDirtyStateMaybeChanged(EntityId id, UnityEngine.Object asset)
    {
        if (!m_Entries.TryGetValue(id, out var entry))
            return;

        var nowDirty = m_Dirty.IsModified(id, asset);
        if (nowDirty == entry.WasDirty)
            return;

        entry.WasDirty = nowDirty;
        AssetDirtyStateChanged?.Invoke(asset);
    }

    // Suppresses the registry's external-change handling for an asset (and, for a VisualTreeAsset, its
    // referenced stylesheets) while ANY tool is saving/discarding it, so a tool's own reimport is never
    // treated as an external conflict. Driven by the Pre/Post save & discard commands on the queue.
    void OnToolSaveBoundary(in CommandContext context)
    {
        if (context.Status != CommandExecutionStatus.Success)
            return;

        switch (context.Command)
        {
            case PreSaveCommand c:
                SuppressToolSave(c.Asset, suppress: true);
                break;
            case PreDiscardCommand c:
                SuppressToolSave(c.Asset, suppress: true);
                break;

            // Always release the suppression, but only re-baseline when the operation actually landed: the Post
            // commands are raised from a `finally`, so a save whose write failed (a read-only or unchecked-out
            // file) still reports here. Re-baselining then would capture the UNSAVED in-memory content as clean
            // and delete its "keep my changes" snapshot, losing the edits with no "*" left to warn about.
            case PostSaveCommand c:
                SuppressToolSave(c.Asset, suppress: false);
                if (c.Succeeded)
                    RebaselineAfterToolSave(c.Asset);
                break;
            case PostDiscardCommand c:
                SuppressToolSave(c.Asset, suppress: false);
                if (c.Succeeded)
                    RebaselineAfterToolSave(c.Asset);
                break;
        }
    }

    // A tool (the UI Builder, the Stage, or the registry itself) just wrote this asset to disk or reverted it,
    // so its in-memory content now matches disk. Re-capture the clean baseline — which crucially refreshes the
    // cached dirty flag the external-reimport handler reads (entry.WasDirty) — so a later genuine external
    // change is diagnosed correctly instead of being mistaken for a lingering unsaved edit. Tools that clear
    // their changes outside the command queue (the Builder's own save clears the engine dirty flag directly)
    // would otherwise leave the registry believing the asset is still dirty.
    void RebaselineAfterToolSave(UnityEngine.Object asset)
    {
        if (asset == null)
            return;

        MarkClean(asset);
        if (asset is VisualTreeAsset vta)
        {
            using var _ = ListPool<StyleSheet>.Get(out var sheets);
            CollectDocumentStyleSheets(vta, sheets);
            foreach (var sheet in sheets)
                if (sheet != null)
                    MarkClean(sheet);
        }
    }

    void SuppressToolSave(UnityEngine.Object asset, bool suppress)
    {
        if (ReferenceEquals(asset, null))
            return;

        var id = asset.GetEntityId();
        List<string> paths;
        if (suppress)
        {
            paths = new List<string>();
            CollectAssetPaths(asset, paths);
            // Remember exactly what we suppressed. The operation's own reimport can replace the managed
            // instance, so recomputing the paths at release time from a (possibly orphaned) asset can yield a
            // different or empty set — which would strand the suppression and make us ignore every later
            // external change to those files.
            m_SuppressedPathsByAsset[id] = paths;
            foreach (var path in paths)
                m_SuppressedReimportPaths.Add(path);
            return;
        }

        if (!m_SuppressedPathsByAsset.Remove(id, out paths))
        {
            paths = new List<string>();
            CollectAssetPaths(asset, paths);
        }

        // Release after the import settles.
        EditorApplication.delayCall += () =>
        {
            foreach (var path in paths)
                m_SuppressedReimportPaths?.Remove(path);
        };
    }

    void CollectAssetPaths(UnityEngine.Object asset, List<string> paths)
    {
        if (asset == null)
            return;

        var path = AssetDatabase.GetAssetPath(asset);
        if (!string.IsNullOrEmpty(path))
            paths.Add(path);

        if (asset is VisualTreeAsset vta)
        {
            using var _ = ListPool<StyleSheet>.Get(out var sheets);
            CollectDocumentStyleSheets(vta, sheets);
            foreach (var sheet in sheets)
            {
                var sheetPath = AssetDatabase.GetAssetPath(sheet);
                if (!string.IsNullOrEmpty(sheetPath))
                    paths.Add(sheetPath);
            }
        }
    }

    void OnAssetsPostprocessed(string[] imported, string[] deleted, string[] moved)
    {
        if (m_Entries == null || m_Entries.Count == 0)
            return;

        EnsureBaselinesRestored();

        var addedPending = false;
        foreach (var path in imported)
        {
            if (!IsAuthoringPath(path))
                continue;
            if (m_SuppressedReimportPaths.Remove(path))
                continue; // our own save

            if (!TryGetEntryByPath(path, out var entry))
                continue;

            // Record the reimport and its dirtiness now, before the deferred flush: a reimport that replaces
            // the managed instance leaves the tracked (orphaned) one still holding the unsaved edits, but the
            // cached WasDirty flag can lag when a tool edits outside the command queue (i.e. the Builder refreshes
            // the registry only when its preview regenerates). OR the cached flag with a live check so a
            // genuine unsaved edit is never mistaken for a clean reload and silently overwritten.
            var wasDirty = entry.WasDirty || m_Dirty.IsModified(entry.Id, entry.Asset);

            // Adopt the freshly imported instance and re-baseline against it right here, synchronously. This is
            // the only moment at which the in-memory asset is guaranteed to still equal what is on disk: by the
            // time the deferred flush runs, a tool that claimed this reimport may already have restored its own
            // edits over it. Doing it now also means a claimed reimport — which the flush deliberately skips —
            // never leaves the entry keyed on the orphaned instance, where IsTracked/IsDirty would report false
            // for the live asset and SaveAll would skip it. The unsaved snapshot is deliberately preserved: it
            // is what "Keep My Changes" restores.
            entry = RebindFreshInstanceFromPath(entry);
            m_Dirty.Capture(entry.Asset);
            // The tracked content now equals disk, so the cached flag has to follow — the event is deliberately
            // not raised from inside an import callback; the flush below reports the settled state.
            entry.WasDirty = false;

            // Record under the entry's CURRENT id: rebinding can have re-keyed (or replaced) it.
            m_PendingReimports[entry.Id] = wasDirty;
            if (Array.IndexOf(moved, path) >= 0)
                m_MovedReimports.Add(entry.Id);
            // A fresh external change voids any conflict decision made for the previous one.
            m_ResolvedConflicts.Remove(entry.Id);
            addedPending = true;
        }

        if (addedPending)
            EditorApplication.delayCall += FlushPendingReimports;

        foreach (var path in deleted)
        {
            if (!IsAuthoringPath(path))
                continue;
            if (TryGetEntryByPath(path, out var entry))
                RemoveDeleted(entry);
        }
    }

    // Drains the reimports recorded this batch. Assets a tool claimed (ClaimExternalChange) are left to that
    // tool — it drives its own reload and pulls the decision via ResolveExternalChange. Everything else the
    // registry reconciles itself and notifies push-observers (e.g. the UI Stage) of.
    void FlushPendingReimports()
    {
        using var _ = ListPool<EntityId>.Get(out var ids);
        foreach (var id in m_PendingReimports.Keys)
            ids.Add(id);

        foreach (var id in ids)
        {
            var wasDirty = m_PendingReimports[id];
            m_PendingReimports.Remove(id); // remove per-id so entries added during processing survive

            if (m_ClaimedReimports.Contains(id))
                continue; // a tool owns the reload+resolution for this asset

            if (m_Entries.TryGetValue(id, out var entry))
                ProcessExternalReimport(entry, wasDirty);
        }

        // Claims are only added synchronously during the import batch (never during this flush), so clearing
        // them here cannot drop a claim for a reimport still to be processed.
        m_ClaimedReimports.Clear();
        m_MovedReimports.Clear();
    }

    /// <summary>
    /// Lets a tool that drives its own reload (the UI Builder) take ownership of an asset's current external
    /// reimport, so the registry defers its own reconciliation/notification for it. Must be called
    /// synchronously from the import callback (it always precedes the deferred flush). The tool then pulls the
    /// resolution decision via <see cref="ResolveExternalChange"/> during its reload.
    /// </summary>
    public void ClaimExternalChange(UnityEngine.Object asset)
    {
        if (ReferenceEquals(asset, null))
            return;

        var id = asset.GetEntityId();
        if (m_Entries.ContainsKey(id))
        {
            AddClaim(id);
            return;
        }

        // The reimport may already have replaced the managed instance — and with it the entry's key — before the
        // tool got here; postprocessor order between us and the tool is not defined. A claim recorded under the
        // dead id would leave the registry thinking nobody owns this reimport, so it would resolve the conflict
        // as well and the user would be prompted twice. Fall back to the path, which survives the replacement.
        var path = AssetDatabase.GetAssetPath(asset);
        if (!string.IsNullOrEmpty(path) && TryGetEntryByPath(path, out var entry))
            AddClaim(entry.Id);
    }

    void AddClaim(EntityId id)
    {
        // Already claimed in this batch means a flush is already scheduled for it.
        if (m_ClaimedReimports.Add(id))
            EditorApplication.delayCall += FlushPendingReimports;
    }

    /// <summary>
    /// Resolves an external-change conflict on a tracked asset that a tool is reloading itself, returning the
    /// chosen resolution. The tool is responsible for applying it (restore its edits, adopt the imported
    /// version, or write a backup). This is the single decision point shared with the registry's own push path.
    /// </summary>
    public UIAssetConflictChoice ResolveExternalChange(UnityEngine.Object asset)
    {
        if (ReferenceEquals(asset, null))
            return UIAssetConflictChoice.Keep;

        // Same instance-replacement hazard as ClaimExternalChange: the reimport may have re-keyed the entry
        // before the tool got here, and a decision cached under a dead id would never be found again.
        var id = asset.GetEntityId();
        if (!m_Entries.ContainsKey(id))
        {
            var path = AssetDatabase.GetAssetPath(asset);
            if (!string.IsNullOrEmpty(path) && TryGetEntryByPath(path, out var entry))
                id = entry.Id;
        }

        // An untracked asset has no reimport hook to void a cached decision, so never cache one for it.
        if (!m_Entries.ContainsKey(id))
            return ResolveConflict(asset);

        return ResolveConflictOnce(id, asset);
    }

    // A single external change yields a single decision, no matter how many holders of the asset ask (two
    // Builder windows sharing a document each drive their own reload). The one-shot backup choice is stored
    // downgraded so only the first resolver writes the backup file; followers just adopt the imported version.
    UIAssetConflictChoice ResolveConflictOnce(EntityId id, UnityEngine.Object asset)
    {
        if (m_ResolvedConflicts.TryGetValue(id, out var resolved))
            return resolved;

        var choice = ResolveConflict(asset);
        m_ResolvedConflicts[id] = choice == UIAssetConflictChoice.SaveBackupAndUseImported
            ? UIAssetConflictChoice.UseImported
            : choice;
        return choice;
    }

    /// <summary>
    /// Notifies push-observers that a tool has finished reloading a claimed asset after an
    /// external change, so they re-clone their views against the now-reconciled content. Called by the claiming
    /// tool once its own reload completes, which guarantees observers re-clone <em>after</em> — never during —
    /// that tool's reconciliation. The claiming tool is not itself a push-observer, so this does not re-enter it.
    /// </summary>
    public void NotifyExternalReload(UnityEngine.Object asset)
    {
        if (ReferenceEquals(asset, null))
            return;

        if (IsTracked(asset))
        {
            AssetReloaded?.Invoke(asset);
            return;
        }

        // The reimport the tool just resolved may have REPLACED the managed instance, so the object the caller
        // still holds can be the orphaned one. Resolve the entry by path instead of dropping the notification,
        // which would leave every other holder (the UI Stage) rendering the pre-import content.
        var path = AssetDatabase.GetAssetPath(asset);
        if (!string.IsNullOrEmpty(path) && TryGetEntryByPath(path, out var entry) && entry.Asset != null)
            AssetReloaded?.Invoke(entry.Asset);
    }

    void ProcessExternalReimport(AssetEntry entry, bool wasDirty)
    {
        // The entry may have been untracked or replaced between the import and this deferred call.
        if (entry == null || !m_Entries.TryGetValue(entry.Id, out var live) || live != entry)
            return;

        // Adopt whatever instance the AssetDatabase now has at the path (a reimport can replace it).
        entry = RebindFreshInstance(entry);

        if (!wasDirty)
        {
            // No unsaved edits: silently take the freshly imported version and let consumers re-clone.
            m_UnsavedSnapshots.Remove(entry.Id);
            m_Dirty.Capture(entry.Asset);
            entry.WasDirty = false;
            AssetReloaded?.Invoke(entry.Asset);
            ReleaseAutoTrackIfClean(entry.Id, entry.Asset);
            return;
        }

        // The asset had unsaved edits and was changed on disk. The registry is the single resolver: it decides
        // keep-vs-use-imported here and reconciles the content, then reports the decision so each tool that
        // holds the asset can react (rebind its live views; preserve its work on the backup choice).
        //
        // A move is the exception: the reimport it triggers rewrites the instance from disk like any other, but
        // the file's content never changed, so there is no conflict to put to the user — only edits to restore.
        var choice = m_MovedReimports.Remove(entry.Id)
            ? UIAssetConflictChoice.Keep
            : ResolveConflictOnce(entry.Id, entry.Asset);

        // Baseline against the now-on-disk (external) content either way.
        m_Dirty.Capture(entry.Asset);

        if (choice == UIAssetConflictChoice.Keep &&
            m_UnsavedSnapshots.TryGetValue(entry.Id, out var snapshot) && !string.IsNullOrEmpty(snapshot))
        {
            // Restore the user's edits over the reimported instance; it now differs from disk again -> dirty.
            UIAssetContent.RestoreFromText(entry.Asset, snapshot);
            // The restore can replace a document's inline stylesheet instance, so re-file the owner mapping;
            // otherwise a later inline-style edit is no longer recognised as a change to this document.
            RegisterInlineSheet(entry.Asset);
            entry.WasDirty = m_Dirty.IsModified(entry.Id, entry.Asset);
        }
        else
        {
            // UseImported and SaveBackupAndUseImported both leave the tracked asset matching disk (clean). On
            // the registry's own push path (an unclaimed asset — the UI Builder is not driving the reload) no
            // tool observes AssetConflictDetected to write the WIP backup, so the registry writes it itself when
            // the user asked to keep one; otherwise "Save Backup" would silently discard the edits.
            if (choice == UIAssetConflictChoice.SaveBackupAndUseImported &&
                m_UnsavedSnapshots.TryGetValue(entry.Id, out var backup) && !string.IsNullOrEmpty(backup))
                WriteConflictBackup(entry.Asset, backup);

            m_UnsavedSnapshots.Remove(entry.Id);
            entry.WasDirty = false;
        }

        // Report the resolution before the reload so a tool can preserve its work-in-progress (write a backup)
        // while it still has it, then rebind its views to the reconciled content.
        AssetConflictDetected?.Invoke(new UIAssetConflict(entry.Asset, choice));
        AssetReloaded?.Invoke(entry.Asset);

        // If the asset ended up clean (UseImported / backup) and was only held via the synthetic auto-track
        // reference, release it so a code-adopted asset does not linger tracked after the conflict settles.
        ReleaseAutoTrackIfClean(entry.Id, entry.Asset);
    }

    // Re-resolves the live instance the AssetDatabase currently has for this entry (a reimport can create a
    // new managed object, orphaning the tracked one) and re-keys the entry if its entity id changed. Returns the
    // entry that tracks the asset afterwards, which is NOT always the one passed in: it can be folded into an
    // entry that already owned the fresh id, so callers must keep using the returned one.
    AssetEntry RebindFreshInstance(AssetEntry entry)
        => RebindTo(entry, GlobalObjectId.GlobalObjectIdentifierToObjectSlow(entry.GlobalId));

    // Same, but resolving through the AssetDatabase by path and kind. Used from the import postprocess
    // callback, where the durable-id lookup is best avoided.
    AssetEntry RebindFreshInstanceFromPath(AssetEntry entry)
    {
        if (string.IsNullOrEmpty(entry.AssetPath))
            return entry;

        var type = entry.Kind == UIAssetKind.VisualTreeAsset ? typeof(VisualTreeAsset) : typeof(StyleSheet);
        return RebindTo(entry, AssetDatabase.LoadAssetAtPath(entry.AssetPath, type));
    }

    AssetEntry RebindTo(AssetEntry entry, UnityEngine.Object fresh)
    {
        if (fresh == null || ReferenceEquals(fresh, entry.Asset))
            return entry;

        var newId = fresh.GetEntityId();
        if (newId != entry.Id)
        {
            m_Entries.Remove(entry.Id);
            m_Dirty.Remove(entry.Id);
            if (m_UnsavedSnapshots.Remove(entry.Id, out var snapshot))
                m_UnsavedSnapshots[newId] = snapshot;

            // Carry a tool's claim over: it is keyed by id, and losing it here would make the registry resolve
            // this reimport as well, prompting the user a second time.
            if (m_ClaimedReimports.Remove(entry.Id))
                m_ClaimedReimports.Add(newId);

            // Same for an already-made conflict decision, or a later holder would be prompted anew.
            if (m_ResolvedConflicts.Remove(entry.Id, out var resolvedChoice))
                m_ResolvedConflicts[newId] = resolvedChoice;

            // Another entry can already own the fresh id — a second holder opened the reimported instance
            // before we rebound. Fold our owners into it rather than replacing it, so their references are not
            // silently dropped (they could then never be released, keeping the asset tracked forever).
            if (m_Entries.TryGetValue(newId, out var existing) && existing != entry)
            {
                foreach (var reference in entry.ReferencesByOwner)
                {
                    if (existing.ReferencesByOwner.TryGetValue(reference.Key, out var previous))
                        ApplyAccessChange(existing, previous, reference.Value);
                    else if (reference.Value == UIAssetAccess.ReadWrite)
                        existing.ReadWriteCount++;
                    existing.ReferencesByOwner[reference.Key] = reference.Value;
                }

                UnregisterInlineSheet(entry.Asset);
                entry.ReferencesByOwner.Clear();
                entry.ReadWriteCount = 0;
                return existing;
            }

            entry.Id = newId;
            m_Entries[newId] = entry;
        }

        UnregisterInlineSheet(entry.Asset);
        entry.Asset = fresh;
        entry.AssetPath = AssetDatabase.GetAssetPath(fresh);
        RegisterInlineSheet(entry.Asset);
        return entry;
    }

    UIAssetConflictChoice ResolveConflict(UnityEngine.Object asset)
    {
        if (ConflictResolver != null)
            return ConflictResolver(asset);

        // A caller that opted out of modals must not be blocked by one. Take the same safe default the dialog's
        // own fallback takes: never silently lose the user's work.
        if (PreventDialogsFromOpening)
            return UIAssetConflictChoice.Keep;

        // Three first-class choices (ok / cancel-slot / alt), mirroring the UI Builder's original dialog.
        var option = EditorUtility.DisplayDialogComplex(
            "UI Asset - External Change Detected",
            $"'{asset.name}' was changed outside the editor while you had unsaved changes.\n\n" +
            "Keep your changes, use the imported version and lose them, or save your changes to a backup " +
            "file and use the imported version?",
            "Keep My Changes",
            "Use Imported Version",
            "Save Backup & Use Imported");

        return option switch
        {
            0 => UIAssetConflictChoice.Keep,
            1 => UIAssetConflictChoice.UseImported,
            2 => UIAssetConflictChoice.SaveBackupAndUseImported,
            _ => UIAssetConflictChoice.Keep, // Safe default: never silently lose the user's work.
        };
    }

    void RemoveDeleted(AssetEntry entry)
    {
        m_Entries.Remove(entry.Id);
        UnregisterInlineSheet(entry.Asset);
        m_Dirty.Remove(entry.Id);
        m_UnsavedSnapshots.Remove(entry.Id);
        AssetUntracked?.Invoke(entry.Asset);
    }

    bool TryGetEntryByPath(string path, out AssetEntry result)
    {
        // Fast path: match the cached path without touching the AssetDatabase. This runs for every imported
        // authoring asset in every postprocess batch, so avoid an O(imports x entries) GetAssetPath storm.
        foreach (var entry in m_Entries.Values)
        {
            if (entry.AssetPath == path)
            {
                result = entry;
                return true;
            }
        }

        // Slow path: a cached path can go stale on a move/rename, so refresh it lazily from the still-valid
        // entity id and retry — only when the cheap comparison above found nothing.
        foreach (var entry in m_Entries.Values)
        {
            if (entry.Asset == null)
                continue;
            var current = AssetDatabase.GetAssetPath(entry.Asset);
            if (current != entry.AssetPath)
                entry.AssetPath = current;
            if (entry.AssetPath == path)
            {
                result = entry;
                return true;
            }
        }

        result = null;
        return false;
    }

    void OnPlayModeStateChanged(PlayModeStateChange change)
    {
        // Entering edit mode, Unity discards in-memory scene changes. Re-baseline read-only (scene-only)
        // assets so play-mode churn is not mistaken for unsaved edits.
        if (change != PlayModeStateChange.EnteredEditMode)
            return;

        EnsureBaselinesRestored();
        foreach (var entry in m_Entries.Values)
            if (entry.ReadWriteCount == 0 && entry.Asset != null)
                m_Dirty.Capture(entry.Asset);
    }

    // Re-files the owner mapping for a document's CURRENT inline stylesheet. Idempotent, and safe to call again
    // after the instance was swapped (a content restore recreates it): the sheet we previously registered for
    // this owner is dropped first, so the map never keeps a dead key while the live sheet goes unregistered.
    void RegisterInlineSheet(UnityEngine.Object asset)
    {
        if (asset is not VisualTreeAsset vta)
            return;

        if (m_RegisteredInlineSheets.TryGetValue(vta, out var previous) && previous != vta.inlineSheet)
            m_InlineSheetOwners.Remove(previous);

        if (vta.inlineSheet == null)
        {
            m_RegisteredInlineSheets.Remove(vta);
            return;
        }

        m_InlineSheetOwners[vta.inlineSheet] = vta;
        m_RegisteredInlineSheets[vta] = vta.inlineSheet;
    }

    void UnregisterInlineSheet(UnityEngine.Object asset)
    {
        if (asset is not VisualTreeAsset vta)
            return;

        // Remove what we actually registered, not just whatever the owner points at now.
        if (m_RegisteredInlineSheets.Remove(vta, out var registered) && registered != null)
            m_InlineSheetOwners.Remove(registered);
        if (vta.inlineSheet != null)
            m_InlineSheetOwners.Remove(vta.inlineSheet);
    }

    static bool IsAuthoringAsset(UnityEngine.Object asset)
        => asset is VisualTreeAsset or StyleSheet;

    static bool IsAuthoringPath(string path)
        => IsDocumentPath(path) || IsStyleSheetPath(path);

    static bool IsDocumentPath(string path)
        => path.EndsWith(".uxml", StringComparison.OrdinalIgnoreCase);

    static bool IsStyleSheetPath(string path)
        => path.EndsWith(".uss", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".tss", StringComparison.OrdinalIgnoreCase);

    // Whether the file at this path is the asset's OWN file. False for a sub-object, which reports its owner's
    // path (a VisualTreeAsset's inline stylesheet reports the .uxml).
    static bool PathMatchesKind(UnityEngine.Object asset, string path)
        => asset switch
        {
            VisualTreeAsset => IsDocumentPath(path),
            StyleSheet => IsStyleSheetPath(path),
            _ => false,
        };

    // A serialized unsaved-edit snapshot: the durable asset id plus the exported (edited) text, persisted so
    // "Keep my changes" can be honored after a domain reload.
    [Serializable]
    struct AssetSnapshot
    {
        public GlobalObjectId AssetId;
        public string Text;
    }

    // Routes AssetDatabase import/delete notifications to the live registry (if any) without forcing the
    // singleton into existence.
    class Postprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            s_Live?.OnAssetsPostprocessed(imported, deleted, moved);
        }
    }
}
