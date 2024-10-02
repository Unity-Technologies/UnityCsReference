// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using uei = UnityEngine.Internal;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Scripting;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Rendering;

namespace UnityEngine
{
    public struct RenderTextureDescriptor
    {
        public int width { get; set; }
        public int height { get; set; }
        public int msaaSamples { get; set; }
        public int volumeDepth { get; set; }
        public int mipCount { get; set; }

        private GraphicsFormat _graphicsFormat;

        public GraphicsFormat graphicsFormat
        {
            get { return _graphicsFormat; }

            set
            {
                _graphicsFormat = value;
                SetOrClearRenderTextureCreationFlag(GraphicsFormatUtility.IsSRGBFormat(value), RenderTextureCreationFlags.SRGB);                
            }
        }

        public GraphicsFormat stencilFormat { get; set; }

        public GraphicsFormat depthStencilFormat { get; set; }

        public RenderTextureFormat colorFormat
        {
            get
            {
                if (graphicsFormat != GraphicsFormat.None)
                {
                    return GraphicsFormatUtility.GetRenderTextureFormat(graphicsFormat);
                }
                else
                {
                    return shadowSamplingMode != ShadowSamplingMode.None ? RenderTextureFormat.Shadowmap : RenderTextureFormat.Depth;
                }
            }
            set
            {
                shadowSamplingMode = RenderTexture.GetShadowSamplingModeForFormat(value);
                GraphicsFormat requestedFormat = GraphicsFormatUtility.GetGraphicsFormat(value, sRGB);
                graphicsFormat = SystemInfo.GetCompatibleFormat(requestedFormat, GraphicsFormatUsage.Render);
                depthStencilFormat = RenderTexture.GetDepthStencilFormatLegacy(depthBufferBits, shadowSamplingMode);                              
            }
        }

        public bool sRGB
        {
            get { return GraphicsFormatUtility.IsSRGBFormat(graphicsFormat); }
            set
            {
                graphicsFormat = (value && QualitySettings.activeColorSpace == ColorSpace.Linear) // Maintain parity with old behavior: respect project color space.
                    ? GraphicsFormatUtility.GetSRGBFormat(graphicsFormat)
                    : GraphicsFormatUtility.GetLinearFormat(graphicsFormat);
            }
        }

        public int depthBufferBits
        {
            get { return GraphicsFormatUtility.GetDepthBits(depthStencilFormat); }
            //Ideally we deprecate the setter but keeping it for now because its a very commonly used api
            //It is very bad practice to use the shadowSamplingMode property here because that makes the result depend on the order of setting the properties
            //However, it's the best what we can do to make sure this is functionally correct.
            //depthBufferBits and colorFormat are legacy APIs that can be used togther in any order to set a combination of the (modern) fields graphicsFormat, dephtStencilFormat and shadowSamplingMode.
            //The use of these legacy APIs should not be combined with setting the modern fields directly, the order can change the results.
            //There should be no "magic" when setting the modern fields, the desc will contain what the users sets, even if the combination is not valid (ie a depthStencilFormat with stencil and shadowSamplingMode CompareDepths).
            set { depthStencilFormat = RenderTexture.GetDepthStencilFormatLegacy(value, shadowSamplingMode); }
        }

        public Rendering.TextureDimension dimension { get; set; }
        public Rendering.ShadowSamplingMode shadowSamplingMode { get; set; }
        public VRTextureUsage vrUsage { get; set; }
        private RenderTextureCreationFlags _flags;
        public RenderTextureCreationFlags flags { get { return _flags; } }
        public RenderTextureMemoryless memoryless { get; set; }

        [uei.ExcludeFromDocs]
        public RenderTextureDescriptor(int width, int height)
            : this(width, height, RenderTextureFormat.Default)
        {
        }

        [uei.ExcludeFromDocs]
        public RenderTextureDescriptor(int width, int height, RenderTextureFormat colorFormat)
            : this(width, height, colorFormat, 0)
        {
        }

        [uei.ExcludeFromDocs]
        public RenderTextureDescriptor(int width, int height, RenderTextureFormat colorFormat, int depthBufferBits)
            : this(width, height, colorFormat, depthBufferBits, Texture.GenerateAllMips)
        {
        }

        [uei.ExcludeFromDocs]
        public RenderTextureDescriptor(int width, int height, GraphicsFormat colorFormat, int depthBufferBits)
            : this(width, height, colorFormat, depthBufferBits, Texture.GenerateAllMips)
        {
        }

        [uei.ExcludeFromDocs]
        public RenderTextureDescriptor(int width, int height, RenderTextureFormat colorFormat, int depthBufferBits, int mipCount)
            : this(width, height, colorFormat, depthBufferBits, mipCount, RenderTextureReadWrite.Linear)
        {
        }

        public RenderTextureDescriptor(int width, int height, [uei.DefaultValue("RenderTextureFormat.Default")] RenderTextureFormat colorFormat, [uei.DefaultValue("0")] int depthBufferBits, [uei.DefaultValue("Texture.GenerateAllMips")] int mipCount, [uei.DefaultValue("RenderTextureReadWrite.Linear")] RenderTextureReadWrite readWrite)
        {
            GraphicsFormat compatibleFormat = RenderTexture.GetCompatibleFormat(colorFormat, readWrite);
            this = new RenderTextureDescriptor(width, height, compatibleFormat, RenderTexture.GetDepthStencilFormatLegacy(depthBufferBits, colorFormat), mipCount);
            this.shadowSamplingMode = RenderTexture.GetShadowSamplingModeForFormat(colorFormat);
        }

        [uei.ExcludeFromDocs]
        public RenderTextureDescriptor(int width, int height, GraphicsFormat colorFormat, int depthBufferBits, int mipCount) : this()
        {
            _flags = RenderTextureCreationFlags.AutoGenerateMips | RenderTextureCreationFlags.AllowVerticalFlip; // Set before graphicsFormat to avoid erasing the flag set by graphicsFormat
            this.width = width;
            this.height = height;
            volumeDepth = 1;
            msaaSamples = 1;
            this.graphicsFormat = colorFormat;
            this.depthStencilFormat = RenderTexture.GetDepthStencilFormatLegacy(depthBufferBits, colorFormat);
            this.mipCount = mipCount;
            dimension = Rendering.TextureDimension.Tex2D;
            shadowSamplingMode =  Rendering.ShadowSamplingMode.None;
            vrUsage = VRTextureUsage.None;
            memoryless = RenderTextureMemoryless.None;
        }

        [uei.ExcludeFromDocs]
        public RenderTextureDescriptor(int width, int height, GraphicsFormat colorFormat, GraphicsFormat depthStencilFormat) : this(width, height, colorFormat, depthStencilFormat, Texture.GenerateAllMips)
        {
        }

        [uei.ExcludeFromDocs]
        public RenderTextureDescriptor(int width, int height, GraphicsFormat colorFormat, GraphicsFormat depthStencilFormat, int mipCount) : this()
        {
            _flags = RenderTextureCreationFlags.AutoGenerateMips | RenderTextureCreationFlags.AllowVerticalFlip; // Set before graphicsFormat to avoid erasing the flag set by graphicsFormat
            this.width = width;
            this.height = height;
            volumeDepth = 1;
            msaaSamples = 1;
            this.graphicsFormat = colorFormat;
            this.depthStencilFormat = depthStencilFormat;
            this.mipCount = mipCount;
            dimension = Rendering.TextureDimension.Tex2D;
            shadowSamplingMode = Rendering.ShadowSamplingMode.None;
            vrUsage = VRTextureUsage.None;
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

        public bool bindMS
        {
            get { return (_flags & RenderTextureCreationFlags.BindMS) != 0; }
            set { SetOrClearRenderTextureCreationFlag(value, RenderTextureCreationFlags.BindMS); }
        }

        internal bool createdFromScript
        {
            get { return (_flags & RenderTextureCreationFlags.CreatedFromScript) != 0; }
            set { SetOrClearRenderTextureCreationFlag(value, RenderTextureCreationFlags.CreatedFromScript); }
        }

        public bool useDynamicScale
        {
            get { return (_flags & RenderTextureCreationFlags.DynamicallyScalable) != 0; }
            set { SetOrClearRenderTextureCreationFlag(value, RenderTextureCreationFlags.DynamicallyScalable); }
        }

        public bool useDynamicScaleExplicit
        {
            get { return (_flags & RenderTextureCreationFlags.DynamicallyScalableExplicit) != 0; }
            set { SetOrClearRenderTextureCreationFlag(value, RenderTextureCreationFlags.DynamicallyScalableExplicit); }
        }
    }

    public partial class RenderTexture : Texture
    {
        [RequiredByNativeCode] // used to create builtin textures
        internal protected RenderTexture()
        {
        }

        public RenderTexture(RenderTextureDescriptor desc)
        {
            ValidateRenderTextureDesc(ref desc);
            Internal_Create(this);
            SetRenderTextureDescriptor(desc);
        }

        public RenderTexture(RenderTexture textureToCopy)
        {
            if (textureToCopy == null)
                throw new ArgumentNullException("textureToCopy");

            RenderTextureDescriptor desc = textureToCopy.descriptor;
            ValidateRenderTextureDesc(ref desc);
            Internal_Create(this);
            SetRenderTextureDescriptor(desc);
        }

        [uei.ExcludeFromDocs]
        public RenderTexture(int width, int height, int depth, DefaultFormat format)
            : this(width, height, GetDefaultColorFormat(format), GetDefaultDepthStencilFormat(format, depth), Texture.GenerateAllMips)
        {
            if (this != null)
                SetShadowSamplingMode(GetShadowSamplingModeForFormat(format));
        }

        [uei.ExcludeFromDocs]
        public RenderTexture(int width, int height, int depth, GraphicsFormat format)
            : this(width, height, depth, format, Texture.GenerateAllMips)
        {
        }

        [uei.ExcludeFromDocs]
        public RenderTexture(int width, int height, int depth, GraphicsFormat format, int mipCount)
        {
            // Note: the code duplication here is because you can't set a descriptor with
            // zero width/height, which our own code (and possibly existing user code) relies on.
            if (format != GraphicsFormat.None && !ValidateFormat(format, GraphicsFormatUsage.Render))
                return;

            Internal_Create(this);
            this.depthStencilFormat = GetDepthStencilFormatLegacy(depth, format);
            this.width = width; this.height = height; this.graphicsFormat = format; SetMipMapCount(mipCount);

            SetSRGBReadWrite(GraphicsFormatUtility.IsSRGBFormat(format));
        }

        [uei.ExcludeFromDocs]
        public RenderTexture(int width, int height, GraphicsFormat colorFormat, GraphicsFormat depthStencilFormat, int mipCount)
        {
            // Note: the code duplication here is because you can't set a descriptor with
            // zero width/height, which our own code (and possibly existing user code) relies on.
            // None is valid here as it indicates a depth only rendertexture.
            if (colorFormat != GraphicsFormat.None && !ValidateFormat(colorFormat, GraphicsFormatUsage.Render))
                return;

            Internal_Create(this);
            this.width = width; this.height = height; this.depthStencilFormat = depthStencilFormat;  this.graphicsFormat = colorFormat; SetMipMapCount(mipCount);

            SetSRGBReadWrite(GraphicsFormatUtility.IsSRGBFormat(colorFormat));
        }

        [uei.ExcludeFromDocs]
        public RenderTexture(int width, int height, GraphicsFormat colorFormat, GraphicsFormat depthStencilFormat)
            : this(width, height, colorFormat, depthStencilFormat, Texture.GenerateAllMips)
        {
        }

        public RenderTexture(int width, int height, int depth, [uei.DefaultValue("RenderTextureFormat.Default")] RenderTextureFormat format, [uei.DefaultValue("RenderTextureReadWrite.Default")] RenderTextureReadWrite readWrite)
        {
            Initialize(width, height, depth, format, readWrite, Texture.GenerateAllMips);
        }

