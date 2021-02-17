// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define DEBUG_INDEXING
//#define DEBUG_LOG_CHANGES

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor.Experimental;
using UnityEditor.Profiling;
using UnityEngine;

namespace UnityEditor.Search
{
    using Task = SearchTask<TaskData>;

    class TaskData
    {
        public TaskData(byte[] bytes, SearchIndexer combinedIndex)
        {
            this.bytes = bytes;
            this.combinedIndex = combinedIndex;
        }

        public readonly byte[] bytes;
        public readonly SearchIndexer combinedIndex;
    }

    [ExcludeFromPreset]
    class SearchDatabase : ScriptableObject, ITaskReporter
    {
        // 1- First version
        // 2- Rename ADBIndex for SearchDatabase
        // 3- Add db name and type
        // 4- Add better ref: property indexing
        // 5- Fix asset has= property indexing.
        // 6- Use produce artifacts async
        // 7- Update how keywords are encoded
        public const int version = (7 << 8) ^ SearchIndexEntryImporter.version;
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

        enum ImportMode
        {
            Synchronous,
            Asynchronous,
            Query
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
        public class Options // TODO: replace this class with a enum flags IndexingOptions
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

        [System.Diagnostics.DebuggerDisplay("{guid} > {path}")]
        class IndexArtifact
        {
            public IndexArtifact(string source, GUID guid)
            {
                this.source = source;
                this.guid = guid;
                key = default;
                path = null;
            }

            public bool valid => key.isValid;

            public readonly string source;
            public readonly GUID guid;
            public Hash128 key;
            public string path;

            public override string ToString()
            {
                return $"{key} / {guid} / {path}";
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

        [SerializeField] public Settings settings;
        [SerializeField, HideInInspector] public byte[] bytes;

        [NonSerialized] private int m_InstanceID = 0;
        [NonSerialized] private Task m_CurrentResolveTask;
        [NonSerialized] private Task m_CurrentUpdateTask;
        [NonSerialized] private static readonly Dictionary<string, Type> IndexerFactory = new Dictionary<string, Type>();
        [NonSerialized] private int m_UpdateTasks = 0;
        [NonSerialized] private string m_IndexSettingsPath;
        [NonSerialized] private ConcurrentBag<AssetIndexChangeSet> m_UpdateQueue = new ConcurrentBag<AssetIndexChangeSet>();

        public ObjectIndexer index { get; internal set; }
        public bool loaded { get; private set; }
        public bool ready => this && loaded && index != null && index.IsReady();
        public bool updating => m_UpdateTasks > 0 || !loaded || m_CurrentResolveTask != null || !ready;
        public string path => m_IndexSettingsPath ?? AssetDatabase.GetAssetPath(this);

        internal static event Action<SearchDatabase> indexLoaded;
        internal static SearchDatabase s_DefaultDB;

        static SearchDatabase()
        {
            IndexerFactory[nameof(IndexType.asset)] = typeof(AssetIndexer);
            IndexerFactory[nameof(IndexType.scene)] = typeof(SceneIndexer);
            IndexerFactory[nameof(IndexType.prefab)] = typeof(SceneIndexer);
        }

        public static SearchDatabase Create(string settingsPath)
        {
            var db = CreateInstance<SearchDatabase>();
            db.hideFlags |= HideFlags.DontUnloadUnusedAsset | HideFlags.DontSaveInEditor;
            return db.Reload(settingsPath);
        }

        public SearchDatabase Reload(string settingsPath)
        {
            m_IndexSettingsPath = settingsPath;
            settings = LoadSettings(settingsPath);
            index = CreateIndexer(settings);
            name = settings.name;
            LoadAsync();
            AssetPostprocessorIndexer.RaiseContentRefreshed(new[] { settingsPath }, new string[0], new string[0]);
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

        public static IEnumerable<string> EnumeratePaths(IndexLocation location, params string[] types)
        {
            string searchDataFindAssetQuery = $"t:SearchDatabase a:{location}";
            return AssetDatabase.FindAssets(searchDataFindAssetQuery).Select(AssetDatabase.GUIDToAssetPath)
                .Concat(location == IndexLocation.all ? new string[] { defaultSearchDatabaseIndexPath } : new string[0])
                .Where(path => IsValidType(path, types))
                .OrderByDescending(p => Path.GetFileNameWithoutExtension(p));
        }

        public static IEnumerable<SearchDatabase> Enumerate(params string[] types)
        {
            return Enumerate(IndexLocation.all, types);
        }

        public static IEnumerable<SearchDatabase> Enumerate(IndexLocation location, params string[] types)
        {
            if (!s_DefaultDB && File.Exists(defaultSearchDatabaseIndexPath))
                s_DefaultDB = Create(defaultSearchDatabaseIndexPath);

            return EnumeratePaths(location, types)
                .Select(path => AssetDatabase.LoadAssetAtPath<SearchDatabase>(path))
                .Concat(new[] { s_DefaultDB })
                .Where(db => db)
                .Select(db => { db.Log("Enumerate"); return db; });
        }

        public static IEnumerable<SearchDatabase> EnumerateAll()
        {
            if (!s_DefaultDB && File.Exists(defaultSearchDatabaseIndexPath))
                s_DefaultDB = Create(defaultSearchDatabaseIndexPath);

            return AssetDatabase.FindAssets("t:SearchDatabase").Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath<SearchDatabase>(path))
                .Concat(new[] { s_DefaultDB })
                .Where(db => db);
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
            }

            if (!IndexerFactory.TryGetValue(settings.type, out var indexerType))
                throw new ArgumentException($"{settings.type} indexer does not exist", nameof(settings.type));
            return (ObjectIndexer)Activator.CreateInstance(indexerType, new object[] {settings});
        }

