// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// TODO: Remove this whole file once the real
// Unity.Entities is part of U6
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.DataModel;


namespace Unity
{
    namespace EntitiesLike
    {
        internal struct Entity
        {
            internal Entity(EntityId id)
            {

            }

            public static implicit operator Entity(EntityId instanceID) => UnsafeUtility.As<EntityId, Entity>(ref instanceID);
        }

        internal struct TypeIndex
        {
            internal int Value;
            public static implicit operator TypeIndex(int value) => new TypeIndex { Value = value };
            public static implicit operator int(TypeIndex ti) => ti.Value;

            internal bool IsZeroSized => false;
        }

        internal struct EntityInChunk : IComparable<EntityInChunk>, IEquatable<EntityInChunk>
        {
            internal static EntityInChunk Null => default;

            public int CompareTo(EntityInChunk other)
            {
                return 0;
            }

            public bool Equals(EntityInChunk other)
            {
                return CompareTo(other) == 0;
            }
        }

        internal unsafe struct EntityComponentStore
        {
            internal static readonly EntityStore s_entityStore = null;

            internal class EntityStore
            {
                internal struct EntityStoreData
                {
                    internal bool Exists(Entity entity)
                    {
                        return false;
                    }

                    internal int GetEntityInChunk(Entity entity)
                    {
                        return 0;
                    }
                }

                internal EntityStoreData Data;
            }

            internal struct ArchetypeChanges
            {
                internal static int StartIndex;
                internal uint ArchetypeTrackingVersion;

                public ArchetypeChanges(int startIndex, uint archetypeTrackingVersion)
                {
                    StartIndex = startIndex;
                    ArchetypeTrackingVersion = archetypeTrackingVersion;
                }
            }
        }

        internal struct Parent
        {

        }

        internal struct Child
        {

        }

        internal unsafe struct EntityArchetype
        {
        }

        unsafe partial struct EntityDataAccess
        {
            internal void PrepareForAdditiveStructuralChanges()
            {
            }

            internal EntityComponentStore.ArchetypeChanges BeginAdditiveStructuralChanges()
            {
                return new EntityComponentStore.ArchetypeChanges();
            }

            internal EntityArchetype GetGameObjectEntityArchetype()
            {
                return default(EntityArchetype);
            }

            internal void AllocateAndAssignChunksToExistingEntitiesInstantiate(Entity srcEntity, Entity* existingEntities, int count)
            {
            }

            internal void EndStructuralChanges(ref EntityComponentStore.ArchetypeChanges changes)
            {
            }

            internal void AllocateAndAssignChunksToExistingEntities(EntityArchetype archetype, Entity* existingEntities, int count)
            {
            }
        }

        internal unsafe struct WorldUnmanaged
        {
            internal EntityManager EntityManager => default(EntityManager);
        }

        internal unsafe partial class World : IDisposable
        {
            internal EntityManager EntityManager => default(EntityManager);

            public void Dispose()
            {
            }
        }

        internal static class ActiveGameObjectWorld
        {
            internal static World World => default(World);
        }

        internal unsafe struct EntityManager
        {
            internal bool HasComponent<T>(Entity entity)
            {
                return false;
            }

            internal bool HasComponent(Entity entity, TypeIndex typeIndex)
            {
                return false;
            }

            internal static bool TryGetWorldUnmanaged(Entity entity, out WorldUnmanaged worldUnmanaged)
            {
                return false;
            }

            internal BufferHeader* GetBufferHeaderRawRO(Entity entity, TypeIndex typeIndex)
            {
                return null;
            }

            internal void* GetBufferRawRO(Entity entity, TypeIndex typeIndex)
            {
                return null;
            }
            internal void* GetComponentDataRawRO(Entity entity, TypeIndex typeIndex)
            {
                return null;
            }

            internal void* GetComponentDataRawRW(Entity entity, TypeIndex typeIndex)
            {
                return null;
            }

            internal ulong GetBufferLength(EntityId id, TypeIndex idx)
            {
                return 0;
            }

            internal int GetComponentCount(Entity entity)
            {
                return 0;
            }

            internal NativeArray<ComponentType> GetComponentTypes(Entity entity, Allocator allocator = Allocator.Temp)
            {
                return new NativeArray<ComponentType>();
            }

            internal bool IsCreated => false;

            internal EntityDataAccess* GetCheckedEntityDataAccess()
            {
                return null;
            }

            internal void AddComponent(Entity entity, TypeIndex typeIndex)
            {

            }

            internal void SetParent(Entity child, Entity newParent, bool preserveWorldTransform = true)
            {

            }

        }

        internal class ListPrivateFieldAccess<T>
        {
#pragma warning disable CS0649
#pragma warning disable CS8618
            internal T[] _items; // Do not rename (binary serialization)
#pragma warning restore CS8618
            internal int _size; // Do not rename (binary serialization)
            internal int _version; // Do not rename (binary serialization)
#pragma warning restore CS0649
        }

        internal struct ComponentType
        {
            internal bool IsZeroSized => false;
            internal TypeIndex TypeIndex;
            internal bool IsBuffer => false;

            public ComponentType(TypeIndex typeIndex)
            {
                TypeIndex = typeIndex;
            }

        }


        [StructLayout(LayoutKind.Explicit)]
        // [NoAlias]
        internal unsafe struct BufferHeader
        {
            internal enum TrashMode
            {
                TrashOldData,
                RetainOldData
            }

            [FieldOffset(0)] internal byte* Pointer;
            [FieldOffset(8)] internal int Length;

            internal static void SetCapacity(BufferHeader* header, int count, int typeSize, int alignment, TrashMode trashMode, bool useMemoryInitPattern, byte memoryInitPattern)
            {

            }

        }

        internal struct EntityHandle
        {

        }

        internal struct FieldDefinitionData
        {

        }

        internal struct DisassemblingTypeProviderData
        {

        }

        internal struct GUID
        {

        }

        internal readonly struct TypeInfo
        {
            internal   readonly int           TypeSize;
            internal   readonly int           AlignmentInBytes;

            public TypeInfo(int typeSize, int alignmentInBytes)
            {
                TypeSize = typeSize;
                AlignmentInBytes = alignmentInBytes;
            }

        }

        internal struct TypeManager
        {
            static TypeInfo m_TypeInfo;
            internal static IEnumerable<Type> GetUdmRTTITypes()
            {
                return null;
            }

            internal static ref readonly TypeInfo GetTypeInfo(TypeIndex typeIndex)
            {
                m_TypeInfo = new TypeInfo();
                return ref m_TypeInfo;
            }

            internal static TypeIndex GetTypeIndex(Type type)
            {
                return default(TypeIndex);
            }

            internal static Type GetType(TypeIndex typeIndex)
            {
                return null;
            }
        }

        internal unsafe readonly struct TransformRef
        {

        }

        internal unsafe struct ExclusiveEntityTransaction
        {
            internal EntityManager EntityManager => default(EntityManager);
        }

        internal struct Simulate
        {

        }

        internal static class EntityAllocator
        {
            internal struct SkipGlobalEntityDeallocation
            {

            }
        }

        internal sealed partial class AssetObjectRegistry
        {
            internal static void GetOrCreateReferencesForObjects(NativeArray<EntityId> instanceID, NativeArray<Reference> udmReferences)
            {

            }
        }

        internal sealed class PureEntity
        {

        }

        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
        internal sealed class ChunkSerializableAttribute : Attribute
        {
        }
    }
}
