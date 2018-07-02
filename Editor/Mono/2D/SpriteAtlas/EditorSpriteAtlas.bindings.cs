// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.Bindings;

namespace UnityEditor.U2D
{
    [NativeHeader("Runtime/2D/SpriteAtlas/SpriteAtlas.h")]
    [NativeHeader("Editor/Src/2D/SpriteAtlas/SpriteAtlasPackingUtilities.h")]
    public class SpriteAtlasUtility
    {
        [FreeFunction("CollectAllSpriteAtlasesAndPack")]
        extern public static void PackAllAtlases(BuildTarget target, bool canCancel = true);

        [FreeFunction("PackSpriteAtlases")]
        extern public static void PackAtlases(SpriteAtlas[] atlases, BuildTarget target, bool canCancel = true);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SpriteAtlasTextureSettings
    {
        [NativeName("anisoLevel")]
        private int m_AnisoLevel;
        [NativeName("compressionQuality")]
        private int m_CompressionQuality;
        [NativeName("maxTextureSize")]
        private int m_MaxTextureSize;
        [NativeName("textureCompression")]
        private int m_TextureCompression;
        [NativeName("filterMode")]
        private int m_FilterMode;
        [NativeName("generateMipMaps")]
        private bool m_GenerateMipMaps;
        [NativeName("readable")]
        private bool m_Readable;
        [NativeName("crunchedCompression")]
        private bool m_CrunchedCompression;
        [NativeName("sRGB")]
        private bool m_sRGB;

        public int anisoLevel { get { return m_AnisoLevel; } set { m_AnisoLevel = value; } }
        public FilterMode filterMode { get { return (FilterMode)m_FilterMode; } set { m_FilterMode = (int)value; } }
        public bool generateMipMaps { get { return m_GenerateMipMaps; } set { m_GenerateMipMaps = value; } }
        public bool readable { get { return m_Readable; } set { m_Readable = value; } }
        public bool sRGB { get { return m_sRGB; } set { m_sRGB = value; } }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SpriteAtlasPackingSettings
    {
        [NativeName("blockOffset")]
        private int m_BlockOffset;
        [NativeName("padding")]
        private int m_Padding;
        [NativeName("allowAlphaSplitting")]
        private bool m_AllowAlphaSplitting;
        [NativeName("enableRotation")]
        private bool m_EnableRotation;
        [NativeName("enableTightPacking")]
        private bool m_EnableTightPacking;

        public int blockOffset { get { return m_BlockOffset; } set { m_BlockOffset = value; } }
        public int padding { get { return m_Padding; } set { m_Padding = value; } }
        public bool enableRotation { get { return m_EnableRotation; } set { m_EnableRotation = value; } }
        public bool enableTightPacking { get { return m_EnableTightPacking; } set { m_EnableTightPacking = value; } }
    }

    [NativeHeader("Editor/Src/AssetPipeline/TextureImporting/TextureImporterTypes.h")]
    [NativeHeader("Editor/Src/AssetPipeline/TextureImporting/TextureImporter.bindings.h")]
    [NativeHeader("Editor/Src/2D/SpriteAtlas/SpriteAtlas_EditorTypes.h")]
    [NativeHeader("Runtime/2D/SpriteAtlas/SpriteAtlas.h")]
    public static class SpriteAtlasExtensions
    {
        extern public static void Add(this SpriteAtlas spriteAtlas, UnityEngine.Object[] objects);
        extern public static void Remove(this SpriteAtlas spriteAtlas, UnityEngine.Object[] objects);
        extern internal static void RemoveAt(this SpriteAtlas spriteAtlas, int index);
        extern public static UnityEngine.Object[] GetPackables(this SpriteAtlas spriteAtlas);
        extern public static SpriteAtlasTextureSettings GetTextureSettings(this SpriteAtlas spriteAtlas);
        extern public static void SetTextureSettings(this SpriteAtlas spriteAtlas, SpriteAtlasTextureSettings src);
        extern public static SpriteAtlasPackingSettings GetPackingSettings(this SpriteAtlas spriteAtlas);
        extern public static void SetPackingSettings(this SpriteAtlas spriteAtlas, SpriteAtlasPackingSettings src);
        extern public static TextureImporterPlatformSettings GetPlatformSettings(this SpriteAtlas spriteAtlas, string buildTarget);
        extern public static void SetPlatformSettings(this SpriteAtlas spriteAtlas, TextureImporterPlatformSettings src);
        extern public static void SetIncludeInBuild(this SpriteAtlas spriteAtlas, bool value);
        extern public static void SetIsVariant(this SpriteAtlas spriteAtlas, bool value);
        extern public static void SetMasterAtlas(this SpriteAtlas spriteAtlas, SpriteAtlas value);
        extern public static void SetVariantScale(this SpriteAtlas spriteAtlas, float value);
        extern internal static void CopyMasterAtlasSettings(this SpriteAtlas spriteAtlas);
        extern internal static string GetHash(this SpriteAtlas spriteAtlas);
        extern internal static Texture2D[] GetPreviewTextures(this SpriteAtlas spriteAtlas);
        extern internal static Texture2D[] GetPreviewAlphaTextures(this SpriteAtlas spriteAtlas);
        extern internal static TextureFormat GetTextureFormat(this SpriteAtlas spriteAtlas, BuildTarget target);
        extern internal static Sprite[] GetPackedSprites(this SpriteAtlas spriteAtlas);
    }
}
