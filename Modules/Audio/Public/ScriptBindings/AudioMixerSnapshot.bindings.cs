// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Internal;

namespace UnityEngine.Audio
{
    ///<summary>Object representing a snapshot in the mixer.</summary>
    [global::UnityEngine.NativeClass("AudioMixerSnapshot", PersistentTypeId = 272)]
    [NativeHeader("Modules/Audio/Public/AudioMixerSnapshot.h")]
    public partial class AudioMixerSnapshot : Object, ISubAssetNotDuplicatable
    {
        protected internal AudioMixerSnapshot(global::UnityEngine.EntityId id) : base(id) {}
        internal AudioMixerSnapshot() {}

        ///<exclude />
        [NativeProperty]
        public extern AudioMixer audioMixer { get; }

        ///<summary>Performs an interpolated transition towards this snapshot over the time interval specified.</summary>
        ///<param name="timeToReach">Relative time after which this snapshot should be reached from any current state.</param>
        public void TransitionTo(float timeToReach)
        {
            audioMixer.TransitionToSnapshot(this, timeToReach);
        }
    }
}
