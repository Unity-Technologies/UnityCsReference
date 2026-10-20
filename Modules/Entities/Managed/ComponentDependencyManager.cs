// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEngine.Assertions;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Profiling;
using Assert = UnityEngine.Assertions.AssertCore;
using ProfilerMarker = Unity.Entities.EntitiesProfilerMarker;

namespace Unity.Entities
{

    /// <summary>
    /// The ComponentDependencyManager maintains JobHandles for each type with any jobs that read or write those component types.
    /// ComponentSafetyHandles which is embedded maintains a safety handle for each component type registered in the TypeManager.
    /// Safety and job handles are only maintained for components that can be modified by jobs:
    /// That means only dynamic buffer components and component data that are not tag components will have valid
    /// safety and job handles. For those components the safety handle represents ReadOnly or ReadWrite access to those
    /// components as well as their change versions.
    /// The Entity type is a special case: It can not be modified by jobs and its safety handle is used to represent the
    /// entire EntityManager state. Any job reading from any part of the EntityManager must contain either a safety handle
    /// for the Entity type OR a safety handle for any other component type.
    /// Job component systems that have no other type dependencies have their JobHandles registered on the Entity type
    /// to ensure that they are completed by CompleteAllJobsAndInvalidateArrays
    /// </summary>
    unsafe partial struct ComponentDependencyManager
    {
        struct DependencyHandle
        {
            public JobHandle WriteFence;
            public int       NumReadFences;
            public TypeIndex TypeIndex;
        }

        const int              kMaxWriteJobHandles = 1;
        const int              kMaxReadJobHandles = 17;
        const int              kMaxTypes = TypeManager.MaximumTypesCount;


        // Indexed by TypeIndex
        ushort*                m_TypeArrayIndices;
        DependencyHandle*      m_DependencyHandles;
        ushort                 m_DependencyHandlesCount;
        JobHandle*             m_ReadJobFences;

        TypeIndex              EntityTypeIndex;
        const ushort           NullTypeIndex = 0xFFFF;

        JobHandle              m_ExclusiveTransactionDependency;
        byte                   _IsInTransaction;

        private ProfilerMarker m_Marker;
        private WorldUnmanaged m_World;

        public ComponentSafetyHandles Safety;
        public ForEachDisallowStructuralChangeSupport ForEachStructuralChange;

        ushort GetTypeArrayIndex(TypeIndex typeIndex)
        {
            var arrayIndex = m_TypeArrayIndices[typeIndex.Index];
            if (arrayIndex != NullTypeIndex)
                return arrayIndex;

            Assert.IsFalse(TypeManager.IsZeroSized(typeIndex) && !TypeManager.IsEnableable(typeIndex));
            arrayIndex = m_DependencyHandlesCount++;
            m_TypeArrayIndices[typeIndex.Index] = arrayIndex;
            m_DependencyHandles[arrayIndex].TypeIndex = typeIndex;
            m_DependencyHandles[arrayIndex].NumReadFences = 0;
            m_DependencyHandles[arrayIndex].WriteFence = new JobHandle();

            return arrayIndex;
        }

        void ClearDependencies()
        {
            for (int i = 0; i < m_DependencyHandlesCount; ++i)
                m_TypeArrayIndices[m_DependencyHandles[i].TypeIndex.Index] = NullTypeIndex;
            m_DependencyHandlesCount = 0;
        }

