using System;

namespace UnityEngine.UIElements
{
    internal interface IStylePainter
    {
        MeshWriteData DrawMesh(int vertexCount, int indexCount, Texture texture, Material material, MeshGenerationContext.MeshFlags flags);
        void DrawText(MeshGenerationContextUtils.TextParams textParams, ITextHandle handle, float pixelsPerPoint);
        void DrawRectangle(MeshGenerationContextUtils.RectangleParams rectParams);
        void DrawBorder(MeshGenerationContextUtils.BorderParams borderParams);
        void DrawImmediate(Action callback, bool cullingEnabled);
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
                font = style.unityFont,
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
