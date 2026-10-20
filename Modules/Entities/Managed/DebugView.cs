// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections.NotBurstCompatible;
using Unity.Core;

namespace Unity.Entities
{
    sealed class ArchetypeChunkDataDebugView
    {
        private ArchetypeChunkData m_ChunkData;

        public ArchetypeChunkDataDebugView(ArchetypeChunkData chunkData)
        {
            m_ChunkData = chunkData;
        }

        public unsafe ArchetypeChunk[] Items
        {
            get
            {
                var result = new ArchetypeChunk[m_ChunkData.Count];
                for (var i = 0; i < result.Length; ++i)
                    result[i] = new ArchetypeChunk(m_ChunkData[i], null);
                return result;
            }
        }
    }

    sealed class UnsafeMatchingArchetypePtrListDebugView
    {
        private UnsafeMatchingArchetypePtrList m_MatchingArchetypeList;

        public UnsafeMatchingArchetypePtrListDebugView(UnsafeMatchingArchetypePtrList MatchingArchetypeList)
        {
            m_MatchingArchetypeList = MatchingArchetypeList;
        }

        public unsafe MatchingArchetype*[] Items
        {
            get
            {
                var result = new MatchingArchetype*[m_MatchingArchetypeList.Length];
                for (var i = 0; i < result.Length; ++i)
                    result[i] = m_MatchingArchetypeList.Ptr[i];
                return result;
            }
        }
    }

    internal class Component_E
    {
        public Component_E(in object value, bool enabled)
        {
            Value = value;
            Enabled = enabled;
        }
        public override string ToString()
        {
            var enabledStr = Enabled ? "Enabled" : "Disabled";
            var valueString = Value.ToString();
            var typeName = Value.GetType().ToString();

            if (valueString == typeName)
                return $"{typeName} ({enabledStr})";
            else
                return $"{typeName} ({enabledStr}) {valueString}";
        }

        public bool Enabled;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public object Value;
    }

    sealed class EntityManagerDebugView
    {
        private EntityManager m_target;
        public EntityManagerDebugView(EntityManager target)
        {
            m_target = target;
        }

        unsafe public Entity_[] Entities
        {
            get
            {
                var query = m_target.UniversalQueryWithSystems._Debugger_GetImpl();
                if (query == null)
                    return null;
                var entity = new List<Entity>();
                if (!query->Debugger_GetData(entity, null))
                    return null;

                var result = Entity_.ResolveArray(new DebuggerDataAccess(m_target.World), entity);
                return result;
            }
        }

        public static string CountAndCapacityViewStr(long count, long capacity, string scope)
        {
            double currentUsage = (double)count / (double)capacity;
            return $"{count} ({currentUsage:P2} {scope} capacity {capacity})";
        }

        public EntityArchetype[] Archetypes
        {
            get
            {
                var entityArchetypes = new NativeList<EntityArchetype>(Allocator.Temp);
                m_target.GetAllArchetypes(entityArchetypes);

                var result = entityArchetypes.ToArrayNBC();
                entityArchetypes.Dispose();
                return result;
            }
        }

        public EntityArchetype[] ArchetypesUsed
        {
            get
            {
                using var entityArchetypes = new NativeList<EntityArchetype>(Allocator.Temp);
                m_target.GetAllArchetypes(entityArchetypes);

                var validArchetypes = new List<EntityArchetype>();
                foreach (var arch in entityArchetypes)
                {
                    if (arch.ChunkCount != 0)
                        validArchetypes.Add(arch);
                }
                return validArchetypes.ToArray();
            }
        }

        public string NumEntities
        {
            get
            {
                var entities = Entities;
                return CountAndCapacityViewStr(entities.Length, EntityComponentStore.k_MaximumEntitiesPerWorld, "World");
            }
        }
        public string NumEntityNames
        {
            get
            {
                return CountAndCapacityViewStr(EntityNameStorage.s_State.Data.entries, EntityNameStorage.kMaxEntries, "Global");
            }
        }
        public World World => m_target.World;
    }

    sealed class WorldDebugView
    {
        private World m_world;
        public WorldDebugView(World world)
        {
            m_world = world;
        }

        public List<SystemDebugView> AllSystems
        {
            get
            {
                var result = new List<SystemDebugView>();
                using var systems = m_world.Unmanaged.GetAllSystems(Allocator.TempJob);
                for (int i = 0; i < systems.Length; i++)
                   result.Add(new SystemDebugView(systems[i]));

                return result;
            }
        }

