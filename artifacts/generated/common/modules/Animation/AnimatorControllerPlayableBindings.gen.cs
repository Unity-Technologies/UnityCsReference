// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;


using System;
using UnityEngine.Playables;
using System.Collections;
using System.Collections.Generic;


namespace UnityEngine.Animations
{


[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct AnimatorControllerPlayable
{
    private static void GetAnimatorClipInfoInternal (ref PlayableHandle handle, int layerIndex, bool isCurrent, object clips) {
        INTERNAL_CALL_GetAnimatorClipInfoInternal ( ref handle, layerIndex, isCurrent, clips );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetAnimatorClipInfoInternal (ref PlayableHandle handle, int layerIndex, bool isCurrent, object clips);
    
    
    private static AnimatorControllerParameter[] GetParametersArrayInternal (ref PlayableHandle handle) {
        return INTERNAL_CALL_GetParametersArrayInternal ( ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static AnimatorControllerParameter[] INTERNAL_CALL_GetParametersArrayInternal (ref PlayableHandle handle);
}

}
