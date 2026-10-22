// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Assertions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using Assert = UnityEngine.Assertions.AssertCore;

namespace Unity.Entities
{
    /// <summary>
    /// This component is attached to converted Entities and is guaranteed to be unique within a World. It can be used
    /// to map back to the authoring GameObject from which it was converted. Note that an EntityGuid does not have
    /// enough information to be persistent across sessions.
    /// </summary>
    [Serializable]
    public struct EntityGuid : IComponentData, IEquatable<EntityGuid>, IComparable<EntityGuid>
    {
        // ReSharper disable InconsistentNaming
        /// <summary>This field, when combined with `b`, is for working with EntityGuid as opaque bits (the packing may
        /// change again in the future, as there are still unused bits remaining).</summary>
        [Obsolete("'a' is obsolete, it is now split up into the fields: 'OriginatingEntityId' and 'OriginatingSubEntityId'.", true)]
        public ulong a { get => throw new InvalidOperationException("a is obsolete."); set => throw new InvalidOperationException("a is obsolete."); }
        /// <summary>Use same as `a` field.</summary>
        [Obsolete("'b' is obsolete, please use 'FullNamespaceId' and 'Serial' instead.", true)]
        public ulong b { get => throw new InvalidOperationException("b is obsolete."); set => throw new InvalidOperationException("b is obsolete."); }
        // ReSharper restore InconsistentNaming

        //Backing fields for the public properties
        [SerializeField] internal EntityId _OriginatingEntityId;
        [SerializeField] internal EntityId _OriginatingSubEntityId;

        /// <summary>Static value that represents an invalid EntityGuid.</summary>
        public static readonly EntityGuid Null = new EntityGuid();

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityGuid"/>.
        /// </summary>
        /// <param name="originatingId">Session-unique ID for the originating object. This is typically the authoring GameObject's InstanceID.</param>
        /// <param name="originatingSubId">Secondary session-unique ID for the originating object. This is typically the authoring Component's InstanceID.</param>
        /// <param name="namespaceId">A unique number to identify the namespace</param>
        /// <param name="serial">A unique number used to differentiate Entities associated with the same originating object and namespace</param>
        [Obsolete("EntityGuid(int originatingId, int originatingSubId, uint namespaceId, uint serial) is obsolete please use EntityGuid(EntityId originatingId, EntityId originatingSubId, uint namespaceId, uint serial) instead.", true)]
        public EntityGuid(int originatingId, int originatingSubId, uint namespaceId, uint serial) => throw new InvalidOperationException("EntityGuid(int originatingId, int originatingSubId, uint namespaceId, uint serial), this constructor is obsolete, use the EntityId version.");


        /// <summary>
        /// Initializes a new instance of the <see cref="EntityGuid"/>.
        /// </summary>
        /// <param name="originatingId">Session-unique ID for the originating object. This is typically the authoring GameObject's EntityId.</param>
        /// <param name="originatingSubId">Secondary session-unique ID for the originating object. This is typically the authoring Component's EntityId.</param>
        /// <param name="namespaceId">A unique number to identify the namespace</param>
        /// <param name="serial">A unique number used to differentiate Entities associated with the same originating object and namespace</param>
        public EntityGuid(EntityId originatingId, EntityId originatingSubId, uint namespaceId, uint serial)
        {
            _OriginatingEntityId = originatingId;
            _OriginatingSubEntityId = originatingSubId;
            FullNamespaceId = namespaceId;
            Serial = serial;
        }

        /// <summary>Session-unique ID for originating object (typically the authoring GameObject's InstanceID).</summary>
        [Obsolete("OriginatingId is deprecated, use OriginatingEntityId instead.", true)]
        public int OriginatingId{get => throw new InvalidOperationException("OriginatingId is obsolete, use OriginatingEntityId instead."); set => throw new InvalidOperationException("OriginatingId is obsolete, use OriginatingEntityId instead."); }

