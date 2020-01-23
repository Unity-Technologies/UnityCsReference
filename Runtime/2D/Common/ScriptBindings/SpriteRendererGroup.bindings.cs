// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Experimental.U2D;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using System.Runtime.CompilerServices;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

[assembly: InternalsVisibleTo("Unity.2D.Hybrid")]
namespace UnityEngine.Experimental.U2D
{
    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/2D/Renderer/SpriteRendererGroup.h")]
    internal struct SpriteIntermediateRendererInfo
    {
        public int SpriteID;
        public int TextureID;
        public int MaterialID;
        public Color Color;
        public Matrix4x4 Transform;
        public Bounds Bounds;
        public int Layer;
        public int SortingLayer;
        public int SortingOrder;
        public ulong SceneCullingMask;
        public IntPtr IndexData;
        public IntPtr VertexData;
        public int IndexCount;
        public int VertexCount;
        public int ShaderChannelMask;
    }

    [NativeHeader("Runtime/2D/Renderer/SpriteRendererGroup.h")]
    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal class SpriteRendererGroup
    {
        public static void AddRenderers(NativeArray<SpriteIntermediateRendererInfo> renderers)
        {
            unsafe
            {
                AddRenderers(renderers.GetUnsafeReadOnlyPtr(), renderers.Length);
            }
        }

        unsafe extern static void AddRenderers(void* renderers, int count);
        public extern static void Clear();
    }
}
