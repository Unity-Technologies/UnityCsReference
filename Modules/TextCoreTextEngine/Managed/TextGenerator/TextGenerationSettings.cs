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

        public FontAsset fontAsset;
        public FontStyles fontStyle = FontStyles.Normal;

        public TextSettings textSettings;

        public TextAlignment textAlignment = TextAlignment.TopLeft;
        public TextOverflowMode overflowMode = TextOverflowMode.Overflow;
        public const float wordWrappingRatio = 0.4f;

        public Color color = Color.white;
        public bool shouldConvertToLinearSpace = true;

        public int fontSize = 18;
        public const bool autoSize = default;
        public const float fontSizeMin = default;
        public const float fontSizeMax = default;

        readonly internal static List<OTL_FeatureTag> fontFeatures = new List<OTL_FeatureTag>() { OTL_FeatureTag.kern };

        public bool emojiFallbackSupport = true;
        public bool richText;
        public bool isRightToLeft;
        public float extraPadding = 6.0f;
        public bool parseControlCharacters = true;
        public bool isPlaceholder = false;
        public const bool tagNoParsing = false;

        public float characterSpacing;
        public float wordSpacing;
        public const float lineSpacing = default;
        public float paragraphSpacing;
        public const float lineSpacingMax = default;
        public TextWrappingMode textWrappingMode = TextWrappingMode.Normal;

        public const int maxVisibleCharacters = 99999;
        public const int maxVisibleWords = 99999;
        public const int maxVisibleLines = 99999;
        public const int firstVisibleCharacter = 0;
        public const bool useMaxVisibleDescender = default;

        public TextFontWeight fontWeight = TextFontWeight.Regular;

        public bool isIMGUI;

        public const float charWidthMaxAdj = default;

        public TextGenerationSettings() { }

        // Used by automated tests
        internal TextGenerationSettings(TextGenerationSettings tgs)
        {
            m_RenderedText = tgs.m_RenderedText;
            m_CachedRenderedText = tgs.m_CachedRenderedText;
            screenRect = tgs.screenRect;
            fontAsset = tgs.fontAsset;
            fontStyle = tgs.fontStyle;
            textSettings = tgs.textSettings;
            textAlignment = tgs.textAlignment;
            overflowMode = tgs.overflowMode;
            shouldConvertToLinearSpace = tgs.shouldConvertToLinearSpace;
            fontSize = tgs.fontSize;
            emojiFallbackSupport = tgs.emojiFallbackSupport;
            richText = tgs.richText;
            isRightToLeft = tgs.isRightToLeft;
            extraPadding = tgs.extraPadding;
            parseControlCharacters = tgs.parseControlCharacters;
            isPlaceholder = tgs.isPlaceholder;
            characterSpacing = tgs.characterSpacing;
            wordSpacing = tgs.wordSpacing;
            paragraphSpacing = tgs.paragraphSpacing;
            textWrappingMode = tgs.textWrappingMode;
            fontWeight = tgs.fontWeight;
            isIMGUI = tgs.isIMGUI;
        }

        public bool Equals(TextGenerationSettings other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;


            return m_RenderedText.Equals(other.m_RenderedText) && screenRect.Equals(other.screenRect)
                && Equals(fontAsset, other.fontAsset)
                && fontStyle == other.fontStyle && Equals(textSettings, other.textSettings)
                && textAlignment == other.textAlignment && overflowMode == other.overflowMode
                && color.Equals(other.color)
                && fontSize.Equals(other.fontSize)
                && shouldConvertToLinearSpace == other.shouldConvertToLinearSpace
                && emojiFallbackSupport == other.emojiFallbackSupport && richText == other.richText
                && isRightToLeft == other.isRightToLeft && extraPadding == other.extraPadding
                && parseControlCharacters == other.parseControlCharacters 
                && isPlaceholder == other.isPlaceholder
                && characterSpacing.Equals(other.characterSpacing) && wordSpacing.Equals(other.wordSpacing)
                && paragraphSpacing.Equals(other.paragraphSpacing)
                && textWrappingMode == other.textWrappingMode
                && fontWeight == other.fontWeight
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
            hashCode.Add(fontAsset);
            hashCode.Add((int)fontStyle);
            hashCode.Add(textSettings);
            hashCode.Add((int)textAlignment);
            hashCode.Add((int)overflowMode);
            hashCode.Add(color);
            hashCode.Add(shouldConvertToLinearSpace);
            hashCode.Add(fontSize);
            hashCode.Add(emojiFallbackSupport);
            hashCode.Add(richText);
            hashCode.Add(isRightToLeft);
            hashCode.Add(extraPadding);
            hashCode.Add(parseControlCharacters);
            hashCode.Add(isPlaceholder);
            hashCode.Add(characterSpacing);
            hashCode.Add(wordSpacing);
            hashCode.Add(paragraphSpacing);
            hashCode.Add((int)textWrappingMode);
            hashCode.Add((int)fontWeight);
            hashCode.Add(isIMGUI);
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
            return $"{nameof(text)}: {text}\n {nameof(screenRect)}: {screenRect}\n {nameof(fontAsset)}: {fontAsset}\n " +
                $"{nameof(fontStyle)}: {fontStyle}\n {nameof(textSettings)}: {textSettings}\n {nameof(textAlignment)}: {textAlignment}\n {nameof(overflowMode)}: {overflowMode}\n {nameof(textWrappingMode)}: {textWrappingMode}\n " +
                $"{nameof(color)}: {color}\n {nameof(fontSize)}: {fontSize}\n {nameof(richText)}: {richText}\n {nameof(isRightToLeft)}: {isRightToLeft}\n {nameof(extraPadding)}: {extraPadding}\n " +
                $"{nameof(parseControlCharacters)}: {parseControlCharacters}\n {nameof(characterSpacing)}: {characterSpacing}\n {nameof(wordSpacing)}: {wordSpacing}\n {nameof(paragraphSpacing)}: {paragraphSpacing}\n " +
                $"{nameof(textWrappingMode)}: {textWrappingMode}\n {nameof(fontWeight)}: {fontWeight}\n {nameof(shouldConvertToLinearSpace)}: {shouldConvertToLinearSpace}\n {nameof(isPlaceholder)}: {isPlaceholder}";
        }
    }
}
