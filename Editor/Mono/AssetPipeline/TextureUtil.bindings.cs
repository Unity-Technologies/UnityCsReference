// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Experimental.Rendering; //for GraphicsFormat
namespace UnityEditor
{
    [NativeHeader("Editor/Src/AssetPipeline/TextureImporting/TextureImporterUtils.h")]
    [NativeHeader("Runtime/Graphics/TextureFormat.h")]
    [NativeHeader("Runtime/Graphics/Format.h")]
    internal static class TextureUtil
    {
        [FreeFunction]
        public static extern long GetStorageMemorySizeLong([NotNull("NullExceptionObject")] Texture t);

        [FreeFunction]
        public static extern long GetRuntimeMemorySizeLong([NotNull("NullExceptionObject")] Texture t);

        [FreeFunction]
        public static extern bool IsNonPowerOfTwo([NotNull("NullExceptionObject")] Texture2D t);

        [FreeFunction]
        public static extern TextureUsageMode GetUsageMode([NotNull("NullExceptionObject")] Texture t);

        [FreeFunction]
        public static extern bool IsNormalMapUsageMode(TextureUsageMode usageMode);

        [FreeFunction]
        public static extern bool IsRGBMUsageMode(TextureUsageMode usageMode);

        [FreeFunction]
        public static extern bool IsDoubleLDRUsageMode(TextureUsageMode usageMode);

        [FreeFunction]
        public static extern int GetBytesFromTextureFormat(TextureFormat inFormat);

        [FreeFunction]
        public static extern int GetRowBytesFromWidthAndFormat(int width, TextureFormat format);

        [FreeFunction]
        public static extern bool IsValidTextureFormat(TextureFormat format);

        [Obsolete("IsCompressedTextureFormat has been moved to GraphicsFormatUtility.IsCompressedFormat(TextureFormat)")]
        [FreeFunction("IsAnyCompressedTextureFormat")]
        public static extern bool IsCompressedTextureFormat(TextureFormat format);

        [Obsolete("IsCompressedCrunchTextureFormat has been moved to GraphicsFormatUtility.IsCrunchFormat(TextureFormat)")]
        [FreeFunction("IsCompressedCrunchTextureFormat")]
        public static extern bool IsCompressedCrunchTextureFormat(TextureFormat format);

        [FreeFunction]
        public static extern TextureFormat GetTextureFormat([NotNull("NullExceptionObject")] Texture texture);

        [Obsolete("IsAlphaOnlyTextureFormat has been moved to GraphicsFormatUtility.IsAlphaOnlyFormat(TextureFormat)")]
        [FreeFunction]
        public static extern bool IsAlphaOnlyTextureFormat(TextureFormat format);

        [Obsolete("IsHDRFormat has been moved to GraphicsFormatUtility.IsHDRFormat(TextureFormat)")]
        [FreeFunction]
        public static extern bool IsHDRFormat(TextureFormat format);

        [Obsolete("IsHDRGraphicsFormat has been moved to GraphicsFormatUtility.IsHDRFormat(GraphicsFormat)")]
        [FreeFunction("IsHDRFormat")]
        public static extern bool IsHDRGraphicsFormat(GraphicsFormat format);

        [Obsolete("HasAlphaTextureFormat has been moved to GraphicsFormatUtility.HasAlphaChannel(TextureFormat)")]
        [FreeFunction]
        public static extern bool HasAlphaTextureFormat(TextureFormat format);

        [Obsolete("GetTextureFormatString has been moved to GraphicsFormatUtility.GetFormatString(TextureFormat)")]
        [FreeFunction]
        public static extern string GetTextureFormatString(TextureFormat format);

        [FreeFunction]
        public static extern string GetTextureColorSpaceString([NotNull("NullExceptionObject")] Texture texture);

        [Obsolete("ConvertToAlphaTextureFormat has been moved to GraphicsFormatUtility.ConvertToAlphaFormat(TextureFormat)")]
        [FreeFunction]
        public static extern TextureFormat ConvertToAlphaTextureFormat(TextureFormat format);

