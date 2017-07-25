// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Playables;

namespace UnityEngine.Animations
{
    [NativeHeader("Runtime/Animation/Director/AnimationMotionXToDeltaPlayable.h")]
    [RequiredByNativeCode]
    internal struct AnimationMotionXToDeltaPlayable : IPlayable, IEquatable<AnimationMotionXToDeltaPlayable>
    {
        PlayableHandle m_Handle;

        public static AnimationMotionXToDeltaPlayable Create(PlayableGraph graph)
        {
            var handle = CreateHandle(graph);
            return new AnimationMotionXToDeltaPlayable(handle);
        }

        private static PlayableHandle CreateHandle(PlayableGraph graph)
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!CreateHandleInternal(graph, ref handle))
                return PlayableHandle.Null;

            handle.SetInputCount(1);
            return handle;
        }

        private AnimationMotionXToDeltaPlayable(PlayableHandle handle)
        {
            if (handle.IsValid())
            {
                if (!handle.IsPlayableOfType<AnimationMotionXToDeltaPlayable>())
                    throw new InvalidCastException("Can't set handle: the playable is not an AnimationMotionXToDeltaPlayable.");
            }

            m_Handle = handle;
        }

        public PlayableHandle GetHandle()
        {
            return m_Handle;
        }

        public static implicit operator Playable(AnimationMotionXToDeltaPlayable playable)
        {
            return new Playable(playable.GetHandle());
        }

        public static explicit operator AnimationMotionXToDeltaPlayable(Playable playable)
        {
            return new AnimationMotionXToDeltaPlayable(playable.GetHandle());
        }

        public bool Equals(AnimationMotionXToDeltaPlayable other)
        {
            return GetHandle() == other.GetHandle();
        }

        // Bindings methods.
        extern private static bool CreateHandleInternal(PlayableGraph graph, ref PlayableHandle handle);
    }
}
