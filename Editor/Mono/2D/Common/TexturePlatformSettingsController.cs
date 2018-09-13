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
        public bool HandleDefaultSettings(List<TextureImporterPlatformSettings> platformSettings, ITexturePlatformSettingsView view)
        {
            Assert.IsTrue(platformSettings.Count > 0, "At least 1 platform setting is needed to display the texture platform setting UI.");

            int allSize = platformSettings[0].maxTextureSize;
            TextureImporterCompression allCompression = platformSettings[0].textureCompression;
            bool allUseCrunchedCompression = platformSettings[0].crunchedCompression;
            int allCompressionQuality = platformSettings[0].compressionQuality;

            var newSize = allSize;
            var newCompression = allCompression;
            var newUseCrunchedCompression = allUseCrunchedCompression;
            var newCompressionQuality = allCompressionQuality;

            bool mixedSize = false;
            bool mixedCompression = false;
            bool mixedUseCrunchedCompression = false;
            bool mixedCompressionQuality = false;

            bool sizeChanged = false;
            bool compressionChanged = false;
            bool useCrunchedCompressionChanged = false;
            bool compressionQualityChanged = false;

            for (var i = 1; i < platformSettings.Count; ++i)
            {
                var settings = platformSettings[i];
                if (settings.maxTextureSize != allSize)
                    mixedSize = true;
                if (settings.textureCompression != allCompression)
                    mixedCompression = true;
                if (settings.crunchedCompression != allUseCrunchedCompression)
                    mixedUseCrunchedCompression = true;
                if (settings.compressionQuality != allCompressionQuality)
                    mixedCompressionQuality = true;
            }

            newSize = view.DrawMaxSize(allSize, mixedSize, out sizeChanged);
            newCompression = view.DrawCompression(allCompression, mixedCompression, out compressionChanged);
            if (!mixedCompression && allCompression != TextureImporterCompression.Uncompressed)
            {
                newUseCrunchedCompression = view.DrawUseCrunchedCompression(allUseCrunchedCompression, mixedUseCrunchedCompression, out useCrunchedCompressionChanged);

                if (!mixedUseCrunchedCompression && allUseCrunchedCompression)
                {
                    newCompressionQuality = view.DrawCompressionQualitySlider(allCompressionQuality, mixedCompressionQuality, out compressionQualityChanged);
                }
            }

            if (sizeChanged || compressionChanged || useCrunchedCompressionChanged || compressionQualityChanged)
            {
                for (var i = 0; i < platformSettings.Count; ++i)
                {
                    if (sizeChanged)
                        platformSettings[i].maxTextureSize = newSize;
                    if (compressionChanged)
                        platformSettings[i].textureCompression = newCompression;
                    if (useCrunchedCompressionChanged)
                        platformSettings[i].crunchedCompression = newUseCrunchedCompression;
                    if (compressionQualityChanged)
                        platformSettings[i].compressionQuality = newCompressionQuality;
                }
                return true;
            }
            else
                return false;
        }

        public bool HandlePlatformSettings(BuildTarget buildTarget, List<TextureImporterPlatformSettings> platformSettings, ITexturePlatformSettingsView view, ITexturePlatformSettingsFormatHelper formatHelper)
        {
            Assert.IsTrue(platformSettings.Count > 0, "At least 1 platform setting is needed to display the texture platform setting UI.");

            bool allOverride = platformSettings[0].overridden;
            int allSize = platformSettings[0].maxTextureSize;
            TextureImporterFormat allFormat = platformSettings[0].format;
            int allCompressionQuality = platformSettings[0].compressionQuality;
            var allAlphaSplit = platformSettings[0].allowsAlphaSplitting;

            var newOverride = allOverride;
            var newSize = allSize;
            var newFormat = allFormat;
            var newCompressionQuality = allCompressionQuality;
            var newAlphaSplit = allAlphaSplit;

            bool mixedOverride = false;
            bool mixedSize = false;
            bool mixedFormat = false;
            bool mixedCompression = false;
            var mixedAlphaSplit = false;

            bool overrideChanged = false;
            bool sizeChanged = false;
            bool formatChanged = false;
            bool compressionChanged = false;
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

            newOverride = view.DrawOverride(allOverride, mixedOverride, out overrideChanged);

            if (!mixedOverride && allOverride)
            {
                newSize = view.DrawMaxSize(allSize, mixedSize, out sizeChanged);
            }

            int[] formatValues = null;
            string[] formatStrings = null;
            formatHelper.AcquireTextureFormatValuesAndStrings(buildTarget, out formatValues, out formatStrings);

            newFormat = view.DrawFormat(allFormat, formatValues, formatStrings, mixedFormat, mixedOverride || !allOverride, out formatChanged);

            if (!mixedFormat && !mixedOverride && allOverride && formatHelper.TextureFormatRequireCompressionQualityInput(allFormat))
            {
                bool showAsEnum =
                    buildTarget == BuildTarget.iOS ||
                    buildTarget == BuildTarget.tvOS ||
                    buildTarget == BuildTarget.Android ||
                    buildTarget == BuildTarget.Tizen
                ;

                if (showAsEnum)
                {
                    int compressionMode = 1;
                    if (allCompressionQuality == (int)TextureCompressionQuality.Fast)
                        compressionMode = 0;
                    else if (allCompressionQuality == (int)TextureCompressionQuality.Best)
                        compressionMode = 2;

                    var returnValue = view.DrawCompressionQualityPopup(compressionMode, mixedCompression, out compressionChanged);

                    if (compressionChanged)
                    {
                        switch (returnValue)
                        {
                            case 0: newCompressionQuality = (int)TextureCompressionQuality.Fast; break;
                            case 1: newCompressionQuality = (int)TextureCompressionQuality.Normal; break;
                            case 2: newCompressionQuality = (int)TextureCompressionQuality.Best; break;

                            default:
                                Assert.IsTrue(false, "ITexturePlatformSettingsView.DrawCompressionQualityPopup should never return compression option value that's not 0, 1 or 2.");
                                break;
                        }
                    }
                }
                else
                {
                    newCompressionQuality = view.DrawCompressionQualitySlider(allCompressionQuality, mixedCompression, out compressionChanged);
                }

                // show the ETC1 split option only for sprites on platforms supporting ETC.
                bool isETCPlatform = formatHelper.IsETC1SupportedByBuildTarget(buildTarget);
                bool isETCFormatSelected = formatHelper.IsTextureFormatETC1Compression((TextureFormat)allFormat);
                if (isETCPlatform && isETCFormatSelected)
                {
                    newAlphaSplit = view.DrawAlphaSplit(allAlphaSplit, mixedAlphaSplit, mixedOverride || !allOverride, out alphaSplitChanged);
                }
            }

            if (overrideChanged || sizeChanged || formatChanged || compressionChanged || alphaSplitChanged)
            {
                for (var i = 0; i < platformSettings.Count; ++i)
                {
                    if (overrideChanged)
                        platformSettings[i].overridden = newOverride;
                    if (sizeChanged)
                        platformSettings[i].maxTextureSize = newSize;
                    if (formatChanged)
                        platformSettings[i].format = newFormat;
                    if (compressionChanged)
                        platformSettings[i].compressionQuality = newCompressionQuality;
                    if (alphaSplitChanged)
                        platformSettings[i].allowsAlphaSplitting = newAlphaSplit;
                }

                return true;
            }
            else
                return false;
        }
    }
}