        [uei.ExcludeFromDocs]
        public RenderTexture(int width, int height, int depth, RenderTextureFormat format)
            : this(width, height, depth, format, Texture.GenerateAllMips)
        {
        }

        [uei.ExcludeFromDocs]
        public RenderTexture(int width, int height, int depth)
            : this(width, height, depth, RenderTextureFormat.Default)
        {
        }

        [uei.ExcludeFromDocs]
        public RenderTexture(int width, int height, int depth, RenderTextureFormat format, int mipCount)
        {
            Initialize(width, height, depth, format, RenderTextureReadWrite.Default, mipCount);
        }

        private void Initialize(int width, int height, int depth, RenderTextureFormat format, RenderTextureReadWrite readWrite, int mipCount)
        {
            GraphicsFormat colorFormat = GetCompatibleFormat(format, readWrite);
            GraphicsFormat depthStencilFormat = GetDepthStencilFormatLegacy(depth, format);

            // Note: the code duplication here is because you can't set a descriptor with
            // zero width/height, which our own code (and possibly existing user code) relies on.
            if (colorFormat != GraphicsFormat.None)
            {
                if (!ValidateFormat(colorFormat, GraphicsFormatUsage.Render))
                    return;
            }

            Internal_Create(this);
            this.width = width; this.height = height; this.depthStencilFormat = depthStencilFormat; this.graphicsFormat = colorFormat;

            SetMipMapCount(mipCount);
            SetSRGBReadWrite(GraphicsFormatUtility.IsSRGBFormat(colorFormat));
            SetShadowSamplingMode(GetShadowSamplingModeForFormat(format));
        }

        internal static GraphicsFormat GetDepthStencilFormatLegacy(int depthBits, GraphicsFormat colorFormat)
        {
            return GetDepthStencilFormatLegacy(depthBits, false);
        }

        internal static GraphicsFormat GetDepthStencilFormatLegacy(int depthBits, RenderTextureFormat format, bool disableFallback = false)
        {
            if (!disableFallback && (format == RenderTextureFormat.Depth || format == RenderTextureFormat.Shadowmap) && depthBits < 16)
            {
                WarnAboutFallbackTo16BitsDepth(format);
                depthBits = 16;
            }
            return GetDepthStencilFormatLegacy(depthBits, format == RenderTextureFormat.Shadowmap);
        }

        internal static GraphicsFormat GetDepthStencilFormatLegacy(int depthBits, DefaultFormat format)
        {
            return GetDepthStencilFormatLegacy(depthBits, format == DefaultFormat.Shadow);
        }

        internal static GraphicsFormat GetDepthStencilFormatLegacy(int depthBits, ShadowSamplingMode shadowSamplingMode)
        {
            return GetDepthStencilFormatLegacy(depthBits, shadowSamplingMode != ShadowSamplingMode.None);
        }

        internal static GraphicsFormat GetDepthStencilFormatLegacy(int depthBits, bool requestedShadowMap)
        {
            GraphicsFormat format = requestedShadowMap ? GraphicsFormatUtility.GetDepthStencilFormat(depthBits, 0) : GraphicsFormatUtility.GetDepthStencilFormat(depthBits);

            // In some very rare/special cases (example: UUM-64340), requesting a shadow map depth format with, for example, 24 bits of depth can make RT creation fail if
            // D24_UNorm and D32_SFLOAT are both incompatible format. (The "GetDepthStencilFormat" overload used in the shadowmap scenario can, by design, return "None"
            // even if we requested depthBits > 0 because it never checks compatible GraphicsFormats with less depth bits)
            // D32_SFLOAT is usually always compatible but, to stay consistent with old behavior, let's use D16_UNorm as a last resort fallback.
            if (depthBits > 16 && format == GraphicsFormat.None && requestedShadowMap)
            {
                Debug.LogWarning($"No compatible shadow map depth format with {depthBits} or more depth bits has been found. Changing to a 16 bit depth buffer.");
                return GraphicsFormat.D16_UNorm;
            }

            return format;
        }

        public RenderTextureDescriptor descriptor
        {
            get { return GetDescriptor(); }
            set { ValidateRenderTextureDesc(ref value); SetRenderTextureDescriptor(value); }
        }


        private static void ValidateRenderTextureDesc(ref RenderTextureDescriptor desc)
        {
            if (desc.graphicsFormat == GraphicsFormat.None && desc.depthStencilFormat == GraphicsFormat.None)
            {
                WarnAboutFallbackTo16BitsDepth(desc.colorFormat);
                desc.depthStencilFormat = GraphicsFormat.D16_UNorm;
            }
            if (desc.graphicsFormat != GraphicsFormat.None && !SystemInfo.IsFormatSupported(desc.graphicsFormat, GraphicsFormatUsage.Render))
                throw new ArgumentException("RenderTextureDesc graphicsFormat must be a supported GraphicsFormat. " + desc.graphicsFormat + " is not supported on this platform.", "desc.graphicsFormat");
            if (desc.depthStencilFormat != GraphicsFormat.None && !GraphicsFormatUtility.IsDepthStencilFormat(desc.depthStencilFormat))
                throw new ArgumentException("RenderTextureDesc depthStencilFormat must be a supported depth/stencil GraphicsFormat. " + desc.depthStencilFormat + " is not supported on this platform.", "desc.depthStencilFormat");
            if (desc.width <= 0)
                throw new ArgumentException("RenderTextureDesc width must be greater than zero.", "desc.width");
            if (desc.height <= 0)
                throw new ArgumentException("RenderTextureDesc height must be greater than zero.", "desc.height");
            if (desc.volumeDepth <= 0)
                throw new ArgumentException("RenderTextureDesc volumeDepth must be greater than zero.", "desc.volumeDepth");
            if (desc.msaaSamples != 1 && desc.msaaSamples != 2 && desc.msaaSamples != 4 && desc.msaaSamples != 8)
                throw new ArgumentException("RenderTextureDesc msaaSamples must be 1, 2, 4, or 8.", "desc.msaaSamples");
            if (desc.dimension == TextureDimension.CubeArray && desc.volumeDepth % 6 != 0)
                throw new ArgumentException("RenderTextureDesc volumeDepth must be a multiple of 6 when dimension is CubeArray", "desc.volumeDepth");
            if (GraphicsFormatUtility.IsDepthStencilFormat(desc.graphicsFormat))
                throw new ArgumentException("RenderTextureDesc graphicsFormat must not be a depth/stencil format. " + desc.graphicsFormat + " is not supported.", "desc.graphicsFormat");
        }

        internal static GraphicsFormat GetDefaultColorFormat(DefaultFormat format)
        {
            switch (format)
            {
                case DefaultFormat.DepthStencil:
                case DefaultFormat.Shadow:
                    return GraphicsFormat.None;
                default:
                    return SystemInfo.GetGraphicsFormat(format);
            }
        }

        internal static GraphicsFormat GetDefaultDepthStencilFormat(DefaultFormat format, int depth)
        {
            switch (format)
            {
                case DefaultFormat.DepthStencil:
                case DefaultFormat.Shadow:
                    return SystemInfo.GetGraphicsFormat(format); // Note that "depth" is explicitly ignored in this case.
                default:
                    return GetDepthStencilFormatLegacy(depth, format);
            }
        }

        internal static ShadowSamplingMode GetShadowSamplingModeForFormat(RenderTextureFormat format)
        {
            return format == RenderTextureFormat.Shadowmap ? ShadowSamplingMode.CompareDepths : ShadowSamplingMode.None;
        }

        internal static ShadowSamplingMode GetShadowSamplingModeForFormat(DefaultFormat format)
        {
            return format == DefaultFormat.Shadow ? ShadowSamplingMode.CompareDepths : ShadowSamplingMode.None;
        }

        // Only warns! Applying the fallback is still up to the calling function.
        internal static void WarnAboutFallbackTo16BitsDepth(RenderTextureFormat format)
        {
            Debug.LogWarning(string.Format("{0} RenderTexture requested without a depth buffer. Changing to a 16 bit depth buffer. To resolve this warning, please specify the desired number of depth bits when creating the render texture.", format));
        }
    }

    public partial class RenderTexture : Texture
    {
        internal static GraphicsFormat GetCompatibleFormat(RenderTextureFormat renderTextureFormat, RenderTextureReadWrite readWrite)
        {
            GraphicsFormat requestedFormat = GraphicsFormatUtility.GetGraphicsFormat(renderTextureFormat, readWrite);
            GraphicsFormat compatibleFormat = SystemInfo.GetCompatibleFormat(requestedFormat, GraphicsFormatUsage.Render);

            if (requestedFormat == compatibleFormat)
            {
                return requestedFormat;
            }
            else
            {
                Debug.LogWarning(String.Format("'{0}' is not supported. RenderTexture::GetTemporary fallbacks to {1} format on this platform. Use 'SystemInfo.IsFormatSupported' C# API to check format support.", requestedFormat.ToString(), compatibleFormat.ToString()));
                return compatibleFormat;
            }
        }

        public static RenderTexture GetTemporary(RenderTextureDescriptor desc)
        {
            ValidateRenderTextureDesc(ref desc);
            desc.createdFromScript = true;
            return GetTemporary_Internal(desc);
        }

        // in old bindings "default args" were expanded into overloads and we must mimic that when migrating to new bindings
        // to keep things sane we will do internal methods WITH default args and do overloads that simply call it

        private static RenderTexture GetTemporaryImpl(int width, int height, GraphicsFormat depthStencilFormat,
            GraphicsFormat colorFormat,
            int antiAliasing = 1, RenderTextureMemoryless memorylessMode = RenderTextureMemoryless.None,
            VRTextureUsage vrUsage = VRTextureUsage.None, bool useDynamicScale = false, ShadowSamplingMode shadowSamplingMode = ShadowSamplingMode.None)
        {
            RenderTextureDescriptor desc = new RenderTextureDescriptor(width, height, colorFormat, depthStencilFormat);
            desc.msaaSamples = antiAliasing;
            desc.memoryless = memorylessMode;
            desc.vrUsage = vrUsage;
            desc.useDynamicScale = useDynamicScale;
            desc.shadowSamplingMode = shadowSamplingMode;
            return GetTemporary(desc);
        }

        // most detailed overload: use it to specify default values for docs
        [uei.ExcludeFromDocs]
        public static RenderTexture GetTemporary(int width, int height, int depthBuffer, GraphicsFormat format,
            [uei.DefaultValue("1")] int antiAliasing,
            [uei.DefaultValue("RenderTextureMemoryless.None")] RenderTextureMemoryless memorylessMode,
            [uei.DefaultValue("VRTextureUsage.None")] VRTextureUsage vrUsage,
            [uei.DefaultValue("false")] bool useDynamicScale)
        {
            ShadowSamplingMode shadowSamplingMode = ShadowSamplingMode.None; // Default value for this overload.
            return GetTemporaryImpl(width, height, GetDepthStencilFormatLegacy(depthBuffer, shadowSamplingMode), format, antiAliasing, memorylessMode, vrUsage, useDynamicScale);
        }

        // the rest will be excluded from docs (to "pretend" we have one method with default args)
        [uei.ExcludeFromDocs]
        public static RenderTexture GetTemporary(int width, int height, int depthBuffer, GraphicsFormat format, int antiAliasing, RenderTextureMemoryless memorylessMode, VRTextureUsage vrUsage)
        {
            return GetTemporary(width, height, depthBuffer, format, antiAliasing, memorylessMode, vrUsage, false);
        }

        [uei.ExcludeFromDocs]
        public static RenderTexture GetTemporary(int width, int height, int depthBuffer, GraphicsFormat format, int antiAliasing, RenderTextureMemoryless memorylessMode)
        {
            return GetTemporary(width, height, depthBuffer, format, antiAliasing, memorylessMode, VRTextureUsage.None);
        }

        [uei.ExcludeFromDocs]
        public static RenderTexture GetTemporary(int width, int height, int depthBuffer, GraphicsFormat format, int antiAliasing)
        {
            return GetTemporary(width, height, depthBuffer, format, antiAliasing, RenderTextureMemoryless.None);
        }

