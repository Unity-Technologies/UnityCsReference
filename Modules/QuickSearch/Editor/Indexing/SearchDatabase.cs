// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define DEBUG_INDEXING
//#define DEBUG_LOG_CHANGES
//#define DEBUG_RESOLVING
//#define INCREMENTAL_UPDATE_REPORT

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor.Profiling;
using UnityEngine;

namespace UnityEditor.Search
{
    using Task = SearchTask<TaskData>;

    class TaskData
    {
        public TaskData(byte[] bytes, SearchIndexer combinedIndex, object userData = null)
        {
            this.bytes = bytes;
            this.combinedIndex = combinedIndex;
            this.userData = userData;
        }

        public readonly byte[] bytes;
        public readonly SearchIndexer combinedIndex;
        public readonly object userData;
    }

    public interface ISearchDatabase
    {
        string name { get; }
        IReadOnlyCollection<string> roots { get; }
        IReadOnlyCollection<string> includePatterns { get; }
        IReadOnlyCollection<string> excludePatterns { get; }
        IndexingOptions indexingOptions { get; }
    }

    [ExcludeFromPreset]
    class SearchDatabase : ScriptableObject, ITaskReporter, ISearchDatabase
    {
        // 1- First version
        // 2- Rename ADBIndex for SearchDatabase
        // 3- Add db name and type
        // 4- Add better ref: property indexing
        // 5- Fix asset has= property indexing.
        // 6- Use produce artifacts async
        // 7- Update how keywords are encoded
        // 8- Update t:prefab
        // 9- Change default storage to LMDB
        public const int version = (9 << 8) ^ SearchIndexArtifactImporter.Version;
        private const string k_QuickSearchLibraryPath = "Library/Search";
        public const string defaultSearchDatabaseIndexPath = "UserSettings/Search.index";
        static readonly TimeSpan k_DefaultProductionTimeLimitPerFrame = TimeSpan.FromMilliseconds(16);

        public enum IndexType
        {
            asset,
            scene,
            prefab
        }

        public enum IndexLocation
        {
            all,
            assets,
            packages
        }

        public enum LoadState
        {
            Loading,
            Canceled,
            Error,
            Complete
        }

        [Flags]
        enum ChangeStatus
        {
            Deleted = 1 << 1,
            Missing = 1 << 2,
            Changed = 1 << 3,

            All = Deleted | Missing | Changed,
            Any = All
        }

        [Serializable]
        public class Options
        {
            public bool disabled = false;           // Disables the index

            public bool types = true;               // Index type information about objects
            public bool properties = false;         // Index serialized properties of objects
            public bool extended = false;           // Index as many properties as possible (i.e. asset import settings)
            public bool dependencies = false;       // Index object dependencies (i.e. ref:<name>)

            public override int GetHashCode()
            {
                return GetHashCode(types, properties, extended, dependencies);
            }

            internal static int GetHashCode(bool types, bool properties, bool extended, bool dependencies)
            {
                return (types ? (int)IndexingOptions.Types : 0) |
                       (properties ? (int)IndexingOptions.Properties : 0) |
                       (extended ? (int)IndexingOptions.Extended : 0) |
                       (dependencies ? (int)IndexingOptions.Dependencies : 0);
            }
        }

        [Serializable]
        public class Settings
        {
            [NonSerialized] public string root;
            [NonSerialized] public string source;
            [NonSerialized] public string guid;

            public string name;
            public string type = nameof(IndexType.asset);
            public string[] roots;
            public string[] includes;
            public string[] excludes;

            public Options options;
            public int baseScore = 100;

            public static bool IsPackages(string path)
            {
                return !string.IsNullOrEmpty(path) && (path.Equals("Packages", StringComparison.InvariantCultureIgnoreCase) || path.Equals("Packages/", StringComparison.InvariantCultureIgnoreCase));
            }

            public bool IsPackagesIndexingEnabled()
            {
                return roots != null && Array.Exists(roots, r => IsPackages(r));
            }

            public void EnablePackagesIndexing(bool enable)
            {
                if (IsPackagesIndexingEnabled() == enable)
                    return;

                if (enable)
                {
                    var newRoots = new string[roots.Length + 1];
                    Array.Copy(roots, newRoots, roots.Length);
                    newRoots[newRoots.Length - 1] = "Packages";
                    roots = newRoots;
                }
                else
                {
                    var newRoots = new string[roots.Length - 1];
                    var newRootIndex = 0;
                    foreach (var root in roots)
                    {
                        if (IsPackages(root))
                            continue;
                        newRoots[newRootIndex++] = root;
                    }
                    roots = newRoots;
                }
            }
        }

