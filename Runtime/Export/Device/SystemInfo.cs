// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace UnityEngine.Device
{
    public static class SystemInfo
    {
        public const string unsupportedIdentifier = UnityEngine.SystemInfo.unsupportedIdentifier;

        public static float batteryLevel => ShimManager.systemInfoShim.batteryLevel;

        public static BatteryStatus batteryStatus => ShimManager.systemInfoShim.batteryStatus;

        public static string operatingSystem => ShimManager.systemInfoShim.operatingSystem;

        public static OperatingSystemFamily operatingSystemFamily => ShimManager.systemInfoShim.operatingSystemFamily;

        public static string processorType => ShimManager.systemInfoShim.processorType;

        public static string processorModel => ShimManager.systemInfoShim.processorModel;

        public static string processorManufacturer => ShimManager.systemInfoShim.processorManufacturer;

        public static int processorFrequency => ShimManager.systemInfoShim.processorFrequency;

        public static int processorCount => ShimManager.systemInfoShim.processorCount;

        public static int systemMemorySize => ShimManager.systemInfoShim.systemMemorySize;

        public static string deviceUniqueIdentifier => ShimManager.systemInfoShim.deviceUniqueIdentifier;

        public static string deviceName => ShimManager.systemInfoShim.deviceName;

        public static string deviceModel => ShimManager.systemInfoShim.deviceModel;

        public static bool supportsAccelerometer => ShimManager.systemInfoShim.supportsAccelerometer;

        public static bool supportsGyroscope => ShimManager.systemInfoShim.supportsGyroscope;

        public static bool supportsLocationService => ShimManager.systemInfoShim.supportsLocationService;

        public static bool supportsVibration => ShimManager.systemInfoShim.supportsVibration;

        public static bool supportsAudio => ShimManager.systemInfoShim.supportsAudio;

        public static DeviceType deviceType => ShimManager.systemInfoShim.deviceType;

        public static int graphicsMemorySize => ShimManager.systemInfoShim.graphicsMemorySize;

        public static string graphicsDeviceName => ShimManager.systemInfoShim.graphicsDeviceName;

        public static string graphicsDeviceVendor => ShimManager.systemInfoShim.graphicsDeviceVendor;

        public static int graphicsDeviceID => ShimManager.systemInfoShim.graphicsDeviceID;

        public static int graphicsDeviceVendorID => ShimManager.systemInfoShim.graphicsDeviceVendorID;

        public static GraphicsDeviceType graphicsDeviceType => ShimManager.systemInfoShim.graphicsDeviceType;

        public static bool graphicsUVStartsAtTop => ShimManager.systemInfoShim.graphicsUVStartsAtTop;

        public static string graphicsDeviceVersion => ShimManager.systemInfoShim.graphicsDeviceVersion;

        public static int graphicsShaderLevel => ShimManager.systemInfoShim.graphicsShaderLevel;

        public static bool graphicsMultiThreaded => ShimManager.systemInfoShim.graphicsMultiThreaded;

        public static Rendering.RenderingThreadingMode renderingThreadingMode => ShimManager.systemInfoShim.renderingThreadingMode;

        public static FoveatedRenderingCaps foveatedRenderingCaps => ShimManager.systemInfoShim.foveatedRenderingCaps;

        public static bool hasHiddenSurfaceRemovalOnGPU => ShimManager.systemInfoShim.hasHiddenSurfaceRemovalOnGPU;

        public static bool hasDynamicUniformArrayIndexingInFragmentShaders =>
            ShimManager.systemInfoShim.hasDynamicUniformArrayIndexingInFragmentShaders;

        public static bool supportsShadows => ShimManager.systemInfoShim.supportsShadows;

        public static bool supportsRawShadowDepthSampling => ShimManager.systemInfoShim.supportsRawShadowDepthSampling;

        public static bool supportsMotionVectors => ShimManager.systemInfoShim.supportsMotionVectors;

        public static bool supports3DTextures => ShimManager.systemInfoShim.supports3DTextures;

        public static bool supportsCompressed3DTextures => ShimManager.systemInfoShim.supportsCompressed3DTextures;

        public static bool supports2DArrayTextures => ShimManager.systemInfoShim.supports2DArrayTextures;

        public static bool supports3DRenderTextures => ShimManager.systemInfoShim.supports3DRenderTextures;

        public static bool supportsCubemapArrayTextures => ShimManager.systemInfoShim.supportsCubemapArrayTextures;

        public static bool supportsAnisotropicFilter => ShimManager.systemInfoShim.supportsAnisotropicFilter;

        public static Rendering.CopyTextureSupport copyTextureSupport => ShimManager.systemInfoShim.copyTextureSupport;

        public static bool supportsComputeShaders => ShimManager.systemInfoShim.supportsComputeShaders;

        public static bool supportsGeometryShaders => ShimManager.systemInfoShim.supportsGeometryShaders;

        public static bool supportsTessellationShaders => ShimManager.systemInfoShim.supportsTessellationShaders;

        public static bool supportsRenderTargetArrayIndexFromVertexShader => ShimManager.systemInfoShim.supportsRenderTargetArrayIndexFromVertexShader;

        public static bool supportsInstancing => ShimManager.systemInfoShim.supportsInstancing;

        public static bool supportsHardwareQuadTopology => ShimManager.systemInfoShim.supportsHardwareQuadTopology;

        public static bool supports32bitsIndexBuffer => ShimManager.systemInfoShim.supports32bitsIndexBuffer;

        public static bool supportsSparseTextures => ShimManager.systemInfoShim.supportsSparseTextures;

        public static int supportedRenderTargetCount => ShimManager.systemInfoShim.supportedRenderTargetCount;

        public static bool supportsSeparatedRenderTargetsBlend => ShimManager.systemInfoShim.supportsSeparatedRenderTargetsBlend;

        public static int supportedRandomWriteTargetCount => ShimManager.systemInfoShim.supportedRandomWriteTargetCount;

        public static int supportsMultisampledTextures => ShimManager.systemInfoShim.supportsMultisampledTextures;

        public static bool supportsMultisampled2DArrayTextures => ShimManager.systemInfoShim.supportsMultisampled2DArrayTextures;

        public static bool supportsMultisampleAutoResolve => ShimManager.systemInfoShim.supportsMultisampleAutoResolve;

        public static int supportsTextureWrapMirrorOnce => ShimManager.systemInfoShim.supportsTextureWrapMirrorOnce;

        public static bool usesReversedZBuffer => ShimManager.systemInfoShim.usesReversedZBuffer;

        public static bool SupportsRenderTextureFormat(RenderTextureFormat format)
        {
            return ShimManager.systemInfoShim.SupportsRenderTextureFormat(format);
        }

        public static bool SupportsBlendingOnRenderTextureFormat(RenderTextureFormat format)
        {
            return ShimManager.systemInfoShim.SupportsBlendingOnRenderTextureFormat(format);
        }

        public static bool SupportsTextureFormat(TextureFormat format)
        {
            return ShimManager.systemInfoShim.SupportsTextureFormat(format);
        }

        public static bool SupportsVertexAttributeFormat(VertexAttributeFormat format, int dimension)
        {
            return ShimManager.systemInfoShim.SupportsVertexAttributeFormat(format, dimension);
        }

        public static NPOTSupport npotSupport => ShimManager.systemInfoShim.npotSupport;

        public static int maxTextureSize => ShimManager.systemInfoShim.maxTextureSize;

        public static int maxTexture3DSize => ShimManager.systemInfoShim.maxTexture3DSize;

        public static int maxTextureArraySlices => ShimManager.systemInfoShim.maxTextureArraySlices;

        public static int maxCubemapSize => ShimManager.systemInfoShim.maxCubemapSize;

        public static int maxAnisotropyLevel => ShimManager.systemInfoShim.maxAnisotropyLevel;

        public static int maxComputeBufferInputsVertex => ShimManager.systemInfoShim.maxComputeBufferInputsVertex;

        public static int maxComputeBufferInputsFragment => ShimManager.systemInfoShim.maxComputeBufferInputsFragment;

        public static int maxComputeBufferInputsGeometry => ShimManager.systemInfoShim.maxComputeBufferInputsGeometry;

        public static int maxComputeBufferInputsDomain => ShimManager.systemInfoShim.maxComputeBufferInputsDomain;

        public static int maxComputeBufferInputsHull => ShimManager.systemInfoShim.maxComputeBufferInputsHull;

        public static int maxComputeBufferInputsCompute => ShimManager.systemInfoShim.maxComputeBufferInputsCompute;

        public static int maxComputeWorkGroupSize => ShimManager.systemInfoShim.maxComputeWorkGroupSize;

        public static int maxComputeWorkGroupSizeX => ShimManager.systemInfoShim.maxComputeWorkGroupSizeX;

        public static int maxComputeWorkGroupSizeY => ShimManager.systemInfoShim.maxComputeWorkGroupSizeY;

        public static int maxComputeWorkGroupSizeZ => ShimManager.systemInfoShim.maxComputeWorkGroupSizeZ;

        public static int computeSubGroupSize => ShimManager.systemInfoShim.computeSubGroupSize;

        public static bool supportsAsyncCompute => ShimManager.systemInfoShim.supportsAsyncCompute;
        public static bool supportsGpuRecorder => ShimManager.systemInfoShim.supportsGpuRecorder;

        public static bool supportsGraphicsFence => ShimManager.systemInfoShim.supportsGraphicsFence;

        public static bool supportsAsyncGPUReadback => ShimManager.systemInfoShim.supportsAsyncGPUReadback;
        public static bool supportsParallelPSOCreation => ShimManager.systemInfoShim.supportsParallelPSOCreation;
        public static bool supportsRayTracingShaders => ShimManager.systemInfoShim.supportsRayTracingShaders;
        public static bool supportsRayTracing => ShimManager.systemInfoShim.supportsRayTracing;
        public static bool supportsInlineRayTracing => ShimManager.systemInfoShim.supportsInlineRayTracing;
        public static bool supportsIndirectDispatchRays => ShimManager.systemInfoShim.supportsIndirectDispatchRays;

        public static bool supportsSetConstantBuffer => ShimManager.systemInfoShim.supportsSetConstantBuffer;

        public static int constantBufferOffsetAlignment => ShimManager.systemInfoShim.constantBufferOffsetAlignment;

        public static int maxConstantBufferSize => ShimManager.systemInfoShim.maxConstantBufferSize;

        public static long maxGraphicsBufferSize => ShimManager.systemInfoShim.maxGraphicsBufferSize;

        public static bool hasMipMaxLevel => ShimManager.systemInfoShim.hasMipMaxLevel;

        public static bool supportsMipStreaming => ShimManager.systemInfoShim.supportsMipStreaming;

        public static bool usesLoadStoreActions => ShimManager.systemInfoShim.usesLoadStoreActions;

        public static HDRDisplaySupportFlags hdrDisplaySupportFlags => ShimManager.systemInfoShim.hdrDisplaySupportFlags;

        public static bool supportsConservativeRaster => ShimManager.systemInfoShim.supportsConservativeRaster;

        public static bool supportsMultiview => ShimManager.systemInfoShim.supportsMultiview;

        public static bool supportsStoreAndResolveAction => ShimManager.systemInfoShim.supportsStoreAndResolveAction;

        public static bool supportsMultisampleResolveDepth => ShimManager.systemInfoShim.supportsMultisampleResolveDepth;

        public static bool supportsMultisampleResolveStencil => ShimManager.systemInfoShim.supportsMultisampleResolveStencil;

        public static bool supportsIndirectArgumentsBuffer => ShimManager.systemInfoShim.supportsIndirectArgumentsBuffer;

        [System.Obsolete("Use overload with a GraphicsFormatUsage parameter instead", false)]
        public static bool IsFormatSupported(GraphicsFormat format, FormatUsage usage)
        {
            GraphicsFormatUsage graphicsFormatUsage = (GraphicsFormatUsage)(1 << (int)usage);
            return IsFormatSupported(format, graphicsFormatUsage);
        }

        public static bool IsFormatSupported(GraphicsFormat format, GraphicsFormatUsage usage)
        {
            return ShimManager.systemInfoShim.IsFormatSupported(format, usage);
        }

        [System.Obsolete("Use overload with a GraphicsFormatUsage parameter instead", false)]
        public static GraphicsFormat GetCompatibleFormat(GraphicsFormat format, FormatUsage usage)
        {
            GraphicsFormatUsage graphicsFormatUsage = (GraphicsFormatUsage)(1 << (int)usage);
            return GetCompatibleFormat(format, graphicsFormatUsage);
        }

        public static GraphicsFormat GetCompatibleFormat(GraphicsFormat format, GraphicsFormatUsage usage)
        {
            return ShimManager.systemInfoShim.GetCompatibleFormat(format, usage);
        }

        public static GraphicsFormat GetGraphicsFormat(DefaultFormat format)
        {
            return ShimManager.systemInfoShim.GetGraphicsFormat(format);
        }

        public static int GetRenderTextureSupportedMSAASampleCount(RenderTextureDescriptor desc)
        {
            return ShimManager.systemInfoShim.GetRenderTextureSupportedMSAASampleCount(desc);
        }

        public static bool SupportsRandomWriteOnRenderTextureFormat(RenderTextureFormat format)
        {
            return ShimManager.systemInfoShim.SupportsRandomWriteOnRenderTextureFormat(format);
        }

    }
}
