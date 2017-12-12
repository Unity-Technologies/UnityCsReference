// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using uei = UnityEngine.Internal;

using TextureDimension = UnityEngine.Rendering.TextureDimension;

namespace UnityEngine
{
    [NativeHeader("Runtime/Graphics/Texture.h")]
    public partial class Texture : Object
    {
        protected Texture() {}

        extern public static int masterTextureLimit { get; set; }

        [NativeProperty("AnisoLimit")] extern public static AnisotropicFiltering anisotropicFiltering { get; set; }
        [NativeName("SetGlobalAnisoLimits")] extern public static void SetGlobalAnisotropicFilteringLimits(int forcedMin, int globalMax);

        extern private int GetDataWidth();
        extern private int GetDataHeight();
        extern private TextureDimension GetDimension();

        // Note: not implemented setters in base class since some classes do need to actually implement them (e.g. RenderTexture)
        virtual public int width  { get { return GetDataWidth(); }  set { throw new NotImplementedException(); } }
        virtual public int height { get { return GetDataHeight(); } set { throw new NotImplementedException(); } }
        virtual public TextureDimension dimension { get { return GetDimension(); } set { throw new NotImplementedException(); } }

        // Note: getter for "wrapMode" returns the U mode on purpose
        extern public TextureWrapMode wrapMode  {[NativeName("GetWrapModeU")] get; set; }

        extern public TextureWrapMode wrapModeU { get; set; }
        extern public TextureWrapMode wrapModeV { get; set; }
        extern public TextureWrapMode wrapModeW { get; set; }
        extern public FilterMode filterMode { get; set; }
        extern public int anisoLevel { get; set; }
        extern public float mipMapBias { get; set; }
        extern public Vector2 texelSize {[NativeName("GetNpotTexelSize")] get; }

        extern public IntPtr GetNativeTexturePtr();
        [Obsolete("Use GetNativeTexturePtr instead.", false)]
        public int GetNativeTextureID() { return (int)GetNativeTexturePtr(); }
    }

    [NativeHeader("Runtime/Graphics/Texture2D.h")]
    [NativeHeader("Runtime/Graphics/GeneratedTextures.h")]
    public sealed partial class Texture2D : Texture
    {
        extern public int mipmapCount {[NativeName("CountDataMipmaps")] get; }
        extern public TextureFormat format {[NativeName("GetTextureFormat")] get; }

        [StaticAccessor("builtintex", StaticAccessorType.DoubleColon)] extern public static Texture2D whiteTexture { get; }
        [StaticAccessor("builtintex", StaticAccessorType.DoubleColon)] extern public static Texture2D blackTexture { get; }

        extern public void Compress(bool highQuality);

        [FreeFunction("Texture2DScripting::Create")]
        extern private static bool Internal_CreateImpl([Writable] Texture2D mono, int w, int h, TextureFormat format, bool mipmap, bool linear, IntPtr nativeTex);
        private static void Internal_Create([Writable] Texture2D mono, int w, int h, TextureFormat format, bool mipmap, bool linear, IntPtr nativeTex)
        {
            if (!Internal_CreateImpl(mono, w, h, format, mipmap, linear, nativeTex))
                throw new UnityException("Failed to create texture because of invalid parameters.");
        }

        [NativeName("GetIsReadable")]    extern private bool  IsReadable();
        [NativeName("Apply")]            extern private void  ApplyImpl(bool updateMipmaps, bool makeNoLongerReadable);
        [NativeName("Resize")]           extern private bool  ResizeImpl(int width, int height);
        [NativeName("SetPixel")]         extern private void  SetPixelImpl(int image, int x, int y, Color color);
        [NativeName("GetPixel")]         extern private Color GetPixelImpl(int image, int x, int y);
        [NativeName("GetPixelBilinear")] extern private Color GetPixelBilinearImpl(int image, float x, float y);

