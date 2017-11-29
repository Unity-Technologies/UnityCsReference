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

namespace UnityEngine
{


[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct BoundingSphere
{
    public Vector3    position;
    public float      radius;
    
    
    public BoundingSphere(Vector3 pos, float rad) { position = pos; radius = rad; }
    public BoundingSphere(Vector4 packedSphere) { position = new Vector3(packedSphere.x, packedSphere.y, packedSphere.z); radius = packedSphere.w; }
}

internal enum CullingQueryOptions
{
    Normal = 0,
    IgnoreVisibility = 1,
    IgnoreDistance = 2
}

[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct CullingGroupEvent
{
    private int m_Index;
    private byte m_PrevState;
    private byte m_ThisState;
    
    
    public int index { get { return m_Index; } }
    
    
    private const byte kIsVisibleMask = 1 << 7;
    private const byte kDistanceMask = (1 << 7) - 1;
    
    
    public bool isVisible             { get { return (m_ThisState & kIsVisibleMask) != 0; } }
    public bool wasVisible            { get { return (m_PrevState & kIsVisibleMask) != 0; } }
    
    
    public bool hasBecomeVisible      { get { return isVisible && !wasVisible; } }
    public bool hasBecomeInvisible    { get { return !isVisible && wasVisible; } }
    
    
    public int currentDistance        { get { return m_ThisState & kDistanceMask; } }
    public int previousDistance       { get { return m_PrevState & kDistanceMask; } }
}

[StructLayout(LayoutKind.Sequential)]
public sealed partial class CullingGroup : IDisposable
{
    internal IntPtr m_Ptr;
    
    
    public delegate void StateChanged(CullingGroupEvent sphere);
    
    
    public CullingGroup()
        {
            Init();
        }
    
    
    ~CullingGroup()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                FinalizerFailure();
            }
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void Dispose () ;

    
            public StateChanged onStateChanged
        {
            get { return m_OnStateChanged; }
            set { m_OnStateChanged = value; }
        }
    
    
    public extern bool enabled
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern Camera targetCamera
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetBoundingSpheres (BoundingSphere[] array) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetBoundingSphereCount (int count) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void EraseSwapBack (int index) ;

    public static void EraseSwapBack<T>(int index, T[] myArray, ref int size)
        {
            size--;
            myArray[index] = myArray[size];
        }
    
    
    public int QueryIndices(bool visible, int[] result, int firstIndex)
        {
            return QueryIndices(visible, -1, CullingQueryOptions.IgnoreDistance, result, firstIndex);
        }
    
    
    public int QueryIndices(int distanceIndex, int[] result, int firstIndex)
        {
            return QueryIndices(false, distanceIndex, CullingQueryOptions.IgnoreVisibility, result, firstIndex);
        }
    
    
    public int QueryIndices(bool visible, int distanceIndex, int[] result, int firstIndex)
        {
            return QueryIndices(visible, distanceIndex, CullingQueryOptions.Normal, result, firstIndex);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private int QueryIndices (bool visible, int distanceIndex, CullingQueryOptions options, int[] result, int firstIndex) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public bool IsVisible (int index) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public int GetDistance (int index) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetBoundingDistances (float[] distances) ;

    public void SetDistanceReferencePoint (Vector3 point) {
        INTERNAL_CALL_SetDistanceReferencePoint ( this, ref point );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetDistanceReferencePoint (CullingGroup self, ref Vector3 point);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetDistanceReferencePoint (Transform transform) ;

    private StateChanged m_OnStateChanged = null;
    
    
    [System.Security.SecuritySafeCritical]
    [RequiredByNativeCode]
    unsafe private static void SendEvents(CullingGroup cullingGroup, IntPtr eventsPtr, int count)
        {
            CullingGroupEvent* events = (CullingGroupEvent*)eventsPtr.ToPointer();
            if (cullingGroup.m_OnStateChanged == null)
                return;

            for (int i = 0; i < count; ++i)
                cullingGroup.m_OnStateChanged(events[i]);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Init () ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void FinalizerFailure () ;

}


}
