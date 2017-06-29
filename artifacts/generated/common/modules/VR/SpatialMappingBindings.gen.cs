// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.XR.WSA;

namespace UnityEngine.XR.WSA
{


public sealed partial class SurfaceObserver : IDisposable
{
    private IntPtr m_Observer;
    
    
    public void Dispose()
        {
            if (m_Observer != IntPtr.Zero)
            {
                Destroy(m_Observer);
                m_Observer = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }
    
    
    private IntPtr Internal_Create () {
        IntPtr result;
        INTERNAL_CALL_Internal_Create ( this, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_Create (SurfaceObserver self, out IntPtr value);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Destroy (IntPtr observer) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void DestroyThreaded (IntPtr observer) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_Update (IntPtr observer, SurfaceChangedDelegate onSurfaceChanged) ;

    private void Internal_SetVolumeAsAxisAlignedBox (IntPtr observer, Vector3 origin, Vector3 extents) {
        INTERNAL_CALL_Internal_SetVolumeAsAxisAlignedBox ( this, observer, ref origin, ref extents );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_SetVolumeAsAxisAlignedBox (SurfaceObserver self, IntPtr observer, ref Vector3 origin, ref Vector3 extents);
    private void Internal_SetVolumeAsOrientedBox (IntPtr observer, Vector3 origin, Vector3 extents, Quaternion orientation) {
        INTERNAL_CALL_Internal_SetVolumeAsOrientedBox ( this, observer, ref origin, ref extents, ref orientation );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_SetVolumeAsOrientedBox (SurfaceObserver self, IntPtr observer, ref Vector3 origin, ref Vector3 extents, ref Quaternion orientation);
    private void Internal_SetVolumeAsSphere (IntPtr observer, Vector3 origin, float radiusMeters) {
        INTERNAL_CALL_Internal_SetVolumeAsSphere ( this, observer, ref origin, radiusMeters );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_SetVolumeAsSphere (SurfaceObserver self, IntPtr observer, ref Vector3 origin, float radiusMeters);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Internal_SetVolumeAsFrustum (IntPtr observer, Plane[] planes) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool Internal_AddToWorkQueue (IntPtr observer, SurfaceDataReadyDelegate onDataReady, int surfaceId, MeshFilter filter, WorldAnchor wa, MeshCollider mc, float trisPerCubicMeter, bool createColliderData) ;

}



}