        public void OnCreate(WorldUnmanaged world)
        {
            m_World = world;
            var memoryLabel = Memory.CreateLabel("Entities", "Jobs.Dependencies", Allocator.Persistent);
            m_TypeArrayIndices = (ushort*)Memory.Unmanaged.Allocate(sizeof(ushort) * kMaxTypes, 16, memoryLabel);
            UnsafeUtility.MemSet(m_TypeArrayIndices, 0xFF, sizeof(ushort) * kMaxTypes);

            m_ReadJobFences = (JobHandle*)Memory.Unmanaged.Allocate(sizeof(JobHandle) * kMaxReadJobHandles * kMaxTypes, 16, memoryLabel);
            UnsafeUtility.MemClear(m_ReadJobFences, sizeof(JobHandle) * kMaxReadJobHandles * kMaxTypes);

            EntityTypeIndex = TypeManager.GetTypeIndex<Entity>();

            m_DependencyHandles = (DependencyHandle*)Memory.Unmanaged.Allocate(sizeof(DependencyHandle) * kMaxTypes, 16, memoryLabel);
            UnsafeUtility.MemClear(m_DependencyHandles, sizeof(DependencyHandle) * kMaxTypes);

            m_DependencyHandlesCount = 0;
            _IsInTransaction = 0;
            m_ExclusiveTransactionDependency = default;

            ForEachStructuralChange.Init();

            Safety.OnCreate();
            m_Marker = new ProfilerMarker("CompleteAllJobs");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        private static void AssertCompleteSyncPoint()
        {
            if (JobsUtility.IsExecutingJob)
            {
                throw new InvalidOperationException(
                    "Jobs accessing the entity manager must issue a complete sync point");
            }
        }

        public void CompleteAllJobs()
        {
            var executingSystem = m_World.ExecutingSystem;
            if (executingSystem != default)
            {
                var systemState = m_World.ResolveSystemState(executingSystem);
                if (systemState != null)
                    systemState->m_JobHandle.Complete();
            }

            if (m_DependencyHandlesCount != 0)
            {
                AssertCompleteSyncPoint();
                m_Marker.Begin();
                GetCombinedDependencyForAllTypes().Complete();
                ClearDependencies();
                m_Marker.End();
            }
        }

        public void CompleteAllJobsAndInvalidateArrays()
        {
            CompleteAllJobs();

            Safety.CompleteAllJobsAndInvalidateArrays();
        }

        public void CompleteAllJobsAndCheckDeallocateAndThrow()
        {
            CompleteAllJobs();

            Safety.CheckAllJobsCanDeallocate();
        }

        public void Dispose()
        {
            GetCombinedDependencyForAllTypes().Complete();

            var memoryLabel = Memory.CreateLabel("Entities", "Jobs.Dependencies", Allocator.Persistent);
            Memory.Unmanaged.Free(m_TypeArrayIndices, memoryLabel);
            Memory.Unmanaged.Free(m_DependencyHandles, memoryLabel);
            m_DependencyHandles = null;

            Memory.Unmanaged.Free(m_ReadJobFences, memoryLabel);
            m_ReadJobFences = null;

            ForEachStructuralChange.Dispose();
            Safety.Dispose();
        }

        public void PreDisposeCheck()
        {
            GetCombinedDependencyForAllTypes().Complete();
            Safety.PreDisposeCheck();
        }

        public void CompleteDependenciesNoChecks(TypeIndex* readerTypes, int readerTypesCount, TypeIndex* writerTypes, int writerTypesCount)
        {
            var combinedJobHandle = GetDependency(readerTypes, readerTypesCount, writerTypes, writerTypesCount,
                clearReadFencesAfterCombining:true);
            combinedJobHandle.Complete();
        }

        public bool HasReaderOrWriterDependency(TypeIndex typeIndex, JobHandle dependency)
        {
            var typeArrayIndex = m_TypeArrayIndices[typeIndex.Index];
            if (typeArrayIndex == NullTypeIndex)
                return false;

            var writer = m_DependencyHandles[typeArrayIndex].WriteFence;
            if (JobHandle.CheckFenceIsDependencyOrDidSyncFence(dependency, writer))
                return true;

            var count = m_DependencyHandles[typeArrayIndex].NumReadFences;
            for (var r = 0; r < count; r++)
            {
                var reader = m_ReadJobFences[typeArrayIndex * kMaxReadJobHandles + r];
                if (JobHandle.CheckFenceIsDependencyOrDidSyncFence(dependency, reader))
                    return true;
            }

            return false;
        }

        // Get the combined JobHandle for the provided reader and writer types. This is the job that must be completed
        // to guarantee that the provided read and writes have completed.
        // If clearReadFencesAfterCombining is true, the writer types will have their read fence counts reset to zero after
        // the combined handle is computed. This should only be used when the caller is immediately going to complete the combined
        // job handle, as a minor optimization to prevent subsequent operations from needlessly gathering the old read fences.
        // When in doubt, it is always safe to pass false.
        public JobHandle GetDependency(TypeIndex* readerTypes, int readerTypesCount, TypeIndex* writerTypes, int writerTypesCount,
            bool clearReadFencesAfterCombining)
        {
            JobHandle *allHandles = stackalloc JobHandle[readerTypesCount * kMaxWriteJobHandles +
                                                         writerTypesCount * (kMaxWriteJobHandles+kMaxReadJobHandles)];
            var allHandleCount = 0;
            for (var i = 0; i != readerTypesCount; i++)
            {
                var typeArrayIndex = m_TypeArrayIndices[readerTypes[i].Index];
                if (typeArrayIndex != NullTypeIndex)
                    allHandles[allHandleCount++] = m_DependencyHandles[typeArrayIndex].WriteFence;
            }

            for (var i = 0; i != writerTypesCount; i++)
            {
                var typeArrayIndex = m_TypeArrayIndices[writerTypes[i].Index];
                if (typeArrayIndex == NullTypeIndex)
                    continue;
                allHandles[allHandleCount++] = m_DependencyHandles[typeArrayIndex].WriteFence;
                int numReadFences = m_DependencyHandles[typeArrayIndex].NumReadFences;
                var readFences = m_ReadJobFences + typeArrayIndex * kMaxReadJobHandles;
                for (var j = 0; j != numReadFences; j++)
                    allHandles[allHandleCount++] = readFences[j];
                // If the caller intends to immediately complete the combined job handle, we can reset the read fence count
                // here while we already have this array element in cache.
                if (clearReadFencesAfterCombining)
                    m_DependencyHandles[typeArrayIndex].NumReadFences = 0;
            }

            if (Hint.Unlikely(allHandleCount == 0))
                return default;
            if (allHandleCount == 1)
                return allHandles[0];
            return JobHandleUnsafeUtility.CombineDependencies(allHandles, allHandleCount);
        }

        public JobHandle AddDependency(TypeIndex* readerTypes, int readerTypesCount, TypeIndex* writerTypes, int writerTypesCount,
            JobHandle dependency)
        {
            JobHandle* combinedDependencies = null;
            var combinedDependenciesCount = 0;
            if (readerTypesCount == 0 && writerTypesCount == 0)
            {
                ushort entityTypeArrayIndex = GetTypeArrayIndex(EntityTypeIndex);
                // if no dependency types are provided add read dependency to the Entity type
                // to ensure these jobs are still synced by CompleteAllJobsAndInvalidateArrays
                m_ReadJobFences[entityTypeArrayIndex * kMaxReadJobHandles +
                                m_DependencyHandles[entityTypeArrayIndex].NumReadFences] = dependency;
                m_DependencyHandles[entityTypeArrayIndex].NumReadFences++;

                if (m_DependencyHandles[entityTypeArrayIndex].NumReadFences == kMaxReadJobHandles)
                {
                    //@TODO: Check dynamically if the job debugger is enabled?
                    return CombineReadDependencies(entityTypeArrayIndex);
                }
                return dependency;
            }

            for (var i = 0; i != writerTypesCount; i++)
            {
                m_DependencyHandles[GetTypeArrayIndex(writerTypes[i])].WriteFence = dependency;
            }


            for (var i = 0; i != readerTypesCount; i++)
            {
                var reader = GetTypeArrayIndex(readerTypes[i]);
                m_ReadJobFences[reader * kMaxReadJobHandles + m_DependencyHandles[reader].NumReadFences] =
                    dependency;
                m_DependencyHandles[reader].NumReadFences++;

                if (m_DependencyHandles[reader].NumReadFences == kMaxReadJobHandles)
                {
                    var combined = CombineReadDependencies(reader);
                    if (combinedDependencies == null)
                    {
                        JobHandle* temp = stackalloc JobHandle[readerTypesCount];
                        combinedDependencies = temp;
                    }

                    combinedDependencies[combinedDependenciesCount++] = combined;
                }
            }

            if (combinedDependencies != null)
            {
                if (combinedDependenciesCount == 1)
                    return combinedDependencies[0];
                return JobHandleUnsafeUtility.CombineDependencies(combinedDependencies, combinedDependenciesCount);
            }

            return dependency;
        }

        internal void CompleteWriteDependencyNoChecks(TypeIndex typeIndex)
        {
            var arrayIndex = m_TypeArrayIndices[typeIndex.Index];
            if (arrayIndex != NullTypeIndex)
                m_DependencyHandles[arrayIndex].WriteFence.Complete();
        }

        internal void CompleteReadAndWriteDependencyNoChecks(TypeIndex typeIndex)
        {
            var arrayIndex = m_TypeArrayIndices[typeIndex.Index];
            if (arrayIndex != NullTypeIndex)
            {
                int readHandleCount = m_DependencyHandles[arrayIndex].NumReadFences;
                if (readHandleCount == 0)
                {
                    m_DependencyHandles[arrayIndex].WriteFence.Complete();
                    return;
                }
                int allHandleCount = readHandleCount + kMaxWriteJobHandles;
                var allHandles = stackalloc JobHandle[allHandleCount];
                JobHandle* readHandlesForType = m_ReadJobFences + arrayIndex * kMaxReadJobHandles;
                for (var i = 0; i < readHandleCount; ++i)
                    allHandles[i] = readHandlesForType[i];
                allHandles[readHandleCount] = m_DependencyHandles[arrayIndex].WriteFence;
                // We know there's at least one read handle and one write handle, so an early-out for
                // allHandleCount == 1 isn't needed.
                JobHandleUnsafeUtility.CombineDependencies(allHandles, allHandleCount).Complete();

                m_DependencyHandles[arrayIndex].NumReadFences = 0;
            }
        }

        public void CompleteWriteDependency(TypeIndex type)
        {
            CompleteWriteDependencyNoChecks(type);
            Safety.CompleteWriteDependency(type);
        }

        public void CompleteReadAndWriteDependency(TypeIndex type)
        {
            CompleteReadAndWriteDependencyNoChecks(type);
            if (TypeManager.GetTypeInfo(type).Category == TypeManager.TypeCategory.EntityData)
                throw new InvalidOperationException($"Can't complete a write-dependency for type Unity.Entities.Entity");
            Safety.CompleteReadAndWriteDependency(type);
        }

        JobHandle CombineReadDependencies(ushort typeArrayIndex)
        {
            var combined = JobHandleUnsafeUtility.CombineDependencies(
                m_ReadJobFences + typeArrayIndex * kMaxReadJobHandles, m_DependencyHandles[typeArrayIndex].NumReadFences);

            m_ReadJobFences[typeArrayIndex * kMaxReadJobHandles] = combined;
            m_DependencyHandles[typeArrayIndex].NumReadFences = 1;

            return combined;
        }

        JobHandle GetCombinedDependencyForAllTypes()
        {
            JobHandle* allHandles = stackalloc JobHandle[m_DependencyHandlesCount*(kMaxReadJobHandles + kMaxWriteJobHandles)];
            int allHandleCount = 0;
            for (var i = 0; i != m_DependencyHandlesCount; i++)
            {
                allHandles[allHandleCount++] = m_DependencyHandles[i].WriteFence;
                int readHandleCount = m_DependencyHandles[i].NumReadFences;
                JobHandle *readHandles = m_ReadJobFences + i * kMaxReadJobHandles;
                for(int j = 0; j < readHandleCount; ++j)
                    allHandles[allHandleCount++] = readHandles[j];
            }
            if (Hint.Unlikely(allHandleCount == 0))
                return default;
            if (allHandleCount == 1)
                return allHandles[0];
            return JobHandleUnsafeUtility.CombineDependencies(allHandles, allHandleCount);
        }
    }

