// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Profiling;
using UnityEditor.SceneManagement;
using UnityEditor.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Search
{
    class AssetIndexer : ObjectIndexer
    {
        static readonly ProfilerMarker k_IndexGameObjectMarker = new($"{nameof(AssetIndexer)}.{nameof(IndexGameObject)}");
        static readonly ProfilerMarker k_IndexCustomGameObjectPropertiesMarker = new($"{nameof(AssetIndexer)}.{nameof(IndexCustomGameObjectProperties)}");

        static string[] s_AssetDabaseRoots;
        static AssetIndexer()
        {
            s_AssetDabaseRoots = Utils.GetAssetRootFolders();
        }

        public AssetIndexer(SearchDatabase.Settings settings)
            : this(settings, new LMDBIndexStorage(FileUtil.GetUniqueTempPathInProject()))
        {}

        public AssetIndexer(SearchDatabase.Settings settings, ISearchIndexerStorage storage)
            : base(string.IsNullOrEmpty(settings.name) ? "assets" : settings.name, settings, storage)
        {}

        internal override IEnumerable<string> GetRoots()
        {
            if (settings.roots == null)
                settings.roots = Array.Empty<string>();
            var roots = settings.roots;
            if (roots.Length == 0)
                roots = new string[] { settings.root };

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return roots
#pragma warning restore RS0030
                .Select(r => r.Replace("\\", "/").Trim('/'))
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                .Concat(roots.SelectMany(r => s_AssetDabaseRoots.Where(adbRoot => adbRoot.StartsWith(r, StringComparison.OrdinalIgnoreCase))))
#pragma warning restore RS0030
                .Distinct()
                .Where(r => Directory.Exists(r));
        }

        internal override List<string> GetDependencies()
        {
            List<string> paths = new List<string>();
            foreach (var root in GetRoots())
            {
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                paths.AddRange(Directory.GetFiles(root, "*.meta", SearchOption.AllDirectories)
#pragma warning restore RS0030
                    .Select(path => path.Replace("\\", "/").Substring(0, path.Length - 5))
                    .Where(path => !SkipEntry(path) && File.Exists(path)));
            }

            return paths;
        }

        internal override Hash128 GetDocumentHash(string path)
        {
            var guid = AssetDatabase.AssetPathToGUID(path);
            return Utils.GetSourceAssetFileHash(guid);
        }

        public string GetPartialPath(string path)
        {
            path = path.Replace("Assets/", "");
            if (path.StartsWith("Packages/", StringComparison.Ordinal))
                path = Regex.Replace(path, @"Packages\/com\.unity\.[^/]+\/", "");
            return path;
        }

        public void IndexTypes(Type objType, int documentIndex, bool isPrefabDocument, string name = "t", bool exact = false)
        {
            Utils.EnumerateIndexedTypesAndInterfaces(objType, isPrefabDocument, (typeName, isFullName) =>
            {
                IndexProperty(documentIndex, name, typeName, saveKeyword: true);
            });
        }

        private void IndexSubAsset(Object subObj, string containerPath, string containerGlobalObjectId, bool checkIfDocumentExists, bool hasCustomIndexers)
        {
            var gid = GlobalObjectId.GetGlobalObjectIdSlow(subObj);
            var id = gid.ToString();
            var containerName = Utils.GetFileNameWithoutExtension(containerPath);
            var isPrefabDocument = containerPath.EndsWith(".prefab");
            var objPathName = $"{containerName}/{subObj.name}";
            var subObjDocumentIndex = AddDocument(id, objPathName, containerPath, checkIfDocumentExists, SearchDocumentFlags.Nested | SearchDocumentFlags.Asset);
            IndexTypes(subObj.GetType(), subObjDocumentIndex, isPrefabDocument, exact: true);
            IndexFolder(subObjDocumentIndex, containerPath);
            IndexDocumentAreaFromPath(subObjDocumentIndex, containerPath);
            if (subObj is GameObject subGo)
            {
                IndexComponents(subObjDocumentIndex, subGo);
            }
            AddProperty("is", "nested", settings.baseScore, subObjDocumentIndex, saveKeyword: true);
            AddProperty("is", "subasset", settings.baseScore, subObjDocumentIndex, saveKeyword: true);
            if (settings.options.dependencies)
            {
                AddProperty("ref", containerPath.ToLowerInvariant(), subObjDocumentIndex);
                AddProperty("ref", containerGlobalObjectId.ToLowerInvariant(), subObjDocumentIndex);
            }

            IndexLabels(subObjDocumentIndex, subObj);

            if (hasCustomIndexers)
                IndexCustomProperties(id, subObjDocumentIndex, subObj);

            if (!string.IsNullOrEmpty(subObj.name))
                IndexWordComponents(subObjDocumentIndex, subObj.name);
            if (settings.options.properties)
                IndexObject(subObjDocumentIndex, subObj, settings.options.dependencies);
        }

        public override void IndexDocument(string path, bool checkIfDocumentExists)
        {
            var assetInstanceId = Utils.GetMainAssetEntityId(path);
            var globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow((EntityId)assetInstanceId);
            var globalObjectIdStr = globalObjectId.ToString();
            var documentIndex = AddDocument(globalObjectIdStr, null, path, checkIfDocumentExists, SearchDocumentFlags.Asset);
            if (documentIndex < 0)
                return;

            AddSourceDocument(path, GetDocumentHash(path));
            IndexFileName(documentIndex, path);

            IndexDocumentAreaFromPath(documentIndex, path);

            var fi = new FileInfo(FileUtil.PathToAbsolutePath(path));
            if (fi.Exists)
            {
                IndexNumber(documentIndex, "size", (double)fi.Length);
                IndexProperty(documentIndex, "ext", fi.Extension.Replace(".", ""), saveKeyword: false);
                IndexNumber(documentIndex, "age", (DateTime.Now - fi.LastWriteTime).TotalDays);
                IndexFolder(documentIndex, path);
                IndexProperty(documentIndex, "t", "file", saveKeyword: true);
            }
            else if (Directory.Exists(path))
            {
                IndexProperty(documentIndex, "t", "folder", saveKeyword: true);
            }

            var at = AssetDatabase.GetMainAssetTypeAtPath(path);
            var hasCustomIndexers = HasCustomIndexers(at);

            if (AssetImporter.GetImportLogEntriesCount(globalObjectId.assetGUID, out var nbErrors, out var nbWarnings))
            {
                IndexNumber(documentIndex, "errors", nbErrors);
                IndexNumber(documentIndex, "warnings", nbWarnings);
                if (nbErrors > 0)
                    IndexProperty(documentIndex, "i", "errors", saveKeyword: true);

                if (nbWarnings > 0)
                    IndexProperty(documentIndex, "i", "warnings", saveKeyword: true);
            }

            var isPrefab = path.EndsWith(".prefab");

            if (at != null)
            {
                IndexWordComponents(documentIndex, at.Name);
                IndexTypes(at, documentIndex, isPrefab);

                if (settings.options.types)
                {
                    IndexProperty(documentIndex, "is", "main", saveKeyword: true);
                    if (AssetImporter.GetAtPath(path) is ModelImporter)
                        IndexProperty(documentIndex, "t", "model", saveKeyword: true);

                    #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    foreach (var obj in AssetDatabase.LoadAllAssetRepresentationsAtPath(path).Where(o => o))
#pragma warning restore RS0030
                    {
                        if (AssetDatabase.IsSubAsset(obj))
                        {
                            IndexTypes(obj.GetType(), documentIndex, isPrefab, "has", exact: true);
                            IndexSubAsset(obj, path, globalObjectIdStr, checkIfDocumentExists, hasCustomIndexers);
                        }
                        else if (!string.IsNullOrEmpty(obj.name))
                            IndexProperty(documentIndex, "name", obj.name, saveKeyword: true);
                    }

                    if (isPrefab || at == typeof(GameObject))
                    {
                        var rootPrefabObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (rootPrefabObject)
                        {
                            IndexComponents(documentIndex, rootPrefabObject);
                        }
                    }
                }
            }

            IndexLabels(documentIndex, path);
            IndexBundles(documentIndex, path);

            if (settings.options.properties || hasCustomIndexers)
                IndexProperties(documentIndex, path, hasCustomIndexers);

            if (settings.options.extended)
                IndexSceneDocument(path, checkIfDocumentExists);

            if (settings.options.dependencies)
                IndexDependencies(documentIndex, path);
        }

        void IndexComponents(in int documentIndex, GameObject go)
        {
            var gocs = go.GetComponents<Component>();
            for (int componentIndex = 1; componentIndex < gocs.Length; ++componentIndex)
            {
                var c = gocs[componentIndex];
                if (!c || (c.hideFlags & (HideFlags.DontSave | HideFlags.HideInInspector)) != 0)
                    continue;
                IndexTypes(c.GetType(), documentIndex, false);
            }
        }

        internal void IndexFileName(in int documentIndex, string filePath)
        {
            var fileName = Utils.GetFileName(filePath);
            var extension = Utils.GetExtensionWithoutDot(fileName);
            var components = SearchUtils.SplitEntryComponents(fileName, SearchUtils.entrySeparators, 1);
            int scoreModifier = 0;
            var minIndexationLength = minWordIndexationLength;
            minWordIndexationLength = 1;
            var componentCount = 0;
            foreach (var c in components)
            {
                // Skip indexing variations on extension
                if (c == extension)
                    continue;

                var component = c.ToLowerInvariant();
                var score = settings.baseScore + scoreModifier++;
                AddWord(component, score, documentIndex);
                componentCount++;
            }

            if (componentCount > 1)
            {
                var fileNameWithoutExtension = Utils.GetFileNameWithoutExtension(fileName);
                IndexProperty(documentIndex, "name", fileNameWithoutExtension, saveKeyword: false, scoreModifier);
                IndexWord(documentIndex, fileNameWithoutExtension, scoreModifier: scoreModifier);
            }

            if (!string.IsNullOrEmpty(extension))
            {
                IndexProperty(documentIndex, "name", fileName, saveKeyword: false, scoreModifier);
                IndexWord(documentIndex, fileName, scoreModifier: scoreModifier);
                IndexWord(documentIndex, extension, scoreModifier: scoreModifier);
            }

            minWordIndexationLength = minIndexationLength;
        }

        internal void IndexFolder(in int documentIndex, string path)
        {
            path = path.ToLowerInvariant();
            var dirPath = Paths.ConvertSeparatorsToUnity(Path.GetDirectoryName(path));
            var dirTokens = dirPath.Split('/');
            for (int i = 0; i < dirTokens.Length; ++i)
            {
                var dir = dirTokens[i];
                AddProperty("dir", dir, 5, documentIndex, saveKeyword: false);
                for (int j = i + 1; j < dirTokens.Length; ++j)
                {
                    dir += "/" + dirTokens[j];
                    AddProperty("dir", dir, 5, documentIndex, saveKeyword: false);
                }
            }
            AddProperty("dir", dirPath, 100, documentIndex, saveKeyword: false);
        }

        private void IndexProperties(in int documentIndex, in string path, in bool hasCustomIndexers)
        {
            bool wasLoaded = AssetDatabase.IsMainAssetAtPathLoaded(path);

            var mainAsset = AssetDatabase.LoadMainAssetAtPath(path);
            if (!mainAsset)
                return;

            if (settings.options.properties)
            {
                IndexObject(documentIndex, mainAsset, settings.options.dependencies);

                if (mainAsset is GameObject go)
                {
                    foreach (var v in go.GetComponents(typeof(Component)))
                    {
                        if (!v || v.GetType() == typeof(Transform) || (v.hideFlags & (HideFlags.DontSave | HideFlags.HideInInspector)) != 0)
                            continue;
                        if (settings.options.properties)
                        {
                            IndexObject(documentIndex, v, dependencies: settings.options.dependencies);
                        }
                    }
                }

                var importSettings = AssetImporter.GetAtPath(path);
                if (importSettings)
                    IndexObject(documentIndex, importSettings, dependencies: settings.options.dependencies, recursive: true);
            }

            if (hasCustomIndexers)
            {
                if (mainAsset is GameObject go)
                    IndexCustomGameObjectProperties(path, documentIndex, go);
                else
                    IndexCustomProperties(path, documentIndex, mainAsset);
            }

            if (!wasLoaded)
            {
                if (mainAsset && !mainAsset.hideFlags.HasFlag(HideFlags.DontUnloadUnusedAsset) &&
                    !(mainAsset is GameObject) &&
                    !(mainAsset is Component) &&
                    !(mainAsset is AssetBundle))
                {
                    Resources.UnloadAsset(mainAsset);
                }
            }
        }

        private void IndexDependencies(in int documentIndex, in string path)
        {
            foreach (var depPath in AssetDatabase.GetDependencies(path, false))
            {
                if (path == depPath)
                    continue;
                AddReference(documentIndex, depPath);
            }
        }

        private void IndexBundles(int documentIndex, string path)
        {
            var bundleName = AssetDatabase.GetImplicitAssetBundleName(path);
            if (string.IsNullOrEmpty(bundleName))
                return;
            IndexProperty(documentIndex, "b", bundleName, saveKeyword:false);
        }

        private void IndexLabels(in int documentIndex, in string path)
        {
            var guid = AssetDatabase.GUIDFromAssetPath(path);
            var labels = AssetDatabase.GetLabels(guid);
            foreach (var label in labels)
                IndexProperty(documentIndex, "l", label, saveKeyword: false);
        }

        private void IndexLabels(in int documentIndex, in UnityEngine.Object obj)
        {
            var labels = AssetDatabase.GetLabels(obj);
            foreach (var label in labels)
                IndexProperty(documentIndex, "l", label, saveKeyword: false);
        }

        public bool IndexSceneDocument(string scenePath, bool checkIfDocumentExists)
        {
            if (scenePath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            {
                IndexScene(scenePath, checkIfDocumentExists);
                return true;
            }
            else if (scenePath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                IndexPrefab(scenePath, checkIfDocumentExists);
                return true;
            }
            return false;
        }

        private void IndexObjects(GameObject[] objects, string type, string containerName, string containerPath, bool checkIfDocumentExists)
        {
            var options = settings.options;
            var globalIds = new GlobalObjectId[objects.Length];
            GlobalObjectId.GetGlobalObjectIdsSlow(objects, globalIds);
            var containerSearchArea = GetSearchAreaFromPath(containerPath);

            for (int i = 0; i < objects.Length; ++i)
            {
                var obj = objects[i];
                if (!obj || (obj.hideFlags & (HideFlags.DontSave | HideFlags.HideInHierarchy)) != 0)
                    continue;

                var gid = globalIds[i];
                if (gid.identifierType == 0 || gid.assetGUID.Empty())
                    continue;

                if (PrefabUtility.IsPrefabAssetMissing(obj))
                    continue;

                if (obj.tag?.Equals("noindex~", StringComparison.Ordinal) ?? false)
                    continue;

                var id = gid.ToString();
                var transformPath = SearchUtils.GetTransformPath(obj.transform);
                var documentIndex = AddDocument(id, options.types ? transformPath : null, containerPath, checkIfDocumentExists, SearchDocumentFlags.Nested | SearchDocumentFlags.Object);
                var depth = GetObjectDepth(obj);
                if (options.types)
                {
                    IndexNumber(documentIndex, "depth", depth);
                    IndexProperty(documentIndex, "from", type, saveKeyword: true);
                }

                if (options.dependencies)
                {
                    IndexProperty(documentIndex, type, containerName, saveKeyword: false);
                    IndexProperty(documentIndex, type, containerPath, saveKeyword: false);
                }

                // TODO Deep Indexing: we are currently not indexing all the properties of a nested objects. It could be worth indexing the same set of properties as a root object:
                // types, path, dir, name, size, age, ext

                IndexWord(documentIndex, obj.transform.name);
                IndexProperty(documentIndex, "is", "nested", saveKeyword: true);
                IndexDocumentArea(documentIndex, containerSearchArea);

                // Root objects have already all their properties indexed.
                IndexGameObject(documentIndex, obj, options);
                IndexCustomGameObjectProperties(id, documentIndex, obj);
            }
        }

        private int GetObjectDepth(GameObject obj)
        {
            int depth = 1;
            var transform = obj.transform;
            while (transform != null && transform.root != transform)
            {
                ++depth;
                transform = transform.parent;
            }
            return depth;
        }

        static void MergeObjects(IList<GameObject> objects, Transform transform, bool skipSelf)
        {
            if (!transform || !transform.gameObject || (transform.gameObject.hideFlags & (HideFlags.DontSave | HideFlags.HideInInspector)) != 0)
                return;
            if (!skipSelf)
                objects.Add(transform.gameObject);
            for (int c = 0, end = transform.childCount; c != end; ++c)
                MergeObjects(objects, transform.GetChild(c), false);
        }

        private void IndexPrefab(string prefabPath, bool checkIfDocumentExists)
        {
            var prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (!prefabRoot)
                return;
            var objects = new List<GameObject>();
            MergeObjects(objects, prefabRoot.transform, true);
            IndexObjects(objects.ToArray(), "prefab", prefabRoot.name, prefabPath, checkIfDocumentExists);
        }

        private void IndexScene(string scenePath, bool checkIfDocumentExists)
        {
            bool sceneAdded = false;
            var scene = EditorSceneManager.GetSceneByPath(scenePath);
            try
            {
                if (scene == null || !scene.isLoaded)
                {
                    scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                    sceneAdded = scene != null && scene.isLoaded;
                }

                if (scene == null || !scene.isLoaded)
                    return;

                var objects = SearchUtils.FetchGameObjects(scene);
                IndexObjects(objects, "scene", scene.name, scenePath, checkIfDocumentExists);
            }
            finally
            {
                if (sceneAdded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private void IndexGameObject(in int documentIndex, in GameObject go, in SearchDatabase.Options options)
        {
            using var _ = k_IndexGameObjectMarker.Auto();
            if (options.types)
            {
                if (go.transform.root != go.transform)
                    IndexProperty(documentIndex, "is", "child", saveKeyword: true);
                else
                    IndexProperty(documentIndex, "is", "root", saveKeyword: true);

                if (go.transform.childCount == 0)
                    IndexProperty(documentIndex, "is", "leaf", saveKeyword: true);

                IndexNumber(documentIndex, "layer", go.layer);
                IndexProperty(documentIndex, "tag", go.tag, saveKeyword: true);
            }

            if (PrefabUtility.IsAnyPrefabInstanceRoot(go))
            {
                IndexProperty(documentIndex, "t", "prefab", saveKeyword: true);

                if (options.types)
                    IndexProperty(documentIndex, "prefab", "root", saveKeyword: true);
            }

            if (options.properties)
            {
                IndexObject(documentIndex, go, options.dependencies);

                IndexerExtensions.IndexPrefabProperties(documentIndex, go, this);
            }

            if (options.dependencies)
            {
                var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                AddReference(documentIndex, prefabPath);
            }

            var gocs = go.GetComponents<Component>();
            IndexNumber(documentIndex, "components", gocs.Length);

            for (int componentIndex = 1; componentIndex < gocs.Length; ++componentIndex)
            {
                var c = gocs[componentIndex];
                if (!c || (c.hideFlags & (HideFlags.DontSave | HideFlags.HideInInspector)) != 0)
                    continue;

                IndexTypes(c.GetType(), documentIndex, false);

                if (options.properties)
                    IndexObject(documentIndex, c, options.dependencies);
            }
        }

        private void IndexCustomGameObjectProperties(string id, int documentIndex, GameObject go)
        {
            using var _ = k_IndexCustomGameObjectPropertiesMarker.Auto();
            if (HasCustomIndexers(go.GetType()))
                IndexCustomProperties(id, documentIndex, go);

            var gocs = go.GetComponents<Component>();
            for (var componentIndex = 0; componentIndex < gocs.Length; ++componentIndex)
            {
                var c = gocs[componentIndex];
                if (!c)
                    continue;
                if (HasCustomIndexers(c.GetType()))
                    IndexCustomProperties(id, documentIndex, c);
            }
        }

        void IndexDocumentAreaFromPath(int documentIndex, string documentSourceFilePath)
        {
            IndexDocumentArea(documentIndex, GetSearchAreaFromPath(documentSourceFilePath));
        }

        void IndexDocumentArea(int documentIndex, SearchArea area)
        {
            switch (area)
            {
                case SearchArea.Assets:
                    IndexProperty(documentIndex, "a", "assets", saveKeyword: true);
                    break;
                case SearchArea.Packages:
                    IndexProperty(documentIndex, "a", "packages", saveKeyword: true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(area), area, null);
            }
        }

        static SearchArea GetSearchAreaFromPath(string path)
        {
            if (path.StartsWith("Packages/", StringComparison.Ordinal))
                return SearchArea.Packages;
            return SearchArea.Assets;
        }
    }
}