        /// <summary>Session-unique ID for originating object (typically the authoring GameObject's EntityId).</summary>
        public EntityId OriginatingEntityId
        {
            get => _OriginatingEntityId;
            set => _OriginatingEntityId = value;
        }

        /// <summary>Secondary session-unique ID for the originating object. This is typically the authoring Component's InstanceID.</summary>
        [Obsolete("OriginatingSubId is deprecated, use OriginatingSubEntityId", true)]
        public int OriginatingSubId{get => throw new InvalidOperationException("OriginatingSubId is obsolete, use OriginatingSubEntityId instead."); set => throw new InvalidOperationException("OriginatingSubId is obsolete, use OriginatingSubEntityId instead."); }

        /// <summary>Secondary session-unique ID for the originating object. This is typically the authoring Component's EntityId.</summary>
        public EntityId OriginatingSubEntityId
        {
            get => _OriginatingSubEntityId;
            set => _OriginatingSubEntityId = value;
        }

        /// <summary>A unique number to identify the namespace.</summary>
        internal uint FullNamespaceId { get; set; }

        /// <summary>A unique number used to differentiate Entities associated with the same originating object and namespace.</summary>
        public uint Serial { get; set; }

        /// <summary>A unique number combining the FullNamespaceId and Serial into a ulong.</summary>
        public ulong NamespaceAndSerial => (((ulong)FullNamespaceId << 32) | (Serial));

        /// <summary>
        /// Checks if two EntityGuid instances are equal.
        /// </summary>
        /// <param name="lhs">An EntityGuid</param>
        /// <param name="rhs">Another EntityGuid</param>
        /// <returns>True, if both EntityGuid instances contain the same opaque bits.</returns>
        public static bool operator==(in EntityGuid lhs, in EntityGuid rhs) => lhs.Equals(rhs);
        /// <summary>
        /// Checks if two EntityGuid instances aren't equal.
        /// </summary>
        /// <param name="lhs">An EntityGuid</param>
        /// <param name="rhs">Another EntityGuid</param>
        /// <returns>True, if any of the opaque bits contained in the EntityGuid instances is different.</returns>
        public static bool operator!=(in EntityGuid lhs, in EntityGuid rhs) => !(lhs.Equals(rhs));

        /// <summary>
        /// Checks if two objects are EntityGuid instances and if they are equal.
        /// </summary>
        /// <param name="obj">An object</param>
        /// <returns>True if <paramref name="obj"/> is an `EntityGuid` instance that contains the same opaque bits.</returns>
        public override bool Equals(object obj) => obj is EntityGuid guid && Equals(guid);

        /// <summary>
        /// Checks if two EntityGuid instances are equal.
        /// </summary>
        /// <param name="other">An EntityGuid to compare with</param>
        /// <returns>True if <paramref name="other"/> contains the same opaque bits.</returns>
        public bool Equals(EntityGuid other)
        {
            return OriginatingEntityId == other.OriginatingEntityId &&
                   OriginatingSubEntityId == other.OriginatingSubEntityId &&
                   FullNamespaceId == other.FullNamespaceId &&
                   Serial == other.Serial;
        }


        /// <summary>
        /// Computes a hashcode to support hash-based collections.
        /// </summary>
        /// <returns>The computed hash.</returns>
        public override int GetHashCode()
        {
            int hashCode = OriginatingEntityId.GetHashCode();
            hashCode = (hashCode * 397) ^ OriginatingSubEntityId.GetHashCode();
            hashCode = (hashCode * 397) ^ FullNamespaceId.GetHashCode();
            hashCode = (hashCode * 397) ^ Serial.GetHashCode();
            return hashCode;
        }

