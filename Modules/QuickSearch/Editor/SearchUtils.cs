// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Pool;
using UnityEditor.Search.Providers;
using UnityEditor.Utils;
using UnityEngine.Search;

using UnityEditor.SceneManagement;

namespace UnityEditor.Search
{
    /// <summary>
    /// Utilities used by multiple components of QuickSearch.
    /// </summary>
    public static class SearchUtils
    {
        private static readonly string[] k_Dots = { ".", "..", "..." };
        internal static readonly char[] KeywordsValueDelimiters = new[] { ':', '=', '<', '>', '!', '|' };
        private static readonly char[] k_AdbInvalidCharacters = {'/', '?', '<', '>', '\\', ':', '*', '|', '"' };

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

        public static Texture2D GetSceneObjectPreview(GameObject obj, Vector2 size, FetchPreviewOptions options, Texture2D thumbnail)
        {
            return Utils.GetSceneObjectPreview(null, obj, size, options, thumbnail);
        }

        public static Texture2D GetSceneObjectPreview(SearchContext ctx, GameObject obj, Vector2 size, FetchPreviewOptions options, Texture2D thumbnail)
        {
            return GetSceneObjectPreview(null, obj, size, options, thumbnail);
        }

        /// <summary>
        /// Tokenize a string each Capital letter.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        static readonly Regex s_CamelCaseSplit = new Regex(@"(?<!^)(?=[A-Z])", RegexOptions.Compiled);
        public static string[] SplitCamelCase(string source)
        {
            return s_CamelCaseSplit.Split(source);
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

        internal static string LowercaseFirst(string s)
        {
            // Check for empty string.
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            // Return char and concat substring.
            return char.ToLower(s[0]) + s.Substring(1);
        }

        internal static string ToPascalWithSpaces(string s, bool uppercaseFirstWordOnly = false)
        {
            // Check for empty string.
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }

            var tokens = Regex.Split(s, @"-+|_+|\s+|(?<!^)(?=[A-Z0-9])")
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select((word, index) =>
                {
                    if (uppercaseFirstWordOnly && index != 0)
                        return LowercaseFirst(word);
                    return UppercaseFirst(word);
                });
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
            return new[] { fcc }.Concat(scc.Where(s => s.Length > 1))
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
        public static IEnumerable<string> SplitFileEntryComponents(string path, in char[] entrySeparators)
        {
            return SplitFileEntryComponents(path, entrySeparators, 1);
        }

        public static IEnumerable<string> SplitFileEntryComponents(string path, in char[] entrySeparators, int minTokenLength)
        {
            path = Utils.RemoveInvalidCharsFromPath(path, '_');
            var name = Path.GetFileNameWithoutExtension(path);
            var nameTokens = name.Split(entrySeparators).Distinct().ToArray();
            var scc = nameTokens.SelectMany(s => SplitCamelCase(s)).Where(s => s.Length > 0).ToArray();
            var fcc = scc.Aggregate("", (current, s) => current + s[0]);
            return new[] { Path.GetExtension(path).Replace(".", "") }
                .Concat(scc)
                .Concat(FindShiftLeftVariations(fcc))
                .Concat(nameTokens)
                .Concat(path.Split(entrySeparators).Reverse())
                .Where(s => s.Length >= minTokenLength)
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
            if (!obj)
                return string.Empty;
            if (obj is Component c)
                return GetTransformPath(c.gameObject.transform);
            var assetPath = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(assetPath))
            {
                if (Utils.IsBuiltInResource(assetPath))
                    return GlobalObjectId.GetGlobalObjectIdSlow(obj).ToString();
                return assetPath;
            }
            if (obj is GameObject go)
                return GetTransformPath(go.transform);
            return obj.name;
        }

        static ulong GetStableHash(in UnityEngine.Object obj, in ulong assetHash = 0)
        {
            var fileIdHint = Utils.GetFileIDHint(obj);
            if (fileIdHint == 0)
                fileIdHint = (ulong)obj.GetInstanceID();
            return fileIdHint * 1181783497276652981UL + assetHash;
        }

        /// <summary>
        /// Return a unique document key owning the object
        /// </summary>
        internal static ulong GetDocumentKey(in UnityEngine.Object obj)
        {
            if (!obj)
                return ulong.MaxValue;
            if (obj is GameObject go)
                return GetStableHash(go, (ulong)(GetHierarchyAssetPath(go)?.GetHashCode() ?? 0));
            if (obj is Component c)
                return GetDocumentKey(c.gameObject);
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

            return SceneModeUtility.GetObjects(goRoots.ToArray(), true);
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

            void AddScene(Scene scene, List<Scene> outScenes)
            {
                if (!scene.IsValid() || !scene.isLoaded)
                    return;
                outScenes.Add(scene);
            }

            using var scenesPoolHandle = ListPool<Scene>.Get(out var scenes);
            var stage = StageUtility.GetCurrentStage();
            if (stage is not MainStage)
            {
                for (int i = 0, c = stage.sceneCount; i < c; ++i)
                {
                    var scene = stage.GetSceneAt(i);
                    AddScene(scene, scenes);
                }
            }
            else
            {
                for (int i = 0; i < SceneManager.sceneCount; ++i)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    AddScene(scene, scenes);
                }

                if (EditorApplication.isPlaying)
                    AddScene(EditorSceneManager.GetDontDestroyOnLoadScene(), scenes);
            }