        public new string name
        {
            get
            {
                if (settings == null || string.IsNullOrEmpty(settings.name))
                    return base.name;
                return settings.name;
            }
            set
            {
                base.name = value;
            }
        }

        public IReadOnlyCollection<string> roots => settings.roots;
        public IReadOnlyCollection<string> includePatterns => settings.includes;
        public IReadOnlyCollection<string> excludePatterns => settings.excludes;
        public IndexingOptions indexingOptions => (IndexingOptions)settings.options.GetHashCode();

        [SerializeField] public Settings settings;
        public long indexSize { get; private set; }

        [NonSerialized] private EntityId m_EntityId = EntityId.None;
        [NonSerialized] private Task m_CurrentResolveTask;
        [NonSerialized] private Task m_CurrentUpdateTask;
        [NonSerialized] private Task m_CurrentIncrementalLoadTask;
        [NonSerialized] private int m_UpdateTasks = 0;
        [NonSerialized] private string m_IndexSettingsPath;
        [NonSerialized] private ConcurrentBag<AssetIndexChangeSet> m_UpdateQueue = new ConcurrentBag<AssetIndexChangeSet>();

        private int m_IndexingRequestTrackerId;
        private int m_IncrementalIndexingRequestTrackerId;

        public ObjectIndexer index { get; internal set; }
        public bool loaded { get; private set; }
        public bool ready => this && loaded && index != null && index.IsReady() && LoadingState == LoadState.Complete;
        public bool updating => m_UpdateTasks > 0 || !loaded || m_CurrentResolveTask != null || !ready;
        internal bool hasPendingTasks => m_CurrentResolveTask != null || m_CurrentUpdateTask != null || m_CurrentIncrementalLoadTask != null;

        // TODO: It might be possible that m_UpdateQueue.Count == 0 while an incrementalUpdate task is still in the Dispatcher.
        public bool needsUpdate => !m_UpdateQueue.IsEmpty;
        public string path
        {
            get
            {
                if (m_IndexSettingsPath != null)
                    return m_IndexSettingsPath;
                var adbPath = AssetDatabase.GetAssetPath(this);
                if (!string.IsNullOrEmpty(adbPath))
                    return adbPath;
                return null;
            }
        }

        public LoadState LoadingState { get; private set; } = LoadState.Loading;

        internal enum AfterPlayModeUpdate
        {
            None,
            Incremental = 1 << 1,
            Build = 1 << 2
        }

        internal AfterPlayModeUpdate afterPlayModeUpdate { get; private set; }

        internal static event Action<SearchDatabase> indexLoaded;
        internal static List<SearchDatabase> s_DBs;

        private static SearchDatabase Create(string settingsPath)
        {
            var db = CreateInstance<SearchDatabase>();
            db.hideFlags |= HideFlags.DontUnloadUnusedAsset | HideFlags.DontSaveInEditor;
            return db.Reload(settingsPath);
        }

        public static SearchDatabase Create(Settings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            var db = CreateInstance<SearchDatabase>();
            db.hideFlags |= HideFlags.DontUnloadUnusedAsset | HideFlags.DontSaveInEditor;
            return db.Reload(settings);
        }

        public SearchDatabase Reload(string settingsPath)
        {
            return Reload(LoadSettings(settingsPath));
        }

        public SearchDatabase Reload(Settings settings)
        {
            if (ReloadWithoutIndexing(settings))
                LoadAsync();
            return this;
        }

        public bool ReloadWithoutIndexing(Settings settings)
        {
            m_IndexSettingsPath = settings.source;
            SearchMonitor.RaiseContentRefreshed(new[] { settings.source }, Array.Empty<string>(), Array.Empty<string>());

            LoadingState = LoadState.Loading;
            loaded = false;

            m_IndexingRequestTrackerId = EditorPerformanceTracker.StartTracker("SearchImporter.IndexingRequest");

            this.settings = settings;
            DisposeIndex();
            index = CreateIndexer(settings);
            name = settings.name;

            return true;
        }

        private static string GetDbGuid(string settingsPath)
        {
            var guid = AssetDatabase.AssetPathToGUID(settingsPath);
            if (string.IsNullOrEmpty(guid))
                return Hash128.Compute(settingsPath).ToString();
            return guid;
        }

