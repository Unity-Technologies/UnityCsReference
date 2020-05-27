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
        static readonly string[] kMaxTextureSizeStrings = { "32", "64", "128", "256", "512", "1024", "2048", "4096", "8192", "16384" };
        static readonly int[] kMaxTextureSizeValues = { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384 };
        static readonly GUIContent maxSize = EditorGUIUtility.TrTextContent("Max Size", "Textures larger than this will be scaled down.");

        static readonly string[] kResizeAlgorithmStrings = { "Mitchell", "Bilinear" };
        static readonly int[] kResizeAlgorithmValues = { (int)TextureResizeAlgorithm.Mitchell, (int)TextureResizeAlgorithm.Bilinear };
        static readonly GUIContent kResizeAlgorithm = EditorGUIUtility.TrTextContent("Resize Algorithm", "Select algorithm to apply for textures when scaled down.");
        static readonly GUIContent kTextureFormat = EditorGUIUtility.TrTextContent("Format");
        static readonly GUIContent kTextureCompression = EditorGUIUtility.TrTextContent("Compression", "How will this texture be compressed?");
        static readonly GUIContent kUseAlphaSplitLabel = EditorGUIUtility.TrTextContent("Split Alpha Channel", "Alpha for this texture will be preserved by splitting the alpha channel to another texture, and both resulting textures will be compressed using ETC1.");
        static readonly GUIContent kCrunchedCompression = EditorGUIUtility.TrTextContent("Use Crunch Compression", "Texture is crunch-compressed to save space on disk when applicable.");
        static readonly GUIContent kCompressionQuality = EditorGUIUtility.TrTextContent("Compressor Quality");
        static readonly GUIContent kCompressionQualitySlider = EditorGUIUtility.TrTextContent("Compressor Quality", "Use the slider to adjust compression quality from 0 (Fastest) to 100 (Best)");
        static readonly GUIContent[] kMobileCompressionQualityOptions =
        {
            EditorGUIUtility.TrTextContent("Fast"),
            EditorGUIUtility.TrTextContent("Normal"),
            EditorGUIUtility.TrTextContent("Best")
        };

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

        public virtual void ShowImportSettings(BaseTextureImportPlatformSettings editor)
        {
            // Max texture size
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = editor.model.overriddenIsDifferent || editor.model.maxTextureSizeIsDifferent;
            int maxTextureSize = EditorGUILayout.IntPopup(maxSize.text, editor.model.platformTextureSettings.maxTextureSize, kMaxTextureSizeStrings, kMaxTextureSizeValues);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                editor.model.SetMaxTextureSizeForAll(maxTextureSize);
            }

            // Resize Algorithm
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = editor.model.overriddenIsDifferent || editor.model.resizeAlgorithmIsDifferent;
            int resizeAlgorithmVal = EditorGUILayout.IntPopup(kResizeAlgorithm.text, (int)editor.model.platformTextureSettings.resizeAlgorithm, kResizeAlgorithmStrings, kResizeAlgorithmValues);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                editor.model.SetResizeAlgorithmForAll((TextureResizeAlgorithm)resizeAlgorithmVal);
            }

            // Texture format
            int[] formatValuesForAll = {};
            string[] formatStringsForAll = {};
            bool formatOptionsAreDifferent = false;

            int formatForAll = 0;
            var textureShape = TextureImporterShape.Texture2D;

            // TODO : This should not be calculated every refresh and be kept in a cache somewhere instead...
            for (int i = 0; i < editor.GetTargetCount(); i++)
            {
                TextureImporterSettings settings = editor.GetImporterSettings(i);
                TextureImporterType textureTypeForThis = editor.textureTypeHasMultipleDifferentValues ? settings.textureType : editor.textureType;
                int format = (int)editor.model.platformTextureSettings.format;

                int[] formatValues = null;
                string[] formatStrings = null;

                if (!editor.model.isDefault && !editor.model.platformTextureSettings.overridden)
                {
                    // If not overriden, show what the auto format is going to be
                    // don't care about alpha in normal maps. If editor.assetTarget is null
                    // then we are dealing with texture preset and we show all options.
                    var showSettingsForPreset = editor.ShowPresetSettings();
                    var sourceHasAlpha = showSettingsForPreset || (editor.DoesSourceTextureHaveAlpha(i) &&
                        textureTypeForThis != TextureImporterType.NormalMap);

                    format = (int)TextureImporter.DefaultFormatFromTextureParameters(settings,
                        editor.model.platformTextureSettings,
                        !showSettingsForPreset && sourceHasAlpha,
                        !showSettingsForPreset && editor.IsSourceTextureHDR(i),
                        editor.model.buildTarget);

                    formatValues = new int[] { format };
                    formatStrings = new string[] { TextureUtil.GetTextureFormatString((TextureFormat)format) };
                }
                else
                {
                    // otherwise show valid formats
                    editor.model.GetValidTextureFormatsAndStrings(textureTypeForThis, out formatValues, out formatStrings);
                }

                // Check if values are the same
                if (i == 0)
                {
                    formatValuesForAll = formatValues;
                    formatStringsForAll = formatStrings;
                    formatForAll = format;
                    textureShape = settings.textureShape;
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
                bool mixedValues = formatOptionsAreDifferent || editor.model.textureFormatIsDifferent;
                EditorGUI.showMixedValue = mixedValues;
                var selectionResult = EditorGUILayout.IntPopup(kTextureFormat, formatForAll, EditorGUIUtility.TempContent(formatStringsForAll), formatValuesForAll);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    editor.model.SetTextureFormatForAll((TextureImporterFormat)selectionResult);
                    formatForAll = selectionResult;
                }

                if (!mixedValues && !Array.Exists(formatValuesForAll, i => i == formatForAll))
                {
                    EditorGUILayout.HelpBox(string.Format(L10n.Tr("The selected format value {0} is not compatible on this platform for the selected texture type, please change it to a valid one from the dropdown."), (TextureImporterFormat)formatForAll), MessageType.Error);
                }
            }

            // Texture Compression
            if (editor.model.isDefault && editor.model.platformTextureSettings.format == TextureImporterFormat.Automatic)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = editor.model.overriddenIsDifferent ||
                    editor.model.textureCompressionIsDifferent ||
                    editor.model.platformTextureSettings.format != TextureImporterFormat.Automatic;
                TextureImporterCompression textureCompression =
                    (TextureImporterCompression)EditorGUILayout.IntPopup(kTextureCompression,
                        (int)editor.model.platformTextureSettings.textureCompression, kTextureCompressionOptions,
                        kTextureCompressionValues);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    editor.model.SetTextureCompressionForAll(textureCompression);
                }
            }

            // Use Crunch Compression
            if (editor.model.isDefault &&
                (TextureImporterFormat)formatForAll == TextureImporterFormat.Automatic &&
                editor.model.platformTextureSettings.textureCompression != TextureImporterCompression.Uncompressed &&
                (textureShape == TextureImporterShape.Texture2D || textureShape == TextureImporterShape.TextureCube)) // 2DArray & 3D don't support Crunch
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = editor.model.overriddenIsDifferent ||
                    editor.model.crunchedCompressionIsDifferent;
                bool crunchedCompression = EditorGUILayout.Toggle(
                    kCrunchedCompression, editor.model.platformTextureSettings.crunchedCompression);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    editor.model.SetCrunchedCompressionForAll(crunchedCompression);
                }
            }

            // compression quality
            bool isCrunchedFormat = false
                || TextureUtil.IsCompressedCrunchTextureFormat((TextureFormat)formatForAll)
            ;

            if (
                (editor.model.isDefault &&
                 (TextureImporterFormat)formatForAll == TextureImporterFormat.Automatic &&
                 editor.model.platformTextureSettings.textureCompression != TextureImporterCompression.Uncompressed &&
                 editor.model.platformTextureSettings.crunchedCompression) ||
                (editor.model.isDefault && editor.model.platformTextureSettings.crunchedCompression && isCrunchedFormat) ||
                (!editor.model.isDefault && isCrunchedFormat) ||
                (!editor.model.textureFormatIsDifferent && ArrayUtility.Contains<TextureImporterFormat>(
                    TextureImporterInspector.kFormatsWithCompressionSettings,
                    (TextureImporterFormat)formatForAll)))
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = editor.model.overriddenIsDifferent ||
                    editor.model.compressionQualityIsDifferent;

                // Prior to exposing compression quality for BC6H/BC7 formats they were always compressed at maximum quality even though the setting was
                // defaulted to 'Normal'.  Now BC6H/BC7 quality is exposed to the user as Fast/Normal/Best 'Normal' maps to one setting down from maximum in the
                // ISPC compressor but to maintain the behaviour of existing projects we need to force their quality up to 'Best'.  The 'forceMaximumCompressionQuality_BC6H_BC7'
                // flag is set when loading existing texture platform settings to do this and cleared when the compression quality level is manually set (by UI or API)
                bool forceBestQuality = editor.model.forceMaximumCompressionQuality_BC6H_BC7 && (((TextureImporterFormat)formatForAll == TextureImporterFormat.BC6H) || ((TextureImporterFormat)formatForAll == TextureImporterFormat.BC7));
                int compressionQuality = forceBestQuality ? (int)TextureCompressionQuality.Best : editor.model.platformTextureSettings.compressionQuality;

                compressionQuality = EditCompressionQuality(editor.model.buildTarget, compressionQuality, isCrunchedFormat, (TextureImporterFormat)formatForAll);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    editor.model.SetCompressionQualityForAll(compressionQuality);
                    //SyncPlatformSettings ();
                }
            }

            // show the ETC1 split option only for sprites on platforms supporting ETC and only when there is an alpha channel
            bool isETCPlatform = TextureImporter.IsETC1SupportedByBuildTarget(BuildPipeline.GetBuildTargetByName(editor.model.platformTextureSettings.name));
            bool isDealingWithSprite = (editor.spriteImportMode != SpriteImportMode.None);
            bool isETCFormatSelected = TextureImporter.IsTextureFormatETC1Compression((TextureFormat)formatForAll);

            if (isETCPlatform && isDealingWithSprite && isETCFormatSelected)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = editor.model.overriddenIsDifferent || editor.model.allowsAlphaSplitIsDifferent;
                bool allowsAlphaSplit = EditorGUILayout.Toggle(kUseAlphaSplitLabel, editor.model.platformTextureSettings.allowsAlphaSplitting);
                if (EditorGUI.EndChangeCheck())
                {
                    editor.model.SetAllowsAlphaSplitForAll(allowsAlphaSplit);
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

                int ret = EditorGUILayout.Popup(kCompressionQuality, compressionMode, kMobileCompressionQualityOptions);

                switch (ret)
                {
                    case 0: return (int)TextureCompressionQuality.Fast;
                    case 1: return (int)TextureCompressionQuality.Normal;
                    case 2: return (int)TextureCompressionQuality.Best;

                    default: return (int)TextureCompressionQuality.Normal;
                }
            }
            else
                compression = EditorGUILayout.IntSlider(kCompressionQualitySlider, compression, 0, 100);

            return compression;
        }
    }
}
