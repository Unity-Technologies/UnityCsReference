// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Embree
{
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    public struct GpuBvhPrimitiveDescriptor
    {
        public Vector3 lowerBound { get; set; }
        public Vector3 upperBound { get; set; }
        public uint primID { get; set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    public struct GpuBvhBuildOptions
    {
        public GpuBvhBuildQuality quality { get; set; }
        public uint minLeafSize { get; set; }
        public uint maxLeafSize { get; set; }
        public bool allowPrimitiveSplits { get; set; }
    };

    public enum GpuBvhBuildQuality : int
    {
        Low = 0, Medium, High
    };

    [NativeHeader("Modules/EmbreeEditor/Embree.bindings.h")]
    public static class GpuBvh
    {
        [return: Unmarshalled]
        extern public static uint[] Build(GpuBvhBuildOptions options, Span<GpuBvhPrimitiveDescriptor> prims);

    }
}
