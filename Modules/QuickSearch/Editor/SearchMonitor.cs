// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define DEBUG_SEARCH_MONITOR
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;


namespace UnityEditor.Search
{
    public readonly struct AssetIndexChangeSet
    {
        static readonly string[] s_EmptyStrings = Array.Empty<string>();
        internal static readonly AssetIndexChangeSet s_Empty = new AssetIndexChangeSet(null, null);

        public readonly string[] updated;
        public readonly string[] removed;

        public AssetIndexChangeSet(string[] updated, string[] removed)
        {
            this.removed = removed ?? s_EmptyStrings;
            this.updated = updated ?? s_EmptyStrings;
        }

        public AssetIndexChangeSet(IEnumerable<string> updated, IEnumerable<string> removed, IEnumerable<string> moved, Func<string, bool> predicate)
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            : this(updated?.Concat(moved).Distinct(), removed, predicate)
#pragma warning restore RS0030
        {
        }

        public AssetIndexChangeSet(IEnumerable<string> updated, IEnumerable<string> removed, Func<string, bool> predicate)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            this.updated = updated?.Except(removed).Where(predicate).ToArray() ?? s_EmptyStrings;
#pragma warning restore RS0030
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            this.removed = removed?.Distinct().Where(predicate).ToArray() ?? s_EmptyStrings;
#pragma warning restore RS0030
        }

