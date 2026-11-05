// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Playables;

namespace UnityEngine.Animations
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct AnimationClipControllerStateSettings
    {
        public float cycleOffset;
        public float speed;
        public bool mirror;
        public bool writeDefaultValues;
    }

    ///<summary>A <see cref="T:UnityEngine.Playables.Playable" /> that controls an <see cref="AnimationClip" />.</summary>
    ///<remarks>NOTE: You can use <see cref="PlayableExtensions" /> methods with AnimationClipPlayable objects.</remarks>
    [NativeHeader("Modules/Animation/ScriptBindings/AnimationClipPlayable.bindings.h")]
    [NativeHeader("Modules/Animation/Director/AnimationClipPlayable.h")]
    [StaticAccessor("AnimationClipPlayableBindings", StaticAccessorType.DoubleColon)]
    [RequiredByNativeCode]
    public struct AnimationClipPlayable : IPlayable, IEquatable<AnimationClipPlayable>
    {
        PlayableHandle m_Handle;

        ///<summary>Creates an <see cref="AnimationClipPlayable" /> in the <see cref="T:UnityEngine.Playables.PlayableGraph" />.</summary>
        ///<param name="graph">The PlayableGraph object that will own the AnimationClipPlayable.</param>
        ///<param name="clip">The AnimationClip that will be added in the PlayableGraph.</param>
        ///<returns>A <see cref="AnimationClipPlayable" /> linked to the <see cref="T:UnityEngine.Playables.PlayableGraph" />.</returns>
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

        ///<exclude />
        public PlayableHandle GetHandle()
        {
            return m_Handle;
        }

        ///<exclude />
        public static implicit operator Playable(AnimationClipPlayable playable)
        {
            return new Playable(playable.GetHandle());
        }

        ///<exclude />
        public static explicit operator AnimationClipPlayable(Playable playable)
        {
            return new AnimationClipPlayable(playable.GetHandle());
        }

        ///<exclude />
        public bool Equals(AnimationClipPlayable other)
        {
            return GetHandle() == other.GetHandle();
        }

        ///<summary>Returns the <see cref="AnimationClip" /> stored in the <see cref="AnimationClipPlayable" />.</summary>
        public AnimationClip GetAnimationClip()
        {
            return GetAnimationClipInternal(ref m_Handle);
        }

        ///<summary>Returns the state of the ApplyFootIK flag.</summary>
        public bool GetApplyFootIK()
        {
            return GetApplyFootIKInternal(ref m_Handle);
        }

        ///<summary>Sets the value of the ApplyFootIK flag.</summary>
        ///<param name="value">The new value of the ApplyFootIK flag.</param>
        public void SetApplyFootIK(bool value)
        {
            SetApplyFootIKInternal(ref m_Handle, value);
        }

        ///<summary>Returns the state of the ApplyPlayableIK flag.</summary>
        public bool GetApplyPlayableIK()
        {
            return GetApplyPlayableIKInternal(ref m_Handle);
        }

        ///<summary>Requests <see cref="M:UnityEngine.MonoBehaviour.OnAnimatorIK" /> to be called on the animated GameObject.</summary>
        ///<remarks>When OnAnimatorIK is called the layer index parameter will always be zero.</remarks>
        public void SetApplyPlayableIK(bool value)
        {
            SetApplyPlayableIKInternal(ref m_Handle, value);
        }

        internal bool GetRemoveStartOffset()
        {
            return GetRemoveStartOffsetInternal(ref m_Handle);
        }

        internal void SetRemoveStartOffset(bool value)
        {
            SetRemoveStartOffsetInternal(ref m_Handle, value);
        }

        internal bool GetOverrideLoopTime()
        {
            return GetOverrideLoopTimeInternal(ref m_Handle);
        }

        internal void SetOverrideLoopTime(bool value)
        {
            SetOverrideLoopTimeInternal(ref m_Handle, value);
        }

        internal bool GetLoopTime()
        {
            return GetLoopTimeInternal(ref m_Handle);
        }

        internal void SetLoopTime(bool value)
        {
            SetLoopTimeInternal(ref m_Handle, value);
        }

        internal float GetSampleRate()
        {
            return GetSampleRateInternal(ref m_Handle);
        }

        internal void SetSampleRate(float value)
        {
            SetSampleRateInternal(ref m_Handle, value);
        }

        internal AnimationClipControllerStateSettings GetControllerStateSettings()
        {
            return GetControllerStateSettingsInternal(ref m_Handle);
        }

        internal void SetControllerStateSettings(AnimationClipControllerStateSettings value)
        {
            SetControllerStateSettingsInternal(ref m_Handle, value);
        }

        [NativeMethod(ThrowsException = true)]
        extern private static bool CreateHandleInternal(PlayableGraph graph, AnimationClip clip, ref PlayableHandle handle);

        [NativeMethod(ThrowsException = true)]
        extern private static AnimationClip GetAnimationClipInternal(ref PlayableHandle handle);

        [NativeMethod(ThrowsException = true)]
        extern private static bool GetApplyFootIKInternal(ref PlayableHandle handle);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetApplyFootIKInternal(ref PlayableHandle handle, bool value);

        [NativeMethod(ThrowsException = true)]
        extern private static bool GetApplyPlayableIKInternal(ref PlayableHandle handle);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetApplyPlayableIKInternal(ref PlayableHandle handle, bool value);

        [NativeMethod(ThrowsException = true)]
        extern private static bool GetRemoveStartOffsetInternal(ref PlayableHandle handle);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetRemoveStartOffsetInternal(ref PlayableHandle handle, bool value);

        [NativeMethod(ThrowsException = true)]
        extern private static bool GetOverrideLoopTimeInternal(ref PlayableHandle handle);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetOverrideLoopTimeInternal(ref PlayableHandle handle, bool value);

        [NativeMethod(ThrowsException = true)]
        extern private static bool GetLoopTimeInternal(ref PlayableHandle handle);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetLoopTimeInternal(ref PlayableHandle handle, bool value);

        [NativeMethod(ThrowsException = true)]
        extern private static float GetSampleRateInternal(ref PlayableHandle handle);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetSampleRateInternal(ref PlayableHandle handle, float value);

        [NativeMethod(ThrowsException = true)]
        extern private static AnimationClipControllerStateSettings GetControllerStateSettingsInternal(ref PlayableHandle handle);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetControllerStateSettingsInternal(ref PlayableHandle handle, AnimationClipControllerStateSettings value);
    }
}
