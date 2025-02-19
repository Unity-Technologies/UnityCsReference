// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.Bindings;
using System;

namespace UnityEditor.U2D
{
    [NativeHeader("Runtime/2D/SpriteAtlas/SpriteAtlas.h")]
    [NativeHeader("Editor/Src/2D/SpriteAtlas/SpriteAtlasPackingUtilities.h")]
    public class SpriteAtlasUtility
    {
        [FreeFunction("SpriteAtlasExtensions::EnableV2Import")]
        extern internal static void EnableV2Import(bool onOff);

        [FreeFunction("SpriteAtlasExtensions::CleanupAtlasPacking")]
        extern public static void CleanupAtlasPacking();

        [FreeFunction("CollectAllSpriteAtlasesAndPack")]
        extern public static void PackAllAtlases(BuildTarget target, bool canCancel = true);

        [FreeFunction("PackSpriteAtlases")]
        extern internal static void PackAtlasesInternal(SpriteAtlas[] atlases, BuildTarget target, bool canCancel = true, bool invokedFromImporter = false, bool unloadSprites = false);

        public static void PackAtlases(SpriteAtlas[] atlases, BuildTarget target, bool canCancel = true)
        {
            if (atlases == null)
                throw new ArgumentNullException("atlases", "Value for parameter atlases is null");
            foreach (var atlas in atlases)
                if (atlas == null)
                    throw new ArgumentNullException("atlases", "One of the elements in atlases is null. Please check your Inputs.");
            PackAtlasesInternal(atlases, target, canCancel, false, true);
        }
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

        public int maxTextureSize { get { return m_MaxTextureSize; } }
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
        [NativeName("enableAlphaDilation")]
        private bool m_EnableAlphaDilation;

        public int blockOffset { get { return m_BlockOffset; } set { m_BlockOffset = value; } }
        public int padding { get { return m_Padding; } set { m_Padding = value; } }
        public bool enableRotation { get { return m_EnableRotation; } set { m_EnableRotation = value; } }
        public bool enableTightPacking { get { return m_EnableTightPacking; } set { m_EnableTightPacking = value; } }
        public bool enableAlphaDilation { get { return m_EnableAlphaDilation; } set { m_EnableAlphaDilation = value; } }
    }

    [NativeHeader("Editor/Src/AssetPipeline/TextureImporting/TextureImporterTypes.h")]
    [NativeHeader("Editor/Src/AssetPipeline/TextureImporting/TextureImporter.bindings.h")]
    [NativeHeader("Editor/Src/2D/SpriteAtlas/SpriteAtlas_EditorTypes.h")]
    [NativeHeader("Runtime/2D/SpriteAtlas/SpriteAtlas.h")]
    public static class SpriteAtlasExtensions
    {
        extern public static void Add([NotNull] this SpriteAtlas spriteAtlas, UnityEngine.Object[] objects);
        extern public static void Remove([NotNull] this SpriteAtlas spriteAtlas, UnityEngine.Object[] objects);
        extern internal static void RemoveAt([NotNull] this SpriteAtlas spriteAtlas, int index);
        extern public static UnityEngine.Object[] GetPackables([NotNull] this SpriteAtlas spriteAtlas);
        extern public static SpriteAtlasTextureSettings GetTextureSettings([NotNull] this SpriteAtlas spriteAtlas);
        extern public static void SetTextureSettings([NotNull] this SpriteAtlas spriteAtlas, SpriteAtlasTextureSettings src);
        extern public static SpriteAtlasPackingSettings GetPackingSettings([NotNull] this SpriteAtlas spriteAtlas);
        extern public static void SetPackingSettings([NotNull] this SpriteAtlas spriteAtlas, SpriteAtlasPackingSettings src);

        [NativeName("GetPlatformSettings")]
        extern private static TextureImporterPlatformSettings GetPlatformSettings_Internal([NotNull] this SpriteAtlas spriteAtlas, string buildTarget);
        public static TextureImporterPlatformSettings GetPlatformSettings(this SpriteAtlas spriteAtlas, string buildTarget)
        {
            buildTarget = TextureImporter.GetTexturePlatformSerializationName(buildTarget); // String may refer to a platform group: if != "Standalone", ensure it refers to a platform instead. E.g.: "iOS", not "iPhone".
            return GetPlatformSettings_Internal(spriteAtlas, buildTarget);
        }

