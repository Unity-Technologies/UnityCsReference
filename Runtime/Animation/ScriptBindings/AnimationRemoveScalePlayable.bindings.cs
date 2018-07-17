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
    [NativeHeader("Runtime/Animation/ScriptBindings/AnimationRemoveScalePlayable.bindings.h")]
    [NativeHeader("Runtime/Animation/Director/AnimationRemoveScalePlayable.h")]
    [NativeHeader("Runtime/Director/Core/HPlayable.h")]
    [StaticAccessor("AnimationRemoveScalePlayableBindings", StaticAccessorType.DoubleColon)]
    [RequiredByNativeCode]
    internal struct AnimationRemoveScalePlayable : IPlayable, IEquatable<AnimationRemoveScalePlayable>
    {
        PlayableHandle m_Handle;

        static readonly AnimationRemoveScalePlayable m_NullPlayable = new AnimationRemoveScalePlayable(PlayableHandle.Null);
        public static AnimationRemoveScalePlayable Null { get { return m_NullPlayable; } }

        public static AnimationRemoveScalePlayable Create(PlayableGraph graph, int inputCount)
        {
            var handle = CreateHandle(graph, inputCount);
            return new AnimationRemoveScalePlayable(handle);
        }

        private static PlayableHandle CreateHandle(PlayableGraph graph, int inputCount)
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!CreateHandleInternal(graph, ref handle))
                return PlayableHandle.Null;
            handle.SetInputCount(inputCount);
            return handle;
        }

        internal AnimationRemoveScalePlayable(PlayableHandle handle)
        {
            if (handle.IsValid())
            {
                if (!handle.IsPlayableOfType<AnimationRemoveScalePlayable>())
                    throw new InvalidCastException("Can't set handle: the playable is not an AnimationRemoveScalePlayable.");
            }

            m_Handle = handle;
        }

        public PlayableHandle GetHandle()
        {
            return m_Handle;
        }

        public static implicit operator Playable(AnimationRemoveScalePlayable playable)
        {
            return new Playable(playable.GetHandle());
        }

        public static explicit operator AnimationRemoveScalePlayable(Playable playable)
        {
            return new AnimationRemoveScalePlayable(playable.GetHandle());
        }

        public bool Equals(AnimationRemoveScalePlayable other)
        {
            return Equals(other.GetHandle());
        }

        [NativeThrows]
        extern private static bool CreateHandleInternal(PlayableGraph graph, ref PlayableHandle handle);
    }
}
