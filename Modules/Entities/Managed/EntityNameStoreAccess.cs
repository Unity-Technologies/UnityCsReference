// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Unity.Entities
{
    internal unsafe struct EntityNameStoreAccessData
    {
        public ulong m_NameChangeBitsSequenceNum;
    }

    internal unsafe struct EntityNameStoreAccess : IDisposable
    {
        public const ulong InitialNameChangeBitsSequenceNum = 1;
        public ulong NameChangeBitsSequenceNum => m_Data->m_NameChangeBitsSequenceNum;

        private UnsafeHashSet<Entity> m_EntitiesWithNames;
        [NativeDisableUnsafePtrRestriction]
        private EntityNameStoreAccessData* m_Data;

        public EntityNameStoreAccess(EntityComponentStore* componentStore)
        {
            m_Data = Memory.Unmanaged.Allocate<EntityNameStoreAccessData>(Allocator.Persistent);
            m_Data->m_NameChangeBitsSequenceNum = InitialNameChangeBitsSequenceNum;
            m_EntitiesWithNames = new UnsafeHashSet<Entity>(1000, Allocator.Persistent);
        }

        public bool IsCreated => m_Data != null;

        public void Dispose()
        {
            Memory.Unmanaged.Free(m_Data, Allocator.Persistent);
            m_EntitiesWithNames.Dispose();

            m_Data = null;
        }

        public ulong IncNameChangeBitsVersion()
        {
            m_Data->m_NameChangeBitsSequenceNum++;
            return m_Data->m_NameChangeBitsSequenceNum;
        }

        public void SetNameChangeBitsVersion(ulong nameChangeBitsVersion)
        {
            m_Data->m_NameChangeBitsSequenceNum = nameChangeBitsVersion;
        }

        public int CountEntitiesWithNames()
        {
            return m_EntitiesWithNames.Count;
        }

        public void ResetEntitiesWithNames()
        {
            m_EntitiesWithNames.Clear();
        }

        public void AddEntityToEntitiesWithNames(Entity entity)
        {
            m_EntitiesWithNames.Add(entity);
        }


        public void RemoveEntitiesFromEntitiesWithNames(Entity* entities, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                m_EntitiesWithNames.Remove(entities[i]);
            }
        }

        public UnsafeHashSet<Entity>.ReadOnly GetEntitiesWithNamesRO()
        {
            return m_EntitiesWithNames.AsReadOnly();
        }

    }
}
