// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEngine.Playables
{
    public class AnimationPlayableUtilities
    {
        static public void Play(Animator animator, PlayableHandle playable, PlayableGraph graph)
        {
            AnimationPlayableOutput playableOutput = graph.CreateAnimationOutput("AnimationClip", animator);
            playableOutput.sourcePlayable = playable;
            playableOutput.sourceInputPort = 0;
            graph.SyncUpdateAndTimeMode(animator);
            graph.Play();
        }

        static public PlayableHandle PlayClip(Animator animator, AnimationClip clip, out PlayableGraph graph)
        {
            graph = PlayableGraph.CreateGraph();
            AnimationPlayableOutput playableOutput = graph.CreateAnimationOutput("AnimationClip", animator);
            var clipPlayable = graph.CreateAnimationClipPlayable(clip);
            playableOutput.sourcePlayable = clipPlayable;
            graph.SyncUpdateAndTimeMode(animator);
            graph.Play();

            return clipPlayable;
        }

        static public PlayableHandle PlayMixer(Animator animator, int inputCount, out PlayableGraph graph)
        {
            graph = PlayableGraph.CreateGraph();
            AnimationPlayableOutput playableOutput = graph.CreateAnimationOutput("Mixer", animator);
            var mixer = graph.CreateAnimationMixerPlayable(inputCount);
            playableOutput.sourcePlayable = mixer;
            graph.SyncUpdateAndTimeMode(animator);
            graph.Play();

            return mixer;
        }

        static public PlayableHandle PlayLayerMixer(Animator animator, int inputCount, out PlayableGraph graph)
        {
            graph = PlayableGraph.CreateGraph();
            AnimationPlayableOutput playableOutput = graph.CreateAnimationOutput("Mixer", animator);
            var mixer = graph.CreateAnimationLayerMixerPlayable(inputCount);
            playableOutput.sourcePlayable = mixer;
            graph.SyncUpdateAndTimeMode(animator);
            graph.Play();

            return mixer;
        }

        static public PlayableHandle PlayAnimatorController(Animator animator, RuntimeAnimatorController controller, out PlayableGraph graph)
        {
            graph = PlayableGraph.CreateGraph();
            AnimationPlayableOutput playableOutput = graph.CreateAnimationOutput("AnimatorControllerPlayable", animator);
            var controllerPlayable = graph.CreateAnimatorControllerPlayable(controller);
            playableOutput.sourcePlayable = controllerPlayable;
            graph.SyncUpdateAndTimeMode(animator);
            graph.Play();

            return controllerPlayable;
        }
    }
}
