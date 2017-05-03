// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Playables;

using UnityObject = UnityEngine.Object;

namespace UnityEngine.Animations
{
    public partial struct AnimationLayerMixerPlayable : IPlayable, IEquatable<AnimationLayerMixerPlayable>
    {
        PlayableHandle m_Handle;

        static readonly AnimationLayerMixerPlayable m_NullPlayable = new AnimationLayerMixerPlayable(PlayableHandle.Null);
        public static AnimationLayerMixerPlayable Null { get { return m_NullPlayable; } }

        public static AnimationLayerMixerPlayable Create(PlayableGraph graph, int inputCount = 0)
        {
            var handle = CreateHandle(graph, inputCount);
            return new AnimationLayerMixerPlayable(handle);
        }

        private static PlayableHandle CreateHandle(PlayableGraph graph, int inputCount = 0)
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!CreateHandleInternal(graph, ref handle))
                return PlayableHandle.Null;
            handle.SetInputCount(inputCount);
            return handle;
        }

        internal AnimationLayerMixerPlayable(PlayableHandle handle)
        {
            if (handle.IsValid())
            {
                if (!handle.IsPlayableOfType<AnimationLayerMixerPlayable>())
                    throw new InvalidCastException("Can't set handle: the playable is not an AnimationLayerMixerPlayable.");
            }

            m_Handle = handle;
        }

        public PlayableHandle GetHandle()
        {
            return m_Handle;
        }

        public static implicit operator Playable(AnimationLayerMixerPlayable playable)
        {
            return new Playable(playable.GetHandle());
        }

        public static explicit operator AnimationLayerMixerPlayable(Playable playable)
        {
            return new AnimationLayerMixerPlayable(playable.GetHandle());
        }

        public bool Equals(AnimationLayerMixerPlayable other)
        {
            return GetHandle() == other.GetHandle();
        }
    }
}
