// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.AnimatedValues;
using UnityEditor.Modules;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using TargetAttributes = UnityEditor.BuildTargetDiscovery.TargetAttributes;

namespace UnityEditor.Modules
{
    //everything happening here in this default extension used to happen in TextureImporterInspector.
    //now, platforms that want to have their own texture import settings can subclass this class,
    //and put the platform-specific stuff (either new, or down below) into the new subclass.
    internal class DefaultTextureImportSettingsExtension : ITextureImportSettingsExtension
    {
        static readonly string[] kMaxTextureSizeStrings = { "32", "64", "128", "256", "512", "1024", "2048", "4096", "8192" };
        static readonly int[] kMaxTextureSizeValues = { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 };
        static readonly GUIContent maxSize = EditorGUIUtility.TrTextContent("Max Size", "Textures larger than this will be scaled down.");

        static readonly string[] kResizeAlgorithmStrings = { "Mitchell", "Bilinear" };
        static readonly int[] kResizeAlgorithmValues = { (int)TextureResizeAlgorithm.Mitchell, (int)TextureResizeAlgorithm.Bilinear };
        static readonly GUIContent resizeAlgorithm = EditorGUIUtility.TrTextContent("Resize Algorithm", "Select algorithm to apply for textures when scaled down.");

        static readonly GUIContent kTextureCompression = EditorGUIUtility.TrTextContent("Compression", "How will this texture be compressed?");
        static readonly GUIContent[] kTextureCompressionOptions =
        {
            EditorGUIUtility.TrTextContent("None", "Texture is not compressed."),
            EditorGUIUtility.TrTextContent("Low Quality", "Texture compressed with low quality but high performance, high compression format."),
            EditorGUIUtility.TrTextContent("Normal Quality", "Texture is compressed with a standard format."),
            EditorGUIUtility.TrTextContent("High Quality", "Texture compressed with a high quality format."),
        };
        static readonly int[] kTextureCompressionValues =
        {
            (int)TextureImporterCompression.Uncompressed,
            (int)TextureImporterCompression.CompressedLQ,
            (int)TextureImporterCompression.Compressed,
            (int)TextureImporterCompression.CompressedHQ
        };

        public virtual void ShowImportSettings(Editor baseEditor, TextureImportPlatformSettings platformSettings)
        {
            TextureImporterInspector editor = baseEditor as TextureImporterInspector;

            // Max texture size
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = platformSettings.overriddenIsDifferent || platformSettings.maxTextureSizeIsDifferent;
            int maxTextureSize = EditorGUILayout.IntPopup(maxSize.text, platformSettings.maxTextureSize, kMaxTextureSizeStrings, kMaxTextureSizeValues);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                platformSettings.SetMaxTextureSizeForAll(maxTextureSize);
            }

            // Resize Algorithm
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = platformSettings.overriddenIsDifferent || platformSettings.resizeAlgorithmIsDifferent;
            int resizeAlgorithmVal = EditorGUILayout.IntPopup(resizeAlgorithm.text, (int)platformSettings.resizeAlgorithm, kResizeAlgorithmStrings, kResizeAlgorithmValues);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                platformSettings.SetResizeAlgorithmForAll((TextureResizeAlgorithm)resizeAlgorithmVal);
            }

            // Texture format
            int[] formatValuesForAll = null;
            string[] formatStringsForAll = null;
            bool formatOptionsAreDifferent = false;

            int formatForAll = 0;

