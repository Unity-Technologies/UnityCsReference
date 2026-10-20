// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    /// <summary>
    /// A [NativeContainer] that provides access to all instances of <see cref="TransformRef"/> components,
    /// indexed by <see cref="Entity"/>.
    /// </summary>
    /// <remarks>
    /// TransformLookup is a native container that provides array-like access to <see cref="TransformRef"/> components.
    /// For example, while iterating over a set of entities, you can use TransformLookup to get and set the transforms
    /// of unrelated entities.
    ///
    /// To get a TransformLookup, call <see cref="SystemState.GetTransformLookup"/>.
    ///
    /// Pass a TransformLookup container to a job by defining a public field of the appropriate type
    /// in your IJob implementation. You can safely read from TransformLookup in any job, but by
    /// default, you cannot write to components in the container in parallel jobs (including
    /// <see cref="IJobEntity"/>, <see cref="SystemAPI.Query{T}"/> and <see cref="IJobChunk"/>). If you know that two
    /// instances of a parallel job can never write to the same index in the container, you can disable the restriction
    /// on parallel writing by adding [NativeDisableParallelForRestrictionAttribute] to the TransformLookup field
    /// definition in the job struct.
    ///
    /// [NativeContainer]: https://docs.unity3d.com/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeContainerAttribute
    /// [NativeDisableParallelForRestrictionAttribute]: https://docs.unity3d.com/ScriptReference/Unity.Collections.NativeDisableParallelForRestrictionAttribute.html
    /// </remarks>
    [NativeContainer]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct TransformLookup
    {
        internal AtomicSafetyHandle m_Safety0;
        // We need to track hierarchy safety correctly as well to handle GameObjects (DOTS-10269).
        internal AtomicSafetyHandle m_HierarchySafety;
        private int m_SafetyReadOnlyCount;
        private int m_SafetyReadWriteCount;

        [NativeDisableUnsafePtrRestriction] private readonly EntityDataAccess* m_Access;
        LookupCache m_Cache;

        uint m_GlobalSystemVersion;
        private readonly byte  m_IsReadOnly;
        private TypeIndex m_TransformTypeIndex;

        internal uint GlobalSystemVersion => m_GlobalSystemVersion;


        internal TransformLookup(EntityDataAccess* access, bool isReadOnly,
                                  AtomicSafetyHandle safety, AtomicSafetyHandle hierarchySafety)
        {
            m_Safety0 = safety;
            m_HierarchySafety = hierarchySafety;
            m_SafetyReadOnlyCount = isReadOnly ? 2 : 0;
            m_SafetyReadWriteCount = isReadOnly ? 0 : 2;
            m_Access = access;
            m_IsReadOnly = isReadOnly ? (byte)1 : (byte)0;
            m_Cache = default;
            m_GlobalSystemVersion = access->EntityComponentStore->GlobalSystemVersion;
            m_TransformTypeIndex = TypeManager.GetTypeIndex<TransformRef>();
        }

        /// <summary>
        /// When a TransformLookup is cached by a system across multiple system updates, calling this function
        /// inside the system's OnUpdate() method performs the minimal incremental updates necessary to make the
        /// lookup safe to use.
        /// </summary>
        /// <param name="systemState">The <see cref="SystemState"/> of the system on which this lookup is
        /// cached.</param>
        public void Update(ref SystemState systemState)
        {
            // The SystemState parameter is not strictly needed to read the version, but requiring it keeps this
            // API main-thread-only and symmetric with BufferLookup<T>.Update
            m_GlobalSystemVersion = systemState.m_EntityComponentStore->GlobalSystemVersion;

            var safetyHandles = &m_Access->DependencyManager->Safety;
            m_Safety0 = safetyHandles->GetSafetyHandleForComponentLookup(m_TransformTypeIndex, m_IsReadOnly != 0);
            // TODO DOTS-10269: this should be a per-hierarchy handle, but for now we use the secondary
            // per-type handle as used by DynamicBuffer types.
            m_HierarchySafety = safetyHandles->GetBufferHandleForBufferLookup(m_TransformTypeIndex);
        }

        /// <summary>
        /// Retrieves the transform components associated with the specified <see cref="Entity"/>, if it exists.
        /// The return value indicates whether the component was successfully retrieved.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="transform">The transform component of type T for the given entity, if it exists.</param>
        /// <returns>True if the entity has a <see cref="TransformRef"/> component, and false if it does not. Also returns false if
        /// the Entity instance refers to an entity that has been destroyed (or never existed).</returns>
        public bool TryGetTransform(Entity entity, out TransformRef transform) => TryGetTransform(entity, out transform, out _);

        /// <summary>
        /// Retrieves the transform components associated with the specified <see cref="Entity"/>, if it exists.
        /// The return value indicates whether the component was successfully retrieved.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="transform">The transform component of type T for the given entity, if it exists.</param>
        /// <param name="entityExists">Denotes whether the given entity exists. Use to distinguish entity non-existence
        /// from TransformRef non-existence.</param>
        /// <returns>True if the entity has a <see cref="TransformRef"/> component, and false if it does not. Also returns false if
        /// the Entity instance refers to an entity that has been destroyed (or never existed).</returns>
        public bool TryGetTransform(Entity entity, out TransformRef transform, out bool entityExists)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety0);
            var ecs = m_Access->EntityComponentStore;
            entityExists = Hint.Likely(ecs->Exists(entity));
            if (entityExists)
            {
                var transformUnion = (m_IsReadOnly != 0)?
                    (TransformUnion*)ecs->GetOptionalComponentDataWithTypeRO(entity, m_TransformTypeIndex, ref m_Cache) :
                    (TransformUnion*)ecs->GetOptionalComponentDataWithTypeRW(entity, m_TransformTypeIndex, m_GlobalSystemVersion, ref m_Cache);

                if (transformUnion != null)
                {
                    transform =
                        new TransformRef(transformUnion, m_IsReadOnly != 0, m_Safety0, m_HierarchySafety);
                    return true;
                }
            }
            // fallback if entity is invalid or doesn't have the component
            transform = default;
            return false;
        }

        /// <summary>
        /// Reports whether the specified entity exists.
        /// Does not consider whether a <see cref="TransformRef"/> exists on the given entity.
        /// </summary>
        /// <param name="entity">The referenced entity.</param>
        /// <returns>True if the entity exists, regardless of whether this entity has a TransformRef.</returns>
        /// <seealso cref="TryGetTransform(Unity.Entities.Entity,out Unity.Entities.TransformRef,out bool)"/>
        public bool EntityExists(Entity entity)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety0);
            var ecs = m_Access->EntityComponentStore;
            return ecs->Exists(entity);
        }

        /// <summary>
        /// Reports whether the specified <see cref="Entity"/> instance still refers to a valid entity and that it has a
        /// transform component.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>True if the entity has a <see cref="TransformRef"/> component, and false if it does not. Also returns false if
        /// the Entity instance refers to an entity that has been destroyed (or never existed).</returns>
        public bool HasTransform(Entity entity) => HasTransform(entity, out _);

        /// <summary>
        /// Reports whether the specified <see cref="Entity"/> instance still refers to a valid entity and that it has a
        /// transform component.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="entityExists">Denotes whether the given entity exists. Use to distinguish entity non-existence
        /// from TransformRef non-existence.</param>
        /// <returns>True if the entity has a <see cref="TransformRef"/> component, and false if it does not. Also returns false if
        /// the Entity instance refers to an entity that has been destroyed (or never existed).</returns>
        public bool HasTransform(Entity entity, out bool entityExists)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety0);
            var ecs = m_Access->EntityComponentStore;
            return ecs->HasComponent(entity, m_TransformTypeIndex, ref m_Cache, out entityExists);
        }

        /// <summary>
        /// Reports whether any of <see cref="TransformRef"/> components in the chunk containing the
        /// specified <see cref="Entity"/> could have changed since a previous version.
        /// </summary>
        /// <remarks>
        /// Note that for efficiency, the change version applies to whole chunks not individual entities. The change
        /// version is incremented even when another job or system that has declared write access to a component does
        /// not actually change the component value.
        /// </remarks>
        /// <param name="entity">The entity.</param>
        /// <param name="version">The version to compare. In a system, this parameter should be set to the
        /// current <see cref="Unity.Entities.ComponentSystemBase.LastSystemVersion"/> at the time the job is run or
        /// scheduled.</param>
        /// <returns>True, if the version number stored in the chunk for this component is more recent than the version
        /// passed to the <paramref name="version"/> parameter.</returns>
        public bool DidChange(Entity entity, uint version)
        {
            var ecs = m_Access->EntityComponentStore;
            var chunk = ecs->GetChunk(entity);
            var archetype = ecs->GetArchetype(chunk);
            if (Hint.Unlikely(archetype != m_Cache.Archetype))
                m_Cache.Update(archetype, m_TransformTypeIndex);
            var typeIndexInArchetype = m_Cache.IndexInArchetype;
            if (typeIndexInArchetype == -1) return false;
            var chunkVersion = archetype->Chunks.GetChangeVersion(typeIndexInArchetype, chunk.ListIndex);

            return ChangeVersionUtility.DidChange(chunkVersion, version);
        }

        /// <summary>
        /// Gets the <see cref="TransformRef"/> instance for the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>A <see cref="TransformRef"/> type.</returns>
        /// <remarks>
        /// Normally, you cannot write to transforms accessed using a TransformLookup instance
        /// in a parallel Job. This restriction is in place because multiple threads could write to the same buffer,
        /// leading to a race condition and nondeterministic results. However, when you are certain that your algorithm
        /// cannot write to the same buffer from different threads, you can manually disable this safety check
        /// by putting the [NativeDisableParallelForRestriction] attribute on the BufferLookup field in the Job.
        ///
        /// [NativeDisableParallelForRestrictionAttribute]: https://docs.unity3d.com/ScriptReference/Unity.Collections.NativeDisableParallelForRestrictionAttribute.html
        /// </remarks>
        /// <exception cref="System.ArgumentException">Thrown if <paramref name="entity"/> does not have a <see cref="TransformRef"/>, or is an invalid entity.
        /// component.</exception>
        public TransformRef this[Entity entity]
        {
            get
            {
                var ecs = m_Access->EntityComponentStore;
                // Note that this check is only for the lookup table into the entity manager
                // The native array performs the actual read only / write only checks
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety0);
                ecs->AssertEntityHasComponent(entity, m_TransformTypeIndex, ref m_Cache);
                var transformUnion = (m_IsReadOnly != 0)?
                    (TransformUnion*)ecs->GetOptionalComponentDataWithTypeRO(entity, m_TransformTypeIndex, ref m_Cache) :
                    (TransformUnion*)ecs->GetOptionalComponentDataWithTypeRW(entity, m_TransformTypeIndex, m_GlobalSystemVersion, ref m_Cache);

                return new TransformRef(transformUnion, m_IsReadOnly != 0, m_Safety0, m_HierarchySafety);
            }
        }
    }
}
