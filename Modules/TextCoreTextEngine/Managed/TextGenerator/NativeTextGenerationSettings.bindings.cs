// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
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
        public string text;
        public int screenWidth;
        public int screenHeight;
        public float fontSize;
        public bool wrapText;
        public LanguageDirection languageDirection;

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal HorizontalAlignment horizontalAlignment;

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal VerticalAlignment verticalAlignment;

        public Color32 color;
        public FontStyles fontStyle = FontStyles.Normal;
        public TextFontWeight fontWeight = TextFontWeight.Regular;

        // Used by automated tests
        internal NativeTextGenerationSettings(NativeTextGenerationSettings tgs)
        {
            text = tgs.text;
            fontSize = tgs.fontSize;
            screenWidth = tgs.screenWidth;
            screenHeight = tgs.screenHeight;
            wrapText = tgs.wrapText;
            horizontalAlignment = tgs.horizontalAlignment;
            verticalAlignment = tgs.verticalAlignment;
            color = tgs.color;
            fontAsset = tgs.fontAsset;
            globalFontAssetFallbacks = tgs.globalFontAssetFallbacks;
            fontStyle = tgs.fontStyle;
            fontWeight = tgs.fontWeight;
            languageDirection = tgs.languageDirection;
        }

        public bool Equals(NativeTextGenerationSettings other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return text == other.text /*&& Equals(fontAsset, other.fontAsset)*/;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((NativeTextGenerationSettings)obj);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(text);
            hashCode.Add(horizontalAlignment);
            hashCode.Add(verticalAlignment);
            hashCode.Add(color);
            hashCode.Add(fontAsset);
            hashCode.Add(fontStyle);
            hashCode.Add(fontWeight);
            hashCode.Add(screenWidth);
            hashCode.Add(screenHeight);
            hashCode.Add(fontSize);
            hashCode.Add(wrapText);
            hashCode.Add(languageDirection);
            return hashCode.ToHashCode();
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
               $"{nameof(wrapText)}: {wrapText}\n" +
               $"{nameof(languageDirection)}: {languageDirection}\n";
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

}
