// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.TextCore.Text;
using System.Collections.Generic;

namespace UnityEngine.TextCore
{
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/TextCoreTextEngine/Native/TextGenerationSettings.h")]
    [UsedByNativeCode("TextGenerationSettings")]
    [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
    internal struct NativeTextGenerationSettings
    {
        public IntPtr fontAsset;
        public IntPtr textSettings;
        public string text;         // Contains the parsed text, meaning the rich text tags have been removed.
        public int screenWidth;     // Encoded in Fixed Point.
        public int screenHeight;    // Encoded in Fixed Point.
        public bool wordWrapEnabled;
        public TextOverflow overflow;
        public LanguageDirection languageDirection;
        public int vertexPadding; // Encoded in Fixed Point.
        [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
        internal HorizontalAlignment horizontalAlignment;

        [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
        internal VerticalAlignment verticalAlignment;

        public int fontSize;        // Encoded in Fixed Point.

        public bool bestFit;
        public int maxFontSize;          // Encoded in Fixed Point.
        public int minFontSize;          // Encoded in Fixed Point.

        public FontStyles fontStyle;
        public TextFontWeight fontWeight;

        public TextSpan[] textSpans;
        public Color32 color;

        public int characterSpacing;        // Encoded in Fixed Point.
        public int wordSpacing;             // Encoded in Fixed Point.
        public int paragraphSpacing;        // Encoded in Fixed Point.

        public PreProcessFlags preProcessFlags;

        public bool disableAdvancedFontFeatures;
        public bool richTextEnabled;

        public bool hasLink => textSpans != null && Array.Exists(textSpans, span => span.linkID != -1);

        public readonly TextSpan CreateTextSpan()
        {
            return new TextSpan()
            {
                fontAsset = this.fontAsset,
                fontSize = this.fontSize,
                color = this.color,
                fontStyle = this.fontStyle,
                fontWeight = this.fontWeight,
                alignment = this.horizontalAlignment,
                highlightColor = RichTextTagParser.k_HighlightColor,
                highlightPadding = Vector4.zero,
                mspace = 0,
                mspaceUnitType = RichTextTagParser.TagUnitType.Pixels,
                cspace = 0,
                cspaceUnitType = RichTextTagParser.TagUnitType.Pixels,
                spriteColor = this.color,
                spriteID = -1,
                spriteScale = 0,
                spriteTint = false,
                margin = 0,
                marginDirection = MarginDirection.Both,
                marginUnitType = RichTextTagParser.TagUnitType.Pixels,
                indent = 0,
                indentUnitType = RichTextTagParser.TagUnitType.Pixels,
                linkID = -1
            };
        }

        // Used by automated tests
        public string GetTextSpanContent(int spanIndex)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new InvalidOperationException("The text property is null or empty.");
            }

            if (textSpans == null || spanIndex < 0 || spanIndex >= textSpans.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(spanIndex), "Invalid span index.");
            }

            TextSpan span = textSpans[spanIndex];

            if (span.startIndex < 0 || span.startIndex >= text.Length || span.startIndex + span.length > text.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(spanIndex), "Invalid startIndex or length for the current text.");
            }

            return text.Substring(span.startIndex, span.length);
        }

        public static NativeTextGenerationSettings Default => new ()
        {
            fontStyle = FontStyles.Normal,
            fontWeight = TextFontWeight.Regular,
            color = Color.black,
        };

        // Used by automated tests
        internal NativeTextGenerationSettings(NativeTextGenerationSettings tgs)
        {
            text = tgs.text;
            fontSize = tgs.fontSize;
            bestFit = tgs.bestFit;
            maxFontSize = tgs.maxFontSize;
            minFontSize = tgs.minFontSize;
            screenWidth = tgs.screenWidth;
            screenHeight = tgs.screenHeight;
            wordWrapEnabled = tgs.wordWrapEnabled;
            horizontalAlignment = tgs.horizontalAlignment;
            verticalAlignment = tgs.verticalAlignment;
            color = tgs.color;
            fontAsset = tgs.fontAsset;
            textSettings = tgs.textSettings;
            fontStyle = tgs.fontStyle;
            fontWeight = tgs.fontWeight;
            languageDirection = tgs.languageDirection;
            vertexPadding = tgs.vertexPadding;
            overflow = tgs.overflow;
            textSpans = tgs.textSpans != null ? (TextSpan[])tgs.textSpans.Clone() : null;
            characterSpacing = tgs.characterSpacing;
            wordSpacing = tgs.wordSpacing;
            paragraphSpacing = tgs.paragraphSpacing;
            preProcessFlags = tgs.preProcessFlags;
            disableAdvancedFontFeatures = tgs.disableAdvancedFontFeatures;
            richTextEnabled = tgs.richTextEnabled;
        }