        /// <summary>
        /// Compares an EntityGuid against a given one to find their sort order.
        /// </summary>
        /// <param name="other">The other EntityGuid to compare to</param>
        /// <returns>Returns -1 if this EntityGuid goes first in the sort order, or 1 if the other should goes first in the sort order. Otherwise returns 0 if they are equal.</returns>
        public int CompareTo(EntityGuid other)
        {
            if (OriginatingEntityId != other.OriginatingEntityId)
            {
                return EntityId.ToULong(OriginatingEntityId) > EntityId.ToULong(other.OriginatingEntityId) ? 1 : -1;
            }

            if (OriginatingSubEntityId != other.OriginatingSubEntityId)
            {
                return EntityId.ToULong(OriginatingSubEntityId) > EntityId.ToULong(other.OriginatingSubEntityId) ? 1 : -1;
            }

            if (FullNamespaceId != other.FullNamespaceId)
            {
                return FullNamespaceId > other.FullNamespaceId ? 1 : -1;
            }

            if (Serial != other.Serial)
            {
                return Serial > other.Serial ? 1 : -1;
            }

            return 0;
        }

        /// <summary>
        /// Converts this EntityGuid to a standard C# <see cref="string"/> representation.
        /// </summary>
        /// <returns>The C# string.</returns>
        public override string ToString() => $"{EntityId.ToULong(OriginatingEntityId)}:{EntityId.ToULong(OriginatingSubEntityId)}:{FullNamespaceId:x8}:{Serial:x8}";
    }

    /// <summary>
    ///Contains all the changes needed to convert and revert a world from one state to another.
    /// </summary>
    /// <remarks>Changes that convert a world from one state to another are called **forward changes**.
    /// Changes that revert the world from one state to another are called **reverse changes**. EntityChanges
    /// store the changes in unmanaged memory. Call <see cref="Dispose"/> to free the used memory</remarks>
    public readonly struct EntityChanges : IDisposable
    {
        readonly EntityChangeSet m_ForwardChangeSet;
        readonly EntityChangeSet m_ReverseChangeSet;

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityChanges"/>.
        /// </summary>
        /// <param name="forwardChangeSet">Changes to convert a World from a state 'A' to a state 'B'</param>
        /// <param name="reverseChangeSet">Changes to revert a World from a state 'B' to a state 'A'</param>
        public EntityChanges(EntityChangeSet forwardChangeSet, EntityChangeSet reverseChangeSet)
        {
            m_ForwardChangeSet = forwardChangeSet;
            m_ReverseChangeSet = reverseChangeSet;
        }

        /// <summary>
        /// Checks if there is any forward or reverse change
        /// </summary>
        /// <returns>True if there is any change.</returns>
        public bool AnyChanges => HasForwardChangeSet || HasReverseChangeSet;
        /// <summary>
        /// Checks if there is any forward change
        /// </summary>
        /// <returns>True if there is any forward change.</returns>
        public bool HasForwardChangeSet => m_ForwardChangeSet.IsCreated && m_ForwardChangeSet.HasChanges;
        /// <summary>
        /// Checks if there is any reverse change
        /// </summary>
        /// <returns>True if there is any reverse change.</returns>
        public bool HasReverseChangeSet => m_ReverseChangeSet.IsCreated && m_ReverseChangeSet.HasChanges;

        /// <summary>
        /// Access to the forward change set
        /// </summary>
        /// <returns>Forward change set.</returns>
        public EntityChangeSet ForwardChangeSet => m_ForwardChangeSet;
        /// <summary>
        /// Access to the reverse change set
        /// </summary>
        /// <returns>Reverse change set</returns>
        public EntityChangeSet ReverseChangeSet => m_ReverseChangeSet;

        /// <summary>
        /// Disposes the memory used by the change sets
        /// </summary>
        public void Dispose()
        {
            if (m_ForwardChangeSet.IsCreated)
                m_ForwardChangeSet.Dispose();

            if (m_ReverseChangeSet.IsCreated)
                m_ReverseChangeSet.Dispose();
        }
    }

