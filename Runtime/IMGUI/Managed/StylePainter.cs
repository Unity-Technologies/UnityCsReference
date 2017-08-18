// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

namespace UnityEngine
{
    internal struct TextureStylePainterParameters
    {
        public Rect layout;
        public Color color;
        public Texture texture;
        public ScaleMode scaleMode;
        public float borderLeftWidth;
        public float borderTopWidth;
        public float borderRightWidth;
        public float borderBottomWidth;
        public float borderTopLeftRadius;
        public float borderTopRightRadius;
        public float borderBottomRightRadius;
        public float borderBottomLeftRadius;
        public int sliceLeft;
        public int sliceTop;
        public int sliceRight;
        public int sliceBottom;
    }

    internal struct RectStylePainterParameters
    {
        public Rect layout;
        public Color color;
        public float borderLeftWidth;
        public float borderTopWidth;
        public float borderRightWidth;
        public float borderBottomWidth;
        public float borderTopLeftRadius;
        public float borderTopRightRadius;
        public float borderBottomRightRadius;
        public float borderBottomLeftRadius;
    }

    internal struct TextStylePainterParameters
    {
        public Rect layout;
        public string text;
        public Font font;
        public int fontSize;
        public FontStyle fontStyle;
        public Color fontColor;
        public TextAnchor anchor;
        public bool wordWrap;
        public float wordWrapWidth;
        public bool richText;
        public TextClipping clipping;
    }

    internal struct CursorPositionStylePainterParameters
    {
        public Rect layout;
        public string text;
        public Font font;
        public int fontSize;
        public FontStyle fontStyle;
        public TextAnchor anchor;
        public float wordWrapWidth;
        public bool richText;
        public int cursorIndex;
    }

    interface IStylePainter
    {
        void DrawRect(RectStylePainterParameters painterParams);
        void DrawTexture(TextureStylePainterParameters painterParams);
        void DrawText(TextStylePainterParameters painterParams);

        Vector2 GetCursorPosition(CursorPositionStylePainterParameters painterParams);

        Rect currentWorldClip { get; set; }
        Vector2 mousePosition { get; set; }
        Matrix4x4 currentTransform { get; set; }

        Event repaintEvent { get; set; }
        float opacity { get; set; }

