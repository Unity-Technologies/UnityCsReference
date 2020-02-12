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
        public virtual string unsupportedIdentifier => EditorSystemInfo.unsupportedIdentifier;

        public virtual float batteryLevel => EditorSystemInfo.batteryLevel;

        public virtual BatteryStatus batteryStatus => EditorSystemInfo.batteryStatus;

        public virtual string operatingSystem => EditorSystemInfo.operatingSystem;

        public virtual OperatingSystemFamily operatingSystemFamily => EditorSystemInfo.operatingSystemFamily;

        public virtual string processorType => EditorSystemInfo.processorType;

        public virtual int processorFrequency => EditorSystemInfo.processorFrequency;

        public virtual int processorCount => EditorSystemInfo.processorCount;

        public virtual int systemMemorySize => EditorSystemInfo.systemMemorySize;

        public virtual string deviceUniqueIdentifier => EditorSystemInfo.deviceUniqueIdentifier;

        public virtual string deviceName => EditorSystemInfo.deviceName;

        public virtual string deviceModel => EditorSystemInfo.deviceModel;

        public virtual bool supportsAccelerometer => EditorSystemInfo.supportsAccelerometer;

        public virtual bool supportsGyroscope => EditorSystemInfo.supportsGyroscope;

        public virtual bool supportsLocationService => EditorSystemInfo.supportsLocationService;

        public virtual bool supportsVibration => EditorSystemInfo.supportsVibration;

        public virtual bool supportsAudio => EditorSystemInfo.supportsAudio;

        public virtual DeviceType deviceType => EditorSystemInfo.deviceType;

        public virtual int graphicsMemorySize => EditorSystemInfo.graphicsMemorySize;

        public virtual string graphicsDeviceName => EditorSystemInfo.graphicsDeviceName;

        public virtual string graphicsDeviceVendor => EditorSystemInfo.graphicsDeviceVendor;

        public virtual int graphicsDeviceID => EditorSystemInfo.graphicsDeviceID;

        public virtual int graphicsDeviceVendorID => EditorSystemInfo.graphicsDeviceVendorID;

        public virtual GraphicsDeviceType graphicsDeviceType => EditorSystemInfo.graphicsDeviceType;

        public virtual bool graphicsUVStartsAtTop => EditorSystemInfo.graphicsUVStartsAtTop;

        public virtual string graphicsDeviceVersion => EditorSystemInfo.graphicsDeviceVersion;

        public virtual int graphicsShaderLevel => EditorSystemInfo.graphicsShaderLevel;

        public virtual bool graphicsMultiThreaded => EditorSystemInfo.graphicsMultiThreaded;

        public virtual Rendering.RenderingThreadingMode renderingThreadingMode => EditorSystemInfo.renderingThreadingMode;

        public virtual bool hasHiddenSurfaceRemovalOnGPU => EditorSystemInfo.hasHiddenSurfaceRemovalOnGPU;

        public virtual bool hasDynamicUniformArrayIndexingInFragmentShaders =>
            EditorSystemInfo.hasDynamicUniformArrayIndexingInFragmentShaders;

        public virtual bool supportsShadows => EditorSystemInfo.supportsShadows;

        public virtual bool supportsRawShadowDepthSampling => EditorSystemInfo.supportsRawShadowDepthSampling;

        public virtual bool supportsMotionVectors => EditorSystemInfo.supportsMotionVectors;

        public virtual bool supports3DTextures => EditorSystemInfo.supports3DTextures;

        public virtual bool supportsCompressed3DTextures => EditorSystemInfo.supportsCompressed3DTextures;

        public virtual bool supports2DArrayTextures => EditorSystemInfo.supports2DArrayTextures;

        public virtual bool supports3DRenderTextures => EditorSystemInfo.supports3DRenderTextures;

        public virtual bool supportsCubemapArrayTextures => EditorSystemInfo.supportsCubemapArrayTextures;

        public virtual Rendering.CopyTextureSupport copyTextureSupport => EditorSystemInfo.copyTextureSupport;

        public virtual bool supportsComputeShaders => EditorSystemInfo.supportsComputeShaders;

        public virtual bool supportsGeometryShaders => EditorSystemInfo.supportsGeometryShaders;

        public virtual bool supportsTessellationShaders => EditorSystemInfo.supportsTessellationShaders;

        public virtual bool supportsInstancing => EditorSystemInfo.supportsInstancing;

        public virtual bool supportsHardwareQuadTopology => EditorSystemInfo.supportsHardwareQuadTopology;

        public virtual bool supports32bitsIndexBuffer => EditorSystemInfo.supports32bitsIndexBuffer;

        public virtual bool supportsSparseTextures => EditorSystemInfo.supportsSparseTextures;

        public virtual int supportedRenderTargetCount => EditorSystemInfo.supportedRenderTargetCount;

        public virtual bool supportsSeparatedRenderTargetsBlend => EditorSystemInfo.supportsSeparatedRenderTargetsBlend;

        public virtual int supportedRandomWriteTargetCount => EditorSystemInfo.supportedRandomWriteTargetCount;

        public virtual int supportsMultisampledTextures => EditorSystemInfo.supportsMultisampledTextures;

        public virtual bool supportsMultisampleAutoResolve => EditorSystemInfo.supportsMultisampleAutoResolve;

        public virtual int supportsTextureWrapMirrorOnce => EditorSystemInfo.supportsTextureWrapMirrorOnce;

        public virtual bool usesReversedZBuffer => EditorSystemInfo.usesReversedZBuffer;

        public virtual bool SupportsRenderTextureFormat(RenderTextureFormat format)
        {
            return EditorSystemInfo.SupportsRenderTextureFormat(format);
        }

        public virtual bool SupportsBlendingOnRenderTextureFormat(RenderTextureFormat format)
        {
            return EditorSystemInfo.SupportsBlendingOnRenderTextureFormat(format);
        }

        public virtual bool SupportsTextureFormat(TextureFormat format)
        {
            return EditorSystemInfo.SupportsTextureFormat(format);
        }

        public virtual bool SupportsVertexAttributeFormat(VertexAttributeFormat format, int dimension)
        {
            return EditorSystemInfo.SupportsVertexAttributeFormat(format, dimension);
        }

        public virtual NPOTSupport npotSupport => EditorSystemInfo.npotSupport;

        public virtual int maxTextureSize => EditorSystemInfo.maxTextureSize;

        public virtual int maxCubemapSize => EditorSystemInfo.maxCubemapSize;

        public virtual int maxComputeBufferInputsVertex => EditorSystemInfo.maxComputeBufferInputsVertex;

        public virtual int maxComputeBufferInputsFragment => EditorSystemInfo.maxComputeBufferInputsFragment;

        public virtual int maxComputeBufferInputsGeometry => EditorSystemInfo.maxComputeBufferInputsGeometry;

        public virtual int maxComputeBufferInputsDomain => EditorSystemInfo.maxComputeBufferInputsDomain;

        public virtual int maxComputeBufferInputsHull => EditorSystemInfo.maxComputeBufferInputsHull;

        public virtual int maxComputeBufferInputsCompute => EditorSystemInfo.maxComputeBufferInputsCompute;

        public virtual int maxComputeWorkGroupSize => EditorSystemInfo.maxComputeWorkGroupSize;

        public virtual int maxComputeWorkGroupSizeX => EditorSystemInfo.maxComputeWorkGroupSizeX;

        public virtual int maxComputeWorkGroupSizeY => EditorSystemInfo.maxComputeWorkGroupSizeY;

        public virtual int maxComputeWorkGroupSizeZ => EditorSystemInfo.maxComputeWorkGroupSizeZ;

        public virtual bool supportsAsyncCompute => EditorSystemInfo.supportsAsyncCompute;
        public virtual bool supportsGpuRecorder => EditorSystemInfo.supportsGpuRecorder;

        public virtual bool supportsGraphicsFence => EditorSystemInfo.supportsGraphicsFence;

        public virtual bool supportsAsyncGPUReadback => EditorSystemInfo.supportsAsyncGPUReadback;
        public virtual bool supportsRayTracing => EditorSystemInfo.supportsRayTracing;

        public virtual bool supportsSetConstantBuffer => EditorSystemInfo.supportsSetConstantBuffer;

        public virtual bool minConstantBufferOffsetAlignment => EditorSystemInfo.minConstantBufferOffsetAlignment;

        public virtual bool hasMipMaxLevel => EditorSystemInfo.hasMipMaxLevel;

        public virtual bool supportsMipStreaming => EditorSystemInfo.supportsMipStreaming;

        public virtual bool usesLoadStoreActions => EditorSystemInfo.usesLoadStoreActions;

        public virtual HDRDisplaySupportFlags hdrDisplaySupportFlags => EditorSystemInfo.hdrDisplaySupportFlags;

        public virtual bool supportsConservativeRaster => EditorSystemInfo.supportsConservativeRaster;

        public virtual bool IsFormatSupported(GraphicsFormat format, FormatUsage usage)
        {
            return EditorSystemInfo.IsFormatSupported(format, usage);
        }

        public virtual GraphicsFormat GetCompatibleFormat(GraphicsFormat format, FormatUsage usage)
        {
            return EditorSystemInfo.GetCompatibleFormat(format, usage);
        }

        public virtual GraphicsFormat GetGraphicsFormat(DefaultFormat format)
        {
            return EditorSystemInfo.GetGraphicsFormat(format);
        }
    }
}

