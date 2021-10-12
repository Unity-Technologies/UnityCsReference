// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShortcutManagement;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.Search.Providers
{
    class SceneProvider : SearchProvider
    {
        private bool m_HierarchyChanged = true;
        private SceneQueryEngine m_SceneQueryEngine;

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

            isEnabledForContextualSearch = () =>
                Utils.IsFocusedWindowTypeName("SceneView") ||
                Utils.IsFocusedWindowTypeName("SceneHierarchyWindow");

            SearchMonitor.sceneChanged += InvalidateScene;
            SearchMonitor.documentsInvalidated += Refresh;

            SearchMonitor.objectChanged += OnObjectChanged;

            supportsSyncViewSearch = true;

            toObject = (item, type) => ObjectFromItem(item, type);
            toKey = (item) => ToKey(item);

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
                return Utils.GetSceneObjectPreview(obj, size, options, item.thumbnail);
            };

            startDrag = (item, context) =>
            {
                if (context.selection.Count > 1)
                    Utils.StartDrag(context.selection.Select(i => ObjectFromItem(i)).ToArray(), item.GetLabel(context, true));
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
                    m_SceneQueryEngine = new SceneQueryEngine(SearchUtils.FetchGameObjects());
                return m_SceneQueryEngine;
            }
        }

        private void Refresh()
        {
            EditorApplication.delayCall -= SearchService.RefreshWindows;
            EditorApplication.delayCall += SearchService.RefreshWindows;
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

        private void InvalidateObject(int instanceId, RefreshFlags flags = RefreshFlags.Default)
        {
            if (UnityEngine.Object.FindObjectFromInstanceID(instanceId) is Component c)
                queryEngine.InvalidateObject(c.gameObject.GetInstanceID());
            else
                queryEngine.InvalidateObject(instanceId);
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
                        InvalidateObject(e.instanceId, RefreshFlags.StructureChanged);
                    }
                    break;
                    case ObjectChangeKind.ChangeGameObjectStructure:
                    {
                        stream.GetChangeGameObjectStructureEvent(i, out var e);
                        InvalidateObject(e.instanceId, RefreshFlags.StructureChanged);
                    }
                    break;
                    case ObjectChangeKind.ChangeGameObjectParent:
                    {
                        stream.GetChangeGameObjectParentEvent(i, out var e);
                        InvalidateObject(e.instanceId);
                    }
                    break;
                    case ObjectChangeKind.ChangeGameObjectOrComponentProperties:
                    {
                        stream.GetChangeGameObjectOrComponentPropertiesEvent(i, out var e);
                        InvalidateObject(e.instanceId);
                    }
                    break;
                    case ObjectChangeKind.UpdatePrefabInstances:
                    {
                        stream.GetUpdatePrefabInstancesEvent(i, out var e);
                        for (int idIndex = 0; idIndex < e.instanceIds.Length; ++idIndex)
                            InvalidateObject(e.instanceIds[idIndex]);
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
                        FrameObjects(items.Select(i => i.provider.toObject(i, typeof(GameObject))).Where(i => i).ToArray());
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
                    execute = (items) => SceneVisibilityManager.instance.Show(items.Select(i => i.ToObject<GameObject>()).Where(i => i).ToArray(), true)
                },

                new SearchAction(providerId, "hide", null, "Hide selected object(s)")
                {
                    enabled = (items) => !IsHidden(items),
                    execute = (items) => SceneVisibilityManager.instance.Hide(items.Select(i => i.ToObject<GameObject>()).Where(i => i).ToArray(), true)
                },
            };
        }

        private static bool IsHidden(IReadOnlyCollection<SearchItem> items)
        {
            var go = items.First().ToObject<GameObject>();
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
                    m_HierarchyChanged = false;
                }

                IEnumerable<GameObject> subset = null;
                if (context.subset != null)
                {
                    subset = context.subset
                        .Where(item => item.provider.id == "scene")
                        .Select(item => ObjectFromItem(item))
                        .Where(obj => obj != null);
                }

                using (SearchMonitor.GetView())
                {
                    yield return queryEngine.Search(context, provider, subset)
                        .Where(go => go)
                        .Select(go => AddResult(context, provider, go));
                }
            }
            else if (context.filterType != null && string.IsNullOrEmpty(context.searchQuery))
            {
                yield return UnityEngine.Object.FindObjectsOfType(context.filterType)
                    .Select(obj =>
                    {
                        if (obj is Component c)
                            return c.gameObject;
                        return obj as GameObject;
                    })
                    .Where(go => go)
                    .Select(go => AddResult(context, provider, go));
            }
        }

        public static SearchItem AddResult(SearchContext context, SearchProvider provider, GameObject go)
        {
            var instanceId = go.GetHashCode();
            var item = provider.CreateItem(context, instanceId.ToString(), ~instanceId, null, null, null, new GameObjectData(go));
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
            Selection.instanceIDs = objects.Select(o => o.GetHashCode()).ToArray();
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.FrameSelected();
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

            var goIcon = Utils.LoadIcon("GameObject Icon");
            yield return new SearchProposition(category: "GameObject", label: "InstanceID", replacement: "id=0", help: "Search object with InstanceID", icon: goIcon);
            yield return new SearchProposition(category: "GameObject", label: "Path", replacement: "path=/root/children1", help: "Search object with Transform path", icon: goIcon);
            yield return new SearchProposition(category: "GameObject", label: "Volume Size", replacement: "size>1", help: "Search object by volume size", icon: goIcon);
            yield return new SearchProposition(category: "GameObject", label: "Components count", replacement: "components>1", help: "Search object with more than # components", icon: goIcon);

            var filterIcon = Utils.LoadIcon("Filter Icon");
            yield return new SearchProposition(category: "Filters", label: "Active", "active=true", "Search active objects", icon: filterIcon);

            var sceneIcon = Utils.LoadIcon("SceneAsset Icon");
            var queryEngineFunctions = TypeCache.GetMethodsWithAttribute<SceneQueryEngineFilterAttribute>();
            foreach (var mi in queryEngineFunctions)
            {
                var attr = mi.GetAttribute<SceneQueryEngineFilterAttribute>();
                var op = attr.supportedOperators == null ? ">" : attr.supportedOperators[0];
                var value = op == ":" ? "" : "1";
                var label = attr.token;
                string help = null;

                if (mi.ReturnType == typeof(Vector4))
                    value = "(,,,)";

                var replacement = $"{attr.token}{op}{value}";

                var descriptionAttr = mi.GetAttribute<System.ComponentModel.DescriptionAttribute>();
                if (descriptionAttr != null)
                {
                    help = label;
                    label = descriptionAttr.Description;
                }

                yield return new SearchProposition(category: "Scene Filters", label: label, help: help, replacement: replacement, icon: sceneIcon);
            }

            var sceneObjects = context.searchView?.results.Count > 0 ?
                context.searchView.results.Select(r => r.ToObject()).Where(o => o) : SearchUtils.FetchGameObjects();
            foreach (var p in SearchUtils.EnumeratePropertyPropositions(sceneObjects).Take(100))
                yield return p;


            yield return new SearchProposition(category: "Reference", "Reference By Path (Object)", "ref=<$object:none,UnityEngine.Object$>", "Find all objects referencing a specific asset.", icon: sceneIcon);
            yield return new SearchProposition(category: "Reference", "Reference By Instance ID (Number)", "ref=1000", "Find all objects referencing a specific instance ID (Number).", icon: sceneIcon);
            yield return new SearchProposition(category: "Reference", "Reference By Asset Expression", "ref={p: }", "Find all objects referencing for a given asset search.", icon: sceneIcon);
        }
    }

    static class BuiltInSceneObjectsProvider
    {
        public const string type = "scene";

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            return new SceneProvider(type, "h:", "Hierarchy");
        }

        [SearchActionsProvider]
        internal static IEnumerable<SearchAction> ActionHandlers()
        {
            return SceneProvider.CreateActionHandlers(type);
        }

        [Shortcut("Help/Search/Hierarchy")]
        internal static void OpenQuickSearch()
        {
            QuickSearch.OpenWithContextualProvider(type, Query.type);
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
