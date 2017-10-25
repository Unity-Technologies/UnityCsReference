// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using uei = UnityEngine.Internal;

namespace UnityEngine
{
    public struct RenderTextureDescriptor
    {
        public int width { get; set; }
        public int height { get; set; }
        public int msaaSamples { get; set; }
        public int volumeDepth { get; set; }

        private int _bindMS;

        public bool bindMS
        {
            get { return _bindMS != 0; }
            set { _bindMS = value ? 1 : 0; }
        }
        public RenderTextureFormat colorFormat { get; set; }
        private int _depthBufferBits;
        private static int[] depthFormatBits = new int[] { 0, 16, 24 };
        public int depthBufferBits
        {
            get { return depthFormatBits[_depthBufferBits]; }
            set
            {
                if (value <= 0)
                    _depthBufferBits = 0;
                else if (value <= 16)
                    _depthBufferBits = 1;
                else
                    _depthBufferBits = 2;
            }
        }

        public Rendering.TextureDimension dimension { get; set; }
        public Rendering.ShadowSamplingMode shadowSamplingMode { get; set; }
        public VRTextureUsage vrUsage { get; set; }
        private RenderTextureCreationFlags _flags;
        public RenderTextureCreationFlags flags { get { return _flags; } }
        public RenderTextureMemoryless memoryless { get; set; }
        public RenderTextureDescriptor(int width, int height) : this(width, height, RenderTextureFormat.Default, 0) {}
        public RenderTextureDescriptor(int width, int height, RenderTextureFormat colorFormat) : this(width, height, colorFormat, 0) {}
        public RenderTextureDescriptor(int width, int height, RenderTextureFormat colorFormat, int depthBufferBits) : this()
        {
            this.width = width;
            this.height = height;
            volumeDepth = 1;
            msaaSamples = 1;
            this.colorFormat = colorFormat;
            this.depthBufferBits = depthBufferBits;
            dimension = Rendering.TextureDimension.Tex2D;
            shadowSamplingMode = Rendering.ShadowSamplingMode.None;
            vrUsage = VRTextureUsage.None;
            _flags = RenderTextureCreationFlags.AutoGenerateMips | RenderTextureCreationFlags.AllowVerticalFlip;
            memoryless = RenderTextureMemoryless.None;
        }

        private void SetOrClearRenderTextureCreationFlag(bool value, RenderTextureCreationFlags flag)
        {
            if (value)
            {
                _flags |= flag;
            }
            else
            {
                _flags &= ~flag;
            }
        }

        public bool sRGB
        {
            get { return (_flags & RenderTextureCreationFlags.SRGB) != 0; }
            set { SetOrClearRenderTextureCreationFlag(value, RenderTextureCreationFlags.SRGB); }
        }

        public bool useMipMap
        {
            get { return (_flags & RenderTextureCreationFlags.MipMap) != 0; }
            set { SetOrClearRenderTextureCreationFlag(value, RenderTextureCreationFlags.MipMap); }
        }

        public bool autoGenerateMips
        {
            get { return (_flags & RenderTextureCreationFlags.AutoGenerateMips) != 0; }
            set { SetOrClearRenderTextureCreationFlag(value, RenderTextureCreationFlags.AutoGenerateMips); }
        }

        public bool enableRandomWrite
        {
            get { return (_flags & RenderTextureCreationFlags.EnableRandomWrite) != 0; }
            set { SetOrClearRenderTextureCreationFlag(value, RenderTextureCreationFlags.EnableRandomWrite); }
        }

        internal bool createdFromScript
        {
            get { return (_flags & RenderTextureCreationFlags.CreatedFromScript) != 0; }
            set { SetOrClearRenderTextureCreationFlag(value, RenderTextureCreationFlags.CreatedFromScript); }
        }

        internal bool useDynamicScale
        {
            get { return (_flags & RenderTextureCreationFlags.DynamicallyScalable) != 0; }
            set { SetOrClearRenderTextureCreationFlag(value, RenderTextureCreationFlags.DynamicallyScalable); }
        }
    }

    public partial class RenderTexture : Texture
    {
        public RenderTexture(RenderTextureDescriptor desc)
        {
            ValidateRenderTextureDesc(desc);
            Internal_Create(this);
            SetRenderTextureDescriptor(desc);
        }

        public RenderTexture(RenderTexture textureToCopy)
        {
            if (textureToCopy == null)
                throw new ArgumentNullException("textureToCopy");

            ValidateRenderTextureDesc(textureToCopy.descriptor);
            Internal_Create(this);
            SetRenderTextureDescriptor(textureToCopy.descriptor);
        }