    // Shared code of the above two different implementation
    partial struct ComponentDependencyManager
    {
        internal bool IsInTransaction
        {
            get { return _IsInTransaction != 0; }
            set { _IsInTransaction = (byte) (value ? 1u : 0u); }
        }

        public JobHandle ExclusiveTransactionDependency
        {
            get { return m_ExclusiveTransactionDependency; }
            set
            {
                if (_IsInTransaction == 0)
                    throw new InvalidOperationException(
                        "EntityManager.ExclusiveEntityTransactionDependency can only be used after EntityManager.BeginExclusiveEntityTransaction has been called.");

                if (!JobHandle.CheckFenceIsDependencyOrDidSyncFence(m_ExclusiveTransactionDependency, value))
                    throw new InvalidOperationException(
                        message: "EntityManager.ExclusiveEntityTransactionDependency must depend on the Entity Transaction job. \n" +
"Correct usage looks like EntityManager.ExclusiveEntityTransactionDependency = \n" +
"yourJob.Schedule(EntityManager.ExclusiveEntityTransactionDependency) or \n" +
"EntityManager.ExclusiveEntityTransactionDependency = \n" +
"yourJob.Schedule(JobHandle.CombineDependencies(EntityManager.ExclusiveEntityTransactionDependency, someOtherJobHandle) \n"+
                "This is so that you don't lose the pre-existing job dependency when adding your own to the chain.");
                m_ExclusiveTransactionDependency = value;
            }
        }

