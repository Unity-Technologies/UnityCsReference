// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.DeviceSimulation
{
    [Serializable]
    internal class DeviceInfo
    {
        public string friendlyName;
        public int version;

        public ScreenData[] screens;
        public SystemInfoData systemInfo;

        public override string ToString()
        {
            return friendlyName;
        }

        public bool IsAndroidDevice()
        {
            return IsGivenDevice("android");
        }

        public bool IsiOSDevice()
        {
            return IsGivenDevice("ios");
        }

        internal bool IsMobileDevice()
        {
            return IsAndroidDevice() || IsiOSDevice();
        }

        internal bool IsConsoleDevice()
        {
            return false; // Return false for now, should revisit when adding console devices.
        }

        private bool IsGivenDevice(string os)
        {
            return systemInfo?.operatingSystem.ToLower().Contains(os) ?? false;
        }
    }

    [Serializable]
    internal class ScreenPresentation
    {
        public string name;
        public string overlayPath;
        public Vector4 borderSize;
        public float cornerRadius;
    }

    [Serializable]
    internal class ScreenData
    {
        public int width;
        public int height;
        public int navigationBarHeight;
        public float dpi;
        public OrientationData[] orientations;
        public ScreenPresentation presentation;
    }

    [Serializable]
    internal class OrientationData
    {
        public ScreenOrientation orientation;
        public Rect safeArea;
        public Rect[] cutouts;
    }

    [Serializable]
    internal class SystemInfoData
    {
        public string deviceModel;
        public DeviceType deviceType;
        public string operatingSystem;
        public OperatingSystemFamily operatingSystemFamily;
        public int processorCount;
        public int processorFrequency;
        public string processorType;
        public string processorModel;
        public string processorManufacturer;
        public bool supportsAccelerometer;
        public bool supportsAudio;
        public bool supportsGyroscope;
        public bool supportsLocationService;
        public bool supportsVibration;
        public int systemMemorySize;
        public GraphicsSystemInfoData[] graphicsDependentData;
    }

    [Serializable]
    internal class GraphicsSystemInfoData
    {
        public GraphicsDeviceType graphicsDeviceType;
        public int graphicsMemorySize;
        public string graphicsDeviceName;
        public string graphicsDeviceVendor;
        public int graphicsDeviceID;
        public int graphicsDeviceVendorID;
        public bool graphicsUVStartsAtTop;
        public string graphicsDeviceVersion;
        public int graphicsShaderLevel;
        public bool graphicsMultiThreaded;
        public RenderingThreadingMode renderingThreadingMode;
        public FoveatedRenderingCaps foveatedRenderingCaps;
        public bool supportsVariableRateShading;
        public bool hasHiddenSurfaceRemovalOnGPU;
        public bool hasDynamicUniformArrayIndexingInFragmentShaders;
        public bool supportsShadows;
        public bool supportsRawShadowDepthSampling;
        public bool supportsMotionVectors;
        public bool supports3DTextures;
        public bool supports2DArrayTextures;
        public bool supports3DRenderTextures;
        public bool supportsCubemapArrayTextures;
        public CopyTextureSupport copyTextureSupport;
        public bool supportsComputeShaders;
        public bool supportsGeometryShaders;
        public bool supportsTessellationShaders;
        public bool supportsInstancing;
        public bool supportsHardwareQuadTopology;
        public bool supports32bitsIndexBuffer;
        public bool supportsSparseTextures;
        public int supportedRenderTargetCount;
        public bool supportsSeparatedRenderTargetsBlend;
        public int supportedRandomWriteTargetCount;
        public int supportsMultisampledTextures;
        public bool supportsMultisampleAutoResolve;
        public int supportsTextureWrapMirrorOnce;
        public bool usesReversedZBuffer;
        public NPOTSupport npotSupport;
        public int maxTextureSize;
        public int maxCubemapSize;
        public int maxComputeBufferInputsVertex;
        public int maxComputeBufferInputsFragment;
        public int maxComputeBufferInputsGeometry;
        public int maxComputeBufferInputsDomain;
        public int maxComputeBufferInputsHull;
        public int maxComputeBufferInputsCompute;
        public int maxComputeWorkGroupSize;
        public int maxComputeWorkGroupSizeX;
        public int maxComputeWorkGroupSizeY;
        public int maxComputeWorkGroupSizeZ;
        public bool supportsAsyncCompute;
        public bool supportsGraphicsFence;
        public bool supportsAsyncGPUReadback;
        public bool supportsParallelPSOCreation;
        public bool supportsRayTracing;
        public bool supportsRayTracingShaders;
        public bool supportsInlineRayTracing;
        public bool supportsIndirectDispatchRays;
        public bool supportsSetConstantBuffer;
        public bool hasMipMaxLevel;
        public bool supportsMipStreaming;
        public bool usesLoadStoreActions;
    }
}