    /// <summary>
    /// An archetype created in an <see cref="EntityChangeSet"/>, with the entities that use it.
    /// </summary>
    public struct FilteredArchetype
    {
        /// <summary>
        /// The number of entities in this archetype.
        /// </summary>
        public int EntityCount;
        /// <summary>
        /// The indices of this archetype's entities in <see cref="EntityChangeSet.Entities"/>.
        /// </summary>
        public UnsafeList<int> PackedEntityIndices;
        /// <summary>
        /// The component types of this archetype.
        /// </summary>
        public UnsafeList<TypeIndex> TypeIndices;
    }

    /// <summary>
    /// Represents a packed component within an <see cref="EntityChangeSet"/>
    /// </summary>
    public struct PackedComponent
    {
        /// <summary>
        /// Entity index in the packed entities array. <see cref="EntityChangeSet.Entities"/>
        /// </summary>
        public int PackedEntityIndex;

        /// <summary>
        /// Type index in the packed stableTypeHash array. <see cref="EntityChangeSet.TypeHashes"/>
        /// </summary>
        public int PackedTypeIndex;
    }

    /// <summary>
    /// Represents a packed component data change within a <see cref="EntityChangeSet"/>
    /// </summary>
    public struct PackedComponentDataChange
    {
        /// <summary>
        /// The entity and component this change is targeted.
        /// </summary>
        public PackedComponent Component;

        /// <summary>
        /// The start offset for this data change.
        /// </summary>
        /// <remarks>
        /// This is the field offset and NOT the payload offset.
        /// </remarks>
        public int Offset;

        /// <summary>
        /// The size of this data change. This is be the size in <see cref="EntityChangeSet.ComponentData"/> for this entry.
        /// </summary>
        public int Size;

        /// <summary>
        /// If this component has a enable bit change. -1 = no change, 0 = false, 1 = true
        /// </summary>
        public int Enabled;
    }

    /// <summary>
    /// Represents an entity reference that was changed within a <see cref="EntityChangeSet"/>
    ///
    /// This structure references the entity by it's unique <see cref="EntityGuid"/>.
    /// </summary>
    /// <remarks>
    /// Multiple patches could exist for the same component with different offsets.
    /// </remarks>
    public struct EntityReferenceChange
    {
        /// <summary>
        /// The entity and component this patched is targeted at.
        /// </summary>
        public PackedComponent Component;

        /// <summary>
        /// The field offset for the <see cref="Entity"/> field.
        /// </summary>
        public int Offset;

        /// <summary>
        /// The entity that the field should reference. Identified by the unique <see cref="EntityGuid"/>.
        /// </summary>
        public EntityGuid Value;
    }

    /// <summary>
    /// Represents a blob asset reference that was changed within a <see cref="EntityChangeSet"/>
    /// </summary>
    public struct BlobAssetReferenceChange
    {
        /// <summary>
        /// The entity and component this patched is targeted at.
        /// </summary>
        public PackedComponent Component;

        /// <summary>
        /// The field offset for the data.
        /// </summary>
        public int Offset;

        /// <summary>
        /// The blob asset this component should point to in the batch.
        /// </summary>
        public ulong Value;
    }

    /// <summary>
    /// Header for a changed blob asset.
    /// </summary>
    public struct BlobAssetChange
    {
        /// <summary>
        /// Byte length of this blob asset in the <see cref="EntityChangeSet.BlobAssetData"/> array.
        /// </summary>
        public int Length;

        /// <summary>
        /// The content hash for this blob asset.
        /// </summary>
        public ulong Hash;
    }

    /// <summary>
    /// Represents a shared component data change in a packed format
    /// </summary>
    public struct PackedSharedComponentDataChange
    {
        /// <summary>
        /// Represents the entity and component the patch is targeted at.
        /// </summary>
        public PackedComponent Component;
        /// <summary>
        /// Offset of the shared component data within the change set's unmanaged shared data buffer, or -1 when the value is the default.
        /// </summary>
        public int UnmanagedSharedValueDataOffset;
    }