        public void PreEndExclusiveTransaction()
        {
            if (_IsInTransaction == 1)
            {
                m_ExclusiveTransactionDependency.Complete();
            }
        }

        public void EndExclusiveTransaction()
        {
            if (_IsInTransaction == 0)
                return;

            Safety.EndExclusiveTransaction();
            _IsInTransaction = 0;
        }

        public void BeginExclusiveTransaction()
        {
            if (_IsInTransaction == 1)
                return;

            Safety.BeginExclusiveTransaction();

            _IsInTransaction = 1;
            m_ExclusiveTransactionDependency = GetCombinedDependencyForAllTypes();
            ClearDependencies();
        }
    }

    internal unsafe struct ForEachDisallowStructuralChangeSupport
    {
        const int         kMaxNestedForEachDisallowStructuralChange = 32;

        internal int      Depth;

        EntityQueryMask*  _forEachQueryMasks;

        public void Init()
        {
            Depth = 0;
            var memoryLabel = Memory.CreateLabel("Entities", "Safety.StructuralChange", Allocator.Persistent);
            _forEachQueryMasks = (EntityQueryMask*)Memory.Unmanaged.Allocate(sizeof(EntityQueryMask) * kMaxNestedForEachDisallowStructuralChange, 16, memoryLabel);
        }

