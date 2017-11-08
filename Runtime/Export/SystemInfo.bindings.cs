// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine.Bindings;

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
    [NativeHeader("Runtime/Camera/RenderLoops/MotionVectorRenderLoop.h")]
    [NativeHeader("Runtime/Input/GetInput.h")]
    public sealed class SystemInfo
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

        public static bool supportsRenderToCubemap
        {
            get { return SupportsRenderToCubemap(); }
        }

        // Are image effects supported? (RO)
        public static bool supportsImageEffects
        {
            get { return SupportsImageEffects(); }
        }

        public static bool supports3DTextures
        {
            get { return Supports3DTextures(); }
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

        // Is GPU draw call instancing supported? (RO)
        public static bool supportsInstancing
        {
            get { return SupportsInstancing();  }
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
            get { return SupportsSparseTextures();  }
        }

        // How many simultaneous render targets (MRTs) are supported? (RO)
        public static int supportedRenderTargetCount
        {
            get { return SupportedRenderTargetCount(); }
        }

        public static int supportsMultisampledTextures
        {
            get { return SupportsMultisampledTextures(); }
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

        // Is render texture format supported?
        public static bool SupportsRenderTextureFormat(RenderTextureFormat format)
        {
            if (!Enum.IsDefined(typeof(RenderTextureFormat), format))
                throw new ArgumentException("Failed SupportsRenderTextureFormat; format is not a valid RenderTextureFormat");

            return HasRenderTextureNative(format);
        }

        // Is render texture format supports blending?
        public static bool SupportsBlendingOnRenderTextureFormat(RenderTextureFormat format)
        {
            if (!Enum.IsDefined(typeof(RenderTextureFormat), format))
                throw new ArgumentException("Failed SupportsBlendingOnRenderTextureFormat; format is not a valid RenderTextureFormat");

            return SupportsBlendingOnRenderTextureFormatNative(format);
        }

        public static bool SupportsTextureFormat(TextureFormat format)
        {
            if (!Enum.IsDefined(typeof(TextureFormat), format))
                throw new ArgumentException("Failed SupportsTextureFormat; format is not a valid TextureFormat");

            return SupportsTextureFormatNative(format);
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

        public static bool supportsAsyncCompute
        {
            get { return SupportsAsyncCompute(); }
        }

        public static bool supportsGPUFence
        {
            get { return SupportsGPUFence(); }
        }

        public static bool supportsAsyncGPUReadback
        {
            get { return SupportsAsyncGPUReadback();  }
        }

        [Obsolete("graphicsPixelFillrate is no longer supported in Unity 5.0+.")]
        public static int graphicsPixelFillrate
        {
            get
            {
                return -1; // was already indicating "unknown GPU/platform" back when we had some support for it
            }
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

        [FreeFunction("ScriptingGraphicsCaps::SupportsShadows")]
        static extern bool SupportsShadows();

        [FreeFunction("ScriptingGraphicsCaps::SupportsRawShadowDepthSampling")]
        static extern bool SupportsRawShadowDepthSampling();

        [FreeFunction("SupportsMotionVectors")]
        static extern bool SupportsMotionVectors();

        [FreeFunction("ScriptingGraphicsCaps::SupportsRenderToCubemap")]
        static extern bool SupportsRenderToCubemap();

        [FreeFunction("ScriptingGraphicsCaps::SupportsImageEffects")]
        static extern bool SupportsImageEffects();

        [FreeFunction("ScriptingGraphicsCaps::Supports3DTextures")]
        static extern bool Supports3DTextures();

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

        [FreeFunction("ScriptingGraphicsCaps::SupportsMultisampledTextures")]
        static extern int SupportsMultisampledTextures();

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

        [FreeFunction("ScriptingGraphicsCaps::GetNPOTSupport")]
        static extern NPOTSupport GetNPOTSupport();

        [FreeFunction("ScriptingGraphicsCaps::GetMaxTextureSize")]
        static extern int GetMaxTextureSize();

        [FreeFunction("ScriptingGraphicsCaps::GetMaxCubemapSize")]
        static extern int GetMaxCubemapSize();

        [FreeFunction("ScriptingGraphicsCaps::GetMaxRenderTextureSize")]
        static extern int GetMaxRenderTextureSize();

        [FreeFunction("ScriptingGraphicsCaps::SupportsAsyncCompute")]
        static extern bool SupportsAsyncCompute();

        [FreeFunction("ScriptingGraphicsCaps::SupportsGPUFence")]
        static extern bool SupportsGPUFence();

        [FreeFunction("ScriptingGraphicsCaps::SupportsAsyncGPUReadback")]
        static extern bool SupportsAsyncGPUReadback();
    }
}
