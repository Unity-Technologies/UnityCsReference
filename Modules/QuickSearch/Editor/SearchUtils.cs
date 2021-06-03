// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

using UnityEditor.SceneManagement;

namespace UnityEditor.Search
{
    /// <summary>
    /// Utilities used by multiple components of QuickSearch.
    /// </summary>
    public static class SearchUtils
    {
        internal static readonly char[] KeywordsValueDelimiters = new[] { ':', '=', '<', '>', '!' };

        /// <summary>
        /// Separators used to split an entry into indexable tokens.
        /// </summary>
        public static readonly char[] entrySeparators = { '/', ' ', '_', '-', '.' };

        private static readonly Stack<StringBuilder> _SbPool = new Stack<StringBuilder>();

        /// <summary>
        /// Extract all variations on a word.
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public static string[] FindShiftLeftVariations(string word)
        {
            if (word.Length <= 1)
                return new string[0];

            var variations = new List<string>(word.Length) { word };
            for (int i = 1, end = word.Length - 1; i < end; ++i)
            {
                word = word.Substring(1);
                variations.Add(word);
            }

            return variations.ToArray();
        }

        /// <summary>
        /// Tokenize a string each Capital letter.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string[] SplitCamelCase(string source)
        {
            return Regex.Split(source, @"(?<!^)(?=[A-Z0-9])");
        }

        internal static string UppercaseFirst(string s)
        {
            // Check for empty string.
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            // Return char and concat substring.
            return char.ToUpper(s[0]) + s.Substring(1);
        }

        internal static string ToPascalWithSpaces(string s)
        {
            // Check for empty string.
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }

            var tokens = Regex.Split(s, @"-+|_+|\s+|(?<!^)(?=[A-Z0-9])")
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(UppercaseFirst);
            return string.Join(" ", tokens);
        }

        /// <summary>
        /// Split an entry according to a specified list of separators.
        /// </summary>
        /// <param name="entry">Entry to split.</param>
        /// <param name="entrySeparators">List of separators that indicate split points.</param>
        /// <returns>Returns list of tokens in lowercase</returns>
        public static IEnumerable<string> SplitEntryComponents(string entry, char[] entrySeparators)
        {
            var nameTokens = entry.Split(entrySeparators).Distinct();
            var scc = nameTokens.SelectMany(s => SplitCamelCase(s)).Where(s => s.Length > 0);
            var fcc = scc.Aggregate("", (current, s) => current + s[0]);
            return new[] { fcc, entry }.Concat(scc.Where(s => s.Length > 1))
                .Where(s => s.Length > 1)
                .Select(s => s.ToLowerInvariant())
                .Distinct();
        }

        /// <summary>
        /// Split a file entry according to a list of separators and find all the variations on the entry name.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="entrySeparators"></param>
        /// <returns>Returns list of tokens and variations in lowercase</returns>
        public static IEnumerable<string> SplitFileEntryComponents(string path, char[] entrySeparators)
        {
            path = Utils.RemoveInvalidCharsFromPath(path, '_');
            var name = Path.GetFileName(path);
            var nameTokens = name.Split(entrySeparators).Distinct().ToArray();
            var scc = nameTokens.SelectMany(s => SplitCamelCase(s)).Where(s => s.Length > 0).ToArray();
            var fcc = scc.Aggregate("", (current, s) => current + s[0]);
            return new[] { name, Path.GetExtension(path).Replace(".", "") }
                .Concat(scc.Where(s => s.Length > 1))
                .Concat(FindShiftLeftVariations(fcc))
                .Concat(nameTokens)
                .Concat(path.Split(entrySeparators).Reverse())
                .Where(s => s.Length > 1)
                .Select(s => s.ToLowerInvariant())
                .Distinct();
        }

        /// <summary>
        /// Format the pretty name of a Transform component by appending all the parents hierarchy names.
        /// </summary>
        /// <param name="tform">Transform to extract name from.</param>
        /// <returns>Returns a transform name using "/" as hierarchy separator.</returns>
        public static string GetTransformPath(Transform tform)
        {
            if (tform.parent == null)
                return "/" + tform.name;
            return GetTransformPath(tform.parent) + "/" + tform.name;
        }

        /// <summary>
        /// Get the path of a Unity Object. If it is a GameObject or a Component it is the <see cref="SearchUtils.GetTransformPath(Transform)"/>. Else it is the asset name.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>Returns the path of an object.</returns>
        public static string GetObjectPath(UnityEngine.Object obj)
        {
            if (obj is GameObject go)
                return GetTransformPath(go.transform);
            if (obj is Component c)
                return GetTransformPath(c.gameObject.transform);
            return obj.name;
        }