        internal void BeginIsInForEach(EntityQueryImpl* query)
        {
            CheckIsWithinMaxAllowedNestedForEach(Depth);
            if (query->_QueryData->HasEnableableComponents != 0)
                _forEachQueryMasks[Depth++] = new EntityQueryMask();
            else
                _forEachQueryMasks[Depth++] = query->GetEntityQueryMask();
        }

        void CheckIsWithinMaxAllowedNestedForEach(int value)
        {
            if (value >= kMaxNestedForEachDisallowStructuralChange)
                throw new InvalidOperationException(
                    $"The maximum allowed number of nested foreach with structural changes is {kMaxNestedForEachDisallowStructuralChange}.");
        }

        internal void SetIsInForEachDisallowStructuralChangeCounter(int counter)
        {
            CheckIsWithinMaxAllowedNestedForEach(counter);
            if (counter > Depth)
            {
                ClearEntityQueryCacheFrom(Depth);
            }

            Depth = counter;
        }

        void ClearEntityQueryCacheFrom(int index)
        {
            UnsafeUtility.MemClear(
                _forEachQueryMasks + index * sizeof(EntityQueryMask),
                sizeof(EntityQueryMask) * (kMaxNestedForEachDisallowStructuralChange - Depth));
        }

        internal void EndIsInForEach()
        {
            Depth--;
        }

        internal bool IsAdditiveStructuralChangePossible(Archetype* archetype)
        {
            for (var i = 0; i < Depth; i++)
            {
                if (_forEachQueryMasks[i].IsCreated() && _forEachQueryMasks[i].Matches(archetype))
                    return false;
            }

            return true;
        }

        public void Dispose()
        {
            var memoryLabel = Memory.CreateLabel("Entities", "Safety.StructuralChange", Allocator.Persistent);
            Memory.Unmanaged.Free(_forEachQueryMasks, memoryLabel);
        }
    }
}
