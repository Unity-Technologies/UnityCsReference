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

namespace UnityEditor.Sprites
{
public sealed partial class SpriteUtility
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Texture2D GetSpriteTexture (Sprite sprite, bool getAtlasData) ;

    [System.Obsolete ("Use Sprite.vertices API instead. This data is the same for packed and unpacked sprites.")]
static public Vector2[] GetSpriteMesh(Sprite sprite, bool getAtlasData)
        {
            return sprite.vertices;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Vector2[] GetSpriteUVs (Sprite sprite, bool getAtlasData) ;

    [System.Obsolete ("Use Sprite.triangles API instead. This data is the same for packed and unpacked sprites.")]
static public UInt16[] GetSpriteIndices(Sprite sprite, bool getAtlasData)
        {
            return sprite.triangles;
        }
    
    
    internal static void GenerateOutline (Texture2D texture, Rect rect, float detail, byte alphaTolerance, bool holeDetection, out Vector2[][] paths) {
        INTERNAL_CALL_GenerateOutline ( texture, ref rect, detail, alphaTolerance, holeDetection, out paths );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GenerateOutline (Texture2D texture, ref Rect rect, float detail, byte alphaTolerance, bool holeDetection, out Vector2[][] paths);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void GenerateOutlineFromSprite (Sprite sprite, float detail, byte alphaTolerance, bool holeDetection, out Vector2[][] paths) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  Vector2[] GeneratePolygonOutlineVerticesOfSize (int sides, int width, int height) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void CreateSpritePolygonAssetAtPath (string pathName, int sides) ;

}

[System.Obsolete ("Use UnityEditor.Sprites.SpriteUtility instead (UnityUpgradable)", true)]
public sealed partial class DataUtility
{
}

}

namespace UnityEditorInternal
{
public sealed partial class InternalSpriteUtility
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Rect[] GenerateAutomaticSpriteRectangles (Texture2D texture, int minRectSize, int extrudeSize) ;

    public static Rect[] GenerateGridSpriteRectangles (Texture2D texture, Vector2 offset, Vector2 size, Vector2 padding) {
        return INTERNAL_CALL_GenerateGridSpriteRectangles ( texture, ref offset, ref size, ref padding );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Rect[] INTERNAL_CALL_GenerateGridSpriteRectangles (Texture2D texture, ref Vector2 offset, ref Vector2 size, ref Vector2 padding);
}

internal static partial class SpriteExtensions
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  Texture GetTextureForPlayMode (this Sprite sprite) ;

}


}



namespace UnityEditor.Experimental.U2D
{
    internal static class SpriteUtility
    {
        public static void GenerateOutline(Texture2D texture, Rect rect, float detail, byte alphaTolerance, bool holeDetection, out Vector2[][] paths)
        {
            UnityEditor.Sprites.SpriteUtility.GenerateOutline(texture, rect, detail, alphaTolerance, holeDetection, out paths);
        }

    }

}
