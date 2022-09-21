// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.TextCore.Text;

namespace UnityEngine.UIElements
{
    internal interface IStylePainter
    {
        MeshWriteData DrawMesh(int vertexCount, int indexCount, Texture texture, Material material, MeshGenerationContext.MeshFlags flags);
        void DrawText(TextInfo textInfo, Vector2 offset);
        void DrawText(string text, Vector2 pos, float fontSize, Color color, FontAsset font);
        void DrawRectangle(MeshGenerationContextUtils.RectangleParams rectParams);
        void DrawBorder(MeshGenerationContextUtils.BorderParams borderParams);
        void DrawImmediate(Action callback, bool cullingEnabled);
        void DrawVectorImage(VectorImage vectorImage, Vector2 pos, Angle rotationAngle, Vector2 scale);
        VisualElement visualElement { get; }
    }

    internal struct CursorPositionStylePainterParameters
    {
        public Rect rect;
        public string text;
        public Font font;
        public int fontSize;
        public FontStyle fontStyle;
        public TextAnchor anchor;
        public float wordWrapWidth;
        public bool richText;
        public int cursorIndex;

        public static CursorPositionStylePainterParameters GetDefault(VisualElement ve, string text)
        {
            var style = ve.computedStyle;
            var painterParams = new CursorPositionStylePainterParameters() {
                rect = ve.contentRect,
                text = text,
                font = TextUtilities.GetFont(ve),
                fontSize = (int)style.fontSize.value,
                fontStyle = style.unityFontStyleAndWeight,
                anchor = style.unityTextAlign,
                wordWrapWidth = style.whiteSpace == WhiteSpace.Normal ? ve.contentRect.width : 0.0f,
                richText = false,
                cursorIndex = 0
            };
            return painterParams;
        }

        internal TextNativeSettings GetTextNativeSettings(float scaling)
        {
            return new TextNativeSettings
            {
                text = text,
                font = font,
                size = fontSize,
                scaling = scaling,
                style = fontStyle,
                color = Color.white, // N/A
                anchor = anchor,
                wordWrap = true,
                wordWrapWidth = wordWrapWidth,
                richText = richText
            };
        }
    }
}
