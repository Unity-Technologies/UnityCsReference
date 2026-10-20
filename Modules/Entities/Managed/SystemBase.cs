// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.Scripting;

namespace Unity.Entities
{
    /// <summary>
    /// Implement SystemBase to create a system in ECS.
    /// </summary>
    /// <remarks>
    /// ### Systems in ECS
    ///
    /// A typical system operates on a set of entities that have specific components. The system identifies
    /// the components of interest, reading and writing data, and performing other entity operations as appropriate.
    ///
    /// The following example shows a basic system that schedules an [IJobEntity] job to iterate over entities.
    /// In this example, the system iterates over all entities with both a Position and a Velocity component and
    /// updates Position based on the delta time elapsed since the last frame.
    ///
    /// <example>
    /// <code source="../DocCodeSamples.Tests/SystemBaseExamples.cs" region="basic-system" title="Basic System Example" language="csharp"/>
    /// </example>
    ///
    /// #### System lifecycle callbacks
    ///
    /// You can define a set of system lifecycle event functions when you implement a system. The runtime invokes these
    /// functions in the following order:
    ///
    /// * <see cref="ComponentSystemBase.OnCreate"/> -- called when the system is created.
    /// * <see cref="ComponentSystemBase.OnStartRunning"/> -- before the first OnUpdate and whenever the system resumes
    ///   running.
    /// * <see cref="OnUpdate"/> -- every frame as long as the system has work to do (see
    ///   <see cref="ComponentSystemBase.ShouldRunSystem"/>) and the system is <see cref="ComponentSystemBase.Enabled"/>.
    /// * <see cref="ComponentSystemBase.OnStopRunning"/> -- whenever the system stops updating because it finds no
    ///   entities matching its queries. Also called before OnDestroy.
    /// * <see cref="ComponentSystemBase.OnDestroy"/> -- when the system is destroyed.
    ///
    /// All of these functions are executed on the main thread. To perform work on background threads, you can schedule
    /// jobs from the <see cref="SystemBase.OnUpdate"/> function.
    ///
    /// #### System update order
    ///
    /// The runtime executes systems in the order determined by their <see cref="ComponentSystemGroup"/>. Place a system
    /// in a group using <see cref="UpdateInGroupAttribute"/>. Use <see cref="UpdateBeforeAttribute"/> and
    /// <see cref="UpdateAfterAttribute"/> to specify the execution order within a group.
    ///
    /// If you do not explicitly place a system in a specific group, the runtime places it in the default <see cref="World"/>
    /// <see cref="SimulationSystemGroup"/>. By default, all systems are discovered, instantiated, and added to the
    /// default World. You can use the <see cref="DisableAutoCreationAttribute"/> to prevent a system from being
    /// created automatically.
    ///
    /// #### Entity queries
    ///
    /// A system caches all queries created through iteration constructs (such as an [IJobEntity] schedule or a
    /// <see cref="SystemAPI.Query{T}"/> loop), through <see cref="ComponentSystemBase.GetEntityQuery"/>, or through
    /// <see cref="ComponentSystemBase.RequireForUpdate"/>. By default, a system calls `OnUpdate()` every frame. You can use
    /// the <see cref="RequireMatchingQueriesForUpdateAttribute"/> to make the system only update when one of these cached
    /// queries finds entities. See <see cref="M:Unity.Entities.ComponentSystemBase.ShouldRunSystem"/> for more details on whether
    /// a system will update.
    ///
    /// #### Entity iteration options
    ///
    /// * **[IJobEntity]** — define a struct with an Execute method taking `ref` / `in` component parameters. Run it using
    ///   `Schedule()`, `ScheduleParallel()`, or `Run()`. Jobs execute asynchronously, and cannot safely access data
    /// outside of their own instance. Put any required data into fields and set
    ///   them when you create the job (for example, `new MyJob { DeltaTime = SystemAPI.Time.DeltaTime }`).
    /// * **<see cref="SystemAPI.Query{T}"/>** — iterate immediately on the main thread (optionally Burst compiled) using
    /// `foreach` over tuples of `RefRO&lt;T&gt;` / `RefRW&lt;T&gt;` wrappers.
    /// * **<see cref="IJobChunk"/>** — implement job structs that run per chunk.
    /// * **C# Jobs (<see cref="IJob"/>, <see cref="IJobParallelFor"/>, etc.)** — general-purpose, non-ECS-specific jobs.
    ///   Use such jobs for computation or processing that does not require an entity query.
    ///
    /// #### System attributes
    ///
    /// You can use a number of attributes on your SystemBase implementation to control when it updates:
    ///
    /// * <seealso cref="UpdateInGroupAttribute"/> -- place the system in a <seealso cref="ComponentSystemGroup"/>.
    /// * <seealso cref="UpdateBeforeAttribute"/> -- always update the system before another system in the same group.
    /// * <seealso cref="UpdateAfterAttribute"/> -- always update the system after another system in the same group.
    /// * <seealso cref="RequireMatchingQueriesForUpdateAttribute"/> -- skip `OnUpdate` if every EntityQuery used by
    ///   the system is empty.
    /// * <seealso cref="DisableAutoCreationAttribute"/> -- do not create the system automatically.
    /// * <seealso cref="AlwaysSynchronizeSystemAttribute"/> -- force a [sync point](xref:concepts-structural-changes) before invoking
    ///   `OnUpdate`.
    ///
    /// Migration note: `Entities.ForEach` and `Job.WithCode` are deprecated as of Entities 1.4. Use [IJobEntity], [IJobChunk] or
    /// <see cref="SystemAPI.Query{T}"/>. For more information, refer to the upgrade guide.
    ///
    /// [JobHandle]: xref:Unity.Jobs.JobHandle
    /// [JobHandle.CompleteDependencies]: xref:Unity.Jobs.JobHandle.CombineDependencies
    /// [C# Job]: xref:JobSystem
    /// [ECB]: xref:Unity.Entities.EntityCommandBuffer
    /// [ComponentSystemBase.GetEntityQuery]: xref:Unity.Entities.ComponentSystemBase.GetEntityQuery*
    /// [ComponentSystemBase.RequireForUpdate]: xref:Unity.Entities.ComponentSystemBase.RequireForUpdate*
    /// </remarks>
    [RequireDerived]
    public unsafe abstract partial class SystemBase : ComponentSystemBase
    {
        /// <summary>
        /// The ECS-related data dependencies of the system.
        /// </summary>
        /// <remarks>
        /// Before <see cref="OnUpdate"/>, the Dependency property represents the combined job handles of any job that
        /// writes to the same components that the current system reads -- or reads the same components that the current
        /// system writes to. When you schedule [IJobEntity] execution, the system uses the Dependency property to specify a job’s
        /// dependencies when scheduling it. The system also combines the new job's <see cref="JobHandle"/> with Dependency so that any subsequent
        /// job scheduled in the system depends on the earlier jobs (in sequence).
        ///
        /// The following example illustrates an `OnUpdate()` implementation that relies on implicit dependency
        /// management. The function schedules multiple IJobEntity jobs, each depending on the previous one:
        ///
        /// <example>
        /// <code source="../DocCodeSamples.Tests/SystemBaseExamples.cs" region="simple-dependency" title="Implicit Dependency Example" language="csharp"/>
        /// </example>
        ///
        /// You can opt out of this default dependency management by explicitly passing a <see cref="JobHandle"/> when scheduling
        /// an IJobEntity. When you pass in a handle, the scheduling API returns a new handle representing the input dependencies
        /// combined with the new job. Handles from jobs scheduled with explicit dependencies are not automatically combined with
        /// the system’s Dependency property. You must set the Dependency property manually to propagate dependencies.
        ///
        /// The following <see cref="OnUpdate"/> function illustrates manual dependency management. The function schedules
        /// two IJobEntity jobs that do not depend upon each other (only the incoming system Dependency). Then a third job
        /// (IJobEntity or an <see cref="IJob"/>) depends on both of the prior jobs; their handles are combined using
        /// <see cref="JobHandle.CombineDependencies(JobHandle, JobHandle)"/>. Finally, the resulting handle is assigned
        /// to the Dependency property so that the ECS safety manager can propagate the dependencies to subsequent systems.
        ///
        /// <example>
        /// <code source="../DocCodeSamples.Tests/SystemBaseExamples.cs" region="manual-dependency" title="Manual Dependency Example" language="csharp"/>
        /// </example>
        ///
        /// You can combine implicit and explicit dependency management (by using [JobHandle.CombineDependencies]);
        /// however, doing so can be error prone. When you set the Dependency property, the assigned [JobHandle]
        /// replaces any existing dependency, it is not combined with them.
        ///
        /// Note that the default, implicit dependency management does not include <see cref="IJobChunk"/> jobs.
        /// You must manage the dependencies for <see cref="IJobChunk"/> explicitly.
        ///
        /// [JobHandle]: https://docs.unity3d.com/ScriptReference/Unity.Jobs.JobHandle.html
        /// [JobHandle.CombineDependencies]: https://docs.unity3d.com/ScriptReference/Unity.Jobs.JobHandle.CombineDependencies.html
        /// </remarks>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        protected JobHandle Dependency { get => CheckedState()->Dependency; set => CheckedState()->Dependency = value; }