        public void Import(string settingsPath)
        {
            settings = LoadSettings(settingsPath);
            index = CreateIndexer(settings);
            name = settings.name;
            DeleteBackupIndex();
        }

        public static void ImportAsset(string settingsPath)
        {
            if (s_DefaultDB && s_DefaultDB.path == settingsPath)
            {
                s_DefaultDB.DeleteBackupIndex();
                s_DefaultDB.Reload(settingsPath);
            }
            else
            {
                if (settingsPath == defaultSearchDatabaseIndexPath)
                {
                    s_DefaultDB = Create(settingsPath);
                }
                else
                {
                    AssetDatabase.ImportAsset(settingsPath);
                }
            }
        }

        internal void OnEnable()
        {
            if (settings == null)
                return;

            index = CreateIndexer(settings, path);

            if (settings.source == null)
                return;

            m_InstanceID = GetInstanceID();

            Log("OnEnable");
            if (bytes?.Length > 0)
                Utils.tick += Load;
            else
                Utils.tick += LoadAsync;
        }

        internal void OnDisable()
        {
            Log("OnDisable");
            AssetPostprocessorIndexer.contentRefreshed -= OnContentRefreshed;
            m_CurrentResolveTask?.Dispose();
            m_CurrentResolveTask = null;
            m_CurrentUpdateTask?.Dispose();
            m_CurrentUpdateTask = null;
        }

        private void LoadAsync()
        {
            Utils.tick -= LoadAsync;
            if (!this)
                return;

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
                $" {Utils.FormatBytes(bytes?.Length ?? 0)}, {index?.documentCount ?? 0} documents, {index?.indexCount ?? 0} elements");
        }

        public void Report(string status, params string[] args)
        {
            Log(status, args);
        }

        private void Load()
        {
            Utils.tick -= Load;
            if (!this)
                return;

            var loadTask = new Task("Load", $"Reading {name.ToLowerInvariant()} search index", (task, data) => Setup(), this);
            loadTask.RunThread(() =>
            {
                var step = 0;
                loadTask.Report($"Reading {bytes.Length} bytes...");
                if (!index.LoadBytes(bytes))
                    Debug.LogError($"Failed to load {name} index. Please re-import it.", this);

                loadTask.Report(++step, step + 1);

                Dispatcher.Enqueue(() => loadTask.Resolve(new TaskData(bytes, index)));
            });
        }

