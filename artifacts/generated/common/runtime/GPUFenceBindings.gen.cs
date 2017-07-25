// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Collections;

namespace UnityEngine.Rendering
{


public enum SynchronisationStage
{
    VertexProcessing = 0,
    PixelProcessing = 1
}

[UsedByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct GPUFence
{
    internal IntPtr m_Ptr;
    
    
    internal int m_Version;
    
    
    public bool passed
        {
            get
            {
                Validate();

                if (!SystemInfo.supportsGPUFence)
                    throw new System.NotSupportedException("Cannot determine if this GPUFence has passed as this platform has not implemented GPUFences.");

                if (!IsFencePending())
                    return true;

                return HasFencePassed_Internal(m_Ptr);
            }
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private bool HasFencePassed_Internal (IntPtr fencePtr) ;

    internal void InitPostAllocation()
        {
            if (m_Ptr == IntPtr.Zero)
            {
                if (SystemInfo.supportsGPUFence)
                {
                    throw new System.NullReferenceException("The internal fence ptr is null, this should not be possible for fences that have been correctly constructed using Graphics.CreateGPUFence() or CommandBuffer.CreateGPUFence()");
                }
                m_Version = GetPlatformNotSupportedVersion();
                return;
            }

            m_Version = GetVersionNumber(m_Ptr);
        }
    
    
    internal bool IsFencePending()
        {
            if (m_Ptr == IntPtr.Zero)
                return false;

            return m_Version == GetVersionNumber(m_Ptr);
        }
    
    
    internal void Validate()
        {
            if (m_Version == 0 || (SystemInfo.supportsGPUFence && m_Version == GetPlatformNotSupportedVersion()))
                throw new System.InvalidOperationException("This GPUFence object has not been correctly constructed see Graphics.CreateGPUFence() or CommandBuffer.CreateGPUFence()");
        }
    
    
    private int GetPlatformNotSupportedVersion()
        {
            return -1;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private int GetVersionNumber (IntPtr fencePtr) ;

}


} 