        /// <summary>
        /// The <see cref="SystemState"/> for this SystemBase.
        /// </summary>
        /// <remarks>
        /// All systems are backed by a <see cref="SystemState"/>. This may be required in, for example, general purpose
        /// utility methods containing a `ref <see cref="SystemState"/>` parameter.
        /// </remarks>
        public ref SystemState CheckedStateRef => ref *CheckedState();

        /// <summary>
        /// Completes job handles registered with this system. See <see cref="Dependency"/> for
        /// more information.
        /// </summary>
        protected void CompleteDependency() => CheckedState()->CompleteDependency();

        /// <summary>
        /// Update the system manually.
        /// </summary>
        /// <remarks>
        /// Systems should never override `Update()`. Instead, implement system behavior in <see cref="OnUpdate"/>.
        ///
        /// If a system manually calls another system's <see cref="Update()"/> method from inside its own
        /// <see cref="OnUpdate()"/> method, <see cref="EntityQuery"/> objects in the caller
        /// system might see unexpected and incorrect change version numbers based on the processing performed in the
        /// target system. For this reason, you shouldn't manually update one system from another if both systems are
        /// processing entity data, especially if either uses <see cref="EntityQuery.SetChangedVersionFilter(ComponentType[])"/>.
        /// This guidance doesn't apply to <see cref="ComponentSystemGroup"/> or other "pass-through" systems which only
        /// update other systems without manipulating entity data.
        /// </remarks>
        public sealed override void Update()
        {
            var state = CheckedState();

            using (state->m_ProfilerMarker.Auto())
            {
                state->BeforeUpdateResetRunTracker();

                if (Enabled && ShouldRunSystem())
                {
                    ref var world = ref World.Unmanaged.GetImpl();
                    var previousGlobalState = new WorldUnmanagedImpl.PreviousSystemGlobalState(ref world, state);

                    state->BeforeOnUpdate();

                    bool success = false;
                    try
                    {
                        if (!state->PreviouslyEnabled)
                        {
                            state->PreviouslyEnabled = true;
                            OnStartRunning();
                        }

                        OnUpdate();
                        success = true;
                    }
                    finally
                    {
                        state->AfterOnUpdate();
                        previousGlobalState.Restore(ref world, state);

                        // Limit follow up errors if we arrived here due to a job related exception by syncing all jobs
                        if (!success)
                            state->m_DependencyManager->Safety.PanicSyncAll();
                    }
                }
                else if (state->PreviouslyEnabled)
                {
                    ref var world = ref World.Unmanaged.GetImpl();
                    var previousGlobalState = new WorldUnmanagedImpl.PreviousSystemGlobalState(ref world, state);

                    state->PreviouslyEnabled = false;
                    state->BeforeOnUpdate();

                    try
                    {
                        OnStopRunningInternal();
                    }
                    finally
                    {
                        state->AfterOnUpdate();
                        previousGlobalState.Restore(ref world, state);
                    }
                }
            }
        }

