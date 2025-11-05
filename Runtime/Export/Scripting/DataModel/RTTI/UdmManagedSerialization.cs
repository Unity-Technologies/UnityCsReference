// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.DataModel;

internal static class UdmManagedSerialization
{
    static void StringFromObjModelToLive(ref byte runtimePtr, ref byte modelPtr, DeserializeContext context)
    {
        unsafe
        {
            var modelStr = new ConstUTF8String {Field = (IntPtr)UnsafeHelper.AsPointer(ref modelPtr)};
            ref string runtimeStr = ref UnsafeHelper.As<byte, string>(ref runtimePtr);
            runtimeStr = modelStr.ToString();
        }
    }

    static void ReferenceFromObjModelToLive(ref byte runtimePtr, ref byte modelPtr, DeserializeContext context)
    {
        {
            ref Reference modelReference = ref UnsafeHelper.As<byte, Reference>(ref modelPtr);
            ref object obj = ref UnsafeHelper.As<byte, object>(ref runtimePtr);
            obj = context.ResolveInstance(modelReference);
        }
    }

    static void InstanceIDFromObjModelToLive(ref byte runtimePtr, ref byte modelPtr, DeserializeContext context)
    {
        unsafe
        {
            ref Reference modelReference = ref UnsafeHelper.As<byte, Reference>(ref modelPtr);
            ref EntityId instanceID = ref UnsafeHelper.As<byte, EntityId>(ref runtimePtr);
            instanceID = context.ResolveInstanceID(modelReference);
        }
    }

    internal static Array ArrayFromObjModelToLive(ConstAccessor accessor, in RttiData rttiData, DeserializeContext context)
    {
        unsafe
        {
            var modelVector = accessor.GetVectorValue();
            var modelVectorSize = modelVector.GetLength();
            int vectorSize = (int)modelVectorSize;

            Array runtimeArray = Array.CreateInstance(rttiData.ElementType, vectorSize);

            // TODO: REPLACE BY THIS IF MemoryMarshal.GetArrayDataReference becomes available across all scripting backends
            //ref byte beginningArrayByte = ref MemoryMarshal.GetArrayDataReference(runtimeValueArray);
            ref byte[] byteArray = ref UnsafeHelper.As<Array, byte[]>(ref runtimeArray);
            fixed (byte* beginningArrayByte = byteArray)
            {
                ConvertBufferFromModelToLive(beginningArrayByte, modelVector, vectorSize, rttiData.Schema.GetVectorElementSchema(), rttiData.ElementType, context);
            }

            return runtimeArray;
        }
    }

    private static void ArrayFromObjModelToLive(ref byte runtimePtr, ref byte modelPtr, in RttiData rttiData, DeserializeContext context)
    {
        unsafe
        {
            var modelVector = new ConstVector
            {
                ElementSchema = rttiData.Schema.GetVectorElementSchema(),
                Field = (IntPtr)UnsafeHelper.AsPointer(ref modelPtr)
            };
            var modelVectorSize = modelVector.GetLength();
            int vectorSize = (int)modelVectorSize;

            ref Array runtimeArray = ref UnsafeHelper.As<byte, Array>(ref runtimePtr);
            runtimeArray = Array.CreateInstance(rttiData.ElementType, vectorSize);

            // TODO: REPLACE BY THIS IF MemoryMarshal.GetArrayDataReference becomes available across all scripting backends
            //ref byte beginningArrayByte = ref MemoryMarshal.GetArrayDataReference(runtimeArray);
            ref byte[] byteArray = ref UnsafeHelper.As<Array, byte[]>(ref runtimeArray);
            fixed (byte* beginningArrayByte = byteArray)
            {
                ConvertBufferFromModelToLive(beginningArrayByte, modelVector, vectorSize, rttiData.Schema.GetVectorElementSchema(), rttiData.ElementType, context);
            }
        }
    }

