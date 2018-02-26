// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityScript.Scripting;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditorInternal;
using UnityEditor.Build;
using UnityEditor.Collaboration;
using UnityEditor.Connect;

namespace UnityEditor
{
internal enum TextureUsageMode
{
    
    Default = 0,
    
    BakedLightmapDoubleLDR = 1,
    
    BakedLightmapRGBM = 2,
    
    NormalmapDXT5nm = 3,
    
    NormalmapPlain = 4,
    RGBMEncoded = 5,
    
    AlwaysPadded = 6,
    DoubleLDR = 7,
    
    BakedLightmapFullHDR = 8,
    RealtimeLightmapRGBM = 9,
}

internal sealed partial class TextureUtil
{
    [Obsolete("GetStorageMemorySize has been deprecated since it is limited to 2GB. Please use GetStorageMemorySizeLong() instead.")]
    public static int GetStorageMemorySize(Texture t)
        {
            return (int)GetStorageMemorySizeLong(t);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  long GetStorageMemorySizeLong (Texture t) ;

    [Obsolete("GetRuntimeMemorySize has been deprecated since it is limited to 2GB. Please use GetRuntimeMemorySizeLong() instead.")]
    public static int GetRuntimeMemorySize(Texture t)
        {
            return (int)GetRuntimeMemorySizeLong(t);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  long GetRuntimeMemorySizeLong (Texture t) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool IsNonPowerOfTwo (Texture2D t) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  TextureUsageMode GetUsageMode (Texture t) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetBytesFromTextureFormat (TextureFormat inFormat) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetRowBytesFromWidthAndFormat (int width, TextureFormat format) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool IsValidTextureFormat (TextureFormat format) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool IsCompressedTextureFormat (TextureFormat format) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  TextureFormat GetTextureFormat (Texture texture) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool IsAlphaOnlyTextureFormat (TextureFormat format) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool HasAlphaTextureFormat (TextureFormat format) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetTextureFormatString (TextureFormat format) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetTextureColorSpaceString (Texture texture) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  TextureFormat ConvertToAlphaTextureFormat (TextureFormat format) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool IsDepthRTFormat (RenderTextureFormat format) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool HasMipMap (Texture t) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetGPUWidth (Texture t) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetGPUHeight (Texture t) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetMipmapCount (Texture t) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool GetLinearSampled (Texture t) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetDefaultCompressionQuality () ;

    public static Vector4 GetTexelSizeVector (Texture t) {
        Vector4 result;
        INTERNAL_CALL_GetTexelSizeVector ( t, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetTexelSizeVector (Texture t, out Vector4 value);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Texture2D GetSourceTexture (Cubemap cubemapRef, CubemapFace face) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetSourceTexture (Cubemap cubemapRef, CubemapFace face, Texture2D tex) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void CopyTextureIntoCubemapFace (Texture2D textureRef, Cubemap cubemapRef, CubemapFace face) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void CopyCubemapFaceIntoTexture (Cubemap cubemapRef, CubemapFace face, Texture2D textureRef) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool ReformatCubemap (ref Cubemap cubemap, int width, int height, TextureFormat textureFormat, bool useMipmap, bool linear) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool ReformatTexture (ref Texture2D texture, int width, int height, TextureFormat textureFormat, bool useMipmap, bool linear) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetAnisoLevelNoDirty (Texture tex, int level) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetWrapModeNoDirty (Texture tex, TextureWrapMode u, TextureWrapMode v, TextureWrapMode w) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetMipMapBiasNoDirty (Texture tex, float bias) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetFilterModeNoDirty (Texture tex, FilterMode mode) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool DoesTextureStillNeedToBeCompressed (string assetPath) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool IsCubemapReadable (Cubemap cubemapRef) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void MarkCubemapReadable (Cubemap cubemapRef, bool readable) ;

}

public enum TextureImporterFormat
{
    Automatic = -1,
    
    [System.Obsolete("Use textureCompression property instead")]
    AutomaticCompressed = -1,
    
    [System.Obsolete("Use textureCompression property instead")]
    Automatic16bit = -2,
    
    [System.Obsolete("Use textureCompression property instead")]
    AutomaticTruecolor = -3,
    
    [System.Obsolete("Use crunchedCompression property instead")]
    AutomaticCrunched = -5,
    
    [System.Obsolete("HDR is handled automatically now")]
    AutomaticHDR = -6,
    
    [System.Obsolete("HDR is handled automatically now")]
    AutomaticCompressedHDR = -7,
    
    DXT1 = 10,
    
    DXT5 = 12,
    
    RGB16 = 7,
    
    RGB24 = 3,
    
    Alpha8 = 1,
    
    ARGB16 = 2,
    
    RGBA32 = 4,
    
    ARGB32 = 5,
    
    RGBA16 = 13,
    
    RGBAHalf = 17,
    
    BC4 = 26,
    
    BC5 = 27,
    
    BC6H = 24,
    
    BC7 = 25,
    
    DXT1Crunched = 28,
    
    DXT5Crunched = 29,
    
    PVRTC_RGB2 = 30,
    
    PVRTC_RGBA2 = 31,
    
    PVRTC_RGB4 = 32,
    
    PVRTC_RGBA4 = 33,
    
    ETC_RGB4 = 34,
    
    ATC_RGB4 = 35,
    
    ATC_RGBA8 = 36,
    
    
    EAC_R = 41,
    
    EAC_R_SIGNED = 42,
    
    EAC_RG = 43,
    
    EAC_RG_SIGNED = 44,
    
    ETC2_RGB4 = 45,
    
    ETC2_RGB4_PUNCHTHROUGH_ALPHA = 46,
    
    ETC2_RGBA8 = 47,
    
    ASTC_RGB_4x4 = 48,
    
    ASTC_RGB_5x5 = 49,
    
    ASTC_RGB_6x6 = 50,
    
    ASTC_RGB_8x8 = 51,
    
    ASTC_RGB_10x10 = 52,
    
    ASTC_RGB_12x12 = 53,
    
    ASTC_RGBA_4x4 = 54,
    
    ASTC_RGBA_5x5 = 55,
    
    ASTC_RGBA_6x6 = 56,
    
    ASTC_RGBA_8x8 = 57,
    
    ASTC_RGBA_10x10 = 58,
    
    ASTC_RGBA_12x12 = 59,
    
    ETC_RGB4Crunched = 64,
    
    ETC2_RGBA8Crunched = 65,
}

public enum TextureImporterMipFilter
{
    
    BoxFilter = 0,
    
    KaiserFilter = 1,
}

public enum TextureImporterGenerateCubemap
{
    
    [System.Obsolete ("This value is deprecated (use TextureImporter.textureShape instead).")]
    None = 0,
    
    Spheremap = 1,
    
    Cylindrical = 2,
    [System.Obsolete ("Obscure shperemap modes are not supported any longer (use TextureImporterGenerateCubemap.Spheremap instead).")]
    SimpleSpheremap = 3,
    [System.Obsolete ("Obscure shperemap modes are not supported any longer (use TextureImporterGenerateCubemap.Spheremap instead).")]
    NiceSpheremap = 4,
    
    FullCubemap = 5,
    
    AutoCubemap = 6
}

public enum TextureImporterNPOTScale
{
    
    None = 0,
    
    ToNearest = 1,
    
    ToLarger = 2,
    
    ToSmaller = 3,
}

public enum TextureImporterNormalFilter
{
    
    Standard = 0,
    
    Sobel = 1,
}

public enum TextureImporterAlphaSource
{
    
    None = 0,
    
    FromInput = 1,
    
    FromGrayScale = 2,
}

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
    
    CompressedHQ = 2,
    
    CompressedLQ = 3
}

public enum TextureResizeAlgorithm
{
    
    Mitchell = 0,
    
    Bilinear = 1
}

[Flags]
public enum TextureImporterShape
{
    Texture2D = 1 << 0,
    TextureCube = 1 << 1,
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
    
    UseBuildSettings = 0,
    
    Quality32Bit = 1,
    
    Quality16Bit = 2,
    
    Quality32BitDownscaled = 3
}

[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct SpriteMetaData
{
    public string name;
    public Rect rect;
    public int alignment;
    public Vector2 pivot;
    public Vector4 border;
}

public sealed partial class TextureImporter : AssetImporter
{
    [System.Obsolete ("textureFormat is no longer accessible at the TextureImporter level. For old 'simple' formats use the textureCompression property for the equivalent automatic choice (Uncompressed for TrueColor, Compressed and HQCommpressed for 16 bits). For platform specific formats use the [[PlatformTextureSettings]] API. Using this setter will setup various parameters to match the new automatic system as well as possible. Getter will return the last value set.")]
    public extern  TextureImporterFormat textureFormat
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    internal extern static string defaultPlatformName
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern int maxTextureSize
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern int compressionQuality
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool crunchedCompression
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool allowAlphaSplitting
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  AndroidETC2FallbackOverride androidETC2FallbackOverride
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  TextureImporterCompression textureCompression
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  TextureImporterAlphaSource alphaSource
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [System.Obsolete ("Use UnityEditor.TextureImporter.alphaSource instead.")]
    public bool grayscaleToAlpha
        {
            get { return alphaSource == TextureImporterAlphaSource.FromGrayScale; }
            set { if (value) alphaSource = TextureImporterAlphaSource.FromGrayScale; else alphaSource = TextureImporterAlphaSource.FromInput; }
        }
    
    
    
    [System.Obsolete ("Use UnityEditor.TextureImporter.GetPlatformTextureSettings() instead.")]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public bool GetAllowsAlphaSplitting () ;

    [System.Obsolete ("Use UnityEditor.TextureImporter.SetPlatformTextureSettings() instead.")]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetAllowsAlphaSplitting (bool flag) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public bool GetPlatformTextureSettings (string platform, out int maxTextureSize, out TextureImporterFormat textureFormat, out int compressionQuality, out bool etc1AlphaSplitEnabled) ;

    public bool GetPlatformTextureSettings(string platform, out int maxTextureSize, out TextureImporterFormat textureFormat, out int compressionQuality)
        {
            bool etc1AlphaSplitEnabled = false;
            return GetPlatformTextureSettings(platform, out maxTextureSize, out textureFormat, out compressionQuality, out etc1AlphaSplitEnabled);
        }
    
    
    public bool GetPlatformTextureSettings(string platform, out int maxTextureSize, out TextureImporterFormat textureFormat)
        {
            int compressionQuality = 0;
            bool etc1AlphaSplitEnabled = false;
            return GetPlatformTextureSettings(platform, out maxTextureSize, out textureFormat, out compressionQuality, out etc1AlphaSplitEnabled);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void Internal_GetPlatformTextureSettings (string platform, TextureImporterPlatformSettings dest) ;

    public TextureImporterPlatformSettings GetPlatformTextureSettings(string platform)
        {
            TextureImporterPlatformSettings dest = new TextureImporterPlatformSettings();
            Internal_GetPlatformTextureSettings(platform, dest);
            return dest;
        }
    
    
    public TextureImporterPlatformSettings GetDefaultPlatformTextureSettings()
        {
            return GetPlatformTextureSettings(TextureImporterInspector.s_DefaultPlatformName);
        }
    
    
    public TextureImporterFormat GetAutomaticFormat(string platform)
        {
            TextureImporterSettings settings = new TextureImporterSettings();
            ReadTextureSettings(settings);
            TextureImporterPlatformSettings platformSettings = GetPlatformTextureSettings(platform);

            List<BuildPlatform> validPlatforms = BuildPlatforms.instance.GetValidPlatforms();
            foreach (BuildPlatform bp in validPlatforms)
            {
                if (bp.name == platform)
                {
                    return TextureImporter.FormatFromTextureParameters(settings,
                        platformSettings,
                        DoesSourceTextureHaveAlpha(),
                        IsSourceTextureHDR(),
                        bp.defaultTarget);

                }
            }

            return TextureImporterFormat.Automatic;
        }
    
    
    [System.Obsolete ("Use UnityEditor.TextureImporter.SetPlatformTextureSettings(TextureImporterPlatformSettings) instead.")]
public void SetPlatformTextureSettings(string platform, int maxTextureSize, TextureImporterFormat textureFormat, int compressionQuality, bool allowsAlphaSplit)
        {
            TextureImporterPlatformSettings dest = new TextureImporterPlatformSettings();
            Internal_GetPlatformTextureSettings(platform, dest);
            dest.overridden = true;
            dest.maxTextureSize = maxTextureSize;
            dest.format = textureFormat;
            dest.compressionQuality = compressionQuality;
            dest.allowsAlphaSplitting = allowsAlphaSplit;
            SetPlatformTextureSettings(dest);
        }
    
    
    [System.Obsolete ("Use UnityEditor.TextureImporter.SetPlatformTextureSettings(TextureImporterPlatformSettings) instead.")]
[uei.ExcludeFromDocs]
public void SetPlatformTextureSettings (string platform, int maxTextureSize, TextureImporterFormat textureFormat) {
    bool allowsAlphaSplit = false;
    SetPlatformTextureSettings ( platform, maxTextureSize, textureFormat, allowsAlphaSplit );
}

[System.Obsolete ("Use UnityEditor.TextureImporter.SetPlatformTextureSettings(TextureImporterPlatformSettings) instead.")]
public void SetPlatformTextureSettings(string platform, int maxTextureSize, TextureImporterFormat textureFormat, [uei.DefaultValue("false")]  bool allowsAlphaSplit )
        {
            TextureImporterPlatformSettings dest = new TextureImporterPlatformSettings();
            Internal_GetPlatformTextureSettings(platform, dest);
            dest.overridden = true;
            dest.maxTextureSize = maxTextureSize;
            dest.format = textureFormat;
            dest.allowsAlphaSplitting = allowsAlphaSplit;
            SetPlatformTextureSettings(dest);
        }

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetPlatformTextureSettings (TextureImporterPlatformSettings platformSettings) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void ClearPlatformTextureSettings (string platform) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  TextureImporterFormat FormatFromTextureParameters (TextureImporterSettings settings, TextureImporterPlatformSettings platformSettings, bool doesTextureContainAlpha, bool sourceWasHDR, BuildTarget destinationPlatform) ;

    public extern TextureImporterGenerateCubemap generateCubemap
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern TextureImporterNPOTScale npotScale
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool isReadable
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool mipmapEnabled
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool borderMipmap
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool sRGBTexture
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool mipMapsPreserveCoverage
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float alphaTestReferenceValue
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern TextureImporterMipFilter mipmapFilter
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool fadeout
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern int mipmapFadeDistanceStart
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern int mipmapFadeDistanceEnd
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [System.Obsolete ("generateMipsInLinearSpace Property deprecated. Mipmaps are always generated in linear space.")]
    public extern  bool generateMipsInLinearSpace
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [System.Obsolete ("correctGamma Property deprecated. Mipmaps are always generated in linear space.")]
    public extern  bool correctGamma
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [System.Obsolete ("linearTexture Property deprecated. Use sRGBTexture instead.")]
    public bool linearTexture { get { return !sRGBTexture; } set { sRGBTexture = !value; } }
    
    
    [System.Obsolete ("normalmap Property deprecated. Check [[TextureImporterSettings.textureType]] instead. Getter will work as expected. Setter will set textureType to NormalMap if true, nothing otherwise.")]
            public bool normalmap
        {
            get { return textureType == TextureImporterType.NormalMap; }
            set { if (value) textureType = TextureImporterType.NormalMap; else textureType = TextureImporterType.Default; }
        }
    
    
    [System.Obsolete ("lightmap Property deprecated. Check [[TextureImporterSettings.textureType]] instead. Getter will work as expected. Setter will set textureType to Lightmap if true, nothing otherwise.")]
            public bool lightmap
        {
            get { return textureType == TextureImporterType.Lightmap; }
            set { if (value) textureType = TextureImporterType.Lightmap; else textureType = TextureImporterType.Default; }
        }
    
    
    public extern bool convertToNormalmap
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern TextureImporterNormalFilter normalmapFilter
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float heightmapScale
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern int anisoLevel
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern FilterMode filterMode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern TextureWrapMode wrapMode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern TextureWrapMode wrapModeU
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern TextureWrapMode wrapModeV
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern TextureWrapMode wrapModeW
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float mipMapBias
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool alphaIsTransparency
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool qualifiesForSpritePacking
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  SpriteImportMode spriteImportMode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  SpriteMetaData[] spritesheet
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  string spritePackingTag
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float spritePixelsPerUnit
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [System.Obsolete("Use spritePixelsPerUnit property instead.")]
    public extern float spritePixelsToUnits
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public Vector2 spritePivot
    {
        get { Vector2 tmp; INTERNAL_get_spritePivot(out tmp); return tmp;  }
        set { INTERNAL_set_spritePivot(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_spritePivot (out Vector2 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_spritePivot (ref Vector2 value) ;

    public Vector4 spriteBorder
    {
        get { Vector4 tmp; INTERNAL_get_spriteBorder(out tmp); return tmp;  }
        set { INTERNAL_set_spriteBorder(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_spriteBorder (out Vector4 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_spriteBorder (ref Vector4 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void GetWidthAndHeight (ref int width, ref int height) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal bool IsSourceTextureHDR () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool IsTextureFormatETC1Compression (TextureFormat fmt) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool IsETC1SupportedByBuildTarget (BuildTarget target) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public bool DoesSourceTextureHaveAlpha () ;

    [System.Obsolete ("DoesSourceTextureHaveColor always returns true in Unity.")]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public bool DoesSourceTextureHaveColor () ;

    public extern  TextureImporterType textureType
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  TextureImporterShape textureShape
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void ReadTextureSettings (TextureImporterSettings dest) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetTextureSettings (TextureImporterSettings src) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal string GetImportWarnings () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void ReadTextureImportInstructions (BuildTarget target, out TextureFormat desiredFormat, out ColorSpace colorSpace, out int compressionQuality) ;

}

[System.Serializable]
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
            int  m_SpriteGenerateFallbackPhysicsShape;
    
    
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
            int     m_FilterMode;
    [SerializeField]
            int     m_Aniso;
    [SerializeField]
            float   m_MipBias;
    [SerializeField]
            int m_WrapU;
    [SerializeField]
            int m_WrapV;
    [SerializeField]
            int m_WrapW;
    
    
    [SerializeField]
            int m_NormalMap;
    [SerializeField]
            int m_TextureFormat;
    [SerializeField]
            int m_MaxTextureSize;
    [SerializeField]
            int m_Lightmap;
    [SerializeField]
            int m_CompressionQuality;
    [SerializeField]
            int m_LinearTexture;
    [SerializeField]
            int m_GrayScaleToAlpha;
    [SerializeField]
            int m_RGBM;
    [SerializeField]
            int m_CubemapConvolutionSteps;
    [SerializeField]
            float m_CubemapConvolutionExponent;
    
    
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
            public bool readable
        {
            get {return m_IsReadable != 0; }
            set { m_IsReadable = value ? 1 : 0; }
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
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool Equal (TextureImporterSettings a, TextureImporterSettings b) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void CopyTo (TextureImporterSettings target) ;

    [Obsolete("ApplyTextureType(TextureImporterType, bool) is deprecated, use ApplyTextureType(TextureImporterType)")]
    public void ApplyTextureType(TextureImporterType type, bool applyAll)
        {
            Internal_ApplyTextureType(this, type);
        }
    
    
    public void ApplyTextureType(TextureImporterType type)
        {
            Internal_ApplyTextureType(this, type);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_ApplyTextureType (TextureImporterSettings s, TextureImporterType type) ;

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
    [System.Obsolete ("Texture max size can only be overridden on a per platform basis. See [[TextureImporter.maxTextureSize]] for Default platform or [[TextureImporterPlatformSettings]]")]
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
    [System.Obsolete ("Texture compression can only be overridden on a per platform basis. See [[TextureImporter.compressionQuality]] for Default platform or [[TextureImporterPlatformSettings]]")]
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

[System.Serializable]
public sealed partial class TextureImporterPlatformSettings
{
    [SerializeField]
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
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void CopyTo (TextureImporterPlatformSettings target) ;

}

}
