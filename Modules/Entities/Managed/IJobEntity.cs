// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// Please refer to the README.md document in the IJobEntity example in the Samples project for more information.

using System;
using Unity.Burst.Intrinsics;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine;

namespace Unity.Entities
{
    /// <summary>
    /// Interface for a job type which runs code on each entity that matches an <see cref="EntityQuery"/>.
    /// </summary>
    /// <remarks>
    /// Any type which implements this interface and also contains an `Execute()` method (with any number of parameters)
    /// will trigger source generation of a corresponding <see cref="IJobChunk"/>. The generated job in turn
    /// invokes the Execute() method on the IJobEntity type with the appropriate arguments.
    ///
    /// IJobChunk offers more flexibility for job authors, and may have slightly lower performance overhead. However,
    /// for simple jobs involving straightforward per-entity computation, IJobEntity can express the same operation
    /// with significantly less boilerplate code.
    /// </remarks>
    /// <example>
    /// <code source="../DocCodeSamples.Tests/JobEntityExamples.cs" region="SimpleSample" title="IJobEntity Example" language="csharp"/>
    /// </example>
    /// <seealso cref="IJobChunk"/>
    public interface IJobEntity {}

    /// <summary>
    /// When added to an implemented <see cref="IJobEntity"/> the two functions
    /// will be called at the beginning and end of each chunk iteration.
    /// </summary>
    public interface IJobEntityChunkBeginEnd
    {
        /// <summary>
        /// Called at the beginning of every chunk iteration in the <see cref="IJobEntity"/>.
        /// It also tells whether or not to run `Execute` on the current chunk/>.
        /// </summary>
        /// <remarks>
        /// Note that the <paramref name="chunkEnabledMask"/> value passed to this function is also used to determine
        /// which entities in the current chunk should be processed. Since the mask has already been computed, any components
        /// which are enabled or disabled in this chunk during OnChunkBegin() will not affect which of the chunk's entities
        /// are processed. To enable/disable components during a chunk pre-pass, it's necessary to use <see cref="IJobChunk"/>
        /// to take full control of the chunk iteration logic.
        /// </remarks>
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
        /// <returns>True if chunk should be executed.</returns>
        bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask);