        private void IncrementalLoad(string indexPath)
        {
            var indexType = settings.type;
            var loadTask = new Task("Read", $"Loading {name.ToLowerInvariant()} search index", (task, data) => Setup(), this);
            loadTask.RunThread(() =>
            {
                var step = 0;
                loadTask.Report($"Loading {indexPath}...");
                var fileBytes = File.ReadAllBytes(indexPath);

                loadTask.Report(++step, step + 1);
                if (!index.LoadBytes(fileBytes))
                    Debug.LogError($"Failed to load {indexPath}.", this);

                var deletedAssets = new HashSet<string>();
                if (indexType == IndexType.asset.ToString())
                {
                    foreach (var d in index.GetDocuments())
                    {
                        if (d.valid && !File.Exists(d.path))
                            deletedAssets.Add(d.path);
                    }
                }

                bytes = fileBytes;
                loadTask.Report($"Checking for changes...");
                loadTask.Report(++step, step + 1);
                Dispatcher.Enqueue(() =>
                {
                    if (!this)
                        return;
                    var diff = AssetPostprocessorIndexer.GetDiff(index.timestamp, deletedAssets, path =>
                    {
                        if (index.SkipEntry(path, true))
                            return false;

                        if (!index.TryGetHash(path, out var hash) || !hash.isValid)
                            return true;

                        return hash != index.GetDocumentHash(path);
                    });
                    if (!diff.empty)
                        IncrementalUpdate(diff);

                    loadTask.Resolve(new TaskData(fileBytes, index));
                });
            });
        }

        private string GetIndexTypeSuffix()
        {
            return $"{settings.type}.{settings.options.GetHashCode():X}.index".ToLowerInvariant();
        }

        private bool ResolveArtifactPaths(IList<IndexArtifact> artifacts, out List<IndexArtifact> unresolvedArtifacts, Task task, ref int completed)
        {
            task.Report($"Resolving {artifacts.Count} artifacts...");

            var artifactIndexSuffix = "." + GetIndexTypeSuffix();
            var indexImporterType = SearchIndexEntryImporter.GetIndexImporterType(settings.type, settings.options.GetHashCode());

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            unresolvedArtifacts = new List<IndexArtifact>(artifacts.Count / 4);
            foreach (var a in artifacts)
            {
                if (a == null || !string.IsNullOrEmpty(a.path) || a.guid == default)
                {
                    ++completed;
                }
                else if (!a.valid)
                {
                    // After 500 ms of resolving artifacts,
                    // simply mark artifact as unresolved and we will resume later.
                    if (sw.ElapsedMilliseconds > 500)
                    {
                        unresolvedArtifacts.Add(a);
                        continue;
                    }

                    a.key = ProduceArtifact(a.guid, indexImporterType, ImportMode.Asynchronous, out var availableState);
                    if (!a.valid)
                    {
                        if (availableState != OnDemandState.Failed)
                            unresolvedArtifacts.Add(a);
                        else
                            ReportWarning(a, artifactIndexSuffix, availableState.ToString());
                        continue;
                    }

                    if (GetArtifactPaths(a.key, out var paths))
                    {
                        a.path = paths.LastOrDefault(p => p.EndsWith(artifactIndexSuffix, StringComparison.Ordinal));
                        if (a.path == null)
                            ReportWarning(a, artifactIndexSuffix, paths);
                        task.Report(++completed);
                    }
                }
            }

            task.Report(completed);
            return unresolvedArtifacts.Count == 0;
        }

        private void ReportWarning(IndexArtifact a, string artifactIndexSuffix, params string[] paths)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(a.guid);
            Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, this,
                $"Cannot find search index artifact for {assetPath} ({a.guid}{artifactIndexSuffix})\n\t- {string.Join("\n\t- ", paths)}");
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

        private void ProduceArtifacts(Task resolveTask, IList<string> paths)
        {
            using (new EditorPerformanceTracker($"Search.ProduceArtifacts"))
            {
                if (resolveTask?.Canceled() ?? false)
                    return;

                resolveTask.Report("Producing artifacts...");
                resolveTask.total = paths.Count;
                if (resolveTask?.Canceled() ?? false)
                    return;

                ResolveArtifacts(CreateArtifacts(paths), null, resolveTask, true);
            }
        }

