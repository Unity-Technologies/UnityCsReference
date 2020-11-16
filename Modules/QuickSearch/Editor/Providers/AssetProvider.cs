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
        internal const string type = "asset";
        private const string displayName = "Project";
        private const int k_ExactMatchScore = -99;

        private static bool reloadAssetIndexes = true;
        private static List<SearchDatabase> m_AssetIndexes = null;
        private static List<SearchDatabase> assetIndexes
        {
            get
            {
                if (reloadAssetIndexes || m_AssetIndexes == null)
                {
                    AssetPostprocessorIndexer.contentRefreshed -= TrackAssetIndexChanges;
                    m_AssetIndexes = SearchDatabase.Enumerate("asset").ToList();
                    reloadAssetIndexes = false;
                    AssetPostprocessorIndexer.contentRefreshed += TrackAssetIndexChanges;
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
                showDetailsOptions = ShowDetailsOptions.Default | ShowDetailsOptions.Inspector,

                isEnabledForContextualSearch = () => Utils.IsFocusedWindowTypeName("ProjectBrowser"),
                toObject = (item, type) => AssetDatabase.LoadAssetAtPath(item.id, type),
                fetchItems = (context, items, provider) => SearchAssets(context, provider),
                fetchDescription = (item, context) => (item.description = GetAssetDescription(item.id)),
                fetchThumbnail = (item, context) => Utils.GetAssetThumbnailFromPath(item.id),
                fetchPreview = (item, context, size, options) => Utils.GetAssetPreviewFromPath(item.id, size, options),
                openContextual = (selection, rect) => OpenContextualMenu(selection, rect),
                startDrag = (item, context) => StartDrag(item, context),
                trackSelection = (item, context) => EditorGUIUtility.PingObject(AssetDatabase.GetMainAssetInstanceID(item.id)),
                fetchPropositions = (context, options) => FetchPropositions(context, options)
            };
        }

        private static IEnumerable<SearchProposition> FetchPropositions(SearchContext context, SearchPropositionOptions options)
        {
            if (context.options.HasFlag(SearchFlags.NoIndexing))
                return null;

            return assetIndexes.SelectMany(db => db.index.GetKeywords().Select(kw => new SearchProposition(kw)));
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

            if (deleted != null && deleted.Length > 0)
            {
                EditorApplication.delayCall -= SearchService.RefreshWindows;
                EditorApplication.delayCall += SearchService.RefreshWindows;
            }

            FindProvider.Update(updated, deleted, moved);
        }

        private static bool OpenContextualMenu(SearchSelection selection, Rect contextRect)
        {
            var old = Selection.instanceIDs;
            SearchUtils.SelectMultipleItems(selection);
            EditorUtility.DisplayPopupMenu(contextRect, "Assets/", null);
            EditorApplication.delayCall += () => EditorApplication.delayCall += () => Selection.instanceIDs = old;
            return true;
        }

        private static void StartDrag(SearchItem item, SearchContext context)
        {
            if (context.selection.Count > 1)
            {
                var selectedObjects = context.selection.Select(i => AssetDatabase.LoadAssetAtPath<Object>(i.id));
                var paths = context.selection.Select(i => i.id).ToArray();
                Utils.StartDrag(selectedObjects.ToArray(), paths, item.GetLabel(context, true));
            }
            else
                Utils.StartDrag(new[] { AssetDatabase.LoadAssetAtPath<Object>(item.id) }, new[] { item.id }, item.GetLabel(context, true));
        }

        private static IEnumerator SearchAssets(SearchContext context, SearchProvider provider)
        {
            var searchQuery = context.searchQuery;
            var useIndexing = !context.options.HasFlag(SearchFlags.NoIndexing);
            if (!string.IsNullOrEmpty(searchQuery))
            {
                // Search by GUID
                var guidPath = AssetDatabase.GUIDToAssetPath(searchQuery);
                if (!string.IsNullOrEmpty(guidPath))
                    yield return provider.CreateItem(context, guidPath, -1, $"{Path.GetFileName(guidPath)} ({searchQuery})", null, null, null);

                // Search indexes that are ready
                if (useIndexing)
                {
                    foreach (var db in assetIndexes.Where(db => db.ready))
                        yield return SearchIndexes(context.searchQuery, context, provider, db);
                }

                // Perform a quick search on asset paths
                var findOptions = FindOptions.Words | FindOptions.Regex | FindOptions.Glob | (context.wantsMore ? FindOptions.Fuzzy : FindOptions.None);
                foreach (var e in FindProvider.Search(context, findOptions))
                    yield return CreateItem(context, provider, "Find", e.path, 998 + e.score);

                // Finally wait for indexes that are being built to end the search.
                if (useIndexing && !context.options.HasFlag(SearchFlags.Synchronous))
                {
                    foreach (var db in assetIndexes.Where(db => !db.ready))
                        yield return SearchIndexes(context.searchQuery, context, provider, db);
                }
            }

            if (context.wantsMore && context.filterType != null && string.IsNullOrEmpty(searchQuery))
            {
                yield return AssetDatabase.FindAssets($"t:{context.filterType.Name}")
                    .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                    .Select(path => CreateItem(context, provider, "More", path, 999));

                if (assetIndexes != null)
                    yield return assetIndexes.Select(db => SearchIndexes($"has={context.filterType.Name}", context, provider, db));
            }
        }

        private static IEnumerator SearchIndexes(string searchQuery, SearchContext context, SearchProvider provider, SearchDatabase db)
        {
            while (!db.ready)
            {
                if (!db || context.options.HasFlag(SearchFlags.Synchronous))
                    yield break;
                yield return null;
            }

            // Search index
            var index = db.index;
            db.Report("Search", searchQuery);
            yield return index.Search(searchQuery.ToLowerInvariant())
                .Where(e => e.id != null)
                .Select(e => CreateItem(context, provider, db.name, e.id, e.score));
        }

        private static SearchItem CreateItem(SearchContext context, SearchProvider provider, string dbName, string assetPath, int itemScore)
        {
            var words = context.searchPhrase;
            var filenameNoExt = Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
            if (filenameNoExt.Equals(words, StringComparison.Ordinal))
                itemScore = k_ExactMatchScore;

            var filename = Path.GetFileName(assetPath);
            if (context.options.HasFlag(SearchFlags.Debug) && !string.IsNullOrEmpty(dbName))
                filename += $" ({dbName}, {itemScore})";
            return provider.CreateItem(context, assetPath, itemScore, filename, null, null, null);
        }

        internal static string GetAssetDescription(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
                return assetPath;
            var fi = new FileInfo(assetPath);
            if (!fi.Exists)
                return $"File <i>{assetPath}</i> does not exist anymore.";
            var fileSize = new FileInfo(assetPath).Length;
            return $"{assetPath} ({EditorUtility.FormatBytes(fileSize)})";
        }

        [SearchActionsProvider]
        internal static IEnumerable<SearchAction> CreateActionHandlers()
        {
            string k_RevealActionLabel = Application.platform != RuntimePlatform.OSXEditor ? "Show in Explorer" : "Reveal in Finder";

            return new[]
            {
                new SearchAction(type, "select", null, "Select")
                {
                    handler = (item) => Utils.FrameAssetFromPath(item.id),
                    execute = (items) => SearchUtils.SelectMultipleItems(items, focusProjectBrowser: true)
                },
                new SearchAction(type, "open", null, "Open")
                {
                    handler = (item) =>
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<Object>(item.id);
                        if (asset == null || !AssetDatabase.OpenAsset(asset))
                            EditorUtility.OpenWithDefaultApp(item.id);
                    }
                },
                new SearchAction(type, "add_scene", null, "Add scene")
                {
                    // Only works in single selection and adds a scene to the current hierarchy.
                    enabled = (items) => items.Count == 1 && items.Last().id.EndsWith(".unity", StringComparison.OrdinalIgnoreCase),
                    handler = (item) => UnityEditor.SceneManagement.EditorSceneManager.OpenScene(item.id, UnityEditor.SceneManagement.OpenSceneMode.Additive)
                },
                new SearchAction(type, "reveal", null, k_RevealActionLabel)
                {
                    handler = (item) => EditorUtility.RevealInFinder(item.id)
                }
            };
        }

        [Shortcut("Help/Search/Assets")]
        internal static void PopQuickSearch()
        {
            QuickSearch.OpenWithContextualProvider(type, Query.type, FindProvider.providerId);
        }
    }
}