    internal static unsafe void ConvertBufferFromModelToLive(byte* beginningArrayByte, ConstVector modelVector, int vectorSize, in Schema elementSchema, in Type elementType,
        DeserializeContext context)
    {
        if (vectorSize == 0)
        {
            return;
        }
        ref byte runtimeElement0ByteRef = ref UnsafeHelper.AsRef<byte>(beginningArrayByte);

        int runtimeElementSize = elementType.IsValueType
            ? EngineHelper.SizeOf(elementType)
            : IntPtr.Size;

        var isElementInstanceId = elementType == typeof(EntityId);

        if (elementSchema.IsReference() && !isElementInstanceId)
        {
            for (ulong index = 0; index < (ulong)vectorSize; ++index)
            {
                ref var runtimeElementPtr =
                    ref UnsafeHelper.Add(ref runtimeElement0ByteRef, runtimeElementSize * (int)index);
                var modelElementAccesor = modelVector.ElementAt(index);
                ref var modelElementPtr = ref UnsafeHelper.AsRef<byte>(modelElementAccesor.Data.ToPointer());

                ReferenceFromObjModelToLive(ref runtimeElementPtr, ref modelElementPtr, context);
            }
        }
        else
        {
            var elementRtti = RttiResolver.GetRTTI(elementType);

            if (elementRtti.Schema.GetUnderlyingTypeId().IsValid())
                EngineHelper.AssertIsTrue(elementRtti.Schema.GetUnderlyingTypeId() == elementSchema.GetUnderlyingTypeId());
            else
                EngineHelper.AssertIsTrue(elementRtti.Schema.SchemaPtr == elementSchema.SchemaPtr);

            var managedElementRtti = (IManagedRtti)elementRtti;
            var commands = managedElementRtti.TransferData;
            if (managedElementRtti.IsBlittable)
            {
                var elementSize = commands[0].CopySize;
                var arrByteSize = elementSize * (uint)vectorSize;

                var udmVectorData = modelVector.GetDataPtr();
                ref var udmVectorDataRef = ref UnsafeHelper.AsRef<byte>(udmVectorData);
                UnsafeHelper.CopyBlock(ref runtimeElement0ByteRef, ref udmVectorDataRef, arrByteSize);
            }
            else
            {
                for (ulong index = 0; index < (ulong)vectorSize; ++index)
                {
                    ref var runtimeElementPtr =
                        ref UnsafeHelper.Add(ref runtimeElement0ByteRef, runtimeElementSize * (int)index);
                    var modelElementAccesor = modelVector.ElementAt(index);
                    ref var modelElementPtr = ref UnsafeHelper.AsRef<byte>(modelElementAccesor.Data.ToPointer());

                    FromObjModelToLive(ref runtimeElementPtr, ref modelElementPtr, commands, context);
                }
            }
        }
    }

    private static void ListFromObjModelToLive(ref byte runtimePtr, ref byte modelPtr, in RttiData rttiData, DeserializeContext context)
    {
        unsafe
        {
            var modelVector = new ConstVector
            {
                ElementSchema = rttiData.Schema.GetVectorElementSchema(),
                Field = (IntPtr)UnsafeHelper.AsPointer(ref modelPtr)
            };
            var modelVectorSize = modelVector.GetLength();
            var vectorSize = (int)modelVectorSize;

            var genericListType = typeof(List<>);
            var concreteListType = genericListType.MakeGenericType(rttiData.ElementType);

            ref IList runtimeList = ref UnsafeHelper.As<byte, IList>(ref runtimePtr);
            List<byte> runtimeByteList;
            if (runtimeList == null || runtimeList.GetType() != concreteListType)
            {
                runtimeList = (IList)Activator.CreateInstance(concreteListType, vectorSize);

                runtimeByteList = UnsafeHelper.As<List<byte>>(runtimeList);
                EngineHelper.ResetListSize(runtimeByteList, vectorSize);
            }
            else
            {
                EngineHelper.ResizeListContents(runtimeList, rttiData.ElementType, vectorSize);
                runtimeByteList = UnsafeHelper.As<List<byte>>(runtimeList);
            }

            // TODO: REPLACE BY THIS IF MemoryMarshal.GetArrayDataReference becomes available across all scripting backends
            byte[] runtimeByteArrayRef = EngineHelper.ExtractArrayFromList(runtimeByteList);
            ref byte[] byteArray = ref runtimeByteArrayRef;
            fixed (byte* beginningArrayByte = byteArray)
            {
                ConvertBufferFromModelToLive(beginningArrayByte, modelVector, vectorSize, rttiData.Schema.GetVectorElementSchema(), rttiData.ElementType, context);
            }
        }
    }

