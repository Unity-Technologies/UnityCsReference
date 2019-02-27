// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine.Bindings;
using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
    [StructLayout(LayoutKind.Sequential)]
    internal class ImmediateStylePainterImpl : ImmediateStylePainter, IStylePainterInternal
    {
        public VisualElement currentElement { get; set; }

        public void DrawRect(RectStylePainterParameters painterParams)
        {
            Rect screenRect = painterParams.rect;
            Color color = painterParams.color * UIElementsUtility.editorPlayModeTintColor;

            var borderWidths = painterParams.border.GetWidths();
            var borderRadiuses = painterParams.border.GetRadiuses();

            DrawRect(screenRect, color * m_OpacityColor, borderWidths, borderRadiuses);
        }

        public void DrawTexture(TextureStylePainterParameters painterParams)
        {
            Rect screenRect = painterParams.rect;
            Rect sourceRect = painterParams.uv != Rect.zero ? painterParams.uv : new Rect(0, 0, 1, 1);
            Texture texture = painterParams.texture;
            Color color = painterParams.color * UIElementsUtility.editorPlayModeTintColor;
            ScaleMode scaleMode = painterParams.scaleMode;
            int sliceLeft = painterParams.sliceLeft;
            int sliceTop = painterParams.sliceTop;
            int sliceRight = painterParams.sliceRight;
            int sliceBottom = painterParams.sliceBottom;
            bool usePremultiplyAlpha = painterParams.usePremultiplyAlpha;

            Rect textureRect = screenRect;

            // Comparing aspects ratio is error-prone because the <c>screenRect</c> may end up being scaled by the
            // transform and the corners will end up being pixel aligned, possibly resulting in blurriness.
            float srcAspect = (texture.width * sourceRect.width) / (texture.height * sourceRect.height);
            float destAspect = screenRect.width / screenRect.height;
            switch (scaleMode)
            {
                case ScaleMode.StretchToFill:
                    break;

                case ScaleMode.ScaleAndCrop:
                    if (destAspect > srcAspect)
                    {
                        float stretch = sourceRect.height * (srcAspect / destAspect);
                        float crop = (sourceRect.height - stretch) * 0.5f;
                        sourceRect = new Rect(sourceRect.x, sourceRect.y + crop, sourceRect.width, stretch);
                    }
                    else
                    {
                        float stretch = sourceRect.width * (destAspect / srcAspect);
                        float crop = (sourceRect.width - stretch) * 0.5f;
                        sourceRect = new Rect(sourceRect.x + crop, sourceRect.y, stretch, sourceRect.height);
                    }
                    break;

                case ScaleMode.ScaleToFit:
                    if (destAspect > srcAspect)
                    {
                        float stretch = srcAspect / destAspect;
                        textureRect = new Rect(screenRect.xMin + screenRect.width * (1.0f - stretch) * .5f, screenRect.yMin, stretch * screenRect.width, screenRect.height);
                    }
                    else
                    {
                        float stretch = destAspect / srcAspect;
                        textureRect = new Rect(screenRect.xMin, screenRect.yMin + screenRect.height * (1.0f - stretch) * .5f, screenRect.width, stretch * screenRect.height);
                    }
                    break;
            }

            var borderWidths = painterParams.border.GetWidths();
            var borderRadiuses = painterParams.border.GetRadiuses();

            DrawTexture(textureRect, texture, sourceRect, color * m_OpacityColor, borderWidths, borderRadiuses, sliceLeft, sliceTop, sliceRight, sliceBottom, usePremultiplyAlpha);
        }

        public void DrawText(TextStylePainterParameters painterParams)
        {
            Rect screenRect = painterParams.rect;
            string text = painterParams.text;
            Font font = painterParams.font;
            int fontSize = painterParams.fontSize;
            FontStyle fontStyle = painterParams.fontStyle;
            Color fontColor = painterParams.fontColor * UIElementsUtility.editorPlayModeTintColor;
            TextAnchor anchor = painterParams.anchor;
            bool wordWrap = painterParams.wordWrap;
            float wordWrapWidth = painterParams.wordWrapWidth;
            bool richText = painterParams.richText;
            TextClipping clipping = painterParams.clipping;

            DrawText(screenRect, text, font, fontSize, fontStyle, fontColor * m_OpacityColor, anchor, wordWrap, wordWrapWidth, richText, clipping);
        }

        public void DrawMesh(MeshStylePainterParameters painterParameters, out NativeSlice<Vertex> vertexData, out NativeSlice<UInt16> indexData)
        {
            vertexData = new NativeSlice<Vertex>();
            indexData = new NativeSlice<UInt16>();
        }

        public void DrawImmediate(System.Action callback)
        {
            callback();
        }

        public void DrawBackground()
        {
            ComputedStyle style = currentElement.computedStyle;

            if (style.backgroundColor != Color.clear)
            {
                var painterParams = RectStylePainterParameters.GetDefault(currentElement);
                painterParams.border.SetWidth(0.0f);
                DrawRect(painterParams);
            }
            if (style.backgroundImage.value.texture != null)
            {
                var painterParams = TextureStylePainterParameters.GetDefault(currentElement);
                if (style.unityBackgroundImageTintColor != Color.clear)
                {
                    painterParams.color = style.unityBackgroundImageTintColor.value;
                }
                painterParams.border.SetWidth(0.0f);
                DrawTexture(painterParams);
            }
        }

        public void DrawBorder()
        {
            ComputedStyle style = currentElement.computedStyle;
            if (style.borderColor != Color.clear && (style.borderLeftWidth.value > 0.0f || style.borderTopWidth.value > 0.0f || style.borderRightWidth.value > 0.0f || style.borderBottomWidth.value > 0.0f))
            {
                var painterParams = RectStylePainterParameters.GetDefault(currentElement);
                painterParams.color = style.borderColor.value;
                DrawRect(painterParams);
            }
        }

        public void DrawText(string text)
        {
            if (!string.IsNullOrEmpty(text) && currentElement.contentRect.width > 0.0f && currentElement.contentRect.height > 0.0f)
            {
                DrawText(TextStylePainterParameters.GetDefault(currentElement, text));
            }
        }

        Color m_OpacityColor = Color.white;
        public float opacity
        {
            get
            {
                return m_OpacityColor.a;
            }
            set
            {
                m_OpacityColor.a = value;
            }
        }
    }
}
