// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using UnityEngine;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEditor
{
internal sealed partial class PolygonEditor
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void StartEditing (Collider2D collider) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void ApplyEditing (Collider2D collider) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void StopEditing () ;

    public static bool GetNearestPoint (Vector2 point, out int pathIndex, out int pointIndex, out float distance) {
        return INTERNAL_CALL_GetNearestPoint ( ref point, out pathIndex, out pointIndex, out distance );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_GetNearestPoint (ref Vector2 point, out int pathIndex, out int pointIndex, out float distance);
    public static bool GetNearestEdge (Vector2 point, out int pathIndex, out int pointIndex0, out int pointIndex1, out float distance, bool loop) {
        return INTERNAL_CALL_GetNearestEdge ( ref point, out pathIndex, out pointIndex0, out pointIndex1, out distance, loop );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_GetNearestEdge (ref Vector2 point, out int pathIndex, out int pointIndex0, out int pointIndex1, out float distance, bool loop);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetPathCount () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetPointCount (int pathIndex) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool GetPoint (int pathIndex, int pointIndex, out Vector2 point) ;

    public static void SetPoint (int pathIndex, int pointIndex, Vector2 value) {
        INTERNAL_CALL_SetPoint ( pathIndex, pointIndex, ref value );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetPoint (int pathIndex, int pointIndex, ref Vector2 value);
    public static void InsertPoint (int pathIndex, int pointIndex, Vector2 value) {
        INTERNAL_CALL_InsertPoint ( pathIndex, pointIndex, ref value );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_InsertPoint (int pathIndex, int pointIndex, ref Vector2 value);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void RemovePoint (int pathIndex, int pointIndex) ;

    public static void TestPointMove (int pathIndex, int pointIndex, Vector2 movePosition, out bool leftIntersect, out bool rightIntersect, bool loop) {
        INTERNAL_CALL_TestPointMove ( pathIndex, pointIndex, ref movePosition, out leftIntersect, out rightIntersect, loop );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_TestPointMove (int pathIndex, int pointIndex, ref Vector2 movePosition, out bool leftIntersect, out bool rightIntersect, bool loop);
}

}
