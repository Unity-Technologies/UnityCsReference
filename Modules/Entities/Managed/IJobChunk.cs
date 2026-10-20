// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Assertions;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Diagnostics;
using Assert = UnityEngine.Assertions.AssertCore;

namespace Unity.Entities
{
    /// <summary>
    /// IJobChunk is a type of [IJob] that iterates over a set of chunks. For each chunk the job runs on,
    /// the job code receives an <see cref="ArchetypeChunk"/> instance representing the full [chunk], plus a bitmask
    /// indicating which entities in the chunk should be processed.
    ///
    /// [IJob]: xref:Unity.Jobs.IJob
    /// [chunk]: xref:concepts-archetypes#archetype-chunks
    /// </summary>
    /// <remarks>
    /// Schedule or run an IJobChunk job inside the <see cref="SystemBase.OnUpdate"/> function of a
    /// <see cref="SystemBase"/> implementation. When the system schedules or runs an IJobChunk job, it uses
    /// the specified <see cref="EntityQuery"/> to select a set of [chunks]. The entities in each chunk are examined to
    /// determine which have the necessary components enabled, according to the <see cref="EntityQuery"/> provided at
    /// schedule time. The job struct's `Execute` function is called for each chunk, along with a bitmask indicating
    /// which entities in the chunk should be processed.
    ///
    /// To pass data to your Execute function (beyond the `Execute` parameters), add public fields to the IJobChunk
    /// struct declaration and set those fields immediately before scheduling the job. You must always pass the
    /// component type information for any components that the job reads or writes using a field of type,
    /// <seealso cref="ComponentTypeHandle{T}"/>. Get this type information by calling the appropriate
    /// <seealso cref="ComponentSystemBase.GetComponentTypeHandle{T}"/> function for the type of
    /// component.
    ///
    /// [chunks]: xref:concepts-archetypes#archetype-chunks
    /// </remarks>
    /// <seealso cref="JobChunkExtensions"/>
    /// <seealso cref="IJobEntity"/>
    [JobProducerType(typeof(JobChunkExtensions.JobChunkProducer<>))]
    public interface IJobChunk
    {
        /// <summary>
        /// Implement the `Execute` function to perform a unit of work on an <see cref="ArchetypeChunk"/> representing
        /// a chunk.
        /// </summary>
        /// <remarks>
        /// The chunks selected by the <see cref="EntityQuery"/> used to schedule the job are the input to your `Execute`
        /// function. The `Execute` function is called once per matching chunk.
        ///
        /// Note that <paramref name="unfilteredChunkIndex"/> is not necessarily guaranteed to be a zero-based,
        /// tightly-packed index into the chunks the job actually runs on. For example, if the query matches 100 chunks,
        /// but the query's uses <see cref="EntityQuery.SetSharedComponentFilter{T}"/> and the first 50 chunks get
        /// filtered out, the <paramref name="unfilteredChunkIndex"/> will range from 50 to 99. If the index relative
        /// to the filtered chunk list is required, use <see cref="EntityQuery.CalculateFilteredChunkIndexArray"/></remarks>
        /// <param name="chunk">An object providing access to the entities within a chunk.</param>
        /// <param name="unfilteredChunkIndex">The index of the current chunk within the list of all chunks in all
        /// archetypes matched by the <see cref="EntityQuery"/> that the job was run against.</param>
        /// <param name="useEnabledMask">If true, the contents of <paramref name="chunkEnabledMask"/> describe which
        /// entities in the chunk match the provided <see cref="EntityQuery"/> and should be processed by this job.
        /// If false, all entities in the chunk match the provided query, and the contents of
        /// <paramref name="chunkEnabledMask"/> are undefined.</param>
        /// <param name="chunkEnabledMask">If bit N in this mask is set, entity N in <paramref name="chunk"/> matches
        /// the <see cref="EntityQuery"/> used to schedule the job. If bit N is clear, entity N does not match the query
        /// and can be skipped. If N is greater than or equal to the number of entities in the chunk, bit N will always
        /// be clear. If <paramref name="useEnabledMask"/> is false, all entities in the chunk match the query, and the
        /// contents of this mask are undefined.</param>
        void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask);
    }

    /// <summary>
    /// Extensions for scheduling and running <see cref="IJobChunk"/> jobs.
    /// </summary>
    public static class JobChunkExtensions
    {
        internal unsafe struct JobChunkWrapper<T> where T : struct
        {
#pragma warning disable 414
            [ReadOnly] public EntityQuerySafetyHandles safety;
#pragma warning restore 414
            // Only used for JobsUtility.PatchBufferMinMaxRanges; the user must also pass this array into the job struct
            // T if they need these indices inside their Execute() implementation. If null, this array will
            // be ignored.
            [NativeDisableUnsafePtrRestriction] [ReadOnly] public int* ChunkBaseEntityIndices;
            public T JobData;

            public UnsafeMatchingArchetypePtrList MatchingArchetypes;
            public UnsafeCachedChunkList CachedChunks;
            public EntityQueryFilter Filter;

            public int IsParallel;
            public int QueryHasEnableableComponents;
        }

        /// <summary>
        /// Gathers and caches reflection data for the internal job system's managed bindings. Unity is responsible for calling this method - don't call it yourself.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <remarks>
        /// When the Jobs package is included in the project, Unity generates code to call EarlyJobInit at startup. This allows Burst compiled code to schedule jobs because the reflection part of initialization, which is not compatible with burst compiler constraints, has already happened in EarlyJobInit.
        ///
        /// __Note__: While the Jobs package code generator handles this automatically for all closed job types, you must register those with generic arguments (like IJobChunk&amp;lt;MyJobType&amp;lt;T&amp;gt;&amp;gt;) manually for each specialization with [[Unity.Jobs.RegisterGenericJobTypeAttribute]].
        /// </remarks>
        public static void EarlyJobInit<T>()
            where T : struct, IJobChunk
        {
            JobChunkProducer<T>.Initialize();
        }

        /// <summary>
        /// Adds an <see cref="IJobChunk"/> instance to the job scheduler queue for sequential (non-parallel) execution.
        /// </summary>
        /// <param name="jobData">An <see cref="IJobChunk"/> instance.</param>
        /// <param name="query">The query selecting chunks with the necessary components.</param>
        /// <param name="dependsOn">The handle identifying already scheduled jobs that must complete before this job is executed.
        /// For example, a job that writes to a component cannot run in parallel with other jobs that read or write that component.
        /// Jobs that only read the same components can run in parallel.<br/>
        /// <br/>
        /// Most frequently, an appropriate value for this parameter is <see cref="SystemState.Dependency"/> to ensure
        /// that jobs registered with the safety system are taken into account as input dependencies.</param>
        /// <typeparam name="T">The specific <see cref="IJobChunk"/> implementation type.</typeparam>
        /// <returns>A handle that combines the current Job with previous dependencies identified by the <paramref name="dependsOn"/>
        /// parameter.</returns>
        public static unsafe JobHandle Schedule<T>(
            this T jobData,
            EntityQuery query,
            JobHandle dependsOn)
            where T : struct, IJobChunk
        {
            return ScheduleInternal(ref jobData, query, dependsOn, ScheduleMode.Single, default(NativeArray<int>));
        }

        /// <summary>
        /// Adds an <see cref="IJobChunk"/> instance to the job scheduler queue for sequential (non-parallel) execution.
        /// </summary>
        /// <param name="jobData">An <see cref="IJobChunk"/> instance. In this variant, the jobData is passed by
        /// reference, which may be necessary for unusually large job structs.</param>
        /// <param name="query">The query selecting chunks with the necessary components.</param>
        /// <param name="dependsOn">The handle identifying already scheduled jobs that must complete before this job is executed.
        /// For example, a job that writes to a component cannot run in parallel with other jobs that read or write that component.
        /// Jobs that only read the same components can run in parallel.<br/>
        /// <br/>
        /// Most frequently, an appropriate value for this parameter is <see cref="SystemState.Dependency"/> to ensure
        /// that jobs registered with the safety system are taken into account as input dependencies.</param>
        /// <typeparam name="T">The specific <see cref="IJobChunk"/> implementation type.</typeparam>
        /// <returns>A handle that combines the current Job with previous dependencies identified by the <paramref name="dependsOn"/>
        /// parameter.</returns>
        public static unsafe JobHandle ScheduleByRef<T>(
            this ref T jobData,
            EntityQuery query,
            JobHandle dependsOn)
            where T : struct, IJobChunk
        {
            return ScheduleInternal(ref jobData, query, dependsOn, ScheduleMode.Single, default(NativeArray<int>));
        }

        /// <summary>
        /// Adds an <see cref="IJobChunk"/> instance to the job scheduler queue for parallel execution.
        /// </summary>
        /// <param name="jobData">An <see cref="IJobChunk"/> instance.</param>
        /// <param name="query">The query selecting chunks with the necessary components.</param>
        /// <param name="dependsOn">The handle identifying already scheduled jobs that must complete before this job is executed.
        /// For example, a job that writes to a component cannot run in parallel with other jobs that read or write that component.
        /// Jobs that only read the same components can run in parallel.<br/>
        /// <br/>
        /// Most frequently, an appropriate value for this parameter is <see cref="SystemState.Dependency"/> to ensure
        /// that jobs registered with the safety system are taken into account as input dependencies.</param>
        /// <typeparam name="T">The specific <see cref="IJobChunk"/> implementation type.</typeparam>
        /// <returns>A handle that combines the current Job with previous dependencies identified by the <paramref name="dependsOn"/>
        /// parameter.</returns>
        public static unsafe JobHandle ScheduleParallel<T>(
            this T jobData,
            EntityQuery query,
            JobHandle dependsOn)
            where T : struct, IJobChunk
        {
            return ScheduleInternal(ref jobData, query, dependsOn, ScheduleMode.Parallel, default(NativeArray<int>));
        }

        /// <summary>
        /// Adds an <see cref="IJobChunk"/> instance to the job scheduler queue for parallel execution.
        /// </summary>
        /// <param name="jobData">An <see cref="IJobChunk"/> instance. In this variant, the jobData is passed by
        /// reference, which may be necessary for unusually large job structs.</param>
        /// <param name="query">The query selecting chunks with the necessary components.</param>
        /// <param name="dependsOn">The handle identifying already scheduled jobs that must complete before this job is executed.
        /// For example, a job that writes to a component cannot run in parallel with other jobs that read or write that component.
        /// Jobs that only read the same components can run in parallel.<br/>
        /// <br/>
        /// Most frequently, an appropriate value for this parameter is <see cref="SystemState.Dependency"/> to ensure
        /// that jobs registered with the safety system are taken into account as input dependencies.</param>
        /// <typeparam name="T">The specific <see cref="IJobChunk"/> implementation type.</typeparam>
        /// <returns>A handle that combines the current Job with previous dependencies identified by the <paramref name="dependsOn"/>
        /// parameter.</returns>
        public static unsafe JobHandle ScheduleParallelByRef<T>(
            this ref T jobData,
            EntityQuery query,
            JobHandle dependsOn)
            where T : struct, IJobChunk
        {
            return ScheduleInternal(ref jobData, query, dependsOn, ScheduleMode.Parallel, default(NativeArray<int>));
        }

        /// <summary>
        /// Runs the job immediately on the current thread.
        /// </summary>
        /// <param name="jobData">An <see cref="IJobChunk"/> instance.</param>
        /// <param name="query">The query selecting chunks with the necessary components.</param>
        /// <typeparam name="T">The specific <see cref="IJobChunk"/> implementation type.</typeparam>
        public static unsafe void Run<T>(this T jobData, EntityQuery query)
            where T : struct, IJobChunk
        {
            ScheduleInternal(ref jobData, query, default(JobHandle), ScheduleMode.Run, default(NativeArray<int>));
        }

        /// <summary>
        /// Runs the job immediately on the current thread.
        /// </summary>
        /// <param name="jobData">An <see cref="IJobChunk"/> instance. In this variant, the jobData is passed by
        /// reference, which may be necessary for unusually large job structs.</param>
        /// <param name="query">The query selecting chunks with the necessary components.</param>
        /// <typeparam name="T">The specific <see cref="IJobChunk"/> implementation type.</typeparam>
        public static unsafe void RunByRef<T>(this ref T jobData, EntityQuery query)
            where T : struct, IJobChunk
        {
            ScheduleInternal(ref jobData, query, default(JobHandle), ScheduleMode.Run, default(NativeArray<int>));
        }

        internal static unsafe void RunByRefWithoutJobs<T>(this ref T jobData, EntityQuery query)
            where T : struct, IJobChunk
        {
            var queryImpl = query._GetImpl();
            if (queryImpl->_Access->IsInExclusiveTransaction)
                throw new InvalidOperationException("You can't schedule an IJobChunk while an exclusive transaction is active");
            queryImpl->_Access->DependencyManager->ForEachStructuralChange.BeginIsInForEach(queryImpl);
            if (query.HasFilter() || queryImpl->_QueryData->HasEnableableComponents != 0)
            {
                // Complete any running jobs that would affect which chunks/entities match the query.
                // This sync may not be strictly necessary, if the caller doesn't care about filtering the query results.
                // But if they DO care, and they forget this sync, they'll have an undetected race condition. So, let's play it safe.
                queryImpl->SyncFilterTypes();

                var chunkCacheIterator = new UnsafeChunkCacheIterator(queryImpl->_Filter, queryImpl->_QueryData->HasEnableableComponents != 0,
                    queryImpl->GetMatchingChunkCache(), queryImpl->_QueryData->MatchingArchetypes.Ptr);

                int chunkIndex = -1;
                v128 chunkEnabledMask = default;
                while (chunkCacheIterator.MoveNextChunk(ref chunkIndex, out var archetypeChunk, out _,
                           out byte useEnabledMask, ref chunkEnabledMask))
                {
                    jobData.Execute(archetypeChunk, chunkIndex, useEnabledMask != 0, chunkEnabledMask);
                }
            }
            else
            {
                // Fast path for queries with no filtering and no enableable components
                var cachedChunkList = queryImpl->GetMatchingChunkCache();
                var chunkIndices = cachedChunkList.ChunkIndices;
                int chunkCount = cachedChunkList.Length;
                ArchetypeChunk chunk = new ArchetypeChunk(ChunkIndex.Null, cachedChunkList.EntityComponentStore);
                v128 defaultMask = default;
                for (int chunkIndex = 0; chunkIndex < chunkCount; ++chunkIndex)
                {
                    chunk.m_Chunk = chunkIndices[chunkIndex];
                    Assert.AreNotEqual(0, chunk.Count);
                    jobData.Execute(chunk, chunkIndex, false, defaultMask);
                }
            }
            queryImpl->_Access->DependencyManager->ForEachStructuralChange.EndIsInForEach();
        }

        internal static unsafe JobHandle ScheduleInternal<T>(
            ref T jobData,
            EntityQuery query,
            JobHandle dependsOn,
            ScheduleMode mode,
            NativeArray<int> chunkBaseEntityIndices)
            where T : struct, IJobChunk
        {
            var queryImpl = query._GetImpl();
            if (queryImpl->_Access->IsInExclusiveTransaction)
                throw new InvalidOperationException("You can't schedule an IJobChunk while an exclusive transaction is active");
            var queryData = queryImpl->_QueryData;
            var cachedChunks = queryImpl->GetMatchingChunkCache();
            var totalChunkCount = cachedChunks.Length;
            bool isParallel = (mode == ScheduleMode.Parallel);

            JobChunkWrapper<T> jobChunkWrapper = new JobChunkWrapper<T>
            {
                safety = new EntityQuerySafetyHandles(queryImpl),
                // If this array exists, it's likely still being written to by a job, so we need to bypass the safety system to get its buffer pointer
                ChunkBaseEntityIndices = (isParallel && chunkBaseEntityIndices.Length > 0)
                    ? (int*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(chunkBaseEntityIndices)
                    : null,
                MatchingArchetypes = queryData->MatchingArchetypes,
                CachedChunks = cachedChunks,
                Filter = queryImpl->_Filter,

                JobData = jobData,
                IsParallel = isParallel ? 1 : 0,

                QueryHasEnableableComponents = queryData->HasEnableableComponents != 0 ? 1 : 0
            };
            JobChunkProducer<T>.Initialize();
            var reflectionData = JobChunkProducer<T>.reflectionData.Data;
            CollectionHelper.CheckReflectionDataCorrect<T>(reflectionData);

            var scheduleParams = new JobsUtility.JobScheduleParameters(
                UnsafeUtility.AddressOf(ref jobChunkWrapper),
                reflectionData,
                dependsOn,
                mode);

            var result = default(JobHandle);
            if (!isParallel)
            {
                result = JobsUtility.Schedule(ref scheduleParams);
            }
            else
            {
                // TODO(DOTS-5740): pick a better innerloopBatchCount
                result = JobsUtility.ScheduleParallelFor(ref scheduleParams, totalChunkCount, 1);
            }

            return result;
        }

        internal struct JobChunkProducer<T>
            where T : struct, IJobChunk
        {
            internal static readonly SharedStatic<IntPtr> reflectionData = SharedStatic<IntPtr>.GetOrCreate<JobChunkProducer<T>>();

            [BurstDiscard]
            internal static void Initialize()
            {
                if (reflectionData.Data == IntPtr.Zero)
                    reflectionData.Data = JobsUtility.CreateJobReflectionData(typeof(JobChunkWrapper<T>), typeof(T), (ExecuteJobFunction)Execute);
            }

            public delegate void ExecuteJobFunction(
                ref JobChunkWrapper<T> jobWrapper,
                IntPtr additionalPtr,
                IntPtr bufferRangePatchData,
                ref JobRanges ranges,
                int jobIndex);

            public static void Execute(
                ref JobChunkWrapper<T> jobWrapper,
                IntPtr additionalPtr,
                IntPtr bufferRangePatchData,
                ref JobRanges ranges,
                int jobIndex)
            {
                ExecuteInternal(ref jobWrapper, bufferRangePatchData, ref ranges, jobIndex);
            }

            internal unsafe static void ExecuteInternal(
                ref JobChunkWrapper<T> jobWrapper,
                IntPtr bufferRangePatchData,
                ref JobRanges ranges,
                int jobIndex)
            {
                var chunks = jobWrapper.CachedChunks;
                var chunkCacheIterator = new UnsafeChunkCacheIterator(jobWrapper.Filter,
                    jobWrapper.QueryHasEnableableComponents != 0,
                    jobWrapper.CachedChunks, jobWrapper.MatchingArchetypes.Ptr);

                bool isParallel = jobWrapper.IsParallel == 1;
                bool isFiltering = jobWrapper.Filter.RequiresMatchesFilter;
                while (true)
                {
                    int beginChunkIndex = 0;
                    int endChunkIndex = chunks.Length;

                    // If we are running the job in parallel, steal some work.
                    if (isParallel)
                    {
                        // If we have no range to steal, exit the loop.
                        if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out beginChunkIndex, out endChunkIndex))
                            break;
                    }

                    // Do the actual user work.
                    if (jobWrapper.QueryHasEnableableComponents == 0 && !isFiltering)
                    {
                        // Fast path with no entity/chunk filtering active: we can just iterate over the cached chunk list directly.
                        var chunkIndices = chunks.ChunkIndices;
                        ArchetypeChunk chunk = new ArchetypeChunk(ChunkIndex.Null, chunks.EntityComponentStore);
                        v128 defaultMask = default;
                        for (int chunkIndex = beginChunkIndex; chunkIndex < endChunkIndex; ++chunkIndex)
                        {
                            chunk.m_Chunk = chunkIndices[chunkIndex];
                            Assert.AreNotEqual(0, chunk.Count);
                            // For containers passed in the job struct, parallel jobs can limit writes to a range of indices.
                            // By default, writes are limited to the element corresponding to the unfiltered chunk index.
                            // If the user has provided a ChunkBaseEntityIndices array, use that instead (limiting writes to the range of entities within the chunk).
                            // This range check can be disabled on a per-container basis by adding [NativeDisableParallelForRestriction] to the container field in the job struct.
                            if (Hint.Unlikely(jobWrapper.ChunkBaseEntityIndices != null))
                            {
                                int chunkBaseEntityIndex = jobWrapper.ChunkBaseEntityIndices[chunkIndex];
                                JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData,
                                    UnsafeUtility.AddressOf(ref jobWrapper), chunkBaseEntityIndex, chunk.Count);
                            }
                            else if (isParallel)
                            {
                                JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData,
                                    UnsafeUtility.AddressOf(ref jobWrapper), chunkIndex, 1);
                            }
                            jobWrapper.JobData.Execute(chunk, chunkIndex, false, defaultMask);
                        }
                    }
                    else
                    {
                        // With any filtering active, we need to iterate using the UnsafeChunkCacheIterator.
                        // Update cache range
                        chunkCacheIterator.Length = endChunkIndex;
                        int chunkIndex = beginChunkIndex - 1;

                        v128 chunkEnabledMask = default;
                        while (chunkCacheIterator.MoveNextChunk(ref chunkIndex, out var chunk, out var chunkEntityCount,
                                   out byte useEnabledMask, ref chunkEnabledMask))
                        {
                            // For containers passed in the job struct, parallel jobs can limit writes to a range of indices.
                            // By default, writes are limited to the element corresponding to the unfiltered chunk index.
                            // If the user has provided a ChunkBaseEntityIndices array, use that instead (limiting writes to the range of entities within the chunk).
                            // This range check can be disabled on a per-container basis by adding [NativeDisableParallelForRestriction] to the container field in the job struct.
                            if (Hint.Unlikely(jobWrapper.ChunkBaseEntityIndices != null))
                            {
                                int chunkBaseEntityIndex = jobWrapper.ChunkBaseEntityIndices[chunkIndex];
                                JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData,
                                    UnsafeUtility.AddressOf(ref jobWrapper), chunkBaseEntityIndex, chunk.Count);
                            }
                            else if (isParallel)
                            {
                                JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData,
                                    UnsafeUtility.AddressOf(ref jobWrapper), chunkIndex, 1);
                            }
                            jobWrapper.JobData.Execute(chunk, chunkIndex, useEnabledMask != 0,
                                chunkEnabledMask);
                        }
                    }

                    // If we are not running in parallel, our job is done.
                    if (!isParallel)
                        break;
                }
            }
        }
    }
}
