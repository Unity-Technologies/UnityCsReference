// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define DEBUG_INDEXING
//#define DEBUG_LOG_CHANGES
//#define DEBUG_RESOLVING

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor.Experimental;
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
        public const int version = (8 << 8) ^ SearchIndexEntryImporter.version;
        private const string k_QuickSearchLibraryPath = "Library/Search";
        public const string defaultSearchDatabaseIndexPath = "UserSettings/Search.index";

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
                return (types ? (int)IndexingOptions.Types        : 0) |
                    (properties ? (int)IndexingOptions.Properties   : 0) |
                    (extended ? (int)IndexingOptions.Extended     : 0) |
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
        }

        class IndexArtifact : IEquatable<IndexArtifact>
        {
            public readonly string source;
            public readonly ArtifactKey key;
            public ArtifactID value;
            public string path;
            public long timestamp;
            public OnDemandState state;

            public GUID guid => key.guid;
            public Type indexerType => key.importerType;
            public bool valid => key.isValid && value.isValid;
            public bool timeout => TimeSpan.FromTicks(DateTime.UtcNow.Ticks - timestamp).TotalSeconds > 10d;

            public IndexArtifact(in string source, in GUID guid, in Type indexerType)
            {
                this.source = source;
                key = new ArtifactKey(guid, indexerType);
                value = default;
                path = null;
                timestamp = long.MaxValue;
                state = OnDemandState.Unavailable;
            }

            public override string ToString()
            {
                return $"{guid} / {path} / {value}";
            }

            public override int GetHashCode()
            {
                return guid.GetHashCode() ^ value.value.GetHashCode();
            }

            public override bool Equals(object other)
            {
                return other is IndexArtifact l && Equals(l);
            }

            public bool Equals(IndexArtifact other)
            {
                return guid == other.guid && value.value == other.value.value;
            }
        }

        public new string name
        {
            get
            {
                if (string.IsNullOrEmpty(settings.name))
                    return base.name;
                return settings.name;
            }
            set
            {
                base.name = value;
            }
        }

        readonly struct ReadLockScope : IDisposable
        {
            readonly ReaderWriterLockSlim m_Lock;

            public ReadLockScope(ReaderWriterLockSlim _lock)
            {
                m_Lock = _lock;
                m_Lock.EnterReadLock();
            }

            public void Dispose()
            {
                m_Lock.ExitReadLock();
            }
        }

        readonly struct WriteLockScope : IDisposable
        {
            readonly ReaderWriterLockSlim m_Lock;

            public WriteLockScope(ReaderWriterLockSlim _lock)
            {
                m_Lock = _lock;
                m_Lock.EnterWriteLock();
            }

            public void Dispose()
            {
                m_Lock.ExitWriteLock();
            }
        }

        readonly struct TryWriteLockScope : IDisposable
        {
            readonly ReaderWriterLockSlim m_Lock;
            readonly bool m_Locked;
            public bool locked => m_Locked;

            public TryWriteLockScope(ReaderWriterLockSlim _lock)
            {
                m_Lock = _lock;
                m_Locked = m_Lock.TryEnterWriteLock(0);
            }

            public void Dispose()
            {
                if (m_Locked)
                    m_Lock.ExitWriteLock();
            }
        }

        public IReadOnlyCollection<string> roots => settings.roots;
        public IReadOnlyCollection<string> includePatterns => settings.includes;
        public IReadOnlyCollection<string> excludePatterns => settings.excludes;
        public IndexingOptions indexingOptions => (IndexingOptions)settings.options.GetHashCode();

        [SerializeField] public Settings settings;
        public long indexSize { get; private set; }

        [NonSerialized] private int m_ProductionLimit = 99;
        [NonSerialized] private int m_InstanceID = 0;
        [NonSerialized] private Task m_CurrentResolveTask;
        [NonSerialized] private Task m_CurrentUpdateTask;
        [NonSerialized] private int m_UpdateTasks = 0;
        [NonSerialized] private string m_IndexSettingsPath;
        [NonSerialized] private ConcurrentBag<AssetIndexChangeSet> m_UpdateQueue = new ConcurrentBag<AssetIndexChangeSet>();
        [NonSerialized] private ReaderWriterLockSlim m_ImmutableLock = new(LockRecursionPolicy.SupportsRecursion);

        public ObjectIndexer index { get; internal set; }
        public bool loaded { get; private set; }
        public bool ready => this && loaded && index != null && index.IsReady() && LoadingState == LoadState.Complete;
        public bool updating => m_UpdateTasks > 0 || !loaded || m_CurrentResolveTask != null || !ready;
        public bool needsUpdate => !m_UpdateQueue.IsEmpty;
        public string path => m_IndexSettingsPath ?? AssetDatabase.GetAssetPath(this);
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

        public static SearchDatabase Create(string settingsPath)
        {
            var db = CreateInstance<SearchDatabase>();
            db.hideFlags |= HideFlags.DontUnloadUnusedAsset | HideFlags.DontSaveInEditor;
            return db.Reload(settingsPath);
        }

        public SearchDatabase Reload(string settingsPath)
        {
            m_IndexSettingsPath = settingsPath;
            SearchMonitor.RaiseContentRefreshed(new[] { settingsPath }, new string[0], new string[0]);
            return Reload(LoadSettings(settingsPath));
        }

        public SearchDatabase Reload(Settings settings)
        {
            LoadingState = LoadState.Loading;
            loaded = false;

            using var writeLockScope = new TryWriteLockScope(m_ImmutableLock);
            if (!writeLockScope.locked)
            {
                Dispatcher.Enqueue(() => Reload(settings));
                return this;
            }

            this.settings = settings;
            index?.Dispose();
            index = CreateIndexer(settings);
            name = settings.name;
            LoadAsync();
            return this;
        }

        private static string GetDbGuid(string settingsPath)
        {
            var guid = AssetDatabase.AssetPathToGUID(settingsPath);
            if (string.IsNullOrEmpty(guid))
                return Hash128.Compute(settingsPath).ToString();
            return guid;
        }

        private static bool IsValidType(string path, string[] types)
        {
            if (!File.Exists(path))
                return false;
            var settings = LoadSettings(path);
            if (settings.options.disabled)
                return false;
            return types.Length == 0 || types.Contains(settings.type);
        }

        public static IEnumerable<SearchDatabase> Enumerate(params string[] types)
        {
            return Enumerate(IndexLocation.all, types);
        }

        public static IEnumerable<SearchDatabase> Enumerate(IndexLocation location, params string[] types)
        {
            return EnumerateAll().Where(db =>
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

        public static IEnumerable<SearchDatabase> EnumerateAll()
        {
            if (s_DBs == null || s_DBs.Count == 0)
            {
                s_DBs = new List<SearchDatabase>();

                string searchDataFindAssetQuery = $"t:{nameof(SearchDatabase)}";
                var dbPaths = AssetDatabase.FindAssets(searchDataFindAssetQuery).Select(AssetDatabase.GUIDToAssetPath)
                    .OrderByDescending(p => Path.GetFileNameWithoutExtension(p));

                s_DBs.AddRange(dbPaths
                    .Select(path => AssetDatabase.LoadAssetAtPath<SearchDatabase>(path))
                    .Where(db => db));

                AddDefaultIndexDatabaseIfNeeded(s_DBs, defaultSearchDatabaseIndexPath);

                SearchMonitor.contentRefreshed -= TrackAssetIndexChanges;
                SearchMonitor.contentRefreshed += TrackAssetIndexChanges;
            }

            foreach (var g in s_DBs.Where(db => db).GroupBy(db => db.ready).OrderBy(g => !g.Key))
                foreach (var db in g.OrderBy(db => db.settings.baseScore))
                    yield return db;
        }

        internal static void AddDefaultIndexDatabaseIfNeeded(List<SearchDatabase> dbs, string defaultSearchDatabasePath)
        {
            if (dbs.Count == 0)
            {
                SetupDefaultIndexFirstUse(defaultSearchDatabasePath);
            }
            if (File.Exists(defaultSearchDatabasePath))
                dbs.Add(Create(defaultSearchDatabasePath));
        }

        internal static void SetupDefaultIndexFirstUse(string defaultSearchDatabasePath)
        {
            if (SearchSettings.onBoardingDoNotAskAgain || Utils.IsRunningTests())
                return;

            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (!Utils.IsMainProcess())
                return;

            if (!File.Exists(defaultSearchDatabasePath))
                CreateDefaultIndex(defaultSearchDatabasePath);

            SearchSettings.onBoardingDoNotAskAgain = true;
            SearchSettings.Save();
        }

        private static IEnumerable<string> FilterIndexes(IEnumerable<string> paths)
        {
            return paths.Where(u => u.EndsWith(".index", StringComparison.OrdinalIgnoreCase));
        }

        private static void TrackAssetIndexChanges(string[] updated, string[] deleted, string[] moved)
        {
            if (s_DBs == null)
                return;

            bool updateViews = false;
            foreach (var p in FilterIndexes(deleted))
            {
                var db = Find(p);
                if (db)
                {
                    updateViews = s_DBs.Remove(db);
                    Unload(db);
                }
            }

            foreach (var p in FilterIndexes(updated))
            {
                var db = Find(p);
                if (db)
                    continue;
                s_DBs.Add(AssetDatabase.LoadAssetAtPath<SearchDatabase>(p));
                updateViews |= true;
            }

            foreach (var p in FilterIndexes(moved))
            {
                var db = Find(p);
                if (db)
                    continue;
                s_DBs.Add(AssetDatabase.LoadAssetAtPath<SearchDatabase>(p));
                updateViews |= true;
            }

            if (updateViews || s_DBs.RemoveAll(db => !db) > 0)
            {
                EditorApplication.delayCall -= SearchService.RefreshWindows;
                EditorApplication.delayCall += SearchService.RefreshWindows;
                Dispatcher.Emit(SearchEvent.SearchIndexesChanged, new SearchEventPayload(new HeadlessSearchViewState(), updated, deleted, moved));
            }

            Providers.FindProvider.Update(updated, deleted, moved);
        }

        public static Settings LoadSettings(string settingsPath)
        {
            var settingsJSON = File.ReadAllText(settingsPath);
            var indexSettings = JsonUtility.FromJson<Settings>(settingsJSON);

            indexSettings.source = settingsPath;
            indexSettings.guid = GetDbGuid(settingsPath);
            indexSettings.root = Path.GetDirectoryName(settingsPath).Replace("\\", "/");
            if (String.IsNullOrEmpty(indexSettings.name))
                indexSettings.name = Path.GetFileNameWithoutExtension(settingsPath);

            return indexSettings;
        }

        public static ObjectIndexer CreateIndexer(Settings settings, string settingsPath = null)
        {
            // Fix the settings root if needed
            if (!String.IsNullOrEmpty(settingsPath))
            {
                if (String.IsNullOrEmpty(settings.source))
                    settings.source = settingsPath;

                if (String.IsNullOrEmpty(settings.root))
                    settings.root = Path.GetDirectoryName(settingsPath).Replace("\\", "/");

                if (String.IsNullOrEmpty(settings.guid))
                    settings.guid = GetDbGuid(settingsPath);

                if (settings.type == "prefab" || settings.type == "scene")
                    settings.options.extended = true;
            }

            return new AssetIndexer(settings);
        }

        public void Import(string settingsPath)
        {
            settings = LoadSettings(settingsPath);
            index?.Dispose();
            index = CreateIndexer(settings);
            name = settings.name;
            DeleteBackupIndex();
        }

        private static SearchDatabase Find(string path)
        {
            return EnumerateAll().Where(db => string.Equals(db.path, path, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        }

        public static SearchDatabase ImportAsset(string settingsPath, bool forceUpdate = false)
        {
            var currentDb = Find(settingsPath);
            if (currentDb)
                currentDb.DeleteBackupIndex();

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
            if (settings == null)
            {
                LoadingState = LoadState.Error;
                return;
            }

            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;

            // In the off chance we call OnEnable on an existing SearchDatabase.
            index?.Dispose();
            index = CreateIndexer(settings, path);

            if (settings.source == null)
            {
                LoadingState = LoadState.Error;
                return;
            }

            m_InstanceID = GetInstanceID();

            Log("OnEnable");

            Dispatcher.Enqueue(LoadAsync);
        }

        internal void OnDisable()
        {
            Log("OnDisable");
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            SearchMonitor.contentRefreshed -= OnContentRefreshed;
            m_CurrentResolveTask?.Dispose();
            m_CurrentResolveTask = null;
            m_CurrentUpdateTask?.Dispose();
            m_CurrentUpdateTask = null;
            index?.Dispose();
        }

        private void LoadAsync()
        {
            if (!this)
            {
                LoadingState = LoadState.Error;
                return;
            }

            var backupIndexPath = GetBackupIndexPath(false);
            if (File.Exists(backupIndexPath))
                IncrementalLoad(backupIndexPath);
            else
                Build();
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
            Debug.LogFormat(logType, LogOption.None, this, $"({m_InstanceID}, {status}) <b>{settings.name}</b>.<b>{callName}</b> {string.Join(", ", args)}" +
                $" {index?.documentCount ?? 0} documents, {index?.indexCount ?? 0} elements");
        }

        public void Report(string status, params string[] args)
        {
            Log(status, args);
        }

        private void IncrementalLoad(string indexPath)
        {
            LoadingState = LoadState.Loading;
            loaded = false;
            var loadTask = new Task("Read", $"Loading {name.ToLowerInvariant()} search index", (task, data) => WaitForReadComplete(task, data, AssignNewIndexAndSetup), this);
            loadTask.RunThread(() =>
            {
                loadTask.Report($"Loading {indexPath}...", -1);
                var fileBytes = File.ReadAllBytes(indexPath);

                var newIndex = new AssetIndexer(settings);
                IncrementLoadBytes(fileBytes, loadTask, newIndex, $"Failed to load {indexPath}.");
            });
        }

        private void IncrementLoadBytes(byte[] indexerBytes, Task loadTask, ObjectIndexer indexer, string loadFailedMessage)
        {
            if (!indexer.LoadBytes(indexerBytes))
                throw new Exception(loadFailedMessage);

            var deletedAssets = new HashSet<string>();
            foreach (var d in indexer.GetDocuments())
            {
                if (d.valid && (!File.Exists(d.source) && !Directory.Exists(d.source)))
                    deletedAssets.Add(d.source);
            }

            Dispatcher.Enqueue(() =>
            {
                if (!this)
                {
                    loadTask.Resolve(null, completed: true);
                    return;
                }

                loadTask.Report($"Checking for changes...", -1);
                var diff = SearchMonitor.GetDiff(indexer.timestamp, deletedAssets, path => KeepChangesetPredicate(path, indexer));
                if (!diff.empty)
                    IncrementalUpdate(diff);

                loadTask.Resolve(new TaskData(indexerBytes, indexer, diff.empty));
            });
        }

        private string GetIndexTypeSuffix()
        {
            return $"{settings.options.GetHashCode():X}.index".ToLowerInvariant();
        }

        internal ArtifactKey GetAssetArtifactKey(string assetPath)
        {
            var indexImporterType = SearchIndexEntryImporter.GetIndexImporterType(settings.options.GetHashCode());
            var artifact = new IndexArtifact(assetPath, AssetDatabase.GUIDFromAssetPath(assetPath), indexImporterType);
            return artifact.key;
        }

        private bool ResolveArtifactPaths(in IList<IndexArtifact> artifacts, out List<IndexArtifact> unresolvedArtifacts, Task task, ref int completed)
        {
            var inProductionCount = 0;
            var artifactIndexSuffix = "." + GetIndexTypeSuffix();

            task.Report($"Resolving {artifacts.Count} artifacts...");
            unresolvedArtifacts = new List<IndexArtifact>((int)(artifacts.Count / 1.5f));

            var sw = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < artifacts.Count; ++i)
            {
                var a = artifacts[i];
                if (a == null || a.guid.Empty())
                {
                    ++completed;
                    continue;
                }

                // Simply mark artifact as unresolved and we will resume later.
                if (inProductionCount > m_ProductionLimit || sw.ElapsedMilliseconds > 250)
                {
                    unresolvedArtifacts.AddRange(artifacts.Skip(i));
                    break;
                }

                if (!ProduceArtifact(a))
                {
                    ++completed;
                    continue;
                }

                if (!a.valid)
                {
                    inProductionCount++;
                    if (!a.timeout)
                        unresolvedArtifacts.Add(a);
                    else
                    {
                        // Check if the asset is still available (maybe it was deleted since last request)
                        // TODO: What happens if exists and is still importing? I.e. it takes more than 10s to import? We should not
                        // try to produce another artifact in that case.
                        var resolvedPath = AssetDatabase.GUIDToAssetPath(a.guid);
                        if (!string.IsNullOrEmpty(resolvedPath) && File.Exists(resolvedPath))
                        {
                            a.timestamp = long.MaxValue;
                            if (ProduceArtifact(a))
                                unresolvedArtifacts.Add(a);
                        }
                    }
                }
                else if (GetArtifactPaths(a.value, out var paths))
                {
                    a.path = paths.LastOrDefault(p => p.EndsWith(artifactIndexSuffix, StringComparison.Ordinal));
                    if (a.path == null)
                        ReportWarning(a, artifactIndexSuffix, paths);
                }
            }

            var producedCount = artifacts.Count - unresolvedArtifacts.Count;
            if (producedCount >= m_ProductionLimit)
                m_ProductionLimit = (int)(m_ProductionLimit * 1.5f);

            task.Report(completed);
            return unresolvedArtifacts.Count == 0;
        }

        private void ReportWarning(in IndexArtifact a, in string artifactIndexSuffix, params string[] paths)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(a.guid);
            Console.WriteLine($"Cannot find search index artifact for {assetPath} ({a.guid}{artifactIndexSuffix})\n\t- {string.Join("\n\t- ", paths)}");
        }

        private Task ResolveArtifacts(string taskName, string title, Task.ResolveHandler finished)
        {
            var resolveTask = new Task(taskName, title, finished, 1, this);
            List<string> paths = null;
            resolveTask.RunThread(() =>
            {
                resolveTask.Report("Scanning dependencies...");
                paths = index.GetDependencies();
            }, () => ProduceArtifacts(resolveTask, paths));

            return resolveTask;
        }

        private void ProduceArtifacts(Task resolveTask, in IList<string> paths)
        {
            if (paths == null || (resolveTask?.Canceled() ?? false))
                return;

            resolveTask.Report("Producing artifacts...");
            resolveTask.total = paths.Count;
            if (resolveTask?.Canceled() ?? false)
                return;

            // TODO: Can't use combineAutoResolve = true here, because we use WaitForReadComplete which might defer the resolver
            // on another frame, and so the task still needs to be valid at that moment.
            ResolveArtifacts(CreateArtifacts(paths), null, resolveTask, true);
        }

        private bool ResolveArtifacts(IndexArtifact[] artifacts, IList<IndexArtifact> partialSet, Task task, bool combineAutoResolve)
        {
            try
            {
                partialSet = partialSet ?? artifacts;

                if (!this || task.Canceled())
                    return false;

                int completed = artifacts.Length - partialSet.Count;
                if (ResolveArtifactPaths(partialSet, out var remainingArtifacts, task, ref completed))
                {
                    if (task.Canceled())
                        return false;
                    return task.RunThread(() => CombineIndexes(settings, artifacts, task, combineAutoResolve));
                }

                // Resume later with remaining artifacts
                Dispatcher.Enqueue(() => ResolveArtifacts(artifacts, remainingArtifacts, task, combineAutoResolve),
                    GetArtifactResolutionCheckDelay(remainingArtifacts.Count));
            }
            catch (Exception err)
            {
                task.Resolve(err);
            }

            return false;
        }

        private double GetArtifactResolutionCheckDelay(int artifactCount)
        {
            if (UnityEditorInternal.InternalEditorUtility.isHumanControllingUs)
                return Math.Max(0.5, Math.Min(artifactCount / 1000.0, 3.0));
            return 1;
        }

        private static void CombineIndexes(in Settings settings, in IndexArtifact[] artifacts, Task task, bool autoResolve)
        {
            if (task.Canceled())
                return;

            // Combine all search index artifacts into one large binary stream.
            var combineIndexer = new SearchIndexer();
            var indexName = settings.name.ToLowerInvariant();
            task.Report("Loading artifacts...", 0);
            var artifactDbs = EnumerateSearchArtifactsDirect(artifacts, task);

            task.Report("Combining indexes...", -1f);
            task.total = artifacts.Length;

            combineIndexer.Start();
            combineIndexer.CombineIndexes(artifactDbs, settings.baseScore, indexName, task);
            foreach (var searchIndexer in artifactDbs)
            {
                searchIndexer.Dispose();
            }

            if (task.Canceled())
                return;

            task.Report($"Saving index...", -1f);
            byte[] bytes = autoResolve ? combineIndexer.SaveBytes() : null;
            Dispatcher.Enqueue(() => task.Resolve(new TaskData(bytes, combineIndexer), completed: autoResolve));
        }

        static List<SearchIndexer> EnumerateSearchArtifactsDirect(IndexArtifact[] artifacts, Task task)
        {
            var results = new List<SearchIndexer>();
            var total = artifacts.Length;
            for (var i = 0; i < total; ++i)
            {
                var a = artifacts[i];
                if (a == null || a.path == null)
                    continue;

                var si = new SearchIndexer(Path.GetFileName(a.source));
                if (!si.ReadIndexFromDisk(a.path))
                    continue;

                results.Add(si);
                task.Report(i+1, total);
            }


            return results;
        }

        private static void AddIndexNameArea(int documentIndex, SearchIndexer indexer, string indexName)
        {
            indexer.AddProperty("a", indexName, indexName.Length, indexName.Length, 0, documentIndex, saveKeyword: true, exact: true);
        }

        private void Build()
        {
            if (EditorApplication.isPlaying)
            {
                afterPlayModeUpdate |= AfterPlayModeUpdate.Build;
                return;
            }

            LoadingState = LoadState.Loading;
            m_CurrentResolveTask?.Cancel();
            m_CurrentResolveTask?.Dispose();
            m_CurrentResolveTask = ResolveArtifacts("Build", $"Building {name.ToLowerInvariant()} search index", (task, data) => WaitForReadComplete(task, data, OnArtifactsResolved));
        }

        // To be used with WaitForReadComplete.
        private void OnArtifactsResolved(Task task, TaskData data)
        {
            m_CurrentResolveTask = null;
            if (task.canceled || task.error != null)
            {
                LoadingState = task.canceled ? LoadState.Canceled : LoadState.Error;
                return;
            }

            // Do not dispose of data.combinedIndex, since we are using its data into index
            index.ApplyFrom(data.combinedIndex);
            indexSize = data.bytes.Length;
            SaveIndex(data.bytes, Setup);
            EmitDatabaseReady(this);
        }

        // To be used with WaitForReadComplete.
        private void AssignNewIndexAndSetup(Task task, TaskData data)
        {
            if (task.error != null)
            {
                LoadingState = LoadState.Error;
                Debug.LogException(task.error);
                return;
            }

            if (data == null)
            {
                LoadingState = LoadState.Error;
                return;
            }

            // Do not dispose of data.combinedIndex, since we are using its data into index
            index.ApplyFrom(data.combinedIndex);
            indexSize = data.bytes.Length;
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

            // Is it considered loaded if there are any pending updates?
            loaded = true;
            LoadingState = LoadState.Complete;
            Log("Setup");
            indexLoaded?.Invoke(this);
            SearchMonitor.contentRefreshed -= OnContentRefreshed;
            SearchMonitor.contentRefreshed += OnContentRefreshed;
        }

        private string GetBackupIndexPath(bool createDirectory)
        {
            if (createDirectory && !Directory.Exists(k_QuickSearchLibraryPath))
                Directory.CreateDirectory(k_QuickSearchLibraryPath);
            return $"{k_QuickSearchLibraryPath}/{settings.guid}.{SearchIndexEntryImporter.version}.{GetIndexTypeSuffix()}";
        }

        internal void SaveIndex(string backupIndexPath = null)
        {
            backupIndexPath ??= GetBackupIndexPath(true);
            index.SaveIndexToDisk(backupIndexPath);
            indexSize = 0;
            var info = new FileInfo(backupIndexPath);
            if (info.Exists)
            {
                indexSize = info.Length;
            }
        }

        private void SaveIndex(string backupIndexPath, byte[] saveBytes, Task saveTask = null)
        {
            try
            {
                saveTask?.Report("Saving search index");
                var tempSave = Path.GetTempFileName();
                File.WriteAllBytes(tempSave, saveBytes);
                indexSize = saveBytes.Length;

                RetriableOperation<IOException>.Execute(() =>
                {
                    if (File.Exists(backupIndexPath))
                        File.Delete(backupIndexPath);

                    File.Move(tempSave, backupIndexPath);
                });
            }
            catch (IOException ex)
            {
                Debug.LogException(ex);
            }
        }

        internal void SaveIndex(byte[] saveBytes, Action savedCallback = null)
        {
            string savePath = GetBackupIndexPath(createDirectory: true);
            var saveTask = new Task("Save", $"Saving {settings.name.ToLowerInvariant()} search index", (task, data) => savedCallback?.Invoke(), this);
            saveTask.RunThread(() =>
            {
                SaveIndex(savePath, saveBytes, saveTask);
                Dispatcher.Enqueue(() => saveTask.Resolve(new TaskData(saveBytes, index)));
            });
        }

        internal void DeleteBackupIndex()
        {
            try
            {
                var backupIndexPath = GetBackupIndexPath(false);
                if (File.Exists(backupIndexPath))
                    File.Delete(backupIndexPath);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Failed to delete backup index.\r\n{ex}");
            }
        }

        private void OnPlayModeChanged(PlayModeStateChange playState)
        {
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

        private void IncrementalUpdate(AssetIndexChangeSet changeset)
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
            if (ready && !updating && !m_ImmutableLock.IsReadLockHeld)
            {
                if (m_UpdateQueue.TryTake(out var diff))
                    ProcessIncrementalUpdate(diff);
            }
            else
            {
                Dispatcher.Enqueue(ProcessIncrementalUpdates, 0.5d);
            }
        }

        private void ProcessIncrementalUpdate(AssetIndexChangeSet changeset)
        {
            var updates = CreateArtifacts(changeset.updated);
            var taskName = $"Updating {settings.name.ToLowerInvariant()} search index";

            Interlocked.Increment(ref m_UpdateTasks);
            m_CurrentUpdateTask = new Task("Update", taskName, (task, data) =>
            {
                WaitForReadComplete(task, data, (t, d) => MergeDocuments(t, d, changeset));
            }, updates.Length, this);
            ResolveArtifacts(updates, null, m_CurrentUpdateTask, false);
        }

        private void WaitForReadComplete(Task task, TaskData data, Action<Task, TaskData> callback)
        {
            using var tryWriteLockScope = new TryWriteLockScope(m_ImmutableLock);
            if (tryWriteLockScope.locked)
                callback(task, data);
            else
            {
                task.Report("Waiting for read completion...");
                Dispatcher.Enqueue(() => WaitForReadComplete(task, data, callback));
            }
        }

        private void MergeDocuments(Task task, TaskData data, AssetIndexChangeSet changeset)
        {
            if (task.canceled || task.error != null)
            {
                ResolveIncrementalUpdate(task);
                return;
            }

            var baseScore = settings.baseScore;
            var indexName = settings.name.ToLowerInvariant();
            var saveIndexCache = !Utils.IsRunningTests();
            var savePath = GetBackupIndexPath(createDirectory: true);

            task.Report("Merging changes to index...");
            task.RunThread(() =>
            {
                using var writeScope = new WriteLockScope(m_ImmutableLock);

                index.Merge(changeset.removed, data.combinedIndex, baseScore,
                    (di, indexer, count) => OnDocumentMerged(indexer, indexName, di), task);
                data.combinedIndex.Dispose();
                if (saveIndexCache)
                    SaveIndex(savePath);
            }, () => ResolveIncrementalUpdate(task));
        }

        private static void OnDocumentMerged(SearchIndexer indexer, string indexName, int documentIndex)
        {
            if (indexer != null)
                AddIndexNameArea(documentIndex, indexer, indexName);
        }

        private void ResolveIncrementalUpdate(Task task)
        {
            if (task.error != null)
                Debug.LogException(task.error);
            m_CurrentUpdateTask?.Dispose();
            m_CurrentUpdateTask = null;
            Interlocked.Decrement(ref m_UpdateTasks);
            ProcessIncrementalUpdates();
            SearchService.RefreshWindows();
            EmitDatabaseReady(this);
        }

        private IndexArtifact[] CreateArtifacts(in IList<string> assetPaths)
        {
            {
                var artifacts = new IndexArtifact[assetPaths.Count];
                var indexImporterType = SearchIndexEntryImporter.GetIndexImporterType(settings.options.GetHashCode());
                for (int i = 0; i < assetPaths.Count; ++i)
                    artifacts[i] = new IndexArtifact(assetPaths[i], AssetDatabase.GUIDFromAssetPath(assetPaths[i]), indexImporterType);

                return artifacts;
            }
        }

        private static bool ProduceArtifact(IndexArtifact artifact)
        {
            artifact.state = AssetDatabaseExperimental.GetOnDemandArtifactProgress(artifact.key).state;

            if (artifact.state == OnDemandState.Failed)
                return false;

            if (artifact.state == OnDemandState.Available)
            {
                artifact.value = AssetDatabaseExperimental.LookupArtifact(artifact.key);
                return true;
            }

            if (artifact.timestamp == long.MaxValue)
            {
                artifact.timestamp = DateTime.UtcNow.Ticks;
                artifact.value = AssetDatabaseExperimental.ProduceArtifactAsync(artifact.key);
            }
            else
            {
                artifact.value = AssetDatabaseExperimental.LookupArtifact(artifact.key);
            }

            return true;
        }

        private static bool GetArtifactPaths(in ArtifactID id, out string[] paths)
        {
            return AssetDatabaseExperimental.GetArtifactPaths(id, out paths);
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
            SearchDatabaseImporter.CreateTemplateIndex("_Default", defaultIndexFolder, defaultIndexFilename);
        }

        internal static SearchDatabase CreateDefaultIndexAndImport()
        {
            CreateDefaultIndex(defaultSearchDatabaseIndexPath);
            return ImportAsset(defaultSearchDatabaseIndexPath);
        }

        // Don't use on the main thread!
        public IDisposable GetImmutableScope()
        {
            return new ReadLockScope(m_ImmutableLock);
        }

        static bool KeepChangesetPredicate(string path, ObjectIndexer index)
        {
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
    }
}
