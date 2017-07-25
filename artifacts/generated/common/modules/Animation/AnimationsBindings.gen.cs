// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;


using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using UnityEngine.Bindings;
using UnityEngine.Playables;

namespace UnityEngine
{


internal enum AnimationEventSource
{
    NoSource = 0,
    Legacy = 1,
    Animator = 2,
}

[System.Serializable]
[StructLayout(LayoutKind.Sequential)]
[RequiredByNativeCode]
public sealed partial class AnimationEvent
{
    
            internal float                  m_Time;
            internal string                 m_FunctionName;
            internal string                 m_StringParameter;
            internal Object                 m_ObjectReferenceParameter;
            internal float                  m_FloatParameter;
            internal int                    m_IntParameter;
    
            internal int                    m_MessageOptions;
            internal AnimationEventSource   m_Source;
            internal AnimationState         m_StateSender;
            internal AnimatorStateInfo      m_AnimatorStateInfo;
            internal AnimatorClipInfo       m_AnimatorClipInfo;
    
    
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
    
    
    [System.Obsolete ("Use stringParameter instead")]
    public string data { get { return m_StringParameter; }  set { m_StringParameter = value; } }
    
    
    public string stringParameter { get { return m_StringParameter; } set { m_StringParameter = value; } }
    
    
    public float floatParameter { get { return m_FloatParameter; } set { m_FloatParameter = value; } }
    
    
    public int intParameter { get { return m_IntParameter; } set { m_IntParameter = value; } }
    
    
    public Object objectReferenceParameter { get { return m_ObjectReferenceParameter; } set { m_ObjectReferenceParameter = value; } }
    
    
    public string functionName { get { return m_FunctionName; } set { m_FunctionName = value; } }
    
    
    public float time { get { return m_Time; } set { m_Time = value; } }
    
    
    public SendMessageOptions messageOptions { get { return (SendMessageOptions)m_MessageOptions; } set { m_MessageOptions = (int)value; } }
    
    
    public bool isFiredByLegacy  { get { return m_Source == AnimationEventSource.Legacy; } }
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

[NativeType("Runtime/Animation/AnimationClip.h")]
public sealed partial class AnimationClip : Motion
{
    public AnimationClip()
        {
            Internal_CreateAnimationClip(this);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SampleAnimation (GameObject go, float time) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_CreateAnimationClip ([Writable] AnimationClip self) ;

    public extern  float length
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    internal extern  float startTime
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    internal extern  float stopTime
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern float frameRate
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetCurve (string relativePath, Type type, string propertyName, AnimationCurve curve) ;

    public void EnsureQuaternionContinuity () {
        INTERNAL_CALL_EnsureQuaternionContinuity ( this );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_EnsureQuaternionContinuity (AnimationClip self);
    public void ClearCurves () {
        INTERNAL_CALL_ClearCurves ( this );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_ClearCurves (AnimationClip self);
    public extern WrapMode wrapMode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public Bounds localBounds
    {
        get { Bounds tmp; INTERNAL_get_localBounds(out tmp); return tmp;  }
        set { INTERNAL_set_localBounds(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_localBounds (out Bounds value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_localBounds (ref Bounds value) ;

    public extern new  bool legacy
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  bool humanMotion
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  bool empty
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public void AddEvent(AnimationEvent evt)
        {
            if (evt == null)
                throw new ArgumentNullException("evt");

            AddEventInternal(evt);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void AddEventInternal (object evt) ;

    public AnimationEvent[] events
        {
            get
            {
                return (AnimationEvent[])GetEventsInternal();
            }
            set
            {
                SetEventsInternal(value);
            }
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void SetEventsInternal (System.Array value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal System.Array GetEventsInternal () ;

    internal extern  bool hasRootMotion
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

}

public enum PlayMode
{
    
    StopSameLayer = 0,
    
    StopAll = 4,
}

public enum QueueMode
{
    
    CompleteOthers = 0,
    
    PlayNow = 2
}

public enum AnimationBlendMode
{
    
    Blend = 0,
    
    Additive = 1
}

public enum AnimationPlayMode { Stop = 0, Queue = 1, Mix = 2 }


public enum AnimationCullingType
{
    
    AlwaysAnimate = 0,
    
    BasedOnRenderers = 1,
    
    [System.Obsolete ("Enum member AnimatorCullingMode.BasedOnClipBounds has been deprecated. Use AnimationCullingType.AlwaysAnimate or AnimationCullingType.BasedOnRenderers instead")]
    BasedOnClipBounds = 2,
    
    [System.Obsolete ("Enum member AnimatorCullingMode.BasedOnUserBounds has been deprecated. Use AnimationCullingType.AlwaysAnimate or AnimationCullingType.BasedOnRenderers instead")]
    BasedOnUserBounds = 3
}

public sealed partial class Animation : Behaviour, IEnumerable
{
    public extern AnimationClip clip
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool playAutomatically
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern WrapMode wrapMode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public void Stop () {
        INTERNAL_CALL_Stop ( this );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Stop (Animation self);
    public void Stop(string name) { Internal_StopByName(name); }
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Internal_StopByName (string name) ;

    public void Rewind(string name) {  Internal_RewindByName(name); }
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Internal_RewindByName (string name) ;

    public void Rewind () {
        INTERNAL_CALL_Rewind ( this );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Rewind (Animation self);
    public void Sample () {
        INTERNAL_CALL_Sample ( this );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Sample (Animation self);
    public extern bool isPlaying
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public bool IsPlaying (string name) ;

    public AnimationState this[string name]
            {
            get { return GetState(name); }
        }
    
    
    [uei.ExcludeFromDocs]
public bool Play () {
    PlayMode mode = PlayMode.StopSameLayer;
    return Play ( mode );
}

public bool Play( [uei.DefaultValue("PlayMode.StopSameLayer")] PlayMode mode ) { return PlayDefaultAnimation(mode); }

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public bool Play (string animation, [uei.DefaultValue("PlayMode.StopSameLayer")]  PlayMode mode ) ;

    [uei.ExcludeFromDocs]
    public bool Play (string animation) {
        PlayMode mode = PlayMode.StopSameLayer;
        return Play ( animation, mode );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void CrossFade (string animation, [uei.DefaultValue("0.3F")]  float fadeLength , [uei.DefaultValue("PlayMode.StopSameLayer")]  PlayMode mode ) ;

    [uei.ExcludeFromDocs]
    public void CrossFade (string animation, float fadeLength ) {
        PlayMode mode = PlayMode.StopSameLayer;
        CrossFade ( animation, fadeLength, mode );
    }

    [uei.ExcludeFromDocs]
    public void CrossFade (string animation) {
        PlayMode mode = PlayMode.StopSameLayer;
        float fadeLength = 0.3F;
        CrossFade ( animation, fadeLength, mode );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void Blend (string animation, [uei.DefaultValue("1.0F")]  float targetWeight , [uei.DefaultValue("0.3F")]  float fadeLength ) ;

    [uei.ExcludeFromDocs]
    public void Blend (string animation, float targetWeight ) {
        float fadeLength = 0.3F;
        Blend ( animation, targetWeight, fadeLength );
    }

    [uei.ExcludeFromDocs]
    public void Blend (string animation) {
        float fadeLength = 0.3F;
        float targetWeight = 1.0F;
        Blend ( animation, targetWeight, fadeLength );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public AnimationState CrossFadeQueued (string animation, [uei.DefaultValue("0.3F")]  float fadeLength , [uei.DefaultValue("QueueMode.CompleteOthers")]  QueueMode queue , [uei.DefaultValue("PlayMode.StopSameLayer")]  PlayMode mode ) ;

    [uei.ExcludeFromDocs]
    public AnimationState CrossFadeQueued (string animation, float fadeLength , QueueMode queue ) {
        PlayMode mode = PlayMode.StopSameLayer;
        return CrossFadeQueued ( animation, fadeLength, queue, mode );
    }

    [uei.ExcludeFromDocs]
    public AnimationState CrossFadeQueued (string animation, float fadeLength ) {
        PlayMode mode = PlayMode.StopSameLayer;
        QueueMode queue = QueueMode.CompleteOthers;
        return CrossFadeQueued ( animation, fadeLength, queue, mode );
    }

    [uei.ExcludeFromDocs]
    public AnimationState CrossFadeQueued (string animation) {
        PlayMode mode = PlayMode.StopSameLayer;
        QueueMode queue = QueueMode.CompleteOthers;
        float fadeLength = 0.3F;
        return CrossFadeQueued ( animation, fadeLength, queue, mode );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public AnimationState PlayQueued (string animation, [uei.DefaultValue("QueueMode.CompleteOthers")]  QueueMode queue , [uei.DefaultValue("PlayMode.StopSameLayer")]  PlayMode mode ) ;

    [uei.ExcludeFromDocs]
    public AnimationState PlayQueued (string animation, QueueMode queue ) {
        PlayMode mode = PlayMode.StopSameLayer;
        return PlayQueued ( animation, queue, mode );
    }

    [uei.ExcludeFromDocs]
    public AnimationState PlayQueued (string animation) {
        PlayMode mode = PlayMode.StopSameLayer;
        QueueMode queue = QueueMode.CompleteOthers;
        return PlayQueued ( animation, queue, mode );
    }

    public void AddClip(AnimationClip clip, string newName) { AddClip(clip, newName, Int32.MinValue, Int32.MaxValue); }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void AddClip (AnimationClip clip, string newName, int firstFrame, int lastFrame, [uei.DefaultValue("false")]  bool addLoopFrame ) ;

    [uei.ExcludeFromDocs]
    public void AddClip (AnimationClip clip, string newName, int firstFrame, int lastFrame) {
        bool addLoopFrame = false;
        AddClip ( clip, newName, firstFrame, lastFrame, addLoopFrame );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void RemoveClip (AnimationClip clip) ;

    public void RemoveClip(string clipName) { RemoveClip2(clipName); }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public int GetClipCount () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void RemoveClip2 (string clipName) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private bool PlayDefaultAnimation (PlayMode mode) ;

    [System.Obsolete ("use PlayMode instead of AnimationPlayMode.")]
public bool Play(AnimationPlayMode mode) { return PlayDefaultAnimation((PlayMode)mode); }
    [System.Obsolete ("use PlayMode instead of AnimationPlayMode.")]
public bool Play(string animation, AnimationPlayMode mode) { return Play(animation, (PlayMode)mode); }
    
    
    
    public void SyncLayer (int layer) {
        INTERNAL_CALL_SyncLayer ( this, layer );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SyncLayer (Animation self, int layer);
    public IEnumerator GetEnumerator()
        {
            return new Animation.Enumerator(this);
        }
    
    
    private sealed partial class Enumerator : IEnumerator    
    {
        
                    private Animation m_Outer;
                    private int       m_CurrentIndex = -1;
        
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

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal AnimationState GetState (string name) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal AnimationState GetStateAtIndex (int index) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal int GetStateCount () ;

    public AnimationClip GetClip(string name)
        {
            AnimationState state = GetState(name);
            if (state)
                return state.clip;
            else
                return null;
        }
    
    
    public extern bool animatePhysics
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [System.Obsolete ("Use cullingType instead")]
    public extern  bool animateOnlyIfVisible
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern AnimationCullingType cullingType
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public Bounds localBounds
    {
        get { Bounds tmp; INTERNAL_get_localBounds(out tmp); return tmp;  }
        set { INTERNAL_set_localBounds(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_localBounds (out Bounds value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_localBounds (ref Bounds value) ;

}

[UsedByNativeCode]
public sealed partial class AnimationState : TrackedReference
{
    public extern bool enabled
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float weight
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern WrapMode wrapMode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float time
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float normalizedTime
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float speed
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float normalizedSpeed
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float length
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern int layer
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern AnimationClip clip
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void AddMixingTransform (Transform mix, [uei.DefaultValue("true")]  bool recursive ) ;

    [uei.ExcludeFromDocs]
    public void AddMixingTransform (Transform mix) {
        bool recursive = true;
        AddMixingTransform ( mix, recursive );
    }

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void RemoveMixingTransform (Transform mix) ;

    public extern  string name
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern AnimationBlendMode blendMode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

}

}
