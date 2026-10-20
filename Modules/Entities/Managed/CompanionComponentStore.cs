// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Scripting.LifecycleManagement;

namespace Unity.Entities
{
    internal unsafe partial class CompanionComponentStore
    {
        readonly EntityComponentStore* m_EntityComponentStore;

        internal delegate void RepointCompanionComponentsDelegate(Entity* dstEntities, UnityEngine.EntityId* dstComponentLinkIds, int instanceCount, EntityComponentStore* entityComponentStore);
        [AutoStaticsCleanupOnCodeReload]
        internal static RepointCompanionComponentsDelegate RepointCompanionComponents;

        internal delegate void AssignCompanionComponentsToCompanionGameObjectsDelegate(EntityManager entityManager, NativeArray<Entity> entities);
        [AutoStaticsCleanupOnCodeReload]
        internal static AssignCompanionComponentsToCompanionGameObjectsDelegate AssignCompanionComponentsToCompanionGameObjects;

        internal delegate void DestroyCompanionGameObjectsDelegate(UnityEngine.EntityId* companionLinkIds, int count);
        [AutoStaticsCleanupOnCodeReload]
        internal static DestroyCompanionGameObjectsDelegate DestroyCompanionGameObjects;

        private sealed class CompanionComponentStoreKeyContext
        {
        }

        private sealed class CompanionLinkTypeIndexStatic
        {
            public static readonly SharedStatic<TypeIndex> Ref = SharedStatic<TypeIndex>.GetOrCreate<CompanionComponentStoreKeyContext, CompanionLinkTypeIndexStatic>();
        }

        public static TypeIndex CompanionLinkTypeIndex
        {
            get => CompanionLinkTypeIndexStatic.Ref.Data;
            set => CompanionLinkTypeIndexStatic.Ref.Data = value;
        }

        private sealed class CompanionLinkTransformTypeIndexStatic
        {
            public static readonly SharedStatic<TypeIndex> Ref = SharedStatic<TypeIndex>.GetOrCreate<CompanionComponentStoreKeyContext, CompanionLinkTransformTypeIndexStatic>();
        }

        public static TypeIndex CompanionLinkTransformTypeIndex
        {
            get => CompanionLinkTransformTypeIndexStatic.Ref.Data;
            set => CompanionLinkTransformTypeIndexStatic.Ref.Data = value;
        }

        public CompanionComponentStore(EntityComponentStore* entityComponentStore)
        {
            m_EntityComponentStore = entityComponentStore;
        }

        public void Dispose()
        {
            if (CompanionLinkTypeIndex != TypeIndex.Null)
            {
                var archetypes = m_EntityComponentStore->m_Archetypes;
                for (var a = 0; a < archetypes.Length; a++)
                {
                    var archetype = archetypes.Ptr[a];
                    var linkIndex = ChunkDataUtility.GetIndexInTypeArray(archetype, CompanionLinkTypeIndex);
                    if (linkIndex == -1)
                        continue;
                    for (int c = 0; c < archetype->Chunks.Count; c++)
                    {
                        var chunk = archetype->Chunks[c];
                        var linkPtr = (UnityEngine.EntityId*)ChunkDataUtility.GetComponentDataRO(chunk, archetype, 0, linkIndex);
                        DestroyCompanionGameObjects(linkPtr, chunk.Count);
                    }
                }
            }
        }

        public void Playback(ref CompanionDeferredCommands companionDeferredCommands)
        {
            var reader = new UnsafeAppendBuffer.Reader(ref companionDeferredCommands.CommandBuffer);
            while (!reader.EndOfBuffer)
            {
                var cmd = reader.ReadNext<int>();
                switch ((CompanionDeferredCommands.Command)cmd)
                {
                    case (CompanionDeferredCommands.Command.RepointCompanionComponents):
                    {
                        var entities = (Entity*)reader.ReadNextArray<Entity>(out var instanceCount);
                        var dstComponentLinkIds = (UnityEngine.EntityId*)reader.ReadNextArray<UnityEngine.EntityId>(out _);

                        // This command is only queued for entities with a CompanionLink, which only exists once
                        // the Hybrid assembly that assigns this delegate is loaded, so the delegate is set here.
                        RepointCompanionComponents(entities, dstComponentLinkIds, instanceCount, m_EntityComponentStore);
                    }
                    break;

                    case (CompanionDeferredCommands.Command.DestroyCompanionGameObjects):
                    {
                        var companionLinkIds = (UnityEngine.EntityId*)reader.ReadNextArray<UnityEngine.EntityId>(out var count);
                        DestroyCompanionGameObjects(companionLinkIds, count);
                    }
                    break;
                }
            }

            companionDeferredCommands.Reset();
        }
    }
}