        [FreeFunction(Name = "Texture2DScripting::SetPixels", HasExplicitThis = true)]
        extern private void SetPixelsImpl(int x, int y, int w, int h, Color[] pixel, int miplevel, int frame);

        [FreeFunction(Name = "Texture2DScripting::LoadRawData", HasExplicitThis = true)]
        extern private bool LoadRawTextureDataImpl(IntPtr data, int size);

        [FreeFunction(Name = "Texture2DScripting::LoadRawData", HasExplicitThis = true)]
        extern private bool LoadRawTextureDataImplArray(byte[] data);
    }

    [NativeHeader("Runtime/Graphics/CubemapTexture.h")]
    public sealed partial class Cubemap : Texture
    {
        extern public int mipmapCount {[NativeName("CountDataMipmaps")] get; }
        extern public TextureFormat format {[NativeName("GetTextureFormat")] get; }

        [FreeFunction("CubemapScripting::Create")]
        extern private static bool Internal_CreateImpl([Writable] Cubemap mono, int ext, TextureFormat format, bool mipmap, IntPtr nativeTex);
        private static void Internal_Create([Writable] Cubemap mono, int ext, TextureFormat format, bool mipmap, IntPtr nativeTex)
        {
            if (!Internal_CreateImpl(mono, ext, format, mipmap, nativeTex))
                throw new UnityException("Failed to create texture because of invalid parameters.");
        }

        [FreeFunction(Name = "CubemapScripting::Apply", HasExplicitThis = true)]
        extern private void ApplyImpl(bool updateMipmaps, bool makeNoLongerReadable);

        [NativeName("GetIsReadable")] extern private bool  IsReadable();
        [NativeName("SetPixel")]      extern private void  SetPixelImpl(int image, int x, int y, Color color);
        [NativeName("GetPixel")]      extern private Color GetPixelImpl(int image, int x, int y);

        [NativeName("FixupEdges")]    extern public   void  SmoothEdges([uei.DefaultValue("1")] int smoothRegionWidthInPixels);
        public void SmoothEdges() { SmoothEdges(1); }
    }

    [NativeHeader("Runtime/Graphics/Texture3D.h")]
    public sealed partial class Texture3D : Texture
    {
        extern public int depth {[NativeName("GetTextureLayerCount")] get; }
        extern public TextureFormat format {[NativeName("GetTextureFormat")] get; }

        [NativeName("GetIsReadable")] extern private bool  IsReadable();

        [FreeFunction("Texture3DScripting::Create")]
        extern private static bool Internal_CreateImpl([Writable] Texture3D mono, int w, int h, int d, TextureFormat format, bool mipmap);
        private static void Internal_Create([Writable] Texture3D mono, int w, int h, int d, TextureFormat format, bool mipmap)
        {
            if (!Internal_CreateImpl(mono, w, h, d, format, mipmap))
                throw new UnityException("Failed to create texture because of invalid parameters.");
        }

        [FreeFunction(Name = "Texture3DScripting::Apply", HasExplicitThis = true)]
        extern private void ApplyImpl(bool updateMipmaps, bool makeNoLongerReadable);
    }

    [NativeHeader("Runtime/Graphics/Texture2DArray.h")]
    public sealed partial class Texture2DArray : Texture
    {
        extern public int depth {[NativeName("GetTextureLayerCount")] get; }
        extern public TextureFormat format {[NativeName("GetTextureFormat")] get; }

        [NativeName("GetIsReadable")] extern private bool  IsReadable();

        [FreeFunction("Texture2DArrayScripting::Create")]
        extern private static bool Internal_CreateImpl([Writable] Texture2DArray mono, int w, int h, int d, TextureFormat format, bool mipmap, bool linear);
        private static void Internal_Create([Writable] Texture2DArray mono, int w, int h, int d, TextureFormat format, bool mipmap, bool linear)
        {
            if (!Internal_CreateImpl(mono, w, h, d, format, mipmap, linear))
                throw new UnityException("Failed to create 2D array texture because of invalid parameters.");
        }

