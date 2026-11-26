// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.Entities
{
    // The Object state of a component that the engine shows as a data component: everything an Object holds outside its
    // serialized data. Native mirrors it field for field as GlobalEntityManager::ComponentObjectState, so the fields go
    // largest first and the layout is the same in both languages.
    [VisibleToOtherModules("UnityEngine.CoreModule")]
    internal struct ComponentObjectState
    {
        public long FileIDHint;
        public EntityId CorrespondingSourceObject;
        public EntityId PrefabInstance;
        public EntityId PrefabAsset;
        public uint DirtyIndex;
        // UnityEngine.HideFlags values
        public int HideFlags;
        public byte TemporaryFlags;

        public bool IsDefault =>
            HideFlags == 0
            && TemporaryFlags == 0
            && FileIDHint == 0
            && DirtyIndex == 0
            && CorrespondingSourceObject == EntityId.None
            && PrefabInstance == EntityId.None
            && PrefabAsset == EntityId.None
            ;
    }

    internal struct ComponentObjectStateKey : IEquatable<ComponentObjectStateKey>
    {
        public Entity Entity;
        public TypeIndex TypeIndex;

        public ComponentObjectStateKey(Entity entity, TypeIndex typeIndex)
        {
            Entity = entity;
            TypeIndex = typeIndex;
        }

        public bool Equals(ComponentObjectStateKey other) => Entity == other.Entity && TypeIndex == other.TypeIndex;

        public override int GetHashCode() => (Entity.GetHashCode() * 397) ^ TypeIndex.Value;
    }

    // Sparse: only components whose Object state isn't the default have an entry. An entry goes with its component:
    // removing the component or destroying the entity drops it, instantiating copies it, and moving the entity to another
    // world moves it. The callers check that the entity has the component.
    internal unsafe partial struct EntityComponentStore
    {
        void CreateComponentObjectStates()
        {
            m_ComponentObjectStates = new UnsafeHashMap<ComponentObjectStateKey, ComponentObjectState>(0, Allocator.Persistent);
        }

        void DisposeComponentObjectStates()
        {
            m_ComponentObjectStates.Dispose();
        }

        public ComponentObjectState GetComponentObjectState(Entity entity, TypeIndex typeIndex)
        {
            m_ComponentObjectStates.TryGetValue(new ComponentObjectStateKey(entity, typeIndex), out var state);
            return state;
        }

        public bool HasComponentObjectState(Entity entity, TypeIndex typeIndex) =>
            m_ComponentObjectStates.ContainsKey(new ComponentObjectStateKey(entity, typeIndex));

        // A state at the defaults removes the entry
        public void SetComponentObjectState(Entity entity, TypeIndex typeIndex, in ComponentObjectState state)
        {
            var key = new ComponentObjectStateKey(entity, typeIndex);
            if (state.IsDefault)
                m_ComponentObjectStates.Remove(key);
            else
                m_ComponentObjectStates[key] = state;
        }

        // Drops the entries of the types in srcArchetype that dstArchetype doesn't have; all of them when it is null
        void RemoveComponentObjectStates(Entity* entities, int count, Archetype* srcArchetype, Archetype* dstArchetype)
        {
            if (m_ComponentObjectStates.IsEmpty)
                return;

            for (var t = 1; t < srcArchetype->TypesCount; ++t)
            {
                var typeIndex = srcArchetype->Types[t].TypeIndex;
                if (dstArchetype != null && ChunkDataUtility.GetIndexInTypeArray(dstArchetype, typeIndex) != -1)
                    continue;
                for (var i = 0; i < count; ++i)
                {
                    m_ComponentObjectStates.Remove(new ComponentObjectStateKey(entities[i], typeIndex));
                }
            }
        }

        // Copies srcEntity's entries, in srcStore, to dstEntities in this store for the types in dstArchetype
        public void CopyComponentObjectStates(EntityComponentStore* srcStore, Entity srcEntity, Entity* dstEntities, int count, Archetype* dstArchetype)
        {
            if (srcStore->m_ComponentObjectStates.IsEmpty)
                return;

            for (var t = 1; t < dstArchetype->TypesCount; ++t)
            {
                var typeIndex = dstArchetype->Types[t].TypeIndex;
                if (!srcStore->m_ComponentObjectStates.TryGetValue(new ComponentObjectStateKey(srcEntity, typeIndex), out var state))
                    continue;
                for (var i = 0; i < count; ++i)
                {
                    m_ComponentObjectStates[new ComponentObjectStateKey(dstEntities[i], typeIndex)] = state;
                }
            }
        }

        // Moves the entries of srcEntities, in srcStore, to dstEntities in this store
        internal void MoveComponentObjectStates(EntityComponentStore* srcStore, Entity* srcEntities, Entity* dstEntities, int count, Archetype* srcArchetype)
        {
            if (srcStore->m_ComponentObjectStates.IsEmpty)
                return;

            for (var t = 1; t < srcArchetype->TypesCount; ++t)
            {
                var typeIndex = srcArchetype->Types[t].TypeIndex;
                for (var i = 0; i < count; ++i)
                {
                    var srcKey = new ComponentObjectStateKey(srcEntities[i], typeIndex);
                    if (!srcStore->m_ComponentObjectStates.TryGetValue(srcKey, out var state))
                        continue;
                    srcStore->m_ComponentObjectStates.Remove(srcKey);
                    m_ComponentObjectStates[new ComponentObjectStateKey(dstEntities[i], typeIndex)] = state;
                }
            }
        }
    }
}