        public bool empty => updated == null || removed == null || (updated.Length == 0 && removed.Length == 0);
        #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
        public IEnumerable<string> all => updated?.Concat(removed ?? Array.Empty<string>()).Distinct() ?? Array.Empty<string>();
#pragma warning restore RS0030
    }

    class AssetChangedListener : AssetPostprocessor
    {
        internal static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] movedTo, string[] movedFrom)
        {
            if (!Utils.IsMainProcess())
                return;

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            SearchMonitor.RaiseContentRefreshed(imported, deleted?.Concat(movedFrom).Distinct().ToArray(), movedTo);
#pragma warning restore RS0030
        }
    }

    public struct SearchMonitorView : IDisposable
    {
        static ConcurrentDictionary<int, SearchMonitorView> s_PropertyDatabaseViews = new ConcurrentDictionary<int, SearchMonitorView>();
        internal IPropertyDatabaseView propertyDatabaseView { get; }
        internal IPropertyDatabaseView propertyAliasesView { get; }

        bool m_Disposed;
        bool m_NeedsDispose;

        public SearchMonitorView(PropertyDatabase propertyDatabase, PropertyDatabase propertyAliases, bool delayedSync = false)
        {
            m_Disposed = false;
            if (s_PropertyDatabaseViews.TryGetValue(Thread.CurrentThread.ManagedThreadId, out var currentViews))
            {
                propertyDatabaseView = currentViews.propertyDatabaseView;
                propertyAliasesView = currentViews.propertyAliasesView;
                m_NeedsDispose = false;
            }
            else
            {
                propertyDatabaseView = propertyDatabase.GetView(delayedSync);
                propertyAliasesView = propertyAliases.GetView(delayedSync);
                m_NeedsDispose = true;
                s_PropertyDatabaseViews.TryAdd(Thread.CurrentThread.ManagedThreadId, this);
            }
        }

        public void Dispose()
        {
            if (m_Disposed || !m_NeedsDispose)
                return;

            s_PropertyDatabaseViews.TryRemove(Thread.CurrentThread.ManagedThreadId, out _);
            propertyDatabaseView.Dispose();
            propertyAliasesView.Dispose();
            m_Disposed = true;
        }

        public bool TryLoadProperty(ulong documentKey, string propertyName, out PropertyDatabaseRecordKey recordKey, out object value, out string alias)
        {
            var propertyHash = PropertyDatabase.CreatePropertyHash(propertyName);
            recordKey = PropertyDatabase.CreateRecordKey(documentKey, propertyHash);
            if (!propertyAliasesView.TryLoad(recordKey, out object aliasObj))
            {
                value = null;
                alias = propertyName;
                SearchMonitor.Log($"<color=red>Failed</color> to load {propertyName} without alias", recordKey, propertyName);
                return false;
            }

            alias = (aliasObj as string) ?? propertyName;
            if (propertyDatabaseView.TryLoad(recordKey, out value))
            {
                SearchMonitor.Log($"Load property {propertyName}", recordKey, value);
                return true;
            }

            SearchMonitor.Log($"<color=red>Failed</color> to load property {propertyName}", recordKey, propertyName);
            return false;
        }

        public bool TryLoadProperty(PropertyDatabaseRecordKey recordKey, out object data)
        {
            if (propertyDatabaseView.TryLoad(recordKey, out data))
            {
                SearchMonitor.Log("Load property", recordKey, data);
                return true;
            }

            SearchMonitor.Log("<color=red>Failed</color> to load property", recordKey);
            return false;
        }

        public void StoreProperty(PropertyDatabaseRecordKey recordKey, object value)
        {
            propertyDatabaseView.Store(recordKey, value);
            SearchMonitor.Log("Store property", recordKey, value);
        }

        public void StoreProperty(PropertyDatabaseRecordKey recordKey, object value, string alias)
        {
            StoreProperty(recordKey, value);
            StoreAlias(recordKey, alias);
        }

        public bool TryLoadAlias(PropertyDatabaseRecordKey recordKey, out string alias)
        {
            if (!propertyAliasesView.TryLoad(recordKey, out object aliasObj))
            {
                SearchMonitor.Log("<color=red>Failed</color> to load alias", recordKey);
                alias = null;
                return false;
            }
            alias = aliasObj as string;
            SearchMonitor.Log("Load alias", recordKey, alias);
            return alias != null;
        }

        public void StoreAlias(PropertyDatabaseRecordKey key, string alias)
        {
            propertyAliasesView.Store(key, alias);
            SearchMonitor.Log("Store alias", key, alias);
        }

        public void InvalidateDocument(ulong documentKey)
        {
            SearchMonitor.Log("Invalidate document", documentKey);
            propertyDatabaseView.Invalidate(documentKey);
            propertyAliasesView.Invalidate(documentKey);
        }

        public void InvalidateDocument(string documentKey)
        {
            SearchMonitor.Log("Invalidate document");
            propertyDatabaseView.Invalidate(documentKey);
            propertyAliasesView.Invalidate(documentKey);
        }

        public void Invalidate(PropertyDatabaseRecordKey recordKey)
        {
            SearchMonitor.Log("Invalidate record", recordKey);
            propertyDatabaseView.Invalidate(recordKey);
            propertyAliasesView.Invalidate(recordKey);
        }
    }

    [InitializeOnLoad]
    public static class SearchMonitor
    {
        static volatile bool s_Initialize = false;
        static bool s_ContentRefreshedEnabled;

        static readonly HashSet<string> s_UpdatedItems = new HashSet<string>();
        static readonly HashSet<string> s_RemovedItems = new HashSet<string>();
        static readonly HashSet<string> s_MovedItems = new HashSet<string>();

        static Action s_ContentRefreshOff;
        static Delayer s_DelayedInvalidate;
        static readonly HashSet<ulong> s_DocumentsToInvalidate = new HashSet<ulong>();

        public static bool pending => s_UpdatedItems.Count > 0 || s_RemovedItems.Count > 0 || s_MovedItems.Count > 0;

        internal static bool enabled => InternalEditorUtility.isHumanControllingUs || File.Exists("Assets/Editor/test_search_monitor.txt");

        public static event Action sceneChanged;
        public static event ObjectChangeEvents.ObjectChangeEventsHandler objectChanged;
        public static event Action documentsInvalidated;

        internal const string k_TransactionDatabasePath = "Library/Search/transactions.db";
        static TransactionManager s_TransactionManager;
        // Internal for testing only
        internal static TransactionManager transactionManager => s_TransactionManager;

        private static PropertyDatabase propertyDatabase;
        private static PropertyDatabase propertyAliases;

        static readonly object s_ContentRefreshedLock = new object();
        static event Action<string[], string[], string[]> s_ContentRefreshed;
        public static event Action<string[], string[], string[]> contentRefreshed
        {
            add
            {
                lock (s_ContentRefreshedLock)
                {
                    EnableContentRefresh();
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
                        DisableContentRefresh();
                }
            }
        }

        static double refreshContentDelay
        {
            get
            {
                if (Utils.runningTests)
                    return 0d;
                return 1d;
            }
        }

        static SearchMonitor()
        {
            if (!Utils.IsMainProcess() || !enabled)
                return;

            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                Init();
            else
                Utils.CallDelayed(Init);
        }

        static void Init()
        {
            if (s_Initialize)
                return;

            Log("Initialize search monitor", 0UL);

            s_TransactionManager = new TransactionManager(k_TransactionDatabasePath);
            s_TransactionManager.Init();

            propertyDatabase = new PropertyDatabase("Library/Search/propertyDatabase.db", true);
            propertyAliases = new PropertyDatabase("Library/Search/propertyAliases.db", true);

            EditorApplication.quitting += OnQuitting;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            EditorSceneManager.activeSceneChangedInEditMode += (_, __) => InvalidateCurrentScene();
            EditorSceneManager.sceneOpened += (scene, mode) => InvalidateCurrentScene();
            EditorSceneManager.sceneClosed += scene => InvalidateCurrentScene();
            PrefabStage.prefabStageOpened += _ => InvalidateCurrentScene();
            PrefabStage.prefabStageClosing += _ => InvalidateCurrentScene();
            EditorApplication.playModeStateChanged += _ => InvalidateCurrentScene();

            s_DelayedInvalidate = Delayer.Debounce(_ => InvalidateDocuments());

            ObjectChangeEvents.changesPublished += OnObjectChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;

            s_Initialize = true;
        }

        public static AssetIndexChangeSet GetDiff(long timestamp, IEnumerable<string> deletedAssets, Func<string, bool> predicate)
        {
            if (s_TransactionManager == null)
                return AssetIndexChangeSet.s_Empty;

            var updated = new HashSet<string>();
            var moved = new HashSet<string>();
            var removed = new HashSet<string>(deletedAssets);
            var transactions = s_TransactionManager.Read(TimeRange.From(DateTime.FromBinary(timestamp).ToUniversalTime(), false));
            foreach (var t in transactions)
            {
                var state = (AssetModification)t.state;
                var assetPath = AssetDatabase.GUIDToAssetPath(t.guid.ToString());
                if ((state & AssetModification.Updated) == AssetModification.Updated)
                    updated.Add(assetPath);
                else if ((state & AssetModification.Moved) == AssetModification.Moved)
                    moved.Add(assetPath);
                else if ((state & AssetModification.Removed) == AssetModification.Removed)
                    removed.Add(assetPath);
            }
            return new AssetIndexChangeSet(updated, removed, moved, predicate);
        }

        public static void RaiseContentRefreshed(string[] updated, string[] removed, string[] moved)
        {
            UpdateTransactionManager(updated, removed, moved);
            InvalidatePropertyDatabase(updated, removed, moved);

            if (!s_ContentRefreshedEnabled)
                return;

            s_UpdatedItems.UnionWith(updated);
            s_RemovedItems.UnionWith(removed);
            s_MovedItems.UnionWith(moved);

            if (s_UpdatedItems.Count > 0 || s_RemovedItems.Count > 0 || s_MovedItems.Count > 0)
            {
                Log("Content changed", (ulong)(s_UpdatedItems.Count + s_RemovedItems.Count + s_MovedItems.Count));
                s_ContentRefreshOff?.Invoke();
                s_ContentRefreshOff = Utils.CallDelayed(RaiseContentRefreshed, refreshContentDelay);
            }
        }

        internal static Task TriggerPropertyDatabaseBackgroundUpdate()
        {
            return Task.WhenAll(propertyDatabase.TriggerBackgroundUpdate(), propertyAliases.TriggerBackgroundUpdate());
        }


        internal static void PrintInfo()
        {
            var sb = new StringBuilder();
            sb.Append(propertyDatabase.GetInfo());
            sb.Append(propertyAliases.GetInfo());
            sb.Append(s_TransactionManager.GetInfo());
            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, sb.ToString());
        }

        public static SearchMonitorView GetView(bool delayedSync = false)
        {
            if (!s_Initialize)
                Init();
            return new SearchMonitorView(propertyDatabase, propertyAliases, delayedSync);
        }


        static void OnPlayModeChanged(PlayModeStateChange playMode)
        {
            if (playMode == PlayModeStateChange.EnteredPlayMode)
            {
                EditorApplication.hierarchyChanged += OnHierarchyChanged;
            }
            else
            {
                EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            }
        }

        static void OnHierarchyChanged()
        {
            InvalidateCurrentScene();
        }

        static void DisableContentRefresh()
        {
            s_ContentRefreshedEnabled = false;
        }

        static void EnableContentRefresh()
        {
            s_ContentRefreshedEnabled = true;
        }

        static void OnObjectChanged(ref ObjectChangeEventStream stream)
        {
            HandleObjectChanged(ref stream);

            var handler = objectChanged;
            handler?.Invoke(ref stream);
        }

        static void InvalidateCurrentScene()
        {
            var handler = sceneChanged;
            handler?.Invoke();
            Log("Invalidate scene", 0UL);
        }

        static void OnQuitting()
        {
            s_ContentRefreshedEnabled = false;
            s_TransactionManager?.Shutdown();
            propertyDatabase?.Dispose();
            propertyAliases?.Dispose();
        }

        static void OnBeforeAssemblyReload()
        {
            s_TransactionManager?.Shutdown();
            propertyDatabase?.Dispose();
            propertyAliases?.Dispose();
        }

        static void RaiseContentRefreshed()
        {
            s_ContentRefreshOff = null;
            if (s_UpdatedItems.Count == 0 && s_RemovedItems.Count == 0 && s_MovedItems.Count == 0)
                return;

            Log("Content refreshed", (ulong)(s_UpdatedItems.Count + s_RemovedItems.Count + s_MovedItems.Count));
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            s_ContentRefreshed?.Invoke(s_UpdatedItems.ToArray(), s_RemovedItems.ToArray(), s_MovedItems.ToArray());
#pragma warning restore RS0030
            s_UpdatedItems.Clear();
            s_RemovedItems.Clear();
            s_MovedItems.Clear();
        }

        static void UpdateTransactionManager(string[] updated, string[] removed, string[] moved)
        {
            if (s_TransactionManager != null && s_TransactionManager.Initialized)
            {
                var timestamp = DateTime.UtcNow.ToBinary();
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var transactions = updated.Select(path => new Transaction(AssetDatabase.AssetPathToGUID(path), AssetModification.Updated, timestamp))
#pragma warning restore RS0030
                    #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    .Concat(removed.Select(path => new Transaction(AssetDatabase.AssetPathToGUID(path), AssetModification.Removed, timestamp)))
#pragma warning restore RS0030
                    #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    .Concat(moved.Select(path => new Transaction(AssetDatabase.AssetPathToGUID(path), AssetModification.Moved, timestamp))).ToList();
#pragma warning restore RS0030
                s_TransactionManager.Write(transactions);
            }
        }

        static void InvalidatePropertyDatabase(string[] updated, string[] removed, string[] moved)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var assetPath in updated.Concat(removed).Concat(moved))
