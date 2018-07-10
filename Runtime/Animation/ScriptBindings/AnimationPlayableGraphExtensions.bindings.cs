// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Playables;

namespace UnityEngine.Animations
{
    [NativeHeader("Runtime/Animation/ScriptBindings/AnimationPlayableGraphExtensions.bindings.h")]
    [NativeHeader("Runtime/Animation/Animator.h")]
    [NativeHeader("Runtime/Director/Core/HPlayableOutput.h")]
    [NativeHeader("Runtime/Director/Core/HPlayable.h")]
    [StaticAccessor("AnimationPlayableGraphExtensionsBindings", StaticAccessorType.DoubleColon)]
    internal static class AnimationPlayableGraphExtensions
    {
        static internal void SyncUpdateAndTimeMode(this PlayableGraph graph, Animator animator)
        {
            InternalSyncUpdateAndTimeMode(ref graph, animator);
        }

        static internal void DestroyOutput(this PlayableGraph graph, PlayableOutputHandle handle)
        {
            InternalDestroyOutput(ref graph, ref handle);
        }

        [NativeThrows]
        extern internal static bool InternalCreateAnimationOutput(ref PlayableGraph graph, string name, out PlayableOutputHandle handle);

        [NativeThrows]
        extern internal static void InternalSyncUpdateAndTimeMode(ref PlayableGraph graph, [NotNull] Animator animator);

        [NativeThrows]
        extern private static void InternalDestroyOutput(ref PlayableGraph graph, ref PlayableOutputHandle handle);

        [NativeThrows]
        extern private static int InternalAnimationOutputCount(ref PlayableGraph graph);

        [NativeThrows]
        extern private static bool InternalGetAnimationOutput(ref PlayableGraph graph, int index, out PlayableOutputHandle handle);
    }
}
