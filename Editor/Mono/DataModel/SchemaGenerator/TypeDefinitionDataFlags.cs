// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.DataModel;

// Flags to describe the type definition
[Flags]
internal enum TypeDefinitionDataFlags
{
    None =                                               0,
    System =                                             1 << 0,    // Type from the engine/system. It will not generate a schema
    Serializable =                                       1 << 1,    // Type that is serializable and will usually generate a schema
    SerializableAsBaseClass =                            1 << 2,    // Type that will only serialize as a base class of a serializable derived type
    SerializableWhenReferenced =                         1 << 3,    // Type that will only serialize where another structures member references this type
    GenericInstance =                                    1 << 4,    // Generic Type
    ValueType =                                          1 << 5,    // Value Type
    MakeDerivedSerializable =                            1 << 6,    // Will make all the derived types Serializable and MakeDerivedSerializable
    MakeDerivedSerializableWhenReferenced =              1 << 7,    // Will make all the derived types SerializableWhenReferenced and MakeDerivedSerializableWhenReferenced
    MakeDerivedValueType =                               1 << 8,    // Will make all the derived types ValueType and MakeDerivedValueType
    ImplementsIComponentData =                           1 << 9,    // The type implements `IComponentData`, so we need to inject an `UdmObjectId` field at the start of its schema
    HasExplicitLayout =                                  1 << 10,   // Has an explicit layout defined
    OpenGeneric =                                        1 << 11,   // Open generic type. It will not generate a schema
    HasSequentialLayout =                                1 << 12,   // Has an sequential layout defined
    FixedBufferType =                                    1 << 13,   // Compiler-generated type for fixed buffers. It is not marked [Serializable].
    ChunkSerializable =                                  1 << 14,   // Marked with [ChunkSerializable], denotes that the type should be treated as blittable
    ImplementsIBufferElementData =                       1 << 15,   // The type implements 'IBufferElementData', so we need to create a schema for the element, but also one for the buffers
}
