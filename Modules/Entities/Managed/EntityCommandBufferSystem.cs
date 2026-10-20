// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;


namespace Unity.Entities
{
    /// <summary>
    /// Interface for use with EntityCommandBufferSystems to store the list of pending buffers,
    /// which can be accessed from a bursted ISystem. You should implement a singleton with this interface
    /// every time you inherit from EntityCommandBufferSystem.
    /// </summary>
    public interface IECBSingleton
    {
        /// <summary>
        /// Sets the list of command buffers to play back when this system updates.
        /// </summary>
        /// <remarks>
        /// This method is only intended for internal use, but must be in the public API due to language
        /// restrictions. Command buffers created with `CreateCommandBuffer` are automatically added to
        /// the system's list of pending buffers to play back.
        /// </remarks>
        /// <param name="buffers">The list of buffers to play back. This list replaces any existing pending command buffers on this system.</param>
        public void SetPendingBufferList(ref UnsafeList<EntityCommandBuffer> buffers);

        /// <summary>
        /// Set the allocator that command buffers created with this singleton should be allocated with.
        /// </summary>
        /// <param name="allocatorIn">The allocator to use</param>
        public void SetAllocator(Allocator allocatorIn);

        /// <summary>
        /// Set the allocator that command buffers created with this singleton should be allocated with.
        /// </summary>
        /// <param name="allocatorIn">The allocator to use</param>
        public void SetAllocator(AllocatorManager.AllocatorHandle allocatorIn) => SetAllocator(allocatorIn.ToAllocator);
    }

    /// <summary>
    /// A system that provides <seealso cref="EntityCommandBuffer"/> objects for other systems.
    /// </summary>
    /// <remarks>
    /// Each system that uses the EntityCommandBuffer provided by a command buffer system must create its own command
    /// buffer instance. This buffer system executes each of these separate command buffers in the order that you created
    /// them. The commands are executed during this system's <see cref="OnUpdate"/> function.
    ///
    /// To create a command buffer, get the buffer system's `Singleton` component with
    /// <see cref="SystemAPI.GetSingleton{T}"/>, and then call `CreateCommandBuffer(WorldUnmanaged)` on that singleton.
    /// If you write to a command buffer created this way from a Job, you don't need to register that Job with the buffer
    /// system: the buffer system completes all Jobs scheduled against its singleton component before it plays the command
    /// buffer back.
    ///
    /// If you instead call <see cref="CreateCommandBuffer"/> on the buffer system instance, and you write to that command
    /// buffer from a Job, you must add the <see cref="JobHandle"/> of that Job to the buffer system's dependency list with
    /// <see cref="AddJobHandleForProducer"/>.
    ///
    /// If you write to a command buffer from a Job that runs in
    /// parallel (and this includes both <see cref="IJobEntity"/> and <see cref="IJobChunk"/>), you must use the
    /// concurrent version of the command buffer (<seealso cref="EntityCommandBuffer.ParallelWriter"/>).
    ///
    /// Executing the commands in an EntityCommandBuffer invokes the corresponding functions of the
    /// <see cref="EntityManager"/>. Any structural change, such as adding or removing entities, adding or removing
    /// components from entities, or changing shared component values, creates a sync-point in your application.
    /// At a sync point, all Jobs accessing entity components must complete before new Jobs can start. Such sync points
    /// make it difficult for the Job scheduler to fully utilize available computing power. To avoid sync points,
    /// you should use as few entity command buffer systems as possible.
    ///
    /// The default ECS <see cref="World"/> code creates a <see cref="ComponentSystemGroup"/> setup with
    /// three main groups, <see cref="InitializationSystemGroup"/>, <see cref="SimulationSystemGroup"/>, and
    /// <see cref="PresentationSystemGroup"/>. Each of these main groups provides an existing EntityCommandBufferSystem
    /// executed at the start and the end of other, child systems.
    ///
    /// Note that unused command buffers systems do not create sync points because there are no commands to execute and
    /// thus no structural changes created.
    ///
    /// The EntityCommandBufferSystem class is abstract, so you must implement a subclass to create your own
    /// entity command buffer system. However, none of its methods are abstract, so you do not need to implement
    /// your own logic. Typically, you create an EntityCommandBufferSystem subclass to create a named buffer system
    /// for other systems to use and update it at an appropriate place in a custom <see cref="ComponentSystemGroup"/>
    /// setup.</remarks>
    ///
    public abstract unsafe partial class EntityCommandBufferSystem : SystemBase
    {
        /// <summary>
        /// List of command buffers that this system has allocated, which are played back and disposed of when the system updates.
        /// </summary>
        protected UnsafeList<EntityCommandBuffer>* m_PendingBuffers;
        internal AllocatorHelper<RewindableAllocator> m_EntityCommandBufferAllocator;

