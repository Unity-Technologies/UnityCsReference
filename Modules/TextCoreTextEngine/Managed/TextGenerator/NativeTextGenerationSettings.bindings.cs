// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.TextCore.Text;

namespace UnityEngine.TextCore
{
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/TextCoreTextEngine/Native/TextGenerationSettings.h")]
    [UsedByNativeCode("TextGenerationSettings")]
    [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
    internal struct NativeTextGenerationSettings : IEquatable<NativeTextGenerationSettings>
    {
        public IntPtr fontAsset;
        public IntPtr[] globalFontAssetFallbacks;
        public string text; // TODO: use RenderedText instead of string here
        public int screenWidth;
        public int screenHeight;
        public float fontSize;
        public WhiteSpace wordWrap;
        public LanguageDirection languageDirection;

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal HorizontalAlignment horizontalAlignment;

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal VerticalAlignment verticalAlignment;

        public Color32 color;
        public FontStyles fontStyle;
        public TextFontWeight fontWeight;

        public static NativeTextGenerationSettings Default => new ()
        {
            fontStyle = FontStyles.Normal,
            fontWeight = TextFontWeight.Regular
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
            globalFontAssetFallbacks = tgs.globalFontAssetFallbacks;
            fontStyle = tgs.fontStyle;
            fontWeight = tgs.fontWeight;
            languageDirection = tgs.languageDirection;
        }

        public override string ToString()
        {
            string fallbacksString = globalFontAssetFallbacks != null
              ? $"{string.Join(", ", globalFontAssetFallbacks)}"
              : "null";

            return $"{nameof(fontAsset)}: {fontAsset}\n" +
               $"{nameof(globalFontAssetFallbacks)}: {fallbacksString}\n" +
               $"{nameof(text)}: {text}\n" +
               $"{nameof(screenWidth)}: {screenWidth}\n" +
               $"{nameof(screenHeight)}: {screenHeight}\n" +
               $"{nameof(fontSize)}: {fontSize}\n" +
               $"{nameof(wordWrap)}: {wordWrap}\n" +
               $"{nameof(languageDirection)}: {languageDirection}\n";
        }

        public bool Equals(NativeTextGenerationSettings other)
        {
            return fontAsset == other.fontAsset &&
                   text == other.text &&
                   screenWidth == other.screenWidth &&
                   screenHeight == other.screenHeight &&
                   fontSize.Equals(other.fontSize) &&
                   wordWrap == other.wordWrap &&
                   languageDirection == other.languageDirection &&
                   horizontalAlignment == other.horizontalAlignment &&
                   verticalAlignment == other.verticalAlignment &&
                   color.Equals(other.color) &&
                   fontStyle == other.fontStyle &&
                   fontWeight == other.fontWeight;
        }

        public override bool Equals(object obj)
        {
            return obj is NativeTextGenerationSettings other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(fontAsset);
            hashCode.Add(text);
            hashCode.Add(screenWidth);
            hashCode.Add(screenHeight);
            hashCode.Add(fontSize);
            hashCode.Add(wordWrap);
            hashCode.Add((int)languageDirection);
            hashCode.Add((int)horizontalAlignment);
            hashCode.Add((int)verticalAlignment);
            hashCode.Add(color);
            hashCode.Add((int)fontStyle);
            hashCode.Add((int)fontWeight);
            return hashCode.ToHashCode();
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

}