        public static IEnumerable<SearchDatabase> Enumerate(params string[] types)
        {
            return Enumerate(IndexLocation.all, types);
        }

        public static IEnumerable<SearchDatabase> Enumerate(IndexLocation location, params string[] types)
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return EnumerateAll().Where(db =>
#pragma warning restore UA2001
            {
                if (types != null && types.Length > 0 && Array.IndexOf(types, db.settings.type) == -1)
                    return false;

                if (location == IndexLocation.all)
                    return true;
                else if (location == IndexLocation.packages)
                    return !string.IsNullOrEmpty(db.path) && db.path.StartsWith("Packages", StringComparison.OrdinalIgnoreCase);

                return string.IsNullOrEmpty(db.path) || db.path.StartsWith("Assets", StringComparison.OrdinalIgnoreCase);
            });
        }

        public static SearchDatabase GetDefaultSearchDatabase()
        {
            #pragma warning disable UA2010 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return EnumerateAll().First();
#pragma warning restore UA2010
        }

        public static IEnumerable<SearchDatabase> EnumerateAll()
        {
            if (s_DBs == null || s_DBs.Count == 0)
            {
                s_DBs = new List<SearchDatabase>();
                var dbs = Resources.FindObjectsOfTypeAll<SearchDatabase>();
                foreach (var db in dbs)
                {
                    if (db.path == defaultSearchDatabaseIndexPath)
                    {
                        s_DBs.Add(db);
                        break;
                    }
                }

                if (s_DBs.Count == 0)
                {
                    AddDefaultIndexDatabaseIfNeeded(s_DBs, defaultSearchDatabaseIndexPath);
                }
            }

            // Force single index.
            yield return s_DBs[0];
        }

        internal static void AddDefaultIndexDatabaseIfNeeded(List<SearchDatabase> dbs, string defaultSearchDatabasePath)
        {
            if (!HasProjectDB(dbs))
            {
                SetupDefaultIndexFirstUse(defaultSearchDatabasePath);
            }
            if (File.Exists(defaultSearchDatabasePath))
                dbs.Add(Create(defaultSearchDatabasePath));
        }

        internal static bool HasProjectDB(List<SearchDatabase> dbs)
        {
            if (dbs.Count == 0)
                return false;

            foreach (var db in dbs)
            {
                if (db.path != null && db.path.StartsWith("Assets"))
                {
                    return true;
                }
            }
            return false;
        }

        internal static void SetupDefaultIndexFirstUse(string defaultSearchDatabasePath)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (!Utils.IsMainProcess())
                return;

            if (!File.Exists(defaultSearchDatabasePath))
                CreateDefaultIndex(defaultSearchDatabasePath);