        protected internal ref UnsafeList<EntityCommandBuffer> PendingBuffers
        {
            get { return ref *m_PendingBuffers; }
        }

        JobHandle m_ProducerHandle;

        /// <summary>
        /// Creates an <seealso cref="EntityCommandBuffer"/> and adds it to this system's list of command buffers.
        /// </summary>
        /// <remarks>
        /// This buffer system executes its list of command buffers during its <see cref="OnUpdate"/> function in the
        /// order you created the command buffers.
        ///
        /// This method requires a managed reference to the buffer system. To create a command buffer from unmanaged systems
        /// code, get the buffer system's `Singleton` component with <see cref="SystemAPI.GetSingleton{T}"/> and call
        /// `CreateCommandBuffer(WorldUnmanaged)` on that singleton instead.
        ///
        /// If you write to a command buffer created by this method in a job, you must add the
        /// job as a dependency of this system by calling <see cref="AddJobHandleForProducer"/>. The dependency ensures
        /// that the buffer system waits for the job to complete before executing the command buffer. This step isn't
        /// necessary for command buffers created through the buffer system's `Singleton` component, because the buffer
        /// system completes all jobs scheduled against that singleton component before it plays the command buffer back.
        ///
        /// If you write to a command buffer from a parallel Job, such as <see cref="IJobEntity"/> or
        /// <see cref="IJobChunk"/>, you must use the concurrent version of the command buffer, provided by
        /// <see cref="EntityCommandBuffer.ParallelWriter"/>.
        /// </remarks>
        /// <returns>A command buffer that will be executed by this system.</returns>
        public EntityCommandBuffer CreateCommandBuffer()
        {
            return CreateCommandBuffer(ref PendingBuffers, m_EntityCommandBufferAllocator.Allocator.Handle.ToAllocator, World.Unmanaged);
        }

        /// <summary>
        /// Adds the specified JobHandle to this system's list of dependencies.
        /// </summary>
        /// <remarks>
        /// When you write to an <see cref="EntityCommandBuffer"/> that you created with <see cref="CreateCommandBuffer"/> from a
        /// job, you must add the <see cref="JobHandle"/> of that job to this <see cref="EntityCommandBufferSystem"/> input
        /// dependencies by calling this function. Otherwise, this system could attempt to execute the command buffer contents
        /// while the writing job is still running, causing a race condition.
        ///
        /// Don't call this function for command buffers that you created through the buffer system's `Singleton` component.
        /// Those command buffers are tracked through the singleton component itself, and this system completes all jobs
        /// scheduled against that component before it plays the command buffers back.
        /// </remarks>
        /// <param name="producerJob">The JobHandle of a Job which this buffer system should wait for before playing back its
        /// pending command buffers.</param>
        /// <example>
        /// The following example illustrates how to use one of the default <see cref="EntityCommandBufferSystem"/>s.
        /// The code selects all entities that have one custom component, in this case, `AsyncProcessInfo`, and
        /// processes each entity in the `Execute()` function of an <see cref="IJobEntity"/> Job. After processing, the Job
        /// uses a <see cref="EntityCommandBuffer"/> to remove the `ProcessInfo` component and add a `ProcessCompleteTag`
        /// component. Another system could use the `ProcessCompleteTag` to find entities that represent the end
        /// results of the process.
        /// <code source="../DocCodeSamples.Tests/EntityCommandBuffers.cs" region="ecb_addjobhandleforproducer" title="AddJobHandleForProducer Example" language="csharp"/>
        /// </example>
        public void AddJobHandleForProducer(JobHandle producerJob)
        {
            m_ProducerHandle = JobHandle.CombineDependencies(m_ProducerHandle, producerJob);
        }

