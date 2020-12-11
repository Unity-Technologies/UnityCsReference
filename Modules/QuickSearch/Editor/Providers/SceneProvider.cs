// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace UnityEditor.Search.Providers
{
    class SceneProvider : SearchProvider
    {
        private bool m_HierarchyChanged = true;
        private List<GameObject> m_GameObjects = null;
        private SceneQueryEngine m_SceneQueryEngine;
        private ISearchView m_LastSearchView;

        public SceneProvider(string providerId, string filterId, string displayName)
            : base(providerId, displayName)
        {
            priority = 50;
            this.filterId = filterId;
            showDetails = true;
            showDetailsOptions = ShowDetailsOptions.Inspector | ShowDetailsOptions.Actions | ShowDetailsOptions.Preview;

            isEnabledForContextualSearch = () =>
                Utils.IsFocusedWindowTypeName("SceneView") ||
                Utils.IsFocusedWindowTypeName("SceneHierarchyWindow");

            EditorSceneManager.activeSceneChangedInEditMode += (_, __) => InvalidateScene();
            PrefabStage.prefabStageOpened += _ => InvalidateScene();
            PrefabStage.prefabStageClosing += _ => InvalidateScene();
            ObjectChangeEvents.changesPublished += OnObjectChanged;

            supportsSyncViewSearch = true;

            toObject = (item, type) => ObjectFromItem(item, type);

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
                    var components = go.GetComponents<Component>();
                    if (components.Length > 2 && components[1] && components[components.Length - 1])
                        item.label = $"{transformPath} ({components[1].GetType().Name}..{components[components.Length - 1].GetType().Name})";
                    else if (components.Length > 1 && components[1])
                        item.label = $"{transformPath} ({components[1].GetType().Name})";
                    else
                        item.label = $"{transformPath} ({item.id})";

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
                return m_SceneQueryEngine?.FindPropositions(context, options);
            };

            trackSelection = (item, context) => PingItem(item);
        }

        private void InvalidateScene()
        {
            m_HierarchyChanged = true;
            m_LastSearchView?.Refresh();
        }

        private void InvalidateObject(int instanceId, RefreshFlags flags = RefreshFlags.Default)
        {
            if (m_SceneQueryEngine.InvalidateObject(instanceId))
                m_LastSearchView?.Refresh(flags);
            else if (UnityEngine.Object.FindObjectFromInstanceID(instanceId) is Component c)
                InvalidateObject(c.gameObject.GetInstanceID());
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
                    enabled = (items) => SceneVisibilityManager.instance.IsHidden(items.First().ToObject<GameObject>()),
                    execute = (items) => SceneVisibilityManager.instance.Show(items.Select(i => i.ToObject<GameObject>()).Where(i => i).ToArray(), true)
                },

                new SearchAction(providerId, "hide", null, "Hide selected object(s)")
                {
                    enabled = (items) => !SceneVisibilityManager.instance.IsHidden(items.First().ToObject<GameObject>()),
                    execute = (items) => SceneVisibilityManager.instance.Hide(items.Select(i => i.ToObject<GameObject>()).Where(i => i).ToArray(), true)
                },
            };
        }

        private IEnumerator SearchItems(SearchContext context, SearchProvider provider)
        {
            m_LastSearchView = context.searchView;
            if (!string.IsNullOrEmpty(context.searchQuery))
            {
                if (m_HierarchyChanged)
                {
                    m_GameObjects = new List<GameObject>(SearchUtils.FetchGameObjects());
                    m_SceneQueryEngine = new SceneQueryEngine(m_GameObjects);
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

                yield return m_SceneQueryEngine.Search(context, provider, subset).Select(gameObject =>
                {
                    if (!gameObject)
                        return null;
                    return AddResult(context, provider, gameObject.GetInstanceID().ToString(), 0, false);
                });
            }
            else if (context.wantsMore && context.filterType != null && string.IsNullOrEmpty(context.searchQuery))
            {
                yield return GameObject.FindObjectsOfType(context.filterType)
                    .Select(obj =>
                    {
                        if (obj is Component c)
                            return c.gameObject;
                        return obj as GameObject;
                    })
                    .Where(go => go)
                    .Select(go => AddResult(context, provider, go.GetInstanceID().ToString(), 999, false));
            }
        }

        private static SearchItem AddResult(SearchContext context, SearchProvider provider, string id, int score, bool useFuzzySearch)
        {
            string description = null;
            var item = provider.CreateItem(context, id, score, null, description, null, null);
            return SetItemDescriptionFormat(item, useFuzzySearch);
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
            Selection.instanceIDs = objects.Select(o => o.GetInstanceID()).ToArray();
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.FrameSelected();
        }

        private static GameObject ObjectFromItem(SearchItem item)
        {
            var instanceID = Convert.ToInt32(item.id);
            var obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            return obj;
        }

        private static UnityEngine.Object ObjectFromItem(SearchItem item, Type type)
        {
            var go = ObjectFromItem(item);
            if (!go)
                return null;

            if (typeof(Component).IsAssignableFrom(type))
                return go.GetComponent(type);

            return ObjectFromItem(item);
        }
    }

    static class BuiltInSceneObjectsProvider
    {
        const string k_DefaultProviderId = "scene";

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            return new SceneProvider(k_DefaultProviderId, "h:", "Hierarchy");
        }

        [SearchActionsProvider]
        internal static IEnumerable<SearchAction> ActionHandlers()
        {
            return SceneProvider.CreateActionHandlers(k_DefaultProviderId);
        }

        [Shortcut("Help/Search/Hierarchy")]
        internal static void OpenQuickSearch()
        {
            QuickSearch.OpenWithContextualProvider(k_DefaultProviderId, Query.type);
        }
    }
}
