// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Animations
{
    [MovedFrom("UnityEngine.Experimental.Animations")]
    public interface IAnimationWindowPreview
    {
        void StartPreview();
        void StopPreview();

        void UpdatePreviewGraph(PlayableGraph graph);
        Playable BuildPreviewGraph(PlayableGraph graph, Playable inputPlayable);
    }
}
