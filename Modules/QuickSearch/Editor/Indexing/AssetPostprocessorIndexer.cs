// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

namespace UnityEditor.Search
{
    readonly struct AssetIndexChangeSet
    {
        public readonly string[] updated;
        public readonly string[] removed;

        public AssetIndexChangeSet(string[] updated, string[] removed)
        {
            this.removed = removed;
            this.updated = updated;
        }

        public AssetIndexChangeSet(IEnumerable<string> updated, IEnumerable<string> removed, IEnumerable<string> moved, Func<string, bool> predicate)
        {
            this.updated = updated.Concat(moved).Distinct().Where(predicate).ToArray();
            this.removed = removed.Distinct().Where(predicate).ToArray();
        }

        public AssetIndexChangeSet(IEnumerable<string> updated, IEnumerable<string> removed, Func<string, bool> predicate)
        {
            this.updated = updated.Where(predicate).ToArray();
            this.removed = removed.Distinct().Where(predicate).ToArray();
        }

        public bool empty => updated?.Length == 0 && removed?.Length == 0;
        public IEnumerable<string> all => updated.Concat(removed).Distinct();
    }

    class AssetPostprocessorIndexer : AssetPostprocessor
    {
        private static bool s_Enabled;
        private static double s_BatchStartTime;

        private static readonly HashSet<string> s_UpdatedItems = new HashSet<string>();
        private static readonly HashSet<string> s_RemovedItems = new HashSet<string>();
        private static readonly HashSet<string> s_MovedItems = new HashSet<string>();

        const string k_TransactionDatabasePath = "Library/QuickSearch/transactions.db";
        private static TransactionManager transactionManager;

        private static readonly object s_ContentRefreshedLock = new object();

        private static event Action<string[], string[], string[]> s_ContentRefreshed;
        public static event Action<string[], string[], string[]> contentRefreshed
        {
            add
            {
                lock (s_ContentRefreshedLock)
                {
                    Enable();
                    s_ContentRefreshed -= value;
                    s_ContentRefreshed += value;
                }
            }

            remove
            {
                lock (s_ContentRefreshedLock)
                {
                    s_ContentRefreshed -= value;
                    if (s_ContentRefreshed == null || s_ContentRefreshed.GetInvocationList().Length == 0)
                        Disable();
                }
            }
        }

        public static bool pending => s_UpdatedItems.Count > 0 || s_RemovedItems.Count > 0 || s_MovedItems.Count > 0;

        static AssetPostprocessorIndexer()
        {
            if (!IsMainProcess())
                return;

            transactionManager = new TransactionManager(k_TransactionDatabasePath);
            transactionManager.Init();

            EditorApplication.quitting += OnQuitting;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        static void OnBeforeAssemblyReload()
        {
            transactionManager?.Shutdown();
        }

        public static bool IsMainProcess()
        {
            if (EditorUtility.isInSafeMode)
                return false;

            if (AssetDatabaseAPI.IsAssetImportWorkerProcess())
                return false;

            if (MPE.ProcessService.level != MPE.ProcessLevel.Main)
                return false;

            return true;
        }

        public static void Enable()
        {
            if (!IsMainProcess())
                return;
            s_Enabled = true;
        }

        public static AssetIndexChangeSet GetDiff(long timestamp, IEnumerable<string> deletedAssets, Func<string, bool> predicate)
        {
            if (transactionManager == null)
                return default;

            var updated = new HashSet<string>();
            var moved = new HashSet<string>();
            var removed = new HashSet<string>(deletedAssets);
            var transactions = transactionManager.Read(TimeRange.From(DateTime.FromBinary(timestamp), false));
            foreach (var t in transactions)
            {
                var state = (AssetModification)t.state;
                var assetPath = AssetDatabase.GUIDToAssetPath(t.guid.ToString());
                if (state.HasFlag(AssetModification.Updated))
                {
                    updated.Add(assetPath);
                }
                else if (state.HasFlag(AssetModification.Moved))
                {
                    moved.Add(assetPath);
                }
                else if (state.HasFlag(AssetModification.Removed))
                {
                    removed.Add(assetPath);
                }
            }
            return new AssetIndexChangeSet(updated, removed, moved, predicate);
        }

        public static void Disable()
        {
            s_Enabled = false;
        }

        private static void OnQuitting()
        {
            transactionManager?.Shutdown();
            s_Enabled = false;
        }

        internal static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] movedTo, string[] movedFrom)
        {
            if (AssetDatabaseAPI.IsAssetImportWorkerProcess())
                return;

            RaiseContentRefreshed(imported, deleted.Concat(movedFrom).Distinct().ToArray(), movedTo);
        }

        internal static void RaiseContentRefreshed(IEnumerable<string> updated, IEnumerable<string> removed, IEnumerable<string> moved)
        {
            if (transactionManager != null && transactionManager.Initialized)
            {
                var timestamp = DateTime.Now.ToBinary();
                var transactions = updated.Select(path => new Transaction(AssetDatabase.AssetPathToGUID(path), AssetModification.Updated, timestamp))
                    .Concat(removed.Select(path => new Transaction(AssetDatabase.AssetPathToGUID(path), AssetModification.Removed, timestamp)))
                    .Concat(moved.Select(path => new Transaction(AssetDatabase.AssetPathToGUID(path), AssetModification.Moved, timestamp)));
                transactionManager.Write(transactions);
            }

            if (!s_Enabled)
                return;

            s_UpdatedItems.UnionWith(updated);
            s_RemovedItems.UnionWith(removed);
            s_MovedItems.UnionWith(moved);

            if (s_UpdatedItems.Count > 0 || s_RemovedItems.Count > 0 || s_MovedItems.Count > 0)
            {
                s_BatchStartTime = EditorApplication.timeSinceStartup;
                EditorApplication.tick -= RaiseContentRefreshed;
                EditorApplication.tick += RaiseContentRefreshed;
            }
        }

        private static void RaiseContentRefreshed()
        {
            EditorApplication.tick -= RaiseContentRefreshed;
            var currentTime = EditorApplication.timeSinceStartup;
            if (currentTime - s_BatchStartTime > 0.5)
            {
                if (s_UpdatedItems.Count == 0 && s_RemovedItems.Count == 0 && s_MovedItems.Count == 0)
                    return;

                s_ContentRefreshed?.Invoke(s_UpdatedItems.ToArray(), s_RemovedItems.ToArray(), s_MovedItems.ToArray());
                s_UpdatedItems.Clear();
                s_RemovedItems.Clear();
                s_MovedItems.Clear();
            }
            else
            {
                EditorApplication.tick += RaiseContentRefreshed;
            }
        }
    }

    static class AssetDatabaseAPI
    {
        public static bool IsAssetImportWorkerProcess()
        {
            #pragma warning disable CS0618 // Type or member is obsolete
            return AssetDatabaseExperimental.IsAssetImportWorkerProcess();
            #pragma warning restore CS0618 // Type or member is obsolete
        }

        public static void RegisterCustomDependency(string name, Hash128 hash)
        {
            #pragma warning disable CS0618 // Type or member is obsolete
            AssetDatabaseExperimental.RegisterCustomDependency(name, hash);
            #pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}
