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
    public enum NavMeshPolyTypes
    {
        Ground = 0,                    // Regular ground polygons.
        OffMeshConnection = 1          // Off-mesh connections.
    }

    [StaticAccessor("NavMeshWorldBindings", StaticAccessorType.DoubleColon)]
    public struct NavMeshWorld
    {
        internal IntPtr world;

        public bool IsValid()
        {
            return world != IntPtr.Zero;
        }

        public static extern NavMeshWorld GetDefaultWorld();

        static extern void AddDependencyInternal(IntPtr navmesh, JobHandle handle);

        public void AddDependency(JobHandle job)
        {
            if (!IsValid())
                throw new InvalidOperationException("The NavMesh world is invalid.");
            AddDependencyInternal(world, job);
        }
    }

    [NativeContainer]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/AI/NavMeshExperimental.bindings.h")]
    [NativeHeader("Modules/AI/Public/NavMeshBindingTypes.h")]
    [NativeHeader("Runtime/Math/Matrix4x4.h")]
    [StaticAccessor("NavMeshQueryBindings", StaticAccessorType.DoubleColon)]
    public struct NavMeshQuery : IDisposable
    {
        [NativeDisableUnsafePtrRestriction]
        internal IntPtr             m_NavMeshQuery;

        const string                k_NoBufferAllocatedErrorMessage = "This query has no buffer allocated for pathfinding operations. " +
            "Create a different NavMeshQuery with an explicit node pool size.";
        internal AtomicSafetyHandle m_Safety;
        [NativeSetClassTypeToNullOnSchedule]
        DisposeSentinel             m_DisposeSentinel;

        public NavMeshQuery(NavMeshWorld world, Allocator allocator, int pathNodePoolSize = 0)
        {
            if (!world.IsValid())
                throw new ArgumentNullException("world", "Invalid world");
            m_NavMeshQuery = Create(world, pathNodePoolSize);

            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 0, allocator);
            AddQuerySafety(m_NavMeshQuery, m_Safety);
        }

        public void Dispose()
        {

            // When the NavMesh destroys itself it will disable read or write access.
            // Since it has been deallocated, we shouldn't deregister the query from it...
            // We need to extract removeQuery before disposing the handle,
            // because the atomic safety handle stores that state.
            var removeQuery = AtomicSafetyHandle.GetAllowReadOrWriteAccess(m_Safety);

            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);

            if (removeQuery)
                RemoveQuerySafety(m_NavMeshQuery, m_Safety);
            Destroy(m_NavMeshQuery);
            m_NavMeshQuery = IntPtr.Zero;
        }

        static extern IntPtr Create(NavMeshWorld world, int nodePoolSize);

        static extern void Destroy(IntPtr navMeshQuery);

        static extern void AddQuerySafety(IntPtr navMeshQuery, AtomicSafetyHandle handle);
        static extern void RemoveQuerySafety(IntPtr navMeshQuery, AtomicSafetyHandle handle);

        [ThreadSafe]
        static extern bool HasNodePool(IntPtr navMeshQuery);

        public unsafe PathQueryStatus BeginFindPath(NavMeshLocation start, NavMeshLocation end,
            int areaMask = NavMesh.AllAreas, NativeArray<float> costs = new NativeArray<float>())
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

            if (!HasNodePool(m_NavMeshQuery))
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

            var agentTypeStart = GetAgentTypeIdForPolygon(start.polygon);
            if (agentTypeStart < 0)
                throw new ArgumentException("The start location doesn't belong to any active NavMesh surface.", "start");

            var agentTypeEnd = GetAgentTypeIdForPolygon(end.polygon);
            if (agentTypeEnd < 0)
                throw new ArgumentException("The end location doesn't belong to any active NavMesh surface.", "end");

            if (agentTypeStart != agentTypeEnd)
                throw new ArgumentException(string.Format(
                    "The start and end locations belong to different NavMesh surfaces, with agent type IDs {0} and {1}.",
                    agentTypeStart, agentTypeEnd));
            void* costsPtr = costs.Length > 0 ? costs.GetUnsafePtr() : null;
            return BeginFindPath(m_NavMeshQuery, start, end, areaMask, costsPtr);
        }

        public PathQueryStatus UpdateFindPath(int iterations, out int iterationsPerformed)
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

            if (!HasNodePool(m_NavMeshQuery))
                throw new InvalidOperationException(k_NoBufferAllocatedErrorMessage);
            return UpdateFindPath(m_NavMeshQuery, iterations, out iterationsPerformed);
        }

        public PathQueryStatus EndFindPath(out int pathSize)
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

            if (!HasNodePool(m_NavMeshQuery))
                throw new InvalidOperationException(k_NoBufferAllocatedErrorMessage);
            return EndFindPath(m_NavMeshQuery, out pathSize);
        }

        public unsafe int GetPathResult(NativeSlice<PolygonId> path)
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

            if (!HasNodePool(m_NavMeshQuery))
                throw new InvalidOperationException(k_NoBufferAllocatedErrorMessage);
            return GetPathResult(m_NavMeshQuery, path.GetUnsafePtr(), path.Length);
        }

        [ThreadSafe]
        static extern unsafe PathQueryStatus BeginFindPath(IntPtr navMeshQuery, NavMeshLocation start, NavMeshLocation end, int areaMask, void* costs);

        [ThreadSafe]
        static extern PathQueryStatus UpdateFindPath(IntPtr navMeshQuery, int iterations, out int iterationsPerformed);

        [ThreadSafe]
        static extern PathQueryStatus EndFindPath(IntPtr navMeshQuery, out int pathSize);

        [ThreadSafe]
        static extern unsafe int GetPathResult(IntPtr navMeshQuery, void* path, int maxPath);

        // If BeginFindPath/UpdateFindPath/EndFindPath existing NativeArray become invalid...
