// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine
{
[UsedByNativeCode]
public sealed partial class ComputeBuffer : IDisposable
{
    #pragma warning disable 414
    internal IntPtr m_Ptr;
    #pragma warning restore 414
    
    
            ~ComputeBuffer()
        {
            Dispose(false);
        }
    
    
    public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    
    private void Dispose(bool disposing)
        {
            if (disposing)
            {
                DestroyBuffer(this);
            }
            else if (m_Ptr != IntPtr.Zero)
            {
                Debug.LogWarning(string.Format("GarbageCollector disposing of ComputeBuffer allocated in {0} at line {1}. Please use ComputeBuffer.Release() or .Dispose() to manually release the buffer.", GetFileName(), GetLineNumber()));
            }

            m_Ptr = IntPtr.Zero;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void InitBuffer (ComputeBuffer buf, int count, int stride, ComputeBufferType type) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void DestroyBuffer (ComputeBuffer buf) ;

    public ComputeBuffer(int count, int stride) : this(count, stride, ComputeBufferType.Default, 3)
        {
        }
    
    
    public ComputeBuffer(int count, int stride, ComputeBufferType type) : this(count, stride, type, 3)
        {
        }
    
    
    internal ComputeBuffer(int count, int stride, ComputeBufferType type, int stackDepth)
        {
            if (count <= 0)
            {
                throw new ArgumentException("Attempting to create a zero length compute buffer", "count");
            }

            if (stride <= 0)
            {
                throw new ArgumentException("Attempting to create a compute buffer with a negative or null stride", "stride");
            }

            m_Ptr = IntPtr.Zero;
            InitBuffer(this, count, stride, type);

            SaveCallstack(stackDepth);
        }
    
    
    public void Release()
        {
            Dispose();
        }
    
    
    public bool IsValid()
        {
            return m_Ptr != IntPtr.Zero;
        }
    
    
    public extern  int count
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  int stride
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    
            [System.Security.SecuritySafeCritical] 
    public void SetData(System.Array data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (!UnsafeUtility.IsBlittable(data.GetType().GetElementType()))
                throw new ArgumentException(string.Format("{0} type used in ComputeBuffer.SetData(array) must be blittable", data.GetType().GetElementType()));

            InternalSetData(data, 0, 0, data.Length, UnsafeUtility.SizeOf(data.GetType().GetElementType()));
        }
    
    
    
            [System.Security.SecuritySafeCritical] 
    public void SetData<T>(List<T> data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (!UnsafeUtility.IsBlittable(typeof(T)))
                throw new ArgumentException(string.Format("{0} type used in ComputeBuffer.SetData(List<>)) must be blittable", typeof(T)));

            InternalSetData(NoAllocHelpers.ExtractArrayFromList(data), 0, 0, NoAllocHelpers.SafeLength(data), Marshal.SizeOf(typeof(T)));
        }
    
    
    
            [System.Security.SecuritySafeCritical] 
    unsafe public void SetData<T>(NativeArray<T> data) where T : struct
        {
            InternalSetNativeData((IntPtr)data.GetUnsafeReadOnlyPtr(), 0, 0, data.Length, UnsafeUtility.SizeOf<T>());
        }
    
    
    
            [System.Security.SecuritySafeCritical] 
    public void SetData(System.Array data, int managedBufferStartIndex, int computeBufferStartIndex, int count)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (!UnsafeUtility.IsBlittable(data.GetType().GetElementType()))
                throw new ArgumentException(string.Format("{0} type used in ComputeBuffer.SetData(array) must be blittable", data.GetType().GetElementType()));

            if (managedBufferStartIndex < 0 || computeBufferStartIndex < 0 || count < 0 || managedBufferStartIndex + count > data.Length)
                throw new ArgumentOutOfRangeException(String.Format("Bad indices/count arguments (managedBufferStartIndex:{0} computeBufferStartIndex:{1} count:{2})", managedBufferStartIndex, computeBufferStartIndex, count));

            InternalSetData(data, managedBufferStartIndex, computeBufferStartIndex, count, Marshal.SizeOf(data.GetType().GetElementType()));
        }
    
    
    
            [System.Security.SecuritySafeCritical] 
    public void SetData<T>(List<T> data, int managedBufferStartIndex, int computeBufferStartIndex, int count)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (!UnsafeUtility.IsBlittable(typeof(T)))
                throw new ArgumentException(string.Format("{0} type used in ComputeBuffer.SetData(List<>)) must be blittable", typeof(T)));

            if (managedBufferStartIndex < 0 || computeBufferStartIndex < 0 || count < 0 || managedBufferStartIndex + count > data.Count)
                throw new ArgumentOutOfRangeException(String.Format("Bad indices/count arguments (managedBufferStartIndex:{0} computeBufferStartIndex:{1} count:{2})", managedBufferStartIndex, computeBufferStartIndex, count));

            InternalSetData(NoAllocHelpers.ExtractArrayFromList(data), managedBufferStartIndex, computeBufferStartIndex, count, Marshal.SizeOf(typeof(T)));
        }
    
    
    
            [System.Security.SecuritySafeCritical] 
    public unsafe void SetData<T>(NativeArray<T> data, int nativeBufferStartIndex, int computeBufferStartIndex, int count) where T : struct
        {
            if (nativeBufferStartIndex < 0 || computeBufferStartIndex < 0 || count < 0 || nativeBufferStartIndex + count > data.Length)
                throw new ArgumentOutOfRangeException(String.Format("Bad indices/count arguments (nativeBufferStartIndex:{0} computeBufferStartIndex:{1} count:{2})", nativeBufferStartIndex, computeBufferStartIndex, count));

            InternalSetNativeData((IntPtr)data.GetUnsafeReadOnlyPtr(), nativeBufferStartIndex, computeBufferStartIndex, count, UnsafeUtility.SizeOf<T>());
        }
    
    
    [System.Security.SecurityCritical] 
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void InternalSetNativeData (IntPtr data, int nativeBufferStartIndex, int computeBufferStartIndex, int count, int elemSize) ;

    [System.Security.SecurityCritical] 
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void InternalSetData (System.Array data, int managedBufferStartIndex, int computeBufferStartIndex, int count, int elemSize) ;

    
            [System.Security.SecurityCritical] 
    public void GetData(System.Array data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (!UnsafeUtility.IsBlittable(data.GetType().GetElementType()))
                throw new ArgumentException(string.Format("{0} type used in ComputeBuffer.GetData(array) must be blittable", data.GetType().GetElementType()));

            InternalGetData(data, 0, 0, data.Length, Marshal.SizeOf(data.GetType().GetElementType()));
        }
    
    
    
            [System.Security.SecurityCritical] 
    public void GetData(System.Array data, int managedBufferStartIndex, int computeBufferStartIndex, int count)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (!UnsafeUtility.IsBlittable(data.GetType().GetElementType()))
                throw new ArgumentException(string.Format("{0} type used in ComputeBuffer.GetData(array) must be blittable", data.GetType().GetElementType()));

            if (managedBufferStartIndex < 0 || computeBufferStartIndex < 0 || count < 0 || managedBufferStartIndex + count > data.Length)
                throw new ArgumentOutOfRangeException(String.Format("Bad indices/count argument (managedBufferStartIndex:{0} computeBufferStartIndex:{1} count:{2})", managedBufferStartIndex, computeBufferStartIndex, count));

            InternalGetData(data, managedBufferStartIndex, computeBufferStartIndex, count, Marshal.SizeOf(data.GetType().GetElementType()));
        }
    
    
    [System.Security.SecurityCritical] 
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void InternalGetData (System.Array data, int managedBufferStartIndex, int computeBufferStartIndex, int count, int elemSize) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetCounterValue (uint counterValue) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void CopyCount (ComputeBuffer src, ComputeBuffer dst, int dstOffsetBytes) ;

    public IntPtr GetNativeBufferPtr () {
        IntPtr result;
        INTERNAL_CALL_GetNativeBufferPtr ( this, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetNativeBufferPtr (ComputeBuffer self, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal string GetFileName () ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal int GetLineNumber () ;

    internal void SaveCallstack(int stackDepth)
        {
            StackFrame frame = new StackFrame(stackDepth, true);
            SaveCallstack_Internal(frame.GetFileName(), frame.GetFileLineNumber());
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void SaveCallstack_Internal (string fileName, int lineNumber) ;

}


}
