// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.AI.Navigation.LowLevel;

///<summary>A buffer that stores intermediate node data during NavMesh pathfinding operations in a NavWorld.</summary>
///<remarks>A NavQueryBuffer is required to use the pathfinding methods of <see cref="NavWorld" />. To obtain a path between two locations on the NavMesh, create a NavQueryBuffer with a <c>maxNodesToVisit</c> value in the range from 1 to 65,535, then call the following <see cref="NavWorld" /> methods in this order: <c>BeginFindPath</c>, <c>ContinueFindPath</c> (can be repeated), <c>EndFindPath</c>, <c>GetResultFromFindPath</c>. These methods store their intermediate state in the NavQueryBuffer.
///
///NavWorld pathfinding operations that use a NavQueryBuffer can be executed inside jobs (<see cref="Unity.Jobs.IJob" />, <see cref="Unity.Jobs.IJobFor" />), as opposed to the operations in the <see cref="UnityEngine.AI.NavMesh" />-related structures.
///
///Copying this object produces only a new reference to the same underlying buffer. It doesn't duplicate the buffer in memory.
///
///All methods throw exceptions if any of their data isn't valid when executed in the Editor.</remarks>
///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/NavQueryBufferExample.cs}]]></code></example>
[NativeContainer]
[StructLayout(LayoutKind.Sequential)]
[NativeHeader("Modules/AI/LowLevel/NavWorld.bindings.h")]
[NativeHeader("Modules/AI/Public/NavMeshBindingTypes.h")]
[NativeHeader("Runtime/Math/Matrix4x4.h")]
[StaticAccessor("NavMeshLowLevel::NavQueryBufferBindings", StaticAccessorType.DoubleColon)]
public struct NavQueryBuffer : IDisposable, IEquatable<NavQueryBuffer>
{
    [NativeDisableUnsafePtrRestriction]
    internal IntPtr m_NavMeshQuery;
    internal uint m_NavMeshUniqueId;
    internal readonly IntPtr navMeshQueryPtr => m_NavMeshQuery;
    internal readonly uint worldUniqueId => m_NavMeshUniqueId;
    internal readonly bool isNull => m_NavMeshQuery == IntPtr.Zero;

    internal AtomicSafetyHandle m_Safety;
    internal uint m_SafetyUniqueId;

    internal static readonly int k_StaticSafetyId = AtomicSafetyHandle.NewStaticSafetyId<NavQueryBuffer>();

    const string k_NoInternalQueryAllocatedErrorMessage =
        "The NavQueryBuffer has been disposed. It is not allowed to access it. " +
        "Create and use a new NavQueryBuffer object.";
    const string k_OutOfScopeErrorMessage =
        "The NavQueryBuffer was allocated as Temp and it is now out of scope. " +
        "Dispose it at the end of the scope where it was created and do not access it again.";

    // Each node in the pool stores an index to the next node anywhere in the pool.
    // To save memory, indices stored in the node pool are of type unsigned short.
    // Keep in sync with kMaxNavMeshNodePoolSize = USHRT_MAX from NavMeshNode.h
    const int k_MaxNavMeshNodePoolSize = ushort.MaxValue;

    ///<summary>Creates a new buffer and allocates memory to store the intermediate NavMesh node data used by pathfinding operations.</summary>
    ///<remarks>Use the <c>maxNodesToVisit</c> parameter to size the buffer for the pathfinding methods that use it, such as <c>NavWorld.BeginFindPath</c>, <c>NavWorld.ContinueFindPath</c>, <c>NavWorld.EndFindPath</c>, and <c>NavWorld.GetResultFromFindPath</c>. If the buffer is too small for the search, the pathfinding method returns a <see cref="NavQueryStatus.MaxNodesToVisitExceeded" /> status. Unity clamps <c>maxNodesToVisit</c> to the range from 1 to 65,535. A value outside that range doesn't prevent Unity from creating the buffer: Unity logs a warning in the Editor and allocates the nearest allowed size instead.
    ///
    ///For more information about the container type that this buffer follows, refer to <see href="https://docs.unity3d.com/Manual/job-system-native-container.html">Introduction to NativeContainer</see>.</remarks>
    ///<param name="world">The <see cref="NavWorld" /> for which this buffer is used. The buffer can be passed only to methods of the same NavWorld it was created with.</param>
    ///<param name="allocator">The label indicating the desired lifetime of the object. The <c>allocator</c> parameter has no effect; the buffer is always allocated as <see cref="Allocator.Persistent">Persistent</see>.</param>
    ///<param name="maxNodesToVisit">The maximum number of nodes that can be stored in the buffer during a search operation. Unity clamps this value to the range from 1 to 65,535. The default value is 1024.</param>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/NavQueryBufferConstructorExample.cs}]]></code></example>
    public NavQueryBuffer(NavWorld world, Allocator allocator, int maxNodesToVisit = 1024)
    {
        if (!world.IsValid())
            throw new ArgumentException(
                "The provided NavWorld is invalid and cannot be used to create a NavQueryBuffer.", nameof(world));

        if (maxNodesToVisit < 1)
            Debug.LogWarning(
                "NavQueryBuffer allocated memory for 1 element " +
                "because it cannot be used when maxNodesToVisit is less than 1.");

        if (maxNodesToVisit > k_MaxNavMeshNodePoolSize)
            Debug.LogWarning(
                $"NavQueryBuffer allocated memory for only {k_MaxNavMeshNodePoolSize} nodes " +
                "because it cannot be used with maxNodesToVisit greater than that limit.");
        m_NavMeshQuery = Create(world.navMeshPtr, maxNodesToVisit);
        if (m_NavMeshQuery != IntPtr.Zero)
        {
            m_NavMeshUniqueId = world.uniqueId;
            UnsafeUtility.LeakRecord(m_NavMeshQuery, LeakCategory.NavQueryBuffer, 0);
        }
        else
        {
            m_NavMeshUniqueId = 0;
        }

        if (m_NavMeshQuery == IntPtr.Zero)
            throw new OutOfMemoryException(
                "Failed to allocate memory for a NavQueryBuffer that pathfinding can use to store " +
                $"{maxNodesToVisit} visited nodes.");

        AtomicSafetyHandle.CreateHandle(out m_Safety, allocator);
        AtomicSafetyHandle.SetStaticSafetyId(ref m_Safety, k_StaticSafetyId);

        AddQuerySafety(m_NavMeshQuery, m_Safety);

        m_SafetyUniqueId = GetUniqueId(m_NavMeshQuery);
        var brokenNodePoolInit = m_SafetyUniqueId == 0;
        if (brokenNodePoolInit)
            m_SafetyUniqueId = uint.MaxValue;
    }