        public RenderTexture(int width, int height, int depth, [uei.DefaultValue("RenderTextureFormat.Default")] RenderTextureFormat format, [uei.DefaultValue("RenderTextureReadWrite.Default")] RenderTextureReadWrite readWrite)
        {
            Internal_Create(this);
            this.width = width; this.height = height; this.depth = depth; this.format = format;

            bool defaultSRGB = QualitySettings.activeColorSpace == ColorSpace.Linear;
            SetSRGBReadWrite(readWrite == RenderTextureReadWrite.Default ? defaultSRGB : readWrite == RenderTextureReadWrite.sRGB);
        }

        public RenderTexture(int width, int height, int depth, RenderTextureFormat format)
            : this(width, height, depth, format, RenderTextureReadWrite.Default)
        {
        }

        public RenderTexture(int width, int height, int depth)
            : this(width, height, depth, RenderTextureFormat.Default, RenderTextureReadWrite.Default)
        {
        }

        public RenderTextureDescriptor descriptor
        {
            get { return GetDescriptor(); }
            set { ValidateRenderTextureDesc(value); SetRenderTextureDescriptor(value); }
        }


        private static void ValidateRenderTextureDesc(RenderTextureDescriptor desc)
        {
            if (desc.width <= 0)
            {
                throw new ArgumentException("RenderTextureDesc width must be greater than zero.", "desc.width");
            }

            if (desc.height <= 0)
            {
                throw new ArgumentException("RenderTextureDesc height must be greater than zero.", "desc.height");
            }

            if (desc.volumeDepth <= 0)
            {
                throw new ArgumentException("RenderTextureDesc volumeDepth must be greater than zero.", "desc.volumeDepth");
            }

            if (desc.msaaSamples != 1 && desc.msaaSamples != 2 && desc.msaaSamples != 4 && desc.msaaSamples != 8)
            {
                throw new ArgumentException("RenderTextureDesc msaaSamples must be 1, 2, 4, or 8.", "desc.msaaSamples");
            }

            if (desc.depthBufferBits != 0 && desc.depthBufferBits != 16 && desc.depthBufferBits != 24)
            {
                throw new ArgumentException("RenderTextureDesc depthBufferBits must be 0, 16, or 24.", "desc.depthBufferBits");
            }
        }
    }

    public partial class RenderTexture : Texture
    {
        public static RenderTexture GetTemporary(RenderTextureDescriptor desc)
        {
            ValidateRenderTextureDesc(desc); desc.createdFromScript = true;
            return GetTemporary_Internal(desc);
        }

        // in old bindings "default args" were expanded into overloads and we must mimic that when migrating to new bindings
        // to keep things sane we will do internal methods WITH default args and do overloads that simply call it

        private static RenderTexture GetTemporaryImpl(int width, int height, int depthBuffer = 0,
            RenderTextureFormat format = RenderTextureFormat.Default, RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default,
            int antiAliasing = 1, RenderTextureMemoryless memorylessMode = RenderTextureMemoryless.None,
            VRTextureUsage vrUsage = VRTextureUsage.None, bool useDynamicScale = false
            )
        {
            RenderTextureDescriptor desc = new RenderTextureDescriptor(width, height, format, depthBuffer);
            desc.sRGB = (readWrite != RenderTextureReadWrite.Linear);
            desc.msaaSamples = antiAliasing;
            desc.memoryless = memorylessMode;
            desc.vrUsage = vrUsage;
            desc.useDynamicScale = useDynamicScale;

            return GetTemporary(desc);
        }

        // most detailed overload: use it to specify default values for docs
        public static RenderTexture GetTemporary(int width, int height,
            [uei.DefaultValue("0")] int depthBuffer, [uei.DefaultValue("RenderTextureFormat.Default")] RenderTextureFormat format,
            [uei.DefaultValue("RenderTextureReadWrite.Default")] RenderTextureReadWrite readWrite, [uei.DefaultValue("1")] int antiAliasing ,
            [uei.DefaultValue("RenderTextureMemoryless.None")] RenderTextureMemoryless memorylessMode,
            [uei.DefaultValue("VRTextureUsage.None")] VRTextureUsage vrUsage, [uei.DefaultValue("false")] bool useDynamicScale
            )
        {
            return GetTemporaryImpl(width, height, depthBuffer, format, readWrite, antiAliasing, memorylessMode, vrUsage, useDynamicScale);
        }

        // the rest
        public static RenderTexture GetTemporary(int width, int height, int depthBuffer, RenderTextureFormat format, RenderTextureReadWrite readWrite, int antiAliasing, RenderTextureMemoryless memorylessMode, VRTextureUsage vrUsage)
        {
            return GetTemporaryImpl(width, height, depthBuffer, format, readWrite, antiAliasing, memorylessMode, vrUsage);
        }