        [FreeFunction(Name = "Texture2DArrayScripting::Apply", HasExplicitThis = true)]
        extern private void ApplyImpl(bool updateMipmaps, bool makeNoLongerReadable);
    }

    [NativeHeader("Runtime/Graphics/CubemapArrayTexture.h")]
    public sealed partial class CubemapArray : Texture
    {
        extern public int cubemapCount { get; }
        extern public TextureFormat format {[NativeName("GetTextureFormat")] get; }

        [NativeName("GetIsReadable")] extern private bool  IsReadable();

        [FreeFunction("CubemapArrayScripting::Create")]
        extern private static bool Internal_CreateImpl([Writable] CubemapArray mono, int ext, int count, TextureFormat format, bool mipmap, bool linear);
        private static void Internal_Create([Writable] CubemapArray mono, int ext, int count, TextureFormat format, bool mipmap, bool linear)
        {
            if (!Internal_CreateImpl(mono, ext, count, format, mipmap, linear))
                throw new UnityException("Failed to create cubemap array texture because of invalid parameters.");
        }

        [FreeFunction(Name = "CubemapArrayScripting::Apply", HasExplicitThis = true)]
        extern private void ApplyImpl(bool updateMipmaps, bool makeNoLongerReadable);
    }

    [NativeHeader("Runtime/Graphics/RenderTexture.h")]
    [NativeHeader("Runtime/Graphics/GraphicsScriptBindings.h")]
    [NativeHeader("Runtime/Camera/Camera.h")]
    public partial class RenderTexture : Texture
    {
        override extern public int width  { get; set; }
        override extern public int height { get; set; }
        override extern public TextureDimension dimension { get; set; }

        [NativeProperty("MipMap")]          extern public bool useMipMap { get; set; }
        [NativeProperty("SRGBReadWrite")]   extern public bool sRGB { get; }
        [NativeProperty("ColorFormat")]     extern public RenderTextureFormat format { get; set; }
        [NativeProperty("VRUsage")]         extern public VRTextureUsage vrUsage { get; set; }
        [NativeProperty("Memoryless")]      extern public RenderTextureMemoryless memorylessMode { get; set; }

        extern public bool autoGenerateMips { get; set; }
        extern public int  volumeDepth { get; set; }
        extern public int  antiAliasing { get; set; }
        extern public bool bindTextureMS { get; set; }
        extern public bool enableRandomWrite { get; set; }
        extern public bool useDynamicScale { get; set; }


        // for some reason we are providing isPowerOfTwo setter which is empty (i dont know what the intent is/was)
        extern private bool GetIsPowerOfTwo();
        public bool isPowerOfTwo { get { return GetIsPowerOfTwo(); } set {} }

        extern public bool Create();
        extern public void Release();
        extern public bool IsCreated();
        extern public void GenerateMips();
        extern public void ConvertToEquirect(RenderTexture equirect, Camera.MonoOrStereoscopicEye eye = Camera.MonoOrStereoscopicEye.Mono);

        extern internal void SetSRGBReadWrite(bool srgb);

        [FreeFunction("RenderTextureScripting::Create")] extern private static void Internal_Create([Writable] RenderTexture rt);
    }

    public partial class RenderTexture : Texture
    {
        [Obsolete("Use RenderTexture.dimension instead.", false)]
        public bool isCubemap
        {
            get { return dimension == TextureDimension.Cube; }
            set { dimension = value ? TextureDimension.Cube : TextureDimension.Tex2D; }
        }

        [Obsolete("Use RenderTexture.dimension instead.", false)]
        public bool isVolume
        {
            get { return dimension == TextureDimension.Tex3D; }
            set { dimension = value ? TextureDimension.Tex3D : TextureDimension.Tex2D; }
        }
    }
}
