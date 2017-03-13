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
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.AI
{


[MovedFrom("UnityEngine")]
public enum NavMeshPathStatus
{
    
    PathComplete = 0,
    
    PathPartial = 1,
    
    PathInvalid = 2
}

[StructLayout(LayoutKind.Sequential)]
[MovedFrom("UnityEngine")]
public sealed partial class NavMeshPath
{
    internal IntPtr m_Ptr;
    internal Vector3[] m_corners;
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public NavMeshPath () ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void DestroyNavMeshPath () ;

    ~NavMeshPath()
        {
            DestroyNavMeshPath();
            m_Ptr = IntPtr.Zero;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public int GetCornersNonAlloc (Vector3[] results) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private Vector3[] CalculateCornersInternal () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void ClearCornersInternal () ;

    public void ClearCorners()
        {
            ClearCornersInternal();
            m_corners = null;
        }
    
    
    private void CalculateCorners()
        {
            if (m_corners == null)
                m_corners = CalculateCornersInternal();
        }
    
    
    public Vector3[] corners { get { CalculateCorners(); return m_corners; } }
    
    
    public extern  NavMeshPathStatus status
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

}

}
