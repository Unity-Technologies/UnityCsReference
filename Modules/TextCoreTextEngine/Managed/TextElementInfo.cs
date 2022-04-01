// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.TextCore.Text
{
    struct TextVertex
    {
        public Vector3 position;
        public Vector4 uv;
        public Vector2 uv2;
        public Color32 color;
    }

    /// <summary>
    /// Structure containing information about individual text elements (character or sprites).
    /// </summary>
    struct TextElementInfo
    {
        public char character; // Should be changed to an int to handle UTF 32
        public int index; // Index of the character in the input string.
        public TextElementType elementType;
        public int stringLength;

        public TextElement textElement;
        public Glyph alternativeGlyph;
        public FontAsset fontAsset;
        public SpriteAsset spriteAsset;
        public int spriteIndex;
        public Material material;
        public int materialReferenceIndex;
        public bool isUsingAlternateTypeface;

        public float pointSize;

        public int lineNumber;
        public int pageNumber;

        public int vertexIndex;
        public TextVertex vertexTopLeft;
        public TextVertex vertexBottomLeft;
        public TextVertex vertexTopRight;
        public TextVertex vertexBottomRight;

        public Vector3 topLeft;
        public Vector3 bottomLeft;
        public Vector3 topRight;
        public Vector3 bottomRight;
        public float origin;
        public float ascender;
        public float baseLine;
        public float descender;
        internal float adjustedAscender;
        internal float adjustedDescender;
        internal float adjustedHorizontalAdvance;

        public float xAdvance;
        public float aspectRatio;
        public float scale;
        public Color32 color;
        public Color32 underlineColor;
        public int underlineVertexIndex;
        public Color32 strikethroughColor;
        public int strikethroughVertexIndex;
        public Color32 highlightColor;
        public HighlightState highlightState;
        public FontStyles style;
        public bool isVisible;
    }
}
