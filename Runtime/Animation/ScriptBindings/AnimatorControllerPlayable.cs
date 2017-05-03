// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Playables;

using UnityObject = UnityEngine.Object;

namespace UnityEngine.Animations
{
    public partial struct AnimatorControllerPlayable : IPlayable, IEquatable<AnimatorControllerPlayable>
    {
        PlayableHandle m_Handle;

        static readonly AnimatorControllerPlayable m_NullPlayable = new AnimatorControllerPlayable(PlayableHandle.Null);
        public static AnimatorControllerPlayable Null { get { return m_NullPlayable; } }

        public static AnimatorControllerPlayable Create(PlayableGraph graph, RuntimeAnimatorController controller)
        {
            var handle = CreateHandle(graph, controller);
            return new AnimatorControllerPlayable(handle);
        }

        private static PlayableHandle CreateHandle(PlayableGraph graph, RuntimeAnimatorController controller)
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!CreateHandleInternal(graph, controller, ref handle))
                return PlayableHandle.Null;

            return handle;
        }

        internal AnimatorControllerPlayable(PlayableHandle handle)
        {
            m_Handle = PlayableHandle.Null;
            SetHandle(handle);
        }

        public PlayableHandle GetHandle()
        {
            return m_Handle;
        }

        public void SetHandle(PlayableHandle handle)
        {
            if (m_Handle.IsValid())
                throw new InvalidOperationException("Cannot call IPlayable.SetHandle on an instance that already contains a valid handle.");

            if (handle.IsValid())
            {
                if (!handle.IsPlayableOfType<AnimatorControllerPlayable>())
                    throw new InvalidCastException("Can't set handle: the playable is not an AnimatorControllerPlayable.");
            }

            m_Handle = handle;
        }

        public static implicit operator Playable(AnimatorControllerPlayable playable)
        {
            return new Playable(playable.GetHandle());
        }

        public static explicit operator AnimatorControllerPlayable(Playable playable)
        {
            return new AnimatorControllerPlayable(playable.GetHandle());
        }

        public bool Equals(AnimatorControllerPlayable other)
        {
            return GetHandle() == other.GetHandle();
        }
    }
}
