#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using udm_schema_builder_ptr = System.IntPtr;

namespace Unity.DataModel
{ 
internal enum SerializeFrequency
{
    Always,
    OnlyDuringDomainReload,
    Never
}

 internal enum VectorType
{
    None = 0,
    Array = 1,
    List = 2,
    DynamicBuffer = 3
}

internal unsafe struct SchemaBuilder : IDisposable
{
    internal SchemaBuilder(IntPtr schema_manager_ptr, string typeName, UdmTypeId typeID, ulong typeVersion, TypeLayout typeLayout = default, bool isFixedBuffer = false, bool inlineTextSerialization = false)
    {
        if (String.IsNullOrEmpty(typeName))
            throw new ArgumentException("The type name cannot be null or an empty string.");

        SchemaManagerPtr = schema_manager_ptr;
        unsafe
        {
            SchemaBuilderPtr = UdmInterop.Instance.udm_schema_builder_new(schema_manager_ptr, null, typeName, &typeID, typeVersion, typeLayout);
        }
    }

    internal SchemaBuilder(IntPtr schema_manager_ptr, Schema baseSchema, string typeName, UdmTypeId typeID, ulong typeVersion, TypeLayout typeLayout = default, bool isFixedBuffer = false, bool inlineTextSerialization = false)
    {
        if (String.IsNullOrEmpty(typeName))
            throw new ArgumentException("The type name cannot be null or an empty string.");

        SchemaManagerPtr = schema_manager_ptr;
        unsafe
        {
            SchemaBuilderPtr = UdmInterop.Instance.udm_schema_builder_new(schema_manager_ptr, (SchemaImpl*)baseSchema.SchemaPtr, typeName, &typeID, typeVersion, typeLayout);
        }
    }

    internal SchemaBuilder AddFundamentalField<T>(string fieldName, int explicitOffset = -1, SerializeFrequency serializeFrequency = SerializeFrequency.Always, T defaultValue = default)
        where T : unmanaged
    {
        if (String.IsNullOrEmpty(fieldName))
            throw new ArgumentException("The name of fieldName can't be null or an empty string");

        ThrowIfInvalid();

        var data = TypeTraits<T>.Get();
        if (!data.IsFundamentalType)
            throw new InvalidOperationException($"This function is only for fundamental types and {data.TypeName} is not a fundamental type.");
        if (!data.Schema.IsValid())
            throw new InvalidOperationException($"This function requires Schema for type {data.TypeName}.");
        unsafe
        {
            var defaultValueAccessor = new ConstAccessor
            {
                Schema = data.Schema,
                Data = (IntPtr)(&defaultValue)
            };
            AddSchemaField(fieldName, defaultValueAccessor, explicitOffset, serializeFrequency);
        }
        return this;
    }

    // TODO: DEFAULT VALUES
    internal SchemaBuilder AddFundamentalVectorField<T>(string fieldName, VectorType[] vectorLevels, int explicitOffset = -1, SerializeFrequency serializeFrequency = SerializeFrequency.Always, ulong vectorRank = 1)
        where T : unmanaged
    {
        if (String.IsNullOrEmpty(fieldName))
            throw new ArgumentException("The name of fieldName can't be null or an empty string");
        ThrowIfInvalid();
        var data = TypeTraits<T>.Get();
        if (!data.IsFundamentalType)
            throw new InvalidOperationException($"This function is only for fundamental types and {data.TypeName} is not a fundamental type.");
        var schema = BuildVectorSchema(SchemaManagerPtr, data.Schema, vectorLevels);
        AddSchemaField(fieldName, schema, explicitOffset, serializeFrequency);
        return this;
    }

    internal SchemaBuilder AddHashField(string fieldName, int explicitOffset = -1, SerializeFrequency serializeFrequency = SerializeFrequency.Always, Hash defaultValue = default)
    {
        if (String.IsNullOrEmpty(fieldName))
            throw new ArgumentException("The name of fieldName can't be null or an empty string");
        ThrowIfInvalid();

        unsafe
        {
            var defaultValueAccessor = new ConstAccessor
            {
                Schema = TypeTraits<Hash>.Get().Schema,
                Data = (IntPtr)(&defaultValue)
            };
            AddSchemaField(fieldName, defaultValueAccessor, explicitOffset, serializeFrequency);
        }
        return this;
    }
    
    internal SchemaBuilder AddHashVectorField(string fieldName, VectorType[] vectorLevels, int explicitOffset = -1, SerializeFrequency serializeFrequency = SerializeFrequency.Always, ulong vectorRank = 1)
    {
        if (String.IsNullOrEmpty(fieldName))
            throw new ArgumentException("The name of fieldName can't be null or an empty string");
        ThrowIfInvalid();

        var schema = BuildVectorSchema(SchemaManagerPtr, TypeTraits<Hash>.Get().Schema, vectorLevels);
        AddSchemaField(fieldName, schema, explicitOffset, serializeFrequency);

        return this;
    }

