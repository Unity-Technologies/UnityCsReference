// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

using Unity.EntitiesLike;
using UnityEngine;

namespace Unity.DataModel;

internal static class RttiBuilder
{
    public class RttiNotFoundException(string message) : Exception(message);

    internal static Rtti CreateNativeRtti(Schema schema, Type managedType)
    {
        if (managedType == null)
            managedType = typeof(UnityEngine.Object);

        return new UnityObjectRtti(managedType, schema);
    }

    internal static RttiGroup CalculateManagedRttiGroup(Type type)
    {
        if (type.IsSubclassOf(typeof(UnityEngine.Object)))
            return RttiGroup.UnityObject;

        if (typeof(ISimpleNativeType).IsAssignableFrom(type))
            return RttiGroup.SimpleNativeType;

        if (typeof(PureEntity).IsAssignableFrom(type))
            return RttiGroup.PureEntity;
        if (type == typeof(string) || typeof(Array).IsAssignableFrom(type))
            return RttiGroup.ImmutablePureManaged;

        return RttiGroup.PureManaged;
    }

    internal static Rtti CreateManagedRtti(Type type, Schema schema, out RttiGroup rttiGroup)
    {
        rttiGroup = CalculateManagedRttiGroup(type);
        if (rttiGroup == RttiGroup.PureEntity)
        {
            return new PureEntityRtti(type, schema);
        }
        else if (rttiGroup == RttiGroup.SimpleNativeType)
        {
            return new SimpleNativeTypeObjectRtti(type, schema);
        }

        if (rttiGroup == RttiGroup.UnityObject)
        {
            var nativeTypeID = UnityHybridObjectRtti.GetNativeTypeID(type);
            var nativeSchema = Schema.GetSchemaByType(nativeTypeID, 0);
            // There must be a valid native schema.
            // If there isn't, it could mean that the user is trying to extend a
            // native abstract type (like Behaviour or Component)
            if (nativeTypeID.IsValid() && !nativeSchema.IsValid())
            {
                Debug.LogError($"'{type}' requires a native class internally. '{type}' could be deriving directly from a class such as Behaviour or Component. Deriving from those classes is not supported for user types.");
                return default;
            }
            return new UnityHybridObjectRtti(type, schema, nativeSchema, null);
        }
        else
        if (rttiGroup == RttiGroup.ImmutablePureManaged)
        {
            return new ImmutablePureManagedObjectRtti(type, schema, null);
        }
        else // RttiGroup.PureManaged
        {
            return new PureManagedObjectRtti(type, schema, null, false);
        }
    }

    internal static void AddCommandsToRtti(Type type, Schema schema, RttiGroup rttiGroup, ref Rtti rtti)
    {
        if (rttiGroup == RttiGroup.PureEntity || rttiGroup == RttiGroup.SimpleNativeType)
        {
            return;
        }

        var transferData = GetRttiDataForSchema(type, schema, out bool isBlittable);
        if(rttiGroup == RttiGroup.UnityObject)
        {
            var hybridRtti = (UnityHybridObjectRtti)rtti;
            hybridRtti.TransferData = transferData;
        }
        else
        if (rttiGroup == RttiGroup.ImmutablePureManaged)
        {
            var immutableRtti = (ImmutablePureManagedObjectRtti)rtti;
            immutableRtti.TransferData = transferData;
        }
        else // RttiGroup.PureManaged
        {
            var pureManagedRtti = (PureManagedObjectRtti)rtti;
            pureManagedRtti.TransferData = transferData;
            pureManagedRtti.IsBlittable = isBlittable;
        }
    }

    private static RttiData[] GetRttiDataForSchema(Type type, Schema schema, out bool isBlittable)
    {
        ulong fieldCount = schema.GetFieldCount();
        var rttiList = new List<RttiData>((int)fieldCount);

        GetRttiDataForSchema(runtimeOffset: 0, modelOffset: 0, copySize: (uint)schema.GetSize(), type, schema, uint.MaxValue, ref rttiList, out isBlittable);

        // Optimise DirectCopy RttiData by copying multiple adjacent fields in a single operation
        OptimiseDirectCopyRttiData(ref rttiList, isBlittable);

        return rttiList.ToArray();
    }

