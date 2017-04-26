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
using UnityEngine.Playables.Audio;

namespace UnityEngine.Playables.Audio
{
public partial struct AudioMixerPlayable : IPlayable {}


[RequiredByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct AudioMixerPlayable
{
    PlayableHandle handle;
    PlayableHandle IPlayable.playableHandle { get { return handle; } set { handle = value; } }
    public PlayableHandle GetHandle() { return handle; }
    
    
    public bool autoNormalizeVolumes
        {
            get { return GetAutoNormalize(ref handle); }
            set { SetAutoNormalize(ref handle, value); }
        }
    
    
    private static bool GetAutoNormalize (ref PlayableHandle hdl) {
        return INTERNAL_CALL_GetAutoNormalize ( ref hdl );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_GetAutoNormalize (ref PlayableHandle hdl);
    private static void SetAutoNormalize (ref PlayableHandle hdl, bool normalise) {
        INTERNAL_CALL_SetAutoNormalize ( ref hdl, normalise );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetAutoNormalize (ref PlayableHandle hdl, bool normalise);
    [uei.ExcludeFromDocs]
public static AudioMixerPlayable CreatePlayable (PlayableGraph graph, int inputCount ) {
    bool normalizeInputVolumes = false;
    return CreatePlayable ( graph, inputCount, normalizeInputVolumes );
}

[uei.ExcludeFromDocs]
public static AudioMixerPlayable CreatePlayable (PlayableGraph graph) {
    bool normalizeInputVolumes = false;
    int inputCount = 0;
    return CreatePlayable ( graph, inputCount, normalizeInputVolumes );
}

public static AudioMixerPlayable CreatePlayable(PlayableGraph graph, [uei.DefaultValue("0")]  int inputCount , [uei.DefaultValue("false")]  bool normalizeInputVolumes )
        {
            var playable = new AudioMixerPlayable { handle = PlayableHandle.Null };
            if (!InternalCreateAudioMixerPlayable(ref graph, inputCount, normalizeInputVolumes, ref playable.handle))
                throw new System.Exception("Could not create AudioMixerPlayable");
            return playable;
        }

    
    
    private static bool InternalCreateAudioMixerPlayable (ref PlayableGraph graph, int inputCount, bool normalizeInputVolumes, ref PlayableHandle handle) {
        return INTERNAL_CALL_InternalCreateAudioMixerPlayable ( ref graph, inputCount, normalizeInputVolumes, ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_InternalCreateAudioMixerPlayable (ref PlayableGraph graph, int inputCount, bool normalizeInputVolumes, ref PlayableHandle handle);
}


}
