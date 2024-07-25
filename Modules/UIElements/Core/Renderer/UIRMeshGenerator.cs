// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore.Text;
using static UnityEngine.UIElements.MeshGenerationContext;

namespace UnityEngine.UIElements.UIR
{
    // For tests
    interface IMeshGenerator
    {
        VisualElement currentElement { get; set; }
        UITKTextJobSystem textJobSystem { get; set; }
        public void DrawText(List<NativeSlice<Vertex>> vertices, List<NativeSlice<ushort>> indices, List<Material> materials, List<GlyphRenderMode> renderModes);
        public void DrawText(string text, Vector2 pos, float fontSize, Color color, FontAsset font);
        public void DrawNativeText(NativeTextInfo textInfo, Vector2 pos);
        public void DrawRectangle(MeshGenerator.RectangleParams rectParams);
        public void DrawBorder(MeshGenerator.BorderParams borderParams);
        public void DrawVectorImage(VectorImage vectorImage, Vector2 offset, Angle rotationAngle, Vector2 scale);
        public void DrawRectangleRepeat(MeshGenerator.RectangleParams rectParams, Rect totalRect, float scaledPixelsPerPoint);

        public void ScheduleJobs(MeshGenerationContext mgc);
    }

    class MeshGenerator : IMeshGenerator, IDisposable
    {
        struct RepeatRectUV
        {
            public Rect rect;
            public Rect uv;
        }

        static readonly ProfilerMarker k_MarkerDrawRectangle = new("MeshGenerator.DrawRectangle");
        static readonly ProfilerMarker k_MarkerDrawBorder = new("MeshGenerator.DrawBorder");
        static readonly ProfilerMarker k_MarkerDrawVectorImage = new("MeshGenerator.DrawVectorImage");
        static readonly ProfilerMarker k_MarkerDrawRectangleRepeat = new("MeshGenerator.DrawRectangleRepeat");

        MeshGenerationContext m_MeshGenerationContext;

        List<RepeatRectUV>[] m_RepeatRectUVList = null;

        GCHandlePool m_GCHandlePool = new();

        NativeArray<TessellationJobParameters> m_JobParameters;

        public MeshGenerator(MeshGenerationContext mgc)
        {
            m_MeshGenerationContext = mgc;
            m_OnMeshGenerationDelegate = OnMeshGeneration;
            textJobSystem = new UITKTextJobSystem();
        }

        public VisualElement currentElement { get; set; }

        public UITKTextJobSystem textJobSystem { get; set; }

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

            // Rectangle which clip the resulting tessellated geometry for background repeat to correctly support rounded corners.
            // When backgroundRepeatRect is not empty, it represent the clipped portion of the original visual element (represented by rect)
            // that should be repeated.
            public Rect backgroundRepeatRect;

            // Allow support of background-properties
            public BackgroundPosition backgroundPositionX;
            public BackgroundPosition backgroundPositionY;
            public BackgroundRepeat backgroundRepeat;
            public BackgroundSize backgroundSize;

            public Texture texture;
            public Sprite sprite;
            public VectorImage vectorImage;
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

            // Inset to apply before rendering (left, top, right, bottom)
            public Vector4 rectInset;

            // The color allocation
            internal ColorPage colorPage;

            internal MeshGenerationContext.MeshFlags meshFlags;

            public static RectangleParams MakeSolid(Rect rect, Color color, Color playModeTintColor)
            {
                return new RectangleParams
                {
                    rect = rect,
                    color = color,
                    uv = new Rect(0, 0, 1, 1),
                    playmodeTintColor = playModeTintColor
                };
            }

            private static void AdjustUVsForScaleMode(Rect rect, Rect uv, Texture texture, ScaleMode scaleMode, out Rect rectOut, out Rect uvOut)
            {
                // Fill the UVs according to scale mode
                // Comparing aspects ratio is error-prone because the screenRect may end up being scaled by the
                // transform and the corners will end up being pixel aligned, possibly resulting in blurriness.

                // UUM-17136: uv width/height can be negative (e.g. when the UVs are flipped)
                float srcAspect = Mathf.Abs((texture.width * uv.width) / (texture.height * uv.height));
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

            private static void AdjustSpriteUVsForScaleMode(Rect containerRect, Rect srcRect, Rect spriteGeomRect, Sprite sprite, ScaleMode scaleMode, out Rect rectOut, out Rect uvOut)
            {
                // Adjust the sprite rect size and then determine where the sprite geometry should be inside it.

                float srcAspect = sprite.rect.width / sprite.rect.height;
                float destAspect = containerRect.width / containerRect.height;

                // Normalize the geom rect for easy scaling
                var geomRectNorm = spriteGeomRect;
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
                        var scale = containerRect.size;
                        containerRect.position = geomRectNorm.position * scale;
                        containerRect.size = geomRectNorm.size * scale;
                    }
                    break;

                    case ScaleMode.ScaleAndCrop:
                    {
                        // This is the complex code path. Scale-and-crop works like the following:
                        // - Scale the sprite rect to match the largest destination rect size
                        // - Evaluate the sprite geometry rect inside that scaled sprite rect
                        // - Compute the intersection of the geometry rect with the destination rect
                        // - Re-evaluate the UVs from that intersection

                        var stretchedRect = containerRect;
                        if (destAspect > srcAspect)
                        {
                            stretchedRect.height = stretchedRect.width / srcAspect;
                            stretchedRect.position = new Vector2(stretchedRect.position.x, -(stretchedRect.height - containerRect.height) / 2.0f);
                        }
                        else
                        {
                            stretchedRect.width = stretchedRect.height * srcAspect;
                            stretchedRect.position = new Vector2(-(stretchedRect.width - containerRect.width) / 2.0f, stretchedRect.position.y);
                        }

                        var scale = stretchedRect.size;
                        stretchedRect.position += geomRectNorm.position * scale;
                        stretchedRect.size = geomRectNorm.size * scale;

                        // Intersect the stretched rect with the destination rect to compute the new UVs
                        var newRect = RectIntersection(containerRect, stretchedRect);
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

                            srcRect.position += uvScale.position * srcRect.size;
                            srcRect.size *= uvScale.size;
                        }

                        containerRect = newRect;
                    }
                    break;

                    case ScaleMode.ScaleToFit:
                    {
                        if (destAspect > srcAspect)
                        {
                            float stretch = srcAspect / destAspect;
                            containerRect = new Rect(containerRect.xMin + containerRect.width * (1.0f - stretch) * .5f, containerRect.yMin, stretch * containerRect.width, containerRect.height);
                        }
                        else
                        {
                            float stretch = destAspect / srcAspect;
                            containerRect = new Rect(containerRect.xMin, containerRect.yMin + containerRect.height * (1.0f - stretch) * .5f, containerRect.width, stretch * containerRect.height);
                        }

                        containerRect.position += geomRectNorm.position * containerRect.size;
                        containerRect.size *= geomRectNorm.size;
                    }
                    break;

                    default:
                        throw new NotImplementedException();
                }


                rectOut = containerRect;
                uvOut = srcRect;
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

