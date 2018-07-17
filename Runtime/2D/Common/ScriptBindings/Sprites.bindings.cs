// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    public enum SpriteMeshType
    {
        FullRect = 0,
        Tight = 1
    }

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
        FlipHorizontal = 1,
        FlipVertical = 2,
        Rotate180 = 3,
        Any = 15
    }

    public enum SpriteSortPoint
    {
        Center = 0,
        Pivot = 1
    }

    [NativeHeader("Runtime/2D/Common/ScriptBindings/SpritesMarshalling.h")]
    [NativeHeader("Runtime/2D/Common/SpriteDataAccess.h")]
    [NativeHeader("Runtime/Graphics/SpriteUtility.h")]
    [NativeType("Runtime/Graphics/SpriteFrame.h")]
    [ExcludeFromPreset]
    public sealed partial class Sprite : Object
    {
        [RequiredByNativeCode] // Used by Unity splash screen.
        private Sprite() {}
        internal extern int GetPackingMode();
        internal extern int GetPackingRotation();
        internal extern int GetPacked();
        internal extern Rect GetTextureRect();
        internal extern Vector2 GetTextureRectOffset();
        internal extern Vector4 GetInnerUVs();
        internal extern Vector4 GetOuterUVs();
        internal extern Vector4 GetPadding();

        // Workaround for Overloads as described in
        [FreeFunction("SpritesBindings::CreateSpriteWithoutTextureScripting")]
        internal extern static Sprite CreateSpriteWithoutTextureScripting(Rect rect, Vector2 pivot, float pixelsToUnits, Texture2D texture);

        [FreeFunction("SpritesBindings::CreateSprite")]
        internal extern static Sprite CreateSprite(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit, uint extrude, SpriteMeshType meshType, Vector4 border, bool generateFallbackPhysicsShape);

        public extern Bounds bounds
        {
            get;
        }

        public extern Rect rect
        {
            get;
        }

        public extern Vector4 border
        {
            get;
        }

        public extern Texture2D texture
        {
            get;
        }

        // The number of pixels in one unit. Note: The C++ side still uses the name pixelsToUnits which is misleading,
        // but has not been changed yet to minimize merge conflicts.
        public extern float pixelsPerUnit
        {
            [NativeMethod("GetPixelsToUnits")]
            get;
        }

        public extern Texture2D associatedAlphaSplitTexture
        {
            [NativeMethod("GetAlphaTexture")]
            get;
        }

        public extern Vector2 pivot
        {
            [NativeMethod("GetPivotInPixels")]
            get;
        }

        public bool packed
        {
            get
            {
                return GetPacked() == 1;
            }
        }

        public SpritePackingMode packingMode
        {
            get
            {
                return (SpritePackingMode)GetPackingMode();
            }
        }

        public SpritePackingRotation packingRotation
        {
            get
            {
                return (SpritePackingRotation)GetPackingRotation();
            }
        }

        public Rect textureRect
        {
            get
            {
                if (packed && (SpritePackingMode)packingMode != SpritePackingMode.Rectangle)
                    return Rect.zero;
                return GetTextureRect();
            }
        }

        public Vector2 textureRectOffset
        {
            get
            {
                if (packed && (SpritePackingMode)packingMode != SpritePackingMode.Rectangle)
                    return Vector2.zero;
                return GetTextureRectOffset();
            }
        }

        public extern Vector2[] vertices
        {
            [FreeFunction("SpriteAccessLegacy::GetSpriteVertices", HasExplicitThis = true)]
            get;
        }

        public extern UInt16[] triangles
        {
            [FreeFunction("SpriteAccessLegacy::GetSpriteIndices", HasExplicitThis = true)]
            get;
        }

        public extern Vector2[] uv
        {
            [FreeFunction("SpriteAccessLegacy::GetSpriteUVs", HasExplicitThis = true)]
            get;
        }

        public extern int GetPhysicsShapeCount();

        public int GetPhysicsShapePointCount(int shapeIdx)
        {
            int physicsShapeCount = GetPhysicsShapeCount();
            if (shapeIdx < 0 || shapeIdx >= physicsShapeCount)
                throw new IndexOutOfRangeException(String.Format("Index({0}) is out of bounds(0 - {1})", shapeIdx, physicsShapeCount - 1));

            return Internal_GetPhysicsShapePointCount(shapeIdx);
        }

        [NativeMethod("GetPhysicsShapePointCount")]
        private extern int Internal_GetPhysicsShapePointCount(int shapeIdx);

        public int GetPhysicsShape(int shapeIdx, List<Vector2> physicsShape)
        {
            int physicsShapeCount = GetPhysicsShapeCount();
            if (shapeIdx < 0 || shapeIdx >= physicsShapeCount)
                throw new IndexOutOfRangeException(String.Format("Index({0}) is out of bounds(0 - {1})", shapeIdx, physicsShapeCount - 1));

            GetPhysicsShapeImpl(this, shapeIdx, physicsShape);
            return physicsShape.Count;
        }

        [FreeFunction("SpritesBindings::GetPhysicsShape", ThrowsException = true)]
        private extern static void GetPhysicsShapeImpl(Sprite sprite, int shapeIdx, List<Vector2> physicsShape);

        public void OverridePhysicsShape(IList<Vector2[]> physicsShapes)
        {
            for (int i = 0; i < physicsShapes.Count; ++i)
            {
                var physicsShape = physicsShapes[i];
                if (physicsShape == null)
                {
                    throw new ArgumentNullException(String.Format("Physics Shape at {0} is null.", i));
                }
                if (physicsShape.Length < 3)
                {
                    throw new ArgumentException(String.Format("Physics Shape at {0} has less than 3 vertices ({1}).", i, physicsShape.Length));
                }
            }

            OverridePhysicsShapeCount(this, physicsShapes.Count);
            for (int idx = 0; idx < physicsShapes.Count; ++idx)
                OverridePhysicsShape(this, physicsShapes[idx], idx);
        }

        [FreeFunction("SpritesBindings::OverridePhysicsShapeCount")]
        private extern static void OverridePhysicsShapeCount(Sprite sprite, int physicsShapeCount);

        [FreeFunction("SpritesBindings::OverridePhysicsShape", ThrowsException = true)]
        private extern static void OverridePhysicsShape(Sprite sprite, Vector2[] physicsShape, int idx);

        [FreeFunction("SpritesBindings::OverrideGeometry", HasExplicitThis = true)]
        public extern void OverrideGeometry(Vector2[] vertices, UInt16[] triangles);

        // Workaround for Overloads as described in
        internal static Sprite Create(Rect rect, Vector2 pivot, float pixelsToUnits, Texture2D texture)
        {
            return CreateSpriteWithoutTextureScripting(rect, pivot, pixelsToUnits, texture);
        }

        internal static Sprite Create(Rect rect, Vector2 pivot, float pixelsToUnits)
        {
            return CreateSpriteWithoutTextureScripting(rect, pivot, pixelsToUnits, null);
        }

        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit, uint extrude, SpriteMeshType meshType, Vector4 border, bool generateFallbackPhysicsShape)
        {
            if (texture == null)
                return null;

            // fix case 659777
            if (rect.xMax > texture.width || rect.yMax > texture.height)
                throw new ArgumentException(String.Format("Could not create sprite ({0}, {1}, {2}, {3}) from a {4}x{5} texture.", rect.x, rect.y, rect.width, rect.height, texture.width, texture.height));

            // fix case 810590
            if (pixelsPerUnit <= 0)
                throw new ArgumentException("pixelsPerUnit must be set to a positive non-zero value.");

            return CreateSprite(texture, rect, pivot, pixelsPerUnit, extrude, meshType, border, generateFallbackPhysicsShape);
        }

        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit, uint extrude, SpriteMeshType meshType, Vector4 border)
        {
            return Create(texture, rect, pivot, pixelsPerUnit, extrude, meshType, border, false);
        }

        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit, uint extrude, SpriteMeshType meshType)
        {
            return Create(texture, rect, pivot, pixelsPerUnit, extrude, meshType, Vector4.zero);
        }

        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit, uint extrude)
        {
            return Create(texture, rect, pivot, pixelsPerUnit, extrude, SpriteMeshType.Tight);
        }

        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit)
        {
            return Create(texture, rect, pivot, pixelsPerUnit, 0);
        }

        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot)
        {
            return Create(texture, rect, pivot, 100.0f);
        }
    }
}
