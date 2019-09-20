// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;
using Unity.Profiling;
using UnityEngine.Scripting;

namespace UnityEngine.UIElements.UIR
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct GfxUpdateBufferRange
    {
        public UInt32 offsetFromWriteStart;
        public UInt32 size;
        public UIntPtr source;
    };

    [StructLayout(LayoutKind.Sequential)]
    internal struct DrawBufferRange
    {
        public int firstIndex;
        public int indexCount;
        public int minIndexVal;
        public int vertsReferenced;
    };

    [NativeHeader("Modules/UIElements/UIRendererUtility.h")]
    internal partial class Utility
    {
        internal enum GPUBufferType { Vertex, Index };
        unsafe public class GPUBuffer<T> : IDisposable where T : struct
        {
            IntPtr buffer;
            int elemCount;
            int elemStride;

            unsafe public GPUBuffer(int elementCount, GPUBufferType type)
            {
                elemCount = elementCount;
                elemStride = UnsafeUtility.SizeOf<T>();
                buffer = AllocateBuffer(elementCount, elemStride, type == GPUBufferType.Vertex);
            }

            public void Dispose()
            {
                FreeBuffer(buffer);
            }

            public void UpdateRanges(NativeSlice<GfxUpdateBufferRange> ranges, int rangesMin, int rangesMax)
            {
                UpdateBufferRanges(buffer, new IntPtr(ranges.GetUnsafePtr()), ranges.Length, rangesMin, rangesMax);
            }

            public int ElementStride { get { return elemStride; } }
            public int Count { get { return elemCount; } }
            internal IntPtr BufferPointer { get { return buffer; } }
        }

        unsafe public static void DrawRanges<I, T>(GPUBuffer<I> ib, GPUBuffer<T> vb, NativeSlice<DrawBufferRange> ranges) where T : struct where I : struct
        {
            Debug.Assert(ib.ElementStride == 2);
            DrawRanges(ib.BufferPointer, vb.BufferPointer, vb.ElementStride, new IntPtr(ranges.GetUnsafePtr()), ranges.Length);
        }

        unsafe public static void SetVectorArray<T>(Material mat, int name, NativeSlice<T> vector4s) where T : struct
        {
            int vector4Count = (vector4s.Length * vector4s.Stride) / (sizeof(float) * 4);
            SetVectorArray(mat, name, new IntPtr(vector4s.GetUnsafePtr()), vector4Count);
        }

        public static event Action<bool> GraphicsResourcesRecreate;
        public static event Action EngineUpdate;
        public static event Action FlushPendingResources;

        [RequiredByNativeCode]
        internal static void RaiseGraphicsResourcesRecreate(bool recreate)
        {
            GraphicsResourcesRecreate?.Invoke(recreate);
        }

        static ProfilerMarker s_MarkerRaiseEngineUpdate = new ProfilerMarker("UIR.RaiseEngineUpdate");

        [RequiredByNativeCode]
        internal static void RaiseEngineUpdate()
        {
            if (EngineUpdate != null)
            {
                s_MarkerRaiseEngineUpdate.Begin();
                EngineUpdate.Invoke();
                s_MarkerRaiseEngineUpdate.End();
            }
        }

        [RequiredByNativeCode]
        internal static void RaiseFlushPendingResources()
        {
            FlushPendingResources?.Invoke();
        }

        extern static IntPtr AllocateBuffer(int elementCount, int elementStride, bool vertexBuffer);
        extern static void FreeBuffer(IntPtr buffer);
        extern static void UpdateBufferRanges(IntPtr buffer, IntPtr ranges, int rangeCount, int writeRangeStart, int writeRangeEnd);
        extern static void DrawRanges(IntPtr ib, IntPtr vb, int vbElemStride, IntPtr ranges, int rangeCount);
        extern static void SetVectorArray(Material mat, int name, IntPtr vector4s, int count);

        public extern static void SetScissorRect(RectInt scissorRect);
        public extern static void DisableScissor();
        public extern static bool IsScissorEnabled();
        public extern static UInt32 InsertCPUFence();
        public extern static bool CPUFencePassed(UInt32 fence);
        public extern static void WaitForCPUFencePassed(UInt32 fence);
        public extern static void SyncRenderThread();
        public extern static RectInt GetActiveViewport();
        public extern static void ProfileDrawChainBegin();
        public extern static void ProfileDrawChainEnd();
        public extern static void ProfileImmediateRendererBegin();
        public extern static void ProfileImmediateRendererEnd();
        public extern static void NotifyOfUIREvents(bool subscribe);
        public extern static bool GetInvertProjectionMatrix();
    }
}
