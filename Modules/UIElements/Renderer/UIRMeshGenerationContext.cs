// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;

namespace UnityEngine.UIElements
{
    public struct Vertex
    {
        public readonly static float nearZ = -1.0f;

        public Vector3 position;
        public Color32 tint;
        public Vector2 uv;
        internal Color32 xformClipPages; // Top-left of xform and clip pages: XY,XY
        internal Color32 idsFlags; //XYZ (xform,clip,opacity) (W flags)
        internal Color32 opacityPageSVGSettingIndex; //XY (ZW SVG setting index)
        // Winding order of vertices matters. CCW is for clipped meshes.
    }

    public class MeshWriteData
    {
        internal MeshWriteData() {}  // Don't want users to instatiate this class themselves

        public int vertexCount { get { return m_Vertices.Length; } }
        public int indexCount { get { return m_Indices.Length; } }
        public Rect uvRegion { get { return m_UVRegion;  } }
        public void SetNextVertex(Vertex vertex) { m_Vertices[currentVertex++] = vertex; }
        public void SetNextIndex(UInt16 index) { m_Indices[currentIndex++] = index; }
        public void SetAllVertices(Vertex[] vertices)
        {
            if (currentVertex == 0)
            {
                m_Vertices.CopyFrom(vertices);
                currentVertex = m_Vertices.Length;
            }
            else throw new InvalidOperationException("SetAllVertices may not be called after using SetNextVertex");
        }

        public void SetAllIndices(UInt16[] indices)
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

            public static RectangleParams MakeTextured(Rect rect, Rect uv, Texture texture, ScaleMode scaleMode, ContextType panelContext)
            {
                var playmodeTintColor = panelContext == ContextType.Editor
                    ? UIElementsUtility.editorPlayModeTintColor
                    : Color.white;

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

            internal static TextParams MakeStyleBased(VisualElement ve, string text)
            {
                ComputedStyle style = ve.computedStyle;
                return new TextParams
                {
                    rect = ve.contentRect,
                    text = text,
                    font = style.unityFont.value,
                    fontSize = (int)style.fontSize.value.value,
                    fontStyle = style.unityFontStyleAndWeight.value,
                    fontColor = style.color.value,
                    anchor = style.unityTextAlign.value,
                    wordWrap = style.whiteSpace.value == WhiteSpace.Normal,
                    wordWrapWidth = style.whiteSpace.value == WhiteSpace.Normal ? ve.contentRect.width : 0.0f,
                    richText = false,
                    playmodeTintColor = ve.panel?.contextType == ContextType.Editor ? UIElementsUtility.editorPlayModeTintColor : Color.white
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

        public static void Text(this MeshGenerationContext mgc, TextParams textParams, TextHandle handle, float pixelsPerPoint)
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
            topLeft = ConvertBorderRadiusPercentToPoints(borderRectSize, computedStyle.borderTopLeftRadius.value);
            bottomLeft = ConvertBorderRadiusPercentToPoints(borderRectSize, computedStyle.borderBottomLeftRadius.value);
            topRight = ConvertBorderRadiusPercentToPoints(borderRectSize, computedStyle.borderTopRightRadius.value);
            bottomRight = ConvertBorderRadiusPercentToPoints(borderRectSize, computedStyle.borderBottomRightRadius.value);
        }
    }

    public class MeshGenerationContext
    {
        [Flags]
        internal enum MeshFlags { None, UVisDisplacement, IsSVGGradients, IsCustomSVGGradients }

        public VisualElement visualElement { get { return painter.visualElement; } }

        internal MeshGenerationContext(IStylePainter painter) { this.painter = painter; }

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
