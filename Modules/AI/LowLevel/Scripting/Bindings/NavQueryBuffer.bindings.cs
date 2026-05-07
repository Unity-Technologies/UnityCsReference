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
    internal uint m_SafetyOpenListId;

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

        m_SafetyOpenListId = GetOpenListId(m_NavMeshQuery);
        var brokenNodePoolInit = m_SafetyOpenListId == 0;
        if (brokenNodePoolInit)
            m_SafetyOpenListId = uint.MaxValue;
    }

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

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public static bool operator ==(NavQueryBuffer left, NavQueryBuffer right)
    {
        return left.Equals(right);
    }

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public static bool operator !=(NavQueryBuffer left, NavQueryBuffer right)
    {
        return !left.Equals(right);
    }

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public readonly bool Equals(NavQueryBuffer other)
    {
        var pointersEqual = m_NavMeshQuery == other.m_NavMeshQuery && m_NavMeshUniqueId == other.m_NavMeshUniqueId;

        pointersEqual = pointersEqual && m_SafetyOpenListId == other.m_SafetyOpenListId;
        return pointersEqual;
    }

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public readonly override bool Equals(object obj)
    {
        return obj is NavQueryBuffer other && Equals(other);
    }

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public readonly override int GetHashCode()
    {
        var hashCode = HashCode.Combine(m_NavMeshQuery, m_NavMeshUniqueId);

        hashCode = HashCode.Combine(hashCode, m_SafetyOpenListId);
        return hashCode;
    }

    static extern void AddQuerySafety(IntPtr navMeshQuery, AtomicSafetyHandle handle);
    static extern void RemoveQuerySafety(IntPtr navMeshQuery, AtomicSafetyHandle handle);

    [NativeMethod(IsThreadSafe = true)]
    static extern uint GetOpenListId(IntPtr navMeshQuery);

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

        var safetyIdAtKnownAddress = GetOpenListId(m_NavMeshQuery);
        if (safetyIdAtKnownAddress != m_SafetyOpenListId)
            throw new ObjectDisposedException(k_NoInternalQueryAllocatedErrorMessage);
    }
}
