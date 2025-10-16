using System;
using System.Runtime.InteropServices;
using System.Numerics;

namespace Unity.DataModel
{
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct Accessor : IEquatable<Accessor>
{
    internal bool IsValid()
    {
        unsafe
        {
            return Schema.IsValid() && Data != IntPtr.Zero;
        }
    }

    internal T GetNumericValue<T>() where T : unmanaged /*, IBinaryNumber<T> */ // Uncomment this when .NET 8
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
            return *(Reference*)Data;
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
                Accessor fieldAccessor = GetFieldAccessor(field);
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
                Accessor fieldAccessor = GetFieldAccessor(field);
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
                Accessor fieldAccessor = GetFieldAccessor(field);
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
                Accessor fieldAccessor = GetFieldAccessor(field);
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
                Accessor fieldAccessor = GetFieldAccessor(field);
                return *(Reference*)fieldAccessor.Data;
            }

            throw new InvalidOperationException($"The field {field.GetName()} with schema '{Schema.GetTypeName()}({Schema.GetTypeId()},{Schema.GetTypeVersion()})' is not a reference.");
        }
    }

    internal void SetNumericValue<T>(in T value)
        where T : unmanaged /*, IBinaryNumber<T> */ // Uncomment this when .NET 8

    {
        ThrowIfInvalid();

        unsafe
        {
            if (Schema.IsUnderlyingType<T>() && TypeTraits<T>.Get().IsFundamentalType)
                *(T*)Data = value;
            else
                throw new InvalidOperationException($"This accessor with schema '{Schema.GetTypeName()}({Schema.GetTypeId()},{Schema.GetTypeVersion()})' is not the expected fundamental type '{typeof(T).FullName}'.");
        }
    }

    internal void SetHashValue(in Hash value)
    {
        ThrowIfInvalid();

        unsafe
        {
            if (Schema.IsUnderlyingType<Hash>())
                *(Hash*)Data = value;
            else
                throw new InvalidOperationException($"This accessor with schema '{Schema.GetTypeName()}({Schema.GetTypeId()},{Schema.GetTypeVersion()})' is not a hash.");
        }
    }

    internal void SetGuidValue(in UdmGuid value)
    {
        ThrowIfInvalid();

        unsafe
        {
            if (Schema.IsUnderlyingType<UdmGuid>())
                *(UdmGuid*)Data = value;
            else
                throw new InvalidOperationException($"This accessor with schema '{Schema.GetTypeName()}({Schema.GetTypeId()},{Schema.GetTypeVersion()})' is not a guid.");
        }
    }

    internal void SetReferenceValue(in Reference value)
    {
        ThrowIfInvalid();

        unsafe
        {
            if (Schema.IsUnderlyingType<Reference>())
                *(Reference*)Data = value;
            else
                throw new InvalidOperationException($"This accessor with schema '{Schema.GetTypeName()}({Schema.GetTypeId()},{Schema.GetTypeVersion()})' is not a reference.");
        }
    }

    internal void SetValueUnsafe<T>(in T value) where T : unmanaged
    {
        ThrowIfInvalid();

        unsafe
        {
            if (Schema.GetSize() == (ulong)sizeof(T))
                *(T*)Data = value;
            else
                throw new InvalidOperationException($"This accessor with schema '{Schema.GetTypeName()}({Schema.GetTypeId()},{Schema.GetTypeVersion()})' doesn't have the same size as '{typeof(T).FullName}'.");
        }
    }

    internal void SetNumericFieldValue<T>(string fieldName, T value)
        where T : unmanaged // , IBinaryNumber<T> // Uncomment this when .NET 8
    {   
        ThrowIfInvalid();

        SchemaField field = Schema.GetFieldByName(fieldName);
        SetNumericFieldValue<T>(field, value);
    }

    internal void SetNumericFieldValue<T>(SchemaField field, T value)
        where T : unmanaged //, IBinaryNumber<T>  // Uncomment this when .NET 8
    {
        ThrowIfInvalid(field);

        var fieldSchema = field.GetSchema();
        unsafe
        {
            if (fieldSchema.IsUnderlyingType<T>() && TypeTraits<T>.Get().IsFundamentalType)
            {
                Accessor fieldAccessor = GetFieldAccessor(field);
                *(T*)fieldAccessor.Data = value;
            }
            else
                throw new InvalidOperationException($"The field {field.GetName()} with schema '{Schema.GetTypeName()}({Schema.GetTypeId()},{Schema.GetTypeVersion()})' is not the expected fundamental type '{typeof(T).FullName}'.");
        }
    }
    internal void SetHashFieldValue(string fieldName, Hash value)
    {
        ThrowIfInvalid();

        SchemaField field = Schema.GetFieldByName(fieldName);
        SetHashFieldValue(field, value);
    }

    internal void SetHashFieldValue(SchemaField field, Hash value)
    {
        ThrowIfInvalid(field);

        var fieldSchema = field.GetSchema();
        unsafe
        {
            if (fieldSchema.IsUnderlyingType<Hash>())
            {
                Accessor fieldAccessor = GetFieldAccessor(field);
                *(Hash*)fieldAccessor.Data = value;
            }
            else
                throw new InvalidOperationException($"The field {field.GetName()} with schema '{Schema.GetTypeName()}({Schema.GetTypeId()},{Schema.GetTypeVersion()})' is not a hash.");
        }
    }

    internal void SetGuidFieldValue(string fieldName, UdmGuid value)
    {
        ThrowIfInvalid();

        SchemaField field = Schema.GetFieldByName(fieldName);
        SetGuidFieldValue(field, value);
    }

    internal void SetGuidFieldValue(SchemaField field, UdmGuid value)
    {
        ThrowIfInvalid(field);

        var fieldSchema = field.GetSchema();
        unsafe
        {
            if (fieldSchema.IsUnderlyingType<UdmGuid>())
            {
                Accessor fieldAccessor = GetFieldAccessor(field);
                *(UdmGuid*)fieldAccessor.Data = value;
            }
            else
                throw new InvalidOperationException($"The field {field.GetName()} with schema '{Schema.GetTypeName()}({Schema.GetTypeId()},{Schema.GetTypeVersion()})' is not a guid.");
        }
    }

    internal void SetReferenceFieldValue(string fieldName, Reference value)
    {
        ThrowIfInvalid();

        SchemaField field = Schema.GetFieldByName(fieldName);
        SetReferenceFieldValue(field, value);
    }

    internal void SetReferenceFieldValue(SchemaField field, Reference value)
    {
        ThrowIfInvalid(field);

        var fieldSchema = field.GetSchema();
        unsafe
        {
            if (fieldSchema.IsUnderlyingType<Reference>())
            {
                Accessor fieldAccessor = GetFieldAccessor(field);
                *(Reference*)fieldAccessor.Data = value;
            }
            else
                throw new InvalidOperationException($"The field {field.GetName()} with schema '{Schema.GetTypeName()}({Schema.GetTypeId()},{Schema.GetTypeVersion()})' is not a reference.");
        }
    }

    internal void SetFieldValueUnsafe<T>(string fieldName, T value)
        where T : unmanaged
    {
        ThrowIfInvalid();

        SchemaField field = Schema.GetFieldByName(fieldName);
        SetFieldValueUnsafe<T>(field, value);
    }

    internal void SetFieldValueUnsafe<T>(SchemaField field, T value)
        where T : unmanaged
    {
        ThrowIfInvalid(field);

        var fieldSchema = field.GetSchema();
        unsafe
        {
            if (fieldSchema.GetSize() == (ulong)sizeof(T))
            {
                Accessor fieldAccessor = GetFieldAccessor(field);
                *(T*)fieldAccessor.Data = value;
            }
            else
                throw new InvalidOperationException($"The field {field.GetName()} with schema '{Schema.GetTypeName()}({Schema.GetTypeId()},{Schema.GetTypeVersion()})' is not the expected fundamental type '{typeof(T).FullName}'.");
        }
    }

    internal UTF8String GetUTF8StringValue()
    {
        ThrowIfInvalid();

        unsafe
        {
            if (Schema.IsUTF8String())
                return new UTF8String(this);

            throw new InvalidOperationException($"This accessor with schema '{Schema.GetTypeName()}({Schema.GetTypeId()},{Schema.GetTypeVersion()})' is not a UTF8String.");
        }
    }

    internal void SetUTF8StringValue(ConstUTF8String value)
    {
        var result = GetUTF8StringValue();
        result.Set(value);
    }

    internal void SetUTF8StringValue(string value)
    {
        var result = GetUTF8StringValue();
        result.Set(value);
    }

    internal UTF8String GetUTF8StringFieldValue(string fieldName)
    {
        ThrowIfInvalid();

        SchemaField field = Schema.GetFieldByName(fieldName);
        return GetUTF8StringFieldValue(field);
    }

    internal UTF8String GetUTF8StringFieldValue(SchemaField field)
    {
        ThrowIfInvalid(field);

        var fieldSchema = field.GetSchema();
        unsafe
        {
            if (fieldSchema.IsUTF8String())
            {
                Accessor fieldAccessor = GetFieldAccessor(field);
                return new UTF8String(fieldAccessor);
            }

            throw new InvalidOperationException($"The field {field.GetName()} with schema '{Schema.GetTypeName()}({Schema.GetTypeId()},{Schema.GetTypeVersion()})' is not a UTF8String."); 
        }
    }

    internal void SetUTF8StringFieldValue(string fieldName, string value)
    {
        var result = GetUTF8StringFieldValue(fieldName);
        result.Set(value);
    }

    internal void SetUTF8StringFieldValue(SchemaField field, string value)
    {
        var result = GetUTF8StringFieldValue(field);
        result.Set(value);
    }

    internal Vector GetVectorValue()
    {
        ThrowIfInvalid();

        unsafe
        {
            if (Schema.GetFlags().HasFlag(SchemaFlags.IsVector))
                return new Vector(this);

            throw new InvalidOperationException($"This accessor with schema '{Schema.GetTypeName()}({Schema.GetTypeId()},{Schema.GetTypeVersion()})' is not a Vector.");
        }
    }

    internal Vector GetVectorFieldValue(string fieldName)
    {
        ThrowIfInvalid();

        SchemaField field = Schema.GetFieldByName(fieldName);
        return GetVectorFieldValue(field);
    }

    // TODO: Support Vector<T>
    internal Vector GetVectorFieldValue(SchemaField field)
    {
        ThrowIfInvalid(field);

        var fieldSchema = field.GetSchema();
        unsafe
        {
            if (fieldSchema.GetFlags().HasFlag(SchemaFlags.IsVector))
            {
                Accessor fieldAccessor = GetFieldAccessor(field);
                return new Vector(fieldAccessor);
            }

            throw new InvalidOperationException($"The field {field.GetName()} with schema '{Schema.GetTypeName()}({Schema.GetTypeId()},{Schema.GetTypeVersion()})' is not a Vector."); 
        }
    }

    internal Accessor GetFieldAccessor(string fieldName)
    {
        ThrowIfInvalid();

        SchemaField field = Schema.GetFieldByName(fieldName);
        if (!field.IsValid())
        {
            return default;
        }
        return GetFieldAccessor(field);
    }

    internal Accessor GetFieldAccessor(SchemaField field)
    {
        ThrowIfInvalid(field);
        unsafe
        {
            if (Schema.HasField(field))
            {
                var fieldSchema = field.GetSchema();
                return new Accessor
                {
                    Schema = fieldSchema,
                    Data = (IntPtr)((byte*)Data + field.GetOffset()),
                    DocumentModel = DocumentModel
                };
            }
        }
        return default;
    }

    internal void ThrowIfInvalid()
    {
        if (!IsValid())
            throw new InvalidOperationException("Trying to use an invalid Accessor");
    }

    internal void ThrowIfInvalid(SchemaField field)
    {
        ThrowIfInvalid();
        field.ThrowIfInvalid();
        GetSchema().ThrowIfInvalid(field);
    }

    internal Schema GetSchema()
    {
        ThrowIfInvalid();
        return Schema;
    }

    internal IntPtr GetDataPtr()
    {
        ThrowIfInvalid();
        return Data;
    }

    public override bool Equals(object other)
    {
        if (other is Accessor accessor)
        {
            return Equals(accessor);
        }
        return false;
    }

    public bool Equals(Accessor other)
    {
        unsafe
        {
            var constOther = (ConstAccessor)other;
            var constThis = (ConstAccessor)this;
            return UdmInterop.Instance.udm_const_accessor_is_equal(&constOther, &constThis) != 0;
        }
    }
    public override int GetHashCode()
    {
        return HashCode.Combine(Schema, Data);
    }

    public static bool operator ==(Accessor lhs, Accessor rhs)
    {
        return lhs.Equals(rhs);
    }

    public static bool operator !=(Accessor lhs, Accessor rhs)
    {
        return !lhs.Equals(rhs);
    }

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
    internal Schema Schema;
    internal IntPtr Data;
    internal DocumentModel DocumentModel;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
}
}
