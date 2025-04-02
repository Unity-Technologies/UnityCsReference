// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace UnityEditor
{
    internal class EditorFontAssetFactory : FontAssetFactory
    {
        // Define platform-specific system font name
        public static readonly string SystemFontName =
            "Liberation Sans"
            ;

        public class FontFamilyConfig
        {
            public string familyName { get; }
            public string regularStyle { get; }
            public string boldStyle { get; }
            public string italicStyle { get; }
            public string boldItalicStyle { get; }

            public FontFamilyConfig(string familyName, string regularStyle, string boldStyle, string italicStyle, string boldItalicStyle)
            {
                this.familyName = familyName;
                this.regularStyle = regularStyle;
                this.boldStyle = boldStyle;
                this.italicStyle = italicStyle;
                this.boldItalicStyle = boldItalicStyle;
            }

            public static readonly FontFamilyConfig k_SystemFontConfig = new FontFamilyConfig(
                SystemFontName,
                "Regular",
                "Bold",
                "Italic",
                "Bold Italic"
            );

            public static readonly FontFamilyConfig k_InterFontConfig = new FontFamilyConfig(
                "Inter",
                "Regular",
                "Semi Bold",
                "Italic",
                "Semi Bold Italic"
            );
        }

        internal static FontAsset CreateFontFamilyAssets(FontFamilyConfig config, Shader shader)
        {
            // Create main (regular) font asset
            FontAsset regularFontAsset = CreateFontAsset(config.familyName, config.regularStyle, 90, shader);
            if (regularFontAsset == null) return null;

            // Create bold font asset
            var boldFontAsset = CreateFontAsset(config.familyName, config.boldStyle, 90, shader);
            if (boldFontAsset != null)
            {
                regularFontAsset.fontWeightTable[7].regularTypeface = boldFontAsset;
            }

            // Create italic font asset
            var italicFontAsset = CreateFontAsset(config.familyName, config.italicStyle, 90, shader);
            if (italicFontAsset != null)
            {
                regularFontAsset.fontWeightTable[4].italicTypeface = italicFontAsset;
            }

            // Create bold italic font asset
            var boldItalicFontAsset = CreateFontAsset(config.familyName, config.boldItalicStyle, 90, shader);
            if (boldItalicFontAsset != null)
            {
                regularFontAsset.fontWeightTable[7].italicTypeface = boldItalicFontAsset;
            }

            return regularFontAsset;
        }

        internal static FontAsset CreateFontAsset(string fontFamily, string fontStyle, int fontSize, Shader shader)
        {
            FontAsset fontAsset = FontAsset.CreateFontAssetInternal(fontFamily, fontStyle, fontSize);
            if (fontAsset != null)
            {
                fontAsset.InternalDynamicOS = true;
                SetupFontAssetSettings(fontAsset, shader);
            }

            return fontAsset;
        }

         internal static void RegisterFontWithPaths(
            List<TextSettings.FontReferenceMap> fontReferences,
            Dictionary<int, FontAsset> fontLookup,
            FontAsset fontAsset,
            Shader shader,
            string[] paths)
        {
            if (fontAsset == null) return;

            foreach (string path in paths)
            {
                var font = EditorGUIUtility.Load(path) as Font;
                if (font == null) continue;

                int id = font.GetHashCode() + shader.GetHashCode();

                if (fontLookup.ContainsKey(id))
                    continue;

                fontReferences.Add(new TextSettings.FontReferenceMap(font, fontAsset));
                fontLookup.Add(id, fontAsset);
            }
        }
    }
}

