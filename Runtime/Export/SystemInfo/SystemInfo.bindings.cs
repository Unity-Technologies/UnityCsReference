// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine
{
    // Enumeration for [[SystemInfo.batteryStatus]]
    public enum BatteryStatus
    {
        Unknown = 0,
        Charging = 1,
        Discharging = 2,
        NotCharging = 3,
        Full = 4
    }

    // The operating system family application is running. Returned by SystemInfo.operatingSystemFamily.
    // NOTE: also match with enum in SystemInfo.h
    // ADD_NEW_OPERATING_SYSTEM_FAMILY_HERE
    public enum OperatingSystemFamily
    {
        // For operating systems that do not fall into any other category.
        Other = 0,

        // MacOSX operating system family.
        MacOSX = 1,

        // Windows operating system family.
        Windows = 2,

        // Linux operating system family.
        Linux = 3,
    }

    // Enumeration for [[SystemInfo.deviceType]], denotes a coarse grouping of kinds of devices.
    public enum DeviceType
    {
        // Device type is unknown. You should never see this in practice.
        Unknown = 0,
        // A handheld device like mobile phone or a tablet.
        Handheld = 1,
        // A stationary gaming console.
        Console = 2,
        // Desktop or laptop computer.
        Desktop = 3,
    }

    // Access system information.
    [NativeHeader("Runtime/Misc/SystemInfo.h")]
    [NativeHeader("Runtime/Shaders/GraphicsCapsScriptBindings.h")]
    [NativeHeader("Runtime/Graphics/GraphicsFormatUtility.bindings.h")]
    [NativeHeader("Runtime/Graphics/Mesh/MeshScriptBindings.h")]
    [NativeHeader("Runtime/Camera/RenderLoops/MotionVectorRenderLoop.h")]
    [NativeHeader("Runtime/Input/GetInput.h")]
    internal sealed class EditorSystemInfo
    {
        public const string unsupportedIdentifier = "n/a";

        [NativeProperty()]
        public static float batteryLevel
        {
            get { return GetBatteryLevel(); }
        }

        public static BatteryStatus batteryStatus
        {
            get { return GetBatteryStatus(); }
        }

        // Operating system name with version (RO).
        public static string operatingSystem
        {
            get { return GetOperatingSystem(); }
        }

        // Operating system family the game is running on (RO).
        public static OperatingSystemFamily operatingSystemFamily
        {
            get { return GetOperatingSystemFamily(); }
        }

        // Processor name (RO).
        public static string processorType
        {
            get { return GetProcessorType(); }
        }

        public static int processorFrequency
        {
            get { return GetProcessorFrequencyMHz(); }
        }

        // Number of processors present (RO).
        public static int processorCount
        {
            get { return GetProcessorCount(); }
        }

        // Amount of system memory present (RO).
        public static int systemMemorySize
        {
            get { return GetPhysicalMemoryMB(); }
        }

        //A unique device identifier. It is guaranteed to be unique for every device (RO).
        public static string deviceUniqueIdentifier
        {
            get { return GetDeviceUniqueIdentifier(); }
        }

        // The user defined name of the device (RO).
        public static string deviceName
        {
            get { return GetDeviceName(); }
        }

        // The model of the device (RO).
        public static string deviceModel
        {
            get { return GetDeviceModel(); }
        }

        // Returns a boolean value that indicates whether an accelerometer is
        public static bool supportsAccelerometer
        {
            get { return SupportsAccelerometer(); }
        }

        // Returns a boolean value that indicates whether a gyroscope is available
        public static bool supportsGyroscope
        {
            get { return IsGyroAvailable(); }
        }

        // Returns a boolean value that indicates whether the device is capable to
        public static bool supportsLocationService
        {
            get { return SupportsLocationService(); }
        }

        // Returns a boolean value that indicates whether the device is capable to
        public static bool supportsVibration
        {
            get { return SupportsVibration(); }
        }

        public static bool supportsAudio
        {
            get { return SupportsAudio(); }
        }

        // Returns the kind of device the application is running on. See [[DeviceType]] enumeration for possible values.
        public static DeviceType deviceType
        {
            get { return GetDeviceType(); }
        }

        public static int graphicsMemorySize
        {
            get { return GetGraphicsMemorySize(); }
        }

        // The name of the graphics device (RO).
        public static string graphicsDeviceName
        {
            get { return GetGraphicsDeviceName(); }
        }

        // The vendor of the graphics device (RO).
        public static string graphicsDeviceVendor
        {
            get { return GetGraphicsDeviceVendor(); }
        }

        // The identifier code of the graphics device (RO).
        public static int graphicsDeviceID
        {
            get { return GetGraphicsDeviceID(); }
        }

        // The identifier code of the graphics device vendor (RO).
        public static int graphicsDeviceVendorID
        {
            get { return GetGraphicsDeviceVendorID(); }
        }

        public static Rendering.GraphicsDeviceType graphicsDeviceType
        {
            get { return GetGraphicsDeviceType(); }
        }

        public static bool graphicsUVStartsAtTop
        {
            get { return GetGraphicsUVStartsAtTop(); }
        }

        // The graphics API version supported by the graphics device (RO).
        public static string graphicsDeviceVersion
        {
            get { return GetGraphicsDeviceVersion(); }
        }

        public static int graphicsShaderLevel
        {
            get { return GetGraphicsShaderLevel(); }
        }

        public static bool graphicsMultiThreaded
        {
            get { return GetGraphicsMultiThreaded(); }
        }

        public static Rendering.RenderingThreadingMode renderingThreadingMode
        {
            get { return GetRenderingThreadingMode(); }
        }

        public static bool hasHiddenSurfaceRemovalOnGPU
        {
            get { return HasHiddenSurfaceRemovalOnGPU(); }
        }

        public static bool hasDynamicUniformArrayIndexingInFragmentShaders
        {
            get { return HasDynamicUniformArrayIndexingInFragmentShaders(); }
        }

        // Are built-in shadows supported? (RO)
        public static bool supportsShadows
        {
            get { return SupportsShadows(); }
        }

        public static bool supportsRawShadowDepthSampling
        {
            get { return SupportsRawShadowDepthSampling(); }
        }

        [Obsolete("supportsRenderTextures always returns true, no need to call it")]
        public static bool supportsRenderTextures
        {
            get { return true; }
        }

        public static bool supportsMotionVectors
        {
            get { return SupportsMotionVectors(); }
        }

        [Obsolete("supportsRenderToCubemap always returns true, no need to call it")]
        public static bool supportsRenderToCubemap
        {
            get { return true; } // all platforms support these days
        }

        [Obsolete("supportsImageEffects always returns true, no need to call it")]
        public static bool supportsImageEffects
        {
            get { return true; } // all platforms support these days
        }

        public static bool supports3DTextures
        {
            get { return Supports3DTextures(); }
        }

        public static bool supportsCompressed3DTextures
        {
            get { return SupportsCompressed3DTextures(); }
        }

        public static bool supports2DArrayTextures
        {
            get { return Supports2DArrayTextures(); }
        }

        // Is rendering into 3D texture volumes supported? (RO)
        public static bool supports3DRenderTextures
        {
            get { return Supports3DRenderTextures(); }
        }

        public static bool supportsCubemapArrayTextures
        {
            get { return SupportsCubemapArrayTextures(); }
        }

        public static Rendering.CopyTextureSupport copyTextureSupport
        {
            get { return GetCopyTextureSupport(); }
        }

        // Are compute shaders supported? (RO)
        public static bool supportsComputeShaders
        {
            get { return SupportsComputeShaders(); }
        }

        // Are geometry shaders supported? (RO)
        public static bool supportsGeometryShaders
        {
            get { return SupportsGeometryShaders(); }
        }

        // Are tessellation shaders supported? (RO)
        public static bool supportsTessellationShaders
        {
            get { return SupportsTessellationShaders(); }
        }

        // Is GPU draw call instancing supported? (RO)
        public static bool supportsInstancing
        {
            get { return SupportsInstancing(); }
        }

        // Is quad topology supported by hardware? (RO)
        public static bool supportsHardwareQuadTopology
        {
            get { return SupportsHardwareQuadTopology(); }
        }

        // Is 32bits index buffer supported? (RO)
        public static bool supports32bitsIndexBuffer
        {
            get { return Supports32bitsIndexBuffer(); }
        }

        public static bool supportsSparseTextures
        {
            get { return SupportsSparseTextures(); }
        }

        // How many simultaneous render targets (MRTs) are supported? (RO)
        public static int supportedRenderTargetCount
        {
            get { return SupportedRenderTargetCount(); }
        }

        public static bool supportsSeparatedRenderTargetsBlend
        {
            get { return SupportsSeparatedRenderTargetsBlend(); }
        }

        public static int supportedRandomWriteTargetCount
        {
            get { return SupportedRandomWriteTargetCount(); }
        }

        public static int supportsMultisampledTextures
        {
            get { return SupportsMultisampledTextures(); }
        }

        public static bool supportsMultisampleAutoResolve
        {
            get { return SupportsMultisampleAutoResolve(); }
        }

        public static int supportsTextureWrapMirrorOnce
        {
            get { return SupportsTextureWrapMirrorOnce(); }
        }

        // Does the current platform use a reversed Z-Buffer (1->0)? (RO)
        public static bool usesReversedZBuffer
        {
            get { return UsesReversedZBuffer(); }
        }

        [Obsolete("supportsStencil always returns true, no need to call it")]
        public static int supportsStencil
        {
            get { return 1; }
        }

        // The enums are only marked as obsolete in the editor.
        /// <summary>
        /// Determine if enum value is obsolete.
        /// If multiple enum members refer to the same value,
        /// the value is considered obsolete if all members are marked obsolete.
        /// </summary>
        internal static bool IsEnumValueObsolete(Enum value)
        {
            foreach (var enumMember in value.GetType().GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                if (!object.Equals(enumMember.GetValue(null), value)) continue;

                var isObsolete = enumMember.GetCustomAttributes(typeof(ObsoleteAttribute), false).Length != 0;
                if (!isObsolete)
                {
                    return false;
                }
            }

            return true;
        }


        static bool IsValidEnumValue(Enum value)
        {
            if (!Enum.IsDefined(value.GetType(), value))
                return false;

            if (IsEnumValueObsolete(value))
                return false;

            return true;
        }

        // Is render texture format supported?
        public static bool SupportsRenderTextureFormat(RenderTextureFormat format)
        {
            if (!IsValidEnumValue(format))
                throw new ArgumentException("Failed SupportsRenderTextureFormat; format is not a valid RenderTextureFormat");

            return HasRenderTextureNative(format);
        }

        // Is render texture format supports blending?
        public static bool SupportsBlendingOnRenderTextureFormat(RenderTextureFormat format)
        {
            if (!IsValidEnumValue(format))
                throw new ArgumentException("Failed SupportsBlendingOnRenderTextureFormat; format is not a valid RenderTextureFormat");

            return SupportsBlendingOnRenderTextureFormatNative(format);
        }

        public static bool SupportsTextureFormat(TextureFormat format)
        {
            if (!IsValidEnumValue(format))
                throw new ArgumentException("Failed SupportsTextureFormat; format is not a valid TextureFormat");

            return SupportsTextureFormatNative(format);
        }

        public static bool SupportsVertexAttributeFormat(VertexAttributeFormat format, int dimension)
        {
            if (!IsValidEnumValue(format))
                throw new ArgumentException("Failed SupportsVertexAttributeFormat; format is not a valid VertexAttributeFormat");
            if (dimension < 1 || dimension > 4)
                throw new ArgumentException("Failed SupportsVertexAttributeFormat; dimension must be in 1..4 range");
            return SupportsVertexAttributeFormatNative(format, dimension);
        }

        /// What [[NPOT|NPOTSupport]] support does GPU provide? (RO)
        ///
        /// SA: [[NPOTSupport]] enum.
        public static NPOTSupport npotSupport
        {
            get { return GetNPOTSupport(); }
        }

        public static int maxTextureSize
        {
            get { return GetMaxTextureSize(); }
        }

        public static int maxCubemapSize
        {
            get { return GetMaxCubemapSize(); }
        }

        internal static int maxRenderTextureSize
        {
            get { return GetMaxRenderTextureSize(); }
        }

        public static int maxComputeBufferInputsVertex
        {
            get { return MaxComputeBufferInputsVertex(); }
        }

        public static int maxComputeBufferInputsFragment
        {
            get { return MaxComputeBufferInputsFragment(); }
        }

        public static int maxComputeBufferInputsGeometry
        {
            get { return MaxComputeBufferInputsGeometry(); }
        }

        public static int maxComputeBufferInputsDomain
        {
            get { return MaxComputeBufferInputsDomain(); }
        }

        public static int maxComputeBufferInputsHull
        {
            get { return MaxComputeBufferInputsHull(); }
        }

        public static int maxComputeBufferInputsCompute
        {
            get { return MaxComputeBufferInputsCompute(); }
        }

        public static int maxComputeWorkGroupSize
        {
            get { return GetMaxComputeWorkGroupSize(); }
        }

        public static int maxComputeWorkGroupSizeX
        {
            get { return GetMaxComputeWorkGroupSizeX(); }
        }

        public static int maxComputeWorkGroupSizeY
        {
            get { return GetMaxComputeWorkGroupSizeY(); }
        }

        public static int maxComputeWorkGroupSizeZ
        {
            get { return GetMaxComputeWorkGroupSizeZ(); }
        }

        public static bool supportsAsyncCompute
        {
            get { return SupportsAsyncCompute(); }
        }
        // support of GPU Recorder API
        public static bool supportsGpuRecorder
        {
            get { return SupportsGpuRecorder(); }
        }

        public static bool supportsGraphicsFence
        {
            // Note that on the native side we'll still use the old GPUFence terms
            get { return SupportsGPUFence(); }
        }

        public static bool supportsAsyncGPUReadback
        {
            get { return SupportsAsyncGPUReadback(); }
        }
        public static bool supportsRayTracing
        {
            get { return SupportsRayTracing(); }
        }

        public static bool supportsSetConstantBuffer
        {
            get { return SupportsSetConstantBuffer(); }
        }
        public static bool minConstantBufferOffsetAlignment
        {
            get { return MinConstantBufferOffsetAlignment(); }
        }

        public static bool hasMipMaxLevel
        {
            get { return HasMipMaxLevel(); }
        }

        public static bool supportsMipStreaming
        {
            get { return SupportsMipStreaming(); }
        }

        [Obsolete("graphicsPixelFillrate is no longer supported in Unity 5.0+.")]
        public static int graphicsPixelFillrate
        {
            get
            {
                return -1; // was already indicating "unknown GPU/platform" back when we had some support for it
            }
        }

        public static bool usesLoadStoreActions
        {
            get { return UsesLoadStoreActions(); }
        }

        public static HDRDisplaySupportFlags hdrDisplaySupportFlags
        {
            get { return GetHDRDisplaySupportFlags(); }
        }

        public static bool supportsConservativeRaster
        {
            get { return SupportsConservativeRaster(); }
        }

        [Obsolete("Vertex program support is required in Unity 5.0+")]
        public static bool supportsVertexPrograms { get { return true; } }

        [FreeFunction("systeminfo::GetBatteryLevel")]
        static extern float GetBatteryLevel();

        [FreeFunction("systeminfo::GetBatteryStatus")]
        static extern BatteryStatus GetBatteryStatus();

        [FreeFunction("systeminfo::GetOperatingSystem")]
        static extern string GetOperatingSystem();

        [FreeFunction("systeminfo::GetOperatingSystemFamily")]
        static extern OperatingSystemFamily GetOperatingSystemFamily();

        [FreeFunction("systeminfo::GetProcessorType")]
        static extern string GetProcessorType();

        [FreeFunction("systeminfo::GetProcessorFrequencyMHz")]
        static extern int GetProcessorFrequencyMHz();

        [FreeFunction("systeminfo::GetProcessorCount")]
        static extern int GetProcessorCount();

        [FreeFunction("systeminfo::GetPhysicalMemoryMB")]
        static extern int GetPhysicalMemoryMB();

        [FreeFunction("systeminfo::GetDeviceUniqueIdentifier")]
        static extern string GetDeviceUniqueIdentifier();

        [FreeFunction("systeminfo::GetDeviceName")]
        static extern string GetDeviceName();

        [FreeFunction("systeminfo::GetDeviceModel")]
        static extern string GetDeviceModel();

        [FreeFunction("systeminfo::SupportsAccelerometer")]
        static extern bool SupportsAccelerometer();

        [FreeFunction]
        static extern bool IsGyroAvailable();

        [FreeFunction("systeminfo::SupportsLocationService")]
        static extern bool SupportsLocationService();

        [FreeFunction("systeminfo::SupportsVibration")]
        static extern bool SupportsVibration();

        [FreeFunction("systeminfo::SupportsAudio")]
        static extern bool SupportsAudio();

        [FreeFunction("systeminfo::GetDeviceType")]
        static extern DeviceType GetDeviceType();

        [FreeFunction("ScriptingGraphicsCaps::GetGraphicsMemorySize")]
        static extern int GetGraphicsMemorySize();

        [FreeFunction("ScriptingGraphicsCaps::GetGraphicsDeviceName")]
        static extern string GetGraphicsDeviceName();

        [FreeFunction("ScriptingGraphicsCaps::GetGraphicsDeviceVendor")]
        static extern string GetGraphicsDeviceVendor();

        [FreeFunction("ScriptingGraphicsCaps::GetGraphicsDeviceID")]
        static extern int GetGraphicsDeviceID();

        [FreeFunction("ScriptingGraphicsCaps::GetGraphicsDeviceVendorID")]
        static extern int GetGraphicsDeviceVendorID();

        [FreeFunction("ScriptingGraphicsCaps::GetGraphicsDeviceType")]
        static extern Rendering.GraphicsDeviceType GetGraphicsDeviceType();

        [FreeFunction("ScriptingGraphicsCaps::GetGraphicsUVStartsAtTop")]
        static extern bool GetGraphicsUVStartsAtTop();

        [FreeFunction("ScriptingGraphicsCaps::GetGraphicsDeviceVersion")]
        static extern string GetGraphicsDeviceVersion();

        [FreeFunction("ScriptingGraphicsCaps::GetGraphicsShaderLevel")]
        static extern int GetGraphicsShaderLevel();

        [FreeFunction("ScriptingGraphicsCaps::GetGraphicsMultiThreaded")]
        static extern bool GetGraphicsMultiThreaded();

        [FreeFunction("ScriptingGraphicsCaps::GetRenderingThreadingMode")]
        static extern Rendering.RenderingThreadingMode GetRenderingThreadingMode();

        [FreeFunction("ScriptingGraphicsCaps::HasHiddenSurfaceRemovalOnGPU")]
        static extern bool HasHiddenSurfaceRemovalOnGPU();

        [FreeFunction("ScriptingGraphicsCaps::HasDynamicUniformArrayIndexingInFragmentShaders")]
        static extern bool HasDynamicUniformArrayIndexingInFragmentShaders();

        [FreeFunction("ScriptingGraphicsCaps::SupportsShadows")]
        static extern bool SupportsShadows();

        [FreeFunction("ScriptingGraphicsCaps::SupportsRawShadowDepthSampling")]
        static extern bool SupportsRawShadowDepthSampling();

        [FreeFunction("SupportsMotionVectors")]
        static extern bool SupportsMotionVectors();

        [FreeFunction("ScriptingGraphicsCaps::Supports3DTextures")]
        static extern bool Supports3DTextures();

        [FreeFunction("ScriptingGraphicsCaps::SupportsCompressed3DTextures")]
        static extern bool SupportsCompressed3DTextures();

        [FreeFunction("ScriptingGraphicsCaps::Supports2DArrayTextures")]
        static extern bool Supports2DArrayTextures();

        [FreeFunction("ScriptingGraphicsCaps::Supports3DRenderTextures")]
        static extern bool Supports3DRenderTextures();

        [FreeFunction("ScriptingGraphicsCaps::SupportsCubemapArrayTextures")]
        static extern bool SupportsCubemapArrayTextures();

        [FreeFunction("ScriptingGraphicsCaps::GetCopyTextureSupport")]
        static extern Rendering.CopyTextureSupport GetCopyTextureSupport();

        [FreeFunction("ScriptingGraphicsCaps::SupportsComputeShaders")]
        static extern bool SupportsComputeShaders();

        [FreeFunction("ScriptingGraphicsCaps::SupportsGeometryShaders")]
        static extern bool SupportsGeometryShaders();

        [FreeFunction("ScriptingGraphicsCaps::SupportsTessellationShaders")]
        static extern bool SupportsTessellationShaders();

        [FreeFunction("ScriptingGraphicsCaps::SupportsInstancing")]
        static extern bool SupportsInstancing();

        [FreeFunction("ScriptingGraphicsCaps::SupportsHardwareQuadTopology")]
        static extern bool SupportsHardwareQuadTopology();

        [FreeFunction("ScriptingGraphicsCaps::Supports32bitsIndexBuffer")]
        static extern bool Supports32bitsIndexBuffer();

        [FreeFunction("ScriptingGraphicsCaps::SupportsSparseTextures")]
        static extern bool SupportsSparseTextures();

        [FreeFunction("ScriptingGraphicsCaps::SupportedRenderTargetCount")]
        static extern int SupportedRenderTargetCount();

        [FreeFunction("ScriptingGraphicsCaps::SupportsSeparatedRenderTargetsBlend")]
        static extern bool SupportsSeparatedRenderTargetsBlend();

        [FreeFunction("ScriptingGraphicsCaps::SupportedRandomWriteTargetCount")]
        static extern int SupportedRandomWriteTargetCount();

        [FreeFunction("ScriptingGraphicsCaps::MaxComputeBufferInputsVertex")]
        static extern int MaxComputeBufferInputsVertex();

        [FreeFunction("ScriptingGraphicsCaps::MaxComputeBufferInputsFragment")]
        static extern int MaxComputeBufferInputsFragment();

        [FreeFunction("ScriptingGraphicsCaps::MaxComputeBufferInputsGeometry")]
        static extern int MaxComputeBufferInputsGeometry();

        [FreeFunction("ScriptingGraphicsCaps::MaxComputeBufferInputsDomain")]
        static extern int MaxComputeBufferInputsDomain();

        [FreeFunction("ScriptingGraphicsCaps::MaxComputeBufferInputsHull")]
        static extern int MaxComputeBufferInputsHull();

        [FreeFunction("ScriptingGraphicsCaps::MaxComputeBufferInputsCompute")]
        static extern int MaxComputeBufferInputsCompute();

        [FreeFunction("ScriptingGraphicsCaps::SupportsMultisampledTextures")]
        static extern int SupportsMultisampledTextures();

        [FreeFunction("ScriptingGraphicsCaps::SupportsMultisampleAutoResolve")]
        static extern bool SupportsMultisampleAutoResolve();

        [FreeFunction("ScriptingGraphicsCaps::SupportsTextureWrapMirrorOnce")]
        static extern int SupportsTextureWrapMirrorOnce();

        [FreeFunction("ScriptingGraphicsCaps::UsesReversedZBuffer")]
        static extern bool UsesReversedZBuffer();

        [FreeFunction("ScriptingGraphicsCaps::HasRenderTexture")]
        static extern bool HasRenderTextureNative(RenderTextureFormat format);

        [FreeFunction("ScriptingGraphicsCaps::SupportsBlendingOnRenderTextureFormat")]
        static extern bool SupportsBlendingOnRenderTextureFormatNative(RenderTextureFormat format);

        [FreeFunction("ScriptingGraphicsCaps::SupportsTextureFormat")]
        static extern bool SupportsTextureFormatNative(TextureFormat format);

        [FreeFunction("ScriptingGraphicsCaps::SupportsVertexAttributeFormat")]
        static extern bool SupportsVertexAttributeFormatNative(VertexAttributeFormat format, int dimension);

        [FreeFunction("ScriptingGraphicsCaps::GetNPOTSupport")]
        static extern NPOTSupport GetNPOTSupport();

        [FreeFunction("ScriptingGraphicsCaps::GetMaxTextureSize")]
        static extern int GetMaxTextureSize();

        [FreeFunction("ScriptingGraphicsCaps::GetMaxCubemapSize")]
        static extern int GetMaxCubemapSize();

        [FreeFunction("ScriptingGraphicsCaps::GetMaxRenderTextureSize")]
        static extern int GetMaxRenderTextureSize();

        [FreeFunction("ScriptingGraphicsCaps::GetMaxComputeWorkGroupSize")]
        static extern int GetMaxComputeWorkGroupSize();

        [FreeFunction("ScriptingGraphicsCaps::GetMaxComputeWorkGroupSizeX")]
        static extern int GetMaxComputeWorkGroupSizeX();

        [FreeFunction("ScriptingGraphicsCaps::GetMaxComputeWorkGroupSizeY")]
        static extern int GetMaxComputeWorkGroupSizeY();

        [FreeFunction("ScriptingGraphicsCaps::GetMaxComputeWorkGroupSizeZ")]
        static extern int GetMaxComputeWorkGroupSizeZ();

        [FreeFunction("ScriptingGraphicsCaps::SupportsAsyncCompute")]
        static extern bool SupportsAsyncCompute();
        [FreeFunction("ScriptingGraphicsCaps::SupportsGpuRecorder")]
        static extern bool SupportsGpuRecorder();

        [FreeFunction("ScriptingGraphicsCaps::SupportsGPUFence")]
        static extern bool SupportsGPUFence();

        [FreeFunction("ScriptingGraphicsCaps::SupportsAsyncGPUReadback")]
        static extern bool SupportsAsyncGPUReadback();

        [FreeFunction("ScriptingGraphicsCaps::SupportsRayTracing")]
        static extern bool SupportsRayTracing();

        [FreeFunction("ScriptingGraphicsCaps::SupportsSetConstantBuffer")]
        static extern bool SupportsSetConstantBuffer();

        [FreeFunction("ScriptingGraphicsCaps::MinConstantBufferOffsetAlignment")]
        static extern bool MinConstantBufferOffsetAlignment();

        [FreeFunction("ScriptingGraphicsCaps::HasMipMaxLevel")]
        static extern bool HasMipMaxLevel();

        [FreeFunction("ScriptingGraphicsCaps::SupportsMipStreaming")]
        static extern bool SupportsMipStreaming();

        [FreeFunction("ScriptingGraphicsCaps::IsFormatSupported")]
        extern public static bool IsFormatSupported(GraphicsFormat format, FormatUsage usage);

        [FreeFunction("ScriptingGraphicsCaps::GetCompatibleFormat")]
        extern public static GraphicsFormat GetCompatibleFormat(GraphicsFormat format, FormatUsage usage);

        [FreeFunction("ScriptingGraphicsCaps::GetGraphicsFormat")]
        extern public static GraphicsFormat GetGraphicsFormat(DefaultFormat format);

        [FreeFunction("ScriptingGraphicsCaps::UsesLoadStoreActions")]
        static extern bool UsesLoadStoreActions();

        [FreeFunction("ScriptingGraphicsCaps::GetHDRDisplaySupportFlags")]
        static extern HDRDisplaySupportFlags GetHDRDisplaySupportFlags();

        [FreeFunction("ScriptingGraphicsCaps::SupportsConservativeRaster")]
        static extern bool SupportsConservativeRaster();
    }


    public sealed partial class SystemInfo
    {
        public const string unsupportedIdentifier = EditorSystemInfo.unsupportedIdentifier;

        public static float batteryLevel => ShimManager.systemInfoShim.batteryLevel;

        public static BatteryStatus batteryStatus => ShimManager.systemInfoShim.batteryStatus;

        public static string operatingSystem => ShimManager.systemInfoShim.operatingSystem;

        public static OperatingSystemFamily operatingSystemFamily => ShimManager.systemInfoShim.operatingSystemFamily;

        public static string processorType => ShimManager.systemInfoShim.processorType;

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

        public static Rendering.GraphicsDeviceType graphicsDeviceType => ShimManager.systemInfoShim.graphicsDeviceType;

        public static bool graphicsUVStartsAtTop => ShimManager.systemInfoShim.graphicsUVStartsAtTop;

        public static string graphicsDeviceVersion => ShimManager.systemInfoShim.graphicsDeviceVersion;

        public static int graphicsShaderLevel => ShimManager.systemInfoShim.graphicsShaderLevel;

        public static bool graphicsMultiThreaded => ShimManager.systemInfoShim.graphicsMultiThreaded;

        public static Rendering.RenderingThreadingMode renderingThreadingMode => ShimManager.systemInfoShim.renderingThreadingMode;

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

        public static Rendering.CopyTextureSupport copyTextureSupport => ShimManager.systemInfoShim.copyTextureSupport;

        public static bool supportsComputeShaders => ShimManager.systemInfoShim.supportsComputeShaders;

        public static bool supportsConservativeRaster => ShimManager.systemInfoShim.supportsConservativeRaster;

        public static bool supportsGeometryShaders => ShimManager.systemInfoShim.supportsGeometryShaders;

        public static bool supportsTessellationShaders => ShimManager.systemInfoShim.supportsTessellationShaders;

        public static bool supportsInstancing => ShimManager.systemInfoShim.supportsInstancing;

        public static bool supportsHardwareQuadTopology => ShimManager.systemInfoShim.supportsHardwareQuadTopology;

        public static bool supports32bitsIndexBuffer => ShimManager.systemInfoShim.supports32bitsIndexBuffer;

        public static bool supportsSparseTextures => ShimManager.systemInfoShim.supportsSparseTextures;

        public static int supportedRenderTargetCount => ShimManager.systemInfoShim.supportedRenderTargetCount;

        public static bool supportsSeparatedRenderTargetsBlend => ShimManager.systemInfoShim.supportsSeparatedRenderTargetsBlend;

        public static int supportedRandomWriteTargetCount => ShimManager.systemInfoShim.supportedRandomWriteTargetCount;

        public static int supportsMultisampledTextures => ShimManager.systemInfoShim.supportsMultisampledTextures;

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

        public static int maxCubemapSize => ShimManager.systemInfoShim.maxCubemapSize;

        internal static int maxRenderTextureSize => EditorSystemInfo.maxRenderTextureSize;

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

        public static bool supportsAsyncCompute => ShimManager.systemInfoShim.supportsAsyncCompute;

        public static bool supportsGpuRecorder => ShimManager.systemInfoShim.supportsGpuRecorder;
        public static bool supportsGraphicsFence => ShimManager.systemInfoShim.supportsGraphicsFence;

        public static bool supportsAsyncGPUReadback => ShimManager.systemInfoShim.supportsAsyncGPUReadback;
        public static bool supportsRayTracing => ShimManager.systemInfoShim.supportsRayTracing;

        public static bool supportsSetConstantBuffer => ShimManager.systemInfoShim.supportsSetConstantBuffer;

        public static bool minConstantBufferOffsetAlignment => ShimManager.systemInfoShim.minConstantBufferOffsetAlignment;

        public static bool hasMipMaxLevel => ShimManager.systemInfoShim.hasMipMaxLevel;

        public static bool supportsMipStreaming => ShimManager.systemInfoShim.supportsMipStreaming;

        public static bool usesLoadStoreActions => ShimManager.systemInfoShim.usesLoadStoreActions;

        public static HDRDisplaySupportFlags hdrDisplaySupportFlags => ShimManager.systemInfoShim.hdrDisplaySupportFlags;

        public static bool IsFormatSupported(GraphicsFormat format, FormatUsage usage)
        {
            return ShimManager.systemInfoShim.IsFormatSupported(format, usage);
        }

        public static GraphicsFormat GetCompatibleFormat(GraphicsFormat format, FormatUsage usage)
        {
            return ShimManager.systemInfoShim.GetCompatibleFormat(format, usage);
        }

        public static GraphicsFormat GetGraphicsFormat(DefaultFormat format)
        {
            return ShimManager.systemInfoShim.GetGraphicsFormat(format);
        }

        [Obsolete("supportsRenderTextures always returns true, no need to call it")]
        public static bool supportsRenderTextures => EditorSystemInfo.supportsRenderTextures;

        [Obsolete("supportsRenderToCubemap always returns true, no need to call it")]
        public static bool supportsRenderToCubemap => EditorSystemInfo.supportsRenderToCubemap;

        [Obsolete("supportsImageEffects always returns true, no need to call it")]
        public static bool supportsImageEffects => EditorSystemInfo.supportsImageEffects;

        [Obsolete("supportsStencil always returns true, no need to call it")]
        public static int supportsStencil => EditorSystemInfo.supportsStencil;

        [Obsolete("graphicsPixelFillrate is no longer supported in Unity 5.0+.")]
        public static int graphicsPixelFillrate => EditorSystemInfo.graphicsPixelFillrate;

        [Obsolete("Vertex program support is required in Unity 5.0+")]
        public static bool supportsVertexPrograms => EditorSystemInfo.supportsVertexPrograms;
    }

}
