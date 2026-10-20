// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.TextCore.Text;
using Unity.Collections.LowLevel.Unsafe;

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
        public IntPtr textBufferPtr;
        public int textBufferLength;
        public int screenWidth;     // Encoded in Fixed Point.
        public int screenHeight;    // Encoded in Fixed Point.
        public bool wordWrapEnabled;
        public TextOverflow overflow;
        public LanguageDirection languageDirection;
        public int vertexPadding;   // Encoded in Fixed Point.
        [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
        internal HorizontalAlignment horizontalAlignment;

        [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
        internal VerticalAlignment verticalAlignment;

        public PreProcessFlags preProcessFlags;
        public int fontSize;        // Encoded in Fixed Point.

        public bool bestFit;
        public int maxFontSize;     // Encoded in Fixed Point.
        public int minFontSize;     // Encoded in Fixed Point.

        public FontStyles fontStyle;
        public TextFontWeight fontWeight;

        public int characterSpacing;        // Encoded in Fixed Point.
        public int wordSpacing;             // Encoded in Fixed Point.
        public int paragraphSpacing;        // Encoded in Fixed Point.
        public int lineSpacing;             // Encoded in Fixed Point.

        public Color32 color;

        public bool disableAdvancedFontFeatures;
        public bool richTextEnabled;

        // Index of the link currently hovered by the pointer (or a HoveredTag sentinel).
        // Read by the native rich-text parser to apply hover styling.
        [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
        internal HoveredTag hoveredTag;

        [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
        internal int pixelsPerPointFixed64;

        // Measure the intrinsic min-content inline size (see native TextGenerationSettings.minContentMeasure).
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal bool minContentMeasure;

        // Emit vertex positions with Y up (Y negated) instead of the default Y-down UI convention.
        public bool flipYAxis;

        [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
        internal unsafe void SetTextBuffer(Unity.Collections.NativeArray<char> buffer, int length)
        {
            textBufferPtr = length > 0
                ? (IntPtr)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(buffer)
                : IntPtr.Zero;
            textBufferLength = length;
        }

        public static NativeTextGenerationSettings Default => new ()
        {
            fontStyle = FontStyles.Normal,
            fontWeight = TextFontWeight.Regular,
            color = Color.black,
            hoveredTag = HoveredTag.None,
            pixelsPerPointFixed64 = 64,
        };

        // Used by automated tests
        internal NativeTextGenerationSettings(NativeTextGenerationSettings tgs)
        {
            textBufferPtr = tgs.textBufferPtr;
            textBufferLength = tgs.textBufferLength;
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
            characterSpacing = tgs.characterSpacing;
            wordSpacing = tgs.wordSpacing;
            paragraphSpacing = tgs.paragraphSpacing;
            lineSpacing = tgs.lineSpacing;
            preProcessFlags = tgs.preProcessFlags;
            disableAdvancedFontFeatures = tgs.disableAdvancedFontFeatures;
            richTextEnabled = tgs.richTextEnabled;
            hoveredTag = tgs.hoveredTag;
            pixelsPerPointFixed64 = tgs.pixelsPerPointFixed64;
            minContentMeasure = tgs.minContentMeasure;
            flipYAxis = tgs.flipYAxis;
        }

        public override string ToString()
        {
            return $"{nameof(fontAsset)}: {fontAsset}\n" +
                $"{nameof(textSettings)}: {textSettings}\n" +
                $"{nameof(textBufferLength)}: {textBufferLength}\n" +
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
                $"{nameof(characterSpacing)}: {characterSpacing}\n" +
                $"{nameof(paragraphSpacing)}: {paragraphSpacing}\n" +
                $"{nameof(wordSpacing)}: {wordSpacing}\n" +
                $"{nameof(lineSpacing)}: {lineSpacing}\n" +
                $"{nameof(preProcessFlags)}: {preProcessFlags}\n" +
                $"{nameof(disableAdvancedFontFeatures)}: {disableAdvancedFontFeatures}\n" +
                $"{nameof(richTextEnabled)}: {richTextEnabled}\n" +
                $"{nameof(minContentMeasure)}: {minContentMeasure}\n" +
                $"{nameof(flipYAxis)}: {flipYAxis}\n";
        }

        // TODO : It's not ideal to have GetHashCode both in C# and C++. We would ideally keep only C++, but because of the string marshalling involved this is too costly for IMGUI.
        // Remove this once we have a free interop to native.
        // Must mix: with a linear hash, "0" and "1" collide when their widths differ slightly (UUM-147879)
        public override unsafe int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(fontAsset);
            hash.Add(textSettings);

            if (textBufferPtr != IntPtr.Zero && textBufferLength > 0)
            {
                char* p = (char*)textBufferPtr;
                for (int i = 0; i < textBufferLength; i++)
                    hash.Add(p[i]);
            }

            hash.Add(screenWidth);
            hash.Add(screenHeight);
            hash.Add(fontSize);
            hash.Add(fontStyle);
            hash.Add(fontWeight);
            hash.Add(wordWrapEnabled);
            hash.Add(overflow);
            hash.Add(languageDirection);
            hash.Add(horizontalAlignment);
            hash.Add(verticalAlignment);
            hash.Add(bestFit);
            hash.Add(disableAdvancedFontFeatures);
            hash.Add(color);
            hash.Add(richTextEnabled);
            hash.Add(minContentMeasure);
            hash.Add(flipYAxis);
            return hash.ToHashCode();
        }

        // GetHashCode can collide; caches keyed by it confirm hits with this plus a text comparison
        [VisibleToOtherModules("UnityEngine.IMGUIModule")]
        internal bool EqualsIgnoringTextBuffer(in NativeTextGenerationSettings other)
        {
            return fontAsset == other.fontAsset
                && textSettings == other.textSettings
                && screenWidth == other.screenWidth
                && screenHeight == other.screenHeight
                && wordWrapEnabled == other.wordWrapEnabled
                && overflow == other.overflow
                && languageDirection == other.languageDirection
                && vertexPadding == other.vertexPadding
                && horizontalAlignment == other.horizontalAlignment
                && verticalAlignment == other.verticalAlignment
                && preProcessFlags == other.preProcessFlags
                && fontSize == other.fontSize
                && bestFit == other.bestFit
                && maxFontSize == other.maxFontSize
                && minFontSize == other.minFontSize
                && fontStyle == other.fontStyle
                && fontWeight == other.fontWeight
                && characterSpacing == other.characterSpacing
                && wordSpacing == other.wordSpacing
                && paragraphSpacing == other.paragraphSpacing
                && lineSpacing == other.lineSpacing
                && color.Equals(in other.color)
                && disableAdvancedFontFeatures == other.disableAdvancedFontFeatures
                && richTextEnabled == other.richTextEnabled
                && hoveredTag == other.hoveredTag
                && pixelsPerPointFixed64 == other.pixelsPerPointFixed64
                && minContentMeasure == other.minContentMeasure
                && flipYAxis == other.flipYAxis;
        }
    }

    ///<summary>How text is horizontally aligned within its layout area.</summary>
    public enum HorizontalAlignment
    {
        ///<summary>Text is aligned to the left of the layout area.</summary>
        Left,
        ///<summary>Text is centered within the layout area.</summary>
        Center,
        ///<summary>Text is aligned to the right of the layout area.</summary>
        Right,
        ///<summary>Spaces are stretched so lines fill the layout area; the last line stays left-aligned.</summary>
        Justified,
        ///<summary>Spaces are stretched so every line, including the last, fills the layout area.</summary>
        Flush
    }

    ///<summary>How text is vertically aligned within its layout area.</summary>
    public enum VerticalAlignment
    {
        ///<summary>Text is aligned to the top of the layout area.</summary>
        Top,
        ///<summary>Text is centered vertically within the layout area.</summary>
        Middle,
        ///<summary>Text is aligned to the bottom of the layout area.</summary>
        Bottom
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
        Ellipsis,
        EllipsisStart,
        EllipsisMiddle
    }

    // Identifies the link currently under the pointer for hover styling. Non-negative
    // values are link indices; the values below are sentinels.
    [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
    internal enum HoveredTag
    {
        None = -1,              // No link is hovered.
        UnderlineAllLinks = -2, // Treat every link as hovered (underline all links).
    }
}
