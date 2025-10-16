using System;
using System.Runtime.InteropServices;

namespace Unity.DataModel
{

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct ConstAccessor : IEquatable<ConstAccessor>
    {
        public static implicit operator ConstAccessor(Accessor accessor)
        {
            return new ConstAccessor
            {
                Schema = accessor.Schema,
                Data = accessor.Data,
                References = accessor.DocumentModel.IsValid() ? (IntPtr)accessor.DocumentModel.GetReferences() : IntPtr.Zero
            };
        }

        internal bool IsValid()
        {
            unsafe
            {
                return Schema.IsValid() && Data != IntPtr.Zero;
            }
        }

        internal T GetNumericValue<T>() where T : unmanaged  // Rename Numeric
        {
            ThrowIfInvalid();

            unsafe
            {
                if (Schema.IsUnderlyingType<T>() && TypeTraits<T>.Get().IsFundamentalType)
                    return *(T*)Data;

                throw new InvalidOperationException($"This accessor with schema '{Schema.GetTypeName()}({Schema.GetTypeId()},{Schema.GetTypeVersion()})' is not the expected fundamental type '{typeof(T).FullName}'.");
            }
        }

        internal T GetValueUnsafe<T>() where T : unmanaged
        {
            ThrowIfInvalid();

            if (Schema.GetSize() == (ulong)sizeof(T))
                return *(T*)Data;
            throw new InvalidOperationException($"The schema size for '{Schema.GetTypeName()}({Schema.GetTypeId()},{Schema.GetTypeVersion()})' and the size of '{typeof(T).FullName}' is not the same.");
        }
        internal Hash GetHashValue()
        {
            ThrowIfInvalid();

            if (Schema.IsUnderlyingType<Hash>())
                return *(Hash*)Data;
            throw new InvalidOperationException($"This accessor with schema '{Schema.GetTypeName()}({Schema.GetTypeId()},{Schema.GetTypeVersion()})' is not a hash");
        }

        internal UdmGuid GetGuidValue()
        {
            ThrowIfInvalid();

            if (Schema.IsUnderlyingType<UdmGuid>())
                return *(UdmGuid*)Data;
            throw new InvalidOperationException($"This accessor with schema '{Schema.GetTypeName()}({Schema.GetTypeId()},{Schema.GetTypeVersion()})' is not a guid");
        }

        internal Reference GetReferenceValue()
        {
            ThrowIfInvalid();

            if (Schema.IsUnderlyingType<Reference>())
                return ((Reference*)References)[((ReferenceField*)Data)->Index];
            throw new InvalidOperationException($"This accessor with schema '{Schema.GetTypeName()}({Schema.GetTypeId()},{Schema.GetTypeVersion()})' is not a reference");
        }

        // SerializeHelper --> Add version for passing a instance value and an accessor --> Get/Set

        internal T GetNumericFieldValue<T>(string fieldName)
            where T : unmanaged /*, IBinaryNumber<T> */ // Uncomment this when .NET 8
        {
            ThrowIfInvalid();

            SchemaField field = Schema.GetFieldByName(fieldName);
            return GetNumericFieldValue<T>(field);
        }

        internal T GetNumericFieldValue<T>(SchemaField field)
            where T : unmanaged /*, IBinaryNumber<T> */ // Uncomment this when .NET 8
        {
            ThrowIfInvalid(field);

            var fieldSchema = field.GetSchema();
            unsafe
            {
                if (fieldSchema.IsUnderlyingType<T>() && TypeTraits<T>.Get().IsFundamentalType)
                {
                    ConstAccessor fieldAccessor = GetFieldAccessor(field);
                    return *(T*)fieldAccessor.Data;
                }

                throw new InvalidOperationException($"The field {field.GetName()} with schema '{Schema.GetTypeName()}({Schema.GetTypeId()},{Schema.GetTypeVersion()})' is not the expected fundamental type '{typeof(T).FullName}'.");
            }
        }

        internal T GetFieldValueUnsafe<T>(string fieldName)
            where T : unmanaged
        {
            ThrowIfInvalid();

            SchemaField field = Schema.GetFieldByName(fieldName);
            return GetFieldValueUnsafe<T>(field);
        }

        internal T GetFieldValueUnsafe<T>(SchemaField field)
            where T : unmanaged
        {
            ThrowIfInvalid(field);

            var fieldSchema = field.GetSchema();
            unsafe
            {
                if (fieldSchema.GetSize() == (ulong)sizeof(T))
                {
                    ConstAccessor fieldAccessor = GetFieldAccessor(field);
                    return *(T*)fieldAccessor.Data;
                }
                throw new InvalidOperationException($"The field {field.GetName()} with schema '{Schema.GetTypeName()}({Schema.GetTypeId()},{Schema.GetTypeVersion()})' doesn't have the same size as '{typeof(T).FullName}'");
            }
        }

        internal Hash GetHashFieldValue(string fieldName)
        {
            ThrowIfInvalid();

            SchemaField field = Schema.GetFieldByName(fieldName);
            return GetHashFieldValue(field);
        }

        internal Hash GetHashFieldValue(SchemaField field)
        {
            ThrowIfInvalid(field);

            var fieldSchema = field.GetSchema();
            unsafe
            {
                if (fieldSchema.IsUnderlyingType<Hash>())
                {
                    ConstAccessor fieldAccessor = GetFieldAccessor(field);
                    return *(Hash*)fieldAccessor.Data;
                }

                throw new InvalidOperationException($"The field {field.GetName()} with schema '{Schema.GetTypeName()}({Schema.GetTypeId()},{Schema.GetTypeVersion()})' is not a hash.");
            }
        }

        internal UdmGuid GetGuidFieldValue(string fieldName)
        {
            ThrowIfInvalid();

            SchemaField field = Schema.GetFieldByName(fieldName);
            return GetGuidFieldValue(field);
        }

        internal UdmGuid GetGuidFieldValue(SchemaField field)
        {
            ThrowIfInvalid(field);

            var fieldSchema = field.GetSchema();
            unsafe
            {
                if (fieldSchema.IsUnderlyingType<UdmGuid>())
                {
                    ConstAccessor fieldAccessor = GetFieldAccessor(field);
                    return *(UdmGuid*)fieldAccessor.Data;
                }

                throw new InvalidOperationException($"The field {field.GetName()} with schema '{Schema.GetTypeName()}({Schema.GetTypeId()},{Schema.GetTypeVersion()})' is not a guid.");
            }
        }

        internal Reference GetReferenceFieldValue(string fieldName)
        {
            ThrowIfInvalid();

            SchemaField field = Schema.GetFieldByName(fieldName);
            return GetReferenceFieldValue(field);
        }

        internal Reference GetReferenceFieldValue(SchemaField field)
        {
            ThrowIfInvalid(field);

            var fieldSchema = field.GetSchema();
            unsafe
            {
                if (fieldSchema.IsUnderlyingType<Reference>())
                {
                    ConstAccessor fieldAccessor = GetFieldAccessor(field);
                    return ((Reference*)References)[((ReferenceField*)fieldAccessor.Data)->Index];
                }

                throw new InvalidOperationException($"The field {field.GetName()} with schema '{Schema.GetTypeName()}({Schema.GetTypeId()},{Schema.GetTypeVersion()})' is not a reference.");
            }
        }

        internal ConstUTF8String GetUTF8StringValue()
        {
            ThrowIfInvalid();

            unsafe
            {
                if (Schema.IsUTF8String())
                    return new ConstUTF8String(this);

                throw new InvalidOperationException("This accessor is not a UTF8String.");
            }
        }

        internal ConstUTF8String GetUTF8StringFieldValue(string fieldName)
        {
            ThrowIfInvalid();

            SchemaField field = Schema.GetFieldByName(fieldName);
            return GetUTF8StringFieldValue(field);
        }

        internal ConstUTF8String GetUTF8StringFieldValue(SchemaField field)
        {
            ThrowIfInvalid(field);

            var fieldSchema = field.GetSchema();
            unsafe
            {
                if (fieldSchema.IsUTF8String())
                {
                    ConstAccessor fieldAccessor = GetFieldAccessor(field);
                    return new ConstUTF8String(fieldAccessor);
                }

                throw new InvalidOperationException($"The field {field.GetName()} is not a UTF8String.");
            }
        }

        internal ConstVector GetVectorValue()
        {
            ThrowIfInvalid();

            unsafe
            {
                if (Schema.GetFlags().HasFlag(SchemaFlags.IsVector))
                    return new ConstVector(this);

                throw new InvalidOperationException("This accessor is not a Vector.");
            }
        }

        internal ConstVector GetVectorFieldValue(string fieldName)
        {
            ThrowIfInvalid();

            SchemaField field = Schema.GetFieldByName(fieldName);
            return GetVectorFieldValue(field);
        }

        // TODO: Support Vector<T>
        internal ConstVector GetVectorFieldValue(SchemaField field)
        {
            ThrowIfInvalid(field);

            var fieldSchema = field.GetSchema();
            unsafe
            {
                if (fieldSchema.GetFlags().HasFlag(SchemaFlags.IsVector))
                {
                    ConstAccessor fieldAccessor = GetFieldAccessor(field);
                    return new ConstVector(fieldAccessor);
                }

                throw new InvalidOperationException($"The field {field.GetName()} is not a Vector.");
            }
        }

        internal ConstAccessor GetFieldAccessor(string fieldName)
        {
            ThrowIfInvalid();

            SchemaField field = Schema.GetFieldByName(fieldName);
            if (!field.IsValid())
            {
                return default;
            }
            return GetFieldAccessor(field);
        }

        internal ConstAccessor GetFieldAccessor(SchemaField field)
        {
            ThrowIfInvalid(field);
            unsafe
            {
                if (Schema.HasField(field))
                {
                    var fieldSchema = field.GetSchema();
                    return new ConstAccessor
                    {
                        Schema = fieldSchema,
                        Data = (IntPtr)((byte*)Data + field.GetOffset()),
                        References = References
                    };
                }
            }
            return default;
        }

        internal void ThrowIfInvalid()
        {
            if (!IsValid())
                throw new InvalidOperationException("Trying to use an invalid ConstAccessor");
        }

        internal void ThrowIfInvalid(SchemaField field)
        {
            ThrowIfInvalid();
            field.ThrowIfInvalid();
            GetSchema().ThrowIfInvalid(field);
        }

        internal Schema GetSchema()
        {
            return Schema;
        }

        public override bool Equals(object other)
        {
            if (other is ConstAccessor accessor)
            {
                return Equals(accessor);
            }
            return false;
        }

        public bool Equals(ConstAccessor other)
        {
            unsafe
            {
                fixed (ConstAccessor* accessorPtr = &this)
                {
                    return UdmInterop.Instance.udm_const_accessor_is_equal(accessorPtr, &other) != 0;
                }
            }
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Schema, Data);
        }

        public static bool operator ==(ConstAccessor lhs, ConstAccessor rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(ConstAccessor lhs, ConstAccessor rhs)
        {
            return !lhs.Equals(rhs);
        }

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
        internal Schema Schema;
        internal IntPtr Data;
        // Pointers to blittable types are not considered blittable by the bindings generator
        // internal Reference* References;
        internal IntPtr References;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
    }
}