        [uei.ExcludeFromDocs]
        public static RenderTexture GetTemporary(int width, int height, int depthBuffer, GraphicsFormat format)
        {
            return GetTemporary(width, height, depthBuffer, format, 1);
        }

        // most detailed overload: use it to specify default values for docs
        public static RenderTexture GetTemporary(int width, int height,
            [uei.DefaultValue("0")] int depthBuffer, [uei.DefaultValue("RenderTextureFormat.Default")] RenderTextureFormat format,
            [uei.DefaultValue("RenderTextureReadWrite.Default")] RenderTextureReadWrite readWrite, [uei.DefaultValue("1")] int antiAliasing,
            [uei.DefaultValue("RenderTextureMemoryless.None")] RenderTextureMemoryless memorylessMode,
            [uei.DefaultValue("VRTextureUsage.None")] VRTextureUsage vrUsage, [uei.DefaultValue("false")] bool useDynamicScale
        )
        {
            GraphicsFormat graphicsFormat = GetCompatibleFormat(format, readWrite);
            GraphicsFormat depthStencilFormat = GetDepthStencilFormatLegacy(depthBuffer, format);
            ShadowSamplingMode shadowSamplingMode = GetShadowSamplingModeForFormat(format);

            return GetTemporaryImpl(width, height, depthStencilFormat, graphicsFormat, antiAliasing, memorylessMode, vrUsage, useDynamicScale, shadowSamplingMode);
        }

        // the rest will be excluded from docs (to "pretend" we have one method with default args)
        [uei.ExcludeFromDocs]
        public static RenderTexture GetTemporary(int width, int height, int depthBuffer, RenderTextureFormat format, RenderTextureReadWrite readWrite, int antiAliasing, RenderTextureMemoryless memorylessMode, VRTextureUsage vrUsage)
        {
            return GetTemporary(width, height, depthBuffer, format, readWrite, antiAliasing, memorylessMode, vrUsage, false);
        }

        [uei.ExcludeFromDocs]
        public static RenderTexture GetTemporary(int width, int height, int depthBuffer, RenderTextureFormat format, RenderTextureReadWrite readWrite, int antiAliasing, RenderTextureMemoryless memorylessMode)
        {
            return GetTemporary(width, height, depthBuffer, format, readWrite, antiAliasing, memorylessMode, VRTextureUsage.None);
        }

        [uei.ExcludeFromDocs]
        public static RenderTexture GetTemporary(int width, int height, int depthBuffer, RenderTextureFormat format, RenderTextureReadWrite readWrite, int antiAliasing)
        {
            return GetTemporary(width, height, depthBuffer, format, readWrite, antiAliasing, RenderTextureMemoryless.None);
        }

        [uei.ExcludeFromDocs]
        public static RenderTexture GetTemporary(int width, int height, int depthBuffer, RenderTextureFormat format, RenderTextureReadWrite readWrite)
        {
            return GetTemporary(width, height, depthBuffer, format, readWrite, 1);
        }

        [uei.ExcludeFromDocs]
        public static RenderTexture GetTemporary(int width, int height, int depthBuffer, RenderTextureFormat format)
        {
            return GetTemporary(width, height, depthBuffer, format, RenderTextureReadWrite.Default);
        }

        [uei.ExcludeFromDocs]
        public static RenderTexture GetTemporary(int width, int height, int depthBuffer)
        {
            return GetTemporary(width, height, depthBuffer, RenderTextureFormat.Default);
        }

        [uei.ExcludeFromDocs]
        public static RenderTexture GetTemporary(int width, int height)
        {
            return GetTemporary(width, height, 0);
        }
    }

    public sealed partial class CustomRenderTexture : RenderTexture
    {
        // Be careful. We can't call base constructor here because it would create the native object twice.
        public CustomRenderTexture(int width, int height, RenderTextureFormat format, [uei.DefaultValue("RenderTextureReadWrite.Default")] RenderTextureReadWrite readWrite)
            : this(width, height, GetCompatibleFormat(format, readWrite))
        {
            if (this != null)
                SetShadowSamplingMode(GetShadowSamplingModeForFormat(format));
        }

        [uei.ExcludeFromDocs]
        public CustomRenderTexture(int width, int height, RenderTextureFormat format)
            : this(width, height, format, RenderTextureReadWrite.Default)
        {
        }

        [uei.ExcludeFromDocs]
        public CustomRenderTexture(int width, int height)
            : this(width, height, SystemInfo.GetGraphicsFormat(DefaultFormat.LDR))
        {
        }

        [uei.ExcludeFromDocs]
        public CustomRenderTexture(int width, int height, [uei.DefaultValue("DefaultFormat.LDR")] DefaultFormat defaultFormat)
            : this(width, height, GetDefaultColorFormat(defaultFormat))
        {
            // CustomRenderTexture's 'depthStencilFormat' is DefaultFormat.Depth by default. This is different from
            // RenderTexture, because the CRT constructors don't touch 'depthStencilFormat', so the
            // RenderTextureDescriptor's 'depthStencilFormat' is left unmodified after construction on the C++ side.
            // As such: to avoid changing behavior here, leave 'depthStencilFormat' alone, unless we are dealing
            // with a depth related 'defaultFormat'. (in which case, we indeed should ensure that
            // 'depthStencilFormat' is correct with regards to the user's request)
            if (defaultFormat == DefaultFormat.DepthStencil || defaultFormat == DefaultFormat.Shadow)
            {
                this.depthStencilFormat = SystemInfo.GetGraphicsFormat(defaultFormat);
                SetShadowSamplingMode(GetShadowSamplingModeForFormat(defaultFormat));
            }
        }

        [uei.ExcludeFromDocs]
        public CustomRenderTexture(int width, int height, GraphicsFormat format)
        {
            if (format != GraphicsFormat.None && !ValidateFormat(format, GraphicsFormatUsage.Render))
                return;

            Internal_CreateCustomRenderTexture(this);
            this.width = width;
            this.height = height;
            this.graphicsFormat = format;

            SetSRGBReadWrite(GraphicsFormatUtility.IsSRGBFormat(format));
        }
    }

    public struct MipmapLimitDescriptor
    {
        public bool useMipmapLimit { get; }
        public string groupName { get; }

        public MipmapLimitDescriptor(bool useMipmapLimit, string groupName)
        {
            this.useMipmapLimit = useMipmapLimit;
            this.groupName = groupName;
        }
    }


    public partial class Texture : Object
    {
        public static readonly int GenerateAllMips = -1;

        // In TextureFormat constructors, we eventually need to convert the TextureFormat to a GraphicsFormat
        // in order to call Internal_Create.
        // When a GraphicsFormat is obtained (with GetGraphicsFormat), Unity is allowed to choose one that
        // doesn't necessarily reflect what the user asked (sRGB / Linear) -- this is by design, TextureFormat
        // should be easy to use, so the GraphicsFormat may indeed be UNorm instead of SRGB in gamma projects.
        // However, if the Internal_Create only receives colorspace information through the GraphicsFormat,
        // then the texture does not know what the (CPU) data colorspace really should have been.
        // The texture then re-uses the GraphicsFormat information, which can cause issues if the user then
        // converts the texture to an asset.
        // That is why we separately pass the result of "GetTextureColorSpace" here to Internal_Create,
        // so that we can have a proper distinction between CPU data colorspace & GPU colorspace.
        internal TextureColorSpace GetTextureColorSpace(bool linear)
        {
            return linear ? TextureColorSpace.Linear : TextureColorSpace.sRGB;
        }

        // Do not use this in TextureFormat constructors, you would otherwise obtain a TextureColorSpace that
        // may not reflect what the user asked. Always use the above in TextureFormat constructors instead.
        internal TextureColorSpace GetTextureColorSpace(GraphicsFormat format)
        {
            return GetTextureColorSpace(!GraphicsFormatUtility.IsSRGBFormat(format));
        }

        internal bool ValidateFormat(RenderTextureFormat format)
        {
            if (SystemInfo.SupportsRenderTextureFormat(format))
            {
                return true;
            }
            else
            {
                Debug.LogError(String.Format("RenderTexture creation failed. '{0}' is not supported on this platform. Use 'SystemInfo.SupportsRenderTextureFormat' C# API to check format support.", format.ToString()), this);
                return false;
            }
        }

        internal bool ValidateFormat(TextureFormat format)
        {
            // If GPU support is detected, pass validation. Caveat support can also be used for uncompressed/non-IEEE754 formats.
            // In contrast to GraphicsFormat, TextureFormat can use fallbacks by design.
            if (SystemInfo.SupportsTextureFormat(format))
            {
                return true;
            }
            // If a compressed format is not supported natively, we check for decompressor support here.
            // If we are able to decompress, the data is decoded into a raw format. Otherwise, validation fails.
            else if (GraphicsFormatUtility.IsCompressedFormat(format) && GraphicsFormatUtility.CanDecompressFormat(GraphicsFormatUtility.GetGraphicsFormat(format, false)))
            {
                return true;
            }
            else
            {
                Debug.LogError(String.Format("Texture creation failed. '{0}' is not supported on this platform. Use 'SystemInfo.SupportsTextureFormat' C# API to check format support.", format.ToString()), this);
                return false;
            }
        }

        internal bool ValidateFormat(GraphicsFormat format, GraphicsFormatUsage usage)
        {
            // *ONLY* GPU support is checked here. If it is not available, fail validation.
            // GraphicsFormat does not use fallbacks by design.
            if (SystemInfo.IsFormatSupported(format, usage))
            {
                return true;
            }
            else
            {
                Debug.LogError(String.Format("Texture creation failed. '{0}' is not supported for {1} usage on this platform. Use 'SystemInfo.IsFormatSupported' C# API to check format support.", format.ToString(), usage.ToString()), this);
                return false;
            }
        }

        internal UnityException CreateNonReadableException(Texture t)
        {
            return new UnityException(
                String.Format("Texture '{0}' is not readable, the texture memory can not be accessed from scripts. You can make the texture readable in the Texture Import Settings.", t.name)
            );
        }

        internal UnityException IgnoreMipmapLimitCannotBeToggledException(Texture t)
        {
            return new UnityException(
                String.Format("Failed to toggle ignoreMipmapLimit, Texture '{0}' is not readable. You can make the texture readable in the Texture Import Settings.", t.name)
            );
        }

        internal UnityException CreateNativeArrayLengthOverflowException()
        {
            return new UnityException("Failed to create NativeArray, length exceeds the allowed maximum of Int32.MaxValue. Use a larger type as template argument to reduce the array length.");
        }
    }


    public partial class Texture2D : Texture
    {
        internal bool ValidateFormat(TextureFormat format, int width, int height)
        {
            bool isValid = ValidateFormat(format);
            if (isValid)
            {
                bool requireSquarePOT = (TextureFormat.PVRTC_RGB2 <= format && format <= TextureFormat.PVRTC_RGBA4);
                if (requireSquarePOT && !(width == height && Mathf.IsPowerOfTwo(width)))
                    throw new UnityException(String.Format("'{0}' demands texture to be square and have power-of-two dimensions", format.ToString()));
            }
            return isValid;
        }

        internal bool ValidateFormat(GraphicsFormat format, int width, int height)
        {
            bool isValid = ValidateFormat(format, GraphicsFormatUsage.Sample);
            if (isValid)
            {
                bool requireSquarePOT = GraphicsFormatUtility.IsPVRTCFormat(format);
                if (requireSquarePOT && !(width == height && Mathf.IsPowerOfTwo(width)))
                    throw new UnityException(String.Format("'{0}' demands texture to be square and have power-of-two dimensions", format.ToString()));
            }
            return isValid;
        }

