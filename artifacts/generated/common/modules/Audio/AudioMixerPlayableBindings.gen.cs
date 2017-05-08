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

namespace UnityEngine.Audio
{
[RequiredByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct AudioMixerPlayable
{
    public bool GetAutoNormalizeVolumes()
        {
            return GetAutoNormalizeInternal(ref m_Handle);
        }
    
    
    public void GetAutoNormalizeVolumes(bool value)
        {
            SetAutoNormalizeInternal(ref m_Handle, value);
        }
    
    
    private static bool GetAutoNormalizeInternal (ref PlayableHandle hdl) {
        return INTERNAL_CALL_GetAutoNormalizeInternal ( ref hdl );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_GetAutoNormalizeInternal (ref PlayableHandle hdl);
    private static void SetAutoNormalizeInternal (ref PlayableHandle hdl, bool normalise) {
        INTERNAL_CALL_SetAutoNormalizeInternal ( ref hdl, normalise );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetAutoNormalizeInternal (ref PlayableHandle hdl, bool normalise);
    private static bool CreateAudioMixerPlayableInternal (ref PlayableGraph graph, int inputCount, bool normalizeInputVolumes, ref PlayableHandle handle) {
        return INTERNAL_CALL_CreateAudioMixerPlayableInternal ( ref graph, inputCount, normalizeInputVolumes, ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_CreateAudioMixerPlayableInternal (ref PlayableGraph graph, int inputCount, bool normalizeInputVolumes, ref PlayableHandle handle);
}


}
