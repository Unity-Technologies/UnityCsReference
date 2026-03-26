// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Bindings;
// ReSharper disable SuggestVarOrType_Elsewhere

namespace Unity.AI.Navigation.LowLevel;

[StructLayout(LayoutKind.Sequential)]
struct NavMeshPointers
{
    public IntPtr m_NavMesh;
    public IntPtr m_ImmutableQuery;
    public uint m_UniqueId;
}

[NativeContainer]
[NativeContainerIsReadOnly]
[StructLayout(LayoutKind.Sequential)]
[StaticAccessor("NavMeshLowLevel::NavWorldBindings", StaticAccessorType.DoubleColon)]
public struct NavWorld : IDisposable, IEquatable<NavWorld>
{
    [NativeDisableUnsafePtrRestriction]
    internal IntPtr m_World;
    [NativeDisableUnsafePtrRestriction]
    internal IntPtr m_ImmutableQuery;
    internal uint m_UniqueId;
    internal readonly IntPtr navMesh => m_World;
    internal readonly uint uniqueId => m_UniqueId;

    internal AtomicSafetyHandle m_Safety;

    internal static readonly int s_staticSafetyId = AtomicSafetyHandle.NewStaticSafetyId<NavWorld>();

    const string k_NoBufferAllocatedErrorMessage =
        "This query has no valid buffer allocated for pathfinding operations. " +
        "Create a different NavQueryBuffer with an explicit node pool size.";

    [NativeMethod(IsThreadSafe = true)]
    static extern bool IsWorldForQueryInternal(IntPtr navMesh, IntPtr query, uint queryUniqueId);

    [NativeMethod(IsThreadSafe = true)]
    static extern bool IsValidWorldInternal(IntPtr navMesh, IntPtr immutableQuery, uint uniqueId);

    public bool IsValid()
    {
        return m_World != IntPtr.Zero
            && m_ImmutableQuery != IntPtr.Zero
            && IsValidWorldInternal(m_World, m_ImmutableQuery, m_UniqueId);
    }

    static extern NavMeshPointers GetDefaultWorldInternal();

    public static NavWorld GetDefaultWorld()
    {
        var pointers = GetDefaultWorldInternal();

        var world = new NavWorld
        {
            m_World = pointers.m_NavMesh,
            m_ImmutableQuery = pointers.m_ImmutableQuery,
            m_UniqueId = pointers.m_UniqueId
        };

        if (!world.IsValid())
            throw new InvalidOperationException(
                "The default NavMesh world could not be created, " +
                "most likely because there is not enough memory left.");

        AtomicSafetyHandle.CreateHandle(out world.m_Safety, Allocator.Persistent);
        AtomicSafetyHandle.SetStaticSafetyId(ref world.m_Safety, s_staticSafetyId);
        AddWorldSafety(world.m_World, world.m_Safety);
        return world;
    }

    // Explicit cleanup of the safety handle which is otherwise
    // removed only when the underlying NavMesh is destroyed.
    public void Dispose()
    {
        if (AtomicSafetyHandle.IsValidNonDefaultHandle(m_Safety))
        {
            var canRemoveSafety = AtomicSafetyHandle.GetAllowReadOrWriteAccess(m_Safety);

            AtomicSafetyHandle.DisposeHandle(ref m_Safety);

            if (canRemoveSafety && m_World != IntPtr.Zero)
                RemoveWorldSafety(m_World, m_Safety);
        }
        m_World = IntPtr.Zero;
        m_ImmutableQuery = IntPtr.Zero;
    }

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public static bool operator ==(NavWorld left, NavWorld right)
    {
        return left.Equals(right);
    }

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public static bool operator !=(NavWorld left, NavWorld right)
    {
        return !left.Equals(right);
    }

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public readonly bool Equals(NavWorld other)
    {
        return m_World == other.m_World
            && m_ImmutableQuery == other.m_ImmutableQuery
            && m_UniqueId == other.m_UniqueId;
    }

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public readonly override bool Equals(object obj)
    {
        return obj is NavWorld other && Equals(other);
    }

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public readonly override int GetHashCode()
    {
        return HashCode.Combine(m_World, m_ImmutableQuery, m_UniqueId);
    }

