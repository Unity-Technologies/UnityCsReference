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
using Object = UnityEngine.Object;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;

namespace UnityEditor.Audio
{


internal enum ParameterTransitionType
{
    Lerp = 0,
    Smoothstep = 1,
    Squared = 2,
    SquareRoot = 3,
    BrickwallStart = 4,
    BrickwallEnd = 5
}

internal sealed partial class AudioMixerSnapshotController : AudioMixerSnapshot
{
    public AudioMixerSnapshotController(AudioMixer owner)
        {
            Internal_CreateAudioMixerSnapshotController(this, owner);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_CreateAudioMixerSnapshotController (AudioMixerSnapshotController mono, AudioMixer owner) ;

    public extern GUID snapshotID
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public void SetValue (GUID guid, float value) {
        INTERNAL_CALL_SetValue ( this, guid, value );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetValue (AudioMixerSnapshotController self, GUID guid, float value);
    public bool GetValue (GUID guid, out float value) {
        return INTERNAL_CALL_GetValue ( this, guid, out value );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_GetValue (AudioMixerSnapshotController self, GUID guid, out float value);
    public void SetTransitionTypeOverride (GUID guid, ParameterTransitionType type) {
        INTERNAL_CALL_SetTransitionTypeOverride ( this, guid, type );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetTransitionTypeOverride (AudioMixerSnapshotController self, GUID guid, ParameterTransitionType type);
    public bool GetTransitionTypeOverride (GUID guid, out ParameterTransitionType type) {
        return INTERNAL_CALL_GetTransitionTypeOverride ( this, guid, out type );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_GetTransitionTypeOverride (AudioMixerSnapshotController self, GUID guid, out ParameterTransitionType type);
    public void ClearTransitionTypeOverride (GUID guid) {
        INTERNAL_CALL_ClearTransitionTypeOverride ( this, guid );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_ClearTransitionTypeOverride (AudioMixerSnapshotController self, GUID guid);
}

}
