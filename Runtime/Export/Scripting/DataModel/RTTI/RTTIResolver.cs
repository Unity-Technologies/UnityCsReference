// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace Unity.DataModel;

[RequiredByNativeCode]
[NativeHeader("Runtime/Serialize/SchemaGenerator.h")]
internal static partial class RttiResolver
{
    [AutoStaticsCleanupOnCodeReload]
    static readonly Dictionary<Type, Rtti> TypeToRTTI = new();
    [AutoStaticsCleanupOnCodeReload]
    static readonly Dictionary<UdmTypeId, Rtti> TypeIDToRTTI = new();

    internal static Rtti GetRTTI(Type type)
    {
        TypeToRTTI.TryGetValue(type, out var rtti);
        return rtti;
    }

    internal static Rtti GetRTTI(object obj)
    {
        var rtti = GetRTTI(obj.GetType());
        if (rtti == null)
        {
            // object could be a pure native type, or an abstract interop type
            if (obj is UnityEngine.Object unityObject)
            {
                return GetRTTI(unityObject.GetObjectUDMTypeID());
            }
        }

        return rtti;
    }

    internal static Rtti GetRTTI(UdmTypeId typeID)
    {
        TypeIDToRTTI.TryGetValue(typeID, out var rtti);
        return rtti;
    }

    internal static Rtti GetRTTI(Schema schema)
    {
        if (TypeIDToRTTI.TryGetValue(schema.GetTypeId(), out var rtti))
        {
            /*
            TODO: requires deterministic schemas for singleton assets (e.g. SpriteAtlasDatabase),
            see https://jira.unity3d.com/browse/UDM-98
            if (rtti.Schema.SchemaPtr != schema.SchemaPtr)
            {
                throw new ArgumentException($"RTTI schema mismatch expected: {schema.GetTypeName().ToString()} actual: {rtti.Schema.GetTypeName().ToString()}");
            }
            */
        }
        else
        {
            rtti = default;
        }
        return rtti;
    }

    internal static Rtti GetRTTI<T>()
    {
        return GetRTTI(typeof(T));
    }

    internal static Rtti GetOrAddRTTI(Type type)
    {
        if (!TypeToRTTI.TryGetValue(type, out var rtti))
        {
            AddTypeInternal(type, out rtti);
        }
        return rtti;
    }

    private static bool IsAnyReferenceSchema(Schema schema)
    {
        while (schema.GetFlags().HasFlag(SchemaFlags.IsVector))
        {
            var elementSchema = schema.GetVectorElementSchema();
            if (elementSchema.IsReference())
                return true;

            schema = elementSchema;
        }

        return schema.IsReference();
    }

    internal static Rtti GetOrAddRTTI(Type type, Schema schema)
    {
        if (IsAnyReferenceSchema(schema))
        {
            return GetOrAddRTTI(type);
        }

        if (TypeToRTTI.TryGetValue(type, out var rtti))
        {
            if (rtti == null)
            {
                // cycle detected (like AnimationWindowClipPopup -> AnimationWindowState -> AnimEditor -> AnimationWindowClipPopup)
                return rtti;
            }
            /*
            TODO: requires deterministic schemas for singleton assets (e.g. SpriteAtlasDatabase),
            see https://jira.unity3d.com/browse/UDM-98
            if (rtti.Schema.SchemaPtr != schema.SchemaPtr)
            {
                throw new ArgumentException($"RTTI schema mismatch expected: {schema.GetTypeName().ToString()} actual: {rtti.Schema.GetTypeName().ToString()}");
            }
            */
        }
        else if (TypeIDToRTTI.TryGetValue(schema.GetTypeId(), out rtti))
        {
            /*
            TODO: requires deterministic schemas for singleton assets (e.g. SpriteAtlasDatabase),
            see https://jira.unity3d.com/browse/UDM-98
            if (rtti.Schema.SchemaPtr != schema.SchemaPtr)
            {
                throw new ArgumentException($"RTTI schema mismatch expected: {schema.GetTypeName().ToString()} actual: {rtti.Schema.GetTypeName().ToString()}");
            }
            */
        }
        else
        {
            rtti = AddTypeInternal(type, schema);
        }
        return rtti;
    }

    internal static Rtti GetOrAddRTTI(object obj)
    {
        var rtti = GetOrAddRTTI(obj.GetType());
        if (rtti == null)
        {
            // object could be a pure native type, or an abstract interop type
            if (obj is UnityEngine.Object unityObject)
            {
                var typeID = unityObject.GetObjectUDMTypeID();
                var schema = Schema.GetSchemaByType(typeID, 0);
                return GetRTTI(schema);
            }
        }

        return rtti;
    }

    internal static Rtti GetOrAddRTTI<T>()
    {
        return GetOrAddRTTI(typeof(T));
    }

