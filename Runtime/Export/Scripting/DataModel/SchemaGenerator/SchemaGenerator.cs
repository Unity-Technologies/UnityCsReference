// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;

namespace Unity.DataModel;

internal sealed class SchemaGenerator
{
    private readonly IReadOnlyList<TypeDefinitionData> TypeDefinitions;

    private struct SchemaCacheElement
    {
        internal bool Generated;
        internal Schema Value;
    }

    private SchemaCacheElement[] SchemaCache;

    internal SchemaGenerator(IReadOnlyList<TypeDefinitionData> types)
    {
        TypeDefinitions = types;
        SchemaCache = new SchemaCacheElement[types.Count];
    }

    internal void GenerateSchemas(IReadOnlyList<int> typeIndexOrder)
    {
        int generated = 0;
        int skipped = 0;
        foreach (var typeIndex in typeIndexOrder)
        {
            if (GetOrGenerateSchema(typeIndex).IsValid())
                ++generated;
            else
                ++skipped;
        }
    }


    private Schema GetOrGenerateSchema(int typeIndex)
    {
        if (SchemaCache[typeIndex].Generated)
            return SchemaCache[typeIndex].Value;

        SchemaCache[typeIndex].Generated = true;
        SchemaCache[typeIndex].Value = GenerateSchema(typeIndex);
        return SchemaCache[typeIndex].Value;
    }

    private static string TrimTypeNameRecursive(string typeName, int genericDepth)
    {
        var builder = new StringBuilder();
        for (int i = 0; i < typeName.Length; i++)
        {
            var c = typeName[i];

            if (c == '.') // Trim namespaces
            {
                builder.Clear();
            }
            else if (c == '`') // Skip generic arg count
            {
                while ((i + 1) < typeName.Length && Char.IsDigit(typeName[i + 1]))
                    i++;
            }
            else if (c == '+')
            {
                if (typeName[i..].StartsWith("+<>c")) // Many types are postfixed with +<>c, which we want to ignore
                    break;

                builder.Append('.');
            }
            else if (c == '<') // Recurse over generic type args
            {
                builder.Append(c);
                builder.Append(TrimTypeNameRecursive(typeName[(i + 1)..], genericDepth + 1));
                break;
            }
            else if (c == '>')
            {
                builder.Append(c);
                if (genericDepth == 0)
                    break;
            }
            else if (c == ',')
            {
                builder.Append(c);
                builder.Append(TrimTypeNameRecursive(typeName[(i + 1)..], genericDepth));
                break;
            }
            else if (Char.IsLetterOrDigit(c) || c == '_' || c == '[' || c == ']')
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    // Example:
    // Input: MyNamespace.GenericType`2<OtherGenericType'1<SimpleType>,OuterType+InnerType>
    // Output: GenericType<OtherGenericType<SimpleType>,OuterType.InnerType>
    private static string TrimTypeName(string typeName)
    {
        return TrimTypeNameRecursive(typeName, genericDepth: 0);
    }

    private Schema GenerateSchema(int typeIndex)
    {
        var typeDefinition = TypeDefinitions[typeIndex];

        if ((typeDefinition.Flags & TypeDefinitionDataFlags.System) != 0)
        {
            var nativeSchema = typeDefinition.NativeSchema;
            if (nativeSchema.ShouldExist)
            {
                if (nativeSchema.Exists)
                    return nativeSchema.Value;
                else
                    // If this exception is thrown, it means that we are generating native schemas too late, and we need to figure out why.
                    throw new InvalidOperationException($"Type {typeDefinition.Name} expected to have a native schema, but none found.");
            }

            return default;
        }

        if (!typeDefinition.IsSerializable && (typeDefinition.Flags & TypeDefinitionDataFlags.SerializableAsBaseClass) == 0)
            return default;

        if ((typeDefinition.Flags & TypeDefinitionDataFlags.OpenGeneric) != 0)
            return default;

        var typeID = typeDefinition.TypeID.IsValid() ? typeDefinition.TypeID : GetIdFromTypeDefinition(typeDefinition);
        var typeName = TrimTypeName(typeDefinition.Name);

        SchemaBuilder builder;
        var schemaManager = UdmInterop.Instance.udm_get_schema_manager();
        ulong baseSchemaOffset = 0;


        if (typeDefinition.BaseIndex > 0)
        {
            var baseSchema = GetOrGenerateSchema(typeDefinition.BaseIndex);
            if (baseSchema.IsValid())
            {
                if (typeDefinition.OverrideLayout && baseSchema.GetFieldCount() > 0)
                {
                    var lastFieldOfBaseSchema = baseSchema.GetFields().GetFieldByIndex(baseSchema.GetFieldCount() - 1);
                    baseSchemaOffset = lastFieldOfBaseSchema.GetOffset() + lastFieldOfBaseSchema.GetSchema().GetSize();
                }

                builder = new SchemaBuilder(schemaManager, baseSchema, typeName, typeID, 1, typeDefinition.TypeLayout, typeDefinition.IsFixedBuffer);
            }
            else
                builder = new SchemaBuilder(schemaManager, typeName, typeID, 1, typeDefinition.TypeLayout, typeDefinition.IsFixedBuffer);
        }
        else
        {
            builder = new SchemaBuilder(schemaManager, typeName, typeID, 1, typeDefinition.TypeLayout, typeDefinition.IsFixedBuffer);
        }

        try
        {
            unsafe
            {
                AddFields(builder, baseSchemaOffset, ref typeDefinition);
                var schema = builder.BuildSchema(false);

                if ((typeDefinition.Flags & TypeDefinitionDataFlags.ImplementsIBufferElementData) != 0)
                {
                    SchemaBuilder.BuildVectorSchema(schemaManager, schema, new[] { VectorType.DynamicBuffer });
                }
                return schema;
            }
        }
        catch (Exception e)
        {
            Console.Error.Write(e.ToString());
        }
        finally
        {
            builder.Dispose();
        }
        return default;
    }

    private void AddFields(SchemaBuilder builder, ulong baseSchemaOffset, ref TypeDefinitionData typeDefinition)
    {
    }


    private static UdmTypeId GetIdFromTypeDefinition(TypeDefinitionData typeDefinition)
    {
        var fullName = typeDefinition.Assembly + "@" + typeDefinition.Name;
        return UdmTypeId.HashString(fullName);
    }
}
