// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace UnityEngine
{
    internal class SystemInfoShimBase
    {
        public virtual string unsupportedIdentifier => UnityEngine.SystemInfo.unsupportedIdentifier;

        public virtual float batteryLevel => UnityEngine.SystemInfo.batteryLevel;

        public virtual BatteryStatus batteryStatus => UnityEngine.SystemInfo.batteryStatus;

        public virtual string operatingSystem => UnityEngine.SystemInfo.operatingSystem;

        public virtual OperatingSystemFamily operatingSystemFamily => UnityEngine.SystemInfo.operatingSystemFamily;

        public virtual string processorType => UnityEngine.SystemInfo.processorType;

        public virtual int processorFrequency => UnityEngine.SystemInfo.processorFrequency;

        public virtual int processorCount => UnityEngine.SystemInfo.processorCount;

        public virtual int systemMemorySize => UnityEngine.SystemInfo.systemMemorySize;

        public virtual string deviceUniqueIdentifier => UnityEngine.SystemInfo.deviceUniqueIdentifier;

        public virtual string deviceName => UnityEngine.SystemInfo.deviceName;

        public virtual string deviceModel => UnityEngine.SystemInfo.deviceModel;

        public virtual bool supportsAccelerometer => UnityEngine.SystemInfo.supportsAccelerometer;

        public virtual bool supportsGyroscope => UnityEngine.SystemInfo.supportsGyroscope;

        public virtual bool supportsLocationService => UnityEngine.SystemInfo.supportsLocationService;

        public virtual bool supportsVibration => UnityEngine.SystemInfo.supportsVibration;

        public virtual bool supportsAudio => UnityEngine.SystemInfo.supportsAudio;

        public virtual DeviceType deviceType => UnityEngine.SystemInfo.deviceType;

        public virtual int graphicsMemorySize => UnityEngine.SystemInfo.graphicsMemorySize;

        public virtual string graphicsDeviceName => UnityEngine.SystemInfo.graphicsDeviceName;

        public virtual string graphicsDeviceVendor => UnityEngine.SystemInfo.graphicsDeviceVendor;

        public virtual int graphicsDeviceID => UnityEngine.SystemInfo.graphicsDeviceID;

        public virtual int graphicsDeviceVendorID => UnityEngine.SystemInfo.graphicsDeviceVendorID;

        public virtual GraphicsDeviceType graphicsDeviceType => UnityEngine.SystemInfo.graphicsDeviceType;

        public virtual bool graphicsUVStartsAtTop => UnityEngine.SystemInfo.graphicsUVStartsAtTop;

        public virtual string graphicsDeviceVersion => UnityEngine.SystemInfo.graphicsDeviceVersion;

        public virtual int graphicsShaderLevel => UnityEngine.SystemInfo.graphicsShaderLevel;

        public virtual bool graphicsMultiThreaded => UnityEngine.SystemInfo.graphicsMultiThreaded;

        public virtual Rendering.RenderingThreadingMode renderingThreadingMode => UnityEngine.SystemInfo.renderingThreadingMode;

        public virtual bool hasHiddenSurfaceRemovalOnGPU => UnityEngine.SystemInfo.hasHiddenSurfaceRemovalOnGPU;

        public virtual bool hasDynamicUniformArrayIndexingInFragmentShaders =>
            UnityEngine.SystemInfo.hasDynamicUniformArrayIndexingInFragmentShaders;

        public virtual bool supportsShadows => UnityEngine.SystemInfo.supportsShadows;

        public virtual bool supportsRawShadowDepthSampling => UnityEngine.SystemInfo.supportsRawShadowDepthSampling;

        public virtual bool supportsMotionVectors => UnityEngine.SystemInfo.supportsMotionVectors;

        public virtual bool supports3DTextures => UnityEngine.SystemInfo.supports3DTextures;

        public virtual bool supportsCompressed3DTextures => UnityEngine.SystemInfo.supportsCompressed3DTextures;

        public virtual bool supports2DArrayTextures => UnityEngine.SystemInfo.supports2DArrayTextures;

        public virtual bool supports3DRenderTextures => UnityEngine.SystemInfo.supports3DRenderTextures;

        public virtual bool supportsCubemapArrayTextures => UnityEngine.SystemInfo.supportsCubemapArrayTextures;

        public virtual Rendering.CopyTextureSupport copyTextureSupport => UnityEngine.SystemInfo.copyTextureSupport;

        public virtual bool supportsComputeShaders => UnityEngine.SystemInfo.supportsComputeShaders;

        public virtual bool supportsGeometryShaders => UnityEngine.SystemInfo.supportsGeometryShaders;

        public virtual bool supportsTessellationShaders => UnityEngine.SystemInfo.supportsTessellationShaders;

        public virtual bool supportsRenderTargetArrayIndexFromVertexShader => UnityEngine.SystemInfo.supportsRenderTargetArrayIndexFromVertexShader;

        public virtual bool supportsInstancing => UnityEngine.SystemInfo.supportsInstancing;

        public virtual bool supportsHardwareQuadTopology => UnityEngine.SystemInfo.supportsHardwareQuadTopology;

        public virtual bool supports32bitsIndexBuffer => UnityEngine.SystemInfo.supports32bitsIndexBuffer;

        public virtual bool supportsSparseTextures => UnityEngine.SystemInfo.supportsSparseTextures;

        public virtual int supportedRenderTargetCount => UnityEngine.SystemInfo.supportedRenderTargetCount;

        public virtual bool supportsSeparatedRenderTargetsBlend => UnityEngine.SystemInfo.supportsSeparatedRenderTargetsBlend;

        public virtual int supportedRandomWriteTargetCount => UnityEngine.SystemInfo.supportedRandomWriteTargetCount;

        public virtual int supportsMultisampledTextures => UnityEngine.SystemInfo.supportsMultisampledTextures;

        public virtual bool supportsMultisampled2DArrayTextures => UnityEngine.SystemInfo.supportsMultisampled2DArrayTextures;

        public virtual bool supportsMultisampleAutoResolve => UnityEngine.SystemInfo.supportsMultisampleAutoResolve;

        public virtual int supportsTextureWrapMirrorOnce => UnityEngine.SystemInfo.supportsTextureWrapMirrorOnce;

        public virtual bool usesReversedZBuffer => UnityEngine.SystemInfo.usesReversedZBuffer;

        public virtual bool SupportsRenderTextureFormat(RenderTextureFormat format)
        {
            return UnityEngine.SystemInfo.SupportsRenderTextureFormat(format);
        }

        public virtual bool SupportsBlendingOnRenderTextureFormat(RenderTextureFormat format)
        {
            return UnityEngine.SystemInfo.SupportsBlendingOnRenderTextureFormat(format);
        }

        public virtual bool SupportsRandomWriteOnRenderTextureFormat(RenderTextureFormat format)
        {
            return UnityEngine.SystemInfo.SupportsRandomWriteOnRenderTextureFormat(format);
        }

        public virtual bool SupportsTextureFormat(TextureFormat format)
        {
            return UnityEngine.SystemInfo.SupportsTextureFormat(format);
        }

        public virtual bool SupportsVertexAttributeFormat(VertexAttributeFormat format, int dimension)
        {
            return UnityEngine.SystemInfo.SupportsVertexAttributeFormat(format, dimension);
        }

        public virtual NPOTSupport npotSupport => UnityEngine.SystemInfo.npotSupport;

        public virtual int maxTextureSize => UnityEngine.SystemInfo.maxTextureSize;

        public virtual int maxTexture3DSize => UnityEngine.SystemInfo.maxTexture3DSize;

        public virtual int maxTextureArraySlices => UnityEngine.SystemInfo.maxTextureArraySlices;

        public virtual int maxCubemapSize => UnityEngine.SystemInfo.maxCubemapSize;

        public virtual int maxComputeBufferInputsVertex => UnityEngine.SystemInfo.maxComputeBufferInputsVertex;

        public virtual int maxComputeBufferInputsFragment => UnityEngine.SystemInfo.maxComputeBufferInputsFragment;

        public virtual int maxComputeBufferInputsGeometry => UnityEngine.SystemInfo.maxComputeBufferInputsGeometry;

        public virtual int maxComputeBufferInputsDomain => UnityEngine.SystemInfo.maxComputeBufferInputsDomain;

        public virtual int maxComputeBufferInputsHull => UnityEngine.SystemInfo.maxComputeBufferInputsHull;

        public virtual int maxComputeBufferInputsCompute => UnityEngine.SystemInfo.maxComputeBufferInputsCompute;

        public virtual int maxComputeWorkGroupSize => UnityEngine.SystemInfo.maxComputeWorkGroupSize;

        public virtual int maxComputeWorkGroupSizeX => UnityEngine.SystemInfo.maxComputeWorkGroupSizeX;

        public virtual int maxComputeWorkGroupSizeY => UnityEngine.SystemInfo.maxComputeWorkGroupSizeY;

        public virtual int maxComputeWorkGroupSizeZ => UnityEngine.SystemInfo.maxComputeWorkGroupSizeZ;

        public virtual int computeSubGroupSize => UnityEngine.SystemInfo.computeSubGroupSize;

        public virtual bool supportsAsyncCompute => UnityEngine.SystemInfo.supportsAsyncCompute;
        public virtual bool supportsGpuRecorder => UnityEngine.SystemInfo.supportsGpuRecorder;

        public virtual bool supportsGraphicsFence => UnityEngine.SystemInfo.supportsGraphicsFence;

        public virtual bool supportsAsyncGPUReadback => UnityEngine.SystemInfo.supportsAsyncGPUReadback;
        public virtual bool supportsRayTracing => UnityEngine.SystemInfo.supportsRayTracing;

        public virtual bool supportsSetConstantBuffer => UnityEngine.SystemInfo.supportsSetConstantBuffer;

        public virtual int constantBufferOffsetAlignment => UnityEngine.SystemInfo.constantBufferOffsetAlignment;

        public virtual int maxConstantBufferSize => UnityEngine.SystemInfo.maxConstantBufferSize;

        public virtual long maxGraphicsBufferSize => UnityEngine.SystemInfo.maxGraphicsBufferSize;

        [Obsolete("Use SystemInfo.constantBufferOffsetAlignment instead.")]
        public virtual bool minConstantBufferOffsetAlignment => UnityEngine.SystemInfo.minConstantBufferOffsetAlignment;

        public virtual bool hasMipMaxLevel => UnityEngine.SystemInfo.hasMipMaxLevel;

        public virtual bool supportsMipStreaming => UnityEngine.SystemInfo.supportsMipStreaming;

        public virtual bool usesLoadStoreActions => UnityEngine.SystemInfo.usesLoadStoreActions;

        public virtual HDRDisplaySupportFlags hdrDisplaySupportFlags => UnityEngine.SystemInfo.hdrDisplaySupportFlags;

        public virtual bool supportsConservativeRaster => UnityEngine.SystemInfo.supportsConservativeRaster;

        public virtual bool supportsMultiview => UnityEngine.SystemInfo.supportsMultiview;

        public virtual bool supportsStoreAndResolveAction => UnityEngine.SystemInfo.supportsStoreAndResolveAction;

        public virtual bool supportsMultisampleResolveDepth => UnityEngine.SystemInfo.supportsMultisampleResolveDepth;

        public virtual bool supportsMultisampleResolveStencil => UnityEngine.SystemInfo.supportsMultisampleResolveStencil;

        public virtual bool supportsIndirectArgumentsBuffer => UnityEngine.SystemInfo.supportsIndirectArgumentsBuffer;

        public virtual bool IsFormatSupported(GraphicsFormat format, FormatUsage usage)
        {
            return UnityEngine.SystemInfo.IsFormatSupported(format, usage);
        }

        public virtual GraphicsFormat GetCompatibleFormat(GraphicsFormat format, FormatUsage usage)
        {
            return UnityEngine.SystemInfo.GetCompatibleFormat(format, usage);
        }

        public virtual GraphicsFormat GetGraphicsFormat(DefaultFormat format)
        {
            return UnityEngine.SystemInfo.GetGraphicsFormat(format);
        }

        public virtual int GetRenderTextureSupportedMSAASampleCount(RenderTextureDescriptor desc)
        {
            return UnityEngine.SystemInfo.GetRenderTextureSupportedMSAASampleCount(desc);
        }
    }
}

