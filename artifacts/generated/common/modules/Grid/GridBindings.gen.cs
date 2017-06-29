// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;


using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine
{
public partial class GridLayout : Behaviour
{
    public enum CellLayout { Rectangle = 0 }
    public enum CellSwizzle { XYZ = 0, XZY = 1, YXZ = 2, YZX = 3, ZXY = 4, ZYX = 5 }
    
    
    public  Vector3 cellSize
    {
        get { Vector3 tmp; INTERNAL_get_cellSize(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_cellSize (out Vector3 value) ;


    public  Vector3 cellGap
    {
        get { Vector3 tmp; INTERNAL_get_cellGap(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_cellGap (out Vector3 value) ;


    public extern GridLayout.CellLayout cellLayout
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern GridLayout.CellSwizzle cellSwizzle
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public Vector3 CellToLocal (Vector3Int cellPosition) {
        Vector3 result;
        INTERNAL_CALL_CellToLocal ( this, ref cellPosition, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_CellToLocal (GridLayout self, ref Vector3Int cellPosition, out Vector3 value);
    public Vector3Int LocalToCell (Vector3 localPosition) {
        Vector3Int result;
        INTERNAL_CALL_LocalToCell ( this, ref localPosition, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_LocalToCell (GridLayout self, ref Vector3 localPosition, out Vector3Int value);
    public Vector3 CellToLocalInterpolated (Vector3 cellPosition) {
        Vector3 result;
        INTERNAL_CALL_CellToLocalInterpolated ( this, ref cellPosition, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_CellToLocalInterpolated (GridLayout self, ref Vector3 cellPosition, out Vector3 value);
    public Vector3 LocalToCellInterpolated (Vector3 localPosition) {
        Vector3 result;
        INTERNAL_CALL_LocalToCellInterpolated ( this, ref localPosition, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_LocalToCellInterpolated (GridLayout self, ref Vector3 localPosition, out Vector3 value);
    public Vector3 CellToWorld (Vector3Int cellPosition) {
        Vector3 result;
        INTERNAL_CALL_CellToWorld ( this, ref cellPosition, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_CellToWorld (GridLayout self, ref Vector3Int cellPosition, out Vector3 value);
    public Vector3Int WorldToCell (Vector3 worldPosition) {
        Vector3Int result;
        INTERNAL_CALL_WorldToCell ( this, ref worldPosition, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_WorldToCell (GridLayout self, ref Vector3 worldPosition, out Vector3Int value);
    public Vector3 LocalToWorld (Vector3 localPosition) {
        Vector3 result;
        INTERNAL_CALL_LocalToWorld ( this, ref localPosition, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_LocalToWorld (GridLayout self, ref Vector3 localPosition, out Vector3 value);
    public Vector3 WorldToLocal (Vector3 worldPosition) {
        Vector3 result;
        INTERNAL_CALL_WorldToLocal ( this, ref worldPosition, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_WorldToLocal (GridLayout self, ref Vector3 worldPosition, out Vector3 value);
    public Bounds GetBoundsLocal (Vector3Int cellPosition) {
        Bounds result;
        INTERNAL_CALL_GetBoundsLocal ( this, ref cellPosition, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetBoundsLocal (GridLayout self, ref Vector3Int cellPosition, out Bounds value);
    public Vector3 GetLayoutCellCenter () {
        Vector3 result;
        INTERNAL_CALL_GetLayoutCellCenter ( this, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetLayoutCellCenter (GridLayout self, out Vector3 value);
}

[RequireComponent(typeof(Transform))]
public sealed partial class Grid : GridLayout
{
    public new Vector3 cellSize
    {
        get { Vector3 tmp; INTERNAL_get_cellSize(out tmp); return tmp;  }
        set { INTERNAL_set_cellSize(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_cellSize (out Vector3 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_set_cellSize (ref Vector3 value) ;

    public new Vector3 cellGap
    {
        get { Vector3 tmp; INTERNAL_get_cellGap(out tmp); return tmp;  }
        set { INTERNAL_set_cellGap(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_cellGap (out Vector3 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_set_cellGap (ref Vector3 value) ;

    public extern new  GridLayout.CellLayout cellLayout
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern new  GridLayout.CellSwizzle cellSwizzle
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public static Vector3 Swizzle (GridLayout.CellSwizzle swizzle, Vector3 position) {
        Vector3 result;
        INTERNAL_CALL_Swizzle ( swizzle, ref position, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Swizzle (GridLayout.CellSwizzle swizzle, ref Vector3 position, out Vector3 value);
    public static Vector3 InverseSwizzle (GridLayout.CellSwizzle swizzle, Vector3 position) {
        Vector3 result;
        INTERNAL_CALL_InverseSwizzle ( swizzle, ref position, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_InverseSwizzle (GridLayout.CellSwizzle swizzle, ref Vector3 position, out Vector3 value);
}


}
