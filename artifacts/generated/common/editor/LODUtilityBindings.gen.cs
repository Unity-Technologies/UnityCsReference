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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;

namespace UnityEditor
{
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
internal partial struct LODVisualizationInformation
{
    public int triangleCount;
    public int vertexCount;
    public int rendererCount;
    public int submeshCount;
    
    
    public int   activeLODLevel;
    public float activeLODFade;
    public float activeDistance;
    public float activeRelativeScreenSize;
    public float activePixelSize;
    public float worldSpaceSize;
}

public sealed partial class LODUtility
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  LODVisualizationInformation CalculateVisualizationData (Camera camera, LODGroup group, int lodLevel) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  float CalculateDistance (Camera camera, float relativeScreenHeight, LODGroup group) ;

    internal static Vector3 CalculateWorldReferencePoint (LODGroup group) {
        Vector3 result;
        INTERNAL_CALL_CalculateWorldReferencePoint ( group, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_CalculateWorldReferencePoint (LODGroup group, out Vector3 value);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool NeedUpdateLODGroupBoundingBox (LODGroup group) ;

    public static void CalculateLODGroupBoundingBox(LODGroup group)
        {
            if (group == null)
                throw new ArgumentNullException("group");
            group.RecalculateBounds();
        }
    
    
}

}
