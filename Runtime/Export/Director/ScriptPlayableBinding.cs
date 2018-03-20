// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Playables;

namespace UnityEngine.Playables
{
    public static class ScriptPlayableBinding
    {
        public static PlayableBinding Create(string name, UnityEngine.Object key, System.Type type)
        {
            return PlayableBinding.CreateInternal(name, key, type, CreateScriptOutput);
        }

        private static PlayableOutput CreateScriptOutput(PlayableGraph graph, string name)
        {
            return (PlayableOutput)ScriptPlayableOutput.Create(graph, name);
        }
    }
}
