// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine.Bindings;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.InteropServices;
namespace UnityEngine
{
    // Stores keyframe based animations.
    [NativeHeader("Modules/Animation/ScriptBindings/AnimationClip.bindings.h")]
    [NativeType("Modules/Animation/AnimationClip.h")]
    public sealed class AnimationClip : Motion
    {
        // Creates a new animation clip
        public AnimationClip()
        {
            Internal_CreateAnimationClip(this);
        }

        [FreeFunction("AnimationClipBindings::Internal_CreateAnimationClip")]
        extern private static void Internal_CreateAnimationClip([Writable] AnimationClip self);

        // This method was moved here to prevent GameObject or the Core from depending on Animation.
        // Helps in modularizing managed code.
        public void SampleAnimation(GameObject go, float time)
        {
            SampleAnimation(go, this, time, this.wrapMode);
        }

        [NativeHeader("Modules/Animation/AnimationUtility.h")]
        [FreeFunction]
        extern internal static void SampleAnimation([NotNull] GameObject go, [NotNull] AnimationClip clip, float inTime, WrapMode wrapMode);


        // Animation length in seconds (RO)
        [NativeProperty("Length", false, TargetType.Function)]
        public extern float length { get; }

        [NativeProperty("StartTime", false, TargetType.Function)]
        internal extern float startTime { get; }

        [NativeProperty("StopTime", false, TargetType.Function)]
        internal extern float stopTime { get; }

        [NativeProperty("SampleRate", false, TargetType.Function)]

        // Frame rate at which keyframes are sampled (RO)
        public extern float frameRate { get; set; }

        [FreeFunction("AnimationClipBindings::Internal_SetCurve", HasExplicitThis = true)]
        public extern void SetCurve([NotNull] string relativePath, [NotNull] Type type, [NotNull] string propertyName, AnimationCurve curve);

        //*undocumented*
        public extern void EnsureQuaternionContinuity();

        // Clears all curves from the clip.
        public extern void ClearCurves();

        // Sets the default wrap mode used in the animation state.
        [NativeProperty("WrapMode", false, TargetType.Function)]
        public extern WrapMode wrapMode { get; set; }

        // AABB of this Animation Clip in local space of Animation component that it is attached too.
        [NativeProperty("Bounds", false, TargetType.Function)]
        public extern Bounds localBounds { get; set; }

        extern public new bool legacy
        {
            [NativeMethod("IsLegacy")]
            get;
            [NativeMethod("SetLegacy")]
            set;
        }

        extern public bool humanMotion
        {
            [NativeMethod("IsHumanMotion")]
            get;
        }

        extern public bool empty
        {
            [NativeMethod("IsEmpty")]
            get;
        }

        extern public bool hasGenericRootTransform
        {
            [NativeMethod("HasGenericRootTransform")]
            get;
        }

        extern public bool hasMotionFloatCurves
        {
            [NativeMethod("HasMotionFloatCurves")]
            get;
        }

        extern public bool hasMotionCurves
        {
            [NativeMethod("HasMotionCurves")]
            get;
        }

        extern public bool hasRootCurves
        {
            [NativeMethod("HasRootCurves")]
            get;
        }

        internal extern bool hasRootMotion
        {
            [FreeFunction(Name = "AnimationClipBindings::Internal_GetHasRootMotion", HasExplicitThis = true)]
            get;
        }

        public void AddEvent(AnimationEvent evt)
        {
            if (evt == null)
                throw new ArgumentNullException("evt");
            var animationEventBlittable = AnimationEventBlittable.FromAnimationEvent(evt);
            AddEventInternal(animationEventBlittable);
            animationEventBlittable.Dispose();
        }

        [FreeFunction(Name = "AnimationClipBindings::AddEventInternal", HasExplicitThis = true)]
        extern private void AddEventInternal(System.Object evt);

        // Retrieves all animation events associated with the animation clip
        unsafe public AnimationEvent[] events
        {
            get
            {
                GetEventsInternal(out var blittableEventsPointer, out var numberOfEvents);
                var animationEvents = AnimationEventBlittable.PointerToAnimationEvents(blittableEventsPointer, numberOfEvents);
                AnimationEventBlittable.DisposeEvents(blittableEventsPointer, numberOfEvents);
                return animationEvents;
            }
            set
            {
                using (NativeArray<AnimationEventBlittable> blittableEvents = new NativeArray<AnimationEventBlittable>(value.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory))
                {
                    var pBlittableEvents = (AnimationEventBlittable*)blittableEvents.GetUnsafePtr();
                    AnimationEventBlittable.FromAnimationEvents(value, pBlittableEvents);
                    SetEventsInternal(pBlittableEvents, blittableEvents.Length);
                    for (var i = 0; i < value.Length; ++i)
                    {
                        pBlittableEvents->Dispose();
                        pBlittableEvents++;
                    }
                }
            }
        }
        [FreeFunction(Name = "AnimationClipBindings::SetEventsInternal", HasExplicitThis = true)]
        extern unsafe private void SetEventsInternal(void* data, int length);
        [FreeFunction(Name = "AnimationClipBindings::GetEventsInternal", HasExplicitThis = true)]
        extern private void GetEventsInternal(out IntPtr values, out int size);
    }

    unsafe class GCHandlePool
    {
        GCHandle[] m_handles;
        int m_current;

        public GCHandlePool()
        {
            m_handles = new GCHandle[128];
        }

        public GCHandle Alloc()
        {
            if (m_current > 0)
            {
                return m_handles[--m_current];
            }

            return GCHandle.Alloc(null);
        }

        public GCHandle Alloc(object o)
        {
            if (m_current > 0)
            {
                var handle = m_handles[--m_current];

                handle.Target = o;

                return handle;
            }

            return GCHandle.Alloc(o);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntPtr AllocHandleIfNotNull(object o)
        {
            if (o == null)
                return IntPtr.Zero;

            return (IntPtr)Alloc(o);
        }

        public void Free(GCHandle h)
        {
            if (m_current == m_handles.Length)
            {
                var newLength = m_handles.Length * 2;
                var newHandles = new GCHandle[newLength];
                Array.Copy(m_handles, newHandles, m_handles.Length);

                m_handles = newHandles;
            }

            h.Target = null;

            m_handles[m_current++] = h;
        }
    }
}
