// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    /// <summary>
    /// An EntityArchetype is a unique combination of component types. The <see cref="EntityManager"/>
    /// uses the archetype to group all entities that have the same sets of components.
    /// </summary>
    /// <remarks>
    /// An entity can change archetype fluidly over its lifespan. For example, when you add or
    /// remove components, the archetype of the affected entity changes.
    ///
    /// An archetype object is not a container; rather it is an identifier to each unique combination
    /// of component types that an application has created at run time, either directly or implicitly.
    ///
    /// You can create archetypes directly using <see cref="EntityManager.CreateArchetype(ComponentType[])"/>.
    /// You also implicitly create archetypes whenever you add or remove a component from an entity. An EntityArchetype
    /// object is an immutable singleton; creating an archetype with the same set of components, either directly or
    /// implicitly, results in the same archetype for a given EntityManager.
    ///
    /// The ECS framework uses archetypes to group entities that have the same structure together. The ECS framework
    /// stores component data in blocks of memory called *chunks*. A given chunk stores only entities having the same
    /// archetype. You can get the EntityArchetype object for a chunk from its <see cref="ArchetypeChunk.Archetype"/>
    /// property.
    ///
    /// Instead of using new EntityArchetype(), use EntityManager.CreateArchetype() to create EntityArchetype values.
    /// </remarks>
    [DebuggerTypeProxy(typeof(EntityArchetypeDebugView))]
    public unsafe struct EntityArchetype : IEquatable<EntityArchetype>
    {
        [NativeDisableUnsafePtrRestriction] internal Archetype* Archetype;

        internal EntityArchetype(Archetype* archetype)
        {
            Archetype = archetype;
        }

        /// <summary>
        /// Reports whether this EntityArchetype instance references a non-null archetype.
        /// </summary>
        /// <value>True, if the archetype is valid.</value>
        public bool Valid => Archetype != null;

        /// <summary>
        /// Compares the archetypes for equality.
        /// </summary>
        /// <param name="lhs">A EntityArchetype object.</param>
        /// <param name="rhs">Another EntityArchetype object.</param>
        /// <returns>True, if these EntityArchetype instances reference the same archetype.</returns>
        public static bool operator==(EntityArchetype lhs, EntityArchetype rhs)
        {
            return lhs.Archetype == rhs.Archetype;
        }

        /// <summary>
        /// Compares the archetypes for inequality.
        /// </summary>
        /// <param name="lhs">A EntityArchetype object.</param>
        /// <param name="rhs">Another EntityArchetype object.</param>
        /// <returns>True, if these EntityArchetype instances reference different archetypes.</returns>
        public static bool operator!=(EntityArchetype lhs, EntityArchetype rhs)
        {
            return lhs.Archetype != rhs.Archetype;
        }

        /// <summary>
        /// Reports whether this EntityArchetype references the same archetype as another object.
        /// </summary>
        /// <param name="compare">The object to compare.</param>
        /// <returns>True, if the compare parameter is a EntityArchetype instance that points to the same
        /// archetype.</returns>
        public override bool Equals(object compare)
        {
            return this == (EntityArchetype)compare;
        }

        /// <summary>
        /// Compares archetypes for equality.
        /// </summary>
        /// <param name="entityArchetype">The EntityArchetype to compare.</param>
        /// <returns>Returns true, if both EntityArchetype instances reference the same archetype.</returns>
        public bool Equals(EntityArchetype entityArchetype)
        {
            return Archetype == entityArchetype.Archetype;
        }

        /// <summary>
        /// Returns the hash of the archetype.
        /// </summary>
        /// <remarks>Two EntityArchetype instances referencing the same archetype return the same hash.</remarks>
        /// <returns>An integer hash code.</returns>
        public override int GetHashCode()
        {
            return (int)Archetype;
        }

        /// <summary>
        /// Construct a string representation of this archetype, for logging or debugging.
        /// </summary>
        /// <returns>A string representation of this archetype.</returns>
        public override string ToString()
        {
            return EntityManager.EntityManagerDebug.GetArchetypeDebugString(Archetype);
        }

        /// <summary>
        /// Gets the types of the components making up this archetype.
        /// </summary>
        /// <remarks>The set of component types in an archetype cannot change; adding components to an entity or
        /// removing components from an entity changes the archetype of that entity (possibly resulting in the
        /// creation of a new archetype). The original archetype remains unchanged.</remarks>
        /// <param name="allocator">The allocation type to use for the returned NativeArray.</param>
        /// <returns>A native array containing the <see cref="ComponentType"/> objects of this archetype.</returns>
        public NativeArray<ComponentType> GetComponentTypes(Allocator allocator = Allocator.Temp)
        {
            return GetComponentTypes((AllocatorManager.AllocatorHandle)allocator);
        }

        /// <summary>
        /// Gets the types of the components making up this archetype.
        /// </summary>
        /// <remarks>The set of component types in an archetype cannot change; adding components to an entity or
        /// removing components from an entity changes the archetype of that entity (possibly resulting in the
        /// creation of a new archetype). The original archetype remains unchanged.</remarks>
        /// <param name="allocator">The allocation type to use for the returned NativeArray.</param>
        /// <returns>A native array containing the <see cref="ComponentType"/> objects of this archetype.</returns>
        public NativeArray<ComponentType> GetComponentTypes(AllocatorManager.AllocatorHandle allocator)
        {
            var archetypeCount = Archetype->TypesCount;
            var types = CollectionHelper.CreateNativeArray<ComponentType>(archetypeCount - 1, allocator);

            // NOTE: Entity is excluded (Entity is always the first type in the archetype)
            for (var i = 1; i < archetypeCount; ++i)
                types[i  - 1] = Archetype->Types[i].ToComponentType();

            return types;
        }

        /// <summary>
        /// Calculates the difference between two archetypes.
        /// Reports what components need to be added to and removed from "before" in order to convert it to "after".
        /// </summary>
        /// <param name="before">First archetype</param>
        /// <param name="after">Second archetype</param>
        /// <param name="addedTypes">Buffer to hold type indices of types present in "after" but
        /// not in "before".  Buffer must be large enough to potentially hold all the types present in "after"</param>
        /// <param name="addedTypesCount">How many types were put into the addedTypes buffer</param>
        /// <param name="removedTypes">Buffer to hold type indices of types present in "before" but
        /// not in "after".  Buffer must be large enough to potentially hold all the types present in "before"</param>
        /// <param name="removedTypesCount">How many types were put into the removedTypes buffer</param>
        public static void CalculateDifference(
            EntityArchetype before, EntityArchetype after,
            TypeIndex* addedTypes, out int addedTypesCount, TypeIndex* removedTypes, out int removedTypesCount)
        {
            int b = 1;
            int a = 1;

            var beforeTypes = before.Archetype->Types;
            var beforeTypesCount = before.Archetype->TypesCount;

            var afterTypes = after.Archetype->Types;
            var afterTypesCount = after.Archetype->TypesCount;

            int addedTypesCounter = 0;
            int removedTypesCounter = 0;

            while (b < beforeTypesCount && a < afterTypesCount)
            {
                if (beforeTypes[b].TypeIndex == afterTypes[a].TypeIndex)
                {
                    a++;
                    b++;
                }
                else if (beforeTypes[b].TypeIndex > afterTypes[a].TypeIndex)
                {
                    addedTypes[addedTypesCounter++] = afterTypes[a].TypeIndex;
                    a++;
                }
                else
                {
                    removedTypes[removedTypesCounter++] = beforeTypes[b].TypeIndex;
                    b++;
                }
            }

            while (a < afterTypesCount)
            {
                addedTypes[addedTypesCounter++] = afterTypes[a].TypeIndex;
                a++;
            }
            while (b < beforeTypesCount)
            {
                removedTypes[removedTypesCounter++] = beforeTypes[b].TypeIndex;
                b++;
            }

            addedTypesCount = addedTypesCounter;
            removedTypesCount = removedTypesCounter;
        }

        /// <summary>
        /// The number of component types this archetype contains.
        /// </summary>
        /// <value>The number of component types.</value>
        public int TypesCount => Archetype->TypesCount;

        /// <summary>
        /// The current number of chunks storing entities having this archetype.
        /// </summary>
        /// <value>The number of chunks.</value>
        /// <remarks>This value can change whenever structural changes occur.
        /// Structural changes include creating or destroying entities, adding components to or removing them from
        /// an entity, and changing the value of shared components, all of which alter where entities are stored.
        /// </remarks>
        public int ChunkCount => Archetype->Chunks.Count;

        /// <summary>
        /// The number of entities having this archetype that can fit into a single chunk of memory.
        /// </summary>
        /// <value>Capacity is determined by the fixed, 16KB size of the memory blocks allocated by the ECS framework
        /// and the total storage size of all the component types in the archetype.</value>
        public int ChunkCapacity => Archetype->ChunkCapacity;

        /// <summary>
        /// Reports whether this EntityArchetype instance describes a Prefab.
        /// </summary>
        /// <value>True, if the archetype is a prefab archetype.</value>
        public bool Prefab => Archetype->Prefab;

        /// <summary>
        /// Reports whether this EntityArchetype instance contains disabled entities.
        /// </summary>
        /// <value>True, if the archetype is a disabled archetype.</value>
        public bool Disabled => Archetype->Disabled;

        /// <summary>
        /// Retrieve the stable hash for this EntityArchetype.
        /// </summary>
        public ulong StableHash => Archetype->StableHash;

        /// <summary>
        /// The component types this archetype contains.
        /// </summary>
        internal ComponentTypeInArchetype* Types => Archetype->Types;

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        internal void CheckValidEntityArchetype()
        {
            if (!Valid)
            {
                throw new ArgumentException("EntityArchetype argument is invalid. Calling new EntityArchetype() produces an invalid value. Call EntityManager.CreateArchetype() to get a valid EntityArchetype value.");
            }
        }
    }
}
