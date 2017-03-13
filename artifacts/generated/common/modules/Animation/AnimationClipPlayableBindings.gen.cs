// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;


using System;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace UnityEngine.Playables
{
[RequiredByNativeCode]
public sealed partial class AnimationClipPlayable : AnimationPlayable
{
    public AnimationClip clip
        {
            get
            {
                return GetAnimationClip(ref handle);
            }
        }
    
    
    public float speed
        {
            get
            {
                return GetSpeed(ref handle);
            }
            set
            {
                SetSpeed(ref handle, value);
            }
        }
    
    
    
    public bool applyFootIK
        {
            get
            {
                return GetApplyFootIK(ref handle);
            }
            set
            {
                SetApplyFootIK(ref handle, value);
            }
        }
    
    
    internal bool removeStartOffset
        {
            get
            {
                return GetRemoveStartOffset(ref handle);
            }
            set
            {
                SetRemoveStartOffset(ref handle, value);
            }
        }
    
    
    private static AnimationClip GetAnimationClip (ref PlayableHandle handle) {
        return INTERNAL_CALL_GetAnimationClip ( ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static AnimationClip INTERNAL_CALL_GetAnimationClip (ref PlayableHandle handle);
    private static float GetSpeed (ref PlayableHandle handle) {
        return INTERNAL_CALL_GetSpeed ( ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static float INTERNAL_CALL_GetSpeed (ref PlayableHandle handle);
    private static void SetSpeed (ref PlayableHandle handle, float value) {
        INTERNAL_CALL_SetSpeed ( ref handle, value );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetSpeed (ref PlayableHandle handle, float value);
    private static bool GetApplyFootIK (ref PlayableHandle handle) {
        return INTERNAL_CALL_GetApplyFootIK ( ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_GetApplyFootIK (ref PlayableHandle handle);
    private static void SetApplyFootIK (ref PlayableHandle handle, bool value) {
        INTERNAL_CALL_SetApplyFootIK ( ref handle, value );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetApplyFootIK (ref PlayableHandle handle, bool value);
    private static bool GetRemoveStartOffset (ref PlayableHandle handle) {
        return INTERNAL_CALL_GetRemoveStartOffset ( ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_GetRemoveStartOffset (ref PlayableHandle handle);
    private static void SetRemoveStartOffset (ref PlayableHandle handle, bool value) {
        INTERNAL_CALL_SetRemoveStartOffset ( ref handle, value );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetRemoveStartOffset (ref PlayableHandle handle, bool value);
}

}
