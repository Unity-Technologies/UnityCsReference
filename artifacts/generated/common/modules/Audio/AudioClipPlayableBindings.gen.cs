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
public partial struct AudioClipPlayable : IPlayable {}


[RequiredByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct AudioClipPlayable
{
    PlayableHandle handle;
    PlayableHandle IPlayable.playableHandle { get { return handle; } set { handle = value; } }
    public PlayableHandle GetHandle() { return handle; }
    
    
    public AudioClip clip
        {
            get { return GetClip(ref handle); }
            set { SetClip(ref handle, value); }
        }
    
    
    private static AudioClip GetClip (ref PlayableHandle hdl) {
        return INTERNAL_CALL_GetClip ( ref hdl );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static AudioClip INTERNAL_CALL_GetClip (ref PlayableHandle hdl);
    private static void SetClip (ref PlayableHandle hdl, AudioClip clip) {
        INTERNAL_CALL_SetClip ( ref hdl, clip );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetClip (ref PlayableHandle hdl, AudioClip clip);
    public bool looped
        {
            get { return GetLooped(ref handle); }
            set { SetLooped(ref handle, value); }
        }
    
    
    private static bool GetLooped (ref PlayableHandle hdl) {
        return INTERNAL_CALL_GetLooped ( ref hdl );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_GetLooped (ref PlayableHandle hdl);
    private static void SetLooped (ref PlayableHandle hdl, bool looped) {
        INTERNAL_CALL_SetLooped ( ref hdl, looped );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetLooped (ref PlayableHandle hdl, bool looped);
    public bool isPlaying
        {
            get { return GetIsPlaying(ref handle); }
        }
    
    
    private static bool GetIsPlaying (ref PlayableHandle hdl) {
        return INTERNAL_CALL_GetIsPlaying ( ref hdl );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_GetIsPlaying (ref PlayableHandle hdl);
    public double startDelay
        {
            get { return GetStartDelay(ref handle); }
            internal set
            {
                ValidateStartDelay(value);
                SetStartDelay(ref handle, value);
            }
        }
    
    
    private static double GetStartDelay (ref PlayableHandle hdl) {
        return INTERNAL_CALL_GetStartDelay ( ref hdl );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static double INTERNAL_CALL_GetStartDelay (ref PlayableHandle hdl);
    private static void SetStartDelay (ref PlayableHandle hdl, double delay) {
        INTERNAL_CALL_SetStartDelay ( ref hdl, delay );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetStartDelay (ref PlayableHandle hdl, double delay);
    public double pauseDelay
        {
            get { return GetPauseDelay(ref handle); }
            internal set
            {
                double currentDelay = GetPauseDelay(ref handle);
                if (handle.playState == PlayState.Playing &&
                    (value < 0.05 || (currentDelay != 0.0 && currentDelay < 0.05)))
                    throw new ArgumentException("AudioClipPlayable.pauseDelay: Setting new delay when existing delay is too small or 0.0 ("
                        + currentDelay + "), audio system will not be able to change in time");

                SetPauseDelay(ref handle, value);
            }
        }
    
    
    private static double GetPauseDelay (ref PlayableHandle hdl) {
        return INTERNAL_CALL_GetPauseDelay ( ref hdl );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static double INTERNAL_CALL_GetPauseDelay (ref PlayableHandle hdl);
    private static void SetPauseDelay (ref PlayableHandle hdl, double delay) {
        INTERNAL_CALL_SetPauseDelay ( ref hdl, delay );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetPauseDelay (ref PlayableHandle hdl, double delay);
    public static AudioClipPlayable CreatePlayable(PlayableGraph graph, AudioClip clip, bool looping)
        {
            var playable = new AudioClipPlayable { handle = PlayableHandle.Null };
            if (!InternalCreateAudioClipPlayable(ref graph, clip, looping, ref playable.handle))
                throw new System.Exception("Could not create AudioClipPlayable");
            if (clip != null)
                playable.handle.duration = clip.length;
            return playable;
        }
    
    
    private static bool InternalCreateAudioClipPlayable (ref PlayableGraph graph, AudioClip clip, bool looping, ref PlayableHandle handle) {
        return INTERNAL_CALL_InternalCreateAudioClipPlayable ( ref graph, clip, looping, ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_InternalCreateAudioClipPlayable (ref PlayableGraph graph, AudioClip clip, bool looping, ref PlayableHandle handle);
    public static AudioClipPlayable CastFrom(PlayableHandle handle)
        {
            if (!ValidateType(ref handle))
                throw new InvalidOperationException("CastFrom: Handle is not a valid AudioClipPlayable");

            AudioClipPlayable ret = new AudioClipPlayable();
            ret.handle = handle;
            return ret;
        }
    
    
    private static bool ValidateType (ref PlayableHandle hdl) {
        return INTERNAL_CALL_ValidateType ( ref hdl );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_ValidateType (ref PlayableHandle hdl);
    [uei.ExcludeFromDocs]
public void Seek (double startTime, double startDelay) {
    double duration = 0;
    Seek ( startTime, startDelay, duration );
}

public void Seek(double startTime, double startDelay, [uei.DefaultValue("0")]  double duration )
        {
            ValidateStartDelay(startDelay);
            SetStartDelay(ref handle, startDelay);
            if (duration > 0)
            {
                handle.duration = duration + startTime;
                SetPauseDelay(ref handle, startDelay + duration);
            }
            else
            {
                handle.duration = double.MaxValue;
                SetPauseDelay(ref handle, 0);
            }

            handle.time = startTime;
            handle.playState = PlayState.Playing;
        }

    
    
    private void ValidateStartDelay(double startDelay)
        {
            double currentDelay = GetStartDelay(ref handle);

            const double validEndDelay = 0.05;
            const double validStartDelay = 0.00001; 

            if (isPlaying &&
                (startDelay < validEndDelay || (currentDelay >= validStartDelay && currentDelay < validEndDelay)))
            {
                Debug.LogWarning("AudioClipPlayable.StartDelay: Setting new delay when existing delay is too small or 0.0 ("
                    + currentDelay + "), audio system will not be able to change in time");
            }
        }
    
    
}


}
