// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using uei = UnityEngine.Internal;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine
{
    // Used by Animation.Play function.
    public enum PlayMode
    {
        // Will stop all animations that were started in the same layer. This is the default when playing animations.
        StopSameLayer = 0,
        // Will stop all animations that were started with this component before playing
        StopAll = 4,
    }

    // Used by Animation.Play function.
    public enum QueueMode
    {
        // Will start playing after all other animations have stopped playing
        CompleteOthers = 0,
        // Starts playing immediately. This can be used if you just want to quickly create a duplicate animation.
        PlayNow = 2
    }

    // Used by Animation.Play function.
    public enum AnimationBlendMode
    {
        // Animations will be blended
        Blend = 0,
        // Animations will be added
        Additive = 1
    }

    // considered deprecated
    public enum AnimationPlayMode { Stop = 0, Queue = 1, Mix = 2 }

    // This enum controlls culling of Animation component.
    public enum AnimationCullingType
    {
        // Animation culling is disabled - object is animated even when offscreen.
        AlwaysAnimate = 0,
        // Animation is disabled when renderers are not visible.
        BasedOnRenderers = 1,

        // Animation is disabled when localBounds are not visible.
        [System.Obsolete("Enum member AnimatorCullingMode.BasedOnClipBounds has been deprecated. Use AnimationCullingType.AlwaysAnimate or AnimationCullingType.BasedOnRenderers instead")]
        BasedOnClipBounds = 2,
        // Animation is disabled when localBounds are not visible.
        [System.Obsolete("Enum member AnimatorCullingMode.BasedOnUserBounds has been deprecated. Use AnimationCullingType.AlwaysAnimate or AnimationCullingType.BasedOnRenderers instead")]
        BasedOnUserBounds = 3
    }

    public enum AnimationUpdateMode
    {
        Normal = 0,
        Fixed = 1
    }

    internal enum AnimationEventSource
    {
        NoSource = 0,
        Legacy = 1,
        Animator = 2,
    }

    // The animation component is used to play back animations.
    [NativeHeader("Modules/Animation/Animation.h")]
    public sealed class Animation : Behaviour, IEnumerable
    {
        public extern AnimationClip clip { get; set; }
        public extern bool playAutomatically { get; set; }
        public extern WrapMode wrapMode { get; set; }

        public extern void Stop();
        public void Stop(string name) { StopNamed(name); }
        [NativeName("Stop")] private extern void StopNamed(string name);
        public extern void Rewind();
        public void Rewind(string name) { RewindNamed(name); }
        [NativeName("Rewind")] private extern void RewindNamed(string name);

        public extern void Sample();
        public extern bool isPlaying { [NativeName("IsPlaying")] get; }
        public extern bool IsPlaying(string name);

        public AnimationState this[string name] { get { return GetState(name); } }

        [uei.ExcludeFromDocs] public bool Play() { return Play(PlayMode.StopSameLayer); }
        public bool Play([uei.DefaultValue("PlayMode.StopSameLayer")] PlayMode mode) { return PlayDefaultAnimation(mode); }
        [NativeName("Play")] extern private bool PlayDefaultAnimation(PlayMode mode);

        [uei.ExcludeFromDocs] public bool Play(string animation) { return Play(animation, PlayMode.StopSameLayer); }
        extern public bool Play(string animation, [uei.DefaultValue("PlayMode.StopSameLayer")] PlayMode mode);

        [uei.ExcludeFromDocs] public void CrossFade(string animation) { CrossFade(animation, 0.3f); }
        [uei.ExcludeFromDocs] public void CrossFade(string animation, float fadeLength) { CrossFade(animation, fadeLength, PlayMode.StopSameLayer); }
        extern public void CrossFade(string animation, [uei.DefaultValue("0.3F")] float fadeLength, [uei.DefaultValue("PlayMode.StopSameLayer")] PlayMode mode);

        [uei.ExcludeFromDocs] public void Blend(string animation) { Blend(animation, 1.0f); }
        [uei.ExcludeFromDocs] public void Blend(string animation, float targetWeight) { Blend(animation, targetWeight, 0.3f); }
        extern public void Blend(string animation, [uei.DefaultValue("1.0F")] float targetWeight, [uei.DefaultValue("0.3F")] float fadeLength);

        [uei.ExcludeFromDocs] public AnimationState CrossFadeQueued(string animation) { return CrossFadeQueued(animation, 0.3F); }
        [uei.ExcludeFromDocs] public AnimationState CrossFadeQueued(string animation, float fadeLength) { return CrossFadeQueued(animation, fadeLength, QueueMode.CompleteOthers); }
        [uei.ExcludeFromDocs] public AnimationState CrossFadeQueued(string animation, float fadeLength, QueueMode queue) { return CrossFadeQueued(animation, fadeLength, queue, PlayMode.StopSameLayer); }
        [FreeFunction("AnimationBindings::CrossFadeQueuedImpl", HasExplicitThis = true)]
        extern public AnimationState CrossFadeQueued(string animation, [uei.DefaultValue("0.3F")] float fadeLength, [uei.DefaultValue("QueueMode.CompleteOthers")] QueueMode queue, [uei.DefaultValue("PlayMode.StopSameLayer")] PlayMode mode);

        [uei.ExcludeFromDocs] public AnimationState PlayQueued(string animation) { return PlayQueued(animation, QueueMode.CompleteOthers); }
        [uei.ExcludeFromDocs] public AnimationState PlayQueued(string animation, QueueMode queue) { return PlayQueued(animation, queue, PlayMode.StopSameLayer); }
        [FreeFunction("AnimationBindings::PlayQueuedImpl", HasExplicitThis = true)]
        extern public AnimationState PlayQueued(string animation, [uei.DefaultValue("QueueMode.CompleteOthers")] QueueMode queue, [uei.DefaultValue("PlayMode.StopSameLayer")] PlayMode mode);

        public void AddClip(AnimationClip clip, string newName) { AddClip(clip, newName, Int32.MinValue, Int32.MaxValue); }
        [uei.ExcludeFromDocs] public void AddClip(AnimationClip clip, string newName, int firstFrame, int lastFrame) { AddClip(clip, newName, firstFrame, lastFrame, false); }
        extern public void AddClip([NotNull("NullExceptionObject")] AnimationClip clip, string newName, int firstFrame, int lastFrame, [uei.DefaultValue("false")] bool addLoopFrame);

        extern public void RemoveClip([NotNull("NullExceptionObject")] AnimationClip clip);

        public void RemoveClip(string clipName) { RemoveClipNamed(clipName); }
        [NativeName("RemoveClip")] extern private void RemoveClipNamed(string clipName);

        extern public int GetClipCount();

        [System.Obsolete("use PlayMode instead of AnimationPlayMode.")]
        public bool Play(AnimationPlayMode mode) { return PlayDefaultAnimation((PlayMode)mode); }
        [System.Obsolete("use PlayMode instead of AnimationPlayMode.")]
        public bool Play(string animation, AnimationPlayMode mode) { return Play(animation, (PlayMode)mode); }

        extern public void SyncLayer(int layer);

        public IEnumerator GetEnumerator() { return new Animation.Enumerator(this); }

        private sealed partial class Enumerator : IEnumerator
        {
            Animation m_Outer;
            int m_CurrentIndex = -1;

            internal Enumerator(Animation outer) { m_Outer = outer; }
            public object Current
            {
                get { return m_Outer.GetStateAtIndex(m_CurrentIndex); }
            }
            public bool MoveNext()
            {
                int childCount = m_Outer.GetStateCount();
                m_CurrentIndex++;
                return m_CurrentIndex < childCount;
            }

            public void Reset() { m_CurrentIndex = -1; }
        }

        [FreeFunction("AnimationBindings::GetState", HasExplicitThis = true)]
        extern internal AnimationState GetState(string name);

        [FreeFunction("AnimationBindings::GetStateAtIndex", HasExplicitThis = true, ThrowsException = true)]
        extern internal AnimationState GetStateAtIndex(int index);

        [NativeName("GetAnimationStateCount")] extern internal int GetStateCount();

        public AnimationClip GetClip(string name)
        {
            AnimationState state = GetState(name);
            if (state)
                return state.clip;
            else
                return null;
        }

        extern public bool animatePhysics { get; set; }

        extern public AnimationUpdateMode updateMode { get; set; }

        [System.Obsolete("Use cullingType instead")]
        public extern bool animateOnlyIfVisible
        {
            [FreeFunction("AnimationBindings::GetAnimateOnlyIfVisible", HasExplicitThis = true)]
            get;
            [FreeFunction("AnimationBindings::SetAnimateOnlyIfVisible", HasExplicitThis = true)]
            set;
        }

        extern public AnimationCullingType cullingType { get; set; }
        extern public Bounds localBounds { [NativeName("GetLocalAABB")] get; [NativeName("SetLocalAABB")] set; }
    }

    [NativeHeader("Modules/Animation/AnimationState.h")]
    [UsedByNativeCode]
    public sealed class AnimationState : TrackedReference
    {
        extern public bool enabled { get; set; }
        extern public float weight { get; set; }
        extern public WrapMode wrapMode { get; set; }
        extern public float time { get; set; }
        extern public float normalizedTime { get; set; }
        extern public float speed { get; set; }
        extern public float normalizedSpeed { get; set; }
        extern public float length { get; }
        extern public int layer { get; set; }
        extern public AnimationClip clip { get; }
        extern public string name { get; set; }
        extern public AnimationBlendMode blendMode { get; set; }

        [uei.ExcludeFromDocs] public void AddMixingTransform(Transform mix) { AddMixingTransform(mix, true); }
        extern public void AddMixingTransform([NotNull("NullExceptionObject")] Transform mix, [uei.DefaultValue("true")] bool recursive);

        extern public void RemoveMixingTransform([NotNull("NullExceptionObject")] Transform mix);
    }

    [System.Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    internal struct AnimationEventBlittable : IDisposable
    {
        internal float m_Time;
        internal IntPtr m_FunctionName;
        internal IntPtr m_StringParameter;
        internal IntPtr m_ObjectReferenceParameter;
        internal float m_FloatParameter;
        internal int m_IntParameter;

        internal int m_MessageOptions;
        internal AnimationEventSource m_Source;
        internal IntPtr m_StateSender;
        internal AnimatorStateInfo m_AnimatorStateInfo;
        internal AnimatorClipInfo m_AnimatorClipInfo;

        internal static AnimationEventBlittable FromAnimationEvent(AnimationEvent animationEvent)
        {
            if (s_handlePool == null)
                s_handlePool = new GCHandlePool();
            var handlePool = s_handlePool;
            var animationEventBlittable = new AnimationEventBlittable
            {
                m_Time = animationEvent.m_Time,
                m_FunctionName = handlePool.AllocHandleIfNotNull(animationEvent.m_FunctionName),
                m_StringParameter = handlePool.AllocHandleIfNotNull(animationEvent.m_StringParameter),
                m_ObjectReferenceParameter = handlePool.AllocHandleIfNotNull(animationEvent.m_ObjectReferenceParameter),
                m_FloatParameter = animationEvent.m_FloatParameter,
                m_IntParameter = animationEvent.m_IntParameter,
                m_MessageOptions = animationEvent.m_MessageOptions,
                m_Source = animationEvent.m_Source,
                m_StateSender = handlePool.AllocHandleIfNotNull(animationEvent.m_StateSender),
                m_AnimatorStateInfo = animationEvent.m_AnimatorStateInfo,
                m_AnimatorClipInfo = animationEvent.m_AnimatorClipInfo
            };

            return animationEventBlittable;
        }

        internal unsafe static void FromAnimationEvents(AnimationEvent[] animationEvents, AnimationEventBlittable* animationEventBlittables)
        {
            if (s_handlePool == null)
                s_handlePool = new GCHandlePool();
            var handlePool = s_handlePool;

            var animationEventBlittable = animationEventBlittables;
            for (var i = 0; i < animationEvents.Length; ++i)
            {
                var animationEvent = animationEvents[i];
                animationEventBlittable->m_Time = animationEvent.m_Time;
                animationEventBlittable->m_FunctionName = handlePool.AllocHandleIfNotNull(animationEvent.m_FunctionName);
                animationEventBlittable->m_StringParameter = handlePool.AllocHandleIfNotNull(animationEvent.m_StringParameter);
                animationEventBlittable->m_ObjectReferenceParameter = handlePool.AllocHandleIfNotNull(animationEvent.m_ObjectReferenceParameter);
                animationEventBlittable->m_FloatParameter = animationEvent.m_FloatParameter;
                animationEventBlittable->m_IntParameter = animationEvent.m_IntParameter;
                animationEventBlittable->m_MessageOptions = animationEvent.m_MessageOptions;
                animationEventBlittable->m_Source = animationEvent.m_Source;
                animationEventBlittable->m_StateSender = handlePool.AllocHandleIfNotNull(animationEvent.m_StateSender);
                animationEventBlittable->m_AnimatorStateInfo = animationEvent.m_AnimatorStateInfo;
                animationEventBlittable->m_AnimatorClipInfo = animationEvent.m_AnimatorClipInfo;

                animationEventBlittable++;
            }
        }

        [RequiredByNativeCode]
        internal unsafe static AnimationEvent PointerToAnimationEvent(IntPtr animationEventBlittable)
        {
            return ToAnimationEvent(*(AnimationEventBlittable*)animationEventBlittable);
        }

        internal unsafe static AnimationEvent[] PointerToAnimationEvents(IntPtr animationEventBlittableArray, int size)
        {
            var animationEvents = new AnimationEvent[size];
            var animationEventsBlittable = (AnimationEventBlittable*)animationEventBlittableArray;
            for (int i = 0; i < size; i++)
            {
                animationEvents[i] = PointerToAnimationEvent((IntPtr)(animationEventsBlittable + i));
            }

            return animationEvents;
        }

        internal unsafe static void DisposeEvents(IntPtr animationEventBlittableArray, int size)
        {
            var animationEventsBlittable = (AnimationEventBlittable*)animationEventBlittableArray;
            for (int i = 0; i < size; i++)
            {
                animationEventsBlittable[i].Dispose();
            }

            FreeEventsInternal(animationEventBlittableArray);
        }

        [FreeFunction(Name = "AnimationClipBindings::FreeEventsInternal")]
        extern static private void FreeEventsInternal(IntPtr value);

        [ThreadStatic]
        static GCHandlePool s_handlePool;

        internal static AnimationEvent ToAnimationEvent(AnimationEventBlittable animationEventBlittable)
        {
            var animationEvent = new AnimationEvent();
            animationEvent.m_Time = animationEventBlittable.m_Time;
            if (animationEventBlittable.m_FunctionName != IntPtr.Zero)
                animationEvent.m_FunctionName = (string)UnsafeUtility.As<IntPtr, GCHandle>(ref animationEventBlittable.m_FunctionName).Target;
            if (animationEventBlittable.m_StringParameter != IntPtr.Zero)
                animationEvent.m_StringParameter = (string)UnsafeUtility.As<IntPtr, GCHandle>(ref animationEventBlittable.m_StringParameter).Target;
            if (animationEventBlittable.m_ObjectReferenceParameter != IntPtr.Zero)
                animationEvent.m_ObjectReferenceParameter = (Object)UnsafeUtility.As<IntPtr, GCHandle>(ref animationEventBlittable.m_ObjectReferenceParameter).Target;
            animationEvent.m_FloatParameter = animationEventBlittable.m_FloatParameter;
            animationEvent.m_IntParameter = animationEventBlittable.m_IntParameter;
            animationEvent.m_MessageOptions = animationEventBlittable.m_MessageOptions;
            animationEvent.m_Source = animationEventBlittable.m_Source;
            if (animationEventBlittable.m_StateSender != IntPtr.Zero)
                animationEvent.m_StateSender = (AnimationState)UnsafeUtility.As<IntPtr, GCHandle>(ref animationEventBlittable.m_StateSender).Target;
            animationEvent.m_AnimatorStateInfo = animationEventBlittable.m_AnimatorStateInfo;
            animationEvent.m_AnimatorClipInfo = animationEventBlittable.m_AnimatorClipInfo;

            return animationEvent;
        }

        public void Dispose()
        {
            if (s_handlePool == null)
                s_handlePool = new GCHandlePool();
            var handlePool = s_handlePool;
            if (m_FunctionName != IntPtr.Zero)
                handlePool.Free(UnsafeUtility.As<IntPtr, GCHandle>(ref m_FunctionName));
            if (m_StringParameter != IntPtr.Zero)
                handlePool.Free(UnsafeUtility.As<IntPtr, GCHandle>(ref m_StringParameter));
            if (m_ObjectReferenceParameter != IntPtr.Zero)
                handlePool.Free(UnsafeUtility.As<IntPtr, GCHandle>(ref m_ObjectReferenceParameter));
            if (m_StateSender != IntPtr.Zero)
                handlePool.Free(UnsafeUtility.As<IntPtr, GCHandle>(ref m_StateSender));
        }
    }

    [System.Serializable]
    [RequiredByNativeCode]
    public sealed class AnimationEvent
    {
        internal float m_Time;
        internal string m_FunctionName;
        internal string m_StringParameter;
        internal Object m_ObjectReferenceParameter;
        internal float m_FloatParameter;
        internal int m_IntParameter;

        internal int m_MessageOptions;
        internal AnimationEventSource m_Source;
        internal AnimationState m_StateSender;
        internal AnimatorStateInfo m_AnimatorStateInfo;
        internal AnimatorClipInfo m_AnimatorClipInfo;

        public AnimationEvent()
        {
            m_Time = 0.0f;
            m_FunctionName = "";
            m_StringParameter = "";
            m_ObjectReferenceParameter = null;
            m_FloatParameter = 0.0f;
            m_IntParameter = 0;
            m_MessageOptions = 0;
            m_Source = AnimationEventSource.NoSource;
            m_StateSender = null;
        }

        [System.Obsolete("Use stringParameter instead")]
        public string data { get { return m_StringParameter; } set { m_StringParameter = value; } }

        public string stringParameter { get { return m_StringParameter; } set { m_StringParameter = value; } }
        public float floatParameter { get { return m_FloatParameter; } set { m_FloatParameter = value; } }
        public int intParameter { get { return m_IntParameter; } set { m_IntParameter = value; } }
        public Object objectReferenceParameter { get { return m_ObjectReferenceParameter; } set { m_ObjectReferenceParameter = value; } }
        public string functionName { get { return m_FunctionName; } set { m_FunctionName = value; } }
        public float time { get { return m_Time; } set { m_Time = value; } }
        public SendMessageOptions messageOptions { get { return (SendMessageOptions)m_MessageOptions; } set { m_MessageOptions = (int)value; } }

        public bool isFiredByLegacy { get { return m_Source == AnimationEventSource.Legacy; } }
        public bool isFiredByAnimator { get { return m_Source == AnimationEventSource.Animator; } }

        public AnimationState animationState
        {
            get
            {
                if (!isFiredByLegacy)
                    Debug.LogError("AnimationEvent was not fired by Animation component, you shouldn't use AnimationEvent.animationState");
                return m_StateSender;
            }
        }

        public AnimatorStateInfo animatorStateInfo
        {
            get
            {
                if (!isFiredByAnimator)
                    Debug.LogError("AnimationEvent was not fired by Animator component, you shouldn't use AnimationEvent.animatorStateInfo");
                return m_AnimatorStateInfo;
            }
        }

        public AnimatorClipInfo animatorClipInfo
        {
            get
            {
                if (!isFiredByAnimator)
                    Debug.LogError("AnimationEvent was not fired by Animator component, you shouldn't use AnimationEvent.animatorClipInfo");
                return m_AnimatorClipInfo;
            }
        }

        internal int GetHash()
        {
            unchecked
            {
                int hash = 0;
                hash = functionName.GetHashCode();
                hash = 33 * hash + time.GetHashCode();
                return hash;
            }
        }
    }
}