            // TODO : This should not be calculated every refresh and be kept in a cache somewhere instead...
            for (int i = 0; i < editor.targets.Length; i++)
            {
                TextureImporter imp = editor.targets[i] as TextureImporter;
                TextureImporterSettings settings = platformSettings.GetSettings(imp);
                TextureImporterType textureTypeForThis = editor.textureTypeHasMultipleDifferentValues ? settings.textureType : editor.textureType;
                int format = (int)platformSettings.format;

                int[] formatValues = null;
                string[] formatStrings = null;

                if (!platformSettings.isDefault && !platformSettings.overridden)
                {
                    // If not overriden, show what the auto format is going to be
                    // don't care about alpha in normal maps. If editor.assetTarget is null
                    // then we are dealing with texture preset and we show all options.
                    var showSettingsForPreset = editor.assetTarget == null;
                    var sourceHasAlpha = showSettingsForPreset || (imp.DoesSourceTextureHaveAlpha() &&
                        textureTypeForThis != TextureImporterType.NormalMap);

                    format = (int)TextureImporter.DefaultFormatFromTextureParameters(settings,
                        platformSettings.platformTextureSettings,
                        editor.assetTarget && sourceHasAlpha,
                        editor.assetTarget && imp.IsSourceTextureHDR(),
                        platformSettings.m_Target);

                    formatValues = new int[] { format };
                    formatStrings = new string[] { TextureUtil.GetTextureFormatString((TextureFormat)format) };
                }
                else
                {
                    // otherwise show valid formats
                    platformSettings.GetValidTextureFormatsAndStrings(textureTypeForThis, out formatValues, out formatStrings);
                }

                // Check if values are the same
                if (i == 0)
                {
                    formatValuesForAll = formatValues;
                    formatStringsForAll = formatStrings;
                    formatForAll = format;
                }
                else
                {
                    if (!formatValues.SequenceEqual(formatValuesForAll) || !formatStrings.SequenceEqual(formatStringsForAll))
                    {
                        formatOptionsAreDifferent = true;
                        break;
                    }
                }
            }

            using (new EditorGUI.DisabledScope(formatOptionsAreDifferent || formatStringsForAll.Length == 1))
            {
                EditorGUI.BeginChangeCheck();
                bool mixedValues = formatOptionsAreDifferent || platformSettings.textureFormatIsDifferent;
                EditorGUI.showMixedValue = mixedValues;
                var selectionResult = EditorGUILayout.IntPopup(TextureImporterInspector.s_Styles.textureFormat, formatForAll, EditorGUIUtility.TempContent(formatStringsForAll), formatValuesForAll);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    platformSettings.SetTextureFormatForAll((TextureImporterFormat)selectionResult);
                    formatForAll = selectionResult;
                }

