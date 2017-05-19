// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

namespace UnityEngine
{
    interface IStylePainter
    {
        void DrawRect(Rect screenRect, Color color, float borderWidth = 0.0f, float borderRadius = 0.0f);
        void DrawTexture(Rect screenRect, Texture texture, Color color, ScaleMode scaleMode = ScaleMode.StretchToFill, float borderWidth = 0.0f, float borderRadius = 0.0f, int leftBorder = 0, int rightBorder = 0, int topBorder = 0, int bottomBorder = 0);
        void DrawText(Rect screenRect, string text, Font font, int fontSize, FontStyle fontStyle, Color fontColor, TextAnchor anchor, bool wordWrap, float wordWrapWidth, bool richText, TextClipping clipping);

        Rect currentWorldClip { get; set; }
        Vector2 mousePosition { get; set; }

        Event repaintEvent { get; set; }
        float opacity { get; set; }

        float ComputeTextWidth(string text, Font font, int fontSize, FontStyle fontStyle, TextAnchor anchor, bool richText);
        float ComputeTextHeight(string text, float width, bool wordWrap, Font font, int fontSize, FontStyle fontStyle, TextAnchor anchor, bool richText);
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

        public void DrawTexture(Rect screenRect, Texture texture, Color color, ScaleMode scaleMode = ScaleMode.StretchToFill, float borderWidth = 0.0f, float borderRadius = 0.0f, int leftBorder = 0, int topBorder = 0, int rightBorder = 0, int bottomBorder = 0)
        {
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

            DrawTexture_Internal(textureRect, texture, sourceRect, color * m_OpacityColor, borderWidth, borderRadius, leftBorder, topBorder, rightBorder, bottomBorder);
        }

        public void DrawRect(Rect screenRect, Color color, float borderWidth = 0.0f, float borderRadius = 0.0f)
        {
            DrawRect_Internal(screenRect, color * m_OpacityColor, borderWidth, borderRadius);
        }

        public void DrawText(Rect screenRect, string text, Font font, int fontSize, FontStyle fontStyle, Color fontColor, TextAnchor anchor, bool wordWrap, float wordWrapWidth, bool richText, TextClipping clipping)
        {
            DrawText_Internal(screenRect, text, font, fontSize, fontStyle, fontColor * m_OpacityColor, anchor, wordWrap, wordWrapWidth, richText, clipping);
        }

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
