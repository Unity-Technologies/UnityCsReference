// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Search.Providers
{
    static class ObjectProvider
    {
        private const string type = "object";

        private static List<SearchDatabase> m_ObjectIndexes;
        private static List<SearchDatabase> indexes
        {
            get
            {
                if (m_ObjectIndexes == null)
                {
                    UpdateObjectIndexes();
                    AssetPostprocessorIndexer.contentRefreshed += TrackAssetIndexChanges;
                }

                return m_ObjectIndexes;
            }
        }

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            return new SearchProvider(type, "Objects")
            {
                priority = 55,
                filterId = "o:",
                isExplicitProvider = true,
                showDetails = true,
                showDetailsOptions = ShowDetailsOptions.Inspector | ShowDetailsOptions.Description | ShowDetailsOptions.Actions | ShowDetailsOptions.Preview,

                toObject = (item, type) => ToObject(item, type),
                fetchItems = (context, items, provider) => SearchObjects(context, provider),
                fetchLabel = (item, context) => FetchLabel(item),
                fetchDescription = (item, context) => FetchDescription(item),
                fetchThumbnail = (item, context) => FetchThumbnail(item),
                fetchPreview = (item, context, size, options) => FetchPreview(item, options),
                startDrag = (item, context) => StartDrag(item, context),
                trackSelection = (item, context) => TrackSelection(item)
            };
        }

        private static void UpdateObjectIndexes()
        {
            m_ObjectIndexes = SearchDatabase.Enumerate("scene", "prefab").ToList();
        }

        private static void TrackAssetIndexChanges(string[] updated, string[] deleted, string[] moved)
        {
            if (updated.Concat(deleted).Any(u => u.EndsWith(".index", StringComparison.OrdinalIgnoreCase)))
                UpdateObjectIndexes();
        }

        private static string FetchLabel(SearchItem item)
        {
            return (item.label = ((SearchDocument)item.data).path);
        }

        private static string FetchDescription(SearchItem item)
        {
            if (item.options.HasFlag(SearchItemOptions.Compacted))
                return FetchLabel(item);

            if (!String.IsNullOrEmpty(item.description))
                return item.description;

            if (!GlobalObjectId.TryParse(item.id, out var gid))
                return null;

            var sourceAssetPath = AssetDatabase.GUIDToAssetPath(gid.assetGUID.ToString());
            return (item.description = $"Source: {GetAssetDescription(sourceAssetPath)}");
        }

        private static Texture2D FetchThumbnail(SearchItem item)
        {
            if (!GlobalObjectId.TryParse(item.id, out var gid))
                return null;
            var sourceAssetPath = AssetDatabase.GUIDToAssetPath(gid.assetGUID.ToString());
            return AssetDatabase.GetCachedIcon(sourceAssetPath) as Texture2D;
        }

        private static Texture2D FetchPreview(SearchItem item, FetchPreviewOptions options)
        {
            if (!GlobalObjectId.TryParse(item.id, out var gid))
                return null;

            if (options.HasFlag(FetchPreviewOptions.Large))
            {
                var go = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid) as GameObject;
                if (go)
                    return AssetPreview.GetAssetPreview(go);
            }

            var sourceAssetPath = AssetDatabase.GUIDToAssetPath(gid.assetGUID.ToString());
            return AssetDatabase.GetCachedIcon(sourceAssetPath) as Texture2D;
        }

        private static void StartDrag(SearchItem item, SearchContext context)
        {
            if (context.selection.Count > 1)
                Utils.StartDrag(context.selection.Select(i => ToObject(i, typeof(Object))).ToArray(), item.GetLabel(context, true));
            else
                Utils.StartDrag(new[] { ToObject(item, typeof(Object)) }, item.GetLabel(context, true));
        }

        private static void TrackSelection(SearchItem item)
        {
            var obj = ToObject(item, typeof(Object));
            if (obj)
                EditorGUIUtility.PingObject(obj);
        }

        private static Object ToObject(SearchItem item, Type type)
        {
            if (!GlobalObjectId.TryParse(item.id, out var gid))
                return null;

            var assetPath = AssetDatabase.GUIDToAssetPath(gid.assetGUID.ToString());
            return ToObjectType(GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid), type) ??
                ToObjectType(AssetDatabase.LoadAssetAtPath(assetPath, type), type) ??
                ToObjectType(AssetDatabase.LoadMainAssetAtPath(assetPath), type);
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

        private static IEnumerator SearchObjects(SearchContext context, SearchProvider provider)
        {
            var searchQuery = context.searchQuery;

            if (searchQuery.Length > 0)
                yield return indexes.Select(db => SearchIndexes(searchQuery, context, provider, db));

            if (context.wantsMore && context.filterType != null && !context.textFilters.Contains("t:"))
            {
                if (searchQuery.Length > 0)
                    searchQuery = $"({context.searchText}) t:{context.filterType.Name}";
                else
                    searchQuery = $"t:{context.filterType.Name}";
                yield return indexes.Select(db => SearchIndexes(searchQuery, context, provider, db, 999));
            }
        }

        private static IEnumerator SearchIndexes(string searchQuery, SearchContext context, SearchProvider provider, SearchDatabase db, int scoreModifier = 0)
        {
            while (db.index == null)
                yield return null;

            // Search index
            var index = db.index;
            while (!index.IsReady())
                yield return null;

            yield return index.Search(searchQuery.ToLowerInvariant(), context, provider).Select(e =>
            {
                var itemScore = e.score + scoreModifier;
                return provider.CreateItem(context, e.id, itemScore, null, null, null, index.GetDocument(e.index));
            });
        }

        internal static string GetAssetDescription(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
                return assetPath;
            var fi = new FileInfo(assetPath);
            if (!fi.Exists)
                return "File does not exist anymore.";
            var fileSize = new FileInfo(assetPath).Length;
            return $"{assetPath} ({EditorUtility.FormatBytes(fileSize)})";
        }

        private static void SelectItems(SearchItem[] items)
        {
            Selection.instanceIDs = items.Select(i => ToObject(i, typeof(Object))).Where(o => o).Select(o => o.GetInstanceID()).ToArray();
            if (Selection.instanceIDs.Length == 0)
                return;
            EditorApplication.delayCall += () =>
            {
                EditorWindow.FocusWindowIfItsOpen(Utils.GetProjectBrowserWindowType());
                EditorApplication.delayCall += () => EditorGUIUtility.PingObject(Selection.instanceIDs.LastOrDefault());
            };
        }

        private static void OpenItem(SearchItem item)
        {
            if (!SelectObjectbyId(item.id, out var assetGUID))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);
                var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                if (asset != null)
                {
                    AssetDatabase.OpenAsset(asset);
                    EditorApplication.delayCall += () => SelectObjectbyId(item.id);
                }
            }
        }

        private static bool SelectObjectbyId(string id)
        {
            return SelectObjectbyId(id, out _);
        }

        private static bool SelectObjectbyId(string id, out string guid)
        {
            guid = null;
            if (!GlobalObjectId.TryParse(id, out var gid))
                return false;
            guid = gid.assetGUID.ToString();
            var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
            if (obj)
            {
                Utils.SelectObject(obj);
                return true;
            }
            return false;
        }

        [SearchActionsProvider]
        internal static IEnumerable<SearchAction> CreateActionHandlers()
        {
            return new[]
            {
                new SearchAction(type, "select", null, "Select", SelectItems),
                new SearchAction(type, "open", null, "Open", OpenItem)
            };
        }
    }
}
