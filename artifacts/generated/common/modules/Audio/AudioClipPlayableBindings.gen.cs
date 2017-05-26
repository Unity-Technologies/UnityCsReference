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
public partial struct AudioClipPlayable
{
    public AudioClip GetClip()
        {
            return GetClipInternal(ref m_Handle);
        }
    
    
    public void GetClip(AudioClip value)
        {
            SetClipInternal(ref m_Handle, value);
        }
    
    
    private static AudioClip GetClipInternal (ref PlayableHandle hdl) {
        return INTERNAL_CALL_GetClipInternal ( ref hdl );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static AudioClip INTERNAL_CALL_GetClipInternal (ref PlayableHandle hdl);
    private static void SetClipInternal (ref PlayableHandle hdl, AudioClip clip) {
        INTERNAL_CALL_SetClipInternal ( ref hdl, clip );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetClipInternal (ref PlayableHandle hdl, AudioClip clip);
    public bool GetLooped()
        {
            return GetLoopedInternal(ref m_Handle);
        }
    
    
    public void SetLooped(bool value)
        {
            SetLoopedInternal(ref m_Handle, value);
        }
    
    
    private static bool GetLoopedInternal (ref PlayableHandle hdl) {
        return INTERNAL_CALL_GetLoopedInternal ( ref hdl );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_GetLoopedInternal (ref PlayableHandle hdl);
    private static void SetLoopedInternal (ref PlayableHandle hdl, bool looped) {
        INTERNAL_CALL_SetLoopedInternal ( ref hdl, looped );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetLoopedInternal (ref PlayableHandle hdl, bool looped);
    public bool IsPlaying()
        {
            return GetIsPlayingInternal(ref m_Handle);
        }
    
    
    private static bool GetIsPlayingInternal (ref PlayableHandle hdl) {
        return INTERNAL_CALL_GetIsPlayingInternal ( ref hdl );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_GetIsPlayingInternal (ref PlayableHandle hdl);
    public double GetStartDelay()
        {
            return GetStartDelayInternal(ref m_Handle);
        }
    
    
    internal void SetStartDelay(double value)
        {
            ValidateStartDelayInternal(value);
            SetStartDelayInternal(ref m_Handle, value);
        }
    
    
    private static double GetStartDelayInternal (ref PlayableHandle hdl) {
        return INTERNAL_CALL_GetStartDelayInternal ( ref hdl );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static double INTERNAL_CALL_GetStartDelayInternal (ref PlayableHandle hdl);
    private static void SetStartDelayInternal (ref PlayableHandle hdl, double delay) {
        INTERNAL_CALL_SetStartDelayInternal ( ref hdl, delay );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetStartDelayInternal (ref PlayableHandle hdl, double delay);
    public double GetPauseDelay()
        {
            return GetPauseDelayInternal(ref m_Handle);
        }
    
    
    internal void GetPauseDelay(double value)
        {
            double currentDelay = GetPauseDelayInternal(ref m_Handle);
            if (m_Handle.GetPlayState() == PlayState.Playing &&
                (value < 0.05 || (currentDelay != 0.0 && currentDelay < 0.05)))
                throw new ArgumentException("AudioClipPlayable.pauseDelay: Setting new delay when existing delay is too small or 0.0 ("
                    + currentDelay + "), audio system will not be able to change in time");

            SetPauseDelayInternal(ref m_Handle, value);
        }
    
    
    private static double GetPauseDelayInternal (ref PlayableHandle hdl) {
        return INTERNAL_CALL_GetPauseDelayInternal ( ref hdl );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static double INTERNAL_CALL_GetPauseDelayInternal (ref PlayableHandle hdl);
    private static void SetPauseDelayInternal (ref PlayableHandle hdl, double delay) {
        INTERNAL_CALL_SetPauseDelayInternal ( ref hdl, delay );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetPauseDelayInternal (ref PlayableHandle hdl, double delay);
    private static bool InternalCreateAudioClipPlayable (ref PlayableGraph graph, AudioClip clip, bool looping, ref PlayableHandle handle) {
        return INTERNAL_CALL_InternalCreateAudioClipPlayable ( ref graph, clip, looping, ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_InternalCreateAudioClipPlayable (ref PlayableGraph graph, AudioClip clip, bool looping, ref PlayableHandle handle);
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
            ValidateStartDelayInternal(startDelay);
            SetStartDelayInternal(ref m_Handle, startDelay);
            if (duration > 0)
            {
                m_Handle.SetDuration(duration + startTime);
                SetPauseDelayInternal(ref m_Handle, startDelay + duration);
            }
            else
            {
                m_Handle.SetDuration(double.MaxValue);
                SetPauseDelayInternal(ref m_Handle, 0);
            }

            m_Handle.SetTime(startTime);
            m_Handle.SetPlayState(PlayState.Playing);
        }

    
    
    private void ValidateStartDelayInternal(double startDelay)
        {
            double currentDelay = GetStartDelayInternal(ref m_Handle);

            const double validEndDelay = 0.05;
            const double validStartDelay = 0.00001; 

            if (IsPlaying() &&
                (startDelay < validEndDelay || (currentDelay >= validStartDelay && currentDelay < validEndDelay)))
            {
                Debug.LogWarning("AudioClipPlayable.StartDelay: Setting new delay when existing delay is too small or 0.0 ("
                    + currentDelay + "), audio system will not be able to change in time");
            }
        }
    
    
}


}
