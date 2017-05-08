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

namespace UnityEngine.Animations
{


public static partial class AnimationPlayableExtensions
{
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
    internal static bool InternalCreateAnimationOutput (ref PlayableGraph graph, string name, out PlayableOutputHandle handle) {
        return INTERNAL_CALL_InternalCreateAnimationOutput ( ref graph, name, out handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_InternalCreateAnimationOutput (ref PlayableGraph graph, string name, out PlayableOutputHandle handle);
    static internal void SyncUpdateAndTimeMode(this PlayableGraph graph, Animator animator)
        {
            InternalSyncUpdateAndTimeMode(ref graph, animator);
        }
    
    
    internal static void InternalSyncUpdateAndTimeMode (ref PlayableGraph graph, Animator animator) {
        INTERNAL_CALL_InternalSyncUpdateAndTimeMode ( ref graph, animator );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_InternalSyncUpdateAndTimeMode (ref PlayableGraph graph, Animator animator);
    static internal PlayableHandle CreateAnimationMotionXToDeltaPlayable(this PlayableGraph graph)
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!InternalCreateAnimationMotionXToDeltaPlayable(ref graph, ref handle))
                return PlayableHandle.Null;

            handle.SetInputCount(1);
            return handle;
        }
    
    
    private static bool InternalCreateAnimationMotionXToDeltaPlayable (ref PlayableGraph graph, ref PlayableHandle handle) {
        return INTERNAL_CALL_InternalCreateAnimationMotionXToDeltaPlayable ( ref graph, ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_InternalCreateAnimationMotionXToDeltaPlayable (ref PlayableGraph graph, ref PlayableHandle handle);
    private static void InternalDestroyOutput (ref PlayableGraph graph, ref PlayableOutputHandle handle) {
        INTERNAL_CALL_InternalDestroyOutput ( ref graph, ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_InternalDestroyOutput (ref PlayableGraph graph, ref PlayableOutputHandle handle);
    static internal void DestroyOutput(this PlayableGraph graph, PlayableOutputHandle handle)
        {
            InternalDestroyOutput(ref graph, ref handle);
        }
    
    
    private static int InternalAnimationOutputCount (ref PlayableGraph graph) {
        return INTERNAL_CALL_InternalAnimationOutputCount ( ref graph );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_InternalAnimationOutputCount (ref PlayableGraph graph);
    private static bool InternalGetAnimationOutput (ref PlayableGraph graph, int index, out PlayableOutputHandle handle) {
        return INTERNAL_CALL_InternalGetAnimationOutput ( ref graph, index, out handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_InternalGetAnimationOutput (ref PlayableGraph graph, int index, out PlayableOutputHandle handle);
}

}
