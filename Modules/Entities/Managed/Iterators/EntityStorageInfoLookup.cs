// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    /// <summary>
    /// Contains information about where an Entity is stored. To retrieve this information, use <see cref="EntityStorageInfoLookup"/>.
    /// </summary>
    public struct EntityStorageInfo
    {
        /// <summary>
        /// The chunk containing the specified Entity.
        /// </summary>
        /// <remarks>
        /// Users should be extremely careful when accessing this field from job code. Specifically, multiple threads
        /// must not modify the same <see cref="ArchetypeChunk"/>. The type is not thread-safe, and parallel writes will
        /// result in race conditions and unpredictable behavior.
        /// </remarks>
        public ArchetypeChunk Chunk;

        /// <summary>
        /// The index of the specified Entity within the entities in <see cref="Chunk"/>.
        /// </summary>
        public int IndexInChunk;
    }

    /// <summary> Obsolete. Use <see  cref="BufferLookup{T}"/> instead.</summary>
    [Obsolete("This type has been renamed to EntityStorageInfoLookup. (RemovedAfter Entities 1.0) (UnityUpgradable) -> EntityStorageInfoLookup", true)]
    public unsafe struct StorageInfoFromEntity
    {
    }

    /// <summary>
    /// A [NativeContainer] that provides access to information about how Entities are stored. <see cref="Entity"/>.
    /// </summary>
    /// <remarks>
    /// EntityStorageInfoLookup is a native container that provides access to information about how Entities are stored.
    /// You can use EntityStorageInfoLookup to look up data associated with one entity while iterating over a
    /// different set of entities.
    ///
    /// To get a EntityStorageInfoLookup, call <see cref="ComponentSystemBase.GetEntityStorageInfoLookup"/>.
    ///
    /// Pass a EntityStorageInfoLookup container to a job by defining a public field of the appropriate type
    /// in your IJob implementation. You can safely read from EntityStorageInfoLookup in any job, and the EntityStorageInfoLookup
    /// will never write data.
    ///
    /// If you would like to access an entity's storage information outside of a job, consider using the
    /// <see cref="EntityManager"/> methods <see cref="EntityManager.GetStorageInfo"/> instead, to avoid the overhead
    /// of creating a EntityStorageInfoLookup object.
    ///
    /// [NativeContainer]: https://docs.unity3d.com/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeContainerAttribute
    /// [NativeContainerIsReadOnly]: https://docs.unity3d.com/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeContainerIsReadOnlyAttribute.html
    /// </remarks>
    [NativeContainer]
    [NativeContainerIsReadOnly]
    public unsafe struct EntityStorageInfoLookup
    {
        AtomicSafetyHandle      m_Safety;
        [NativeDisableUnsafePtrRestriction]
        readonly EntityDataAccess* m_EntityDataAccess;


        internal EntityStorageInfoLookup(EntityDataAccess* entityDataAccess, AtomicSafetyHandle safety)
        {
            m_Safety = safety;
            m_EntityDataAccess = entityDataAccess;
        }

        /// <summary>
        /// When a EntityStorageInfoLookup is cached by a system across multiple system updates, calling this function
        /// inside the system's OnUpdate() method performs the minimal incremental updates necessary to make the
        /// object safe to use.
        /// </summary>
        /// <param name="system">The system in which this object is used.</param>
        public void Update(SystemBase system) => Update(ref *system.m_StatePtr);

        /// <summary>
        /// When a EntityStorageInfoLookup is cached by a system across multiple system updates, calling this function
        /// inside the system's OnUpdate() method performs the minimal incremental updates necessary to make the
        /// object safe to use.
        /// </summary>
        /// <param name="systemState">The SystemState of the system in which this object is used.</param>
        public void Update(ref SystemState systemState)
        {
            var safetyHandles = &m_EntityDataAccess->DependencyManager->Safety;
            m_Safety = safetyHandles->GetSafetyHandleForEntityTypeHandle();
        }

        /// <summary>
        /// Reports whether the specified <see cref="Entity"/> instance still refers to a valid entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>True if the entity exists and is valid, and returns false if
        /// the Entity instance refers to an entity that has been destroyed.</returns>
        public bool Exists(Entity entity)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            return m_EntityDataAccess->EntityComponentStore->Exists(entity);
        }

        /// <summary>
        /// Gets an <see cref="EntityStorageInfo"/> for the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <exception cref="System.ArgumentException">Thrown if T is zero-size.</exception>
        public EntityStorageInfo this[Entity entity]
        {
            get
            {
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
                m_EntityDataAccess->EntityComponentStore->AssertEntitiesExist(&entity, 1);

                var entityInChunk = m_EntityDataAccess->EntityComponentStore->GetEntityInChunk(entity);

                return new EntityStorageInfo
                {
                    Chunk = new ArchetypeChunk(entityInChunk.Chunk, m_EntityDataAccess->EntityComponentStore),
                    IndexInChunk = entityInChunk.IndexInChunk
                };
            }
        }
    }
}
