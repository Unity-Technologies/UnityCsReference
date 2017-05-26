// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;


using System;
using UnityEngine.Playables;

namespace UnityEngine.Animations
{
[RequiredByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
internal partial struct AnimationOffsetPlayable
{
    private static bool CreateHandleInternal (PlayableGraph graph, Vector3 position, Quaternion rotation, ref PlayableHandle handle) {
        return INTERNAL_CALL_CreateHandleInternal ( ref graph, ref position, ref rotation, ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_CreateHandleInternal (ref PlayableGraph graph, ref Vector3 position, ref Quaternion rotation, ref PlayableHandle handle);
    public Vector3 GetPosition()
        {
            return GetPositionInternal(ref m_Handle);
        }
    
    
    public void SetPosition(Vector3 value)
        {
            SetPositionInternal(ref m_Handle, value);
        }
    
    
    public Quaternion GetRotation()
        {
            return GetRotationInternal(ref m_Handle);
        }
    
    
    public void SetRotation(Quaternion value)
        {
            SetRotationInternal(ref m_Handle, value);
        }
    
    
    private static Vector3 GetPositionInternal (ref PlayableHandle handle) {
        Vector3 result;
        INTERNAL_CALL_GetPositionInternal ( ref handle, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetPositionInternal (ref PlayableHandle handle, out Vector3 value);
    private static void SetPositionInternal (ref PlayableHandle handle, Vector3 value) {
        INTERNAL_CALL_SetPositionInternal ( ref handle, ref value );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetPositionInternal (ref PlayableHandle handle, ref Vector3 value);
    private static Quaternion GetRotationInternal (ref PlayableHandle handle) {
        Quaternion result;
        INTERNAL_CALL_GetRotationInternal ( ref handle, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetRotationInternal (ref PlayableHandle handle, out Quaternion value);
    private static void SetRotationInternal (ref PlayableHandle handle, Quaternion value) {
        INTERNAL_CALL_SetRotationInternal ( ref handle, ref value );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetRotationInternal (ref PlayableHandle handle, ref Quaternion value);
}

}
