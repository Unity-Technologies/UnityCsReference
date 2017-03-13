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

namespace UnityEngine
{



public sealed partial class Gizmos
{
    public static void DrawRay(Ray r)
        {
            Gizmos.DrawLine(r.origin, r.origin + r.direction);
        }
    
    
    public static void DrawRay(Vector3 from, Vector3 direction)
        {
            Gizmos.DrawLine(from, from + direction);
        }
    
    
    public static void DrawLine (Vector3 from, Vector3 to) {
        INTERNAL_CALL_DrawLine ( ref from, ref to );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_DrawLine (ref Vector3 from, ref Vector3 to);
    public static void DrawWireSphere (Vector3 center, float radius) {
        INTERNAL_CALL_DrawWireSphere ( ref center, radius );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_DrawWireSphere (ref Vector3 center, float radius);
    public static void DrawSphere (Vector3 center, float radius) {
        INTERNAL_CALL_DrawSphere ( ref center, radius );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_DrawSphere (ref Vector3 center, float radius);
    public static void DrawWireCube (Vector3 center, Vector3 size) {
        INTERNAL_CALL_DrawWireCube ( ref center, ref size );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_DrawWireCube (ref Vector3 center, ref Vector3 size);
    public static void DrawCube (Vector3 center, Vector3 size) {
        INTERNAL_CALL_DrawCube ( ref center, ref size );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_DrawCube (ref Vector3 center, ref Vector3 size);
    [uei.ExcludeFromDocs]
public static void DrawMesh (Mesh mesh, Vector3 position , Quaternion rotation ) {
    Vector3 scale = Vector3.one;
    DrawMesh ( mesh, position, rotation, scale );
}

[uei.ExcludeFromDocs]
public static void DrawMesh (Mesh mesh, Vector3 position ) {
    Vector3 scale = Vector3.one;
    Quaternion rotation = Quaternion.identity;
    DrawMesh ( mesh, position, rotation, scale );
}

[uei.ExcludeFromDocs]
public static void DrawMesh (Mesh mesh) {
    Vector3 scale = Vector3.one;
    Quaternion rotation = Quaternion.identity;
    Vector3 position = Vector3.zero;
    DrawMesh ( mesh, position, rotation, scale );
}

public static void DrawMesh(Mesh mesh, [uei.DefaultValue("Vector3.zero")]  Vector3 position , [uei.DefaultValue("Quaternion.identity")]  Quaternion rotation , [uei.DefaultValue("Vector3.one")]  Vector3 scale )
        {
            DrawMesh(mesh, -1, position, rotation, scale);
        }

    
    
    public static void DrawMesh (Mesh mesh, int submeshIndex, [uei.DefaultValue("Vector3.zero")]  Vector3 position , [uei.DefaultValue("Quaternion.identity")]  Quaternion rotation , [uei.DefaultValue("Vector3.one")]  Vector3 scale ) {
        INTERNAL_CALL_DrawMesh ( mesh, submeshIndex, ref position, ref rotation, ref scale );
    }

    [uei.ExcludeFromDocs]
    public static void DrawMesh (Mesh mesh, int submeshIndex, Vector3 position , Quaternion rotation ) {
        Vector3 scale = Vector3.one;
        INTERNAL_CALL_DrawMesh ( mesh, submeshIndex, ref position, ref rotation, ref scale );
    }

    [uei.ExcludeFromDocs]
    public static void DrawMesh (Mesh mesh, int submeshIndex, Vector3 position ) {
        Vector3 scale = Vector3.one;
        Quaternion rotation = Quaternion.identity;
        INTERNAL_CALL_DrawMesh ( mesh, submeshIndex, ref position, ref rotation, ref scale );
    }

    [uei.ExcludeFromDocs]
    public static void DrawMesh (Mesh mesh, int submeshIndex) {
        Vector3 scale = Vector3.one;
        Quaternion rotation = Quaternion.identity;
        Vector3 position = Vector3.zero;
        INTERNAL_CALL_DrawMesh ( mesh, submeshIndex, ref position, ref rotation, ref scale );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_DrawMesh (Mesh mesh, int submeshIndex, ref Vector3 position, ref Quaternion rotation, ref Vector3 scale);
    [uei.ExcludeFromDocs]
public static void DrawWireMesh (Mesh mesh, Vector3 position , Quaternion rotation ) {
    Vector3 scale = Vector3.one;
    DrawWireMesh ( mesh, position, rotation, scale );
}

[uei.ExcludeFromDocs]
public static void DrawWireMesh (Mesh mesh, Vector3 position ) {
    Vector3 scale = Vector3.one;
    Quaternion rotation = Quaternion.identity;
    DrawWireMesh ( mesh, position, rotation, scale );
}

[uei.ExcludeFromDocs]
public static void DrawWireMesh (Mesh mesh) {
    Vector3 scale = Vector3.one;
    Quaternion rotation = Quaternion.identity;
    Vector3 position = Vector3.zero;
    DrawWireMesh ( mesh, position, rotation, scale );
}

public static void DrawWireMesh(Mesh mesh, [uei.DefaultValue("Vector3.zero")]  Vector3 position , [uei.DefaultValue("Quaternion.identity")]  Quaternion rotation , [uei.DefaultValue("Vector3.one")]  Vector3 scale )
        {
            DrawWireMesh(mesh, -1, position, rotation, scale);
        }

    
    
    public static void DrawWireMesh (Mesh mesh, int submeshIndex, [uei.DefaultValue("Vector3.zero")]  Vector3 position , [uei.DefaultValue("Quaternion.identity")]  Quaternion rotation , [uei.DefaultValue("Vector3.one")]  Vector3 scale ) {
        INTERNAL_CALL_DrawWireMesh ( mesh, submeshIndex, ref position, ref rotation, ref scale );
    }

    [uei.ExcludeFromDocs]
    public static void DrawWireMesh (Mesh mesh, int submeshIndex, Vector3 position , Quaternion rotation ) {
        Vector3 scale = Vector3.one;
        INTERNAL_CALL_DrawWireMesh ( mesh, submeshIndex, ref position, ref rotation, ref scale );
    }

    [uei.ExcludeFromDocs]
    public static void DrawWireMesh (Mesh mesh, int submeshIndex, Vector3 position ) {
        Vector3 scale = Vector3.one;
        Quaternion rotation = Quaternion.identity;
        INTERNAL_CALL_DrawWireMesh ( mesh, submeshIndex, ref position, ref rotation, ref scale );
    }

    [uei.ExcludeFromDocs]
    public static void DrawWireMesh (Mesh mesh, int submeshIndex) {
        Vector3 scale = Vector3.one;
        Quaternion rotation = Quaternion.identity;
        Vector3 position = Vector3.zero;
        INTERNAL_CALL_DrawWireMesh ( mesh, submeshIndex, ref position, ref rotation, ref scale );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_DrawWireMesh (Mesh mesh, int submeshIndex, ref Vector3 position, ref Quaternion rotation, ref Vector3 scale);
    public static void DrawIcon (Vector3 center, string name, [uei.DefaultValue("true")]  bool allowScaling ) {
        INTERNAL_CALL_DrawIcon ( ref center, name, allowScaling );
    }

    [uei.ExcludeFromDocs]
    public static void DrawIcon (Vector3 center, string name) {
        bool allowScaling = true;
        INTERNAL_CALL_DrawIcon ( ref center, name, allowScaling );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_DrawIcon (ref Vector3 center, string name, bool allowScaling);
    [uei.ExcludeFromDocs]
public static void DrawGUITexture (Rect screenRect, Texture texture) {
    Material mat = null;
    DrawGUITexture ( screenRect, texture, mat );
}

public static void DrawGUITexture(Rect screenRect, Texture texture, [uei.DefaultValue("null")]  Material mat ) { DrawGUITexture(screenRect, texture, 0, 0, 0, 0, mat); }

    public static void DrawGUITexture (Rect screenRect, Texture texture, int leftBorder, int rightBorder, int topBorder, int bottomBorder, [uei.DefaultValue("null")]  Material mat ) {
        INTERNAL_CALL_DrawGUITexture ( ref screenRect, texture, leftBorder, rightBorder, topBorder, bottomBorder, mat );
    }

    [uei.ExcludeFromDocs]
    public static void DrawGUITexture (Rect screenRect, Texture texture, int leftBorder, int rightBorder, int topBorder, int bottomBorder) {
        Material mat = null;
        INTERNAL_CALL_DrawGUITexture ( ref screenRect, texture, leftBorder, rightBorder, topBorder, bottomBorder, mat );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_DrawGUITexture (ref Rect screenRect, Texture texture, int leftBorder, int rightBorder, int topBorder, int bottomBorder, Material mat);
    public static Color color
    {
        get { Color tmp; INTERNAL_get_color(out tmp); return tmp;  }
        set { INTERNAL_set_color(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_get_color (out Color value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_set_color (ref Color value) ;

    public static Matrix4x4 matrix
    {
        get { Matrix4x4 tmp; INTERNAL_get_matrix(out tmp); return tmp;  }
        set { INTERNAL_set_matrix(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_get_matrix (out Matrix4x4 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_set_matrix (ref Matrix4x4 value) ;

    public static void DrawFrustum (Vector3 center, float fov, float maxRange, float minRange, float aspect) {
        INTERNAL_CALL_DrawFrustum ( ref center, fov, maxRange, minRange, aspect );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_DrawFrustum (ref Vector3 center, float fov, float maxRange, float minRange, float aspect);
}

}
