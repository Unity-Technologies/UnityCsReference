// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine.TextCore.Text;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Represents a vertex of geometry for drawing content of <see cref="VisualElement"/>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        /// <summary>
        /// A special value representing the near clipping plane. Always use this value as the vertex position's z component when building 2D (flat) UI geometry.
        /// </summary>
        public readonly static float nearZ = UIRUtility.k_MeshPosZ;

        /// <summary>
        /// Describes the vertex's position.
        /// </summary>
        /// <remarks>
        /// Note this value is a <see cref="Vector3"/>. If the vertex represents flat UI geometry, set the z component of this position field to <see cref="Vertex.nearZ"/>. The position value is relative to the <see cref="VisualElement"/>'s local rectangle top-left corner. The coordinate system is X+ to the right, and Y+ goes down. The unit of position is <see cref="VisualElement"/> points. When the vertices are indexed, the triangles described must follow clock-wise winding order given that Y+ goes down.
        /// </remarks>
        public Vector3 position;
        /// <summary>
        /// A color value for the vertex.
        /// </summary>
        /// <remarks>
        /// This value is multiplied by any other color information of the <see cref="VisualElement"/> (e.g. texture). Use <see cref="Color.white"/> to disable tinting on the vertex.
        /// </remarks>
        public Color32 tint;
        /// <summary>
        /// The UV coordinate of the vertex.
        /// </summary>
        /// <remarks>
        /// This is used to sample the required region of the associated texture if any. Values outside the <see cref="MeshWriteData.uvRegion"/> are currently not supported and could lead to undefined results.
        /// </remarks>
        public Vector2 uv;
        internal Color32 xformClipPages; // Top-left of xform and clip pages: XY,XY
        internal Color32 ids; //XYZW (xform,clip,opacity,color/textcore)
        internal Color32 flags; //X (flags) Y (textcore-dilate) Z (is-arc) W (is-dynamic-color)
        internal Color32 opacityColorPages; //XY (opacity) ZW (color, textcore or SVG setting)
        internal Vector4 circle; // XY (outer) ZW (inner)
        internal float textureId;

        // Winding order of vertices matters. CCW is for clipped meshes.
    }

    /// <summary>
    /// A class that represents the vertex and index data allocated for drawing the content of a <see cref="VisualElement"/>.
    /// </summary>
    /// <remarks>
    /// You can use this object to fill the values for the vertices and indices only during a callback to the <see cref="VisualElement.generateVisualContent"/> delegate. Do not store the passed <see cref="MeshWriteData"/> outside the scope of <see cref="VisualElement.generateVisualContent"/> as Unity could recycle it for other callbacks.
    /// </remarks>
    public class MeshWriteData
    {
        internal MeshWriteData() {}  // Don't want users to instantiate this class themselves

        /// <summary>
        /// The number of vertices successfully allocated for <see cref="VisualElement"/> content drawing.
        /// </summary>
        public int vertexCount { get { return m_Vertices.Length; } }

        /// <summary>
        /// The number of indices successfully allocated for <see cref="VisualElement"/> content drawing.
        /// </summary>
        public int indexCount { get { return m_Indices.Length; } }

        /// <summary>
        /// A rectangle describing the UV region holding the texture passed to <see cref="MeshGenerationContext.Allocate"/>.
        /// </summary>
        /// <remarks>
        /// Internally, the texture passed to <see cref="MeshGenerationContext.Allocate"/> may either be used directly or is automatically integrated within a larger atlas. It is therefore required to use this property to scale and offset the UVs for the generated vertices in order to sample the correct texels.
        /// Correct use of <see cref="MeshWriteData.uvRegion"/> is simple: given an input UV in [0,1] range, multiply the UV by (<see cref="uvRegion.width"/>,<see cref="uvRegion.height"/>) then add <see cref="uvRegion.xMin,uvRegion.yMin"/> and store the result in <see cref="Vertex.uv"/>.
        /// </remarks>
        public Rect uvRegion { get { return m_UVRegion;  } }

        /// <summary>
        /// Assigns the value of the next vertex of the allocated vertices list.
        /// </summary>
        /// <param name="vertex">The value of the next vertex.</param>
        /// <remarks>
        /// Used to iteratively fill the values of the allocated vertices via repeated calls to this function until all values have been provided. This way of filling vertex data is mutually exclusive with the use of <see cref="SetAllVertices"/>.
        /// After each invocation to this function, the internal counter for the next vertex is automatically incremented.
        /// When this method is called, it is not possible to use <see cref="SetAllVertices"/> to fill the vertices.
        ///
        /// Note that calling <see cref="SetNextVertex"/> fewer times than the allocated number of vertices will leave the remaining vertices with random values as <see cref="MeshGenerationContext.Allocate"/> does not initialize the returned data to 0 to avoid redundant work.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetNextVertex(Vertex vertex) { m_Vertices[currentVertex++] = vertex; }

        /// <summary>
        /// Assigns the value of the next index of the allocated indices list.
        /// </summary>
        /// <param name="index">The value of the next index.</param>
        /// <remarks>
        /// Used to iteratively fill the values of the allocated indices via repeated calls to this function until all values have been provided. This way of filling index data is mutually exclusive with the use of <see cref="SetAllIndices"/>.
        /// After each invocation to this function, the internal counter for the next index is automatically incremented.
        /// When this method is called, it is not possible to use <see cref="SetAllIndices"/> to fill the indices.
        /// The index values provided refer directly to the vertices allocated in the same <see cref="MeshWriteData"/> object. Thus, an index of 0 means the first vertex and index 1 means the second vertex and so on.
        /// </remarks>
        /// <remarks>
        /// Note that calling <see cref="SetNextIndex"/> fewer times than the allocated number of indices will leave the remaining indices with random values as <see cref="MeshGenerationContext.Allocate"/> does not initialize the returned data to 0 to avoid redundant work.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetNextIndex(UInt16 index) { m_Indices[currentIndex++] = index; }

        /// <summary>
        /// Fills the values of the allocated vertices with values copied directly from an array.
        /// When this method is called, it is not possible to use <see cref="SetNextVertex"/> to fill the allocated vertices array.
        /// </summary>
        /// <param name="vertices">The array of vertices to copy from. The length of the array must match the allocated vertex count.</param>
        /// <remarks>
        /// When this method is called, it is not possible to use <see cref="SetNextVertex"/> to fill the vertices.
        /// </remarks>
        /// <example>
        /// <code>
        /// public class MyVisualElement : VisualElement
        /// {
        ///     void MyGenerateVisualContent(MeshGenerationContext mgc)
        ///     {
        ///         var meshWriteData = mgc.Allocate(4, 6);
        ///         // meshWriteData has been allocated with 6 indices for 2 triangles
        ///
        ///         // ... set the vertices
        ///
        ///         // Set indices for the first triangle
        ///         meshWriteData.SetNextIndex(0);
        ///         meshWriteData.SetNextIndex(1);
        ///         meshWriteData.SetNextIndex(2);
        ///
        ///         // Set indices for the second triangle
        ///         meshWriteData.SetNextIndex(2);
        ///         meshWriteData.SetNextIndex(1);
        ///         meshWriteData.SetNextIndex(3);
        ///     }
        /// }
        /// </code>
        /// </example>
        public void SetAllVertices(Vertex[] vertices)
        {
            if (currentVertex == 0)
            {
                m_Vertices.CopyFrom(vertices);
                currentVertex = m_Vertices.Length;
            }
            else throw new InvalidOperationException("SetAllVertices may not be called after using SetNextVertex");
        }

        /// <summary>
        /// Fills the values of the allocated vertices with values copied directly from an array.
        /// When this method is called, it is not possible to use <see cref="SetNextVertex"/> to fill the allocated vertices array.
        /// </summary>
        /// <param name="vertices">The array of vertices to copy from. The length of the array must match the allocated vertex count.</param>
        /// <remarks>
        /// When this method is called, it is not possible to use <see cref="SetNextVertex"/> to fill the vertices.
        /// </remarks>
        public void SetAllVertices(NativeSlice<Vertex> vertices)
        {
            if (currentVertex == 0)
            {
                m_Vertices.CopyFrom(vertices);
                currentVertex = m_Vertices.Length;
            }
            else throw new InvalidOperationException("SetAllVertices may not be called after using SetNextVertex");
        }

        /// <summary>
        /// Fills the values of the allocated indices with values copied directly from an array. Each 3 consecutive indices form a single triangle.
        /// </summary>
        /// <param name="indices">The array of indices to copy from. The length of the array must match the allocated index count.</param>
        /// <remarks>
        /// When this method is called, it is not possible to use <see cref="SetNextIndex"/> to fill the indices.
        /// </remarks>
        public void SetAllIndices(UInt16[] indices)
        {
            if (currentIndex == 0)
            {
                m_Indices.CopyFrom(indices);
                currentIndex = m_Indices.Length;
            }
            else throw new InvalidOperationException("SetAllIndices may not be called after using SetNextIndex");
        }

        /// <summary>
        /// Fills the values of the allocated indices with values copied directly from an array. Each 3 consecutive indices form a single triangle.
        /// </summary>
        /// <param name="indices">The array of indices to copy from. The length of the array must match the allocated index count.</param>
        /// <remarks>
        /// When this method is called, it is not possible to use <see cref="SetNextIndex"/> to fill the indices.
        /// </remarks>
        public void SetAllIndices(NativeSlice<UInt16> indices)
        {
            if (currentIndex == 0)
            {
                m_Indices.CopyFrom(indices);
                currentIndex = m_Indices.Length;
            }
            else throw new InvalidOperationException("SetAllIndices may not be called after using SetNextIndex");
        }

        internal void Reset(NativeSlice<Vertex> vertices, NativeSlice<UInt16> indices)
        {
            m_Vertices = vertices;
            m_Indices = indices;
            m_UVRegion = new Rect(0, 0, 1, 1);
            currentIndex = currentVertex = 0;
        }

        internal void Reset(NativeSlice<Vertex> vertices, NativeSlice<UInt16> indices, Rect uvRegion)
        {
            m_Vertices = vertices;
            m_Indices = indices;
            m_UVRegion = uvRegion;
            currentIndex = currentVertex = 0;
        }

        internal NativeSlice<Vertex> m_Vertices;
        internal NativeSlice<UInt16> m_Indices;
        internal Rect m_UVRegion;
        internal int currentIndex, currentVertex;
    }

    internal struct ColorPage
    {
        public bool isValid;
        public Color32 pageAndID;

        public static ColorPage Init(UIR.RenderChain renderChain, UIR.BMPAlloc alloc)
        {
            bool isValid = alloc.IsValid();
            return new ColorPage() {
                isValid = isValid,
                pageAndID = isValid ? renderChain.shaderInfoAllocator.ColorAllocToVertexData(alloc) : new Color32()
            };
        }

        public MeshBuilderNative.NativeColorPage ToNativeColorPage()
        {
            return new MeshBuilderNative.NativeColorPage() {
                isValid = isValid ? 1 : 0,
                pageAndID = pageAndID
            };
        }
    }

    internal static class MeshGenerationContextUtils
    {
        public struct BorderParams
        {
            public Rect rect;
            public Color playmodeTintColor;

            public Color leftColor;
            public Color topColor;
            public Color rightColor;
            public Color bottomColor;

            public float leftWidth;
            public float topWidth;
            public float rightWidth;
            public float bottomWidth;

            public Vector2 topLeftRadius;
            public Vector2 topRightRadius;
            public Vector2 bottomRightRadius;
            public Vector2 bottomLeftRadius;

            public Material material;

            // The color allocations
            internal ColorPage leftColorPage;
            internal ColorPage topColorPage;
            internal ColorPage rightColorPage;
            internal ColorPage bottomColorPage;

            internal MeshBuilderNative.NativeBorderParams ToNativeParams()
            {
                return new MeshBuilderNative.NativeBorderParams() {
                    rect = rect,
                    leftColor = leftColor,
                    topColor = topColor,
                    rightColor = rightColor,
                    bottomColor = bottomColor,
                    leftWidth = leftWidth,
                    topWidth = topWidth,
                    rightWidth = rightWidth,
                    bottomWidth = bottomWidth,
                    topLeftRadius = topLeftRadius,
                    topRightRadius = topRightRadius,
                    bottomRightRadius = bottomRightRadius,
                    bottomLeftRadius = bottomLeftRadius,
                    leftColorPage = leftColorPage.ToNativeColorPage(),
                    topColorPage = topColorPage.ToNativeColorPage(),
                    rightColorPage = rightColorPage.ToNativeColorPage(),
                    bottomColorPage = bottomColorPage.ToNativeColorPage()
                };
            }
        }

        public struct RectangleParams
        {
            public Rect rect;
            public Rect uv;
            public Color color;

            // Normalized visible sub-region
            public Rect subRect;

            // Allow support of background-properties
            public BackgroundPosition backgroundPositionX;
            public BackgroundPosition backgroundPositionY;
            public BackgroundRepeat backgroundRepeat;
            public BackgroundSize backgroundSize;

            public Texture texture;
            public Sprite sprite;
            public VectorImage vectorImage;
            public Material material;
            public ScaleMode scaleMode;
            public Color playmodeTintColor;

            public Vector2 topLeftRadius;
            public Vector2 topRightRadius;
            public Vector2 bottomRightRadius;
            public Vector2 bottomLeftRadius;

            public Vector2 contentSize;
            public Vector2 textureSize;

            public int leftSlice;
            public int topSlice;
            public int rightSlice;
            public int bottomSlice;

            public float sliceScale;

            // Cached sprite geometry, which is expensive to evaluate.
            internal Rect spriteGeomRect;

            // The color allocation
            internal ColorPage colorPage;

            internal MeshGenerationContext.MeshFlags meshFlags;

            public static RectangleParams MakeSolid(Rect rect, Color color, ContextType panelContext)
            {
                var playmodeTintColor = panelContext == ContextType.Editor
                    ? UIElementsUtility.editorPlayModeTintColor
                    : Color.white;

                return new RectangleParams
                {
                    rect = rect,
                    color = color,
                    uv = new Rect(0, 0, 1, 1),
                    playmodeTintColor = playmodeTintColor
                };
            }

            private static void AdjustUVsForScaleMode(Rect rect, Rect uv, Texture texture, ScaleMode scaleMode, out Rect rectOut, out Rect uvOut)
            {
                // Fill the UVs according to scale mode
                // Comparing aspects ratio is error-prone because the screenRect may end up being scaled by the
                // transform and the corners will end up being pixel aligned, possibly resulting in blurriness.

                float srcAspect = (texture.width * uv.width) / (texture.height * uv.height);
                float destAspect = rect.width / rect.height;

                switch (scaleMode)
                {
                    case ScaleMode.StretchToFill:
                        break;

                    case ScaleMode.ScaleAndCrop:
                        if (destAspect > srcAspect)
                        {
                            float stretch = uv.height * (srcAspect / destAspect);
                            float crop = (uv.height - stretch) * 0.5f;
                            uv = new Rect(uv.x, uv.y + crop, uv.width, stretch);
                        }
                        else
                        {
                            float stretch = uv.width * (destAspect / srcAspect);
                            float crop = (uv.width - stretch) * 0.5f;
                            uv = new Rect(uv.x + crop, uv.y, stretch, uv.height);
                        }
                        break;

                    case ScaleMode.ScaleToFit:
                        if (destAspect > srcAspect)
                        {
                            float stretch = srcAspect / destAspect;
                            rect = new Rect(rect.xMin + rect.width * (1.0f - stretch) * .5f, rect.yMin, stretch * rect.width, rect.height);
                        }
                        else
                        {
                            float stretch = destAspect / srcAspect;
                            rect = new Rect(rect.xMin, rect.yMin + rect.height * (1.0f - stretch) * .5f, rect.width, stretch * rect.height);
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }

                rectOut = rect;
                uvOut = uv;
            }

            private static void AdjustSpriteUVsForScaleMode(Rect rect, Rect uv, Rect geomRect, Texture texture, Sprite sprite, ScaleMode scaleMode, out Rect rectOut, out Rect uvOut)
            {
                // Adjust the sprite rect size and then determine where the sprite geometry should be inside it.

                float srcAspect = sprite.rect.width / sprite.rect.height;
                float destAspect = rect.width / rect.height;

                // Normalize the geom rect for easy scaling
                var geomRectNorm = geomRect;
                geomRectNorm.position -= (Vector2)sprite.bounds.min;
                geomRectNorm.position /= sprite.bounds.size;
                geomRectNorm.size /= sprite.bounds.size;

                // Convert to Y-down convention
                var p = geomRectNorm.position;
                p.y = 1.0f - geomRectNorm.size.y - p.y;
                geomRectNorm.position = p;

                switch (scaleMode)
                {
                    case ScaleMode.StretchToFill:
                    {
                        var scale = rect.size;
                        rect.position = geomRectNorm.position * scale;
                        rect.size = geomRectNorm.size * scale;
                    }
                    break;

                    case ScaleMode.ScaleAndCrop:
                    {
                        // This is the complex code path. Scale-and-crop works like the following:
                        // - Scale the sprite rect to match the largest destination rect size
                        // - Evaluate the sprite geometry rect inside that scaled sprite rect
                        // - Compute the intersection of the geometry rect with the destination rect
                        // - Re-evaluate the UVs from that intersection

                        var stretchedRect = rect;
                        if (destAspect > srcAspect)
                        {
                            stretchedRect.height = stretchedRect.width / srcAspect;
                            stretchedRect.position = new Vector2(stretchedRect.position.x, -(stretchedRect.height - rect.height) / 2.0f);
                        }
                        else
                        {
                            stretchedRect.width = stretchedRect.height * srcAspect;
                            stretchedRect.position = new Vector2(-(stretchedRect.width - rect.width) / 2.0f, stretchedRect.position.y);
                        }

                        var scale = stretchedRect.size;
                        stretchedRect.position += geomRectNorm.position * scale;
                        stretchedRect.size = geomRectNorm.size * scale;

                        // Intersect the stretched rect with the destination rect to compute the new UVs
                        var newRect = RectIntersection(rect, stretchedRect);
                        if (newRect.width < UIRUtility.k_Epsilon || newRect.height < UIRUtility.k_Epsilon)
                            newRect = Rect.zero;
                        else
                        {
                            var uvScale = newRect;
                            uvScale.position -= stretchedRect.position;
                            uvScale.position /= stretchedRect.size;
                            uvScale.size /= stretchedRect.size;

                            // Textures are using a Y-up convention
                            var scalePos = uvScale.position;
                            scalePos.y = 1.0f - uvScale.size.y - scalePos.y;
                            uvScale.position = scalePos;

                            uv.position += uvScale.position * uv.size;
                            uv.size *= uvScale.size;
                        }

                        rect = newRect;
                    }
                    break;

                    case ScaleMode.ScaleToFit:
                    {
                        if (destAspect > srcAspect)
                        {
                            float stretch = srcAspect / destAspect;
                            rect = new Rect(rect.xMin + rect.width * (1.0f - stretch) * .5f, rect.yMin, stretch * rect.width, rect.height);
                        }
                        else
                        {
                            float stretch = destAspect / srcAspect;
                            rect = new Rect(rect.xMin, rect.yMin + rect.height * (1.0f - stretch) * .5f, rect.width, stretch * rect.height);
                        }

                        rect.position += geomRectNorm.position * rect.size;
                        rect.size *= geomRectNorm.size;
                    }
                    break;

                    default:
                        throw new NotImplementedException();
                }


                rectOut = rect;
                uvOut = uv;
            }

            internal static Rect RectIntersection(Rect a, Rect b)
            {
                var r = Rect.zero;
                r.min = Vector2.Max(a.min, b.min);
                r.max = Vector2.Min(a.max, b.max);
                r.size = Vector2.Max(r.size, Vector2.zero);
                return r;
            }

            static Rect ComputeGeomRect(Sprite sprite)
            {
                var vMin = new Vector2(float.MaxValue, float.MaxValue);
                var vMax = new Vector2(float.MinValue, float.MinValue);
                foreach (var uv in sprite.vertices)
                {
                    vMin = Vector2.Min(vMin, uv);
                    vMax = Vector2.Max(vMax, uv);
                }
                return new Rect(vMin, vMax - vMin);
            }

            static Rect ComputeUVRect(Sprite sprite)
            {
                var uvMin = new Vector2(float.MaxValue, float.MaxValue);
                var uvMax = new Vector2(float.MinValue, float.MinValue);
                foreach (var uv in sprite.uv)
                {
                    uvMin = Vector2.Min(uvMin, uv);
                    uvMax = Vector2.Max(uvMax, uv);
                }
                return new Rect(uvMin, uvMax - uvMin);
            }

            static Rect ApplyPackingRotation(Rect uv, SpritePackingRotation rotation)
            {
                switch (rotation)
                {
                    case SpritePackingRotation.FlipHorizontal:
                    {
                        uv.position += new Vector2(uv.size.x, 0.0f);
                        var size = uv.size;
                        size.x = -size.x;
                        uv.size = size;
                    }
                    break;
                    case SpritePackingRotation.FlipVertical:
                    {
                        uv.position += new Vector2(0.0f, uv.size.y);
                        var size = uv.size;
                        size.y = -size.y;
                        uv.size = size;
                    }
                    break;
                    case SpritePackingRotation.Rotate180:
                    {
                        uv.position += uv.size;
                        uv.size = -uv.size;
                    }
                    break;
                    default:
                        break;
                }

                return uv;
            }

            public static RectangleParams MakeTextured(Rect rect, Rect uv, Texture texture, ScaleMode scaleMode, ContextType panelContext)
            {
                var playmodeTintColor = panelContext == ContextType.Editor
                    ? UIElementsUtility.editorPlayModeTintColor
                    : Color.white;

                AdjustUVsForScaleMode(rect, uv, texture, scaleMode, out rect, out uv);

                var textureSize = new Vector2(texture.width, texture.height);

                var rp = new RectangleParams
                {
                    rect = rect,
                    subRect = new Rect(0,0,1,1),
                    uv = uv,
                    color = Color.white,
                    texture = texture,
                    contentSize = textureSize,
                    textureSize = textureSize,
                    scaleMode = scaleMode,
                    playmodeTintColor = playmodeTintColor
                };
                return rp;
            }

            public static RectangleParams MakeSprite(Rect rect, Sprite sprite, ScaleMode scaleMode, ContextType panelContext, bool hasRadius, ref Vector4 slices, bool useForRepeat = false)
            {
                if (sprite == null || sprite.bounds.size.x < UIRUtility.k_Epsilon || sprite.bounds.size.y < UIRUtility.k_Epsilon)
                    return new RectangleParams();

                if (sprite.texture == null)
                {
                    Debug.LogWarning($"Ignoring textureless sprite named \"{sprite.name}\", please import as a VectorImage instead");
                    return new RectangleParams();
                }

                var playmodeTintColor = panelContext == ContextType.Editor
                    ? UIElementsUtility.editorPlayModeTintColor
                    : Color.white;

                var geomRect = ComputeGeomRect(sprite);
                var uv = ComputeUVRect(sprite);

                // Use a textured quad (ignoring tight-mesh) if dealing with slicing or with
                // scale-and-crop scale mode. This avoids expensive CPU-side transformation and
                // polygon clipping.
                var border = sprite.border;
                bool hasSlices = (border != Vector4.zero) || (slices != Vector4.zero);
                bool useTexturedQuad = (scaleMode == ScaleMode.ScaleAndCrop) || hasSlices || hasRadius || useForRepeat;

                if (useTexturedQuad && sprite.packed && sprite.packingRotation != SpritePackingRotation.None)
                    uv = ApplyPackingRotation(uv, sprite.packingRotation);

                AdjustSpriteUVsForScaleMode(rect, uv, geomRect, sprite.texture, sprite, scaleMode, out rect, out uv);

                // Compute normalized subRect
                var subRect = geomRect;
                subRect.size /= (Vector2)sprite.bounds.size;
                subRect.position -= (Vector2)sprite.bounds.min;
                subRect.position /= (Vector2)sprite.bounds.size;
                subRect.position = new Vector2(subRect.position.x, 1.0f - (subRect.position.y + subRect.height)); // Y-down for UIR

                var rp = new RectangleParams
                {
                    rect = rect,
                    uv = uv,
                    subRect = subRect,
                    color = Color.white,
                    texture = useTexturedQuad ? sprite.texture : (Texture2D)null,
                    sprite = useTexturedQuad ? (Sprite)null : sprite,
                    contentSize = sprite.rect.size,
                    textureSize = new Vector2(sprite.texture.width, sprite.texture.height),
                    spriteGeomRect = geomRect,
                    scaleMode = scaleMode,
                    playmodeTintColor = playmodeTintColor,
                    meshFlags = sprite.packed ? MeshGenerationContext.MeshFlags.SkipDynamicAtlas : MeshGenerationContext.MeshFlags.None
                };

                // Store the slices in VisualElement order (left, top, right, bottom)
                var spriteBorders = new Vector4(border.x, border.w, border.z, border.y);

                if (slices != Vector4.zero && spriteBorders != Vector4.zero && spriteBorders != slices)
                    // Both the asset slices and the style slices are defined, warn the user
                    Debug.LogWarning($"Sprite \"{sprite.name}\" borders {spriteBorders} are overridden by style slices {slices}");
                else if (slices == Vector4.zero)
                    slices = spriteBorders;

                return rp;
            }

            public static RectangleParams MakeVectorTextured(Rect rect, Rect uv, VectorImage vectorImage, ScaleMode scaleMode, ContextType panelContext)
            {
                var playmodeTintColor = panelContext == ContextType.Editor
                    ? UIElementsUtility.editorPlayModeTintColor
                    : Color.white;

                var rp = new RectangleParams
                {
                    rect = rect,
                    subRect = new Rect(0,0,1,1),
                    uv = uv,
                    color = Color.white,
                    vectorImage = vectorImage,
                    contentSize = new Vector2(vectorImage.width, vectorImage.height),
                    scaleMode = scaleMode,
                    playmodeTintColor = playmodeTintColor
                };
                return rp;
            }

            internal bool HasRadius(float epsilon)
            {
                return ((topLeftRadius.x > epsilon) && (topLeftRadius.y > epsilon)) ||
                    ((topRightRadius.x > epsilon) && (topRightRadius.y > epsilon)) ||
                    ((bottomRightRadius.x > epsilon) && (bottomRightRadius.y > epsilon)) ||
                    ((bottomLeftRadius.x > epsilon) && (bottomLeftRadius.y > epsilon));
            }

            internal bool HasSlices(float epsilon)
            {
                return (leftSlice > epsilon) || (topSlice > epsilon) || (rightSlice > epsilon) || (bottomSlice > epsilon);
            }

            internal MeshBuilderNative.NativeRectParams ToNativeParams(Rect uvRegion)
            {
                return new MeshBuilderNative.NativeRectParams() {
                    rect = rect,
                    subRect = subRect,
                    uv = uv,
                    uvRegion = uvRegion,
                    color = color,
                    scaleMode = scaleMode,
                    topLeftRadius = topLeftRadius,
                    topRightRadius = topRightRadius,
                    bottomRightRadius = bottomRightRadius,
                    bottomLeftRadius = bottomLeftRadius,
                    contentSize = contentSize,
                    textureSize = textureSize,
                    texturePixelsPerPoint = texture is Texture2D ? (texture as Texture2D).pixelsPerPoint : 1.0f,
                    leftSlice = leftSlice,
                    topSlice = topSlice,
                    rightSlice = rightSlice,
                    bottomSlice = bottomSlice,
                    sliceScale = sliceScale,
                    colorPage = colorPage.ToNativeColorPage()
                };
            }
        }

        public static void Rectangle(this MeshGenerationContext mgc, RectangleParams rectParams)
        {
            mgc.painter.DrawRectangle(rectParams);
        }

        public static void Border(this MeshGenerationContext mgc, BorderParams borderParams)
        {
            mgc.painter.DrawBorder(borderParams);
        }

        public static void Text(this MeshGenerationContext mgc, TextElement te)
        {
            if (TextUtilities.IsFontAssigned(te))
                mgc.painter.DrawText(te);
        }

        static Vector2 ConvertBorderRadiusPercentToPoints(Vector2 borderRectSize, Length length)
        {
            float x = length.value;
            float y = length.value;
            if (length.unit == LengthUnit.Percent)
            {
                x = borderRectSize.x * length.value / 100;
                y = borderRectSize.y * length.value / 100;
            }

            // Make sure to not return negative radius
            x = Mathf.Max(x, 0);
            y = Mathf.Max(y, 0);

            return new Vector2(x, y);
        }

        public static void GetVisualElementRadii(VisualElement ve, out Vector2 topLeft, out Vector2 bottomLeft, out Vector2 topRight, out Vector2 bottomRight)
        {
            IResolvedStyle style = ve.resolvedStyle;
            var borderRectSize = new Vector2(style.width, style.height);

            var computedStyle = ve.computedStyle;
            topLeft = ConvertBorderRadiusPercentToPoints(borderRectSize, computedStyle.borderTopLeftRadius);
            bottomLeft = ConvertBorderRadiusPercentToPoints(borderRectSize, computedStyle.borderBottomLeftRadius);
            topRight = ConvertBorderRadiusPercentToPoints(borderRectSize, computedStyle.borderTopRightRadius);
            bottomRight = ConvertBorderRadiusPercentToPoints(borderRectSize, computedStyle.borderBottomRightRadius);
        }

        public static void AdjustBackgroundSizeForBorders(VisualElement visualElement, ref Rect rect)
        {
            var style = visualElement.resolvedStyle;

            // If the border width allows it, slightly shrink the background size to avoid
            // having both the border and background blending together after antialiasing.
            if (style.borderLeftWidth >= 1.0f && style.borderLeftColor.a >= 1.0f) { rect.x += 0.5f; rect.width -= 0.5f; }
            if (style.borderTopWidth >= 1.0f && style.borderTopColor.a >= 1.0f) { rect.y += 0.5f; rect.height -= 0.5f; }
            if (style.borderRightWidth >= 1.0f && style.borderRightColor.a >= 1.0f) { rect.width -= 0.5f; }
            if (style.borderBottomWidth >= 1.0f && style.borderBottomColor.a >= 1.0f) { rect.height -= 0.5f; }
        }
    }

    /// <summary>
    /// Provides methods for generating a <see cref="VisualElement"/>'s visual content during the <see cref="generateVisualContent"/> callback.
    /// </summary>
    /// <remarks>
    /// <para>Visual content is generated by first allocating a mesh, using <see cref="Allocate"/>, and then filling the vertices and indices.</para>
    /// <para>
    /// If a texture is provided during the allocation, you can use the <see cref="Vertex.uv"/> vertex values to map it to the resulting mesh.
    /// To improve performance, the renderer can store the texture in an internal atlas. In that case, you must remap the UVs
    /// inside the <see cref="MeshWriteData.uvRegion"/> rectangle. If you do not remap the UVs, the texture may display incorrectly when atlassed.
    /// The following example demonstrates the correct way to generate UVs.
    /// </para>
    /// </remarks>
    ///<example>
    /// <code>
    /// <![CDATA[
    /// class TexturedElement : VisualElement
    /// {
    ///     static readonly Vertex[] k_Vertices = new Vertex[4];
    ///     static readonly ushort[] k_Indices = { 0, 1, 2, 2, 3, 0 };
    ///
    ///     static TexturedElement()
    ///     {
    ///         k_Vertices[0].tint = Color.white;
    ///         k_Vertices[1].tint = Color.white;
    ///         k_Vertices[2].tint = Color.white;
    ///         k_Vertices[3].tint = Color.white;
    ///     }
    ///
    ///     public TexturedElement()
    ///     {
    ///         generateVisualContent += OnGenerateVisualContent;
    ///         m_Texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/tex.png");
    ///     }
    ///
    ///     Texture2D m_Texture;
    ///
    ///     void OnGenerateVisualContent(MeshGenerationContext mgc)
    ///     {
    ///         Rect r = contentRect;
    ///         if (r.width < 0.01f || r.height < 0.01f)
    ///             return; // Skip rendering when too small.
    ///
    ///         float left = 0;
    ///         float right = r.width;
    ///         float top = 0;
    ///         float bottom = r.height;
    ///
    ///         k_Vertices[0].position = new Vector3(left, bottom, Vertex.nearZ);
    ///         k_Vertices[1].position = new Vector3(left, top, Vertex.nearZ);
    ///         k_Vertices[2].position = new Vector3(right, top, Vertex.nearZ);
    ///         k_Vertices[3].position = new Vector3(right, bottom, Vertex.nearZ);
    ///
    ///         MeshWriteData mwd = mgc.Allocate(k_Vertices.Length, k_Indices.Length, m_Texture);
    ///
    ///         // Since the texture may be stored in an atlas, the UV coordinates need to be
    ///         // adjusted. Simply rescale them in the provided uvRegion.
    ///         Rect uvRegion = mwd.uvRegion;
    ///         k_Vertices[0].uv = new Vector2(0, 0) * uvRegion.size + uvRegion.min;
    ///         k_Vertices[1].uv = new Vector2(0, 1) * uvRegion.size + uvRegion.min;
    ///         k_Vertices[2].uv = new Vector2(1, 1) * uvRegion.size + uvRegion.min;
    ///         k_Vertices[3].uv = new Vector2(1, 0) * uvRegion.size + uvRegion.min;
    ///
    ///         mwd.SetAllVertices(k_Vertices);
    ///         mwd.SetAllIndices(k_Indices);
    ///     }
    /// }
    /// ]]>
    /// </code>
    /// </example>
    public class MeshGenerationContext
    {
        [Flags]
        internal enum MeshFlags
        {
            None = 0,
            UVisDisplacement = 1 << 0,
            SkipDynamicAtlas = 1 << 1
        }

        /// <summary>
        /// The element for which <see cref="VisualElement.generateVisualContent"/> was invoked.
        /// </summary>
        public VisualElement visualElement { get { return painter.visualElement; } }

        /// <summary>
        /// The vector painter object used to issue drawing commands.
        /// </summary>
        public Painter2D painter2D
        {
            get
            {
                if (m_Painter2D == null)
                    m_Painter2D = new Painter2D(this);
                return m_Painter2D;
            }
        }
        private Painter2D m_Painter2D;

        internal bool hasPainter2D => m_Painter2D != null;

        internal MeshGenerationContext(IStylePainter painter) { this.painter = painter; }

        private static readonly ProfilerMarker s_AllocateMarker = new ProfilerMarker("UIR.MeshGenerationContext.Allocate");
        private static readonly ProfilerMarker s_DrawVectorImageMarker = new ProfilerMarker("UIR.MeshGenerationContext.DrawVectorImage");

        /// <summary>
        /// Allocates the specified number of vertices and indices required to express geometry for drawing the content of a <see cref="VisualElement"/>.
        /// </summary>
        /// <param name="vertexCount">The number of vertices to allocate. The maximum is 65535 (or UInt16.MaxValue).</param>
        /// <param name="indexCount">The number of triangle list indices to allocate. Each 3 indices represent one triangle, so this value should be multiples of 3.</param>
        /// <param name="texture">An optional texture to be applied on the triangles allocated. Pass null to rely on vertex colors only.</param>
        /// <remarks>
        /// See <see cref="Vertex.position"/> for details on geometry generation conventions. If a valid texture was passed, then the returned <see cref="MeshWriteData"/> will also describe a rectangle for the UVs to use to sample the passed texture. This is needed because textures passed to this API can be internally copied into a larger atlas.
        /// </remarks>
        /// <returns>An object that gives access to the newely allocated data. If the returned vertex count is 0, then allocation failed (the system ran out of memory).</returns>
        public MeshWriteData Allocate(int vertexCount, int indexCount, Texture texture = null)
        {
            using (s_AllocateMarker.Auto())
                return painter.DrawMesh(vertexCount, indexCount, texture, null, MeshFlags.None);
        }

        internal MeshWriteData Allocate(int vertexCount, int indexCount, Texture texture, Material material, MeshFlags flags)
        {
            using (s_AllocateMarker.Auto())
                return painter.DrawMesh(vertexCount, indexCount, texture, material, flags);
        }

        /// <summary>
        /// Draws a <see cref="VectorImage" /> asset.
        /// </summary>
        /// <param name="vectorImage">The vector image to draw.</param>
        /// <param name="offset">The position offset where to draw the vector image.</param>
        /// <param name="rotationAngle">The rotation of the vector image.</param>
        /// <param name="scale">The scale of the vector image</param>
        /// <remarks>
        ///
        /// <remarks>
        public void DrawVectorImage(VectorImage vectorImage, Vector2 offset, Angle rotationAngle, Vector2 scale)
        {
            using (s_DrawVectorImageMarker.Auto())
                painter.DrawVectorImage(vectorImage, offset, rotationAngle, scale);
        }

        /// <summary>
        /// Draw a string of text.
        /// </summary>
        /// <param name="text">The text to display.</param>
        /// <param name="pos">The start position where the text will be displayed.</param>
        /// <param name="fontSize">The font size to use.</param>
        /// <param name="color">The text color.</param>
        /// <param name="font">The font asset to use.</param>
        public void DrawText(string text, Vector2 pos, float fontSize, Color color, FontAsset font = null)
        {
            if (font == null)
                font = TextUtilities.GetFontAsset(visualElement);
            painter.DrawText(text, pos, fontSize, color, font);
        }

        internal IStylePainter painter;
    }
}