    /// <summary>
    /// Represents a linked entity group change
    /// </summary>
    public struct LinkedEntityGroupChange
    {
        /// <summary>
        /// <see cref="EntityGuid"/> of the root entity for the linked entity group
        /// </summary>
        public EntityGuid RootEntityGuid;
        /// <summary>
        /// <see cref="EntityGuid"/> of a child entity for the linked entity group
        /// </summary>
        public EntityGuid ChildEntityGuid;
    }

    /// <summary>
    /// A flag to determine if a component type is a chunk component or not
    /// </summary>
    [Flags]
    public enum ComponentTypeFlags : byte
    {
        /// <summary>
        /// Not a chunk component type
        /// </summary>
        None = 0,
        /// <summary>
        /// A chunk component type
        /// </summary>
        ChunkComponent = 1 << 0
    }

    /// <summary>
    /// Represents a hash of a component type.
    /// </summary>
    /// <remarks>This struct is mostly intended for internal use in the Entity differ and patcher code.
    /// This hash is NOT expected to be stable across versions.</remarks>
    public struct ComponentTypeHash : IEquatable<ComponentTypeHash>
    {
        /// <summary>
        /// The hash value. Not expected to be stable across versions.
        /// </summary>
        public ulong StableTypeHash;
        /// <summary>
        /// Flags for this component type.
        /// </summary>
        public ComponentTypeFlags Flags;

        /// <summary>
        /// Checks if another ComponentTypeHash instance is equal to this one
        /// </summary>
        /// <param name="other">A ComponentTypeHash to compare with</param>
        /// <returns>True if <paramref name="other"/> contains the hash value and flags.</returns>
        public bool Equals(ComponentTypeHash other) => StableTypeHash == other.StableTypeHash && Flags == other.Flags;
        /// <summary>
        /// Checks if a ComponentTypeHash equals some object which may or may not be a ComponentTypeHash
        /// </summary>
        /// <param name="obj">An object to compare with</param>
        /// <returns>True if <paramref name="obj"/> is a ComponentTypeHash, and contains the same hash value and flags.</returns>
        public override bool Equals(object obj) => obj is ComponentTypeHash other && Equals(other);
        /// <summary>
        /// Checks if two ComponentTypeHash instances are equal
        /// </summary>
        /// <param name="left">A ComponentTypeHash to compare</param>
        /// <param name="right">A ComponentTypeHash to compare</param>
        /// <returns>True if <paramref name="left"/> and <paramref name="right"/> are equal.</returns>
        public static bool operator==(ComponentTypeHash left, ComponentTypeHash right) => left.Equals(right);
        /// <summary>
        /// Checks if two ComponentTypeHash instances are not equal
        /// </summary>
        /// <param name="left">A ComponentTypeHash to compare</param>
        /// <param name="right">A ComponentTypeHash to compare</param>
        /// <returns>True if <paramref name="left"/> and <paramref name="right"/> are not equal.</returns>
        public static bool operator!=(ComponentTypeHash left, ComponentTypeHash right) => !left.Equals(right);

