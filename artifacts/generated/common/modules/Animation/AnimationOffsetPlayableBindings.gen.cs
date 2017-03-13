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
internal sealed partial class AnimationOffsetPlayable : AnimationPlayable
{
    public Vector3 position
        {
            get
            {
                return GetPosition(ref handle);
            }
            set
            {
                SetPosition(ref handle, value);
            }
        }
    
    
    public Quaternion rotation
        {
            get
            {
                return GetRotation(ref handle);
            }
            set
            {
                SetRotation(ref handle, value);
            }
        }
    
    
    
    private static Vector3 GetPosition (ref PlayableHandle handle) {
        Vector3 result;
        INTERNAL_CALL_GetPosition ( ref handle, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetPosition (ref PlayableHandle handle, out Vector3 value);
    private static void SetPosition (ref PlayableHandle handle, Vector3 value) {
        INTERNAL_CALL_SetPosition ( ref handle, ref value );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetPosition (ref PlayableHandle handle, ref Vector3 value);
    private static Quaternion GetRotation (ref PlayableHandle handle) {
        Quaternion result;
        INTERNAL_CALL_GetRotation ( ref handle, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetRotation (ref PlayableHandle handle, out Quaternion value);
    private static void SetRotation (ref PlayableHandle handle, Quaternion value) {
        INTERNAL_CALL_SetRotation ( ref handle, ref value );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetRotation (ref PlayableHandle handle, ref Quaternion value);
}

}
