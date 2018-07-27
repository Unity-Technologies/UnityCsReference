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
    [NativeHeader("Runtime/Animation/ScriptBindings/AnimationPosePlayable.bindings.h")]
    [NativeHeader("Runtime/Animation/Director/AnimationPosePlayable.h")]
    [NativeHeader("Runtime/Director/Core/HPlayable.h")]
    [StaticAccessor("AnimationPosePlayableBindings", StaticAccessorType.DoubleColon)]
    [RequiredByNativeCode]
    internal struct AnimationPosePlayable : IPlayable, IEquatable<AnimationPosePlayable>
    {
        PlayableHandle m_Handle;

        static readonly AnimationPosePlayable m_NullPlayable = new AnimationPosePlayable(PlayableHandle.Null);
        public static AnimationPosePlayable Null { get { return m_NullPlayable; } }

        public static AnimationPosePlayable Create(PlayableGraph graph)
        {
            var handle = CreateHandle(graph);
            return new AnimationPosePlayable(handle);
        }

        private static PlayableHandle CreateHandle(PlayableGraph graph)
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!CreateHandleInternal(graph, ref handle))
                return PlayableHandle.Null;

            return handle;
        }

        internal AnimationPosePlayable(PlayableHandle handle)
        {
            if (handle.IsValid())
            {
                if (!handle.IsPlayableOfType<AnimationPosePlayable>())
                    throw new InvalidCastException("Can't set handle: the playable is not an AnimationPosePlayable.");
            }

            m_Handle = handle;
        }

        public PlayableHandle GetHandle()
        {
            return m_Handle;
        }

        public static implicit operator Playable(AnimationPosePlayable playable)
        {
            return new Playable(playable.GetHandle());
        }

        public static explicit operator AnimationPosePlayable(Playable playable)
        {
            return new AnimationPosePlayable(playable.GetHandle());
        }

        public bool Equals(AnimationPosePlayable other)
        {
            return Equals(other.GetHandle());
        }

        public bool GetMustReadPreviousPose()
        {
            return GetMustReadPreviousPoseInternal(ref m_Handle);
        }

        public void SetMustReadPreviousPose(bool value)
        {
            SetMustReadPreviousPoseInternal(ref m_Handle, value);
        }

        public bool GetReadDefaultPose()
        {
            return GetReadDefaultPoseInternal(ref m_Handle);
        }

        public void SetReadDefaultPose(bool value)
        {
            SetReadDefaultPoseInternal(ref m_Handle, value);
        }

        public bool GetApplyFootIK()
        {
            return GetApplyFootIKInternal(ref m_Handle);
        }

        public void SetApplyFootIK(bool value)
        {
            SetApplyFootIKInternal(ref m_Handle, value);
        }

        // Bindings methods.
        [NativeThrows]
        extern private static bool CreateHandleInternal(PlayableGraph graph, ref PlayableHandle handle);

        [NativeThrows]
        extern static private bool GetMustReadPreviousPoseInternal(ref PlayableHandle handle);
        [NativeThrows]
        extern static private void SetMustReadPreviousPoseInternal(ref PlayableHandle handle, bool value);
        [NativeThrows]
        extern static private bool GetReadDefaultPoseInternal(ref PlayableHandle handle);
        [NativeThrows]
        extern static private void SetReadDefaultPoseInternal(ref PlayableHandle handle, bool value);
        [NativeThrows]
        extern static private bool GetApplyFootIKInternal(ref PlayableHandle handle);
        [NativeThrows]
        extern static private void SetApplyFootIKInternal(ref PlayableHandle handle, bool value);
    }
}