        public override string ToString()
        {
            string textSpansString = "null";
            if (textSpans != null)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append("[");
                for (int i = 0; i < textSpans.Length; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(", ");
                    }
                    sb.Append(textSpans[i].ToString());
                }
                sb.Append("]");
                textSpansString = sb.ToString();
            }

            return $"{nameof(fontAsset)}: {fontAsset}\n" +
                $"{nameof(textSettings)}: {textSettings}\n" +
                $"{nameof(text)}: {text}\n" +
                $"{nameof(screenWidth)}: {screenWidth}\n" +
                $"{nameof(screenHeight)}: {screenHeight}\n" +
                $"{nameof(fontSize)}: {fontSize}\n" +
                $"{nameof(bestFit)}: {bestFit}\n" +
                $"{nameof(maxFontSize)}: {maxFontSize}\n" +
                $"{nameof(minFontSize)}: {minFontSize}\n" +
                $"{nameof(wordWrapEnabled)}: {wordWrapEnabled}\n" +
                $"{nameof(languageDirection)}: {languageDirection}\n" +
                $"{nameof(horizontalAlignment)}: {horizontalAlignment}\n" +
                $"{nameof(verticalAlignment)}: {verticalAlignment}\n" +
                $"{nameof(color)}: {color}\n" +
                $"{nameof(fontStyle)}: {fontStyle}\n" +
                $"{nameof(fontWeight)}: {fontWeight}\n" +
                $"{nameof(vertexPadding)}: {vertexPadding}\n" +
                $"{nameof(overflow)}: {overflow}\n" +
                $"{nameof(textSpans)}: {textSpansString}\n" +
                $"{nameof(characterSpacing)}: {characterSpacing}\n" +
                $"{nameof(paragraphSpacing)}: {paragraphSpacing}\n" +
                $"{nameof(wordSpacing)}: {wordSpacing}\n" +
                $"{nameof(preProcessFlags)}: {preProcessFlags}\n" +
                $"{nameof(disableAdvancedFontFeatures)}: {disableAdvancedFontFeatures}\n" +
                $"{nameof(richTextEnabled)}: {richTextEnabled}\n";
        }


        // TODO : It's not ideal to have GetHashCode both in C# and C++. We would ideally keep only C++, but because of the string marshalling involved this is too costly for IMGUI.
        // Remove this once we have a free interop to native.
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + fontAsset.GetHashCode();
                hash = hash * 23 + textSettings.GetHashCode();
                hash = hash * 23 + (text != null ? text.GetHashCode() : 0);
                hash = hash * 23 + screenWidth;
                hash = hash * 23 + screenHeight;
                hash = hash * 23 + fontSize;
                hash = hash * 23 + fontStyle.GetHashCode();
                hash = hash * 23 + fontWeight.GetHashCode();
                hash = hash * 23 + wordWrapEnabled.GetHashCode();
                hash = hash * 23 + overflow.GetHashCode();
                hash = hash * 23 + languageDirection.GetHashCode();
                hash = hash * 23 + horizontalAlignment.GetHashCode();
                hash = hash * 23 + verticalAlignment.GetHashCode();
                hash = hash * 23 + bestFit.GetHashCode();
                hash = hash * 23 + disableAdvancedFontFeatures.GetHashCode();
                hash = hash * 23 + color.GetHashCode();
                hash = hash * 23 + richTextEnabled.GetHashCode();
                return hash;
            }
        }
    }

    [VisibleToOtherModules( "UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
    [StructLayout(LayoutKind.Sequential)]
    internal struct TextSpan
    {
        public int startIndex;
        public int length;
        public IntPtr fontAsset;
        public IntPtr gradientAsset;
        public int fontSize;        // Encoded in Fixed Point.
        public Color32 color;
        public FontStyles fontStyle;
        public TextFontWeight fontWeight;
        public int mspace;          // Encoded in Fixed Point.
        public RichTextTagParser.TagUnitType mspaceUnitType;
        public int cspace;           // Encoded in Fixed Point.
        public RichTextTagParser.TagUnitType cspaceUnitType;
        public int linkID;
        public HorizontalAlignment alignment;
        public Color32 highlightColor;
        public Vector4 highlightPadding;
        public GlyphMetrics spriteMetrics;
        public int spriteID;
        public bool spriteTint;
        public int spriteScale;
        public Color32 spriteColor;
        public int margin;
        public MarginDirection marginDirection;
        public RichTextTagParser.TagUnitType marginUnitType;
        public int indent;          // Encoded in Fixed Point.
        public RichTextTagParser.TagUnitType indentUnitType;

        public override string ToString()
        {
            return $"{nameof(color)}: {color}\n" +
                $"{nameof(fontStyle)}: {fontStyle}\n" +
                $"{nameof(fontWeight)}: {fontWeight}\n" +
                $"{nameof(linkID)}: {linkID}\n" +
                $"{nameof(fontSize)}: {fontSize}\n" +
                $"{nameof(fontAsset)}: {fontAsset}" +
                $"{nameof(gradientAsset)}: {gradientAsset}\n" +
                $"{nameof(startIndex)}: {startIndex}\n" +
                $"{nameof(length)}: {length}";
        }
    }

    [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
    internal enum HorizontalAlignment
    {
        Left,
        Center,
        Right,
        Justified,
        Flush
    }

    [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
    internal enum VerticalAlignment
    {
        Top,
        Middle,
        Bottom
    }

    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    internal enum MarginDirection
    {
        Both,
        Left,
        Right
    }

    /// <summary>
    /// Indicates the directionality of the element's text.
    /// </summary>
    [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
    internal enum LanguageDirection
    {
        /// <summary>
        /// Left-to-right language direction.
        /// </summary>
        LTR,
        /// <summary>
        /// Right-to-left language direction.
        /// </summary>
        RTL
    }

    [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
    internal enum WhiteSpace
    {
        Normal,
        NoWrap,
        Pre,
        PreWrap
    }

    [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
    internal enum PreProcessFlags
    {
        None,
        CollapseWhiteSpaces,
        ParseEscapeSequences
    }

    [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
    internal enum TextOverflow
    {
        Clip,
        Ellipsis
    }
}
