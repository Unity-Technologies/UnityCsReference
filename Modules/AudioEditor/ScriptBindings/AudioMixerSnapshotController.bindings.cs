// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Bindings;

namespace UnityEditor.Audio
{
    internal enum ParameterTransitionType
    {
        Lerp = 0,
        Smoothstep = 1,
        Squared = 2,
        SquareRoot = 3,
        BrickwallStart = 4,
        BrickwallEnd = 5,
        Attenuation = 6
    }

    [NativeHeader("Editor/Src/Audio/Mixer/AudioMixerSnapshotController.h")]
    [NativeHeader("Modules/AudioEditor/ScriptBindings/AudioMixerSnapshotController.bindings.h")]
    internal class AudioMixerSnapshotController : AudioMixerSnapshot
    {
        public AudioMixerSnapshotController(AudioMixer owner)
        {
            Internal_CreateAudioMixerSnapshotController(this, owner);
        }

        [FreeFunction("AudioMixerSnapshotControllerBindings::Internal_CreateAudioMixerSnapshotController")]
        private static extern void Internal_CreateAudioMixerSnapshotController([Writable] AudioMixerSnapshotController mono, AudioMixer owner);

        public extern GUID snapshotID { get; }

        public extern void SetValue(GUID guid, float value);
        public extern bool GetValue(GUID guid, out float value);

        public extern void SetTransitionTypeOverride(GUID guid, ParameterTransitionType type);
        public extern bool GetTransitionTypeOverride(GUID guid, out ParameterTransitionType type);
        public extern void ClearTransitionTypeOverride(GUID guid);
    }
}
