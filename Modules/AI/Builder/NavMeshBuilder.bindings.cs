// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEngine.AI
{
    ///<summary>Navigation mesh builder interface.</summary>
    [NativeHeader("Modules/AI/Builder/NavMeshBuilder.bindings.h")]
    [StaticAccessor("NavMeshBuilderBindings", StaticAccessorType.DoubleColon)]
    public static class NavMeshBuilder
    {
        ///<summary>Collects renderers or physics colliders, and terrains within a volume. This function might not collect some MeshColliders that are a distance greater than 1E7 from the origin.</summary>
        ///<remarks>For convenience, you can create a list of build sources directly from the current geometry.
        ///
        ///The collection can be controlled in terms of layers, type of geometry and by collecting either by hierarchy or volume.</remarks>
        ///<param name="includedWorldBounds">The queried objects must overlap these bounds to be included in the results.</param>
        ///<param name="includedLayerMask">Specifies which layers are included in the query.</param>
        ///<param name="geometry">Which type of geometry to collect - e.g. physics colliders.</param>
        ///<param name="defaultArea">Area type to assign to results, unless modified by <see cref="NavMeshBuildMarkup" />.</param>
        ///<param name="generateLinksByDefault">If true, all the source will be considered for generating links. Otherwise, only the marked sources will be considered.</param>
        ///<param name="markups">List of markups which allows finer control over how objects are collected.</param>
        ///<param name="includeOnlyMarkedObjects">Specifies if only objects with markups are collected.</param>
        ///<param name="results">List where results are stored, the list is cleared at the beginning of the call.</param>
        public static void CollectSources(
            Bounds includedWorldBounds, int includedLayerMask, NavMeshCollectGeometry geometry, int defaultArea, bool generateLinksByDefault,
            List<NavMeshBuildMarkup> markups, bool includeOnlyMarkedObjects, List<NavMeshBuildSource> results)
        {
            if (markups == null)
                throw new ArgumentNullException(nameof(markups));
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            // Ensure strictly positive extents
            includedWorldBounds.extents = Vector3.Max(includedWorldBounds.extents, 0.001f * Vector3.one);
            var resultsArray = CollectSourcesInternal(
                includedLayerMask, includedWorldBounds, null, true, geometry, defaultArea, generateLinksByDefault,
                markups.ToArray(), includeOnlyMarkedObjects);
            results.Clear();
            results.AddRange(resultsArray);
        }

        ///<summary>Collects renderers or physics colliders, and terrains within a volume. This function might not collect some MeshColliders that are a distance greater than 1E7 from the origin.</summary>
        ///<remarks>For convenience, you can create a list of build sources directly from the current geometry.
        ///
        ///The collection can be controlled in terms of layers, type of geometry and by collecting either by hierarchy or volume.</remarks>
        ///<param name="includedWorldBounds">The queried objects must overlap these bounds to be included in the results.</param>
        ///<param name="includedLayerMask">Specifies which layers are included in the query.</param>
        ///<param name="geometry">Which type of geometry to collect - e.g. physics colliders.</param>
        ///<param name="defaultArea">Area type to assign to results, unless modified by <see cref="NavMeshBuildMarkup" />.</param>
        ///<param name="markups">List of markups which allows finer control over how objects are collected.</param>
        ///<param name="results">List where results are stored, the list is cleared at the beginning of the call.</param>
        public static void CollectSources(
            Bounds includedWorldBounds, int includedLayerMask, NavMeshCollectGeometry geometry, int defaultArea,
            List<NavMeshBuildMarkup> markups, List<NavMeshBuildSource> results)
        {
            CollectSources(includedWorldBounds, includedLayerMask, geometry, defaultArea, false, markups, false, results);
        }

        ///<summary>Collects renderers or physics colliders, and terrains within a transform hierarchy.</summary>
        ///<remarks>For convenience, you can create a list of build sources directly from the current geometry.
        ///
        ///The collection can be controlled in terms of layers, type of geometry and by collecting either by hierarchy or volume.</remarks>
        ///<param name="root">If not null, consider only root and its children in the query; if null, includes everything loaded.</param>
        ///<param name="includedLayerMask">Specifies which layers are included in the query.</param>
        ///<param name="geometry">Which type of geometry to collect - e.g. physics colliders.</param>
        ///<param name="defaultArea">Area type to assign to results, unless modified by NavMeshMarkup.</param>
        ///<param name="generateLinksByDefault">If true, all the source will be considered for generating links. Otherwise, only the marked sources will be considered.</param>
        ///<param name="markups">List of markups which allows finer control over how objects are collected.</param>
        ///<param name="includeOnlyMarkedObjects">Specifies if only objects with markups are collected.</param>
        ///<param name="results">List where results are stored, the list is cleared at the beginning of the call.</param>
        public static void CollectSources(
            Transform root, int includedLayerMask, NavMeshCollectGeometry geometry, int defaultArea, bool generateLinksByDefault,
            List<NavMeshBuildMarkup> markups, bool includeOnlyMarkedObjects, List<NavMeshBuildSource> results)
        {
            if (markups == null)
                throw new ArgumentNullException(nameof(markups));
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            // root == null is a valid argument

            var empty = new Bounds();
            var resultsArray = CollectSourcesInternal(
                includedLayerMask, empty, root, false, geometry, defaultArea, generateLinksByDefault,
                markups.ToArray(), includeOnlyMarkedObjects);
            results.Clear();
            results.AddRange(resultsArray);
        }

        ///<summary>Collects renderers or physics colliders, and terrains within a transform hierarchy.</summary>
        ///<remarks>For convenience, you can create a list of build sources directly from the current geometry.
        ///
        ///The collection can be controlled in terms of layers, type of geometry and by collecting either by hierarchy or volume.</remarks>
        ///<param name="root">If not null, consider only root and its children in the query; if null, includes everything loaded.</param>
        ///<param name="includedLayerMask">Specifies which layers are included in the query.</param>
        ///<param name="geometry">Which type of geometry to collect - e.g. physics colliders.</param>
        ///<param name="defaultArea">Area type to assign to results, unless modified by NavMeshMarkup.</param>
        ///<param name="markups">List of markups which allows finer control over how objects are collected.</param>
        ///<param name="results">List where results are stored, the list is cleared at the beginning of the call.</param>
        public static void CollectSources(
            Transform root, int includedLayerMask, NavMeshCollectGeometry geometry, int defaultArea,
            List<NavMeshBuildMarkup> markups, List<NavMeshBuildSource> results)
        {
            CollectSources(root, includedLayerMask, geometry, defaultArea, false, markups, false, results);
        }

        static extern NavMeshBuildSource[] CollectSourcesInternal(
            int includedLayerMask, Bounds includedWorldBounds, Transform root, bool useBounds,
            NavMeshCollectGeometry geometry, int defaultArea, bool generateLinksByDefault,
            NavMeshBuildMarkup[] markups, bool includeOnlyMarkedObjects);

        // Immediate NavMeshData building
        ///<summary>Builds a NavMesh data object from the provided input sources.</summary>
        ///<remarks>Note: that <see cref="NavMeshBuilder.BuildNavMeshData" /> has same effect as creating a new empty <see cref="NavMeshData" /> and calling <see cref="NavMeshBuilder.UpdateNavMeshData" />.</remarks>
        ///<param name="buildSettings">Settings for the bake process, see <see cref="NavMeshBuildSettings" />.</param>
        ///<param name="sources">List of input geometry used for baking, they describe the surfaces to walk on or obstacles to avoid.</param>
        ///<param name="localBounds">Bounding box relative to position and rotation which describes the volume where the NavMesh should be built. Empty bounds is treated as no bounds, i.e. the NavMesh will cover all the inputs.</param>
        ///<param name="position">Center of the NavMeshData. This specifies the origin for the NavMesh tiles.</param>
        ///<param name="rotation">Orientation of the NavMeshData, you can use this to generate NavMesh with an arbitrary up-vector – e.g. for walkable vertical surfaces.</param>
        ///<returns>The newly built NavMeshData, or null if the NavMeshData was empty or an error occurred.</returns>
        ///<seealso cref="NavMeshBuildSettings.tileSize" />
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

            UpdateNavMeshDataListInternal(data, buildSettings, NoAllocHelpers.CreateReadOnlySpan(sources), localBounds);
            return data;
        }

        // Immediate NavMeshData updating
        ///<summary>Incrementally updates the NavMeshData based on the sources.</summary>
        ///<remarks>Each time NavMeshData is built or updated, the source data is hashed, and the hashes are stored along with the <see cref="NavMeshData" />.
        ///
        ///When called, first the hashes are recomputed and compared and only changed portions are rebuilt. For this reason, the list of sources should always contain all the input geometry, even if they haven't moved or changed. If the list of sources is modified between calls to UpdateNavMeshData the missing/added sources are considered changes. Try to provide the sources that have not changed since the last update in the same relative order as before because their sequence can affect the values of the hashes. This measure ensures that unchanged portions don't get rebuilt unnecessarily.
        ///
        ///You must supply a <see cref="Bounds">Bounds</see> struct for the <c>localBounds</c> parameter.</remarks>
        ///<param name="data">The NavMeshData to update.</param>
        ///<param name="buildSettings">The build settings which is used to update the NavMeshData. The build settings is also hashed along with the data, so changing settings will cause a full rebuild.</param>
        ///<param name="sources">List of input geometry used for baking, they describe the surfaces to walk on or obstacles to avoid.</param>
        ///<param name="localBounds">Bounding box relative to position and rotation which describes the volume where the NavMesh should be built.</param>
        ///<returns>true if the update was successful.</returns>
        ///<seealso cref="NavMeshBuilder.UpdateNavMeshDataAsync" />
        public static bool UpdateNavMeshData(
            NavMeshData data, NavMeshBuildSettings buildSettings, List<NavMeshBuildSource> sources, Bounds localBounds)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (sources == null)
                throw new ArgumentNullException(nameof(sources));

            return UpdateNavMeshDataListInternal(data, buildSettings, NoAllocHelpers.CreateReadOnlySpan(sources), localBounds);
        }

        static extern bool UpdateNavMeshDataListInternal(
            NavMeshData data, NavMeshBuildSettings buildSettings, ReadOnlySpan<NavMeshBuildSource> sources, Bounds localBounds);

        // Async NavMeshData updating
        ///<summary>Asynchronously and incrementally updates the NavMeshData based on the sources.</summary>
        ///<remarks>Each time NavMeshData is built or updated, the source data is hashed, and the hashes are stored along with the NavMeshData.
        ///
        ///
        ///When UpdateNavMeshDataAsync() is called, first the hashes are compared and only changed portions are rebuilt. For this reason, the list of sources should always contain all the input geometry, even if they haven't moved or changed. If the list of sources is modified between calls to UpdateNavMeshDataAsync the missing/added sources are considered changes. Try to provide the sources that have not changed since the last update in the same relative order as before because their sequence can affect the values of the hashes. This measure ensures that unchanged portions don't get rebuilt unnecessarily.
        ///
        ///You must supply a <see cref="Bounds">Bounds</see> struct for the <c>localBounds</c> parameter.</remarks>
        ///<param name="data">The NavMeshData to update.</param>
        ///<param name="buildSettings">The build settings used to update the NavMeshData. The build settings are also hashed along with the data, so changing the settings is likely to cause a full rebuild.</param>
        ///<param name="sources">List of input geometry used for baking, they describe the surfaces to walk on or obstacles to avoid.</param>
        ///<param name="localBounds">Bounding box relative to position and rotation which describes to volume where the NavMesh should be built.</param>
        ///<returns>Can be used to check the progress of the update.</returns>
        ///<seealso cref="NavMeshBuilder.Cancel" />
        public static AsyncOperation UpdateNavMeshDataAsync(
            NavMeshData data, NavMeshBuildSettings buildSettings, List<NavMeshBuildSource> sources, Bounds localBounds)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (sources == null)
                throw new ArgumentNullException(nameof(sources));

            return UpdateNavMeshDataAsyncListInternal(data, buildSettings, NoAllocHelpers.CreateReadOnlySpan(sources), localBounds);
        }

        ///<summary>Cancels an asynchronous update of the specified NavMesh data.</summary>
        ///<param name="data">The data associated with asynchronous updating.</param>
        ///<seealso cref="NavMeshBuilder.UpdateNavMeshDataAsync" />
        [NativeHeader("Modules/AI/NavMeshManager.h")]
        [StaticAccessor("GetNavMeshManager().GetNavMeshBuildManager()", StaticAccessorType.Arrow)]
        [NativeMethod("Purge")]
        public static extern void Cancel(NavMeshData data);

        static extern AsyncOperation UpdateNavMeshDataAsyncListInternal(
            NavMeshData data, NavMeshBuildSettings buildSettings, ReadOnlySpan<NavMeshBuildSource> sources, Bounds localBounds);
    }
}
