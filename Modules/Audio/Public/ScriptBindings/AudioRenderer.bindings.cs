// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Bindings;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

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
        unsafe internal static bool AddMixerGroupSink(AudioMixerGroup mixerGroup, NativeArray<float> buffer, bool excludeFromMix)
        {
            return Internal_AudioRenderer_AddMixerGroupSink(mixerGroup, buffer.GetUnsafePtr(), buffer.Length, excludeFromMix);
        }

        unsafe public static bool Render(NativeArray<float> buffer)
        {
            return Internal_AudioRenderer_Render(buffer.GetUnsafePtr(), buffer.Length);
        }

        internal static extern bool Internal_AudioRenderer_Start();
        internal static extern bool Internal_AudioRenderer_Stop();
        internal static extern int  Internal_AudioRenderer_GetSampleCountForCaptureFrame();
        unsafe internal static extern bool Internal_AudioRenderer_AddMixerGroupSink(AudioMixerGroup mixerGroup, void* ptr, int length, bool excludeFromMix);
        unsafe internal static extern bool Internal_AudioRenderer_Render(void* ptr, int length);
    }
}