        private bool ResolveArtifacts(IndexArtifact[] artifacts, IList<IndexArtifact> partialSet, Task task, bool combineAutoResolve)
        {
            try
            {
                using (new EditorPerformanceTracker("Search.ResolveArtifacts"))
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
                    Utils.CallDelayed(() => ResolveArtifacts(artifacts, remainingArtifacts, task, combineAutoResolve),
                        GetArtifactResolutionCheckDelay(remainingArtifacts.Count));
                }
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

        private byte[] CombineIndexes(Settings settings, IndexArtifact[] artifacts, Task task, bool autoResolve)
        {
            var completed = 0;
            var combineIndexer = new SearchIndexer();
            var indexName = settings.name.ToLowerInvariant();

            task.Report("Combining indexes...");

            combineIndexer.Start();
            foreach (var a in artifacts)
            {
                if (task.Canceled())
                    return null;

                task.Report(completed++);

                if (a == null || a.path == null)
                    continue;

                var si = new SearchIndexer();
                if (!si.ReadIndexFromDisk(a.path))
                    continue;

                if (task.Canceled())
                    return null;

                combineIndexer.CombineIndexes(si, baseScore: settings.baseScore, (di, indexer) => AddIndexNameArea(di, indexer, indexName));
            }

            if (task.Canceled())
                return null;

            task.Report($"Sorting {combineIndexer.indexCount} indexes...");

            if (task.async)
            {
                if (autoResolve)
                {
                    combineIndexer.Finish((bytes) => task.Resolve(new TaskData(bytes, combineIndexer)), null, saveBytes: true);
                }
                else
                {
                    combineIndexer.Finish(() => task.Resolve(new TaskData(null, combineIndexer), completed: false));
                }
            }
            else
            {
                combineIndexer.Finish(removedDocuments: null);
                return combineIndexer.SaveBytes();
            }

            return null;
        }

        private static void AddIndexNameArea(int documentIndex, SearchIndexer indexer, string indexName)
        {
            indexer.AddProperty("a", indexName, indexName.Length, indexName.Length, 0, documentIndex, saveKeyword: true, exact: true);
        }

        private void Build()
        {
            m_CurrentResolveTask?.Cancel();
            m_CurrentResolveTask?.Dispose();
            m_CurrentResolveTask = ResolveArtifacts("Build", $"Building {name.ToLowerInvariant()} search index", OnArtifactsResolved);
        }

        private void OnArtifactsResolved(Task task, TaskData data)
        {
            m_CurrentResolveTask = null;
            if (task.canceled || task.error != null)
                return;
            index.ApplyFrom(data.combinedIndex);
            bytes = data.bytes;
            // Do not cache indexes while running tests
            if (!Utils.IsRunningTests())
                SaveIndex(data.bytes, Setup);
            else
                Setup();
        }

        private void Setup()
        {
            if (!this)
                return;

            loaded = true;
            Log("Setup");
            indexLoaded?.Invoke(this);
            AssetPostprocessorIndexer.contentRefreshed -= OnContentRefreshed;
            AssetPostprocessorIndexer.contentRefreshed += OnContentRefreshed;
        }

        private string GetBackupIndexPath(bool createDirectory)
        {
            if (createDirectory && !Directory.Exists(k_QuickSearchLibraryPath))
                Directory.CreateDirectory(k_QuickSearchLibraryPath);
            return $"{k_QuickSearchLibraryPath}/{settings.guid}.{GetIndexTypeSuffix()}";
        }

        private static void SaveIndex(string backupIndexPath, byte[] saveBytes, Task saveTask = null)
        {
            try
            {
                saveTask?.Report("Saving search index");
                var tempSave = Path.GetTempFileName();
                File.WriteAllBytes(tempSave, saveBytes);

                try
                {
                    if (File.Exists(backupIndexPath))
                        File.Delete(backupIndexPath);
                }
                catch (IOException)
                {
                    // ignore file index persistence operation, since it is not critical and will redone later.
                }

                File.Move(tempSave, backupIndexPath);
            }
            catch (IOException ex)
            {
                Debug.LogException(ex);
            }
        }

        private void SaveIndex(byte[] saveBytes, Action savedCallback = null)
        {
            string savePath = GetBackupIndexPath(createDirectory: true);
            var saveTask = new Task("Save", $"Saving {settings.name.ToLowerInvariant()} search index", (task, data) => savedCallback?.Invoke(), this);
            saveTask.RunThread(() =>
            {
                SaveIndex(savePath, saveBytes, saveTask);
                Dispatcher.Enqueue(() => saveTask.Resolve(new TaskData(saveBytes, index)));
            });
        }

        private void DeleteBackupIndex()
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

        private void OnContentRefreshed(string[] updated, string[] removed, string[] moved)
        {
            if (!this || settings.options.disabled || updating)
                return;
            var changeset = new AssetIndexChangeSet(updated, removed, moved, p => !index.SkipEntry(p, true));
            if (changeset.empty)
                return;
            IncrementalUpdate(changeset);
        }

        private void IncrementalUpdate(AssetIndexChangeSet changeset)
        {
            if (!this)
                return;

            m_UpdateQueue.Add(changeset);
            ProcessIncrementalUpdates();
        }

        private void ProcessIncrementalUpdates()
        {
            if (ready && !updating)
            {
                if (m_UpdateQueue.TryTake(out var diff))
                    ProcessIncrementalUpdate(diff);
            }
            else
            {
                Utils.CallDelayed(ProcessIncrementalUpdates, 0.5);
            }
        }

        private void ProcessIncrementalUpdate(AssetIndexChangeSet changeset)
        {
            using (new EditorPerformanceTracker("Search.IncrementalUpdate"))
            {
                var updates = CreateArtifacts(changeset.updated);
                var taskName = $"Updating {settings.name.ToLowerInvariant()} search index";

                Interlocked.Increment(ref m_UpdateTasks);
                m_CurrentUpdateTask = new Task("Update", taskName, (task, data) => MergeDocuments(task, data, changeset), updates.Length, this);
                ResolveArtifacts(updates, null, m_CurrentUpdateTask, false);
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
                index.Merge(changeset.removed, data.combinedIndex, baseScore,
                    (di, indexer, count) => OnDocumentMerged(task, indexer, indexName, di, count));
                if (saveIndexCache)
                    SaveIndex(savePath, index.SaveBytes(), task);
            }, () => ResolveIncrementalUpdate(task));
        }

