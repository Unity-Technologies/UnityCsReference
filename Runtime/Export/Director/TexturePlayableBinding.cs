// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Playables;

namespace UnityEngine.Experimental.Playables
{
    public static class TexturePlayableBinding
    {
        public static PlayableBinding Create(string name, UnityEngine.Object key)
        {
            return PlayableBinding.CreateInternal(name, key, typeof(RenderTexture), CreateTextureOutput);
        }

        private static PlayableOutput CreateTextureOutput(PlayableGraph graph, string name)
        {
            return (PlayableOutput)TexturePlayableOutput.Create(graph, name, null);
        }
    }
}