        public static bool IsDepthRTFormat(RenderTextureFormat format)
        {
            return format == RenderTextureFormat.Depth || format == RenderTextureFormat.Shadowmap;
        }

        [FreeFunction]
        public static extern bool HasMipMap([NotNull("NullExceptionObject")] Texture t);

        [FreeFunction]
        public static extern bool NeedsExposureControl([NotNull("NullExceptionObject")] Texture t);

        [FreeFunction]
        public static extern int GetGPUWidth([NotNull("NullExceptionObject")] Texture t);

        [FreeFunction]
        public static extern int GetGPUHeight([NotNull("NullExceptionObject")] Texture t);

        [FreeFunction]
        public static extern int GetMipmapCount([NotNull("NullExceptionObject")] Texture t);

        [FreeFunction]
        public static extern bool GetLinearSampled([NotNull("NullExceptionObject")] Texture t);

        public static int GetDefaultCompressionQuality()
        {
            return (int)TextureCompressionQuality.Normal;
        }

        [FreeFunction]
        public static extern Vector4 GetTexelSizeVector([NotNull("NullExceptionObject")] Texture t);

        [FreeFunction]
        public static extern Texture2D GetSourceTexture([NotNull("NullExceptionObject")] Cubemap cubemapRef, CubemapFace face);

        [FreeFunction]
        public static extern void SetSourceTexture([NotNull("NullExceptionObject")] Cubemap cubemapRef, CubemapFace face, Texture2D tex);

        [FreeFunction]
        public static extern void CopyTextureIntoCubemapFace([NotNull("NullExceptionObject")] Texture2D textureRef, [NotNull("NullExceptionObject")] Cubemap cubemapRef, CubemapFace face);

        [FreeFunction]
        public static extern void CopyCubemapFaceIntoTexture([NotNull("NullExceptionObject")] Cubemap cubemapRef, CubemapFace face, [NotNull("NullExceptionObject")] Texture2D textureRef);

        [FreeFunction]
        public static extern bool ReformatCubemap([NotNull("NullExceptionObject")] Cubemap cubemap, int width, int height, TextureFormat textureFormat, bool useMipmap, bool linear);

        [FreeFunction]
        public static extern bool ReformatTexture([NotNull("NullExceptionObject")] ref Texture2D texture, int width, int height, TextureFormat textureFormat, bool useMipmap, bool linear);

        [FreeFunction]
        public static extern void SetAnisoLevelNoDirty(Texture tex, int level);

        [FreeFunction]
        public static extern void SetWrapModeNoDirty(Texture tex, TextureWrapMode u, TextureWrapMode v, TextureWrapMode w);

        [FreeFunction]
        public static extern void SetMipMapBiasNoDirty(Texture tex, float bias);

        [FreeFunction]
        public static extern void SetFilterModeNoDirty(Texture tex, FilterMode mode);

        [FreeFunction]
        public static extern bool IsCubemapReadable([NotNull("NullExceptionObject")] Cubemap cubemapRef);

        [FreeFunction]
        public static extern void MarkCubemapReadable(Cubemap cubemapRef, bool readable);

        [FreeFunction]
        public static extern bool GetTexture2DStreamingMipmaps(Texture2D texture);

        [FreeFunction]
        public static extern int GetTexture2DStreamingMipmapsPriority(Texture2D texture);

        [FreeFunction]
        public static extern bool GetCubemapStreamingMipmaps(Cubemap cubemap);

        [FreeFunction]
        public static extern int GetCubemapStreamingMipmapsPriority(Cubemap cubemap);

        [FreeFunction]
        public static extern void SetTexture2DStreamingMipmaps(Texture2D textureRef, bool streaming);

        [FreeFunction]
        public static extern void SetTexture2DStreamingMipmapsPriority(Texture2D textureRef, int priority);

        [FreeFunction]
        public static extern void SetCubemapStreamingMipmaps(Cubemap cubemapRef, bool streaming);

        [FreeFunction]
        public static extern void SetCubemapStreamingMipmapsPriority(Cubemap cubemapRef, int priority);
    }
}
