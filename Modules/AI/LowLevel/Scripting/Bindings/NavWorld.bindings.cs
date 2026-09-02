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

///<summary>Provides access to the navigation mesh world and performs navigation query operations.</summary>
///<remarks>The navigation mesh world assembles together a collection of NavMesh surfaces and links that are used as a whole for performing navigation operations. NavWorld operations can be executed inside jobs (<see cref="Unity.Jobs.IJob" />, <see cref="Unity.Jobs.IJobFor" />), as opposed to the operations in the <see cref="NavMesh" />-related structures.
///
///Operations are initialized against one world, can use only the NavMeshes inside that world and aren't aware of the existence of any other NavWorld.
///
///Copying this object produces only a new reference to the same NavMesh data. It doesn't duplicate the data in memory. Use <see cref="NavWorld.Equals(NavWorld)" /> or the <c>==</c> operator to determine whether two NavWorld handles refer to the same navigation data.
///
///Every call to <see cref="NavWorld.GetDefaultWorld" /> produces a handle that you must release with <see cref="NavWorld.Dispose" /> when you stop using it. Obtain the handle once and keep it for as long as you run queries, rather than requesting a new one on every frame.
///
///To obtain a path between two locations on the NavMesh, first create a <see cref="NavQueryBuffer" /> with a <c>maxNodesToVisit</c> value in the range from 1 to 65,535, then call the following NavWorld methods in this order: <c>BeginFindPath</c>, <c>ContinueFindPath</c>, <c>EndFindPath</c>, <c>GetResultFromFindPath</c>. You can call <c>ContinueFindPath</c> repeatedly. These methods store their intermediate state within the <see cref="NavQueryBuffer" />. Other methods can be called in any order since they don't change state data.
///
///All methods throw exceptions if any of their parameters aren't valid when executed in the Editor.
///
///<see cref="NavWorld.IsValid()" /> checks a NavWorld, a <see cref="NavNode" />, or a <see cref="NavLocation" />, so you can call it beforehand when their validity isn't already guaranteed. It doesn't cover every argument that a method validates, such as the size of an array or the agent type behind a location. These checks are optional: skip them when the surrounding code already produces valid arguments and you don't want to pay for the verification on every call.
///
///**Important**: You can use only a single NavMesh world. Obtain a reference to it through the <see cref="NavWorld.GetDefaultWorld" /> method.</remarks>
[NativeContainer]
[NativeContainerIsReadOnly]
[StructLayout(LayoutKind.Sequential)]
[StaticAccessor("NavMeshLowLevel::NavWorldBindings", StaticAccessorType.DoubleColon)]
public struct NavWorld : IDisposable, IEquatable<NavWorld>
{
    [NativeDisableUnsafePtrRestriction]
    internal IntPtr m_NavMeshPtr;
    [NativeDisableUnsafePtrRestriction]
    internal IntPtr m_ImmutableQuery;
    internal uint m_UniqueId;
    internal readonly IntPtr navMeshPtr => m_NavMeshPtr;
    internal readonly uint uniqueId => m_UniqueId;

    internal AtomicSafetyHandle m_Safety;

    internal static readonly int k_StaticSafetyId = AtomicSafetyHandle.NewStaticSafetyId<NavWorld>();

    const string k_NoBufferAllocatedErrorMessage =
        "This query has no valid buffer allocated for pathfinding operations. " +
        "Create and use a new NavQueryBuffer.";

    [NativeMethod(IsThreadSafe = true)]
    static extern bool IsWorldForQueryInternal(IntPtr navMesh, IntPtr query, uint queryUniqueId);

    [NativeMethod(IsThreadSafe = true)]
    static extern bool IsValidWorldInternal(IntPtr navMesh, IntPtr immutableQuery, uint uniqueId);

    ///<summary>Checks whether the NavWorld has been properly initialized.</summary>
    ///<remarks>The only way to obtain the single possible valid NavMesh world is through a call to <see cref="NavWorld.GetDefaultWorld" />. A NavWorld becomes invalid after a call to <see cref="NavMesh.RemoveAllNavMeshData" /> destroys the underlying navigation data. Use this method before you invoke any other NavWorld operation, to avoid exceptions that Unity throws in the Editor.</remarks>
    ///<returns><c>true</c> if the NavWorld has been properly initialized, or <c>false</c> if the underlying navigation system has been destroyed.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/IsValidWorldExample.cs}]]></code></example>
    public readonly bool IsValid()
    {
        return m_NavMeshPtr != IntPtr.Zero
            && m_ImmutableQuery != IntPtr.Zero
            && IsValidWorldInternal(m_NavMeshPtr, m_ImmutableQuery, m_UniqueId);
    }

    static extern NavMeshPointers GetDefaultWorldInternal();

