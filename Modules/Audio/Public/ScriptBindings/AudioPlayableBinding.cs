// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Playables;

namespace UnityEngine.Audio
{
    public static class AudioPlayableBinding
    {
        public static PlayableBinding Create(string name, UnityEngine.Object key)
        {
            return PlayableBinding.CreateInternal(name, key, typeof(AudioSource), CreateAudioOutput);
        }

        private static PlayableOutput CreateAudioOutput(PlayableGraph graph, string name)
        {
            return (PlayableOutput)AudioPlayableOutput.Create(graph, name, null);
        }
    }
}
