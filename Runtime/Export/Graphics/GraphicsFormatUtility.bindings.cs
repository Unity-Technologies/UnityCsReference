// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Rendering;

namespace UnityEngine
{
    namespace Experimental
    {
        namespace Rendering
        {
            [NativeHeader("Runtime/Graphics/Format.h")]
            [NativeHeader("Runtime/Graphics/TextureFormat.h")]
            [NativeHeader("Runtime/Graphics/GraphicsFormatUtility.bindings.h")]
            public class GraphicsFormatUtility
            {
                [FreeFunction]
                extern internal static GraphicsFormat GetFormat(Texture texture);

                public static GraphicsFormat GetGraphicsFormat(TextureFormat format, bool isSRGB)
                {
                    return GetGraphicsFormat_Native_TextureFormat(format, isSRGB);
                }

                [FreeFunction(IsThreadSafe = true)]
                extern private static GraphicsFormat GetGraphicsFormat_Native_TextureFormat(TextureFormat format, bool isSRGB);

                public static TextureFormat GetTextureFormat(GraphicsFormat format)
                {
                    return GetTextureFormat_Native_GraphicsFormat(format);
                }

                [FreeFunction(IsThreadSafe = true)]
                extern private static TextureFormat GetTextureFormat_Native_GraphicsFormat(GraphicsFormat format);

                public static GraphicsFormat GetGraphicsFormat(RenderTextureFormat format, bool isSRGB)
                {
                    return GetGraphicsFormat_Native_RenderTextureFormat(format, isSRGB);
                }

                [FreeFunction]
                extern private static GraphicsFormat GetGraphicsFormat_Native_RenderTextureFormat(RenderTextureFormat format, bool isSRGB);

                public static GraphicsFormat GetGraphicsFormat(RenderTextureFormat format, RenderTextureReadWrite readWrite)
                {
                    bool defaultSRGB = QualitySettings.activeColorSpace == ColorSpace.Linear;
                    bool sRGB = readWrite == RenderTextureReadWrite.Default ? defaultSRGB : readWrite == RenderTextureReadWrite.sRGB;
                    return GetGraphicsFormat(format, sRGB);
                }

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsSRGBFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsSwizzleFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static GraphicsFormat GetSRGBFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static GraphicsFormat GetLinearFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static RenderTextureFormat GetRenderTextureFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static UInt32 GetColorComponentCount(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static UInt32 GetAlphaComponentCount(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static UInt32 GetComponentCount(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static string GetFormatString(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsCompressedFormat(GraphicsFormat format);

                [FreeFunction("IsAnyCompressedTextureFormat", true)]
                extern internal static bool IsCompressedTextureFormat(TextureFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsPackedFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool Is16BitPackedFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static GraphicsFormat ConvertToAlphaFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsAlphaOnlyFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsAlphaTestFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool HasAlphaChannel(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsDepthFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsStencilFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsIEEE754Format(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsFloatFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsHalfFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsUnsignedFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsSignedFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsNormFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsUNormFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsSNormFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsIntegerFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsUIntFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsSIntFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsXRFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsDXTCFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsRGTCFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsBPTCFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsBCFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsPVRTCFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsETCFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsEACFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsASTCFormat(GraphicsFormat format);

                public static bool IsCrunchFormat(TextureFormat format)
                {
                    return format == TextureFormat.DXT1Crunched || format == TextureFormat.DXT5Crunched || format == TextureFormat.ETC_RGB4Crunched || format == TextureFormat.ETC2_RGBA8Crunched;
                }

                [FreeFunction(IsThreadSafe = true)]
                extern public static FormatSwizzle GetSwizzleR(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static FormatSwizzle GetSwizzleG(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static FormatSwizzle GetSwizzleB(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static FormatSwizzle GetSwizzleA(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static UInt32 GetBlockSize(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static UInt32 GetBlockWidth(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static UInt32 GetBlockHeight(GraphicsFormat format);

                public static UInt32 ComputeMipmapSize(int width, int height, GraphicsFormat format)
                {
                    return ComputeMipmapSize_Native_2D(width, height, format);
                }

                [FreeFunction]
                extern private static UInt32 ComputeMipmapSize_Native_2D(int width, int height, GraphicsFormat format);

                public static UInt32 ComputeMipmapSize(int width, int height, int depth, GraphicsFormat format)
                {
                    return ComputeMipmapSize_Native_3D(width, height, depth, format);
                }

                [FreeFunction]
                extern private static UInt32 ComputeMipmapSize_Native_3D(int width, int height, int depth, GraphicsFormat format);
            }
        } // namespace Rendering
    } // namespace Experimental
} // Namespace
