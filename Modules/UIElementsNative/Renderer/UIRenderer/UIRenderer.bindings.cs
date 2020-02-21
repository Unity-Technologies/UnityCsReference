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
using UnityEngine.Rendering;

namespace UnityEngine.UIElements.UIR
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct GfxUpdateBufferRange
    {
        public UInt32 offsetFromWriteStart;
        public UInt32 size;
        public UIntPtr source;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DrawBufferRange
    {
        public int firstIndex;
        public int indexCount;
        public int minIndexVal;
        public int vertsReferenced;
    }

    [NativeHeader("Modules/UIElementsNative/UIRendererUtility.h")]
    [VisibleToOtherModules("Unity.UIElements")]
    internal partial class Utility
    {
        [Flags] internal enum RendererCallbacks { RendererCallback_Init = 1, RendererCallback_Exec = 2, RendererCallback_Cleanup = 4 } // Combinable bit-wise flags
        internal enum GPUBufferType { Vertex, Index }
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

        unsafe public static void SetVectorArray<T>(MaterialPropertyBlock props, int name, NativeSlice<T> vector4s) where T : struct
        {
            int vector4Count = (vector4s.Length * vector4s.Stride) / (sizeof(float) * 4);
            SetVectorArray(props, name, new IntPtr(vector4s.GetUnsafePtr()), vector4Count);
        }

        public static event Action<bool> GraphicsResourcesRecreate;
        public static event Action EngineUpdate;
        public static event Action FlushPendingResources;
        public static event Action<Camera> RegisterIntermediateRenderers;
        public static event Action<IntPtr> RenderNodeAdd, RenderNodeExecute, RenderNodeCleanup;

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

        [RequiredByNativeCode]
        internal static void RaiseRegisterIntermediateRenderers(Camera camera)
        {
            RegisterIntermediateRenderers?.Invoke(camera);
        }

        [RequiredByNativeCode]
        internal static void RaiseRenderNodeAdd(IntPtr userData)
        {
            RenderNodeAdd?.Invoke(userData);
        }

        [RequiredByNativeCode]
        internal static void RaiseRenderNodeExecute(IntPtr userData)
        {
            RenderNodeExecute?.Invoke(userData);
        }

        [RequiredByNativeCode]
        internal static void RaiseRenderNodeCleanup(IntPtr userData)
        {
            RenderNodeCleanup?.Invoke(userData);
        }

        [ThreadSafe] extern static IntPtr AllocateBuffer(int elementCount, int elementStride, bool vertexBuffer);
        [ThreadSafe] extern static void FreeBuffer(IntPtr buffer);
        [ThreadSafe] extern static void UpdateBufferRanges(IntPtr buffer, IntPtr ranges, int rangeCount, int writeRangeStart, int writeRangeEnd);
        [ThreadSafe] extern static void SetVectorArray(MaterialPropertyBlock props, int name, IntPtr vector4s, int count);
        [ThreadSafe] public extern static IntPtr GetVertexDeclaration(VertexAttributeDescriptor[] vertexAttributes);

        public extern static void RegisterIntermediateRenderer(
            Camera camera, Material material, Matrix4x4 transform, Bounds aabb,
            int renderLayer, int shadowCasting, bool receiveShadows, int sameDistanceSortPriority,
            UInt64 sceneCullingMask, int rendererCallbackFlags, IntPtr userData, int userDataSize);
        [ThreadSafe] public extern unsafe static void DrawRanges(IntPtr ib, IntPtr* vertexStreams, int streamCount, IntPtr ranges, int rangeCount, IntPtr vertexDecl);
        [ThreadSafe] public extern static void SetPropertyBlock(MaterialPropertyBlock props);
        [ThreadSafe] public extern static void SetScissorRect(RectInt scissorRect);
        [ThreadSafe] public extern static void DisableScissor();
        [ThreadSafe] public extern static bool IsScissorEnabled();
        [ThreadSafe] public extern static UInt32 InsertCPUFence();
        [ThreadSafe] public extern static bool CPUFencePassed(UInt32 fence);
        [ThreadSafe] public extern static void WaitForCPUFencePassed(UInt32 fence);
        [ThreadSafe] public extern static void SyncRenderThread();
        [ThreadSafe] public extern static RectInt GetActiveViewport();
        [ThreadSafe] public extern static void ProfileDrawChainBegin();
        [ThreadSafe] public extern static void ProfileDrawChainEnd();
        public extern static void ProfileImmediateRendererBegin();
        public extern static void ProfileImmediateRendererEnd();
        public extern static void NotifyOfUIREvents(bool subscribe);
        [ThreadSafe] public extern static Matrix4x4 GetUnityProjectionMatrix();
        [ThreadSafe] public extern static Matrix4x4 GetDeviceProjectionMatrix();
        [ThreadSafe] public extern static bool DebugIsMainThread(); // For debug code only
    }
}