        /// <summary>
        /// Return a unique document key owning the object
        /// </summary>
        internal static ulong GetDocumentKey(UnityEngine.Object obj)
        {
            if (!obj)
                return ulong.MaxValue;
            if (obj is GameObject go)
                return GetTransformPath(go.transform).GetHashCode64();
            if (obj is Component c)
                return GetTransformPath(c.gameObject.transform).GetHashCode64();
            var assetPath = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(assetPath))
                return ulong.MaxValue;
            return AssetDatabase.AssetPathToGUID(assetPath).GetHashCode64();
        }

        /// <summary>
        /// Get the hierarchy path of a GameObject possibly including the scene name.
        /// </summary>
        /// <param name="gameObject">GameObject to extract a path from.</param>
        /// <param name="includeScene">If true, will append the scene name to the path.</param>
        /// <returns>Returns the path of a GameObject.</returns>
        public static string GetHierarchyPath(GameObject gameObject, bool includeScene = true)
        {
            if (gameObject == null)
                return String.Empty;

            StringBuilder sb;
            if (_SbPool.Count > 0)
            {
                sb = _SbPool.Pop();
                sb.Clear();
            }
            else
            {
                sb = new StringBuilder(200);
            }

            try
            {
                if (includeScene)
                {
                    var sceneName = gameObject.scene.name;
                    if (sceneName == string.Empty)
                    {
                        var prefabStage = PrefabStageUtility.GetPrefabStage(gameObject);
                        if (prefabStage != null)
                            sceneName = "Prefab Stage";
                        else
                            sceneName = "Unsaved Scene";
                    }

                    sb.Append("<b>" + sceneName + "</b>");
                }

                sb.Append(GetTransformPath(gameObject.transform));

                var path = sb.ToString();
                sb.Clear();
                return path;
            }
            finally
            {
                _SbPool.Push(sb);
            }
        }

        /// <summary>
        /// Get the path of the scene (or prefab) containing a GameObject.
        /// </summary>
        /// <param name="gameObject">GameObject to find the scene path.</param>
        /// <param name="prefabOnly">If true, will return a path only if the GameObject is a prefab.</param>
        /// <returns>Returns the path of a scene or prefab</returns>
        public static string GetHierarchyAssetPath(GameObject gameObject, bool prefabOnly = false)
        {
            if (gameObject == null)
                return String.Empty;

            bool isPrefab = PrefabUtility.GetPrefabAssetType(gameObject.gameObject) != PrefabAssetType.NotAPrefab;
            if (isPrefab)
                return PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);

            if (prefabOnly)
                return null;

            return gameObject.scene.path;
        }

        /// <summary>
        /// Select and ping multiple objects in the Project Browser.
        /// </summary>
        /// <param name="items">Search Items to select and ping.</param>
        /// <param name="focusProjectBrowser">If true, will focus the project browser before pinging the objects.</param>
        /// <param name="pingSelection">If true, will ping the selected objects.</param>
        public static void SelectMultipleItems(IEnumerable<SearchItem> items, bool focusProjectBrowser = false, bool pingSelection = true)
        {
            Selection.objects = items.Select(i => i.ToObject()).Where(o => o).ToArray();
            if (Selection.objects.Length == 0)
            {
                var firstItem = items.FirstOrDefault();
                if (firstItem != null)
                    EditorUtility.OpenWithDefaultApp(firstItem.id);
                return;
            }
            EditorApplication.delayCall += () =>
            {
                if (focusProjectBrowser)
                    EditorWindow.FocusWindowIfItsOpen(Utils.GetProjectBrowserWindowType());
                if (pingSelection)
                    EditorApplication.delayCall += () => EditorGUIUtility.PingObject(Selection.objects.LastOrDefault());
            };
        }

        /// <summary>
        /// Helper function to match a string against the SearchContext. This will try to match the search query against each tokens of content (similar to the AddComponent menu workflow)
        /// </summary>
        /// <param name="context">Search context containing the searchQuery that we try to match.</param>
        /// <param name="content">String content that will be tokenized and use to match the search query.</param>
        /// <param name="ignoreCase">Perform matching ignoring casing.</param>
        /// <returns>Has a match occurred.</returns>
        public static bool MatchSearchGroups(SearchContext context, string content, bool ignoreCase = false)
        {
            return MatchSearchGroups(context.searchQuery, context.searchWords, content, out _, out _,
                ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }

        internal static bool MatchSearchGroups(string searchContext, string[] tokens, string content, out int startIndex, out int endIndex, StringComparison sc = StringComparison.OrdinalIgnoreCase)
        {
            startIndex = endIndex = -1;
            if (String.IsNullOrEmpty(content))
                return false;

            if (string.IsNullOrEmpty(searchContext))
                return false;

            if (searchContext == content)
            {
                startIndex = 0;
                endIndex = content.Length - 1;
                return true;
            }

            return MatchSearchGroups(tokens, content, out startIndex, out endIndex, sc);
        }

        internal static bool MatchSearchGroups(string[] tokens, string content, out int startIndex, out int endIndex, StringComparison sc = StringComparison.OrdinalIgnoreCase)
        {
            startIndex = endIndex = -1;
            if (String.IsNullOrEmpty(content))
                return false;

            // Each search group is space separated
            // Search group must match in order and be complete.
            var searchGroups = tokens;
            var startSearchIndex = 0;
            foreach (var searchGroup in searchGroups)
            {
                if (searchGroup.Length == 0)
                    continue;

                startSearchIndex = content.IndexOf(searchGroup, startSearchIndex, sc);
                if (startSearchIndex == -1)
                {
                    return false;
                }

                startIndex = startIndex == -1 ? startSearchIndex : startIndex;
                startSearchIndex = endIndex = startSearchIndex + searchGroup.Length - 1;
            }

            return startIndex != -1 && endIndex != -1;
        }

        /// <summary>
        /// Utility function to fetch all the game objects in a particular scene.
        /// </summary>
        /// <param name="scene">Scene to get objects from.</param>
        /// <returns>The array of game objects in the scene.</returns>
        public static GameObject[] FetchGameObjects(Scene scene)
        {
            var goRoots = new List<UnityEngine.Object>();
            if (!scene.IsValid() || !scene.isLoaded)
                return new GameObject[0];
            var sceneRootObjects = scene.GetRootGameObjects();
            if (sceneRootObjects != null && sceneRootObjects.Length > 0)
                goRoots.AddRange(sceneRootObjects);

            return SceneModeUtility.GetObjects(goRoots.ToArray(), true)
                .Where(o => (o.hideFlags & HideFlags.HideInHierarchy) != HideFlags.HideInHierarchy).ToArray();
        }

        /// <summary>
        /// Utility function to fetch all the game objects for the current stage (i.e. scene or prefab)
        /// </summary>
        /// <returns>The array of game objects in the current stage.</returns>
        public static IEnumerable<GameObject> FetchGameObjects()
        {
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
                return SceneModeUtility.GetObjects(new[] { prefabStage.prefabContentsRoot }, true);

            var goRoots = new List<UnityEngine.Object>();
            for (int i = 0; i < SceneManager.sceneCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded)
                    continue;

                var sceneRootObjects = scene.GetRootGameObjects();
                if (sceneRootObjects != null && sceneRootObjects.Length > 0)
                    goRoots.AddRange(sceneRootObjects);
            }

            return SceneModeUtility.GetObjects(goRoots.ToArray(), true)
                .Where(o => (o.hideFlags & HideFlags.HideInHierarchy) != HideFlags.HideInHierarchy);
        }

        internal static ISet<string> GetReferences(UnityEngine.Object obj, int level = 1)
        {
            var refs = new HashSet<string>();

            var objPath = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(objPath))
                refs.UnionWith(AssetDatabase.GetDependencies(objPath));

            if (obj is GameObject go)
            {
                foreach (var c in go.GetComponents<Component>())
                {
                    using (var so = new SerializedObject(c))
                    {
                        var p = so.GetIterator();
                        var next = p.NextVisible(true);
                        while (next)
                        {
                            if (p.propertyType == SerializedPropertyType.ObjectReference && p.objectReferenceValue)
                            {
                                var refValue = AssetDatabase.GetAssetPath(p.objectReferenceValue);
                                if (!String.IsNullOrEmpty(refValue))
                                    refs.Add(refValue);
                            }

                            next = p.NextVisible(!p.isArray && !p.isFixedBuffer);
                        }
                    }
                }
            }

            var lvlRefs = refs;
            while (level-- > 0)
            {
                var nestedRefs = new HashSet<string>();

                foreach (var r in lvlRefs)
                    nestedRefs.UnionWith(AssetDatabase.GetDependencies(r, false));

                lvlRefs = nestedRefs;
                lvlRefs.ExceptWith(refs);
                refs.UnionWith(nestedRefs);
            }

            refs.Remove(objPath);

            return refs;
        }

        static readonly Dictionary<string, SearchProvider> s_GroupProviders = new Dictionary<string, SearchProvider>();
        internal static SearchProvider CreateGroupProvider(SearchProvider templateProvider, string groupId, int groupPriority, bool cacheProvider = false)
        {
            if (cacheProvider && s_GroupProviders.TryGetValue(groupId, out var groupProvider))
                return groupProvider;

            groupProvider = new SearchProvider($"_group_provider_{groupId}", groupId)
            {
                type = templateProvider.id,
                priority = groupPriority,
                isExplicitProvider = true,
                actions = templateProvider.actions,
                showDetails = templateProvider.showDetails,
                showDetailsOptions = templateProvider.showDetailsOptions,
                fetchDescription = templateProvider.fetchDescription,
                fetchItems = templateProvider.fetchItems,
                fetchLabel = templateProvider.fetchLabel,
                fetchPreview = templateProvider.fetchPreview,
                fetchThumbnail = templateProvider.fetchThumbnail,
                startDrag = templateProvider.startDrag,
                toObject = templateProvider.toObject,
                trackSelection = templateProvider.trackSelection
            };

            if (cacheProvider)
                s_GroupProviders[groupId] = groupProvider;

            return groupProvider;
        }
    }
}
