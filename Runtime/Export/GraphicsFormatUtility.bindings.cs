// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine
{
    namespace Experimental
    {
        namespace Rendering
        {
            [NativeHeader("Runtime/Graphics/TextureFormat.h")]
            [NativeHeader("Runtime/Graphics/GraphicsFormatUtility.bindings.h")]
            public class GraphicsFormatUtility
            {
                public static GraphicsFormat GetGraphicsFormat(TextureFormat format, bool isSRGB)
                {
                    return GetGraphicsFormat_Native_TextureFormat(format, isSRGB);
                }

                [FreeFunction]
                extern private static GraphicsFormat GetGraphicsFormat_Native_TextureFormat(TextureFormat format, bool isSRGB);

                [FreeFunction]
                extern public static TextureFormat GetTextureFormat(GraphicsFormat format);

                public static GraphicsFormat GetGraphicsFormat(RenderTextureFormat format, bool isSRGB)
                {
                    return GetGraphicsFormat_Native_RenderTextureFormat(format, isSRGB);
                }

                [FreeFunction]
                extern private static GraphicsFormat GetGraphicsFormat_Native_RenderTextureFormat(RenderTextureFormat format, bool isSRGB);

                [FreeFunction]
                extern public static bool IsSRGBFormat(GraphicsFormat format);

                [FreeFunction]
                extern public static RenderTextureFormat GetRenderTextureFormat(GraphicsFormat format);

                [FreeFunction]
                extern public static UInt32 GetColorComponentCount(GraphicsFormat format);

                [FreeFunction]
                extern public static UInt32 GetAlphaComponentCount(GraphicsFormat format);

                [FreeFunction]
                extern public static UInt32 GetComponentCount(GraphicsFormat format);

                [FreeFunction]
                extern public static bool IsCompressedFormat(GraphicsFormat format);

                [FreeFunction("IsAnyCompressedTextureFormat")]
                extern internal static bool IsCompressedTextureFormat(TextureFormat format);

                [FreeFunction]
                extern public static bool IsPackedFormat(GraphicsFormat format);

                [FreeFunction]
                extern public static bool Is16BitPackedFormat(GraphicsFormat format);

                [FreeFunction]
                extern public static GraphicsFormat ConvertToAlphaFormat(GraphicsFormat format);

                [FreeFunction]
                extern public static bool IsAlphaOnlyFormat(GraphicsFormat format);

                [FreeFunction]
                extern public static bool HasAlphaChannel(GraphicsFormat format);

                [FreeFunction]
                extern public static bool IsDepthFormat(GraphicsFormat format);

                [FreeFunction]
                extern public static bool IsStencilFormat(GraphicsFormat format);

                [FreeFunction]
                extern public static bool IsIEEE754Format(GraphicsFormat format);

                [FreeFunction]
                extern public static bool IsFloatFormat(GraphicsFormat format);

                [FreeFunction]
                extern public static bool IsHalfFormat(GraphicsFormat format);

                [FreeFunction]
                extern public static bool IsUnsignedFormat(GraphicsFormat format);

                [FreeFunction]
                extern public static bool IsSignedFormat(GraphicsFormat format);

                [FreeFunction]
                extern public static bool IsNormFormat(GraphicsFormat format);

                [FreeFunction]
                extern public static bool IsUNormFormat(GraphicsFormat format);

                [FreeFunction]
                extern public static bool IsSNormFormat(GraphicsFormat format);

                [FreeFunction]
                extern public static bool IsIntegerFormat(GraphicsFormat format);

                [FreeFunction]
                extern public static bool IsUIntFormat(GraphicsFormat format);

                [FreeFunction]
                extern public static bool IsSIntFormat(GraphicsFormat format);


                [FreeFunction]
                extern public static bool IsDXTCFormat(GraphicsFormat format);

                [FreeFunction]
                extern public static bool IsRGTCFormat(GraphicsFormat format);

                [FreeFunction]
                extern public static bool IsBPTCFormat(GraphicsFormat format);

                [FreeFunction]
                extern public static bool IsBCFormat(GraphicsFormat format);

                [FreeFunction]
                extern public static bool IsPVRTCFormat(GraphicsFormat format);

                [FreeFunction]
                extern public static bool IsETCFormat(GraphicsFormat format);

                [FreeFunction]
                extern public static bool IsASTCFormat(GraphicsFormat format);

                public static bool IsCrunchFormat(TextureFormat format)
                {
                    return format == TextureFormat.DXT1Crunched || format == TextureFormat.DXT5Crunched || format == TextureFormat.ETC_RGB4Crunched || format == TextureFormat.ETC2_RGBA8Crunched;
                }

                [FreeFunction]
                extern public static UInt32 GetBlockSize(GraphicsFormat format);

                [FreeFunction]
                extern public static UInt32 GetBlockWidth(GraphicsFormat format);

                [FreeFunction]
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