        private static void OnDocumentMerged(Task task, SearchIndexer indexer, string indexName, int documentIndex, int documentCount)
        {
            AddIndexNameArea(documentIndex, indexer, indexName);
            task.Report(documentIndex + 1, documentCount);
        }

        private void ResolveIncrementalUpdate(Task task)
        {
            if (task.error != null)
                Debug.LogException(task.error);
            m_CurrentUpdateTask?.Dispose();
            m_CurrentUpdateTask = null;
            Interlocked.Decrement(ref m_UpdateTasks);
            ProcessIncrementalUpdates();
        }

        private IndexArtifact[] CreateArtifacts(IList<string> assetPaths)
        {
            var artifacts = new IndexArtifact[assetPaths.Count];
            for (int i = 0; i < assetPaths.Count; ++i)
                artifacts[i] = new IndexArtifact(assetPaths[i], AssetDatabase.GUIDFromAssetPath(assetPaths[i]));
            return artifacts;
        }

        private static Hash128 ProduceArtifact(GUID guid, Type importerType, ImportMode mode, out OnDemandState state)
        {
            var akey = new ArtifactKey(guid, importerType);
            state = AssetDatabaseExperimental.GetOnDemandArtifactProgress(akey).state;

            if (state == OnDemandState.Failed)
                return default;

            if (state == OnDemandState.Available)
                return AssetDatabaseExperimental.LookupArtifact(akey).value;

            if (state == OnDemandState.Unavailable)
            {
                switch (mode)
                {
                    case ImportMode.Synchronous: return AssetDatabaseExperimental.ProduceArtifact(akey).value;
                    case ImportMode.Asynchronous: return AssetDatabaseExperimental.ProduceArtifactAsync(akey).value;
                }
            }

            return default;
        }

        private static bool GetArtifactPaths(Hash128 artifactHash, out string[] paths)
        {
            return AssetDatabaseExperimental.GetArtifactPaths(new ArtifactID { value = artifactHash }, out paths);
        }

        internal static SearchDatabase CreateDefaultIndex()
        {
            if (File.Exists(defaultSearchDatabaseIndexPath))
                File.Delete(defaultSearchDatabaseIndexPath);
            var defaultIndexFilename = Path.GetFileNameWithoutExtension(defaultSearchDatabaseIndexPath);
            var defaultIndexFolder = Path.GetDirectoryName(defaultSearchDatabaseIndexPath);
            var defaultDbIndexPath = SearchDatabaseImporter.CreateTemplateIndex("_Default", defaultIndexFolder, defaultIndexFilename);
            ImportAsset(defaultDbIndexPath);
            return s_DefaultDB;
        }
    }
}
