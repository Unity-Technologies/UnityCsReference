// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Bindings;
using UnityEngine.Collections;

namespace UnityEngine
{
    [NativeType(Header = "Modules/Audio/Public/ScriptBindings/AudioRenderer.bindings.h")]
    public class AudioRenderer
    {
        public static bool Start()
        {
            return Internal_AudioRenderer_Start();
        }

        public static bool Stop()
        {
            return Internal_AudioRenderer_Stop();
        }

        public static int GetSampleCountForCaptureFrame()
        {
            return Internal_AudioRenderer_GetSampleCountForCaptureFrame();
        }

        // We should consider making this delegate-based in order to provide information like channel count and format. Also the term "sink" is quite audio-domain specific.
        internal static bool AddMixerGroupSink(AudioMixerGroup mixerGroup, NativeArray<float> buffer, bool excludeFromMix)
        {
            return Internal_AudioRenderer_AddMixerGroupSink(mixerGroup, buffer.UnsafePtr, buffer.Length, excludeFromMix);
        }

        public static bool Render(NativeArray<float> buffer)
        {
            return Internal_AudioRenderer_Render(buffer.UnsafePtr, buffer.Length);
        }

        internal static extern bool Internal_AudioRenderer_Start();
        internal static extern bool Internal_AudioRenderer_Stop();
        internal static extern int  Internal_AudioRenderer_GetSampleCountForCaptureFrame();
        internal static extern bool Internal_AudioRenderer_AddMixerGroupSink(AudioMixerGroup mixerGroup, IntPtr ptr, int length, bool excludeFromMix);
        internal static extern bool Internal_AudioRenderer_Render(IntPtr ptr, int length);
    }
}
