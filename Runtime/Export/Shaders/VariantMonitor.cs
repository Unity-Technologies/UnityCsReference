// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using UnityEngine.Shaders;

namespace UnityEngine.Shaders
{
    [NativeHeader("Runtime/Shaders/Public/Variants/VariantMonitor.h")]
    public static class VariantsUploadedToGpuLastFrame
    {
        [RequiredByNativeCode(Optional = true)]
        [StructLayout(LayoutKind.Sequential)]
        [NativeClass("VariantUploadData", "namespace ShaderRuntime { namespace Variants { struct VariantUploadData; } }")]
        [NativeHeader("Runtime/Shaders/Public/Variants/VariantMonitor.h")]
        public readonly struct UploadData
        {
            public readonly LocalKeyword[] keywords;
            public readonly Shader shader;
            public readonly PassIdentifier passIdentifier;
            public readonly double uploadTimeInMilliseconds;
            public readonly ShaderStageFlags stages;
        }

        [NativeName("GetVariantCountUploadedToGpuDuringLastFrame")]
        extern private static uint GetCount();
        public static uint count { get { return GetCount(); } }
        [NativeName("GetVariantUploadedToGpuDuringLastFrame")]
        extern private static UploadData GetUploadDataImpl(uint index);

        public static UploadData GetUploadData(uint index)
        {
            if (index >= count)
                throw new ArgumentOutOfRangeException($"Trying to access {index} upload data out of {count}.");
            return GetUploadDataImpl(index);
        }

        public static bool TryGetUploadData(uint index, out UploadData uploadData)
        {
            if (index >= count)
            {
                uploadData = new UploadData();
                return false;
            }

            uploadData = GetUploadDataImpl(index);
            return true;
        }
    }
}
