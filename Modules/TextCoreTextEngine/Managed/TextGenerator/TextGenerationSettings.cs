// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEngine.TextCore.Text
{
    [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
    internal class TextGenerationSettings : IEquatable<TextGenerationSettings>
    {
        [VisibleToOtherModules("UnityEngine.IMGUIModule",  "UnityEngine.UIElementsModule")]
        internal static Func<bool> IsEditorTextRenderingModeBitmap;

        private RenderedText m_RenderedText;
        private string m_CachedRenderedText;

        public RenderedText renderedText
        {
            get => m_RenderedText;
            set
            {
                m_RenderedText = value;
                m_CachedRenderedText = null;
            }
        }

        public string text
        {
            get => m_CachedRenderedText ??= renderedText.CreateString();
            set => renderedText = new RenderedText(value);
        }

        public Rect screenRect;
        public Vector4 margins;
        public float pixelsPerPoint = 1f;
        public bool isEditorRenderingModeBitmap = false;

        public FontAsset fontAsset;
        public Material material;
        public SpriteAsset spriteAsset;
        public TextStyleSheet styleSheet;
        public FontStyles fontStyle = FontStyles.Normal;

        public TextSettings textSettings;

        public TextAlignment textAlignment = TextAlignment.TopLeft;
        public TextOverflowMode overflowMode = TextOverflowMode.Overflow;
        public float wordWrappingRatio;

        public Color color = Color.white;
        public TextColorGradient fontColorGradient;
        public TextColorGradient fontColorGradientPreset;
        public bool tintSprites;
        public bool overrideRichTextColors;
        public bool shouldConvertToLinearSpace = true;

        public float fontSize = 18;
        public bool autoSize;
        public float fontSizeMin;
        public float fontSizeMax;

        public List<OTL_FeatureTag> fontFeatures = new List<OTL_FeatureTag>();
        public bool emojiFallbackSupport = true;
        public bool richText;
        public bool isRightToLeft;
        public float extraPadding = 6.0f;
        public bool parseControlCharacters = true;
        public bool isOrthographic = true;
        public bool isPlaceholder = false;
        public bool tagNoParsing = false;

        public float characterSpacing;
        public float wordSpacing;
        public float lineSpacing;
        public float paragraphSpacing;
        public float lineSpacingMax;
        public TextWrappingMode textWrappingMode = TextWrappingMode.Normal;

        public int maxVisibleCharacters = 99999;
        public int maxVisibleWords = 99999;
        public int maxVisibleLines = 99999;
        public int firstVisibleCharacter = 0;
        public bool useMaxVisibleDescender;

        public TextFontWeight fontWeight = TextFontWeight.Regular;
        public int pageToDisplay = 1;

        public TextureMapping horizontalMapping = TextureMapping.Character;
        public TextureMapping verticalMapping = TextureMapping.Character;
        public float uvLineOffset;
        public VertexSortingOrder geometrySortingOrder = VertexSortingOrder.Normal;
        public bool inverseYAxis;
        public bool isIMGUI;

        public float charWidthMaxAdj;
        internal TextInputSource inputSource = TextInputSource.TextString;

        public TextGenerationSettings() { }

        // Used by automated tests
        internal TextGenerationSettings(TextGenerationSettings tgs)
        {
            m_RenderedText = tgs.m_RenderedText;
            m_CachedRenderedText = tgs.m_CachedRenderedText;
            screenRect = tgs.screenRect;
            margins = tgs.margins;
            pixelsPerPoint = tgs.pixelsPerPoint;
            fontAsset = tgs.fontAsset;
            material = tgs.material;
            spriteAsset = tgs.spriteAsset;
            styleSheet = tgs.styleSheet;
            fontStyle = tgs.fontStyle;
            textSettings = tgs.textSettings;
            textAlignment = tgs.textAlignment;
            overflowMode = tgs.overflowMode;
            wordWrappingRatio = tgs.wordWrappingRatio;
            fontColorGradient = tgs.fontColorGradient;
            fontColorGradientPreset = tgs.fontColorGradientPreset;
            tintSprites = tgs.tintSprites;
            overrideRichTextColors = tgs.overrideRichTextColors;
            shouldConvertToLinearSpace = tgs.shouldConvertToLinearSpace;
            fontSize = tgs.fontSize;
            autoSize = tgs.autoSize;
            fontSizeMin = tgs.fontSizeMin;
            fontSizeMax = tgs.fontSizeMax;
            emojiFallbackSupport = tgs.emojiFallbackSupport;
            richText = tgs.richText;
            isRightToLeft = tgs.isRightToLeft;
            extraPadding = tgs.extraPadding;
            parseControlCharacters = tgs.parseControlCharacters;
            isOrthographic = tgs.isOrthographic;
            isPlaceholder = tgs.isPlaceholder;
            tagNoParsing = tgs.tagNoParsing;
            characterSpacing = tgs.characterSpacing;
            wordSpacing = tgs.wordSpacing;
            lineSpacing = tgs.lineSpacing;
            paragraphSpacing = tgs.paragraphSpacing;
            lineSpacingMax = tgs.lineSpacingMax;
            textWrappingMode = tgs.textWrappingMode;
            maxVisibleCharacters = tgs.maxVisibleCharacters;
            maxVisibleWords = tgs.maxVisibleWords;
            maxVisibleLines = tgs.maxVisibleLines;
            firstVisibleCharacter = tgs.firstVisibleCharacter;
            useMaxVisibleDescender = tgs.useMaxVisibleDescender;
            fontWeight = tgs.fontWeight;
            pageToDisplay = tgs.pageToDisplay;
            horizontalMapping = tgs.horizontalMapping;
            verticalMapping = tgs.verticalMapping;
            uvLineOffset = tgs.uvLineOffset;
            geometrySortingOrder = tgs.geometrySortingOrder;
            inverseYAxis = tgs.inverseYAxis;
            isIMGUI = tgs.isIMGUI;
            charWidthMaxAdj = tgs.charWidthMaxAdj;
            inputSource = tgs.inputSource;
        }

        public bool Equals(TextGenerationSettings other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            bool pixelPerPointsEqual = true;
            if (IsEditorTextRenderingModeBitmap())
                pixelPerPointsEqual = pixelsPerPoint.Equals(other.pixelsPerPoint);

            return m_RenderedText.Equals(other.m_RenderedText) && screenRect.Equals(other.screenRect) && margins.Equals(other.margins)
                && pixelPerPointsEqual && Equals(fontAsset, other.fontAsset) && Equals(material, other.material)
                && Equals(spriteAsset, other.spriteAsset) && Equals(styleSheet, other.styleSheet)
                && fontStyle == other.fontStyle && Equals(textSettings, other.textSettings)
                && textAlignment == other.textAlignment && overflowMode == other.overflowMode
                && wordWrappingRatio.Equals(other.wordWrappingRatio)
                && color.Equals(other.color) && Equals(fontColorGradient, other.fontColorGradient)
                && Equals(fontColorGradientPreset, other.fontColorGradientPreset) && tintSprites == other.tintSprites
                && overrideRichTextColors == other.overrideRichTextColors
                && shouldConvertToLinearSpace == other.shouldConvertToLinearSpace && fontSize.Equals(other.fontSize)
                && autoSize == other.autoSize && fontSizeMin.Equals(other.fontSizeMin)
                && fontSizeMax.Equals(other.fontSizeMax) && Equals(fontFeatures, other.fontFeatures)
                && emojiFallbackSupport == other.emojiFallbackSupport && richText == other.richText
                && isRightToLeft == other.isRightToLeft && extraPadding == other.extraPadding
                && parseControlCharacters == other.parseControlCharacters && isOrthographic == other.isOrthographic
                && isPlaceholder == other.isPlaceholder && tagNoParsing == other.tagNoParsing
                && characterSpacing.Equals(other.characterSpacing) && wordSpacing.Equals(other.wordSpacing)
                && lineSpacing.Equals(other.lineSpacing) && paragraphSpacing.Equals(other.paragraphSpacing)
                && lineSpacingMax.Equals(other.lineSpacingMax) && textWrappingMode == other.textWrappingMode
                && maxVisibleCharacters == other.maxVisibleCharacters && maxVisibleWords == other.maxVisibleWords
                && maxVisibleLines == other.maxVisibleLines && firstVisibleCharacter == other.firstVisibleCharacter
                && useMaxVisibleDescender == other.useMaxVisibleDescender && fontWeight == other.fontWeight
                && horizontalMapping == other.horizontalMapping && verticalMapping == other.verticalMapping
                && uvLineOffset.Equals(other.uvLineOffset) && geometrySortingOrder == other.geometrySortingOrder
                && inverseYAxis == other.inverseYAxis && charWidthMaxAdj.Equals(other.charWidthMaxAdj)
                && isIMGUI == other.isIMGUI;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TextGenerationSettings)obj);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(m_RenderedText);
            hashCode.Add(screenRect);
            hashCode.Add(margins);
            hashCode.Add(fontAsset);
            hashCode.Add(material);
            hashCode.Add(spriteAsset);
            hashCode.Add(styleSheet);
            hashCode.Add((int)fontStyle);
            hashCode.Add(textSettings);
            hashCode.Add((int)textAlignment);
            hashCode.Add((int)overflowMode);
            hashCode.Add(wordWrappingRatio);
            hashCode.Add(color);
            hashCode.Add(fontColorGradient);
            hashCode.Add(fontColorGradientPreset);
            hashCode.Add(tintSprites);
            hashCode.Add(overrideRichTextColors);
            hashCode.Add(shouldConvertToLinearSpace);
            hashCode.Add(fontSize);
            hashCode.Add(autoSize);
            hashCode.Add(fontSizeMin);
            hashCode.Add(fontSizeMax);
            hashCode.Add(fontFeatures);
            hashCode.Add(emojiFallbackSupport);
            hashCode.Add(richText);
            hashCode.Add(isRightToLeft);
            hashCode.Add(extraPadding);
            hashCode.Add(parseControlCharacters);
            hashCode.Add(isOrthographic);
            hashCode.Add(isPlaceholder);
            hashCode.Add(tagNoParsing);
            hashCode.Add(characterSpacing);
            hashCode.Add(wordSpacing);
            hashCode.Add(lineSpacing);
            hashCode.Add(paragraphSpacing);
            hashCode.Add(lineSpacingMax);
            hashCode.Add((int)textWrappingMode);
            hashCode.Add(maxVisibleCharacters);
            hashCode.Add(maxVisibleWords);
            hashCode.Add(maxVisibleLines);
            hashCode.Add(firstVisibleCharacter);
            hashCode.Add(useMaxVisibleDescender);
            hashCode.Add((int)fontWeight);
            hashCode.Add((int)horizontalMapping);
            hashCode.Add((int)verticalMapping);
            hashCode.Add(uvLineOffset);
            hashCode.Add((int)geometrySortingOrder);
            hashCode.Add(inverseYAxis);
            hashCode.Add(charWidthMaxAdj);
            hashCode.Add(isIMGUI);

            if (IsEditorTextRenderingModeBitmap())
                hashCode.Add(pixelsPerPoint);
            return hashCode.ToHashCode();
        }

        public static bool operator ==(TextGenerationSettings left, TextGenerationSettings right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TextGenerationSettings left, TextGenerationSettings right)
        {
            return !Equals(left, right);
        }

        public override string ToString()
        {
            return $"{nameof(text)}: {text}\n {nameof(screenRect)}: {screenRect}\n {nameof(margins)}: {margins}\n {nameof(pixelsPerPoint)}: {pixelsPerPoint}\n {nameof(fontAsset)}: {fontAsset}\n {nameof(material)}: {material}\n {nameof(spriteAsset)}: {spriteAsset}\n {nameof(styleSheet)}: {styleSheet}\n {nameof(fontStyle)}: {fontStyle}\n {nameof(textSettings)}: {textSettings}\n {nameof(textAlignment)}: {textAlignment}\n {nameof(overflowMode)}: {overflowMode}\n {nameof(textWrappingMode)}: {textWrappingMode}\n {nameof(wordWrappingRatio)}: {wordWrappingRatio}\n {nameof(color)}: {color}\n {nameof(fontColorGradient)}: {fontColorGradient}\n {nameof(fontColorGradientPreset)}: {fontColorGradientPreset}\n {nameof(tintSprites)}: {tintSprites}\n {nameof(overrideRichTextColors)}: {overrideRichTextColors}\n {nameof(shouldConvertToLinearSpace)}: {shouldConvertToLinearSpace}\n {nameof(fontSize)}: {fontSize}\n {nameof(autoSize)}: {autoSize}\n {nameof(fontSizeMin)}: {fontSizeMin}\n {nameof(fontSizeMax)}: {fontSizeMax}\n {nameof(richText)}: {richText}\n {nameof(isRightToLeft)}: {isRightToLeft}\n {nameof(extraPadding)}: {extraPadding}\n {nameof(parseControlCharacters)}: {parseControlCharacters}\n {nameof(isOrthographic)}: {isOrthographic}\n {nameof(tagNoParsing)}: {tagNoParsing}\n {nameof(characterSpacing)}: {characterSpacing}\n {nameof(wordSpacing)}: {wordSpacing}\n {nameof(lineSpacing)}: {lineSpacing}\n {nameof(paragraphSpacing)}: {paragraphSpacing}\n {nameof(lineSpacingMax)}: {lineSpacingMax}\n {nameof(textWrappingMode)}: {textWrappingMode}\n {nameof(maxVisibleCharacters)}: {maxVisibleCharacters}\n {nameof(maxVisibleWords)}: {maxVisibleWords}\n {nameof(maxVisibleLines)}: {maxVisibleLines}\n {nameof(firstVisibleCharacter)}: {firstVisibleCharacter}\n {nameof(useMaxVisibleDescender)}: {useMaxVisibleDescender}\n {nameof(fontWeight)}: {fontWeight}\n {nameof(pageToDisplay)}: {pageToDisplay}\n {nameof(horizontalMapping)}: {horizontalMapping}\n {nameof(verticalMapping)}: {verticalMapping}\n {nameof(uvLineOffset)}: {uvLineOffset}\n {nameof(geometrySortingOrder)}: {geometrySortingOrder}\n {nameof(inverseYAxis)}: {inverseYAxis}\n {nameof(charWidthMaxAdj)}: {charWidthMaxAdj}\n {nameof(inputSource)}: {inputSource}\n {nameof(isPlaceholder)}: {isPlaceholder}";
        }
    }
}