        float ComputeTextWidth(TextStylePainterParameters painterParams);
        float ComputeTextHeight(TextStylePainterParameters painterParams);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal partial class StylePainter : IStylePainter
    {
        [NonSerialized]
        internal IntPtr m_Ptr;

        public StylePainter()
        {
            Init();
        }

        public StylePainter(Vector2 pos)
            : this()
        {
            mousePosition = pos;
        }

        public void DrawRect(RectStylePainterParameters painterParams)
        {
            Rect screenRect = painterParams.layout;
            Color color = painterParams.color;

            var borderWidths = new Vector4(
                    painterParams.borderLeftWidth,
                    painterParams.borderTopWidth,
                    painterParams.borderRightWidth,
                    painterParams.borderBottomWidth);
            var borderRadiuses = new Vector4(
                    painterParams.borderTopLeftRadius,
                    painterParams.borderTopRightRadius,
                    painterParams.borderBottomRightRadius,
                    painterParams.borderBottomLeftRadius);

            DrawRect_Internal(screenRect, color * m_OpacityColor, borderWidths, borderRadiuses);
        }

        public void DrawTexture(TextureStylePainterParameters painterParams)
        {
            Rect screenRect = painterParams.layout;
            Texture texture = painterParams.texture;
            Color color = painterParams.color;
            ScaleMode scaleMode = painterParams.scaleMode;
            int sliceLeft = painterParams.sliceLeft;
            int sliceTop = painterParams.sliceTop;
            int sliceRight = painterParams.sliceRight;
            int sliceBottom = painterParams.sliceBottom;

            Rect textureRect = screenRect;
            Rect sourceRect = new Rect(0, 0, 1, 1);
            float textureAspect = (float)texture.width / texture.height;
            float destAspect = screenRect.width / screenRect.height;
            switch (scaleMode)
            {
                case ScaleMode.StretchToFill:
                    break;

                case ScaleMode.ScaleAndCrop:
                    if (destAspect > textureAspect)
                    {
                        float stretch = textureAspect / destAspect;
                        sourceRect = new Rect(0, (1 - stretch) * .5f, 1, stretch);
                    }
                    else
                    {
                        float stretch = destAspect / textureAspect;
                        sourceRect = new Rect(.5f - stretch * .5f, 0, stretch, 1);
                    }
                    break;

                case ScaleMode.ScaleToFit:
                    if (destAspect > textureAspect)
                    {
                        float stretch = textureAspect / destAspect;
                        textureRect = new Rect(screenRect.xMin + screenRect.width * (1.0f - stretch) * .5f, screenRect.yMin, stretch * screenRect.width, screenRect.height);
                    }
                    else
                    {
                        float stretch = destAspect / textureAspect;
                        textureRect = new Rect(screenRect.xMin, screenRect.yMin + screenRect.height * (1.0f - stretch) * .5f, screenRect.width, stretch * screenRect.height);
                    }
                    break;
            }

            var borderWidths = new Vector4(
                    painterParams.borderLeftWidth,
                    painterParams.borderTopWidth,
                    painterParams.borderRightWidth,
                    painterParams.borderBottomWidth);
            var borderRadiuses = new Vector4(
                    painterParams.borderTopLeftRadius,
                    painterParams.borderTopRightRadius,
                    painterParams.borderBottomRightRadius,
                    painterParams.borderBottomLeftRadius);

            DrawTexture_Internal(textureRect, texture, sourceRect, color * m_OpacityColor, borderWidths, borderRadiuses, sliceLeft, sliceTop, sliceRight, sliceBottom);
        }

        public void DrawText(TextStylePainterParameters painterParams)
        {
            Rect screenRect = painterParams.layout;
            string text = painterParams.text;
            Font font = painterParams.font;
            int fontSize = painterParams.fontSize;
            FontStyle fontStyle = painterParams.fontStyle;
            Color fontColor = painterParams.fontColor;
            TextAnchor anchor = painterParams.anchor;
            bool wordWrap = painterParams.wordWrap;
            float wordWrapWidth = painterParams.wordWrapWidth;
            bool richText = painterParams.richText;
            TextClipping clipping = painterParams.clipping;

            DrawText_Internal(screenRect, text, font, fontSize, fontStyle, fontColor * m_OpacityColor, anchor, wordWrap, wordWrapWidth, richText, clipping);
        }

        public Vector2 GetCursorPosition(CursorPositionStylePainterParameters painterParams)
        {
            Font font = painterParams.font;
            if (font == null)
            {
                Debug.LogError("StylePainter: Can't process a null font.");
                return Vector2.zero;
            }

            string text = painterParams.text;
            int fontSize = painterParams.fontSize;
            FontStyle fontStyle = painterParams.fontStyle;
            TextAnchor anchor = painterParams.anchor;
            float wordWrapWidth = painterParams.wordWrapWidth;
            bool richText = painterParams.richText;
            Rect layout = painterParams.layout;
            int cursorIndex = painterParams.cursorIndex;

            return GetCursorPosition_Internal(text, font, fontSize, fontStyle, anchor, wordWrapWidth, richText, layout, cursorIndex);
        }

        public float ComputeTextWidth(TextStylePainterParameters painterParams)
        {
            string text = painterParams.text;
            float wordWrapWidth = painterParams.wordWrapWidth;
            bool wordWrap = painterParams.wordWrap;
            Font font = painterParams.font;
            int fontSize = painterParams.fontSize;
            FontStyle fontStyle = painterParams.fontStyle;
            TextAnchor anchor = painterParams.anchor;
            bool richText = painterParams.richText;

            return ComputeTextWidth_Internal(text, wordWrapWidth, wordWrap, font, fontSize, fontStyle, anchor, richText);
        }

        public float ComputeTextHeight(TextStylePainterParameters painterParams)
        {
            string text = painterParams.text;
            float wordWrapWidth = painterParams.wordWrapWidth;
            bool wordWrap = painterParams.wordWrap;
            Font font = painterParams.font;
            int fontSize = painterParams.fontSize;
            FontStyle fontStyle = painterParams.fontStyle;
            TextAnchor anchor = painterParams.anchor;
            bool richText = painterParams.richText;

            return ComputeTextHeight_Internal(text, wordWrapWidth, wordWrap, font, fontSize, fontStyle, anchor, richText);
        }

        public Matrix4x4 currentTransform { get; set; }
        public Vector2 mousePosition { get; set; }
        public Rect currentWorldClip { get; set; }
        public Event repaintEvent { get; set; }

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
