// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Sprites
{
    [NativeHeader("Editor/Mono/SpritesEditor.bindings.h")]
    [StaticAccessor("SpriteUtilityBindings", StaticAccessorType.DoubleColon)]
    public sealed class SpriteUtility
    {
        [NativeThrows]
        extern public static Texture2D GetSpriteTexture([NotNull] Sprite sprite, bool getAtlasData);

        [System.Obsolete("Use Sprite.vertices API instead. This data is the same for packed and unpacked sprites.")]
        static public Vector2[] GetSpriteMesh(Sprite sprite, bool getAtlasData) { return sprite.vertices; }

        [NativeThrows]
        extern public static Vector2[] GetSpriteUVs([NotNull] Sprite sprite, bool getAtlasData);

        [System.Obsolete("Use Sprite.triangles API instead. This data is the same for packed and unpacked sprites.")]
        static public ushort[] GetSpriteIndices(Sprite sprite, bool getAtlasData) { return sprite.triangles; }

        internal static void GenerateOutline(Texture2D texture, Rect rect, float detail, byte alphaTolerance, bool holeDetection, out Vector2[][] paths)
        {
            var res = GenerateOutlineImpl(texture, ref rect, detail, alphaTolerance, holeDetection);
            paths = (Vector2[][])res;
        }

        extern private static System.Object GenerateOutlineImpl([NotNull] Texture2D texture, ref Rect rect, float detail, byte alphaTolerance, bool holeDetection);

        internal static void GenerateOutlineFromSprite(Sprite sprite, float detail, byte alphaTolerance, bool holeDetection, out Vector2[][] paths)
        {
            var res = GenerateOutlineFromSpriteImpl(sprite, detail, alphaTolerance, holeDetection);
            paths = (Vector2[][])res;
        }

        extern private static System.Object GenerateOutlineFromSpriteImpl([NotNull] Sprite sprite, float detail, byte alphaTolerance, bool holeDetection);

        extern internal static Vector2[] GeneratePolygonOutlineVerticesOfSize(int sides, int width, int height);

        [FreeFunction("::CreateSpriteForPrimitive")]
        extern internal static void CreateSpritePolygonAssetAtPath(string pathName, int sides);
    }

    // Class renamed to UnityEditor.Sprites.SpriteUtility. Empty dummy class left here for scriptupdater.
    [System.Obsolete("Use UnityEditor.Sprites.SpriteUtility instead (UnityUpgradable)", true)]
    public sealed partial class DataUtility
    {
    }
}

namespace UnityEditorInternal
{
    [StaticAccessor("SpriteUtilityBindings", StaticAccessorType.DoubleColon)]
    public sealed class InternalSpriteUtility
    {
        extern public static Rect[] GenerateAutomaticSpriteRectangles([NotNull] Texture2D texture, int minRectSize, int extrudeSize);
        extern public static Rect[] GenerateGridSpriteRectangles([NotNull] Texture2D texture, Vector2 offset, Vector2 size, Vector2 padding, bool keepEmptyRects);

        public static Rect[] GenerateGridSpriteRectangles(Texture2D texture, Vector2 offset, Vector2 size, Vector2 padding)
        {
            return GenerateGridSpriteRectangles(texture, offset, size, padding, false);
        }
    }

    [StaticAccessor("SpriteUtilityBindings", StaticAccessorType.DoubleColon)]
    internal static class SpriteExtensions
    {
        extern internal static Texture GetTextureForPlayMode([NotNull] this Sprite sprite);
    }
}