        [NativeName("SetPlatformSettings")]
        extern private static void SetPlatformSettings_Internal([NotNull] this SpriteAtlas spriteAtlas, TextureImporterPlatformSettings src);
        public static void SetPlatformSettings(this SpriteAtlas spriteAtlas, TextureImporterPlatformSettings src)
        {
            src.name = TextureImporter.GetTexturePlatformSerializationName(src.name); // String may refer to a platform group: if != "Standalone", ensure it refers to a platform instead. E.g.: "iOS", not "iPhone".
            SetPlatformSettings_Internal(spriteAtlas, src);
        }

        extern public static void SetIncludeInBuild([NotNull] this SpriteAtlas spriteAtlas, bool value);
        extern public static void SetIsVariant([NotNull] this SpriteAtlas spriteAtlas, bool value);
        extern public static void SetMasterAtlas([NotNull] this SpriteAtlas spriteAtlas, SpriteAtlas value);
        extern public static void SetVariantScale([NotNull] this SpriteAtlas spriteAtlas, float value);
        extern public static bool IsIncludeInBuild([NotNull] this SpriteAtlas spriteAtlas);
        extern public static SpriteAtlas GetMasterAtlas([NotNull] this SpriteAtlas spriteAtlas);
        extern internal static void CopyMasterAtlasSettings([NotNull] this SpriteAtlas spriteAtlas);
        extern internal static string GetHash([NotNull] this SpriteAtlas spriteAtlas);
        extern internal static Texture2D[] GetPreviewTextures([NotNull] this SpriteAtlas spriteAtlas);
        extern internal static Texture2D[] GetPreviewAlphaTextures([NotNull] this SpriteAtlas spriteAtlas);
        extern internal static TextureFormat GetTextureFormat([NotNull] this SpriteAtlas spriteAtlas, BuildTarget target);
        extern internal static Sprite[] GetPackedSprites([NotNull] this SpriteAtlas spriteAtlas);
        extern internal static Hash128 GetStoredHash([NotNull] this SpriteAtlas spriteAtlas);

        [NativeName("GetSecondaryPlatformSettings")]
        extern private static TextureImporterPlatformSettings GetSecondaryPlatformSettings_Internal([NotNull] this SpriteAtlas spriteAtlas, string buildTarget, string secondaryTextureName);
        internal static TextureImporterPlatformSettings GetSecondaryPlatformSettings(this SpriteAtlas spriteAtlas, string buildTarget, string secondaryTextureName)
        {
            buildTarget = TextureImporter.GetTexturePlatformSerializationName(buildTarget); // String may refer to a platform group: if != "Standalone", ensure it refers to a platform instead. E.g.: "iOS", not "iPhone".
            return GetSecondaryPlatformSettings_Internal(spriteAtlas, buildTarget, secondaryTextureName);
        }

        [NativeName("SetSecondaryPlatformSettings")]
        extern private static void SetSecondaryPlatformSettings_Internal([NotNull] this SpriteAtlas spriteAtlas, TextureImporterPlatformSettings src, string secondaryTextureName);
        internal static void SetSecondaryPlatformSettings(this SpriteAtlas spriteAtlas, TextureImporterPlatformSettings src, string secondaryTextureName)
        {
            src.name = TextureImporter.GetTexturePlatformSerializationName(src.name); // String may refer to a platform group: if != "Standalone", ensure it refers to a platform instead. E.g.: "iOS", not "iPhone".
            SetSecondaryPlatformSettings_Internal(spriteAtlas, src, secondaryTextureName);
        }

        extern internal static bool GetSecondaryColorSpace([NotNull] this SpriteAtlas spriteAtlas, string secondaryTextureName);
        extern internal static void SetSecondaryColorSpace([NotNull] this SpriteAtlas spriteAtlas, string secondaryTextureName, bool srGB);
        extern internal static void DeleteSecondaryPlatformSettings([NotNull] this SpriteAtlas spriteAtlas, string secondaryTextureName);
        extern internal static string GetSecondaryTextureNameInAtlas(string atlasTextureName);
        extern internal static string GetPageNumberInAtlas(string atlasTextureName);
    }
}
