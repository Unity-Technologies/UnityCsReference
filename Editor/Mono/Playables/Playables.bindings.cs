// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Playables;
using UnityEngine.Scripting;

namespace UnityEditor.Playables
{
    [NativeHeader("Editor/Src/Playables/Playables.bindings.h")]
    static public class Utility
    {
        static public event Action<PlayableGraph> graphCreated;
        static public event Action<PlayableGraph> destroyingGraph;

        [RequiredByNativeCode]
        static private void OnPlayableGraphCreated(PlayableGraph graph)
        {
            if (graphCreated != null)
                graphCreated(graph);
        }

        [RequiredByNativeCode]
        static private void OnDestroyingPlayableGraph(PlayableGraph graph)
        {
            if (destroyingGraph != null)
                destroyingGraph(graph);
        }

        extern static public PlayableGraph[] GetAllGraphs();
    }
}
