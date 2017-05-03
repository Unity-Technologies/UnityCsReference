// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Animations;

namespace UnityEngine.Playables
{
    public static class AnimationPlayableUtilities
    {
        static public void Play(Animator animator, Playable playable, PlayableGraph graph)
        {
            AnimationPlayableOutput playableOutput = AnimationPlayableOutput.Create(graph, "AnimationClip", animator);
            playableOutput.SetSourcePlayable(playable);
            playableOutput.SetSourceInputPort(0);
            graph.SyncUpdateAndTimeMode(animator);
            graph.Play();
        }

        static public AnimationClipPlayable PlayClip(Animator animator, AnimationClip clip, out PlayableGraph graph)
        {
            graph = PlayableGraph.Create();
            AnimationPlayableOutput playableOutput = AnimationPlayableOutput.Create(graph, "AnimationClip", animator);
            var clipPlayable = AnimationClipPlayable.Create(graph, clip);
            playableOutput.SetSourcePlayable(clipPlayable);
            graph.SyncUpdateAndTimeMode(animator);
            graph.Play();

            return clipPlayable;
        }

        static public AnimationMixerPlayable PlayMixer(Animator animator, int inputCount, out PlayableGraph graph)
        {
            graph = PlayableGraph.Create();
            AnimationPlayableOutput playableOutput = AnimationPlayableOutput.Create(graph, "Mixer", animator);
            var mixer = AnimationMixerPlayable.Create(graph, inputCount);
            playableOutput.SetSourcePlayable(mixer);
            graph.SyncUpdateAndTimeMode(animator);
            graph.Play();

            return mixer;
        }

        static public AnimationLayerMixerPlayable PlayLayerMixer(Animator animator, int inputCount, out PlayableGraph graph)
        {
            graph = PlayableGraph.Create();
            AnimationPlayableOutput playableOutput = AnimationPlayableOutput.Create(graph, "Mixer", animator);
            var mixer = AnimationLayerMixerPlayable.Create(graph, inputCount);
            playableOutput.SetSourcePlayable(mixer);
            graph.SyncUpdateAndTimeMode(animator);
            graph.Play();

            return mixer;
        }

        static public AnimatorControllerPlayable PlayAnimatorController(Animator animator, RuntimeAnimatorController controller, out PlayableGraph graph)
        {
            graph = PlayableGraph.Create();
            AnimationPlayableOutput playableOutput = AnimationPlayableOutput.Create(graph, "AnimatorControllerPlayable", animator);
            var controllerPlayable = AnimatorControllerPlayable.Create(graph, controller);
            playableOutput.SetSourcePlayable(controllerPlayable);
            graph.SyncUpdateAndTimeMode(animator);
            graph.Play();

            return controllerPlayable;
        }
    }
}