        public ComponentSystemBase InitializationSystemGroup => m_world.GetExistingSystemManaged<InitializationSystemGroup>();
        public ComponentSystemBase SimulationSystemGroup => m_world.GetExistingSystemManaged<SimulationSystemGroup>();
        public ComponentSystemBase PresentationSystemGroup => m_world.GetExistingSystemManaged<PresentationSystemGroup>();
        public EntityManager EntityManager => m_world.EntityManager;
    }
    sealed class ArchetypeChunkDebugView
    {
        private ArchetypeChunk m_ArchetypeChunk;
        public ArchetypeChunkDebugView(ArchetypeChunk ArchetypeChunk)
        {
            m_ArchetypeChunk = ArchetypeChunk;
        }

        public unsafe EntityArchetype Archetype => new EntityArchetype(m_ArchetypeChunk.Archetype.Archetype);


        public unsafe Entity_[] Entities
        {
            get
            {
                var chunk = m_ArchetypeChunk.m_Chunk;
                if (chunk == ChunkIndex.Null)
                    return null;

                var length = m_ArchetypeChunk.Count;
                var result = new Entity_[length];

                var entityPtr = (Entity*) ChunkIterationUtility.GetChunkComponentDataROPtr(m_ArchetypeChunk.Archetype.Archetype, chunk, 0);
                for (int i = 0; i < length; i++)
                    result[i] = new Entity_(m_ArchetypeChunk.m_EntityComponentStore, entityPtr[i], false);

                return result;
            }
        }

        public unsafe object[] SharedComponents
        {
            get
            {
                var chunk = m_ArchetypeChunk.m_Chunk;
                if (chunk == ChunkIndex.Null)
                    return Array.Empty<object>();

                var archetype = m_ArchetypeChunk.Archetype.Archetype;
                object[] result = new object[archetype->NumSharedComponents];
                var types = archetype->TypesCount;
                int sharedIter = 0;
                for (int i = archetype->FirstSharedComponent, count = archetype->SharedComponentsEnd; i < count; ++i)
                {
                    var componentType = archetype->Types[i];
                    var sharedComponentValues = archetype->Chunks.GetSharedComponentValues(chunk.ListIndex);
                    result[sharedIter] = new DebuggerDataAccess(m_ArchetypeChunk.m_EntityComponentStore).GetSharedComponentDataBoxed(sharedComponentValues[sharedIter], componentType.TypeIndex);
                    sharedIter++;
                }
                return result;
            }
        }

        public unsafe Entity_ ChunkComponent
        {
            get
            {
                var chunk = m_ArchetypeChunk.m_Chunk;
                if (chunk == ChunkIndex.Null)
                    return Entity_.Null;

                return new Entity_(m_ArchetypeChunk.m_EntityComponentStore, m_ArchetypeChunk.m_Chunk.MetaChunkEntity, false);
            }
        }


        public unsafe ComponentType_[] ComponentTypes
        {
            get
            {
                var chunk = m_ArchetypeChunk.m_Chunk;
                if (chunk == ChunkIndex.Null)
                    return null;

                var archetype = m_ArchetypeChunk.Archetype.Archetype;
                ComponentType_[] result = new ComponentType_[archetype->TypesCount];
                for (var i = 0; i < archetype->TypesCount; ++i)
                {
                    int memoryOrderIndexInArchetype = archetype->TypeIndexInArchetypeToMemoryOrderIndex[i];
                    var componentType = archetype->Types[i].ToComponentType();
                    result[i].Type = componentType;
                    result[i].Version = archetype->Chunks.GetChangeVersion(i, chunk.ListIndex);
                    result[i].IsEnableable = TypeManager.IsEnableable(componentType.TypeIndex);
                    result[i].NumDisabledEntitiesInChunk = archetype->Chunks.GetChunkDisabledCountForType(memoryOrderIndexInArchetype, chunk.ListIndex);
                }

                return result;
            }
        }

        public unsafe uint OrderVersion
        {
            get
            {
                var chunk = m_ArchetypeChunk.m_Chunk;
                if (chunk == ChunkIndex.Null)
                    return 0;

                return m_ArchetypeChunk.Archetype.Archetype->Chunks.GetOrderVersion(chunk.ListIndex);
            }
        }
    }

    struct ComponentType_
    {
        public ComponentType Type;
        public uint          Version;
        public bool          IsEnableable;
        public int           NumDisabledEntitiesInChunk;

