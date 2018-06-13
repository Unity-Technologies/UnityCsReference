// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/AssetPipeline/TextureImporting/TextureImporterUtils.h")]
    [NativeHeader("Runtime/Graphics/TextureFormat.h")]
    internal static class TextureUtil
    {
        [Obsolete("GetStorageMemorySize has been deprecated since it is limited to 2GB. Please use GetStorageMemorySizeLong() instead.")]
        public static int GetStorageMemorySize(Texture t)
        {
            return (int)GetStorageMemorySizeLong(t);
        }

        [FreeFunction]
        public static extern long GetStorageMemorySizeLong(Texture t);

        [Obsolete("GetRuntimeMemorySize has been deprecated since it is limited to 2GB. Please use GetRuntimeMemorySizeLong() instead.")]
        public static int GetRuntimeMemorySize(Texture t)
        {
            return (int)GetRuntimeMemorySizeLong(t);
        }

        [FreeFunction]
        public static extern long GetRuntimeMemorySizeLong(Texture t);

        [FreeFunction]
        public static extern bool IsNonPowerOfTwo(Texture2D t);

        [FreeFunction]
        public static extern TextureUsageMode GetUsageMode(Texture t);

        [FreeFunction]
        public static extern int GetBytesFromTextureFormat(TextureFormat inFormat);

        [FreeFunction]
        public static extern int GetRowBytesFromWidthAndFormat(int width, TextureFormat format);

        [FreeFunction]
        public static extern bool IsValidTextureFormat(TextureFormat format);

        [FreeFunction("IsAnyCompressedTextureFormat")]
        public static extern bool IsCompressedTextureFormat(TextureFormat format);

        [FreeFunction("IsCompressedCrunchTextureFormat")]
        public static extern bool IsCompressedCrunchTextureFormat(TextureFormat format);

        [FreeFunction]
        public static extern TextureFormat GetTextureFormat(Texture texture);

        [FreeFunction]
        public static extern bool IsAlphaOnlyTextureFormat(TextureFormat format);

        [FreeFunction]
        public static extern bool HasAlphaTextureFormat(TextureFormat format);

        [FreeFunction]
        public static extern string GetTextureFormatString(TextureFormat format);

        [FreeFunction]
        public static extern string GetTextureColorSpaceString(Texture texture);

        [FreeFunction]
        public static extern TextureFormat ConvertToAlphaTextureFormat(TextureFormat format);

        [FreeFunction]
        public static extern bool IsDepthRTFormat(RenderTextureFormat format);

        [FreeFunction]
        public static extern bool HasMipMap(Texture t);

        [FreeFunction]
        public static extern int GetGPUWidth(Texture t);

        [FreeFunction]
        public static extern int GetGPUHeight(Texture t);

        [FreeFunction]
        public static extern int GetMipmapCount(Texture t);

        [FreeFunction]
        public static extern bool GetLinearSampled(Texture t);

        public static int GetDefaultCompressionQuality()
        {
            return (int)TextureCompressionQuality.Normal;
        }

        [FreeFunction]
        public static extern Vector4 GetTexelSizeVector(Texture t);

        [FreeFunction]
        public static extern Texture2D GetSourceTexture(Cubemap cubemapRef, CubemapFace face);

        [FreeFunction]
        public static extern void SetSourceTexture(Cubemap cubemapRef, CubemapFace face, Texture2D tex);

        [FreeFunction]
        public static extern void CopyTextureIntoCubemapFace(Texture2D textureRef, Cubemap cubemapRef, CubemapFace face);

        [FreeFunction]
        public static extern void CopyCubemapFaceIntoTexture(Cubemap cubemapRef, CubemapFace face, Texture2D textureRef);

        [FreeFunction]
        public static extern bool ReformatCubemap(ref Cubemap cubemap, int width, int height, TextureFormat textureFormat, bool useMipmap, bool linear);

        [FreeFunction]
        public static extern bool ReformatTexture(ref Texture2D texture, int width, int height, TextureFormat textureFormat, bool useMipmap, bool linear);

        [FreeFunction]
        public static extern void SetAnisoLevelNoDirty(Texture tex, int level);

        [FreeFunction]
        public static extern void SetWrapModeNoDirty(Texture tex, TextureWrapMode u, TextureWrapMode v, TextureWrapMode w);

        [FreeFunction]
        public static extern void SetMipMapBiasNoDirty(Texture tex, float bias);

        [FreeFunction]
        public static extern void SetFilterModeNoDirty(Texture tex, FilterMode mode);

        [FreeFunction]
        public static extern bool DoesTextureStillNeedToBeCompressed(string assetPath);

        [FreeFunction]
        public static extern bool IsCubemapReadable(Cubemap cubemapRef);

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