#pragma warning restore RS0030
                InvalidateDocument(AssetDatabase.AssetPathToGUID(assetPath));
        }

        static void HandleObjectChanged(ref ObjectChangeEventStream stream)
        {
            for (int i = 0; i < stream.length; ++i)
            {
                var eventType = stream.GetEventType(i);
                switch (eventType)
                {
                    case ObjectChangeKind.None:
                    case ObjectChangeKind.ChangeScene:
                    case ObjectChangeKind.ChangeAssetObjectProperties:
                    case ObjectChangeKind.CreateAssetObject:
                    case ObjectChangeKind.DestroyAssetObject:
                        break;

                    case ObjectChangeKind.DestroyGameObjectHierarchy:
                    {
                        stream.GetDestroyGameObjectHierarchyEvent(i, out var e);
                        InvalidateObject(e.entityId);
                    }
                    break;

                    case ObjectChangeKind.CreateGameObjectHierarchy:
                    {
                        stream.GetCreateGameObjectHierarchyEvent(i, out var e);
                        InvalidateObject(e.entityId);
                    }
                    break;

                    case ObjectChangeKind.ChangeGameObjectStructureHierarchy:
                    {
                        stream.GetChangeGameObjectStructureHierarchyEvent(i, out var e);
                        InvalidateObject(e.entityId);
                    }
                    break;

                    case ObjectChangeKind.ChangeGameObjectStructure:
                    {
                        stream.GetChangeGameObjectStructureEvent(i, out var e);
                        InvalidateObject(e.entityId);
                    }
                    break;
                    case ObjectChangeKind.ChangeGameObjectParent:
                    {
                        stream.GetChangeGameObjectParentEvent(i, out var e);
                        InvalidateObject(e.entityId);
                    }
                    break;
                    case ObjectChangeKind.ChangeGameObjectOrComponentProperties:
                    {
                        stream.GetChangeGameObjectOrComponentPropertiesEvent(i, out var e);
                        InvalidateObject(e.entityId);
                    }
                    break;
                    case ObjectChangeKind.UpdatePrefabInstances:
                    {
                        stream.GetUpdatePrefabInstancesEvent(i, out var e);
                        for (int idIndex = 0; idIndex < e.entityIds.Length; ++idIndex)
                            InvalidateObject(e.entityIds[idIndex]);
                    }
                    break;
                }
            }
        }

        public static void Reset()
        {
            propertyDatabase?.Clear();
            propertyAliases?.Clear();
            s_TransactionManager?.ClearAll();
        }

        private static void InvalidateObject(EntityId entityId)
        {
            var obj = EditorUtility.EntityIdToObject(entityId);
            var documentKey = SearchUtils.GetDocumentKey(obj);
            InvalidateDocument(documentKey);
        }

        private static void InvalidateDocument(string documentId)
        {
            InvalidateDocument(documentId.GetHashCode64());
        }

        private static void InvalidateDocument(ulong documentKey)
        {
            s_DocumentsToInvalidate.Add(documentKey);
            s_DelayedInvalidate?.Execute();
        }

        private static void InvalidateDocuments()
        {
            using (var view = GetView(true))
            {
                foreach (var k in s_DocumentsToInvalidate)
                    view.InvalidateDocument(k);
            }
            s_DocumentsToInvalidate.Clear();
            documentsInvalidated?.Invoke();
        }

        [System.Diagnostics.Conditional("DEBUG_SEARCH_MONITOR")]
        public static void Log(string message, ulong documentKey = 0UL)
        {
            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, $"{message}: <b>{documentKey}</b>");
        }

        [System.Diagnostics.Conditional("DEBUG_SEARCH_MONITOR")]
        internal static void Log(string message, PropertyDatabaseRecordKey recordKey, object value = null)
        {
            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, $"{message}: <b>{recordKey.documentKey}</b>, {recordKey.propertyKey}={value}");
        }

    }

    static class AssetDatabaseAPI
    {
        public static bool IsAssetImportWorkerProcess()
        {
            return AssetDatabase.IsAssetImportWorkerProcess();
        }

        public static void RegisterCustomDependency(string name, Hash128 hash)
        {
            AssetDatabase.RegisterCustomDependency(name, hash);
        }
    }
}