//      extern NavMeshPathStatus GetPath(out NativeArray<PolygonId> outputPath);

        //void DidScheduleJob(JobHandle handle);


        [ThreadSafe]
        static extern bool IsValidPolygon(IntPtr navMeshQuery, PolygonId polygon);

        public bool IsValid(PolygonId polygon)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            return polygon.polyRef != 0 && IsValidPolygon(m_NavMeshQuery, polygon);
        }

        public bool IsValid(NavMeshLocation location)
        {
            return IsValid(location.polygon);
        }

        [ThreadSafe]
        static extern int GetAgentTypeIdForPolygon(IntPtr navMeshQuery, PolygonId polygon);
        public int GetAgentTypeIdForPolygon(PolygonId polygon)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            return GetAgentTypeIdForPolygon(m_NavMeshQuery, polygon);
        }

        [ThreadSafe]
        static extern bool IsPositionInPolygon(IntPtr navMeshQuery, Vector3 position, PolygonId polygon);

        [ThreadSafe]
        static extern PathQueryStatus GetClosestPointOnPoly(IntPtr navMeshQuery, PolygonId polygon, Vector3 position, out Vector3 nearest);

        public NavMeshLocation CreateLocation(Vector3 position, PolygonId polygon)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            Vector3 nearest;
            var status = GetClosestPointOnPoly(m_NavMeshQuery, polygon, position, out nearest);
            return (status & PathQueryStatus.Success) != 0 ? new NavMeshLocation(nearest, polygon) : new NavMeshLocation();
        }

        [ThreadSafe]
        static extern NavMeshLocation MapLocation(IntPtr navMeshQuery, Vector3 position, Vector3 extents, int agentTypeID, int areaMask = NavMesh.AllAreas);
        public NavMeshLocation MapLocation(Vector3 position, Vector3 extents, int agentTypeID, int areaMask = NavMesh.AllAreas)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            return MapLocation(m_NavMeshQuery, position, extents, agentTypeID, areaMask);
        }

        [ThreadSafe]
        static extern unsafe void MoveLocations(IntPtr navMeshQuery, void* locations, void* targets, void* areaMasks, int count);
        public unsafe void MoveLocations(NativeSlice<NavMeshLocation> locations, NativeSlice<Vector3> targets, NativeSlice<int> areaMasks)
        {
            if (locations.Length != targets.Length || locations.Length != areaMasks.Length)
                throw new ArgumentException("locations.Length, targets.Length and areaMasks.Length must be equal");

            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            MoveLocations(m_NavMeshQuery, locations.GetUnsafePtr(), targets.GetUnsafeReadOnlyPtr(), areaMasks.GetUnsafeReadOnlyPtr(), locations.Length);
        }

        [ThreadSafe]
        static extern unsafe void MoveLocationsInSameAreas(IntPtr navMeshQuery, void* locations, void* targets, int count, int areaMask);
        public unsafe void MoveLocationsInSameAreas(NativeSlice<NavMeshLocation> locations, NativeSlice<Vector3> targets, int areaMask = NavMesh.AllAreas)
        {
            if (locations.Length != targets.Length)
                throw new ArgumentException("locations.Length and targets.Length must be equal");

            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            MoveLocationsInSameAreas(m_NavMeshQuery, locations.GetUnsafePtr(), targets.GetUnsafeReadOnlyPtr(), locations.Length, areaMask);
        }

        [ThreadSafe]
        static extern NavMeshLocation MoveLocation(IntPtr navMeshQuery, NavMeshLocation location, Vector3 target, int areaMask);
        public NavMeshLocation MoveLocation(NavMeshLocation location, Vector3 target, int areaMask = NavMesh.AllAreas)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            return MoveLocation(m_NavMeshQuery, location, target, areaMask);
        }

        [ThreadSafe]
        static extern bool GetPortalPoints(IntPtr navMeshQuery, PolygonId polygon, PolygonId neighbourPolygon, out Vector3 left, out Vector3 right);
        public bool GetPortalPoints(PolygonId polygon, PolygonId neighbourPolygon, out Vector3 left, out Vector3 right)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            return GetPortalPoints(m_NavMeshQuery, polygon, neighbourPolygon, out left, out right);
        }

        [ThreadSafe]
        static extern Matrix4x4 PolygonLocalToWorldMatrix(IntPtr navMeshQuery, PolygonId polygon);
        public Matrix4x4 PolygonLocalToWorldMatrix(PolygonId polygon)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            return PolygonLocalToWorldMatrix(m_NavMeshQuery, polygon);
        }

        [ThreadSafe]
        static extern Matrix4x4 PolygonWorldToLocalMatrix(IntPtr navMeshQuery, PolygonId polygon);
        public Matrix4x4 PolygonWorldToLocalMatrix(PolygonId polygon)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            return PolygonWorldToLocalMatrix(m_NavMeshQuery, polygon);
        }

        [ThreadSafe]
        static extern NavMeshPolyTypes GetPolygonType(IntPtr navMeshQuery, PolygonId polygon);
        public NavMeshPolyTypes GetPolygonType(PolygonId polygon)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            return GetPolygonType(m_NavMeshQuery, polygon);
        }

        //NavMeshStatus MoveAlongSurface(NavMeshLocation location, Vector3 targetPosition, int agentTypeID, int areaMask,
        //    out NavMeshLocation outputLocation, NativeArray<PolygonId> visitedBuffer, out int actualVisited);

        // Trace a ray between two points on the NavMesh.
        [ThreadSafe]
        static extern unsafe PathQueryStatus Raycast(IntPtr navMeshQuery, NavMeshLocation start, Vector3 targetPosition,
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
            var costsPtr = costs.Length == kAreaCount ? costs.GetUnsafePtr() : null;
            var status = Raycast(m_NavMeshQuery, start, targetPosition, areaMask, costsPtr, out hit, null, out pathCount, 0);
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
            var costsPtr = costs.Length == kAreaCount ? costs.GetUnsafePtr() : null;
            var pathPtr = path.Length > 0 ? path.GetUnsafePtr() : null;
            var maxPath = pathPtr != null ? path.Length : 0;
            var status = Raycast(m_NavMeshQuery, start, targetPosition, areaMask, costsPtr, out hit, pathPtr, out pathCount, maxPath);
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