    ///<summary>Obtains a reference to the navigation mesh world where all navigation operations occur.</summary>
    ///<remarks>The returned world comprises all the NavMesh surfaces and connections that are also used through the <see cref="NavMesh" />-related structures.
    ///
    ///The world becomes invalid after a call to <see cref="NavMesh.RemoveAllNavMeshData" />. A new call to <c>GetDefaultWorld</c> returns a world that is different than the one that has been destroyed.
    ///
    ///Each call returns a separate handle that holds resources of its own. Pair every call with a <see cref="NavWorld.Dispose" /> call, for example through a <c>using</c> declaration, otherwise the handles accumulate for as long as the navigation data exists.</remarks>
    ///<returns>A reference to the single <see cref="NavWorld" /> that can currently exist and be used in Unity.</returns>
    ///<seealso cref="NavWorld.IsValid()" />
    ///<seealso cref="NavWorld.Dispose" />
    ///<seealso cref="NavMesh.AddNavMeshData(NavMeshData)" />
    ///<seealso cref="NavMesh.AddLink(NavMeshLinkData)" />
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/GetDefaultWorldExample.cs}]]></code></example>
    public static NavWorld GetDefaultWorld()
    {
        var pointers = GetDefaultWorldInternal();

        var world = new NavWorld
        {
            m_NavMeshPtr = pointers.m_NavMesh,
            m_ImmutableQuery = pointers.m_ImmutableQuery,
            m_UniqueId = pointers.m_UniqueId
        };

        if (!world.IsValid())
            throw new InvalidOperationException(
                "The default NavMesh world could not be created, " +
                "most likely because there is not enough memory left.");

        AtomicSafetyHandle.CreateHandle(out world.m_Safety, Allocator.Persistent);
        AtomicSafetyHandle.SetStaticSafetyId(ref world.m_Safety, k_StaticSafetyId);
        AddWorldSafety(world.m_NavMeshPtr, world.m_Safety);
        return world;
    }

    // Explicit cleanup of the safety handle which is otherwise
    // removed only when the underlying NavMesh is destroyed.
    ///<summary>Releases this NavWorld handle and prevents any further operations on it.</summary>
    ///<remarks>Call this method when you no longer need to use the NavWorld. After calling it, the NavWorld becomes invalid and all of its methods return invalid results or throw exceptions when executed in the Editor. Other handles to the same navigation data aren't affected. To destroy the underlying navigation system use <see cref="NavMesh.RemoveAllNavMeshData" />.</remarks>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/NavWorldLifecycleExample.cs}]]></code></example>
    public void Dispose()
    {
        if (AtomicSafetyHandle.IsValidNonDefaultHandle(m_Safety))
        {
            var canRemoveSafety = AtomicSafetyHandle.GetAllowReadOrWriteAccess(m_Safety);

            AtomicSafetyHandle.DisposeHandle(ref m_Safety);

            if (canRemoveSafety && m_NavMeshPtr != IntPtr.Zero)
                RemoveWorldSafety(m_NavMeshPtr, m_Safety);
        }
        m_NavMeshPtr = IntPtr.Zero;
        m_ImmutableQuery = IntPtr.Zero;
    }

    ///<summary>Checks whether two NavWorld values refer to the same underlying NavMesh world.</summary>
    ///<remarks>This operator is a syntactic shortcut equivalent to calling <see cref="NavWorld.Equals(NavWorld)" />. Use it inside expressions where the <c>==</c> syntax is more concise than an explicit method call. Two <see cref="NavWorld" /> values are equal only when they point to the same internal navigation system.</remarks>
    ///<param name="left">The <see cref="NavWorld" /> on the left side of the operator.</param>
    ///<param name="right">The <see cref="NavWorld" /> on the right side of the operator.</param>
    ///<returns><c>true</c> if the two <see cref="NavWorld" /> values reference the same navigation system, <c>false</c> otherwise.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/NavWorldEqualityExample.cs}]]></code></example>
    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public static bool operator ==(NavWorld left, NavWorld right)
    {
        return left.Equals(right);
    }

    ///<summary>Checks whether two NavWorld values refer to different underlying NavMesh worlds.</summary>
    ///<remarks>This operator is a syntactic shortcut equivalent to negating the result of <see cref="NavWorld.Equals(NavWorld)" />. Use it inside expressions where the <c>!=</c> syntax is more concise than an explicit method call. Two <see cref="NavWorld" /> values are different when they point to separate internal navigation systems.</remarks>
    ///<param name="left">The <see cref="NavWorld" /> on the left side of the operator.</param>
    ///<param name="right">The <see cref="NavWorld" /> on the right side of the operator.</param>
    ///<returns><c>true</c> if the two <see cref="NavWorld" /> values reference different navigation systems, <c>false</c> otherwise.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/NavWorldEqualityExample.cs}]]></code></example>
    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public static bool operator !=(NavWorld left, NavWorld right)
    {
        return !left.Equals(right);
    }

    ///<summary>Determines whether two NavWorld handles refer to the same underlying NavMesh world.</summary>
    ///<remarks>The comparison checks whether both NavWorld values point to the same internal navigation system. A NavWorld obtained from a previous <see cref="NavWorld.GetDefaultWorld" /> call isn't equal to one obtained after a <see cref="NavMesh.RemoveAllNavMeshData" /> call, because the underlying world has been destroyed and re-created.</remarks>
    ///<param name="other">The NavWorld handle to compare this one with.</param>
    ///<returns><c>true</c> if the two NavWorld values refer to the same underlying NavMesh world, and <c>false</c> otherwise.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/NavWorldEqualityExample.cs}]]></code></example>
    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public readonly bool Equals(NavWorld other)
    {
        return m_NavMeshPtr == other.m_NavMeshPtr
            && m_ImmutableQuery == other.m_ImmutableQuery
            && m_UniqueId == other.m_UniqueId;
    }

    ///<summary>Determines whether two NavWorld handles refer to the same underlying NavMesh world.</summary>
    ///<remarks>The comparison checks whether both NavWorld values point to the same internal navigation system. A NavWorld obtained from a previous <see cref="NavWorld.GetDefaultWorld" /> call isn't equal to one obtained after a <see cref="NavMesh.RemoveAllNavMeshData" /> call, because the underlying world has been destroyed and re-created.</remarks>
    ///<param name="obj">An object to interpret as a NavWorld and to compare this one with.</param>
    ///<returns><c>true</c> if the two NavWorld values refer to the same underlying NavMesh world, and <c>false</c> otherwise.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/NavWorldEqualityExample.cs}]]></code></example>
    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public readonly override bool Equals(object obj)
    {
        return obj is NavWorld other && Equals(other);
    }

    ///<summary>Calculates the hash code for use in collections.</summary>
    ///<remarks>The returned value is derived from the internal identifier of the <see cref="NavWorld" /> and is suitable for storing worlds in hash-based collections such as Dictionary or HashSet. Two NavWorld values that compare equal through <see cref="NavWorld.Equals(NavWorld)" /> also produce the same hash code.
    ///
    ///The reverse doesn't hold. Two NavWorld handles that refer to different navigation data can produce the same hash code, so a matching hash code on its own doesn't establish that two handles are the same. Use <see cref="NavWorld.Equals(NavWorld)" /> or the <c>==</c> operator whenever you need to determine that.</remarks>
    ///<returns>The hash code derived from the internal identifier of this NavWorld, suitable for use in hash-based collections.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/NavWorldEqualityExample.cs}]]></code></example>
    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public readonly override int GetHashCode()
    {
        return HashCode.Combine(m_NavMeshPtr, m_ImmutableQuery, m_UniqueId);
    }

    readonly void CheckValidPtrAndThrow()
    {
        if (!IsValid())
            throw new InvalidOperationException(
                "The NavWorld is invalid. Call NavWorld.GetDefaultWorld() to obtain a valid world.");

        AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
    }

    readonly void CheckBufferMatchAndThrow(NavQueryBuffer queryBuffer)
    {
        if (!IsWorldForQueryInternal(m_NavMeshPtr, queryBuffer.navMeshQueryPtr, queryBuffer.worldUniqueId))
            throw new InvalidOperationException(
                "The NavWorld methods cannot use a NavQueryBuffer created for a different NavWorld. " +
                "Call the method with a NavQueryBuffer created for this NavWorld " +
                $"(index {uniqueId} instead of {queryBuffer.worldUniqueId}).");
    }

    static extern void AddWorldSafety(IntPtr navMesh, AtomicSafetyHandle handle);

    static extern void RemoveWorldSafety(IntPtr navMesh, AtomicSafetyHandle handle);

    static extern void AddDependencyInternal(IntPtr navMesh, JobHandle handle);

    ///<summary>Tells the NavMesh world to halt any changes until the specified job is completed.</summary>
    ///<remarks>When jobs process <see cref="NavWorld" /> operations, it is essential that the NavMesh data doesn't change. Every time you schedule a job that contains NavWorld operations, call this method to pass the job's <see cref="JobHandle" /> to the NavWorld. Otherwise, in the Editor, Unity logs an error and forces the job to complete before it allows the NavMesh data to change.</remarks>
    ///<param name="job">The job to complete before the NavMesh world changes in any way.</param>
    ///<seealso cref="IJob" />
    ///<seealso cref="IJobFor" />
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/NavWorldDependencyExample.cs}]]></code></example>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/FindPathJobExample.cs}]]></code></example>
    public readonly void AddDependency(JobHandle job)
    {
        CheckValidPtrAndThrow();

        if (JobsUtility.IsExecutingJob)
            throw new InvalidOperationException("NavWorld.AddDependency cannot be called from a job.");
        AddDependencyInternal(m_NavMeshPtr, job);
    }

    [NativeMethod(IsThreadSafe = true)]
    static extern NavLocation MapLocation(IntPtr navMeshQuery, Vector3 position, Vector3 extents,
        int agentTypeID, int areaMask = NavMesh.AllAreas);

    ///<summary>Finds the closest point and NavNode on the NavMesh for a given world position.</summary>
    ///<remarks>The search applies only to the specified type of NavMesh surface, for one or more desired area types and is limited to within the specified search area. It doesn't search for positions on <see href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/api/Unity.AI.Navigation.NavMeshLink.html">NavMesh Links</see>.
    ///
    ///The Humanoid agent type exists for all NavMeshes and has an ID of 0. You can define other agent types manually in the Editor, and you must bake a separate NavMesh surface for each agent type.
    ///
    ///Unity prefers nearby NavMesh surfaces directly above or below the specified position. When none exist up or down within the specified search extents, it samples the surfaces closest sideways.
    ///
    ///**Important**: The returned position isn't aligned vertically to the <see href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/HeightMesh.html">HeightMesh</see>, if one exists.
    ///
    ///For more information about area types and the way they influence navigation, refer to <see href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/AreasAndCosts.html">Areas and Costs</see>.</remarks>
    ///<param name="position">The world position for which to find the closest point on the NavMesh.</param>
    ///<param name="extents">Maximum distance from the specified <c>position</c>, expanding along all three axes, within which Unity searches for NavMesh surfaces.</param>
    ///<param name="agentTypeId">The identifier for the agent type whose NavMesh surfaces this operation uses.</param>
    ///<param name="areaMask">Bitmask with values of 1 at the indices of areas that Unity samples, and values of 0 for areas that it ignores. The default value is <see cref="NavMesh.AllAreas" />.</param>
    ///<returns>An object with position and valid <see cref="NavNode" /> if Unity finds a point on the NavMesh.
    ///An invalid object if Unity doesn't find a NavMesh surface with the desired features within the search area.</returns>
    ///<seealso cref="NavMesh.SamplePosition(Vector3, out NavMeshHit, float, int)" />
    ///<seealso cref="NavWorld.IsValid(NavLocation)" />
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/MapLocationExample.cs}]]></code></example>
    public readonly NavLocation MapLocation(Vector3 position, Vector3 extents, int agentTypeId,
        int areaMask = NavMesh.AllAreas)
    {
        CheckValidPtrAndThrow();
        return MapLocation(m_ImmutableQuery, position, extents, agentTypeId, areaMask);
    }

    ///<summary>Initiates a pathfinding operation between two locations on the NavMesh.</summary>
    ///<remarks>The path always begins at the specified location. If the desired end location isn't directly accessible, the search algorithm tries to find a valid location nearby.
    ///
    ///Calling this method overrides the progress made by the specified <c>queryBuffer</c> in its previous pathfinding operation. Each <see cref="NavQueryBuffer" /> stores its own progress, so calling this method with a different buffer doesn't affect a search that's still in progress using another buffer.
    ///
    ///Call <see cref="NavWorld.ContinueFindPath(NavQueryBuffer, int, out int)" /> after this method to process the path search.
    ///
    ///In the Editor, most invalid arguments throw an exception instead of producing a <c>Failure</c> status. This applies to a NavWorld or a <see cref="NavQueryBuffer" /> that isn't valid, a <c>start</c> or <c>end</c> location whose node is no longer part of the NavMesh, start and end locations that belong to NavMeshes built for different agent types, and a <c>costs</c> array that doesn't have exactly 32 elements or that contains a value below <c>1.0f</c>.
    ///
    ///For more information about area types and the traversal costs the search applies to them, refer to <see href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/AreasAndCosts.html">Areas and Costs</see>.</remarks>
    ///<param name="queryBuffer">The <see cref="NavQueryBuffer" /> used to store intermediate node data for this search operation.</param>
    ///<param name="start">The start location on the NavMesh for the path.</param>
    ///<param name="end">The location on the NavMesh where the path ends.</param>
    ///<param name="areaMask">Bitmask with values of 1 at the indices of areas that can be traversed, and values of 0 for areas that aren't traversable. The default value is <see cref="NavMesh.AllAreas" />.</param>
    ///<param name="costs">Array of custom cost values for all of the 32 possible area types. Each value must be at least <c>1.0f</c>. The default value is the set of area costs configured in the project settings.</param>
    ///<returns>A bitfield with one of the following three main flags set:
    ///
    ///<c>InProgress</c> if the operation is successful and the query is ready to search for a path.
    ///
    ///<c>Failure</c>, combined with <see cref="NavQueryStatus.InvalidParameter" />, if the <c>queryBuffer</c> isn't created for this NavWorld or is no longer valid. Outside the Editor this status also covers the other invalid arguments that the Editor reports by throwing an exception.
    ///
    ///<c>Success</c> if the pathfinding operation starts and ends in the same <see cref="NavNode" />.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/FindPathExample.cs}]]></code></example>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/FindPathWithAreaCostsExample.cs}]]></code></example>
    ///<seealso cref="NavQueryStatus" />
    ///<seealso cref="NavMesh.GetAreaCost" />
    public readonly unsafe NavQueryStatus BeginFindPath(NavQueryBuffer queryBuffer,
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

        var agentTypeStart = GetAgentTypeIdForNode(m_NavMeshPtr, start.node);
        var agentTypeEnd = GetAgentTypeIdForNode(m_NavMeshPtr, end.node);
        if (agentTypeStart != agentTypeEnd)
            throw new ArgumentException(string.Format(
                "The start and end locations belong to different NavMesh surfaces, with agent type IDs {0} and {1}.",
                agentTypeStart, agentTypeEnd));
        if (queryBuffer.isNull || queryBuffer.worldUniqueId != m_UniqueId)
            return NavQueryStatus.Failure | NavQueryStatus.InvalidParameter;

        void* costsPtr = costs.Length > 0 ? costs.GetUnsafePtr() : null;
        return BeginFindPath(queryBuffer.navMeshQueryPtr, start, end, areaMask, costsPtr);
    }

    ///<summary>Continues a path search that is in progress.</summary>
    ///<remarks>The operation needs to have been initialized previously with <see cref="NavWorld.BeginFindPath(NavQueryBuffer, NavLocation, NavLocation, int, NativeArray{float})" /> and it runs until the entire route is found or the specified number of iterations have been executed.
    ///
    ///As long as the previous call returned a state of <c>InProgress</c> this method can be called repeatedly, across different frames, until the operation is successful. Use <see cref="NavWorld.EndFindPath(NavQueryBuffer, out int)" /> afterwards to prepare the path data for retrieval, along with the number of contained nodes.</remarks>
    ///<param name="queryBuffer">The container used to store intermediate node data for this search operation.</param>
    ///<param name="nodesToVisit">Maximum number of nodes to be traversed by the search algorithm during this call.</param>
    ///<param name="nodesVisited">Outputs the actual number of nodes that have been traversed during this call.</param>
    ///<returns>A bitfield with one of the following three main flags set:
    ///
    ///<c>InProgress</c> if the search needs to continue further by calling <c>ContinueFindPath</c> again.
    ///
    ///<c>Success</c> if the search is completed and a path has been found or not.
    ///
    ///<c>Failure</c> if the NavMesh has changed significantly since the search started, so the search can't complete.
    ///
    ///Additionally, the returned status can contain the <see cref="NavQueryStatus.MaxNodesToVisitExceeded" /> flag when the <c>maxNodesToVisit</c> parameter of the <see cref="NavQueryBuffer" /> wasn't large enough to accommodate the search space.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/FindPathExample.cs}]]></code></example>
    ///<seealso cref="NavQueryStatus.StatusDetailMask" />
    public readonly NavQueryStatus ContinueFindPath(NavQueryBuffer queryBuffer, int nodesToVisit, out int nodesVisited)
    {
        CheckValidPtrAndThrow();
        CheckBufferMatchAndThrow(queryBuffer);
        queryBuffer.CheckWriteSafetyAndThrow();

        if (!queryBuffer.HasNodePool())
            throw new InvalidOperationException(k_NoBufferAllocatedErrorMessage);
        if (queryBuffer.isNull || queryBuffer.worldUniqueId != m_UniqueId)
        {
            nodesVisited = 0;
            return NavQueryStatus.Failure | NavQueryStatus.InvalidParameter;
        }

        return ContinueFindPath(queryBuffer.navMeshQueryPtr, nodesToVisit, out nodesVisited);
    }

    ///<summary>Obtains the number of nodes in the path computed by a successful NavWorld.ContinueFindPath operation.</summary>
    ///<remarks>This method prepares the path data so that you can then call <see cref="NavWorld.GetResultFromFindPath(NavQueryBuffer, NativeSlice{NavNode})" /> to retrieve the array of <see cref="NavNode" /> values that make up the path.
    ///
    ///**Important**: Call this method only once, at the end of the pathfinding operation. Calling it more than once invalidates the stored path.</remarks>
    ///<param name="queryBuffer">The container that stores intermediate node data for this search operation.</param>
    ///<param name="pathSize">The number of NavMesh nodes in the found path. This method sets the value before it returns.</param>
    ///<returns>A bitfield with one of the following two main flags set:
    ///
    ///<c>Success</c> when the method retrieves the number of nodes in the path correctly.
    ///
    ///<c>Failure</c> when the method can't evaluate the path size because the preceding <c>ContinueFindPath</c> call wasn't successful.
    ///
    ///Additionally, the returned status can contain the <see cref="NavQueryStatus.PartialResult" /> flag when the search finds a path that falls short of the desired end location. The value also carries over any detail flags from the preceding <c>ContinueFindPath</c> operation.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/FindPathExample.cs}]]></code></example>
    ///<seealso cref="NavQueryStatus.StatusDetailMask" />
    public readonly NavQueryStatus EndFindPath(NavQueryBuffer queryBuffer, out int pathSize)
    {
        CheckValidPtrAndThrow();
        CheckBufferMatchAndThrow(queryBuffer);
        queryBuffer.CheckWriteSafetyAndThrow();

        if (!queryBuffer.HasNodePool())
            throw new InvalidOperationException(k_NoBufferAllocatedErrorMessage);
        if (queryBuffer.isNull || queryBuffer.worldUniqueId != m_UniqueId)
        {
            pathSize = 0;
            return NavQueryStatus.Failure | NavQueryStatus.InvalidParameter;
        }

        return EndFindPath(queryBuffer.navMeshQueryPtr, out pathSize);
    }

    ///<summary>Copies the NavMesh nodes that form the path found by the NavWorld operation into the provided array.</summary>
    ///<remarks>Call this method at the end of a successful <see cref="NavWorld.BeginFindPath(NavQueryBuffer, NavLocation, NavLocation, int, NativeArray{float})" /> - <see cref="NavWorld.ContinueFindPath(NavQueryBuffer, int, out int)" /> - <see cref="NavWorld.EndFindPath(NavQueryBuffer, out int)" /> sequence to obtain the resulting path.
    ///
    ///You can call it multiple times as long as <see cref="NavWorld.BeginFindPath(NavQueryBuffer, NavLocation, NavLocation, int, NativeArray{float})" /> hasn't been called for that same query.
    ///
    ///If the path stored in the query is longer than the provided array, this method copies only as many nodes as the array holds, starting from the beginning of the path.
    ///
    ///**Important**: If a NavMesh modification removed the start node of the path since the initial <c>BeginFindPath</c> call of the pathfinding operation, the returned path is empty.</remarks>
    ///<param name="queryBuffer">The container that holds the path nodes obtained by a completed pathfinding query.</param>
    ///<param name="path">The array to fill with the sequence of NavMesh nodes that comprises the found path.</param>
    ///<returns>The number of path nodes copied into the provided array.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/FindPathExample.cs}]]></code></example>
    public readonly unsafe int GetResultFromFindPath(NavQueryBuffer queryBuffer, NativeSlice<NavNode> path)
    {
        CheckValidPtrAndThrow();
        CheckBufferMatchAndThrow(queryBuffer);
        queryBuffer.CheckWriteSafetyAndThrow();

        if (!queryBuffer.HasNodePool())
            throw new InvalidOperationException(k_NoBufferAllocatedErrorMessage);
        if (path.Length == 0 || queryBuffer.isNull || queryBuffer.worldUniqueId != m_UniqueId)
            return 0;

        return GetResultFromFindPath(queryBuffer.navMeshQueryPtr, path.GetUnsafePtr(), path.Length);
    }

    [NativeMethod(IsThreadSafe = true)]
    static extern unsafe NavQueryStatus BeginFindPath(IntPtr navMeshQuery,
        NavLocation start, NavLocation end, int areaMask, void* costs);

    [NativeMethod(IsThreadSafe = true)]
    static extern NavQueryStatus ContinueFindPath(IntPtr navMeshQuery, int nodesToVisit, out int nodesVisited);

    [NativeMethod(IsThreadSafe = true)]
    static extern NavQueryStatus EndFindPath(IntPtr navMeshQuery, out int pathSize);

    [NativeMethod(IsThreadSafe = true)]
    static extern unsafe int GetResultFromFindPath(IntPtr navMeshQuery, void* path, int maxPath);

    [NativeMethod(IsThreadSafe = true)]
    static extern bool IsValidNode(IntPtr navMeshPtr, NavNode node);

    ///<summary>Checks whether the node referenced by the specified NavNode is active in the NavMesh.</summary>
    ///<remarks>NavMesh nodes become invalid when Unity removes the NavMesh surface or the links they belong to, or when a modification of the NavMesh in their region replaces them. Calls to <see cref="NavMesh.RemoveNavMeshData" /> or <see cref="NavMesh.RemoveLink" /> remove NavMesh surfaces and links. Calls to <see cref="NavMeshBuilder.UpdateNavMeshData" />, or carving the NavMesh with a <see cref="UnityEngine.AI.NavMeshObstacle" />, modify the NavMesh.</remarks>
    ///<param name="node">The identifier of the NavMesh node to check.</param>
    ///<returns><c>true</c> if the node referenced by the specified NavNode is active in the NavMesh. Otherwise, <c>false</c>.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/IsValidNodeExample.cs}]]></code></example>
    public readonly bool IsValid(NavNode node)
    {
        CheckValidPtrAndThrow();
        return node.m_PolyRef != 0 && IsValidNode(m_NavMeshPtr, node);
    }

    ///<summary>Checks whether the node referenced by the NavNode contained in the NavLocation is active in the NavMesh.</summary>
    ///<remarks>A <see cref="NavLocation" /> returned by <see cref="NavWorld.MapLocation(Vector3, Vector3, int, int)" /> can become invalid if the NavMesh data changes around its position, for example through carving by a <see cref="UnityEngine.AI.NavMeshObstacle" /> or a <see cref="NavMeshBuilder.UpdateNavMeshData" /> call. Verify the location before passing it to other <see cref="NavWorld" /> operations.</remarks>
    ///<param name="location">The location on the NavMesh to check. Checking the location is the same as checking <c>location.node</c> directly.</param>
    ///<returns><c>true</c> if the node referenced by the NavNode contained in the NavLocation is active in the NavMesh. Otherwise, <c>false</c>.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/IsValidLocationExample.cs}]]></code></example>
    public readonly bool IsValid(NavLocation location)
    {
        return IsValid(location.node);
    }

    [NativeMethod(IsThreadSafe = true)]
    static extern int GetAgentTypeIdForNode(IntPtr navMeshPtr, NavNode node);

    ///<summary>Obtains the identifier of the agent type that the NavMesh surface or link containing the specified node is built for.</summary>
    ///<remarks>When you bake NavMesh surfaces or configure links, you must specify the **Agent Type** that can use them. A unique integer identifies each **Agent Type**. Operations such as <see cref="NavWorld.MapLocation(Vector3, Vector3, int, int)" />, <see cref="NavMesh.GetSettingsByID" />, <see cref="NavMesh.GetSettingsNameFromID" />, <see cref="NavMeshBuilder.BuildNavMeshData" />, and <see cref="NavMesh.CalculatePath(Vector3, Vector3, int, NavMeshPath)" /> all require an agent type, to distinguish between NavMeshes built for different agent configurations.
    ///
    ///For more information about configuring agent types, refer to <see href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/NavigationWindow.html#agents-tab">Agent Types</see>.</remarks>
    ///<param name="node">The identifier of a node from a NavMesh surface or link.</param>
    ///<returns>The integer identifier of the agent type that the specified node is built or configured for.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/AgentTypeForNodeExample.cs}]]></code></example>
    ///<seealso cref="NavMeshBuildSettings.agentTypeID" />
    ///<seealso cref="NavMeshLinkData.agentTypeID" />
    ///<seealso cref="NavMesh.CreateSettings" />
    public readonly int GetAgentTypeIdForNode(NavNode node)
    {
        CheckValidPtrAndThrow();
        return GetAgentTypeIdForNode(m_NavMeshPtr, node);
    }

    [NativeMethod(IsThreadSafe = true)]
    static extern int GetAreaIndexForNode(IntPtr navMeshPtr, NavNode node);

    ///<summary>Obtains the area type index assigned to the specified NavMesh node.</summary>
    ///<remarks>You define area types in the **Navigation** settings, and an index from 0 to 31 identifies each one. Use the returned value with <see cref="NavMesh.GetAreaCost" /> to look up the traversal cost for that area.
    ///
    ///For more information about defining area types, refer to <see href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/AreasAndCosts.html">Areas and Costs</see>.</remarks>
    ///<param name="node">The identifier of the NavMesh node to get the area type index for.</param>
    ///<returns>An integer from 0 to 31 that identifies the area type of the node.
    ///
    ///Returns 1, the index of the built-in **Not Walkable** area, when the node isn't valid in this NavWorld.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/AreaIndexForNodeExample.cs}]]></code></example>
    public readonly int GetAreaIndexForNode(NavNode node)
    {
        CheckValidPtrAndThrow();
        return GetAreaIndexForNode(m_NavMeshPtr, node);
    }

    [NativeMethod(IsThreadSafe = true)]
    static extern NavQueryStatus GetClosestPointOnPoly(IntPtr navMeshQuery, NavNode node, Vector3 position,
        out Vector3 nearest);

    ///<summary>Constructs a valid NavLocation from a position and a navigation node.</summary>
    ///<remarks>When the specified node is a <see cref="NavNodeType.Polygon">polygon</see>, the returned position is the point on the surface of the polygon that is closest to the specified position.
    ///
    ///When the specified node is a <see cref="NavNodeType.Link">link</see>, the returned position is the point on the link's end segments that is closest to the specified position. The position lies on the contact line between that segment of the link and a NavMesh surface.
    ///
    ///**Important**: The returned position isn't aligned vertically to the <see href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/HeightMesh.html">HeightMesh</see>, if one exists.
    ///
    ///To obtain reliable positions on the NavMesh, you can also use <see cref="NavWorld.MapLocation(Vector3, Vector3, int, int)" />, <see cref="NavWorld.MoveLocation(NavLocation, Vector3, int)" />, and <see cref="NavWorld.GetPortalPoints(NavNode, NavNode, out Vector3, out Vector3)" />.</remarks>
    ///<param name="position">The world position of the <see cref="NavLocation" /> to create.</param>
    ///<param name="node">The valid identifier of the NavMesh node.</param>
    ///<returns>An object that contains the specified NavMesh node, if valid, and a position located on that node.
    ///
    ///When the specified node is invalid, the returned object has an <see cref="NavNode.IsNull">empty</see> and invalid <c>node</c>, and a <c>position</c> equal to <see cref="UnityEngine.Vector3.zero" />.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/CreateLocationExample.cs}]]></code></example>
    public readonly NavLocation CreateLocation(Vector3 position, NavNode node)
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

    ///<summary>Translates a series of NavMesh locations to other positions without losing contact with the surface.</summary>
    ///<remarks>This method performs the same operation as <see cref="NavWorld.MoveLocation(NavLocation, Vector3, int)" />, but acts sequentially on a batch of locations, given their respective destinations and area filters. All three array parameters must have the same length. Otherwise, Unity throws an <c>ArgumentException</c> when this method executes in the Editor.
    ///
    ///This method writes the results in place, into the <c>locations</c> array.
    ///
    ///You can safely call this operation while a separate pathfinding query is in progress on the same query buffer instance.</remarks>
    ///<param name="locations">The array of positions to move across the NavMesh surface. At the end of the method call, this array contains the resulting locations.</param>
    ///<param name="destinations">The world positions to use as movement targets for each of the locations.</param>
    ///<param name="areaMasks">The filters for the areas that each of the movements can traverse.</param>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/MoveLocationsExample.cs}]]></code></example>
    ///<seealso cref="NavWorld.MoveLocation(NavLocation, Vector3, int)" />
    ///<seealso cref="NavLocation" />
    public readonly unsafe void MoveLocations(NativeSlice<NavLocation> locations, NativeSlice<Vector3> destinations,
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

    ///<summary>Translates a series of NavMesh locations to other positions without losing contact with the surface, given one common area filter for all of them.</summary>
    ///<remarks>This method performs the same operation as <see cref="NavWorld.MoveLocations(NativeSlice{NavLocation}, NativeSlice{Vector3}, NativeSlice{int})" />, but applies the same area filter to all the movements. The <c>locations</c> and <c>destinations</c> arrays must have the same length. Otherwise, Unity throws an <c>ArgumentException</c> when this method executes in the Editor.
    ///
    ///You can safely call this operation while a separate pathfinding query is in progress on the same query buffer instance.</remarks>
    ///<param name="locations">The array of positions to move across the NavMesh surface. At the end of the method call, this array contains the resulting locations.</param>
    ///<param name="destinations">The world positions to use as movement targets for each of the locations.</param>
    ///<param name="areaMask">The filter for the areas that all of the movements can traverse. The default value is <see cref="NavMesh.AllAreas" />.</param>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/MoveLocationsSharedMaskExample.cs}]]></code></example>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/MoveLocationsSliceExample.cs}]]></code></example>
    ///<seealso cref="NavWorld.MoveLocation(NavLocation, Vector3, int)" />
    ///<seealso cref="NavLocation" />
    public readonly unsafe void MoveLocations(NativeSlice<NavLocation> locations,
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

    ///<summary>Translates a NavMesh location to another position without losing contact with the surface.</summary>
    ///<remarks>This method returns the location on the NavMesh that is closest to the <c>destination</c> position and that also has a continuous connection on the NavMesh surface, through the allowed area types, all the way to the start position specified by the <c>location</c> parameter. If the <c>destination</c> position is outside the edges of the surface or of its allowed areas, this method returns a position at the edge. The search for a connected pathway is constrained to a circular area between the start and destination points. This constraint makes the operation fast, but it doesn't find a path that deviates widely to go around large obstacles.
    ///
    ///The movement doesn't cross <see href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/api/Unity.AI.Navigation.NavMeshLink.html">NavMesh Links</see>.
    ///
    ///You can safely call this operation while a separate pathfinding query is in progress on the same query buffer instance.
    ///
    ///For more information about the area types that <c>areaMask</c> filters, refer to <see href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/AreasAndCosts.html">Areas and Costs</see>.</remarks>
    ///<param name="location">The position to move across the NavMesh surface.</param>
    ///<param name="destination">The world position to move the location toward.</param>
    ///<param name="areaMask">A bitmask with a value of 1 at the indexes of the areas that can be traversed, and a value of 0 for areas that aren't traversable. The default value is <see cref="NavMesh.AllAreas" />.</param>
    ///<returns>A new location on the NavMesh placed as closely as possible to the specified <c>destination</c> position.
    ///
    ///Returns the start <c>location</c> when that start is inside an area that the <c>areaMask</c> doesn't allow.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/MoveLocationExample.cs}]]></code></example>
    ///<seealso cref="NavWorld.MoveLocations(NativeSlice{NavLocation}, NativeSlice{Vector3}, int)" />
    public readonly NavLocation MoveLocation(NavLocation location, Vector3 destination, int areaMask = NavMesh.AllAreas)
    {
        CheckValidPtrAndThrow();
        return MoveLocation(m_ImmutableQuery, location, destination, areaMask);
    }

    [NativeMethod(IsThreadSafe = true)]
    static extern bool GetPortalPoints(IntPtr navMeshQuery, NavNode node, NavNode neighbor,
        out Vector3 left, out Vector3 right);

    ///<summary>Obtains the end points of the line segment common to two adjacent NavMesh nodes.</summary>
    ///<remarks>For two polygons that are part of a NavMesh surface, this method returns the edge where both polygons meet. Any movement that crosses from one of the two nodes to the other passes through this edge. If the two polygons are in different NavMesh tiles, the connected edges can be of different length or have different start and end positions from each other. If this happens, the resulting separation edge is the overlapping part of the edges, which can be shorter than either of the individual edges.
    ///
    ///When one node is a link and the other is a polygon, the returned points are placed where the link intersects the polygon.
    ///
    ///The resulting positions are in world space. To transform them into a NavMesh's local space, use the inverse of the results from <see cref="NavWorld.GetInstanceTransform(NavNode, out Vector3, out Quaternion)" />.</remarks>
    ///<param name="node">The first NavMesh node of the adjacent pair.</param>
    ///<param name="neighbor">The second NavMesh node of the adjacent pair.</param>
    ///<param name="left">One of the world points of the resulting separation edge. This point is the left side of the edge when traversing from the first node to the second.</param>
    ///<param name="right">One of the world points of the resulting separation edge. This point is the right side of the edge when traversing from the first node to the second.</param>
    ///<returns><c>true</c> if a connection exists between the two NavMesh nodes.
    ///
    ///<c>false</c> if no connection exists between the two NavMesh nodes.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/PortalPointsExample.cs}]]></code></example>
    ///<seealso cref="NavWorld.GetEdgesAndNeighbors(NavNode, NativeSlice{Vector3}, NativeSlice{NavNode}, NativeSlice{byte}, out int, out int)" />
    public readonly bool GetPortalPoints(NavNode node, NavNode neighbor, out Vector3 left, out Vector3 right)
    {
        CheckValidPtrAndThrow();
        return GetPortalPoints(m_ImmutableQuery, node, neighbor, out left, out right);
    }

    [NativeMethod(IsThreadSafe = true)]
    static extern void GetInstanceTransform(IntPtr navMesh, NavNode node,
        out Vector3 position, out Quaternion rotation);

    ///<summary>Retrieves the position and rotation of the NavMesh surface that contains the specified NavMesh node.</summary>
    ///<remarks>Unity defines the transform of a <see cref="NavMeshData" /> surface from the <c>position</c> and <c>rotation</c> values that you declare when you bake the surface with <see cref="NavMeshBuilder.BuildNavMeshData" />, or as part of a <see href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/api/Unity.AI.Navigation.NavMeshSurface.html">NavMesh Surface</see>, or when you explicitly set the values for <see cref="NavMeshData.position" /> and <see cref="NavMeshData.rotation" />.
    ///
    ///You can also specify custom transforms for <see cref="NavMeshDataInstance">NavMeshDataInstances</see> when you create them, by passing explicit <c>position</c> and <c>rotation</c> values to the <see cref="NavMesh.AddNavMeshData(NavMeshData, Vector3, Quaternion)" /> method.
    ///
    ///**Important**: This method doesn't return the position and orientation of a single NavMesh polygon. It returns the position of the surface that owns the polygon.
    ///
    ///**Note**: This method returns zero and identity instead of the actual transform for NavMesh Links that you instantiate with a call to <see cref="NavMesh.AddLink(NavMeshLinkData, Vector3, Quaternion)" />.</remarks>
    ///<param name="node">The NavMesh node whose owner's transform to retrieve.</param>
    ///<param name="position">Outputs the world position of the NavMesh surface that owns the specified node.
    ///
    ///This is a zero vector when the NavMesh node is a <see href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/api/Unity.AI.Navigation.NavMeshLink.html">NavMesh Link</see>.</param>
    ///<param name="rotation">Outputs the world rotation of the NavMesh surface that owns the specified node.
    ///
    ///This is <see cref="Quaternion.identity" /> when the NavMesh node is a NavMesh Link.</param>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/InstanceTransformExample.cs}]]></code></example>
    ///<seealso cref="NavWorld.GetNodeType" />
    public readonly void GetInstanceTransform(NavNode node, out Vector3 position, out Quaternion rotation)
    {
        CheckValidPtrAndThrow();
        GetInstanceTransform(m_NavMeshPtr, node, out position, out rotation);
    }

    [NativeMethod(IsThreadSafe = true)]
    [StaticAccessor("NavMesh", StaticAccessorType.DoubleColon)]
    [NativeName("DecodePolyIdType")]
    static extern int GetNodeTypeInternal(NavNode node);

    ///<summary>Determines whether the NavMesh node is a polygon or a link.</summary>
    ///<remarks>Unity encodes the type in the <see cref="NavNode" /> identifier itself, so you can determine the type even after the specified node becomes invalid in the query's NavWorld. Use this method to decide whether to treat a node as a NavMesh surface polygon or as a link before you inspect its geometry.</remarks>
    ///<param name="node">The identifier of a node from a NavMesh surface or link.</param>
    ///<returns><c>Polygon</c> when the node is a polygon on a NavMesh surface.
    ///
    ///<c>Link</c> when the node is a <see href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/api/Unity.AI.Navigation.NavMeshLink.html">NavMesh Link</see> or a generated link baked into a NavMeshData object.
    ///
    ///<c>Undefined</c> when the node is empty and no query has produced it.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/GetNodeTypeExample.cs}]]></code></example>
    ///<seealso cref="NavNodeType" />
    public readonly NavNodeType GetNodeType(NavNode node)
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

    ///<summary>Retrieves the navigation node of a link that connects to one or more NavMeshes.</summary>
    ///<remarks>The resulting link identifier is <see cref="NavWorld.IsValid(NavNode)">valid</see> for any link instance that is <see cref="NavMesh.IsLinkValid">valid</see>. This is true even if the link doesn't connect to any NavMeshes, or if the link is deactivated with <see cref="NavMesh.SetLinkActive" />. A link instance preserves its unique identifier for the entire duration of the game, regardless of whether it connects to new NavMeshes or it disconnects from existing ones. The link identifier becomes invalid when the link is <see cref="NavMesh.RemoveLink">removed</see> from the navigation system, or when all data of the navigation system is <see cref="NavMesh.RemoveAllNavMeshData">removed</see>.
    ///
    ///**Note**: You can't change the properties of an existing link instance. However, you can remove the existing instance and then replace it with one that has the modified <see cref="NavMeshLinkData">properties</see>.
    ///
    ///**Note**: The navigation system doesn't provide a way to retrieve the <see cref="NavMeshLinkInstance" /> that corresponds to a <see cref="NavNode" />. You need to address that use case yourself. For example, you can store link instance and identifier pairs in a list or dictionary, which you can then search when needed.</remarks>
    ///<param name="linkInstance">The object that identifies a navigation link created between locations on one or more NavMeshes.</param>
    ///<returns>The identifier of the node that represents the link in the navigation system.</returns>
    ///<example nocheck="true"><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/ShowNavMeshLinkConnectivityInfo.cs}]]></code></example>
    ///<seealso cref="NavMesh.AddLink(NavMeshLinkData)" />
    ///<seealso cref="NavWorld.GetEdgesAndNeighbors(NavNode, NativeSlice{Vector3}, NativeSlice{NavNode}, NativeSlice{byte}, out int, out int)" />
    public readonly NavNode GetLinkNode(NavMeshLinkInstance linkInstance)
    {
        CheckValidPtrAndThrow();
        return GetLinkNode(linkInstance.id);
    }

    // Trace a ray between two points on the NavMesh.
    [NativeMethod(IsThreadSafe = true)]
    static extern unsafe NavQueryStatus Raycast(IntPtr navMeshQuery, NavLocation start, Vector3 targetPosition,
        int areaMask, void* costs, out NavMeshHit hit, void* path, out int pathCount, int maxPath);

    ///<summary>Traces a line between two points on the NavMesh.</summary>
    ///<remarks>The <c>areaMask</c> bitmask uses one bit per area type, with indexes from 0 to 31. Set a bit to 1 to allow the ray to pass through that area, or to 0 to block it. The <c>costs</c> array provides multipliers for each of the 32 area types and acts on the distance reported by the ray when it crosses each area. It must be either empty or exactly 32 elements long. Otherwise, Unity throws an <c>ArgumentException</c> when this method executes in the Editor. Unlike <see cref="NavWorld.BeginFindPath(NavQueryBuffer, NavLocation, NavLocation, int, NativeArray{float})" />, this method doesn't reject individual cost values below <c>1.0f</c>. For more information, refer to <see href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/AreasAndCosts.html">Areas and Costs</see> and <see cref="NavMesh.GetAreaCost" />.
    ///
    ///This method is similar to <see cref="NavMesh.Raycast(Vector3, Vector3, out NavMeshHit, int)" /> and shares the same underlying implementation.
    ///
    ///This method has the following differences:
    ///
    ///- You can use it in parallel [jobs](xref:JobSystem).
    ///- It returns status flags that indicate whether the operation succeeded or failed.
    ///- The area costs affect the reported <c>hit.distance</c>.
    ///- It doesn't adjust the resulting <c>hit.position</c> on the vertical axis according to the <see href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/HeightMesh.html">HeightMesh</see>, if one exists.
    ///- It has a variant that also returns the list of polygons through which the ray passes.
    ///
    ///The returned <c>hit.distance</c> represents the straight line between the start and termination point. It also takes into account the list of the provided area costs. It is the result of summing up all the distances covered by the ray over each separate area, multiplied by the cost of that respective area.
    ///
    ///First, this method verifies that the start location is valid in the NavWorld, and maps the target point onto the NavMesh. It then traces a ray from the start point toward the target. If the computation succeeds, the <c>hit</c> data contains information about the furthest point that the ray reaches. This happens whether or not an obstruction blocks the path from the source to the target.
    ///
    ///If the computation fails, the returned <c>hit</c> contains invalid data. Most notably, the <c>hit.distance</c> field has the value <c>positiveInfinity</c>.
    ///
    ///If the raycast terminates on an outer edge, <c>hit.mask</c> is 0; otherwise it contains the area mask of the blocking polygon.
    ///
    ///Use this method to check whether an agent can walk unobstructed between two points on the NavMesh. For example, if your character has an evasive dodge move that needs space, you can trace rays from the character's location in multiple directions to find a spot that the character can dodge to.
    ///
    ///This method differs from <c>Physics.Raycast</c>. It detects all kinds of navigation obstruction, such as holes in the ground. It can also climb up slopes, if the area is navigable.</remarks>
    ///<param name="hit">Receives the properties of the location where the ray terminates.</param>
    ///<param name="start">The start location of the ray on the NavMesh. <c>start.node</c> must be of the type <see cref="NavNodeType.Polygon" />.</param>
    ///<param name="targetPosition">The desired end of the ray, in world coordinates.</param>
    ///<param name="areaMask">A bitmask that correlates index positions with area types. A value of 1 allows the ray to pass through that area, and a value of 0 blocks it. The default value is <see cref="NavMesh.AllAreas" />.</param>
    ///<param name="costs">The cost multipliers for the 32 possible area types. These multipliers affect the reported ray distance. The default value is the set of area costs configured in the project settings.</param>
    ///<returns>A bitfield with one of the following two main flags set:
    ///
    ///<c>Success</c> if this method can trace the ray correctly with the provided arguments.
    ///
    ///<c>Failure</c> if the <c>start</c> location isn't valid in the query's NavWorld, or if it's inside an area that the <c>areaMask</c> argument doesn't permit, or if it's on a <see href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/api/Unity.AI.Navigation.NavMeshLink.html">NavMesh Link</see>.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/TargetReachable.cs}]]></code></example>
    public readonly unsafe NavQueryStatus Raycast(out NavMeshHit hit, NavLocation start, Vector3 targetPosition,
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
        status &= ~NavQueryStatus.MoreDataAvailable;
        return status;
    }

    ///<summary>Traces a line between two points on the NavMesh, and returns the list of polygons through which it passes.</summary>
    ///<remarks>Even if the <c>path</c> buffer is too small, it still holds as many polygons as it has room for, starting from the ray's origin location. In that case, the returned status also carries the <see cref="NavQueryStatus.MoreDataAvailable" /> flag.
    ///
    ///In every other respect, this overload behaves like <see cref="NavWorld.Raycast(out NavMeshHit, NavLocation, Vector3, int, NativeArray{float})" />. The documentation for that overload describes in detail how this method traces the ray, how the area costs affect <c>hit.distance</c>, and what the <c>hit</c> data contains.</remarks>
    ///<param name="hit">Receives the properties of the location where the ray terminates.</param>
    ///<param name="path">A buffer that receives the sequence of polygons through which the ray passes.</param>
    ///<param name="pathCount">The reported number of polygons through which the ray passes, all stored in the <c>path</c> buffer. It is never greater than <c>path.Length</c>.</param>
    ///<param name="start">The start location of the ray on the NavMesh. <c>start.node</c> must be of the type <see cref="NavNodeType.Polygon" />.</param>
    ///<param name="targetPosition">The desired end of the ray, in world coordinates.</param>
    ///<param name="areaMask">A bitmask that correlates index positions with area types. A value of 1 allows the ray to pass through that area, and a value of 0 blocks it. The default value is <see cref="NavMesh.AllAreas" />.</param>
    ///<param name="costs">The cost multipliers for the 32 possible area types. These multipliers affect the reported ray distance. The default value is the set of area costs configured in the project settings.</param>
    ///<returns>A bitfield with one of the following two main flags set:
    ///
    ///<c>Success</c> if this method can trace the ray correctly with the provided arguments.
    ///
    ///<c>Failure</c> if the <c>start</c> location isn't valid in the query's NavWorld, or if it's inside an area that the <c>areaMask</c> argument doesn't permit, or if it's on a <see href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/api/Unity.AI.Navigation.NavMeshLink.html">NavMesh Link</see>.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/StraightPathFromRay.cs}]]></code></example>
    ///<seealso cref="NavNode" />
    public readonly unsafe NavQueryStatus Raycast(out NavMeshHit hit, NativeSlice<NavNode> path, out int pathCount,
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
    static extern unsafe NavQueryStatus GetEdgesAndNeighbors(IntPtr navMeshPtr, NavNode node,
        int maxVertices, int maxNei,
        void* vertices, void* neighbors, void* edgeIndices,
        out int vertCount, out int neighborsCount);

    ///<summary>Retrieves the vertices of a specified node and the identifiers of all the navigation nodes to which it connects.</summary>
    ///<remarks><see cref="NavNodeType.Polygon">Polygonal</see> nodes of the NavMesh have a minimum of three and a maximum of six vertices, while <see cref="NavNodeType.Link">link</see> nodes always have four vertices regardless of their width. The index of an element in <c>edgeIndices</c> is also an index in the <c>neighbors</c> array, and the value of that <c>edgeIndices</c> element is an index in the <c>edgeVertices</c> array.
    ///
    ///A <see cref="NavNodeType.Polygon">polygon</see> of a NavMesh surface connects to all other neighboring polygons with which it shares an edge, as well as all the <see cref="NavNodeType.Link">NavMesh Links</see> that leave from anywhere on its surface. The polygon doesn't connect to other polygons with which it shares only a vertex.
    ///
    ///Each point returned in the <c>edgeVertices</c> array represents the start of a <c>node</c>'s edge and the subsequent element in the array is the end point of that edge. All vertices form a closed polygonal line. The last and first elements define the last edge.
    ///
    ///A <see cref="NavNodeType.Link">NavMesh Link</see> connects to all the NavMesh polygons that each end of the link intersects with, regardless of whether the link is unidirectional.
    ///
    ///For link nodes, the returned <c>edgeVertices</c> array contains two pairs of points, at indexes [0] and [1], and at indexes [2] and [3], that define the end points of the start and end edges of the link, in this order. These are the world positions that Unity establishes when it instantiates the link in the NavMesh world. For nodes of NavMesh Links with the width set to 0, the pairs contain the same value in both of their elements.
    ///
    ///A node from the <c>neighbors</c> array lies at the edge returned in <c>edgeIndices</c> at the same index.
    ///
    ///If both the specified <c>node</c> and its neighbor are NavMesh <see cref="NavNodeType.Polygon">polygons</see>, then the corresponding <c>edgeIndices</c> value represents the index of the polygon edge that leads from <c>node</c> to the neighbor. For example, <c>edgeVertices[edgeIndices[2]]</c> represents the start point of the edge that is common between <c>node</c> and the <c>neighbors[2]</c> node, and <c>edgeVertices[edgeIndices[2] + 1]</c> is the end point of that edge.
    ///
    ///A NavMesh polygon can have a maximum of 6 edges. This means the <c>edgeIndices</c> value corresponding to a polygon-polygon connection is from 0 to 5. An edge usually connects only the two polygons that share it, but edges that sit at a tile border can connect one polygon in the first tile to multiple polygons in the second tile. In this case, <c>edgeIndices</c> report the same value for all of those neighbors.
    ///
    ///If either the specified <c>node</c> or the <c>neighbor</c> is a <see cref="NavNodeType.Link">link</see>, then the corresponding <c>edgeIndices</c> value represents the side on the link where the connection is made: 0 for <see cref="NavMeshLinkData.startPosition">start</see> and 2 for <see cref="NavMeshLinkData.endPosition">end</see>. When the <c>node</c> is a polygon and the <c>neighbor</c> is a link, the value acts only as information about the side of the link where the two nodes connect. Don't use it as an index in the <c>edgeVertices</c> array.
    ///
    ///When the <c>neighbors</c> and <c>edgeIndices</c> buffers both have positive capacity, they must be the same size. Otherwise, Unity throws an <c>ArgumentException</c> when this method executes in the Editor.
    ///
    ///You can set any of the buffers to have zero capacity for the cases when you don't need the results.
    ///
    ///The returned <c>verticesCount</c> and <c>neighborsCount</c> values express the number of elements that comprise the result in the output buffers of sufficient size. Unity still fills buffers that aren't large enough with valid nodes, up to their full capacity.
    ///
    ///The five result parameters (<c>edgeVertices</c>, <c>neighbors</c>, <c>edgeIndices</c>, <c>verticesCount</c>, and <c>neighborsCount</c>) don't act as input and don't change the internal navigation data in any way. Unity modifies them only when the operation returns a <c>Success</c> status.</remarks>
    ///<param name="node">The identifier of the node from a NavMesh surface or a <see href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/api/Unity.AI.Navigation.NavMeshLink.html">NavMesh Link</see> to retrieve the vertices and neighbors for.</param>
    ///<param name="edgeVertices">The result buffer that receives the world positions describing the geometry of the input navigation <c>node</c>. It can have zero capacity.</param>
    ///<param name="neighbors">The result buffer that holds the identifiers of all the navigation nodes immediately reachable from the specified <c>node</c>. It can have zero capacity.</param>
    ///<param name="edgeIndices">The helper result buffer that maps each neighbor node to an edge of the specified <c>node</c>. It can have zero capacity.</param>
    ///<param name="verticesCount">The total number of vertices that describe the geometry of the input <c>node</c>. This is independent of the capacity of the <c>edgeVertices</c> result buffer.</param>
    ///<param name="neighborsCount">The total number of navigation nodes the input <c>node</c> connects to. This is independent of the capacity of the result buffers (<c>neighbors</c> and <c>edgeIndices</c>).</param>
    ///<returns>A bitfield with one of the following two main flags set:
    ///
    ///<c>Success</c> if Unity can evaluate the neighbors and vertices of the specified node, regardless of the result. The <c>verticesCount</c> and <c>neighborsCount</c> are always valid in this case.
    ///
    ///<c>Failure</c> if Unity can't use the <c>node</c> identifier to retrieve the neighbors or geometry information. Unity doesn't modify any of the five result parameters (<c>edgeVertices</c>, <c>neighbors</c>, <c>edgeIndices</c>, <c>verticesCount</c>, or <c>neighborsCount</c>) in this case.
    ///
    ///<c>InvalidParameter</c> is part of the returned flags if the specified navigation node isn't <see cref="NavWorld.IsValid(NavNode)">valid</see> in the query's NavWorld.
    ///
    ///<c>MoreDataAvailable</c> is part of the flags that Unity returns from this method when any of the result buffers you provide aren't large enough to hold all the neighbor nodes the input <c>node</c> connects to, or all of its edge vertices.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/NavMeshNodeEdgesDrawer.cs}]]></code></example>
    ///<seealso cref="NavWorld.GetNodeType" />
    ///<seealso cref="NavWorld.GetPortalPoints(NavNode, NavNode, out Vector3, out Vector3)" />
    public readonly unsafe NavQueryStatus GetEdgesAndNeighbors(NavNode node,
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
        var status = GetEdgesAndNeighbors(m_NavMeshPtr, node, maxVertices, maxNeighbors,
            vertPtr, neiPtr, edgesPtr,
            out verticesCount, out neighborsCount);
        return status;
    }

    [NativeMethod(IsThreadSafe = true)]
    [StaticAccessor("GetNavMeshManager()")]
    [NativeName("GetPolyRefsForGeneratedLinks")]
    static extern unsafe int GetGeneratedLinkNodes(int navMeshDataInstance, void* nodes, int nodesLength,
        int start, int size);

    ///<summary>Retrieves the NavNode identifiers for internal navigation links baked into the specified NavMeshDataInstance.</summary>
    ///<remarks>Certain NavMesh baking workflows produce internal navigation links stored directly in the <see cref="NavMeshData" /> that act similarly to <see href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/api/Unity.AI.Navigation.NavMeshLink.html">NavMesh Links</see> but aren't individually accessible as separate runtime objects. Use <see cref="NavWorld.GetGeneratedLinksCount" /> to determine how many such links a <see cref="NavMeshDataInstance" /> contains before sizing the <c>linkNodes</c> buffer.
    ///
    ///The <c>start</c> and <c>length</c> parameters let you retrieve a subset of the full list. Retrieving a subset is useful when you process large batches.</remarks>
    ///<param name="navMeshInstance">The NavMeshDataInstance that contains the baked links.</param>
    ///<param name="linkNodes">The buffer to fill with the NavNode identifiers of the baked links. It can have zero capacity when you need only the count returned by <see cref="NavWorld.GetGeneratedLinksCount" />.</param>
    ///<param name="start">The zero-based index of the first baked link to retrieve. The default value is 0.</param>
    ///<param name="length">The maximum number of baked links to retrieve. The default value is <c>int.MaxValue</c>, which retrieves all the remaining links from <c>start</c>.</param>
    ///<returns>The number of NavNode identifiers written into <c>linkNodes</c>.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/GeneratedLinkNodesExample.cs}]]></code></example>
    ///<seealso cref="NavWorld.GetGeneratedLinksCount" />
    ///<seealso cref="NavWorld.GetLinkNode(NavMeshLinkInstance)" />
    ///<seealso cref="NavWorld.IsValid(NavNode)" />
    public readonly unsafe int GetGeneratedLinkNodes(NavMeshDataInstance navMeshInstance,
        NativeSlice<NavNode> linkNodes, int start = 0, int length = int.MaxValue)
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

    ///<summary>Obtains the total number of internal navigation links baked into the specified NavMeshDataInstance.</summary>
    ///<remarks>Use the returned count to size the buffer that you pass to <see cref="NavWorld.GetGeneratedLinkNodes(NavMeshDataInstance, NativeSlice{NavNode}, int, int)" />, so that a single call retrieves all the baked links. The count covers only the links baked into the NavMeshData itself, not the links added separately to the navigation system at runtime.</remarks>
    ///<param name="navMeshInstance">The NavMeshDataInstance to count the baked links for.</param>
    ///<returns>The total number of navigation links baked into the specified NavMeshDataInstance.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/GeneratedLinksCountExample.cs}]]></code></example>
    ///<seealso cref="NavWorld.GetGeneratedLinkNodes(NavMeshDataInstance, NativeSlice{NavNode}, int, int)" />
    public readonly int GetGeneratedLinksCount(NavMeshDataInstance navMeshInstance)
    {
        CheckValidPtrAndThrow();
        return GetGeneratedLinksCountInternal(navMeshInstance.id);
    }
}
