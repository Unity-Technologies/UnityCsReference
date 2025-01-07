// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace UnityEditor.DeviceSimulation
{
    internal class SystemInfoSimulation : SystemInfoShimBase
    {
        // Many SystemInfo values are optional in a .device file. HashSets store which fields are available. In case an optional field is missing we return a default value for the editor.
        private readonly SystemInfoData m_SystemInfo;
        private readonly HashSet<string> m_SystemInfoFields;
        private readonly GraphicsSystemInfoData m_GraphicsSystemInfo;
        private readonly HashSet<string> m_GraphicsSystemInfoFields;
        public SystemInfoSimulation(DeviceInfoAsset device, SimulationPlayerSettings playerSettings)
        {
            m_SystemInfo = device.deviceInfo.systemInfo;
            m_SystemInfoFields = device.availableSystemInfoFields;
            if (device.deviceInfo.systemInfo.graphicsDependentData?.Length > 0)
            {
                if (device.deviceInfo.IsAndroidDevice())
                {
                    m_GraphicsSystemInfo = (
                        from selected in playerSettings.androidGraphicsAPIs
                        from gfxDevice in m_SystemInfo.graphicsDependentData
                        where selected == gfxDevice.graphicsDeviceType select gfxDevice).FirstOrDefault();
                }
                else if (device.deviceInfo.IsiOSDevice())
                {
                    m_GraphicsSystemInfo = (
                        from selected in playerSettings.iOSGraphicsAPIs
                        from gfxDevice in m_SystemInfo.graphicsDependentData
                        where selected == gfxDevice.graphicsDeviceType select gfxDevice).FirstOrDefault();
                }
                if (m_GraphicsSystemInfo == null)
                {
                    Debug.LogWarning("Could not pick GraphicsDeviceType, the game would fail to launch");
                }
            }
            m_GraphicsSystemInfoFields = m_GraphicsSystemInfo != null ? device.availableGraphicsSystemInfoFields[m_GraphicsSystemInfo.graphicsDeviceType] : new HashSet<string>();
            Enable();
        }

        public void Enable()
        {
            ShimManager.UseShim(this);
        }

        public void Disable()
        {
            ShimManager.RemoveShim(this);
        }

        public void Dispose()
        {
            Disable();
        }

        public override string operatingSystem => m_SystemInfo.operatingSystem;
        public override OperatingSystemFamily operatingSystemFamily => m_SystemInfoFields.Contains("operatingSystemFamily") ? m_SystemInfo.operatingSystemFamily : base.operatingSystemFamily;
        public override string processorType => m_SystemInfoFields.Contains("processorType") ? m_SystemInfo.processorType : base.processorType;
        public override string processorModel => m_SystemInfoFields.Contains("processorModel") ? m_SystemInfo?.processorModel : base.processorModel;
        public override string processorManufacturer => m_SystemInfoFields.Contains("processorManufacturer") ? m_SystemInfo?.processorManufacturer : base.processorManufacturer;
        public override int processorFrequency => m_SystemInfoFields.Contains("processorFrequency") ? m_SystemInfo.processorFrequency : base.processorFrequency;
        public override int processorCount => m_SystemInfoFields.Contains("processorCount") ? m_SystemInfo.processorCount : base.processorCount;
        public override int systemMemorySize => m_SystemInfoFields.Contains("systemMemorySize") ? m_SystemInfo.systemMemorySize : base.systemMemorySize;
        public override string deviceModel => m_SystemInfoFields.Contains("deviceModel") ? m_SystemInfo.deviceModel : base.deviceModel;
        public override bool supportsAccelerometer => m_SystemInfoFields.Contains("supportsAccelerometer") ? m_SystemInfo.supportsAccelerometer : base.supportsAccelerometer;
        public override bool supportsGyroscope => m_SystemInfoFields.Contains("supportsGyroscope") ? m_SystemInfo.supportsGyroscope : base.supportsGyroscope;
        public override bool supportsLocationService => m_SystemInfoFields.Contains("supportsLocationService") ? m_SystemInfo.supportsLocationService : base.supportsLocationService;
        public override bool supportsVibration => m_SystemInfoFields.Contains("supportsVibration") ? m_SystemInfo.supportsVibration : base.supportsVibration;
        public override bool supportsAudio => m_SystemInfoFields.Contains("supportsAudio") ? m_SystemInfo.supportsAudio : base.supportsAudio;
        public override DeviceType deviceType => m_SystemInfoFields.Contains("deviceType") ? m_SystemInfo.deviceType : base.deviceType;

        public override GraphicsDeviceType graphicsDeviceType => m_GraphicsSystemInfo?.graphicsDeviceType ?? base.graphicsDeviceType;
        public override int graphicsMemorySize  =>  m_GraphicsSystemInfoFields.Contains("graphicsMemorySize") ? m_GraphicsSystemInfo.graphicsMemorySize : base.graphicsMemorySize;
        public override string graphicsDeviceName  =>  m_GraphicsSystemInfoFields.Contains("graphicsDeviceName") ? m_GraphicsSystemInfo.graphicsDeviceName : base.graphicsDeviceName;
        public override string graphicsDeviceVendor  =>  m_GraphicsSystemInfoFields.Contains("graphicsDeviceVendor") ? m_GraphicsSystemInfo.graphicsDeviceVendor : base.graphicsDeviceVendor;
        public override int graphicsDeviceID  =>  m_GraphicsSystemInfoFields.Contains("graphicsDeviceID") ? m_GraphicsSystemInfo.graphicsDeviceID : base.graphicsDeviceID;
        public override int graphicsDeviceVendorID  =>  m_GraphicsSystemInfoFields.Contains("graphicsDeviceVendorID") ? m_GraphicsSystemInfo.graphicsDeviceVendorID : base.graphicsDeviceVendorID;
        public override bool graphicsUVStartsAtTop  =>  m_GraphicsSystemInfoFields.Contains("graphicsUVStartsAtTop") ? m_GraphicsSystemInfo.graphicsUVStartsAtTop : base.graphicsUVStartsAtTop;
        public override string graphicsDeviceVersion  =>  m_GraphicsSystemInfoFields.Contains("graphicsDeviceVersion") ? m_GraphicsSystemInfo.graphicsDeviceVersion : base.graphicsDeviceVersion;
        public override int graphicsShaderLevel  =>  m_GraphicsSystemInfoFields.Contains("graphicsShaderLevel") ? m_GraphicsSystemInfo.graphicsShaderLevel : base.graphicsShaderLevel;
        public override bool graphicsMultiThreaded  =>  m_GraphicsSystemInfoFields.Contains("graphicsMultiThreaded") ? m_GraphicsSystemInfo.graphicsMultiThreaded : base.graphicsMultiThreaded;
        public override RenderingThreadingMode renderingThreadingMode  =>  m_GraphicsSystemInfoFields.Contains("renderingThreadingMode") ? m_GraphicsSystemInfo.renderingThreadingMode : base.renderingThreadingMode;
        public override FoveatedRenderingCaps foveatedRenderingCaps => m_GraphicsSystemInfoFields.Contains("foveatedRenderingCaps") ? m_GraphicsSystemInfo.foveatedRenderingCaps : base.foveatedRenderingCaps;
        public override bool supportsVariableRateShading => m_GraphicsSystemInfoFields.Contains("supportsVariableRateShading") ? m_GraphicsSystemInfo.supportsVariableRateShading : base.supportsVariableRateShading;
        public override bool hasTiledGPU => m_GraphicsSystemInfoFields.Contains("hasTiledGPU") ? m_GraphicsSystemInfo.hasTiledGPU : base.hasTiledGPU;
        public override bool hasHiddenSurfaceRemovalOnGPU  =>  m_GraphicsSystemInfoFields.Contains("hasHiddenSurfaceRemovalOnGPU") ? m_GraphicsSystemInfo.hasHiddenSurfaceRemovalOnGPU : base.hasHiddenSurfaceRemovalOnGPU;
        public override bool hasDynamicUniformArrayIndexingInFragmentShaders  =>  m_GraphicsSystemInfoFields.Contains("hasDynamicUniformArrayIndexingInFragmentShaders") ? m_GraphicsSystemInfo.hasDynamicUniformArrayIndexingInFragmentShaders : base.hasDynamicUniformArrayIndexingInFragmentShaders;
        public override bool supportsShadows  =>  m_GraphicsSystemInfoFields.Contains("supportsShadows") ? m_GraphicsSystemInfo.supportsShadows : base.supportsShadows;
        public override bool supportsRawShadowDepthSampling  =>  m_GraphicsSystemInfoFields.Contains("supportsRawShadowDepthSampling") ? m_GraphicsSystemInfo.supportsRawShadowDepthSampling : base.supportsRawShadowDepthSampling;
        public override bool supportsMotionVectors  =>  m_GraphicsSystemInfoFields.Contains("supportsMotionVectors") ? m_GraphicsSystemInfo.supportsMotionVectors : base.supportsMotionVectors;
        public override bool supports3DTextures  =>  m_GraphicsSystemInfoFields.Contains("supports3DTextures") ? m_GraphicsSystemInfo.supports3DTextures : base.supports3DTextures;
        public override bool supports2DArrayTextures  =>  m_GraphicsSystemInfoFields.Contains("supports2DArrayTextures") ? m_GraphicsSystemInfo.supports2DArrayTextures : base.supports2DArrayTextures;
        public override bool supports3DRenderTextures  =>  m_GraphicsSystemInfoFields.Contains("supports3DRenderTextures") ? m_GraphicsSystemInfo.supports3DRenderTextures : base.supports3DRenderTextures;
        public override bool supportsCubemapArrayTextures  =>  m_GraphicsSystemInfoFields.Contains("supportsCubemapArrayTextures") ? m_GraphicsSystemInfo.supportsCubemapArrayTextures : base.supportsCubemapArrayTextures;
        public override CopyTextureSupport copyTextureSupport  =>  m_GraphicsSystemInfoFields.Contains("copyTextureSupport") ? m_GraphicsSystemInfo.copyTextureSupport : base.copyTextureSupport;
        public override bool supportsComputeShaders  =>  m_GraphicsSystemInfoFields.Contains("supportsComputeShaders") ? m_GraphicsSystemInfo.supportsComputeShaders : base.supportsComputeShaders;
        public override bool supportsGeometryShaders  =>  m_GraphicsSystemInfoFields.Contains("supportsGeometryShaders") ? m_GraphicsSystemInfo.supportsGeometryShaders : base.supportsGeometryShaders;
        public override bool supportsTessellationShaders  =>  m_GraphicsSystemInfoFields.Contains("supportsTessellationShaders") ? m_GraphicsSystemInfo.supportsTessellationShaders : base.supportsTessellationShaders;
        public override bool supportsInstancing  =>  m_GraphicsSystemInfoFields.Contains("supportsInstancing") ? m_GraphicsSystemInfo.supportsInstancing : base.supportsInstancing;
        public override bool supportsHardwareQuadTopology  =>  m_GraphicsSystemInfoFields.Contains("supportsHardwareQuadTopology") ? m_GraphicsSystemInfo.supportsHardwareQuadTopology : base.supportsHardwareQuadTopology;
        public override bool supports32bitsIndexBuffer  =>  m_GraphicsSystemInfoFields.Contains("supports32bitsIndexBuffer") ? m_GraphicsSystemInfo.supports32bitsIndexBuffer : base.supports32bitsIndexBuffer;
        public override bool supportsSparseTextures  =>  m_GraphicsSystemInfoFields.Contains("supportsSparseTextures") ? m_GraphicsSystemInfo.supportsSparseTextures : base.supportsSparseTextures;
        public override int supportedRenderTargetCount  =>  m_GraphicsSystemInfoFields.Contains("supportedRenderTargetCount") ? m_GraphicsSystemInfo.supportedRenderTargetCount : base.supportedRenderTargetCount;
        public override bool supportsSeparatedRenderTargetsBlend  =>  m_GraphicsSystemInfoFields.Contains("supportsSeparatedRenderTargetsBlend") ? m_GraphicsSystemInfo.supportsSeparatedRenderTargetsBlend : base.supportsSeparatedRenderTargetsBlend;
        public override int supportedRandomWriteTargetCount  =>  m_GraphicsSystemInfoFields.Contains("supportedRandomWriteTargetCount") ? m_GraphicsSystemInfo.supportedRandomWriteTargetCount : base.supportedRandomWriteTargetCount;
        public override int supportsMultisampledTextures  =>  m_GraphicsSystemInfoFields.Contains("supportsMultisampledTextures") ? m_GraphicsSystemInfo.supportsMultisampledTextures : base.supportsMultisampledTextures;
        public override bool supportsMultisampleAutoResolve  =>  m_GraphicsSystemInfoFields.Contains("supportsMultisampleAutoResolve") ? m_GraphicsSystemInfo.supportsMultisampleAutoResolve : base.supportsMultisampleAutoResolve;
        public override int supportsTextureWrapMirrorOnce  =>  m_GraphicsSystemInfoFields.Contains("supportsTextureWrapMirrorOnce") ? m_GraphicsSystemInfo.supportsTextureWrapMirrorOnce : base.supportsTextureWrapMirrorOnce;
        public override bool usesReversedZBuffer  =>  m_GraphicsSystemInfoFields.Contains("usesReversedZBuffer") ? m_GraphicsSystemInfo.usesReversedZBuffer : base.usesReversedZBuffer;
        public override NPOTSupport npotSupport  =>  m_GraphicsSystemInfoFields.Contains("npotSupport") ? m_GraphicsSystemInfo.npotSupport : base.npotSupport;
        public override int maxTextureSize  =>  m_GraphicsSystemInfoFields.Contains("maxTextureSize") ? m_GraphicsSystemInfo.maxTextureSize : base.maxTextureSize;
        public override int maxCubemapSize  =>  m_GraphicsSystemInfoFields.Contains("maxCubemapSize") ? m_GraphicsSystemInfo.maxCubemapSize : base.maxCubemapSize;
        public override int maxComputeBufferInputsVertex  =>  m_GraphicsSystemInfoFields.Contains("maxComputeBufferInputsVertex") ? m_GraphicsSystemInfo.maxComputeBufferInputsVertex : base.maxComputeBufferInputsVertex;
        public override int maxComputeBufferInputsFragment  =>  m_GraphicsSystemInfoFields.Contains("maxComputeBufferInputsFragment") ? m_GraphicsSystemInfo.maxComputeBufferInputsFragment : base.maxComputeBufferInputsFragment;
        public override int maxComputeBufferInputsGeometry  =>  m_GraphicsSystemInfoFields.Contains("maxComputeBufferInputsGeometry") ? m_GraphicsSystemInfo.maxComputeBufferInputsGeometry : base.maxComputeBufferInputsGeometry;
        public override int maxComputeBufferInputsDomain  =>  m_GraphicsSystemInfoFields.Contains("maxComputeBufferInputsDomain") ? m_GraphicsSystemInfo.maxComputeBufferInputsDomain : base.maxComputeBufferInputsDomain;
        public override int maxComputeBufferInputsHull  =>  m_GraphicsSystemInfoFields.Contains("maxComputeBufferInputsHull") ? m_GraphicsSystemInfo.maxComputeBufferInputsHull : base.maxComputeBufferInputsHull;
        public override int maxComputeBufferInputsCompute  =>  m_GraphicsSystemInfoFields.Contains("maxComputeBufferInputsCompute") ? m_GraphicsSystemInfo.maxComputeBufferInputsCompute : base.maxComputeBufferInputsCompute;
        public override int maxComputeWorkGroupSize  =>  m_GraphicsSystemInfoFields.Contains("maxComputeWorkGroupSize") ? m_GraphicsSystemInfo.maxComputeWorkGroupSize : base.maxComputeWorkGroupSize;
        public override int maxComputeWorkGroupSizeX  =>  m_GraphicsSystemInfoFields.Contains("maxComputeWorkGroupSizeX") ? m_GraphicsSystemInfo.maxComputeWorkGroupSizeX : base.maxComputeWorkGroupSizeX;
        public override int maxComputeWorkGroupSizeY  =>  m_GraphicsSystemInfoFields.Contains("maxComputeWorkGroupSizeY") ? m_GraphicsSystemInfo.maxComputeWorkGroupSizeY : base.maxComputeWorkGroupSizeY;
        public override int maxComputeWorkGroupSizeZ  =>  m_GraphicsSystemInfoFields.Contains("maxComputeWorkGroupSizeZ") ? m_GraphicsSystemInfo.maxComputeWorkGroupSizeZ : base.maxComputeWorkGroupSizeZ;
        public override bool supportsAsyncCompute  =>  m_GraphicsSystemInfoFields.Contains("supportsAsyncCompute") ? m_GraphicsSystemInfo.supportsAsyncCompute : base.supportsAsyncCompute;
        public override bool supportsGraphicsFence  =>  m_GraphicsSystemInfoFields.Contains("supportsGraphicsFence") ? m_GraphicsSystemInfo.supportsGraphicsFence : base.supportsGraphicsFence;
        public override bool supportsAsyncGPUReadback  =>  m_GraphicsSystemInfoFields.Contains("supportsAsyncGPUReadback") ? m_GraphicsSystemInfo.supportsAsyncGPUReadback : base.supportsAsyncGPUReadback;
        public override bool supportsParallelPSOCreation  =>  m_GraphicsSystemInfoFields.Contains("supportsParallelPSOCreation") ? m_GraphicsSystemInfo.supportsParallelPSOCreation : base.supportsParallelPSOCreation;
        public override bool supportsRayTracing => m_GraphicsSystemInfoFields.Contains("supportsRayTracing") ? m_GraphicsSystemInfo.supportsRayTracing : base.supportsRayTracing;
        public override bool supportsRayTracingShaders => m_GraphicsSystemInfoFields.Contains("supportsRayTracingShaders") ? m_GraphicsSystemInfo.supportsRayTracingShaders : base.supportsRayTracingShaders;
        public override bool supportsInlineRayTracing => m_GraphicsSystemInfoFields.Contains("supportsInlineRayTracing") ? m_GraphicsSystemInfo.supportsInlineRayTracing : base.supportsInlineRayTracing;
        public override bool supportsIndirectDispatchRays => m_GraphicsSystemInfoFields.Contains("supportsIndirectDispatchRays") ? m_GraphicsSystemInfo.supportsIndirectDispatchRays : base.supportsIndirectDispatchRays;
        public override bool supportsMachineLearning => m_GraphicsSystemInfoFields.Contains("supportsMachineLearning") ? m_GraphicsSystemInfo.supportsMachineLearning : base.supportsMachineLearning;
        public override bool supportsSetConstantBuffer  =>  m_GraphicsSystemInfoFields.Contains("supportsSetConstantBuffer") ? m_GraphicsSystemInfo.supportsSetConstantBuffer : base.supportsSetConstantBuffer;
        public override bool hasMipMaxLevel  =>  m_GraphicsSystemInfoFields.Contains("hasMipMaxLevel") ? m_GraphicsSystemInfo.hasMipMaxLevel : base.hasMipMaxLevel;
        public override bool supportsMipStreaming  =>  m_GraphicsSystemInfoFields.Contains("supportsMipStreaming") ? m_GraphicsSystemInfo.supportsMipStreaming : base.supportsMipStreaming;
        public override bool usesLoadStoreActions  =>  m_GraphicsSystemInfoFields.Contains("usesLoadStoreActions") ? m_GraphicsSystemInfo.usesLoadStoreActions : base.usesLoadStoreActions;
    }
}