        internal sealed override void OnBeforeCreateInternal(World world)
        {
            base.OnBeforeCreateInternal(world);
        }

        internal sealed override void OnBeforeDestroyInternal()
        {
            base.OnBeforeDestroyInternal();
            CheckedState()->m_JobHandle.Complete();
        }

        /// <summary>Implement `OnUpdate()` to perform the major work of this system.</summary>
        /// <remarks>
        /// <p>
        /// By default, the system invokes `OnUpdate()` once every frame on the main thread.
        /// To skip OnUpdate if all of the system's [EntityQueries] are empty, use the
        /// [RequireMatchingQueriesForUpdateAttribute]. To limit when OnUpdate is invoked, you can
        /// specify components that must exist, or queries that match specific Entities. To do
        /// this, call <see cref="M:Unity.Entities.ComponentSystemBase.RequireForUpdate``1"/> or
        /// <see cref="M:Unity.Entities.ComponentSystemBase.RequireForUpdate(Unity.Entities.EntityQuery)"/>
        /// in the system's OnCreate method. For more information, see [ShouldRunSystem].
        /// </p>
        /// <p>
        /// Use [IJobEntity] or <see cref="SystemAPI.Query{T}"/> for common iteration patterns. You can also instantiate and schedule an <see cref="IJobChunk"/> instance;
        /// implement other Unity.Jobs interfaces or perform work on the main thread. If you call <see cref="EntityManager"/> methods
        /// that perform structural changes on the main thread, be sure to arrange the system order to minimize the
        /// performance impact of the resulting [sync points].
        /// </p>
        ///
        /// [sync points]: xref:concepts-structural-changes
        /// [C# Job System]: xref:JobSystem
        /// [IJobEntity]: xref:Unity.Entities.IJobEntity
        /// [EntityQueries]: xref:Unity.Entities.EntityQuery
        /// [RequireMatchingQueriesForUpdateAttribute]: xref:Unity.Entities.RequireMatchingQueriesForUpdateAttribute
        /// [ShouldRunSystem]: xref:Unity.Entities.ComponentSystemBase.ShouldRunSystem
        /// </remarks>
        [RequiredMember]
        protected abstract void OnUpdate();

