// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#nullable enable
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.DataModel;

internal static class UdmManager
{
    internal static void CreateBasicUdmTypeData()
    {
        unsafe
        {
            var schemaManager = UdmInterop.Instance.udm_get_schema_manager();

            // Fundamental types
            var boolSchema = SchemaBuilder.BuildBasicSchemaWithUnderlyingType(schemaManager, UdmTypeId.UInt8, "bool", UdmTypeId.HashString("System.Boolean"), 1);
            var byteSchema = SchemaBuilder.BuildBasicSchemaWithUnderlyingType(schemaManager, UdmTypeId.UInt8, "byte", UdmTypeId.HashString("System.Byte"), 1);
            var charSchema = SchemaBuilder.BuildBasicSchemaWithUnderlyingType(schemaManager, UdmTypeId.UInt8, "char", UdmTypeId.HashString("System.Char"), 1);

            TypeTraits<bool>     .Set(new TypeTraitsData { TypeName = "bool",   Schema = boolSchema, IsFundamentalType = true });
            TypeTraits<byte>     .Set(new TypeTraitsData { TypeName = "byte",   Schema = byteSchema, IsFundamentalType = true });
            TypeTraits<char>     .Set(new TypeTraitsData { TypeName = "char",   Schema = charSchema, IsFundamentalType = true });
            TypeTraits<sbyte>    .Set(new TypeTraitsData { TypeName = "int8",   Schema = UdmInterop.Instance.udm_int8_schema(),   IsFundamentalType = true });
            TypeTraits<short>    .Set(new TypeTraitsData { TypeName = "int16",  Schema = UdmInterop.Instance.udm_int16_schema(),  IsFundamentalType = true });
            TypeTraits<ushort>   .Set(new TypeTraitsData { TypeName = "uint16", Schema = UdmInterop.Instance.udm_uint16_schema(), IsFundamentalType = true });
            TypeTraits<int>      .Set(new TypeTraitsData { TypeName = "int32",  Schema = UdmInterop.Instance.udm_int32_schema(),  IsFundamentalType = true });
            TypeTraits<uint>     .Set(new TypeTraitsData { TypeName = "uint32", Schema = UdmInterop.Instance.udm_uint32_schema(), IsFundamentalType = true });
            TypeTraits<long>     .Set(new TypeTraitsData { TypeName = "int64",  Schema = UdmInterop.Instance.udm_int64_schema(),  IsFundamentalType = true });
            TypeTraits<ulong>    .Set(new TypeTraitsData { TypeName = "uint64", Schema = UdmInterop.Instance.udm_uint64_schema(), IsFundamentalType = true });
            TypeTraits<float>    .Set(new TypeTraitsData { TypeName = "float",  Schema = UdmInterop.Instance.udm_float_schema(),  IsFundamentalType = true });
            TypeTraits<double>   .Set(new TypeTraitsData { TypeName = "double", Schema = UdmInterop.Instance.udm_double_schema(), IsFundamentalType = true });

            // Non-fundamental types
            var guidSchema = UdmInterop.Instance.udm_guid_schema();
            var referenceSchema = UdmInterop.Instance.udm_reference_schema();
            var hashSchema = UdmInterop.Instance.udm_hash_schema();
            
            TypeTraits<string>   .Set(new TypeTraitsData { TypeName = "utf8string", Schema = UdmInterop.Instance.udm_utf8string_schema() });
            TypeTraits<Hash>     .Set(new TypeTraitsData { TypeName = "hash",       Schema = hashSchema });
            TypeTraits<UdmGuid>  .Set(new TypeTraitsData { TypeName = "guid",       Schema = guidSchema });
            TypeTraits<Reference>.Set(new TypeTraitsData { TypeName = "reference",  Schema = referenceSchema });

            // Unity types
            var unityHashSchema = SchemaBuilder.BuildBasicSchemaWithUnderlyingType(schemaManager, UdmTypeId.Hash, "hash", UdmTypeId.HashString("UnityEngine.Hash128"), 1);
            var unityGuidSchema = SchemaBuilder.BuildBasicSchemaWithUnderlyingType(schemaManager, UdmTypeId.Guid, "guid", UdmTypeId.HashString("UnityEngine.GUID"), 1);
            var entityIdSchema  = SchemaBuilder.BuildBasicSchemaWithUnderlyingType(schemaManager, UdmTypeId.Reference, "entityId", UdmTypeId.HashString("UnityEngine.EntityId"), 1);

            TypeTraits<UnityEngine.Hash128> .Set(new TypeTraitsData { TypeName = "hash", Schema = unityHashSchema});
            TypeTraits<UnityEngine.EntityId>.Set(new TypeTraitsData { TypeName = "entityId", Schema = entityIdSchema});
        }
    }
}
