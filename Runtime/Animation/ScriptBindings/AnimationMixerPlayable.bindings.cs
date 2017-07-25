// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Playables;

using UnityObject = UnityEngine.Object;

namespace UnityEngine.Animations
{
    [NativeHeader("Runtime/Animation/ScriptBindings/AnimationMixerPlayable.bindings.h")]
    [NativeHeader("Runtime/Animation/Director/AnimationMixerPlayable.h")]
    [NativeHeader("Runtime/Director/Core/HPlayable.h")]
    [StaticAccessor("AnimationMixerPlayableBindings", StaticAccessorType.DoubleColon)]
    [RequiredByNativeCode]
    public struct AnimationMixerPlayable : IPlayable, IEquatable<AnimationMixerPlayable>
    {
        PlayableHandle m_Handle;

        public static AnimationMixerPlayable Create(PlayableGraph graph, int inputCount = 0, bool normalizeWeights = false)
        {
            var handle = CreateHandle(graph, inputCount, normalizeWeights);
            return new AnimationMixerPlayable(handle);
        }

        private static PlayableHandle CreateHandle(PlayableGraph graph, int inputCount = 0, bool normalizeWeights = false)
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!CreateHandleInternal(graph, inputCount, normalizeWeights, ref handle))
                return PlayableHandle.Null;
            handle.SetInputCount(inputCount);
            return handle;
        }

        internal AnimationMixerPlayable(PlayableHandle handle)
        {
            if (handle.IsValid())
            {
                if (!handle.IsPlayableOfType<AnimationMixerPlayable>())
                    throw new InvalidCastException("Can't set handle: the playable is not an AnimationMixerPlayable.");
            }

            m_Handle = handle;
        }

        public PlayableHandle GetHandle()
        {
            return m_Handle;
        }

        public static implicit operator Playable(AnimationMixerPlayable playable)
        {
            return new Playable(playable.GetHandle());
        }

        public static explicit operator AnimationMixerPlayable(Playable playable)
        {
            return new AnimationMixerPlayable(playable.GetHandle());
        }

        public bool Equals(AnimationMixerPlayable other)
        {
            return GetHandle() == other.GetHandle();
        }

        // Bindings methods.
        extern private static bool CreateHandleInternal(PlayableGraph graph, int inputCount, bool normalizeWeights, ref PlayableHandle handle);
    }
}