        /// <summary>
        /// Look up the value of a component for an entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <typeparam name="T">The type of component to retrieve.</typeparam>
        /// <returns>A struct of type T containing the component value.</returns>
        /// <remarks>
        /// Use this method to look up data in another entity using its <see cref="Entity"/> object. For example, if you
        /// have a component that contains an Entity field, you can look up the component data for the referenced
        /// entity using this method.
        ///
        /// When iterating over entities coming from a query, do not use this method to access data of the
        /// current entity in the set. This function is much slower than accessing the data directly.
        ///
        /// When you call this method on the main thread, it invokes <see cref="EntityManager.GetComponentData{T}"/>.
        ///
        /// This lookup method results in a slower, indirect memory access. When possible, organize your
        /// data to minimize the need for indirect lookups.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown if the component type has no fields.</exception>
        [Obsolete("Use SystemAPI.GetComponent instead (RemovedAfter Entities 1.0)")]
        protected internal T GetComponent<T>(Entity entity) where T : unmanaged, IComponentData
        {
            return EntityManager.GetComponentData<T>(entity);
        }

        /// <summary>
        /// Sets the value of a component of an entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="component">The data to set.</param>
        /// <typeparam name="T">The component type.</typeparam>
        /// <remarks>
        /// Use this method to look up and set data in another entity using its <see cref="Entity"/> object. For example, if you
        /// have a component that contains an Entity field, you can update the component data for the referenced
        /// entity using this method.
        ///
        /// When iterating over entities coming from a query, do not use this method to update data of the
        /// current entity in the set. This function is much slower than accessing the data directly.
        ///
        /// When you call this method on the main thread, it invokes <see cref="EntityManager.SetComponentData{T}"/>.
        ///
        /// This lookup method results in a slower, indirect memory access. When possible, organize your
        /// data to minimize the need for indirect lookups.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown if the component type has no fields.</exception>
        [Obsolete("Use SystemAPI.SetComponent instead (RemovedAfter Entities 1.0)")]
        protected internal void SetComponent<T>(Entity entity, T component) where T : unmanaged, IComponentData
        {
            EntityManager.SetComponentData(entity, component);
        }

        /// <summary>
        /// Checks whether an entity has a specific type of component.
        /// </summary>
        /// <param name="entity">The Entity object.</param>
        /// <typeparam name="T">The data type of the component.</typeparam>
        /// <remarks>
        /// Always returns false for an entity that has been destroyed.
        ///
        /// Use this method to check if another entity has a given type of component using its <see cref="Entity"/>
        /// object. For example, if you have a component that contains an Entity field, you can check whether the
        /// referenced entity has a specific type of component using this method. (Entities in the set always have
        /// required components, so you don’t need to check for them.)
        ///
        /// When iterating over entities coming from a query, avoid using this method with the
        /// current entity in the set. It is generally faster to adjust your query to avoid optional components.
        ///
        /// When you call this method on the main thread, it invokes <see cref="EntityManager.HasComponent{T}"/>.
        ///
        /// This lookup method results in a slower, indirect memory access. When possible, organize your
        /// data to minimize the need for indirect lookups.
        /// </remarks>
        /// <returns>True, if the specified entity has the component.</returns>
        [Obsolete("Use SystemAPI.HasComponent instead (RemovedAfter Entities 1.0)")]
        protected internal bool HasComponent<T>(Entity entity) where T : unmanaged, IComponentData
        {
            return EntityManager.HasComponent<T>(entity);
        }

