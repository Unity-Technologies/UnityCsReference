// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Playables;

namespace UnityEngine.Experimental.Animations
{
    [NativeHeader("Runtime/Animation/ScriptBindings/AnimationScriptPlayable.bindings.h")]
    [NativeHeader("Runtime/Director/Core/HPlayableGraph.h")]
    [NativeHeader("Runtime/Director/Core/HPlayable.h")]
    [StaticAccessor("AnimationScriptPlayableBindings", StaticAccessorType.DoubleColon)]
    [RequiredByNativeCode]
    public struct AnimationScriptPlayable : IAnimationJobPlayable, IEquatable<AnimationScriptPlayable>
    {
        private PlayableHandle m_Handle;

        static readonly AnimationScriptPlayable m_NullPlayable = new AnimationScriptPlayable(PlayableHandle.Null);
        public static AnimationScriptPlayable Null { get { return m_NullPlayable; } }

        public static AnimationScriptPlayable Create<T>(PlayableGraph graph, T jobData, int inputCount = 0)
            where T : struct, IAnimationJob
        {
            var handle = CreateHandle<T>(graph, inputCount);
            var playable = new AnimationScriptPlayable(handle);
            playable.SetJobData(jobData);
            return playable;
        }

        private static PlayableHandle CreateHandle<T>(PlayableGraph graph, int inputCount)
            where T : struct, IAnimationJob
        {
            IntPtr jobReflectionData = ProcessAnimationJobStruct<T>.GetJobReflectionData();

            PlayableHandle handle = PlayableHandle.Null;
            if (!CreateHandleInternal(graph, ref handle, jobReflectionData))
                return PlayableHandle.Null;

            handle.SetInputCount(inputCount);

            return handle;
        }

        internal AnimationScriptPlayable(PlayableHandle handle)
        {
            if (handle.IsValid())
            {
                if (!handle.IsPlayableOfType<AnimationScriptPlayable>())
                    throw new InvalidCastException("Can't set handle: the playable is not an AnimationScriptPlayable.");
            }

            m_Handle = handle;
        }

        public PlayableHandle GetHandle()
        {
            return m_Handle;
        }

        private void CheckJobTypeValidity<T>()
        {
            var jobType = GetHandle().GetJobType();
            if (jobType != typeof(T))
                throw new ArgumentException(string.Format("Wrong type: the given job type ({0}) is different from the creation job type ({1}).", typeof(T).FullName, jobType.FullName));
        }

        public unsafe T GetJobData<T>()
            where T : struct, IAnimationJob
        {
            CheckJobTypeValidity<T>();

            T data;
            UnsafeUtility.CopyPtrToStructure<T>((void*)GetHandle().GetAdditionalPayload(), out data);
            return data;
        }

        public unsafe void SetJobData<T>(T jobData)
            where T : struct, IAnimationJob
        {
            CheckJobTypeValidity<T>();

            UnsafeUtility.CopyStructureToPtr(ref jobData, (void*)GetHandle().GetAdditionalPayload());
        }

        public static implicit operator Playable(AnimationScriptPlayable playable)
        {
            return new Playable(playable.GetHandle());
        }

        public static explicit operator AnimationScriptPlayable(Playable playable)
        {
            return new AnimationScriptPlayable(playable.GetHandle());
        }

        public bool Equals(AnimationScriptPlayable other)
        {
            return GetHandle() == other.GetHandle();
        }

        public void SetProcessInputs(bool value)
        {
            SetProcessInputsInternal(GetHandle(), value);
        }

        public bool GetProcessInputs()
        {
            return GetProcessInputsInternal(GetHandle());
        }

        [NativeThrows]
        extern private static bool CreateHandleInternal(PlayableGraph graph, ref PlayableHandle handle, IntPtr jobReflectionData);

        [NativeThrows]
        extern private static void SetProcessInputsInternal(PlayableHandle handle, bool value);

        [NativeThrows]
        extern private static bool GetProcessInputsInternal(PlayableHandle handle);
    }
}
