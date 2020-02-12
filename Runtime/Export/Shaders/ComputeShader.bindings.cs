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
    // Data buffer to hold data for compute shaders.
    [UsedByNativeCode]
    [NativeHeader("Runtime/Shaders/ComputeShader.h")]
    [NativeHeader("Runtime/Export/Shaders/ComputeShader.bindings.h")]
    public sealed class ComputeBuffer : IDisposable
    {
#pragma warning disable 414
        internal IntPtr m_Ptr;
#pragma warning restore 414

        AtomicSafetyHandle m_Safety;

        // IDisposable implementation, with Release() for explicit cleanup.

        ~ComputeBuffer()
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
                Debug.LogWarning(string.Format("GarbageCollector disposing of ComputeBuffer allocated in {0} at line {1}. Please use ComputeBuffer.Release() or .Dispose() to manually release the buffer.", GetFileName(), GetLineNumber()));
            }

            m_Ptr = IntPtr.Zero;
        }

        [FreeFunction("ComputeShader_Bindings::InitBuffer")]
        extern private static IntPtr InitBuffer(int count, int stride, ComputeBufferType type, ComputeBufferMode usage);

        [FreeFunction("ComputeShader_Bindings::DestroyBuffer")]
        extern private static void DestroyBuffer(ComputeBuffer buf);

        ///*listonly*
        public ComputeBuffer(int count, int stride) : this(count, stride, ComputeBufferType.Default, ComputeBufferMode.Immutable, 3)
        {
        }

        // Create a Compute Buffer.
        public ComputeBuffer(int count, int stride, ComputeBufferType type) : this(count, stride, type, ComputeBufferMode.Immutable, 3)
        {
        }

        public ComputeBuffer(int count, int stride, ComputeBufferType type, ComputeBufferMode usage) : this(count, stride, type, usage, 3)
        {
        }

        internal ComputeBuffer(int count, int stride, ComputeBufferType type, ComputeBufferMode usage, int stackDepth)
        {
            if (count <= 0)
            {
                throw new ArgumentException("Attempting to create a zero length compute buffer", "count");
            }

            if (stride <= 0)
            {
                throw new ArgumentException("Attempting to create a compute buffer with a negative or null stride", "stride");
            }

            m_Ptr = InitBuffer(count, stride, type, usage);

            SaveCallstack(stackDepth);
        }

        // Release a Compute Buffer.
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

        extern private ComputeBufferMode usage { get; }

        // Set buffer data.
        [System.Security.SecuritySafeCritical] // due to Marshal.SizeOf
        public void SetData(System.Array data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (!UnsafeUtility.IsArrayBlittable(data))
            {
                throw new ArgumentException(
                    string.Format("Array passed to ComputeBuffer.SetData(array) must be blittable.\n{0}",
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
                    string.Format("List<{0}> passed to ComputeBuffer.SetData(List<>) must be blittable.\n{1}",
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
        public void SetData(System.Array data, int managedBufferStartIndex, int computeBufferStartIndex, int count)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (!UnsafeUtility.IsArrayBlittable(data))
            {
                throw new ArgumentException(
                    string.Format("Array passed to ComputeBuffer.SetData(array) must be blittable.\n{0}",
                        UnsafeUtility.GetReasonForArrayNonBlittable(data)));
            }

            if (managedBufferStartIndex < 0 || computeBufferStartIndex < 0 || count < 0 || managedBufferStartIndex + count > data.Length)
                throw new ArgumentOutOfRangeException(String.Format("Bad indices/count arguments (managedBufferStartIndex:{0} computeBufferStartIndex:{1} count:{2})", managedBufferStartIndex, computeBufferStartIndex, count));

            InternalSetData(data, managedBufferStartIndex, computeBufferStartIndex, count, Marshal.SizeOf(data.GetType().GetElementType()));
        }

        // Set partial buffer data
        [System.Security.SecuritySafeCritical] // due to Marshal.SizeOf
        public void SetData<T>(List<T> data, int managedBufferStartIndex, int computeBufferStartIndex, int count) where T : struct
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (!UnsafeUtility.IsGenericListBlittable<T>())
            {
                throw new ArgumentException(
                    string.Format("List<{0}> passed to ComputeBuffer.SetData(List<>) must be blittable.\n{1}",
                        typeof(T), UnsafeUtility.GetReasonForGenericListNonBlittable<T>()));
            }

            if (managedBufferStartIndex < 0 || computeBufferStartIndex < 0 || count < 0 || managedBufferStartIndex + count > data.Count)
                throw new ArgumentOutOfRangeException(String.Format("Bad indices/count arguments (managedBufferStartIndex:{0} computeBufferStartIndex:{1} count:{2})", managedBufferStartIndex, computeBufferStartIndex, count));

            InternalSetData(NoAllocHelpers.ExtractArrayFromList(data), managedBufferStartIndex, computeBufferStartIndex, count, Marshal.SizeOf(typeof(T)));
        }

        [System.Security.SecuritySafeCritical] // due to Marshal.SizeOf
        public unsafe void SetData<T>(NativeArray<T> data, int nativeBufferStartIndex, int computeBufferStartIndex, int count) where T : struct
        {
            // Note: no IsBlittable test here because it's already done at NativeArray creation time
            if (nativeBufferStartIndex < 0 || computeBufferStartIndex < 0 || count < 0 || nativeBufferStartIndex + count > data.Length)
                throw new ArgumentOutOfRangeException(String.Format("Bad indices/count arguments (nativeBufferStartIndex:{0} computeBufferStartIndex:{1} count:{2})", nativeBufferStartIndex, computeBufferStartIndex, count));

            InternalSetNativeData((IntPtr)data.GetUnsafeReadOnlyPtr(), nativeBufferStartIndex, computeBufferStartIndex, count, UnsafeUtility.SizeOf<T>());
        }

        [System.Security.SecurityCritical] // to prevent accidentally making this public in the future
        [FreeFunction(Name = "ComputeShader_Bindings::InternalSetNativeData", HasExplicitThis = true, ThrowsException = true)]
        extern private void InternalSetNativeData(IntPtr data, int nativeBufferStartIndex, int computeBufferStartIndex, int count, int elemSize);

        [System.Security.SecurityCritical] // to prevent accidentally making this public in the future
        [FreeFunction(Name = "ComputeShader_Bindings::InternalSetData", HasExplicitThis = true, ThrowsException = true)]
        extern private void InternalSetData(System.Array data, int managedBufferStartIndex, int computeBufferStartIndex, int count, int elemSize);

        // Read buffer data.
        [System.Security.SecurityCritical] // due to Marshal.SizeOf
        public void GetData(System.Array data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (!UnsafeUtility.IsArrayBlittable(data))
            {
                throw new ArgumentException(
                    string.Format("Array passed to ComputeBuffer.GetData(array) must be blittable.\n{0}",
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
                    string.Format("Array passed to ComputeBuffer.GetData(array) must be blittable.\n{0}",
                        UnsafeUtility.GetReasonForArrayNonBlittable(data)));
            }

            if (managedBufferStartIndex < 0 || computeBufferStartIndex < 0 || count < 0 || managedBufferStartIndex + count > data.Length)
                throw new ArgumentOutOfRangeException(String.Format("Bad indices/count argument (managedBufferStartIndex:{0} computeBufferStartIndex:{1} count:{2})", managedBufferStartIndex, computeBufferStartIndex, count));

            InternalGetData(data, managedBufferStartIndex, computeBufferStartIndex, count, Marshal.SizeOf(data.GetType().GetElementType()));
        }

        [System.Security.SecurityCritical] // to prevent accidentally making this public in the future
        [FreeFunction(Name = "ComputeShader_Bindings::InternalGetData", HasExplicitThis = true, ThrowsException = true)]
        extern private void InternalGetData(System.Array data, int managedBufferStartIndex, int computeBufferStartIndex, int count, int elemSize);

        extern unsafe private void* BeginBufferWrite(int offset = 0, int size = 0);

        public NativeArray<T> BeginWrite<T>(int computeBufferStartIndex, int count) where T : struct
        {
            if (usage != ComputeBufferMode.SubUpdates)
                throw new ArgumentException("ComputeBuffer must be created with usage mode ComputeBufferMode.SubUpdates to be able to be mapped with BeginWrite");

            var elementSize = UnsafeUtility.SizeOf<T>();
            if (computeBufferStartIndex < 0 || count < 0 || (computeBufferStartIndex + count) * elementSize > this.count * this.stride)
                throw new ArgumentOutOfRangeException(String.Format("Bad indices/count arguments (computeBufferStartIndex:{0} count:{1} elementSize:{2}, this.count:{3}, this.stride{4})", computeBufferStartIndex, count, elementSize, this.count, this.stride));

            NativeArray<T> array;
            unsafe
            {
                var ptr = BeginBufferWrite(computeBufferStartIndex * elementSize, count * elementSize);
                array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>((void*)ptr, count, Allocator.Invalid);
            }
            m_Safety = AtomicSafetyHandle.Create();
            AtomicSafetyHandle.SetAllowSecondaryVersionWriting(m_Safety, true);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, m_Safety);
            return array;
        }

        extern private void EndBufferWrite(int bytesWritten = 0);

        public void EndWrite<T>(int countWritten) where T : struct
        {
            try
            {
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
                AtomicSafetyHandle.Release(m_Safety);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("ComputeBuffer.EndWrite was called without matching ComputeBuffer.BeginWrite", e);
            }
            if (countWritten < 0)
                throw new ArgumentOutOfRangeException(String.Format("Bad indices/count arguments (countWritten:{0})", countWritten));

            var elementSize = UnsafeUtility.SizeOf<T>();
            EndBufferWrite(countWritten * elementSize);
        }

        // Buffer name for graphics debuggers
        public string name { set { SetName(value); } }

        [FreeFunction(Name = "ComputeShader_Bindings::SetName", HasExplicitThis = true)]
        extern private void SetName(string name);

        // Set counter value of append/consume buffer.
        extern public void SetCounterValue(uint counterValue);

        // Copy counter value of append/consume buffer into another buffer.
        extern public static void CopyCount(ComputeBuffer src, ComputeBuffer dst, int dstOffsetBytes);

        extern public IntPtr GetNativeBufferPtr();

        [ThreadSafe]
        extern internal string GetFileName();

        [ThreadSafe]
        extern internal int GetLineNumber();

        internal void SaveCallstack(int stackDepth)
        {
            StackFrame frame = new StackFrame(stackDepth, true);
            SaveCallstack_Internal(frame.GetFileName(), frame.GetFileLineNumber());
        }

        [NativeName("SetAllocationData")]
        extern private void SaveCallstack_Internal(string fileName, int lineNumber);
    }
}
