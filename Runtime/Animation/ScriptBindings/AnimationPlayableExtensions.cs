// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Playables;

using UnityObject = UnityEngine.Object;

namespace UnityEngine.Animations
{
    public static partial class AnimationPlayableExtensions
    {
        public static void SetAnimatedProperties<U>(this U playable, AnimationClip clip)
            where U : struct, IPlayable
        {
            var handle = playable.GetHandle();
            SetAnimatedPropertiesInternal(ref handle, clip);
        }
    }
}
