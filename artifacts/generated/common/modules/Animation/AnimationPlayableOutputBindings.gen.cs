// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine.Playables;

using Object = UnityEngine.Object;

namespace UnityEngine.Animations
{
[RequiredByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct AnimationPlayableOutput
{
    public Animator GetTarget()
        {
            return InternalGetTarget(ref m_Handle);
        }
    
    
    public void SetTarget(Animator value)
        {
            InternalSetTarget(ref m_Handle, value);
        }
    
    
    private static Animator InternalGetTarget (ref PlayableOutputHandle handle) {
        return INTERNAL_CALL_InternalGetTarget ( ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Animator INTERNAL_CALL_InternalGetTarget (ref PlayableOutputHandle handle);
    private static void InternalSetTarget (ref PlayableOutputHandle handle, Animator target) {
        INTERNAL_CALL_InternalSetTarget ( ref handle, target );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_InternalSetTarget (ref PlayableOutputHandle handle, Animator target);
}

}
