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

        [FreeFunction(Name = "VFXMemorySerializerBindings::Internal_ExtractObjects")]
        extern public static ScriptableObject[] ExtractObjects(string data, bool asACopy);

        [FreeFunction(Name = "VFXMemorySerializerBindings::Internal_DuplicateObjects")]
        extern public static ScriptableObject[] DuplicateObjects(ScriptableObject[] objects);
    }
}
