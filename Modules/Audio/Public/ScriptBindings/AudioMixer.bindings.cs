// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using JetBrains.Annotations;
using UnityEngine.Bindings;

namespace UnityEngine.Audio
{
    public enum AudioMixerUpdateMode
    {
        Normal = 0,
        UnscaledTime = 1
    }

    /// A container for DSP graph (audio mix tree) information.
    /// An AudioMixer defines the routing graph for audio signals from [[AudioSources]] through to the listener [[AudioListener]]
    /// AudioMixers are referenced by AudioListeners and are used to apply effects, attenuation, submixing etc to the audio signal path.
    ///
    /// SA: [[class-AudioListener|AudioListener component]] in the Components Reference
    [ExcludeFromPreset]
    [ExcludeFromObjectFactory]
    [NativeHeader("Modules/Audio/Public/AudioMixer.h")]
    [NativeHeader("Modules/Audio/Public/ScriptBindings/AudioMixer.bindings.h")]
    public partial class AudioMixer : Object
    {
        internal AudioMixer() {}

        [NativeProperty]
        public extern AudioMixerGroup outputAudioMixerGroup { get; set; }

        [NativeMethod("FindSnapshotFromName")]
        public extern AudioMixerSnapshot FindSnapshot(string name);

        [NativeMethod("AudioMixerBindings::FindMatchingGroups", IsFreeFunction = true, HasExplicitThis = true)]
        public extern AudioMixerGroup[] FindMatchingGroups(string subPath);

        internal void TransitionToSnapshot(AudioMixerSnapshot snapshot, float timeToReach)
        {
            if (snapshot == null)
                throw new ArgumentException("null Snapshot passed to AudioMixer.TransitionToSnapshot of AudioMixer '" + name + "'");

            if (snapshot.audioMixer != this)
                throw new ArgumentException("Snapshot '" + snapshot.name + "' passed to AudioMixer.TransitionToSnapshot is not a snapshot from AudioMixer '" + name + "'");

            TransitionToSnapshotInternal(snapshot, timeToReach);
        }

        [NativeMethod("TransitionToSnapshot")]
        private extern void TransitionToSnapshotInternal(AudioMixerSnapshot snapshot, float timeToReach);

        [NativeMethod("AudioMixerBindings::TransitionToSnapshots", IsFreeFunction = true, HasExplicitThis = true, ThrowsException = true)]
        public extern void TransitionToSnapshots(AudioMixerSnapshot[] snapshots, float[] weights, float timeToReach);

        [NativeProperty]
        public extern AudioMixerUpdateMode updateMode { get; set; }

        [NativeMethod]
        public extern bool SetFloat(string name, float value);

        [NativeMethod]
        public extern bool ClearFloat(string name);

        [NativeMethod]
        public extern bool GetFloat(string name, out float value);

        [NativeMethod("AudioMixerBindings::GetAbsoluteAudibilityFromGroup", HasExplicitThis = true, IsFreeFunction = true)]
        internal extern float GetAbsoluteAudibilityFromGroup(AudioMixerGroup group);

        [NativeMethod]
        internal extern bool HasValidSnapshots();
    }
}