        public override string ToString()
        {
            if (IsEnableable)
                return $"{Type} {Version} ({NumDisabledEntitiesInChunk} Disabled)";
            else
                return $"{Type} {Version}";
        }
    }

    sealed class ComponentTypeSetDebugView
    {
        private ComponentTypeSet m_ComponentTypeSet;
        public ComponentTypeSetDebugView(in ComponentTypeSet componentTypeSet)
        {
            m_ComponentTypeSet = componentTypeSet;
        }
        public unsafe ComponentType[] ComponentTypes
        {
            get
            {
                var result = new ComponentType[m_ComponentTypeSet.Length];
                for (var i = 0; i < m_ComponentTypeSet.Length; ++i)
                {
                    result[i] = m_ComponentTypeSet.GetComponentType(i);
                }
                return result;
            }
        }
    }

    sealed class EntityArchetypeDebugView
    {
        private EntityArchetype m_EntityArchetype;
        public EntityArchetypeDebugView(EntityArchetype entityArchetype)
        {
            m_EntityArchetype = entityArchetype;
        }

        public unsafe ComponentType[] ComponentTypes
        {
            get
            {
                var archetype = m_EntityArchetype.Archetype;
                if (archetype == null)
                    return null;
                var types = archetype->TypesCount;

                var result = new ComponentType[types];
                for (var i = 0; i < types; ++i)
                {
                    result[i] = archetype->Types[i].ToComponentType();
                }
                return result;
            }
        }

        public struct ChunkReport
        {
            public float AvgUtilization;
            public float WorstUtilization;
            public List<float> PerChunk;

            public override string ToString()
            {
                return $"{AvgUtilization * 100}%";
            }
        }
        public unsafe ChunkReport ChunkUtilization
        {
            get
            {
                var archetype = m_EntityArchetype.Archetype;
                if (archetype == null)
                    return default;

                var numChunks = m_EntityArchetype.ChunkCount;

                if (numChunks <= 0)
                    return default;

                var chunkCapacity = (float)archetype->ChunkCapacity;

                var result = new ChunkReport
                {
                    PerChunk = new List<float>(numChunks),
                    WorstUtilization = archetype->Chunks[0].Count / chunkCapacity
                };

                for (var i = 0; i < numChunks; ++i)
                {
                    var avg = archetype->Chunks[i].Count / chunkCapacity;
                    result.PerChunk.Add(avg);
                    result.AvgUtilization += avg;
                    if (avg < result.WorstUtilization)
                        result.WorstUtilization = avg;
                }

                result.AvgUtilization /= numChunks;
                return result;
            }
        }

        public unsafe List<ArchetypeChunk> ArchetypeChunks
        {
            get
            {
                List<ArchetypeChunk> result = new List<ArchetypeChunk>();
                var archetype = m_EntityArchetype.Archetype;
                if (archetype == null)
                    return null;
                var chunkCount = archetype->Chunks.Count;
                for (int i = 0; i < chunkCount; i++)
                {
                    var srcChunk = archetype->Chunks[i];
                    result.Add(new ArchetypeChunk(srcChunk, m_EntityArchetype.Archetype->EntityComponentStore));
                }
                return result;
            }
        }

        public unsafe int[] Offsets
        {
            get
            {
                var archetype = m_EntityArchetype.Archetype;
                if (archetype == null)
                    return null;
                int[] result = new int[archetype->TypesCount];
                Marshal.Copy((IntPtr)archetype->Offsets, result, 0, archetype->TypesCount);
                return result;
            }
        }

        public unsafe int[] SizeOfs
        {
            get
            {
                var archetype = m_EntityArchetype.Archetype;
                if (archetype == null)
                    return null;
                int[] result = new int[archetype->TypesCount];
                Marshal.Copy((IntPtr)archetype->SizeOfs, result, 0, archetype->TypesCount);
                return result;
            }
        }