    void CheckValidPtrAndThrow()
    {
        if (!IsValid())
            throw new InvalidOperationException(
                "The NavMesh world is invalid. Call NavWorld.GetDefaultWorld() to obtain a valid world.");

        AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
    }

    void CheckBufferMatchAndThrow(NavQueryBuffer queryBuffer)
    {
        if (!IsWorldForQueryInternal(m_World, queryBuffer.id, queryBuffer.worldUniqueId))
            throw new InvalidOperationException(
                "The NavWorld cannot use this NavQueryBuffer because it was created for a different NavWorld.");
    }

    static extern void AddWorldSafety(IntPtr navMesh, AtomicSafetyHandle handle);

    static extern void RemoveWorldSafety(IntPtr navMesh, AtomicSafetyHandle handle);

    static extern void AddDependencyInternal(IntPtr navWorld, JobHandle handle);

    public void AddDependency(JobHandle job)
    {
        CheckValidPtrAndThrow();

        if (JobsUtility.IsExecutingJob)
            throw new InvalidOperationException("NavWorld.AddDependency cannot be called from a job.");
        AddDependencyInternal(m_World, job);
    }

    [NativeMethod(IsThreadSafe = true)]
    static extern NavLocation MapLocation(IntPtr navMeshQuery, Vector3 position, Vector3 extents,
        int agentTypeID, int areaMask = NavMesh.AllAreas);

    public NavLocation MapLocation(Vector3 position, Vector3 extents, int agentTypeId,
        int areaMask = NavMesh.AllAreas)
    {
        CheckValidPtrAndThrow();
        return MapLocation(m_ImmutableQuery, position, extents, agentTypeId, areaMask);
    }

    public unsafe NavQueryStatus BeginFindPath(NavQueryBuffer queryBuffer,
        NavLocation start, NavLocation end,
        int areaMask = NavMesh.AllAreas, NativeArray<float> costs = new())
    {
        CheckValidPtrAndThrow();
        CheckBufferMatchAndThrow(queryBuffer);
        queryBuffer.CheckWriteSafetyAndThrow();

        if (!queryBuffer.HasNodePool())
            throw new InvalidOperationException(k_NoBufferAllocatedErrorMessage);

        const int kAreaCount = 32;
        if (costs.Length != 0)
        {
            if (costs.Length != kAreaCount)
                throw new ArgumentException(
                    string.Format(
                        "The number of costs ({0}) must be exactly {1}, one for each possible area type.",
                        costs.Length, kAreaCount), nameof(costs));

            for (var i = 0; i < costs.Length; i++)
            {
                if (costs[i] < 1.0f)
                    throw new ArgumentException(
                        string.Format(
                            "The area cost ({0}) at index ({1}) must be greater or equal to 1.",
                            costs[i], i), nameof(costs));
            }
        }

        if (!IsValid(start.node))
            throw new ArgumentException(
                "The start location doesn't belong to any active NavMesh surface.",
                nameof(start));


        if (!IsValid(end.node))
            throw new ArgumentException(
                "The end location doesn't belong to any active NavMesh surface.", nameof(end));

        var agentTypeStart = GetAgentTypeIdForNode(queryBuffer.id, start.node);
        var agentTypeEnd = GetAgentTypeIdForNode(queryBuffer.id, end.node);
        if (agentTypeStart != agentTypeEnd)
            throw new ArgumentException(string.Format(
                "The start and end locations belong to different NavMesh surfaces, with agent type IDs {0} and {1}.",
                agentTypeStart, agentTypeEnd));
        void* costsPtr = costs.Length > 0 ? costs.GetUnsafePtr() : null;
        return BeginFindPath(queryBuffer.id, start, end, areaMask, costsPtr);
    }

