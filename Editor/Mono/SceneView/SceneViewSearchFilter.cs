// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace UnityEditor
{
    // Marks the GameObjects of the current stage that match the Scene view search text
    internal static partial class SceneViewSearchFilter
    {
        // The scene search engine of the Search module, which evaluates the Search query language
        const string k_SearchEngineName = "Advanced";

        [AutoStaticsCleanupOnCodeReload]
        [IgnoreForUAL0015("Engine re-resolved when the next filtering pass opens a search session after a code reload")]
        static SearchService.ISceneSearchEngineV2 s_Engine;
        [AutoStaticsCleanupOnCodeReload]
        [IgnoreForUAL0015("Null means no search session is open, which the next filtering pass opens")]
        static SearchService.SceneSearchContext s_SearchContext;

        [AutoStaticsCleanupOnCodeReload]
        [IgnoreForUAL0015("Null means the applied marks are unknown, which the next SetQuery resolves by applying its query")]
        static string s_Query;
        [AutoStaticsCleanupOnCodeReload]
        static bool s_Listening;
        [AutoStaticsCleanupOnCodeReload]
        static bool s_ReapplyPending;

        internal static string query => s_Query ?? string.Empty;

        internal static bool isFiltering => !string.IsNullOrEmpty(s_Query);

        internal static void SetQuery(string searchQuery)
        {
            searchQuery ??= string.Empty;
            if (s_Query != null && s_Query == searchQuery)
                return;

            s_Query = searchQuery;
            s_ReapplyPending = false;

            if (isFiltering)
                StartListening();
            else
            {
                StopListening();
                EndSearchSession();
            }

            Apply();
        }

        // Called when the last Scene view is disabled
        internal static void Release()
        {
            StopListening();
            EndSearchSession();
            s_ReapplyPending = false;
            s_Query = null;
        }

        static void Apply()
        {
            // The legacy Hierarchy marks the GameObjects itself
            if (EditorSettings.useLegacyHierarchy)
                return;

            using var visibleHandle = ListPool<EntityId>.Get(out var visible);
            using var hiddenHandle = ListPool<EntityId>.Get(out var hidden);
            var stage = StageUtility.GetCurrentStage();
            if (isFiltering)
                AddMatches(CreateStageIterator(stage), visible, hidden);
            else
            {
                AddAll(CreateStageIterator(stage), visible);

                // Clearing also releases the marks of the main stage behind the current one
                if (stage is not MainStage)
                    AddAll(CreateStageIterator(null), visible);
            }

            SceneView.SetSearchFilterMarks(NoAllocHelpers.CreateReadOnlySpan(visible), NoAllocHelpers.CreateReadOnlySpan(hidden));
            SceneView.RepaintAll();
        }

        // Walks the GameObjects the Hierarchy shows for the stage, or the main stage when null
        static HierarchyIterator CreateStageIterator(Stage stage)
        {
            var iterator = new HierarchyIterator(HierarchyType.GameObjects);
            if (SceneHierarchyHooks.provideSubScenes != null)
                iterator.SetSubScenes(SceneHierarchyHooks.provideSubScenes());

            if (stage == null || stage is MainStage)
                return iterator;

            using var scenesHandle = ListPool<Scene>.Get(out var scenes);
            for (int i = 0, c = stage.sceneCount; i < c; ++i)
            {
                var scene = stage.GetSceneAt(i);
                if (scene.IsValid() && scene.isLoaded)
                    scenes.Add(scene);
            }

            // Without custom scenes the iterator walks the main stage
            if (scenes.Count == 0)
                return null;

            iterator.SetCustomScenes(scenes.ToArray());
            return iterator;
        }

        static void AddMatches(HierarchyIterator iterator, List<EntityId> visible, List<EntityId> hidden)
        {
            if (iterator == null)
                return;

            var engine = BeginSearchSession();
            // Without the engine nothing is filtered
            if (engine == null)
            {
                AddAll(iterator, visible);
                return;
            }

            s_SearchContext.rootIterator = iterator;
            engine.BeginSearch(s_SearchContext, s_Query);
            try
            {
                while (iterator.Next(null))
                {
                    if (iterator.isSceneHeader)
                        continue;

                    var matches = engine.Filter(s_SearchContext, s_Query, iterator);
                    (matches ? visible : hidden).Add(iterator.entityId);
                }
            }
            finally
            {
                engine.EndSearch(s_SearchContext);
                s_SearchContext.rootIterator = null;
            }
        }

        static void AddAll(HierarchyIterator iterator, List<EntityId> entityIds)
        {
            if (iterator == null)
                return;

            while (iterator.Next(null))
            {
                if (!iterator.isSceneHeader)
                    entityIds.Add(iterator.entityId);
            }
        }

        // The session stays open while filtering so the engine keeps its cached GameObject data between passes
        static SearchService.ISceneSearchEngineV2 BeginSearchSession()
        {
            if (s_SearchContext != null)
                return s_Engine;

            s_Engine = SearchService.SceneSearch.GetSearchEngine(k_SearchEngineName);
            if (s_Engine == null)
                return null;

            s_SearchContext = new SearchService.SceneSearchContext();
            s_Engine.BeginSession(s_SearchContext);
            return s_Engine;
        }

        static void EndSearchSession()
        {
            if (s_SearchContext == null)
                return;

            s_Engine.EndSession(s_SearchContext);
            s_SearchContext = null;
            s_Engine = null;
        }

        static void StartListening()
        {
            if (s_Listening)
                return;

            s_Listening = true;
            // New GameObjects start marked visible, and changes can alter what matches
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            ObjectChangeEvents.changesPublished += OnChangesPublished;
        }

        static void StopListening()
        {
            if (!s_Listening)
                return;

            s_Listening = false;
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            ObjectChangeEvents.changesPublished -= OnChangesPublished;
        }

        static void OnHierarchyChanged() => RequestReapply();

        static void OnChangesPublished(ref ObjectChangeEventStream stream) => RequestReapply();

        // Coalesces the notifications of one tick into a single pass
        static void RequestReapply()
        {
            if (s_ReapplyPending)
                return;

            s_ReapplyPending = true;
            EditorApplication.delayCall += Reapply;
        }

        static void Reapply()
        {
            if (!s_ReapplyPending)
                return;

            s_ReapplyPending = false;
            if (isFiltering)
                Apply();
        }
    }
}
