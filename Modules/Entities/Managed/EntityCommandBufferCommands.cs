// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;

namespace Unity.Entities
{
    internal enum ECBCommand
    {
        InstantiateEntity,

        CreateEntity,
        DestroyEntity,

        AddComponent,
        AddMultipleComponents,
        RemoveComponent,
        RemoveMultipleComponents,
        SetComponent,
        SetEntityEnabled,
        SetComponentEnabled,
        SetName,

        AddBuffer,
        SetBuffer,
        AppendToBuffer,

        AddComponentLinkedEntityGroup,
        SetComponentLinkedEntityGroup,
        ReplaceComponentLinkedEntityGroup,

        AddUnmanagedSharedComponentData,
        SetUnmanagedSharedComponentData,

        AddUnmanagedSharedComponentValueForMultipleEntities,
        SetUnmanagedSharedComponentValueForMultipleEntities,
        AddUnmanagedSharedComponentValueForEntityQuery,
        SetUnmanagedSharedComponentValueForEntityQuery,

        AddComponentForEntityQuery,
        AddMultipleComponentsForMultipleEntities,
        AddMultipleComponentsForEntityQuery,
        RemoveComponentForEntityQuery,
        RemoveMultipleComponentsForMultipleEntities,
        RemoveMultipleComponentsForEntityQuery,

        AddComponentForMultipleEntities,
        RemoveComponentForMultipleEntities,

        DestroyMultipleEntities,
        DestroyForEntityQuery,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BasicCommand
    {
        public ECBCommand CommandType;
        public int TotalSize;
        public int SortKey;  /// Used to order command execution during playback
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct CreateCommand
    {
        public BasicCommand Header;
        public EntityArchetype Archetype;
        public int EntityCount;
        public Entity* Entities;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct EntityCommand
    {
        public BasicCommand Header;
        public Entity Entity;
        public int EntityCount;
        public Entity* Entities;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct EntityQueryCommand
    {
        public BasicCommand Header;
        public EntityQueryImpl* QueryImpl;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct EntityQueryComponentCommand
    {
        public EntityQueryCommand Header;
        public TypeIndex ComponentTypeIndex;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct EntityQueryComponentTypeSetCommand
    {
        public EntityQueryCommand Header;
        public ComponentTypeSet TypeSet;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct EntityQueryComponentCommandWithUnmanagedSharedComponent
    {
        public EntityQueryComponentCommand Header;
        public int ComponentSize;
        public int HashCode;
        public byte IsDefault;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct MultipleEntitiesCommand
    {
        public BasicCommand Header;
        public EntityNode Entities;
        public int EntitiesCount;
        public AllocatorManager.AllocatorHandle Allocator;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct MultipleEntitiesComponentCommand
    {
        public MultipleEntitiesCommand Header;
        public TypeIndex ComponentTypeIndex;
        public short ComponentSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct MultipleEntitiesCommand_WithUnmanagedSharedComponent
    {
        public MultipleEntitiesCommand Header;
        public TypeIndex ComponentTypeIndex;
        public int ComponentSize;
        public int HashCode;
        public byte IsDefault;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MultipleEntitiesAndComponentsCommand
    {
        public MultipleEntitiesCommand Header;
        public ComponentTypeSet TypeSet;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct EntityComponentCommand
    {
        public EntityCommand Header;
        public TypeIndex ComponentTypeIndex;
        public short ComponentSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct EntityEnabledCommand
    {
        public EntityCommand Header;
        public byte IsEnabled;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct EntityComponentEnabledCommand
    {
        public EntityEnabledCommand Header;
        public TypeIndex ComponentTypeIndex;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct EntityNameCommand
    {
        public EntityCommand Header;
        public FixedString64Bytes Name;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct EntityQueryMaskCommand
    {
        public EntityComponentCommand Header;
        public EntityQueryMask Mask;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct EntityMultipleComponentsCommand
    {
        public EntityCommand Header;
        public ComponentTypeSet TypeSet;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct EntityBufferCommand
    {
        public EntityCommand Header;
        public TypeIndex ComponentTypeIndex;
        public short ComponentSize;
        public BufferHeaderNode BufferNode; // must be the last field; contains a BufferHeader that extends beyond the end of the struct
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct EntityUnmanagedSharedComponentCommand
    {
        public EntityCommand Header;
        public TypeIndex ComponentTypeIndex;
        public int HashCode;
        public byte IsDefault;
    }


    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct BufferHeaderNode
    {
        public BufferHeaderNode* Prev;
        public BufferHeader TempBuffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct EntityNode
    {
        public Entity* Ptr;
        public EntityNode* Prev;
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    internal unsafe struct ChainCleanup
    {
        public EntityNode* EntityArraysCleanupList;
        public BufferHeaderNode* BufferCleanupList;
    }

}
