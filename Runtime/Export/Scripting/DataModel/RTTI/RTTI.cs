// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEngine.Assertions;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.DataModel;

internal enum RttiGroup
{
    /// <summary>
    /// Main RTTI in C#
    /// </summary>
    PureManaged,
    /// <summary>
    /// Edge-case for boxed immutable objects in c# (strings/arrays) that need special handling
    /// </summary>
    ImmutablePureManaged,
    UnityObject,
    /// <summary>
    /// This is a pure entity, so there isn't a Unity Object backing it
    /// </summary>
    PureEntity,
    /// <summary>
    /// RTTI for Simple Native Types, i.e. native types that <br />
    /// 1) do NOT derive from `Unity.Object`, and <br />
    /// 2) have managed counterparts that contain ONLY pointer fields pointing to their native instances.
    /// </summary>
    SimpleNativeType
}

internal abstract class Rtti
{
    internal readonly RttiGroup RttiGroup;
    internal readonly Type Type;
    internal readonly Schema Schema;

    internal Rtti(Type type, Schema schema, RttiGroup rttiGroup)
    {
        Type = type;
        Schema = schema;
        RttiGroup = rttiGroup;
    }

    internal static bool IsTypeDerived(UdmTypeId typeId, UdmTypeId derivedTypeId)
    {
        var rtti = RttiResolver.GetRTTI(typeId);
        var derivedRtti = RttiResolver.GetRTTI(derivedTypeId);
        return rtti.IsDerived(derivedRtti);
    }

    private bool IsDerived(Rtti derivedType)
    {
        return this.Type.IsAssignableFrom(derivedType.Type);
    }

    internal static ConstructorInfo GetParameterlessConstructor(Type type)
    {
        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        return type.GetConstructor(flags, null, Type.EmptyTypes, null);
    }
}

internal interface IManagedRtti
{
    RttiData[] TransferData { get; }
    bool IsBlittable { get; }
}

internal sealed class PureManagedObjectRtti : Rtti, IManagedRtti
{
    internal readonly ConstructorInfo Constructor;
    public RttiData[] TransferData { get; set; }
    public bool IsBlittable { get; set; }

    internal PureManagedObjectRtti(Type type, Schema schema, RttiData[] transferData, bool isBlittable)
        : base(type, schema, RttiGroup.PureManaged)
    {
        Constructor = GetParameterlessConstructor(type);
        TransferData = transferData;

        // If the type is blittable, the size of the copy must match the size of the runtime type
        EngineHelper.AssertIsTrue(!isBlittable || (int)transferData[0].CopySize == EngineHelper.SizeOf(type));

        // IsBlittable means that the representation of the data is the same between the model and the runtime, aka. can be copied 1-1.
        IsBlittable = isBlittable;
    }
};

// Handles special cases of immutable object types (string/array) as ObjectModel.
// These types cannot be initialized using RuntimeHelpers.GetUninitializedObject(type).
internal sealed class ImmutablePureManagedObjectRtti : Rtti, IManagedRtti
{
    public RttiData[] TransferData { get; set; }
    public bool IsBlittable { get; }

    internal ImmutablePureManagedObjectRtti(Type type, Schema schema, RttiData[] transferData)
        : base(type, schema, RttiGroup.ImmutablePureManaged)
    {
        TransferData = transferData;
        IsBlittable = false;
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("CodeReloadSafety", "UAL0001:Unsealed Public Class", Justification = "Unsealed on purpose")]
internal class UnityObjectRtti : Rtti
{
    public Int32 PersistentTypeID { get; protected set; }

    internal UnityObjectRtti(Type type, Schema schema)
        : base(type, schema, RttiGroup.UnityObject)
    {
        PersistentTypeID = GetPersistentTypeID(schema);
    }

    protected UnityObjectRtti(Type type, Schema schema, Schema nativeSchema)
        : base(type, schema, RttiGroup.UnityObject)
    {
        PersistentTypeID = GetPersistentTypeID(nativeSchema);
    }

    internal static Int32 GetPersistentTypeID(Schema schema)
    {
        return (Int32)schema.GetTypeId().hash.Uint64Data1;
    }
};

internal sealed class UnityHybridObjectRtti : UnityObjectRtti, IManagedRtti
{
    internal readonly Schema NativeSchema;
    internal readonly ConstructorInfo Constructor;
    public RttiData[] TransferData { get; set; }
    public bool IsBlittable { get; }

    internal UnityHybridObjectRtti(Type type, Schema schema, Schema nativeSchema, RttiData[] transferData)
        : base(type, schema, nativeSchema)
    {
        NativeSchema = nativeSchema;
        Constructor = Rtti.GetParameterlessConstructor(type);
        TransferData = transferData;
        IsBlittable = false;
    }

    internal static UdmTypeId GetNativeTypeID(Type type)
    {
        if (!type.IsSubclassOf(typeof(UnityEngine.Object)))
        {
            throw new ArgumentException($"Type {type.Name} is not a hybrid type");
        }

        var nativeTypeID = UnityEngine.Object.GetUDMTypeID(type);
        if (!nativeTypeID.IsValid())
        {
            // TODO: Instead of returning `UdmTypeId.Default`, we should throw an exception here.
            // Currently we fail to retrieve the native type ID of `SixWaySmokeLit` (which inherits from
            // `RenderPipelineMaterial`), even though we expect it to exist. This should be investigated
            // and fixed in a future PR.
            return UdmTypeId.Default;
        }

        return nativeTypeID;
    }
};

internal sealed class PureEntityRtti : Rtti
{
    internal PureEntityRtti(Type type, Schema schema)
        : base(type, schema, RttiGroup.PureEntity)
    {
    }
};

internal sealed class SimpleNativeTypeObjectRtti : Rtti
{
    internal readonly ConstructorInfo Constructor;

    internal SimpleNativeTypeObjectRtti(Type type, Schema schema)
        : base(type, schema, RttiGroup.SimpleNativeType)
    {
        Constructor = Rtti.GetParameterlessConstructor(type);
    }
}
