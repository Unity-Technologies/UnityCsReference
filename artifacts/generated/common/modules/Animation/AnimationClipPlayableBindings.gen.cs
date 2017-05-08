// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;


using System;
using UnityEngine.Playables;

namespace UnityEngine.Animations
{
[RequiredByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct AnimationClipPlayable
{
    private static bool CreateHandleInternal (PlayableGraph graph, AnimationClip clip, ref PlayableHandle handle) {
        return INTERNAL_CALL_CreateHandleInternal ( ref graph, clip, ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_CreateHandleInternal (ref PlayableGraph graph, AnimationClip clip, ref PlayableHandle handle);
    public AnimationClip GetAnimationClip()
        {
            return GetAnimationClipInternal(ref m_Handle);
        }
    
    
    public bool GetApplyFootIK()
        {
            return GetApplyFootIKInternal(ref m_Handle);
        }
    
    
    public void SetApplyFootIK(bool value)
        {
            SetApplyFootIKInternal(ref m_Handle, value);
        }
    
    
    internal bool GetRemoveStartOffset()
        {
            return GetRemoveStartOffsetInternal(ref m_Handle);
        }
    
    
    internal void SetRemoveStartOffset(bool value)
        {
            SetRemoveStartOffsetInternal(ref m_Handle, value);
        }
    
    
    private static AnimationClip GetAnimationClipInternal (ref PlayableHandle handle) {
        return INTERNAL_CALL_GetAnimationClipInternal ( ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static AnimationClip INTERNAL_CALL_GetAnimationClipInternal (ref PlayableHandle handle);
    private static bool GetApplyFootIKInternal (ref PlayableHandle handle) {
        return INTERNAL_CALL_GetApplyFootIKInternal ( ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_GetApplyFootIKInternal (ref PlayableHandle handle);
    private static void SetApplyFootIKInternal (ref PlayableHandle handle, bool value) {
        INTERNAL_CALL_SetApplyFootIKInternal ( ref handle, value );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetApplyFootIKInternal (ref PlayableHandle handle, bool value);
    private static bool GetRemoveStartOffsetInternal (ref PlayableHandle handle) {
        return INTERNAL_CALL_GetRemoveStartOffsetInternal ( ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_GetRemoveStartOffsetInternal (ref PlayableHandle handle);
    private static void SetRemoveStartOffsetInternal (ref PlayableHandle handle, bool value) {
        INTERNAL_CALL_SetRemoveStartOffsetInternal ( ref handle, value );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetRemoveStartOffsetInternal (ref PlayableHandle handle, bool value);
}

}
