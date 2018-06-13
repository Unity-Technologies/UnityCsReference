// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SpriteMetaData
    {
        public string name;
        public Rect rect;
        public int alignment;
        public Vector2 pivot;
        public Vector4 border;
    }

    // Note: MUST match memory layout of TextureImporterSettings in TextureImporter.h!
    // This means you need to be careful about field sizes (e.g. don't use "bool" since they are different size in C# and C++).
    [System.Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [NativeAsStruct]
    [NativeHeader("Editor/Src/AssetPipeline/TextureImporting/TextureImporter.bindings.h")]
    [NativeHeader("Editor/Src/AssetPipeline/TextureImporting/TextureImporterTypes.h")]
    public sealed partial class TextureImporterSettings
    {
        [SerializeField]
        int   m_AlphaSource;
        [SerializeField]
        int   m_MipMapMode;
        [SerializeField]
        int  m_EnableMipMap;
        [SerializeField]
        int  m_FadeOut;
        [SerializeField]
        int  m_BorderMipMap;
        [SerializeField]
        int m_MipMapsPreserveCoverage;
        [SerializeField]
        float m_AlphaTestReferenceValue;
        [SerializeField]
        int    m_MipMapFadeDistanceStart;
        [SerializeField]
        int    m_MipMapFadeDistanceEnd;

        #pragma warning disable 169

        [SerializeField]
        int  m_ConvertToNormalMap;
        [SerializeField]
        float  m_HeightScale;
        [SerializeField]
        int     m_NormalMapFilter;
        [SerializeField]
        int  m_IsReadable;

        [SerializeField]
        int  m_StreamingMipmaps;
        [SerializeField]
        int  m_StreamingMipmapsPriority;

        [SerializeField]
        int    m_NPOTScale;
        [SerializeField]
        int  m_sRGBTexture;

        [SerializeField]
        int    m_SpriteMode;
        [SerializeField]
        uint   m_SpriteExtrude;
        [SerializeField]
        int   m_SpriteMeshType;
        [SerializeField]
        int    m_Alignment;
        [SerializeField]
        Vector2    m_SpritePivot;
        [SerializeField]
        float  m_SpritePixelsToUnits;
        [SerializeField]
        Vector4    m_SpriteBorder;
        [SerializeField]
        int m_SpriteGenerateFallbackPhysicsShape;

        [SerializeField]
        int    m_GenerateCubemap;
        [SerializeField]
        int    m_CubemapConvolution;
        [SerializeField]
        int    m_SeamlessCubemap;

        [SerializeField]
        int m_AlphaIsTransparency;

        [SerializeField]
        float m_SpriteTessellationDetail;

        [SerializeField]
        int m_TextureType;
        [SerializeField]
        int m_TextureShape;

        [SerializeField]
        int m_SingleChannelComponent;

        // memory layout of these is in TextureSettings.h
        [SerializeField]
        [NativeName("m_TextureSettings.m_FilterMode")]
        int     m_FilterMode;
        [SerializeField]
        [NativeName("m_TextureSettings.m_Aniso")]
        int     m_Aniso;
        [SerializeField]
        [NativeName("m_TextureSettings.m_MipBias")]
        float   m_MipBias;
        [SerializeField]
        [NativeName("m_TextureSettings.m_WrapU")]
        int m_WrapU;
        [SerializeField]
        [NativeName("m_TextureSettings.m_WrapV")]
        int m_WrapV;
        [SerializeField]
        [NativeName("m_TextureSettings.m_WrapW")]
        int m_WrapW;

        // Deprecated since texture importer overhaul. Kept for backward compatibility purpose.
        [SerializeField]
        [NativeName("m_NormalMap_Deprecated")]
        int m_NormalMap;
        [SerializeField]
        [NativeName("m_TextureFormat_Deprecated")]
        int m_TextureFormat;
        [SerializeField]
        [NativeName("m_MaxTextureSize_Deprecated")]
        int m_MaxTextureSize;
        [SerializeField]
        [NativeName("m_Lightmap_Deprecated")]
        int m_Lightmap;
        [SerializeField]
        [NativeName("m_CompressionQuality_Deprecated")]
        int m_CompressionQuality;
        [SerializeField]
        [NativeName("m_LinearTexture_Deprecated")]
        int m_LinearTexture;
        [SerializeField]
        [NativeName("m_GrayScaleToAlpha_Deprecated")]
        int m_GrayScaleToAlpha;
        [SerializeField]
        [NativeName("m_RGBM_Deprecated")]
        int m_RGBM;
        [SerializeField]
        [NativeName("m_CubemapConvolutionSteps_Deprecated")]
        int m_CubemapConvolutionSteps;
        [SerializeField]
        [NativeName("m_CubemapConvolutionExponent_Deprecated")]
        float m_CubemapConvolutionExponent;

        // These are just part of a hack to support backward compatibility for maxTextureSize, textureFormat and compressionQualityProperties
        [SerializeField]
        private int m_MaxTextureSizeSet;
        [SerializeField]
        private int m_CompressionQualitySet;
        [SerializeField]
        private int m_TextureFormatSet;

        public TextureImporterType textureType
        {
            get {return (TextureImporterType)m_TextureType; }
            set { m_TextureType = (int)value; }
        }

        public TextureImporterShape textureShape
        {
            get {return (TextureImporterShape)m_TextureShape; }
            set { m_TextureShape = (int)value; }
        }

        public TextureImporterMipFilter mipmapFilter
        {
            get {return (TextureImporterMipFilter)m_MipMapMode; }
            set { m_MipMapMode = (int)value; }
        }
        public bool mipmapEnabled
        {
            get {return m_EnableMipMap != 0; }
            set { m_EnableMipMap = value ? 1 : 0; }
        }

        [Obsolete("Texture mips are now always generated in linear space")]
        public bool generateMipsInLinearSpace
        {
            get { return true; }
            set {}
        }
        public bool sRGBTexture
        {
            get {return m_sRGBTexture != 0; }
            set { m_sRGBTexture = value ? 1 : 0; }
        }
        public bool fadeOut
        {
            get {return m_FadeOut != 0; }
            set { m_FadeOut = value ? 1 : 0; }
        }
        public bool borderMipmap
        {
            get {return m_BorderMipMap != 0; }
            set { m_BorderMipMap = value ? 1 : 0; }
        }
        public bool mipMapsPreserveCoverage
        {
            get { return m_MipMapsPreserveCoverage != 0; }
            set { m_MipMapsPreserveCoverage = value ? 1 : 0; }
        }
        public float alphaTestReferenceValue
        {
            get { return m_AlphaTestReferenceValue; }
            set { m_AlphaTestReferenceValue = value; }
        }
        public int mipmapFadeDistanceStart
        {
            get {return m_MipMapFadeDistanceStart; }
            set { m_MipMapFadeDistanceStart = value; }
        }
        public int mipmapFadeDistanceEnd
        {
            get {return m_MipMapFadeDistanceEnd; }
            set { m_MipMapFadeDistanceEnd = value; }
        }
        public bool convertToNormalMap
        {
            get {return m_ConvertToNormalMap != 0; }
            set { m_ConvertToNormalMap = value ? 1 : 0; }
        }
        public float heightmapScale
        {
            get {return m_HeightScale; }
            set { m_HeightScale = value; }
        }
        public TextureImporterNormalFilter normalMapFilter
        {
            get {return (TextureImporterNormalFilter)m_NormalMapFilter; }
            set { m_NormalMapFilter = (int)value; }
        }
        public TextureImporterAlphaSource alphaSource
        {
            get {return (TextureImporterAlphaSource)m_AlphaSource; }
            set { m_AlphaSource = (int)value; }
        }
        public TextureImporterSingleChannelComponent singleChannelComponent
        {
            get {return (TextureImporterSingleChannelComponent)m_SingleChannelComponent; }
            set { m_SingleChannelComponent = (int)value; }
        }

        public bool readable
        {
            get {return m_IsReadable != 0; }
            set { m_IsReadable = value ? 1 : 0; }
        }

        public bool streamingMipmaps
        {
            get {return m_StreamingMipmaps != 0; }
            set { m_StreamingMipmaps = value ? 1 : 0; }
        }
        public int streamingMipmapsPriority
        {
            get {return m_StreamingMipmapsPriority; }
            set { m_StreamingMipmapsPriority = value; }
        }

        public TextureImporterNPOTScale npotScale
        {
            get {return (TextureImporterNPOTScale)m_NPOTScale; }
            set { m_NPOTScale = (int)value; }
        }
        public TextureImporterGenerateCubemap generateCubemap
        {
            get {return (TextureImporterGenerateCubemap)m_GenerateCubemap; }
            set { m_GenerateCubemap = (int)value; }
        }
        public TextureImporterCubemapConvolution cubemapConvolution
        {
            get {return (TextureImporterCubemapConvolution)m_CubemapConvolution; }
            set { m_CubemapConvolution = (int)value; }
        }
        public bool seamlessCubemap
        {
            get {return m_SeamlessCubemap != 0; }
            set { m_SeamlessCubemap = value ? 1 : 0; }
        }
        public FilterMode filterMode
        {
            get {return (FilterMode)m_FilterMode; }
            set { m_FilterMode = (int)value; }
        }
        public int aniso
        {
            get {return m_Aniso; }
            set { m_Aniso = value; }
        }
        public float mipmapBias
        {
            get {return m_MipBias; }
            set { m_MipBias = value; }
        }
        public TextureWrapMode wrapMode
        {
            get { return (TextureWrapMode)m_WrapU; }
            set { m_WrapU = (int)value; m_WrapV = (int)value; m_WrapW = (int)value; }
        }
        public TextureWrapMode wrapModeU
        {
            get { return (TextureWrapMode)m_WrapU; }
            set { m_WrapU = (int)value; }
        }
        public TextureWrapMode wrapModeV
        {
            get { return (TextureWrapMode)m_WrapV; }
            set { m_WrapV = (int)value; }
        }
        public TextureWrapMode wrapModeW
        {
            get { return (TextureWrapMode)m_WrapW; }
            set { m_WrapW = (int)value; }
        }
        public bool alphaIsTransparency
        {
            get {return m_AlphaIsTransparency != 0; }
            set { m_AlphaIsTransparency = value ? 1 : 0; }
        }

        public int spriteMode
        {
            get {return m_SpriteMode; }
            set { m_SpriteMode = value; }
        }

        // The number of pixels in one unit. Note: Internally, the name m_SpritePixelsToUnits has not been changed yet to minimize merge conflicts.
        public float spritePixelsPerUnit
        {
            get {return m_SpritePixelsToUnits; }
            set { m_SpritePixelsToUnits = value; }
        }
        [System.Obsolete("Use spritePixelsPerUnit property instead.")]
        public float spritePixelsToUnits
        {
            get {return m_SpritePixelsToUnits; }
            set { m_SpritePixelsToUnits = value; }
        }

        public float spriteTessellationDetail
        {
            get {return m_SpriteTessellationDetail; }
            set { m_SpriteTessellationDetail = value; }
        }
        public uint spriteExtrude
        {
            get { return m_SpriteExtrude; }
            set { m_SpriteExtrude = value; }
        }

        public SpriteMeshType spriteMeshType
        {
            get { return (SpriteMeshType)m_SpriteMeshType; }
            set { m_SpriteMeshType = (int)value; }
        }

        public int spriteAlignment
        {
            get {return m_Alignment; }
            set { m_Alignment = value; }
        }

        public Vector2 spritePivot
        {
            get { return m_SpritePivot; }
            set { m_SpritePivot = value; }
        }

        public Vector4 spriteBorder
        {
            get { return m_SpriteBorder; }
            set { m_SpriteBorder = value; }
        }

        public bool spriteGenerateFallbackPhysicsShape
        {
            get {return m_SpriteGenerateFallbackPhysicsShape != 0; }
            set { m_SpriteGenerateFallbackPhysicsShape = value ? 1 : 0; }
        }

        [FreeFunction("TextureImporterBindings::Equal")]
        public static extern bool Equal(TextureImporterSettings a, TextureImporterSettings b);

        public void CopyTo(TextureImporterSettings target)
        {
            Copy(this, target);
        }

        // Test texture importer settings for equality.
        [FreeFunction("TextureImporterBindings::CopyTo")]
        private static extern void Copy([NotNull] TextureImporterSettings self, [Out][NotNull] TextureImporterSettings target);

        // Configure parameters to import a texture for a purpose of ''type'', as described [[TextureImporterType|here]].
        [Obsolete("ApplyTextureType(TextureImporterType, bool) is deprecated, use ApplyTextureType(TextureImporterType)")]
        public void ApplyTextureType(TextureImporterType type, bool applyAll)
        {
            Internal_ApplyTextureType(this, type);
        }

        public void ApplyTextureType(TextureImporterType type)
        {
            Internal_ApplyTextureType(this, type);
        }

        [FreeFunction("TextureImporterBindings::ApplyTextureType")]
        private static extern void Internal_ApplyTextureType([Out][NotNull] TextureImporterSettings self, TextureImporterType type);

        // Deprecated APIs
        [Obsolete("Use sRGBTexture instead")]
        public bool linearTexture
        {
            get { return !sRGBTexture; }
            set { sRGBTexture = !value; }
        }
        [Obsolete("Check importer.textureType against TextureImporterType.NormalMap instead. Getter will work as expected. Setter will set textureType to NormalMap if true, nothing otherwise")]
        public bool normalMap
        {
            get { return textureType == TextureImporterType.NormalMap; }
            set { if (value) textureType = TextureImporterType.NormalMap; else textureType = TextureImporterType.Default; }
        }
        [Obsolete("Texture format can only be overridden on a per platform basis. See [[TextureImporterPlatformSettings]]")]
        public TextureImporterFormat textureFormat
        {
            get {return (TextureImporterFormat)m_TextureFormat; }
            set {m_TextureFormat = (int)textureFormat; textureFormatSet = 1; }
        }
        [Obsolete("Texture max size can only be overridden on a per platform basis. See [[TextureImporter.maxTextureSize]] for Default platform or [[TextureImporterPlatformSettings]]")]
        public int maxTextureSize
        {
            get { return m_MaxTextureSize; }
            set { m_MaxTextureSize = value; maxTextureSizeSet = 1; }
        }
        [Obsolete("Check importer.textureType against TextureImporterType.Lightmap instead. Getter will work as expected. Setter will set textureType to Lightmap if true, nothing otherwise.")]
        public bool lightmap
        {
            get { return textureType == TextureImporterType.Lightmap; }
            set { if (value) textureType = TextureImporterType.Lightmap; else textureType = TextureImporterType.Default; }
        }
        [Obsolete("RGBM is no longer a user's choice but has become an implementation detail hidden to the user.")]
        public TextureImporterRGBMMode rgbm
        {
            get { return (TextureImporterRGBMMode)m_RGBM; }
            set { m_RGBM = (int)value; }
        }
        [Obsolete("Use UnityEditor.TextureImporter.alphaSource instead")]
        public bool grayscaleToAlpha
        {
            get { return alphaSource == TextureImporterAlphaSource.FromGrayScale; }
            set { if (value) alphaSource = TextureImporterAlphaSource.FromGrayScale; else alphaSource = TextureImporterAlphaSource.FromInput; }
        }

        [Obsolete("Not used anymore. The right values are automatically picked by the importer.")]
        public int cubemapConvolutionSteps
        {
            get {return m_CubemapConvolutionSteps; }
            set {m_CubemapConvolutionSteps = value; }
        }
        [Obsolete("Not used anymore. The right values are automatically picked by the importer.")]
        public float cubemapConvolutionExponent
        {
            get {return m_CubemapConvolutionExponent; }
            set {m_CubemapConvolutionExponent = value; }
        }
        [Obsolete("Texture compression can only be overridden on a per platform basis. See [[TextureImporter.compressionQuality]] for Default platform or [[TextureImporterPlatformSettings]]")]
        public int compressionQuality
        {
            get { return m_CompressionQuality; }
            set { m_CompressionQuality = value; compressionQualitySet = 1; }
        }
        private int maxTextureSizeSet
        {
            get { return m_MaxTextureSizeSet; }
            set { m_MaxTextureSizeSet = value; }
        }

        private int textureFormatSet
        {
            get { return m_TextureFormatSet; }
            set { m_TextureFormatSet = value; }
        }

        private int compressionQualitySet
        {
            get { return m_CompressionQualitySet; }
            set { m_CompressionQualitySet = value; }
        }
    }

    // Note: MUST match memory layout of TextureImporterPlatformSettings in TextureImporterTypes.h!
    // This means you need to be careful about field sizes (e.g. don't use "bool" since they are different size in C# and C++).
    [System.Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [NativeAsStruct]
    [NativeType(CodegenOptions.Custom, "TextureImporterPlatformSettings_Marshalling")]
    [NativeHeader("Editor/Src/AssetPipeline/TextureImporting/TextureImporter.bindings.h")]
    public sealed partial class TextureImporterPlatformSettings
    {
        [SerializeField]
        [NativeName("m_BuildTarget")]
        string m_Name = TextureImporterInspector.s_DefaultPlatformName;
        [SerializeField]
        int m_Overridden = 0;
        [SerializeField]
        int m_MaxTextureSize = 2048;
        [SerializeField]
        int m_ResizeAlgorithm = (int)TextureResizeAlgorithm.Mitchell;
        [SerializeField]
        int m_TextureFormat = (int)TextureImporterFormat.Automatic;
        [SerializeField]
        int m_TextureCompression = (int)TextureImporterCompression.Compressed;
        [SerializeField]
        int m_CompressionQuality = (int)TextureCompressionQuality.Normal;
        [SerializeField]
        int m_CrunchedCompression = 0;
        [SerializeField]
        int m_AllowsAlphaSplitting = 0;
        [SerializeField]
        int m_AndroidETC2FallbackOverride = (int)AndroidETC2FallbackOverride.UseBuildSettings;

        public string name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        public bool overridden
        {
            get { return m_Overridden != 0; }
            set { m_Overridden = value ? 1 : 0; }
        }

        public int maxTextureSize
        {
            get { return m_MaxTextureSize; }
            set { m_MaxTextureSize = value; }
        }

        public TextureResizeAlgorithm resizeAlgorithm
        {
            get { return (TextureResizeAlgorithm)m_ResizeAlgorithm; }
            set { m_ResizeAlgorithm = (int)value; }
        }

        public TextureImporterFormat format
        {
            get { return (TextureImporterFormat)m_TextureFormat; }
            set { m_TextureFormat = (int)value; }
        }
        public TextureImporterCompression textureCompression
        {
            get { return (TextureImporterCompression)m_TextureCompression; }
            set { m_TextureCompression = (int)value; }
        }

        public int compressionQuality
        {
            get { return m_CompressionQuality; }
            set { m_CompressionQuality = value; }
        }

        public bool crunchedCompression
        {
            get { return m_CrunchedCompression != 0; }
            set { m_CrunchedCompression = value ? 1 : 0; }
        }

        public bool allowsAlphaSplitting
        {
            get { return m_AllowsAlphaSplitting != 0; }
            set { m_AllowsAlphaSplitting = value ? 1 : 0; }
        }

        public AndroidETC2FallbackOverride androidETC2FallbackOverride
        {
            get { return (AndroidETC2FallbackOverride)m_AndroidETC2FallbackOverride; }
            set { m_AndroidETC2FallbackOverride = (int)value; }
        }

        public void CopyTo(TextureImporterPlatformSettings target)
        {
            Copy(this, target);
        }

        [FreeFunction("TextureImporterBindings::CopyTo")]
        private static extern void Copy([NotNull] TextureImporterPlatformSettings self, [Out][NotNull] TextureImporterPlatformSettings target);
    }
}
