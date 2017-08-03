// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;

namespace UnityEditor
{


[RequiredByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
internal partial struct ChangeTrackerHandle
{
    IntPtr    m_Handle;
    
    
    internal static ChangeTrackerHandle AcquireTracker(UnityEngine.Object obj)
        {
            if (obj == null)
                throw new ArgumentNullException("Not a valid unity engine object");
            return Internal_AcquireTracker(obj);
        }
    
    
    private static ChangeTrackerHandle Internal_AcquireTracker (UnityEngine.Object o) {
        ChangeTrackerHandle result;
        INTERNAL_CALL_Internal_AcquireTracker ( o, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_AcquireTracker (UnityEngine.Object o, out ChangeTrackerHandle value);
    internal void ReleaseTracker()
        {
            if (m_Handle == IntPtr.Zero)
                throw new ArgumentNullException("Not a valid handle, has it been released already?");

            Internal_ReleaseTracker(this);
            m_Handle = IntPtr.Zero;
        }
    
    
    private static void Internal_ReleaseTracker (ChangeTrackerHandle h) {
        INTERNAL_CALL_Internal_ReleaseTracker ( ref h );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_ReleaseTracker (ref ChangeTrackerHandle h);
    internal bool PollForChanges()
        {
            if (m_Handle == IntPtr.Zero)
                throw new ArgumentNullException("Not a valid handle, has it been released already?");
            return Internal_PollChanges();
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private bool Internal_PollChanges () ;

    internal void ForceDirtyNextPoll()
        {
            if (m_Handle == IntPtr.Zero)
                throw new ArgumentNullException("Not a valid handle, has it been released already?");
            Internal_ForceUpdate();
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Internal_ForceUpdate () ;

}

}
