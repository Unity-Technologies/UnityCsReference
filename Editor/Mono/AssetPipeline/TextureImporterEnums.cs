// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;

namespace UnityEditor
{
    // Lightmap format of a [[Texture2D|texture]].
    internal enum TextureUsageMode
    {
        // Not a lightmap.
        Default = 0,
        // Range [0;2] packed to [0;1] with loss of precision.
        BakedLightmapDoubleLDR = 1,
        // Range [0;kLightmapRGBMMax] packed to [0;1] with multiplier stored in the alpha channel.
        BakedLightmapRGBM = 2,
        // Compressed DXT5 normal map
        NormalmapDXT5nm = 3,
        // Plain RGB normal map
        NormalmapPlain = 4,
        RGBMEncoded = 5,
        // Texture is always padded if NPOT and on low-end hardware
        AlwaysPadded = 6,
        DoubleLDR = 7,
        // Baked lightmap without any encoding
        BakedLightmapFullHDR = 8,
        RealtimeLightmapRGBM = 9,
    }

    // Imported texture format for [[TextureImporter]].
    public enum TextureImporterFormat
    {
        Automatic = -1,
        // Choose a compressed format automatically.
        [System.Obsolete("Use textureCompression property instead")]
        AutomaticCompressed = -1,
        // Choose a 16 bit format automatically.
        [System.Obsolete("Use textureCompression property instead")]
        Automatic16bit = -2,
        // Choose a Truecolor format automatically.
        [System.Obsolete("Use textureCompression property instead")]
        AutomaticTruecolor = -3,
        // Choose a Crunched format automatically.
        [System.Obsolete("Use crunchedCompression property instead")]
        AutomaticCrunched = -5,
        // Choose an HDR format automatically.
        [System.Obsolete("HDR is handled automatically now")]
        AutomaticHDR = -6,
        // Choose a compresssed HDR format automatically.
        [System.Obsolete("HDR is handled automatically now")]
        AutomaticCompressedHDR = -7,

        // DXT1 compressed texture format.
        DXT1 = 10,
        // DXT5 compressed texture format.
        DXT5 = 12,
        // RGB 16 bit texture format.
        RGB16 = 7,
        // RGB 24 bit texture format.
        RGB24 = 3,
        // Alpha 8 bit texture format.
        // RGBA 32 bit texture format.
        Alpha8 = 1,
        // Red 16 bit texture format.
        R16 = 9,
        // Red 8 bit texture format.
        R8 = 63,
        // RG 16 bit texture format.
        RG16 = 62,
        // RGBA 16 bit texture format.
        ARGB16 = 2,
        // RGBA 32 bit texture format.
        RGBA32 = 4,
        // ARGB 32 bit texture format.
        ARGB32 = 5,
        // RGBA 16 bit (4444) texture format.
        RGBA16 = 13,

        // R 16 bit texture format.
        RHalf = 15,
        // RG 32 bit texture format.
        RGHalf = 16,
        // RGBA 64 bit texture format.
        RGBAHalf = 17,

        // R 32 bit texture format.
        RFloat = 18,
        // RG 64 bit texture format.
        RGFloat = 19,
        // RGBA 128 bit texture format.
        RGBAFloat = 20,

        // RGB 32 bit packed float format.
        RGB9E5 = 22,

        // R BC4 compressed texture format.
        BC4 = 26,
        // RG BC5 compressed texture format.
        BC5 = 27,
        // HDR RGB BC6 compressed texture format.
        BC6H = 24,
        // RGBA BC7 compressed texture format.
        BC7 = 25,

        // DXT1 crunched texture format.
        DXT1Crunched = 28,
        // DXT5 crunched texture format.
        DXT5Crunched = 29,

        // PowerVR (iPhone) 2 bits/pixel compressed color texture format.
        PVRTC_RGB2 = 30,
        // PowerVR (iPhone) 2 bits/pixel compressed with alpha channel texture format.
        PVRTC_RGBA2 = 31,
        // PowerVR (iPhone) 4 bits/pixel compressed color texture format.
        PVRTC_RGB4 = 32,
        // PowerVR (iPhone) 4 bits/pixel compressed with alpha channel texture format.
        PVRTC_RGBA4 = 33,
        // ETC (GLES2.0) 4 bits/pixel compressed RGB texture format.
        ETC_RGB4 = 34,