            using var gameObjectRootPoolHandle = ListPool<UnityEngine.Object>.Get(out var goRoots);
            using var sceneRootGameObjectPoolHandle = ListPool<GameObject>.Get(out var sceneRootObjects);
            foreach (var scene in scenes)
            {
                scene.GetRootGameObjects(sceneRootObjects);
                if (sceneRootObjects.Count > 0)
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
        public static SearchProvider CreateGroupProvider(SearchProvider templateProvider, string groupId, int groupPriority, bool cacheProvider = false)
        {
            if (cacheProvider && s_GroupProviders.TryGetValue(groupId, out var groupProvider))
                return groupProvider;

            groupProvider = new SearchProvider($"_group_provider_{groupId}", groupId, templateProvider, groupPriority);

            if (cacheProvider)
                s_GroupProviders[groupId] = groupProvider;

            return groupProvider;
        }

        public static string GetAssetPath(in SearchItem item)
        {
            if (item.provider.type == Providers.AssetProvider.type)
                return Providers.AssetProvider.GetAssetPath(item);
            if (item.provider.type == "dep")
                return AssetDatabase.GUIDToAssetPath(item.id);
            return null;
        }

        static Dictionary<Type, List<Type>> s_BaseTypes = new Dictionary<Type, List<Type>>();
        internal static IEnumerable<SearchProposition> FetchTypePropositions<T>(string category = "Types", Type blockType = null, int priority = -1444) where T : UnityEngine.Object
        {
            if (category != null)
            {
                yield return new SearchProposition(category: category, label: "Prefabs", replacement: "t:prefab",
                    icon: GetTypeIcon(typeof(GameObject)), data: typeof(GameObject), type: blockType, priority: priority, color: QueryColors.type);
            }

            if (string.Equals(category, "Types", StringComparison.Ordinal))
            {
                yield return new SearchProposition(category: "Types", label: "Scripts", replacement: "t:script",
                    icon: GetTypeIcon(typeof(MonoScript)), data: typeof(MonoScript), type: blockType, priority: priority, color: QueryColors.type);
                yield return new SearchProposition(category: "Types", label: "Scenes", replacement: "t:scene",
                    icon: GetTypeIcon(typeof(SceneAsset)), data: typeof(SceneAsset), type: blockType, priority: priority, color: QueryColors.type);
                yield return new SearchProposition(category: "Types", label: "Presets", replacement: "t:preset",
                    icon: GetTypeIcon(typeof(Presets.Preset)), data: typeof(Presets.Preset), type: blockType, priority: priority, color: QueryColors.type);
            }

            if (!s_BaseTypes.TryGetValue(typeof(T), out var types))
            {
                var ignoredAssemblies = new[]
                {
                    typeof(EditorApplication).Assembly,
                    typeof(UnityEditorInternal.InternalEditorUtility).Assembly
                };
                types = TypeCache.GetTypesDerivedFrom<T>()
                .Where(t => !t.IsGenericType)
                .Where(t => !ignoredAssemblies.Contains(t.Assembly))
                .Where(t => !typeof(Editor).IsAssignableFrom(t))
                .Where(t => !typeof(EditorWindow).IsAssignableFrom(t))
                .Where(t => t.Assembly.GetName().Name.IndexOf("Editor", StringComparison.Ordinal) == -1).ToList();
                s_BaseTypes[typeof(T)] = types;
            }
            foreach (var t in types)
            {
                yield return new SearchProposition(
                    priority: t.Name[0] + priority,
                    category: category,
                    label: t.Name,
                    replacement: $"t:{t.Name}",
                    data: t,
                    help: $"Search {t.Name}",
                    type: blockType,
                    icon: GetTypeIcon(t),
                    color: QueryColors.type);
            }
        }

        internal static SearchProposition CreateKeywordProposition(in string keyword)
        {
            if (keyword.IndexOf('|') == -1)
                return SearchProposition.invalid;

            var tokens = keyword.Split('|');
            if (tokens.Length != 5)
                return SearchProposition.invalid;

            // <0:fieldname>:|<1:display name>|<2:help text>|<3:property type>|<4: owner type string>
            var valueType = tokens[3];
            var replacement = ParseBlockContent(valueType, tokens[0], out Type blockType);
            var ownerType = FindType<UnityEngine.Object>(tokens[4]);
            if (ownerType == null)
                return SearchProposition.invalid;
            return new SearchProposition(
                priority: (ownerType.Name[0] << 4) + tokens[1][0],
                category: $"Properties/{ObjectNames.NicifyVariableName(ownerType.Name)}",
                label: $"{tokens[1]} ({blockType?.Name ?? valueType})",
                replacement: replacement,
                help: tokens[2],
                color: replacement.StartsWith("#", StringComparison.Ordinal) ? QueryColors.property : QueryColors.filter,
                icon:
                    AssetPreview.GetMiniTypeThumbnailFromType(blockType) ??
                    GetTypeIcon(ownerType));
        }

        internal static IEnumerable<SearchProposition> FetchEnumPropositions<T>(string category = null, string replacementId = null, string replacementOp = null, Type blockType = null, int priority = 0, Texture2D icon = null, Color color = default) where T : Enum
        {
            var type = typeof(T);
            return FetchEnumPropositions(type, category, replacementId, replacementOp, blockType, priority, icon, color);
        }

        internal static IEnumerable<SearchProposition> FetchEnumPropositions(Type enumType, string category = null, string replacementId = null, string replacementOp = null, Type blockType = null, int priority = 0, Texture2D icon = null, Color color = default)
        {
            if (!enumType?.IsEnum ?? true)
                throw new ArgumentException("Type should of an enum.", nameof(enumType));

            if (blockType == null)
                blockType = typeof(QueryFilterBlock);

            var replacementBase = $"{replacementId}{replacementOp}";

            var enumNames = Enum.GetNames(enumType).Select(n => Utils.FastToLower(n)).ToList();
            var fields = enumType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
            foreach (var fieldInfo in fields)
            {
                if (fieldInfo.FieldType != enumType)
                    continue;

                var enumName = fieldInfo.Name;
                var label = ToPascalWithSpaces(enumName, true);
                var data = Utils.FastToLower(enumName);
                var replacement = label;
                if (blockType == typeof(QueryFilterBlock))
                {
                    replacement = $"{replacementBase}<$enum:{enumName},{enumType.FullName}$>";
                }
                else if (blockType == typeof(QueryListMarkerBlock))
                {
                    replacement = $"{replacementBase}{GetListMarkerReplacementText(data, enumNames, Utils.GetIconSkinAgnosticName(icon), color)}";
                    data = replacement;
                }

                string help = null;
                var descriptionAttribute = fieldInfo.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
                if (descriptionAttribute != null)
                    help = descriptionAttribute.Description;

                yield return new SearchProposition(category: category, label: label, replacement: replacement, help: help,
                    data: data, priority: priority, icon: icon, type: blockType, color: color);
            }
        }

        static Dictionary<Type, Texture2D> s_TypeIcons = new Dictionary<Type, Texture2D>();
        public static Texture2D GetTypeIcon(in Type type)
        {
            if (s_TypeIcons.TryGetValue(type, out var t) && t)
                return t;
            if (!type.IsAbstract && typeof(MonoBehaviour) != type && typeof(MonoBehaviour).IsAssignableFrom(type))
            {
                var script = EditorGUIUtility.GetScript(type.Name);
                if (!script)
                    return s_TypeIcons[type] = AssetPreview.GetMiniTypeThumbnail(type) ?? AssetPreview.GetMiniTypeThumbnail(typeof(DefaultAsset));

                var obj = EditorUtility.InstanceIDToObject(script.GetInstanceID());
                var customIcon = AssetPreview.GetMiniThumbnail(obj);
                return s_TypeIcons[type] = customIcon;
            }
            return s_TypeIcons[type] = AssetPreview.GetMiniTypeThumbnail(type) ?? AssetPreview.GetMiniTypeThumbnail(typeof(MonoScript));
        }

        internal static IEnumerable<SearchProposition> EnumeratePropertyPropositions(IEnumerable<UnityEngine.Object> objs)
        {
            return EnumeratePropertyKeywords(objs).Select(k => CreateKeywordProposition(k));
        }

        internal static void IterateSupportedProperties(SerializedObject so, Action<SerializedProperty> handler)
        {
            var p = so.GetIterator();
            var next = p.NextVisible(true);
            while (next)
            {
                var supported = SearchUtils.IsPropertyTypeSupported(p);
                if (supported)
                {
                    handler(p);
                }
                next = p.NextVisible(IterateSupportedProperties_EnterChildren(p));
            }
        }

        internal static bool IterateSupportedProperties_EnterChildren(SerializedProperty p)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.AnimationCurve:
                case SerializedPropertyType.Bounds:
                case SerializedPropertyType.Gradient:
                case SerializedPropertyType.Vector3Int:
                case SerializedPropertyType.Vector2Int:
                case SerializedPropertyType.Vector2:
                case SerializedPropertyType.Vector3:
                case SerializedPropertyType.Vector4:
                case SerializedPropertyType.Quaternion:
                    return false;
            }

