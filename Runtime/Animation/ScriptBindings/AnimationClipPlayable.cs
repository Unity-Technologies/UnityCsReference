// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Playables;

using UnityObject = UnityEngine.Object;

namespace UnityEngine.Animations
{
    public partial struct AnimationClipPlayable : IPlayable, IEquatable<AnimationClipPlayable>
    {
        PlayableHandle m_Handle;

        public static AnimationClipPlayable Create(PlayableGraph graph, AnimationClip clip)
        {
            var handle = CreateHandle(graph, clip);
            return new AnimationClipPlayable(handle);
        }

        private static PlayableHandle CreateHandle(PlayableGraph graph, AnimationClip clip)
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!CreateHandleInternal(graph, clip, ref handle))
                return PlayableHandle.Null;

            return handle;
        }

        internal AnimationClipPlayable(PlayableHandle handle)
        {
            if (handle.IsValid())
            {
                if (!handle.IsPlayableOfType<AnimationClipPlayable>())
                    throw new InvalidCastException("Can't set handle: the playable is not an AnimationClipPlayable.");
            }

            m_Handle = handle;
        }

        public PlayableHandle GetHandle()
        {
            return m_Handle;
        }

        public static implicit operator Playable(AnimationClipPlayable playable)
        {
            return new Playable(playable.GetHandle());
        }

        public static explicit operator AnimationClipPlayable(Playable playable)
        {
            return new AnimationClipPlayable(playable.GetHandle());
        }

        public bool Equals(AnimationClipPlayable other)
        {
            return GetHandle() == other.GetHandle();
        }
    }
}