        /// <summary>
        /// Initializes this command buffer system.
        /// </summary>
        /// <remarks>If you override this method, you should call `base.OnCreate()` to retain the default
        /// initialization logic.</remarks>
        protected override void OnCreate()
        {
            base.OnCreate();

            m_EntityCommandBufferAllocator = new AllocatorHelper<RewindableAllocator>(Allocator.Persistent);
            m_EntityCommandBufferAllocator.Allocator.Initialize(16 * 1024);
            m_PendingBuffers = UnsafeList<EntityCommandBuffer>.Create(1, Allocator.Persistent);
        }

        /// <summary>
        /// Destroys this system. Any pending command buffers will be disposed, but their commands will NOT be played back.
        /// </summary>
        /// <remarks>If you override this method, you should call `base.OnDestroy()` to retain the default
        /// destruction logic.</remarks>
        protected override void OnDestroy()
        {
            FlushPendingBuffers(false);
            PendingBuffers.Clear();

            m_PendingBuffers->Dispose();

            AllocatorManager.Free(Allocator.Persistent, m_PendingBuffers);
            m_EntityCommandBufferAllocator.Allocator.Dispose();
            m_EntityCommandBufferAllocator.Dispose();

            base.OnDestroy();
        }

        /// <summary>
        /// Executes the command buffers in this system in the order they were created.
        /// </summary>
        /// <remarks>If you override this method, you should call `base.OnUpdate()` to retain the default
        /// update logic.</remarks>
        protected override void OnUpdate()
        {
            FlushPendingBuffers(true);
            PendingBuffers.Clear();
        }

        internal void FlushPendingBuffers(bool playBack)
        {
            if (m_PendingBuffers->IsEmpty)
                return;

            CompleteDependency();
            m_ProducerHandle.Complete();
            m_ProducerHandle = new JobHandle();

            int length = PendingBuffers.Length;

            List<string> playbackErrorLog = null;
            bool completeAllJobsBeforeDispose = false;
            for (int i = 0; i < length; ++i)
            {
                var buffer = PendingBuffers[i];
                if (!buffer.IsCreated)
                {
                    continue;
                }
                if (playBack)
                {
                    try
                    {
                        var system = World.Unmanaged.TryGetSystemStateForId(buffer.SystemID); // System is likely to be in our world.
                        if (system == null) system = World.FindSystemStateForId(buffer.SystemID);
                        if (system != null) system->m_ProfilerMarker.Begin();
                        buffer.Playback(EntityManager);
                        if (system != null) system->m_ProfilerMarker.End();
                    }
                    catch (Exception e)
                    {
                        var system = World.FindSystemStateForId(buffer.SystemID);
                        var systemType = system != null ? system->DebugName.ToString() : "Unknown";
                        var error = $"{e.Message}\nEntityCommandBuffer was recorded in {systemType} and played back in {GetType()}.\n" + e.StackTrace;
                        if (playbackErrorLog == null)
                        {
                            playbackErrorLog = new List<string>();
                        }
                        playbackErrorLog.Add(error);
                        completeAllJobsBeforeDispose = true;
                    }
                }

                try
                {
                    if (completeAllJobsBeforeDispose)
                    {
                        // If we get here, there was an error during playback (potentially a race condition on the
                        // buffer itself), and we should wait for all jobs writing to this command buffer to complete before attempting
                        // to dispose of the command buffer to prevent a potential race condition.
                        buffer.WaitForWriterJobs();
                        completeAllJobsBeforeDispose = false;
                    }
                    buffer.Dispose();
                }
                catch (Exception e)
                {
                    var system = World.FindSystemStateForId(buffer.SystemID);
                    var systemType = system != null ? system->DebugName.ToString() : "Unknown";
                    var error = $"{e.Message}\nEntityCommandBuffer was recorded in {systemType} and disposed in {GetType()}.\n" + e.StackTrace;
                    if (playbackErrorLog == null)
                    {
                        playbackErrorLog = new List<string>();
                    }
                    playbackErrorLog.Add(error);
                }
                PendingBuffers[i] = buffer;
            }

            if (playbackErrorLog != null)
            {
                var exceptionMessage = new StringBuilder();
                foreach (var err in playbackErrorLog)
                {
                    exceptionMessage.AppendLine(err);
                }
                throw new ArgumentException(exceptionMessage.ToString());
            }

            m_EntityCommandBufferAllocator.Allocator.Rewind();
        }

