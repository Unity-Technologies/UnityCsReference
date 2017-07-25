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
    [NativeHeader("Runtime/Animation/ScriptBindings/AnimationOffsetPlayable.bindings.h")]
    [NativeHeader("Runtime/Animation/Director/AnimationOffsetPlayable.h")]
    [NativeHeader("Runtime/Director/Core/HPlayable.h")]
    [StaticAccessor("AnimationOffsetPlayableBindings", StaticAccessorType.DoubleColon)]
    [RequiredByNativeCode]
    internal struct AnimationOffsetPlayable : IPlayable, IEquatable<AnimationOffsetPlayable>
    {
        PlayableHandle m_Handle;

        static readonly AnimationOffsetPlayable m_NullPlayable = new AnimationOffsetPlayable(PlayableHandle.Null);
        public static AnimationOffsetPlayable Null { get { return m_NullPlayable; } }

        public static AnimationOffsetPlayable Create(PlayableGraph graph, Vector3 position, Quaternion rotation, int inputCount)
        {
            var handle = CreateHandle(graph, position, rotation, inputCount);
            return new AnimationOffsetPlayable(handle);
        }

        private static PlayableHandle CreateHandle(PlayableGraph graph, Vector3 position, Quaternion rotation, int inputCount)
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!CreateHandleInternal(graph, position, rotation, ref handle))
                return PlayableHandle.Null;
            handle.SetInputCount(inputCount);
            return handle;
        }

        internal AnimationOffsetPlayable(PlayableHandle handle)
        {
            if (handle.IsValid())
            {
                if (!handle.IsPlayableOfType<AnimationOffsetPlayable>())
                    throw new InvalidCastException("Can't set handle: the playable is not an AnimationOffsetPlayable.");
            }

            m_Handle = handle;
        }

        public PlayableHandle GetHandle()
        {
            return m_Handle;
        }

        public static implicit operator Playable(AnimationOffsetPlayable playable)
        {
            return new Playable(playable.GetHandle());
        }

        public static explicit operator AnimationOffsetPlayable(Playable playable)
        {
            return new AnimationOffsetPlayable(playable.GetHandle());
        }

        public bool Equals(AnimationOffsetPlayable other)
        {
            return Equals(other.GetHandle());
        }

        public Vector3 GetPosition()
        {
            return GetPositionInternal(ref m_Handle);
        }

        public void SetPosition(Vector3 value)
        {
            SetPositionInternal(ref m_Handle, value);
        }

        public Quaternion GetRotation()
        {
            return GetRotationInternal(ref m_Handle);
        }

        public void SetRotation(Quaternion value)
        {
            SetRotationInternal(ref m_Handle, value);
        }

        // Bindings methods.
        extern private static bool CreateHandleInternal(PlayableGraph graph, Vector3 position, Quaternion rotation, ref PlayableHandle handle);
        extern static private Vector3 GetPositionInternal(ref PlayableHandle handle);
        extern static private void SetPositionInternal(ref PlayableHandle handle, Vector3 value);
        extern static private Quaternion GetRotationInternal(ref PlayableHandle handle);
        extern static private void SetRotationInternal(ref PlayableHandle handle, Quaternion value);
    }
}
