// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Search.Providers
{
    static class AssetProvider
    {
        enum IdentifierType { kNullIdentifier = 0, kImportedAsset = 1, kSceneObject = 2, kSourceAsset = 3, kBuiltInAsset = 4 };

        struct AssetMetaInfo
        {
            public readonly string path;
            private readonly string gidString;
            public readonly SearchDocumentFlags flags;

            private GlobalObjectId m_GID;
            public GlobalObjectId gid
            {
                get
                {
                    if (m_GID.assetGUID == default)
                    {
                        if (gidString != null && GlobalObjectId.TryParse(gidString, out m_GID))
                            return m_GID;

                        if (!string.IsNullOrEmpty(path))
                        {
                            m_GID = GetGID(path);
                            return m_GID;
                        }

                        throw new Exception($"Failed to resolve GID for {path}, {gidString}");
                    }

                    return m_GID;
                }
            }

            private string m_Source;
            public string source
            {
                get
                {
                    if (m_Source == null)
                        m_Source = AssetDatabase.GUIDToAssetPath(gid.assetGUID);
                    return m_Source;
                }
            }

            private bool m_HasType;
            private Type m_Type;
            public Type type
            {
                get
                {
                    if (!m_HasType)
                    {
                        if (source.EndsWith("prefab", StringComparison.OrdinalIgnoreCase))
                            m_Type = AssetDatabase.GetTypeFromPathAndFileID(source, (long)gid.targetObjectId);
                        else
                            m_Type = AssetDatabase.GetMainAssetTypeAtPath(source);
                        m_HasType = true;
                    }
                    return m_Type;
                }
            }

            private Object m_Object;
            public Object obj
            {
                get
                {
                    if (!m_Object)
                    {
                        m_Object = AssetDatabase.LoadMainAssetAtGUID(gid.assetGUID);
                    }
                    return m_Object;
                }
            }

            public AssetMetaInfo(string path, GlobalObjectId gid, SearchDocumentFlags flags)
            {
                this.path = path;
                gidString = null;
                m_GID = gid;
                m_Source = null;
                m_HasType = false;
                m_Type = null;
                m_Object = null;
                this.flags = flags;
            }

            public AssetMetaInfo(string path, string gid, SearchDocumentFlags flags)
            {
                this.path = path;
                this.gidString = gid;
                m_GID = default;
                m_Source = null;
                m_HasType = false;
                m_Type = null;
                m_Object = null;
                this.flags = flags;
            }
        }

        internal const string type = "asset";
        private const string displayName = "Project";

        internal static bool reloadAssetIndexes = true;
        private static List<SearchDatabase> m_AssetIndexes = null;
        private static List<SearchDatabase> assetIndexes
        {
            get
            {
                if (reloadAssetIndexes || m_AssetIndexes == null)
                {
                    SearchMonitor.contentRefreshed -= TrackAssetIndexChanges;
                    m_AssetIndexes = SearchDatabase.Enumerate().ToList();
                    reloadAssetIndexes = false;
                    SearchMonitor.contentRefreshed += TrackAssetIndexChanges;
                }
                return m_AssetIndexes;
            }
        }

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            return new SearchProvider(type, displayName)
            {
                priority = 25,
                filterId = "p:",
                showDetails = true,
                showDetailsOptions = ShowDetailsOptions.Default | ShowDetailsOptions.Inspector | ShowDetailsOptions.DefaultGroup,
                supportsSyncViewSearch = true,
                isEnabledForContextualSearch = () => Utils.IsFocusedWindowTypeName("ProjectBrowser"),
                toObject = (item, type) => GetObject(item, type),
                toKey = (item) => GetInfo(item).source.GetHashCode64(),
                fetchItems = (context, items, provider) => SearchAssets(context, provider),
                fetchLabel = (item, context) => FetchLabel(item),
                fetchDescription = (item, context) => FetchDescription(item),
                fetchThumbnail = (item, context) => FetchThumbnail(item),
                fetchPreview = (item, context, size, options) => FetchPreview(item, size, options),
                startDrag = (item, context) => StartDrag(item, context),
                trackSelection = (item, context) => EditorGUIUtility.PingObject(GetInstanceId(item)),
                fetchPropositions = (context, options) => FetchPropositions(context, options),
                fetchColumns = (context, items) => AssetSelectors.Enumerate(items)
            };
        }

        private static Texture2D FetchPreview(SearchItem item, Vector2 size, FetchPreviewOptions options)
        {
            var info = GetInfo(item);
            if (info.gid.assetGUID == default)
                return null;

            if (item.preview && item.preview.width >= size.x && item.preview.height >= size.y)
                return item.preview;

            if (info.gid.identifierType == (int)IdentifierType.kSceneObject)
                return AssetDatabase.GetCachedIcon(info.source) as Texture2D;

            var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(info.gid);
            if (obj is GameObject go)
                return (item.preview = Utils.GetSceneObjectPreview(go, size, options, item.thumbnail));
            else if (obj)
            {
                var p = AssetPreview.GetAssetPreview(obj);
                if (p)
                    return p;
            }

            return (item.preview = Utils.GetAssetPreviewFromPath(info.source, size, options));
        }

        private static Texture2D FetchThumbnail(in SearchItem item)
        {
            var info = GetInfo(item);

            if (item.thumbnail)
                return item.thumbnail;

            return AssetPreview.GetMiniTypeThumbnail(info.type ?? typeof(GameObject));
        }

        private static string TrimLabel(in string label, in bool trim)
        {
            if (!trim)
                return label;
            var dp = label.LastIndexOfAny(Path.GetInvalidFileNameChars());
            if (dp > 0)
                return label.Substring(dp);
            return label;
        }

        private static string FetchLabel(SearchItem item)
        {
            var info = GetInfo(item);
            var displayCompact = IsDisplayCompact(item);

            if (!string.IsNullOrEmpty(item.label))
                return item.label;

            if (info.flags.HasAny(SearchDocumentFlags.Object))
                return TrimLabel((item.label = info.path), displayCompact);
            return (item.label = Path.GetFileName(info.path));
        }

        private static string FetchDescription(SearchItem item)
        {
            var info = GetInfo(item);

            if (IsDisplayCompact(item))
                return info.path;

            if (!string.IsNullOrEmpty(item.description))
                return item.description;

            if (info.flags.HasAny(SearchDocumentFlags.Asset))
                return (item.description = GetAssetDescription(info.source) ?? info.path);
            return (item.description = $"Source: {GetAssetDescription(info.source) ?? info.path}");
        }

        static bool IsDisplayCompact(in SearchItem item)
        {
            if (item.options.HasAny(SearchItemOptions.Compacted))
                return true;
            return item.context?.searchView?.displayMode == DisplayMode.Grid;
        }

        private static AssetMetaInfo GetInfo(SearchItem item)
        {
            return (AssetMetaInfo)item.data;
        }

        private static GlobalObjectId GetGID(SearchItem item)
        {
            return GetInfo(item).gid;
        }

        private static int GetInstanceId(SearchItem item)
        {
            var gid = GetGID(item);
            return GlobalObjectId.GlobalObjectIdentifierToInstanceIDSlow(gid);
        }

        private static Object GetObject(SearchItem item)
        {
            return GetObject(item, typeof(UnityEngine.Object));
        }

        private static Object GetObject(SearchItem item, Type type)
        {
            var info = GetInfo(item);

            if (typeof(AssetImporter).IsAssignableFrom(type))
            {
                var importer = AssetImporter.GetAtPath(info.source);
                if (importer)
                    return importer;
            }

            if (info.flags.HasAny(SearchDocumentFlags.Asset))
            {
                var assetType = AssetDatabase.GetMainAssetTypeAtPath(info.source);
                if (!type.IsAssignableFrom(assetType))
                    return null;
                var obj = AssetDatabase.LoadAssetAtPath(info.source, type);
                if (obj)
                    return obj;
            }

            return ToObjectType(GlobalObjectId.GlobalObjectIdentifierToObjectSlow(info.gid), type);
        }

        private static Object ToObjectType(Object obj, Type type)
        {
            if (!obj)
                return null;

            if (type == null)
                return obj;
            var objType = obj.GetType();
            if (type.IsAssignableFrom(objType))
                return obj;

            if (obj is GameObject go && typeof(Component).IsAssignableFrom(type))
                return go.GetComponent(type);

            return null;
        }

        static char[] s_KeywordsValueDelimiters = new[] { ':', '=', '<', '>', '!' };
        private static IEnumerable<SearchProposition> FetchPropositions(SearchContext context, SearchPropositionOptions options)
        {
            if (context.options.HasAny(SearchFlags.NoIndexing))
                return null;

            var token = options.tokens[0];
            var ft = token.LastIndexOfAny(s_KeywordsValueDelimiters);
            if (ft >= 0)
                token = token.Substring(0, ft);
            return assetIndexes.SelectMany(db => db.index.GetKeywords()
                .Where(kw => kw.StartsWith(token, StringComparison.OrdinalIgnoreCase)))
                .Select(kw => new SearchProposition(kw));
        }

        private static IEnumerable<string> FilterIndexes(IEnumerable<string> paths)
        {
            return paths.Where(u => u.EndsWith(".index", StringComparison.OrdinalIgnoreCase));
        }

        private static void TrackAssetIndexChanges(string[] updated, string[] deleted, string[] moved)
        {
            var loaded = assetIndexes?.Where(db => db).Select(db => db.path).ToArray() ?? new string[0];
            if (FilterIndexes(updated).Except(loaded).Count() > 0 || loaded.Intersect(FilterIndexes(deleted)).Count() > 0)
                reloadAssetIndexes = true;

            if (deleted.Length > 0)
            {
                EditorApplication.delayCall -= SearchService.RefreshWindows;
                EditorApplication.delayCall += SearchService.RefreshWindows;
            }

            FindProvider.Update(updated, deleted, moved);
        }

        private static void StartDrag(SearchItem item, SearchContext context)
        {
            if (context.selection.Count > 1)
            {
                var selectedObjects = context.selection.Select(i => GetObject(i));
                var paths = context.selection.Select(i => GetAssetPath(i)).ToArray();
                Utils.StartDrag(selectedObjects.ToArray(), paths, item.GetLabel(context, true));
            }
            else
                Utils.StartDrag(new[] { GetObject(item) }, new[] { GetAssetPath(item) }, item.GetLabel(context, true));
        }

        private static AssetMetaInfo CreateMetaInfo(string path, string gid, SearchDocumentFlags flags)
        {
            return new AssetMetaInfo(path, gid, flags);
        }

        private static IEnumerator SearchAssets(SearchContext context, SearchProvider provider)
        {
            var searchQuery = context.searchQuery;
            var useIndexing = !context.options.HasAny(SearchFlags.NoIndexing) && assetIndexes.Count > 0;
            if (!string.IsNullOrEmpty(searchQuery))
            {
                // Search by GUID
                var guidPath = AssetDatabase.GUIDToAssetPath(searchQuery);
                if (!string.IsNullOrEmpty(guidPath))
                {
                    var info = new AssetMetaInfo(guidPath, GetGID(guidPath), SearchDocumentFlags.Asset);
                    yield return provider.CreateItem(context, info.gid.ToString(), -1, $"{Path.GetFileName(guidPath)} ({searchQuery})", null, null, info);
                }

                // Search indexes that are ready
                bool allIndexesReady = false;
                if (useIndexing)
                {
                    allIndexesReady = assetIndexes.All(db => db.ready);
                    if (allIndexesReady)
                    {
                        foreach (var db in assetIndexes)
                            yield return SearchIndexes(context.searchQuery, context, provider, db);
                    }
                }

                if (!useIndexing || !allIndexesReady || context.wantsMore)
                {
                    // Perform a quick search on asset paths
                    var findOptions = FindOptions.Words | FindOptions.Regex | FindOptions.Glob | (context.wantsMore ? FindOptions.Fuzzy : FindOptions.None);
                    foreach (var e in FindProvider.Search(context, provider, findOptions))
                        yield return CreateItem(context, provider, "Find", null, e.source, 998 + e.score, useGroupProvider: false, SearchDocumentFlags.Asset);
                }

                // Finally wait for indexes that are being built to end the search.
                if (useIndexing && !allIndexesReady && !context.options.HasAny(SearchFlags.Synchronous))
                {
                    foreach (var db in assetIndexes)
                        yield return SearchIndexes(context.searchQuery, context, provider, db);
                }
            }

            if (context.wantsMore && context.filterType != null)
                yield return SearchPickableAssets(context, provider);
        }

        private static IEnumerator SearchPickableAssets(SearchContext context, SearchProvider provider)
        {
            var filteredQuery = $"t={context.filterType.Name}";
            if (string.IsNullOrEmpty(context.searchQuery))
            {
                yield return AssetDatabase.FindAssets($"t:{context.filterType.Name}")
                    .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                    .Select(path => CreateItem(context, provider, "More", null, path, 999, useGroupProvider: false, SearchDocumentFlags.Asset));
            }
            else
            {
                filteredQuery = $"({context.searchText}) t={context.filterType.Name}";
            }

            if (assetIndexes != null)
            {
                foreach (var db in assetIndexes)
                    yield return SearchIndexes(filteredQuery, context, provider, db);
            }
        }

        public static GlobalObjectId GetGID(string assetPath)
        {
            return TaskEvaluatorManager.EvaluateMainThread(() =>
            {
                var assetInstanceId = Utils.GetMainAssetInstanceID(assetPath);
                return GlobalObjectId.GetGlobalObjectIdSlow(assetInstanceId);
            });
        }

        private static IEnumerator SearchIndexes(string searchQuery, SearchContext context, SearchProvider provider, SearchDatabase db)
        {
            while (!db.ready)
            {
                if (!db || context.options.HasAny(SearchFlags.Synchronous))
                    yield break;
                yield return null;
            }

            // Search index
            var index = db.index;
            var useGroupProvider = db.name.IndexOf("project", StringComparison.OrdinalIgnoreCase) == -1 &&
                db.name.IndexOf("assets", StringComparison.OrdinalIgnoreCase) == -1;
            yield return index.Search(searchQuery.ToLowerInvariant(), context, provider)
                .Select(e => CreateItem(context, provider, db, e, useGroupProvider));
        }

        private static SearchItem CreateItem(in SearchContext context, in SearchProvider provider, in SearchDatabase db, in SearchResult e, in bool useGroupProvider)
        {
            var score = e.score;
            var doc = db.index.GetDocument(e.index);
            var docPath = doc.m_Name ?? doc.m_Source;
            if (doc.m_Name != null)
                score <<= 2;
            if (!string.IsNullOrEmpty(docPath))
            {
                var sourceFilename = Path.GetFileName(docPath);
                foreach (var w in context.searchWords)
                {
                    if (sourceFilename.LastIndexOf(w, StringComparison.OrdinalIgnoreCase) != -1)
                        score = (score >> 1) + sourceFilename.Length - w.Length;
                }
            }
            return CreateItem(context, provider, db.name, doc.id, doc.name, score, useGroupProvider, doc.flags);
        }

        public static SearchItem CreateItem(
            in SearchContext context, in SearchProvider provider,
            in string dbName, in string gid, in string path, in int itemScore,
            in bool useGroupProvider, in SearchDocumentFlags flags)
        {
            string filename = null;
            if (context.options.HasAny(SearchFlags.Debug) && !string.IsNullOrEmpty(dbName))
            {
                filename = Path.GetFileName(path);
                filename += $" ({dbName}, {itemScore})";
            }

            var groupProvider = useGroupProvider ? SearchUtils.CreateGroupProvider(provider, GetProviderGroupName(dbName, path), provider.priority, cacheProvider: true) : provider;
            return groupProvider.CreateItem(context, gid ?? GetGID(path).ToString(), itemScore, filename, null, null, CreateMetaInfo(path, gid, flags));
        }

        private static string GetProviderGroupName(string dbName, string path)
        {
            if (string.IsNullOrEmpty(path))
                return dbName;
            if (path.StartsWith("Packages/", StringComparison.Ordinal))
                return "Packages";
            return dbName;
        }

        [SearchSelector("path", provider: type)]
        public static string GetAssetPath(SearchItem item)
        {
            var info = GetInfo(item);
            return info.source;
        }

        public static string GetAssetPath(string id)
        {
            if (GlobalObjectId.TryParse(id, out var gid))
                return AssetDatabase.GUIDToAssetPath(gid.assetGUID);
            return AssetDatabase.GUIDToAssetPath(id);
        }

        public static string GetAssetPath(SearchResult result)
        {
            return GetAssetPath(result.id);
        }

        public static string GetAssetPath(SearchDocument doc)
        {
            return GetAssetPath(doc.id);
        }

        private static string GetAssetDescription(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
                return assetPath;
            try
            {
                var fi = new FileInfo(assetPath);
                if (!fi.Exists)
                    return $"File <i>{assetPath}</i> does not exist anymore.";
                return $"{assetPath} ({EditorUtility.FormatBytes(fi.Length)})";
            }
            catch
            {
                return null;
            }
        }

        private static void ReimportAssets(IEnumerable<SearchItem> items)
        {
            const ImportAssetOptions reimportAssetOptions =
                ImportAssetOptions.ForceUpdate |
                ImportAssetOptions.ImportRecursive |
                ImportAssetOptions.DontDownloadFromCacheServer |
                (ImportAssetOptions)(1 << 7); // AssetDatabase::kMayCancelImport

            foreach (var searchItem in items)
                AssetDatabase.ImportAsset(GetAssetPath(searchItem), reimportAssetOptions);
        }

        #region DocumentationDeleteAssets
        private static void DeleteAssets(IEnumerable<SearchItem> items)
        {
            var oldSelection = Selection.objects;
            SearchUtils.SelectMultipleItems(items, pingSelection: false);
            // We call ProjectBrowser.DeleteSelectedAssets for the confirmation popup.
            ProjectBrowser.DeleteSelectedAssets(true);
            Selection.objects = oldSelection;
        }

        #endregion

        // We have our own OpenPropertyEditorsOnSelection so we don't have to worry about global selection
        private static void OpenPropertyEditorsOnSelection(IEnumerable<SearchItem> items)
        {
            var objs = items.Select(i => i.ToObject()).Where(o => o).ToArray();
            if (objs.Length == 1)
            {
                PropertyEditor.OpenPropertyEditor(objs[0]);
            }
            else
            {
                var firstPropertyEditor = PropertyEditor.OpenPropertyEditor(objs[0]);
                EditorApplication.delayCall += () =>
                {
                    var dock = firstPropertyEditor.m_Parent as DockArea;
                    if (dock == null)
                        return;
                    for (var i = 1; i < objs.Length; ++i)
                        dock.AddTab(PropertyEditor.OpenPropertyEditor(objs[i], false));
                };
            }
        }

        [SearchActionsProvider]
        internal static IEnumerable<SearchAction> CreateActionHandlers()
        {
            return new[]
            {
                new SearchAction(type, "select", null, "Select")
                {
                    handler = (item) => SelectItem(item),
                    execute = (items) => SearchUtils.SelectMultipleItems(items, focusProjectBrowser: true)
                },
                new SearchAction(type, "open", null, "Open", OpenItem),
                new SearchAction(type, "reimport", null, "Reimport", ReimportAssets),
                new SearchAction(type, "add_scene", null, "Add scene")
                {
                    // Only works in single selection and adds a scene to the current hierarchy.
                    enabled = (items) => CanAddScene(items),
                    handler = (item) => SceneManagement.EditorSceneManager.OpenScene(GetAssetPath(item), SceneManagement.OpenSceneMode.Additive)
                },
                new SearchAction(type, "reveal", null, Utils.GetRevealInFinderLabel(), item => EditorUtility.RevealInFinder(GetAssetPath(item))),
                new SearchAction(type, "delete", null, "Delete", DeleteAssets),
                new SearchAction(type, "copy_path", null, "Copy Path")
                {
                    enabled = items => items.Count == 1,
                    handler = item =>
                    {
                        var selectedPath = GetAssetPath(item);
                        Clipboard.stringValue = selectedPath;
                    }
                },
                new SearchAction(type, "properties", null, "Properties", OpenPropertyEditorsOnSelection)
            };
        }

        private static void SelectItem(SearchItem item)
        {
            var info = GetInfo(item);
            var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(info.gid);
            if (obj)
            {
                EditorApplication.delayCall += () =>
                {
                    if (!SelectObjectbyId(info.gid))
                    {
                        if (EditorUtility.DisplayDialog("Container scene is not opened", $"Do you want to open container scene {info.source}?", "Yes", "No"))
                            OpenItem(item);
                    }
                };
            }
            else
                Utils.FrameAssetFromPath(GetAssetPath(item));
            item.preview = null;
        }

        private static void OpenItem(SearchItem item)
        {
            var info = GetInfo(item);
            if (info.gid.identifierType == (int)IdentifierType.kSceneObject)
            {
                var containerAsset = AssetDatabase.LoadAssetAtPath<Object>(info.source);
                if (containerAsset != null)
                {
                    AssetDatabase.OpenAsset(containerAsset);
                    EditorApplication.delayCall += () => SelectObjectbyId(info.gid);
                }
            }

            var asset = GetObject(item);
            if (asset == null || !AssetDatabase.OpenAsset(asset))
                EditorUtility.OpenWithDefaultApp(GetAssetPath(item));
        }

        private static bool SelectObjectbyId(GlobalObjectId gid)
        {
            var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
            if (obj)
            {
                Utils.SelectObject(obj);
                return true;
            }
            return false;
        }

        private static bool CanAddScene(IReadOnlyCollection<SearchItem> items)
        {
            if (items.Count != 1)
                return false;
            var singleItem = items.Last();
            var info = GetInfo(singleItem);
            if (info.gid.identifierType != (int)IdentifierType.kImportedAsset)
                return false;
            return info.path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase);
        }

        [Shortcut("Help/Search/Assets")]
        internal static void PopQuickSearch()
        {
            QuickSearch.OpenWithContextualProvider(type, Query.type, FindProvider.providerId);
        }
    }
}