        /// <summary>
        /// Called at the end of every chunk iteration in the <see cref="IJobEntity"/>.
        /// Will still be called even if <see cref="OnChunkBegin"/> returned false.
        /// You can handle this case by checking <paramref name="chunkWasExecuted"/>.
        /// </summary>
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
        /// <param name="chunkWasExecuted">true if <see cref="OnChunkBegin"/> returned true</param>
        void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask, bool chunkWasExecuted);
    }

    /// <summary>Obsolete. Use <see cref="EntityIndexInQuery"/> instead.</summary>
    /// <remarks>**Obsolete.** Use <see cref="EntityIndexInQuery"/> instead.
    ///
    /// Specifies that this int parameter is used as a way to get the packed entity index inside the current query.
    /// Usage: An int parameter found inside the execute method of an IJobEntity.
    ///
    /// This is generally way more expensive than <see cref="ChunkIndexInQuery"/> and <see cref="EntityIndexInChunk"/>.
    /// As it it will schedule a <see cref="EntityQuery.CalculateBaseEntityIndexArrayAsync"/> job to get an offset buffer.
    /// If you just want a sortkey for your <see cref="EntityCommandBuffer.ParallelWriter"/> simply use <see cref="ChunkIndexInQuery"/>
    /// as it is different for every thread, which is all a ParallelWriter needs to sort with.
    /// </remarks>
    /// <seealso cref="IJobEntity"/>
    [AttributeUsage(AttributeTargets.Parameter)]
    [Obsolete("Use EntityIndexInQuery (Removed after Entities 1.0) (UnityUpgradable) -> EntityIndexInQuery", true)]
    public sealed class EntityInQueryIndex : Attribute {}

    /// <summary>
    /// Specifies that this int parameter is used as a way to get the packed entity index inside the current query.
    /// Usage: An int parameter found inside the execute method of an IJobEntity.
    /// </summary>
    /// <remarks>
    /// This is generally way more expensive than <see cref="ChunkIndexInQuery"/> and <see cref="EntityIndexInChunk"/>.
    /// As it it will schedule a <see cref="EntityQuery.CalculateBaseEntityIndexArrayAsync"/> job to get an offset buffer.
    /// If you just want a sortkey for your <see cref="EntityCommandBuffer.ParallelWriter"/> simply use <see cref="ChunkIndexInQuery"/>
    /// as it is different for every thread, which is all a ParallelWriter needs to sort with.
    /// </remarks>
    /// <seealso cref="IJobEntity"/>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class EntityIndexInQuery : Attribute {}

    /// <summary>
    /// Specifies that this int parameter is used as the entity index inside the current chunk.
    /// Usage: An int parameter found inside the execute method of an IJobEntity.
    /// </summary>
    /// <seealso cref="IJobEntity"/>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class EntityIndexInChunk : Attribute {}

    /// <summary>
    /// Specifies that this int parameter is used as the chunk index inside the current query.
    /// Usage: An int parameter found inside the execute method of an IJobEntity.
    /// </summary>
    /// <remarks>
    /// Efficient sort-key for <see cref="EntityCommandBuffer"/>.
    /// </remarks>
    /// <seealso cref="IJobEntity"/>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class ChunkIndexInQuery : Attribute {}

    /// <summary>
    /// Specify that an IJobEntity should not include any of the component types specified as part of the attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
    public sealed class WithAbsentAttribute : Attribute
    {
        /// <summary>
        /// Specifies that this IJobEntity should not include any of the component types specified as part of the attribute.
        /// </summary>
        /// <param name="types">Absent component types</param>
        public WithAbsentAttribute(params Type[] types){}
    }

    /// <summary>
    /// Specify that an IJobEntity should include all of the component types specified as part of the attribute, but they must all be disabled.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
    public sealed class WithDisabledAttribute : Attribute
    {
        /// <summary>
        /// Specifies that this IJobEntity should include all of the component types specified as part of the attribute, but they must all be disabled.
        /// </summary>
        /// <param name="types">The required disabled component types</param>
        public WithDisabledAttribute(params Type[] types){}
    }

    /// <summary>
    /// Specify that an IJobEntity should include all of the component types specified as part of the attribute, whether or not the components
    /// are enabled on individual entities.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
    public sealed class WithPresentAttribute : Attribute
    {
        /// <summary>
        /// Specifies that this IJobEntity should include all of the component types specified as part of the attribute, whether or not the components
        /// are enabled on individual entities.
        /// </summary>
        /// <param name="types">Present component types</param>
        public WithPresentAttribute(params Type[] types){}
    }

    /// <summary>
    /// Specifies that this IJobEntity should include all ComponentTypes found as attributes of the IJobEntity
    /// </summary>
    /// <seealso cref="IJobEntity"/>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
    public sealed class WithAllAttribute : Attribute
    {
        /// <summary>
        /// Specifies that this IJobEntity should include all ComponentTypes found as attributes of the IJobEntity
        /// </summary>
        /// <param name="types">The required component types</param>
        public WithAllAttribute(params Type[] types){}
    }

    /// <summary>
    /// Specifies that this IJobEntity should include no ComponentTypes found as attributes of the IJobEntity
    /// </summary>
    /// <seealso cref="IJobEntity"/>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
    public sealed class WithNoneAttribute : Attribute
    {
        /// <summary>
        /// Specifies that this IJobEntity should include no ComponentTypes found as attributes of the IJobEntity
        /// </summary>
        /// <param name="types">The excluded component types</param>
        public WithNoneAttribute(params Type[] types){}
    }

    /// <summary>
    /// Specifies that this IJobEntity should include any ComponentTypes found as attributes of the IJobEntity
    /// </summary>
    /// <seealso cref="IJobEntity"/>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
    public sealed class WithAnyAttribute : Attribute
    {
        /// <summary>
        /// Specifies that this IJobEntity should include any ComponentTypes found as attributes of the IJobEntity
        /// </summary>
        /// <param name="types">The optional component types</param>
        public WithAnyAttribute(params Type[] types){}
    }

    /// <summary>
    /// Specifies that this IJobEntity should only process a chunk if any of the ComponentTypes found as attributes of the IJobEntity have changed.
    /// </summary>
    /// <seealso cref="IJobEntity"/>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Struct, AllowMultiple = true)]
    public sealed class WithChangeFilterAttribute : Attribute
    {
        /// <summary>
        /// Specifies that this IJobEntity should only process a chunk if any of the ComponentTypes found as attributes of the IJobEntity have changed.
        /// </summary>
        /// <param name="types">The component types for which change filtering should be enabled</param>
        public WithChangeFilterAttribute(params Type[] types){}
    }

    /// <summary>
    /// Obsolete. Use <see cref="WithOptionsAttribute"/> instead.
    /// </summary>
    /// <remarks>**Obsolete.** Use <see cref="WithOptionsAttribute"/> instead.
    ///
    /// Specifies that this IJobEntity should include a given EntityQueryOption found as attributes of the IJobEntity</remarks>
    /// <seealso cref="IJobEntity"/>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
    [Obsolete("This type has been renamed to WithOptions. (Removed after Entities 1.0) (UnityUpgradable) -> WithOptionsAttribute", true)]
    public sealed class WithEntityQueryOptionsAttribute : Attribute
    {
        /// <summary>
        /// Specifies that this IJobEntity should include a given EntityQueryOption found as attributes of the IJobEntity
        /// </summary>
        /// <param name="option">The query options</param>
        public WithEntityQueryOptionsAttribute(EntityQueryOptions option){}
        /// <summary>
        /// Specifies that this IJobEntity should include a given EntityQueryOption found as attributes of the IJobEntity
        /// </summary>
        /// <param name="options">The query options</param>
        public WithEntityQueryOptionsAttribute(params EntityQueryOptions[] options){}
    }

    /// <summary>
    /// Specifies that this IJobEntity should include a given EntityQueryOption found as attributes of the IJobEntity
    /// </summary>
    /// <seealso cref="IJobEntity"/>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
    public sealed class WithOptionsAttribute : Attribute
    {
        /// <summary>
        /// Specifies that this IJobEntity should include a given EntityQueryOption found as attributes of the IJobEntity
        /// </summary>
        /// <param name="option">The query options</param>
        public WithOptionsAttribute(EntityQueryOptions option){}
        /// <summary>
        /// Specifies that this IJobEntity should include a given EntityQueryOption found as attributes of the IJobEntity
        /// </summary>
        /// <param name="options">The query options</param>
        public WithOptionsAttribute(params EntityQueryOptions[] options){}
    }

    /// <summary>
    /// Extension methods for IJobEntity job type
    /// </summary>
    public static class IJobEntityExtensions
    {
        // Mirrors all of the schedule methods for IJobChunk,
        // except we must also have a version that takes no query as IJobEntity can generate the query for you
        // and there are also one's that don't take a JobHandle so that the built-in Dependency properties in systems
        // can be used.
        // These methods must all be replicated in the generated job struct to prevent compiler ambiguity
        // These methods keep the full type names so that it can be easily copy pasted into JobEntityDescriptionSourceFactor.cs when updated

        #region Schedule

        /// <summary>
        /// Adds an <see cref="IJobEntity"/> instance to the job scheduler queue for sequential (non-parallel) execution.
        /// </summary>
        /// <param name="jobData">An <see cref="IJobEntity"/> instance.</param>
        /// <param name="dependsOn">The handle identifying already scheduled jobs that could constrain this job.
        /// A job that writes to a component cannot run in parallel with other jobs that read or write that component.
        /// Jobs that only read the same components can run in parallel.</param>
        /// <typeparam name="T">The specific <see cref="IJobEntity"/> implementation type.</typeparam>
        /// <returns>A handle that combines the current Job with previous dependencies identified by the `dependsOn`
        /// parameter.</returns>
        public static JobHandle Schedule<T>(this T jobData, JobHandle dependsOn)
            where T : unmanaged, IJobEntity  => throw InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Adds an <see cref="IJobEntity"/> instance to the job scheduler queue for sequential (non-parallel) execution.
        /// </summary>
        /// <param name="jobData">An <see cref="IJobEntity"/> instance. In this variant, the jobData is passed by
        /// reference, which may be necessary for unusually large job structs.</param>
        /// <param name="dependsOn">The handle identifying already scheduled jobs that could constrain this job.
        /// A job that writes to a component cannot run in parallel with other jobs that read or write that component.
        /// Jobs that only read the same components can run in parallel.</param>
        /// <typeparam name="T">The specific <see cref="IJobEntity"/> implementation type.</typeparam>
        /// <returns>A handle that combines the current Job with previous dependencies identified by the `dependsOn`
        /// parameter.</returns>
        public static JobHandle ScheduleByRef<T>(this ref T jobData, JobHandle dependsOn)
            where T : unmanaged, IJobEntity  => throw InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Adds an <see cref="IJobEntity"/> instance to the job scheduler queue for sequential (non-parallel) execution.
        /// </summary>
        /// <param name="jobData">An <see cref="IJobEntity"/> instance.</param>
        /// <param name="query">The query selecting chunks with the necessary components.</param>
        /// <param name="dependsOn">The handle identifying already scheduled jobs that could constrain this job.
        /// A job that writes to a component cannot run in parallel with other jobs that read or write that component.
        /// Jobs that only read the same components can run in parallel.</param>
        /// <typeparam name="T">The specific <see cref="IJobEntity"/> implementation type.</typeparam>
        /// <returns>A handle that combines the current Job with previous dependencies identified by the `dependsOn`
        /// parameter.</returns>
        public static JobHandle Schedule<T>(this T jobData, EntityQuery query, JobHandle dependsOn)
            where T : unmanaged, IJobEntity  => throw InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Adds an <see cref="IJobEntity"/> instance to the job scheduler queue for sequential (non-parallel) execution.
        /// </summary>
        /// <param name="jobData">An <see cref="IJobEntity"/> instance. In this variant, the jobData is passed by
        /// reference, which may be necessary for unusually large job structs.</param>
        /// <param name="query">The query selecting chunks with the necessary components.</param>
        /// <param name="dependsOn">The handle identifying already scheduled jobs that could constrain this job.
        /// A job that writes to a component cannot run in parallel with other jobs that read or write that component.
        /// Jobs that only read the same components can run in parallel.</param>
        /// <typeparam name="T">The specific <see cref="IJobEntity"/> implementation type.</typeparam>
        /// <returns>A handle that combines the current Job with previous dependencies identified by the `dependsOn`
        /// parameter.</returns>
        public static JobHandle ScheduleByRef<T>(this ref T jobData, EntityQuery query, JobHandle dependsOn)
            where T : unmanaged, IJobEntity  => throw InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Adds an <see cref="IJobEntity"/> instance to the job scheduler queue for sequential (non-parallel) execution.
        /// </summary>
        /// <param name="jobData">An <see cref="IJobEntity"/> instance.</param>
        /// <typeparam name="T">The specific <see cref="IJobEntity"/> implementation type.</typeparam>
        /// <remarks>This job automatically uses the system's Dependency property as the input and output dependency.</remarks>
        public static void Schedule<T>(this T jobData)
            where T : unmanaged, IJobEntity  => throw InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Adds an <see cref="IJobEntity"/> instance to the job scheduler queue for sequential (non-parallel) execution.
        /// </summary>
        /// <param name="jobData">An <see cref="IJobEntity"/> instance. In this variant, the jobData is passed by
        /// reference, which may be necessary for unusually large job structs.</param>
        /// <typeparam name="T">The specific <see cref="IJobEntity"/> implementation type.</typeparam>
        /// <remarks>This job automatically uses the system's Dependency property as the input and output dependency.</remarks>
        public static void ScheduleByRef<T>(this ref T jobData)
            where T : unmanaged, IJobEntity  => throw InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Adds an <see cref="IJobEntity"/> instance to the job scheduler queue for sequential (non-parallel) execution.
        /// </summary>
        /// <param name="jobData">An <see cref="IJobEntity"/> instance.</param>
        /// <param name="query">The query selecting chunks with the necessary components.</param>
        /// <typeparam name="T">The specific <see cref="IJobEntity"/> implementation type.</typeparam>
        /// <remarks>This job automatically uses the system's Dependency property as the input and output dependency.</remarks>
        public static void Schedule<T>(this T jobData, EntityQuery query)
            where T : unmanaged, IJobEntity  => throw InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Adds an <see cref="IJobEntity"/> instance to the job scheduler queue for sequential (non-parallel) execution.
        /// </summary>
        /// <param name="jobData">An <see cref="IJobEntity"/> instance. In this variant, the jobData is passed by
        /// reference, which may be necessary for unusually large job structs.</param>
        /// <param name="query">The query selecting chunks with the necessary components.</param>
        /// <typeparam name="T">The specific <see cref="IJobEntity"/> implementation type.</typeparam>
        /// <remarks>This job automatically uses the system's Dependency property as the input and output dependency.</remarks>
        public static void ScheduleByRef<T>(this ref T jobData, EntityQuery query)
            where T : unmanaged, IJobEntity  => throw InternalCompilerInterface.ThrowCodeGenException();

        #endregion

        #region ScheduleParallel

        /// <summary>
        /// Adds an <see cref="IJobEntity"/> instance to the job scheduler queue for parallel execution.
        /// </summary>
        /// <param name="jobData">An <see cref="IJobEntity"/> instance.</param>
        /// <param name="dependsOn">The handle identifying already scheduled jobs that could constrain this job.
        /// A job that writes to a component cannot run in parallel with other jobs that read or write that component.
        /// Jobs that only read the same components can run in parallel.</param>
        /// <typeparam name="T">The specific <see cref="IJobEntity"/> implementation type.</typeparam>
        /// <returns>A handle that combines the current Job with previous dependencies identified by the `dependsOn`
        /// parameter.</returns>
        public static JobHandle ScheduleParallel<T>(this T jobData, JobHandle dependsOn)
            where T : unmanaged, IJobEntity  => throw InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Adds an <see cref="IJobEntity"/> instance to the job scheduler queue for parallel execution.
        /// </summary>
        /// <param name="jobData">An <see cref="IJobEntity"/> instance. In this variant, the jobData is passed by
        /// reference, which may be necessary for unusually large job structs.</param>
        /// <param name="dependsOn">The handle identifying already scheduled jobs that could constrain this job.
        /// A job that writes to a component cannot run in parallel with other jobs that read or write that component.
        /// Jobs that only read the same components can run in parallel.</param>
        /// <typeparam name="T">The specific <see cref="IJobEntity"/> implementation type.</typeparam>
        /// <returns>A handle that combines the current Job with previous dependencies identified by the `dependsOn`
        /// parameter.</returns>
        public static JobHandle ScheduleParallelByRef<T>(this ref T jobData, JobHandle dependsOn)
            where T : unmanaged, IJobEntity  => throw InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Adds an <see cref="IJobEntity"/> instance to the job scheduler queue for parallel execution.
        /// </summary>
        /// <param name="jobData">An <see cref="IJobEntity"/> instance.</param>
        /// <param name="query">The query selecting chunks with the necessary components.</param>
        /// <param name="dependsOn">The handle identifying already scheduled jobs that could constrain this job.
        /// A job that writes to a component cannot run in parallel with other jobs that read or write that component.
        /// Jobs that only read the same components can run in parallel.</param>
        /// <typeparam name="T">The specific <see cref="IJobEntity"/> implementation type.</typeparam>
        /// <returns>A handle that combines the current Job with previous dependencies identified by the `dependsOn`
        /// parameter.</returns>
        public static JobHandle ScheduleParallel<T>(this T jobData, EntityQuery query, JobHandle dependsOn)
            where T : unmanaged, IJobEntity  => throw InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Adds an <see cref="IJobEntity"/> instance to the job scheduler queue for parallel execution.
        /// </summary>
        /// <param name="jobData">An <see cref="IJobEntity"/> instance. In this variant, the jobData is passed by
        /// reference, which may be necessary for unusually large job structs.</param>
        /// <param name="query">The query selecting chunks with the necessary components.</param>
        /// <param name="dependsOn">The handle identifying already scheduled jobs that could constrain this job.
        /// A job that writes to a component cannot run in parallel with other jobs that read or write that component.
        /// Jobs that only read the same components can run in parallel.</param>
        /// <typeparam name="T">The specific <see cref="IJobEntity"/> implementation type.</typeparam>
        /// <returns>A handle that combines the current Job with previous dependencies identified by the `dependsOn`
        /// parameter.</returns>
        public static JobHandle ScheduleParallelByRef<T>(this ref T jobData, EntityQuery query, JobHandle dependsOn)
            where T : unmanaged, IJobEntity  => throw InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Adds an <see cref="IJobEntity"/> instance to the job scheduler queue for parallel execution.
        /// </summary>
        /// <param name="jobData">An <see cref="IJobEntity"/> instance.</param>
        /// <typeparam name="T">The specific <see cref="IJobEntity"/> implementation type.</typeparam>
        /// <remarks>This job automatically uses the system's Dependency property as the input and output dependency.</remarks>
        public static void ScheduleParallel<T>(this T jobData)
            where T : unmanaged, IJobEntity  => throw InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Adds an <see cref="IJobEntity"/> instance to the job scheduler queue for parallel execution.
        /// </summary>
        /// <param name="jobData">An <see cref="IJobEntity"/> instance. In this variant, the jobData is passed by
        /// reference, which may be necessary for unusually large job structs.</param>
        /// <typeparam name="T">The specific <see cref="IJobEntity"/> implementation type.</typeparam>
        /// <remarks>This job automatically uses the system's Dependency property as the input and output dependency.</remarks>
        public static void ScheduleParallelByRef<T>(this ref T jobData)
            where T : unmanaged, IJobEntity  => throw InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Adds an <see cref="IJobEntity"/> instance to the job scheduler queue for parallel execution.
        /// </summary>
        /// <param name="jobData">An <see cref="IJobEntity"/> instance.</param>
        /// <param name="query">The query selecting chunks with the necessary components.</param>
        /// <typeparam name="T">The specific <see cref="IJobEntity"/> implementation type.</typeparam>
        /// <remarks>This job automatically uses the system's Dependency property as the input and output dependency.</remarks>
        public static void ScheduleParallel<T>(this T jobData, EntityQuery query)
            where T : unmanaged, IJobEntity  => throw InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Adds an <see cref="IJobEntity"/> instance to the job scheduler queue for parallel execution.
        /// </summary>
        /// <param name="jobData">An <see cref="IJobEntity"/> instance. In this variant, the jobData is passed by
        /// reference, which may be necessary for unusually large job structs.</param>
        /// <param name="query">The query selecting chunks with the necessary components.</param>
        /// <typeparam name="T">The specific <see cref="IJobEntity"/> implementation type.</typeparam>
        /// <remarks>This job automatically uses the system's Dependency property as the input and output dependency.</remarks>
        public static void ScheduleParallelByRef<T>(this ref T jobData, EntityQuery query)
            where T : unmanaged, IJobEntity  => throw InternalCompilerInterface.ThrowCodeGenException();

        #endregion

        #region Run

        /// <summary>
        /// Runs the job immediately on the current thread.
        /// </summary>
        /// <param name="jobData">An <see cref="IJobEntity"/> instance.</param>
        /// <typeparam name="T">The specific <see cref="IJobEntity"/> implementation type.</typeparam>
        public static void Run<T>(this T jobData)
            where T : struct, IJobEntity  => throw InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Runs the job immediately on the current thread.
        /// </summary>
        /// <param name="jobData">An <see cref="IJobEntity"/> instance. In this variant, the jobData is passed by
        /// reference, which may be necessary for unusually large job structs.</param>
        /// <typeparam name="T">The specific <see cref="IJobEntity"/> implementation type.</typeparam>
        public static void RunByRef<T>(this ref T jobData)
            where T : struct, IJobEntity  => throw InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Runs the job immediately on the current thread.
        /// </summary>
        /// <param name="jobData">An <see cref="IJobEntity"/> instance.</param>
        /// <param name="query">The query selecting chunks with the necessary components.</param>
        /// <typeparam name="T">The specific <see cref="IJobEntity"/> implementation type.</typeparam>
        public static void Run<T>(this T jobData, EntityQuery query)
            where T : struct, IJobEntity  => throw InternalCompilerInterface.ThrowCodeGenException();

        /// <summary>
        /// Runs the job immediately on the current thread.
        /// </summary>
        /// <param name="jobData">An <see cref="IJobEntity"/> instance. In this variant, the jobData is passed by
        /// reference, which may be necessary for unusually large job structs.</param>
        /// <param name="query">The query selecting chunks with the necessary components.</param>
        /// <typeparam name="T">The specific <see cref="IJobEntity"/> implementation type.</typeparam>
        public static void RunByRef<T>(this ref T jobData, EntityQuery query)
            where T : struct, IJobEntity  => throw InternalCompilerInterface.ThrowCodeGenException();

        #endregion
    }
}