        internal Texture2D(int width, int height, GraphicsFormat format, TextureCreationFlags flags, int mipCount, IntPtr nativeTex, MipmapLimitDescriptor mipmapLimitDescriptor)
        {
            bool useMipmapLimit = mipmapLimitDescriptor.useMipmapLimit;
            string mipmapLimitGroupName = mipmapLimitDescriptor.groupName;

            // Obsolete, see TextureCreationFlags::IgnoreMipmapLimit (1 << 11)
            // No additional warning, deprecation warning was already shown
            bool deprecatedIgnoreFlagWasSet = ((int)flags & (1 << 11)) != 0;
            if (deprecatedIgnoreFlagWasSet) useMipmapLimit = false;


            if (ValidateFormat(format, width, height))
                Internal_Create(this, width, height, mipCount, format, GetTextureColorSpace(format), flags, nativeTex, !useMipmapLimit, mipmapLimitGroupName);
        }

        [uei.ExcludeFromDocs]
        public Texture2D(int width, int height, DefaultFormat format, TextureCreationFlags flags)
            : this(width, height, SystemInfo.GetGraphicsFormat(format), flags)
        {
        }

        [uei.ExcludeFromDocs]
        public Texture2D(int width, int height, DefaultFormat format, int mipCount, TextureCreationFlags flags)
           : this(width, height, SystemInfo.GetGraphicsFormat(format), flags, mipCount, IntPtr.Zero, default)
        {
        }

        [uei.ExcludeFromDocs]
        // In theory, users were able to create a group without actually having mipmap limits enabled (and to enable it later on, at runtime);
        // this will no longer be supported (which allows us to rename that boolean flag)
        [Obsolete("Please provide mipmap limit information using a MipmapLimitDescriptor argument", false)]
        public Texture2D(int width, int height, DefaultFormat format, int mipCount, string mipmapLimitGroupName, TextureCreationFlags flags)
            : this(width, height, SystemInfo.GetGraphicsFormat(format), flags, mipCount, IntPtr.Zero, new MipmapLimitDescriptor(true, mipmapLimitGroupName))
        {
        }

        [uei.ExcludeFromDocs]
        public Texture2D(int width, int height, DefaultFormat format, int mipCount, TextureCreationFlags flags, MipmapLimitDescriptor mipmapLimitDescriptor)
            : this(width, height, SystemInfo.GetGraphicsFormat(format), flags, mipCount, IntPtr.Zero, mipmapLimitDescriptor)
        {
        }

        [uei.ExcludeFromDocs]
        public Texture2D(int width, int height, GraphicsFormat format, TextureCreationFlags flags)
            : this(width, height, format, flags, Texture.GenerateAllMips, IntPtr.Zero, default)
        {
        }

        [uei.ExcludeFromDocs]
        public Texture2D(int width, int height, GraphicsFormat format, int mipCount, TextureCreationFlags flags)
            : this(width, height, format, flags, mipCount, IntPtr.Zero, default)
        {
        }

        [uei.ExcludeFromDocs]
        [Obsolete("Please provide mipmap limit information using a MipmapLimitDescriptor argument", false)]
        public Texture2D(int width, int height, GraphicsFormat format, int mipCount, string mipmapLimitGroupName, TextureCreationFlags flags)
            : this(width, height, format, flags, mipCount, IntPtr.Zero, new MipmapLimitDescriptor(true, mipmapLimitGroupName))
        {
        }

        [uei.ExcludeFromDocs]
        public Texture2D(int width, int height, GraphicsFormat format, int mipCount, TextureCreationFlags flags, MipmapLimitDescriptor mipmapLimitDescriptor)
            : this(width, height, format, flags, mipCount, IntPtr.Zero, mipmapLimitDescriptor)
        {
        }

        internal Texture2D(int width, int height, TextureFormat textureFormat, int mipCount, bool linear, IntPtr nativeTex, bool createUninitialized, MipmapLimitDescriptor mipmapLimitDescriptor)
        {
            if (!ValidateFormat(textureFormat, width, height))
                return;

            GraphicsFormat format = GraphicsFormatUtility.GetGraphicsFormat(textureFormat, !linear);
            TextureCreationFlags flags = (mipCount != 1) ? TextureCreationFlags.MipChain : TextureCreationFlags.None;
            if (GraphicsFormatUtility.IsCrunchFormat(textureFormat))
                flags |= TextureCreationFlags.Crunch;
            if (createUninitialized)
                flags |= TextureCreationFlags.DontUploadUponCreate | TextureCreationFlags.DontInitializePixels;
            Internal_Create(this, width, height, mipCount, format, GetTextureColorSpace(linear), flags, nativeTex, !mipmapLimitDescriptor.useMipmapLimit, mipmapLimitDescriptor.groupName);
        }

        public Texture2D(int width, int height, [uei.DefaultValue("TextureFormat.RGBA32")] TextureFormat textureFormat, [uei.DefaultValue("-1")] int mipCount, [uei.DefaultValue("false")] bool linear)
            : this(width, height, textureFormat, mipCount, linear, IntPtr.Zero, false, default)
        {
        }

        public Texture2D(int width, int height, [uei.DefaultValue("TextureFormat.RGBA32")] TextureFormat textureFormat, [uei.DefaultValue("-1")] int mipCount, [uei.DefaultValue("false")] bool linear, [uei.DefaultValue("false")] bool createUninitialized)
           : this(width, height, textureFormat, mipCount, linear, IntPtr.Zero, createUninitialized, default)
        {
        }

        public Texture2D(int width, int height, [uei.DefaultValue("TextureFormat.RGBA32")] TextureFormat textureFormat, [uei.DefaultValue("-1")] int mipCount, [uei.DefaultValue("false")] bool linear, [uei.DefaultValue("false")] bool createUninitialized, MipmapLimitDescriptor mipmapLimitDescriptor)
            : this(width, height, textureFormat, mipCount, linear, IntPtr.Zero, createUninitialized, mipmapLimitDescriptor)
        {
        }

        // In theory, users were able to create a group without actually having mipmap limits enabled (and to enable it later on, at runtime);
        // this will no longer be supported (which allows us to rename that boolean flag)
        [Obsolete("Please provide mipmap limit information using a MipmapLimitDescriptor argument", false)]
        public Texture2D(int width, int height, [uei.DefaultValue("TextureFormat.RGBA32")] TextureFormat textureFormat, [uei.DefaultValue("-1")] int mipCount, [uei.DefaultValue("false")] bool linear, [uei.DefaultValue("false")] bool createUninitialized, [uei.DefaultValue("true")] bool ignoreMipmapLimit, [uei.DefaultValue("null")] string mipmapLimitGroupName)
           : this(width, height, textureFormat, mipCount, linear, IntPtr.Zero, createUninitialized, new MipmapLimitDescriptor(!ignoreMipmapLimit, mipmapLimitGroupName))
        {
        }

        public Texture2D(int width, int height, [uei.DefaultValue("TextureFormat.RGBA32")] TextureFormat textureFormat, [uei.DefaultValue("true")] bool mipChain, [uei.DefaultValue("false")] bool linear)
            : this(width, height, textureFormat, mipChain ? Texture.GenerateAllMips : 1, linear, IntPtr.Zero, false, default)
        {
        }

        public Texture2D(int width, int height, [uei.DefaultValue("TextureFormat.RGBA32")] TextureFormat textureFormat, [uei.DefaultValue("true")] bool mipChain, [uei.DefaultValue("false")] bool linear, [uei.DefaultValue("false")] bool createUninitialized)
           : this(width, height, textureFormat, mipChain ? Texture.GenerateAllMips : 1, linear, IntPtr.Zero, createUninitialized, default)
        {
        }

        public Texture2D(int width, int height, TextureFormat textureFormat, bool mipChain)
            : this(width, height, textureFormat, mipChain ? Texture.GenerateAllMips : 1, false, IntPtr.Zero, false, default)
        {
        }

        public Texture2D(int width, int height)
        {
            TextureFormat format = TextureFormat.RGBA32;
            const bool linear = false;
            if (width == 0 && height == 0)
                Internal_CreateEmptyImpl(this);
            else if (ValidateFormat(format, width, height))
                Internal_Create(this, width, height, Texture.GenerateAllMips, GraphicsFormatUtility.GetGraphicsFormat(format, !linear), GetTextureColorSpace(linear), TextureCreationFlags.MipChain, IntPtr.Zero, true, null);
        }

        public static Texture2D CreateExternalTexture(int width, int height, TextureFormat format, bool mipChain, bool linear, IntPtr nativeTex)
        {
            if (nativeTex == IntPtr.Zero)
                throw new ArgumentException("nativeTex can not be null");
            return new Texture2D(width, height, format, mipChain ? -1 : 1, linear, nativeTex, false, default);
        }

        [uei.ExcludeFromDocs]
        public void SetPixel(int x, int y, Color color)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            SetPixelImpl(0, 0, x, y, color);
        }

        public void SetPixel(int x, int y, Color color, [uei.DefaultValue("0")] int mipLevel)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            SetPixelImpl(0, mipLevel, x, y, color);
        }