    internal static UdmTypeId GetTypeID(Type type)
    {
        if (TypeToRTTI.TryGetValue(type, out var rtti))
        {
            if (rtti != null)
                return rtti.Schema.GetTypeId();
        }

        // This must match the Type ID generated by the SchemaGenerator
        var udmTypeIDAttribute = Attribute.GetCustomAttribute(type, typeof(UDMTypeIDAttribute), false) as UDMTypeIDAttribute;
        if (udmTypeIDAttribute != null)
        {
            var udmTypeIDHex = udmTypeIDAttribute.TypeID.Replace("-", string.Empty);
            return UdmTypeId.FromHex(udmTypeIDHex);
        }

        if (type.IsSubclassOf(typeof(UnityEngine.Object)) && !SerializeUtilities.ExtendsANativeType(type))
        {
            // The `NativeClass` attribute is used by the proxy generator (when creating bindings) to know the native class name that corresponds to the managed type.
            if (Attribute.GetCustomAttribute(type, typeof(NativeClassAttribute)) is NativeClassAttribute nativeClassAttr)
            {
                // If a managed type has an instance of the `NativeClass` attribute like so: `[NativeCode(null)]`
                if (string.IsNullOrEmpty(nativeClassAttr.QualifiedNativeName))
                {
                    return UdmTypeId.Default;
                }
            }
            return UnityHybridObjectRtti.GetNativeTypeID(type);
        }

        if (type.IsArray)
        {
            var elementType = type.GetElementType();
            var elementTypeID = GetTypeID(elementType);
            return SchemaBuilder.GetVectorTypeId(elementTypeID, VectorType.Array);
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            var elementType = type.GenericTypeArguments[0];
            var elementTypeID = GetTypeID(elementType);
            return SchemaBuilder.GetVectorTypeId(elementTypeID, VectorType.List);
        }

        return UdmTypeId.HashString(GetTypeHashableName(type));
    }

    private static string GetTypeHashableName(Type type)
    {
        StringBuilder builder = new StringBuilder(128);
        builder.Append(type.Assembly.GetName().Name);
        builder.Append("@");
        GetTypeHashableNameInternal(type, builder);
        return builder.ToString();
    }

    private static void GetTypeHashableNameInternal(Type type, StringBuilder builder)
    {
        if (type.IsGenericType && !type.IsGenericTypeDefinition)
        {
            builder.Append(type.GetGenericTypeDefinition().FullName);
            Type[] arguments = type.GenericTypeArguments;
            builder.Append("<");
            for (int index = 0; index < arguments.Length; ++index)
            {
                var argument = arguments[index];
                if (index > 0)
                {
                    builder.Append(",");
                }
                GetTypeHashableNameInternal(argument, builder);
            }
            builder.Append(">");
        }
        else
        {
            builder.Append(type.FullName);
        }
    }

    private static bool AddTypeInternal(Type type, out Rtti rtti, bool required = false)
    {
        if (type.IsAbstract || type.IsGenericType && type.IsGenericTypeDefinition)
        {
            rtti = default;
            return false;
        }

        var typeID = GetTypeID(type);
        Schema schema = Schema.GetSchemaByType(typeID, 0); // TODO: Native types use version 0, we should reconcile this or have a way to get the latest version for a type

        if (!schema.IsValid())
            schema = Schema.GetSchemaByType(typeID);

        if (!schema.IsValid())
        {
            if (required)
                throw new ArgumentException($"Schema for type not found: {type.Name} ID {typeID} from type fullname {type.FullName}");

            rtti = default;
            return false;
        }

        var rttiGroup = RttiBuilder.CalculateManagedRttiGroup(type);
        if (rttiGroup == RttiGroup.UnityObject && !schema.GetFlags().HasFlag(SchemaFlags.IsManaged))
        {
            rtti = default;
            return false;
        }

        rtti = AddTypeInternal(type, schema);
        return rtti != null;
    }

    internal static Rtti AddTypeInternal(Type type, Schema schema)
    {
        var typeID = schema.GetTypeId();

        // We are first creating the rtti instance and register it incomplete in TypeIDToRTTI and TypeToRTTI
        // Because there could be recursive definitions (like a type A having directly or indirectly a
        // field of type A (or Array/List of type A).
        // In this way everyone can hold into the correct RTTI instance, even if it is incomplete.
        // This is relevant only for reference types, for value types the problem is more complex and
        // it will be dealt in the future.
        var rtti = RttiBuilder.CreateManagedRtti(type, schema, out var rttiGroup);

        // Registering the RTTI entry
        TypeIDToRTTI[typeID] = rtti;
        TypeToRTTI[type] = rtti;

        if (rtti != null)
        {
            // Add the commands to the already existing RTTI type
            RttiBuilder.AddCommandsToRtti(type, schema, rttiGroup, ref rtti);
        }
        return rtti;
    }

    internal static Rtti AddNativeTypeInternal(Type type, Schema schema, bool hasDirectMapping)
    {
        var rtti = RttiBuilder.CreateNativeRtti(schema, type);
        TypeIDToRTTI[schema.GetTypeId()] = rtti;
        if (hasDirectMapping)
        {
            if (!TypeToRTTI.TryAdd(type, rtti))
            {
                throw new ArgumentException($"Managed type has already been registered: {type.Name}");
            }
        }
        return rtti;
    }

    internal static void Clear()
    {
        TypeIDToRTTI.Clear();
        TypeToRTTI.Clear();
    }

    [RequiredByNativeCode]
    internal static Int32 GetPersistentTypeID(IntPtr schemaPtr)
    {
        Schema schema = schemaPtr;
        var rtti = GetRTTI(schema);
        return rtti is UnityObjectRtti unityObjectRtti ? unityObjectRtti.PersistentTypeID : -1;
    }

    internal static Int32 GetPersistentTypeID(Schema schema)
    {
        return GetPersistentTypeID(schema.SchemaPtr);
    }

    [RequiredByNativeCode]
    internal static Type GetScriptingTypeForUDMType(IntPtr schemaPtr)
    {
        Schema schema = schemaPtr;
        var rtti = GetRTTI(schema);
        return rtti == null ? null : rtti.Type;
    }

    internal static Type GetScriptingTypeForUDMType(Schema schema)
    {
        return GetScriptingTypeForUDMType(schema.SchemaPtr);
    }

    [RequiredByNativeCode]
    internal static void GetUDMTypeFromScriptingType(Type type, out UdmTypeId udmTypeID)
    {
        var rtti = GetRTTI(type);
        udmTypeID = rtti == null ? UdmTypeId.Default : rtti.Schema.GetTypeId();
    }
}
