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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.XR.WSA;

#pragma warning disable 649

namespace UnityEngine.XR.WSA
{



[MovedFrom("UnityEngine.VR.WSA")]
[RequireComponent(typeof(Transform))]
public sealed partial class WorldAnchor : Component
{
    private WorldAnchor() {}
    
    
    public delegate void OnTrackingChangedDelegate(WorldAnchor self, bool located);
    public event OnTrackingChangedDelegate OnTrackingChanged;
    
    
    public bool isLocated { get { return IsLocated_Internal(); } }
    
    
    public void SetNativeSpatialAnchorPtr(IntPtr spatialAnchorPtr)
        {
        }
    
    
    public IntPtr GetNativeSpatialAnchorPtr()
        {
            return IntPtr.Zero;
        }
    
    
    private bool IsLocated_Internal () {
        return INTERNAL_CALL_IsLocated_Internal ( this );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_IsLocated_Internal (WorldAnchor self);
    [RequiredByNativeCode]
    private static void Internal_TriggerEventOnTrackingLost(WorldAnchor self, bool located)
        {
            if (self != null && self.OnTrackingChanged != null)
            {
                self.OnTrackingChanged(self, located);
            }
        }
    
    
}


}

namespace UnityEngine.XR.WSA.Persistence
{
public sealed partial class WorldAnchorStore
{
}


}

namespace UnityEngine.XR.WSA.Sharing
{
public sealed partial class WorldAnchorTransferBatch
{
}


}