        public unsafe ComponentType[] TypesInMemoryOrder
        {
            get
            {
                var archetype = m_EntityArchetype.Archetype;
                if (archetype == null)
                    return Array.Empty<ComponentType>();
                var result = new ComponentType[archetype->TypesCount];
                for (int typeMemoryOrderIndex = 0; typeMemoryOrderIndex < archetype->TypesCount; ++typeMemoryOrderIndex)
                {
                    int indexInArchetype = archetype->TypeMemoryOrderIndexToIndexInArchetype[typeMemoryOrderIndex];
                    result[typeMemoryOrderIndex] = archetype->Types[indexInArchetype].ToComponentType();
                }
                return result;
            }
        }
        public unsafe Dictionary<TypeIndex,int> TypeMemoryOrderIndex
        {
            get
            {
                var archetype = m_EntityArchetype.Archetype;
                if (archetype == null)
                    return new Dictionary<TypeIndex, int>(0);
                var result = new Dictionary<TypeIndex, int>(archetype->TypesCount);
                for (int indexInArchetype = 0; indexInArchetype < archetype->TypesCount; ++indexInArchetype)
                {
                    result[archetype->Types[indexInArchetype].TypeIndex] =
                        archetype->TypeIndexInArchetypeToMemoryOrderIndex[indexInArchetype];
                }
                return result;
            }
        }
    }

    sealed class EntityQueryDebugView
    {
        private EntityQuery m_EntityQuery;

        public EntityQueryDesc[] Desc
        {
            get
            {
                unsafe
                {
                    var impl = m_EntityQuery._Debugger_GetImpl();
                    return impl == null ? default : impl->GetEntityQueryDescs();
                }
            }
        }

        public unsafe Entity_[] MatchingEntities
        {
            get
            {
                var impl = m_EntityQuery._Debugger_GetImpl();
                if (impl == null)
                    return null;

                var entity = new List<Entity>();
                if (!impl->Debugger_GetData(entity, null))
                    return null;

                return Entity_.ResolveArray(new DebuggerDataAccess(impl->_Access->EntityComponentStore), entity);
            }
        }

        public unsafe List<ArchetypeChunk> MatchingChunks
        {
            get
            {
                var impl = m_EntityQuery._Debugger_GetImpl();
                if (impl == null)
                    return null;

                var chunks = new List<ArchetypeChunk>();
                if (!impl->Debugger_GetData(null, chunks))
                    return null;
                return chunks;
            }
        }

        public unsafe EntityQueryFilter.ChangedFilter ChangedFilter
        {
            get
            {
                var impl = m_EntityQuery._GetImpl();
                return impl == null ? default : impl->_Filter.Changed;
            }
        }

        public unsafe object[] SharedFilter
        {
            get
            {
                var impl = m_EntityQuery._GetImpl();
                if (impl == null)
                    return default;
                int numFilterValues = impl->_Filter.Shared.Count;
                object[] result = new object[numFilterValues];
                for (var i = 0; i < numFilterValues; ++i)
                {
                    int valueIndex = impl->_Filter.Shared.SharedComponentIndex[i];
                    int indexInQuery = impl->_Filter.Shared.IndexInEntityQuery[i];
                    var typeIndex = impl->_QueryData->RequiredComponents[indexInQuery].TypeIndex;
                    result[i] = new DebuggerDataAccess(impl->_Access->EntityComponentStore).GetSharedComponentDataBoxed(valueIndex, typeIndex);
                }
                return result;
            }
        }

        public EntityQueryDebugView(EntityQuery query)
        {
            m_EntityQuery = query;
        }
    }

    class SystemDebugView
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly World               m_World;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly SystemHandle m_unmanagedSystem;

        public SystemDebugView(SystemHandle mUnmanagedSystem)
        {
            m_unmanagedSystem = mUnmanagedSystem;

            m_World = null;
            foreach (var w in World.s_AllWorlds)
            {
                if (w.SequenceNumber == m_unmanagedSystem.m_WorldSeqNo)
                    m_World = w;
            }
        }