            public static RectangleParams MakeTextured(Rect rect, Rect uv, Texture texture, ScaleMode scaleMode, Color playModeTintColor)
            {
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
                    playmodeTintColor = playModeTintColor
                };
                return rp;
            }

            public static RectangleParams MakeSprite(Rect containerRect, Rect subRect, Sprite sprite, ScaleMode scaleMode, Color playModeTintColor, bool hasRadius, ref Vector4 slices, bool useForRepeat = false)
            {
                if (sprite == null || sprite.bounds.size.x < UIRUtility.k_Epsilon || sprite.bounds.size.y < UIRUtility.k_Epsilon)
                    return new RectangleParams();

                if (sprite.texture == null)
                {
                    Debug.LogWarning($"Ignoring textureless sprite named \"{sprite.name}\", please import as a VectorImage instead");
                    return new RectangleParams();
                }

                var spriteGeomRect = ComputeGeomRect(sprite); // Min/Max Positions in the sprite
                var spriteUVRect = ComputeUVRect(sprite); // Min/Max UVs in the sprite

                // Use a textured quad (ignoring tight-mesh) if dealing with slicing or with
                // scale-and-crop scale mode. This avoids expensive CPU-side transformation and
                // polygon clipping.
                var border = sprite.border;
                bool hasSlices = (border != Vector4.zero) || (slices != Vector4.zero);
                bool hasSubRect = subRect != new Rect(0, 0, 1, 1); // In the future, we could implement flips with geometry flip
                bool useTexturedQuad = (scaleMode == ScaleMode.ScaleAndCrop) || hasSlices || hasRadius || useForRepeat || hasSubRect;

                // The sprite UVs are adjusted according to the rotation. But When we use a texture quad, we generate
                // the UVs ourselves, so we need to apply the rotation to our rect.
                if (useTexturedQuad && sprite.packed && sprite.packingRotation != SpritePackingRotation.None)
                    spriteUVRect = ApplyPackingRotation(spriteUVRect, sprite.packingRotation);

                Rect srcRect;
                if (hasSubRect)
                {
                    // Remap the subRect within the sprite rect
                    srcRect = subRect;
                    srcRect.position *= spriteUVRect.size;
                    srcRect.position += spriteUVRect.position;
                    srcRect.size *= spriteUVRect.size;
                }
                else
                    srcRect = spriteUVRect;

                AdjustSpriteUVsForScaleMode(containerRect, srcRect, spriteGeomRect, sprite, scaleMode, out Rect adjustedDstRect, out Rect adjustedSrcRect);

                // Compute normalized subRect
                var normalizedRect = spriteGeomRect;
                normalizedRect.size /= (Vector2)sprite.bounds.size;
                normalizedRect.position -= (Vector2)sprite.bounds.min;
                normalizedRect.position /= (Vector2)sprite.bounds.size;
                normalizedRect.position = new Vector2(normalizedRect.position.x, 1.0f - (normalizedRect.position.y + normalizedRect.height)); // Y-down for UIR

                var rp = new RectangleParams
                {
                    rect = adjustedDstRect,
                    uv = adjustedSrcRect,
                    subRect = normalizedRect,
                    color = Color.white,
                    texture = useTexturedQuad ? sprite.texture : null,
                    sprite = useTexturedQuad ? null : sprite,
                    contentSize = sprite.rect.size,
                    textureSize = new Vector2(sprite.texture.width, sprite.texture.height),
                    spriteGeomRect = spriteGeomRect,
                    scaleMode = scaleMode,
                    playmodeTintColor = playModeTintColor,
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

            public static RectangleParams MakeVectorTextured(Rect rect, Rect uv, VectorImage vectorImage, ScaleMode scaleMode, Color playModeTintColor)
            {
                var rp = new RectangleParams
                {
                    rect = rect,
                    subRect = new Rect(0,0,1,1),
                    uv = uv,
                    color = Color.white,
                    vectorImage = vectorImage,
                    contentSize = new Vector2(vectorImage.width, vectorImage.height),
                    scaleMode = scaleMode,
                    playmodeTintColor = playModeTintColor
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

            internal MeshBuilderNative.NativeRectParams ToNativeParams()
            {
                return new MeshBuilderNative.NativeRectParams() {
                    rect = rect,
                    subRect = subRect,
                    backgroundRepeatRect = backgroundRepeatRect,
                    uv = uv,
                    color = color,
                    scaleMode = scaleMode,
                    topLeftRadius = topLeftRadius,
                    topRightRadius = topRightRadius,
                    bottomRightRadius = bottomRightRadius,
                    bottomLeftRadius = bottomLeftRadius,
                    spriteGeomRect = spriteGeomRect,
                    contentSize = contentSize,
                    textureSize = textureSize,
                    texturePixelsPerPoint = texture is Texture2D ? (texture as Texture2D).pixelsPerPoint : 1.0f,
                    leftSlice = leftSlice,
                    topSlice = topSlice,
                    rightSlice = rightSlice,
                    bottomSlice = bottomSlice,
                    sliceScale = sliceScale,
                    rectInset = rectInset,
                    colorPage = colorPage.ToNativeColorPage(),
                    meshFlags = (int)meshFlags
                };
            }
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

        public static void AdjustBackgroundSizeForBorders(VisualElement visualElement, ref MeshGenerator.RectangleParams rectParams)
        {
            var style = visualElement.resolvedStyle;

            var inset = Vector4.zero;

            // If the border width allows it, slightly shrink the background size to avoid
            // having both the border and background blending together after antialiasing.
            if (style.borderLeftWidth >= 1.0f && style.borderLeftColor.a >= 1.0f) { inset.x = 0.5f; }
            if (style.borderTopWidth >= 1.0f && style.borderTopColor.a >= 1.0f) { inset.y = 0.5f; }
            if (style.borderRightWidth >= 1.0f && style.borderRightColor.a >= 1.0f) { inset.z = 0.5f; }
            if (style.borderBottomWidth >= 1.0f && style.borderBottomColor.a >= 1.0f) { inset.w = 0.5f; }

            rectParams.rectInset = inset;
        }

        public void DrawText(List<NativeSlice<Vertex>> vertices, List<NativeSlice<ushort>> indices, List<Material> materials, List<GlyphRenderMode> renderModes)
        {
            DrawTextInfo(vertices, indices, materials, renderModes);
        }

        TextCore.Text.TextInfo m_TextInfo = new TextCore.Text.TextInfo(VertexDataLayout.VBO);
        TextCore.Text.TextGenerationSettings m_Settings = new TextCore.Text.TextGenerationSettings()
        {
            screenRect = Rect.zero,
            richText = true,
            inverseYAxis = true
        };

        List<NativeSlice<Vertex>> m_VerticesArray = new List<NativeSlice<Vertex>>();
        List<NativeSlice<ushort>> m_IndicesArray = new List<NativeSlice<ushort>>();
        List<Material> m_Materials = new List<Material>();
        List<GlyphRenderMode> m_RenderModes = new List<GlyphRenderMode>();

        public void DrawText(string text, Vector2 pos, float fontSize, Color color, FontAsset font)
        {
            var textSettings = TextUtilities.GetTextSettingsFrom(currentElement);

            m_TextInfo.Clear();
            m_Settings.text = text;
            m_Settings.fontAsset = font;
            m_Settings.textSettings = textSettings;
            m_Settings.fontSize = fontSize;
            m_Settings.color = color;
            m_Settings.material = font.material;
            m_Settings.textWrappingMode = TextWrappingMode.NoWrap;

            TextCore.Text.TextGenerator.GetTextGenerator().GenerateText(m_Settings, m_TextInfo);

            DrawTextBase(m_TextInfo, new NativeTextInfo(), pos, false);
        }

        public void DrawNativeText(NativeTextInfo textInfo, Vector2 pos)
        {
            DrawTextBase(null, textInfo, pos, true);

            // Call Texture.Apply for all texture still dirty
            // There are no other place where we are calling this to export the texture to the gpu
            // for the ATG. This is as late as it could be right now.

            // I am putting it here as this will only be call if an ATG-text has been modified
            // and it will not be called when non-atg text only are modified
            // Trying to keep the codepath separated for now.

            // Finally, calling this once per text element is not optimal but the code underneath
            // should simply retrun if there is nothing to apply
            FontAsset.UpdateFontAssetsInUpdateQueue();
        }

        void DrawTextBase(TextCore.Text.TextInfo textInfo, NativeTextInfo nativeTextInfo, Vector2 pos, bool isNative)
        {
            for (int i = 0, meshInfoCount = isNative ? nativeTextInfo.meshInfos.Length : textInfo.meshInfo.Length; i < meshInfoCount; i++)
            {
                MeshInfo meshInfo = new();
                FontAsset fa = null;
                int remainingVertexCount;
                if (!isNative)
                {
                    meshInfo = textInfo.meshInfo[i];
                    Debug.Assert((meshInfo.vertexCount & 0b11) == 0); // Quads only
                    remainingVertexCount = meshInfo.vertexCount;
                }
                else
                {
                    int glyphAmount = nativeTextInfo.meshInfos[i].textElementInfos.Length;
                    remainingVertexCount = glyphAmount * 4;
                    fa = nativeTextInfo.meshInfos[i].fontAsset;
                }

                int verticesPerAlloc = (int)(UIRenderDevice.maxVerticesPerPage & ~3); // Round down to multiple of 4

                while (remainingVertexCount > 0)
                {
                    int vertexCount = Mathf.Min(remainingVertexCount, verticesPerAlloc);
                    int quadCount = vertexCount >> 2;
                    int indexCount = quadCount * 6;

                    m_Materials.Add(isNative ? fa.material : meshInfo.material);
                    m_RenderModes.Add(isNative ? fa.atlasRenderMode : meshInfo.glyphRenderMode);

                    m_MeshGenerationContext.AllocateTempMesh(vertexCount, indexCount, out var vertices, out var indices);

                    for (int vDst = 0, vSrc = 0, j = 0; vDst < vertexCount; vDst += 4, vSrc += 1, j += 6)
                    {
                        if (isNative)
                        {
                            vertices[vDst + 0] = ConvertTextVertexToUIRVertex(nativeTextInfo.meshInfos[i].textElementInfos[vSrc].bottomLeft, pos);
                            vertices[vDst + 1] = ConvertTextVertexToUIRVertex(nativeTextInfo.meshInfos[i].textElementInfos[vSrc].topLeft, pos);
                            vertices[vDst + 2] = ConvertTextVertexToUIRVertex(nativeTextInfo.meshInfos[i].textElementInfos[vSrc].topRight, pos);
                            vertices[vDst + 3] = ConvertTextVertexToUIRVertex(nativeTextInfo.meshInfos[i].textElementInfos[vSrc].bottomRight, pos);
                        }
                        else
                        {
                            vertices[vDst + 0] = ConvertTextVertexToUIRVertex(meshInfo.vertexData[vDst + 0], pos);
                            vertices[vDst + 1] = ConvertTextVertexToUIRVertex(meshInfo.vertexData[vDst + 1], pos);
                            vertices[vDst + 2] = ConvertTextVertexToUIRVertex(meshInfo.vertexData[vDst + 2], pos);
                            vertices[vDst + 3] = ConvertTextVertexToUIRVertex(meshInfo.vertexData[vDst + 3], pos);
                        }

                        indices[j + 0] = (ushort)(vDst + 0);
                        indices[j + 1] = (ushort)(vDst + 1);
                        indices[j + 2] = (ushort)(vDst + 2);
                        indices[j + 3] = (ushort)(vDst + 2);
                        indices[j + 4] = (ushort)(vDst + 3);
                        indices[j + 5] = (ushort)(vDst + 0);
                    }

                    m_VerticesArray.Add(vertices);
                    m_IndicesArray.Add(indices);

                    remainingVertexCount -= vertexCount;
                }
                Debug.Assert(remainingVertexCount == 0);
            }

            DrawTextInfo(m_VerticesArray, m_IndicesArray, m_Materials, m_RenderModes);

            m_VerticesArray.Clear();
            m_IndicesArray.Clear();
            m_Materials.Clear();
            m_RenderModes.Clear();
        }

        void DrawTextInfo(List<NativeSlice<Vertex>> vertices, List<NativeSlice<ushort>> indices, List<Material> materials, List<GlyphRenderMode> renderModes)
        {
            if (vertices == null)
                return;

            for (int i = 0, drawCount = vertices.Count; i < drawCount; i++)
            {
                if (vertices[i].Length == 0)
                    continue;

                // SpriteAssets and Color Glyphs use an RGBA texture
                if (((Texture2D)materials[i].mainTexture).format != TextureFormat.Alpha8)
                {
                    // Assume a sprite asset or Color Glyph
                    MakeText(
                        materials[i].mainTexture,
                        vertices[i],
                        indices[i],
                        false,
                        0,
                        0);
                }
                else
                {
                    // SDF scale is used to differentiate between Bitmap and SDF. The Bitmap Material doesn't have the
                    // GradientScale property which results in sdfScale always being 0.

                    float sdfScale = 0;
                    if (!TextGeneratorUtilities.IsBitmapRendering(renderModes[i]))
                        sdfScale = materials[i].GetFloat(TextShaderUtilities.ID_GradientScale);

                    var sharpnessId = TextShaderUtilities.ID_Sharpness;
                    var sharpness = materials[i].HasProperty(sharpnessId) ? materials[i].GetFloat(sharpnessId) : 0.0f;
                    // Set the dynamic-color hint on TextCore fancy-text or the EditorUIE shader applies the
                    // tint over the fragment output, affecting the outline/shadows.
                    if (sharpness == 0.0f && currentElement.panel.contextType == ContextType.Editor)
                    {
                        sharpness = TextUtilities.textSettings.GetEditorTextSharpness();
                    }

                    MakeText(
                        materials[i].mainTexture,
                        vertices[i],
                        indices[i],
                        true,
                        sdfScale,
                        sharpness);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vertex ConvertTextVertexToUIRVertex(TextCoreVertex vertex, Vector2 posOffset, bool isDynamicColor = false)
        {
            float dilate = 0.0f;
            // If Bold, dilate the shape (this value is hardcoded, should be set from the font actual bold weight)
            if (vertex.uv2.y < 0.0f) dilate = 1.0f;
            return new Vertex
            {
                position = new Vector3(vertex.position.x + posOffset.x, vertex.position.y + posOffset.y, UIRUtility.k_MeshPosZ),
                uv = new Vector2(vertex.uv0.x, vertex.uv0.y),
                tint = vertex.color,
                // TODO: Don't set the flags here. The mesh conversion should perform these changes
                flags = new Color32(0, (byte)(dilate * 255), 0, isDynamicColor ? (byte)1 : (byte)0)
            };
        }

        void MakeText(Texture texture, NativeSlice<Vertex> vertices, NativeSlice<ushort> indices, bool isSdf, float sdfScale, float sharpness)
        {
            if (isSdf)
                m_MeshGenerationContext.entryRecorder.DrawSdfText(m_MeshGenerationContext.parentEntry, vertices, indices, texture, sdfScale, sharpness);
            else
                m_MeshGenerationContext.entryRecorder.DrawMesh(m_MeshGenerationContext.parentEntry, vertices, indices, texture, true);
        }

        public void DrawRectangle(RectangleParams rectParams)
        {
            if (rectParams.rect.width < UIRUtility.k_Epsilon || rectParams.rect.height < UIRUtility.k_Epsilon)
                return; // Nothing to draw

            k_MarkerDrawRectangle.Begin();
            if (currentElement.panel.contextType == ContextType.Editor)
                rectParams.color *= rectParams.playmodeTintColor;

            var rectangleJobParameters = new TessellationJobParameters() { isBorderJob = false, rectParams = rectParams.ToNativeParams() };

            rectangleJobParameters.rectParams.texture = m_GCHandlePool.GetIntPtr(rectParams.texture);
            rectangleJobParameters.rectParams.sprite = m_GCHandlePool.GetIntPtr(rectParams.sprite);
            if (rectParams.sprite != null && rectParams.sprite.texture != null)
            {
                rectangleJobParameters.rectParams.spriteTexture = m_GCHandlePool.GetIntPtr(rectParams.sprite.texture);
                rectangleJobParameters.rectParams.spriteVertices = m_GCHandlePool.GetIntPtr(rectParams.sprite.vertices);
                rectangleJobParameters.rectParams.spriteUVs = m_GCHandlePool.GetIntPtr(rectParams.sprite.uv);
                rectangleJobParameters.rectParams.spriteTriangles = m_GCHandlePool.GetIntPtr(rectParams.sprite.triangles);
            }
            rectangleJobParameters.rectParams.vectorImage = m_GCHandlePool.GetIntPtr(rectParams.vectorImage);

            bool isUsingGradients = rectParams.vectorImage?.atlas != null;
            rectangleJobParameters.rectParams.meshFlags |= isUsingGradients ? (int)MeshFlags.IsUsingVectorImageGradients : (int)MeshFlags.None;

            m_MeshGenerationContext.InsertUnsafeMeshGenerationNode(out var unsafeNode);
            rectangleJobParameters.node = unsafeNode;
            m_TesselationJobParameters.Add(rectangleJobParameters);

            k_MarkerDrawRectangle.End();
        }

        public void DrawBorder(BorderParams borderParams)
        {
            k_MarkerDrawBorder.Begin();
            if (currentElement.panel.contextType == ContextType.Editor)
            {
                borderParams.leftColor *= borderParams.playmodeTintColor;
                borderParams.topColor *= borderParams.playmodeTintColor;
                borderParams.rightColor *= borderParams.playmodeTintColor;
                borderParams.bottomColor *= borderParams.playmodeTintColor;
            }

            var borderJobParams = new TessellationJobParameters() { isBorderJob = true, borderParams = borderParams };
            m_MeshGenerationContext.InsertUnsafeMeshGenerationNode(out var unsafeNode);
            borderJobParams.node = unsafeNode;
            m_TesselationJobParameters.Add(borderJobParams);

            k_MarkerDrawBorder.End();
        }

        public void DrawVectorImage(VectorImage vectorImage, Vector2 offset, Angle rotationAngle, Vector2 scale)
        {
            if (vectorImage == null || vectorImage.vertices.Length == 0 || vectorImage.indices.Length == 0)
                return;

            k_MarkerDrawVectorImage.Begin();
            m_MeshGenerationContext.AllocateTempMesh(vectorImage.vertices.Length, vectorImage.indices.Length, out NativeSlice<Vertex> vertices, out NativeSlice<ushort> indices);

            bool hasGradients = vectorImage.atlas != null;
            if (hasGradients)
                m_MeshGenerationContext.entryRecorder.DrawGradients(m_MeshGenerationContext.parentEntry, vertices, indices, vectorImage);
            else
                m_MeshGenerationContext.entryRecorder.DrawMesh(m_MeshGenerationContext.parentEntry, vertices, indices);

            var matrix = Matrix4x4.TRS(offset, Quaternion.AngleAxis(rotationAngle.ToDegrees(), Vector3.forward), new Vector3(scale.x, scale.y, 1.0f));
            bool flipWinding = (scale.x < 0.0f) ^ (scale.y < 0.0f);

            int vertexCount = vectorImage.vertices.Length;
            for (int i = 0; i < vertexCount; ++i)
            {
                var v = vectorImage.vertices[i];
                var p = matrix.MultiplyPoint3x4(v.position);
                p.z = Vertex.nearZ;
                var si = new Color32((byte)(v.settingIndex >> 8), (byte)v.settingIndex, 0, 0);

                vertices[i] = new Vertex { position = p, tint = v.tint, uv = v.uv, settingIndex = si, flags = v.flags, circle = v.circle };
            }

            if (!flipWinding)
                indices.CopyFrom(vectorImage.indices);
            else
            {
                var srcIndices = vectorImage.indices;
                for (int i = 0; i < srcIndices.Length; i += 3)
                {
                    indices[i + 0] = srcIndices[i + 0];
                    indices[i + 1] = srcIndices[i + 2];
                    indices[i + 2] = srcIndices[i + 1];
                }
            }

            k_MarkerDrawVectorImage.End();
        }

        public void DrawRectangleRepeat(RectangleParams rectParams, Rect totalRect, float scaledPixelsPerPoint)
        {
            k_MarkerDrawRectangleRepeat.Begin();
            DoDrawRectangleRepeat(ref rectParams, totalRect, scaledPixelsPerPoint);
            k_MarkerDrawRectangleRepeat.End();
        }

        // This method should not be called directly. Use the DrawRectangleRepeat wrapper instead which is properly
        // instrumented with performance counters.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void DoDrawRectangleRepeat(ref RectangleParams rectParams, Rect totalRect, float scaledPixelsPerPoint)
        {
            var uv = new Rect(0, 0, 1, 1);

            if (m_RepeatRectUVList == null)
            {
                m_RepeatRectUVList = new List<RepeatRectUV>[2];
                m_RepeatRectUVList[0] = new List<RepeatRectUV>();
                m_RepeatRectUVList[1] = new List<RepeatRectUV>();
            }
            else
            {
                m_RepeatRectUVList[0].Clear();
                m_RepeatRectUVList[1].Clear();
            }

            // Compute the destination size for one repetition before clipping/offset is considered.
            var targetRect = rectParams.rect;
            if (rectParams.backgroundSize.sizeType != BackgroundSizeType.Length)
            {
                if (rectParams.backgroundSize.sizeType == BackgroundSizeType.Contain)
                {
                    float ratioX = totalRect.width / targetRect.width;
                    float ratioY = totalRect.height / targetRect.height;

                    // The source is uniformly scaled to fit inside the total rect.
                    // At this point, we ignore repetitions that may be used to fill the voids.
                    Rect rect = targetRect;
                    if (ratioX < ratioY)
                    {
                        rect.width = totalRect.width;
                        rect.height = targetRect.height * totalRect.width / targetRect.width;
                    }
                    else
                    {
                        rect.width = targetRect.width * totalRect.height / targetRect.height;
                        rect.height = totalRect.height;
                    }

                    targetRect = rect;
                }
                else if (rectParams.backgroundSize.sizeType == BackgroundSizeType.Cover)
                {
                    float ratioX = totalRect.width / targetRect.width;
                    float ratioY = totalRect.height / targetRect.height;

                    // The source is uniformly scaled to completely cover the total rect.
                    // At this point, we ignore cropping, but it will happen later.
                    Rect rect = targetRect;
                    if (ratioX > ratioY)
                    {
                        rect.width = totalRect.width;
                        rect.height = targetRect.height * totalRect.width / targetRect.width;
                    }
                    else
                    {
                        rect.width = targetRect.width * totalRect.height / targetRect.height;
                        rect.height = totalRect.height;
                    }

                    targetRect = rect;
                }
            }
            else
            {
                if (!rectParams.backgroundSize.x.IsNone() || !rectParams.backgroundSize.y.IsNone())
                {
                    if ((!rectParams.backgroundSize.x.IsNone()) && (rectParams.backgroundSize.y.IsAuto()))
                    {
                        Rect rect = targetRect;
                        if (rectParams.backgroundSize.x.unit == LengthUnit.Percent)
                        {
                            rect.width = totalRect.width * rectParams.backgroundSize.x.value / 100.0f;
                            rect.height = rect.width * targetRect.height / targetRect.width;
                        }
                        else if (rectParams.backgroundSize.x.unit == LengthUnit.Pixel)
                        {
                            rect.width = rectParams.backgroundSize.x.value;
                            rect.height = rect.width * targetRect.height / targetRect.width;
                        }
                        targetRect = rect;
                    }
                    else if ((!rectParams.backgroundSize.x.IsNone()) && (!rectParams.backgroundSize.y.IsNone()))
                    {
                        Rect rect = targetRect;
                        if (!rectParams.backgroundSize.x.IsAuto())
                        {
                            if (rectParams.backgroundSize.x.unit == LengthUnit.Percent)
                            {
                                rect.width = totalRect.width * rectParams.backgroundSize.x.value / 100.0f;
                            }
                            else if (rectParams.backgroundSize.x.unit == LengthUnit.Pixel)
                            {
                                rect.width = rectParams.backgroundSize.x.value;
                            }
                        }

                        if (!rectParams.backgroundSize.y.IsAuto())
                        {
                            if (rectParams.backgroundSize.y.unit == LengthUnit.Percent)
                            {
                                rect.height = totalRect.height * rectParams.backgroundSize.y.value / 100.0f;
                            }
                            else if (rectParams.backgroundSize.y.unit == LengthUnit.Pixel)
                            {
                                rect.height = rectParams.backgroundSize.y.value;
                            }

                            if (rectParams.backgroundSize.x.IsAuto())
                            {
                                rect.width = rect.height * targetRect.width / targetRect.height;
                            }
                        }
                        targetRect = rect;
                    }
                }
            }

            // Skip invalid size
            if ((targetRect.size.x <= UIRUtility.k_Epsilon) || (targetRect.size.y <= UIRUtility.k_Epsilon))
            {
                return;
            }

            // Skip empty background
            if ((totalRect.size.x <= UIRUtility.k_Epsilon) || (totalRect.size.y <= UIRUtility.k_Epsilon))
            {
                return;
            }

            // Adjust size when background-repeat is round and other axis background-size is auto
            if ((rectParams.backgroundSize.x.IsAuto()) && (rectParams.backgroundRepeat.y == Repeat.Round))
            {
                float invTargetHeight = 1f / targetRect.height;
                int count = (int)(totalRect.height * invTargetHeight + 0.5f);
                count = Math.Max(count, 1);

                Rect rect = new Rect();
                rect.height = totalRect.height / count;
                rect.width = rect.height * targetRect.width * invTargetHeight;
                targetRect = rect;
            }
            else if ((rectParams.backgroundSize.y.IsAuto()) && (rectParams.backgroundRepeat.x == Repeat.Round))
            {
                float invTargetWidth = 1f / targetRect.width;
                int count = (int)(totalRect.width * invTargetWidth + 0.5f);
                count = Math.Max(count, 1);

                Rect rect = new Rect();
                rect.width = totalRect.width / count;
                rect.height = rect.width * targetRect.height * invTargetWidth;
                targetRect = rect;
            }

            for (int axis = 0; axis < 2; ++axis)
            {
                Repeat repeat = (axis == 0) ? rectParams.backgroundRepeat.x : rectParams.backgroundRepeat.y;

                BackgroundPosition backgroundPosition = (axis == 0) ? rectParams.backgroundPositionX : rectParams.backgroundPositionY;

                float linear_size = 0;
                if (repeat == Repeat.NoRepeat)
                {
                    RepeatRectUV repeatRectUV;
                    Rect rect = targetRect;

                    repeatRectUV.uv = uv;
                    repeatRectUV.rect = rect;
                    linear_size = rect.size[axis];
                    m_RepeatRectUVList[axis].Add(repeatRectUV);
                }
                else if (repeat == Repeat.Repeat)
                {
                    Rect rect = targetRect;

                    // We might attempt to align the texture on the pixel grid. In this process, we will offset the
                    // texture by a fraction of a pixel. For this reason, we add one pixel to the total rect size.
                    int count = (int)((totalRect.size[axis] + 1 / scaledPixelsPerPoint) / targetRect.size[axis]);

                    if (backgroundPosition.keyword == BackgroundPositionKeyword.Center)
                    {
                        // For center, we must keep an odd number of repetitions
                        if ((count & 1) == 1)
                            count += 2;
                        else
                            count++;
                    }
                    else
                    {
                        // For other alignment, always add 2, to avoid changing what's in the center.
                        count += 2;
                    }

                    for (int i = 0; i < count; ++i)
                    {
                        Vector2 r = rect.position;
                        r[axis] = (i * targetRect.size[axis]);
                        rect.position = r;

                        RepeatRectUV s;
                        s.rect = rect;
                        s.uv = uv;

                        linear_size += s.rect.size[axis];

                        m_RepeatRectUVList[axis].Add(s);
                    }
                }
                else if (repeat == Repeat.Space)
                {
                    Rect rect = targetRect;

                    int count = (int)(totalRect.size[axis] / targetRect.size[axis]);

                    if (count >= 0)
                    {
                        RepeatRectUV s;
                        s.rect = rect;
                        s.uv = uv;
                        m_RepeatRectUVList[axis].Add(s);
                        linear_size = targetRect.size[axis];
                    }

                    if (count >= 2)
                    {
                        RepeatRectUV s;

                        Vector2 r = rect.position;
                        r[axis] = totalRect.size[axis] - targetRect.size[axis];
                        rect.position = r;

                        s.rect = rect;
                        s.uv = uv;

                        m_RepeatRectUVList[axis].Add(s);
                        linear_size = totalRect.size[axis];
                    }

                    if (count > 2)
                    {
                        float spaceOffset = (totalRect.size[axis] - targetRect.size[axis] * count) / (count - 1);

                        for (int i = 0; i < (count - 2); ++i)
                        {
                            RepeatRectUV s;
                            Vector2 r = rect.position;
                            r[axis] = (targetRect.size[axis] + spaceOffset) * (1 + i);
                            rect.position = r;

                            s.rect = rect;
                            s.uv = uv;

                            m_RepeatRectUVList[axis].Add(s);
                        }
                    }
                }
                else if (repeat == Repeat.Round)
                {
                    int count = (int)((totalRect.size[axis] + targetRect.size[axis] * 0.5f) / targetRect.size[axis]);
                    count = Math.Max(count, 1);

                    float new_size = (totalRect.size[axis] / count);

                    if (backgroundPosition.keyword == BackgroundPositionKeyword.Center)
                    {
                        if ((count & 1) == 1)
                        {
                            count += 2;
                        }
                        else
                        {
                            count++;
                        }
                    }
                    else
                    {
                        count++;
                    }

                    Rect rect = targetRect;
                    Vector2 d = rect.size;

                    d[axis] = new_size;
                    rect.size = d;

                    for (int i = 0; i < count; ++i)
                    {
                        RepeatRectUV s;
                        Vector2 r = rect.position;
                        r[axis] = new_size * i;
                        rect.position = r;
                        s.rect = rect;
                        s.uv = uv;
                        m_RepeatRectUVList[axis].Add(s);

                        linear_size += s.rect.size[axis];
                    }
                }

                // Adjust for position
                float offset = 0;
                bool alignToGrid = false;

                if (backgroundPosition.keyword == BackgroundPositionKeyword.Center)
                {
                    offset = (totalRect.size[axis] - linear_size) * 0.5f;
                    alignToGrid = true;
                }
                else if (repeat != Repeat.Space)
                {
                    if (backgroundPosition.offset.unit == LengthUnit.Percent)
                    {
                        offset = (totalRect.size[axis] - targetRect.size[axis]) * backgroundPosition.offset.value / 100.0f;
                        alignToGrid = true;
                    }
                    else if (backgroundPosition.offset.unit == LengthUnit.Pixel)
                    {
                        offset = backgroundPosition.offset.value;
                    }

                    if ((backgroundPosition.keyword == BackgroundPositionKeyword.Right) || (backgroundPosition.keyword == BackgroundPositionKeyword.Bottom))
                    {

                        offset = (totalRect.size[axis] - linear_size) - offset;
                    }
                }

                // UUM-36753: we need to round the offset to the nearest pixel. Otherwise, when the texture
                // junctions are not pixel aligned, we get a seam because of the blending with the background.
                // If the mesh is transformed (e.g. dynamic transform or nudging), the issues is likely to happen
                // again though. The ultimate fix will be to modify the generation algorithm to avoid blending
                // with arc-aa in the seams.
                //
                // Note that this doesn't work for non-rectangular sprites and vector images because their size
                // is based on their mesh, which can be of non-integer size. As a result, the provided rect is
                // fractional and we can't round it in a way that makes sense.
                if (alignToGrid && rectParams.sprite == null && rectParams.vectorImage == null)
                {
                    // If the height of the rect is not an integer, it's not worth aligning at all because the end
                    // is doomed to not be aligned on the pixel grid.
                    float sizeInPixels = targetRect.size[axis] * scaledPixelsPerPoint;
                    if (Mathf.Abs(Mathf.Round(sizeInPixels) - sizeInPixels) < 0.001f)
                    {
                        offset = AlignmentUtils.CeilToPixelGrid(offset, scaledPixelsPerPoint);
                    }
                }

                // adjust offset position for repeat and round
                if (repeat == Repeat.Repeat || repeat == Repeat.Round)
                {
                    float size = targetRect.size[axis];
                    if (size > UIRUtility.k_Epsilon)
                    {
                        if (offset < -size)
                        {
                            int mod = (int)(-offset / size);
                            offset += mod * size;
                        }

                        if (offset > 0.0f)
                        {
                            int mod = (int)(offset / size);
                            offset -= (1 + mod) * size;
                        }
                    }
                }

                for (int i = 0; i < m_RepeatRectUVList[axis].Count; ++i)
                {
                    RepeatRectUV item = m_RepeatRectUVList[axis][i];
                    Vector2 pos = item.rect.position;

                    pos[axis] += offset;
                    item.rect.position = pos;
                    m_RepeatRectUVList[axis][i] = item;
                }
            }

            Rect originalUV = new Rect(uv);

            foreach (var y in m_RepeatRectUVList[1])
            {
                targetRect.y = y.rect.y;
                targetRect.height = y.rect.height;
                uv.y = y.uv.y;
                uv.height = y.uv.height;

                if (targetRect.y < totalRect.y)
                {
                    float left = totalRect.y - targetRect.y;
                    float right = targetRect.height - left;

                    float total = left + right;
                    float new_height = originalUV.height * right / total;
                    float new_y = originalUV.height * left / total;

                    uv.y = new_y + originalUV.y;
                    uv.height = new_height;

                    targetRect.y = totalRect.y;
                    targetRect.height = right;
                }

                if (targetRect.yMax > totalRect.yMax)
                {
                    float right = targetRect.yMax - totalRect.yMax;
                    float left = targetRect.height - right;
                    float total = left + right;

                    float new_height = uv.height * left / total;
                    uv.height = new_height;
                    uv.y = uv.yMax - new_height;
                    targetRect.height = left;
                }

                if (rectParams.vectorImage == null)
                {
                    // offset y
                    float before = uv.y - originalUV.y;
                    float after = originalUV.yMax - uv.yMax;
                    uv.y += (after - before);
                }

                foreach (var x in m_RepeatRectUVList[0])
                {
                    targetRect.x = x.rect.x;
                    targetRect.width = x.rect.width;
                    uv.x = x.uv.x;
                    uv.width = x.uv.width;

                    if (targetRect.x < totalRect.x)
                    {
                        float left = totalRect.x - targetRect.x;
                        float right = targetRect.width - left;

                        float total = left + right;
                        float new_width = uv.width * right / total;
                        float new_x = originalUV.x + originalUV.width * left / total;

                        uv.x = new_x;
                        uv.width = new_width;

                        targetRect.x = totalRect.x;
                        targetRect.width = right;
                    }

                    if (targetRect.xMax > totalRect.xMax)
                    {
                        float right = targetRect.xMax - totalRect.xMax;
                        float left = targetRect.width - right;
                        float total = left + right;

                        float new_width = uv.width * left / total;
                        uv.width = new_width;
                        targetRect.width = left;
                    }

                    StampRectangleWithSubRect(rectParams, targetRect, totalRect, uv);
                }
            }
        }

        void StampRectangleWithSubRect(RectangleParams rectParams, Rect targetRect, Rect totalRect, Rect targetUV)
        {
            if (targetRect.width < UIRUtility.k_Epsilon || targetRect.height < UIRUtility.k_Epsilon)
                return;

            // Remap the subRect inside the targetRect
            var fullRect = targetRect;
            fullRect.size /= targetUV.size;
            fullRect.position -= new Vector2(targetUV.position.x, 1.0f - targetUV.position.y - targetUV.size.y) * fullRect.size;

            var subRect = rectParams.subRect;
            subRect.position *= fullRect.size;
            subRect.position += fullRect.position;
            subRect.size *= fullRect.size;

            if (rectParams.HasSlices(UIRUtility.k_Epsilon))
            {
                // Use the full target rect when working with slices. The content will stretch to the full target.
                rectParams.backgroundRepeatRect = Rect.zero;
                rectParams.rect = targetRect;
            }
            else
            {
                // Find where the subRect intersects with the targetRect.
                var rect = RectangleParams.RectIntersection(subRect, targetRect);
                if (rect.size.x < UIRUtility.k_Epsilon || rect.size.y < UIRUtility.k_Epsilon)
                    return;

                if (rect.size != subRect.size)
                {
                    // There was an intersection, we need to adjust the UVs
                    var sizeRatio = rect.size / subRect.size;
                    var newUVSize = rectParams.uv.size * sizeRatio;
                    var uvDiff = rectParams.uv.size - newUVSize;
                    if (rect.x > subRect.x)
                    {
                        float overflow = ((subRect.xMax - rect.xMax) / subRect.width) * rectParams.uv.size.x;
                        rectParams.uv.x += uvDiff.x - overflow;
                    }
                    if (rect.yMax < subRect.yMax)
                    {
                        float overflow = ((rect.y - subRect.y) / subRect.height) * rectParams.uv.size.y;
                        rectParams.uv.y += uvDiff.y - overflow;
                    }

                    rectParams.uv.size = newUVSize;
                }

                if (rectParams.vectorImage != null)
                {
                    rectParams.backgroundRepeatRect = Rect.zero;
                    rectParams.rect = rect;
                }
                else
                {
                    if (totalRect == rect)
                    {
                        rectParams.backgroundRepeatRect = Rect.zero;
                    }
                    else
                    {
                        rectParams.backgroundRepeatRect = rect;
                    }

                    rectParams.rect = totalRect;
                }
            }

            DrawRectangle(rectParams);
        }

        static void AdjustSpriteWinding(Vector2[] vertices, UInt16[] indices, NativeSlice<UInt16> newIndices)
        {
            for (int i = 0; i < indices.Length; i += 3)
            {
                var v0 = (Vector3)vertices[indices[i]];
                var v1 = (Vector3)vertices[indices[i + 1]];
                var v2 = (Vector3)vertices[indices[i + 2]];

                var v = (v1 - v0).normalized;
                var w = (v2 - v0).normalized;
                var c = Vector3.Cross(v, w);
                if (c.z >= 0.0f)
                {
                    newIndices[i] = indices[i + 1];
                    newIndices[i + 1] = indices[i];
                    newIndices[i + 2] = indices[i + 2];
                }
                else
                {
                    newIndices[i] = indices[i];
                    newIndices[i + 1] = indices[i + 1];
                    newIndices[i + 2] = indices[i + 2];
                }
            }
        }

        public void ScheduleJobs(MeshGenerationContext mgc)
        {
            int parameterCount = m_TesselationJobParameters.Count;
            if (parameterCount == 0)
                return;

            if (m_JobParameters.Length < parameterCount)
            {
                m_JobParameters.Dispose();
                m_JobParameters = new NativeArray<TessellationJobParameters>(parameterCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }

            for (int i = 0; i < parameterCount; ++i)
                m_JobParameters[i] = m_TesselationJobParameters[i];
            m_TesselationJobParameters.Clear();

            var job = new TessellationJob() { jobParameters = m_JobParameters.Slice(0, parameterCount) };
            mgc.GetTempMeshAllocator(out job.allocator);

            var jobHandle = job.Schedule(parameterCount, 1);

            mgc.AddMeshGenerationJob(jobHandle);
            mgc.AddMeshGenerationCallback(m_OnMeshGenerationDelegate, null, MeshGenerationCallbackType.Work, true);
        }

        UIR.MeshGenerationCallback m_OnMeshGenerationDelegate;
        void OnMeshGeneration(MeshGenerationContext ctx, object data)
        {
            m_GCHandlePool.ReturnAll();
        }

        struct TessellationJobParameters
        {
            public bool isBorderJob;
            public MeshBuilderNative.NativeRectParams rectParams;
            public MeshGenerator.BorderParams borderParams;
            public UnsafeMeshGenerationNode node;
        }
        List<TessellationJobParameters> m_TesselationJobParameters = new(256);

        struct TessellationJob : IJobParallelFor
        {
            [ReadOnly] public TempMeshAllocator allocator;
            [ReadOnly] public NativeSlice<TessellationJobParameters> jobParameters;

            public unsafe void Execute(int i)
            {
                var jobParams = jobParameters[i];

                if (jobParams.isBorderJob)
                {
                    DrawBorder(jobParams.node, ref jobParams.borderParams);
                }
                else
                {
                    ref var rectParams = ref jobParams.rectParams;
                    if (rectParams.vectorImage != IntPtr.Zero)
                        DrawVectorImage(jobParams.node, ref rectParams, ExtractHandle<VectorImage>(rectParams.vectorImage));
                    else if (rectParams.sprite != IntPtr.Zero)
                        DrawSprite(jobParams.node, ref rectParams, ExtractHandle<Sprite>(rectParams.sprite));
                    else
                        DrawRectangle(jobParams.node, ref rectParams, ExtractHandle<Texture>(rectParams.texture));
                }
            }

            T ExtractHandle<T>(IntPtr handlePtr) where T : class
            {
                var handle = handlePtr != IntPtr.Zero ? GCHandle.FromIntPtr(handlePtr) : new GCHandle();
                return handle.IsAllocated ? handle.Target as T : null;
            }

            void DrawBorder(UnsafeMeshGenerationNode node, ref BorderParams borderParams)
            {
                var meshData = MeshBuilderNative.MakeBorder(borderParams.ToNativeParams(), UIRUtility.k_MeshPosZ);

                if (meshData.vertexCount == 0 || meshData.indexCount == 0)
                    return;

                NativeSlice<Vertex> nativeVertices;
                NativeSlice<UInt16> nativeIndices;
                unsafe
                {
                    nativeVertices = UIRenderDevice.PtrToSlice<Vertex>((void*)meshData.vertices, meshData.vertexCount);
                    nativeIndices = UIRenderDevice.PtrToSlice<UInt16>((void*)meshData.indices, meshData.indexCount);
                }
                if (nativeVertices.Length == 0 || nativeIndices.Length == 0)
                    return;

                allocator.AllocateTempMesh(nativeVertices.Length, nativeIndices.Length, out var vertices, out var indices);

                Debug.Assert(vertices.Length == nativeVertices.Length);
                Debug.Assert(indices.Length == nativeIndices.Length);
                vertices.CopyFrom(nativeVertices);
                indices.CopyFrom(nativeIndices);

                node.DrawMesh(vertices, indices);
            }

            void ApplyInset(ref MeshBuilderNative.NativeRectParams rectParams, Texture tex)
            {
                var rect = rectParams.rect;
                var inset = rectParams.rectInset;
                if (Mathf.Approximately(rect.size.x, 0.0f) || Mathf.Approximately(rect.size.y, 0.0f) || inset == Vector4.zero)
                    return;

                var prevRect = rect;
                rect.x += inset.x;
                rect.y += inset.y;
                rect.width -= (inset.x + inset.z);
                rect.height -= (inset.y + inset.w);
                rectParams.rect = rect;

                var uv = rectParams.uv;
                if (tex != null && uv.width > UIRUtility.k_Epsilon && uv.height > UIRUtility.k_Epsilon)
                {
                    var uvScale = new Vector2(1.0f / prevRect.width, 1.0f / prevRect.height);
                    uv.x += (inset.x * uvScale.x);
                    uv.y += (inset.w * uvScale.y);
                    uv.width -= ((inset.x + inset.z) * uvScale.x);
                    uv.height -= ((inset.y + inset.w) * uvScale.y);
                    rectParams.uv = uv;
                }
            }

            void DrawRectangle(UnsafeMeshGenerationNode node, ref MeshBuilderNative.NativeRectParams rectParams, Texture tex)
            {
                ApplyInset(ref rectParams, tex);

                MeshWriteDataInterface meshData;
                if (rectParams.texture != IntPtr.Zero)
                    meshData = MeshBuilderNative.MakeTexturedRect(rectParams, UIRUtility.k_MeshPosZ);
                else
                    meshData = MeshBuilderNative.MakeSolidRect(rectParams, UIRUtility.k_MeshPosZ);

                if (meshData.vertexCount == 0 || meshData.indexCount == 0)
                    return;

                var meshFlags = (MeshGenerationContext.MeshFlags)rectParams.meshFlags;

                NativeSlice<Vertex> nativeVertices;
                NativeSlice<UInt16> nativeIndices;
                unsafe
                {
                    nativeVertices = UIRenderDevice.PtrToSlice<Vertex>((void*)meshData.vertices, meshData.vertexCount);
                    nativeIndices = UIRenderDevice.PtrToSlice<UInt16>((void*)meshData.indices, meshData.indexCount);
                }
                if (nativeVertices.Length == 0 || nativeIndices.Length == 0)
                    return;

                allocator.AllocateTempMesh(nativeVertices.Length, nativeIndices.Length, out var vertices, out var indices);

                Debug.Assert(vertices.Length == nativeVertices.Length);
                Debug.Assert(indices.Length == nativeIndices.Length);
                vertices.CopyFrom(nativeVertices);
                indices.CopyFrom(nativeIndices);

                node.DrawMesh(vertices, indices, tex);
            }

            void DrawSprite(UnsafeMeshGenerationNode node, ref MeshBuilderNative.NativeRectParams rectParams, Sprite sprite)
            {
                if (rectParams.spriteTexture == IntPtr.Zero)
                    return; // Textureless sprites not supported, should use VectorImage instead

                var spriteTexture = ExtractHandle<Texture2D>(rectParams.spriteTexture);
                var spriteVertices = ExtractHandle<Vector2[]>(rectParams.spriteVertices);
                var spriteUV = ExtractHandle<Vector2[]>(rectParams.spriteUVs);
                var spriteIndices = ExtractHandle<UInt16[]>(rectParams.spriteTriangles);

                if (spriteIndices?.Length == 0)
                    return;

                var vertexCount = spriteVertices.Length;
                allocator.AllocateTempMesh(vertexCount, spriteIndices.Length, out NativeSlice<Vertex> vertices, out NativeSlice<UInt16> indices);

                AdjustSpriteWinding(spriteVertices, spriteIndices, indices);

                var colorPage = rectParams.colorPage;
                var pageAndID = colorPage.pageAndID;

                var flags = new Color32(0, 0, 0, (colorPage.isValid != 0) ? (byte)1 : (byte)0);
                var page = new Color32(0, 0, colorPage.pageAndID.r, colorPage.pageAndID.g);
                var ids = new Color32(0, 0, 0, colorPage.pageAndID.b);

                for (int i = 0; i < vertexCount; ++i)
                {
                    var v = spriteVertices[i];
                    v -= rectParams.spriteGeomRect.position;
                    v /= rectParams.spriteGeomRect.size;
                    v.y = 1.0f - v.y;
                    v *= rectParams.rect.size;
                    v += rectParams.rect.position;

                    vertices[i] = new Vertex
                    {
                        position = new Vector3(v.x, v.y, Vertex.nearZ),
                        tint = rectParams.color,
                        uv = spriteUV[i],
                        flags = flags,
                        opacityColorPages = page,
                        ids = ids
                    };
                }

                var meshFlags = (MeshGenerationContext.MeshFlags)rectParams.meshFlags;
                bool skipAtlas = (meshFlags == MeshGenerationContext.MeshFlags.SkipDynamicAtlas);

                node.DrawMeshInternal(vertices, indices, spriteTexture, skipAtlas);
            }

            void DrawVectorImage(UnsafeMeshGenerationNode node, ref MeshBuilderNative.NativeRectParams rectParams, VectorImage vi)
            {
                bool isUsingGradients = (rectParams.meshFlags & (int)MeshGenerationContext.MeshFlags.IsUsingVectorImageGradients) != 0;

                // Convert the VectorImage's serializable vertices to Vertex instances
                int vertexCount = vi.vertices.Length;
                var svgVertices = new Vertex[vertexCount];
                for (int i = 0; i < vertexCount; ++i)
                {
                    var v = vi.vertices[i];
                    svgVertices[i] = new Vertex() {
                        position = v.position,
                        tint = v.tint,
                        uv = v.uv,
                        settingIndex = new Color32((byte)(v.settingIndex >> 8), (byte)v.settingIndex, 0, 0),
                        flags = v.flags,
                        circle = v.circle
                    };
                }
                MeshWriteDataInterface meshData;
                if (rectParams.leftSlice <= UIRUtility.k_Epsilon &&
                    rectParams.topSlice <= UIRUtility.k_Epsilon &&
                    rectParams.rightSlice <= UIRUtility.k_Epsilon &&
                    rectParams.bottomSlice <= UIRUtility.k_Epsilon)
                {
                    meshData = MeshBuilderNative.MakeVectorGraphicsStretchBackground(svgVertices, vi.indices, vi.size.x, vi.size.y, rectParams.rect, rectParams.uv, rectParams.scaleMode, rectParams.color, rectParams.colorPage);
                }
                else
                {
                    var sliceLTRB = new Vector4(rectParams.leftSlice, rectParams.topSlice, rectParams.rightSlice, rectParams.bottomSlice);
                    meshData = MeshBuilderNative.MakeVectorGraphics9SliceBackground(svgVertices, vi.indices, vi.size.x, vi.size.y, rectParams.rect, sliceLTRB, rectParams.color, rectParams.colorPage);
                }

                NativeSlice<Vertex> nativeVertices;
                NativeSlice<UInt16> nativeIndices;
                unsafe
                {
                    nativeVertices = UIRenderDevice.PtrToSlice<Vertex>((void*)meshData.vertices, meshData.vertexCount);
                    nativeIndices = UIRenderDevice.PtrToSlice<UInt16>((void*)meshData.indices, meshData.indexCount);
                }
                if (nativeVertices.Length == 0 || nativeIndices.Length == 0)
                    return;

                allocator.AllocateTempMesh(nativeVertices.Length, nativeIndices.Length, out var vertices, out var indices);

                Debug.Assert(vertices.Length == nativeVertices.Length);
                Debug.Assert(indices.Length == nativeIndices.Length);
                vertices.CopyFrom(nativeVertices);
                indices.CopyFrom(nativeIndices);

                if (isUsingGradients)
                    node.DrawGradientsInternal(vertices, indices, vi);
                else
                    node.DrawMesh(vertices, indices);
            }
        }

        #region Dispose Pattern

        internal bool disposed { get; private set; }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                m_GCHandlePool.Dispose();
                m_JobParameters.Dispose();
            }

            disposed = true;
        }

        #endregion // Dispose Pattern
    }
}
