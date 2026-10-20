// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    // Temporary until native code can access ECS chunks directly. Reads and writes the components of entities for EntityComponentDataAccess.
    // An exception must not cross back into native code, so each callback logs it and returns 0.
    [NativeHeader("Runtime/Mono/SerializationBackend_DirectMemoryAccess/EntityComponentDataAccess.h")]
    static unsafe partial class EntityComponentDataAccessCallbacks
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int HasComponentDelegate(ulong entityId, uint componentTypeId);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate uint GetComponentCountDelegate(ulong entityId);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate uint GetComponentTypeIdAtDelegate(ulong entityId, uint index);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int ReadComponentDelegate(ulong entityId, uint componentTypeId, IntPtr dst, uint size);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int WriteComponentDelegate(ulong entityId, uint componentTypeId, IntPtr src, uint size);

        // Native code holds pointers to these, so they must outlive the registration.
        [NoAutoStaticsCleanup] static readonly HasComponentDelegate k_HasComponent = HasComponent;
        [NoAutoStaticsCleanup] static readonly GetComponentCountDelegate k_GetComponentCount = GetComponentCount;
        [NoAutoStaticsCleanup] static readonly GetComponentTypeIdAtDelegate k_GetComponentTypeIdAt = GetComponentTypeIdAt;
        [NoAutoStaticsCleanup] static readonly ReadComponentDelegate k_ReadComponent = ReadComponent;
        [NoAutoStaticsCleanup] static readonly WriteComponentDelegate k_WriteComponent = WriteComponent;

        [FreeFunction("EntityComponentDataAccess::SetEntityCallbacks")]
        static extern void SetEntityCallbacks(IntPtr hasComponent, IntPtr getComponentCount, IntPtr getComponentTypeIdAt, IntPtr readComponent, IntPtr writeComponent);

        [OnCodeLoaded]
        static void Register()
        {
            SetEntityCallbacks(
                Marshal.GetFunctionPointerForDelegate(k_HasComponent),
                Marshal.GetFunctionPointerForDelegate(k_GetComponentCount),
                Marshal.GetFunctionPointerForDelegate(k_GetComponentTypeIdAt),
                Marshal.GetFunctionPointerForDelegate(k_ReadComponent),
                Marshal.GetFunctionPointerForDelegate(k_WriteComponent));
        }

        [OnCodeUnloading]
        static void Unregister()
        {
            SetEntityCallbacks(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        }

        static bool TryFindEntityManager(Entity entity, out EntityManager entityManager)
        {
            foreach (var world in World.All)
            {
                if (!world.IsCreated)
                    continue;
                try
                {
                    if (world.EntityManager.Exists(entity))
                    {
                        entityManager = world.EntityManager;
                        return true;
                    }
                }
                catch (InvalidOperationException)
                {
                    // The World is in an exclusive transaction, such as a subscene load, and can't be used until it ends.
                }
            }
            entityManager = default;
            return false;
        }

        static bool IsSupported(TypeIndex typeIndex)
        {
            if (!typeIndex.IsComponentType || typeIndex.IsChunkComponent)
                return false;
            var type = TypeManager.GetType(typeIndex);
            return type != null && type.IsValueType;
        }

        // EntityId.TypeId is the TypeIndex without its flag bits.
        static bool TryGetTypeIndex(uint componentTypeId, out TypeIndex typeIndex)
        {
            typeIndex = TypeManager.GetTypeInfo(new TypeIndex { Value = (int)componentTypeId }).TypeIndex;
            return typeIndex.Index == (int)componentTypeId && typeIndex.Index != 0 && IsSupported(typeIndex);
        }

        static int HasComponent(ulong entityId, uint componentTypeId)
        {
            try
            {
                var entity = UnsafeUtility.As<ulong, Entity>(ref entityId);
                if (!TryFindEntityManager(entity, out var entityManager) || !TryGetTypeIndex(componentTypeId, out var typeIndex))
                    return 0;
                return entityManager.HasComponent(entity, ComponentType.ReadOnly(typeIndex)) ? 1 : 0;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return 0;
            }
        }

        static uint GetComponentCount(ulong entityId)
        {
            try
            {
                var entity = UnsafeUtility.As<ulong, Entity>(ref entityId);
                if (!TryFindEntityManager(entity, out var entityManager))
                    return 0;

                uint count = 0;
                using (var types = entityManager.GetComponentTypes(entity, Allocator.Temp))
                {
                    foreach (var type in types)
                    {
                        if (IsSupported(type.TypeIndex))
                            ++count;
                    }
                }
                return count;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return 0;
            }
        }

        static uint GetComponentTypeIdAt(ulong entityId, uint index)
        {
            try
            {
                var entity = UnsafeUtility.As<ulong, Entity>(ref entityId);
                if (!TryFindEntityManager(entity, out var entityManager))
                    return 0;

                uint supportedIndex = 0;
                using (var types = entityManager.GetComponentTypes(entity, Allocator.Temp))
                {
                    foreach (var type in types)
                    {
                        if (!IsSupported(type.TypeIndex))
                            continue;
                        if (supportedIndex == index)
                            return (uint)type.TypeIndex.Index;
                        ++supportedIndex;
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return 0;
            }
        }

        static bool TryFindComponent(ulong entityId, uint componentTypeId, uint size, out EntityManager entityManager, out Entity entity, out TypeIndex typeIndex)
        {
            entity = UnsafeUtility.As<ulong, Entity>(ref entityId);
            typeIndex = default;
            if (!TryFindEntityManager(entity, out entityManager) || !TryGetTypeIndex(componentTypeId, out typeIndex))
                return false;
            if (!entityManager.HasComponent(entity, ComponentType.ReadOnly(typeIndex)))
                return false;

            // Tags take no chunk space, but the managed size of an empty struct is 1, so the size of a tag is not checked.
            if (typeIndex.IsZeroSized)
                return true;

            int sizeInChunk = TypeManager.GetTypeInfo(typeIndex).SizeInChunk;
            if (sizeInChunk != (int)size)
            {
                Debug.LogError($"Size mismatch accessing component {TypeManager.GetType(typeIndex)}: {size} requested, {sizeInChunk} stored.");
                return false;
            }

            // A job may still be using the component.
            entityManager.CompleteAllTrackedJobs();
            return true;
        }

        static int ReadComponent(ulong entityId, uint componentTypeId, IntPtr dst, uint size)
        {
            try
            {
                if (!TryFindComponent(entityId, componentTypeId, size, out var entityManager, out var entity, out var typeIndex))
                    return 0;
                if (typeIndex.IsZeroSized)
                {
                    UnsafeUtility.MemClear((void*)dst, size);
                    return 1;
                }

                var storageInfo = entityManager.GetStorageInfo(entity);
                var typeHandle = entityManager.GetDynamicComponentTypeHandle(ComponentType.ReadOnly(typeIndex));
                var src = (byte*)storageInfo.Chunk.GetComponentDataPtrRO(ref typeHandle);
                if (src == null)
                    return 0;
                UnsafeUtility.MemCpy((void*)dst, src + (long)storageInfo.IndexInChunk * size, size);
                return 1;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return 0;
            }
        }

        static int WriteComponent(ulong entityId, uint componentTypeId, IntPtr src, uint size)
        {
            try
            {
                if (!TryFindComponent(entityId, componentTypeId, size, out var entityManager, out var entity, out var typeIndex))
                    return 0;
                if (typeIndex.IsZeroSized)
                    return 1;

                var storageInfo = entityManager.GetStorageInfo(entity);
                // A read-write handle marks the component as changed, so change filters in systems see the edit.
                var typeHandle = entityManager.GetDynamicComponentTypeHandle(ComponentType.ReadWrite(typeIndex));
                var components = storageInfo.Chunk.GetDynamicComponentDataArrayReinterpret<byte>(ref typeHandle, (int)size);
                if (components.Length == 0)
                    return 0;
                UnsafeUtility.MemCpy((byte*)components.GetUnsafePtr() + (long)storageInfo.IndexInChunk * size, (void*)src, size);
                return 1;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return 0;
            }
        }
    }
}
