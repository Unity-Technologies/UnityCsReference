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
        internal float transformID;   // Allocator gives an int, but we only take floats, so set to ((float)transformID)
        internal float clipRectID;    // Comes from the same pool as transformIDs
        internal float flags;         // Solid,Font,AtlasTextured,CustomTextured,Edge,SVG with gradients,...
        // Winding order of vertices matters. CCW is for clipped meshes.
    }

    public struct MeshWriteData
    {
        public int vertexCount { get { return m_Vertices.Length; } }
        public int indexCount { get { return m_Indices.Length; } }
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

        internal NativeSlice<Vertex> m_Vertices;
        internal NativeSlice<UInt16> m_Indices;
        int currentIndex, currentVertex;
    }

    internal static class MeshGenerationContextUtils
    {
        public struct BorderParams
        {
            public Rect rect;
            public Color color;

            public float leftWidth;
            public float topWidth;
            public float rightWidth;
            public float bottomWidth;

            public Vector2 topLeftRadius;
            public Vector2 topRightRadius;
            public Vector2 bottomRightRadius;
            public Vector2 bottomLeftRadius;

            public Material material;

            public static BorderParams MakeSimple(Rect rect, float width, Vector2 radius, Color color)
            {
                return new BorderParams()
                {
                    rect = rect,
                    color = color,
                    topWidth = width, rightWidth = width, bottomWidth = width, leftWidth = width,
                    topLeftRadius = radius, topRightRadius = radius, bottomRightRadius = radius, bottomLeftRadius = radius
                };
            }
        }

        public struct RectangleParams
        {
            public Rect rect;
            public Rect uv;
            public Color color;
            public Texture texture;
            public Material material;

            public Vector2 topLeftRadius;
            public Vector2 topRightRadius;
            public Vector2 bottomRightRadius;
            public Vector2 bottomLeftRadius;

            public int leftSlice;
            public int topSlice;
            public int rightSlice;
            public int bottomSlice;

            public static RectangleParams MakeSolid(Rect rect, Color color)
            {
                return new RectangleParams() { rect = rect, color = color, uv = new Rect(0, 0, 1, 1) };
            }

            public static RectangleParams MakeTextured(Rect rect, Rect uv, Texture texture, ScaleMode scaleMode)
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

                var rp = new RectangleParams() { rect = rect, uv = uv, color = Color.white, texture = texture };
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
                };
            }

            internal static TextNativeSettings GetTextNativeSettings(TextParams textParams, float scaling)
            {
                return new TextNativeSettings
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

        public static void Text(this MeshGenerationContext mgc, TextParams textParams)
        {
            if (textParams.font != null)
                mgc.painter.DrawText(textParams);
        }

        public static Vector2 GetVisualElementRadius(Length length, VisualElement parent)
        {
            float x = length.value;
            float y = length.value;
            if (length.unit == LengthUnit.Percent)
            {
                if (parent == null)
                    return Vector2.zero;

                x = parent.resolvedStyle.width * length.value / 100;
                y = parent.resolvedStyle.height * length.value / 100;
            }

            // Make sure to not return negative radius
            x = Mathf.Max(x, 0);
            y = Mathf.Max(y, 0);

            return new Vector2(x, y);
        }
    }

    public class MeshGenerationContext
    {
        [Flags]
        internal enum MeshFlags { None, UVisDisplacement };

        internal MeshGenerationContext(IStylePainter painter) { this.painter = painter; }

        public MeshWriteData Allocate(int vertexCount, int indexCount)
        {
            return painter.DrawMesh(vertexCount, indexCount, null, MeshFlags.None);
        }

        internal MeshWriteData Allocate(int vertexCount, int indexCount, Material material, MeshFlags flags)
        {
            return painter.DrawMesh(vertexCount, indexCount, material, flags);
        }

        internal IStylePainter painter;
    }
}
