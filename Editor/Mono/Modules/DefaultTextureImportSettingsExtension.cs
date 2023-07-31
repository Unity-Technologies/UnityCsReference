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
using UnityEngine.Experimental.Rendering;
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
        static readonly string kMaxSizeOverrideString = L10n.Tr("Max texture size is overriden to {0} in Build Settings window.");

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
            Rect controlRect = EditorGUILayout.GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.popup);
            GUIContent label = maxSize;
            if (editor.model.maxTextureSizeProperty != null)
            {
                label = EditorGUI.BeginProperty(controlRect, label, editor.model.maxTextureSizeProperty);
            }
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = editor.model.maxTextureSizeIsDifferent;
            int maxTextureSize = EditorGUI.IntPopup(controlRect, label, editor.model.platformTextureSettings.maxTextureSize, GUIContent.Temp(kMaxTextureSizeStrings), kMaxTextureSizeValues);
            if (EditorGUI.EndChangeCheck())
            {
                editor.model.SetMaxTextureSizeForAll(maxTextureSize);
            }
            if (editor.model.maxTextureSizeProperty != null)
            {
                EditorGUI.EndProperty();
            }

            // Show a note if max size is overriden globally by the user
            var userMaxSizeOverride = EditorUserBuildSettings.overrideMaxTextureSize;
            if (userMaxSizeOverride > 0 && userMaxSizeOverride < maxTextureSize)
                EditorGUILayout.HelpBox(string.Format(kMaxSizeOverrideString, userMaxSizeOverride), MessageType.Info);

            // Resize Algorithm
            controlRect = EditorGUILayout.GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.popup);
            label = kResizeAlgorithm;
            if (editor.model.resizeAlgorithmProperty != null)
            {
                label = EditorGUI.BeginProperty(controlRect, label, editor.model.resizeAlgorithmProperty);
            }
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = editor.model.resizeAlgorithmIsDifferent;
            int resizeAlgorithmVal = EditorGUI.IntPopup(controlRect, label, (int)editor.model.platformTextureSettings.resizeAlgorithm, GUIContent.Temp(kResizeAlgorithmStrings), kResizeAlgorithmValues);
            if (EditorGUI.EndChangeCheck())
            {
                editor.model.SetResizeAlgorithmForAll((TextureResizeAlgorithm)resizeAlgorithmVal);
            }
            if (editor.model.resizeAlgorithmProperty != null)
            {
                EditorGUI.EndProperty();
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
                    formatStrings = new string[] { GraphicsFormatUtility.GetFormatString((TextureFormat)format) };
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
                controlRect = EditorGUILayout.GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.popup);
                label = kTextureFormat;
                if (editor.model.textureFormatProperty != null)
                {
                    label = EditorGUI.BeginProperty(controlRect, label, editor.model.textureFormatProperty);
                }
                EditorGUI.BeginChangeCheck();
                bool mixedValues = formatOptionsAreDifferent || editor.model.textureFormatIsDifferent;
                EditorGUI.showMixedValue = mixedValues;
                var selectionResult = EditorGUI.IntPopup(controlRect, label, formatForAll, GUIContent.Temp(formatStringsForAll), formatValuesForAll);
                if (EditorGUI.EndChangeCheck())
                {
                    editor.model.SetTextureFormatForAll((TextureImporterFormat)selectionResult);
                    formatForAll = selectionResult;
                }
                if (editor.model.textureFormatProperty != null)
                {
                    EditorGUI.EndProperty();
                }

                if (!mixedValues && !Array.Exists(formatValuesForAll, i => i == formatForAll))
                {
                    EditorGUILayout.HelpBox(string.Format(L10n.Tr("The selected format value {0} is not compatible on this platform for the selected texture type, please change it to a valid one from the dropdown."), (TextureImporterFormat)formatForAll), MessageType.Error);
                }
            }

            // Texture Compression
            if (editor.model.isDefault && editor.model.platformTextureSettings.format == TextureImporterFormat.Automatic)
            {
                controlRect = EditorGUILayout.GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.popup);
                label = kTextureCompression;
                if (editor.model.textureCompressionProperty != null)
                {
                    label = EditorGUI.BeginProperty(controlRect, label, editor.model.textureCompressionProperty);
                }
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = editor.model.overriddenIsDifferent ||
                    editor.model.textureCompressionIsDifferent;
                TextureImporterCompression textureCompression =
                    (TextureImporterCompression)EditorGUI.IntPopup(controlRect,
                        label, (int)editor.model.platformTextureSettings.textureCompression, kTextureCompressionOptions,
                        kTextureCompressionValues);
                if (EditorGUI.EndChangeCheck())
                {
                    editor.model.SetTextureCompressionForAll(textureCompression);
                }
                if (editor.model.textureCompressionProperty != null)
                {
                    EditorGUI.EndProperty();
                }
            }

            // Use Crunch Compression
            if (editor.model.isDefault &&
                (TextureImporterFormat)formatForAll == TextureImporterFormat.Automatic &&
                editor.model.platformTextureSettings.textureCompression != TextureImporterCompression.Uncompressed &&
                (textureShape == TextureImporterShape.Texture2D || textureShape == TextureImporterShape.TextureCube)) // 2DArray & 3D don't support Crunch
            {
                controlRect = EditorGUILayout.GetToggleRect(true);
                label = kCrunchedCompression;
                if (editor.model.crunchedCompressionProperty != null)
                {
                    label = EditorGUI.BeginProperty(controlRect, label, editor.model.crunchedCompressionProperty);
                }
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = editor.model.overriddenIsDifferent ||
                    editor.model.crunchedCompressionIsDifferent;
                bool crunchedCompression = EditorGUI.Toggle(
                    controlRect, label, editor.model.platformTextureSettings.crunchedCompression);
                if (EditorGUI.EndChangeCheck())
                {
                    editor.model.SetCrunchedCompressionForAll(crunchedCompression);
                }
                if (editor.model.crunchedCompressionProperty != null)
                {
                    EditorGUI.EndProperty();
                }
            }

            // compression quality
            bool isCrunchedFormat = false
                || GraphicsFormatUtility.IsCrunchFormat((TextureFormat)formatForAll)
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
                EditCompressionQuality(editor, isCrunchedFormat, (TextureImporterFormat)formatForAll);
            }

            // show the ETC1 split option only for sprites on platforms supporting ETC and only when there is an alpha channel
            bool isETCPlatform = TextureImporter.IsETC1SupportedByBuildTarget(BuildPipeline.GetBuildTargetByName(editor.model.platformTextureSettings.name));
            bool isDealingWithSprite = (editor.spriteImportMode != SpriteImportMode.None);
            bool isETCFormatSelected = TextureImporter.IsTextureFormatETC1Compression((TextureFormat)formatForAll);

            if (isETCPlatform && isDealingWithSprite && isETCFormatSelected)
            {
                controlRect = EditorGUILayout.GetToggleRect(true);
                label = kUseAlphaSplitLabel;
                if (editor.model.alphaSplitProperty != null)
                {
                    label = EditorGUI.BeginProperty(controlRect, label, editor.model.alphaSplitProperty);
                }
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = editor.model.overriddenIsDifferent || editor.model.allowsAlphaSplitIsDifferent;
                bool allowsAlphaSplit = EditorGUI.Toggle(controlRect, label, editor.model.platformTextureSettings.allowsAlphaSplitting);
                if (EditorGUI.EndChangeCheck())
                {
                    editor.model.SetAllowsAlphaSplitForAll(allowsAlphaSplit);
                }
                if (editor.model.alphaSplitProperty != null)
                {
                    EditorGUI.EndProperty();
                }
            }
        }

        private void EditCompressionQuality(BaseTextureImportPlatformSettings editor, bool isCrunchedFormat, TextureImporterFormat textureFormat)
        {
            bool showAsEnum = !isCrunchedFormat && (BuildTargetDiscovery.PlatformHasFlag(editor.model.buildTarget, TargetAttributes.HasIntegratedGPU) || (textureFormat == TextureImporterFormat.BC6H) || (textureFormat == TextureImporterFormat.BC7));

            Rect controlRect = showAsEnum ? EditorGUILayout.GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.popup) : EditorGUILayout.GetSliderRect(true);
            GUIContent label = showAsEnum ? kCompressionQuality : kCompressionQualitySlider;
            if (editor.model.compressionQualityProperty != null)
            {
                label = EditorGUI.BeginProperty(controlRect, label, editor.model.compressionQualityProperty);
            }

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = editor.model.overriddenIsDifferent ||
                editor.model.compressionQualityIsDifferent;

            // Prior to exposing compression quality for BC6H/BC7 formats they were always compressed at maximum quality even though the setting was
            // defaulted to 'Normal'.  Now BC6H/BC7 quality is exposed to the user as Fast/Normal/Best 'Normal' maps to one setting down from maximum in the
            // ISPC compressor but to maintain the behaviour of existing projects we need to force their quality up to 'Best'.  The 'forceMaximumCompressionQuality_BC6H_BC7'
            // flag is set when loading existing texture platform settings to do this and cleared when the compression quality level is manually set (by UI or API)
            bool forceBestQuality = editor.model.forceMaximumCompressionQuality_BC6H_BC7 && ((textureFormat == TextureImporterFormat.BC6H) || (textureFormat == TextureImporterFormat.BC7));
            int compression = forceBestQuality ? (int)TextureCompressionQuality.Best : editor.model.platformTextureSettings.compressionQuality;

            if (showAsEnum)
            {
                int compressionMode = 1;
                if (compression == (int)TextureCompressionQuality.Fast)
                    compressionMode = 0;
                else if (compression == (int)TextureCompressionQuality.Best)
                    compressionMode = 2;

                int ret = EditorGUI.Popup(controlRect, label, compressionMode, kMobileCompressionQualityOptions);

                switch (ret)
                {
                    case 0:
                        compression = (int)TextureCompressionQuality.Fast;
                        break;
                    case 1:
                        compression = (int)TextureCompressionQuality.Normal;
                        break;
                    case 2:
                        compression = (int)TextureCompressionQuality.Best;
                        break;

                    default:
                        compression = (int)TextureCompressionQuality.Normal;
                        break;
                }
            }
            else
                compression = EditorGUI.IntSlider(controlRect, label, compression, 0, 100);

            if (EditorGUI.EndChangeCheck())
            {
                editor.model.SetCompressionQualityForAll(compression);
                //SyncPlatformSettings ();
            }
            if (editor.model.compressionQualityProperty != null)
            {
                EditorGUI.EndProperty();
            }
        }
    }
}
