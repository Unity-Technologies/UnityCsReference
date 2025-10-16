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
        }
    }

    static void ReferenceFromObjModelToLive(ref byte runtimePtr, ref byte modelPtr, DeserializeContext context)
    {
        unsafe
        {
        }
    }

    static void InstanceIDFromObjModelToLive(ref byte runtimePtr, ref byte modelPtr, DeserializeContext context)
    {
        unsafe
        {
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

            return runtimeArray;
        }
    }

    private static void ArrayFromObjModelToLive(ref byte runtimePtr, ref byte modelPtr, in RttiData rttiData, DeserializeContext context)
    {
    }

    internal static unsafe void ConvertBufferFromModelToLive(byte* beginningArrayByte, ConstVector modelVector, int vectorSize, in Schema elementSchema, in Type elementType,
        DeserializeContext context)
    {
    }

    private static void ListFromObjModelToLive(ref byte runtimePtr, ref byte modelPtr, in RttiData rttiData, DeserializeContext context)
    {
    }

    private static void DefaultFromObjModelToLive(ref byte runtimePtr, ref byte modelPtr, in RttiData rttiData, DeserializeContext context)
    {
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
    }

    internal static void ReadFromAccessor(IReadOnlyList<RttiData> commands, ConstAccessor objectModelAccessor, object data, DeserializeContext context)
    {
    }

    internal static void ReadFromAccessor(IReadOnlyList<RttiData> commands, ConstAccessor objectModelAccessor, ref byte runtimeBasePtr, DeserializeContext context)
    {
    }

    static void StringFromLiveToObjModel(ref byte runtimePtr, ref byte modelPtr, SerializeContext context)
    {
    }

    static void ReferenceFromLiveToObjModel(ref byte runtimePtr, ref byte modelPtr, SerializeContext context)
    {
    }

    static void InstanceIDFromLiveToObjModel(ref byte runtimePtr, ref byte modelPtr, SerializeContext context)
    {
    }

    private static void ArrayFromLiveToObjModel(ref byte runtimePtr, ref byte modelPtr, in RttiData rttiData, SerializeContext context)
    {
    }

    internal static unsafe void ConvertBufferFromLiveToObjModel(byte* beginningArrayByte, Vector modelVector, ulong length, in Type elementType, SerializeContext context)
    {
    }

    private static void ListFromLiveToObjModel(ref byte runtimePtr, ref byte modelPtr, in RttiData rttiData, SerializeContext context)
    {
    }

    private static void DefaultFromLiveToObjModel(ref byte runtimePtr, ref byte modelPtr, in RttiData rttiData, SerializeContext context)
    {
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
    }

    internal static void WriteToAccessor(IReadOnlyList<RttiData> commands, Accessor objectModelAccessor, object data, SerializeContext context)
    {
    }
}
