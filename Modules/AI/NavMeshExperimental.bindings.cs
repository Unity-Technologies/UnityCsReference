// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine.Bindings;
using UnityEngine.AI;

namespace UnityEngine.Experimental.AI
{
    [Obsolete("The experimental PolygonId struct has been deprecated. Use NavNode instead.")]
    public struct PolygonId : IEquatable<PolygonId>
    {
        internal ulong polyRef;

        public bool IsNull() { return polyRef == 0; }

        public static bool operator==(PolygonId x, PolygonId y) { return x.polyRef == y.polyRef; }
        public static bool operator!=(PolygonId x, PolygonId y) { return x.polyRef != y.polyRef; }
        public override int GetHashCode() { return polyRef.GetHashCode(); }
        public bool Equals(PolygonId rhs) { return rhs == this; }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is PolygonId))
                return false;
            var rhs = (PolygonId)obj;
            return rhs == this;
        }
    }

    [Obsolete("The experimental NavMeshLocation struct has been deprecated. Use NavLocation instead.")]
    public struct NavMeshLocation
    {
        public PolygonId polygon { get; }
        public Vector3 position { get; }

        internal NavMeshLocation(Vector3 position, PolygonId polygon)
        {
            this.position = position;
            this.polygon = polygon;
        }
    }

    //public struct NavMeshHit
    //{
    //    public NavMeshLocation  position;
    //    public Vector3          normal;
    //    public float            distance;

    //    public int              area; //Think if this should be a struct etc

    //    public bool             hit;
    //}

    //public struct NavMeshPolyData
    //{
    //    internal unsafe fixed ulong      neighbors[6];
    //    internal unsafe fixed float      vertices[6 * 3];
    //    internal int                     areaType;
    //    internal int                     vertexCount;
    //}

    //public struct NavMeshSegment
    //{
    //    public Vector3  begin;
    //    public Vector3  end;
    //}

    // Keep in sync with the values in NavMeshTypes.h
    [Obsolete("The experimental PathQueryStatus enum has been deprecated. Use NavQueryStatus instead.")]
    [Flags]
    public enum PathQueryStatus
    {
        // High level status.
        Failure = 1 << 31,
        Success = 1 << 30,
        InProgress = 1 << 29,

        // Detail information for status.
        StatusDetailMask = 0x0ffffff,
        WrongMagic = 1 << 0,        // Input data is not recognized.
        WrongVersion = 1 << 1,      // Input data is in wrong version.
        OutOfMemory = 1 << 2,       // Operation ran out of memory.
        InvalidParam = 1 << 3,      // An input parameter was invalid.
        BufferTooSmall = 1 << 4,    // Result buffer for the query was too small to store all results.
        OutOfNodes = 1 << 5,        // Query ran out of nodes during search.
        PartialResult = 1 << 6      // Query did not reach the end location, returning best guess.
    }

    // Flags describing polygon properties. Keep in sync with the enum declared in NavMesh.h
    [Obsolete("The experimental NavMeshPolyTypes enum has been deprecated. Use NavNodeType instead.")]
    public enum NavMeshPolyTypes
    {
        Ground = 0,                    // Regular ground polygons.
        OffMeshConnection = 1          // Off-mesh connections.
    }

    [Obsolete("The experimental NavMeshWorld struct has been deprecated. Use NavWorld instead.")]
    [StaticAccessor("NavMeshWorldBindingsExperimental", StaticAccessorType.DoubleColon)]
    [NativeHeader("Modules/AI/NavMeshExperimental.bindings.h")]
    [NativeType(CodegenOptions.Auto, "NavMeshWorldExp")]
    public struct NavMeshWorld
    {
        internal IntPtr world;

        public bool IsValid()
        {
            return world != IntPtr.Zero;
        }

        static extern NavMeshWorld GetDefaultWorldExp();
        public static NavMeshWorld GetDefaultWorld()
        {
            return GetDefaultWorldExp();
        }

        static extern void AddDependencyInternalExp(IntPtr navmesh, JobHandle handle);

        public void AddDependency(JobHandle job)
        {
            if (!IsValid())
                throw new InvalidOperationException("The NavMesh world is invalid.");
            AddDependencyInternalExp(world, job);
        }
    }

    [Obsolete("The experimental NavMeshQuery struct has been deprecated. Use NavWorld instead.")]
    [NativeContainer]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/AI/NavMeshExperimental.bindings.h")]
    [NativeHeader("Modules/AI/Public/NavMeshBindingTypes.h")]
    [NativeHeader("Runtime/Math/Matrix4x4.h")]
    [StaticAccessor("NavMeshQueryBindingsExperimental", StaticAccessorType.DoubleColon)]
    [NativeType(CodegenOptions.Auto, "NavMeshQueryExp")]   
    public struct NavMeshQuery : IDisposable
    {
        [NativeDisableUnsafePtrRestriction]
        internal IntPtr             m_NavMeshQuery;

        const string                k_NoBufferAllocatedErrorMessage = "This query has no buffer allocated for pathfinding operations. " +
            "Create a different NavMeshQuery with an explicit node pool size.";
        internal AtomicSafetyHandle m_Safety;

        // Each node in the pool stores an index to the next node anywhere in the pool.
        // To save memory, indices stored in the node pool are of type unsigned short.
        // Keep in sync with kMaxNavMeshNodePoolSize = USHRT_MAX from NavMeshNode.h
        const int k_MaxNavMeshNodePoolSize = ushort.MaxValue;

        public NavMeshQuery(NavMeshWorld world, Allocator allocator, int pathNodePoolSize = 0)
        {
            if (!world.IsValid())
                throw new ArgumentNullException("world", "Invalid world");

            if (pathNodePoolSize < 0 || pathNodePoolSize > k_MaxNavMeshNodePoolSize)
                throw new ArgumentException(
                    $"The path node pool size ({pathNodePoolSize}) must be greater than or equal to 0 and less than {k_MaxNavMeshNodePoolSize + 1}.",
                    nameof(pathNodePoolSize));
            m_NavMeshQuery = CreateExp(world, pathNodePoolSize);

            UnsafeUtility.LeakRecord(m_NavMeshQuery, LeakCategory.NavMeshQuery, 0);
            AtomicSafetyHandle.CreateHandle(out m_Safety, allocator);
            AddQuerySafetyExp(m_NavMeshQuery, m_Safety);
        }

        public void Dispose()
        {

            // When the NavMesh destroys itself it will disable read or write access.
            // Since it has been deallocated, we shouldn't deregister the query from it...
            // We need to extract removeQuery before disposing the handle,
            // because the atomic safety handle stores that state.
            var removeQuery = AtomicSafetyHandle.GetAllowReadOrWriteAccess(m_Safety);

            AtomicSafetyHandle.DisposeHandle(ref m_Safety);

            if (removeQuery)
                RemoveQuerySafetyExp(m_NavMeshQuery, m_Safety);
            UnsafeUtility.LeakErase(m_NavMeshQuery, LeakCategory.NavMeshQuery);
            DestroyExp(m_NavMeshQuery);
            m_NavMeshQuery = IntPtr.Zero;
        }

        static extern IntPtr CreateExp(NavMeshWorld world, int nodePoolSize);

        static extern void DestroyExp(IntPtr navMeshQuery);

        static extern void AddQuerySafetyExp(IntPtr navMeshQuery, AtomicSafetyHandle handle);
        static extern void RemoveQuerySafetyExp(IntPtr navMeshQuery, AtomicSafetyHandle handle);

        [NativeMethod(IsThreadSafe = true)]
        static extern bool HasNodePoolExp(IntPtr navMeshQuery);

        public unsafe PathQueryStatus BeginFindPath(NavMeshLocation start, NavMeshLocation end,
            int areaMask = NavMesh.AllAreas, NativeArray<float> costs = new NativeArray<float>())
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

            if (!HasNodePoolExp(m_NavMeshQuery))
                throw new InvalidOperationException(k_NoBufferAllocatedErrorMessage);

            const int kAreaCount = 32;
            if (costs.Length != 0)
            {
                if (costs.Length != kAreaCount)
                    throw new ArgumentException(
                        string.Format("The number of costs ({0}) must be exactly {1}, one for each possible area type.", costs.Length, kAreaCount)
                        , "costs");

                for (var i = 0; i < costs.Length; i++)
                {
                    if (costs[i] < 1.0f)
                        throw new ArgumentException(
                            string.Format("The area cost ({0}) at index ({1}) must be greater or equal to 1.", costs[i], i), "costs");
                }
            }

            if (!IsValid(start.polygon))
                throw new ArgumentException("The start location doesn't belong to any active NavMesh surface.", "start");


            if (!IsValid(end.polygon))
                throw new ArgumentException("The end location doesn't belong to any active NavMesh surface.", "end");

            var agentTypeStart = GetAgentTypeIdForPolygon(start.polygon);
            var agentTypeEnd = GetAgentTypeIdForPolygon(end.polygon);
            if (agentTypeStart != agentTypeEnd)
                throw new ArgumentException(string.Format(
                    "The start and end locations belong to different NavMesh surfaces, with agent type IDs {0} and {1}.",
                    agentTypeStart, agentTypeEnd));
            void* costsPtr = costs.Length > 0 ? costs.GetUnsafePtr() : null;
            return BeginFindPathExp(m_NavMeshQuery, start, end, areaMask, costsPtr);
        }

        public PathQueryStatus UpdateFindPath(int iterations, out int iterationsPerformed)
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

            if (!HasNodePoolExp(m_NavMeshQuery))
                throw new InvalidOperationException(k_NoBufferAllocatedErrorMessage);
            return UpdateFindPathExp(m_NavMeshQuery, iterations, out iterationsPerformed);
        }

        public PathQueryStatus EndFindPath(out int pathSize)
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

            if (!HasNodePoolExp(m_NavMeshQuery))
                throw new InvalidOperationException(k_NoBufferAllocatedErrorMessage);
            return EndFindPathExp(m_NavMeshQuery, out pathSize);
        }

        public unsafe int GetPathResult(NativeSlice<PolygonId> path)
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

            if (!HasNodePoolExp(m_NavMeshQuery))
                throw new InvalidOperationException(k_NoBufferAllocatedErrorMessage);
            return GetPathResultExp(m_NavMeshQuery, path.GetUnsafePtr(), path.Length);
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern unsafe PathQueryStatus BeginFindPathExp(IntPtr navMeshQuery, NavMeshLocation start, NavMeshLocation end, int areaMask, void* costs);

        [NativeMethod(IsThreadSafe = true)]
        static extern PathQueryStatus UpdateFindPathExp(IntPtr navMeshQuery, int iterations, out int iterationsPerformed);

        [NativeMethod(IsThreadSafe = true)]
        static extern PathQueryStatus EndFindPathExp(IntPtr navMeshQuery, out int pathSize);

        [NativeMethod(IsThreadSafe = true)]
        static extern unsafe int GetPathResultExp(IntPtr navMeshQuery, void* path, int maxPath);

        // If BeginFindPath/UpdateFindPath/EndFindPath existing NativeArray become invalid...