        public static RenderTexture GetTemporary(int width, int height, int depthBuffer, RenderTextureFormat format, RenderTextureReadWrite readWrite, int antiAliasing, RenderTextureMemoryless memorylessMode)
        {
            return GetTemporaryImpl(width, height, depthBuffer, format, readWrite, antiAliasing, memorylessMode);
        }

        public static RenderTexture GetTemporary(int width, int height, int depthBuffer, RenderTextureFormat format, RenderTextureReadWrite readWrite, int antiAliasing)
        {
            return GetTemporaryImpl(width, height, depthBuffer, format, readWrite, antiAliasing);
        }

        public static RenderTexture GetTemporary(int width, int height, int depthBuffer, RenderTextureFormat format, RenderTextureReadWrite readWrite)
        {
            return GetTemporaryImpl(width, height, depthBuffer, format, readWrite);
        }

        public static RenderTexture GetTemporary(int width, int height, int depthBuffer, RenderTextureFormat format)
        {
            return GetTemporaryImpl(width, height, depthBuffer, format);
        }

        public static RenderTexture GetTemporary(int width, int height, int depthBuffer)
        {
            return GetTemporaryImpl(width, height, depthBuffer);
        }

        public static RenderTexture GetTemporary(int width, int height)
        {
            return GetTemporaryImpl(width, height);
        }
    }

    public partial class Texture : Object
    {
        internal UnityException CreateNonReadableException(Texture t)
        {
            return new UnityException(
                String.Format("Texture '{0}' is not readable, the texture memory can not be accessed from scripts. You can make the texture readable in the Texture Import Settings.", t.name)
                );
        }
    }


    public partial class Texture2D : Texture
    {
        internal Texture2D(int width, int height, TextureFormat format, bool mipmap, bool linear, IntPtr nativeTex)
        {
            Internal_Create(this, width, height, format, mipmap, linear, nativeTex);
        }

        public Texture2D(int width, int height, [uei.DefaultValue("TextureFormat.RGBA32")] TextureFormat format, [uei.DefaultValue("true")] bool mipmap, [uei.DefaultValue("false")] bool linear)
            : this(width, height, format, mipmap, linear, IntPtr.Zero)
        {
        }

        public Texture2D(int width, int height, TextureFormat format, bool mipmap)
            : this(width, height, format, mipmap, false, IntPtr.Zero)
        {
        }

        public Texture2D(int width, int height)
            : this(width, height, TextureFormat.RGBA32, true, false, IntPtr.Zero)
        {
        }

        public static Texture2D CreateExternalTexture(int width, int height, TextureFormat format, bool mipmap, bool linear, IntPtr nativeTex)
        {
            if (nativeTex == IntPtr.Zero)
                throw new ArgumentException("nativeTex can not be null");
            return new Texture2D(width, height, format, mipmap, linear, nativeTex);
        }

        public void SetPixel(int x, int y, Color color)
        {
            if (!IsReadable()) throw CreateNonReadableException(this);
            SetPixelImpl(0, x, y, color);
        }

        public void SetPixels(int x, int y, int blockWidth, int blockHeight, Color[] colors, [uei.DefaultValue("0")] int miplevel)
        {
            if (!IsReadable()) throw CreateNonReadableException(this);
            SetPixelsImpl(x, y, blockWidth, blockHeight, colors, miplevel, 0);
        }

        public void SetPixels(int x, int y, int blockWidth, int blockHeight, Color[] colors)
        {
            SetPixels(x, y, blockWidth, blockHeight, colors, 0);
        }

        public void SetPixels(Color[] colors, [uei.DefaultValue("0")] int miplevel)
        {
            int w = width >> miplevel; if (w < 1) w = 1;
            int h = height >> miplevel; if (h < 1) h = 1;
            SetPixels(0, 0, w, h, colors, miplevel);
        }

        public void SetPixels(Color[] colors)
        {
            SetPixels(0, 0, width, height, colors, 0);
        }

        public Color GetPixel(int x, int y)
        {
            if (!IsReadable()) throw CreateNonReadableException(this);
            return GetPixelImpl(0, x, y);
        }

        public Color GetPixelBilinear(float x, float y)
        {
            if (!IsReadable()) throw CreateNonReadableException(this);
            return GetPixelBilinearImpl(0, x, y);
        }

        public void LoadRawTextureData(IntPtr data, int size)
        {
            if (!IsReadable()) throw CreateNonReadableException(this);
            if (data == IntPtr.Zero || size == 0) { Debug.LogError("No texture data provided to LoadRawTextureData", this); return; }
            if (!LoadRawTextureDataImpl(data, size))
                throw new UnityException("LoadRawTextureData: not enough data provided (will result in overread).");
        }

