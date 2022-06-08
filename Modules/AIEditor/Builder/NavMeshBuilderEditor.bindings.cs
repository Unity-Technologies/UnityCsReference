// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Bindings;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.AI
{
    [MovedFrom("UnityEditor")]
    [NativeHeader("Modules/AIEditor/Builder/NavMeshBuilderEditor.bindings.h")]
    [StaticAccessor("NavMeshBuilderEditorBindings", StaticAccessorType.DoubleColon)]
    public sealed class NavMeshBuilder
    {
        // Object containing settings for the Navmesh
        //*undocumented*
        public static extern UnityEngine.Object navMeshSettingsObject { get; }

        // Build the Navmesh.
        public static extern void BuildNavMesh();

        // Build the Navmesh Asynchronously.
        public static extern void BuildNavMeshAsync();

        // Clear all Navmeshes.
        [StaticAccessor("NavMeshBuilder", StaticAccessorType.DoubleColon)]
        public static extern void ClearAllNavMeshes();

        // Returns true if an asynchronous build is still running.
        public static extern bool isRunning
        {
            [StaticAccessor("NavMeshBuilder", StaticAccessorType.DoubleColon)]
            [NativeMethod("IsBuildingNavMeshAsync")]
            get;
        }

        // Cancel Navmesh construction.
        [StaticAccessor("NavMeshBuilder", StaticAccessorType.DoubleColon)]
        public static extern void Cancel();

        internal static extern UnityEngine.Object sceneNavMeshData { get; set; }

        public static void BuildNavMeshForMultipleScenes(string[] paths)
        {
            if (paths.Length == 0)
                return;

            for (int i = 0; i < paths.Length; i++)
            {
                for (int j = i + 1; j < paths.Length; j++)
                {
                    if (paths[i] == paths[j])
                        throw new Exception("No duplicate scene names are allowed");
                }
            }

            // TODO SL Store current SceneManager setup
            // Save scenes or cancel
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // Combine all scenes
            if (!EditorSceneManager.OpenScene(paths[0]).IsValid())
            {
                throw new Exception("Could not open scene: " + paths[0]);
            }
            for (int i = 1; i < paths.Length; ++i)
                EditorSceneManager.OpenScene(paths[i], OpenSceneMode.Additive);

            // Build navmesh for the combined scenes
            BuildNavMesh();
            UnityEngine.Object asset = sceneNavMeshData;

            for (int i = 0; i < paths.Length; ++i)
            {
                if (EditorSceneManager.OpenScene(paths[i]).IsValid())
                {
                    sceneNavMeshData = asset;
                    // We only have one scene open so it is ok to simply get the active scene here
                    EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                }
            }

            // TODO SL Restore SceneManager setup
        }

        public static void CollectSourcesInStage(
            Bounds includedWorldBounds, int includedLayerMask, NavMeshCollectGeometry geometry, int defaultArea, bool generateLinksByDefault,
            List<NavMeshBuildMarkup> markups, bool includeOnlyMarkedObjects, Scene stageProxy, List<NavMeshBuildSource> results)
        {
            if (markups == null)
                throw new ArgumentNullException(nameof(markups));
            if (results == null)
                throw new ArgumentNullException(nameof(results));
            if (!stageProxy.IsValid())
                throw new ArgumentException("Stage cannot be deduced from invalid scene.", nameof(stageProxy));

            // Ensure strictly positive extents
            includedWorldBounds.extents = Vector3.Max(includedWorldBounds.extents, 0.001f * Vector3.one);
            var resultsArray = CollectSourcesInStageInternal(
                includedLayerMask, includedWorldBounds, null, true, geometry, defaultArea, generateLinksByDefault, markups.ToArray(), includeOnlyMarkedObjects, stageProxy);
            results.Clear();
            results.AddRange(resultsArray);
        }

        public static void CollectSourcesInStage(Bounds includedWorldBounds, int includedLayerMask, NavMeshCollectGeometry geometry, int defaultArea, List<NavMeshBuildMarkup> markups, Scene stageProxy, List<NavMeshBuildSource> results)
        {
            CollectSourcesInStage(includedWorldBounds, includedLayerMask, geometry, defaultArea, generateLinksByDefault:false, markups, includeOnlyMarkedObjects:false, stageProxy, results);
        }

        public static void CollectSourcesInStage(
            Transform root, int includedLayerMask, NavMeshCollectGeometry geometry, int defaultArea, bool generateLinksByDefault,
            List<NavMeshBuildMarkup> markups, bool includeOnlyMarkedObjects, Scene stageProxy, List<NavMeshBuildSource> results)
        {
            if (markups == null)
                throw new ArgumentNullException(nameof(markups));
            if (results == null)
                throw new ArgumentNullException(nameof(results));
            if (!stageProxy.IsValid())
                throw new ArgumentException("Stage cannot be deduced from invalid scene.", nameof(stageProxy));

            // root == null is a valid argument

            var empty = new Bounds();
            var resultsArray = CollectSourcesInStageInternal(
                includedLayerMask, empty, root, false, geometry, defaultArea, generateLinksByDefault, markups.ToArray(), includeOnlyMarkedObjects, stageProxy);
            results.Clear();
            results.AddRange(resultsArray);
        }

        public static void CollectSourcesInStage(
            Transform root, int includedLayerMask, NavMeshCollectGeometry geometry, int defaultArea,
            List<NavMeshBuildMarkup> markups, Scene stageProxy, List<NavMeshBuildSource> results)
        {
            CollectSourcesInStage(root, includedLayerMask, geometry, defaultArea, generateLinksByDefault:false, markups, includeOnlyMarkedObjects:false, stageProxy, results);
        }

        [FreeFunction("NavMeshBuilderEditorBindings::CollectSourcesInStageInternal")]
        static extern NavMeshBuildSource[] CollectSourcesInStageInternal(
            int includedLayerMask, Bounds includedWorldBounds, Transform root, bool useBounds, NavMeshCollectGeometry geometry,
            int defaultArea, bool generateLinksByDefault, NavMeshBuildMarkup[] markups, bool includeOnlyMarkedObjects, Scene stageProxy);
    }
}
