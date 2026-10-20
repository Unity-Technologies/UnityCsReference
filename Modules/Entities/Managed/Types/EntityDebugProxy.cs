// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Jobs.LowLevel.Unsafe;


namespace Unity.Entities
{
    /// <summary>
    /// EntityDebugProxy for an entity, the world isn't explicitly known, so there is some world disambiguation here.
    /// </summary>
    class EntityDebugProxy
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        Entity _Entity;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        World _World;

        public EntityDebugProxy(Entity entity)
        {
            _Entity = entity;
            _World = GetWorld(entity);
        }

        // Used by Entity DebuggerDisplay
        public static string GetDebugName(int index, int version)
        {
            Entity entity = new Entity {Index = index, Version = version};
            return new DebuggerDataAccess(GetWorld(entity)).GetDebugNameWithWorld(entity);
        }

        public static World GetWorld(Entity entity)
        {
            unsafe
            {
                if (!EntityComponentStore.s_entityStore.Data.Exists(entity))
                {
                    return null;
                }

                var entityInChunk = EntityComponentStore.s_entityStore.Data.GetEntityInChunk(entity);
                var chunkData = EntityComponentStore.PerChunkArray.ChunkData[entityInChunk.Chunk];

                var archetype = chunkData.Archetype;
                if (archetype == null)
                {
                    return null;
                }

                var store = archetype->EntityComponentStore;
                if (store == null)
                {
                    return null;
                }

                var sequenceNumber = store->WorldSequenceNumber;

                foreach (var world in World.All)
                {
                    if (sequenceNumber == world.SequenceNumber)
                    {
                        return world;
                    }
                }
            }
            return null;
        }

        public EntityArchetype Archetype => new DebuggerDataAccess(_World).GetArchetype(_Entity);
        public ArchetypeChunk Chunk => new DebuggerDataAccess(_World).GetChunk(_Entity);

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public object[] Components => new DebuggerDataAccess(_World).GetComponents(_Entity);

        public World World => _World;

    }

    /// <summary>
    /// Entity debug proxy when the world is explicitly known
    /// </summary>
    unsafe internal struct Entity_
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        Entity _Entity;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        DebuggerDataAccess _Access;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _IncludeWorldName;

        public static Entity_[] ResolveArray(DebuggerDataAccess access, List<Entity> entities)
        {
            var resolved = new Entity_[entities.Count];
            for (int i = 0; i != resolved.Length; i++)
                resolved[i] = new Entity_(access, entities[i], false);
            return resolved;
        }

        public static Entity_ Null
        {
            get
            {
                Entity_ entity;
                entity._Entity = default;
                entity._Access = new DebuggerDataAccess((EntityComponentStore*)null);
                entity._IncludeWorldName = false;
                return entity;
            }
        }

        public Entity_(World world, Entity entity, bool includeWorldName)
            : this(new DebuggerDataAccess(world), entity, includeWorldName) { }

        public Entity_(EntityComponentStore* store, Entity entity, bool includeWorldName)
            : this(new DebuggerDataAccess(store), entity, includeWorldName) { }

        public Entity_(DebuggerDataAccess access, Entity entity, bool includeWorldName)
        {
            _Entity = entity;
            _Access = access;
            _IncludeWorldName = includeWorldName;
        }
        public override string ToString()
        {
            if (_IncludeWorldName)
                return _Access.GetDebugNameWithWorld(_Entity);
            else
                return _Access.GetDebugNameWithoutWorld(_Entity);
        }

        public EntityArchetype Archetype => _Access.GetArchetype(_Entity);
        public ArchetypeChunk Chunk => _Access.GetChunk(_Entity);

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public object[] Components => _Access.GetComponents(_Entity);
        public World World => _Access.GetWorld();
    }
}