    internal SchemaBuilder AddGuidField(string fieldName, int explicitOffset = -1, SerializeFrequency serializeFrequency = SerializeFrequency.Always, UdmGuid defaultValue = default)
    {
        if (String.IsNullOrEmpty(fieldName))
            throw new ArgumentException("The name of fieldName can't be null or an empty string");
        ThrowIfInvalid();

        unsafe
        {
            var defaultValueAccessor = new ConstAccessor
            {
                Schema = TypeTraits<UdmGuid>.Get().Schema,
                Data = (IntPtr)(&defaultValue)
            };
            AddSchemaField(fieldName, defaultValueAccessor, explicitOffset, serializeFrequency);
        }
        return this;
    }

    internal SchemaBuilder AddGuidVectorField(string fieldName, VectorType[] vectorLevels, int explicitOffset = -1, SerializeFrequency serializeFrequency = SerializeFrequency.Always, ulong vectorRank = 1)
    {
        if (String.IsNullOrEmpty(fieldName))
            throw new ArgumentException("The name of fieldName can't be null or an empty string");
        ThrowIfInvalid();

        var schema = BuildVectorSchema(SchemaManagerPtr, TypeTraits<UdmGuid>.Get().Schema, vectorLevels);
        AddSchemaField(fieldName, schema, explicitOffset, serializeFrequency);

        return this;
    }

    internal SchemaBuilder AddReferenceField(string fieldName, int explicitOffset = -1, SerializeFrequency serializeFrequency = SerializeFrequency.Always, Reference defaultValue = default)
    {
        if (String.IsNullOrEmpty(fieldName))
            throw new ArgumentException("The name of fieldName can't be null or an empty string");
        ThrowIfInvalid();

        unsafe
        {
            var defaultValueAccessor = new ConstAccessor
            {
                Schema = TypeTraits<Reference>.Get().Schema,
                Data = (IntPtr)(&defaultValue)
            };
            AddSchemaField(fieldName, defaultValueAccessor, explicitOffset, serializeFrequency);
        }
        return this;
    }

    // TODO: DEFAULT VALUES
    internal SchemaBuilder AddReferenceVectorField(string fieldName, UdmTypeId elementTypeId, string elementTypeName, VectorType[] vectorLevels, int explicitOffset = -1, SerializeFrequency serializeFrequency = SerializeFrequency.Always)
    {
        if (String.IsNullOrEmpty(fieldName))
            throw new ArgumentException("The name of fieldName can't be null or an empty string");
        ThrowIfInvalid();

        var schema = BuildReferenceVectorSchema(SchemaManagerPtr, elementTypeId, elementTypeName, vectorLevels);
        AddSchemaField(fieldName, schema, explicitOffset, serializeFrequency);

        return this;
    }

    // Pointer fields are always only treated as padding: values are not serialized
    internal SchemaBuilder AddPointerField(string fieldName, int explicitOffset = -1, IntPtr defaultValue = default)
    {
        if (String.IsNullOrEmpty(fieldName))
            throw new ArgumentException("The name of fieldName can't be null or an empty string");
        ThrowIfInvalid();
        
        Schema schema;
        if (sizeof(IntPtr) == 64)
            schema = TypeTraits<long>.Get().Schema;
        else
            schema = TypeTraits<int>.Get().Schema;

        unsafe
        {
            var defaultValueAccessor = new ConstAccessor
            {
                Schema = schema,
                Data = (IntPtr)(&defaultValue)
            };
            AddSchemaField(fieldName, defaultValueAccessor, explicitOffset, SerializeFrequency.Always, true);
        }
        return this;
    }

    // Pointer fields are always only treated as padding: values are not serialized
    internal SchemaBuilder AddPointerVectorField(string fieldName, VectorType[] vectorLevels, int explicitOffset = -1)
    {
        if (String.IsNullOrEmpty(fieldName))
            throw new ArgumentException("The name of fieldName can't be null or an empty string");
        ThrowIfInvalid();

        Schema schema;
        if (sizeof(IntPtr) == 64)
            schema = BuildVectorSchema(SchemaManagerPtr, TypeTraits<long>.Get().Schema, vectorLevels);
        else
            schema = BuildVectorSchema(SchemaManagerPtr, TypeTraits<int>.Get().Schema, vectorLevels);
        
        AddSchemaField(fieldName, schema, explicitOffset, SerializeFrequency.Always, true);

        return this;
    }

