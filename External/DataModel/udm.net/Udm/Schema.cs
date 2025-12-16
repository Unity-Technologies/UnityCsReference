#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using UnityEngine.Bindings;

using static Unity.DataModel.DocumentModel;

namespace Unity.DataModel
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct SchemaFields
    {
        internal SchemaField GetFieldByIndex(ulong index)
        {
            return &Fields[index];
        }

        internal SchemaFieldImpl* Fields;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct SchemaImpl
    {
        internal SchemaFlags Flags;
        internal UTF8StringField TypeName;
        internal UdmTypeId TypeId;
        internal ulong TypeVersion;
        internal UdmTypeId UnderlyingTypeId;

        internal ulong Alignment;
        internal ulong Size;                // size of fixed length data
        internal ulong DefaultValuesSize;   // size of fixed length data + variable length data
        internal ulong DefaultValuesOffset; // offset of data

        internal VectorField Fields;
        internal VectorField FieldKeys;
        internal VectorField References;
    }

    [StructLayout(LayoutKind.Sequential)]
    [VisibleToOtherModules]
    internal unsafe struct Schema : IEquatable<Schema>
    {
        internal SchemaFlags GetFlags()
        {
            ThrowIfInvalid();
            unsafe
            {
                return ((SchemaImpl*)SchemaPtr)->Flags;
            }
        }

        internal static Schema GetSchemaById(SchemaId schemaId)
        {
            unsafe
            {
                return UdmInterop.Instance.udm_schema_get_by_id(&schemaId);
            }
        }

        internal static Schema GetOrCreateSchemaById(SchemaId schemaId)
        {
            unsafe
            {
                return UdmInterop.Instance.udm_schema_get_or_create_by_id(&schemaId);
            }
        }

        internal static Schema GetSchemaByType(UdmTypeId typeId, ulong typeVersion = 1)
        {
            unsafe
            {
                return UdmInterop.Instance.udm_schema_get_by_type(&typeId, typeVersion);
            }
        }

        // This is temporary, while .net 8 is supported by the rest of the build pipeline
        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static unsafe int StreamWriteCallback(IntPtr userContext, byte* buffer, ulong size)
        {
            object? target = GCHandle.FromIntPtr(userContext).Target;
            if (target is Stream stream)
            {
                stream.Write(new Span<byte>(buffer, (int)size));
                return 0;
            }


            return 0;
        }

        internal unsafe void ToText(Stream outputStream)
        {
            var handle = GCHandle.Alloc(outputStream);

            try
            {
                // This is temporary, while .net 8 is supported by the rest of the build pipeline
                delegate* unmanaged[Cdecl]<IntPtr, byte*, ulong, int> writeCallback = &StreamWriteCallback;
                UdmInterop.Instance.udm_schema_to_text((SchemaImpl*)SchemaPtr, (IntPtr)handle, writeCallback);
            }
            finally
            {
                handle.Free();
            }
        }

        internal ConstAccessor GetAccessor()
        {
            unsafe
            {
                var ptr = (byte*)SchemaPtr + ((SchemaImpl*)SchemaPtr)->DefaultValuesOffset;
                return new ConstAccessor
                {
                    Schema = this,
                    Data = (IntPtr)ptr,
                    References = (IntPtr)((SchemaImpl*)SchemaPtr)->References.GetDataPtr()
                };
            }
        }

        internal ConstUTF8String GetTypeName()
        {
            ThrowIfInvalid();
            unsafe
            {
                return new ConstUTF8String
                {
                    Field = (IntPtr)(&((SchemaImpl*)SchemaPtr)->TypeName)
                };
            }
        }

        internal UdmTypeId GetTypeId()
        {
            ThrowIfInvalid();
            unsafe
            {
                return ((SchemaImpl*)SchemaPtr)->TypeId;
            }
        }

        internal ulong GetTypeVersion()
        {
            ThrowIfInvalid();
            unsafe
            {
                return ((SchemaImpl*)SchemaPtr)->TypeVersion;
            }
        }

        internal UdmTypeId GetUnderlyingTypeId()
        {
            ThrowIfInvalid();
            unsafe
            {
                return ((SchemaImpl*)SchemaPtr)->UnderlyingTypeId;
            }
        }

        internal SchemaId GetSchemaId()
        {
            ThrowIfInvalid();
            unsafe
            {
                return UdmInterop.Instance.udm_schema_get_id((SchemaImpl*)SchemaPtr);
            }
        }

        internal ulong GetAlignment()
        {
            ThrowIfInvalid();
            unsafe
            {
                return ((SchemaImpl*)SchemaPtr)->Alignment;
            }
        }

        internal ulong GetSize()
        {
            ThrowIfInvalid();
            unsafe
            {
                return ((SchemaImpl*)SchemaPtr)->Size;
            }
        }

        internal Schema GetVectorElementSchema()
        {
            ThrowIfInvalid();
            if (!((SchemaImpl*)SchemaPtr)->Flags.HasFlag(SchemaFlags.IsVector))
            {
                throw new InvalidOperationException("Schema is not a schema of vector type");
            }

            ReadOnlySpan<SchemaFieldImpl> fields = GetFieldsInternalUnsafe();
            if (fields.Length != 1)
            {
                throw new InvalidOperationException("Vector schema is expected to have exactly one field");
            }

            return Schema.GetOrCreateSchemaById(fields[0].SchemaId);
        }

        internal Schema GetMapKeySchema()
        {
            ThrowIfInvalid();
            if (!((SchemaImpl*)SchemaPtr)->Flags.HasFlag(SchemaFlags.IsMap))
            {
                throw new InvalidOperationException("Schema is not a schema of map type");
            }

            Schema elementSchema = GetVectorElementSchema();
            SchemaField keyField = elementSchema.GetFieldByName("first");
            if (!keyField.IsValid())
            {
                throw new InvalidOperationException("Map element is expected to have a field with name 'first'");
            }

            return keyField.GetSchema();
        }

        internal Schema GetMapValueSchema()
        {
            ThrowIfInvalid();
            if (!((SchemaImpl*)SchemaPtr)->Flags.HasFlag(SchemaFlags.IsMap))
            {
                throw new InvalidOperationException("Schema is not a schema of map type");
            }

            Schema elementSchema = GetVectorElementSchema();
            SchemaField valueField = elementSchema.GetFieldByName("second");
            if (!valueField.IsValid())
            {
                throw new InvalidOperationException("Map element is expected to have a field with name 'second'");
            }

            return valueField.GetSchema();
        }

        internal ulong GetFieldCount()
        {
            ThrowIfInvalid();
            unsafe
            {
                return ((SchemaImpl*)SchemaPtr)->Fields.Size;
            }
        }

        internal SchemaFields GetFields()
        {
            ThrowIfInvalid();
            return new SchemaFields
            {
                Fields = (SchemaFieldImpl*)((SchemaImpl*)SchemaPtr)->Fields.GetDataPtr()
            };
        }

        private ReadOnlySpan<SchemaFieldImpl> GetFieldsInternalUnsafe()
        {
            unsafe
            {
                var ptr = ((SchemaImpl*)SchemaPtr)->Fields.GetDataPtr();
                return new ReadOnlySpan<SchemaFieldImpl>((SchemaFieldImpl*)ptr, (int)((SchemaImpl*)SchemaPtr)->Fields.Size);
            }
        }

        private ReadOnlySpan<SchemaFieldKeyImpl> GetFieldKeysInternalUnsafe()
        {
            unsafe
            {
                var ptr = ((SchemaImpl*)SchemaPtr)->FieldKeys.GetDataPtr();
                return new ReadOnlySpan<SchemaFieldKeyImpl>((SchemaFieldKeyImpl*)ptr, (int)((SchemaImpl*)SchemaPtr)->FieldKeys.Size);
            }
        }

        internal ulong GetFieldIndex(SchemaField field)
        {
            ThrowIfInvalid(field);
            unsafe
            {
                return (ulong)((byte*)field.SchemaFieldPtr - ((SchemaImpl*)SchemaPtr)->Fields.GetDataPtr()) / (ulong)sizeof(SchemaFieldImpl);
            }
        }

        internal bool HasField(SchemaField field)
        {
            ThrowIfInvalid();
            unsafe
            {
                SchemaFieldImpl* fields = (SchemaFieldImpl*)((SchemaImpl*)SchemaPtr)->Fields.GetDataPtr();

                return field.SchemaFieldPtr >= fields && field.SchemaFieldPtr < fields + ((SchemaImpl*)SchemaPtr)->Fields.Size;
            }
        }

        internal bool HasField(string fieldName)
        {
            ThrowIfInvalid();
            unsafe
            {
                var field = GetFieldByName(fieldName);
                return field.IsValid();
            }
        }

        [VisibleToOtherModules]
        internal bool IsValid()
        {
            unsafe
            {
                return SchemaPtr != IntPtr.Zero;
            }
        }

        internal bool IsUnderlyingType<T>()
        {
            ThrowIfInvalid();
            var data = TypeTraits<T>.Get();
            return GetUnderlyingTypeId() == data.Schema.GetUnderlyingTypeId();
        }

        internal bool Is(Type type)
        {
            ThrowIfInvalid();
            return GetTypeName().ToString() == type.Name;
        }

        internal bool IsHash()
        {
            ThrowIfInvalid();
            return GetUnderlyingTypeId() == UdmTypeId.Hash;
        }

        internal bool IsGuid()
        {
            ThrowIfInvalid();
            return GetUnderlyingTypeId() == UdmTypeId.Guid;
        }

        internal bool IsReference()
        {
            ThrowIfInvalid();
            return GetUnderlyingTypeId() == UdmTypeId.Reference;
        }

        internal bool IsUTF8String()
        {
            ThrowIfInvalid();
            return GetUnderlyingTypeId() == UdmTypeId.Utf8String;
        }

        internal void ThrowIfInvalid()
        {
            if (!IsValid())
                throw new InvalidOperationException("Trying to use an invalid Schema");
        }

        internal void ThrowIfInvalid(SchemaField field)
        {
            ThrowIfInvalid();
            field.ThrowIfInvalid();
            if (!HasField(field))
                throw new InvalidOperationException($"Field {field.GetName()} not belonging to the schema {GetTypeName()}");
        }

        internal SchemaField GetFieldByName(string fieldName)
        {
            ThrowIfInvalid();
            var bytes = Encoding.UTF8.GetBytes(fieldName);
            uint hash;
            unsafe
            {
                fixed (byte* ptr = bytes)
                {
                    hash = UdmInterop.Instance.udm_xxhash32(ptr, (ulong)bytes.Length);
                }
            }
            return GetFieldByNameInternalUnsafe(hash, bytes);
        }

        private SchemaFieldImpl* GetFieldByNameInternalUnsafe(uint fieldNameHash, ReadOnlySpan<byte> fieldName)
        {
            ReadOnlySpan<SchemaFieldImpl> fields = GetFieldsInternalUnsafe();
            ReadOnlySpan<SchemaFieldKeyImpl> fieldKeys = GetFieldKeysInternalUnsafe();

            ReadOnlySpan<SchemaFieldKeyImpl> range = EqualRange(fieldKeys, fieldNameHash);
            foreach (ref readonly var fieldKey in range)
            {
                ref readonly var field = ref fields[(int)fieldKey.Index];
                if (field.Name.AsReadOnlySpan().SequenceEqual(fieldName))
                {
                    fixed (SchemaFieldImpl* fieldRef = &field)
                    {
                        return fieldRef;
                    }
                }
            }
            return null;
        }

        private static ReadOnlySpan<SchemaFieldKeyImpl> EqualRange(ReadOnlySpan<SchemaFieldKeyImpl> fieldKeys, uint fieldNameHash)
        {
            int low = 0;
            int high = fieldKeys.Length - 1;

            while (low <= high)
            {
                int mid = (low + high) >> 1;

                if (fieldKeys[mid].FieldNameHash < fieldNameHash)
                {
                    low = mid + 1;
                }
                else if (fieldKeys[mid].FieldNameHash > fieldNameHash)
                {
                    high = mid - 1;
                }
                else
                {
                    int startIndex = mid;
                    int endIndex = mid;

                    while (startIndex > 0 && fieldKeys[startIndex - 1].FieldNameHash == fieldNameHash)
                    {
                        startIndex--;
                    }

                    while (endIndex < fieldKeys.Length - 1 && fieldKeys[endIndex + 1].FieldNameHash == fieldNameHash)
                    {
                        endIndex++;
                    }

                    return fieldKeys.Slice(startIndex, endIndex - startIndex + 1);
                }
            }

            return ReadOnlySpan<SchemaFieldKeyImpl>.Empty;
        }

        public override bool Equals(object other)
        {
            if (other is Schema schema)
            {
                return Equals(schema);
            }
            return false;
        }

        public bool Equals(Schema other)
        {
            return SchemaPtr == other.SchemaPtr;
        }

        public override int GetHashCode()
        {
            return ((IntPtr)SchemaPtr).GetHashCode();
        }

        public static bool operator ==(Schema lhs, Schema rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(Schema lhs, Schema rhs)
        {
            return !lhs.Equals(rhs);
        }

        public static unsafe implicit operator Schema(SchemaImpl* ptr)
        {
            return new Schema
            {
                SchemaPtr = (IntPtr)ptr
            };
        }

        public static unsafe implicit operator Schema(IntPtr ptr)
        {
            return new Schema
            {
                SchemaPtr = (IntPtr)ptr
            };
        }

        // Pointers to blittable types are not considered blittable by the bindings generator
        //internal unsafe SchemaImpl* SchemaPtr;
        internal unsafe IntPtr SchemaPtr;
    }
}
