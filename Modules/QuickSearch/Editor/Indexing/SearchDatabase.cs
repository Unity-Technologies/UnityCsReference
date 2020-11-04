// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define DEBUG_INDEXING
//#define DEBUG_LOG_CHANGES

using System;
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
        private const string k_QuickSearchLibraryPath = "Library/QuickSearch";
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
            NoImport
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
            public IndexArtifact(string source, string guid)
            {
                this.source = source;
                this.guid = guid;
                key = default;
                path = null;
            }

            public bool valid => key.isValid;

            public readonly string source;
            public readonly string guid;
            public Hash128 key;
            public string path;

            public override string ToString()
            {
                return $"{key} / {guid} / {path}";
            }
        }

        [SerializeField] public Settings settings;
        [SerializeField, HideInInspector] public byte[] bytes;

        [NonSerialized] private int m_InstanceID = 0;
        [NonSerialized] private Task m_CurrentTask;
        [NonSerialized] private static readonly Dictionary<string, Type> IndexerFactory = new Dictionary<string, Type>();
        [NonSerialized] private int m_UpdateTasks = 0;
        [NonSerialized] private string m_IndexSettingsPath;

        public ObjectIndexer index { get; internal set; }
        public bool loaded { get; private set; }
        public bool ready => this && loaded && index != null && index.IsReady();
        public bool updating => m_UpdateTasks > 0 || !loaded || m_CurrentTask != null || !ready;
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
                .Where(db => db != null)
                .Select(db => { db.Log("Enumerate"); return db; });
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
            AssetPostprocessorIndexer.contentRefreshed -= OnContentRefreshed;

            settings = LoadSettings(settingsPath);
            index = CreateIndexer(settings);
            name = settings.name;

            DeleteBackupIndex();

            var paths = index.GetDependencies();
            using (var importTask = new Task("Import", $"Importing {name} index", paths.Count, this))
            {
                var completed = 0;
                var artifacts = ProduceArtifacts(paths);
                if (ResolveArtifactPaths(artifacts, out var _, importTask, ref completed))
                    SaveIndex(CombineIndexes(settings, artifacts, importTask));
            }
        }

        public static void ImportAsset(string settingsPath)
        {
            if (s_DefaultDB && s_DefaultDB.path == settingsPath)
                s_DefaultDB.Reload(settingsPath);
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
            {
                EditorApplication.tick -= Load;
                EditorApplication.tick += Load;
            }
            else
            {
                LoadAsync();
            }
        }

        internal void OnDisable()
        {
            Log("OnDisable");
            AssetPostprocessorIndexer.contentRefreshed -= OnContentRefreshed;
        }

        private void LoadAsync()
        {
            var backupIndexPath = GetBackupIndexPath(false);
            if (File.Exists(backupIndexPath))
            {
                IncrementalLoad(backupIndexPath);
            }
            else
            {
                EditorApplication.tick -= Build;
                EditorApplication.tick += Build;
            }
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
            EditorApplication.tick -= Load;
            AssetPostprocessorIndexer.contentRefreshed -= OnContentRefreshed;

            if (!this)
                return;

            var loadTask = new Task("Load", $"Loading {name} index", (task, data) => Setup(), this);
            loadTask.RunThread(() =>
            {
                var step = 0;
                loadTask.Report($"Loading {bytes.Length} bytes...");
                if (!index.LoadBytes(bytes))
                    Debug.LogError($"Failed to load {name} index. Please re-import it.", this);

                loadTask.Report(++step, step + 1);

                Dispatcher.Enqueue(() => loadTask.Resolve(new TaskData(bytes, index)));
            });
        }

        private void IncrementalLoad(string indexPath)
        {
            var indexType = settings.type;
            var loadTask = new Task("Read", $"Loading {name} index", (task, data) => Setup(), this);
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
                        if (d != null && !File.Exists(d.id))
                            deletedAssets.Add(d.id);
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

            var artifactIndexSuffix = GetIndexTypeSuffix();
            var indexImporterType = SearchIndexEntryImporter.GetIndexImporterType(settings.type, settings.options.GetHashCode());

            unresolvedArtifacts = new List<IndexArtifact>();
            foreach (var a in artifacts)
            {
                if (a == null || !string.IsNullOrEmpty(a.path) || string.IsNullOrWhiteSpace(a.guid))
                {
                    ++completed;
                    continue;
                }

                // Make sure the asset is still valid at this stage, otherwise ignore it
                // It could be a temporary file that was created since import.
                var assetPath = AssetDatabase.GUIDToAssetPath(a.guid);
                var invalidAsset = string.IsNullOrEmpty(assetPath);
                if (invalidAsset)
                {
                    Debug.LogWarning($"Cannot resolve index artifact for {a.guid}");
                    continue;
                }

                if (!a.valid)
                {
                    a.key = ProduceArtifact(new GUID(a.guid), indexImporterType, ImportMode.NoImport, out var availableState);
                    if (!a.valid)
                    {
                        if (availableState == OnDemandState.Unavailable)
                            ProduceArtifact(a.guid, indexImporterType, ImportMode.Asynchronous);
                        unresolvedArtifacts.Add(a);
                        continue;
                    }
                }

                if (GetArtifactPaths(a.key, out var paths))
                {
                    var resultPath = "." + artifactIndexSuffix;
                    a.path = paths.LastOrDefault(p => p.EndsWith(resultPath, StringComparison.Ordinal));
                    if (a.path == null)
                    {
                        Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, this,
                            $"Cannot find index artifact {resultPath} for {assetPath} ({a.guid})\n\t- {String.Join("\n\t- ", paths)}");
                    }
                    ++completed;
                }
            }

            task.Report(++completed);
            return unresolvedArtifacts.Count == 0;
        }

        private IndexArtifact[] InitializeIndexArtifacts(IList<string> paths)
        {
            var artifacts = new IndexArtifact[paths.Count];
            for (int i = 0; i < paths.Count; ++i)
                artifacts[i] = new IndexArtifact(paths[i], AssetDatabase.AssetPathToGUID(paths[i]));
            return artifacts;
        }

        private void ResolveArtifacts(string taskName, string title, Task.ResolveHandler finished)
        {
            m_CurrentTask?.Cancel();
            var resolveTask = new Task(taskName, title, finished, 1, this);

            m_CurrentTask = resolveTask;
            List<string> paths = null;
            resolveTask.RunThread(() =>
            {
                resolveTask.Report("Resolving GUIDs...");
                paths = index.GetDependencies();
            }, () =>
                {
                    if (resolveTask?.Canceled() ?? false)
                        return;

                    resolveTask.Report("Producing artifacts...");
                    resolveTask.total = paths.Count;
                    var artifacts = ProduceArtifacts(paths);
                    if (resolveTask?.Canceled() ?? false)
                        return;

                    ResolveArtifacts(artifacts, null, resolveTask);
                });
        }

        private bool ResolveArtifacts(IndexArtifact[] artifacts, IList<IndexArtifact> partialSet, Task task)
        {
            try
            {
                partialSet = partialSet ?? artifacts;

                if (task.Canceled())
                    return false;

                int completed = artifacts.Length - partialSet.Count;
                if (ResolveArtifactPaths(partialSet, out var remainingArtifacts, task, ref completed))
                {
                    if (task.Canceled())
                        return false;
                    return task.RunThread(() => CombineIndexes(settings, artifacts, task));
                }

                // Retry
                EditorApplication.CallDelayed(() => ResolveArtifacts(artifacts, remainingArtifacts, task), 0.5);
            }
            catch (Exception err)
            {
                task.Resolve(err);
            }

            return false;
        }

        private byte[] CombineIndexes(Settings settings, IndexArtifact[] artifacts, Task task)
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

                combineIndexer.CombineIndexes(si, baseScore: settings.baseScore,
                    (di, indexer) => indexer.AddProperty("a", indexName, di, saveKeyword: true, exact: true));
            }

            if (task.Canceled())
                return null;

            task.Report($"Sorting {combineIndexer.indexCount} indexes...");

            if (task.async)
            {
                combineIndexer.Finish((bytes) => task.Resolve(new TaskData(bytes, combineIndexer)), null, saveBytes: true);
            }
            else
            {
                combineIndexer.Finish(removedDocuments: null);
                return combineIndexer.SaveBytes();
            }

            return null;
        }

        private void Build()
        {
            EditorApplication.tick -= Build;
            AssetPostprocessorIndexer.contentRefreshed -= OnContentRefreshed;

            if (!this)
                return;

            ResolveArtifacts("Build", $"Building {name} index", OnArtifactsResolved);
        }

        private void Setup()
        {
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

        private byte[] SaveIndex(byte[] saveBytes, Action savedCallback = null)
        {
            var saveTask = new Task("Save", $"Saving {settings.name} index", (task, data) => savedCallback?.Invoke(), this);
            saveTask.RunThread(() =>
            {
                try
                {
                    var backupIndexPath = GetBackupIndexPath(createDirectory: true);
                    saveTask.Report(backupIndexPath);
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
                    //Debug.Log($"Save backup index {backupIndexPath}, {DateTime.FromBinary(index.timestamp)}");
                }
                catch (IOException ex)
                {
                    Debug.LogException(ex);
                    // ignore
                }
                saveTask.Report(99, 100);
                Dispatcher.Enqueue(() => saveTask.Resolve(new TaskData(saveBytes, index)));
            });

            return saveBytes;
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

            AssetPostprocessorIndexer.contentRefreshed -= OnContentRefreshed;

            var baseScore = settings.baseScore;
            var indexName = settings.name.ToLowerInvariant();
            var updates = ProduceArtifacts(changeset.updated);

            Interlocked.Increment(ref m_UpdateTasks);
            ResolveArtifacts(updates, null, new Task("Update", $"Updating {settings.name} index ({updates.Length})", (Task task, TaskData data) =>
            {
                if (task.canceled || task.error != null)
                {
                    if (task.error != null)
                        Debug.LogException(task.error);
                    Interlocked.Decrement(ref m_UpdateTasks);
                    return;
                }

                byte[] mergedBytes = null;
                task.Report("Merging changes to index...");
                task.RunThread(() =>
                {
                    index.Merge(changeset.removed, data.combinedIndex, baseScore, (di, indexer) => indexer.AddProperty("a", indexName, di, true, true));
                    mergedBytes = index.SaveBytes();
                }, () =>
                    {
                        Interlocked.Decrement(ref m_UpdateTasks);
                        bytes = SaveIndex(mergedBytes, Setup);
                    });
            }, updates.Length, this));
        }

        private void OnArtifactsResolved(Task task, TaskData data)
        {
            m_CurrentTask = null;
            if (task.canceled || task.error != null)
                return;
            index.ApplyFrom(data.combinedIndex);
            bytes = SaveIndex(data.bytes, Setup);
        }

        private IndexArtifact[] ProduceArtifacts(IList<string> assetPaths)
        {
            var artifacts = InitializeIndexArtifacts(assetPaths);
            var indexImporterType = SearchIndexEntryImporter.GetIndexImporterType(settings.type, settings.options.GetHashCode());

            var artifactIds = AssetDatabaseExperimental.ProduceArtifactsAsync(artifacts.Select(a => new GUID(a.guid)).ToArray(), indexImporterType);
            for (int i = 0; i < artifactIds.Length; ++i)
                artifacts[i].key = artifactIds[i].value;

            return artifacts;
        }

        private static Hash128 ProduceArtifact(GUID guid, Type importerType, ImportMode mode)
        {
            return ProduceArtifact(guid, importerType, mode, out _);
        }

        private static Hash128 ProduceArtifact(GUID guid, Type importerType, ImportMode mode, out OnDemandState state)
        {
            state = OnDemandState.Unavailable;
            switch (mode)
            {
                case ImportMode.Asynchronous:
                    return AssetDatabaseExperimental.ProduceArtifactAsync(new ArtifactKey(guid, importerType)).value;
                case ImportMode.Synchronous:
                    return AssetDatabaseExperimental.ProduceArtifact(new ArtifactKey(guid, importerType)).value;
                case ImportMode.NoImport:
                    var akey = new ArtifactKey(guid, importerType);
                    var id = AssetDatabaseExperimental.LookupArtifact(akey).value;
                    if (!id.isValid)
                        state = AssetDatabaseExperimental.GetOnDemandArtifactProgress(akey).state;
                    return id;
            }

            return default;
        }

        private static Hash128 ProduceArtifact(string guid, Type importerType, ImportMode mode)
        {
            return ProduceArtifact(new GUID(guid), importerType, mode);
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
