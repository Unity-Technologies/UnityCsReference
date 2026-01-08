// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace UnityEditor.Search.Providers
{
    class SceneProvider : SearchProvider
    {
        private bool m_HierarchyChanged = true;
        private SceneQueryEngine m_SceneQueryEngine;

        internal static event Action<SceneProvider> s_QueueProviderRefresh;
        static bool s_Init;

        readonly struct GameObjectData
        {
            public readonly GameObject go;
            public readonly ulong key;

            public GameObjectData(GameObject go)
            {
                this.go = go;
                this.key = SearchUtils.GetDocumentKey(go);
            }
        }

        public SceneProvider(string providerId, string filterId, string displayName)
            : base(providerId, displayName)
        {
            priority = 50;
            this.filterId = filterId;
            showDetails = true;
            showDetailsOptions = ShowDetailsOptions.Inspector | ShowDetailsOptions.Actions | ShowDetailsOptions.Preview | ShowDetailsOptions.DefaultGroup;

            isEnabledForContextualSearch = () => EditorWindow.focusedWindow is ISearchableContainer searchable && searchable.HierarchyType == HierarchyType.GameObjects;

            supportsSyncViewSearch = true;

            onEnable = () =>
            {
                if (!s_Init)
                {
                    SearchMonitor.sceneChanged += InvalidateScene;
                    SearchMonitor.documentsInvalidated += Refresh;
                    SearchMonitor.objectChanged += OnObjectChanged;
                    s_Init = true;
                }
            };

            toObject = (item, type) => ObjectFromItem(item, type);
            toKey = (item) => ToKey(item);
            toEntityId = (item) => GetItemInstanceId(item);

            fetchItems = (context, items, provider) => SearchItems(context, provider);

            fetchLabel = (item, context) =>
            {
                if (item.label != null)
                    return item.label;

                var go = ObjectFromItem(item);
                if (!go)
                    return item.id;

                if (context == null || context.searchView == null || context.searchView.displayMode == DisplayMode.List)
                {
                    var transformPath = SearchUtils.GetTransformPath(go.transform);
                    if (item.options.HasAny(SearchItemOptions.Compacted))
                        item.label = transformPath;
                    else
                    {
                        var components = go.GetComponents<Component>();
                        if (components.Length > 2 && components[1] && components[components.Length - 1])
                            item.label = $"{transformPath} ({components[1].GetType().Name}..{components[components.Length - 1].GetType().Name})";
                        else if (components.Length > 1 && components[1])
                            item.label = $"{transformPath} ({components[1].GetType().Name})";
                        else
                            item.label = $"{transformPath} ({item.id})";
                    }

                    if (context != null)
                    {
                        long score = 1;
                        List<int> matches = new List<int>();
                        var sq = Utils.CleanString(context.searchQuery);
                        if (FuzzySearch.FuzzyMatch(sq, Utils.CleanString(item.label), ref score, matches))
                            item.label = RichTextFormatter.FormatSuggestionTitle(item.label, matches);
                    }
                }
                else
                {
                    item.label = go.name;
                }

                return item.label;
            };

            fetchDescription = (item, context) =>
            {
                var go = ObjectFromItem(item);
                if (item.options.HasAny(SearchItemOptions.Compacted))
                    return go.name;
                return (item.description = SearchUtils.GetHierarchyPath(go));
            };

            fetchThumbnail = (item, context) =>
            {
                var obj = ObjectFromItem(item);
                if (obj == null)
                    return null;

                return (item.thumbnail = Utils.GetThumbnailForGameObject(obj));
            };

            fetchPreview = (item, context, size, options) =>
            {
                var obj = ObjectFromItem(item);
                if (obj == null)
                    return item.thumbnail;
                return Utils.GetSceneObjectPreview(context, obj, size, options, item.thumbnail);
            };

            startDrag = (item, context) =>
            {
                if (context.selection.Count > 1)
                    #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    Utils.StartDrag(context.selection.Select(i => ObjectFromItem(i)).ToArray(), item.GetLabel(context, true));
#pragma warning restore RS0030
                else
                    Utils.StartDrag(new[] { ObjectFromItem(item) }, item.GetLabel(context, true));
            };

            fetchPropositions = (context, options) =>
            {
                if (options.HasAny(SearchPropositionFlags.QueryBuilder))
                    return FetchQueryBuilderPropositions(context);
                return m_SceneQueryEngine == null ? new SearchProposition[0] : m_SceneQueryEngine.FindPropositions(context, options);
            };

            trackSelection = (item, context) => PingItem(item);

            fetchColumns = (context, items) => SceneSelectors.Enumerate(items);
        }

        internal SceneQueryEngine queryEngine
        {
            get
            {
                if (m_SceneQueryEngine == null)
                {
                    m_SceneQueryEngine = new SceneQueryEngine(SearchUtils.FetchGameObjects());
                    m_SceneQueryEngine.SetupQueryEnginePropositions();
                }
                return m_SceneQueryEngine;
            }
        }

        private void Refresh()
        {
            EditorApplication.delayCall -= SearchService.RefreshWindows;
            EditorApplication.delayCall += SearchService.RefreshWindows;
            s_QueueProviderRefresh?.Invoke(this);
        }

        private ulong ToKey(SearchItem item)
        {
            if (item.data is GameObjectData data)
                return data.key;
            return ulong.MaxValue;
        }

        private void InvalidateScene()
        {
            m_HierarchyChanged = true;
            Refresh();
        }

        private void InvalidateObject(EntityId entityId)
        {
            if (UnityEngine.Object.FindObjectFromInstanceID(entityId) is Component c)
                queryEngine.InvalidateObject(c.gameObject.GetEntityId());
            else
                queryEngine.InvalidateObject(entityId);
        }

        private void InvalidateObjectAndRefs(EntityId entityId)
        {
            if (UnityEngine.Object.FindObjectFromInstanceID(entityId) is Component c)
                queryEngine.InvalidateObjectAndRefs(c.gameObject.GetEntityId());
            else
                queryEngine.InvalidateObjectAndRefs(entityId);
        }

        private void OnObjectChanged(ref ObjectChangeEventStream stream)
        {
            if (m_SceneQueryEngine == null)
                return;

            for (int i = 0; i < stream.length; ++i)
            {
                var eventType = stream.GetEventType(i);
                switch (eventType)
                {
                    case ObjectChangeKind.None:
                    case ObjectChangeKind.CreateAssetObject:
                    case ObjectChangeKind.DestroyAssetObject:
                    case ObjectChangeKind.ChangeAssetObjectProperties:
                        break;

                    case ObjectChangeKind.ChangeScene:
                    case ObjectChangeKind.CreateGameObjectHierarchy:
                    case ObjectChangeKind.DestroyGameObjectHierarchy:
                        InvalidateScene();
                        break;

                    case ObjectChangeKind.ChangeGameObjectStructureHierarchy:
                    {
                        stream.GetChangeGameObjectStructureHierarchyEvent(i, out var e);
                        InvalidateObjectAndRefs(e.entityId);
                    }
                    break;
                    case ObjectChangeKind.ChangeGameObjectStructure:
                    {
                        stream.GetChangeGameObjectStructureEvent(i, out var e);
                        InvalidateObjectAndRefs(e.entityId);
                    }
                    break;
                    case ObjectChangeKind.ChangeGameObjectParent:
                    {
                        stream.GetChangeGameObjectParentEvent(i, out var e);
                        InvalidateObjectAndRefs(e.entityId);
                    }
                    break;
                    case ObjectChangeKind.ChangeGameObjectOrComponentProperties:
                    {
                        stream.GetChangeGameObjectOrComponentPropertiesEvent(i, out var e);
                        InvalidateObject(e.entityId);
                    }
                    break;
                    case ObjectChangeKind.UpdatePrefabInstances:
                    {
                        stream.GetUpdatePrefabInstancesEvent(i, out var e);
                        for (int idIndex = 0; idIndex < e.entityIds.Length; ++idIndex)
                            InvalidateObject(e.entityIds[idIndex]);
                    }
                    break;
                }
            }
        }

        public static IEnumerable<SearchAction> CreateActionHandlers(string providerId)
        {
            return new SearchAction[]
            {
                new SearchAction(providerId, "select", null, "Select object(s) in scene...")
                {
                    execute = (items) =>
                    {
                        #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                        FrameObjects(items.Select(i => i.provider.toObject(i, typeof(GameObject))).Where(i => i).ToArray());
#pragma warning restore RS0030
                    }
                },

                new SearchAction(providerId, "open", null, "Select containing asset")
                {
                    handler = (item) =>
                    {
                        var pingedObject = PingItem(item);
                        if (pingedObject != null)
                        {
                            var go = pingedObject as GameObject;
                            var assetPath = SearchUtils.GetHierarchyAssetPath(go);
                            if (!String.IsNullOrEmpty(assetPath))
                                Utils.FrameAssetFromPath(assetPath);
                            else
                                FrameObject(go);
                        }
                    }
                },

                new SearchAction(providerId, "show", null, "Show selected object(s)")
                {
                    enabled = (items) => IsHidden(items),
                    #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    execute = (items) => SceneVisibilityManager.instance.Show(items.Select(i => i.ToObject<GameObject>()).Where(i => i).ToArray(), true)
#pragma warning restore RS0030
                },

                new SearchAction(providerId, "hide", null, "Hide selected object(s)")
                {
                    enabled = (items) => !IsHidden(items),
                    #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    execute = (items) => SceneVisibilityManager.instance.Hide(items.Select(i => i.ToObject<GameObject>()).Where(i => i).ToArray(), true)
#pragma warning restore RS0030
                },
            };
        }

        private static bool IsHidden(IReadOnlyCollection<SearchItem> items)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var go = items.First().ToObject<GameObject>();
#pragma warning restore RS0030
            if (!go)
                return false;
            return SceneVisibilityManager.instance.IsHidden(go);
        }

        private IEnumerator SearchItems(SearchContext context, SearchProvider provider)
        {
            if (!string.IsNullOrEmpty(context.searchQuery))
            {
                if (m_HierarchyChanged)
                {
                    m_SceneQueryEngine = new SceneQueryEngine(SearchUtils.FetchGameObjects());
                    m_SceneQueryEngine.SetupQueryEnginePropositions();
                    m_HierarchyChanged = false;
                }

                using (SearchMonitor.GetView())
                {
                    #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    yield return queryEngine.Search(context, provider, null)
#pragma warning restore RS0030
                        .Select(go => AddResult(context, provider, go));
                }
            }
            else if (context.filterType != null && string.IsNullOrEmpty(context.searchQuery))
            {
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                yield return UnityEngine.Object.FindObjectsByType(context.filterType, UnityEngine.FindObjectsSortMode.None)
#pragma warning restore RS0030
                    .Select(obj =>
                    {
                        if (obj is Component c)
                            return c.gameObject;
                        return obj as GameObject;
                    })
                    .Select(go => AddResult(context, provider, go));
            }
        }

        public static SearchItem AddResult(SearchContext context, SearchProvider provider, GameObject go)
        {
            if (!go)
                return null;

            var entityId = go.GetEntityId();
            var item = provider.CreateItem(context, entityId.ToString(), entityId.GetHashCode(), null, null, null, new GameObjectData(go));
            return SetItemDescriptionFormat(item, useFuzzySearch: false);
        }

        private static SearchItem SetItemDescriptionFormat(SearchItem item, bool useFuzzySearch)
        {
            item.options = SearchItemOptions.Ellipsis
                | SearchItemOptions.RightToLeft
                | (useFuzzySearch ? SearchItemOptions.FuzzyHighlight : SearchItemOptions.Highlight);
            return item;
        }

        private static UnityEngine.Object PingItem(SearchItem item)
        {
            var obj = ObjectFromItem(item);
            if (obj == null)
                return null;
            EditorGUIUtility.PingObject(obj);
            return obj;
        }

        private static void FrameObject(object obj)
        {
            Selection.activeGameObject = obj as GameObject ?? Selection.activeGameObject;
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.FrameSelected();
        }

        private static void FrameObjects(UnityEngine.Object[] objects)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            Selection.entityIds = objects.Select(o => o.GetHashCode()).ToArray().ToEntityIdArray();
#pragma warning restore RS0030
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.FrameSelected();
        }

        private EntityId GetItemInstanceId(SearchItem item)
        {
            var obj = ObjectFromItem(item);
            return obj.GetEntityId();
        }

        private static GameObject ObjectFromItem(in SearchItem item)
        {
            if (item.data is GameObjectData data)
                return data.go;
            return null;
        }

        private static UnityEngine.Object ObjectFromItem(in SearchItem item, Type type)
        {
            var go = ObjectFromItem(item);
            if (!go)
                return null;

            if (typeof(Component).IsAssignableFrom(type))
                return go.GetComponent(type);

            return ObjectFromItem(item);
        }

        private IEnumerable<SearchProposition> FetchQueryBuilderPropositions(SearchContext context)
        {
            return FetchQueryBuilderPropositions(queryEngine, context);
        }

        internal static IEnumerable<SearchProposition> FetchQueryBuilderPropositions(SceneQueryEngine engine, SearchContext context)
        {
            foreach (var p in QueryAndOrBlock.BuiltInQueryBuilderPropositions())
                yield return p;

            foreach (var t in QueryListBlockAttribute.GetPropositions(typeof(QueryComponentBlock)))
                yield return t;

            foreach (var f in QueryListBlockAttribute.GetPropositions(typeof(QueryIsFilterBlock)))
                yield return f;

            foreach (var f in QueryListBlockAttribute.GetPropositions(typeof(QueryMissingBlock)))
                yield return f;

            foreach (var f in QueryListBlockAttribute.GetPropositions(typeof(QueryRenderingLayerBlock)))
                yield return f;

            foreach (var f in QueryListBlockAttribute.GetPropositions(typeof(QuerySceneFilterBlock)))
                yield return f;

            foreach (var p in engine.engine.GetPropositions())
                yield return p;

            yield return new SearchProposition(category: "Options", "Fuzzy Matches", "+fuzzy",
                "Use fuzzy search to match object names", priority: 0, moveCursor: TextCursorPlacement.MoveAutoComplete, icon: Icons.toggles,
                color: QueryColors.toggle);

            IEnumerable<UnityEngine.Object> sceneObjects;
            if (context != null && context.searchView != null && context.searchView.results.Count > 0 && !context.searchInProgress)
            {
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                sceneObjects = context.searchView.results.Select(r => r.ToObject()).Where(o => o);
#pragma warning restore RS0030
            }
            else
            {
                sceneObjects = SearchUtils.FetchGameObjects();
            }

            foreach (var p in SearchUtils.EnumeratePropertyPropositions(sceneObjects, IterateNonVisibleProperties))
                yield return p;
        }

        static IEnumerable<SerializedProperty> IterateNonVisibleProperties(SerializedObject so)
        {
            if (so.targetObject is Transform)
                yield break;

            var sp = so.FindProperty("m_Enabled");
            if (sp is { isValid: true })
                yield return sp;
        }
    }

    static class BuiltInSceneObjectsProvider
    {
        public const string type = "scene";
        public const string filterId = "h:";

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            return new SceneProvider(type, filterId, "Hierarchy");
        }

        [SearchActionsProvider]
        internal static IEnumerable<SearchAction> ActionHandlers()
        {
            return SceneProvider.CreateActionHandlers(type);
        }

        [Shortcut("Help/Search/Hierarchy")]
        internal static void OpenQuickSearch()
        {
            SearchUtils.OpenWithContextualProviders(type);
        }

        [SearchTemplate(description = "Find mesh object", providerId = type)] internal static string ST1() => @"t=MeshFilter vertices>=1024";
        [SearchTemplate(description = "Find objects that refers to asset", providerId = type)]
        internal static string ST2()
        {
            if (Selection.activeObject && AssetDatabase.GetAssetPath(Selection.activeObject) is string assetPath && !string.IsNullOrEmpty(assetPath))
                return $"ref=\"{assetPath}\"";
            return "ref=<$object:none$>";
        }
    }
}
