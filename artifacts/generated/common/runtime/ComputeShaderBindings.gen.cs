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

namespace UnityEngine
{


public sealed partial class ComputeShader : Object
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public int FindKernel (string name) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public bool HasKernel (string name) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void GetKernelThreadGroupSizes (int kernelIndex, out uint x, out uint y, out uint z) ;

    public void SetFloat(string name, float val)
        {
            SetFloat(Shader.PropertyToID(name), val);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetFloat (int nameID, float val) ;

    public void SetInt(string name, int val)
        {
            SetInt(Shader.PropertyToID(name), val);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetInt (int nameID, int val) ;

    public void SetBool(string name, bool val)
        {
            SetInt(name, val ? 1 : 0);
        }
    
    
    public void SetBool(int nameID, bool val)
        {
            SetInt(nameID, val ? 1 : 0);
        }
    
    
    public void SetVector(string name, Vector4 val)
        {
            SetVector(Shader.PropertyToID(name), val);
        }
    
    
    public void SetVector (int nameID, Vector4 val) {
        INTERNAL_CALL_SetVector ( this, nameID, ref val );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetVector (ComputeShader self, int nameID, ref Vector4 val);
    public void SetVectorArray(string name, Vector4[] values)
        {
            SetVectorArray(Shader.PropertyToID(name), values);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetVectorArray (int nameID, Vector4[] values) ;

    public void SetMatrix(string name, Matrix4x4 val)
        {
            SetMatrix(Shader.PropertyToID(name), val);
        }
    
    
    public void SetMatrix (int nameID, Matrix4x4 val) {
        INTERNAL_CALL_SetMatrix ( this, nameID, ref val );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetMatrix (ComputeShader self, int nameID, ref Matrix4x4 val);
    public void SetMatrixArray(string name, Matrix4x4[] values)
        {
            SetMatrixArray(Shader.PropertyToID(name), values);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetMatrixArray (int nameID, Matrix4x4[] values) ;

    public void SetFloats(string name, params float[] values)
        {
            Internal_SetFloats(Shader.PropertyToID(name), values);
        }
    
    
    public void SetFloats(int nameID, params float[] values)
        {
            Internal_SetFloats(nameID, values);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Internal_SetFloats (int nameID, float[] values) ;

    public void SetInts(string name, params int[] values)
        {
            Internal_SetInts(Shader.PropertyToID(name), values);
        }
    
    
    public void SetInts(int nameID, params int[] values)
        {
            Internal_SetInts(nameID, values);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Internal_SetInts (int nameID, int[] values) ;

    public void SetTexture(int kernelIndex, string name, Texture texture)
        {
            SetTexture(kernelIndex, Shader.PropertyToID(name), texture);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetTexture (int kernelIndex, int nameID, Texture texture) ;

    public void SetTextureFromGlobal(int kernelIndex, string name, string globalTextureName)
        {
            SetTextureFromGlobal(kernelIndex, Shader.PropertyToID(name), Shader.PropertyToID(globalTextureName));
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetTextureFromGlobal (int kernelIndex, int nameID, int globalTextureNameID) ;

    public void SetBuffer(int kernelIndex, string name, ComputeBuffer buffer)
        {
            SetBuffer(kernelIndex, Shader.PropertyToID(name), buffer);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetBuffer (int kernelIndex, int nameID, ComputeBuffer buffer) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void Dispatch (int kernelIndex, int threadGroupsX, int threadGroupsY, int threadGroupsZ) ;

    [uei.ExcludeFromDocs]
public void DispatchIndirect (int kernelIndex, ComputeBuffer argsBuffer) {
    uint argsOffset = 0;
    DispatchIndirect ( kernelIndex, argsBuffer, argsOffset );
}

public void DispatchIndirect(int kernelIndex, ComputeBuffer argsBuffer, [uei.DefaultValue("0")]  uint argsOffset )
        {
            if (argsBuffer == null) throw new ArgumentNullException("argsBuffer");
            if (argsBuffer.m_Ptr == IntPtr.Zero) throw new System.ObjectDisposedException("argsBuffer");

            Internal_DispatchIndirect(kernelIndex, argsBuffer, argsOffset);
        }

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Internal_DispatchIndirect (int kernelIndex, ComputeBuffer argsBuffer, uint argsOffset) ;

}

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

            if (stride < 0)
            {
                throw new ArgumentException("Attempting to create a compute buffer with a negative stride", "stride");
            }

            m_Ptr = IntPtr.Zero;
            InitBuffer(this, count, stride, type);

            SaveCallstack(stackDepth);
        }
    
    
    public void Release()
        {
            Dispose();
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

            InternalSetData(data, 0, 0, data.Length, Marshal.SizeOf(data.GetType().GetElementType()));
        }
    
    
    
            [System.Security.SecuritySafeCritical] 
    public void SetData<T>(List<T> data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            InternalSetData(NoAllocHelpers.ExtractArrayFromList(data), 0, 0, NoAllocHelpers.SafeLength(data), Marshal.SizeOf(typeof(T)));
        }
    
    
    
            [System.Security.SecuritySafeCritical] 
    public void SetData(System.Array data, int managedBufferStartIndex, int computeBufferStartIndex, int count)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (managedBufferStartIndex < 0 || computeBufferStartIndex < 0 || count < 0 || managedBufferStartIndex + count > data.Length)
                throw new ArgumentOutOfRangeException(String.Format("Bad indices/count arguments (managedBufferStartIndex:{0} computeBufferStartIndex:{1} count:{2})", managedBufferStartIndex, computeBufferStartIndex, count));

            InternalSetData(data, managedBufferStartIndex, computeBufferStartIndex, count, Marshal.SizeOf(data.GetType().GetElementType()));
        }
    
    
    
            [System.Security.SecuritySafeCritical] 
    public void SetData<T>(List<T> data, int managedBufferStartIndex, int computeBufferStartIndex, int count)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (managedBufferStartIndex < 0 || computeBufferStartIndex < 0 || count < 0 || managedBufferStartIndex + count > data.Count)
                throw new ArgumentOutOfRangeException(String.Format("Bad indices/count arguments (managedBufferStartIndex:{0} computeBufferStartIndex:{1} count:{2})", managedBufferStartIndex, computeBufferStartIndex, count));

            InternalSetData(NoAllocHelpers.ExtractArrayFromList(data), managedBufferStartIndex, computeBufferStartIndex, count, Marshal.SizeOf(typeof(T)));
        }
    
    
    [System.Security.SecurityCritical] 
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void InternalSetData (System.Array data, int managedBufferStartIndex, int computeBufferStartIndex, int count, int elemSize) ;

    
            [System.Security.SecurityCritical] 
    public void GetData(System.Array data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            InternalGetData(data, 0, 0, data.Length, Marshal.SizeOf(data.GetType().GetElementType()));
        }
    
    
    
            [System.Security.SecurityCritical] 
    public void GetData(System.Array data, int managedBufferStartIndex, int computeBufferStartIndex, int count)
        {
            if (data == null)
                throw new ArgumentNullException("data");

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