        // ATC (Android) 4 bits/pixel compressed RGB texture format.
        [System.Obsolete("Use ETC_RGB4 (UnityUpgradable) -> ETC_RGB4")]
        ATC_RGB4 = 35,
        [System.Obsolete("Use ETC2_RGBA8 (UnityUpgradable) -> ETC2_RGBA8")]
        ATC_RGBA8 = 36,

        // EAC 4 bits/pixel compressed 16-bit R texture format
        EAC_R = 41,
        // EAC 4 bits/pixel compressed 16-bit signed R texture format
        EAC_R_SIGNED = 42,
        // EAC 8 bits/pixel compressed 16-bit RG texture format
        EAC_RG = 43,
        // EAC 8 bits/pixel compressed 16-bit signed RG texture format
        EAC_RG_SIGNED = 44,

        // ETC2 (GLES3.0) 4 bits/pixel compressed RGB texture format.
        ETC2_RGB4 = 45,
        // ETC2 (GLES3.0) 4 bits/pixel compressed RGB + 1-bit alpha texture format.
        ETC2_RGB4_PUNCHTHROUGH_ALPHA = 46,
        // ETC2 (GLES3.0) 8 bits/pixel compressed RGBA texture format.
        ETC2_RGBA8 = 47,

        // ASTC uses 128bit block of varying sizes (we use only square blocks). It does not distinguish RGB/RGBA
        ASTC_4x4 = 48,
        ASTC_5x5 = 49,
        ASTC_6x6 = 50,
        ASTC_8x8 = 51,
        ASTC_10x10 = 52,
        ASTC_12x12 = 53,

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Obsolete("Use ASTC_4x4 (UnityUpgradable) -> ASTC_4x4")]
        ASTC_RGB_4x4 = 48,
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Obsolete("Use ASTC_5x5 (UnityUpgradable) -> ASTC_5x5")]
        ASTC_RGB_5x5 = 49,
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Obsolete("Use ASTC_6x6 (UnityUpgradable) -> ASTC_6x6")]
        ASTC_RGB_6x6 = 50,
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Obsolete("Use ASTC_8x8 (UnityUpgradable) -> ASTC_8x8")]
        ASTC_RGB_8x8 = 51,
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Obsolete("Use ASTC_10x10 (UnityUpgradable) -> ASTC_10x10")]
        ASTC_RGB_10x10 = 52,
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Obsolete("Use ASTC_12x12 (UnityUpgradable) -> ASTC_12x12")]
        ASTC_RGB_12x12 = 53,
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Obsolete("Use ASTC_4x4 (UnityUpgradable) -> ASTC_4x4")]
        ASTC_RGBA_4x4 = 54,
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Obsolete("Use ASTC_5x5 (UnityUpgradable) -> ASTC_5x5")]
        ASTC_RGBA_5x5 = 55,
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Obsolete("Use ASTC_6x6 (UnityUpgradable) -> ASTC_6x6")]
        ASTC_RGBA_6x6 = 56,
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Obsolete("Use ASTC_8x8 (UnityUpgradable) -> ASTC_8x8")]
        ASTC_RGBA_8x8 = 57,
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Obsolete("Use ASTC_10x10 (UnityUpgradable) -> ASTC_10x10")]
        ASTC_RGBA_10x10 = 58,
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Obsolete("Use ASTC_12x12 (UnityUpgradable) -> ASTC_12x12")]
        ASTC_RGBA_12x12 = 59,

        // Nintendo 3DS-flavoured ETC
        [System.Obsolete("Nintendo 3DS is no longer supported.")]
        ETC_RGB4_3DS = 60,
        [System.Obsolete("Nintendo 3DS is no longer supported.")]
        ETC_RGBA8_3DS = 61,

        // ETC1 crunched texture format.
        ETC_RGB4Crunched = 64,
        // ETC2_RGBA8 crunched texture format.
        ETC2_RGBA8Crunched = 65,

        // ASTC (block size 4x4) compressed HDR RGB(A) texture format.
        ASTC_HDR_4x4 = 66,
        // ASTC (block size 5x5) compressed HDR RGB(A)  texture format.
        ASTC_HDR_5x5 = 67,
        // ASTC (block size 4x6x6) compressed HDR RGB(A) texture format.
        ASTC_HDR_6x6 = 68,
        // ASTC (block size 8x8) compressed HDR RGB(A) texture format.
        ASTC_HDR_8x8 = 69,
        // ASTC (block size 10x10) compressed HDR RGB(A) texture format.
        ASTC_HDR_10x10 = 70,
        // ASTC (block size 12x12) compressed HDR RGB(A) texture format.
        ASTC_HDR_12x12 = 71,

        RG32 = 72,
        RGB48 = 73,
        RGBA64 = 74,
    }

    // Mip map filter for [[TextureImporter]].
    public enum TextureImporterMipFilter
    {
        // Box mipmap filter.
        BoxFilter = 0,
        // Kaiser mipmap filter.
        KaiserFilter = 1,
    }

    // Cubemap generation mode for [[TextureImporter]].
    public enum TextureImporterGenerateCubemap
    {
        // Do not generate cubemap (default).
        [System.Obsolete("This value is deprecated (use TextureImporter.textureShape instead).")]
        None = 0,

        // Generate cubemap from spheremap texture.
        Spheremap = 1,

        // Generate cubemap from cylindrical texture.
        Cylindrical = 2,
        [System.Obsolete("Obscure shperemap modes are not supported any longer (use TextureImporterGenerateCubemap.Spheremap instead).")]
        SimpleSpheremap = 3,
        [System.Obsolete("Obscure shperemap modes are not supported any longer (use TextureImporterGenerateCubemap.Spheremap instead).")]
        NiceSpheremap = 4,

        // Generate cubemap from vertical or horizontal cross texture.
        FullCubemap = 5,

        // Automatically determine type of cubemap generation from the source image.
        AutoCubemap = 6
    }

    // Scaling mode for non power of two textures in [[TextureImporter]].
    public enum TextureImporterNPOTScale
    {
        // Keep non power of two textures as is.
        None = 0,
        // Scale to nearest power of two.
        ToNearest = 1,
        // Scale to larger power of two.
        ToLarger = 2,
        // Scale to smaller power of two.
        ToSmaller = 3,
    }

    // Normal map filtering mode for [[TextureImporter]].
    public enum TextureImporterNormalFilter
    {
        // Standard normal map filter.
        Standard = 0,
        // Sobel normal map filter.
        Sobel = 1,
    }

    // Texture Alpha Usage [[TextureImporter]].
    public enum TextureImporterAlphaSource
    {
        // Alpha won't be used.
        None = 0,
        // Alpha comes from input texture if one is provided.
        FromInput = 1,
        // Alpha is generated from image gray scale
        FromGrayScale = 2,
    }

    // Single Channel Texture Component [[TextureImporter]].
    public enum TextureImporterSingleChannelComponent
    {
        // Use the Alpha channel.
        Alpha = 0,
        // Use the Red color channel.
        Red = 1,
    }

    [RequiredByNativeCode]
    public enum TextureImporterType
    {
        Default = 0,
        [System.Obsolete("Use Default (UnityUpgradable) -> Default")]
        Image = 0,
        NormalMap = 1,
        [System.Obsolete("Use NormalMap (UnityUpgradable) -> NormalMap")]
        Bump = 1,
        GUI = 2,
        Sprite = 8,
        Cursor = 7,
        [System.Obsolete("Use importer.textureShape = TextureImporterShape.TextureCube")]
        Cubemap = 3,
        [System.Obsolete("Use a texture setup as a cubemap with glossy reflection instead")]
        Reflection = 3,
        Cookie = 4,
        Lightmap = 6,
        [System.Obsolete("HDRI is not supported anymore")]
        HDRI = 9,
        [System.Obsolete("Use Default instead. All texture types now have an Advanced foldout (UnityUpgradable) -> Default")]
        Advanced = 5,
        SingleChannel = 10
    }

    public enum TextureImporterCompression
    {
        Uncompressed = 0,
        Compressed = 1,
        // High quality compression formats
        CompressedHQ = 2,
        // Low quality compression formats but high Performance - low bandwidth - max compression
        CompressedLQ = 3
    }

    public enum TextureResizeAlgorithm
    {
        // Default high quality one size fits ALMOST all cases
        Mitchell = 0,
        // Might provide better result for some noise textures, when sharp details wanted
        Bilinear = 1
    }

    [Flags]
    public enum TextureImporterShape
    {
        Texture2D = 1 << 0,
        TextureCube = 1 << 1,
        Texture2DArray = 1 << 2,
        Texture3D = 1 << 3,
    }

    public enum SpriteImportMode
    {
        None = 0,
        Single = 1,
        Multiple = 2,
        Polygon = 3
    }

    public enum AndroidETC2FallbackOverride
    {
        // Use build settings
        UseBuildSettings = 0,
        // 32-bit uncompressed
        Quality32Bit = 1,
        // 16-bit uncompressed
        Quality16Bit = 2,
        // 32-bit uncompressed, downscaled 2x
        Quality32BitDownscaled = 3
    }
}
