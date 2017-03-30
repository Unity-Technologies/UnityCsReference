// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine;
using UnityEngine.Playables;

using Object = UnityEngine.Object;

namespace UnityEngine.Playables
{


public static partial class AnimationPlayableExtensions
{
    public static AnimationClip GetAnimatedProperties(this PlayableHandle handle)
        {
            return GetAnimatedPropertiesInternal(ref handle);
        }
    
    
    public static void SetAnimatedProperties(this PlayableHandle handle, AnimationClip clip)
        {
            SetAnimatedPropertiesInternal(ref handle, clip);
        }
    
    
    internal static AnimationClip GetAnimatedPropertiesInternal (ref PlayableHandle playable) {
        return INTERNAL_CALL_GetAnimatedPropertiesInternal ( ref playable );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static AnimationClip INTERNAL_CALL_GetAnimatedPropertiesInternal (ref PlayableHandle playable);
    internal static void SetAnimatedPropertiesInternal (ref PlayableHandle playable, AnimationClip animatedProperties) {
        INTERNAL_CALL_SetAnimatedPropertiesInternal ( ref playable, animatedProperties );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetAnimatedPropertiesInternal (ref PlayableHandle playable, AnimationClip animatedProperties);
}

public static partial class AnimationPlayableGraphExtensions
{
    static public AnimationPlayableOutput CreateAnimationOutput(this PlayableGraph graph, string name, Animator target)
        {
            AnimationPlayableOutput output = new AnimationPlayableOutput();
            if (!InternalCreateAnimationOutput(ref graph, name, out output.m_Output))
                return AnimationPlayableOutput.Null;

            output.target = target;

            return output;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool InternalCreateAnimationOutput (ref PlayableGraph graph, string name, out PlayableOutput output) ;

    static internal void SyncUpdateAndTimeMode(this PlayableGraph graph, Animator animator)
        {
            InternalSyncUpdateAndTimeMode(ref graph, animator);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void InternalSyncUpdateAndTimeMode (ref PlayableGraph graph, Animator animator) ;

    static public PlayableHandle CreateAnimationClipPlayable(this PlayableGraph graph, AnimationClip clip)
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!InternalCreateAnimationClipPlayable(ref graph, clip, ref handle))
                return PlayableHandle.Null;

            return handle;
        }
    
    
    private static bool InternalCreateAnimationClipPlayable (ref PlayableGraph graph, AnimationClip clip, ref PlayableHandle handle) {
        return INTERNAL_CALL_InternalCreateAnimationClipPlayable ( ref graph, clip, ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_InternalCreateAnimationClipPlayable (ref PlayableGraph graph, AnimationClip clip, ref PlayableHandle handle);
    [uei.ExcludeFromDocs]
public static PlayableHandle CreateAnimationMixerPlayable (this PlayableGraph graph, int inputCount ) {
    bool normalizeWeights = false;
    return CreateAnimationMixerPlayable ( graph, inputCount, normalizeWeights );
}

[uei.ExcludeFromDocs]
public static PlayableHandle CreateAnimationMixerPlayable (this PlayableGraph graph) {
    bool normalizeWeights = false;
    int inputCount = 0;
    return CreateAnimationMixerPlayable ( graph, inputCount, normalizeWeights );
}

public static PlayableHandle CreateAnimationMixerPlayable(this PlayableGraph graph, [uei.DefaultValue("0")]  int inputCount , [uei.DefaultValue("false")]  bool normalizeWeights )
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!InternalCreateAnimationMixerPlayable(ref graph, inputCount, normalizeWeights, ref handle))
                return PlayableHandle.Null;
            handle.inputCount = inputCount;
            return handle;
        }

    
    
    private static bool InternalCreateAnimationMixerPlayable (ref PlayableGraph graph, int inputCount, bool normalizeWeights, ref PlayableHandle handle) {
        return INTERNAL_CALL_InternalCreateAnimationMixerPlayable ( ref graph, inputCount, normalizeWeights, ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_InternalCreateAnimationMixerPlayable (ref PlayableGraph graph, int inputCount, bool normalizeWeights, ref PlayableHandle handle);
    static public PlayableHandle CreateAnimatorControllerPlayable(this PlayableGraph graph, RuntimeAnimatorController controller)
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!InternalCreateAnimatorControllerPlayable(ref graph, controller, ref handle))
                return PlayableHandle.Null;

            return handle;
        }
    
    
    private static bool InternalCreateAnimatorControllerPlayable (ref PlayableGraph graph, RuntimeAnimatorController controller, ref PlayableHandle handle) {
        return INTERNAL_CALL_InternalCreateAnimatorControllerPlayable ( ref graph, controller, ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_InternalCreateAnimatorControllerPlayable (ref PlayableGraph graph, RuntimeAnimatorController controller, ref PlayableHandle handle);
    static internal PlayableHandle CreateAnimationMotionXToDeltaPlayable(this PlayableGraph graph)
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!InternalCreateAnimationMotionXToDeltaPlayable(ref graph, ref handle))
                return PlayableHandle.Null;

            handle.inputCount = 1;
            return handle;
        }
    
    
    private static bool InternalCreateAnimationMotionXToDeltaPlayable (ref PlayableGraph graph, ref PlayableHandle handle) {
        return INTERNAL_CALL_InternalCreateAnimationMotionXToDeltaPlayable ( ref graph, ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_InternalCreateAnimationMotionXToDeltaPlayable (ref PlayableGraph graph, ref PlayableHandle handle);
    [uei.ExcludeFromDocs]
public static PlayableHandle CreateAnimationLayerMixerPlayable (this PlayableGraph graph) {
    int inputCount = 0;
    return CreateAnimationLayerMixerPlayable ( graph, inputCount );
}

public static PlayableHandle CreateAnimationLayerMixerPlayable(this PlayableGraph graph, [uei.DefaultValue("0")]  int inputCount )
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!InternalCreateAnimationLayerMixerPlayable(ref graph, ref handle))
                return PlayableHandle.Null;
            handle.inputCount = inputCount;
            return handle;
        }

    
    
    private static bool InternalCreateAnimationLayerMixerPlayable (ref PlayableGraph graph, ref PlayableHandle handle) {
        return INTERNAL_CALL_InternalCreateAnimationLayerMixerPlayable ( ref graph, ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_InternalCreateAnimationLayerMixerPlayable (ref PlayableGraph graph, ref PlayableHandle handle);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void InternalDestroyOutput (ref PlayableGraph graph, ref PlayableOutput output) ;

    static public void DestroyOutput(this PlayableGraph graph, AnimationPlayableOutput output)
        {
            InternalDestroyOutput(ref graph, ref output.m_Output);
        }
    
    
    public static int GetAnimationOutputCount(this PlayableGraph graph)
        {
            return InternalAnimationOutputCount(ref graph);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int InternalAnimationOutputCount (ref PlayableGraph graph) ;

    public static AnimationPlayableOutput GetAnimationOutput(this PlayableGraph graph, int index)
        {
            AnimationPlayableOutput output = new AnimationPlayableOutput();
            if (!InternalGetAnimationOutput(ref graph, index, out output.m_Output))
                return AnimationPlayableOutput.Null;
            return output;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool InternalGetAnimationOutput (ref PlayableGraph graph, int index, out PlayableOutput output) ;

}

[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct AnimationPlayableOutput
{
    internal PlayableOutput m_Output;
    
    
    public static AnimationPlayableOutput Null
        {
            get { return new AnimationPlayableOutput() { m_Output = PlayableOutput.Null }; }
        }
    
    
    internal Object referenceObject
        {
            get { return PlayableOutput.GetInternalReferenceObject(ref m_Output); }
            set { PlayableOutput.SetInternalReferenceObject(ref m_Output, value); }
        }
    
    
    public Object userData
        {
            get { return PlayableOutput.GetInternalUserData(ref m_Output); }
            set { PlayableOutput.SetInternalUserData(ref m_Output, value); }
        }
    
    
    public bool IsValid()
        {
            return PlayableOutput.IsValidInternal(ref m_Output);
        }
    
    
    public Animator target
        {
            get { return InternalGetTarget(ref m_Output); }
            set { InternalSetTarget(ref m_Output, value); }
        }
    
    
    public float weight
        {
            get { return PlayableOutput.InternalGetWeight(ref m_Output); }
            set { PlayableOutput.InternalSetWeight(ref m_Output, value); }
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  Animator InternalGetTarget (ref PlayableOutput output) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void InternalSetTarget (ref PlayableOutput output, Animator target) ;

    public PlayableHandle sourcePlayable
        {
            get { return PlayableOutput.InternalGetSourcePlayable(ref m_Output); }
            set { PlayableOutput.InternalSetSourcePlayable(ref m_Output, ref value);  }
        }
    
    
    public int sourceInputPort
        {
            get { return PlayableOutput.InternalGetSourceInputPort(ref m_Output); }
            set { PlayableOutput.InternalSetSourceInputPort(ref m_Output, value); }
        }
    
    
}

}

namespace UnityEngineInternal.Playables
{


public static partial class AnimationPlayableGraphUtility
{
    static public PlayableHandle CreateAnimationOffsetPlayable(PlayableGraph graph, Vector3 position, Quaternion rotation, int inputCount)
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!InternalCreateAnimationOffsetPlayable(ref graph, position, rotation, ref handle))
                return PlayableHandle.Null;
            handle.inputCount = inputCount;
            return handle;
        }
    
    
    private static bool InternalCreateAnimationOffsetPlayable (ref PlayableGraph graph, Vector3 position, Quaternion rotation, ref PlayableHandle handle) {
        return INTERNAL_CALL_InternalCreateAnimationOffsetPlayable ( ref graph, ref position, ref rotation, ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_InternalCreateAnimationOffsetPlayable (ref PlayableGraph graph, ref Vector3 position, ref Quaternion rotation, ref PlayableHandle handle);
}

}