    public NavQueryStatus ContinueFindPath(NavQueryBuffer queryBuffer, int maxNodesToVisit, out int nodesVisited)
    {
        CheckValidPtrAndThrow();
        CheckBufferMatchAndThrow(queryBuffer);
        queryBuffer.CheckWriteSafetyAndThrow();

        if (!queryBuffer.HasNodePool())
            throw new InvalidOperationException(k_NoBufferAllocatedErrorMessage);
        return ContinueFindPath(queryBuffer.id, maxNodesToVisit, out nodesVisited);
    }

    public NavQueryStatus EndFindPath(NavQueryBuffer queryBuffer, out int pathSize)
    {
        CheckValidPtrAndThrow();
        CheckBufferMatchAndThrow(queryBuffer);
        queryBuffer.CheckWriteSafetyAndThrow();

        if (!queryBuffer.HasNodePool())
            throw new InvalidOperationException(k_NoBufferAllocatedErrorMessage);

        return EndFindPath(queryBuffer.id, out pathSize);
    }

    public unsafe int GetResultFromFindPath(NavQueryBuffer queryBuffer, NativeSlice<NavNode> path)
    {
        CheckValidPtrAndThrow();
        CheckBufferMatchAndThrow(queryBuffer);
        queryBuffer.CheckWriteSafetyAndThrow();

        if (!queryBuffer.HasNodePool())
            throw new InvalidOperationException(k_NoBufferAllocatedErrorMessage);

        return GetResultFromFindPath(queryBuffer.id, path.GetUnsafePtr(), path.Length);
    }

    [NativeMethod(IsThreadSafe = true)]
    static extern unsafe NavQueryStatus BeginFindPath(IntPtr navMeshQuery, NavLocation start,
        NavLocation end, int areaMask, void* costs);

    [NativeMethod(IsThreadSafe = true)]
    static extern NavQueryStatus ContinueFindPath(IntPtr navMeshQuery, int maxNodesToVisit, out int nodesVisited);

    [NativeMethod(IsThreadSafe = true)]
    static extern NavQueryStatus EndFindPath(IntPtr navMeshQuery, out int pathSize);

    [NativeMethod(IsThreadSafe = true)]
    static extern unsafe int GetResultFromFindPath(IntPtr navMeshQuery, void* path, int maxPath);

    [NativeMethod(IsThreadSafe = true)]
    static extern bool IsValidNode(IntPtr navMeshQuery, NavNode node);

    public bool IsValid(NavNode node)
    {
        CheckValidPtrAndThrow();
        return node.m_PolyRef != 0 && IsValidNode(m_ImmutableQuery, node);
    }

    public bool IsValid(NavLocation location)
    {
        return IsValid(location.node);
    }

    [NativeMethod(IsThreadSafe = true)]
    static extern int GetAgentTypeIdForNode(IntPtr navMeshQuery, NavNode node);

    public int GetAgentTypeIdForNode(NavNode node)
    {
        CheckValidPtrAndThrow();
        return GetAgentTypeIdForNode(m_ImmutableQuery, node);
    }

    [NativeMethod(IsThreadSafe = true)]
    static extern int GetAreaIndexForNode(IntPtr navMeshQuery, NavNode node);
    public int GetAreaIndexForNode(NavNode node)
    {
        CheckValidPtrAndThrow();
        return GetAreaIndexForNode(m_ImmutableQuery, node);
    }

    [NativeMethod(IsThreadSafe = true)]
    static extern NavQueryStatus GetClosestPointOnPoly(IntPtr navMeshQuery, NavNode node, Vector3 position,
        out Vector3 nearest);

    public NavLocation CreateLocation(Vector3 position, NavNode node)
    {
        CheckValidPtrAndThrow();
        var status = GetClosestPointOnPoly(m_ImmutableQuery, node, position, out var nearest);
        return (status & NavQueryStatus.Success) != 0
            ? new NavLocation(nearest, node)
            : new NavLocation();
    }

    [NativeMethod(IsThreadSafe = true)]
    static extern unsafe void MoveLocations(IntPtr navMeshQuery, void* locations, void* targets, void* areaMasks,
        int count);

