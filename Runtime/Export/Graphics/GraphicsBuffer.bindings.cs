// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // Data buffer to hold data for vertex/index buffers.
    [UsedByNativeCode]
    [NativeHeader("Runtime/GfxDevice/GfxBuffer.h")]
    [NativeHeader("Runtime/Shaders/ComputeShader.h")]
    [NativeHeader("Runtime/Shaders/GraphicsBuffer.h")]
    [NativeHeader("Runtime/Export/Graphics/GraphicsBuffer.bindings.h")]
    public sealed class GraphicsBuffer : IDisposable
    {
#pragma warning disable 414
        internal IntPtr m_Ptr;
#pragma warning restore 414

        // IDisposable implementation, with Release() for explicit cleanup.

        [Flags]
        public enum Target
        {
            Vertex            = 1 << 0,
            Index             = 1 << 1,
            Structured        = 1 << 4,
            Raw               = 1 << 5,
            Append            = 1 << 6,
            Counter           = 1 << 7,
            IndirectArguments = 1 << 8,
            Constant          = 1 << 9,
        }

        ~GraphicsBuffer()
        {
            Dispose(false);
        }

        //*undocumented*
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Release native resources
                DestroyBuffer(this);
            }
            else if (m_Ptr != IntPtr.Zero)
            {
                // We cannot call DestroyBuffer through GC - it is scripting_api and requires main thread, prefer leak instead of a crash
                Debug.LogWarning("GarbageCollector disposing of GraphicsBuffer. Please use GraphicsBuffer.Release() or .Dispose() to manually release the buffer.");
            }

            m_Ptr = IntPtr.Zero;
        }

        private static bool RequiresCompute(Target target)
        {
            int iTarget = (int)target;
            int vbIbMask = (int)(Target.Vertex | Target.Index);

            // Compute support is required if target contains any flag that is not Vertex or Index
            return (iTarget & vbIbMask) != iTarget;
        }

        [FreeFunction("GraphicsBuffer_Bindings::InitBuffer")]
        extern private static IntPtr InitBuffer(Target target, int count, int stride);

        [FreeFunction("GraphicsBuffer_Bindings::DestroyBuffer")]
        extern private static void DestroyBuffer(GraphicsBuffer buf);

        // Create a Graphics Buffer.
        public GraphicsBuffer(Target target, int count, int stride)
        {
            if (RequiresCompute(target) && !SystemInfo.supportsComputeShaders)
            {
                throw new ArgumentException("Attempting to create a graphics buffer that requires compute shader support, but compute shaders are not supported on this platform. Target: " + target);
            }

            if (count <= 0)
            {
                throw new ArgumentException("Attempting to create a zero length graphics buffer", "count");
            }

            if (stride <= 0)
            {
                throw new ArgumentException("Attempting to create a graphics buffer with a negative or null stride", "stride");
            }

            if ((target & Target.Index) != 0 && stride != 2 && stride != 4)
            {
                throw new ArgumentException("Attempting to create an index buffer with an invalid stride: " + stride, "stride");
            }
            else if (RequiresCompute(target) && stride % 4 != 0)
            {
                throw new ArgumentException("Stride must be a multiple of 4 unless the buffer is only used as a vertex buffer and/or index buffer ", "stride");
            }

            m_Ptr = InitBuffer(target, count, stride);
        }

        // Release a Graphics Buffer.
        public void Release()
        {
            Dispose();
        }

        public bool IsValid()
        {
            return m_Ptr != IntPtr.Zero;
        }

        // Number of elements in the buffer (RO).
        extern public int count { get; }

        // Size of one element in the buffer (RO).
        extern public int stride { get; }

        // Set buffer data.
        [System.Security.SecuritySafeCritical] // due to Marshal.SizeOf
        public void SetData(System.Array data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (!UnsafeUtility.IsArrayBlittable(data))
            {
                throw new ArgumentException(
                    string.Format("Array passed to GraphicsBuffer.SetData(array) must be blittable.\n{0}",
                        UnsafeUtility.GetReasonForArrayNonBlittable(data)));
            }

            InternalSetData(data, 0, 0, data.Length, UnsafeUtility.SizeOf(data.GetType().GetElementType()));
        }

        // Set buffer data.
        [System.Security.SecuritySafeCritical] // due to Marshal.SizeOf
        public void SetData<T>(List<T> data) where T : struct
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (!UnsafeUtility.IsGenericListBlittable<T>())
            {
                throw new ArgumentException(
                    string.Format("List<{0}> passed to GraphicsBuffer.SetData(List<>) must be blittable.\n{1}",
                        typeof(T), UnsafeUtility.GetReasonForGenericListNonBlittable<T>()));
            }

            InternalSetData(NoAllocHelpers.ExtractArrayFromList(data), 0, 0, NoAllocHelpers.SafeLength(data), Marshal.SizeOf(typeof(T)));
        }

        [System.Security.SecuritySafeCritical] // due to Marshal.SizeOf
        unsafe public void SetData<T>(NativeArray<T> data) where T : struct
        {
            // Note: no IsBlittable test here because it's already done at NativeArray creation time
            InternalSetNativeData((IntPtr)data.GetUnsafeReadOnlyPtr(), 0, 0, data.Length, UnsafeUtility.SizeOf<T>());
        }

        // Set partial buffer data
        [System.Security.SecuritySafeCritical] // due to Marshal.SizeOf
        public void SetData(System.Array data, int managedBufferStartIndex, int graphicsBufferStartIndex, int count)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (!UnsafeUtility.IsArrayBlittable(data))
            {
                throw new ArgumentException(
                    string.Format("Array passed to GraphicsBuffer.SetData(array) must be blittable.\n{0}",
                        UnsafeUtility.GetReasonForArrayNonBlittable(data)));
            }

            if (managedBufferStartIndex < 0 || graphicsBufferStartIndex < 0 || count < 0 || managedBufferStartIndex + count > data.Length)
                throw new ArgumentOutOfRangeException(String.Format("Bad indices/count arguments (managedBufferStartIndex:{0} graphicsBufferStartIndex:{1} count:{2})", managedBufferStartIndex, graphicsBufferStartIndex, count));

            InternalSetData(data, managedBufferStartIndex, graphicsBufferStartIndex, count, Marshal.SizeOf(data.GetType().GetElementType()));
        }

        // Set partial buffer data
        [System.Security.SecuritySafeCritical] // due to Marshal.SizeOf
        public void SetData<T>(List<T> data, int managedBufferStartIndex, int graphicsBufferStartIndex, int count) where T : struct
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (!UnsafeUtility.IsGenericListBlittable<T>())
            {
                throw new ArgumentException(
                    string.Format("List<{0}> passed to GraphicsBuffer.SetData(List<>) must be blittable.\n{1}",
                        typeof(T), UnsafeUtility.GetReasonForGenericListNonBlittable<T>()));
            }

            if (managedBufferStartIndex < 0 || graphicsBufferStartIndex < 0 || count < 0 || managedBufferStartIndex + count > data.Count)
                throw new ArgumentOutOfRangeException(String.Format("Bad indices/count arguments (managedBufferStartIndex:{0} graphicsBufferStartIndex:{1} count:{2})", managedBufferStartIndex, graphicsBufferStartIndex, count));

            InternalSetData(NoAllocHelpers.ExtractArrayFromList(data), managedBufferStartIndex, graphicsBufferStartIndex, count, Marshal.SizeOf(typeof(T)));
        }

        [System.Security.SecuritySafeCritical] // due to Marshal.SizeOf
        public unsafe void SetData<T>(NativeArray<T> data, int nativeBufferStartIndex, int graphicsBufferStartIndex, int count) where T : struct
        {
            // Note: no IsBlittable test here because it's already done at NativeArray creation time
            if (nativeBufferStartIndex < 0 || graphicsBufferStartIndex < 0 || count < 0 || nativeBufferStartIndex + count > data.Length)
                throw new ArgumentOutOfRangeException(String.Format("Bad indices/count arguments (nativeBufferStartIndex:{0} graphicsBufferStartIndex:{1} count:{2})", nativeBufferStartIndex, graphicsBufferStartIndex, count));

            InternalSetNativeData((IntPtr)data.GetUnsafeReadOnlyPtr(), nativeBufferStartIndex, graphicsBufferStartIndex, count, UnsafeUtility.SizeOf<T>());
        }

        [System.Security.SecurityCritical] // to prevent accidentally making this public in the future
        [FreeFunction(Name = "GraphicsBuffer_Bindings::InternalSetNativeData", HasExplicitThis = true, ThrowsException = true)]
        extern private void InternalSetNativeData(IntPtr data, int nativeBufferStartIndex, int graphicsBufferStartIndex, int count, int elemSize);

        [System.Security.SecurityCritical] // to prevent accidentally making this public in the future
        [FreeFunction(Name = "GraphicsBuffer_Bindings::InternalSetData", HasExplicitThis = true, ThrowsException = true)]
        extern private void InternalSetData(System.Array data, int managedBufferStartIndex, int graphicsBufferStartIndex, int count, int elemSize);

        [System.Security.SecurityCritical] // due to Marshal.SizeOf
        public void GetData(System.Array data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (!UnsafeUtility.IsArrayBlittable(data))
            {
                throw new ArgumentException(
                    string.Format("Array passed to GraphicsBuffer.GetData(array) must be blittable.\n{0}",
                        UnsafeUtility.GetReasonForArrayNonBlittable(data)));
            }

            InternalGetData(data, 0, 0, data.Length, Marshal.SizeOf(data.GetType().GetElementType()));
        }

        // Read partial buffer data.
        [System.Security.SecurityCritical] // due to Marshal.SizeOf
        public void GetData(System.Array data, int managedBufferStartIndex, int computeBufferStartIndex, int count)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (!UnsafeUtility.IsArrayBlittable(data))
            {
                throw new ArgumentException(
                    string.Format("Array passed to GraphicsBuffer.GetData(array) must be blittable.\n{0}",
                        UnsafeUtility.GetReasonForArrayNonBlittable(data)));
            }

            if (managedBufferStartIndex < 0 || computeBufferStartIndex < 0 || count < 0 || managedBufferStartIndex + count > data.Length)
                throw new ArgumentOutOfRangeException(String.Format("Bad indices/count argument (managedBufferStartIndex:{0} computeBufferStartIndex:{1} count:{2})", managedBufferStartIndex, computeBufferStartIndex, count));

            InternalGetData(data, managedBufferStartIndex, computeBufferStartIndex, count, Marshal.SizeOf(data.GetType().GetElementType()));
        }

        [System.Security.SecurityCritical] // to prevent accidentally making this public in the future
        [FreeFunction(Name = "GraphicsBuffer_Bindings::InternalGetData", HasExplicitThis = true, ThrowsException = true)]
        extern private void InternalGetData(System.Array data, int managedBufferStartIndex, int computeBufferStartIndex, int count, int elemSize);

        [FreeFunction(Name = "GraphicsBuffer_Bindings::InternalGetNativeBufferPtr", HasExplicitThis = true)]
        extern public IntPtr GetNativeBufferPtr();

        [FreeFunction(Name = "GraphicsBuffer_Bindings::SetName", HasExplicitThis = true)]
        extern private void SetName(string name);

        // Set counter value of append/consume buffer.
        extern public void SetCounterValue(uint counterValue);

        // Copy counter value of append/consume buffer into another buffer.
        [FreeFunction(Name = "GraphicsBuffer_Bindings::CopyCount")]
        extern private static void CopyCountCC(ComputeBuffer src, ComputeBuffer dst, int dstOffsetBytes);
        [FreeFunction(Name = "GraphicsBuffer_Bindings::CopyCount")]
        extern private static void CopyCountGC(GraphicsBuffer src, ComputeBuffer dst, int dstOffsetBytes);
        [FreeFunction(Name = "GraphicsBuffer_Bindings::CopyCount")]
        extern private static void CopyCountCG(ComputeBuffer src, GraphicsBuffer dst, int dstOffsetBytes);
        [FreeFunction(Name = "GraphicsBuffer_Bindings::CopyCount")]
        extern private static void CopyCountGG(GraphicsBuffer src, GraphicsBuffer dst, int dstOffsetBytes);

        public static void CopyCount(ComputeBuffer src, ComputeBuffer dst, int dstOffsetBytes)
        {
            CopyCountCC(src, dst, dstOffsetBytes);
        }

        public static void CopyCount(GraphicsBuffer src, ComputeBuffer dst, int dstOffsetBytes)
        {
            CopyCountGC(src, dst, dstOffsetBytes);
        }

        public static void CopyCount(ComputeBuffer src, GraphicsBuffer dst, int dstOffsetBytes)
        {
            CopyCountCG(src, dst, dstOffsetBytes);
        }

        public static void CopyCount(GraphicsBuffer src, GraphicsBuffer dst, int dstOffsetBytes)
        {
            CopyCountGG(src, dst, dstOffsetBytes);
        }
    }
}
