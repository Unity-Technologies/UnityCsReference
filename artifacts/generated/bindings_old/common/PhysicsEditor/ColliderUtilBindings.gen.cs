// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine;

namespace UnityEditor
{


internal sealed partial class ColliderUtil
{
    public static Vector3 GetCapsuleExtents (CapsuleCollider cc) {
        Vector3 result;
        INTERNAL_CALL_GetCapsuleExtents ( cc, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetCapsuleExtents (CapsuleCollider cc, out Vector3 value);
    public static Matrix4x4 CalculateCapsuleTransform (CapsuleCollider cc) {
        Matrix4x4 result;
        INTERNAL_CALL_CalculateCapsuleTransform ( cc, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_CalculateCapsuleTransform (CapsuleCollider cc, out Matrix4x4 value);
}

}