    // TODO: DEFAULT VALUES
    internal SchemaBuilder AddUTF8StringField(string fieldName, int explicitOffset = -1, SerializeFrequency serializeFrequency = SerializeFrequency.Always)
    {
        if (String.IsNullOrEmpty(fieldName))
            throw new ArgumentException("The name of fieldName can't be null or an empty string");
        ThrowIfInvalid();

        unsafe
        {
            AddSchemaField(fieldName, TypeTraits<string>.Get().Schema, explicitOffset, serializeFrequency);
        }
        return this;
	}

    // TODO: DEFAULT VALUES
    internal SchemaBuilder AddUTF8StringVectorField(string fieldName, VectorType[] vectorLevels, int explicitOffset = -1, SerializeFrequency serializeFrequency = SerializeFrequency.Always, ulong vectorRank = 1)
    {
        if (String.IsNullOrEmpty(fieldName))
            throw new ArgumentException("The name of fieldName can't be null or an empty string");
        ThrowIfInvalid();
        var schema = BuildVectorSchema(SchemaManagerPtr, TypeTraits<string>.Get().Schema, vectorLevels);
        AddSchemaField(fieldName, schema, explicitOffset, serializeFrequency);

        return this;
    }

    internal SchemaBuilder AddSchemaField(string fieldName, Schema schema, int explicitOffset = -1, SerializeFrequency serializeFrequency = SerializeFrequency.Always, bool treatAsPadding = false)
    {
        var accessor = schema.GetAccessor();
        return AddSchemaField(fieldName, accessor, explicitOffset, serializeFrequency, treatAsPadding);
    }

    internal SchemaBuilder AddSchemaField(string fieldName, ConstAccessor defaultValue, int explicitOffset = -1, SerializeFrequency serializeFrequency = SerializeFrequency.Always, bool treatAsPadding = false)
    {
        if (String.IsNullOrEmpty(fieldName))
            throw new ArgumentException("The name of fieldName can't be null or an empty string");
        ThrowIfInvalid();

        SchemaFieldFlags flags = SchemaFieldFlags.IsManaged;
        switch (serializeFrequency)
        {
            case SerializeFrequency.OnlyDuringDomainReload:
                flags.SetFlag(SchemaFieldFlags.SerializeOnlyDuringDomainReload);
                break;
            case SerializeFrequency.Never:
                flags.SetFlag(SchemaFieldFlags.DontSerialize);
                break;
        }

        if (treatAsPadding)
            flags |= SchemaFieldFlags.TreatAsPadding;

        UdmInterop.Instance.udm_schema_builder_add_field(SchemaBuilderPtr,
            fieldName,
            explicitOffset,
            defaultValue,
            flags);
        return this;
    }

    // TODO: DEFAULT VALUES
    internal SchemaBuilder AddSchemaVectorField(string fieldName, Schema elementSchema, VectorType[] vectorLevels, int explicitOffset = -1, SerializeFrequency serializeFrequency = SerializeFrequency.Always)
    {
        if (String.IsNullOrEmpty(fieldName))
            throw new ArgumentException("The name of fieldName can't be null or an empty string");
        ThrowIfInvalid();
        var schema = BuildVectorSchema(SchemaManagerPtr, elementSchema, vectorLevels);
        AddSchemaField(fieldName, schema, explicitOffset, serializeFrequency);

        return this;
    }

    internal SchemaBuilder AddTypelessField<T>(string fieldName, int explicitOffset = -1, SerializeFrequency serializeFrequency = SerializeFrequency.Always, byte[]? defaultValue = default)
        where T : unmanaged
    {
        if (String.IsNullOrEmpty(fieldName))
            throw new ArgumentException("The name of fieldName can't be null or an empty string");

        ThrowIfInvalid();

        SchemaFieldFlags flags = SchemaFieldFlags.IsManaged | SchemaFieldFlags.IsTypeless;
        switch (serializeFrequency)
        {
            case SerializeFrequency.OnlyDuringDomainReload:
                flags.SetFlag(SchemaFieldFlags.SerializeOnlyDuringDomainReload);
                break;
            case SerializeFrequency.Never:
                flags.SetFlag(SchemaFieldFlags.DontSerialize);
                break;
        }

        Schema uint8VectorSchema = BuildVectorSchema(SchemaManagerPtr, TypeTraits<byte>.Get().Schema, new[] { VectorType.Array });
        using var document = DocumentModel.CreateNew();
        var objectModel = document.CreateObjectModel(uint8VectorSchema);
        var accessor = objectModel.GetAccessor();
        var vector = new Vector(accessor);
        fixed (byte* defaultValuePtr = defaultValue)
        {
            vector.Assign(defaultValuePtr, defaultValue != null ? (ulong)defaultValue.Length : 0);
        }

        UdmInterop.Instance.udm_schema_builder_add_field(SchemaBuilderPtr,
            fieldName,
            explicitOffset,
            objectModel.GetConstAccessor(),
            flags);

        return this;
    }