        /// <summary>
        /// Checks whether an entity has a dynamic buffer of a specific IBufferElementData type.
        /// </summary>
        /// <param name="entity">The Entity object.</param>
        /// <typeparam name="T">The IBufferElementData type.</typeparam>
        /// <remarks>
        /// Always returns false for an entity that has been destroyed.
        ///
        /// Use this method to check if another entity has a dynamic buffer of a given IBufferElementData type using its <see cref="Entity"/>
        /// object.
        ///
        /// When iterating over entities coming from a query, avoid using this method with the
        /// current entity in the set. It is generally faster to change your query methods to avoid optional components.
        ///
        /// When you call this method on the main thread, it invokes <see cref="EntityManager.HasBuffer{T}"/>.
        ///
        /// This lookup method results in a slower, indirect memory access. When possible, organize your
        /// data to minimize the need for indirect lookups.
        /// </remarks>
        /// <returns>True, if the specified entity has the component.</returns>
        protected internal bool HasBuffer<T>(Entity entity) where T : struct, IBufferElementData
        {
            return EntityManager.HasBuffer<T>(entity);
        }

        /// <summary>
        /// Manually gets a dictionary-like container containing all components of type T, keyed by Entity.
        /// </summary>
        /// <remarks>Remember to call <see cref="ComponentLookup{T}.Update(SystemBase)"/>. </remarks>
        /// <param name="isReadOnly">Whether the data is only read, not written. Access data as
        /// read-only whenever possible.</param>
        /// <typeparam name="T">A struct that implements <see cref="IComponentData"/>.</typeparam>
        /// <remarks>
        /// When you call this method on the main thread, it invokes <see cref="ComponentSystemBase.GetComponentLookup{T}"/>.
        /// </remarks>
        /// <returns>All component data of type T.</returns>
        /// <remarks> Prefer using <see cref="SystemAPI.GetComponentLookup{T}"/> as it will cache in OnCreate for you
        /// and call .Update(this) at the call-site. Also works with IJobEntity and SystemAPI.Query. </remarks>
        public new ComponentLookup<T> GetComponentLookup<T>(bool isReadOnly = false)
            where T : unmanaged, IComponentData
        {
            return base.GetComponentLookup<T>(isReadOnly);
        }
        /// <summary> Obsolete. Use <see cref="GetComponentLookup{T}"/> instead.</summary>
        /// <param name="isReadOnly">Whether the data is only read, not written. Access data as
        /// read-only whenever possible.</param>
        /// <typeparam name="T">A struct that implements <see cref="IComponentData"/>.</typeparam>
        /// <returns>All component data of type T.</returns>
        [Obsolete("This method has been renamed to GetComponentLookup. (RemovedAFter Entities 1.0)", true)] // Can't use (UnityUpgradable) due to similar rename in ComponentSystemBase
        public new ComponentLookup<T> GetComponentDataFromEntity<T>(bool isReadOnly = false)
            where T : unmanaged, IComponentData
        {
            return base.GetComponentLookup<T>(isReadOnly);
        }

        /// <summary>
        /// Gets the dynamic buffer of an entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <remarks>
        /// When you call this method on the main thread, it invokes <see cref="EntityManager.GetBuffer{T}"/>.
        /// </remarks>
        /// <param name="isReadOnly">Whether the buffer data is only read or is also written. Access data in
        /// a read-only fashion whenever possible.</param>
        /// <typeparam name="T">The type of the buffer's elements.</typeparam>
        /// <returns>The DynamicBuffer object for accessing the buffer contents.</returns>
        /// <exception cref="ArgumentException">Thrown if T is an unsupported type.</exception>
        [Obsolete("Use SystemAPI.GetBuffer instead (RemovedAfter Entities 1.0)")]
        public DynamicBuffer<T> GetBuffer<T>(Entity entity, bool isReadOnly = false) where T : unmanaged, IBufferElementData
        {
            return CheckedState()->GetBuffer<T>(entity, isReadOnly);
        }