    ///<summary>Destroys the NavQueryBuffer and deallocates all memory used by it.</summary>
    ///<remarks>Call this method when the buffer is no longer needed. After you dispose it, the NavQueryBuffer can't be reused, and any subsequent <see cref="NavWorld" /> pathfinding call that receives it throws an exception when executed in the Editor. Each NavQueryBuffer you create must be paired with exactly one call to <see cref="NavQueryBuffer.Dispose" />.</remarks>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/NavQueryBufferDisposeExample.cs}]]></code></example>
    [WriteAccessRequired]
    public void Dispose()
    {
        // Throw if the buffer has already been disposed (e.g. from a copy of the struct).
        // Without this check the runtime will crash when trying to dispose the same internal query a second time.
        CheckValidAndThrow();

        if (AtomicSafetyHandle.IsValidNonDefaultHandle(m_Safety))
        {
            // When the NavMesh destroys itself it disables read or write access of all stored safeties,
            // in ReleaseSafetiesAndForceCompletion().
            // Since the NavMesh has been deallocated, we shouldn't deregister the NavQueryBuffer from it.
            // We need to extract canRemoveSafety before disposing the handle,
            // because the atomic safety handle stores that state.
            var canRemoveSafety = AtomicSafetyHandle.GetAllowReadOrWriteAccess(m_Safety);

            AtomicSafetyHandle.DisposeHandle(ref m_Safety);

            if (canRemoveSafety && m_NavMeshQuery != IntPtr.Zero)
                RemoveQuerySafety(m_NavMeshQuery, m_Safety);
        }
        if (m_NavMeshQuery == IntPtr.Zero)
            return;

        UnsafeUtility.LeakErase(m_NavMeshQuery, LeakCategory.NavQueryBuffer);
        Destroy(m_NavMeshQuery);
        m_NavMeshQuery = IntPtr.Zero;
        m_NavMeshUniqueId = 0;
    }

    static extern IntPtr Create(IntPtr navMesh, int nodePoolSize);

    static extern void Destroy(IntPtr navMeshQuery);

    ///<summary>Checks whether two NavQueryBuffer values refer to the same underlying pathfinding buffer.</summary>
    ///<remarks>This operator is a syntactic shortcut equivalent to calling <see cref="NavQueryBuffer.Equals(NavQueryBuffer)" />. Use it inside expressions where the <c>==</c> syntax is more concise than an explicit method call. The comparison only succeeds when both values point to the same allocation in memory.</remarks>
    ///<param name="left">The first NavQueryBuffer to compare.</param>
    ///<param name="right">The second NavQueryBuffer to compare.</param>
    ///<returns><c>true</c> if the two NavQueryBuffer values point to the same allocation, <c>false</c> otherwise.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/NavQueryBufferEqualOperatorExample.cs}]]></code></example>
    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public static bool operator ==(NavQueryBuffer left, NavQueryBuffer right)
    {
        return left.Equals(right);
    }

