// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    // Empty interface use internally to call the protected DoRepaint with a StylePainter
    // that is casted to an IStylePainterInternal when required.
    // New public functions will be added to the public interface once the API as been determined.
    internal interface IStylePainter
    {
    }

    internal interface IStylePainterInternal : IStylePainter
    {
        void DrawRect(RectStylePainterParameters painterParams);
        void DrawTexture(TextureStylePainterParameters painterParams);
        void DrawText(TextStylePainterParameters painterParams);
        void DrawMesh(MeshStylePainterParameters painterParameters);

        // The DrawImmediate method allow for inserting direct-GL calls at the right spot in the draw chain.
        void DrawImmediate(System.Action callback);

        void DrawBackground();
        void DrawBorder();
        void DrawText(string text);

        float opacity { get; set; }
    }

    internal struct BorderParameters
    {
        public float leftWidth;
        public float topWidth;
        public float rightWidth;
        public float bottomWidth;

        public float topLeftRadius;
        public float topRightRadius;
        public float bottomRightRadius;
        public float bottomLeftRadius;

        public void SetWidth(float top, float right, float bottom, float left)
        {
            topWidth = top;
            rightWidth = right;
            bottomWidth = bottom;
            leftWidth = left;
        }

        public void SetWidth(float allBorders)
        {
            SetWidth(allBorders, allBorders, allBorders, allBorders);
        }

        public void SetRadius(float topLeft, float topRight, float bottomRight, float bottomLeft)
        {
            topLeftRadius = topLeft;
            topRightRadius = topRight;
            bottomRightRadius = bottomRight;
            bottomLeftRadius = bottomLeft;
        }

        public void SetRadius(float radius)
        {
            SetRadius(radius, radius, radius, radius);
        }

        public Vector4 GetWidths()
        {
            return new Vector4(leftWidth,
                topWidth,
                rightWidth,
                bottomWidth);
        }

        public Vector4 GetRadiuses()
        {
            return new Vector4(topLeftRadius,
                topRightRadius,
                bottomRightRadius,
                bottomLeftRadius);
        }

        public static void SetFromStyle(ref BorderParameters border, IComputedStyle style)
        {
            border.SetWidth(style.borderTopWidth.value, style.borderRightWidth.value, style.borderBottomWidth.value, style.borderLeftWidth.value);
            border.SetRadius(style.borderTopLeftRadius.value.value, style.borderTopRightRadius.value.value, style.borderBottomRightRadius.value.value, style.borderBottomLeftRadius.value.value);
        }
    }

    internal struct TextureStylePainterParameters
    {
        public Rect rect;
        public Rect uv;
        public Color color;
        public Texture texture;
        public ScaleMode scaleMode;
        public BorderParameters border;
        public int sliceLeft;
        public int sliceTop;
        public int sliceRight;
        public int sliceBottom;
        public bool usePremultiplyAlpha;

        public static TextureStylePainterParameters GetDefault(VisualElement ve)
        {
            IComputedStyle style = ve.computedStyle;
            var painterParams = new TextureStylePainterParameters
            {
                rect = GUIUtility.AlignRectToDevice(ve.rect),
                uv = new Rect(0, 0, 1, 1),
                color = (Color)Color.white,
                texture = style.backgroundImage.value.texture,
                scaleMode = style.unityBackgroundScaleMode.value,
                sliceLeft = style.unitySliceLeft.value,
                sliceTop = style.unitySliceTop.value,
                sliceRight = style.unitySliceRight.value,
                sliceBottom = style.unitySliceBottom.value
            };

            BorderParameters.SetFromStyle(ref painterParams.border, style);
            return painterParams;
        }
    }

    internal struct RectStylePainterParameters
    {
        public Rect rect;
        public Color color;
        public BorderParameters border;

        public static RectStylePainterParameters GetDefault(VisualElement ve)
        {
            IComputedStyle style = ve.computedStyle;
            var painterParams = new RectStylePainterParameters
            {
                rect = GUIUtility.AlignRectToDevice(ve.rect),
                color = style.backgroundColor.value,
            };
            BorderParameters.SetFromStyle(ref painterParams.border, style);
            return painterParams;
        }
    }

    internal struct TextStylePainterParameters
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
        public TextClipping clipping;

        public static TextStylePainterParameters GetDefault(VisualElement ve, string text)
        {
            IComputedStyle style = ve.computedStyle;
            var painterParams = new TextStylePainterParameters
            {
                rect = ve.contentRect,
                text = text,
                font = style.unityFont.value,
                fontSize = (int)style.fontSize.value.value,
                fontStyle = style.unityFontStyleAndWeight.value,
                fontColor = style.color.GetSpecifiedValueOrDefault(Color.black),
                anchor = style.unityTextAlign.value,
                wordWrap = style.whiteSpace.value == WhiteSpace.Normal,
                wordWrapWidth = style.whiteSpace.value == WhiteSpace.Normal ? ve.contentRect.width : 0.0f,
                richText = false,
                clipping = (TextClipping)style.overflow.value
            };
            return painterParams;
        }

        public static TextStylePainterParameters GetDefault(TextElement te)
        {
            return GetDefault(te, te.text);
        }

        public TextNativeSettings GetTextNativeSettings(float scaling)
        {
            return new TextNativeSettings
            {
                text = text,
                font = font,
                size = fontSize,
                scaling = scaling,
                style = fontStyle,
                color = fontColor,
                anchor = anchor,
                wordWrap = wordWrap,
                wordWrapWidth = wordWrapWidth,
                richText = richText
            };
        }
    }

    internal struct MeshStylePainterParameters
    {
        public Mesh mesh;
        public Material material;
        public int pass;

        public static MeshStylePainterParameters GetDefault(Mesh mesh, Material mat)
        {
            var painterParams = new MeshStylePainterParameters()
            {
                mesh = mesh,
                material = mat
            };
            return painterParams;
        }
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
            IComputedStyle style = ve.computedStyle;
            var painterParams = new CursorPositionStylePainterParameters() {
                rect = ve.contentRect,
                text = text,
                font = style.unityFont.value,
                fontSize = (int)style.fontSize.value.value,
                fontStyle = style.unityFontStyleAndWeight.value,
                anchor = style.unityTextAlign.value,
                wordWrapWidth = style.whiteSpace.value == WhiteSpace.Normal ? ve.contentRect.width : 0.0f,
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
