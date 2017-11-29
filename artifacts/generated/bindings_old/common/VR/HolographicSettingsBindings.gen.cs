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
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

#pragma warning disable 649

namespace UnityEngine.XR.WSA
{


[MovedFrom("UnityEngine.VR.WSA")]
public sealed partial class HolographicSettings
{
    static public void SetFocusPointForFrame(Vector3 position)
        {
            InternalSetFocusPointForFrame(position);
        }
    
    
    private static void InternalSetFocusPointForFrame (Vector3 position) {
        INTERNAL_CALL_InternalSetFocusPointForFrame ( ref position );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_InternalSetFocusPointForFrame (ref Vector3 position);
    static public void SetFocusPointForFrame(Vector3 position, Vector3 normal)
        {
            InternalSetFocusPointForFrameWithNormal(position, normal);
        }
    
    
    private static void InternalSetFocusPointForFrameWithNormal (Vector3 position, Vector3 normal) {
        INTERNAL_CALL_InternalSetFocusPointForFrameWithNormal ( ref position, ref normal );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_InternalSetFocusPointForFrameWithNormal (ref Vector3 position, ref Vector3 normal);
    static public void SetFocusPointForFrame(Vector3 position, Vector3 normal, Vector3 velocity)
        {
        }
    
    
    private static void InternalSetFocusPointForFrameWithNormalVelocity (Vector3 position, Vector3 normal, Vector3 velocity) {
        INTERNAL_CALL_InternalSetFocusPointForFrameWithNormalVelocity ( ref position, ref normal, ref velocity );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_InternalSetFocusPointForFrameWithNormalVelocity (ref Vector3 position, ref Vector3 normal, ref Vector3 velocity);
    public extern static bool IsDisplayOpaque
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

}


}
