// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Bindings;
using UnityEngine.SceneManagement;

namespace UnityEditor.AI
{
    [NativeHeader("Modules/AIEditor/Builder/NavMeshBuilderEditor.bindings.h")]
    [StaticAccessor("NavMeshBuilderEditorBindings", StaticAccessorType.DoubleColon)]
    public partial class NavMeshBuilder
    {
        public static void CollectSourcesInStage(
            Bounds includedWorldBounds, int includedLayerMask, NavMeshCollectGeometry geometry, int defaultArea,
            List<NavMeshBuildMarkup> markups, Scene stageProxy, List<NavMeshBuildSource> results)
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
                includedLayerMask, includedWorldBounds, null, true, geometry, defaultArea, markups.ToArray(), stageProxy);
            results.Clear();
            results.AddRange(resultsArray);
        }

        public static void CollectSourcesInStage(
            Transform root, int includedLayerMask, NavMeshCollectGeometry geometry, int defaultArea,
            List<NavMeshBuildMarkup> markups, Scene stageProxy, List<NavMeshBuildSource> results)
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
                includedLayerMask, empty, root, false, geometry, defaultArea, markups.ToArray(), stageProxy);
            results.Clear();
            results.AddRange(resultsArray);
        }

        static extern NavMeshBuildSource[] CollectSourcesInStageInternal(
            int includedLayerMask, Bounds includedWorldBounds, Transform root, bool useBounds,
            NavMeshCollectGeometry geometry, int defaultArea, NavMeshBuildMarkup[] markups, Scene stageProxy);
    }
}
