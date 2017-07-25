// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Playables;

namespace UnityEngine.Animations
{
    // Animated Properties are an extension because they rely on AnimationClip
    [NativeHeader("Runtime/Animation/Director/AnimationPlayableExtensions.h")]
    [NativeHeader("Runtime/Animation/AnimationClip.h")]
    [NativeHeader("Runtime/Director/Core/HPlayable.h")]
    public static class AnimationPlayableExtensions
    {
        public static void SetAnimatedProperties<U>(this U playable, AnimationClip clip)
            where U : struct, IPlayable
        {
            var handle = playable.GetHandle();
            SetAnimatedPropertiesInternal(ref handle, clip);
        }

        extern internal static void SetAnimatedPropertiesInternal(ref PlayableHandle playable, AnimationClip animatedProperties);
    };
}
