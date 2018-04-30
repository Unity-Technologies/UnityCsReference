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
using System.Collections;

namespace UnityEngine
{
public enum SpriteAlignment
{
    Center = 0,
    TopLeft = 1,
    TopCenter = 2,
    TopRight = 3,
    LeftCenter = 4,
    RightCenter = 5,
    BottomLeft = 6,
    BottomCenter = 7,
    BottomRight = 8,
    Custom = 9,
}

public enum SpritePackingMode
{
    Tight = 0,
    Rectangle
}

public enum SpritePackingRotation
{
    None = 0,
    
    Any = 15
}

public enum SpriteMeshType
{
    FullRect = 0,
    Tight = 1
}

public enum SpriteDrawMode
{
    Simple,
    Sliced,
    Tiled
}

public enum SpriteTileMode
{
    Continuous,
    Adaptive
}

public enum SpriteMaskInteraction
{
    None = 0,
    VisibleInsideMask = 1,
    VisibleOutsideMask = 2
}

public sealed partial class Sprite : Object
{
    public static Sprite Create (Texture2D texture, Rect rect, Vector2 pivot, [uei.DefaultValue("100.0f")]  float pixelsPerUnit , [uei.DefaultValue("0")]  uint extrude , [uei.DefaultValue("SpriteMeshType.Tight")]  SpriteMeshType meshType , [uei.DefaultValue("Vector4.zero")]  Vector4 border , [uei.DefaultValue("false")]  bool generateFallbackPhysicsShape ) {
        return INTERNAL_CALL_Create ( texture, ref rect, ref pivot, pixelsPerUnit, extrude, meshType, ref border, generateFallbackPhysicsShape );
    }

    [uei.ExcludeFromDocs]
    public static Sprite Create (Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit , uint extrude , SpriteMeshType meshType , Vector4 border ) {
        bool generateFallbackPhysicsShape = false;
        return INTERNAL_CALL_Create ( texture, ref rect, ref pivot, pixelsPerUnit, extrude, meshType, ref border, generateFallbackPhysicsShape );
    }

    [uei.ExcludeFromDocs]
    public static Sprite Create (Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit , uint extrude , SpriteMeshType meshType ) {
        bool generateFallbackPhysicsShape = false;
        Vector4 border = Vector4.zero;
        return INTERNAL_CALL_Create ( texture, ref rect, ref pivot, pixelsPerUnit, extrude, meshType, ref border, generateFallbackPhysicsShape );
    }

    [uei.ExcludeFromDocs]
    public static Sprite Create (Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit , uint extrude ) {
        bool generateFallbackPhysicsShape = false;
        Vector4 border = Vector4.zero;
        SpriteMeshType meshType = SpriteMeshType.Tight;
        return INTERNAL_CALL_Create ( texture, ref rect, ref pivot, pixelsPerUnit, extrude, meshType, ref border, generateFallbackPhysicsShape );
    }

    [uei.ExcludeFromDocs]
    public static Sprite Create (Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit ) {
        bool generateFallbackPhysicsShape = false;
        Vector4 border = Vector4.zero;
        SpriteMeshType meshType = SpriteMeshType.Tight;
        uint extrude = 0;
        return INTERNAL_CALL_Create ( texture, ref rect, ref pivot, pixelsPerUnit, extrude, meshType, ref border, generateFallbackPhysicsShape );
    }

