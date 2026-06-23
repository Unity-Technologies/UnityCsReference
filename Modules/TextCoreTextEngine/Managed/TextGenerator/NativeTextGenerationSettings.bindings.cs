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

        public Color32 color;

        public bool disableAdvancedFontFeatures;
        public bool richTextEnabled;

        // Index of the link currently hovered by the pointer (or a HoveredTag sentinel).
        // Read by the native rich-text parser to apply hover styling.
        [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
        internal HoveredTag hoveredTag;

        [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
        internal int pixelsPerPointFixed64;

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
            preProcessFlags = tgs.preProcessFlags;
            disableAdvancedFontFeatures = tgs.disableAdvancedFontFeatures;
            richTextEnabled = tgs.richTextEnabled;
            hoveredTag = tgs.hoveredTag;
            pixelsPerPointFixed64 = tgs.pixelsPerPointFixed64;
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
                $"{nameof(preProcessFlags)}: {preProcessFlags}\n" +
                $"{nameof(disableAdvancedFontFeatures)}: {disableAdvancedFontFeatures}\n" +
                $"{nameof(richTextEnabled)}: {richTextEnabled}\n";
        }

        // TODO : It's not ideal to have GetHashCode both in C# and C++. We would ideally keep only C++, but because of the string marshalling involved this is too costly for IMGUI.
        // Remove this once we have a free interop to native.
        public override unsafe int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + fontAsset.GetHashCode();
                hash = hash * 23 + textSettings.GetHashCode();

                if (textBufferPtr != IntPtr.Zero && textBufferLength > 0)
                {
                    char* p = (char*)textBufferPtr;
                    for (int i = 0; i < textBufferLength; i++)
                        hash = hash * 23 + p[i];
                }

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

    // Identifies the link currently under the pointer for hover styling. Non-negative
    // values are link indices; the values below are sentinels.
    [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
    internal enum HoveredTag
    {
        None = -1,              // No link is hovered.
        UnderlineAllLinks = -2, // Treat every link as hovered (underline all links).
    }
}