    private static void DefaultFromObjModelToLive(ref byte runtimePtr, ref byte modelPtr, in RttiData rttiData, DeserializeContext context)
    {
        UnsafeHelper.CopyBlock(ref runtimePtr, ref modelPtr, rttiData.CopySize);
    }

    internal static void FromObjModelToLive(ref byte runtimePtr, ref byte modelPtr, in RttiData rttiData, DeserializeContext context)
    {
        switch (rttiData.RttiDataType)
        {
            case RttiDataType.Array:
                {
                    ArrayFromObjModelToLive(ref runtimePtr, ref modelPtr, in rttiData, context);
                    break;
                }
            case RttiDataType.List:
                {
                    ListFromObjModelToLive(ref runtimePtr, ref modelPtr, in rttiData, context);
                    break;
                }
            case RttiDataType.DynamicBuffer:
                {
                    throw new InvalidOperationException(
                        "DynamicBuffer should only be deserialized through EcsComponentSerializer.cs");
                }
            case RttiDataType.String:
                {
                    StringFromObjModelToLive(ref runtimePtr, ref modelPtr, context);
                    break;
                }
            case RttiDataType.Reference:
                {
                    ReferenceFromObjModelToLive(ref runtimePtr, ref modelPtr, context);
                    break;
                }
            case RttiDataType.EntityId:
                {
                    InstanceIDFromObjModelToLive(ref runtimePtr, ref modelPtr, context);
                    break;
                }
            case RttiDataType.DirectCopy:
                {
                    DefaultFromObjModelToLive(ref runtimePtr, ref modelPtr, in rttiData, context);
                    break;
                }
        }
    }

    internal static void FromObjModelToLive(ref byte runtimeBasePtr, ref byte objectModelBasePtr, IReadOnlyList<RttiData> commands, DeserializeContext context)
    {
        for (int commandIndex = 0; commandIndex < commands.Count; ++commandIndex)
        {
            var rttiData = commands[commandIndex];
            context.NullObjectData.FieldIndex = rttiData.FieldIndex;

            ref var runtimePtr = ref UnsafeHelper.Add(ref runtimeBasePtr, (int)rttiData.RuntimeOffset);
            ref var modelPtr = ref UnsafeHelper.Add(ref objectModelBasePtr, (int)rttiData.ModelOffset);
            FromObjModelToLive(ref runtimePtr, ref modelPtr, in rttiData, context);
        }
    }

    internal static void ReadFromAccessor(IReadOnlyList<RttiData> commands, ConstAccessor objectModelAccessor, object data, DeserializeContext context)
    {
        unsafe
        {
            ref byte runtimeBasePtr = ref SerializeUtilities.GetBasePointerForUdm(ref data);
            ref byte modelBasePtr = ref UnsafeHelper.AsRef<byte>(objectModelAccessor.Data.ToPointer());

            context.NullObjectData = new NullObjectData(objectModelAccessor.GetSchema());
            FromObjModelToLive(ref runtimeBasePtr, ref modelBasePtr, commands, context);
        }
    }

    internal static void ReadFromAccessor(IReadOnlyList<RttiData> commands, ConstAccessor objectModelAccessor, ref byte runtimeBasePtr, DeserializeContext context)
    {
        unsafe
        {
            ref byte modelBasePtr = ref UnsafeHelper.AsRef<byte>(objectModelAccessor.Data.ToPointer());

            context.NullObjectData = new NullObjectData(objectModelAccessor.GetSchema());
            FromObjModelToLive(ref runtimeBasePtr, ref modelBasePtr, commands, context);
        }
    }

    static void StringFromLiveToObjModel(ref byte runtimePtr, ref byte modelPtr, SerializeContext context)
    {
        unsafe
        {
            var modelStr = new UTF8String
            {
                Field = (IntPtr)UnsafeHelper.AsPointer(ref modelPtr),
                DocumentModel = context.Document
            };
            ref string runtimeString = ref UnsafeHelper.As<byte, string>(ref runtimePtr);
            if (string.IsNullOrEmpty(runtimeString))
            {
                modelStr.Clear();
                return;
            }

            modelStr.Set(runtimeString);
        }
    }

