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
using UnityEngine.Scripting;

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
            AddDependencyInternal(world, job);
        }
    }

    [NativeContainer]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/AI/NavMeshExperimental.bindings.h")]
    [NativeHeader("Runtime/Math/Matrix4x4.h")]
    [StaticAccessor("NavMeshQueryBindings", StaticAccessorType.DoubleColon)]
    public struct NavMeshQuery : IDisposable
    {
        [NativeDisableUnsafePtrRestriction]
        internal IntPtr             m_NavMeshQuery;
        Allocator                   m_Allocator;
        const string                k_NoBufferAllocatedErrorMessage = "This query has no buffer allocated for pathfinding operations. " +
            "Create a different NavMeshQuery with an explicit node pool size.";

        public NavMeshQuery(NavMeshWorld world, Allocator allocator, int pathNodePoolSize = 0)
        {
            m_Allocator = allocator;
            m_NavMeshQuery = Create(world, pathNodePoolSize);

        }

        public void Dispose()
        {
            Destroy(m_NavMeshQuery);
            m_NavMeshQuery = IntPtr.Zero;
        }

        static extern IntPtr Create(NavMeshWorld world, int nodePoolSize);

        static extern void Destroy(IntPtr navMeshQuery);



        public unsafe PathQueryStatus BeginFindPath(NavMeshLocation start, NavMeshLocation end,
            int areaMask = NavMesh.AllAreas, NativeArray<float> costs = new NativeArray<float>())
        {
            void* costsPtr = costs.Length > 0 ? costs.GetUnsafePtr() : null;
            return BeginFindPath(m_NavMeshQuery, start, end, areaMask, costsPtr);
        }

        public PathQueryStatus UpdateFindPath(int iterations, out int iterationsPerformed)
        {
            return UpdateFindPath(m_NavMeshQuery, iterations, out iterationsPerformed);
        }

        public PathQueryStatus EndFindPath(out int pathSize)
        {
            return EndFindPath(m_NavMeshQuery, out pathSize);
        }

        public unsafe int GetPathResult(NativeSlice<PolygonId> path)
        {
            return GetPathResult(m_NavMeshQuery, path.GetUnsafePtr(), path.Length);
        }

        [ThreadSafe]
        unsafe static extern PathQueryStatus BeginFindPath(IntPtr navMeshQuery, NavMeshLocation start, NavMeshLocation end, int areaMask, void* costs);

        [ThreadSafe]
        static extern PathQueryStatus UpdateFindPath(IntPtr navMeshQuery, int iterations, out int iterationsPerformed);

        [ThreadSafe]
        static extern PathQueryStatus EndFindPath(IntPtr navMeshQuery, out int pathSize);

        [ThreadSafe]
        unsafe static extern int GetPathResult(IntPtr navMeshQuery, void* path, int maxPath);

        // If BeginFindPath/UpdateFindPath/EndFindPath existing NativeArray become invalid...
//      extern NavMeshPathStatus GetPath(out NativeArray<PolygonId> outputPath);

        //void DidScheduleJob(JobHandle handle);


        [ThreadSafe]
        static extern bool IsValidPolygon(IntPtr navMeshQuery, PolygonId polygon);

        public bool IsValid(PolygonId polygon)
        {
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
            return GetAgentTypeIdForPolygon(m_NavMeshQuery, polygon);
        }

        [ThreadSafe]
        static extern bool IsPositionInPolygon(IntPtr navMeshQuery, Vector3 position, PolygonId polygon);

        public NavMeshLocation CreateLocation(Vector3 position, PolygonId polygon)
        {
            return new NavMeshLocation(position, polygon);
        }

        [ThreadSafe]
        static extern NavMeshLocation MapLocation(IntPtr navMeshQuery, Vector3 position, Vector3 extents, int agentTypeID, int areaMask = NavMesh.AllAreas);
        public NavMeshLocation MapLocation(Vector3 position, Vector3 extents, int agentTypeID, int areaMask = NavMesh.AllAreas)
        {
            return MapLocation(m_NavMeshQuery, position, extents, agentTypeID, areaMask);
        }

        [ThreadSafe]
        static unsafe extern void MoveLocations(IntPtr navMeshQuery, void* locations, void* targets, void* areaMasks, int count);
        public unsafe void MoveLocations(NativeSlice<NavMeshLocation> locations, NativeSlice<Vector3> targets, NativeSlice<int> areaMasks)
        {
            MoveLocations(m_NavMeshQuery, locations.GetUnsafePtr(), targets.GetUnsafeReadOnlyPtr(), areaMasks.GetUnsafeReadOnlyPtr(), locations.Length);
        }

        [ThreadSafe]
        unsafe static extern void MoveLocationsInSameAreas(IntPtr navMeshQuery, void* locations, void* targets, int count, int areaMask);
        unsafe public void MoveLocationsInSameAreas(NativeSlice<NavMeshLocation> locations, NativeSlice<Vector3> targets, int areaMask = NavMesh.AllAreas)
        {
            MoveLocationsInSameAreas(m_NavMeshQuery, locations.GetUnsafePtr(), targets.GetUnsafeReadOnlyPtr(), locations.Length, areaMask);
        }

        [ThreadSafe]
        static extern NavMeshLocation MoveLocation(IntPtr navMeshQuery, NavMeshLocation location, Vector3 target, int areaMask);
        public NavMeshLocation MoveLocation(NavMeshLocation location, Vector3 target, int areaMask = NavMesh.AllAreas)
        {
            return MoveLocation(m_NavMeshQuery, location, target, areaMask);
        }

        [ThreadSafe]
        static extern bool GetPortalPoints(IntPtr navMeshQuery, PolygonId polygon, PolygonId neighbourPolygon, out Vector3 left, out Vector3 right);
        public bool GetPortalPoints(PolygonId polygon, PolygonId neighbourPolygon, out Vector3 left, out Vector3 right)
        {
            return GetPortalPoints(m_NavMeshQuery, polygon, neighbourPolygon, out left, out right);
        }

        [ThreadSafe]
        static extern Matrix4x4 PolygonLocalToWorldMatrix(IntPtr navMeshQuery, PolygonId polygon);
        public Matrix4x4 PolygonLocalToWorldMatrix(PolygonId polygon)
        {
            return PolygonLocalToWorldMatrix(m_NavMeshQuery, polygon);
        }

        [ThreadSafe]
        static extern Matrix4x4 PolygonWorldToLocalMatrix(IntPtr navMeshQuery, PolygonId polygon);
        public Matrix4x4 PolygonWorldToLocalMatrix(PolygonId polygon)
        {
            return PolygonWorldToLocalMatrix(m_NavMeshQuery, polygon);
        }

        [ThreadSafe]
        static extern NavMeshPolyTypes GetPolygonType(IntPtr navMeshQuery, PolygonId polygon);
        public NavMeshPolyTypes GetPolygonType(PolygonId polygon)
        {
            return GetPolygonType(m_NavMeshQuery, polygon);
        }

        //NavMeshStatus MoveAlongSurface(NavMeshLocation location, Vector3 targetPosition, int agentTypeID, int areaMask,
        //    out NavMeshLocation outputLocation, NativeArray<PolygonId> visitedBuffer, out int actualVisited);

        //// Trace a ray between two points on the NavMesh.
        //extern bool Raycast(NavMeshLocation location, Vector3 targetPosition, out NavMeshHit hit, int agentTypeID, int areaMask, NativeArray<float> costs);

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

