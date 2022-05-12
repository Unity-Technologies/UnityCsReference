// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using uei = UnityEngine.Internal;

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
                [FreeFunction("GetGraphicsFormat_Native_Texture")]
                extern internal static GraphicsFormat GetFormat([NotNull("NullExceptionObject")] Texture texture);

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

                // Explicitly NOT thread safe. It accesses the GraphicsCaps. That object has some properties that can be changed at runtime (default formats).
                [FreeFunction(IsThreadSafe = false)]
                extern private static GraphicsFormat GetGraphicsFormat_Native_RenderTextureFormat(RenderTextureFormat format, bool isSRGB);

                public static GraphicsFormat GetGraphicsFormat(RenderTextureFormat format, RenderTextureReadWrite readWrite)
                {
                    bool defaultSRGB = QualitySettings.activeColorSpace == ColorSpace.Linear;
                    bool sRGB = readWrite == RenderTextureReadWrite.Default ? defaultSRGB : readWrite == RenderTextureReadWrite.sRGB;
                    return GetGraphicsFormat(format, sRGB);
                }

                [FreeFunction(IsThreadSafe = true)]
                extern private static GraphicsFormat GetDepthStencilFormatFromBitsLegacy_Native(int minimumDepthBits);

                internal static GraphicsFormat GetDepthStencilFormat(int minimumDepthBits)
                {
                    return GetDepthStencilFormatFromBitsLegacy_Native(minimumDepthBits);
                }

                [FreeFunction(IsThreadSafe = true)]
                extern public static int GetDepthBits(GraphicsFormat format);


                //the legacy DepthBufferFormatFromBits dropped stencil if depthBits < 16 but we need/can to handle it differently with an explicit stencilBits value.
                //8bit or 16bit depth + stencil maps to D24_UNorm_S8_UInt (so more bits for depth)
                //depth bit depth 0, 8, 16, 24, 32
                private static readonly GraphicsFormat[] tableNoStencil = { GraphicsFormat.None, GraphicsFormat.D16_UNorm, GraphicsFormat.D16_UNorm, GraphicsFormat.D24_UNorm, GraphicsFormat.D32_SFloat };
                private static readonly GraphicsFormat[] tableStencil = { GraphicsFormat.S8_UInt, GraphicsFormat.D16_UNorm_S8_UInt, GraphicsFormat.D16_UNorm_S8_UInt, GraphicsFormat.D24_UNorm_S8_UInt, GraphicsFormat.D32_SFloat_S8_UInt };

                public static GraphicsFormat GetDepthStencilFormat(int minimumDepthBits, int minimumStencilBits)
                {
                    if (minimumDepthBits == 0 && minimumStencilBits == 0)
                        return GraphicsFormat.None;

                    if (minimumDepthBits < 0 || minimumStencilBits < 0)
                        throw new ArgumentException("Number of bits in DepthStencil format can't be negative.");

                    if (minimumDepthBits > 32)
                        throw new ArgumentException("Number of depth buffer bits cannot exceed 32.");

                    if (minimumStencilBits > 8)
                        throw new ArgumentException("Number of stencil buffer bits cannot exceed 8.");

                    //the legacy DepthBufferFormatFromBits rounded up so mimicking that here
                    if (minimumDepthBits == 0)
                    {
                        minimumDepthBits = 0;
                    }
                    else if (minimumDepthBits <= 16)
                    {
                        minimumDepthBits = 16;
                    }
                    else if (minimumDepthBits <= 24)
                    {
                        minimumDepthBits = 24;
                    }
                    else
                    {
                        minimumDepthBits = 32;
                    }

                    if (minimumStencilBits != 0)
                        minimumStencilBits = 8;

                    Debug.Assert(tableNoStencil.Length == tableStencil.Length);

                    GraphicsFormat[] table = (minimumStencilBits > 0) ? tableStencil : tableNoStencil;

                    //the minimum format we need to guarantee the minimum bits
                    int formatIndex = minimumDepthBits / 8;

                    //verify that the format works on this platform. And try other formats with more bits if the formats fails.
                    for (int i = formatIndex; i < table.Length; ++i)
                    {
                        GraphicsFormat format = table[i];
                        if (SystemInfo.IsFormatSupported(format, FormatUsage.Render))
                            return format;
                    }

                    return GraphicsFormat.None;
                }

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsSRGBFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsSwizzleFormat(GraphicsFormat format);

                public static bool IsSwizzleFormat(TextureFormat format)
                {
                    return IsSwizzleFormat(GetGraphicsFormat(format, false));
                }

                [FreeFunction(IsThreadSafe = true)]
                extern public static GraphicsFormat GetSRGBFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static GraphicsFormat GetLinearFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static RenderTextureFormat GetRenderTextureFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static UInt32 GetColorComponentCount(GraphicsFormat format);

                public static UInt32 GetColorComponentCount(TextureFormat format)
                {
                    return GetColorComponentCount(GetGraphicsFormat(format, false));
                }

                [FreeFunction(IsThreadSafe = true)]
                extern public static UInt32 GetAlphaComponentCount(GraphicsFormat format);

                public static UInt32 GetAlphaComponentCount(TextureFormat format)
                {
                    return GetAlphaComponentCount(GetGraphicsFormat(format, false));
                }

                [FreeFunction(IsThreadSafe = true)]
                extern public static UInt32 GetComponentCount(GraphicsFormat format);

                public static UInt32 GetComponentCount(TextureFormat format)
                {
                    return GetComponentCount(GetGraphicsFormat(format, false));
                }

                [FreeFunction(IsThreadSafe = true)]
                extern public static string GetFormatString(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern private static string GetFormatString_Native_TextureFormat(TextureFormat format);

                public static string GetFormatString(TextureFormat format)
                {
                    return GetFormatString_Native_TextureFormat(format);
                }

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsCompressedFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern private static bool IsCompressedFormat_Native_TextureFormat(TextureFormat format);

                [Obsolete("IsCompressedTextureFormat is obsolete, please use IsCompressedFormat instead.")]
                internal static bool IsCompressedTextureFormat(TextureFormat format)
                {
                    return IsCompressedFormat(format);
                }

                public static bool IsCompressedFormat(TextureFormat format)
                {
                    return IsCompressedFormat_Native_TextureFormat(format);
                }

                [FreeFunction(IsThreadSafe = true)]
                extern private static bool CanDecompressFormat(GraphicsFormat format, bool wholeImage);

                internal static bool CanDecompressFormat(GraphicsFormat format)
                {
                    // Always returns false for uncompressed formats.
                    return CanDecompressFormat(format, true);
                }

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsPackedFormat(GraphicsFormat format);

                public static bool IsPackedFormat(TextureFormat format)
                {
                    return IsPackedFormat(GetGraphicsFormat(format, false));
                }

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool Is16BitPackedFormat(GraphicsFormat format);

                public static bool Is16BitPackedFormat(TextureFormat format)
                {
                    return Is16BitPackedFormat(GetGraphicsFormat(format, false));
                }

                [FreeFunction(IsThreadSafe = true)]
                extern public static GraphicsFormat ConvertToAlphaFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern private static TextureFormat ConvertToAlphaFormat_Native_TextureFormat(TextureFormat format);

                public static TextureFormat ConvertToAlphaFormat(TextureFormat format)
                {
                    return ConvertToAlphaFormat_Native_TextureFormat(format);
                }

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsAlphaOnlyFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern private static bool IsAlphaOnlyFormat_Native_TextureFormat(TextureFormat format);

                public static bool IsAlphaOnlyFormat(TextureFormat format)
                {
                    return IsAlphaOnlyFormat_Native_TextureFormat(format);
                }

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsAlphaTestFormat(GraphicsFormat format);

                public static bool IsAlphaTestFormat(TextureFormat format)
                {
                    return IsAlphaTestFormat(GetGraphicsFormat(format, false));
                }

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool HasAlphaChannel(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern private static bool HasAlphaChannel_Native_TextureFormat(TextureFormat format);

                public static bool HasAlphaChannel(TextureFormat format)
                {
                    return HasAlphaChannel_Native_TextureFormat(format);
                }

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsDepthFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsStencilFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsDepthStencilFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsIEEE754Format(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsFloatFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsHalfFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsUnsignedFormat(GraphicsFormat format);

                public static bool IsUnsignedFormat(TextureFormat format)
                {
                    return IsUnsignedFormat(GetGraphicsFormat(format, false));
                }

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsSignedFormat(GraphicsFormat format);

                public static bool IsSignedFormat(TextureFormat format)
                {
                    return IsSignedFormat(GetGraphicsFormat(format, false));
                }

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

                public static bool IsDXTCFormat(TextureFormat format)
                {
                    return IsDXTCFormat(GetGraphicsFormat(format, false));
                }

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsRGTCFormat(GraphicsFormat format);

                public static bool IsRGTCFormat(TextureFormat format)
                {
                    return IsRGTCFormat(GetGraphicsFormat(format, false));
                }

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsBPTCFormat(GraphicsFormat format);

                public static bool IsBPTCFormat(TextureFormat format)
                {
                    return IsBPTCFormat(GetGraphicsFormat(format, false));
                }

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsBCFormat(GraphicsFormat format);

                public static bool IsBCFormat(TextureFormat format)
                {
                    return IsBCFormat(GetGraphicsFormat(format, false));
                }

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsPVRTCFormat(GraphicsFormat format);

                public static bool IsPVRTCFormat(TextureFormat format)
                {
                    return IsPVRTCFormat(GetGraphicsFormat(format, false));
                }

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsETCFormat(GraphicsFormat format);

                public static bool IsETCFormat(TextureFormat format)
                {
                    return IsETCFormat(GetGraphicsFormat(format, false));
                }

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsEACFormat(GraphicsFormat format);

                public static bool IsEACFormat(TextureFormat format)
                {
                    return IsEACFormat(GetGraphicsFormat(format, false));
                }

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsASTCFormat(GraphicsFormat format);

                public static bool IsASTCFormat(TextureFormat format)
                {
                    return IsASTCFormat(GetGraphicsFormat(format, false));
                }

                [FreeFunction(IsThreadSafe = true)]
                extern public static bool IsHDRFormat(GraphicsFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern private static bool IsHDRFormat_Native_TextureFormat(TextureFormat format);

                public static bool IsHDRFormat(TextureFormat format)
                {
                    return IsHDRFormat_Native_TextureFormat(format);
                }

                [FreeFunction("IsCompressedCrunchTextureFormat", IsThreadSafe = true)]
                extern public static bool IsCrunchFormat(TextureFormat format);

                [FreeFunction(IsThreadSafe = true)]
                extern public static FormatSwizzle GetSwizzleR(GraphicsFormat format);

                public static FormatSwizzle GetSwizzleR(TextureFormat format)
                {
                    return GetSwizzleR(GetGraphicsFormat(format, false));
                }

                [FreeFunction(IsThreadSafe = true)]
                extern public static FormatSwizzle GetSwizzleG(GraphicsFormat format);

                public static FormatSwizzle GetSwizzleG(TextureFormat format)
                {
                    return GetSwizzleG(GetGraphicsFormat(format, false));
                }

                [FreeFunction(IsThreadSafe = true)]
                extern public static FormatSwizzle GetSwizzleB(GraphicsFormat format);

                public static FormatSwizzle GetSwizzleB(TextureFormat format)
                {
                    return GetSwizzleB(GetGraphicsFormat(format, false));
                }

                [FreeFunction(IsThreadSafe = true)]
                extern public static FormatSwizzle GetSwizzleA(GraphicsFormat format);

                public static FormatSwizzle GetSwizzleA(TextureFormat format)
                {
                    return GetSwizzleA(GetGraphicsFormat(format, false));
                }

                [FreeFunction(IsThreadSafe = true)]
                extern public static UInt32 GetBlockSize(GraphicsFormat format);

                public static UInt32 GetBlockSize(TextureFormat format)
                {
                    return GetBlockSize(GetGraphicsFormat(format, false));
                }

                [FreeFunction(IsThreadSafe = true)]
                extern public static UInt32 GetBlockWidth(GraphicsFormat format);

                public static UInt32 GetBlockWidth(TextureFormat format)
                {
                    return GetBlockWidth(GetGraphicsFormat(format, false));
                }

                [FreeFunction(IsThreadSafe = true)]
                extern public static UInt32 GetBlockHeight(GraphicsFormat format);

                public static UInt32 GetBlockHeight(TextureFormat format)
                {
                    return GetBlockHeight(GetGraphicsFormat(format, false));
                }

                public static UInt32 ComputeMipmapSize(int width, int height, GraphicsFormat format)
                {
                    return ComputeMipChainSize_Native_2D(width, height, format, 1);
                }

                public static UInt32 ComputeMipmapSize(int width, int height, TextureFormat format)
                {
                    return ComputeMipmapSize(width, height, GetGraphicsFormat(format, false));
                }

                [FreeFunction(IsThreadSafe = true)]
                extern private static UInt32 ComputeMipChainSize_Native_2D(int width, int height, GraphicsFormat format, int mipCount);

                public static UInt32 ComputeMipChainSize(int width, int height, GraphicsFormat format, [uei.DefaultValue("-1")] int mipCount = -1)
                {
                    return ComputeMipChainSize_Native_2D(width, height, format, mipCount);
                }

                public static UInt32 ComputeMipChainSize(int width, int height, TextureFormat format, [uei.DefaultValue("-1")] int mipCount = -1)
                {
                    return ComputeMipChainSize_Native_2D(width, height, GetGraphicsFormat(format, false), mipCount);
                }

                public static UInt32 ComputeMipmapSize(int width, int height, int depth, GraphicsFormat format)
                {
                    return ComputeMipChainSize_Native_3D(width, height, depth, format, 1);
                }

                public static UInt32 ComputeMipmapSize(int width, int height, int depth, TextureFormat format)
                {
                    return ComputeMipmapSize(width, height, depth, GetGraphicsFormat(format, false));
                }

                [FreeFunction(IsThreadSafe = true)]
                extern private static UInt32 ComputeMipChainSize_Native_3D(int width, int height, int depth, GraphicsFormat format, int mipCount);

                public static UInt32 ComputeMipChainSize(int width, int height, int depth, GraphicsFormat format, [uei.DefaultValue("-1")] int mipCount = -1)
                {
                    return ComputeMipChainSize_Native_3D(width, height, depth, format, mipCount);
                }

                public static UInt32 ComputeMipChainSize(int width, int height, int depth, TextureFormat format, [uei.DefaultValue("-1")] int mipCount = -1)
                {
                    return ComputeMipChainSize_Native_3D(width, height, depth, GetGraphicsFormat(format, false), mipCount);
                }
            }
        } // namespace Rendering
    } // namespace Experimental
} // Namespace