//      extern NavMeshPathStatus GetPath(out NativeArray<PolygonId> outputPath);

        //void DidScheduleJob(JobHandle handle);


        [NativeMethod(IsThreadSafe = true)]
        static extern bool IsValidPolygonExp(IntPtr navMeshQuery, PolygonId polygon);

        public bool IsValid(PolygonId polygon)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            return polygon.polyRef != 0 && IsValidPolygonExp(m_NavMeshQuery, polygon);
        }

        public bool IsValid(NavMeshLocation location)
        {
            return IsValid(location.polygon);
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern int GetAgentTypeIdForPolygonExp(IntPtr navMeshQuery, PolygonId polygon);
        public int GetAgentTypeIdForPolygon(PolygonId polygon)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            return GetAgentTypeIdForPolygonExp(m_NavMeshQuery, polygon);
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern bool IsPositionInPolygonExp(IntPtr navMeshQuery, Vector3 position, PolygonId polygon);

        [NativeMethod(IsThreadSafe = true)]
        static extern PathQueryStatus GetClosestPointOnPolyExp(IntPtr navMeshQuery, PolygonId polygon, Vector3 position, out Vector3 nearest);

        public NavMeshLocation CreateLocation(Vector3 position, PolygonId polygon)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            Vector3 nearest;
            var status = GetClosestPointOnPolyExp(m_NavMeshQuery, polygon, position, out nearest);
            return (status & PathQueryStatus.Success) != 0 ? new NavMeshLocation(nearest, polygon) : new NavMeshLocation();
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern NavMeshLocation MapLocationExp(IntPtr navMeshQuery, Vector3 position, Vector3 extents, int agentTypeID, int areaMask = NavMesh.AllAreas);
        public NavMeshLocation MapLocation(Vector3 position, Vector3 extents, int agentTypeID, int areaMask = NavMesh.AllAreas)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            return MapLocationExp(m_NavMeshQuery, position, extents, agentTypeID, areaMask);
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern unsafe void MoveLocationsExp(IntPtr navMeshQuery, void* locations, void* targets, void* areaMasks, int count);
        public unsafe void MoveLocations(NativeSlice<NavMeshLocation> locations, NativeSlice<Vector3> targets, NativeSlice<int> areaMasks)
        {
            if (locations.Length != targets.Length || locations.Length != areaMasks.Length)
                throw new ArgumentException("locations.Length, targets.Length and areaMasks.Length must be equal");

            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            MoveLocationsExp(m_NavMeshQuery, locations.GetUnsafePtr(), targets.GetUnsafeReadOnlyPtr(), areaMasks.GetUnsafeReadOnlyPtr(), locations.Length);
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern unsafe void MoveLocationsInSameAreasExp(IntPtr navMeshQuery, void* locations, void* targets, int count, int areaMask);
        public unsafe void MoveLocationsInSameAreas(NativeSlice<NavMeshLocation> locations, NativeSlice<Vector3> targets, int areaMask = NavMesh.AllAreas)
        {
            if (locations.Length != targets.Length)
                throw new ArgumentException("locations.Length and targets.Length must be equal");

            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            MoveLocationsInSameAreasExp(m_NavMeshQuery, locations.GetUnsafePtr(), targets.GetUnsafeReadOnlyPtr(), locations.Length, areaMask);
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern NavMeshLocation MoveLocationExp(IntPtr navMeshQuery, NavMeshLocation location, Vector3 target, int areaMask);
        public NavMeshLocation MoveLocation(NavMeshLocation location, Vector3 target, int areaMask = NavMesh.AllAreas)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            return MoveLocationExp(m_NavMeshQuery, location, target, areaMask);
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern bool GetPortalPointsExp(IntPtr navMeshQuery, PolygonId polygon, PolygonId neighbourPolygon, out Vector3 left, out Vector3 right);
        public bool GetPortalPoints(PolygonId polygon, PolygonId neighbourPolygon, out Vector3 left, out Vector3 right)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            return GetPortalPointsExp(m_NavMeshQuery, polygon, neighbourPolygon, out left, out right);
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern Matrix4x4 PolygonLocalToWorldMatrixExp(IntPtr navMeshQuery, PolygonId polygon);
        public Matrix4x4 PolygonLocalToWorldMatrix(PolygonId polygon)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            return PolygonLocalToWorldMatrixExp(m_NavMeshQuery, polygon);
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern Matrix4x4 PolygonWorldToLocalMatrixExp(IntPtr navMeshQuery, PolygonId polygon);
        public Matrix4x4 PolygonWorldToLocalMatrix(PolygonId polygon)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            return PolygonWorldToLocalMatrixExp(m_NavMeshQuery, polygon);
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern NavMeshPolyTypes GetPolygonTypeExp(IntPtr navMeshQuery, PolygonId polygon);
        public NavMeshPolyTypes GetPolygonType(PolygonId polygon)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            return GetPolygonTypeExp(m_NavMeshQuery, polygon);
        }

        //NavMeshStatus MoveAlongSurface(NavMeshLocation location, Vector3 targetPosition, int agentTypeID, int areaMask,
        //    out NavMeshLocation outputLocation, NativeArray<PolygonId> visitedBuffer, out int actualVisited);

        // Trace a ray between two points on the NavMesh.
        [NativeMethod(IsThreadSafe = true)]
        static extern unsafe PathQueryStatus RaycastExp(IntPtr navMeshQuery, NavMeshLocation start, Vector3 targetPosition,
            int areaMask, void* costs, out NavMeshHit hit, void* path, out int pathCount, int maxPath);

        public unsafe PathQueryStatus Raycast(out NavMeshHit hit, NavMeshLocation start, Vector3 targetPosition,
            int areaMask = NavMesh.AllAreas, NativeArray<float> costs = new NativeArray<float>())
        {
            const int kAreaCount = 32;
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);

            if (costs.Length != 0)
            {
                if (costs.Length != kAreaCount)
                    throw new ArgumentException(
                        string.Format("The number of costs ({0}) must be exactly {1}, one for each possible area type.", costs.Length, kAreaCount), "costs");
            }
            int pathCount;
            void* costsPtr = costs.Length == kAreaCount ? costs.GetUnsafePtr() : null;
            var status = RaycastExp(m_NavMeshQuery, start, targetPosition, areaMask, costsPtr, out hit, null, out pathCount, 0);
            status &= ~PathQueryStatus.BufferTooSmall;
            return status;
        }

        public unsafe PathQueryStatus Raycast(out NavMeshHit hit, NativeSlice<PolygonId> path, out int pathCount,
            NavMeshLocation start, Vector3 targetPosition,
            int areaMask = NavMesh.AllAreas, NativeArray<float> costs = new NativeArray<float>())
        {
            const int kAreaCount = 32;
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);

            if (costs.Length != 0)
            {
                if (costs.Length != kAreaCount)
                    throw new ArgumentException(
                        string.Format("The number of costs ({0}) must be exactly {1}, one for each possible area type.", costs.Length, kAreaCount), "costs");
            }
            void* costsPtr = costs.Length == kAreaCount ? costs.GetUnsafePtr() : null;
            void* pathPtr = path.Length > 0 ? path.GetUnsafePtr() : null;
            var maxPath = pathPtr != null ? path.Length : 0;
            var status = RaycastExp(m_NavMeshQuery, start, targetPosition, areaMask, costsPtr, out hit, pathPtr, out pathCount, maxPath);
            return status;
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern unsafe PathQueryStatus GetEdgesAndNeighborsExp(IntPtr navMeshQuery, PolygonId node, int maxVerts, int maxNei,
            void* verts, void* neighbors, void* edgeIndices,
            out int vertCount, out int neighborsCount);

        public unsafe PathQueryStatus GetEdgesAndNeighbors(PolygonId node,
            NativeSlice<Vector3> edgeVertices, NativeSlice<PolygonId> neighbors, NativeSlice<byte> edgeIndices,
            out int verticesCount, out int neighborsCount)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);

            if (edgeIndices.Length != neighbors.Length && neighbors.Length > 0 && edgeIndices.Length > 0)
            {
                throw new ArgumentException($"The length of the {nameof(edgeIndices)} buffer ({edgeIndices.Length}) " +
                    $"needs to be the same as that of the {nameof(neighbors)} buffer ({neighbors.Length}) " +
                    "because the elements from the two arrays will pair up at the same index.");
            }
            void* vertPtr = edgeVertices.Length > 0 ? edgeVertices.GetUnsafePtr() : null;
            void* neiPtr = neighbors.Length > 0 ? neighbors.GetUnsafePtr() : null;
            void* edgesPtr = edgeIndices.Length > 0 ? edgeIndices.GetUnsafePtr() : null;
            var maxVertices = edgeVertices.Length;
            var maxNeighbors = neighbors.Length > 0 ? neighbors.Length : edgeIndices.Length;
            var status = GetEdgesAndNeighborsExp(m_NavMeshQuery, node, maxVertices, maxNeighbors,
                vertPtr, neiPtr, edgesPtr,
                out verticesCount, out neighborsCount);
            return status;
        }

        //// Polygon Queries
        //public NavMeshPolyData GetPolygon(PolygonId poly);
        //public void GetPolygon(NativeArray<PolygonId> polygonIDs, NativeArray<NavMeshPolyData> polygons);
        //public void GetPolygons(MappedPosition position, float distance, NativeList<NavMeshPolyData> polygons);

        //public static void LocalizePolygonIndices(NativeArray<NavMeshPolyData> polygons);

        //// Segments
        //public NativeArray<NavMeshSegment> FindBoundarySegments (MappedPosition position, float distance, Allocator allocator);

        //// Voxel rasterize
        //public void Rasterize (MappedPosition position, Quaternion orientation, float cellWidth, float cellHeight, NativeArray2D<bool> grid);

        //// DetailMesh queries
        //void ProjectToDetailMesh(NativeArray<MappedPosition> positions, NativeArray<Vector3> outputPositions);
    }
}