        /// <summary>
        /// Creates a command buffer, sets it up, and appends it to a list of command buffers.
        /// </summary>
        /// <param name="pendingBuffers">The list of command buffers to append to</param>
        /// <param name="allocator">The allocator to allocate from, when building the command buffer</param>
        /// <param name="world">The world that this command buffer buffers changes for</param>
        /// <returns>Returns the command buffer</returns>
        public static EntityCommandBuffer CreateCommandBuffer(
            ref UnsafeList<EntityCommandBuffer> pendingBuffers,
            AllocatorManager.AllocatorHandle allocator,
            WorldUnmanaged world)
        {
            var cmds = new EntityCommandBuffer(allocator);
            var state = world.ResolveSystemState(world.ExecutingSystem);
            cmds.SystemID = state != null ? state->m_SystemID : 0;
            cmds.OriginSystemHandle = state != null ? state->m_Handle : default;

            pendingBuffers.Add(cmds);

            return cmds;
        }

        /// <summary>
        /// Creates a command buffer that allocates from a World's Update Allocator, sets it up,
        /// and appends it to a list of command buffers.
        /// </summary>
        /// <param name="pendingBuffers">The list of command buffers to append to</param>
        /// <param name="world">The world that this command buffer buffers changes for</param>
        /// <returns>Returns the command buffer</returns>
        public static EntityCommandBuffer CreateCommandBuffer(
            ref UnsafeList<EntityCommandBuffer> pendingBuffers,
            WorldUnmanaged world)
        {
            return CreateCommandBuffer(ref pendingBuffers, world.UpdateAllocator.Handle.ToAllocator, world);
        }
    }

    /// <summary>
    /// Extension methods for EntityCommandBufferSystem.
    /// </summary>
    public static class ECBExtensionMethods
    {
        /// <summary>
        /// Every unmanaged Singleton that implements IECBSingleton must be registered by this function, at the end of
        /// its owner's EntityCommandBufferSystem.OnCreate method, in order to prepare the Singleton with the
        /// unmanaged data necessary for it to work from unmanaged code, without holding a managed reference to the
        /// EntityCommandBufferSystem. This Singleton component will be added to the system entity for this
        /// EntityCommandBufferSystem derived type.
        /// </summary>
        /// <param name="system">The managed EntityCommandBufferSystem that owns the Singleton</param>
        /// <param name="pendingBuffers">The list of command buffers in the managed System to append to</param>
        /// <param name="world">The world that this command buffer buffers changes for</param>
        /// <typeparam name="T">
        /// The unmanaged Singleton type, that corresponds to the managed EntityCommandBufferSystem subclass
        /// </typeparam>
        public static void RegisterSingleton<T>(
            this EntityCommandBufferSystem system,
            ref UnsafeList<EntityCommandBuffer> pendingBuffers,
            WorldUnmanaged world)
            where T : unmanaged, IECBSingleton, IComponentData
        {
            world.EntityManager.AddComponent(system.SystemHandle, ComponentType.ReadWrite<T>());

            var query = new EntityQueryBuilder(system.WorldUpdateAllocator).WithAllRW<T>().WithOptions(EntityQueryOptions.IncludeSystems).Build(system);
            ref var s = ref query.GetSingletonRW<T>().ValueRW;

            s.SetPendingBufferList(ref pendingBuffers);
            s.SetAllocator(system.m_EntityCommandBufferAllocator.Allocator.ToAllocator);
        }

        /// <summary>Obsolete. System entities are used for ECB component data, rather than entityName.</summary>
        /// <param name="system">The managed EntityCommandBufferSystem that owns the Singleton</param>
        /// <param name="pendingBuffers">The list of command buffers in the managed System to append to</param>
        /// <param name="world">The world that this command buffer buffers changes for</param>
        /// <param name="entityName">The name of the entity.</param>
        /// <typeparam name="T">
        /// The unmanaged Singleton type, that corresponds to the managed EntityCommandBufferSystem subclass
        /// </typeparam>
        [Obsolete("The entityName parameter is obsolete. System entities are now used for ECB component data.")]
        public static void RegisterSingleton<T>(
            this EntityCommandBufferSystem system,
            ref UnsafeList<EntityCommandBuffer> pendingBuffers,
            WorldUnmanaged world,
            string entityName)
            where T : unmanaged, IECBSingleton, IComponentData
        {
            RegisterSingleton<T>(system, ref pendingBuffers, world);
        }
    }
}
