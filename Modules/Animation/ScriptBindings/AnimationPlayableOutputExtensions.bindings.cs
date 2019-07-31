// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using UnityEngine.Playables;
using UnityEngine.Animations;

namespace UnityEngine.Experimental.Animations
{
    public enum AnimationStreamSource
    {
        DefaultValues,
        PreviousInputs
    }

    [NativeHeader("Modules/Animation/ScriptBindings/AnimationPlayableOutputExtensions.bindings.h")]
    [NativeHeader("Modules/Animation/AnimatorDefines.h")]
    [StaticAccessor("AnimationPlayableOutputExtensionsBindings", StaticAccessorType.DoubleColon)]
    public static class AnimationPlayableOutputExtensions
    {
        public static AnimationStreamSource GetAnimationStreamSource(this AnimationPlayableOutput output)
        {
            return InternalGetAnimationStreamSource(output.GetHandle());
        }

        public static void SetAnimationStreamSource(this AnimationPlayableOutput output, AnimationStreamSource streamSource)
        {
            InternalSetAnimationStreamSource(output.GetHandle(), streamSource);
        }

        public static ushort GetSortingOrder(this AnimationPlayableOutput output)
        {
            return (ushort)InternalGetSortingOrder(output.GetHandle());
        }

        public static void SetSortingOrder(this AnimationPlayableOutput output, ushort sortingOrder)
        {
            InternalSetSortingOrder(output.GetHandle(), (int)sortingOrder);
        }

        [NativeThrows]
        extern private static AnimationStreamSource InternalGetAnimationStreamSource(PlayableOutputHandle output);
        [NativeThrows]
        extern private static void InternalSetAnimationStreamSource(PlayableOutputHandle output, AnimationStreamSource streamSource);

        [NativeThrows]
        extern private static int InternalGetSortingOrder(PlayableOutputHandle output);
        [NativeThrows]
        extern private static void InternalSetSortingOrder(PlayableOutputHandle output, int sortingOrder);
    };
}