    private static void GetRttiDataForReference(
        ulong runtimeOffset,
        ulong modelOffset,
        uint copySize,
        Type type,
        Schema schema,
        uint fieldIndex,
        ref List<RttiData> rttiList,
        out bool isBlittable)
    {
        isBlittable = false;
        if (type == typeof(EntityId) || type == typeof(Entity))
        {
            rttiList.Add(new RttiData(
                runtimeOffset: runtimeOffset,
                modelOffset: modelOffset,
                copySize,
                rttiDataType: RttiDataType.EntityId,
                null,
                schema,
                fieldIndex)
            );
        }
        else
        {
            rttiList.Add(new RttiData(
                runtimeOffset: runtimeOffset,
                modelOffset: modelOffset,
                copySize,
                rttiDataType: RttiDataType.Reference,
                null,
                schema,
                fieldIndex)
            );
        }
        // This will add close generic types, like for example HashSet<int>
        // This could be removed if we implement more advanced references were we specify the generic hash and the generic parameters hashes.
        RttiResolver.GetOrAddRTTI(type);
    }

    private static void GetRttiDataForSchema(
        ulong runtimeOffset,
        ulong modelOffset,
        uint copySize,
        Type type,
        Schema schema,
        uint fieldIndex,
        ref List<RttiData> rttiList,
        out bool isBlittable)
    {
        if (type == default)
            throw new ArgumentNullException(nameof(type));

        if (schema.IsReference())
        {
            GetRttiDataForReference(runtimeOffset, modelOffset, copySize, type, schema, fieldIndex, ref rttiList, out isBlittable);
        }
        else if (schema.GetFlags().HasFlag(SchemaFlags.IsFundamental) || schema.IsHash() || schema.IsGuid() || IsFixedBuffer(type))
        {
            EngineHelper.AssertIsTrue(type != typeof(bool) || (schema.GetSize() == 1 && EngineHelper.SizeOf(type) == 1));
            // These types are blittable in all cases but two: either the type itself or the element type of a fixed buffer is a char,
            // as char is not blittable (size == 2, schema.size == 1)
            isBlittable = type != typeof(Char) && (!IsFixedBuffer(type) || type.GetFields()[0].FieldType != typeof(Char));

            rttiList.Add(new RttiData(
                runtimeOffset: runtimeOffset,
                modelOffset: modelOffset,
                copySize,
                rttiDataType: RttiDataType.DirectCopy,
                null,
                schema,
                fieldIndex)
            );
        }
        else if (schema.IsUTF8String())
        {
            EngineHelper.AssertIsTrue(type == typeof(string));
            isBlittable = false;
            rttiList.Add(new RttiData(
                runtimeOffset: runtimeOffset,
                modelOffset: modelOffset,
                copySize,
                rttiDataType: RttiDataType.String,
                null,
                schema,
                fieldIndex)
            );
        }
        else if (schema.GetFlags().HasFlag(SchemaFlags.IsVector))
        {
            isBlittable = false;
            var elementSchema = schema.GetVectorElementSchema();
            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                rttiList.Add(new RttiData(
                    runtimeOffset: runtimeOffset,
                    modelOffset: modelOffset,
                    copySize,
                    rttiDataType: RttiDataType.Array,
                    elementType,
                    schema,
                    fieldIndex));
                // This will add close generic types, like for example HashSet<int>
                // This could be removed if we implement more advanced references were we specify the generic hash and the generic parameters hashes.
                RttiResolver.GetOrAddRTTI(elementType, elementSchema);
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var elementType = type.GenericTypeArguments[0];
                rttiList.Add(new RttiData(
                    runtimeOffset: runtimeOffset,
                    modelOffset: modelOffset,
                    copySize,
                    rttiDataType: RttiDataType.List,
                    elementType,
                    schema,
                    fieldIndex));
                // This will add close generic types, like for example HashSet<int>.
                // This could be removed if we implement more advanced references were we specify the generic hash and the generic parameters hashes.
                RttiResolver.GetOrAddRTTI(elementType, elementSchema);
            }
            else
            {
                throw new ArgumentOutOfRangeException("Type is neither an array nor a list.");
            }
        }
        else
        {
            var allFields = SerializeUtilities.GetFields(type);
            FieldInfoAndSchema[] allFieldsOrdered = new FieldInfoAndSchema[allFields.Length];

            // Override and always add all padding
            var markedAsChunkSerializable = false;
            markedAsChunkSerializable = Attribute.IsDefined(type, typeof(ChunkSerializableAttribute));

            var isBlittableSoFar = CheckIntermediateBlittability(schema, type, allFields.Length);
            var isAutoLayout = type.IsAutoLayout || (type.IsLayoutSequential && (!schema.GetFlags().HasFlag(SchemaFlags.IsTriviallyCopyable) || schema.GetFlags().HasFlag(SchemaFlags.HasReferenceFields)));
            if (isAutoLayout && markedAsChunkSerializable)
            {
                // Warning is disabled until Entities (references) are blittable (currently breaks on SceneTag)
                //EngineHelper.LogWarning($"The type {type.Name} is marked as [ChunkSerializable] but has a TypeLayout.Auto which is not blittable. Either make the layout Sequential or Explicit or remove the [ChunkSerializable] attribute");
                markedAsChunkSerializable = false;
            }

            FilterAndSortFields(allFields, allFieldsOrdered, schema, type, ref markedAsChunkSerializable, ref isBlittableSoFar);

            for (int index = 0; index < allFieldsOrdered.Length; index++)
            {
                if (!allFieldsOrdered[index].IsValid())
                    continue;

                var fieldInfo = allFieldsOrdered[index].fieldInfo;
                SchemaField field = allFieldsOrdered[index].schemaField;
                Schema fieldSchema = field.GetSchema();

                var flags = field.GetFlags();
                var isManaged = flags.HasFlag(SchemaFieldFlags.IsManaged);

                if (!isManaged)
                    continue;

                var fieldType = fieldInfo.FieldType;
                var fieldRuntimeOffset = (ulong)SerializeUtilities.OffsetOf(type, fieldInfo);
                var fieldModelOffset = field.GetOffset();

                uint fieldPadding = CalculatePadding(index, allFieldsOrdered, schema.GetSize(), markedAsChunkSerializable, isBlittableSoFar, isAutoLayout, ref fieldRuntimeOffset, ref fieldModelOffset);

                // References are handled different here, because the field doesn't contain a value type
                // of the type fieldType, but a reference to it.
                if (fieldSchema.IsReference())
                {
                    GetRttiDataForReference(runtimeOffset + fieldRuntimeOffset,
                                            modelOffset + fieldModelOffset,
                                            (uint)fieldSchema.GetSize() + fieldPadding,
                                            fieldType,
                                            fieldSchema,
                                            field.GetIndex(),
                                            ref rttiList,
                                            out bool fieldTypeIsBlittable);
                    isBlittableSoFar &= fieldTypeIsBlittable;
                }
                else
                {
                    // We use IManagedRtti instead of PureManagedRtti as we could have IComponentData type fields (e.g. Unity.EntitiesLike.ComponentAuthoringBase)
                    IManagedRtti fieldRTTI = (IManagedRtti)RttiResolver.GetOrAddRTTI(fieldType, fieldSchema);
                    if (fieldRTTI == null)
                    {
                        throw new RttiNotFoundException($"Rtti expected and not found for {type.FullName}.{field.GetName().ToString()} ('{fieldType.FullName}')");
                    }
                    isBlittableSoFar &= fieldRTTI.IsBlittable;
                    // We reuse the RTTI result that we got from a previous pass
                    var fieldTransferData = fieldRTTI.TransferData;
                    if (fieldTransferData != null)
                    {
                        var runtimeFieldBaseOffset = runtimeOffset + fieldRuntimeOffset;
                        var modelFieldBaseOffset = modelOffset + fieldModelOffset;
                        foreach (var fieldTransfer in fieldTransferData)
                        {
                            rttiList.Add(new RttiData(
                                runtimeOffset: fieldTransfer.RuntimeOffset + runtimeFieldBaseOffset,
                                modelOffset: fieldTransfer.ModelOffset + modelFieldBaseOffset,
                                copySize: fieldTransfer.CopySize + fieldPadding,
                                fieldTransfer.RttiDataType,
                                fieldTransfer.ElementType,
                                fieldTransfer.Schema,
                                field.GetIndex()));
                        }
                    }
                }
            }

            isBlittable = isBlittableSoFar;
        }
    }

    private static void FilterAndSortFields(FieldInfo[] allFields, FieldInfoAndSchema[] allFieldsOrdered, Schema schema, Type type, ref bool markedAsChunkSerializable, ref bool isBlittableSoFar)
    {
        for (int index = 0; index < allFields.Length; index++)
        {
            var fieldInfo = allFields[index];

            string fieldName = fieldInfo.Name;
            SchemaField field = schema.GetFieldByName(fieldName);
            if (!field.IsValid()) // private field
            {
                allFieldsOrdered[index] = new FieldInfoAndSchema()
                {
                    RuntimeOffset = (ulong)SerializeUtilities.OffsetOf(type, fieldInfo),
                    Size = (ulong)0
                };
                isBlittableSoFar = markedAsChunkSerializable; // Removes all types with a private or non-serializable field
            }
            else
            {
                allFieldsOrdered[index] = new FieldInfoAndSchema()
                {
                    RuntimeOffset = (ulong)SerializeUtilities.OffsetOf(type, fieldInfo),
                    Size = field.GetSchema().GetSize(),
                    fieldInfo = fieldInfo,
                    schemaField = field
                };
                // If at some point the offset doesn't match, the previous field might be private, or be a struct with non-serializable types
                // This is an early out technique that is not completely fool-proof: we still check below if any fields are non-serializable through recursion
                if ((ulong)SerializeUtilities.OffsetOf(type, fieldInfo) != field.GetOffset())
                    isBlittableSoFar = false;
            }
        }

        Array.Sort(allFieldsOrdered, CompareFieldInfo);

        // If a type is marked [ChunkSerializable], we need to treat pointer fields as padding
        // if the pointer-treated-as-padding is in between 'valid' fields the padding is counted in the field.Padding (calculated when creating the schema)
        // if the pointer-treated-as-padding is at the start or end of the type, we need to do make sure that the
        // pointer-treated-as-padding is counted with the start and end padding (calculated in CalculatePadding()), so we need to know what the first 'valid'
        // field is, even if that fields index isn't 0, and the same for the last 'valid' field
        if (markedAsChunkSerializable)
        {
            int firstValidField = -1;
            int lastValidField = -1;

            for (int index = 0; index < allFieldsOrdered.Length; index++)
            {
                if (!allFieldsOrdered[index].IsValid())
                    continue;

                if (firstValidField == -1)
                    firstValidField = index;
                lastValidField = index;
            }

            if(firstValidField > -1)
            {
                allFieldsOrdered[firstValidField].isFirstValidField = true;
                allFieldsOrdered[lastValidField].isLastValidField = true;
            }
            else
            { // Empty types are not blittable
                isBlittableSoFar = false;
                markedAsChunkSerializable = false;
            }
        }
    }

    private static uint CalculatePadding(int index, FieldInfoAndSchema[] allFieldsOrdered, ulong schemaSize, bool markedAsChunkSerializable, bool isBlittableSoFar, bool isAutoLayout, ref ulong fieldRuntimeOffset, ref ulong fieldModelOffset)
    {
        uint fieldPadding = 0;
        if (isAutoLayout)
            return fieldPadding;

        SchemaField field = allFieldsOrdered[index].schemaField;

        // Add inter-field padding
        // If the next field is invalid, it is a private field, don't add padding to the current field (it is not padding, but the private field)
        // If the next field is not a DirectCopy, we won't be able to merge the commands anyway, so ignore padding
        if (index < allFieldsOrdered.Length - 1 && (markedAsChunkSerializable || FieldIsValidAndDirectCopyCommand(allFieldsOrdered[index + 1])))
        {
            fieldPadding = field.GetPadding();
        }

        // Add start and end padding
        if (markedAsChunkSerializable || isBlittableSoFar)
        {
            // Since we early out on invalid fields, the first 'valid' field might not have index 0. Normally this would mean that the type is not blittable
            // (for example the first field is a private field) and we don't want to add start padding in that case.
            // However, if the type is marked [ChunkSerializable] (forces blittability) we do want to add this extra padding (there is a pointer-treated-as-padding as the first field)
            // In this case we want the first valid field to get all the padding, from the start of the type, including the pointer-treated-as-padding
            // The same goes for the last valid field
            if (index == 0 || (markedAsChunkSerializable && allFieldsOrdered[index].isFirstValidField))
            {
                fieldPadding += (uint)fieldModelOffset;
                fieldRuntimeOffset = 0;
                fieldModelOffset = 0;
            }
            if (index == allFieldsOrdered.Length - 1 || (markedAsChunkSerializable && allFieldsOrdered[index].isLastValidField))
            {
                fieldPadding += (uint)(schemaSize - (field.GetOffset() + allFieldsOrdered[index].Size));
            }
        }

        return fieldPadding;
    }
    private static bool FieldIsValidAndDirectCopyCommand(FieldInfoAndSchema field)
    {
        if (field.IsValid())
        {
            var fieldSchema = field.schemaField.GetSchema();
            return fieldSchema.GetFlags().HasFlag(SchemaFlags.IsTriviallyCopyable) && !fieldSchema.IsReference() && !fieldSchema.GetFlags().HasFlag(SchemaFlags.HasReferenceFields);
        }
        return false;
    }
    private static bool CheckIntermediateBlittability(Schema schema, Type type, int allFieldsLength)
    {
        return schema.GetFlags().HasFlag(SchemaFlags.IsTriviallyCopyable) && !schema.GetFlags().HasFlag(SchemaFlags.HasReferenceFields) // Check that it has no arrays or strings or references
                && type.IsValueType // Checks that it is a struct
                && allFieldsLength > 0 // Empty structs are not blittable (size == 1 byte in C#/C++, schema size when type is used as a field is zero)
                && (int)schema.GetSize() == EngineHelper.SizeOf(type);
    }

    private struct FieldInfoAndSchema
    {
        internal ulong RuntimeOffset;
        internal ulong Size;
        internal FieldInfo fieldInfo;
        internal SchemaField schemaField;
        internal bool isFirstValidField;
        internal bool isLastValidField;

        internal bool IsValid()
        {
            return fieldInfo != null && schemaField.IsValid();
        }
    }

    private static int CompareFieldInfo(FieldInfoAndSchema left, FieldInfoAndSchema right)
    {
        var offsetCompare = left.RuntimeOffset.CompareTo(right.RuntimeOffset);
        if (offsetCompare != 0)
            return offsetCompare;

        return left.Size.CompareTo(right.Size);
    }

    private static void OptimiseDirectCopyRttiData(ref List<RttiData> rttiList, bool isBlittableType)
    {
        if (isBlittableType && rttiList.Count > 1)
        {
            // we can combine all copy commands in one go, as we know they are all able to merge
            ulong runtimeNewEnd = rttiList[0].RuntimeOffset + rttiList[0].CopySize;
            for (int i = 1; i < rttiList.Count; i++)
            {
                // Find the end (note: doesn't have to be the last field because explicit types may have overlapping or out of order fields)
                runtimeNewEnd = Math.Max(runtimeNewEnd, rttiList[i].RuntimeOffset + rttiList[i].CopySize);
            }

            rttiList[0] = new RttiData(
                rttiList[0].RuntimeOffset,
                rttiList[0].ModelOffset,
                copySize : (uint)(runtimeNewEnd - rttiList[0].RuntimeOffset),
                rttiList[0].RttiDataType,
                rttiList[0].ElementType,
                rttiList[0].Schema,
                uint.MaxValue);  // FieldIndex is not meaningful for combined RttiData
            rttiList.RemoveRange(1,rttiList.Count - 1); // We have already combined everything, so remove all other copy commands
            return;
        }

        // There are assumptions elsewhere in the code that the "Array" and "List" RttiData types are never combined
        for (int i = 0; i < rttiList.Count - 1;)
        {
            // Current and next field are both Direct Copy types
            bool directCopyTypes = rttiList[i].RttiDataType == RttiDataType.DirectCopy &&
                                   rttiList[i + 1].RttiDataType == RttiDataType.DirectCopy;
            // End of current and start of next field align
            bool adjacentEntries = rttiList[i].RuntimeOffset + rttiList[i].CopySize == rttiList[i + 1].RuntimeOffset &&
                                   rttiList[i].ModelOffset + rttiList[i].CopySize == rttiList[i + 1].ModelOffset;
            // Start of the next field is before the end of the current field
            // and the model and runtime offsets of this and the next field are the same (this case is for explicit offsets, runtime and model need to match)
            bool overlappingExplicitEntries = rttiList[i].RuntimeOffset + rttiList[i].CopySize > rttiList[i + 1].RuntimeOffset &&
                                              rttiList[i].ModelOffset == rttiList[i].RuntimeOffset && rttiList[i + 1].ModelOffset == rttiList[i + 1].RuntimeOffset;

            if (directCopyTypes && (adjacentEntries || overlappingExplicitEntries))
            {
                // the start of the next field is right at the end or before the end of the current:
                // fields border each other or overlap (overlapping happens for explicit layout, note: fields are sorted on offset)
                var runtimeNewEnd = Math.Max(rttiList[i].RuntimeOffset + rttiList[i].CopySize, rttiList[i + 1].RuntimeOffset + rttiList[i + 1].CopySize);

                rttiList[i] = new RttiData(
                    rttiList[i].RuntimeOffset,
                    rttiList[i].ModelOffset,
                    copySize : (uint)(runtimeNewEnd - rttiList[i].RuntimeOffset),
                    rttiList[i].RttiDataType,
                    rttiList[i].ElementType,
                    rttiList[i].Schema,
                    uint.MaxValue);  // FieldIndex is not meaningful for combined RttiData

                rttiList.RemoveAt(i + 1);
            }
            else
            {
                i++;
            }
        }
    }

    // In C#, fixed buffer types are represented by compiler-generated structs (see below).
    // They are always marked with the [CompilerGenerated] and [UnsafeValueType] attribute
    // and named <fieldName>e__FixedBuffer, which is what we're looking for here.
    //
    // Example source:
    // [Serializable]
    // public unsafe struct MyStruct
    // {
    //     public fixed int myField[20];
    // }

    // Compiler generated:
    // [Serializable]
    // public struct MyStruct
    // {
    //     [FixedBuffer(typeof(int), 20)]
    //     public MyStruct.<myField>e__FixedBuffer myField;
    //
    //     [CompilerGenerated]
    //     [UnsafeValueType]
    //     [StructLayout(LayoutKind.Sequential, Size = 80)]
    //     public struct <myField>e__FixedBuffer
    //     {
    //         public int FixedElementField;
    //     }
    // }
    private static bool IsFixedBuffer(Type type)
    {
        var hasCompilerGeneratedAttribute = false;
        var hasUnsafeValueTypeAttribute = false;

        foreach (var attribute in type.GetCustomAttributesData())
        {
            if (attribute.AttributeType == typeof(CompilerGeneratedAttribute))
                hasCompilerGeneratedAttribute = true;

            if (attribute.AttributeType == typeof(UnsafeValueTypeAttribute))
                hasUnsafeValueTypeAttribute = true;
        }

        if (hasCompilerGeneratedAttribute && hasUnsafeValueTypeAttribute)
        {
            if (type.Name.Contains(">e__FixedBuffer"))
                return true;
        }

        return false;
    }
}
