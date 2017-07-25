// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine;

using Object = UnityEngine.Object;

namespace UnityEngine.Playables
{
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct PlayableOutputHandle
{
    internal Object GetUserData()
        {
            return GetInternalUserData(ref this);
        }
    
    
    internal void SetUserData(Object value)
        {
            SetInternalUserData(ref this, value);
        }
    
    
    internal static Object GetInternalUserData (ref PlayableOutputHandle handle) {
        return INTERNAL_CALL_GetInternalUserData ( ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Object INTERNAL_CALL_GetInternalUserData (ref PlayableOutputHandle handle);
    internal static void SetInternalUserData (ref PlayableOutputHandle handle, [Writable] Object target) {
        INTERNAL_CALL_SetInternalUserData ( ref handle, target );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetInternalUserData (ref PlayableOutputHandle handle, [Writable]Object target);
}

}