            if (p.propertyType == SerializedPropertyType.Generic)
            {
                if (string.Equals(p.type, "map", StringComparison.Ordinal))
                    return false;
                if (string.Equals(p.type, "Matrix4x4f", StringComparison.Ordinal))
                    return false;
            }

            return (p.propertyType == SerializedPropertyType.String || !p.isArray) && !p.isFixedBuffer && p.propertyPath.LastIndexOf('[') == -1;
        }

        internal static IEnumerable<string> EnumeratePropertyKeywords(IEnumerable<UnityEngine.Object> objs)
        {
            var templates = GetTemplates(objs);
            foreach (var obj in templates)
            {
                var objType = obj.GetType();
                var isAsset = !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(obj));
                using (var so = new SerializedObject(obj))
                {
                    var p = so.GetIterator();
                    var next = p.NextVisible(true);
                    while (next)
                    {
                        var supported = SearchUtils.IsPropertyTypeSupported(p);
                        var propertyType = GetPropertyManagedTypeString(p);
                        if (propertyType != null)
                        {
                            var keyword = CreateKeyword(p, propertyType);
                            yield return keyword;
                        }
                        next = p.NextVisible(IterateSupportedProperties_EnterChildren(p));
                    }
                }
            }
        }

        private static string CreateKeyword(in SerializedProperty p, in string propertyType)
        {
            var path = p.propertyPath;
            if (path.IndexOf(' ') != -1)
                path = p.name;
            return $"#{path.Replace(" ", "")}|{p.displayName}|{p.tooltip}|{propertyType}|{p.serializedObject?.targetObject?.GetType().AssemblyQualifiedName}";
        }

        internal static string GetPropertyManagedTypeString(in SerializedProperty p)
        {
            Type managedType;
            switch (p.propertyType)
            {
                case SerializedPropertyType.Vector2:
                case SerializedPropertyType.Vector3:
                case SerializedPropertyType.Vector4:
                case SerializedPropertyType.Boolean:
                case SerializedPropertyType.String:
                    return p.propertyType.ToString();

                case SerializedPropertyType.Integer:
                    managedType = p.GetManagedType();
                    if (managedType != null && !managedType.IsPrimitive)
                        return managedType.AssemblyQualifiedName;
                    return "Number";

                case SerializedPropertyType.Character:
                case SerializedPropertyType.ArraySize:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.RenderingLayerMask:
                case SerializedPropertyType.Float:
                    return "Number";

                case SerializedPropertyType.Generic:
                    if (p.isArray)
                        return "Count";
                    return null;

                case SerializedPropertyType.ObjectReference:
                    if (p.objectReferenceValue)
                        return p.objectReferenceValue.GetType().AssemblyQualifiedName;
                    if (p.type.StartsWith("PPtr<", StringComparison.Ordinal) && TryFindType<UnityEngine.Object>(p.type.Substring(5, p.type.Length - 6), out managedType))
                        return managedType.AssemblyQualifiedName;
                    managedType = p.GetManagedType();
                    if (managedType != null && !managedType.IsPrimitive)
                        return managedType.AssemblyQualifiedName;
                    return null;
            }

            if (p.isArray)
                return "Count";

            managedType = p.GetManagedType();
            if (managedType != null && !managedType.IsPrimitive)
                return managedType.AssemblyQualifiedName;

            return p.propertyType.ToString();
        }

        internal static bool IsPropertyTypeSupported(SerializedProperty p)
        {
            var isAsset = !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(p.serializedObject.targetObject));
            return IsPropertyTypeSupported(p, isAsset);
        }

        internal static bool IsPropertyTypeSupported(SerializedProperty p, bool isOwnerAsset)
        {
            var isSupported = isOwnerAsset ? ObjectIndexer.IsIndexableProperty(p.propertyType) : SearchValue.IsSearchableProperty(p.propertyType);
            return isSupported && (p.propertyType == SerializedPropertyType.String || !p.isArray) && !p.isFixedBuffer && p.propertyPath.LastIndexOf('[') == -1;
        }

        internal static IEnumerable<UnityEngine.Object> GetTemplates(IEnumerable<UnityEngine.Object> objects)
        {
            var seenTypes = new HashSet<Type>();
            foreach (var obj in objects)
            {
                if (!obj)
                    continue;
                var ct = obj.GetType();
                if (!seenTypes.Contains(ct))
                {
                    seenTypes.Add(ct);
                    yield return obj;
                }

                if (obj is GameObject go)
                {
                    foreach (var comp in go.GetComponents<Component>())
                    {
                        if (!comp)
                            continue;
                        ct = comp.GetType();
                        if (!seenTypes.Contains(ct))
                        {
                            seenTypes.Add(ct);
                            yield return comp;
                        }
                    }
                }

                var path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path))
                {
                    var importer = AssetImporter.GetAtPath(path);
                    if (importer)
                    {
                        var it = importer.GetType();
                        if (it != typeof(AssetImporter) && !seenTypes.Contains(it))
                        {
                            seenTypes.Add(it);
                            yield return importer;
                        }
                    }
                }
            }
        }

        internal static string ParseSearchText(string searchText, IEnumerable<SearchProvider> providers, out SearchProvider filteredProvider)
        {
            filteredProvider = null;
            var searchQuery = searchText.TrimStart();
            if (string.IsNullOrEmpty(searchQuery))
                return searchQuery;

            foreach (var p in providers)
            {
                if (searchQuery.StartsWith(p.filterId, StringComparison.OrdinalIgnoreCase))
                {
                    filteredProvider = p;
                    searchQuery = searchQuery.Remove(0, p.filterId.Length).TrimStart();
                    break;
                }
            }
            return searchQuery;
        }

        static string ParseBlockContent(string type, in string content, out Type valueType)
        {
            var replacement = content;
            var del = content.LastIndexOf(':');
            if (del != -1)
                replacement = content.Substring(0, del);

            valueType = Type.GetType(type);
            type = valueType?.Name ?? type;

            if (QueryListBlockAttribute.TryGetReplacement(replacement.ToLower(), type, ref valueType, out var replacementText))
                return replacementText;

            switch (type)
            {
                case "Enum":
                    return $"{replacement}=0";
                case "String":
                    return $"{replacement}:\"\"";
                case "Boolean":
                    return $"{replacement}=true";
                case "Array":
                case "Count":
                    return $"{replacement}>=1";
                case "Integer":
                case "Float":
                case "Number":
                    return $"{replacement}>0";
                case "Color":
                    return $"{replacement}=#00ff00";
                case "Vector2":
                    return $"{replacement}=(,)";
                case "Vector3":
                case "Quaternion":
                    return $"{replacement}=(,,)";
                case "Vector4":
                    return $"{replacement}=(,,,)";

                default:
                    if (valueType != null)
                    {
                        if (typeof(UnityEngine.Object).IsAssignableFrom(valueType))
                            return $"{replacement}=<$object:none,{valueType.FullName}$>";
                        if (valueType.IsEnum)
                        {
                            var enums = valueType.GetEnumValues();
                            if (enums.Length > 0)
                                return $"{replacement}=<$enum:{enums.GetValue(0)},{valueType.FullName}$>";
                        }
                    }
                    break;
            }

            return replacement;
        }

        internal static bool TryFindType<T>(in string typeString, out Type type)
        {
            type = FindType<T>(typeString);
            return type != null;
        }

        static Dictionary<string, Type> s_CachedTypes = new Dictionary<string, Type>();
        internal static Type FindType<T>(in string typeString)
        {
            if (s_CachedTypes.TryGetValue(typeString, out var foundType))
                return foundType;

            var selfType = typeof(T);
            if (string.Equals(selfType.Name, typeString, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(selfType.FullName, typeString, StringComparison.Ordinal))
                return s_CachedTypes[typeString] = selfType;

            var type = Type.GetType(typeString);
            if (type != null)
                return s_CachedTypes[typeString] = type;
            foreach (var t in TypeCache.GetTypesDerivedFrom<T>())
            {
                if (!t.IsVisible)
                    continue;
                if (t.GetAttribute<ObsoleteAttribute>() != null)
                    continue;
                if (string.Equals(t.Name, typeString, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(t.FullName, typeString, StringComparison.Ordinal))
                {
                    return s_CachedTypes[typeString] = t;
                }
            }
            return s_CachedTypes[typeString] = null;
        }

        internal static IEnumerable<Type> FindTypes<T>(string typeString)
        {
            foreach (var t in TypeCache.GetTypesDerivedFrom<T>())
            {
                if (!t.IsVisible)
                    continue;
                if (t.GetAttribute<ObsoleteAttribute>() != null)
                    continue;
                if (string.Equals(t.Name, typeString, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(t.FullName, typeString, StringComparison.Ordinal))
                {
                    yield return t;
                }
            }
        }

        internal static string GetListMarkerReplacementText(string currentValue, IEnumerable<string> choices, string iconName = null, Color? color = null)
        {
            var sb = new StringBuilder($"<$list:{currentValue}, [{string.Join(", ", choices)}]");
            if (!string.IsNullOrEmpty(iconName))
                sb.Append($", {iconName}");
            if (color.HasValue)
                sb.Append($", #{ColorUtility.ToHtmlStringRGBA(color.Value)}");

            sb.Append("$>");

            return sb.ToString();
        }

        public static ISearchQuery CreateQuery(in string name, SearchContext context, SearchTable tableConfig)
        {
            return new SearchQuery()
            {
                name = name,
                viewState = new SearchViewState(context),
                displayName = name
            };
        }

        public static ISearchQuery FindQuery(string guid)
        {
            return SearchQuery.searchQueries.FirstOrDefault(sq => sq.guid == guid);
        }

        public static ISearchView OpenQuery(ISearchQuery sq, SearchFlags flags)
        {
            return SearchQuery.Open(sq, flags);
        }

        public static IEnumerable<ISearchQuery> EnumerateAllQueries()
        {
            return SearchQuery.searchQueries.Cast<ISearchQuery>().Concat(SearchQueryAsset.savedQueries);
        }

        public static SearchItem CreateSceneResult(SearchContext context, SearchProvider sceneProvider, GameObject go)
        {
            return Providers.SceneProvider.AddResult(context, sceneProvider, go);
        }

        public static void ShowIconPicker(Action<Texture2D, bool> iconSelectedHandler)
        {
            var pickIconContext = SearchService.CreateContext(new[] { "adb", "asset" }, "", SearchFlags.WantsMore);
            var viewState = SearchViewState.CreatePickerState("Icon", pickIconContext,
                (newIcon, canceled) => iconSelectedHandler(newIcon as Texture2D, canceled),
                null,
                "Texture",
                typeof(Texture2D));
            SearchService.ShowPicker(viewState);
        }

        public static void ShowColumnSelector(Action<IEnumerable<SearchColumn>, int> columnsAddedHandler, IEnumerable<SearchColumn> columns, Vector2 mousePosition, int activeColumnIndex)
        {
            ColumnSelector.AddColumns(columnsAddedHandler, columns, mousePosition, activeColumnIndex);
        }

        [Obsolete("IMGUI support has been removed", error: false)] // 2023.1
        public static EditorWindow ShowColumnEditor(IMGUI.Controls.MultiColumnHeaderState.Column column, Action<IMGUI.Controls.MultiColumnHeaderState.Column> editHandler)
        {
            throw new NotSupportedException("Search IMGUI support has been removed");
        }

        public static bool TryParse<T>(string expression, out T result)
        {
            return Utils.TryParse<T>(expression, out result);
        }

        public static string FormatCount(ulong count)
        {
            return Utils.FormatCount(count);
        }

        public static string FormatBytes(long byteCount)
        {
            return Utils.FormatBytes(byteCount);
        }

        public static int GetMainAssetInstanceID(string assetPath)
        {
            return Utils.GetMainAssetInstanceID(assetPath);
        }

        public static void PingAsset(string assetPath)
        {
            Utils.PingAsset(assetPath);
        }

        public static void StartDrag(UnityEngine.Object[] objects, string label = null)
        {
            Utils.StartDrag(objects, label);
        }

        public static void StartDrag(UnityEngine.Object[] objects, string[] paths, string label = null)
        {
            Utils.StartDrag(objects, paths, label);
        }

        public static Rect GetMainWindowCenteredPosition(Vector2 size)
        {
            return Utils.GetMainWindowCenteredPosition(size);
        }

        public static Texture2D GetAssetThumbnailFromPath(SearchContext context, string path)
        {
            return Utils.GetAssetThumbnailFromPath(context, path);
        }

        public static Texture2D GetAssetThumbnailFromPath(string path)
        {
            return GetAssetThumbnailFromPath(null, path);
        }

        public static Texture2D GetAssetPreviewFromPath(string path, FetchPreviewOptions previewOptions)
        {
            return GetAssetPreviewFromPath(null, path, previewOptions);
        }

        public static Texture2D GetAssetPreviewFromPath(SearchContext context, string path, FetchPreviewOptions previewOptions, Vector2 previewSize = new())
        {
            return Utils.GetAssetPreviewFromPath(context, path, previewSize == Vector2.zero ? new Vector2(128, 128) : previewSize, previewOptions);
        }

        public static Texture2D GetAssetPreviewFromPath(string path, Vector2 previewSize, FetchPreviewOptions previewOptions)
        {
            return GetAssetPreviewFromPath(null, path, previewOptions, previewSize);
        }

        public static void FrameAssetFromPath(string path)
        {
            Utils.FrameAssetFromPath(path);
        }

        internal static void OpenPreferences()
        {
            SettingsService.OpenUserPreferences(SearchSettings.settingsPreferencesKey);
            SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.QuickSearchOpenPreferences);
        }

        internal static IEnumerable<SearchProvider> GetActiveProviders(IEnumerable<SearchProvider> providers)
        {
            return providers.Where(p => p.active);
        }

        internal static IEnumerable<SearchProvider> SortProvider(IEnumerable<SearchProvider> providers)
        {
            return providers.OrderBy(p => p.priority + (p.isExplicitProvider ? 100000 : 0));
        }

        internal static IEnumerable<SearchProvider> GetMergedProviders(IEnumerable<SearchProvider> initialProviders, IEnumerable<string> providerIds)
        {
            var providers = SearchService.GetProviders(providerIds);
            if (initialProviders == null)
                return providers;

            return initialProviders.Concat(providers).Distinct();
        }

        internal static bool SearchViewSyncEnabled(string groupId)
        {
            switch (groupId)
            {
                case "asset":
                    return UnityEditor.SearchService.ProjectSearch.HasEngineOverride();
                case "scene":
                    return UnityEditor.SearchService.SceneSearch.HasEngineOverride();
                default:
                    return false;
            }
        }

        internal static string FormatStatusMessage(SearchContext context, int totalCount)
        {
            var providers = context.providers.ToList();
            if (providers.Count == 0)
                return L10n.Tr("There is no activated search provider");

            var msg = "Searching ";
            if (providers.Count > 1)
                msg += Utils.FormatProviderList(providers.Where(p => !p.isExplicitProvider), showFetchTime: !context.searchInProgress);
            else
                msg += Utils.FormatProviderList(providers);

            if (totalCount > 0)
            {
                msg += $" and found <b>{totalCount}</b> result";
                if (totalCount > 1)
                    msg += "s";
                if (!context.searchInProgress)
                {
                    if (context.searchElapsedTime > 1.0)
                        msg += $" in {PrintTime(context.searchElapsedTime)}";
                }
                else
                    msg += " so far";
            }
            else if (!string.IsNullOrEmpty(context.searchQuery))
            {
                if (!context.searchInProgress)
                    msg += " and found nothing";
            }

            if (context.searchInProgress)
                msg += k_Dots[(int)EditorApplication.timeSinceStartup % k_Dots.Length];

            return msg;
        }

        private static string PrintTime(double timeMs)
        {
            if (timeMs >= 1000)
                return $"{Math.Round(timeMs / 1000.0)} seconds";
            return $"{Math.Round(timeMs)} ms";
        }

        internal static void SetupColumns(SearchContext context, SearchExpression expression)
        {
            if (context.searchView == null || context.searchView.displayMode != DisplayMode.Table)
                return;

            if (expression.evaluator.name == nameof(Evaluators.Select))
            {
                var selectors = expression.parameters.Skip(1).Where(e => Evaluators.IsSelectorLiteral(e));
                var tableViewFields = new List<SearchField>(selectors.Select(s => new SearchField(s.innerText.ToString(), s.alias.ToString())));
                context.searchView.SetupColumns(tableViewFields);
            }
        }

        internal static Texture2D GetIconFromDisplayMode(DisplayMode displayMode)
        {
            switch (displayMode)
            {
                case DisplayMode.Grid:
                    return EditorGUIUtility.LoadIconRequired("GridView");
                case DisplayMode.Table:
                    return EditorGUIUtility.LoadIconRequired("TableView");
                default:
                    return EditorGUIUtility.LoadIconRequired("ListView");
            }
        }

        internal static DisplayMode GetDisplayModeFromItemSize(float itemSize)
        {
            if (itemSize <= (int)DisplayMode.List)
                return DisplayMode.List;

            if (itemSize >= (int)DisplayMode.Table)
                return DisplayMode.Table;

            return DisplayMode.Grid;
        }

        internal static string CreateFindObjectReferenceQuery(UnityEngine.Object obj)
        {
            var objPath = GetObjectPath(obj);
            if (string.IsNullOrEmpty(objPath))
                return null;
            var query = $"ref=\"{objPath}\"";
            return query;
        }

        internal static string RemoveInvalidChars(string filename)
        {
            filename = string.Concat(filename.Split(Paths.invalidFilenameChars));
            if (filename.Length > 0 && !char.IsLetterOrDigit(filename[0]))
                filename = filename.Substring(1);
            return filename;
        }

        internal static bool ValidateAssetPath(ref string path, string requiredExtensionWithDot, out string errorMessage)
        {
            if (!Paths.IsValidAssetPath(path, requiredExtensionWithDot, out errorMessage))
            {
                errorMessage = $"Save Search Query has failed. {errorMessage}";
                return false;
            }
            path = Utils.CleanPath(path);
            var fileName = Path.GetFileName(path);

            // On Mac Path.GetInvalidFileNameChars() doesn't include <,> but these characters are invalid for ADB.
            if (fileName.IndexOfAny(k_AdbInvalidCharacters) >= 0)
            {
                errorMessage = $"Filename has invalid characters.";
                return false;
            }

            var directory = Utils.CleanPath(Path.GetDirectoryName(path));
            if (!System.IO.Directory.Exists(directory))
            {
                errorMessage = $"Directory does not exists {directory}";
                return false;
            }

            if (!Utils.IsPathUnderProject(path))
            {
                errorMessage = $"Path is not under the project or packages: {path}";
                return false;
            }

            path = Utils.GetPathUnderProject(path);

            return true;
        }

        [CommandHandler("OpenToFindReferenceOnObject")]
        internal static void OpenToFindReferenceOnObject(CommandExecuteContext c)
        {
            var obj = c.GetArgument<UnityEngine.Object>(0);
            if(obj == null)
                return;
            OpenToFindReferenceOnObject(obj);
        }

        internal static ISearchView OpenToFindReferenceOnObject(UnityEngine.Object obj)
        {
            var query = CreateFindObjectReferenceQuery(obj);
            if (string.IsNullOrEmpty(query))
                return OpenDefaultQuickSearch();

            return OpenWithProviders(query,
                new [] { Providers.AssetProvider.type, Providers.BuiltInSceneObjectsProvider.type },
                SearchFlags.Default,
                "Find References");
        }

        [CommandHandler("OpenToSearchByProperty")]
        internal static void OpenToSearchByProperty(CommandExecuteContext c)
        {
            var prop = c.GetArgument<SerializedProperty>(0);
            if (prop == null)
                return;
            OpenToSearchByProperty(prop);
        }

        [CommandHandler("IsPropertyValidForQuery")]
        internal static void IsPropertyValidForQuery(CommandExecuteContext c)
        {
            var prop = c.GetArgument<SerializedProperty>(0);
            if (prop == null)
            {
                c.result = false;
                return;
            }
            c.result = IsPropertyValidForQuery(prop) && FormatPropertyQuery(prop, out var _) != null;
        }

        internal static bool IsPropertyValidForQuery(SerializedProperty prop)
        {
            var valid = !(prop == null ||
                prop.serializedObject == null ||
                !prop.serializedObject.isValid ||
                !prop.serializedObject.targetObject ||
                prop.serializedObject.targetObject == null);
            return valid && IsPropertyTypeSupported(prop);
        }

        readonly static string[] kAsset_PropertyQueryProviders = new[] { Providers.AssetProvider.type };
        readonly static string[] kScene_PropertyQueryProviders = new[] { Providers.BuiltInSceneObjectsProvider.type };

        internal static ISearchView OpenToSearchByProperty(SerializedProperty prop)
        {
            if (!IsPropertyValidForQuery(prop))
                return OpenDefaultQuickSearch();

            var query = FormatPropertyQuery(prop, out var isAssetQuery);
            if (query == null)
                return OpenDefaultQuickSearch();

            return OpenWithProviders(query, isAssetQuery ? kAsset_PropertyQueryProviders : kScene_PropertyQueryProviders);
        }

        internal static string GetPropertyValueForQuery(SerializedProperty prop)
        {
            var value = PropertySelectors.GetSerializedPropertyValue(prop);
            switch(prop.propertyType)
            {
                case SerializedPropertyType.Color:
                    return $"#{ColorUtility.ToHtmlStringRGB((Color)value)}";
                case SerializedPropertyType.ObjectReference:
                case SerializedPropertyType.ManagedReference:
                case SerializedPropertyType.ExposedReference:
                    {
                        if (value == null)
                            return "none";

                        var path = GetObjectPath(value as UnityEngine.Object);
                        return $"\"{path}\"";
                    }
                case SerializedPropertyType.String:
                    return $"\"{value}\"";
                default:
                    return value == null ? null : value.ToString();
            }
        }

        internal static string GetPropertyOperator(SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Float:
                    return ":";
                default:
                    return "=";
            }
        }

        internal static string GetPropertyName(SerializedProperty prop, bool isAsset)
        {
            if (isAsset)
                return ObjectIndexer.GetFieldName(prop.displayName);
            return prop.propertyPath.Replace(" ", "").ToLowerInvariant();
        }

        internal static string FormatPropertyQuery(SerializedProperty prop, out bool isAssetQuery)
        {
            string query = null;
            isAssetQuery = false;
            if (!IsPropertyValidForQuery(prop))
                return query;

            var target = prop.serializedObject.targetObject;
            var assetPath = AssetDatabase.GetAssetPath(target);
            isAssetQuery = !string.IsNullOrEmpty(assetPath);
            var propertyName = GetPropertyName(prop, isAssetQuery);
            var propertyValue = GetPropertyValueForQuery(prop);
            var operatorStr = GetPropertyOperator(prop);
            var typeStr = $"t:{target.GetType().Name} ";
            var propertyQuery = $"{propertyName}{operatorStr}{propertyValue}";
            if (isAssetQuery)
            {
                // Format asset Query;
                return $"{AssetProvider.filterId}{typeStr}{propertyQuery}";
            }

            if (target is UnityEngine.GameObject || target is MonoBehaviour || target is Component)
            {
                // Format Hierarchy Query:
                return $"{BuiltInSceneObjectsProvider.filterId}{typeStr}#{propertyQuery}";
            }
            return null;
        }

        internal static SearchProvider[] GetProviderForContextualSearch(IEnumerable<SearchProvider> providers)
        {
            return providers.Where(p => p.isEnabledForContextualSearch?.Invoke() ?? false).ToArray();
        }

        internal static ISearchView OpenNewWindow()
        {
            var newContext = SearchService.CreateContext("", SearchFlags.OpenDefault);
            var viewState = new SearchViewState(newContext, SearchViewFlags.IgnoreSavedSearches);
            viewState.LoadDefaults();
            var window = SearchService.ShowWindow(viewState) as SearchWindow;
            SearchAnalytics.SendEvent(window.state.sessionId, SearchAnalytics.GenericEventType.QuickSearchOpen, "NewWindow");
            return window;
        }

        internal static ISearchView OpenDefaultQuickSearch()
        {
            var window = SearchWindow.Open(flags: SearchFlags.OpenGlobal);
            SearchAnalytics.SendEvent(window.state.sessionId, SearchAnalytics.GenericEventType.QuickSearchOpen, "Default");
            return window;
        }

        const SearchFlags kWithProviderDefaultFlags = SearchFlags.Multiselect | SearchFlags.Dockable;

        internal static ISearchView OpenWithProviders(params string[] providerIds)
        {
            return OpenWithProviders(null, providerIds);
        }

        [Obsolete("Use OpenWithProviders")]
        internal static ISearchView OpenWithContextualProvider(string searchQuery, string[] providerIds, SearchFlags flags = kWithProviderDefaultFlags, string topic = null, bool useExplicitProvidersAsNormalProviders = false)
        {
            return OpenWithProviders(searchQuery, providerIds, flags, topic, useExplicitProvidersAsNormalProviders);
        }

        internal static ISearchView OpenWithProviders(string searchQuery, string[] providerIds, SearchFlags flags = kWithProviderDefaultFlags, string topic = null, bool useExplicitProvidersAsNormalProviders = false)
        {
            var providers = SearchService.GetProviders(providerIds).ToArray();
            if (providers.Length != providerIds.Length)
                Debug.LogWarning($"Cannot find one of these search providers {string.Join(", ", providerIds)}");

            if (providers.Length == 0)
                return OpenDefaultQuickSearch();

            var context = SearchService.CreateContext(providers, searchQuery, flags);
            context.useExplicitProvidersAsNormalProviders = useExplicitProvidersAsNormalProviders;
            topic = topic ?? string.Join(", ", providers.Select(p => p.name.ToLower()));
            var qsWindow = SearchWindow.Create<SearchWindow>(context, topic, flags);

            var evt = SearchAnalytics.GenericEvent.Create(qsWindow.state.sessionId, SearchAnalytics.GenericEventType.QuickSearchOpen, "Contextual");
            evt.message = providers[0].id;
            if (providers.Length > 1)
                evt.description = providers[1].id;
            if (providers.Length > 2)
                evt.description = providers[2].id;
            if (providers.Length > 3)
                evt.stringPayload1 = providers[3].id;
            if (providers.Length > 4)
                evt.stringPayload1 = providers[4].id;

            SearchAnalytics.SendEvent(evt);

            return qsWindow.ShowWindow();
        }

        internal static ISearchView OpenFromContextWindow(EditorWindow window = null)
        {
            var query = "";
            var focusWindow = window ?? EditorWindow.focusedWindow;
            if (window != null)
            {
                window.Focus();
            }
            if (focusWindow is SearchableEditorWindow searchableWindow)
            {
                query = searchableWindow.m_SearchFilter;
            }
            else if (focusWindow is ISearchableContainer searchable)
            {
                query = searchable.searchText;
            }
            return OpenFromContextWindow(query, "Help/Search Contextual", ignoreSaveSearches: true, syncSearch: false);
        }

        internal static ISearchView OpenFromContextWindow(string query, string sourceContext, bool ignoreSaveSearches, bool syncSearch)
        {
            var contextualProviders = GetProviderForContextualSearch(SearchService.Providers);
            if (contextualProviders.Length == 0)
            {
                return OpenDefaultQuickSearch();
            }
            var flags = SearchFlags.OpenContextual | SearchFlags.ReuseExistingWindow;
            var context = SearchService.CreateContext(contextualProviders, query);
            context.options |= flags;
            var viewState = new SearchViewState(context) { title = null };
            viewState.LoadDefaults();
            viewState.ignoreSaveSearches = ignoreSaveSearches;
            var searchWindow = SearchWindow.Create(viewState) as SearchWindow;
            searchWindow.ShowWindow(flags: flags);
            searchWindow.SendEvent(SearchAnalytics.GenericEventType.QuickSearchJumpToSearch, searchWindow.currentGroup, sourceContext);
            ((ISearchView)searchWindow).syncSearch = syncSearch;
            return searchWindow;
        }
    }
}

