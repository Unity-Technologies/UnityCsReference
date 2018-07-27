// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.U2D.Interface;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.U2D.Common
{
    internal class TexturePlatformSettingsViewController : ITexturePlatformSettingsController
    {
        public bool HandleDefaultSettings(List<TextureImporterPlatformSettings> platformSettings, ITexturePlatformSettingsView view, ITexturePlatformSettingsFormatHelper formatHelper)
        {
            Assert.IsTrue(platformSettings.Count > 0, "At least 1 platform setting is needed to display the texture platform setting UI.");

            var allSize = platformSettings[0].maxTextureSize;
            var allFormat = platformSettings[0].format;
            var allCompression = platformSettings[0].textureCompression;
            var allUseCrunchedCompression = platformSettings[0].crunchedCompression;
            var allCompressionQuality = platformSettings[0].compressionQuality;

            var mixedSize = false;
            var mixedFormat = false;
            var mixedCompression = false;
            var mixedUseCrunchedCompression = false;
            var mixedCompressionQuality = false;

            var sizeChanged = false;
            var formatChanged = false;
            var compressionChanged = false;
            var useCrunchedCompressionChanged = false;
            var compressionQualityChanged = false;

            for (var i = 1; i < platformSettings.Count; ++i)
            {
                var settings = platformSettings[i];
                if (settings.maxTextureSize != allSize)
                    mixedSize = true;
                if (settings.format != allFormat)
                    mixedFormat = true;
                if (settings.textureCompression != allCompression)
                    mixedCompression = true;
                if (settings.crunchedCompression != allUseCrunchedCompression)
                    mixedUseCrunchedCompression = true;
                if (settings.compressionQuality != allCompressionQuality)
                    mixedCompressionQuality = true;
            }

            allSize = view.DrawMaxSize(allSize, mixedSize, false, out sizeChanged);

            int[] formatValues = null;
            string[] formatStrings = null;
            formatHelper.AcquireDefaultTextureFormatValuesAndStrings(out formatValues, out formatStrings);

            allFormat = view.DrawFormat(allFormat, formatValues, formatStrings, mixedFormat, false, out formatChanged);

            if (allFormat == TextureImporterFormat.Automatic && (!mixedFormat || formatChanged))
            {
                allCompression = view.DrawCompression(allCompression, mixedCompression, false, out compressionChanged);

                if (allCompression != TextureImporterCompression.Uncompressed && (!mixedCompression || compressionChanged))
                {
                    allUseCrunchedCompression = view.DrawUseCrunchedCompression(allUseCrunchedCompression,
                        mixedUseCrunchedCompression, false, out useCrunchedCompressionChanged);

                    if (allUseCrunchedCompression && (!mixedUseCrunchedCompression || useCrunchedCompressionChanged))
                    {
                        allCompressionQuality = view.DrawCompressionQualitySlider(allCompressionQuality,
                            mixedCompressionQuality, false, out compressionQualityChanged);
                    }
                }
            }

            if (sizeChanged || compressionChanged || formatChanged || useCrunchedCompressionChanged || compressionQualityChanged)
            {
                for (var i = 0; i < platformSettings.Count; ++i)
                {
                    if (sizeChanged)
                        platformSettings[i].maxTextureSize = allSize;
                    if (formatChanged)
                        platformSettings[i].format = allFormat;
                    if (compressionChanged)
                        platformSettings[i].textureCompression = allCompression;
                    if (useCrunchedCompressionChanged)
                        platformSettings[i].crunchedCompression = allUseCrunchedCompression;
                    if (compressionQualityChanged)
                        platformSettings[i].compressionQuality = allCompressionQuality;
                }
                return true;
            }
            else
                return false;
        }

        public bool HandlePlatformSettings(BuildTarget buildTarget, List<TextureImporterPlatformSettings> platformSettings, ITexturePlatformSettingsView view, ITexturePlatformSettingsFormatHelper formatHelper)
        {
            Assert.IsTrue(platformSettings.Count > 0, "At least 1 platform setting is needed to display the texture platform setting UI.");

            var allOverride = platformSettings[0].overridden;
            var allSize = platformSettings[0].maxTextureSize;
            var allFormat = platformSettings[0].format;
            var allCompressionQuality = platformSettings[0].compressionQuality;
            var allAlphaSplit = platformSettings[0].allowsAlphaSplitting;

            var mixedOverride = false;
            var mixedSize = false;
            var mixedFormat = false;
            var mixedCompression = false;
            var mixedAlphaSplit = false;

            var overrideChanged = false;
            var sizeChanged = false;
            var formatChanged = false;
            var compressionChanged = false;
            var alphaSplitChanged = false;

            for (var i = 1; i < platformSettings.Count; ++i)
            {
                var settings = platformSettings[i];
                if (settings.overridden != allOverride)
                    mixedOverride = true;
                if (settings.maxTextureSize != allSize)
                    mixedSize = true;
                if (settings.format != allFormat)
                    mixedFormat = true;
                if (settings.compressionQuality != allCompressionQuality)
                    mixedCompression = true;
                if (settings.allowsAlphaSplitting != allAlphaSplit)
                    mixedAlphaSplit = true;
            }

            allOverride = view.DrawOverride(allOverride, mixedOverride, out overrideChanged);

            allSize = view.DrawMaxSize(allSize, mixedSize, mixedOverride || !allOverride, out sizeChanged);

            int[] formatValues = null;
            string[] formatStrings = null;

            formatHelper.AcquireTextureFormatValuesAndStrings(buildTarget, out formatValues, out formatStrings);

            allFormat = view.DrawFormat(allFormat, formatValues, formatStrings, mixedFormat, mixedOverride || !allOverride, out formatChanged);

            if (!mixedFormat && formatHelper.TextureFormatRequireCompressionQualityInput(allFormat))
            {
                bool showAsEnum =
                    buildTarget == BuildTarget.iOS ||
                    buildTarget == BuildTarget.tvOS ||
                    buildTarget == BuildTarget.Android
                ;

                if (showAsEnum)
                {
                    var compressionMode = 1;
                    if (allCompressionQuality == (int)TextureCompressionQuality.Fast)
                        compressionMode = 0;
                    else if (allCompressionQuality == (int)TextureCompressionQuality.Best)
                        compressionMode = 2;

                    compressionMode = view.DrawCompressionQualityPopup(compressionMode, mixedCompression, mixedOverride || !allOverride, out compressionChanged);

                    if (compressionChanged)
                    {
                        switch (compressionMode)
                        {
                            case 0: allCompressionQuality = (int)TextureCompressionQuality.Fast; break;
                            case 1: allCompressionQuality = (int)TextureCompressionQuality.Normal; break;
                            case 2: allCompressionQuality = (int)TextureCompressionQuality.Best; break;

                            default:
                                Assert.IsTrue(false, "ITexturePlatformSettingsView.DrawCompressionQualityPopup should never return compression option value that's not 0, 1 or 2.");
                                break;
                        }
                    }
                }
                else
                {
                    allCompressionQuality = view.DrawCompressionQualitySlider(allCompressionQuality, mixedCompression, mixedOverride || !allOverride, out compressionChanged);
                }

                // show the ETC1 split option only for sprites on platforms supporting ETC.
                bool isETCPlatform = TextureImporter.IsETC1SupportedByBuildTarget(buildTarget);
                bool isETCFormatSelected = TextureImporter.IsTextureFormatETC1Compression((TextureFormat)allFormat);
                if (isETCPlatform && isETCFormatSelected)
                {
                    allAlphaSplit = view.DrawAlphaSplit(allAlphaSplit, mixedAlphaSplit, mixedOverride || !allOverride, out alphaSplitChanged);
                }
            }

            if (overrideChanged || sizeChanged || formatChanged || compressionChanged || alphaSplitChanged)
            {
                for (var i = 0; i < platformSettings.Count; ++i)
                {
                    if (overrideChanged)
                        platformSettings[i].overridden = allOverride;
                    if (sizeChanged)
                        platformSettings[i].maxTextureSize = allSize;
                    if (formatChanged)
                        platformSettings[i].format = allFormat;
                    if (compressionChanged)
                        platformSettings[i].compressionQuality = allCompressionQuality;
                    if (alphaSplitChanged)
                        platformSettings[i].allowsAlphaSplitting = allAlphaSplit;
                }

                return true;
            }
            else
                return false;
        }
    }
}
