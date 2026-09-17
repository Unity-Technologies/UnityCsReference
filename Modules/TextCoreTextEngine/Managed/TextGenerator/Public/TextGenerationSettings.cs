// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.TextCore.Text;

namespace UnityEngine.TextCore
{
    ///<summary>The base direction text is laid out in.</summary>
    public enum TextDirection
    {
        ///<summary>Left-to-right language direction.</summary>
        LTR,
        ///<summary>Right-to-left language direction.</summary>
        RTL
    }

    ///<summary>Whether lines wrap at the horizontal extent.</summary>
    public enum TextWrapMode
    {
        ///<summary>Lines wrap at the horizontal extent.</summary>
        Wrap,
        ///<summary>Lines do not wrap.</summary>
        NoWrap
    }

    ///<summary>How sequences of white space are handled.</summary>
    public enum WhitespaceCollapse
    {
        ///<summary>Sequences of white space collapse into a single space.</summary>
        Collapse,
        ///<summary>White space is preserved.</summary>
        Preserve
    }

    ///<summary>Automatic font sizing settings for the Advanced Text Generator.</summary>
    public struct FontAutoSize
    {
        ///<summary>Shrink or grow the font size between <see cref="minSize"/> and <see cref="maxSize"/> so the text fits the extents.</summary>
        public bool enabled;

        ///<summary>Minimum font size, in pixels.</summary>
        public float minSize;

        ///<summary>Maximum font size, in pixels.</summary>
        public float maxSize;

        ///<summary>Creates automatic sizing settings.</summary>
        ///<param name="enabled">Whether automatic sizing is applied.</param>
        ///<param name="minSize">Minimum font size, in pixels.</param>
        ///<param name="maxSize">Maximum font size, in pixels.</param>
        public FontAutoSize(bool enabled, float minSize, float maxSize)
        {
            this.enabled = enabled;
            this.minSize = minSize;
            this.maxSize = maxSize;
        }

        ///<summary>Automatic sizing disabled, with a size range of 0 to 100 pixels.</summary>
        public static FontAutoSize Default => new() { enabled = false, minSize = 0, maxSize = 100 };

        ///<exclude />
        public bool Equals(FontAutoSize other)
        {
            return enabled == other.enabled
                && Mathf.Approximately(minSize, other.minSize)
                && Mathf.Approximately(maxSize, other.maxSize);
        }
    }
}

namespace UnityEngine.TextCore.Generation
{
    ///<summary>A struct that stores the settings for the Advanced Text Generator.</summary>
    public struct TextGenerationSettings
    {
        ///<summary>The text to generate.</summary>
        public string text;

        ///<summary>Font to use for generation.</summary>
        public Font font;

        ///<summary>Font size, in pixels.</summary>
        public float fontSize;

        ///<summary>The base color for the text generation.</summary>
        public Color color;

        ///<summary>Font style.</summary>
        public FontStyles fontStyle;

        ///<summary>Font weight.</summary>
        public TextFontWeight fontWeight;

        ///<summary>Horizontal alignment of the text within the extents.</summary>
        public HorizontalAlignment horizontalAlignment;

        ///<summary>Vertical alignment of the text within the extents. Takes effect when the vertical extent is positive.</summary>
        public VerticalAlignment verticalAlignment;

        ///<summary>The base direction paragraphs are laid out in.</summary>
        public TextDirection languageDirection;

        ///<summary>The layout area, in pixels. Text is aligned within it and lines wrap at a positive horizontal extent when <see cref="wrapMode"/> allows it. A zero or negative axis is unconstrained.</summary>
        public Vector2 extents;

        ///<summary>Whether lines wrap at the horizontal extent.</summary>
        public TextWrapMode wrapMode;

        ///<summary>How sequences of white space are handled.</summary>
        public WhitespaceCollapse whitespaceCollapse;

        ///<summary>Automatic font sizing, which shrinks or grows the font size so the text fits the extents.</summary>
        public FontAutoSize autoSize;

        ///<summary>Allow rich text markup in generation.</summary>
        public bool richText;

        static bool CompareColors(Color left, Color right)
        {
            return Mathf.Approximately(left.r, right.r)
                && Mathf.Approximately(left.g, right.g)
                && Mathf.Approximately(left.b, right.b)
                && Mathf.Approximately(left.a, right.a);
        }

        static bool CompareVector2(Vector2 left, Vector2 right)
        {
            return Mathf.Approximately(left.x, right.x) && Mathf.Approximately(left.y, right.y);
        }

        // This differs from the native version because not exactly the same fields are used.
        // Also, if we wanted to compare in native, we would need to materialize the FontAsset and the TextSettings, which we don't want to do in an Equals method.
        ///<exclude />
        public bool Equals(TextGenerationSettings other)
        {
            return text == other.text
                && font == other.font
                && Mathf.Approximately(fontSize, other.fontSize)
                && CompareColors(color, other.color)
                && CompareVector2(extents, other.extents)
                && fontStyle == other.fontStyle
                && fontWeight == other.fontWeight
                && horizontalAlignment == other.horizontalAlignment
                && verticalAlignment == other.verticalAlignment
                && languageDirection == other.languageDirection
                && wrapMode == other.wrapMode
                && whitespaceCollapse == other.whitespaceCollapse
                && richText == other.richText
                && autoSize.Equals(other.autoSize);
        }

        /// <summary>
        /// A set of settings initialized to sensible defaults.
        /// </summary>
        public static TextGenerationSettings Default => new()
        {
            fontSize = 18,
            color = Color.white,
            fontStyle = FontStyles.Normal,
            fontWeight = TextFontWeight.Regular,
            horizontalAlignment = HorizontalAlignment.Left,
            verticalAlignment = VerticalAlignment.Top,
            wrapMode = TextWrapMode.Wrap,
            whitespaceCollapse = WhitespaceCollapse.Preserve,
            autoSize = FontAutoSize.Default,
        };
    }
}
