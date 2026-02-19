// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.U2D
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SpriteBlendShapeVertex
    {
        public uint index;
        public Vector3 vertex;
        public Vector3 normal;
        public Vector3 tangent;
    }
}

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

    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    public struct SecondarySpriteTexture : IEquatable<SecondarySpriteTexture>
    {
        public string name;
        public Texture2D texture;

        public bool Equals(SecondarySpriteTexture other)
        {
            return name == other.name && Equals(texture, other.texture);
        }

        public override bool Equals(object obj)
        {
            return obj is SecondarySpriteTexture other && Equals(other);
        }

        public override int GetHashCode() => HashCode.Combine(name, texture);
        public static bool operator ==(SecondarySpriteTexture lhs, SecondarySpriteTexture rhs) => lhs.Equals(rhs);
        public static bool operator !=(SecondarySpriteTexture lhs, SecondarySpriteTexture rhs) => !(lhs == rhs);
    }

    [NativeHeader("Runtime/2D/Common/ScriptBindings/SpritesMarshalling.h")]
    [NativeHeader("Runtime/2D/Common/SpriteDataAccess.h")]
    [NativeHeader("Runtime/Graphics/SpriteUtility.h")]
    [NativeHeader("Runtime/Graphics/SpriteFrame.h")]
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
        extern static Sprite CreateSpriteInternal(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit, uint extrude, SpriteMeshType meshType, Vector4 border, bool generateFallbackPhysicsShape, SecondarySpriteTexture[] secondaryTexture);

        internal static Sprite CreateSprite(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit, uint extrude, SpriteMeshType meshType, Vector4 border, bool generateFallbackPhysicsShape, SecondarySpriteTexture[] secondaryTexture)
        {
            if (secondaryTexture != null)
            {
                foreach (var secondarySpriteTexture in secondaryTexture)
                {
                    if (string.IsNullOrEmpty(secondarySpriteTexture.name))
                        throw new ArgumentException("Empty secondary texture name provided.");
                }
            }

            return CreateSpriteInternal(texture, rect, pivot,pixelsPerUnit, extrude, meshType, border, generateFallbackPhysicsShape, secondaryTexture);  

        }

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

        internal extern uint extrude
        {
            get;
        }

        // Get Secondary Textures.
        internal extern Texture2D GetSecondaryTexture(int index);
        // Get Secondary Texture count
        public extern int GetSecondaryTextureCount();
        [FreeFunction("SpritesBindings::GetSecondaryTextures", HasExplicitThis = true)]
        public extern int GetSecondaryTextures([NotNull][Out] SecondarySpriteTexture[] secondaryTexture);

        // The number of pixels in one unit. Note: The C++ side still uses the name pixelsToUnits which is misleading,
        // but has not been changed yet to minimize merge conflicts.
        public extern float pixelsPerUnit
        {
            [NativeMethod("GetPixelsToUnits")]
            get;
        }

        public extern float spriteAtlasTextureScale
        {
            [NativeMethod("GetSpriteAtlasTextureScale")]
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
                return GetTextureRect();
            }
        }

        public Vector2 textureRectOffset
        {
            get
            {
                return GetTextureRectOffset();
            }
        }

        public extern Vector2[] vertices
        {
            [FreeFunction("SpriteAccessLegacy::GetSpriteVertices", HasExplicitThis = true)]
            [return:UnityMarshalAs(NativeType.ScriptingObjectPtr)]
            get;
        }

        public extern UInt16[] triangles
        {
            [FreeFunction("SpriteAccessLegacy::GetSpriteIndices", HasExplicitThis = true)]
            [return:UnityMarshalAs(NativeType.ScriptingObjectPtr)]
            get;
        }

        public extern Vector2[] uv
        {
            [FreeFunction("SpriteAccessLegacy::GetSpriteUVs", HasExplicitThis = true)]
            [return:UnityMarshalAs(NativeType.ScriptingObjectPtr)]
            get;
        }

        public extern int GetPhysicsShapeCount();

        public extern uint GetScriptableObjectsCount();
        [FreeFunction("SpritesBindings::GetScriptableObjects", HasExplicitThis = true)]
        public extern uint GetScriptableObjects([NotNull][Out] ScriptableObject[] scriptableObjects);
        public extern bool AddScriptableObject([NotNull]ScriptableObject obj);
        public extern bool RemoveScriptableObjectAt(uint i);
        public extern bool SetScriptableObjectAt([NotNull]ScriptableObject obj, uint i);

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

        public ReadOnlySpan<Vector2> GetPhysicsShape(int shapeIdx)
        {
            int physicsShapeCount = GetPhysicsShapeCount();
            if (shapeIdx < 0 || shapeIdx >= physicsShapeCount)
                throw new IndexOutOfRangeException(String.Format("Index({0}) is out of bounds(0 - {1})", shapeIdx, physicsShapeCount - 1));

            return GetPhysicsShapeSpanImpl(this, shapeIdx);
        }

        [FreeFunction("SpritesBindings::GetPhysicsShape", ThrowsException = true)]
        private extern static void GetPhysicsShapeImpl(Sprite sprite, int shapeIdx, [NotNull] List<Vector2> physicsShape);

        [FreeFunction("SpritesBindings::GetPhysicsShape", ThrowsException = true)]
        private extern static ReadOnlySpan<Vector2> GetPhysicsShapeSpanImpl(Sprite sprite, int shapeIdx);

        public void OverridePhysicsShape(IList<Vector2[]> physicsShapes)
        {
            if (physicsShapes == null)
                throw new ArgumentNullException(nameof(physicsShapes));

            for (int i = 0; i < physicsShapes.Count; ++i)
            {
                var physicsShape = physicsShapes[i];
                if (physicsShape == null)
                {
                    throw new ArgumentNullException(nameof(physicsShape), String.Format("Physics Shape at {0} is null.", i));
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
        private extern static void OverridePhysicsShape(Sprite sprite, [NotNull] Vector2[] physicsShape, int idx);

        [FreeFunction("SpritesBindings::OverrideGeometry", HasExplicitThis = true)]
        public extern void OverrideGeometry([NotNull] Vector2[] vertices, [NotNull] UInt16[] triangles);

        // BlendShape API
        public extern int blendShapeCount
        {
            [NativeMethod("GetBlendShapeChannelCount")]
            get;
        }

        [FreeFunction(Name = "SpritesBindings::ClearBlendShapes", HasExplicitThis = true)]
        public extern void ClearBlendShapes();

        [FreeFunction(Name = "SpritesBindings::GetBlendShapeName", HasExplicitThis = true, ThrowsException = true)]
        public extern string GetBlendShapeName(int shapeIndex);

        /// <summary>
        /// Returns the index of the blend shape with the given name, or -1 if it does not exist.
        /// </summary>
        /// <param name="blendShapeName">The name of the blend shape.</param>
        /// <returns>The index of the blend shape (0-based), or -1 if not found.</returns>
        /// <exception cref="System.ArgumentNullException">blendShapeName is null or empty.</exception>
        [FreeFunction(Name = "SpritesBindings::GetBlendShapeIndex", HasExplicitThis = true, ThrowsException = true)]
        extern internal int GetBlendShapeIndexInternal(string blendShapeName);

        public int GetBlendShapeIndex(string blendShapeName)
        {
            if (string.IsNullOrEmpty(blendShapeName))
                throw new System.ArgumentNullException(nameof(blendShapeName));
            return GetBlendShapeIndexInternal(blendShapeName);
        }

        /// <summary>
        /// Returns the number of frames in the given blend shape.
        /// </summary>
        /// <param name="shapeIndex">The index of the blend shape.</param>
        /// <returns>The number of frames in the blend shape.</returns>
        /// <exception cref="System.ArgumentException">shapeIndex is out of range.</exception>
        [FreeFunction(Name = "SpritesBindings::GetBlendShapeFrameCount", HasExplicitThis = true, ThrowsException = true)]
        public extern int GetBlendShapeFrameCount(int shapeIndex);

        /// <summary>
        /// Returns the weight of the given frame in the blend shape.
        /// </summary>
        /// <param name="shapeIndex">The index of the blend shape.</param>
        /// <param name="frameIndex">The index of the frame within the blend shape.</param>
        /// <returns>The weight of the blend shape frame.</returns>
        /// <exception cref="System.ArgumentException">shapeIndex or frameIndex is out of range.</exception>
        [FreeFunction(Name = "SpritesBindings::GetBlendShapeFrameWeight", HasExplicitThis = true, ThrowsException = true)]
        public extern float GetBlendShapeFrameWeight(int shapeIndex, int frameIndex);

        [FreeFunction(Name = "SpritesBindings::AddBlendShape", HasExplicitThis = true, ThrowsException = true)]
        extern internal int AddBlendShapeInternal(string shapeName);

        public int AddBlendShape(string shapeName)
        {
            if (string.IsNullOrEmpty(shapeName))
                throw new System.ArgumentNullException(nameof(shapeName));
            return AddBlendShapeInternal(shapeName);
        }

        [FreeFunction(Name = "SpritesBindings::AddBlendShapeFrame", HasExplicitThis = true, ThrowsException = true)]
        private extern int AddBlendShapeFrame_Internal(int shapeIndex, float frameWeight, IntPtr vertices, int vertexCount);

        public int AddBlendShapeFrame(int shapeIndex, float frameWeight, NativeArray<U2D.SpriteBlendShapeVertex> vertices)
        {
            unsafe
            {
                return AddBlendShapeFrame_Internal(shapeIndex, frameWeight, (IntPtr)vertices.GetUnsafePtr(), vertices.Length);
            }
        }

        [FreeFunction(Name = "SpritesBindings::GetBlendShapeFrameVertexCount", HasExplicitThis = true, ThrowsException = true)]
        public extern int GetBlendShapeFrameVertexCount(int shapeIndex, int frameIndex);

        [FreeFunction(Name = "SpritesBindings::GetBlendShapeFrame", HasExplicitThis = true, ThrowsException = true)]
        private extern void GetBlendShapeFrame_Internal(int shapeIndex, int frameIndex, IntPtr outVertices, int outVertexCount);

        public void GetBlendShapeFrame(int shapeIndex, int frameIndex, NativeArray<U2D.SpriteBlendShapeVertex> outVertices)
        {
            unsafe
            {
                GetBlendShapeFrame_Internal(shapeIndex, frameIndex, (IntPtr)outVertices.GetUnsafePtr(), outVertices.Length);
            }
        }

        [FreeFunction(Name = "SpritesBindings::UpdateBlendShapeFrame", HasExplicitThis = true, ThrowsException = true)]
        private extern void UpdateBlendShapeFrame_Internal(int shapeIndex, int frameIndex, IntPtr vertices, int vertexCount);

        public void UpdateBlendShapeFrame(int shapeIndex, int frameIndex, NativeArray<U2D.SpriteBlendShapeVertex> vertices)
        {
            unsafe
            {
                UpdateBlendShapeFrame_Internal(shapeIndex, frameIndex, (IntPtr)vertices.GetUnsafePtr(), vertices.Length);
            }
        }

        [FreeFunction(Name = "SpritesBindings::GetBlendShapeBufferPtr", HasExplicitThis = true, ThrowsException = true)]
        extern GraphicsBuffer GetBlendShapeBufferImpl(int layout);

        public GraphicsBuffer GetBlendShapeBuffer(Rendering.BlendShapeBufferLayout layout)
        {
            if (this == null)
                throw new System.NullReferenceException();

            if (!SystemInfo.supportsComputeShaders)
            {
                Debug.LogError("Only possible to access Blend Shape buffer on platforms that supports compute shaders.");
                return null;
            }

            var buf = GetBlendShapeBufferImpl((int)layout);
            if (buf != null)
                buf.AddBufferToLeakDetector();
            return buf;
        }

        public GraphicsBuffer GetBlendShapeBuffer()
        {
            if (this == null)
                throw new System.NullReferenceException();

            if (!SystemInfo.supportsComputeShaders)
            {
                Debug.LogError("Only possible to access Blend Shape buffer on platforms that supports compute shaders.");
                return null;
            }

            var buf = GetBlendShapeBufferImpl((int)Rendering.BlendShapeBufferLayout.PerShape);
            if (buf != null)
                buf.AddBufferToLeakDetector();
            return buf;
        }

        // Workaround for Overloads as described in
        [VisibleToOtherModules] 
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
            return Create(texture, rect, pivot, pixelsPerUnit, extrude, meshType, border, generateFallbackPhysicsShape, null);
        }

        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit, uint extrude, SpriteMeshType meshType, Vector4 border, bool generateFallbackPhysicsShape, SecondarySpriteTexture[] secondaryTextures)
        {
            if (texture == null)
                return null;

            // fix case 659777
            if (rect.xMax > texture.width || rect.yMax > texture.height)
                throw new ArgumentException(String.Format("Could not create sprite ({0}, {1}, {2}, {3}) from a {4}x{5} texture.", rect.x, rect.y, rect.width, rect.height, texture.width, texture.height));

            // fix case 810590
            if (pixelsPerUnit <= 0)
                throw new ArgumentException("pixelsPerUnit must be set to a positive non-zero value.");

            if (secondaryTextures != null)
            {
                foreach(var st in secondaryTextures)
                {
                    if(st.texture == texture)
                        throw new ArgumentException(string.Format("{0} is using source Texture as Secondary Texture.", st.name));
                }
            }
            return CreateSprite(texture, rect, pivot, pixelsPerUnit, extrude, meshType, border, generateFallbackPhysicsShape, secondaryTextures);

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
