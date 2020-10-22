// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define DEBUG_INDEXING

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityEditor.Search
{
    class SceneIndexer : ObjectIndexer
    {
        public SceneIndexer(SearchDatabase.Settings settings)
            : base(String.IsNullOrEmpty(settings.name) ? "objects" : settings.name, settings)
        {
        }

        public override bool SkipEntry(string path, bool checkRoots = false)
        {
            if (checkRoots && !GetRoots().Any(p => p == path))
                return true;
            return base.SkipEntry(path, false);
        }

        private string ConvertTypeToFilePattern(string type)
        {
            type = type.ToLowerInvariant();
            if (type == "scene")
                return "*.unity";
            return $"*.{type}";
        }

        private IEnumerable<string> FindFiles(string root, string searchFilePattern)
        {
            if (!Directory.Exists(root))
                return Enumerable.Empty<string>();
            return Directory.EnumerateFiles(root, searchFilePattern, SearchOption.AllDirectories)
                .Select(path => path.Replace("\\", "/"));
        }

        internal override IEnumerable<string> GetRoots()
        {
            var scenePaths = new List<string>();
            var searchFilePattern = ConvertTypeToFilePattern(settings.type);
            if (searchFilePattern == null)
                return new string[0];

            if (settings.roots == null || settings.roots.Length == 0)
            {
                scenePaths.AddRange(FindFiles(settings.root, searchFilePattern));
            }
            else
            {
                foreach (var path in settings.roots)
                {
                    if (File.Exists(path))
                        scenePaths.Add(path);
                    else if (Directory.Exists(path))
                        scenePaths.AddRange(FindFiles(path, searchFilePattern));
                }
            }

            return scenePaths;
        }

        internal override List<string> GetDependencies()
        {
            return GetRoots()
                .Where(path => !base.SkipEntry(path, false)).ToList();
        }

        internal override Hash128 GetDocumentHash(string path)
        {
            return AssetDatabase.GetAssetDependencyHash(path);
        }

        public override void IndexDocument(string scenePath, bool checkIfDocumentExists)
        {
            if (scenePath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                IndexScene(scenePath, checkIfDocumentExists);
            else if (scenePath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                IndexPrefab(scenePath, checkIfDocumentExists);
            AddDocumentHash(scenePath, GetDocumentHash(scenePath));
        }

        private void IndexObjects(GameObject[] objects, string type, string containerName, bool checkIfDocumentExists)
        {
            var options = settings.options;
            var globalIds = new GlobalObjectId[objects.Length];
            GlobalObjectId.GetGlobalObjectIdsSlow(objects, globalIds);

            for (int i = 0; i < objects.Length; ++i)
            {
                var obj = objects[i];
                if (!obj)
                    continue;

                if (PrefabUtility.IsPrefabAssetMissing(obj))
                    continue;

                if (obj.tag?.Equals("noindex~", StringComparison.Ordinal) ?? false)
                    continue;

                var gid = globalIds[i];
                var id = gid.ToString();
                var path = SearchUtils.GetTransformPath(obj.transform);
                var documentIndex = AddDocument(id, path, checkIfDocumentExists);

                if (!string.IsNullOrEmpty(name))
                    IndexProperty(documentIndex, "a", name, saveKeyword: true);

                var depth = GetObjectDepth(obj);
                IndexNumber(documentIndex, "depth", depth);

                IndexWordComponents(documentIndex, path.Replace(containerName, "").Trim(' ', '/'));
                IndexProperty(documentIndex, "from", type, saveKeyword: true, exact: true);
                IndexProperty(documentIndex, type, containerName, saveKeyword: true);
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

        private void IndexPrefab(string prefabPath, bool checkIfDocumentExists)
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (!prefabRoot)
                return;
            try
            {
                var objects = SceneModeUtility.GetObjects(new[] { prefabRoot }, true);
                IndexObjects(objects, "prefab", prefabRoot.name, checkIfDocumentExists);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
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
                IndexObjects(objects, "scene", scene.name, checkIfDocumentExists);
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
                IndexObject(documentIndex, go);

            var gocs = go.GetComponents<Component>();
            IndexNumber(documentIndex, "components", gocs.Length);

            for (int componentIndex = 1; componentIndex < gocs.Length; ++componentIndex)
            {
                var c = gocs[componentIndex];
                if (!c || c.hideFlags.HasFlag(HideFlags.HideInInspector))
                    continue;

                if (options.types)
                    IndexProperty(documentIndex, "t", c.GetType().Name.ToLowerInvariant(), saveKeyword: true);

                if (options.properties)
                    IndexObject(documentIndex, c);
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