    static void ReferenceFromLiveToObjModel(ref byte runtimePtr, ref byte modelPtr, SerializeContext context)
    {
        unsafe
        {
            ref object runtimeObject = ref UnsafeHelper.As<byte, object>(ref runtimePtr);
            ref Reference modelReference = ref UnsafeHelper.As<byte, Reference>(ref modelPtr);
            modelReference = context.ResolveReference(runtimeObject);
        }
    }

    static void InstanceIDFromLiveToObjModel(ref byte runtimePtr, ref byte modelPtr, SerializeContext context)
    {
        unsafe
        {
            ref EntityId instanceID = ref UnsafeHelper.As<byte, EntityId>(ref runtimePtr);
            ref Reference modelReference = ref UnsafeHelper.As<byte, Reference>(ref modelPtr);
            modelReference = new Reference { UdmObjectId = instanceID.GetRawData()};
        }
    }

    private static void ArrayFromLiveToObjModel(ref byte runtimePtr, ref byte modelPtr, in RttiData rttiData, SerializeContext context)
    {
        unsafe
        {
            ref Array runtimeValueArray = ref UnsafeHelper.As<byte, Array>(ref runtimePtr);

            var modelVector = new Vector
            {
                ElementSchema = rttiData.Schema.GetVectorElementSchema(),
                Field = (IntPtr)UnsafeHelper.AsPointer(ref modelPtr),
                DocumentModel = context.Document
            };

            if (runtimeValueArray == null || runtimeValueArray.Length == 0)
            {
                modelVector.Clear();
                return;
            }

            if (runtimeValueArray.Rank != 1)
                throw new ArgumentException("Only one dimensional arrays are supported in serialization");

            ulong length = (ulong)runtimeValueArray.GetLongLength(0);

            // TODO: REPLACE BY THIS IF MemoryMarshal.GetArrayDataReference becomes available across all scripting backends
            //ref byte beginningArrayByte = ref MemoryMarshal.GetArrayDataReference(runtimeValueArray);
            ref byte[] byteArray = ref UnsafeHelper.As<Array, byte[]>(ref runtimeValueArray);
            fixed (byte* beginningArrayByte = byteArray)
            {
                ConvertBufferFromLiveToObjModel(beginningArrayByte, modelVector, length, rttiData.ElementType, context);
            }
        }
    }

    internal static unsafe void ConvertBufferFromLiveToObjModel(byte* beginningArrayByte, Vector modelVector, ulong length, in Type elementType, SerializeContext context)
    {
        var elementSchema = modelVector.GetElementSchema();

        ref byte runtimeElement0ByteRef = ref UnsafeHelper.AsRef<byte>(beginningArrayByte);

        int elementSize = elementType.IsValueType ? EngineHelper.SizeOf(elementType) : IntPtr.Size;

        var isElementInstanceId = elementType == typeof(EntityId);

        if (elementSchema.IsReference() && !isElementInstanceId)
        {
            modelVector.Clear();
            modelVector.Reserve(length);

            for (ulong index = 0; index < length; ++index)
            {
                ref var runtimeElementPtr = ref UnsafeHelper.Add(ref runtimeElement0ByteRef, elementSize * (int)index);
                var modelElementAccesor = modelVector.Add();
                ref var modelElementPtr = ref UnsafeHelper.AsRef<byte>(modelElementAccesor.Data.ToPointer());

                ReferenceFromLiveToObjModel(ref runtimeElementPtr, ref modelElementPtr, context);
            }
        }
        else
        {
            var elementRtti = RttiResolver.GetRTTI(elementType);

            if (elementRtti.Schema.GetUnderlyingTypeId().IsValid())
                EngineHelper.AssertIsTrue(elementRtti.Schema.GetUnderlyingTypeId() == elementSchema.GetUnderlyingTypeId());
            else
                EngineHelper.AssertIsTrue(elementRtti.Schema.SchemaPtr == elementSchema.SchemaPtr);

            var managedElementRtti = (IManagedRtti)elementRtti;
            if (managedElementRtti.IsBlittable)
            {
                modelVector.Assign(beginningArrayByte, length);
            }
            else
            {
                modelVector.Clear();
                modelVector.Reserve(length);

                var commands = managedElementRtti.TransferData;
                for (ulong index = 0; index < length; ++index)
                {
                    ref var runtimeElementPtr = ref UnsafeHelper.Add(ref runtimeElement0ByteRef, elementSize * (int)index);
                    var modelElementAccesor = modelVector.Add();
                    ref var modelElementPtr = ref UnsafeHelper.AsRef<byte>(modelElementAccesor.Data.ToPointer());

                    FromLiveToObjModel(ref runtimeElementPtr, ref modelElementPtr, commands, context);
                }
            }
        }
    }

