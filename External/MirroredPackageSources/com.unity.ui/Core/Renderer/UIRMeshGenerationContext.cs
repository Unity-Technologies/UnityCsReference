using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;

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
        internal Color32 idsFlags; //XYZ (xform,clip,opacity) (W flags)
        internal Color32 opacityPageSVGSettingIndex; //XY (ZW SVG setting index)
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
        internal MeshWriteData() {}  // Don't want users to instatiate this class themselves

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
        }

        public struct RectangleParams
        {
            public Rect rect;
            public Rect uv;
            public Color color;
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

            public int leftSlice;
            public int topSlice;
            public int rightSlice;
            public int bottomSlice;

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

            public static RectangleParams MakeTextured(Rect rect, Rect uv, Texture texture, ScaleMode scaleMode, ContextType panelContext)
            {
                var playmodeTintColor = panelContext == ContextType.Editor
                    ? UIElementsUtility.editorPlayModeTintColor
                    : Color.white;

                AdjustUVsForScaleMode(rect, uv, texture, scaleMode, out rect, out uv);

                var rp = new RectangleParams
                {
                    rect = rect,
                    uv = uv,
                    color = Color.white,
                    texture = texture,
                    scaleMode = scaleMode,
                    playmodeTintColor = playmodeTintColor
                };
                return rp;
            }

            private static Rect ComputeUVRect(Sprite sprite)
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

            public static RectangleParams MakeSprite(Rect rect, Sprite sprite, ScaleMode scaleMode, ContextType panelContext, bool hasRadius, ref Vector4 slices)
            {
                if (sprite.texture == null)
                {
                    Debug.LogWarning($"Ignoring textureless sprite named \"{sprite.name}\", please import as a VectorImage instead");
                    return new RectangleParams();
                }

                var playmodeTintColor = panelContext == ContextType.Editor
                    ? UIElementsUtility.editorPlayModeTintColor
                    : Color.white;

                var uv = ComputeUVRect(sprite);
                AdjustUVsForScaleMode(rect, uv, sprite.texture, scaleMode, out rect, out uv);

                // Use a textured quad (ignoring tight-mesh) if dealing with slicing or with
                // scale-and-crop scale mode. This avoids expensive CPU-side transformation and
                // polygon clipping.
                var border = sprite.border;
                bool hasSlices = (border != Vector4.zero) || (slices != Vector4.zero);
                bool useFullTexture = (scaleMode == ScaleMode.ScaleAndCrop) || hasSlices || hasRadius;

                var rp = new RectangleParams
                {
                    rect = rect,
                    uv = uv,
                    color = Color.white,
                    texture = useFullTexture ? sprite.texture : (Texture2D)null,
                    sprite = useFullTexture ? (Sprite)null : sprite,
                    scaleMode = scaleMode,
                    playmodeTintColor = playmodeTintColor
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
                    uv = uv,
                    color = Color.white,
                    vectorImage = vectorImage,
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
        }

        public struct TextParams
        {
            public Rect rect;
            public string text;
            public Font font;
            public int fontSize;
            public FontStyle fontStyle;
            public Color fontColor;
            public TextAnchor anchor;
            public bool wordWrap;
            public float wordWrapWidth;
            public bool richText;
            public Material material;
            public Color playmodeTintColor;
            public TextOverflow textOverflow;
            public TextOverflowPosition textOverflowPosition;
            public OverflowInternal overflow;

            public override int GetHashCode()
            {
                var hashCode = rect.GetHashCode();
                hashCode = (hashCode * 397) ^ (text != null ? text.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (font != null ? font.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ fontSize;
                hashCode = (hashCode * 397) ^ (int)fontStyle;
                hashCode = (hashCode * 397) ^ fontColor.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)anchor;
                hashCode = (hashCode * 397) ^ wordWrap.GetHashCode();
                hashCode = (hashCode * 397) ^ wordWrapWidth.GetHashCode();
                hashCode = (hashCode * 397) ^ richText.GetHashCode();
                hashCode = (hashCode * 397) ^ (material != null ? material.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ playmodeTintColor.GetHashCode();
                hashCode = (hashCode * 397) ^ textOverflow.GetHashCode();
                hashCode = (hashCode * 397) ^ textOverflowPosition.GetHashCode();
                hashCode = (hashCode * 397) ^ overflow.GetHashCode();
                return hashCode;
            }

            internal static TextParams MakeStyleBased(VisualElement ve, string text)
            {
                var style = ve.computedStyle;
                return new TextParams
                {
                    rect = ve.contentRect,
                    text = text,
                    font = style.unityFont,
                    fontSize = (int)style.fontSize.value,
                    fontStyle = style.unityFontStyleAndWeight,
                    fontColor = style.color,
                    anchor = style.unityTextAlign,
                    wordWrap = style.whiteSpace == WhiteSpace.Normal,
                    wordWrapWidth = style.whiteSpace == WhiteSpace.Normal ? ve.contentRect.width : 0.0f,
                    richText = false,
                    playmodeTintColor = ve.panel?.contextType == ContextType.Editor ? UIElementsUtility.editorPlayModeTintColor : Color.white,
                    textOverflow = style.textOverflow,
                    textOverflowPosition = style.unityTextOverflowPosition,
                    overflow = style.overflow
                };
            }

            internal static TextNativeSettings GetTextNativeSettings(TextParams textParams, float scaling)
            {
                var settings = new TextNativeSettings
                {
                    text = textParams.text,
                    font = textParams.font,
                    size = textParams.fontSize,
                    scaling = scaling,
                    style = textParams.fontStyle,
                    color = textParams.fontColor,
                    anchor = textParams.anchor,
                    wordWrap = textParams.wordWrap,
                    wordWrapWidth = textParams.wordWrapWidth,
                    richText = textParams.richText
                };

                settings.color *= textParams.playmodeTintColor;

                return settings;
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

        public static void Text(this MeshGenerationContext mgc, TextParams textParams, ITextHandle handle, float pixelsPerPoint)
        {
            if (textParams.font != null)
                mgc.painter.DrawText(textParams, handle, pixelsPerPoint);
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
    }

    /// <summary>
    /// Offers functionality for generating visual content of a <see cref="VisualElement"/> during the <see cref="generateVisualContent"/> callback.
    /// </summary>
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

        internal MeshGenerationContext(IStylePainter painter) { this.painter = painter; }

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
            return painter.DrawMesh(vertexCount, indexCount, texture, null, MeshFlags.None);
        }

        internal MeshWriteData Allocate(int vertexCount, int indexCount, Texture texture, Material material, MeshFlags flags)
        {
            return painter.DrawMesh(vertexCount, indexCount, texture, material, flags);
        }

        internal IStylePainter painter;
    }
}