            SearchSettings.onBoardingDoNotAskAgain = true;
            SearchSettings.Save();
        }

        internal static bool SupportsExtendedIndexing(string assetPath)
        {
            return assetPath.EndsWith(".prefab") || assetPath.EndsWith(".unity");
        }

        internal static int GetImporterHashCode(Settings settings, string assetPath)
        {
            var importerHashCode = SupportsExtendedIndexing(assetPath)
                ? settings.options.GetHashCode()
                // Note: When indexing asset that is NOT a scene or prefabs, be sure to always index without extended.
                : Options.GetHashCode(settings.options.types, settings.options.properties, extended: false, settings.options.dependencies);
            return importerHashCode;
        }

        internal static Type GetIndexImporterTypeForAsset(Settings settings, string assetPath)
        {
            var importerHashCode = GetImporterHashCode(settings, assetPath);
            return SearchIndexArtifactImporter.GetIndexImporterType(importerHashCode);
        }

        internal static string GetIndexTypeSuffix(Settings settings, string assetPath)
        {
            var importHashCode = assetPath == null ? settings.options.GetHashCode() : GetImporterHashCode(settings, assetPath);
            return SearchIndexArtifactImporter.GetArtifactPathSuffix(importHashCode);
        }

        internal static void ForceRebuildIndex(SearchDatabase db)
        {
            MakeDatabaseDirty(db);
            SearchDatabase.ImportAsset(db.path, true);
        }

        internal static void MakeDatabaseDirty(SearchDatabase db)
        {
            var settings = db.settings;
            MakeIndexImporterDirty(settings.options.types, settings.options.properties, settings.options.extended, settings.options.dependencies);
            if (settings.options.extended)
            {
                MakeIndexImporterDirty(settings.options.types, settings.options.properties, false, settings.options.dependencies);
            }

            AssetDatabase.Refresh();
        }

        internal static void MakeIndexImporterDirty(bool types, bool properties, bool extended, bool dependencies)
        {
            var indexImporterType = SearchIndexArtifactImporter.GetIndexImporterType(Options.GetHashCode(types, properties, extended, dependencies));
            var customDependencyName = SearchIndexArtifactImporter.GetCustomDependencyName(indexImporterType);
            AssetDatabaseAPI.RegisterCustomDependency(customDependencyName, Hash128.Parse(Guid.NewGuid().ToString("N")));
        }

        private static IEnumerable<string> FilterIndexes(IEnumerable<string> paths)
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return paths.Where(u => u == defaultSearchDatabaseIndexPath);
#pragma warning restore UA2001
        }

        public static Settings LoadSettings(string settingsPath)
        {
            var settingsJSON = File.ReadAllText(settingsPath);
            Settings indexSettings = null;
            try
            {
                indexSettings = JsonUtility.FromJson<Settings>(settingsJSON);
            }
            finally
            {
                if (indexSettings == null)
                {
                    indexSettings = JsonUtility.FromJson<Settings>(SearchDatabaseTemplates.@default);
                }
            }

            indexSettings.source = settingsPath;
            indexSettings.guid = GetDbGuid(settingsPath);
            indexSettings.root = Path.GetDirectoryName(settingsPath).Replace("\\", "/");
            if (String.IsNullOrEmpty(indexSettings.name))
                indexSettings.name = Path.GetFileNameWithoutExtension(settingsPath);

            return indexSettings;
        }

        internal static ObjectIndexer CreateIndexer(Settings settings, string settingsPath = null)
        {
            return CreateIndexer(settings, settingsPath, new LMDBIndexStorage(GetBackupIndexPath(true, settings, settingsPath)));
        }

        public static ObjectIndexer CreateIndexer(Settings settings, string settingsPath, ISearchIndexerStorage storage)
        {
            FixSettings(settings, settingsPath);
            return new AssetIndexer(settings, storage);
        }

        public void Import(string settingsPath)
        {
            settings = LoadSettings(settingsPath);
            DisposeIndex();
            DeleteBackupIndex();
            index = CreateIndexer(settings, null, new LMDBIndexStorage(GetBackupIndexPath(true)));
            name = settings.name;
        }

        private static SearchDatabase Find(string path)
        {
            if (s_DBs == null)
                return null;
            #pragma warning disable UA2001, UA2011 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return s_DBs.Where(db => string.Equals(db.path, path, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
#pragma warning restore UA2001, UA2011
        }

        public static SearchDatabase ImportAsset(string settingsPath, bool forceUpdate = false)
        {
            if (settingsPath != defaultSearchDatabaseIndexPath)
                return null;

            var currentDb = Find(settingsPath);
            if (currentDb)
            {
                currentDb.DisposeIndex();
                currentDb.DeleteBackupIndex();
            }

            AssetDatabase.ImportAsset(settingsPath, forceUpdate ? ImportAssetOptions.ForceUpdate : ImportAssetOptions.Default);
            if (currentDb)
                currentDb.Reload(settingsPath);
            else
            {
                currentDb = AssetDatabase.LoadAssetAtPath<SearchDatabase>(settingsPath);
                if (!currentDb)
                    currentDb = Create(settingsPath);
                s_DBs.Add(currentDb);
            }

            return currentDb;
        }

        public static void Unload(SearchDatabase db)
        {
            s_DBs?.Remove(db);
            if (EditorUtility.IsPersistent(db))
                Resources.UnloadAsset(db);
            else
                DestroyImmediate(db);
        }

        internal void OnEnable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            m_EntityId = GetEntityId();

            Log("OnEnable");

            if (settings == null)
            {
                LoadingState = LoadState.Error;
                return;
            }
            // In the off chance we call OnEnable on an existing SearchDatabase.
            DisposeIndex();
            index = CreateIndexer(settings, path);

            if (settings.source == null)
            {
                LoadingState = LoadState.Error;
                return;
            }

            // Set default loading state
            LoadingState = LoadState.Loading;

            Dispatcher.Enqueue(LoadAsync);
        }

        internal void OnDisable()
        {
            Log("OnDisable");
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            SearchMonitor.contentRefreshed -= OnContentRefreshed;
            DisposeIndex();
        }

        internal void DisposeIndex()
        {
            // We need to cancel all pending tasks that might be using the index before we can dispose it.
            CancelAllPendingTasksImmediately();
            index?.Dispose();
        }

        internal void CancelAllPendingTasksImmediately()
        {
            m_CurrentResolveTask?.Dispose();
            m_CurrentResolveTask = null;
            m_CurrentIncrementalLoadTask?.Dispose();
            m_CurrentIncrementalLoadTask = null;
            m_CurrentUpdateTask?.Dispose();
            m_CurrentUpdateTask = null;
        }

        private void LoadAsync()
        {
            if (!this)
            {
                LoadingState = LoadState.Error;
                return;
            }

            var backupIndexPath = GetBackupIndexPath(false);
            if (!IsFullBuildNeeded())
                IncrementalLoad(backupIndexPath);
            else
                Build();
        }

        bool IsFullBuildNeeded()
        {
            if (index.storage is LMDBIndexStorage lmdbStorage)
            {
                // Timestamp is set to 0 by default, so no indexing was done yet. Full build needed.
                if (lmdbStorage.Timestamp == 0)
                    return true;

                // If the version is not the default one, we need to rebuild the index.
                if (lmdbStorage.Version != LMDBIndexStorage.DefaultVersion)
                    return true;
            }

            return false;
        }

        [System.Diagnostics.Conditional("DEBUG_INDEXING")]
        private void Log(string callName, params string[] args)
        {
            Log(LogType.Log, callName, args);
        }

        [System.Diagnostics.Conditional("DEBUG_INDEXING")]
        private void Log(LogType logType, string callName, params string[] args)
        {
            if (!this || index == null || index.settings.options.disabled)
                return;
            var status = "";
            if (ready) status += "R";
            if (index != null && index.IsReady()) status += "I";
            if (loaded) status += "L";
            if (updating) status += "~";
            Debug.LogFormat(logType, LogOption.None, this, $"({m_EntityId}, {status}) <b>{settings.name}</b>.<b>{callName}</b> {string.Join(", ", args)}" +
                $" {index?.documentCount ?? 0} documents, {index?.indexCount ?? 0} elements");
        }

        public void Report(string status, params string[] args)
        {
            Log(status, args);
        }

        internal void IncrementalLoad(string indexPath)
        {
            LoadingState = LoadState.Loading;
            loaded = false;
            var deletedAssets = new HashSet<string>();
            m_CurrentIncrementalLoadTask?.Dispose();
            m_CurrentIncrementalLoadTask = new Task("Read", $"Loading {name.ToLowerInvariant()} search index", OnIncrementalLoadFinished, this);
            m_CurrentIncrementalLoadTask.RunThread(() =>
            {
                m_CurrentIncrementalLoadTask.Report($"Loading {indexPath}...", -1);

                foreach (var d in index.GetDocuments())
                {
                    if (d.valid && (!File.Exists(d.source) && !Directory.Exists(d.source)))
                        deletedAssets.Add(d.source);
                }
            }, finalize: () => IncrementLoadBytes(m_CurrentIncrementalLoadTask, deletedAssets));
        }

        private void IncrementLoadBytes(Task loadTask, HashSet<string> deletedAssets)
        {
            if (!this)
            {
                loadTask.Resolve(null);
                return;
            }

            loadTask.Report("Checking for changes...", -1);
            var diff = SearchMonitor.GetDiff(index.timestamp, deletedAssets, path => KeepChangesetPredicate(path, index));
            if (!diff.empty)
                IncrementalUpdate(diff);

            loadTask.Resolve(new TaskData(null, index, diff.empty));
        }

        // This method should be run in a thread.
        void MergeProducedArtifacts(SearchIndexArtifactImportContext importContext, Task mergeTask, EventWaitHandle doneHandle, EventWaitHandle artifactImportedHandle, int baseScore)
        {
            if (mergeTask.Canceled() || index == null)
                return;

            if (index.storage is not LMDBIndexStorage lmdbStorage)
                return;

            mergeTask.Report("Merging artifacts...", 0, importContext.TotalCount);
            mergeTask.total = importContext.TotalCount;

            var artifactsMerged = 0;
            var lastImportedCount = 0;
            while (!doneHandle.WaitOne(TimeSpan.Zero) || artifactsMerged < importContext.ImportedCount)
            {
                artifactImportedHandle.WaitOne();
                if (mergeTask.Canceled())
                    return;

                // We do not need to protect access to ImportedCount here. The production task only ever increments it.
                // Which means that at any given time when we check this value, it is either the same or higher than the last time we checked it.
                // So if we read this value only once per loop, we are guaranteed to never miss any produced artifacts.
                var currentImportedCount = importContext.ImportedCount;
                if (currentImportedCount > lastImportedCount)
                {
                    var batch = importContext.ArtifactImportData.AsBatch(lastImportedCount, currentImportedCount - lastImportedCount);
                    lmdbStorage.MergeArtifactImportDataBatch(Array.Empty<string>(), in batch, baseScore, 10, mergeTask);
                    artifactsMerged += batch.Length;
                    lastImportedCount = currentImportedCount;
                }
            }

            if (mergeTask.Canceled())
                return;
            lmdbStorage.Finish(Array.Empty<string>());
        }

        void ProduceArtifactsAndMerge(SearchIndexArtifactImportContext importContext, Task parentTask, int baseScore, Action<Task> onBeforeMerge)
        {
            if (parentTask.Canceled())
                return;

            var productionTaskDoneWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
            var artifactsImportedHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

            // Merge task. Merges produced artifacts as they are produced by the production task.
            // Will only terminate once the production task notifies it is done and all artifacts have been merged.
            // Only this task resolves the parent task.
            // Create the merge task first, since the resolving of the task on cancel is done in LIFO order. We want the
            // Production task to resolve first in that case.
            var mergeTask = parentTask.CreateChildTask("Merge", "Merging artifacts", (currentTask, data) =>
            {
                // Resolve parent task when the merge task is done unless cancelled.
                if (!parentTask.Canceled())
                    parentTask.Resolve(new TaskData(null, index));

                // Dispose of the wait handles in the resolver to make sure that it is always done no matter what happens (i.e. completion, cancellation, etc.).
                productionTaskDoneWaitHandle.Dispose();
                artifactsImportedHandle.Dispose();

            }, this);
            mergeTask.SetProgressPriority(Progress.Priority.Low); // Set a lower priority than the production task to display it after.

            // Production task. Produces artifacts in the main thread, but is time sliced. When done it notifies the merge task.
            var productionTask = parentTask.CreateChildTask("Produce", "Producing artifacts", (currentTask, data) =>
            {
                // Do this in the resolver so that if anything fails it is still called and the merge task can terminate.
                productionTaskDoneWaitHandle.Set();
                artifactsImportedHandle.Set();
            }, this);
            productionTask.SetProgressPriority(Progress.Priority.Normal); // Set a higher priority than the merge task to display it first.

            // Start the merge task thread before starting the production task.
            mergeTask.RunThread(routine: () =>
            {
                // If there is any setup to do before merging, do it now.
                onBeforeMerge?.Invoke(mergeTask);
                MergeProducedArtifacts(importContext, mergeTask, productionTaskDoneWaitHandle, artifactsImportedHandle, baseScore);
            }, () =>
            {
                // Resolve merge task when thread is done
                mergeTask.Resolve(new TaskData(null, null));
            });

            // Setup artifact imported callback
            void OnArtifactsImported(SearchIndexArtifactImportContext context, Task _, in SearchIndexArtifactImportData.Batch importedBatch)
            {
                // Notify the merge task that new artifacts are available to merge.
                artifactsImportedHandle.Set();
            }

            // Start the production task
            SearchIndexArtifactGeneration.ProduceArtifacts(importContext, productionTask, OnArtifactsImported, (context, currentTask) =>
            {
                // Resolve the production task, this will notify the merge task that production is done.
                currentTask.Resolve(new TaskData(null, null));
            });
        }

        internal void Build()
        {
            if (EditorApplication.isPlaying)
            {
                afterPlayModeUpdate |= AfterPlayModeUpdate.Build;
                return;
            }

            LoadingState = LoadState.Loading;
            m_CurrentResolveTask?.Dispose();
            m_CurrentResolveTask = new Task("Build", $"Building {name.ToLowerInvariant()} search index", OnBuildFinished, 1, this);

            var importContext = new SearchIndexArtifactImportContext(this)
            {
                ArtifactProductionFrameTimeLimit = k_DefaultProductionTimeLimitPerFrame
            };
            m_CurrentResolveTask.userData = importContext;
            SearchIndexArtifactGeneration.GetIndexDependenciesAsync(this, m_CurrentResolveTask, (sources, parentTask) =>
            {
                // Reset progress so that global progress is computed from children
                parentTask.Report("", -2.0f);
                importContext.SetSources(sources);
                ProduceArtifactsAndMerge(importContext, parentTask, 0, (_) =>
                {
                    // Clear existing index
                    index.storage.Clear();
                });
            });
        }

        private void OnBuildFinished(Task task, TaskData data)
        {
            var taskCancelled = task?.Canceled() ?? false;
            m_CurrentResolveTask = null;
            if (!this || taskCancelled || task?.error != null)
            {
                LoadingState = taskCancelled ? LoadState.Canceled : LoadState.Error;
                return;
            }

            indexSize = (long)(index.storage as LMDBIndexStorage).MapSize;
            Setup();
            EmitDatabaseReady(this);
        }

        private void OnIncrementalLoadFinished(Task task, TaskData data)
        {
            var taskCancelled = task?.Canceled() ?? false;
            m_CurrentIncrementalLoadTask = null;
            if (!this || taskCancelled || task?.error != null)
            {
                LoadingState = taskCancelled ? LoadState.Canceled : LoadState.Error;
                if (task?.error != null)
                    Debug.LogException(task.error);
                return;
            }

            if (data == null)
            {
                LoadingState = LoadState.Error;
                return;
            }

            indexSize = (long)(index.storage as LMDBIndexStorage).MapSize;
            Setup();

            if (data.userData is true)
                EmitDatabaseReady(this);
        }

        private void Setup()
        {
            if (!this)
            {
                LoadingState = LoadState.Error;
                return;
            }

            if (m_IndexingRequestTrackerId != 0)
            {
                EditorPerformanceTracker.StopTracker(m_IndexingRequestTrackerId);
            }
            m_IndexingRequestTrackerId = 0;

            // Is it considered loaded if there are any pending updates?
            loaded = true;
            LoadingState = LoadState.Complete;
            Log("Setup");
            indexLoaded?.Invoke(this);
            SearchMonitor.contentRefreshed -= OnContentRefreshed;
            SearchMonitor.contentRefreshed += OnContentRefreshed;
        }

        public string GetBackupIndexPath(bool createDirectory)
        {
            return GetBackupIndexPath(createDirectory, settings, settings.source);
        }

        static string GetBackupIndexPath(bool createDirectory, Settings settings, string settingsPath)
        {
            FixSettings(settings, settingsPath);
            if (createDirectory && !Directory.Exists(k_QuickSearchLibraryPath))
                Directory.CreateDirectory(k_QuickSearchLibraryPath);
            return $"{k_QuickSearchLibraryPath}/{settings.guid}.{nameof(SearchIndexArtifactImporter)}.{SearchIndexArtifactImporter.Version}.{GetIndexTypeSuffix(settings, null)}";
        }

        internal void DeleteBackupIndex()
        {
            try
            {
                // Make sure the index is correctly disposed before we delete the backup index file.
                // With the LMDB storage, an opened index means this file is actively used.
                DisposeIndex();
                var backupIndexPath = GetBackupIndexPath(false);
                if (File.Exists(backupIndexPath))
                    File.Delete(backupIndexPath);
                var lockFile = backupIndexPath + "-lock";
                if (File.Exists(lockFile))
                    File.Delete(lockFile);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Failed to delete backup index.\r\n{ex}");
            }
        }

        private void OnPlayModeChanged(PlayModeStateChange playState)
        {
            if (LoadingState == LoadState.Error)
                return;
            if (playState == PlayModeStateChange.EnteredEditMode && afterPlayModeUpdate != AfterPlayModeUpdate.None)
            {
                if (afterPlayModeUpdate.HasFlag(AfterPlayModeUpdate.Build))
                {
                    Build();
                }
                else if (afterPlayModeUpdate.HasFlag(AfterPlayModeUpdate.Incremental))
                {
                    ProcessIncrementalUpdates();
                }
                afterPlayModeUpdate = AfterPlayModeUpdate.None;
            }
        }

        private void OnContentRefreshed(string[] updated, string[] removed, string[] moved)
        {
            if (!this || settings.options.disabled)
                return;

            var changeset = new AssetIndexChangeSet(updated, removed, moved, p => KeepChangesetPredicate(p, index));
            if (changeset.empty)
                return;
            IncrementalUpdate(changeset);
        }

        internal void SaveSettingsOptions(bool startIndexing)
        {
            var json = JsonUtility.ToJson(settings, true);
            Utils.WriteTextFileToDisk(path, json);
            if (startIndexing)
            {
                SearchDatabase.ImportAsset(path);
            }
        }

        internal void IncrementalUpdate(AssetIndexChangeSet changeset)
        {
            if (!this)
                return;

            m_UpdateQueue.Add(changeset);

            if (EditorApplication.isPlaying)
            {
                afterPlayModeUpdate |= AfterPlayModeUpdate.Incremental;
                return;
            }

            ProcessIncrementalUpdates();
        }

        private void ProcessIncrementalUpdates()
        {
            if (!this)
                return;

            if (ready && !updating)
            {
                if (m_UpdateQueue.TryTake(out var diff))
                    ProcessIncrementalUpdate(diff);
            }
            else
            {
                Dispatcher.Enqueue(ProcessIncrementalUpdates, 0.5d);
            }
        }

        private void StopIncrementalUpdateTracker()
        {
        }

        private int StartIncrementalUpdateTracker()
        {
            return 0;
        }

        internal void ProcessIncrementalUpdate(AssetIndexChangeSet changeset)
        {
            var taskName = $"Updating {settings.name.ToLowerInvariant()} search index";

            m_IncrementalIndexingRequestTrackerId = StartIncrementalUpdateTracker();

            Interlocked.Increment(ref m_UpdateTasks);
            m_CurrentUpdateTask = new Task("Update", taskName, (task, data) =>
            {
                ResolveIncrementalUpdate(task);
            }, this);

            var importContext = new SearchIndexArtifactImportContext(this)
            {
                ArtifactProductionFrameTimeLimit = k_DefaultProductionTimeLimitPerFrame
            };
            m_CurrentUpdateTask.userData = importContext;
            importContext.SetSources(changeset.updated);
            ProduceArtifactsAndMerge(importContext, m_CurrentUpdateTask, settings.baseScore, (mergeTask) =>
            {
                if (index.storage is LMDBIndexStorage lmdbStorage)
                {
                    // Remove deleted artifacts from the index storage
                    using var cancellationToken = new SearchCancellationToken(mergeTask.cancellationToken);
                    lmdbStorage.RemoveDocuments(changeset.removed, cancellationToken);
                }
            });
        }

        private void ResolveIncrementalUpdate(Task task)
        {
            if (task.error != null)
                Debug.LogException(task.error);
            m_CurrentUpdateTask = null;
            Interlocked.Decrement(ref m_UpdateTasks);
            ProcessIncrementalUpdates();
            SearchService.RefreshWindows();
            EmitDatabaseReady(this);
            StopIncrementalUpdateTracker();
        }

        internal static void CreateDefaultIndex()
        {
            CreateDefaultIndex(defaultSearchDatabaseIndexPath);
        }

        internal static void CreateDefaultIndex(string indexPath)
        {
            if (File.Exists(indexPath))
                File.Delete(indexPath);
            var defaultIndexFilename = Path.GetFileNameWithoutExtension(indexPath);
            var defaultIndexFolder = Path.GetDirectoryName(indexPath);
            SearchDatabaseImporter.CreateTemplateIndex(SearchDatabaseTemplates.defaultTemplate, defaultIndexFolder, defaultIndexFilename);
        }

        internal static SearchDatabase CreateDefaultIndexAndImport()
        {
            CreateDefaultIndex(defaultSearchDatabaseIndexPath);
            return ImportAsset(defaultSearchDatabaseIndexPath);
        }

        static bool KeepChangesetPredicate(string path, ObjectIndexer index)
        {
            if (string.IsNullOrEmpty(path) || index == null || index.settings.options.disabled)
                return false;

            if (index.SkipEntry(path, true))
                return false;

            if (!index.TryGetHash(path, out var hash) || !hash.isValid)
                return true;

            return hash != index.GetDocumentHash(path);
        }

        static void EmitDatabaseReady(SearchDatabase db)
        {
            Dispatcher.Emit(SearchEvent.SearchIndexReady, new SearchEventPayload(new HeadlessSearchViewState(), db));
        }

        static void FixSettings(Settings settings, string settingsPath)
        {
            // Fix the settings root if needed
            if (string.IsNullOrEmpty(settingsPath) || settings == null)
                return;

            if (string.IsNullOrEmpty(settings.source))
                settings.source = settingsPath;

            if (string.IsNullOrEmpty(settings.root))
                settings.root = Path.GetDirectoryName(settingsPath).Replace("\\", "/");

            if (string.IsNullOrEmpty(settings.guid))
                settings.guid = GetDbGuid(settingsPath);

            if (settings.type == "prefab" || settings.type == "scene")
                settings.options.extended = true;
        }
    }
}
