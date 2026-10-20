// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;

namespace Unity.Entities
{
    /// <summary>
    /// The <see cref="EntityManagerDiffer"/> is used to efficiently track changes to a given world over time.
    /// </summary>
    public struct EntityManagerDiffer : IDisposable
    {
        // Built per call: the ComponentType holds a TypeIndex, which changes when TypeManager initializes again.
        internal static EntityQueryDesc EntityGuidQueryDesc => new EntityQueryDesc
        {
            All = new ComponentType[]
            {
                typeof(EntityGuid)
            },
            Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab
        };

        World m_ShadowWorld;

        EntityDiffer.CachedComponentChanges m_CachedComponentChanges;
        EntityManager m_SourceEntityManager;
        EntityManager m_ShadowEntityManager;
        EntityQueryDesc m_EntityQueryDesc;
        BlobAssetCache m_BlobAssetCache;

        internal EntityManager ShadowEntityManager => m_ShadowEntityManager;

        /// <summary>
        /// Constructs a new EntityManagerDiffer object.
        /// </summary>
        /// <param name="sourceEntityManager">The EntityManager to associate with this differ.</param>
        /// <param name="allocator">The allocator to use for any internal memory allocations for this object.</param>
        /// <param name="entityQueryDesc">If non-null, the differ limits its change-tracking to the entities that this query matches.</param>
        public EntityManagerDiffer(EntityManager sourceEntityManager, AllocatorManager.AllocatorHandle allocator, EntityQueryDesc entityQueryDesc = null)
        {
            m_CachedComponentChanges = new EntityDiffer.CachedComponentChanges(1024);
            m_SourceEntityManager = sourceEntityManager;

            m_EntityQueryDesc = entityQueryDesc ?? EntityGuidQueryDesc;
            m_ShadowWorld = new World(sourceEntityManager.World.Name + " (Shadow)", sourceEntityManager.World.Flags | WorldFlags.Shadow);
            m_ShadowEntityManager = m_ShadowWorld.EntityManager;
            m_BlobAssetCache = new BlobAssetCache(allocator);
        }

        /// <summary>
        /// Disposes an EntityManagerDiffer object.
        /// </summary>
        public void Dispose()
        {
            m_CachedComponentChanges.Dispose();
            m_SourceEntityManager = default;

            if (m_ShadowWorld != null && m_ShadowWorld.IsCreated)
                m_ShadowWorld.Dispose();

            m_BlobAssetCache.Dispose();
            m_ShadowWorld = null;
            m_ShadowEntityManager = default;
            m_EntityQueryDesc = null;
        }

        /// <summary>
        /// Generates a detailed change set for the world.
        /// All entities to be considered for diffing must have the <see cref="EntityGuid"/> component with a unique value.
        /// </summary>
        /// <remarks>
        /// The resulting <see cref="EntityChanges"/> must be disposed when no longer needed.
        /// </remarks>
        /// <param name="options">A set of options which can be toggled.</param>
        /// <param name="allocator">The allocator to use for the results object.</param>
        /// <returns>A set of changes for the world since the last fast-forward.</returns>
        public EntityChanges GetChanges(EntityManagerDifferOptions options, AllocatorManager.AllocatorHandle allocator)
        {
            var changes = EntityDiffer.GetChanges(
                ref m_CachedComponentChanges,
                srcEntityManager: m_SourceEntityManager,
                dstEntityManager: m_ShadowEntityManager,
                options,
                m_EntityQueryDesc,
                m_BlobAssetCache,
                allocator);

            return changes;
        }
    }
}