        public void LoadRawTextureData(byte[] data)
        {
            if (!IsReadable()) throw CreateNonReadableException(this);
            if (data == null || data.Length == 0) { Debug.LogError("No texture data provided to LoadRawTextureData", this); return; }
            if (!LoadRawTextureDataImplArray(data))
                throw new UnityException("LoadRawTextureData: not enough data provided (will result in overread).");
        }

        public void Apply([uei.DefaultValue("true")] bool updateMipmaps, [uei.DefaultValue("false")] bool makeNoLongerReadable)
        {
            if (!IsReadable()) throw CreateNonReadableException(this);
            ApplyImpl(updateMipmaps, makeNoLongerReadable);
        }

        public void Apply(bool updateMipmaps)   { Apply(updateMipmaps, false); }
        public void Apply()                     { Apply(true, false); }

        public bool Resize(int width, int height)
        {
            if (!IsReadable()) throw CreateNonReadableException(this);
            return ResizeImpl(width, height);
        }
    }

    public sealed partial class Cubemap : Texture
    {
        internal Cubemap(int ext, TextureFormat format, bool mipmap, IntPtr nativeTex)
        {
            Internal_Create(this, ext, format, mipmap, nativeTex);
        }

        public Cubemap(int ext, TextureFormat format, bool mipmap) : this(ext, format, mipmap, IntPtr.Zero)
        {
        }

        public static Cubemap CreateExternalTexture(int ext, TextureFormat format, bool mipmap, IntPtr nativeTex)
        {
            if (nativeTex == IntPtr.Zero)
                throw new ArgumentException("nativeTex can not be null");
            return new Cubemap(ext, format, mipmap, nativeTex);
        }

        public void SetPixel(CubemapFace face, int x, int y, Color color)
        {
            if (!IsReadable()) throw CreateNonReadableException(this);
            SetPixelImpl((int)face, x, y, color);
        }

        public Color GetPixel(CubemapFace face, int x, int y)
        {
            if (!IsReadable()) throw CreateNonReadableException(this);
            return GetPixelImpl((int)face, x, y);
        }

        public void Apply([uei.DefaultValue("true")] bool updateMipmaps, [uei.DefaultValue("false")] bool makeNoLongerReadable)
        {
            ApplyImpl(updateMipmaps, makeNoLongerReadable);
        }

        public void Apply(bool updateMipmaps)   { Apply(updateMipmaps, false); }
        public void Apply()                     { Apply(true, false); }
    }

    public sealed partial class Texture3D : Texture
    {
        public Texture3D(int width, int height, int depth, TextureFormat format, bool mipmap)
        {
            Internal_Create(this, width, height, depth, format, mipmap);
        }

        public void Apply([uei.DefaultValue("true")] bool updateMipmaps, [uei.DefaultValue("false")] bool makeNoLongerReadable)
        {
            if (!IsReadable()) throw CreateNonReadableException(this);
            ApplyImpl(updateMipmaps, makeNoLongerReadable);
        }

        public void Apply(bool updateMipmaps)   { Apply(updateMipmaps, false); }
        public void Apply()                     { Apply(true, false); }
    }

    public sealed partial class Texture2DArray : Texture
    {
        public Texture2DArray(int width, int height, int depth, TextureFormat format, bool mipmap, [uei.DefaultValue("false")] bool linear)
        {
            Internal_Create(this, width, height, depth, format, mipmap, linear);
        }

        public Texture2DArray(int width, int height, int depth, TextureFormat format, bool mipmap)
            : this(width, height, depth, format, mipmap, false)
        {
        }

        public void Apply([uei.DefaultValue("true")] bool updateMipmaps, [uei.DefaultValue("false")] bool makeNoLongerReadable)
        {
            if (!IsReadable()) throw CreateNonReadableException(this);
            ApplyImpl(updateMipmaps, makeNoLongerReadable);
        }

        public void Apply(bool updateMipmaps)   { Apply(updateMipmaps, false); }
        public void Apply()                     { Apply(true, false); }
    }

    public sealed partial class CubemapArray : Texture
    {
        public CubemapArray(int faceSize, int cubemapCount, TextureFormat format, bool mipmap, [uei.DefaultValue("false")] bool linear)
        {
            Internal_Create(this, faceSize, cubemapCount, format, mipmap, linear);
        }

        public CubemapArray(int faceSize, int cubemapCount, TextureFormat format, bool mipmap)
            : this(faceSize, cubemapCount, format, mipmap, false)
        {
        }

        public void Apply([uei.DefaultValue("true")] bool updateMipmaps, [uei.DefaultValue("false")] bool makeNoLongerReadable)
        {
            if (!IsReadable()) throw CreateNonReadableException(this);
            ApplyImpl(updateMipmaps, makeNoLongerReadable);
        }

        public void Apply(bool updateMipmaps)   { Apply(updateMipmaps, false); }
        public void Apply()                     { Apply(true, false); }
    }
}
