// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine.TextCore.Text;

namespace UnityEngine.UIElements.UIR
{
    interface IMeshGenerator
    {
        VisualElement currentElement { get; set; }
        public void DrawText(TextInfo textInfo, Vector2 offset);
        public void DrawText(string text, Vector2 pos, float fontSize, Color color, FontAsset font);
        public void DrawRectangle(MeshGenerator.RectangleParams rectParams);
        public void DrawBorder(MeshGenerator.BorderParams borderParams);
        public void DrawVectorImage(VectorImage vectorImage, Vector2 offset, Angle rotationAngle, Vector2 scale);
        public void DrawRectangleRepeat(MeshGenerator.RectangleParams rectParams, Rect totalRect);
    }

    class MeshGenerator : IMeshGenerator
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

        public MeshGenerator(MeshGenerationContext mgc)
        {
            m_MeshGenerationContext = mgc;
        }

        public VisualElement currentElement { get; set; }

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

            public static RectangleParams MakeSprite(Rect containerRect, Rect subRect, Sprite sprite, ScaleMode scaleMode, ContextType panelContext, bool hasRadius, ref Vector4 slices, bool useForRepeat = false)
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

            internal MeshBuilderNative.NativeRectParams ToNativeParams()
            {
                return new MeshBuilderNative.NativeRectParams() {
                    rect = rect,
                    subRect = subRect,
                    uv = uv,
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

        void BuildEntryFromNativeMesh(MeshWriteDataInterface meshData, Texture texture, bool skipAtlas)
        {
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

            m_MeshGenerationContext.AllocateTempMesh(nativeVertices.Length, nativeIndices.Length, out var vertices, out var indices);

            Debug.Assert(vertices.Length == nativeVertices.Length);
            Debug.Assert(indices.Length == nativeIndices.Length);
            vertices.CopyFrom(nativeVertices);
            indices.CopyFrom(nativeIndices);

            m_MeshGenerationContext.entryRecorder.DrawMesh(vertices, indices, texture, skipAtlas);
        }

        void BuildGradientEntryFromNativeMesh(MeshWriteDataInterface meshData, VectorImage gradientsOwner)
        {
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
            m_MeshGenerationContext.AllocateTempMesh(nativeVertices.Length, nativeIndices.Length, out var vertices, out var indices);
            Debug.Assert(vertices.Length == nativeVertices.Length);
            Debug.Assert(indices.Length == nativeIndices.Length);
            vertices.CopyFrom(nativeVertices);
            indices.CopyFrom(nativeIndices);
            m_MeshGenerationContext.entryRecorder.DrawGradients(vertices, indices, gradientsOwner);
        }

        public void DrawText(TextInfo textInfo, Vector2 offset)
        {
            DrawTextInfo(textInfo, offset, true);
        }

        TextInfo m_TextInfo = new TextInfo(VertexDataLayout.VBO);

        public void DrawText(string text, Vector2 pos, float fontSize, Color color, FontAsset font)
        {
            var textSettings = TextUtilities.GetTextSettingsFrom(currentElement);

            m_TextInfo.Clear();
            var textGenerationSettings = new TextCore.Text.TextGenerationSettings() {
                text = text,
                screenRect = Rect.zero,
                fontAsset = font,
                textSettings = textSettings,
                fontSize = fontSize,
                color = color,
                material = font.material,
                inverseYAxis = true
            };
            TextCore.Text.TextGenerator.GenerateText(textGenerationSettings, m_TextInfo);

            DrawTextInfo(m_TextInfo, pos, false);
        }

        void DrawTextInfo(TextInfo textInfo, Vector2 offset, bool useHints)
        {
            for (int i = 0; i < textInfo.materialCount; i++)
            {
                if (textInfo.meshInfo[i].vertexCount == 0)
                    continue;

                // SpriteAssets use an RGBA texture
                if(((Texture2D)textInfo.meshInfo[i].material.mainTexture).format != TextureFormat.Alpha8)
                {
                    // Assume a sprite asset
                    MakeText(
                        textInfo.meshInfo[i].material.mainTexture,
                        textInfo.meshInfo[i],
                        offset,
                        false,
                        false,
                        0,
                        0);
                }
                else
                {
                    // SDF scale is used to differentiate between Bitmap and SDF. The Bitmap Material doesn't have the
                    // GradientScale property which results in sdfScale always being 0.
                    float sdfScale = 0;
                    if (!TextGeneratorUtilities.IsBitmapRendering(textInfo.meshInfo[i].glyphRenderMode))
                        sdfScale = textInfo.meshInfo[i].material.GetFloat(TextShaderUtilities.ID_GradientScale);
                    bool isDynamicColor = useHints && RenderEvents.NeedsColorID(currentElement);
                    var sharpness = textInfo.meshInfo[i].material.GetFloat("_Sharpness");
                    // Set the dynamic-color hint on TextCore fancy-text or the EditorUIE shader applies the
                    // tint over the fragment output, affecting the outline/shadows.
                    if (useHints)
                        isDynamicColor = isDynamicColor || (sdfScale > 0 && RenderEvents.NeedsTextCoreSettings(currentElement));
                    if (sharpness == 0.0f && currentElement.panel.contextType == ContextType.Editor)
                    {
                        var font = TextUtilities.GetFont(currentElement);
                        if (font)
                            sharpness = TextUtilities.getEditorTextSharpness(font.name);
                    }

                    MakeText(
                        textInfo.meshInfo[i].material.mainTexture,
                        textInfo.meshInfo[i],
                        offset,
                        true,
                        isDynamicColor,
                        sdfScale,
                        sharpness);
                }
            }
        }

        static readonly int s_MaxTextMeshVertices = 0xC000; // Max 48k vertices. We leave room for masking, borders, background, etc.

        static Vertex ConvertTextVertexToUIRVertex(MeshInfo info, int index, Vector2 offset, bool isDynamicColor = false)
        {
            float dilate = 0.0f;
            // If Bold, dilate the shape (this value is hardcoded, should be set from the font actual bold weight)
            if (info.vertexData[index].uv2.y < 0.0f) dilate = 1.0f;
            return new Vertex
            {
                position = new Vector3(info.vertexData[index].position.x + offset.x, info.vertexData[index].position.y + offset.y, UIRUtility.k_MeshPosZ),
                uv = new Vector2(info.vertexData[index].uv0.x, info.vertexData[index].uv0.y),
                tint = info.vertexData[index].color,
                // TODO: Don't set the flags here. The mesh conversion should perform these changes
                flags = new Color32(0, (byte)(dilate * 255), 0, isDynamicColor ? (byte)1 : (byte)0)
            };
        }

        static int LimitTextVertices(int vertexCount, bool logTruncation = true)
        {
            if (vertexCount <= s_MaxTextMeshVertices)
                return vertexCount;

            if (logTruncation)
                Debug.LogWarning($"Generated text will be truncated because it exceeds {s_MaxTextMeshVertices} vertices.");

            return s_MaxTextMeshVertices;
        }

        void MakeText(Texture texture, MeshInfo meshInfo, Vector2 offset, bool isSdf, bool isDynamicColor, float sdfScale, float sharpness)
        {
            int vertexCount = LimitTextVertices(meshInfo.vertexCount);
            int quadCount = vertexCount / 4;
            int indexCount = quadCount * 6;

            m_MeshGenerationContext.AllocateTempMesh(quadCount * 4, indexCount, out var vertices, out var indices);

            for (int q = 0, v = 0, i = 0; q < quadCount; ++q, v += 4, i += 6)
            {
                vertices[v + 0] = ConvertTextVertexToUIRVertex(meshInfo, v + 0, offset, isDynamicColor);
                vertices[v + 1] = ConvertTextVertexToUIRVertex(meshInfo, v + 1, offset, isDynamicColor);
                vertices[v + 2] = ConvertTextVertexToUIRVertex(meshInfo, v + 2, offset, isDynamicColor);
                vertices[v + 3] = ConvertTextVertexToUIRVertex(meshInfo, v + 3, offset, isDynamicColor);

                indices[i + 0] = (ushort)(v + 0);
                indices[i + 1] = (ushort)(v + 1);
                indices[i + 2] = (ushort)(v + 2);
                indices[i + 3] = (ushort)(v + 2);
                indices[i + 4] = (ushort)(v + 3);
                indices[i + 5] = (ushort)(v + 0);
            }

            if (isSdf)
                m_MeshGenerationContext.entryRecorder.DrawSdfText(vertices, indices, texture, sdfScale, sharpness);
            else
                m_MeshGenerationContext.entryRecorder.DrawMesh(vertices, indices, texture, true);
        }

        public void DrawRectangle(RectangleParams rectParams)
        {
            if (rectParams.rect.width < UIRUtility.k_Epsilon || rectParams.rect.height < UIRUtility.k_Epsilon)
                return; // Nothing to draw

            k_MarkerDrawRectangle.Begin();
            if (currentElement.panel.contextType == ContextType.Editor)
                rectParams.color *= rectParams.playmodeTintColor;

            if (rectParams.vectorImage != null)
                DrawVectorImage(rectParams);
            else if (rectParams.sprite != null)
                DrawSprite(rectParams);
            else
            {
                MeshWriteDataInterface meshData;
                if (rectParams.texture != null)
                    meshData = MeshBuilderNative.MakeTexturedRect(rectParams.ToNativeParams(), UIRUtility.k_MeshPosZ);
                else
                    meshData = MeshBuilderNative.MakeSolidRect(rectParams.ToNativeParams(), UIRUtility.k_MeshPosZ);

                bool skipAtlas = (rectParams.meshFlags & MeshGenerationContext.MeshFlags.SkipDynamicAtlas) == MeshGenerationContext.MeshFlags.SkipDynamicAtlas;
                BuildEntryFromNativeMesh(meshData, rectParams.texture, skipAtlas);
            }

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

            var meshData = MeshBuilderNative.MakeBorder(borderParams.ToNativeParams(), UIRUtility.k_MeshPosZ);
            BuildEntryFromNativeMesh(meshData, null, true);
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
                m_MeshGenerationContext.entryRecorder.DrawGradients(vertices, indices, vectorImage);
            else
                m_MeshGenerationContext.entryRecorder.DrawMesh(vertices, indices);

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

        public void DrawRectangleRepeat(RectangleParams rectParams, Rect totalRect)
        {
            k_MarkerDrawRectangleRepeat.Begin();
            DoDrawRectangleRepeat(ref rectParams, totalRect);
            k_MarkerDrawRectangleRepeat.End();
        }

        // This method should not be called directly. Use the DrawRectangleRepeat wrapper instead which is properly
        // instrumented with performance counters.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void DoDrawRectangleRepeat(ref RectangleParams rectParams, Rect totalRect)
        {
            var uv = new Rect(0, 0, 1, 1);
            var targetRect = rectParams.rect;

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

            if (rectParams.backgroundSize.sizeType != BackgroundSizeType.Length)
            {
                if (rectParams.backgroundSize.sizeType == BackgroundSizeType.Contain)
                {
                    float ratioX = totalRect.width / targetRect.width;
                    float ratioY = totalRect.height / targetRect.height;

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
                int count = (int)((totalRect.size[1] + targetRect.size[1] * 0.5f) / targetRect.size[1]);
                count = Math.Max(count, 1);

                float new_size = (totalRect.size[1] / count);
                Rect rect = new Rect();
                rect.height = new_size;
                rect.width = rect.height * targetRect.width / targetRect.height;
                targetRect = rect;
            }
            else if ((rectParams.backgroundSize.y.IsAuto()) && (rectParams.backgroundRepeat.x == Repeat.Round))
            {
                int count = (int)((totalRect.size[0] + targetRect.size[0] * 0.5f) / targetRect.size[0]);
                count = Math.Max(count, 1);

                float new_size = (totalRect.size[0] / count);
                Rect rect = new Rect();
                rect.width = new_size;
                rect.height = rect.width * targetRect.height / targetRect.width;
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

                    int count = (int)(totalRect.size[axis] / targetRect.size[axis]);

                    if (backgroundPosition.keyword == BackgroundPositionKeyword.Center)
                    {
                        if ((count % 2) == 1)
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
                        if ((count % 2) == 1)
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

                if (backgroundPosition.keyword == BackgroundPositionKeyword.Center)
                {
                    offset = (totalRect.size[axis] - linear_size) * 0.5f;
                }
                else if (repeat != Repeat.Space)
                {
                    if (backgroundPosition.offset.unit == LengthUnit.Percent)
                    {
                        offset = (totalRect.size[axis] - targetRect.size[axis]) * backgroundPosition.offset.value / 100.0f;
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

                // adjust offset position for repeat and round
                if (repeat == Repeat.Repeat || repeat == Repeat.Round)
                {
                    float size = targetRect.size[axis];
                    if (size > UIRUtility.k_Epsilon)
                    {
                        if (offset < -size)
                        {
                            int mod = (int)(-offset/size);
                            offset += mod * size;
                        }

                        if (offset > 0.0f)
                        {
                            int mod = (int)(offset/size);
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

                    StampRectangleWithSubRect(rectParams, targetRect, uv);
                }
            }
        }

        void StampRectangleWithSubRect(RectangleParams rectParams, Rect targetRect, Rect targetUV)
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

                rectParams.rect = rect;
            }

            DrawRectangle(rectParams);
        }

        static void AdjustSpriteWinding(Vector2[] vertices, ushort[] indices, NativeSlice<ushort> newIndices)
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

        void DrawSprite(RectangleParams rectParams)
        {
            var sprite = rectParams.sprite;
            System.Diagnostics.Debug.Assert(sprite != null);

            if (sprite.texture == null || sprite.triangles.Length == 0)
                return; // Textureless sprites not supported, should use VectorImage instead

            System.Diagnostics.Debug.Assert(sprite.border == Vector4.zero, "Sliced sprites should be rendered as regular textured rectangles");

            // Remap vertices inside rect
            var spriteVertices = sprite.vertices;
            var spriteIndices = sprite.triangles;
            var spriteUV = sprite.uv;

            var vertexCount = sprite.vertices.Length;

            m_MeshGenerationContext.AllocateTempMesh(vertexCount, spriteIndices.Length, out NativeSlice<Vertex> vertices, out NativeSlice<ushort> indices);

            AdjustSpriteWinding(spriteVertices, spriteIndices, indices);

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
                    uv = spriteUV[i]
                };
            }

            bool skipAtlas = rectParams.meshFlags == MeshGenerationContext.MeshFlags.SkipDynamicAtlas;
            m_MeshGenerationContext.entryRecorder.DrawMesh(vertices, indices, sprite.texture, skipAtlas);
        }

        void DrawVectorImage(RectangleParams rectParams)
        {
            var vi = rectParams.vectorImage;
            bool isUsingGradients = vi.atlas != null;
            Debug.Assert(vi != null);
            // Convert the VectorImage's serializable vertices to Vertex instances
            int vertexCount = vi.vertices.Length;
            var vertices = new Vertex[vertexCount];
            for (int i = 0; i < vertexCount; ++i)
            {
                var v = vi.vertices[i];
                vertices[i] = new Vertex() {
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
                meshData = MeshBuilderNative.MakeVectorGraphicsStretchBackground(vertices, vi.indices, vi.size.x, vi.size.y, rectParams.rect, rectParams.uv, rectParams.scaleMode, rectParams.color, rectParams.colorPage.ToNativeColorPage());
            }
            else
            {
                var sliceLTRB = new Vector4(rectParams.leftSlice, rectParams.topSlice, rectParams.rightSlice, rectParams.bottomSlice);
                meshData = MeshBuilderNative.MakeVectorGraphics9SliceBackground(vertices, vi.indices, vi.size.x, vi.size.y, rectParams.rect, sliceLTRB, rectParams.color, rectParams.colorPage.ToNativeColorPage());
            }

            if (isUsingGradients)
                BuildGradientEntryFromNativeMesh(meshData, vi);
            else
                BuildEntryFromNativeMesh(meshData, null, true);
        }
    }
}
