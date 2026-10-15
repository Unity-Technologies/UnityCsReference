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
        public bool isTopLevel { get; set; }
    };

    public enum GpuBvhBuildQuality : int
    {
        Low = 0, Medium, High
    };

    [NativeHeader("Modules/EmbreeEditor/Embree.bindings.h")]
    public static class GpuBvh
    {
        // Embree::LeafNode holds at most kMaxPrimPerLeaf primitive ids, see Modules/EmbreeEditor/GpuBvhBuild.cpp
        const uint k_MaxLeafSize = 4;

        public static uint[] Build(GpuBvhBuildOptions options, Span<GpuBvhPrimitiveDescriptor> prims)
        {
            if (options.maxLeafSize == 0 || options.maxLeafSize > k_MaxLeafSize)
                throw new ArgumentException($"{nameof(GpuBvhBuildOptions.maxLeafSize)} must be between 1 and {k_MaxLeafSize}, but was {options.maxLeafSize}.", nameof(options));

            return BuildInternal(options, prims);
        }

        [NativeName("Build")]
        static extern uint[] BuildInternal(GpuBvhBuildOptions options, Span<GpuBvhPrimitiveDescriptor> prims);
    }
}