    [uei.ExcludeFromDocs]
    public static Sprite Create (Texture2D texture, Rect rect, Vector2 pivot) {
        bool generateFallbackPhysicsShape = false;
        Vector4 border = Vector4.zero;
        SpriteMeshType meshType = SpriteMeshType.Tight;
        uint extrude = 0;
        float pixelsPerUnit = 100.0f;
        return INTERNAL_CALL_Create ( texture, ref rect, ref pivot, pixelsPerUnit, extrude, meshType, ref border, generateFallbackPhysicsShape );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Sprite INTERNAL_CALL_Create (Texture2D texture, ref Rect rect, ref Vector2 pivot, float pixelsPerUnit, uint extrude, SpriteMeshType meshType, ref Vector4 border, bool generateFallbackPhysicsShape);
    public  Bounds bounds
    {
        get { Bounds tmp; INTERNAL_get_bounds(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_bounds (out Bounds value) ;


    public  Rect rect
    {
        get { Rect tmp; INTERNAL_get_rect(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_rect (out Rect value) ;


    public extern  Texture2D texture
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  Texture2D associatedAlphaSplitTexture
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public  Rect textureRect
    {
        get { Rect tmp; INTERNAL_get_textureRect(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_textureRect (out Rect value) ;


    public Vector2 textureRectOffset
        {
            get
            {
                Vector2 v;
                Internal_GetTextureRectOffset(this, out v);
                return v;
            }
        }
    
    
    public extern  bool packed
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  SpritePackingMode packingMode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  SpritePackingRotation packingRotation
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_GetTextureRectOffset (Sprite sprite, out Vector2 output) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_GetPivot (Sprite sprite, out Vector2 output) ;

    public Vector2 pivot
        {
            get
            {
                Vector2 v;
                Internal_GetPivot(this, out v);
                return v;
            }
        }
    
    
    public  Vector4 border
    {
        get { Vector4 tmp; INTERNAL_get_border(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_border (out Vector4 value) ;


    public extern  Vector2[] vertices
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  UInt16[] triangles
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  Vector2[] uv
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void OverrideGeometry (Vector2[] vertices, UInt16[] triangles) ;

}

[RequireComponent(typeof(Transform))]
public sealed partial class SpriteRenderer : Renderer
{
    
            public Sprite sprite
        {
            get
            {
                return GetSprite_INTERNAL();
            }
            set
            {
                SetSprite_INTERNAL(value);
            }
        }
    
    
    public extern SpriteDrawMode drawMode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    internal extern  bool shouldSupportTiling
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public Vector2 size
    {
        get { Vector2 tmp; INTERNAL_get_size(out tmp); return tmp;  }
        set { INTERNAL_set_size(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_size (out Vector2 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_size (ref Vector2 value) ;

    public extern float adaptiveModeThreshold
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern SpriteTileMode tileMode
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
    extern private Sprite GetSprite_INTERNAL () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void SetSprite_INTERNAL (Sprite sprite) ;

    public Color color
    {
        get { Color tmp; INTERNAL_get_color(out tmp); return tmp;  }
        set { INTERNAL_set_color(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_color (out Color value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_color (ref Color value) ;

    public extern bool flipX
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool flipY
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern SpriteMaskInteraction maskInteraction
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    internal Bounds GetSpriteBounds () {
        Bounds result;
        INTERNAL_CALL_GetSpriteBounds ( this, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetSpriteBounds (SpriteRenderer self, out Bounds value);
}


}

namespace UnityEngine.Sprites
{
public sealed partial class DataUtility
{
    public static Vector4 GetInnerUV (Sprite sprite) {
        Vector4 result;
        INTERNAL_CALL_GetInnerUV ( sprite, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetInnerUV (Sprite sprite, out Vector4 value);
    public static Vector4 GetOuterUV (Sprite sprite) {
        Vector4 result;
        INTERNAL_CALL_GetOuterUV ( sprite, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetOuterUV (Sprite sprite, out Vector4 value);
    public static Vector4 GetPadding (Sprite sprite) {
        Vector4 result;
        INTERNAL_CALL_GetPadding ( sprite, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetPadding (Sprite sprite, out Vector4 value);
    static public Vector2 GetMinSize(Sprite sprite)
        {
            Vector2 v;
            Internal_GetMinSize(sprite, out v);
            return v;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_GetMinSize (Sprite sprite, out Vector2 output) ;

}

}
