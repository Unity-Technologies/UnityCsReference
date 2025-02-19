// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor.Build;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor
{
    // Texture importer lets you modify [[Texture2D]] import settings from editor scripts.
    [NativeHeader("Editor/Src/AssetPipeline/TextureImporting/TextureImporter.h")]
    [NativeHeader("Editor/Src/AssetPipeline/TextureImporting/TextureImporter.deprecated.h")]
    [NativeHeader("Editor/Src/AssetPipeline/TextureImporting/TextureImporterUtils.h")]
    [NativeHeader("Editor/Src/AssetPipeline/TextureImporting/TextureImporterPlatformSettingsUtils.h")]
    [NativeHeader("Editor/Src/EditorUserBuildSettings.h")]
    public sealed partial class TextureImporter : AssetImporter
    {
        [FreeFunction]
        internal static extern string GetTexturePlatformSerializationName(string platformName);

        [Obsolete("textureFormat is no longer accessible at the TextureImporter level. For old 'simple' formats use the textureCompression property for the equivalent automatic choice (Uncompressed for TrueColor, Compressed and HQCommpressed for 16 bits). For platform specific formats use the [[PlatformTextureSettings]] API. Using this setter will setup various parameters to match the new automatic system as well as possible. Getter will return the last value set.")]
        public extern TextureImporterFormat textureFormat
        {
            [FreeFunction("GetTextureFormat", HasExplicitThis = true)]
            get;
            [FreeFunction("SetTextureFormat", HasExplicitThis = true)]
            set;
        }

        [NativeProperty("TextureImporter::s_DefaultPlatformName", true, TargetType.Field)]
        internal static extern string defaultPlatformName
        {
            get;
        }

        public extern int maxTextureSize { get; set; }
        [NativeProperty("TextureCompressionQuality", false, TargetType.Function)]
        public extern int compressionQuality { get; set; }
        public extern bool crunchedCompression { get; set; }
        public extern bool allowAlphaSplitting { get; set; }

        public extern AndroidETC2FallbackOverride androidETC2FallbackOverride { get; set; }
        public extern TextureImporterCompression textureCompression { get; set; }
        public extern TextureImporterAlphaSource alphaSource { get; set; }

        internal extern bool forceMaximumCompressionQuality_BC6H_BC7 { get; set; }

        // Generate alpha channel from intensity?
        [Obsolete("Use UnityEditor.TextureImporter.alphaSource instead.")]
        public bool grayscaleToAlpha
        {
            get { return alphaSource == TextureImporterAlphaSource.FromGrayScale; }
            set { if (value) alphaSource = TextureImporterAlphaSource.FromGrayScale; else alphaSource = TextureImporterAlphaSource.FromInput; }
        }

        // TODO: make this use struct for possible future expansion

        // Whether the texture allows alpha splitting for compressions like ETC1
        [Obsolete("Use UnityEditor.TextureImporter.GetPlatformTextureSettings() instead.")]
        [NativeMethod(HasExplicitThis = true)]
        public extern bool GetAllowsAlphaSplitting();

        [Obsolete("Use UnityEditor.TextureImporter.SetPlatformTextureSettings() instead.")]
        [NativeMethod(HasExplicitThis = true)]
        public extern void SetAllowsAlphaSplitting(bool flag);

        public bool GetPlatformTextureSettings(string platform, out int maxTextureSize, out TextureImporterFormat textureFormat, out int compressionQuality, out bool etc1AlphaSplitEnabled)
        {
            TextureImporterPlatformSettings settings = GetPlatformTextureSettings(platform);
            maxTextureSize = settings.maxTextureSize;
            textureFormat = settings.format;
            compressionQuality = settings.compressionQuality;
            etc1AlphaSplitEnabled = settings.allowsAlphaSplitting;

            return settings.overridden;
        }

        public bool GetPlatformTextureSettings(string platform, out int maxTextureSize, out TextureImporterFormat textureFormat, out int compressionQuality)
        {
            bool etc1AlphaSplitEnabled = false;
            return GetPlatformTextureSettings(platform, out maxTextureSize, out textureFormat, out compressionQuality, out etc1AlphaSplitEnabled);
        }

        public bool GetPlatformTextureSettings(string platform, out int maxTextureSize, out TextureImporterFormat textureFormat)
        {
            int compressionQuality = 0;
            bool etc1AlphaSplitEnabled = false;
            return GetPlatformTextureSettings(platform, out maxTextureSize, out textureFormat, out compressionQuality, out etc1AlphaSplitEnabled);
        }

        // C++ implementation will return default platform if the requested platform is not valid.
        // See "Editor/Mono/BuildPipeline/BuildPlatform.cs" -> "GetValidPlatformNames" for more
        // information regarding what is considered to be a valid platform.
        [NativeName("GetPlatformTextureSettings")]
        private extern TextureImporterPlatformSettings GetPlatformTextureSetting_Internal(string platform);

        // Read texture settings for specified platform into [[TextureImporterPlatformSettings]] class.
        // public API will always return a valid TextureImporterPlatformSettings, creating it based on the default one if it did not exist.
        public TextureImporterPlatformSettings GetPlatformTextureSettings(string platform)
        {
            platform = GetTexturePlatformSerializationName(platform); // String may refer to a platform group: if != "Standalone", ensure it refers to a platform instead. E.g.: "iOS", not "iPhone".

            TextureImporterPlatformSettings dest = GetPlatformTextureSetting_Internal(platform);
            if (platform != dest.name)
            {
                dest.name = platform;
                dest.overridden = false;
            }
            return dest;
        }

        public TextureImporterPlatformSettings GetDefaultPlatformTextureSettings()
        {
            return GetPlatformTextureSettings(TextureImporterInspector.s_DefaultPlatformName);
        }

        public TextureImporterFormat GetAutomaticFormat(string platform)
        {
            platform = GetTexturePlatformSerializationName(platform); // String may refer to a platform group: if != "Standalone", ensure it refers to a platform instead. E.g.: "iOS", not "iPhone".
            TextureImporterSettings settings = new TextureImporterSettings();
            ReadTextureSettings(settings);
            TextureImporterPlatformSettings platformSettings = GetPlatformTextureSettings(platform);

            BuildTarget buildTarget = BuildPipeline.GetBuildTargetByName(platform);
            if (buildTarget != BuildTarget.NoTarget)
            {
                return DefaultFormatFromTextureParameters(settings,
                    !platformSettings.overridden ? GetDefaultPlatformTextureSettings() : platformSettings,
                    DoesSourceTextureHaveAlpha(),
                    IsSourceTextureHDR(),
                    buildTarget);

                // Regarding the "GetDefaultPlatformTextureSettings" call: in case 1281084, we made it so that platform settings stop automatically
                // resetting to the default platform's settings when the platform override is disabled. This introduced a regression where
                // "GetAutomaticFormat" would not return the actual format used by platforms with a disabled override, (as in, the one indicated in
                // the default platform's settings) which is why we pass in the default platform's settings instead.
            }

            return TextureImporterFormat.Automatic;
        }

        [Obsolete("Use UnityEditor.TextureImporter.SetPlatformTextureSettings(TextureImporterPlatformSettings) instead.")]
        public void SetPlatformTextureSettings(string platform, int maxTextureSize, TextureImporterFormat textureFormat, int compressionQuality, bool allowsAlphaSplit)
        {
            TextureImporterPlatformSettings dest = GetPlatformTextureSettings(platform);
            dest.overridden = true;
            dest.maxTextureSize = maxTextureSize;
            dest.format = textureFormat;
            dest.compressionQuality = compressionQuality;
            dest.allowsAlphaSplitting = allowsAlphaSplit;
            SetPlatformTextureSettings(dest);
        }

        [Obsolete("Use UnityEditor.TextureImporter.SetPlatformTextureSettings(TextureImporterPlatformSettings) instead.")]
        public void SetPlatformTextureSettings(string platform, int maxTextureSize, TextureImporterFormat textureFormat)
        {
            SetPlatformTextureSettings(platform, maxTextureSize, textureFormat, false);
        }

        [Obsolete("Use UnityEditor.TextureImporter.SetPlatformTextureSettings(TextureImporterPlatformSettings) instead.")]
        public void SetPlatformTextureSettings(string platform, int maxTextureSize, TextureImporterFormat textureFormat, [DefaultValue(false)] bool allowsAlphaSplit)
        {
            TextureImporterPlatformSettings dest = GetPlatformTextureSettings(platform);
            dest.overridden = true;
            dest.maxTextureSize = maxTextureSize;
            dest.format = textureFormat;
            dest.allowsAlphaSplitting = allowsAlphaSplit;
            SetPlatformTextureSettings(dest);
        }

        [NativeName("SetPlatformTextureSettings")]
        private extern void SetPlatformTextureSettings_Internal(TextureImporterPlatformSettings platformSettings);

        // Set specific target platform settings
        public void SetPlatformTextureSettings(TextureImporterPlatformSettings platformSettings)
        {
            platformSettings.name = GetTexturePlatformSerializationName(platformSettings.name); // String may refer to a platform group: if != "Standalone", ensure it refers to a platform instead. E.g.: "iOS", not "iPhone".
            SetPlatformTextureSettings_Internal(platformSettings);
        }

        // Clear specific target platform settings
        [NativeName("ClearPlatformTextureSettings")]
        private extern void ClearPlatformTextureSettings_Internal(string platform);

        public void ClearPlatformTextureSettings(string platform)
        {
            platform = GetTexturePlatformSerializationName(platform); // String may refer to a platform group: if != "Standalone", ensure it refers to a platform instead. E.g.: "iOS", not "iPhone".
            ClearPlatformTextureSettings_Internal(platform);
        }

        [FreeFunction]
        internal static extern  TextureImporterFormat DefaultFormatFromTextureParameters([NotNull] TextureImporterSettings settings, TextureImporterPlatformSettings platformSettings, bool doesTextureContainAlpha, bool sourceWasHDR, BuildTarget destinationPlatform);

        [FreeFunction]
        internal static extern TextureImporterFormat[] RecommendedFormatsFromTextureTypeAndPlatform(TextureImporterType textureType, BuildTarget destinationPlatform);

        [RequiredByNativeCode]
        public static bool IsPlatformTextureFormatValid(TextureImporterType textureType, BuildTarget target, TextureImporterFormat currentFormat)
        {
            if (currentFormat != TextureImporterFormat.Automatic)
            {
                int[] formatValues;
                string[] formatStrings;
                TextureImportValidFormats.GetPlatformTextureFormatValuesAndStrings(textureType, target, out formatValues, out formatStrings);
                return Array.Exists(formatValues, i => i == (int)currentFormat);
            }

            return true;
        }

        [RequiredByNativeCode]
        public static bool IsDefaultPlatformTextureFormatValid(TextureImporterType textureType, TextureImporterFormat currentFormat)
        {
            if (currentFormat != TextureImporterFormat.Automatic)
            {
                int[] formatValues;
                string[] formatStrings;
                TextureImportValidFormats.GetDefaultTextureFormatValuesAndStrings(textureType, out formatValues, out formatStrings);
                return Array.Exists(formatValues, i => i == (int)currentFormat);
            }

            return true;
        }

        // Cubemap generation mode.
        public extern TextureImporterGenerateCubemap generateCubemap { get; set; }
        // Scaling mode for non power of two textures.
        [NativeProperty("NPOTScale")]
        public extern TextureImporterNPOTScale npotScale { get; set; }

        // Is texture data readable from scripts.
        public extern bool isReadable { get; set; }

        // Is texture data able to be streamed by mip level.
        [NativeConditional("ENABLE_TEXTURE_STREAMING")]
        public extern bool streamingMipmaps { get; set; }
        // This texture's mipmap streaming priority.

        [NativeConditional("ENABLE_TEXTURE_STREAMING")]
        public extern int streamingMipmapsPriority { get; set; }

        // Is texture VT only
        [NativeConditional("ENABLE_VIRTUALTEXTURING")]
        [NativeProperty("VTOnly")]
        public extern bool vtOnly { get; set; }

        public extern bool ignoreMipmapLimit { get; set; }
        public extern string mipmapLimitGroupName { get; set; }

        // Generate mip maps for the texture?
        public extern bool mipmapEnabled { get; set; }
        // Keep texture borders the same when generating mipmaps?
        public extern bool borderMipmap { get; set; }
        // When in linear rendering should this texture be sampled with hardware gamma correction (sRGB) or without (linear)?
        [NativeProperty("sRGBTexture")]
        public extern bool sRGBTexture { get; set; }

        // Should alpha MIP maps preserve coverage during the alpha test?
        public extern bool mipMapsPreserveCoverage { get; set; }
        // Alpha test reference value which determines the coverage.
        public extern float alphaTestReferenceValue { get; set; }

        // Mipmap filtering mode.
        [NativeProperty("MipmapMode")]
        public extern TextureImporterMipFilter mipmapFilter { get; set; }
        // Fade out mip levels to gray color?
        public extern bool fadeout { get; set; }
        // Mip level where texture begins to fade out.
        public extern int mipmapFadeDistanceStart { get; set; }
        // Mip level where texture is faded out completely.
        public extern int mipmapFadeDistanceEnd { get; set; }

        // Should mip maps be generated with gamma correction?
        [Obsolete("generateMipsInLinearSpace Property deprecated. Mipmaps are always generated in linear space.")]
        public bool generateMipsInLinearSpace
        {
            get { return true; }
            set {}
        }

        [Obsolete("correctGamma Property deprecated. Mipmaps are always generated in linear space.")]
        public bool correctGamma
        {
            get { return true; }
            set {}
        }

        [Obsolete("linearTexture Property deprecated. Use sRGBTexture instead.")]
        public bool linearTexture { get { return !sRGBTexture; } set { sRGBTexture = !value; } }

        [Obsolete("normalmap Property deprecated. Check [[TextureImporterSettings.textureType]] instead. Getter will work as expected. Setter will set textureType to NormalMap if true, nothing otherwise.")]
        public bool normalmap
        {
            get { return textureType == TextureImporterType.NormalMap; }
            set { if (value) textureType = TextureImporterType.NormalMap; else textureType = TextureImporterType.Default; }
        }

        [Obsolete("lightmap Property deprecated. Check [[TextureImporterSettings.textureType]] instead. Getter will work as expected. Setter will set textureType to Lightmap if true, nothing otherwise.")]
        public bool lightmap
        {
            get { return textureType == TextureImporterType.Lightmap; }
            set { if (value) textureType = TextureImporterType.Lightmap; else textureType = TextureImporterType.Default; }
        }

        public extern bool convertToNormalmap { get; set; }
        public extern TextureImporterNormalFilter normalmapFilter { get; set; }
        public extern bool flipGreenChannel { get; set; }

        extern uint swizzle { get; set; }
        public TextureImporterSwizzle swizzleR
        {
            get => (TextureImporterSwizzle)(swizzle & 0xFF);
            set => swizzle = (swizzle & 0xFFFFFF00) | (uint)value;
        }
        public TextureImporterSwizzle swizzleG
        {
            get => (TextureImporterSwizzle)((swizzle >> 8) & 0xFF);
            set => swizzle = (swizzle & 0xFFFF00FF) | ((uint)value<<8);
        }
        public TextureImporterSwizzle swizzleB
        {
            get => (TextureImporterSwizzle)((swizzle >> 16) & 0xFF);
            set => swizzle = (swizzle & 0xFF00FFFF) | ((uint)value<<16);
        }
        public TextureImporterSwizzle swizzleA
        {
            get => (TextureImporterSwizzle)((swizzle >> 24) & 0xFF);
            set => swizzle = (swizzle & 0x00FFFFFF) | ((uint)value<<24);
        }

        [NativeProperty("NormalmapHeightScale")]
        public extern float heightmapScale { get; set; }

        // Anisotropic filtering level of the texture.
        public extern int anisoLevel { get; set; }

        // Filtering mode of the texture.
        public extern FilterMode filterMode { get; set; }

        // note: wrapMode getter returns U wrapping axis
        public extern TextureWrapMode wrapMode
        {
            [NativeName("GetWrapU")]
            get;
            [NativeName("SetWrapUVW")]
            set;
        }
        [NativeProperty("WrapU")]
        public extern TextureWrapMode wrapModeU { get; set; }
        [NativeProperty("WrapV")]
        public extern TextureWrapMode wrapModeV { get; set; }
        [NativeProperty("WrapW")]
        public extern TextureWrapMode wrapModeW { get; set; }

        // Mip map bias of the texture.
        public extern float mipMapBias { get; set; }

        // Use alpha channel as transparency. Removes white borders from transparent textures
        public extern bool alphaIsTransparency { get; set; }

        public extern bool qualifiesForSpritePacking { get; }

        [NativeProperty("SpriteMode")]
        public extern  SpriteImportMode spriteImportMode { get; set; }

        [NativeProperty("SpriteMetaDatas")]
        [Obsolete("Support for accessing sprite meta data through spritesheet has been removed. Please use the UnityEditor.U2D.Sprites.ISpriteEditorDataProvider interface instead.")]
        public extern SpriteMetaData[] spritesheet { get; set; }

        public extern SecondarySpriteTexture[] secondarySpriteTextures { get; set; }

        [Obsolete("Support for packing sprites through spritePackingTag has been removed. Please use SpriteAtlas instead.")]
        public string spritePackingTag { get { return ""; } set { } }

        // The number of pixels in one unit. Note: The C++ side still uses the name pixelsToUnits which is misleading,
        // but has not been changed yet to minimize merge conflicts.
        [NativeProperty("SpritePixelsToUnits")]
        public extern float spritePixelsPerUnit { get; set; }

        [System.Obsolete("Use spritePixelsPerUnit property instead.")]
        public extern float spritePixelsToUnits { get; set; }

        public extern Vector2 spritePivot { get; set; }
        public extern Vector4 spriteBorder { get; set; }

        internal void GetWidthAndHeight(ref int width, ref int height)
        {
            var info = GetSourceTextureInformation();
            width = info.width;
            height = info.height;
        }

        public void GetSourceTextureWidthAndHeight(out int width, out int height)
        {
            var info = GetSourceTextureInformation();
            if (info.width == -1)
                throw new InvalidOperationException("The texture has not yet finished importing. This most likely means this method was called in an AssetPostprocessor.OnPreprocessAsset callback.");

            width = info.width;
            height = info.height;
        }

        internal bool IsSourceTextureHDR()
        {
            return GetSourceTextureInformation().hdr;
        }

        private extern SourceTextureInformation GetSourceTextureInformation();

        [FreeFunction("IsCompressedETCTextureFormat")]
        internal static extern  bool IsTextureFormatETC1Compression(TextureFormat fmt);

        [FreeFunction("IsBuildTargetETC")]
        internal static extern  bool IsETC1SupportedByBuildTarget(BuildTarget target);

        // Does textures source image have alpha channel.
        public bool DoesSourceTextureHaveAlpha()
        {
            var info = GetSourceTextureInformation();
            if (info.width == -1)
                throw new ArgumentException("May only be called in OnPostProcessTexture");

            return info.containsAlpha;
        }

        // Does textures source image have RGB channels.
        [System.Obsolete("DoesSourceTextureHaveColor always returns true in Unity.")]
        public bool DoesSourceTextureHaveColor()
        {
            return true;
        }

        // Which type of texture are we dealing with here
        public extern TextureImporterType textureType { get; set; }
        public extern TextureImporterShape textureShape { get; set; }

        // Read texture settings into [[TextureImporterSettings]] class.
        public void ReadTextureSettings(TextureImporterSettings dest)
        {
            settings.CopyTo(dest);
        }

        // Set texture importers settings from [[TextureImporterSettings]] class.
        public void SetTextureSettings(TextureImporterSettings src)
        {
            ValidateAndCorrectTextureImporterSettings(src);
            settings = src;
        }

        private void ValidateAndCorrectTextureImporterSettings(TextureImporterSettings m_Settings)
        {
            switch (m_Settings.textureType)
            {
                case TextureImporterType.Sprite:
                    m_Settings.npotScale = ValidateAndCorrectSetting(m_Settings.npotScale, TextureImporterNPOTScale.None, nameof(m_Settings.npotScale));
                    break;
            }
        }

        private T ValidateAndCorrectSetting<T>(T actual, T expected, string settingName)
        {
            if (!actual.Equals(expected))
            {
                Debug.LogWarning($"You cannot set {settingName} to {actual} for this texture type. It has been reset to {expected}.");
                return expected;
            }
            return actual;
        }

        private extern TextureImporterSettings settings { get; set; }

        [NativeName("GetImportInspectorWarning")]
        internal extern string GetImportWarnings();

        public extern void ReadTextureImportInstructions(BuildTarget target, out TextureFormat desiredFormat, out ColorSpace colorSpace, out int compressionQuality);

        internal extern bool textureStillNeedsToBeCompressed { [NativeName("DoesTextureStillNeedToBeCompressed")] get; }

        // This is pure backward compatibility codepath. It can be removed when we decide that the time has come
        internal extern bool removeMatte { get; set; }

        public extern bool ignorePngGamma { get; set; }

        // This is for remapping Sprite that are renamed.
        extern internal bool GetNameFromInternalIDMap(long id, ref string name);

        [NativeName("GetSpriteMetaDatas")]
        internal extern SpriteMetaData[] GetSpriteMetaDatas();
    }
}
