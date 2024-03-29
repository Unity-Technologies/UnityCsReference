// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace UnityEditor.VFX
{
    [UsedByNativeCode]
    [NativeHeader("Modules/VFXEditor/Public/VFXMemorySerializer.h")]
    [StaticAccessor("VFXMemorySerializerBindings", StaticAccessorType.DoubleColon)]
    internal static class VFXMemorySerializer
    {

        [FreeFunction(Name = "VFXMemorySerializerBindings::StoreObjects", ThrowsException = true)]
        extern public static string StoreObjects(ScriptableObject[] objects);

        [FreeFunction(Name = "VFXMemorySerializerBindings::StoreObjectsToByteArray", ThrowsException = true)]
        extern public static byte[] StoreObjectsToByteArray(ScriptableObject[] objects, CompressionLevel compressionLevel = CompressionLevel.None);

        [FreeFunction(Name = "VFXMemorySerializerBindings::Internal_ExtractObjects_FromString", ThrowsException = true)]
        extern private static ScriptableObject[] ExtractObjects_FromString(string data, bool asACopy);

        [FreeFunction(Name = "VFXMemorySerializerBindings::Internal_ExtractObjects_FromByteArray", ThrowsException = true)]
        extern private static ScriptableObject[] ExtractObjects_FromByteArray(byte[] data, bool asACopy);

        [FreeFunction(Name = "VFXMemorySerializerBindings::Internal_DuplicateObjects", ThrowsException = true)]
        extern public static ScriptableObject[] DuplicateObjects(ScriptableObject[] objects);

        public static ScriptableObject[] ExtractObjects(string data, bool asACopy)
        {
            return ExtractObjects_FromString(data, asACopy);
        }

        public static ScriptableObject[] ExtractObjects(byte[] data, bool asACopy)
        {
            return ExtractObjects_FromByteArray(data, asACopy);
        }
    }
}