        /// <summary>
        /// Computes a hashcode to support hash-based collections.
        /// </summary>
        /// <returns>The computed hash.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (StableTypeHash.GetHashCode() * 397) ^ (int)Flags;
            }
        }
    }

    /// <summary>
    /// An atomic package of changes to entity and component data.
    /// </summary>
    public readonly struct EntityChangeSet : IDisposable
    {
        /// <summary>
        /// Number of entities from the start of <see cref="Entities"/> that should be considered as created.
        /// </summary>
        public readonly int CreatedEntityCount;

        /// <summary>
        /// Number of entities from the end of <see cref="Entities"/> that should be considered as destroyed.
        /// </summary>
        public readonly int DestroyedEntityCount;

        /// <summary>
        /// Number of entities of which names changed in <see cref="NameChangedEntityGuids"/> in this change-set,
        /// not including created and destroyed entities.
        /// </summary>
        public readonly int NameChangedCount;

        /// <summary>
        /// A packed array of all entities in this change-set.
        /// </summary>
        public readonly NativeArray<EntityGuid> Entities;

        /// <summary>
        /// A packed array of all types in this change-set.
        /// </summary>
        public readonly NativeArray<ComponentTypeHash> TypeHashes;

        /// <summary>
        /// Changed names including created and destroyed entities in this change-set.
        /// </summary>
        public readonly NativeArray<FixedString64Bytes> Names;

        /// <summary>
        /// Entities of which names changed in this change-set, not including created and destroyed entities.
        /// </summary>
        public readonly NativeArray<EntityGuid> NameChangedEntityGuids;

        /// <summary>
        /// A set of all component additions in this change-set.
        /// </summary>
        public readonly NativeArray<PackedComponent> AddComponents;

        /// <summary>
        /// A set of all archetype created in this change-set.
        /// </summary>
        public readonly NativeArray<FilteredArchetype> AddArchetypes;

        /// <summary>
        /// A set of all component removals in this change-set.
        /// </summary>
        public readonly NativeArray<PackedComponent> RemoveComponents;

        /// <summary>
        /// A set of all component data modifications in this change-set.
        /// </summary>
        public readonly NativeArray<PackedComponentDataChange> SetComponents;

        /// <summary>
        /// Data payload for all component changes specified in <see cref="SetComponents"/>
        /// </summary>
        /// <remarks>
        /// Data changes are tightly packed. Use the <see cref="PackedComponentDataChange.Size"/> to read back.
        /// </remarks>
        public readonly NativeArray<byte> ComponentData;

        /// <summary>
        /// A packed set of all entity references to patch.
        /// </summary>
        public readonly NativeArray<EntityReferenceChange> EntityReferenceChanges;

        /// <summary>
        /// A packed set of all blob asset references to patch.
        /// </summary>
        public readonly NativeArray<BlobAssetReferenceChange> BlobAssetReferenceChanges;

        /// <summary>
        /// A set of all shared component data changes.
        /// </summary>
        public readonly PackedSharedComponentDataChange[] SetSharedComponents;

        /// <summary>
        /// All unmanaged shared data are stored here
        /// </summary>
        public readonly UnsafeAppendBuffer UnmanagedSharedComponentData;

        /// <summary>
        /// A set of all linked entity group additions.
        /// </summary>
        public readonly NativeArray<LinkedEntityGroupChange> LinkedEntityGroupAdditions;

        /// <summary>
        /// A set of all linked entity group removals.
        /// </summary>
        public readonly NativeArray<LinkedEntityGroupChange> LinkedEntityGroupRemovals;

        /// <summary>
        /// A set of all blob asset creations in this change set.
        /// </summary>
        /// <remarks>
        /// The <see cref="BlobAssetChange"/> is used to describe the payload within the <see cref="BlobAssetData"/> array.
        /// </remarks>
        public readonly NativeArray<BlobAssetChange> CreatedBlobAssets;

        /// <summary>
        /// A set of all blob assets destroyed in this change set. Identified by the content hash.
        /// </summary>
        public readonly NativeArray<ulong> DestroyedBlobAssets;

        /// <summary>
        /// The payload for all blob assets in this change set.
        /// </summary>
        public readonly NativeArray<byte> BlobAssetData;

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityChangeSet"/>.
        /// </summary>
        /// <param name="createdEntityCount">Number of entities from the start of <see cref="Entities"/> that should be considered as created.</param>
        /// <param name="destroyedEntityCount">Number of entities from the end of <see cref="Entities"/> that should be considered as destroyed.</param>
        /// <param name="nameChangedCount">Number of entities of which names changed in <see cref="NameChangedEntityGuids"/> in this change-set,
        /// not including created and destroyed entities.</param>
        /// <param name="entities">A packed array of all entities in this change-set.</param>
        /// <param name="typeHashes">A packed array of all types in this change-set.</param>
        /// <param name="names">Changed names including created and destroyed entities in this change-set.</param>
        /// <param name="nameChangedEntityGuids">Entities of which names changed in this change-set, not including created and destroyed entities.</param>
        /// <param name="addComponents">A set of all component additions in this change-set.</param>
        /// <param name="addArchetypes">A set of all archetype additions in this change-set.</param>
        /// <param name="removeComponents">A set of all component removals in this change-set.</param>
        /// <param name="setComponents">A set of all component data modifications in this change-set.</param>
        /// <param name="componentData">Data payload for all component changes specified in <see cref="SetComponents"/>.</param>
        /// <param name="entityReferenceChanges">A packed set of all entity references to patch.</param>
        /// <param name="blobAssetReferenceChanges">A packed set of all blob asset references to patch.</param>
        /// <param name="setSharedComponents">A set of all shared component data changes.</param>
        /// <param name="unmanagedSharedComponentData">All unmanaged shared data are stored here.</param>
        /// <param name="linkedEntityGroupAdditions">A set of all linked entity group additions.</param>
        /// <param name="linkedEntityGroupRemovals">A set of all linked entity group removals.</param>
        /// <param name="createdBlobAssets">A set of all blob asset creations in this change set.</param>
        /// <param name="destroyedBlobAssets">A set of all blob assets destroyed in this change set. Identified by the content hash.</param>
        /// <param name="blobAssetData">The payload for all blob assets in this change set.</param>
        public EntityChangeSet(
            int createdEntityCount,
            int destroyedEntityCount,
            int nameChangedCount,
            NativeArray<EntityGuid> entities,
            NativeArray<ComponentTypeHash> typeHashes,
            NativeArray<FixedString64Bytes> names,
            NativeArray<EntityGuid> nameChangedEntityGuids,
            NativeArray<PackedComponent> addComponents,
            NativeArray<FilteredArchetype> addArchetypes,
            NativeArray<PackedComponent> removeComponents,
            NativeArray<PackedComponentDataChange> setComponents,
            NativeArray<byte> componentData,
            NativeArray<EntityReferenceChange> entityReferenceChanges,
            NativeArray<BlobAssetReferenceChange> blobAssetReferenceChanges,
            PackedSharedComponentDataChange[] setSharedComponents,
            UnsafeAppendBuffer unmanagedSharedComponentData,
            NativeArray<LinkedEntityGroupChange> linkedEntityGroupAdditions,
            NativeArray<LinkedEntityGroupChange> linkedEntityGroupRemovals,
            NativeArray<BlobAssetChange> createdBlobAssets,
            NativeArray<ulong> destroyedBlobAssets,
            NativeArray<byte> blobAssetData)
        {
            CreatedEntityCount = createdEntityCount;
            DestroyedEntityCount = destroyedEntityCount;
            NameChangedCount = nameChangedCount;
            Entities = entities;
            TypeHashes = typeHashes;
            Names = names;
            NameChangedEntityGuids = nameChangedEntityGuids;
            AddComponents = addComponents;
            AddArchetypes = addArchetypes;
            RemoveComponents = removeComponents;
            SetComponents = setComponents;
            ComponentData = componentData;
            EntityReferenceChanges = entityReferenceChanges;
            BlobAssetReferenceChanges = blobAssetReferenceChanges;
            SetSharedComponents = setSharedComponents;
            UnmanagedSharedComponentData = unmanagedSharedComponentData;
            LinkedEntityGroupAdditions = linkedEntityGroupAdditions;
            LinkedEntityGroupRemovals = linkedEntityGroupRemovals;
            CreatedBlobAssets = createdBlobAssets;
            DestroyedBlobAssets = destroyedBlobAssets;
            BlobAssetData = blobAssetData;
            IsCreated = true;
        }

        /// <summary>
        /// Returns true if this object is allocated.
        /// </summary>
        public bool IsCreated { get; }

        /// <summary>
        /// Returns true if this object contains any change.
        /// </summary>
        public bool HasChanges => HasChangesIncludeNames();

        internal bool HasChangesIncludeNames(bool ignoreNameChangeCount = false, bool ignoreData = false)
        {
            bool hasChange = CreatedEntityCount != 0 ||
            DestroyedEntityCount != 0 ||
            AddComponents.Length != 0 ||
            AddArchetypes.Length != 0 ||
            RemoveComponents.Length != 0 ||
            SetComponents.Length != 0 ||
            EntityReferenceChanges.Length != 0 ||
            BlobAssetReferenceChanges.Length != 0 ||
            SetSharedComponents.Length != 0 ||
            LinkedEntityGroupAdditions.Length != 0 ||
            LinkedEntityGroupRemovals.Length != 0 ||
            CreatedBlobAssets.Length != 0 ||
            DestroyedBlobAssets.Length != 0;

            if (!ignoreData)
            {
                hasChange = hasChange || ComponentData.Length != 0;
                hasChange = hasChange || BlobAssetData.Length != 0;
            }

            if(!ignoreNameChangeCount)
            {
                hasChange = hasChange || NameChangedCount != 0;
            }

            return hasChange;
        }

        /// <summary>
        /// Disposes the memory used by the change set
        /// </summary>
        public unsafe void Dispose()
        {
            if (!IsCreated)
            {
                return;
            }

            foreach (var shared in SetSharedComponents)
            {
                // Default values (offset -1) store no data and were never retained when the change set was built, so there is nothing to release.
                if (shared.UnmanagedSharedValueDataOffset == -1)
                    continue;

                var typeIndex = TypeManager.GetTypeIndexFromStableTypeHash(
                    TypeHashes[shared.Component.PackedTypeIndex].StableTypeHash);
                if (typeIndex.IsRefCounted)
                {
                    (TypeManager.ConstructComponentFromBuffer(
                        typeIndex,
                        UnmanagedSharedComponentData.Ptr + shared.UnmanagedSharedValueDataOffset) as IRefCounted)?.Release();
                }
            }

            Entities.Dispose();
            TypeHashes.Dispose();
            Names.Dispose();
            NameChangedEntityGuids.Dispose();
            AddComponents.Dispose();
            foreach (var arch in AddArchetypes)
            {
                arch.TypeIndices.Dispose();
                arch.PackedEntityIndices.Dispose();
            }
            AddArchetypes.Dispose();
            RemoveComponents.Dispose();
            SetComponents.Dispose();
            ComponentData.Dispose();
            EntityReferenceChanges.Dispose();
            BlobAssetReferenceChanges.Dispose();
            LinkedEntityGroupAdditions.Dispose();
            LinkedEntityGroupRemovals.Dispose();
            CreatedBlobAssets.Dispose();
            DestroyedBlobAssets.Dispose();
            BlobAssetData.Dispose();
            if (UnmanagedSharedComponentData.IsCreated)
            {
                UnmanagedSharedComponentData.Dispose();
            }
        }

        internal static ulong GetCompanionLinkStableTypeHash()
        {
            return TypeManager.GetTypeInfo(CompanionComponentStore.CompanionLinkTypeIndex).StableTypeHash;
        }

        internal static int GetCompanionLinkPackedTypeIndex(NativeArray<ComponentTypeHash> typeHashes)
        {
            // Avoids referring to CompanionLink directly
            var companionLinkTypeHash = EntityChangeSet.GetCompanionLinkStableTypeHash();
            for (var i = 0; i < typeHashes.Length; i++)
            {
                if (typeHashes[i].StableTypeHash == companionLinkTypeHash)
                    return i;
            }
            return -1;
        }

    }
}
