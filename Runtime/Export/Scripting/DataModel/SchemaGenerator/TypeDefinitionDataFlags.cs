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
    GenericInstance =                                    1 << 3,    // Generic Type
    ValueType =                                          1 << 4,    // Value Type
    MakeDerivedSerializable =                            1 << 5,    // Will make all the derived types Serializable and MakeDerivedSerializable
    MakeDerivedValueType =                               1 << 6,    // Will make all the derived types ValueType and MakeDerivedValueType
    ImplementsIComponentData =                           1 << 7,    // The type implements `IComponentData`, so we need to inject an `UdmObjectId` field at the start of its schema
    HasExplicitLayout =                                  1 << 8,   // Has an explicit layout defined
    OpenGeneric =                                        1 << 9,   // Open generic type. It will not generate a schema
    HasSequentialLayout =                                1 << 10,   // Has an sequential layout defined
    FixedBufferType =                                    1 << 11,   // Compiler-generated type for fixed buffers. It is not marked [Serializable].
    ChunkSerializable =                                  1 << 12,   // Marked with [ChunkSerializable], denotes that the type should be treated as blittable
    ImplementsIBufferElementData =                       1 << 13    // The type implements 'IBufferElementData', so we need to create a schema for the element, but also one for the buffers
}