    internal ulong GetFieldCount()
    {
        ThrowIfInvalid();
        unsafe
        {
            return UdmInterop.Instance.udm_schema_builder_get_fields_count(SchemaBuilderPtr);
        }
    }

    internal Schema BuildSchema(bool disposeBuilder = true)
    {
        ThrowIfInvalid();

        Schema schema = UdmInterop.Instance.udm_schema_builder_build_schema(SchemaBuilderPtr);
        if (disposeBuilder)
            Dispose();

        return schema;
    }

    // TODO: move this out of the SchemaBuilder?
    internal static UdmTypeId GetVectorTypeId(UdmTypeId elementTypeId, VectorType vectorType)
    {
        var vectorTypeName = elementTypeId.ToHex() + vectorType.ToString();
        return UdmTypeId.HashString(vectorTypeName);
    }

    internal static string GetVectorTypeName(string elementTypeName, VectorType vectorType)
    {
        StringBuilder sb = new StringBuilder();
        string postfix = string.Empty;
        switch (vectorType)
        {
            case VectorType.Array:
                postfix += "[]";
                break;
            case VectorType.List:
                sb.Append($"List<");
                postfix += ">";
                break;
            case VectorType.DynamicBuffer:
                sb.Append($"DynamicBuffer<");
                postfix += ">";
                break;
            default:
                throw new ArgumentException($"Unsupported vector type: {vectorType}");
        }

        sb.Append(elementTypeName);
        sb.Append(postfix);
        
        return sb.ToString();
    }

    private static Schema BuildVectorSchemaInternal(IntPtr schemaManager, Schema elementSchema, VectorType[] vectorLevels, int startIndex)
    {
        for (int i = startIndex; i < vectorLevels.Length; i++)
        {
            var elementTypeId = elementSchema.GetTypeId();
            var elementTypeName = elementSchema.GetTypeName().ToString();
            elementSchema = BuildSingleVectorSchema(schemaManager, elementSchema.SchemaPtr, elementTypeId, elementTypeName, vectorLevels[i]);
        }
        return elementSchema;
    }

    private static Schema BuildSingleVectorSchema(IntPtr schemaManager, IntPtr baseSchemaPtr, UdmTypeId elementTypeId, string elementTypeName, VectorType vectorType)
    {
        var typeId = GetVectorTypeId(elementTypeId, vectorType);
        var typeName = GetVectorTypeName(elementTypeName, vectorType);
        return UdmInterop.Instance.udm_schema_builder_build_vector_schema(schemaManager, (SchemaImpl*)baseSchemaPtr, typeName, &typeId, 1);
    }

    internal static Schema BuildReferenceVectorSchema(IntPtr schemaManager, UdmTypeId elementTypeId, string elementTypeName, VectorType[] vectorLevels)
    {
        // For vectors of reference types, we always use the generic reference schema as underlying schema
        Schema elementSchema = BuildSingleVectorSchema(schemaManager, TypeTraits<Reference>.Get().Schema.SchemaPtr, elementTypeId, elementTypeName, vectorLevels[0]);

        return BuildVectorSchemaInternal(schemaManager, elementSchema, vectorLevels, startIndex: 1);
    }

    internal static Schema BuildVectorSchema(IntPtr schemaManager, Schema elementSchema, VectorType[] vectorLevels)
    {
        return BuildVectorSchemaInternal(schemaManager, elementSchema, vectorLevels, startIndex: 0);
    }

    internal static Schema BuildBasicSchemaWithUnderlyingType(IntPtr schemaManager, UdmTypeId underlyingTypeId, string typeName, UdmTypeId typeId, ulong typeVersion)
        {
            if (String.IsNullOrEmpty(typeName))
                throw new ArgumentException("The type name cannot be null or an empty string.");

            return UdmInterop.Instance.udm_schema_builder_build_basic_schema_with_underlying_type(schemaManager, &underlyingTypeId, typeName, &typeId, typeVersion, 1);
        }

    internal void ThrowIfInvalid()
    {
        if (!IsValid())
            throw new InvalidOperationException("Trying to use a deleted or not initialized SchemaBuilder");
    }

    internal readonly bool IsValid()
    {
        return SchemaBuilderPtr != IntPtr.Zero;
    }

    public void Dispose()
    {
        if (IsValid())
        {
            UdmInterop.Instance.udm_schema_builder_delete(SchemaBuilderPtr);
            SchemaBuilderPtr = IntPtr.Zero;
        }
    }

    internal udm_schema_builder_ptr SchemaBuilderPtr;
    internal IntPtr SchemaManagerPtr;
}
}
