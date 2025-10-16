// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#nullable enable

using System.Collections.ObjectModel;
using Unity.EntitiesLike;

namespace Unity.DataModel;

internal struct NativeSchema
{
    internal bool ShouldExist { get; private set; }
    internal bool Exists { get; private set; }
    internal Schema Value  { get; private set; }
    internal UdmTypeId PersistentTypeId { get; private set; }

    internal static NativeSchema From(UdmTypeId persistentTypeId, ulong typeVersion = 0)
    {
        var schema = Schema.GetSchemaByType(persistentTypeId, typeVersion);
        return new NativeSchema
        {
            ShouldExist = true,
            Exists = schema.IsValid(),
            Value = schema,
            PersistentTypeId = persistentTypeId
        };
    }
}

// Struct to store the data of a type definition
// TODO: Consider converting this to a class to simply the code.
internal struct TypeDefinitionData
{
    // We need to put all the fields in base classes here
    internal string Name;
    internal string QualifiedName;
    internal UdmTypeId TypeID;
    internal EntityHandle TypeDefinitionHandle;
    internal int  ReaderIndex;
    internal int  BaseIndex;
    internal TypeDefinitionState DefinitionDataState;
    internal string Assembly;
    internal string Module;
    internal TypeDefinitionDataFlags Flags;
    internal NativeSchema NativeSchema;
    internal bool IsRequiredType;
    internal bool BeingProcessed;
    internal bool IsEnum;

    // Used for StructLayout.Sequential
    // Documentation:
    // https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.structlayoutattribute.pack?view=net-9.0
    // https://learn.microsoft.com/en-us/dotnet/api/system.reflection.emit.packingsize?view=net-9.0
    internal uint EffectivePackingSize; // The pack size for this type (used to align this type) based on size and/or fields
    internal uint TypeSizeInMemory; // The size of the type including all padding (including start and ending padding)

    // Used for both StructLayout.Sequential and StructLayout.Explicit
    internal uint DefinedPackSize; // The manually defined Pack Size for this type
    internal uint DefinedStructSize; // The manually defined struct/class Size for this type

    internal ReadOnlyCollection<FieldDefinitionData>? Fields;
    internal DisassemblingTypeProviderData Signature;

    internal bool IsSerializable => (Flags & TypeDefinitionDataFlags.Serializable) != 0;
    internal bool IsFixedBuffer => (Flags & TypeDefinitionDataFlags.FixedBufferType) != 0;
    // Note: If the layout is not overridden with Explicit or Sequential (i.e StructLayout.Auto) we do expect the runtime and model offsets to match
    // Because of this, these types are not expected to be blittable
    internal bool OverrideLayout => (Flags & TypeDefinitionDataFlags.HasExplicitLayout) != 0 || (Flags & TypeDefinitionDataFlags.HasSequentialLayout) != 0;

    internal TypeLayout TypeLayout => new()
    {
        HasExplicitLayout = (byte)((Flags & TypeDefinitionDataFlags.HasExplicitLayout) != 0 ? 1 : 0),
        HasSequentialLayout = (byte)((Flags & TypeDefinitionDataFlags.HasSequentialLayout) != 0 ? 1 : 0),
        OverrideAlignment = (short)(OverrideLayout ? EffectivePackingSize : 0),
        OverrideSize = (int)(OverrideLayout ? DefinedStructSize : 0)
    };
}