                if (!mixedValues && !Array.Exists(formatValuesForAll, i => i == formatForAll))
                {
                    EditorGUILayout.HelpBox(string.Format(L10n.Tr("The selected format value {0} is not compatible on this platform for the selected texture type, please change it to a valid one from the dropdown."), (TextureImporterFormat)formatForAll), MessageType.Error);
                }
            }

            // Texture Compression
            if (platformSettings.isDefault && platformSettings.format == TextureImporterFormat.Automatic)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = platformSettings.overriddenIsDifferent ||
                    platformSettings.textureCompressionIsDifferent ||
                    platformSettings.format != TextureImporterFormat.Automatic;
                TextureImporterCompression textureCompression =
                    (TextureImporterCompression)EditorGUILayout.IntPopup(kTextureCompression,
                        (int)platformSettings.textureCompression, kTextureCompressionOptions,
                        kTextureCompressionValues);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    platformSettings.SetTextureCompressionForAll(textureCompression);
                }
            }

            // Use Crunch Compression
            if (platformSettings.isDefault &&
                (TextureImporterFormat)formatForAll == TextureImporterFormat.Automatic &&
                platformSettings.textureCompression != TextureImporterCompression.Uncompressed)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = platformSettings.overriddenIsDifferent ||
                    platformSettings.crunchedCompressionIsDifferent;
                bool crunchedCompression = EditorGUILayout.Toggle(
                    TextureImporterInspector.s_Styles.crunchedCompression, platformSettings.crunchedCompression);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    platformSettings.SetCrunchedCompressionForAll(crunchedCompression);
                }
            }

            // compression quality
            bool isCrunchedFormat = false
                || TextureUtil.IsCompressedCrunchTextureFormat((TextureFormat)formatForAll)
            ;

            if (
                (platformSettings.isDefault &&
                 (TextureImporterFormat)formatForAll == TextureImporterFormat.Automatic &&
                 platformSettings.textureCompression != TextureImporterCompression.Uncompressed &&
                 platformSettings.crunchedCompression) ||
                (platformSettings.isDefault && platformSettings.crunchedCompression && isCrunchedFormat) ||
                (!platformSettings.isDefault && isCrunchedFormat) ||
                (!platformSettings.textureFormatIsDifferent && ArrayUtility.Contains<TextureImporterFormat>(
                    TextureImporterInspector.kFormatsWithCompressionSettings,
                    (TextureImporterFormat)formatForAll)))
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = platformSettings.overriddenIsDifferent ||
                    platformSettings.compressionQualityIsDifferent;

                // Prior to exposing compression quality for BC6H/BC7 formats they were always compressed at maximum quality even though the setting was
                // defaulted to 'Normal'.  Now BC6H/BC7 quality is exposed to the user as Fast/Normal/Best 'Normal' maps to one setting down from maximum in the
                // ISPC compressor but to maintain the behaviour of existing projects we need to force their quality up to 'Best'.  The 'forceMaximumCompressionQuality_BC6H_BC7'
                // flag is set when loading existing texture platform settings to do this and cleared when the compression quality level is manually set (by UI or API)
                bool forceBestQuality = platformSettings.forceMaximumCompressionQuality_BC6H_BC7 && (((TextureImporterFormat)formatForAll == TextureImporterFormat.BC6H) || ((TextureImporterFormat)formatForAll == TextureImporterFormat.BC7));
                int compressionQuality = forceBestQuality ? (int)TextureCompressionQuality.Best : platformSettings.compressionQuality;

                compressionQuality = EditCompressionQuality(platformSettings.m_Target, compressionQuality, isCrunchedFormat, (TextureImporterFormat)formatForAll);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    platformSettings.SetCompressionQualityForAll(compressionQuality);
                    //SyncPlatformSettings ();
                }
            }

            // show the ETC1 split option only for sprites on platforms supporting ETC and only when there is an alpha channel
            bool isETCPlatform = TextureImporter.IsETC1SupportedByBuildTarget(BuildPipeline.GetBuildTargetByName(platformSettings.name));
            bool isDealingWithSprite = (editor.spriteImportMode != SpriteImportMode.None);
            bool isETCFormatSelected = TextureImporter.IsTextureFormatETC1Compression((TextureFormat)formatForAll);

            if (isETCPlatform && isDealingWithSprite && isETCFormatSelected)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = platformSettings.overriddenIsDifferent || platformSettings.allowsAlphaSplitIsDifferent;
                bool allowsAlphaSplit = EditorGUILayout.Toggle(TextureImporterInspector.s_Styles.useAlphaSplitLabel, platformSettings.allowsAlphaSplitting);
                if (EditorGUI.EndChangeCheck())
                {
                    platformSettings.SetAllowsAlphaSplitForAll(allowsAlphaSplit);
                }
            }
        }

        private int EditCompressionQuality(BuildTarget target, int compression, bool isCrunchedFormat, TextureImporterFormat textureFormat)
        {
            bool showAsEnum = !isCrunchedFormat && (BuildTargetDiscovery.PlatformHasFlag(target, TargetAttributes.HasIntegratedGPU) || (textureFormat == TextureImporterFormat.BC6H) || (textureFormat == TextureImporterFormat.BC7));

            if (showAsEnum)
            {
                int compressionMode = 1;
                if (compression == (int)TextureCompressionQuality.Fast)
                    compressionMode = 0;
                else if (compression == (int)TextureCompressionQuality.Best)
                    compressionMode = 2;

                int ret = EditorGUILayout.Popup(TextureImporterInspector.s_Styles.compressionQuality, compressionMode, TextureImporterInspector.s_Styles.mobileCompressionQualityOptions);

                switch (ret)
                {
                    case 0: return (int)TextureCompressionQuality.Fast;
                    case 1: return (int)TextureCompressionQuality.Normal;
                    case 2: return (int)TextureCompressionQuality.Best;

                    default: return (int)TextureCompressionQuality.Normal;
                }
            }
            else
                compression = EditorGUILayout.IntSlider(TextureImporterInspector.s_Styles.compressionQualitySlider, compression, 0, 100);

            return compression;
        }
    }
}