        /// <summary>
        /// Manually gets a BufferLookup&lt;T&gt; object that can access a <seealso cref="DynamicBuffer{T}"/>.
        /// </summary>
        /// <remarks>Remember to call <see cref="BufferLookup{T}.Update(SystemBase)"/>. </remarks>
        /// <remarks>Assign the returned object to a field of your Job struct so that you can access the
        /// contents of the buffer in a Job.
        /// When you call this method on the main thread, it invokes <see cref="ComponentSystemBase.GetBufferLookup{T}"/>.
        /// </remarks>
        /// <param name="isReadOnly">Whether the buffer data is only read or is also written. Access data in
        /// a read-only fashion whenever possible.</param>
        /// <typeparam name="T">The type of <see cref="IBufferElementData"/> stored in the buffer.</typeparam>
        /// <returns>An array-like object that provides access to buffers, indexed by <see cref="Entity"/>.</returns>
        /// <seealso cref="ComponentLookup{T}"/>
        /// <remarks> Prefer using <see cref="SystemAPI.GetBufferLookup{T}"/> as it will cache in OnCreate for you
        /// and call .Update(this) at the call-site. Also works with IJobEntity and SystemAPI.Query. </remarks>
        public new BufferLookup<T> GetBufferLookup<T>(bool isReadOnly = false) where T : unmanaged, IBufferElementData
        {
            return base.GetBufferLookup<T>(isReadOnly);
        }
        /// <summary> Obsolete. Use <see cref="GetBufferLookup{T}"/> instead.</summary>
        /// <param name="isReadOnly">Whether the buffer data is only read or is also written. Access data in
        /// a read-only fashion whenever possible.</param>
        /// <typeparam name="T">The type of <see cref="IBufferElementData"/> stored in the buffer.</typeparam>
        /// <returns>An array-like object that provides access to buffers, indexed by <see cref="Entity"/>.</returns>
        [Obsolete("This method has been renamed to GetBufferLookup. (RemovedAFter Entities 1.0)", true)] // Can't use (UnityUpgradable) due to similar rename in ComponentSystemBase
        public new BufferLookup<T> GetBufferFromEntity<T>(bool isReadOnly = false) where T : unmanaged, IBufferElementData
        {
            return base.GetBufferLookup<T>(isReadOnly);
        }

        /// <summary>
        /// Manually gets an EntityStorageInfoLookup object that can access a <see cref="EntityStorageInfo"/>.
        /// </summary>
        /// <remarks>Remember to call <see cref="EntityStorageInfoLookup.Update(SystemBase)"/>. </remarks>
        /// <remarks>Assign the returned object to a field of your Job struct so that you can access the
        /// contents in a Job.
        /// </remarks>
        /// <returns>A dictionary-like object that provides access to information about how Entities are stored,
        /// indexed by <see cref="Entity"/>.</returns>
        /// <seealso cref="EntityStorageInfoLookup"/>
        /// <remarks> Prefer using <see cref="SystemAPI.GetEntityStorageInfoLookup"/> as it will cache in OnCreate for you
        /// and call .Update(this) at the call-site. </remarks>
        public new EntityStorageInfoLookup GetEntityStorageInfoLookup() => base.GetEntityStorageInfoLookup();
        /// <summary> Obsolete. Use <see cref="GetEntityStorageInfoLookup"/> instead.</summary>
        /// <returns>True if the given entity exists or the entity has a Cleanup Component that is yet to be destroyed</returns>
        [Obsolete("This method has been renamed to GetEntityStorageInfoLookup. (RemovedAFter Entities 1.0)", true)] // Can't use (UnityUpgradable) due to similar rename in ComponentSystemBase
        public new EntityStorageInfoLookup GetStorageInfoFromEntity() => base.GetEntityStorageInfoLookup();

        /// <summary>
        /// Checks if the entity exists inside this system's EntityManager.
        /// </summary>
        /// <remarks>
        /// This returns true for an entity that was destroyed with DestroyEntity, but still has a cleanup component.
        /// Prefer <see cref="ComponentLookup{T}.TryGetComponent"/> where applicable.
        /// </remarks>
        /// <param name="entity">The entity to check</param>
        /// <returns>True if the given entity exists or the entity has a Cleanup Component that is yet to be destroyed</returns>
        /// <seealso cref="EntityManager.Exists"/>
        [Obsolete("Use SystemAPI.Exists instead (RemovedAfter Entities 1.0)")]
        public bool Exists(Entity entity)
        {
            return EntityManager.Exists(entity);
        }
    }
}