    ///<summary>Checks whether two NavQueryBuffer values refer to different underlying pathfinding buffers.</summary>
    ///<remarks>This operator is a syntactic shortcut equivalent to negating the result of <see cref="NavQueryBuffer.Equals(NavQueryBuffer)" />. Use it inside expressions where the <c>!=</c> syntax is more concise than an explicit method call. Two buffers created by separate <see cref="NavQueryBuffer.NavQueryBuffer(NavWorld, Allocator, int)">constructor</see> calls are always considered different.</remarks>
    ///<param name="left">The first NavQueryBuffer to compare.</param>
    ///<param name="right">The second NavQueryBuffer to compare.</param>
    ///<returns><c>true</c> if the two NavQueryBuffer values point to different allocations, <c>false</c> otherwise.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/NavQueryBufferNotEqualOperatorExample.cs}]]></code></example>
    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public static bool operator !=(NavQueryBuffer left, NavQueryBuffer right)
    {
        return !left.Equals(right);
    }

    ///<summary>Checks whether two NavQueryBuffer values refer to the same underlying pathfinding buffer.</summary>
    ///<remarks>The comparison checks whether both values point to the same allocation in memory. Two <see cref="NavQueryBuffer" /> instances that were created by separate calls to the <see cref="NavQueryBuffer.NavQueryBuffer(NavWorld, Allocator, int)">constructor</see> are never equal, even if their <c>maxNodesToVisit</c> values are identical.</remarks>
    ///<param name="other">A NavQueryBuffer to compare this one with.</param>
    ///<returns><c>true</c> if the two NavQueryBuffer values refer to the same underlying pathfinding buffer, and <c>false</c> otherwise.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/NavQueryBufferEqualsExample.cs}]]></code></example>
    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public readonly bool Equals(NavQueryBuffer other)
    {
        var pointersEqual = m_NavMeshQuery == other.m_NavMeshQuery && m_NavMeshUniqueId == other.m_NavMeshUniqueId;

        pointersEqual = pointersEqual && m_SafetyUniqueId == other.m_SafetyUniqueId;
        return pointersEqual;
    }

    ///<summary>Checks whether two NavQueryBuffer values refer to the same underlying pathfinding buffer.</summary>
    ///<remarks>The comparison checks whether both values point to the same allocation in memory. Two <see cref="NavQueryBuffer" /> instances that were created by separate calls to the <see cref="NavQueryBuffer.NavQueryBuffer(NavWorld, Allocator, int)">constructor</see> are never equal, even if their <c>maxNodesToVisit</c> values are identical.</remarks>
    ///<param name="obj">An object to interpret as a NavQueryBuffer and compare this one with.</param>
    ///<returns><c>true</c> if the two NavQueryBuffer values refer to the same underlying pathfinding buffer, and <c>false</c> otherwise.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/NavQueryBufferEqualsExample.cs}]]></code></example>
    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public readonly override bool Equals(object obj)
    {
        return obj is NavQueryBuffer other && Equals(other);
    }

    ///<summary>Computes a hash code for identifying this NavQueryBuffer in hash-based collections.</summary>
    ///<remarks>The returned value is suitable for storing the NavQueryBuffer in hash-based collections such as Dictionary or HashSet. Two NavQueryBuffer values that compare equal through <see cref="NavQueryBuffer.Equals(NavQueryBuffer)" /> also produce the same hash code.</remarks>
    ///<returns>A hash code derived from the buffer's internal allocation identifier.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/NavQueryBufferHashExample.cs}]]></code></example>
    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public readonly override int GetHashCode()
    {
        var hashCode = HashCode.Combine(m_NavMeshQuery, m_NavMeshUniqueId);

        hashCode = HashCode.Combine(hashCode, m_SafetyUniqueId);
        return hashCode;
    }

    static extern void AddQuerySafety(IntPtr navMeshQuery, AtomicSafetyHandle handle);
    static extern void RemoveQuerySafety(IntPtr navMeshQuery, AtomicSafetyHandle handle);

    [NativeMethod(IsThreadSafe = true)]
    static extern uint GetUniqueId(IntPtr navMeshQuery);

    [NativeMethod(IsThreadSafe = true)]
    static extern bool HasNodePool(IntPtr navMeshQuery);

    internal readonly bool HasNodePool()
    {
        if (m_NavMeshQuery == IntPtr.Zero)
            throw new InvalidOperationException(k_NoInternalQueryAllocatedErrorMessage);

        return HasNodePool(m_NavMeshQuery);
    }

    internal readonly void CheckWriteSafetyAndThrow()
    {
        AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
        CheckValidAndThrow();
    }

    internal readonly void CheckValidAndThrow()
    {
        if (!AtomicSafetyHandle.IsDefaultValue(m_Safety) && !AtomicSafetyHandle.IsHandleValid(m_Safety))
        {
            if (AtomicSafetyHandle.IsTempMemoryHandle(m_Safety))
                throw new ObjectDisposedException(k_OutOfScopeErrorMessage);
            else
                throw new ObjectDisposedException(k_NoInternalQueryAllocatedErrorMessage);
        }

        var currentUniqueId = GetUniqueId(m_NavMeshQuery);
        if (currentUniqueId != m_SafetyUniqueId)
            throw new ObjectDisposedException(k_NoInternalQueryAllocatedErrorMessage);
    }
}
