// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
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

        public override string ToString()
        {
            return $"{nameof(character)}: {character}\n{nameof(index)}: {index}\n{nameof(elementType)}: {elementType}\n{nameof(stringLength)}: {stringLength}\n{nameof(textElement)}: {textElement}\n{nameof(alternativeGlyph)}: {alternativeGlyph}\n{nameof(fontAsset)}: {fontAsset}\n{nameof(spriteAsset)}: {spriteAsset}\n{nameof(spriteIndex)}: {spriteIndex}\n{nameof(material)}: {material}\n{nameof(materialReferenceIndex)}: {materialReferenceIndex}\n{nameof(isUsingAlternateTypeface)}: {isUsingAlternateTypeface}\n{nameof(pointSize)}: {pointSize}\n{nameof(lineNumber)}: {lineNumber}\n{nameof(pageNumber)}: {pageNumber}\n{nameof(vertexIndex)}: {vertexIndex}\n{nameof(vertexTopLeft)}: {vertexTopLeft}\n{nameof(vertexBottomLeft)}: {vertexBottomLeft}\n{nameof(vertexTopRight)}: {vertexTopRight}\n{nameof(vertexBottomRight)}: {vertexBottomRight}\n{nameof(topLeft)}: {topLeft}\n{nameof(bottomLeft)}: {bottomLeft}\n{nameof(topRight)}: {topRight}\n{nameof(bottomRight)}: {bottomRight}\n{nameof(origin)}: {origin}\n{nameof(ascender)}: {ascender}\n{nameof(baseLine)}: {baseLine}\n{nameof(descender)}: {descender}\n{nameof(adjustedAscender)}: {adjustedAscender}\n{nameof(adjustedDescender)}: {adjustedDescender}\n{nameof(adjustedHorizontalAdvance)}: {adjustedHorizontalAdvance}\n{nameof(xAdvance)}: {xAdvance}\n{nameof(aspectRatio)}: {aspectRatio}\n{nameof(scale)}: {scale}\n{nameof(color)}: {color}\n{nameof(underlineColor)}: {underlineColor}\n{nameof(underlineVertexIndex)}: {underlineVertexIndex}\n{nameof(strikethroughColor)}: {strikethroughColor}\n{nameof(strikethroughVertexIndex)}: {strikethroughVertexIndex}\n{nameof(highlightColor)}: {highlightColor}\n{nameof(highlightState)}: {highlightState}\n{nameof(style)}: {style}\n{nameof(isVisible)}: {isVisible}";
        }

        // Used in automated tests.
        internal string ToStringTest()
        {
            return $"topLeft.x: {topLeft.x.ToString("F4")}\n topLeft.y: {topLeft.y.ToString("F4")}\n topRight.x: {topRight.x.ToString("F4")}\n topRight.y: {topRight.y.ToString("F4")}\n  bottomLeft.x: {bottomLeft.x.ToString("F4")}\n bottomLeft.y: {bottomLeft.y.ToString("F4")}\n  bottomRight.x: {bottomRight.x.ToString("F4")}\n bottomRight.y: {bottomRight.y.ToString("F4")}\n{nameof(origin)}: {origin.ToString("F4")}\n{nameof(xAdvance)}: {xAdvance.ToString("F4")}\n";
        }
    }
}
