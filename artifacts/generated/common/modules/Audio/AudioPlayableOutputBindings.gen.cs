// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

namespace UnityEngine.Audio
{
[RequiredByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct AudioPlayableOutput
{
    public AudioSource GetTarget()
        {
            return InternalGetTarget(ref m_Handle);
        }
    
    
    public void SetTarget(AudioSource value)
        {
            InternalSetTarget(ref m_Handle, value);
        }
    
    
    private static AudioSource InternalGetTarget (ref PlayableOutputHandle output) {
        return INTERNAL_CALL_InternalGetTarget ( ref output );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static AudioSource INTERNAL_CALL_InternalGetTarget (ref PlayableOutputHandle output);
    private static void InternalSetTarget (ref PlayableOutputHandle output, AudioSource target) {
        INTERNAL_CALL_InternalSetTarget ( ref output, target );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_InternalSetTarget (ref PlayableOutputHandle output, AudioSource target);
}


}
