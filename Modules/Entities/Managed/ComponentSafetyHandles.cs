// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Text;
using UnityEngine.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling;
using Unity.Scripting.LifecycleManagement;
using Assert = UnityEngine.Assertions.AssertCore;
using Debug = Unity.Scripting.LowLevel.Debug;
using ProfilerMarker = Unity.Entities.EntitiesProfilerMarker;

namespace Unity.Entities
{
    [GenerateTestsForBurstCompatibility(RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS", CompileTarget = GenerateTestsForBurstCompatibilityAttribute.BurstCompatibleCompileTarget.Editor)]
    // internal for BurstCompatible test support
    unsafe internal partial struct ComponentSafetyHandles
    {
        const int                   kMaxTypes = TypeManager.MaximumTypesCount;

        ComponentSafetyHandle*      m_ComponentSafetyHandles;
        ushort                      m_ComponentSafetyHandlesCount;
        TypeIndex                   EntityTypeIndex;

        ushort*                     m_TypeArrayIndices;
        const ushort                NullTypeIndex = 0xFFFF;

        unsafe struct StaticSafetyIdData
        {
            // Per-component-type Static safety IDs are shared across all Worlds.
            public int* m_StaticSafetyIdsForComponentLookup;
            public int* m_StaticSafetyIdsForArchetypeChunkArrays;
            public int m_StaticSafetyIdForDynamicSharedComponentTypeHandle;
            public int m_StaticSafetyIdForDynamicComponentTypeHandle;
            public int m_StaticSafetyIdForEntityTypeHandle;
        }
        static readonly SharedStatic<StaticSafetyIdData> m_StaticSafetyIdData = SharedStatic<StaticSafetyIdData>.GetOrCreate<StaticSafetyIdData>();

        static readonly FixedString128Bytes m_CustomDeallocatedErrorMessageBytes = "Attempted to access {5} which has been invalidated by a structural change.";
        static readonly FixedString128Bytes m_CustomDeallocatedFromJobErrorMessageBytes = "Attempted to access the {5} {3} which has been invalidated by a structural change.";

        public void SetCustomErrorMessage(int staticSafetyId, AtomicSafetyErrorType errorType, FixedString128Bytes messageBytes)
        {
            AtomicSafetyHandle.SetCustomErrorMessage(staticSafetyId, errorType, messageBytes.GetUnsafePtr(), messageBytes.Length);
        }

        int CreateStaticSafetyId(FixedString512Bytes ownerTypeName)
        {
            int staticSafetyId = AtomicSafetyHandle.NewStaticSafetyId(ownerTypeName.GetUnsafePtr(), ownerTypeName.Length);

            SetCustomErrorMessage(staticSafetyId, AtomicSafetyErrorType.Deallocated, m_CustomDeallocatedErrorMessageBytes);
            SetCustomErrorMessage(staticSafetyId, AtomicSafetyErrorType.DeallocatedFromJob, m_CustomDeallocatedFromJobErrorMessageBytes);

            return staticSafetyId;
        }

        void CreateStaticSafetyIdsForType(TypeIndex typeIndex)
        {
            var typeIndexWithoutFlags = typeIndex.Index;
            if (m_StaticSafetyIdData.Data.m_StaticSafetyIdsForComponentLookup[typeIndexWithoutFlags] == 0)
            {
                if (typeIndex.IsBuffer)
                {
                    m_StaticSafetyIdData.Data.m_StaticSafetyIdsForComponentLookup[typeIndexWithoutFlags] =
                        CreateStaticSafetyId(
                            $"BufferLookup<{TypeManager.GetTypeInfo(typeIndex).DebugTypeName}>");
                }
                else if (typeIndex.IsTransform)
                {
                    m_StaticSafetyIdData.Data.m_StaticSafetyIdsForComponentLookup[typeIndexWithoutFlags] =
                        CreateStaticSafetyId($"TransformLookup");
                }
                else
                {
                    m_StaticSafetyIdData.Data.m_StaticSafetyIdsForComponentLookup[typeIndexWithoutFlags] =
                        CreateStaticSafetyId(
                            $"ComponentLookup<{TypeManager.GetTypeInfo(typeIndex).DebugTypeName}>");
                }
            }
            if (m_StaticSafetyIdData.Data.m_StaticSafetyIdsForArchetypeChunkArrays[typeIndexWithoutFlags] == 0)
            {
                if (typeIndex.IsBuffer)
                {
                    m_StaticSafetyIdData.Data.m_StaticSafetyIdsForArchetypeChunkArrays[typeIndexWithoutFlags] =
                        CreateStaticSafetyId(
                            $"BufferTypeHandle<{TypeManager.GetTypeInfo(typeIndex).DebugTypeName}>");
                }
                else if (typeIndex.IsTransform)
                {
                    m_StaticSafetyIdData.Data.m_StaticSafetyIdsForArchetypeChunkArrays[typeIndexWithoutFlags] =
                        CreateStaticSafetyId($"TransformTypeHandle");
                }
                else if (typeIndex.IsSharedComponentType)
                {
                    m_StaticSafetyIdData.Data.m_StaticSafetyIdsForArchetypeChunkArrays[typeIndexWithoutFlags] =
                        CreateStaticSafetyId(
                            $"SharedComponentTypeHandle<{TypeManager.GetTypeInfo(typeIndex).DebugTypeName}>");
                }
                else
                {
                    m_StaticSafetyIdData.Data.m_StaticSafetyIdsForArchetypeChunkArrays[typeIndexWithoutFlags] =
                        CreateStaticSafetyId(
                            $"ComponentTypeHandle<{TypeManager.GetTypeInfo(typeIndex).DebugTypeName}>");
                }
            }
        }

        private void SetStaticSafetyIdForHandle_ArchetypeChunk(ref AtomicSafetyHandle handle, TypeIndex typeIndex, DynamicComponentTypeHandleType dynamicType)
        {
            // Configure safety handle static safety ID for ArchetypeChunk*Type by default
            int staticSafetyId = 0;
            if (dynamicType == DynamicComponentTypeHandleType.Dynamic)
                staticSafetyId = m_StaticSafetyIdData.Data.m_StaticSafetyIdForDynamicComponentTypeHandle;
            else if (dynamicType == DynamicComponentTypeHandleType.DynamicShared)
                staticSafetyId = m_StaticSafetyIdData.Data.m_StaticSafetyIdForDynamicSharedComponentTypeHandle;
            else if (typeIndex == EntityTypeIndex)
                staticSafetyId = m_StaticSafetyIdData.Data.m_StaticSafetyIdForEntityTypeHandle;
            else
                staticSafetyId = m_StaticSafetyIdData.Data.m_StaticSafetyIdsForArchetypeChunkArrays[typeIndex.Index];
            AtomicSafetyHandle.SetStaticSafetyId(ref handle, staticSafetyId);
        }

        private void SetStaticSafetyIdForHandle_FromEntity(ref AtomicSafetyHandle handle, TypeIndex typeIndex)
        {
            // Configure safety handle static safety ID for ArchetypeChunk*Type by default
            int staticSafetyId = m_StaticSafetyIdData.Data.m_StaticSafetyIdsForComponentLookup[typeIndex.Index];
            AtomicSafetyHandle.SetStaticSafetyId(ref handle, staticSafetyId);
        }

        ushort GetTypeArrayIndex(TypeIndex typeIndex)
        {
            var typeIndexWithoutFlags = typeIndex.Index;
            var arrayIndex = m_TypeArrayIndices[typeIndexWithoutFlags];
            if (arrayIndex != NullTypeIndex)
                return arrayIndex;

            arrayIndex = m_ComponentSafetyHandlesCount++;
            m_TypeArrayIndices[typeIndexWithoutFlags] = arrayIndex;
            m_ComponentSafetyHandles[arrayIndex].TypeIndex = typeIndex;

            m_ComponentSafetyHandles[arrayIndex].SafetyHandle = AtomicSafetyHandle.Create();
            AtomicSafetyHandle.SetAllowSecondaryVersionWriting(m_ComponentSafetyHandles[arrayIndex].SafetyHandle, false);
            AtomicSafetyHandle.SetNestedContainer(m_ComponentSafetyHandles[arrayIndex].SafetyHandle, typeIndex.HasNativeContainer);
            m_ComponentSafetyHandles[arrayIndex].BufferHandle = AtomicSafetyHandle.Create();
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_ComponentSafetyHandles[arrayIndex].BufferHandle, true);
            AtomicSafetyHandle.SetNestedContainer(m_ComponentSafetyHandles[arrayIndex].BufferHandle, typeIndex.HasNativeContainer);

            // Create static safety IDs for this type if they don't already exist.
            CreateStaticSafetyIdsForType(typeIndex);
            // Set default static safety IDs for handles
            SetStaticSafetyIdForHandle_ArchetypeChunk(ref m_ComponentSafetyHandles[arrayIndex].SafetyHandle, typeIndex, DynamicComponentTypeHandleType.None);
            SetStaticSafetyIdForHandle_ArchetypeChunk(ref m_ComponentSafetyHandles[arrayIndex].BufferHandle, typeIndex, DynamicComponentTypeHandleType.None);
            return arrayIndex;
        }

        void ClearAllTypeArrayIndices()
        {
            for (int i = 0; i < m_ComponentSafetyHandlesCount; ++i)
                m_TypeArrayIndices[m_ComponentSafetyHandles[i].TypeIndex.Index] = NullTypeIndex;
            m_ComponentSafetyHandlesCount = 0;
        }

        public void OnCreate()
        {
            var memoryLabel = Memory.CreateLabel("Entities", "Safety.ComponentHandles", Allocator.Persistent);
            m_TypeArrayIndices = (ushort*)Memory.Unmanaged.Allocate(sizeof(ushort) * kMaxTypes, 16, memoryLabel);
            UnsafeUtility.MemSet(m_TypeArrayIndices, 0xFF, sizeof(ushort) * kMaxTypes);

            m_ComponentSafetyHandles = (ComponentSafetyHandle*)Memory.Unmanaged.Allocate(sizeof(ComponentSafetyHandle) * kMaxTypes, 16, memoryLabel);
            UnsafeUtility.MemClear(m_ComponentSafetyHandles, sizeof(ComponentSafetyHandle) * kMaxTypes);

            m_TempSafety = AtomicSafetyHandle.Create();
            m_ComponentSafetyHandlesCount = 0;
            EntityTypeIndex = TypeManager.GetTypeIndex<Entity>();

            m_InvalidateArraysMarker = new ProfilerMarker("InvalidateArrays");

            m_StaticSafetyIdData.Data.m_StaticSafetyIdForDynamicComponentTypeHandle = CreateStaticSafetyId("Unity.Entities.DynamicComponentTypeHandle");
            m_StaticSafetyIdData.Data.m_StaticSafetyIdForDynamicSharedComponentTypeHandle = CreateStaticSafetyId("Unity.Entities.DynamicSharedComponentTypeHandle");
            m_StaticSafetyIdData.Data.m_StaticSafetyIdForEntityTypeHandle = CreateStaticSafetyId("Unity.Entities.EntityTypeHandle");
        }

        [NoAutoStaticsCleanup] // Shutdown runs as code unloads.
        static bool s_Initialized;

        [ExcludeFromBurstCompatTesting("Uses managed delegates")]
        public static void Initialize()
        {
            if (s_Initialized)
                return;
            s_Initialized = true;
            var memoryLabel = Memory.CreateLabel("Entities", "Safety.ComponentHandles", Allocator.Persistent);
            m_StaticSafetyIdData.Data.m_StaticSafetyIdsForComponentLookup =
                (int*)Memory.Unmanaged.Allocate(sizeof(int) * kMaxTypes, 16, memoryLabel);
            UnsafeUtility.MemClear(m_StaticSafetyIdData.Data.m_StaticSafetyIdsForComponentLookup, sizeof(int) * kMaxTypes);
            m_StaticSafetyIdData.Data.m_StaticSafetyIdsForArchetypeChunkArrays =
                (int*)Memory.Unmanaged.Allocate(sizeof(int) * kMaxTypes, 16, memoryLabel);
            UnsafeUtility.MemClear(m_StaticSafetyIdData.Data.m_StaticSafetyIdsForArchetypeChunkArrays, sizeof(int) * kMaxTypes);
        }

        [OnCodeUnloading]
        static void Shutdown()
        {
            if (s_Initialized)
            {
                var memoryLabel = Memory.CreateLabel("Entities", "Safety.ComponentHandles", Allocator.Persistent);
                Memory.Unmanaged.Free(m_StaticSafetyIdData.Data.m_StaticSafetyIdsForComponentLookup, memoryLabel);
                Memory.Unmanaged.Free(m_StaticSafetyIdData.Data.m_StaticSafetyIdsForArchetypeChunkArrays, memoryLabel);
                s_Initialized = false;
            }
        }

        public AtomicSafetyHandle ExclusiveTransactionSafety;
        private ProfilerMarker m_InvalidateArraysMarker;

        public void CompleteAllJobsAndInvalidateArrays()
        {
            if (m_ComponentSafetyHandlesCount == 0)
                return;

            m_InvalidateArraysMarker.Begin();
            for (var i = 0; i != m_ComponentSafetyHandlesCount; i++)
            {
                AtomicSafetyHandle.CheckDeallocateAndThrow(m_ComponentSafetyHandles[i].SafetyHandle);
                AtomicSafetyHandle.CheckDeallocateAndThrow(m_ComponentSafetyHandles[i].BufferHandle);
            }

            for (var i = 0; i != m_ComponentSafetyHandlesCount; i++)
            {
                AtomicSafetyHandle.Release(m_ComponentSafetyHandles[i].SafetyHandle);
                AtomicSafetyHandle.Release(m_ComponentSafetyHandles[i].BufferHandle);
            }

            ClearAllTypeArrayIndices();
            m_InvalidateArraysMarker.End();
        }

        public void CheckAllJobsCanDeallocate()
        {
            for (var i = 0; i != m_ComponentSafetyHandlesCount; i++)
            {
                AtomicSafetyHandle.CheckDeallocateAndThrow(m_ComponentSafetyHandles[i].SafetyHandle);
                AtomicSafetyHandle.CheckDeallocateAndThrow(m_ComponentSafetyHandles[i].BufferHandle);
            }
        }

        public void Dispose()
        {
            for (var i = 0; i < m_ComponentSafetyHandlesCount; i++)
            {
                var res0 = AtomicSafetyHandle.EnforceAllBufferJobsHaveCompletedAndRelease(m_ComponentSafetyHandles[i].SafetyHandle);
                var res1 = AtomicSafetyHandle.EnforceAllBufferJobsHaveCompletedAndRelease(m_ComponentSafetyHandles[i].BufferHandle);

                if (res0 == EnforceJobResult.DidSyncRunningJobs || res1 == EnforceJobResult.DidSyncRunningJobs)
                    Debug.LogError(
                        "Disposing EntityManager but a job is still running against the ComponentData. It appears the job has not been registered with SystemBase.AddDependency.");
            }

            AtomicSafetyHandle.Release(m_TempSafety);

            var memoryLabel = Memory.CreateLabel("Entities", "Safety.ComponentHandles", Allocator.Persistent);
            Memory.Unmanaged.Free(m_TypeArrayIndices, memoryLabel);
            Memory.Unmanaged.Free(m_ComponentSafetyHandles, memoryLabel);
            m_ComponentSafetyHandles = null;
        }

        internal void PanicSyncAll()
        {
            for (var i = 0; i < m_ComponentSafetyHandlesCount; i++)
            {
                var res0 = AtomicSafetyHandle.EnforceAllBufferJobsHaveCompleted(m_ComponentSafetyHandles[i].SafetyHandle);
                var res1 = AtomicSafetyHandle.EnforceAllBufferJobsHaveCompleted(m_ComponentSafetyHandles[i].BufferHandle);
            }
        }

        public void PreDisposeCheck()
        {
            for (var i = 0; i < m_ComponentSafetyHandlesCount; i++)
            {
                var res0 = AtomicSafetyHandle.EnforceAllBufferJobsHaveCompleted(m_ComponentSafetyHandles[i].SafetyHandle);
                var res1 = AtomicSafetyHandle.EnforceAllBufferJobsHaveCompleted(m_ComponentSafetyHandles[i].BufferHandle);
                if (res0 == EnforceJobResult.DidSyncRunningJobs || res1 == EnforceJobResult.DidSyncRunningJobs)
                    Debug.LogError(
                        "Disposing EntityManager but a job is still running against the ComponentData. It appears the job has not been registered with SystemBase.AddDependency.");
            }
        }

        public void CompleteWriteDependency(TypeIndex typeIndex)
        {
            var arrayIndex = m_TypeArrayIndices[typeIndex.Index];
            if (arrayIndex == NullTypeIndex)
                return;

            AtomicSafetyHandle.CheckReadAndThrow(m_ComponentSafetyHandles[arrayIndex].SafetyHandle);
            AtomicSafetyHandle.CheckReadAndThrow(m_ComponentSafetyHandles[arrayIndex].BufferHandle);
        }

        public void CompleteReadAndWriteDependency(TypeIndex typeIndex)
        {
            var arrayIndex = m_TypeArrayIndices[typeIndex.Index];
            if (arrayIndex == NullTypeIndex)
                return;

            AtomicSafetyHandle.CheckWriteAndThrow(m_ComponentSafetyHandles[arrayIndex].SafetyHandle);
            AtomicSafetyHandle.CheckWriteAndThrow(m_ComponentSafetyHandles[arrayIndex].BufferHandle);
        }

        public AtomicSafetyHandle GetEntityManagerSafetyHandle()
        {
            var handle = m_ComponentSafetyHandles[GetTypeArrayIndex(EntityTypeIndex)].SafetyHandle;
            AtomicSafetyHandle.UseSecondaryVersion(ref handle);
            return handle;
        }

        public AtomicSafetyHandle GetSafetyHandleForComponentLookup(TypeIndex type, bool isReadOnly)
        {
            var handle = GetSafetyHandle(type, isReadOnly);
            // Override the handle's default static safety ID
            SetStaticSafetyIdForHandle_FromEntity(ref handle, type);
            return handle;
        }

        public AtomicSafetyHandle GetBufferHandleForBufferLookup(TypeIndex type)
        {
            Assert.IsTrue(type.IsBuffer || type.IsTransform); // TODO DOTS-10269: Transforms should not be using this safety handle
            var handle = GetBufferSafetyHandle(type);
            // Override the handle's default static safety ID
            SetStaticSafetyIdForHandle_FromEntity(ref handle, type);
            return handle;
        }

        public AtomicSafetyHandle GetSafetyHandleForComponentTypeHandle(TypeIndex type, bool isReadOnly)
        {
            // safety handles are configured with the static safety ID for ArchetypeChunk*Type by default,
            // so no further static safety ID setup is necessary in this path.
            return GetSafetyHandle(type, isReadOnly);
        }

        private enum DynamicComponentTypeHandleType
        {
            None,
            Dynamic,
            DynamicShared
        }

        public AtomicSafetyHandle GetSafetyHandleForDynamicComponentTypeHandle(TypeIndex type, bool isReadOnly)
        {
            var handle = GetSafetyHandle(type, isReadOnly);
            // We need to override the handle's default static safety ID to use the DynamicComponentTypeHandle version.
            SetStaticSafetyIdForHandle_ArchetypeChunk(ref handle, type, DynamicComponentTypeHandleType.Dynamic);
            return handle;
        }

        public AtomicSafetyHandle GetSafetyHandleForDynamicSharedComponentTypeHandle(TypeIndex type, bool isReadOnly)
        {
            var handle = GetSafetyHandle(type, isReadOnly);
            // We need to override the handle's default static safety ID to use the DynamicComponentTypeHandle version.
            SetStaticSafetyIdForHandle_ArchetypeChunk(ref handle, type, DynamicComponentTypeHandleType.DynamicShared);
            return handle;
        }

        public AtomicSafetyHandle GetSafetyHandleForBufferTypeHandle(TypeIndex type, bool isReadOnly)
        {
            // safety handles are configured with the static safety ID for ArchetypeChunk*Type by default,
            // so no further static safety ID setup is necessary in this path.
            return GetSafetyHandle(type, isReadOnly);
        }

        public AtomicSafetyHandle GetBufferHandleForBufferTypeHandle(TypeIndex type)
        {
            Assert.IsTrue(type.IsBuffer || type.IsTransform);
            // safety handles are configured with the static safety ID for ArchetypeChunk*Type by default,
            // so no further static safety ID setup is necessary in this path.
            return GetBufferSafetyHandle(type);
        }

        public AtomicSafetyHandle GetSafetyHandleForSharedComponentTypeHandle(TypeIndex type)
        {
            Assert.IsTrue(type.IsSharedComponentType);
            var handle = GetSafetyHandle(type, false);
            // safety handles are configured with the static safety ID for ArchetypeChunk*Type by default,
            // so no further static safety ID setup is necessary in this path.
            return handle;
        }

        public AtomicSafetyHandle GetSafetyHandleForEntityTypeHandle()
        {
            var handle = GetEntityManagerSafetyHandle();
            // The EntityTypeIndex safety handle is pre-configured with a static safety ID for EntityTypeHandle,
            // so no further configuration is necessary here.
            return handle;
        }

        public AtomicSafetyHandle GetSafetyHandle(TypeIndex type, bool isReadOnly)
        {
            var arrayIndex = GetTypeArrayIndex(type);
            var handle = m_ComponentSafetyHandles[arrayIndex].SafetyHandle;
            if (isReadOnly)
                AtomicSafetyHandle.UseSecondaryVersion(ref handle);
            return handle;
        }

        public AtomicSafetyHandle GetBufferSafetyHandle(TypeIndex type)
        {
            Assert.IsTrue(type.IsBuffer || type.IsTransform); // TODO DOTS-10269: Transforms should not be using this safety handle
            var arrayIndex = GetTypeArrayIndex(type);
            return m_ComponentSafetyHandles[arrayIndex].BufferHandle;
        }

        public void BeginExclusiveTransaction()
        {
            for (var i = 0; i != m_ComponentSafetyHandlesCount; i++)
            {
                AtomicSafetyHandle.CheckDeallocateAndThrow(m_ComponentSafetyHandles[i].SafetyHandle);
                AtomicSafetyHandle.CheckDeallocateAndThrow(m_ComponentSafetyHandles[i].BufferHandle);
            }

            for (var i = 0; i != m_ComponentSafetyHandlesCount; i++)
            {
                AtomicSafetyHandle.Release(m_ComponentSafetyHandles[i].SafetyHandle);
                AtomicSafetyHandle.Release(m_ComponentSafetyHandles[i].BufferHandle);
            }

            ExclusiveTransactionSafety = AtomicSafetyHandle.Create();
            ClearAllTypeArrayIndices();
        }

        public void EndExclusiveTransaction()
        {
            var res = AtomicSafetyHandle.EnforceAllBufferJobsHaveCompletedAndRelease(ExclusiveTransactionSafety);
            if (res != EnforceJobResult.AllJobsAlreadySynced)
                //@TODO: Better message
                Debug.LogError("ExclusiveEntityTransaction job has not been registered");
        }

        struct ComponentSafetyHandle
        {
            public AtomicSafetyHandle SafetyHandle;
            public AtomicSafetyHandle BufferHandle;
            public TypeIndex          TypeIndex;
        }

        AtomicSafetyHandle m_TempSafety;
    }
}
