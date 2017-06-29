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
public enum PositionalLocatorState
{
    Unavailable       = 0,
    OrientationOnly   = 1,
    Activating        = 2,
    Active            = 3,
    Inhibited         = 4
}

[MovedFrom("UnityEngine.VR.WSA")]
public sealed partial class WorldManager
{
    public delegate void OnPositionalLocatorStateChangedDelegate(PositionalLocatorState oldState, PositionalLocatorState newState);
    
    
    public static event OnPositionalLocatorStateChangedDelegate OnPositionalLocatorStateChanged;
    
    
    [RequiredByNativeCode]
    private static void Internal_TriggerPositionalLocatorStateChanged(PositionalLocatorState oldState, PositionalLocatorState newState)
        {
            if (OnPositionalLocatorStateChanged != null)
                OnPositionalLocatorStateChanged(oldState, newState);
        }
    
    
    public extern static PositionalLocatorState state
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public static IntPtr GetNativeISpatialCoordinateSystemPtr () {
        IntPtr result;
        INTERNAL_CALL_GetNativeISpatialCoordinateSystemPtr ( out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetNativeISpatialCoordinateSystemPtr (out IntPtr value);
}


}