        public void SetPixels(int x, int y, int blockWidth, int blockHeight, Color[] colors, [uei.DefaultValue("0")] int miplevel)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            SetPixelsImpl(x, y, blockWidth, blockHeight, colors, miplevel, 0);
        }

        [uei.ExcludeFromDocs]
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

        [uei.ExcludeFromDocs]
        public void SetPixels(Color[] colors)
        {
            SetPixels(0, 0, width, height, colors, 0);
        }

        [uei.ExcludeFromDocs]
        public Color GetPixel(int x, int y)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            return GetPixelImpl(0,  0, x, y);
        }

        public Color GetPixel(int x, int y, [uei.DefaultValue("0")] int mipLevel)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            return GetPixelImpl(0, mipLevel, x, y);
        }

        [uei.ExcludeFromDocs]
        public Color GetPixelBilinear(float u, float v)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            return GetPixelBilinearImpl(0, 0, u, v);
        }

        public Color GetPixelBilinear(float u, float v, [uei.DefaultValue("0")] int mipLevel)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            return GetPixelBilinearImpl(0, mipLevel, u, v);
        }

        public void LoadRawTextureData(IntPtr data, int size)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            if (data == IntPtr.Zero || size == 0) { Debug.LogError("No texture data provided to LoadRawTextureData", this); return; }
            if (!LoadRawTextureDataImpl(data, (ulong)size))
                throw new UnityException("LoadRawTextureData: not enough data provided (will result in overread).");
        }

        public void LoadRawTextureData(byte[] data)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            if (data == null || data.Length == 0) { Debug.LogError("No texture data provided to LoadRawTextureData", this); return; }
            if (!LoadRawTextureDataImplArray(data))
                throw new UnityException("LoadRawTextureData: not enough data provided (will result in overread).");
        }

        unsafe public void LoadRawTextureData<T>(NativeArray<T> data) where T : struct
        {
            if (!isReadable) throw CreateNonReadableException(this);
            if (!data.IsCreated || data.Length == 0) throw new UnityException("No texture data provided to LoadRawTextureData");
            if (!LoadRawTextureDataImpl((IntPtr)data.GetUnsafeReadOnlyPtr(), (ulong)data.Length * (ulong)UnsafeUtility.SizeOf<T>()))
                throw new UnityException("LoadRawTextureData: not enough data provided (will result in overread).");
        }

        public void SetPixelData<T>(T[] data, int mipLevel, [uei.DefaultValue("0")] int sourceDataStartIndex = 0)
        {
            if (sourceDataStartIndex < 0) throw new UnityException("SetPixelData: sourceDataStartIndex cannot be less than 0.");

            if (!isReadable) throw CreateNonReadableException(this);
            if (data == null || data.Length == 0) throw new UnityException("No texture data provided to SetPixelData.");
            SetPixelDataImplArray(data, mipLevel, System.Runtime.InteropServices.Marshal.SizeOf(data[0]), data.Length, sourceDataStartIndex);
        }

        unsafe public void SetPixelData<T>(NativeArray<T> data, int mipLevel, [uei.DefaultValue("0")] int sourceDataStartIndex = 0) where T : struct
        {
            if (sourceDataStartIndex < 0) throw new UnityException("SetPixelData: sourceDataStartIndex cannot be less than 0.");

            if (!isReadable) throw CreateNonReadableException(this);
            if (!data.IsCreated || data.Length == 0) throw new UnityException("No texture data provided to SetPixelData.");
            SetPixelDataImpl((IntPtr)data.GetUnsafeReadOnlyPtr(), mipLevel, UnsafeUtility.SizeOf<T>(), data.Length, sourceDataStartIndex);
        }

        public unsafe NativeArray<T> GetPixelData<T>(int mipLevel) where T : struct
        {
            if (!isReadable) throw CreateNonReadableException(this);
            if (mipLevel < 0 || mipLevel >= mipmapCount) throw new ArgumentException("The passed in miplevel " + mipLevel + " is invalid. It needs to be in the range 0 and " + (mipmapCount - 1));
            if (GetWritableImageData(0).ToInt64() == 0) throw new UnityException($"Texture '{name}' has no data.");

            ulong chainOffset = GetPixelDataOffset(mipLevel);
            ulong arraySize = GetPixelDataSize(mipLevel);
            int stride = UnsafeUtility.SizeOf<T>();
            ulong arrayLength = arraySize / (ulong)stride;

            if (arrayLength > Int32.MaxValue) throw CreateNativeArrayLengthOverflowException();

            IntPtr dataPtr = new IntPtr((long)((ulong)GetWritableImageData(0) + chainOffset));

            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>((void*)dataPtr, (int)arrayLength, Allocator.None);

            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, this.GetSafetyHandleForSlice(mipLevel));
            return array;
        }

        public unsafe NativeArray<T> GetRawTextureData<T>() where T : struct
        {
            if (!isReadable) throw CreateNonReadableException(this);

            int stride = UnsafeUtility.SizeOf<T>();
            ulong arrayLength = GetImageDataSize() / (ulong)stride;

            if (arrayLength > Int32.MaxValue) throw CreateNativeArrayLengthOverflowException();

            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>((void*)GetWritableImageData(0), (int)arrayLength, Allocator.None);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, Texture2D.GetSafetyHandle(this));
            return array;
        }

        public void Apply([uei.DefaultValue("true")] bool updateMipmaps, [uei.DefaultValue("false")] bool makeNoLongerReadable)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            ApplyImpl(updateMipmaps, makeNoLongerReadable);
        }

        [uei.ExcludeFromDocs] public void Apply(bool updateMipmaps) { Apply(updateMipmaps, false); }
        [uei.ExcludeFromDocs] public void Apply() { Apply(true, false); }

        public bool Reinitialize(int width, int height)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            return ReinitializeImpl(width, height);
        }

        public bool Reinitialize(int width, int height, TextureFormat format, bool hasMipMap)
        {
            return ReinitializeWithTextureFormatImpl(width, height, format, hasMipMap);
        }

        public bool Reinitialize(int width, int height, GraphicsFormat format, bool hasMipMap)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            return ReinitializeWithFormatImpl(width, height, format, hasMipMap);
        }

        [Obsolete("Texture2D.Resize(int, int) has been deprecated because it actually reinitializes the texture. Use Texture2D.Reinitialize(int, int) instead (UnityUpgradable) -> Reinitialize([*] System.Int32, [*] System.Int32)", false)]
        public bool Resize(int width, int height)
        {
            return Reinitialize(width, height);
        }

        [Obsolete("Texture2D.Resize(int, int, TextureFormat, bool) has been deprecated because it actually reinitializes the texture. Use Texture2D.Reinitialize(int, int, TextureFormat, bool) instead (UnityUpgradable) -> Reinitialize([*] System.Int32, [*] System.Int32, UnityEngine.TextureFormat, [*] System.Boolean)", false)]
        public bool Resize(int width, int height, TextureFormat format, bool hasMipMap)
        {
            return Reinitialize(width, height, format, hasMipMap);
        }

        [Obsolete("Texture2D.Resize(int, int, GraphicsFormat, bool) has been deprecated because it actually reinitializes the texture. Use Texture2D.Reinitialize(int, int, GraphicsFormat, bool) instead (UnityUpgradable) -> Reinitialize([*] System.Int32, [*] System.Int32, UnityEngine.Experimental.Rendering.GraphicsFormat, [*] System.Boolean)", false)]
        public bool Resize(int width, int height, GraphicsFormat format, bool hasMipMap)
        {
            return Reinitialize(width, height, format, hasMipMap);
        }

        public void ReadPixels(Rect source, int destX, int destY, [uei.DefaultValue("true")] bool recalculateMipMaps)
        {
            //if (ValidateFormat(GraphicsFormatUtility.GetGraphicsFormat(format, ), GraphicsFormatUsage.ReadPixels))
            //    Debug.LogError("No texture data provided to LoadRawTextureData", this);
            if (!isReadable) throw CreateNonReadableException(this);
            ReadPixelsImpl(source, destX, destY, recalculateMipMaps);
        }

        [uei.ExcludeFromDocs] public void ReadPixels(Rect source, int destX, int destY) { ReadPixels(source, destX, destY, true); }

        public static bool GenerateAtlas(Vector2[] sizes, int padding, int atlasSize, List<Rect> results)
        {
            if (sizes == null)
                throw new ArgumentException("sizes array can not be null");
            if (results == null)
                throw new ArgumentException("results list cannot be null");
            if (padding < 0)
                throw new ArgumentException("padding can not be negative");
            if (atlasSize <= 0)
                throw new ArgumentException("atlas size must be positive");

            results.Clear();
            if (sizes.Length == 0)
                return true;

            NoAllocHelpers.EnsureListElemCount(results, sizes.Length);
            GenerateAtlasImpl(sizes, padding, atlasSize, NoAllocHelpers.ExtractArrayFromList(results));
            return results.Count != 0;
        }

        public void SetPixels32(Color32[] colors, [uei.DefaultValue("0")] int miplevel)
        {
            SetAllPixels32(colors, miplevel);
        }

        [uei.ExcludeFromDocs]
        public void SetPixels32(Color32[] colors)
        {
            SetPixels32(colors, 0);
        }

        public void SetPixels32(int x, int y, int blockWidth, int blockHeight, Color32[] colors, [uei.DefaultValue("0")] int miplevel)
        {
            SetBlockOfPixels32(x, y, blockWidth, blockHeight, colors, miplevel);
        }

        [uei.ExcludeFromDocs]
        public void SetPixels32(int x, int y, int blockWidth, int blockHeight, Color32[] colors)
        {
            SetPixels32(x, y, blockWidth, blockHeight, colors, 0);
        }

        public Color[] GetPixels([uei.DefaultValue("0")] int miplevel)
        {
            int w = width >> miplevel; if (w < 1) w = 1;
            int h = height >> miplevel; if (h < 1) h = 1;
            return GetPixels(0, 0, w, h, miplevel);
        }

        [uei.ExcludeFromDocs]
        public Color[] GetPixels()
        {
            return GetPixels(0);
        }

        public void CopyPixels(Texture src)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            if (!src.isReadable) throw CreateNonReadableException(src);
            CopyPixels_Full(src);
        }

        public void CopyPixels(Texture src, int srcElement, int srcMip, int dstMip)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            if (!src.isReadable) throw CreateNonReadableException(src);
            CopyPixels_Slice(src, srcElement, srcMip, dstMip);
        }

        public void CopyPixels(Texture src, int srcElement, int srcMip, int srcX, int srcY, int srcWidth, int srcHeight, int dstMip, int dstX, int dstY)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            if (!src.isReadable) throw CreateNonReadableException(src);
            CopyPixels_Region(src, srcElement, srcMip, srcX, srcY, srcWidth, srcHeight, dstMip, dstX, dstY);
        }

        public bool ignoreMipmapLimit
        {
            get { return IgnoreMipmapLimit();}
            set
            {
                if (!isReadable) throw IgnoreMipmapLimitCannotBeToggledException(this);
                SetIgnoreMipmapLimitAndReload(value);
            }
        }

        [Flags]
        public enum EXRFlags
        {
            None = 0,
            OutputAsFloat = 1 << 0, // Default is Half
            // Compression are mutually exclusive.
            CompressZIP = 1 << 1,
            CompressRLE = 1 << 2,
            CompressPIZ = 1 << 3,
        }
    }

    public sealed partial class Cubemap : Texture
    {
        internal bool ValidateFormat(TextureFormat format, int width)
        {
            bool isValid = ValidateFormat(format);
            if (isValid)
            {
                bool requireSquarePOT = (TextureFormat.PVRTC_RGB2 <= format && format <= TextureFormat.PVRTC_RGBA4);
                if (requireSquarePOT && !Mathf.IsPowerOfTwo(width))
                    throw new UnityException(String.Format("'{0}' demands texture to have power-of-two dimensions", format.ToString()));
            }
            return isValid;
        }

        internal bool ValidateFormat(GraphicsFormat format, int width)
        {
            bool isValid = ValidateFormat(format, GraphicsFormatUsage.Sample);
            if (isValid)
            {
                bool requireSquarePOT = GraphicsFormatUtility.IsPVRTCFormat(format);
                if (requireSquarePOT && !Mathf.IsPowerOfTwo(width))
                    throw new UnityException(String.Format("'{0}' demands texture to have power-of-two dimensions", format.ToString()));
            }
            return isValid;
        }

        [uei.ExcludeFromDocs]
        public Cubemap(int width, DefaultFormat format, TextureCreationFlags flags)
            : this(width, SystemInfo.GetGraphicsFormat(format), flags)
        {
        }

        [uei.ExcludeFromDocs]
        public Cubemap(int width, DefaultFormat format, TextureCreationFlags flags, int mipCount)
            : this(width, SystemInfo.GetGraphicsFormat(format), flags, mipCount)
        {
        }

        [uei.ExcludeFromDocs]
        [RequiredByNativeCode] // used to create builtin textures
        public Cubemap(int width, GraphicsFormat format, TextureCreationFlags flags)
            : this(width, format, flags, Texture.GenerateAllMips)
        {
        }

        [uei.ExcludeFromDocs]
        public Cubemap(int width, GraphicsFormat format, TextureCreationFlags flags, int mipCount)
        {
            if (!ValidateFormat(format, width))
                return;

            ValidateIsNotCrunched(flags); // Script created Crunched Cubemaps not supported

            Internal_Create(this, width, mipCount, format, GetTextureColorSpace(format), flags, IntPtr.Zero);
        }

        internal Cubemap(int width, TextureFormat textureFormat, int mipCount, IntPtr nativeTex, bool createUninitialized)
        {
            if (!ValidateFormat(textureFormat, width))
                return;

            const bool linear = true;
            GraphicsFormat format = GraphicsFormatUtility.GetGraphicsFormat(textureFormat, !linear);
            TextureCreationFlags flags = (mipCount != 1) ? TextureCreationFlags.MipChain : TextureCreationFlags.None;
            if (GraphicsFormatUtility.IsCrunchFormat(textureFormat))
                flags |= TextureCreationFlags.Crunch;
            if (createUninitialized)
                flags |= TextureCreationFlags.DontUploadUponCreate | TextureCreationFlags.DontInitializePixels;
            ValidateIsNotCrunched(flags); // Script created Crunched Cubemaps not supported
            Internal_Create(this, width, mipCount, format, GetTextureColorSpace(linear), flags, nativeTex);
        }

        public Cubemap(int width, TextureFormat textureFormat, bool mipChain)
            : this(width, textureFormat, mipChain ? Texture.GenerateAllMips : 1, IntPtr.Zero, false)
        {
        }

        public Cubemap(int width, TextureFormat textureFormat, bool mipChain, [uei.DefaultValue("false")] bool createUninitialized)
            : this(width, textureFormat, mipChain ? Texture.GenerateAllMips : 1, IntPtr.Zero, createUninitialized)
        {
        }

        public Cubemap(int width, TextureFormat format, int mipCount)
           : this(width, format, mipCount, IntPtr.Zero, false)
        {
        }

        public Cubemap(int width, TextureFormat format, int mipCount, [uei.DefaultValue("false")] bool createUninitialized)
          : this(width, format, mipCount, IntPtr.Zero, createUninitialized)
        {
        }

        public static Cubemap CreateExternalTexture(int width, TextureFormat format, bool mipmap, IntPtr nativeTex)
        {
            if (nativeTex == IntPtr.Zero)
                throw new ArgumentException("nativeTex can not be null");
            return new Cubemap(width, format, mipmap ? Texture.GenerateAllMips : 1, nativeTex, false);
        }

        public void SetPixelData<T>(T[] data, int mipLevel, CubemapFace face, [uei.DefaultValue("0")] int sourceDataStartIndex = 0)
        {
            if (sourceDataStartIndex < 0) throw new UnityException("SetPixelData: sourceDataStartIndex cannot be less than 0.");

            if (!isReadable) throw CreateNonReadableException(this);
            if (data == null || data.Length == 0) throw new UnityException("No texture data provided to SetPixelData.");
            SetPixelDataImplArray(data, mipLevel, (int)face, System.Runtime.InteropServices.Marshal.SizeOf(data[0]), data.Length, sourceDataStartIndex);
        }

        unsafe public void SetPixelData<T>(NativeArray<T> data, int mipLevel, CubemapFace face, [uei.DefaultValue("0")] int sourceDataStartIndex = 0) where T : struct
        {
            if (sourceDataStartIndex < 0) throw new UnityException("SetPixelData: sourceDataStartIndex cannot be less than 0.");

            if (!isReadable) throw CreateNonReadableException(this);
            if (!data.IsCreated || data.Length == 0) throw new UnityException("No texture data provided to SetPixelData.");

            SetPixelDataImpl((IntPtr)data.GetUnsafeReadOnlyPtr(), mipLevel, (int)face, UnsafeUtility.SizeOf<T>(), data.Length, sourceDataStartIndex);
        }

        public unsafe NativeArray<T> GetPixelData<T>(int mipLevel, CubemapFace face) where T : struct
        {
            if (!isReadable) throw CreateNonReadableException(this);
            if (mipLevel < 0 || mipLevel >= mipmapCount) throw new ArgumentException("The passed in miplevel " + mipLevel + " is invalid. The valid range is 0 through " + (mipmapCount - 1));
            if ((int)face < 0 || (int)face >= 6) throw new ArgumentException("The passed in face " + face + " is invalid. The valid range is 0 through 5.");
            if (GetWritableImageData(0).ToInt64() == 0) throw new UnityException($"Texture '{name}' has no data.");

            ulong singleElementDataSize = GetPixelDataOffset(this.mipmapCount, (int)face);
            ulong chainOffset = GetPixelDataOffset(mipLevel, (int)face);
            ulong arraySize = GetPixelDataSize(mipLevel, (int)face);
            int stride = UnsafeUtility.SizeOf<T>();
            ulong arrayLength = arraySize / (ulong)stride;

            if (arrayLength > Int32.MaxValue) throw CreateNativeArrayLengthOverflowException();

            IntPtr dataPtr = new IntPtr((long)((ulong)GetWritableImageData(0) + (singleElementDataSize * (ulong)face + chainOffset)));
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>((void*)dataPtr, (int)arrayLength, Allocator.None);

            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, this.GetSafetyHandleForSlice(mipLevel, (int)face));
            return array;
        }

        [uei.ExcludeFromDocs]
        public void SetPixel(CubemapFace face, int x, int y, Color color)
        {
            SetPixel(face, x, y, color, 0);
        }

        public void SetPixel(CubemapFace face, int x, int y, Color color, [uei.DefaultValue("0")] int mip)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            SetPixelImpl((int)face, mip, x, y, color);
        }

        [uei.ExcludeFromDocs]
        public Color GetPixel(CubemapFace face, int x, int y)
        {
            return GetPixel(face, x, y, 0);
        }

        public Color GetPixel(CubemapFace face, int x, int y, [uei.DefaultValue("0")] int mip)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            return GetPixelImpl((int)face, mip, x, y);
        }

        public void Apply([uei.DefaultValue("true")] bool updateMipmaps, [uei.DefaultValue("false")] bool makeNoLongerReadable)
        {
            ApplyImpl(updateMipmaps, makeNoLongerReadable);
        }

        [uei.ExcludeFromDocs]  public void Apply(bool updateMipmaps) { Apply(updateMipmaps, false); }
        [uei.ExcludeFromDocs]  public void Apply() { Apply(true, false); }

        public void CopyPixels(Texture src)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            if (!src.isReadable) throw CreateNonReadableException(src);
            CopyPixels_Full(src);
        }

        public void CopyPixels(Texture src, int srcElement, int srcMip, CubemapFace dstFace, int dstMip)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            if (!src.isReadable) throw CreateNonReadableException(src);
            CopyPixels_Slice(src, srcElement, srcMip, (int)dstFace, dstMip);
        }

        public void CopyPixels(Texture src, int srcElement, int srcMip, int srcX, int srcY, int srcWidth, int srcHeight, CubemapFace dstFace, int dstMip, int dstX, int dstY)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            if (!src.isReadable) throw CreateNonReadableException(src);
            CopyPixels_Region(src, srcElement, srcMip, srcX, srcY, srcWidth, srcHeight, (int)dstFace, dstMip, dstX, dstY);
        }

        private static void ValidateIsNotCrunched(TextureCreationFlags flags)
        {
            if ((flags &= TextureCreationFlags.Crunch) != 0)
                throw new ArgumentException("Crunched Cubemap is not supported for textures created from script.");
        }
    }

    public sealed partial class Texture3D : Texture
    {
        [uei.ExcludeFromDocs]
        public Texture3D(int width, int height, int depth, DefaultFormat format, TextureCreationFlags flags)
            : this(width, height, depth, SystemInfo.GetGraphicsFormat(format), flags)
        {
        }

        [uei.ExcludeFromDocs]
        public Texture3D(int width, int height, int depth, DefaultFormat format, TextureCreationFlags flags, int mipCount)
            : this(width, height, depth, SystemInfo.GetGraphicsFormat(format), flags, mipCount)
        {
        }

        [uei.ExcludeFromDocs]
        [RequiredByNativeCode] // used to create builtin textures
        public Texture3D(int width, int height, int depth, GraphicsFormat format, TextureCreationFlags flags)
            : this(width, height, depth, format, flags, Texture.GenerateAllMips)
        {
        }

        [uei.ExcludeFromDocs]
        public Texture3D(int width, int height, int depth, GraphicsFormat format, TextureCreationFlags flags, [uei.DefaultValue("Texture.GenerateAllMips")] int mipCount)
        {
            if (!ValidateFormat(format, GraphicsFormatUsage.Sample))
                return;

            ValidateIsNotCrunched(flags);
            Internal_Create(this, width, height, depth, mipCount, format, GetTextureColorSpace(format), flags, IntPtr.Zero);
        }

        [uei.ExcludeFromDocs]
        public Texture3D(int width, int height, int depth, TextureFormat textureFormat, int mipCount)
            : this(width, height, depth, textureFormat, mipCount, IntPtr.Zero)
        {
        }

        public Texture3D(int width, int height, int depth, TextureFormat textureFormat, int mipCount, [uei.DefaultValue("IntPtr.Zero")] IntPtr nativeTex)
            : this(width, height, depth, textureFormat, mipCount, nativeTex, false)
        {
        }

        public Texture3D(int width, int height, int depth, TextureFormat textureFormat, int mipCount, [uei.DefaultValue("IntPtr.Zero")] IntPtr nativeTex, [uei.DefaultValue("false")] bool createUninitialized)
        {
            if (!ValidateFormat(textureFormat))
                return;

            const bool linear = true;
            GraphicsFormat format = GraphicsFormatUtility.GetGraphicsFormat(textureFormat, !linear);
            TextureCreationFlags flags = (mipCount != 1) ? TextureCreationFlags.MipChain : TextureCreationFlags.None;
            if (GraphicsFormatUtility.IsCrunchFormat(textureFormat))
                flags |= TextureCreationFlags.Crunch;
            if (createUninitialized)
                flags |= TextureCreationFlags.DontUploadUponCreate | TextureCreationFlags.DontInitializePixels;
            ValidateIsNotCrunched(flags);
            Internal_Create(this, width, height, depth, mipCount, format, GetTextureColorSpace(linear), flags, nativeTex);
        }

        [uei.ExcludeFromDocs]
        public Texture3D(int width, int height, int depth, TextureFormat textureFormat, bool mipChain)
            : this(width, height, depth, textureFormat, mipChain ? Texture.GenerateAllMips : 1)
        {
        }

        public Texture3D(int width, int height, int depth, TextureFormat textureFormat, bool mipChain, [uei.DefaultValue("false")] bool createUninitialized)
            : this(width, height, depth, textureFormat, mipChain ? Texture.GenerateAllMips : 1, IntPtr.Zero, createUninitialized)
        {
        }

        public Texture3D(int width, int height, int depth, TextureFormat textureFormat, bool mipChain, [uei.DefaultValue("IntPtr.Zero")] IntPtr nativeTex)
            : this(width, height, depth, textureFormat, mipChain ? Texture.GenerateAllMips : 1, nativeTex)
        {
        }

        public static Texture3D CreateExternalTexture(int width, int height, int depth, TextureFormat format, bool mipChain, IntPtr nativeTex)
        {
            if (nativeTex == IntPtr.Zero)
                throw new ArgumentException($"{nameof(nativeTex)} may not be zero");

            return new Texture3D(width, height, depth, format, mipChain ? -1 : 1, nativeTex, false);
        }

        public void Apply([uei.DefaultValue("true")] bool updateMipmaps, [uei.DefaultValue("false")] bool makeNoLongerReadable)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            ApplyImpl(updateMipmaps, makeNoLongerReadable);
        }

        [uei.ExcludeFromDocs]  public void Apply(bool updateMipmaps) { Apply(updateMipmaps, false); }
        [uei.ExcludeFromDocs]  public void Apply() { Apply(true, false); }

        [uei.ExcludeFromDocs]
        public void SetPixel(int x, int y, int z, Color color)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            SetPixelImpl(0, x, y, z, color);
        }

        public void SetPixel(int x, int y, int z, Color color, [uei.DefaultValue("0")] int mipLevel)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            SetPixelImpl(mipLevel, x, y, z, color);
        }

        [uei.ExcludeFromDocs]
        public Color GetPixel(int x, int y, int z)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            return GetPixelImpl(0, x, y, z);
        }

        public Color GetPixel(int x, int y, int z, [uei.DefaultValue("0")] int mipLevel)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            return GetPixelImpl(mipLevel, x, y, z);
        }

        [uei.ExcludeFromDocs]
        public Color GetPixelBilinear(float u, float v, float w)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            return GetPixelBilinearImpl(0, u, v, w);
        }

        public Color GetPixelBilinear(float u, float v, float w, [uei.DefaultValue("0")] int mipLevel)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            return GetPixelBilinearImpl(mipLevel, u, v, w);
        }

        public void SetPixelData<T>(T[] data, int mipLevel, [uei.DefaultValue("0")] int sourceDataStartIndex = 0)
        {
            if (sourceDataStartIndex < 0) throw new UnityException("SetPixelData: sourceDataStartIndex cannot be less than 0.");

            if (!isReadable) throw CreateNonReadableException(this);
            if (data == null || data.Length == 0) throw new UnityException("No texture data provided to SetPixelData.");
            SetPixelDataImplArray(data, mipLevel, System.Runtime.InteropServices.Marshal.SizeOf(data[0]), data.Length, sourceDataStartIndex);
        }

        unsafe public void SetPixelData<T>(NativeArray<T> data, int mipLevel, [uei.DefaultValue("0")] int sourceDataStartIndex = 0) where T : struct
        {
            if (sourceDataStartIndex < 0) throw new UnityException("SetPixelData: sourceDataStartIndex cannot be less than 0.");

            if (!isReadable) throw CreateNonReadableException(this);
            if (!data.IsCreated || data.Length == 0) throw new UnityException("No texture data provided to SetPixelData.");

            SetPixelDataImpl((IntPtr)data.GetUnsafeReadOnlyPtr(), mipLevel, UnsafeUtility.SizeOf<T>(), data.Length, sourceDataStartIndex);
        }

        public unsafe NativeArray<T> GetPixelData<T>(int mipLevel) where T : struct
        {
            if (!isReadable) throw CreateNonReadableException(this);
            if (mipLevel < 0 || mipLevel >= mipmapCount) throw new ArgumentException("The passed in miplevel " + mipLevel + " is invalid. The valid range is 0 through  " + (mipmapCount - 1));
            if (GetImageData().ToInt64() == 0) throw new UnityException($"Texture '{name}' has no data.");

            ulong chainOffset = GetPixelDataOffset(mipLevel);
            ulong arraySize = GetPixelDataSize(mipLevel);
            int stride = UnsafeUtility.SizeOf<T>();
            ulong arrayLength = arraySize / (ulong)stride;

            if (arrayLength > Int32.MaxValue) throw CreateNativeArrayLengthOverflowException();

            IntPtr dataPtr = new IntPtr((long)((ulong)GetImageData() + chainOffset));
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>((void*)dataPtr, (int)arrayLength, Allocator.None);

            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, this.GetSafetyHandleForSlice(mipLevel));
            return array;
        }

        public void CopyPixels(Texture src)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            if (!src.isReadable) throw CreateNonReadableException(src);
            CopyPixels_Full(src);
        }

        public void CopyPixels(Texture src, int srcElement, int srcMip, int dstElement, int dstMip)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            if (!src.isReadable) throw CreateNonReadableException(src);
            CopyPixels_Slice(src, srcElement, srcMip, dstElement, dstMip);
        }

        public void CopyPixels(Texture src, int srcElement, int srcMip, int srcX, int srcY, int srcWidth, int srcHeight, int dstElement, int dstMip, int dstX, int dstY)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            if (!src.isReadable) throw CreateNonReadableException(src);
            CopyPixels_Region(src, srcElement, srcMip, srcX, srcY, srcWidth, srcHeight, dstElement, dstMip, dstX, dstY);
        }

        private static void ValidateIsNotCrunched(TextureCreationFlags flags)
        {
            if ((flags &= TextureCreationFlags.Crunch) != 0)
                throw new ArgumentException("Crunched Texture3D is not supported.");
        }
    }

    public sealed partial class Texture2DArray : Texture
    {
        internal bool ValidateFormat(TextureFormat format, int width, int height)
        {
            bool isValid = ValidateFormat(format);
            if (isValid)
            {
                bool requireSquarePOT = (TextureFormat.PVRTC_RGB2 <= format && format <= TextureFormat.PVRTC_RGBA4);
                if (requireSquarePOT && !(width == height && Mathf.IsPowerOfTwo(width)))
                    throw new UnityException(String.Format("'{0}' demands texture to be square and have power-of-two dimensions", format.ToString()));
            }
            return isValid;
        }

        internal bool ValidateFormat(GraphicsFormat format, int width, int height)
        {
            bool isValid = ValidateFormat(format, GraphicsFormatUsage.Sample);
            if (isValid)
            {
                bool requireSquarePOT = GraphicsFormatUtility.IsPVRTCFormat(format);
                if (requireSquarePOT && !(width == height && Mathf.IsPowerOfTwo(width)))
                    throw new UnityException(String.Format("'{0}' demands texture to be square and have power-of-two dimensions", format.ToString()));
            }
            return isValid;
        }

        [uei.ExcludeFromDocs]
        public Texture2DArray(int width, int height, int depth, DefaultFormat format, TextureCreationFlags flags)
            : this(width, height, depth, SystemInfo.GetGraphicsFormat(format), flags)
        {
        }

        [uei.ExcludeFromDocs]
        public Texture2DArray(int width, int height, int depth, DefaultFormat format, TextureCreationFlags flags, int mipCount)
           : this(width, height, depth, SystemInfo.GetGraphicsFormat(format), flags, mipCount)
        {
        }

        [uei.ExcludeFromDocs]
        public Texture2DArray(int width, int height, int depth, DefaultFormat format, TextureCreationFlags flags, int mipCount, MipmapLimitDescriptor mipmapLimitDescriptor)
            : this(width, height, depth, SystemInfo.GetGraphicsFormat(format), flags, mipCount, mipmapLimitDescriptor)
        {
        }

        [RequiredByNativeCode] // used to create builtin textures
        public Texture2DArray(int width, int height, int depth, GraphicsFormat format, TextureCreationFlags flags)
            : this(width, height, depth, format, flags, Texture.GenerateAllMips, new MipmapLimitDescriptor())
        {
        }

        [uei.ExcludeFromDocs]
        public Texture2DArray(int width, int height, int depth, GraphicsFormat format, TextureCreationFlags flags, int mipCount)
            : this(width, height, depth, format, flags, mipCount, new MipmapLimitDescriptor())
        {
        }

        [uei.ExcludeFromDocs]
        public Texture2DArray(int width, int height, int depth, GraphicsFormat format, TextureCreationFlags flags, int mipCount, MipmapLimitDescriptor mipmapLimitDescriptor)
        {
            if (!ValidateFormat(format, width, height))
                return;

            ValidateIsNotCrunched(flags);
            Internal_Create(this, width, height, depth, mipCount, format, GetTextureColorSpace(format), flags, !mipmapLimitDescriptor.useMipmapLimit, mipmapLimitDescriptor.groupName);
        }

        public Texture2DArray(int width, int height, int depth, TextureFormat textureFormat, int mipCount, bool linear, bool createUninitialized, MipmapLimitDescriptor mipmapLimitDescriptor)
        {
            if (!ValidateFormat(textureFormat, width, height))
                return;

            GraphicsFormat format = GraphicsFormatUtility.GetGraphicsFormat(textureFormat, !linear);
            TextureCreationFlags flags = (mipCount != 1) ? TextureCreationFlags.MipChain : TextureCreationFlags.None;
            if (GraphicsFormatUtility.IsCrunchFormat(textureFormat))
                flags |= TextureCreationFlags.Crunch;
            if (createUninitialized)
                flags |= TextureCreationFlags.DontUploadUponCreate | TextureCreationFlags.DontInitializePixels;
            ValidateIsNotCrunched(flags);
            Internal_Create(this, width, height, depth, mipCount, format, GetTextureColorSpace(linear), flags, !mipmapLimitDescriptor.useMipmapLimit, mipmapLimitDescriptor.groupName);
        }

        public Texture2DArray(int width, int height, int depth, TextureFormat textureFormat, int mipCount, bool linear, bool createUninitialized)
            : this(width, height, depth, textureFormat, mipCount, linear, createUninitialized, default)
        {
        }

        public Texture2DArray(int width, int height, int depth, TextureFormat textureFormat, int mipCount, bool linear)
            : this(width, height, depth, textureFormat, mipCount,linear, false, default)
        {
        }

        public Texture2DArray(int width, int height, int depth, TextureFormat textureFormat, bool mipChain, [uei.DefaultValue("false")] bool linear, [uei.DefaultValue("false")] bool createUninitialized)
          : this(width, height, depth, textureFormat, mipChain ? Texture.GenerateAllMips : 1, linear, createUninitialized, default)
        {
        }

        public Texture2DArray(int width, int height, int depth, TextureFormat textureFormat, bool mipChain, [uei.DefaultValue("false")] bool linear)
            : this(width, height, depth, textureFormat, mipChain ? Texture.GenerateAllMips : 1, linear)
        {
        }

        [uei.ExcludeFromDocs]
        public Texture2DArray(int width, int height, int depth, TextureFormat textureFormat, bool mipChain)
            : this(width, height, depth, textureFormat, mipChain ? Texture.GenerateAllMips : 1, false)
        {
        }

        public void Apply([uei.DefaultValue("true")] bool updateMipmaps, [uei.DefaultValue("false")] bool makeNoLongerReadable)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            ApplyImpl(updateMipmaps, makeNoLongerReadable);
        }

        [uei.ExcludeFromDocs] public void Apply(bool updateMipmaps) { Apply(updateMipmaps, false); }
        [uei.ExcludeFromDocs] public void Apply() { Apply(true, false); }

        public void SetPixelData<T>(T[] data, int mipLevel, int element, [uei.DefaultValue("0")] int sourceDataStartIndex = 0)
        {
            if (sourceDataStartIndex < 0) throw new UnityException("SetPixelData: sourceDataStartIndex cannot be less than 0.");

            if (!isReadable) throw CreateNonReadableException(this);
            if (data == null || data.Length == 0) throw new UnityException("No texture data provided to SetPixelData.");
            SetPixelDataImplArray(data, mipLevel, element, System.Runtime.InteropServices.Marshal.SizeOf(data[0]), data.Length, sourceDataStartIndex);
        }

        unsafe public void SetPixelData<T>(NativeArray<T> data, int mipLevel, int element, [uei.DefaultValue("0")] int sourceDataStartIndex = 0) where T : struct
        {
            if (sourceDataStartIndex < 0) throw new UnityException("SetPixelData: sourceDataStartIndex cannot be less than 0.");

            if (!isReadable) throw CreateNonReadableException(this);
            if (!data.IsCreated || data.Length == 0) throw new UnityException("No texture data provided to SetPixelData.");

            SetPixelDataImpl((IntPtr)data.GetUnsafeReadOnlyPtr(), mipLevel, element, UnsafeUtility.SizeOf<T>(), data.Length, sourceDataStartIndex);
        }

        public unsafe NativeArray<T> GetPixelData<T>(int mipLevel, int element) where T : struct
        {
            if (!isReadable) throw CreateNonReadableException(this);
            if (mipLevel < 0 || mipLevel >= mipmapCount) throw new ArgumentException("The passed in miplevel " + mipLevel + " is invalid. The valid range is 0 through " + (mipmapCount - 1));
            if (element < 0 || element >= depth) throw new ArgumentException("The passed in element " + element + " is invalid. The valid range is 0 through " + (depth - 1));

            ulong singleElementDataSize = GetPixelDataOffset(this.mipmapCount, element);
            ulong chainOffset = GetPixelDataOffset(mipLevel, element);
            ulong arraySize = GetPixelDataSize(mipLevel, element);
            int stride = UnsafeUtility.SizeOf<T>();
            ulong arrayLength = arraySize / (ulong)stride;

            if (arrayLength > Int32.MaxValue) throw CreateNativeArrayLengthOverflowException();

            IntPtr dataPtr = new IntPtr((long)((ulong)GetImageData() + (singleElementDataSize * (ulong)element + chainOffset)));
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>((void*)dataPtr, (int)arrayLength, Allocator.None);

            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, this.GetSafetyHandleForSlice(mipLevel, element));
            return array;
        }

        public void CopyPixels(Texture src)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            if (!src.isReadable) throw CreateNonReadableException(src);
            CopyPixels_Full(src);
        }

        public void CopyPixels(Texture src, int srcElement, int srcMip, int dstElement, int dstMip)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            if (!src.isReadable) throw CreateNonReadableException(src);
            CopyPixels_Slice(src, srcElement, srcMip, dstElement, dstMip);
        }

        public void CopyPixels(Texture src, int srcElement, int srcMip, int srcX, int srcY, int srcWidth, int srcHeight, int dstElement, int dstMip, int dstX, int dstY)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            if (!src.isReadable) throw CreateNonReadableException(src);
            CopyPixels_Region(src, srcElement, srcMip, srcX, srcY, srcWidth, srcHeight, dstElement, dstMip, dstX, dstY);
        }

        public bool ignoreMipmapLimit
        {
            get { return IgnoreMipmapLimit(); }
            set
            {
                if (!isReadable) throw IgnoreMipmapLimitCannotBeToggledException(this);
                SetIgnoreMipmapLimitAndReload(value);
            }
        }

        private static void ValidateIsNotCrunched(TextureCreationFlags flags)
        {
            if ((flags &= TextureCreationFlags.Crunch) != 0)
                throw new ArgumentException("Crunched Texture2DArray is not supported.");
        }
    }

    public sealed partial class CubemapArray : Texture
    {
        [uei.ExcludeFromDocs]
        public CubemapArray(int width, int cubemapCount, DefaultFormat format, TextureCreationFlags flags)
            : this(width, cubemapCount, SystemInfo.GetGraphicsFormat(format), flags)
        {
        }

        [uei.ExcludeFromDocs]
        public CubemapArray(int width, int cubemapCount, DefaultFormat format, TextureCreationFlags flags, [uei.DefaultValue("Texture.GenerateAllMips")] int mipCount)
           : this(width, cubemapCount, SystemInfo.GetGraphicsFormat(format), flags, mipCount)
        {
        }

        [RequiredByNativeCode]
        public CubemapArray(int width, int cubemapCount, GraphicsFormat format, TextureCreationFlags flags)
            : this(width, cubemapCount, format, flags, Texture.GenerateAllMips)
        {
        }

        [uei.ExcludeFromDocs]
        public CubemapArray(int width, int cubemapCount, GraphicsFormat format, TextureCreationFlags flags, [uei.DefaultValue("Texture.GenerateAllMips")] int mipCount)
        {
            if (!ValidateFormat(format, GraphicsFormatUsage.Sample))
                return;

            ValidateIsNotCrunched(flags);
            Internal_Create(this, width, cubemapCount, mipCount, format, GetTextureColorSpace(format), flags);
        }

        public CubemapArray(int width, int cubemapCount, TextureFormat textureFormat, int mipCount, bool linear, [uei.DefaultValue("false")] bool createUninitialized)
        {
            if (!ValidateFormat(textureFormat))
                return;

            GraphicsFormat format = GraphicsFormatUtility.GetGraphicsFormat(textureFormat, !linear);
            TextureCreationFlags flags = (mipCount != 1) ? TextureCreationFlags.MipChain : TextureCreationFlags.None;
            if (GraphicsFormatUtility.IsCrunchFormat(textureFormat))
                flags |= TextureCreationFlags.Crunch;
            if (createUninitialized)
                flags |= TextureCreationFlags.DontUploadUponCreate | TextureCreationFlags.DontInitializePixels;
            ValidateIsNotCrunched(flags);
            Internal_Create(this, width, cubemapCount, mipCount, format, GetTextureColorSpace(linear), flags);
        }

        public CubemapArray(int width, int cubemapCount, TextureFormat textureFormat, int mipCount, bool linear)
            : this(width, cubemapCount, textureFormat, mipCount, linear, false)
        { }

        public CubemapArray(int width, int cubemapCount, TextureFormat textureFormat, bool mipChain, [uei.DefaultValue("false")] bool linear, [uei.DefaultValue("false")] bool createUninitialized)
            : this(width, cubemapCount, textureFormat, mipChain ? Texture.GenerateAllMips : 1, linear, createUninitialized)
        {
        }

        [uei.ExcludeFromDocs]
        public CubemapArray(int width, int cubemapCount, TextureFormat textureFormat, bool mipChain, [uei.DefaultValue("false")] bool linear)
            : this(width, cubemapCount, textureFormat, mipChain ? Texture.GenerateAllMips : 1, linear)
        {
        }

        public CubemapArray(int width, int cubemapCount, TextureFormat textureFormat, bool mipChain)
            : this(width, cubemapCount, textureFormat, mipChain ? Texture.GenerateAllMips : 1, false)
        {
        }

        public void Apply([uei.DefaultValue("true")] bool updateMipmaps, [uei.DefaultValue("false")] bool makeNoLongerReadable)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            ApplyImpl(updateMipmaps, makeNoLongerReadable);
        }

        [uei.ExcludeFromDocs] public void Apply(bool updateMipmaps) { Apply(updateMipmaps, false); }
        [uei.ExcludeFromDocs] public void Apply() { Apply(true, false); }

        public void SetPixelData<T>(T[] data, int mipLevel, CubemapFace face, int element, [uei.DefaultValue("0")] int sourceDataStartIndex = 0)
        {
            if (sourceDataStartIndex < 0) throw new UnityException("SetPixelData: sourceDataStartIndex cannot be less than 0.");

            if (!isReadable) throw CreateNonReadableException(this);
            if (data == null || data.Length == 0) throw new UnityException("No texture data provided to SetPixelData.");

            SetPixelDataImplArray(data, mipLevel, (int)face, element, System.Runtime.InteropServices.Marshal.SizeOf(data[0]), data.Length, sourceDataStartIndex);
        }

        unsafe public void SetPixelData<T>(NativeArray<T> data, int mipLevel, CubemapFace face, int element, [uei.DefaultValue("0")] int sourceDataStartIndex = 0) where T : struct
        {
            if (sourceDataStartIndex < 0) throw new UnityException("SetPixelData: sourceDataStartIndex cannot be less than 0.");

            if (!isReadable) throw CreateNonReadableException(this);
            if (!data.IsCreated || data.Length == 0) throw new UnityException("No texture data provided to SetPixelData.");

            SetPixelDataImpl((IntPtr)data.GetUnsafeReadOnlyPtr(), mipLevel, (int)face, element, UnsafeUtility.SizeOf<T>(), data.Length, sourceDataStartIndex);
        }

        public unsafe NativeArray<T> GetPixelData<T>(int mipLevel, CubemapFace face, int element) where T : struct
        {
            if (!isReadable) throw CreateNonReadableException(this);
            if (mipLevel < 0 || mipLevel >= mipmapCount) throw new ArgumentException("The passed in miplevel " + mipLevel + " is invalid. The valid range is 0 through " + (mipmapCount - 1));
            if ((int)face < 0 || (int)face >= 6) throw new ArgumentException("The passed in face " + face + " is invalid.  The valid range is 0 through 5");
            if (element < 0 || element >= cubemapCount) throw new ArgumentException("The passed in element " + element + " is invalid. The valid range is 0 through " + (cubemapCount - 1));

            int elementOffset = element * 6 + (int)face;
            ulong singleElementDataSize = GetPixelDataOffset(this.mipmapCount, elementOffset);
            ulong chainOffset = GetPixelDataOffset(mipLevel, elementOffset);
            ulong arraySize = GetPixelDataSize(mipLevel, elementOffset);
            int stride = UnsafeUtility.SizeOf<T>();
            ulong arrayLength = arraySize / (ulong)stride;

            if (arrayLength > Int32.MaxValue) throw CreateNativeArrayLengthOverflowException();

            IntPtr dataPtr = new IntPtr((long)((ulong)GetImageData() + (singleElementDataSize * (ulong)elementOffset + chainOffset)));
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>((void*)dataPtr, (int)(arrayLength), Allocator.None);

            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, this.GetSafetyHandleForSlice(mipLevel, (int)face, element));
            return array;
        }

        public void CopyPixels(Texture src)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            if (!src.isReadable) throw CreateNonReadableException(src);
            CopyPixels_Full(src);
        }

        public void CopyPixels(Texture src, int srcElement, int srcMip, int dstElement, int dstMip)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            if (!src.isReadable) throw CreateNonReadableException(src);
            CopyPixels_Slice(src, srcElement, srcMip, dstElement, dstMip);
        }

        public void CopyPixels(Texture src, int srcElement, int srcMip, int srcX, int srcY, int srcWidth, int srcHeight, int dstElement, int dstMip, int dstX, int dstY)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            if (!src.isReadable) throw CreateNonReadableException(src);
            CopyPixels_Region(src, srcElement, srcMip, srcX, srcY, srcWidth, srcHeight, dstElement, dstMip, dstX, dstY);
        }

        private static void ValidateIsNotCrunched(TextureCreationFlags flags)
        {
            if ((flags &= TextureCreationFlags.Crunch) != 0)
                throw new ArgumentException("Crunched TextureCubeArray is not supported.");
        }
    }

    public sealed partial class SparseTexture : Texture
    {
        internal bool ValidateFormat(TextureFormat format, int width, int height)
        {
            bool isValid = ValidateFormat(format);
            if (isValid)
            {
                bool requireSquarePOT = (TextureFormat.PVRTC_RGB2 <= format && format <= TextureFormat.PVRTC_RGBA4);
                if (requireSquarePOT && !(width == height && Mathf.IsPowerOfTwo(width)))
                    throw new UnityException(String.Format("'{0}' demands texture to be square and have power-of-two dimensions", format.ToString()));
            }
            return isValid;
        }

        internal bool ValidateFormat(GraphicsFormat format, int width, int height)
        {
            bool isValid = ValidateFormat(format, GraphicsFormatUsage.Sparse);
            if (isValid)
            {
                bool requireSquarePOT = GraphicsFormatUtility.IsPVRTCFormat(format);
                if (requireSquarePOT && !(width == height && Mathf.IsPowerOfTwo(width)))
                    throw new UnityException(String.Format("'{0}' demands texture to be square and have power-of-two dimensions", format.ToString()));
            }
            return isValid;
        }

        internal bool ValidateSize(int width, int height, GraphicsFormat format)
        {
            if (GraphicsFormatUtility.GetBlockSize(format) * (width / GraphicsFormatUtility.GetBlockWidth(format)) * (height / GraphicsFormatUtility.GetBlockHeight(format)) < 65536)
            {
                Debug.LogError("SparseTexture creation failed. The minimum size in bytes of a SparseTexture is 64KB.", this);
                return false;
            }
            return true;
        }

        private static void ValidateIsNotCrunched(TextureFormat textureFormat)
        {
            if (GraphicsFormatUtility.IsCrunchFormat(textureFormat))
                throw new ArgumentException("Crunched SparseTexture is not supported.");
        }

        [uei.ExcludeFromDocs]
        public SparseTexture(int width, int height, DefaultFormat format, int mipCount)
            : this(width, height, SystemInfo.GetGraphicsFormat(format), mipCount)
        {
        }

        [uei.ExcludeFromDocs]
        public SparseTexture(int width, int height, GraphicsFormat format, int mipCount)
        {
            if (!ValidateFormat(format, width, height))
                return;

            if (!ValidateSize(width, height, format))
                return;

            Internal_Create(this, width, height, format, GetTextureColorSpace(format), mipCount);
        }

        [uei.ExcludeFromDocs]
        public SparseTexture(int width, int height, TextureFormat textureFormat, int mipCount)
            : this(width, height, textureFormat, mipCount, false)
        {
        }

        public SparseTexture(int width, int height, TextureFormat textureFormat, int mipCount, [uei.DefaultValue("false")] bool linear)
        {
            if (!ValidateFormat(textureFormat, width, height))
                return;

            ValidateIsNotCrunched(textureFormat);

            GraphicsFormat format = GraphicsFormatUtility.GetGraphicsFormat(textureFormat, !linear);
            if (!SystemInfo.IsFormatSupported(format, GraphicsFormatUsage.Sparse))
            {
                // Special case: SystemInfo.SupportsTextureFormat(textureFormat) tells us whether we
                // can use the format for a more "regular" texture type, but not necessarily whether
                // we can use that same format for a SparseTexture. This is because the function
                // checks the Sample FormatUsage, hence the extra GraphicsFormatUsage.Sparse check here
                // to prevent various crashes, errors, ...
                Debug.LogError($"Creation of a SparseTexture with '{textureFormat}' is not supported on this platform.");
                // Note about the usage of LogError above (versus an exception): according to
                // https://confluence.unity3d.com/pages/viewpage.action?spaceKey=DEV&title=Error+Handling
                // : exceptions should only be thrown if the user invokes a method with bad data (example: null)
                // or when the program is not in a valid state anymore. (example: disk failure/disconnected)
                // Additionally, according to the scripting team: throwing an exception for an unsupported
                // format sounds wrong in general.
                return;
            }

            if (!ValidateSize(width, height, format))
                return;

            Internal_Create(this, width, height, format, GetTextureColorSpace(linear), mipCount);
        }
    }
}
