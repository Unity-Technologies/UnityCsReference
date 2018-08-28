// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEngine.AI
{
    [NativeHeader("Modules/AI/Builder/NavMeshBuilder.bindings.h")]
    [StaticAccessor("NavMeshBuilderBindings", StaticAccessorType.DoubleColon)]
    public static class NavMeshBuilder
    {
        public static void CollectSources(
            Bounds includedWorldBounds, int includedLayerMask, NavMeshCollectGeometry geometry, int defaultArea,
            List<NavMeshBuildMarkup> markups, List<NavMeshBuildSource> results)
        {
            if (markups == null)
                throw new ArgumentNullException(nameof(markups));
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            // Ensure strictly positive extents
            includedWorldBounds.extents = Vector3.Max(includedWorldBounds.extents, 0.001f * Vector3.one);
            var resultsArray = CollectSourcesInternal(
                includedLayerMask, includedWorldBounds, null, true, geometry, defaultArea, markups.ToArray());
            results.Clear();
            results.AddRange(resultsArray);
        }

        public static void CollectSources(
            Transform root, int includedLayerMask, NavMeshCollectGeometry geometry, int defaultArea,
            List<NavMeshBuildMarkup> markups, List<NavMeshBuildSource> results)
        {
            if (markups == null)
                throw new ArgumentNullException(nameof(markups));
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            // root == null is a valid argument

            var empty = new Bounds();
            var resultsArray = CollectSourcesInternal(
                includedLayerMask, empty, root, false, geometry, defaultArea, markups.ToArray());
            results.Clear();
            results.AddRange(resultsArray);
        }

        static extern NavMeshBuildSource[] CollectSourcesInternal(
            int includedLayerMask, Bounds includedWorldBounds, Transform root, bool useBounds,
            NavMeshCollectGeometry geometry, int defaultArea, NavMeshBuildMarkup[] markups);

        // Immediate NavMeshData building
        public static NavMeshData BuildNavMeshData(
            NavMeshBuildSettings buildSettings, List<NavMeshBuildSource> sources,
            Bounds localBounds, Vector3 position, Quaternion rotation)
        {
            if (sources == null)
                throw new ArgumentNullException(nameof(sources));

            var data = new NavMeshData(buildSettings.agentTypeID)
            {
                position = position,
                rotation = rotation
            };
            UpdateNavMeshDataListInternal(data, buildSettings, sources, localBounds);
            return data;
        }

        // Immediate NavMeshData updating
        public static bool UpdateNavMeshData(
            NavMeshData data, NavMeshBuildSettings buildSettings, List<NavMeshBuildSource> sources, Bounds localBounds)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (sources == null)
                throw new ArgumentNullException(nameof(sources));

            return UpdateNavMeshDataListInternal(data, buildSettings, sources, localBounds);
        }

        static extern bool UpdateNavMeshDataListInternal(
            NavMeshData data, NavMeshBuildSettings buildSettings, object sources, Bounds localBounds);

        // Async NavMeshData updating
        public static AsyncOperation UpdateNavMeshDataAsync(
            NavMeshData data, NavMeshBuildSettings buildSettings, List<NavMeshBuildSource> sources, Bounds localBounds)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (sources == null)
                throw new ArgumentNullException(nameof(sources));

            return UpdateNavMeshDataAsyncListInternal(data, buildSettings, sources, localBounds);
        }

        [NativeHeader("Modules/AI/NavMeshManager.h")]
        [StaticAccessor("GetNavMeshManager().GetNavMeshBuildManager()", StaticAccessorType.Arrow)]
        [NativeMethod("Purge")]
        public static extern void Cancel(NavMeshData data);

        static extern AsyncOperation UpdateNavMeshDataAsyncListInternal(
            NavMeshData data, NavMeshBuildSettings buildSettings, object sources, Bounds localBounds);
    }
}
