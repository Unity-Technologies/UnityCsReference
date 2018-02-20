// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEditor.Experimental.AssetImporters
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SpriteImportData
    {
        private string m_Name;
        private Rect m_Rect;
        private SpriteAlignment m_Alignment;
        private Vector2 m_Pivot;
        private Vector4 m_Border;
        private float m_TessellationDetail;
        private string m_SpriteID;
        private List<Vector2[]> m_Outline;

        public string name { get { return m_Name; } set { m_Name = value; } }
        public Rect rect { get { return m_Rect; } set { m_Rect = value; } }
        public SpriteAlignment alignment { get { return m_Alignment; } set { m_Alignment = value; } }
        public Vector2 pivot { get { return m_Pivot; } set { m_Pivot = value; } }
        public Vector4 border { get { return m_Border; } set { m_Border = value; } }
        public List<Vector2[]> outline { get { return m_Outline; } set { m_Outline = value; } }
        public float tessellationDetail {get { return m_TessellationDetail; } set { m_TessellationDetail = value; } }
        public string spriteID {get { return m_SpriteID; } set { m_SpriteID = value; } }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TextureGenerationOutput
    {
        [NativeName("texture")]
        private Texture2D m_Texture;
        [NativeName("importInspectorWarnings")]
        private string m_ImportInspectorWarnings;
        [NativeName("importWarnings")]
        private string[] m_ImportWarnings;
        [NativeName("thumbNail")]
        private Texture2D m_ThumbNail;
        [NativeName("sprites")]
        private Sprite[] m_Sprites;

        public Texture2D texture { get { return m_Texture; } }
        public string importInspectorWarnings { get { return m_ImportInspectorWarnings; } }
        public string[] importWarnings { get { return m_ImportWarnings; } }
        public Texture2D thumbNail { get { return m_ThumbNail; } }
        public Sprite[] sprites { get { return m_Sprites; } }
    };


    [StructLayout(LayoutKind.Sequential)]
    [NativeAsStruct]
    public class SourceTextureInformation
    {
        [NativeName("width")]
        private int m_Width;
        [NativeName("height")]
        private int m_Height;
        [NativeName("sourceContainsAlpha")]
        private bool m_SourceContainsAlpha;
        [NativeName("sourceWasHDR")]
        private bool m_SourceWasHDR;

        public int width { get { return m_Width; } set {m_Width = value; } }
        public int height { get { return m_Height; } set {m_Height = value; } }
        public bool containsAlpha { get { return m_SourceContainsAlpha; } set {m_SourceContainsAlpha = value; } }
        public bool hdr { get { return m_SourceWasHDR; } set {m_SourceWasHDR = value; } }
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct TextureGenerationSettings
    {
        [NativeName("assetPath")]
        private string m_AssetPath;
        [NativeName("qualifyForSpritePacking")]
        private bool m_QualifyForSpritePacking;
        [NativeName("enablePostProcessor")]
        private bool m_EnablePostProcessor;
        [NativeName("tiSettings")]
        private TextureImporterSettings m_Settings;
        [NativeName("platformSettings")]
        private TextureImporterPlatformSettings m_PlatformSettings;
        [NativeName("sourceTextureInformation")]
        private SourceTextureInformation m_SourceTextureInformation;
        [NativeName("spriteSheetData")]
        private SpriteImportData[] m_SpriteImportData;
        [NativeName("spritePackingTag")]
        private string m_SpritePackingTag;


        public TextureGenerationSettings(TextureImporterType type)
        {
            m_EnablePostProcessor = true;
            m_AssetPath = "";
            m_QualifyForSpritePacking = false;
            m_SpritePackingTag = "";
            m_SpriteImportData = null;

            m_SourceTextureInformation = new SourceTextureInformation();
            m_SourceTextureInformation.width = m_SourceTextureInformation.height = 0;
            m_SourceTextureInformation.containsAlpha = false;
            m_SourceTextureInformation.hdr = false;

            m_PlatformSettings = new TextureImporterPlatformSettings();
            m_PlatformSettings.overridden = false;
            m_PlatformSettings.format = TextureImporterFormat.Automatic;
            m_PlatformSettings.maxTextureSize = 2048;
            m_PlatformSettings.allowsAlphaSplitting = false;
            m_PlatformSettings.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
            m_PlatformSettings.compressionQuality = (int)TextureCompressionQuality.Normal;
            m_PlatformSettings.crunchedCompression = false;
            m_PlatformSettings.name = TextureImporter.defaultPlatformName;

            // Values from TextureImporterSettings native constructor
            m_Settings = new TextureImporterSettings();
            m_Settings.textureType = type;
            m_Settings.textureShape = TextureImporterShape.Texture2D;
            m_Settings.convertToNormalMap = false;
            m_Settings.mipmapEnabled = true;
            m_Settings.mipmapFilter = TextureImporterMipFilter.BoxFilter;
            m_Settings.sRGBTexture = true;
            m_Settings.borderMipmap = false;
            m_Settings.mipMapsPreserveCoverage = false;
            m_Settings.alphaTestReferenceValue = 0.5f;
            m_Settings.readable = false;
            m_Settings.fadeOut = false;
            m_Settings.mipmapFadeDistanceStart = 1;
            m_Settings.mipmapFadeDistanceEnd = 3;
            m_Settings.heightmapScale = 0.25f;
            m_Settings.normalMapFilter = TextureImporterNormalFilter.Standard;
            m_Settings.cubemapConvolution = 0;
            m_Settings.generateCubemap = TextureImporterGenerateCubemap.AutoCubemap;
            m_Settings.seamlessCubemap = false;
            m_Settings.npotScale = TextureImporterNPOTScale.ToNearest;
            m_Settings.spriteMode = (int)SpriteImportMode.Single;
            m_Settings.spriteExtrude = 1;
            m_Settings.spriteMeshType = SpriteMeshType.Tight;
            m_Settings.spriteAlignment = (int)SpriteAlignment.Center;
            m_Settings.spritePivot = Vector2.one * 0.5f;
            m_Settings.spritePixelsPerUnit = 100;
            m_Settings.spriteBorder = Vector4.zero;
            m_Settings.alphaSource = TextureImporterAlphaSource.FromInput;
            m_Settings.alphaIsTransparency = false;
            m_Settings.spriteTessellationDetail = -1;
            m_Settings.wrapMode = m_Settings.wrapModeU = m_Settings.wrapModeV = m_Settings.wrapModeW = TextureWrapMode.Repeat;

            // From TextureImporterSettings::ApplyTextureType
            switch (type)
            {
                case TextureImporterType.Default:
                    m_Settings.sRGBTexture = true;
                    m_Settings.mipmapEnabled = true;
                    break;
                case TextureImporterType.NormalMap:
                    m_Settings.sRGBTexture = false;
                    break;
                case TextureImporterType.GUI:
                    m_Settings.sRGBTexture = false;
                    m_Settings.mipmapEnabled = false;
                    m_Settings.alphaIsTransparency = true;
                    m_Settings.npotScale = TextureImporterNPOTScale.None;
                    m_Settings.aniso = 1;
                    m_Settings.wrapMode = m_Settings.wrapModeU = m_Settings.wrapModeV = m_Settings.wrapModeW = TextureWrapMode.Clamp;
                    break;
                case TextureImporterType.Sprite:
                    m_Settings.npotScale = TextureImporterNPOTScale.None;
                    m_Settings.alphaIsTransparency = true;
                    m_Settings.mipmapEnabled = false;
                    m_Settings.sRGBTexture = true;
                    m_Settings.wrapMode = m_Settings.wrapModeU = m_Settings.wrapModeV = m_Settings.wrapModeW = TextureWrapMode.Clamp;
                    m_Settings.alphaSource = TextureImporterAlphaSource.FromInput;
                    break;
                case TextureImporterType.Cursor:
                    m_Settings.readable = true;
                    m_Settings.alphaIsTransparency = true;
                    m_Settings.mipmapEnabled = false;
                    m_Settings.npotScale = TextureImporterNPOTScale.None;
                    m_Settings.aniso = 1;
                    m_Settings.wrapMode = m_Settings.wrapModeU = m_Settings.wrapModeV = m_Settings.wrapModeW = TextureWrapMode.Clamp;
                    break;
                case TextureImporterType.Cookie:
                    m_Settings.borderMipmap = true;
                    m_Settings.wrapMode = m_Settings.wrapModeU = m_Settings.wrapModeV = m_Settings.wrapModeW = TextureWrapMode.Clamp;
                    m_Settings.aniso = 0;
                    break;
                case TextureImporterType.Lightmap:
                    m_Settings.sRGBTexture = true;
                    m_Settings.npotScale = TextureImporterNPOTScale.ToNearest;
                    m_Settings.alphaIsTransparency = false;
                    m_Settings.alphaSource = TextureImporterAlphaSource.None;
                    break;
                case TextureImporterType.SingleChannel:
                    m_Settings.sRGBTexture = false;
                    break;
            }
        }

        public string assetPath { get { return m_AssetPath; } set { m_AssetPath = value; } }
        public bool qualifyForSpritePacking { get { return m_QualifyForSpritePacking; } set { m_QualifyForSpritePacking = value; } }
        public bool enablePostProcessor { get { return m_EnablePostProcessor; } set { m_EnablePostProcessor = value; } }
        public TextureImporterSettings textureImporterSettings { get { return m_Settings; } set { m_Settings = value; } }
        public TextureImporterPlatformSettings platformSettings { get { return m_PlatformSettings; } set { m_PlatformSettings = value; } }
        public SourceTextureInformation sourceTextureInformation { get { return m_SourceTextureInformation; } set { m_SourceTextureInformation = value; } }
        public SpriteImportData[] spriteImportData { get { return m_SpriteImportData; } set { m_SpriteImportData = value; } }
        public string spritePackingTag { get { return m_SpritePackingTag; } set { m_SpritePackingTag = value; } }
    };


    [NativeHeader("Editor/Src/AssetPipeline/TextureImporting/TextureGenerator.h")]
    [NativeHeader("Editor/Src/AssetPipeline/TextureImporting/TextureImporterTypes.h")]
    [NativeHeader("Editor/Src/AssetPipeline/TextureImporting/TextureImporter.bindings.h")]
    [NativeHeader("Runtime/Serialize/BuildTarget.h")]
    public static unsafe class TextureGenerator
    {
        public static TextureGenerationOutput GenerateTexture(TextureGenerationSettings settings, NativeArray<Color32> colorBuffer)
        {
            return GenerateTextureImpl(settings, colorBuffer.GetUnsafeReadOnlyPtr(), colorBuffer.Length * UnsafeUtility.SizeOf<Color32>());
        }

        [NativeThrows]
        [NativeMethod("GenerateTextureScripting")]
        extern static unsafe TextureGenerationOutput GenerateTextureImpl(TextureGenerationSettings settings, void* colorBuffer, int colorBufferLength);
    }
}
