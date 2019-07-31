// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Internal;

namespace UnityEngine.Audio
{
    [NativeHeader("Modules/Audio/Public/AudioMixerGroup.h")]
    public class AudioMixerGroup : Object, ISubAssetNotDuplicatable
    {
        // Make constructor internal
        internal AudioMixerGroup() {}

        [NativeProperty]
        public extern AudioMixer audioMixer { get; }
    }
}
