// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

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
            Internal_CreateRenderTexture(this);
            SetRenderTextureDescriptor(desc);
        }

        public RenderTexture(RenderTexture textureToCopy)
        {
            if (textureToCopy == null)
            {
                throw new ArgumentNullException("textureToCopy");
            }
            ValidateRenderTextureDesc(textureToCopy.descriptor);
            Internal_CreateRenderTexture(this);
            SetRenderTextureDescriptor(textureToCopy.descriptor);
        }

        public static RenderTexture GetTemporary(RenderTextureDescriptor desc)
        {
            ValidateRenderTextureDesc(desc);
            desc.createdFromScript = true;
            return GetTemporary_Internal(desc);
        }

        public RenderTextureDescriptor descriptor
        {
            get { return GetDescriptor(); }
            set
            {
                ValidateRenderTextureDesc(value);
                SetRenderTextureDescriptor(value);
            }
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
}
