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
using Unity.Jobs;

namespace UnityEngine.UIElements.UIR
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct GfxUpdateBufferRange
    {
        public UInt32 offsetFromWriteStart;
        public UInt32 size;
        public UIntPtr source;
    }

    // Keep in sync with GfxCopyBufferRange in GfxDeviceTypes.h
    [StructLayout(LayoutKind.Sequential)]
    struct GfxCopyBufferRange
    {
        public UInt32 srcOffset; // Offset (in bytes) from start of source buffer to begin copying
        public UInt32 dstOffset; // Offset (in bytes) from start of destination buffer where data is written
        public UInt32 size;      // Size (in bytes) of the region to copy
    };

    // Keep in sync with GfxCopyBufferRangesFlags in GfxDeviceTypes.h
    [Flags]
    enum GfxCopyBufferRangesFlags : uint
    {
        None = 0,
        AcquiredPointer = (1 << 0), // Safe to use ranges data by pointer, no need to copy
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DrawBufferRange
    {
        public int firstIndex;
        public int indexCount;
        public int minIndexVal;
        public int vertsReferenced;
    }

    [Flags]
    enum GpuBufferFlags
    {
        BufferFlags_Target_Vertex = 1 << 0,
        BufferFlags_Target_Index = 1 << 1,
        BufferFlags_Target_CopySrc = 1 << 2,
        BufferFlags_Target_CopyDst = 1 << 3,

        BufferFlags_Mode_Immutable = 1 << 4,
        BufferFlags_Mode_Dynamic = 1 << 5,
        BufferFlags_Mode_SubUpdates = 1 << 6,
    };

    [NativeHeader("Modules/UIElements/Core/Native/Renderer/UIRendererUtility.h")]
    [VisibleToOtherModules("Unity.UIElements")]
    unsafe partial class Utility
    {
        internal enum GPUBufferType { Vertex, Index }
        unsafe public class GPUBuffer : IDisposable
        {
            IntPtr buffer;
            int elementCount;
            int elementStride;

            unsafe public GPUBuffer(int elementCount, int elementStride, GpuBufferFlags bufferFlags)
            {
                this.elementCount = elementCount;
                this.elementStride = elementStride;
                buffer = AllocateBuffer(elementCount, elementStride, (int)bufferFlags);
            }

            public void Dispose()
            {
                FreeBuffer(buffer);
            }

            // writeStart: Offset in bytes from the beginning of the buffer where the write begins
            // writeEnd: Offset in bytes from the beginning of the buffer where the write ends (exclusive)
            public void UpdateRanges(NativeSlice<GfxUpdateBufferRange> ranges, int writeStart, int writeEnd)
            {
                UpdateBufferRanges(buffer, new IntPtr(ranges.GetUnsafePtr()), ranges.Length, writeStart, writeEnd);
            }

            public int ElementStride { get { return elementStride; } }
            public int ByteCount { get { return elementCount * elementStride; } }
            internal IntPtr BufferPointer { get { return buffer; } }
        }

        unsafe public static void SetVectorArray(IntPtr shaderPropertySheet, int nameID, Vector4[] vector4s)
        {
            SetVectorArray(shaderPropertySheet, nameID, vector4s, vector4s.Length);
        }

        public static event Action<bool> GraphicsResourcesRecreate;
        public static event Action EngineUpdate;
        public static event Action FlushPendingResources;

        [RequiredByNativeCode]
        internal static void RaiseGraphicsResourcesRecreate(bool recreate)
        {
            GraphicsResourcesRecreate?.Invoke(recreate);
        }

        static ProfilerMarker s_MarkerRaiseEngineUpdate = new ProfilerMarker(ProfilerCategory.UIToolkit, "UIR.RaiseEngineUpdate");

        [RequiredByNativeCode]
        internal static void RaiseEngineUpdate()
        {
            if (EngineUpdate != null)
            {
                using (s_MarkerRaiseEngineUpdate.Auto())
                {
                    EngineUpdate.Invoke();
                }
            }
        }

        [RequiredByNativeCode]
        internal static void RaiseFlushPendingResources()
        {
            FlushPendingResources?.Invoke();
        }

        [NativeMethod(IsThreadSafe = true)] extern static IntPtr AllocateBuffer(int elementCount, int elementStride, int bufferFlags);
        [NativeMethod(IsThreadSafe = true)] extern static void FreeBuffer(IntPtr buffer);
        extern static void UpdateBufferRanges(IntPtr buffer, IntPtr ranges, int rangeCount, int writeRangeStart, int writeRangeEnd);
        public extern static void CopyBufferRanges(IntPtr srcBuffer, IntPtr dstBuffer, IntPtr ranges, int rangeCount, GfxCopyBufferRangesFlags flags);
        public extern static void SyncJobFence(JobHandle fence);
        [NativeMethod(IsThreadSafe = true)] extern static void SetVectorArray(IntPtr shaderPropertySheet, int name, Vector4[] values, int count);
        [NativeMethod(IsThreadSafe = true)] public extern static IntPtr GetVertexDeclaration(VertexAttributeDescriptor[] vertexAttributes);
        [NativeMethod(IsThreadSafe = true)] public extern unsafe static void DrawRanges(IntPtr ib, IntPtr* vertexStreams, int streamCount, IntPtr ranges, int rangeCount, IntPtr vertexDecl, KickRangesReason kickReason);
        [NativeMethod(IsThreadSafe = true)] public extern static IntPtr AllocateShaderPropertySheet();
        [NativeMethod(IsThreadSafe = true)] public extern static void SetAllTextures(IntPtr shaderPropertySheet, IntPtr textureNames, IntPtr texturePtrs, int count);
        [NativeMethod(IsThreadSafe = true)] public extern static void SetPropertyBlock(MaterialPropertyBlock props);
        [NativeMethod(IsThreadSafe = true)] public extern static void SetPropertyBlockPtr(IntPtr props);
        [NativeMethod(IsThreadSafe = true)] public extern static void ApplyShaderPropertySheet(IntPtr shaderPropertySheet);
        [NativeMethod(IsThreadSafe = true)] public extern static void ReleasePropertySheet(IntPtr shaderPropertySheet);

        [NativeMethod(IsThreadSafe = true)] public extern static IntPtr AllocateTextureRef(Texture texture);
        [NativeMethod(IsThreadSafe = true)] public extern static void ReleaseTextureRef(IntPtr textureRef);

        [NativeMethod(IsThreadSafe = true)] public extern static void SetScissorRect(RectInt scissorRect);
        [NativeMethod(IsThreadSafe = true)] public extern static void DisableScissor();
        [NativeMethod(IsThreadSafe = true)] public extern static bool IsScissorEnabled();
        [NativeMethod(IsThreadSafe = true)] public extern static IntPtr CreateStencilState(StencilState stencilState);
        [NativeMethod(IsThreadSafe = true)] public extern static void SetStencilState(IntPtr stencilState, int stencilRef);
        [NativeMethod(IsThreadSafe = true)] public extern static bool HasMappedBufferRange();
        [NativeMethod(IsThreadSafe = true)] public extern static UInt32 InsertCPUFence();
        [NativeMethod(IsThreadSafe = true)] public extern static bool CPUFencePassed(UInt32 fence);
        [NativeMethod(IsThreadSafe = true)] public extern static void WaitForCPUFencePassed(UInt32 fence);
        [NativeMethod(IsThreadSafe = true)] public extern static void SyncRenderThread();
        [NativeMethod(IsThreadSafe = true)] public extern static RectInt GetActiveViewport();
        [NativeMethod(IsThreadSafe = true)] public extern static void ProfileDrawChainBegin(EntityId owner);
        [NativeMethod(IsThreadSafe = true)] public extern static void ProfileDrawChainEnd();
        public extern static void NotifyOfUIREvents(bool subscribe);
        [NativeMethod(IsThreadSafe = true)] public extern static Matrix4x4 GetUnityProjectionMatrix();
        [NativeMethod(IsThreadSafe = true)] public extern static Matrix4x4 GetDeviceProjectionMatrix();
        [NativeMethod(IsThreadSafe = true)] public extern static bool DebugIsMainThread(); // For debug code only
    }
}
