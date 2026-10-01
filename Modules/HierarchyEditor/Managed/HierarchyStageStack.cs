// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Unity.Hierarchy.Editor
{
    /// <summary>
    /// What happened to the shared data, so consumers can tell a stage transition from a rebuild in place.
    /// </summary>
    enum HierarchyStageChange
    {
        /// <summary>The same stage's data was rebuilt; consumers restore what they were showing.</summary>
        DataRebuilt,
        /// <summary>A different stage became current, so its persisted view state applies.</summary>
        StageSwitched,
        /// <summary>The same stage's data was replaced by a reload of its contents.</summary>
        StageReloaded
    }

    /// <summary>
    /// Owns the <see cref="Hierarchy"/> and <see cref="HierarchyFlattened"/> shared by every
    /// <see cref="HierarchyWindow"/>, one entry per open stage.
    /// </summary>
    /// <remarks>
    /// Entries outlive the windows bound to them. Consumers drive the shared data themselves, so nothing is
    /// updated while no window is open and the content stays stale until the next update; that is expected. An
    /// entry is only ever created for the stage that is current at that moment, because the scene handler
    /// resolves its scenes from the current stage once, when it is initialized.
    /// </remarks>
    static partial class HierarchyStageStack
    {
        sealed class Entry
        {
            public Stage Stage;
            public Hierarchy Hierarchy;
            public HierarchyFlattened Flattened;
            public int UndoId;
        }

        const string k_UndoIdKey = "HierarchyStageStack";

        // Cleaned up explicitly by Shutdown so disposal never depends on statics-cleanup ordering.
        [NoAutoStaticsCleanup]
        static readonly List<Entry> s_Entries = new();

        [NoAutoStaticsCleanup]
        static bool s_Subscribed;

        /// <summary>
        /// Raised before the current entry is replaced. Consumers must stop referencing the shared data.
        /// </summary>
        [NoAutoStaticsCleanup]
        internal static event Action Changing;

        /// <summary>
        /// Raised after the current entry has been replaced. Consumers should rebind to the shared data. The
        /// argument says what happened, which is what decides whether persisted per-stage view state applies.
        /// </summary>
        [NoAutoStaticsCleanup]
        internal static event Action<HierarchyStageChange> Changed;

        /// <summary>
        /// Whether an entry exists for the current stage. Reading this never creates one.
        /// </summary>
        internal static bool IsCreated => FindEntry(CurrentStage) != null;

        /// <summary>
        /// The <see cref="Hierarchy"/> for the current stage, created on first access.
        /// </summary>
        internal static Hierarchy Current => EnsureCurrentEntry().Hierarchy;

        /// <summary>
        /// The <see cref="HierarchyFlattened"/> for the current stage, created on first access.
        /// </summary>
        internal static HierarchyFlattened CurrentFlattened => EnsureCurrentEntry().Flattened;

        /// <summary>
        /// The <see cref="HierarchyUndoManager"/> id of the current stage's entry.
        /// </summary>
        internal static int CurrentUndoId => EnsureCurrentEntry().UndoId;

        /// <summary>
        /// Disposes every entry. The current stage's entry is recreated on next access.
        /// </summary>
        /// <remarks>
        /// This is the only way to drop the nodes of an unregistered node type handler, because the shared data
        /// outlives the windows and a handler's nodes cannot be stripped in place.
        /// </remarks>
        internal static void Reload()
        {
            Changing?.Invoke();
            DisposeAllEntries();
            Changed?.Invoke(HierarchyStageChange.DataRebuilt);
        }

        /// <summary>
        /// Invokes <paramref name="action"/> for the shared data of every stage that currently has an entry.
        /// </summary>
        internal static void ForEachEntry(Action<Hierarchy, HierarchyFlattened> action)
        {
            foreach (var entry in s_Entries)
            {
                if (entry.Hierarchy is { IsCreated: true } && entry.Flattened is { IsCreated: true })
                    action(entry.Hierarchy, entry.Flattened);
            }
        }

        /// <summary>
        /// Instantiates the node type handlers registered with <see cref="HierarchyWindowManager"/> on every entry.
        /// </summary>
        internal static void InstantiateNodeTypeHandlers()
        {
            foreach (var entry in s_Entries)
            {
                if (entry.Hierarchy is { IsCreated: true })
                    HierarchyWindowManager.InstantiateNodeTypeHandlers(entry.Hierarchy);
            }
        }

        // Lazy so an editor that never opens a Hierarchy window does not build the StageNavigationManager singleton
        static void EnsureSubscribed()
        {
            if (s_Subscribed)
                return;

            s_Subscribed = true;
            StageNavigationManager.instance.afterSuccessfullySwitchedToStage += OnAfterSuccessfullySwitchedToStage;
            PrefabStage.prefabStageReloaded += OnPrefabStageReloaded;
            PrefabUtility.prefabInstanceUpdated += OnPrefabInstanceUpdated;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        [OnCodeDeinitializing]
        static void Shutdown()
        {
            // Guarded so teardown does not build the ScriptableSingleton just to unsubscribe from it
            if (s_Subscribed)
            {
                s_Subscribed = false;
                Undo.undoRedoPerformed -= OnUndoRedoPerformed;
                PrefabUtility.prefabInstanceUpdated -= OnPrefabInstanceUpdated;
                PrefabStage.prefabStageReloaded -= OnPrefabStageReloaded;
                StageNavigationManager.instance.afterSuccessfullySwitchedToStage -= OnAfterSuccessfullySwitchedToStage;
            }

            // Dispose here rather than leaving it to a finalizer: the native Hierarchy destructor calls back into
            // managed code to release its handlers, which is only valid on the main thread with the domain alive.
            Changing?.Invoke();
            DisposeAllEntries();

            Changing = null;
            Changed = null;
        }

        static Stage CurrentStage => StageNavigationManager.instance.currentStage;

        static Entry EnsureCurrentEntry()
        {
            EnsureSubscribed();

            var stage = CurrentStage;
            var entry = FindEntry(stage);
            if (entry != null)
                return entry;

            entry = CreateEntry(stage);
            s_Entries.Add(entry);
            return entry;
        }

        static Entry CreateEntry(Stage stage)
        {
            var hierarchy = new Hierarchy();
            HierarchyWindowManager.InstantiateNodeTypeHandlers(hierarchy);

            var entry = new Entry
            {
                Stage = stage,
                Hierarchy = hierarchy,
                Flattened = new HierarchyFlattened(hierarchy),
                UndoId = ComputeUndoId(stage)
            };

            HierarchyUndoManager.Register(entry.UndoId, hierarchy);
            return entry;
        }

        static int ComputeUndoId(Stage stage)
        {
            // The id is stored inside undo records, so it has to resolve to this stage's entry again after a
            // domain reload. Id 0 is reserved by HierarchyUndoManager to mean unregistered.
            var undoId = StageUtility.CreateWindowAndStageIdentifier(k_UndoIdKey, stage).GetHashCode();
            return undoId != 0 ? undoId : 1;
        }

        static Entry FindEntry(Stage stage)
        {
            if (stage == null)
                return null;

            foreach (var entry in s_Entries)
            {
                if (ReferenceEquals(entry.Stage, stage))
                    return entry;
            }

            return null;
        }

        static int IndexOfEntry(Stage stage)
        {
            for (var i = 0; i < s_Entries.Count; ++i)
            {
                if (ReferenceEquals(s_Entries[i].Stage, stage))
                    return i;
            }

            return -1;
        }

        static void DisposeAllEntries()
        {
            for (var i = s_Entries.Count - 1; i >= 0; --i)
                DisposeEntryAt(i);
        }

        static void DisposeEntryAt(int index)
        {
            var entry = s_Entries[index];
            s_Entries.RemoveAt(index);

            HierarchyUndoManager.Unregister(entry.UndoId);

            // The flattened holds a reference to the hierarchy, so it has to go first.
            if (entry.Flattened is { IsCreated: true })
                entry.Flattened.Dispose();
            if (entry.Hierarchy is { IsCreated: true })
                entry.Hierarchy.Dispose();
        }

        static void OnAfterSuccessfullySwitchedToStage(Stage stage)
        {
            // Create the new entry and let consumers move onto it before disposing what is left behind, so a
            // consumer rebinds in a single step and never holds a reference to a disposed entry.
            EnsureCurrentEntry();
            Changed?.Invoke(HierarchyStageChange.StageSwitched);
            PruneEntriesNotInHistory();
        }

        static void OnPrefabStageReloaded(PrefabStage stage)
        {
            // The stage's preview scene was replaced, so every node in its entry refers to dead objects. The
            // entry has to be disposed before its replacement exists, so consumers are told to let go first.
            Changing?.Invoke();

            var index = IndexOfEntry(stage);
            if (index >= 0)
                DisposeEntryAt(index);

            PruneEntriesNotInHistory();
            EnsureCurrentEntry();
            Changed?.Invoke(HierarchyStageChange.StageReloaded);
        }

        static void PruneEntriesNotInHistory()
        {
            var history = StageNavigationManager.instance.stageHistory;
            for (var i = s_Entries.Count - 1; i >= 0; --i)
            {
                if (!IsInHistory(history, s_Entries[i].Stage))
                    DisposeEntryAt(i);
            }
        }

        static bool IsInHistory(List<Stage> history, Stage stage)
        {
            foreach (var historyStage in history)
            {
                if (ReferenceEquals(historyStage, stage))
                    return true;
            }

            return false;
        }

        static void OnPrefabInstanceUpdated(GameObject gameObject)
        {
            if (gameObject == null || !gameObject)
                return;

            SetAllDirty();
        }

        static void OnUndoRedoPerformed() => SetAllDirty();

        static void SetAllDirty()
        {
            foreach (var entry in s_Entries)
            {
                if (entry.Hierarchy is { IsCreated: true })
                    entry.Hierarchy.SetDirty();
            }
        }
    }
}