    public unsafe void MoveLocations(NativeSlice<NavLocation> locations, NativeSlice<Vector3> destinations,
        NativeSlice<int> areaMasks)
    {
        CheckValidPtrAndThrow();

        if (locations.Length != destinations.Length || locations.Length != areaMasks.Length)
            throw new ArgumentException("locations.Length, destinations.Length and areaMasks.Length must be equal");
        MoveLocations(m_ImmutableQuery, locations.GetUnsafePtr(), destinations.GetUnsafeReadOnlyPtr(),
            areaMasks.GetUnsafeReadOnlyPtr(), locations.Length);
    }

    [NativeMethod(IsThreadSafe = true)]
    static extern unsafe void MoveLocationsInSameAreas(IntPtr navMeshQuery, void* locations, void* targets,
        int count, int areaMask);

    public unsafe void MoveLocations(NativeSlice<NavLocation> locations,
        NativeSlice<Vector3> destinations, int areaMask = NavMesh.AllAreas)
    {
        CheckValidPtrAndThrow();

        if (locations.Length != destinations.Length)
            throw new ArgumentException("locations.Length and destinations.Length must be equal");
        MoveLocationsInSameAreas(m_ImmutableQuery, locations.GetUnsafePtr(), destinations.GetUnsafeReadOnlyPtr(),
            locations.Length, areaMask);
    }

    [NativeMethod(IsThreadSafe = true)]
    static extern NavLocation MoveLocation(IntPtr navMeshQuery, NavLocation location, Vector3 target,
        int areaMask);

    public NavLocation MoveLocation(NavLocation location, Vector3 destination, int areaMask = NavMesh.AllAreas)
    {
        CheckValidPtrAndThrow();
        return MoveLocation(m_ImmutableQuery, location, destination, areaMask);
    }

    [NativeMethod(IsThreadSafe = true)]
    static extern bool GetPortalPoints(IntPtr navMeshQuery, NavNode node, NavNode neighbor,
        out Vector3 left, out Vector3 right);

    public bool GetPortalPoints(NavNode node, NavNode neighbor, out Vector3 left, out Vector3 right)
    {
        CheckValidPtrAndThrow();
        return GetPortalPoints(m_ImmutableQuery, node, neighbor, out left, out right);
    }

    [NativeMethod(IsThreadSafe = true)]
    static extern void GetInstanceTransform(IntPtr navMesh, NavNode node,
        out Vector3 position, out Quaternion rotation);

    public void GetInstanceTransform(NavNode node, out Vector3 position, out Quaternion rotation)
    {
        CheckValidPtrAndThrow();
        GetInstanceTransform(m_World, node, out position, out rotation);
    }

    [NativeMethod(IsThreadSafe = true)]
    [StaticAccessor("NavMesh", StaticAccessorType.DoubleColon)]
    [NativeName("DecodePolyIdType")]
    static extern int GetNodeTypeInternal(NavNode node);

    public NavNodeType GetNodeType(NavNode node)
    {
        CheckValidPtrAndThrow();
        if (node.IsNull())
            return NavNodeType.Undefined;

        return (NavNodeType)GetNodeTypeInternal(node);
    }

    [NativeMethod(IsThreadSafe = true)]
    [StaticAccessor("GetNavMeshManager()")]
    [NativeName("GetLinkPolyRef")]
    static extern NavNode GetLinkNode(int linkInstance);

    public NavNode GetLinkNode(NavMeshLinkInstance linkInstance)
    {
        CheckValidPtrAndThrow();
        return GetLinkNode(linkInstance.id);
    }

    // Trace a ray between two points on the NavMesh.
    [NativeMethod(IsThreadSafe = true)]
    static extern unsafe NavQueryStatus Raycast(IntPtr navMeshQuery, NavLocation start, Vector3 targetPosition,
        int areaMask, void* costs, out NavMeshHit hit, void* path, out int pathCount, int maxPath);

