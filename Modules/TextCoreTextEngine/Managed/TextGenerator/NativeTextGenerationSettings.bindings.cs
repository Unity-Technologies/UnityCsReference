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
        public WhiteSpace wordWrap;
        public TextOverflow overflow;
        public LanguageDirection languageDirection;
        public int vertexPadding; // Encoded in Fixed Point.
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal HorizontalAlignment horizontalAlignment;

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal VerticalAlignment verticalAlignment;

        public int fontSize;        // Encoded in Fixed Point.
        public FontStyles fontStyle;
        public TextFontWeight fontWeight;

        public TextSpan[] textSpans;
        public Color32 color;

        public int characterSpacing;        // Encoded in Fixed Point.
        public int wordSpacing;             // Encoded in Fixed Point.
        public int paragraphSpacing;        // Encoded in Fixed Point.

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
            screenWidth = tgs.screenWidth;
            screenHeight = tgs.screenHeight;
            wordWrap = tgs.wordWrap;
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
                $"{nameof(wordWrap)}: {wordWrap}\n" +
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
                $"{nameof(wordSpacing)}: {wordSpacing}\n";
        }
    }

    [VisibleToOtherModules( "UnityEngine.UIElementsModule")]
    [StructLayout(LayoutKind.Sequential)]
    internal struct TextSpan
    {
        public int startIndex;
        public int length;
        public IntPtr fontAsset;
        public int fontSize;        // Encoded in Fixed Point.
        public Color32 color;
        public FontStyles fontStyle;
        public TextFontWeight fontWeight;
        public int linkID;

        public override string ToString()
        {
            return $"{nameof(color)}: {color}\n" +
                $"{nameof(fontStyle)}: {fontStyle}\n" +
                $"{nameof(fontWeight)}: {fontWeight}\n" +
                $"{nameof(linkID)}: {linkID}\n" +
                $"{nameof(fontSize)}: {fontSize}\n" +
                $"{nameof(fontAsset)}: {fontAsset}" +
                $"{nameof(startIndex)}: {startIndex}\n" +
                $"{nameof(length)}: {length}";
        }
    }

    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    internal enum HorizontalAlignment
    {
        Left,
        Center,
        Right,
        Justified
    }

    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    internal enum VerticalAlignment
    {
        Top,
        Middle,
        Bottom
    }

    /// <summary>
    /// Indicates the directionality of the element's text.
    /// </summary>
    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
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

    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    internal enum WhiteSpace
    {
        Normal,
        NoWrap,
        Pre,
        PreWrap
    }

    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    internal enum TextOverflow
    {
        Clip,
        Ellipsis
    }
}
