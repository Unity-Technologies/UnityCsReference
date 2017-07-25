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
    [NativeHeader("Runtime/Animation/ScriptBindings/AnimationLayerMixerPlayable.bindings.h")]
    [NativeHeader("Runtime/Animation/Director/AnimationLayerMixerPlayable.h")]
    [NativeHeader("Runtime/Director/Core/HPlayable.h")]
    [StaticAccessor("AnimationLayerMixerPlayableBindings", StaticAccessorType.DoubleColon)]
    [RequiredByNativeCode]
    public struct AnimationLayerMixerPlayable : IPlayable, IEquatable<AnimationLayerMixerPlayable>
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

        public bool IsLayerAdditive(uint layerIndex)
        {
            if (layerIndex >= m_Handle.GetInputCount())
                throw new ArgumentOutOfRangeException("layerIndex", String.Format("layerIndex {0} must be in the range of 0 to {1}.", layerIndex, m_Handle.GetInputCount() - 1));

            return IsLayerAdditiveInternal(ref m_Handle, layerIndex);
        }

        public void SetLayerAdditive(uint layerIndex, bool value)
        {
            if (layerIndex >= m_Handle.GetInputCount())
                throw new ArgumentOutOfRangeException("layerIndex", String.Format("layerIndex {0} must be in the range of 0 to {1}.", layerIndex, m_Handle.GetInputCount() - 1));

            SetLayerAdditiveInternal(ref m_Handle, layerIndex, value);
        }

        public void SetLayerMaskFromAvatarMask(uint layerIndex, AvatarMask mask)
        {
            if (layerIndex >= m_Handle.GetInputCount())
                throw new ArgumentOutOfRangeException("layerIndex", String.Format("layerIndex {0} must be in the range of 0 to {1}.", layerIndex, m_Handle.GetInputCount() - 1));

            if (mask == null)
                throw new System.ArgumentNullException("mask");

            SetLayerMaskFromAvatarMaskInternal(ref m_Handle, layerIndex, mask);
        }

        // Bindings methods.
        extern private static bool CreateHandleInternal(PlayableGraph graph, ref PlayableHandle handle);
        extern private static bool IsLayerAdditiveInternal(ref PlayableHandle handle, uint layerIndex);
        extern private static void SetLayerAdditiveInternal(ref PlayableHandle handle, uint layerIndex, bool value);
        extern private static void SetLayerMaskFromAvatarMaskInternal(ref PlayableHandle handle, uint layerIndex, AvatarMask mask);
    }
}