    public unsafe NavQueryStatus Raycast(out NavMeshHit hit, NavLocation start, Vector3 targetPosition,
        int areaMask = NavMesh.AllAreas, NativeArray<float> costs = new())
    {
        const int kAreaCount = 32;
        CheckValidPtrAndThrow();

        if (costs.Length != 0)
        {
            if (costs.Length != kAreaCount)
                throw new ArgumentException(
                    string.Format("The number of costs ({0}) must be exactly {1}, one for each possible area type.",
                        costs.Length, kAreaCount), nameof(costs));
        }
        void* costsPtr = costs.Length == kAreaCount ? costs.GetUnsafePtr() : null;
        var status = Raycast(m_ImmutableQuery, start, targetPosition, areaMask, costsPtr, out hit, null, out _, 0);
        status &= ~NavQueryStatus.BufferTooSmall;
        return status;
    }

    public unsafe NavQueryStatus Raycast(out NavMeshHit hit, NativeSlice<NavNode> path, out int pathCount,
        NavLocation start, Vector3 targetPosition,
        int areaMask = NavMesh.AllAreas, NativeArray<float> costs = new())
    {
        const int kAreaCount = 32;
        CheckValidPtrAndThrow();

        if (costs.Length != 0)
        {
            if (costs.Length != kAreaCount)
                throw new ArgumentException(
                    string.Format("The number of costs ({0}) must be exactly {1}, one for each possible area type.",
                        costs.Length, kAreaCount), nameof(costs));
        }
        void* costsPtr = costs.Length == kAreaCount ? costs.GetUnsafePtr() : null;
        void* pathPtr = path.Length > 0 ? path.GetUnsafePtr() : null;
        var maxPath = pathPtr != null ? path.Length : 0;
        var status = Raycast(m_ImmutableQuery, start, targetPosition, areaMask, costsPtr, out hit, pathPtr,
            out pathCount, maxPath);
        return status;
    }

    [NativeMethod(IsThreadSafe = true)]
    static extern unsafe NavQueryStatus GetEdgesAndNeighbors(IntPtr navMeshQuery, NavNode node,
        int maxVertices, int maxNei,
        void* vertices, void* neighbors, void* edgeIndices,
        out int vertCount, out int neighborsCount);

    public unsafe NavQueryStatus GetEdgesAndNeighbors(NavNode node,
        NativeSlice<Vector3> edgeVertices, NativeSlice<NavNode> neighbors, NativeSlice<byte> edgeIndices,
        out int verticesCount, out int neighborsCount)
    {
        CheckValidPtrAndThrow();

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
        var status = GetEdgesAndNeighbors(m_ImmutableQuery, node, maxVertices, maxNeighbors,
            vertPtr, neiPtr, edgesPtr,
            out verticesCount, out neighborsCount);
        return status;
    }

    [NativeMethod(IsThreadSafe = true)]
    [StaticAccessor("GetNavMeshManager()")]
    [NativeName("GetPolyRefsForGeneratedLinks")]
    static extern unsafe int GetGeneratedLinkNodes(int navMeshDataInstance, void* nodes, int nodesLength, int start, int size);

    public unsafe int GetGeneratedLinkNodes(NavMeshDataInstance navMeshInstance, NativeSlice<NavNode> linkNodes,
        int start = 0, int length = int.MaxValue)
    {
        CheckValidPtrAndThrow();
        void* nodesPtr = linkNodes.Length > 0 ? linkNodes.GetUnsafePtr() : null;
        var maxNodes = linkNodes.Length;
        return GetGeneratedLinkNodes(navMeshInstance.id, nodesPtr, maxNodes, start, length);
    }

    [NativeMethod(IsThreadSafe = true)]
    [StaticAccessor("GetNavMeshManager()")]
    [NativeName("GetGeneratedLinksCount")]
    static extern int GetGeneratedLinksCountInternal(int navMeshDataInstanceId);

    public int GetGeneratedLinksCount(NavMeshDataInstance navMeshInstance)
    {
        CheckValidPtrAndThrow();
        return GetGeneratedLinksCountInternal(navMeshInstance.id);
    }
}
