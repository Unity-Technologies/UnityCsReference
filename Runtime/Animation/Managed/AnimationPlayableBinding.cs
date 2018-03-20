// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Playables;

namespace UnityEngine.Animations
{
    public static class AnimationPlayableBinding
    {
        public static PlayableBinding Create(string name, UnityEngine.Object key)
        {
            return PlayableBinding.CreateInternal(name, key, typeof(Animator), CreateAnimationOutput);
        }

        private static PlayableOutput CreateAnimationOutput(PlayableGraph graph, string name)
        {
            return (PlayableOutput)AnimationPlayableOutput.Create(graph, name, null);
        }
    }
}
