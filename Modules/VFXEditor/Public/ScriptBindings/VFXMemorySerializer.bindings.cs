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

namespace UnityEditor.Experimental.VFX
{
    [UsedByNativeCode]
    [NativeHeader("Modules/VFXEditor/Public/VFXMemorySerializer.h")]
    [StaticAccessor("VFXMemorySerializerBindings", StaticAccessorType.DoubleColon)]
    internal static class VFXMemorySerializer
    {
        extern public static string StoreObjects(ScriptableObject[] objects);

        extern public static byte[] StoreObjectsToByteArray(ScriptableObject[] objects, CompressionLevel compressionLevel = CompressionLevel.None);

        [FreeFunction(Name = "VFXMemorySerializerBindings::Internal_ExtractObjects_FromString")]
        extern private static ScriptableObject[] ExtractObjects_FromString(string data, bool asACopy);

        [FreeFunction(Name = "VFXMemorySerializerBindings::Internal_ExtractObjects_FromByteArray")]
        extern private static ScriptableObject[] ExtractObjects_FromByteArray(byte[] data, bool asACopy);

        public static ScriptableObject[] ExtractObjects(string data, bool asACopy)
        {
            return ExtractObjects_FromString(data, asACopy);
        }

        public static ScriptableObject[] ExtractObjects(byte[] data, bool asACopy)
        {
            return ExtractObjects_FromByteArray(data, asACopy);
        }

        [FreeFunction(Name = "VFXMemorySerializerBindings::Internal_DuplicateObjects")]
        extern public static ScriptableObject[] DuplicateObjects(ScriptableObject[] objects);
    }
}
