// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using UnityEngine.XR;

namespace UnityEngine.Internal.VR
{


public static partial class VRTestMock
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void Reset () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void AddTrackedDevice (XRNode nodeType) ;

    public static void UpdateTrackedDevice (XRNode nodeType, Vector3 position, Quaternion rotation) {
        INTERNAL_CALL_UpdateTrackedDevice ( nodeType, ref position, ref rotation );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_UpdateTrackedDevice (XRNode nodeType, ref Vector3 position, ref Quaternion rotation);
    public static void UpdateLeftEye (Vector3 position, Quaternion rotation) {
        INTERNAL_CALL_UpdateLeftEye ( ref position, ref rotation );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_UpdateLeftEye (ref Vector3 position, ref Quaternion rotation);
    public static void UpdateRightEye (Vector3 position, Quaternion rotation) {
        INTERNAL_CALL_UpdateRightEye ( ref position, ref rotation );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_UpdateRightEye (ref Vector3 position, ref Quaternion rotation);
    public static void UpdateCenterEye (Vector3 position, Quaternion rotation) {
        INTERNAL_CALL_UpdateCenterEye ( ref position, ref rotation );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_UpdateCenterEye (ref Vector3 position, ref Quaternion rotation);
    public static void UpdateHead (Vector3 position, Quaternion rotation) {
        INTERNAL_CALL_UpdateHead ( ref position, ref rotation );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_UpdateHead (ref Vector3 position, ref Quaternion rotation);
    public static void UpdateLeftHand (Vector3 position, Quaternion rotation) {
        INTERNAL_CALL_UpdateLeftHand ( ref position, ref rotation );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_UpdateLeftHand (ref Vector3 position, ref Quaternion rotation);
    public static void UpdateRightHand (Vector3 position, Quaternion rotation) {
        INTERNAL_CALL_UpdateRightHand ( ref position, ref rotation );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_UpdateRightHand (ref Vector3 position, ref Quaternion rotation);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void AddController (string controllerName) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void UpdateControllerAxis (string controllerName, int axis, float value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void UpdateControllerButton (string controllerName, int button, bool pressed) ;

}


}
