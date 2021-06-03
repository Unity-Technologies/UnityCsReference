// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Search
{
    class AssetIndexer : ObjectIndexer
    {
        static string[] s_AssetDabaseRoots;
        static AssetIndexer()
        {
            s_AssetDabaseRoots = Utils.GetAssetRootFolders();
        }

        public AssetIndexer(SearchDatabase.Settings settings)
            : base(string.IsNullOrEmpty(settings.name) ? "assets" : settings.name, settings)
        {
        }

        internal override IEnumerable<string> GetRoots()
        {
            if (settings.roots == null)
                settings.roots = new string[0];
            var roots = settings.roots;
            if (roots.Length == 0)
                roots = new string[] { settings.root };

            return roots
                .Select(r => r.Replace("\\", "/").Trim('/'))
                .Concat(roots.SelectMany(r => s_AssetDabaseRoots.Where(adbRoot => adbRoot.StartsWith(r, StringComparison.OrdinalIgnoreCase))))
                .Distinct()
                .Where(r => Directory.Exists(r));
        }

        internal override List<string> GetDependencies()
        {
            List<string> paths = new List<string>();
            foreach (var root in GetRoots())
            {
                paths.AddRange(Directory.GetFiles(root, "*.meta", SearchOption.AllDirectories)
                    .Select(path => path.Replace("\\", "/").Substring(0, path.Length - 5))
                    .Where(path => File.Exists(path) && !SkipEntry(path)));
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

        public void IndexTypes(Type objType, int documentIndex)
        {
            while (objType != null && objType != typeof(Object))
            {
                if (objType == typeof(GameObject))
                    IndexProperty(documentIndex, "t", "prefab", saveKeyword: true);
                if (objType == typeof(MonoScript))
                    IndexProperty(documentIndex, "t", "script", saveKeyword: true);
                IndexProperty(documentIndex, "t", objType.Name, saveKeyword: true);
                objType = objType.BaseType;
            }
        }

        private void IndexSubAsset(Object subObj, string containerPath, bool checkIfDocumentExists, bool hasCustomIndexers)
        {
            var gid = GlobalObjectId.GetGlobalObjectIdSlow(subObj);
            var id = gid.ToString();
            var containerName = Path.GetFileNameWithoutExtension(containerPath);
            var objPathName = Utils.RemoveInvalidCharsFromPath($"{containerName}/{subObj.name}", ' ');
            var subObjDocumentIndex = AddDocument(id, objPathName, containerPath, checkIfDocumentExists, SearchDocumentFlags.Nested | SearchDocumentFlags.Asset);

            IndexTypes(subObj.GetType(), subObjDocumentIndex);
            IndexProperty(subObjDocumentIndex, "is", "nested", saveKeyword: true, exact: true);
            IndexProperty(subObjDocumentIndex, "is", "subasset", saveKeyword: true, exact: true);
            if (settings.options.dependencies)
                AddProperty("ref", containerPath.ToLowerInvariant(), subObjDocumentIndex);

            if (hasCustomIndexers)
                IndexCustomProperties(id, subObjDocumentIndex, subObj);

            IndexWordComponents(subObjDocumentIndex, subObj.name);
            if (settings.options.properties)
                IndexObject(subObjDocumentIndex, subObj, settings.options.dependencies);
        }

        public override void IndexDocument(string path, bool checkIfDocumentExists)
        {
            int assetInstanceId = Utils.GetMainAssetInstanceID(path);
            var globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(assetInstanceId);
            var documentIndex = AddDocument(globalObjectId.ToString(), null, path, checkIfDocumentExists, SearchDocumentFlags.Asset);
            if (documentIndex < 0)
                return;

            AddSourceDocument(path, GetDocumentHash(path));
            IndexWordComponents(documentIndex, GetPartialPath(path));

            var fileName = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            IndexWord(documentIndex, fileName, fileName.Length, true);
            IndexProperty(documentIndex, "name", fileName, saveKeyword: false);

            IndexWord(documentIndex, path, path.Length, exact: true);
            IndexProperty(documentIndex, "id", path, saveKeyword: false, exact: true);

            if (path.StartsWith("Packages/", StringComparison.Ordinal))
                IndexProperty(documentIndex, "a", "packages", saveKeyword: true, exact: true);
            else
                IndexProperty(documentIndex, "a", "assets", saveKeyword: true, exact: true);

            var fi = new FileInfo(path);
            if (fi.Exists)
            {
                IndexNumber(documentIndex, "size", (double)fi.Length);
                IndexProperty(documentIndex, "ext", fi.Extension.Replace(".", "").ToLowerInvariant(), saveKeyword: false);
                IndexNumber(documentIndex, "age", (DateTime.Now - fi.LastWriteTime).TotalDays);

                foreach (var dir in Path.GetDirectoryName(path).Split(new[] { '/', '\\' }).Skip(1))
                    IndexProperty(documentIndex, "dir", dir.ToLowerInvariant(), saveKeyword: false, exact: true);

                IndexProperty(documentIndex, "t", "file", saveKeyword: true, exact: true);
            }
            else if (Directory.Exists(path))
            {
                IndexProperty(documentIndex, "t", "folder", saveKeyword: true, exact: true);
            }

            var at = AssetDatabase.GetMainAssetTypeAtPath(path);
            var hasCustomIndexers = HasCustomIndexers(at);

            bool isPrefab = path.EndsWith(".prefab");
            if (settings.options.types && at != null)
            {
                IndexWord(documentIndex, at.Name);
                IndexTypes(at, documentIndex);

                foreach (var obj in AssetDatabase.LoadAllAssetRepresentationsAtPath(path).Where(o => o))
                {
                    IndexTypes(obj.GetType(), documentIndex);

                    if (!AssetDatabase.IsSubAsset(obj))
                        continue;

                    IndexSubAsset(obj, path, checkIfDocumentExists, hasCustomIndexers);
                }

                if (isPrefab)
                {
                    var rootPrefabObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (rootPrefabObject)
                    {
                        var gocs = rootPrefabObject.GetComponents<Component>();
                        for (int componentIndex = 1; componentIndex < gocs.Length; ++componentIndex)
                        {
                            var c = gocs[componentIndex];
                            if (!c || (c.hideFlags & (HideFlags.DontSave | HideFlags.HideInInspector)) != 0)
                                continue;
                            IndexProperty(documentIndex, "t", c.GetType().Name, saveKeyword: true);
                        }
                    }
                }
            }
            else if (at != null)
            {
                IndexProperty(documentIndex, "t", at.Name, saveKeyword: true);
            }

            var guid = AssetDatabase.GUIDFromAssetPath(path);
            var labels = AssetDatabase.GetLabels(guid);
            foreach (var label in labels)
                IndexProperty(documentIndex, "l", label, saveKeyword: true);

            if (settings.options.properties)
            {
                bool wasLoaded = AssetDatabase.IsMainAssetAtPathLoaded(path);

                var mainAsset = isPrefab ? PrefabUtility.LoadPrefabContents(path) : AssetDatabase.LoadMainAssetAtPath(path);
                if (!mainAsset)
                    return;

                if (hasCustomIndexers)
                    IndexCustomProperties(path, documentIndex, mainAsset);

                if (!string.IsNullOrEmpty(mainAsset.name))
                    IndexWord(documentIndex, mainAsset.name, true);

                if (settings.options.properties)
                    IndexObject(documentIndex, mainAsset, settings.options.dependencies);

                if (mainAsset is GameObject go)
                {
                    foreach (var v in go.GetComponents(typeof(Component)))
                    {
                        if (!v || v.GetType() == typeof(Transform) || (v.hideFlags & (HideFlags.DontSave | HideFlags.HideInInspector)) != 0)
                            continue;
                        IndexPropertyComponents(documentIndex, "t", v.GetType().Name);

                        if (settings.options.properties)
                            IndexObject(documentIndex, v, dependencies: settings.options.dependencies);
                    }
                }

                var importSettings = AssetImporter.GetAtPath(path);
                if (importSettings)
                    IndexObject(documentIndex, importSettings, dependencies: settings.options.dependencies, recursive: true);

                if (!wasLoaded)
                {
                    if (isPrefab && mainAsset is GameObject prefabObject)
                        PrefabUtility.UnloadPrefabContents(prefabObject);
                    else if (mainAsset && !mainAsset.hideFlags.HasFlag(HideFlags.DontUnloadUnusedAsset) &&
                             !(mainAsset is GameObject) &&
                             !(mainAsset is Component) &&
                             !(mainAsset is AssetBundle))
                    {
                        Resources.UnloadAsset(mainAsset);
                    }
                }
            }

            if (settings.options.extended)
                IndexSceneDocument(path, checkIfDocumentExists);

            if (settings.options.dependencies)
            {
                foreach (var depPath in AssetDatabase.GetDependencies(path, false))
                {
                    if (path == depPath)
                        continue;
                    AddReference(documentIndex, depPath);
                }
            }
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
                var documentIndex = AddDocument(id, transformPath, containerPath, checkIfDocumentExists, SearchDocumentFlags.Nested | SearchDocumentFlags.Object);

                IndexNumber(documentIndex, "depth", GetObjectDepth(obj));
                IndexWordComponents(documentIndex, Path.GetFileName(transformPath));
                IndexProperty(documentIndex, "is", "nested", saveKeyword: true, exact: true);
                IndexProperty(documentIndex, "from", type, saveKeyword: true, exact: true);
                IndexProperty(documentIndex, type, containerName, saveKeyword: false);
                IndexProperty(documentIndex, type, containerPath, exact: true, saveKeyword: false);
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
            if (!skipSelf || transform.childCount == 0)
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

        private void IndexGameObject(int documentIndex, GameObject go, SearchDatabase.Options options)
        {
            if (go.transform.root != go.transform)
                IndexProperty(documentIndex, "is", "child", saveKeyword: true, exact: true);
            else
                IndexProperty(documentIndex, "is", "root", saveKeyword: true, exact: true);

            if (go.transform.childCount == 0)
                IndexProperty(documentIndex, "is", "leaf", saveKeyword: true, exact: true);

            IndexNumber(documentIndex, "layer", go.layer);
            IndexProperty(documentIndex, "tag", go.tag, saveKeyword: true);

            if (PrefabUtility.IsAnyPrefabInstanceRoot(go))
            {
                IndexProperty(documentIndex, "t", "prefab", saveKeyword: true, exact: true);
                IndexProperty(documentIndex, "prefab", "root", saveKeyword: true, exact: true);
            }

            if (options.types)
            {
                if (PrefabUtility.IsPartOfPrefabInstance(go)) IndexProperty(documentIndex, "prefab", "instance", saveKeyword: true, exact: true);
                if (PrefabUtility.IsOutermostPrefabInstanceRoot(go)) IndexProperty(documentIndex, "prefab", "top", saveKeyword: true, exact: true);
                if (PrefabUtility.IsPartOfNonAssetPrefabInstance(go)) IndexProperty(documentIndex, "prefab", "nonasset", saveKeyword: true, exact: true);
                if (PrefabUtility.IsPartOfPrefabAsset(go)) IndexProperty(documentIndex, "prefab", "asset", saveKeyword: true, exact: true);
                if (PrefabUtility.IsPartOfAnyPrefab(go)) IndexProperty(documentIndex, "prefab", "any", saveKeyword: true, exact: true);
                if (PrefabUtility.IsPartOfModelPrefab(go)) IndexProperty(documentIndex, "prefab", "model", saveKeyword: true, exact: true);
                if (PrefabUtility.IsPartOfRegularPrefab(go)) IndexProperty(documentIndex, "prefab", "regular", saveKeyword: true, exact: true);
                if (PrefabUtility.IsPartOfVariantPrefab(go)) IndexProperty(documentIndex, "prefab", "variant", saveKeyword: true, exact: true);
                if (PrefabUtility.HasPrefabInstanceAnyOverrides(go, false)) IndexProperty(documentIndex, "prefab", "modified", saveKeyword: true, exact: true);

                if (options.dependencies)
                {
                    var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                    AddReference(documentIndex, prefabPath);
                }
            }

            if (options.properties)
                IndexObject(documentIndex, go, options.dependencies);

            var gocs = go.GetComponents<Component>();
            IndexNumber(documentIndex, "components", gocs.Length);

            for (int componentIndex = 1; componentIndex < gocs.Length; ++componentIndex)
            {
                var c = gocs[componentIndex];
                if (!c || (c.hideFlags & (HideFlags.DontSave | HideFlags.HideInInspector)) != 0)
                    continue;

                if (options.types)
                    IndexProperty(documentIndex, "t", c.GetType().Name.ToLowerInvariant(), saveKeyword: true);

                if (options.properties)
                    IndexObject(documentIndex, c, options.dependencies);
            }
        }

        private void IndexCustomGameObjectProperties(string id, int documentIndex, GameObject go)
        {
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
    }
}