    private static void ListFromLiveToObjModel(ref byte runtimePtr, ref byte modelPtr, in RttiData rttiData, SerializeContext context)
    {
        unsafe
        {
            ref IList runtimeValueList = ref UnsafeHelper.As<byte, IList>(ref runtimePtr);

            var modelVector = new Vector
            {
                ElementSchema = rttiData.Schema.GetVectorElementSchema(),
                Field = (IntPtr)UnsafeHelper.AsPointer(ref modelPtr),
                DocumentModel = context.Document
            };

            if (runtimeValueList == null || runtimeValueList.Count == 0)
            {
                modelVector.Clear();
                return;
            }

            ref List<byte> runtimeValueListRef = ref UnsafeHelper.As<IList, List<byte>>(ref runtimeValueList);
            Span<byte> span = EngineHelper.CreateSpan(runtimeValueListRef);

            ulong length = (ulong)runtimeValueList.Count;

            // TODO: Check during RTTI building that ODM and Runtime element size are the same!!!
            fixed (byte* beginningArrayByte = span)
            {
                ConvertBufferFromLiveToObjModel(beginningArrayByte, modelVector, length, rttiData.ElementType, context);
            }
        }
    }

    private static void DefaultFromLiveToObjModel(ref byte runtimePtr, ref byte modelPtr, in RttiData rttiData, SerializeContext context)
    {
        UnsafeHelper.CopyBlock(ref modelPtr, ref runtimePtr, rttiData.CopySize);
    }

    internal static void FromLiveToObjModel(ref byte runtimePtr, ref byte modelPtr, in RttiData rttiData, SerializeContext context)
    {
        switch (rttiData.RttiDataType)
        {
            case RttiDataType.Array:
            {
                ArrayFromLiveToObjModel(ref runtimePtr, ref modelPtr, in rttiData, context);
                break;
            }
            case RttiDataType.List:
            {
                ListFromLiveToObjModel(ref runtimePtr, ref modelPtr, in rttiData, context);
                break;
            }
            case RttiDataType.DynamicBuffer:
            {
                throw new InvalidOperationException(
                    "DynamicBuffer should only be serialized through EcsComponentSerializer.cs");
            }
            case RttiDataType.String:
            {
                StringFromLiveToObjModel(ref runtimePtr, ref modelPtr, context);
                break;
            }
            case RttiDataType.Reference:
            {
                ReferenceFromLiveToObjModel(ref runtimePtr, ref modelPtr, context);
                break;
            }
            case RttiDataType.EntityId:
            {
                InstanceIDFromLiveToObjModel(ref runtimePtr, ref modelPtr, context);
                break;
            }
            case RttiDataType.DirectCopy:
            {
                DefaultFromLiveToObjModel(ref runtimePtr, ref modelPtr, in rttiData, context);
                break;
            }
        }
    }
    internal static void FromLiveToObjModel(ref byte runtimeBasePtr, ref byte objectModelBasePtr, IReadOnlyList<RttiData> commands, SerializeContext context)
    {
        for (int commandIndex = 0; commandIndex < commands.Count; ++commandIndex)
        {
            var rttiData = commands[commandIndex];
            ref var runtimePtr = ref UnsafeHelper.Add(ref runtimeBasePtr, (int)rttiData.RuntimeOffset);
            ref var modelPtr = ref UnsafeHelper.Add(ref objectModelBasePtr, (int)rttiData.ModelOffset);
            FromLiveToObjModel(ref runtimePtr, ref modelPtr, in rttiData, context);
        }
    }

    internal static void WriteToAccessor(IReadOnlyList<RttiData> commands, Accessor objectModelAccessor, object data, SerializeContext context)
    {
        unsafe
        {
            ref byte runtimeBasePtr = ref SerializeUtilities.GetBasePointerForUdm(ref data);
            ref byte objectModelBasePtr = ref UnsafeHelper.AsRef<byte>(objectModelAccessor.Data.ToPointer());
            FromLiveToObjModel(ref runtimeBasePtr, ref objectModelBasePtr, commands, context);
        }
    }
}