        unsafe public SystemDebugView(ComponentSystemBase mManagedSystem)
        {
            if (mManagedSystem != null && mManagedSystem.World != null && mManagedSystem.World.IsCreated)
            {
                m_World = mManagedSystem.World;
                m_unmanagedSystem = mManagedSystem.CheckedState()->SystemHandle;
            }
            else
            {
                m_unmanagedSystem = default;
                m_World = null;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        unsafe public object UserData
        {
            get
            {
                var state = m_World.Unmanaged.ResolveSystemState(m_unmanagedSystem);
                if (state->m_SystemPtr != null)
                {
                    var type = m_World.Unmanaged.GetTypeOfSystem(m_unmanagedSystem);
                    if (type == null)
                        return null;

                    var obj = Activator.CreateInstance(type);
                    if (obj == null)
                        return null;

                    var gcHandle = GCHandle.Alloc(obj, GCHandleType.Pinned);
                    UnsafeUtility.MemCpy((void*)gcHandle.AddrOfPinnedObject(), state->m_SystemPtr, Marshal.SizeOf(type));
                    gcHandle.Free();

                    return obj;
                }
                else
                {
                    return state->ManagedSystem;
                }
            }
        }

        unsafe public SystemState_ SystemState
        {
            get
            {
                if (m_World == null)
                    return null;

                return SystemState_.FromPointer(m_World.Unmanaged.ResolveSystemState(m_unmanagedSystem));
            }
        }

        public Type Type
        {
            get
            {
                if (m_World == null)
                    return null;
                return m_World.Unmanaged.GetTypeOfSystem(m_unmanagedSystem);
            }
        }

        public override string ToString()
        {
            if (m_World == null)
                return "World has been disposed";

            var type = m_World.Unmanaged.GetTypeOfSystem(m_unmanagedSystem);
            if (type == null)
                return "System has been disposed";
            return type.Name;
        }
    }

    sealed class ComponentSystemGroupDebugView
    {
        private ComponentSystemGroup m_componentSystemGroup;

        public ComponentSystemGroupDebugView(ComponentSystemGroup mComponentSystemGroup)
        {
            m_componentSystemGroup = mComponentSystemGroup;
        }

        public SystemDebugView[] Systems
        {
            get
            {
                var numSystems = m_componentSystemGroup.m_MasterUpdateList.Length;
                var systems = new SystemDebugView[numSystems];

                for (int i = 0; i < numSystems; i++)
                {
                    var updateIndex = m_componentSystemGroup.m_MasterUpdateList[i];
                    if (updateIndex.IsManaged)
                    {
                        systems[i] = new SystemDebugView(m_componentSystemGroup.m_managedSystemsToUpdate[updateIndex.Index]);
                    }
                    else
                    {
                        systems[i] = new SystemDebugView(m_componentSystemGroup.m_UnmanagedSystemsToUpdate[updateIndex.Index]);
                    }
                }

                return systems;
            }
        }

        public bool Enabled => m_componentSystemGroup.Enabled;
        public bool EnableSystemSorting => m_componentSystemGroup.EnableSystemSorting;
        public World World => m_componentSystemGroup.World;
    }

    sealed unsafe class SystemState_
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        SystemHandle _SystemHandle;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        World               _World;

        public SystemState_(ref SystemState systemState)
        {
            _SystemHandle = systemState.SystemHandle;
            _World = systemState.World;
        }

        public static SystemState_ FromPointer(SystemState* systemState)
        {
            if (systemState != null)
                return new SystemState_(ref *systemState);
            else
                return null;
        }

        public EntityManager EntityManager => _World.EntityManager;

        static string GetName(SystemHandle handle, World world)
        {
            if (world == null || !world.IsCreated)
                return null;

            var state = world.Unmanaged.ResolveSystemState(handle);
            if (state == null)
                return null;

            return state->DebugName.ToString();
        }

        //NOTE: Dependency property is specifically not exposed since the getter modifies state.

        public SystemDebugView Data => new SystemDebugView(_SystemHandle);

        public TimeData Time => Resolve()->World.Time;
        public uint LastSystemVersion => Resolve()->LastSystemVersion;
        public uint GlobalSystemVersion => Resolve()->GlobalSystemVersion;

        public bool Enabled => Resolve()->Enabled;
        private bool RequireMatchingQueriesForUpdate => Resolve()->RequireMatchingQueriesForUpdate;

        SystemState* Resolve()
        {
            if (_World == null || !_World.IsCreated)
                throw new NullReferenceException("World is null");

            var state = _World.Unmanaged.ResolveSystemState(_SystemHandle);
            if (state == null)
                throw new NullReferenceException("SystemState is null");
            return state;
        }

        public List<EntityQuery> EntityQueries
        {
            get
            {
                var unsafeQueries = Resolve()->EntityQueries;
                var result = new List<EntityQuery>();
                foreach(var query in unsafeQueries)
                    result.Add(query);

                return result;
            }
        }
        public List<ComponentType> ReadOnlyTypes
        {
            get
            {
                var result = new List<ComponentType>();
                foreach (var type in Resolve()->m_JobDependencyForReadingSystems)
                    result.Add(ComponentType.ReadOnly(type));

                return result;
            }
        }

        public List<ComponentType> ReadWriteTypes
        {
            get
            {
                var result = new List<ComponentType>();
                foreach (var type in Resolve()->m_JobDependencyForWritingSystems)
                    result.Add(ComponentType.ReadWrite(type));

                return result;
            }
        }


        public override string ToString()
        {
            return GetName(_SystemHandle, _World);
        }
    }
}
