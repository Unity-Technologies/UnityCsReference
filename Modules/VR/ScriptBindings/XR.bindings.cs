// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace UnityEngine.Experimental.XR
{
    // Temporary empty namespace to allow for a weird package dependency with com.unity.xr.integration-tests
    // Remove this once that package is fixed.
    internal enum DeleteMe
    {
        Please = 0
    }
}

namespace UnityEngine.XR.WSA.Input
{
    // Temporary empty namespace to allow for a weird package dependency with com.unity.2d.animation.
    // Remove this once that package is fixed.
    internal enum DeleteMe
    {
        Please = 0
    }
}

namespace UnityEngine.XR.WSA
{
    // Temporary empty namespace to allow for  package dependency with com.unity.xr.windowsmr.
    // Remove this once that package is fixed.
    public enum RemoteDeviceVersion
    {
        V1,
        V2
    }
}

namespace UnityEngineInternal.XR.WSA
{
    // Temporary empty namespace to allow for  package dependency with com.unity.xr.windowsmr.
    // Remove this once that package is fixed.
    public class RemoteSpeechAccess
    {
        [System.Obsolete(@"Support for built-in VR will be removed in Unity 2020.2. Please update to the new Unity XR Plugin System. More information about the new XR Plugin System can be found at https://docs.unity3d.com/2019.3/Documentation/Manual/XR.html.", false)]
        public static void EnableRemoteSpeech(UnityEngine.XR.WSA.RemoteDeviceVersion version) {}
        [System.Obsolete(@"Support for built-in VR will be removed in Unity 2020.2. Please update to the new Unity XR Plugin System. More information about the new XR Plugin System can be found at https://docs.unity3d.com/2019.3/Documentation/Manual/XR.html.", false)]
        public static void DisableRemoteSpeech() {}
    }
}

namespace UnityEngine.XR
{
    // Offsets must match UnityVRBlitMode in IUnityVR.h
    public enum GameViewRenderMode
    {
        None = 0,
        LeftEye = 1,
        RightEye = 2,
        BothEyes = 3,
        OcclusionMesh = 4,
    }

    [NativeHeader("Modules/VR/ScriptBindings/XR.bindings.h")]
    [NativeHeader("Runtime/Interfaces/IVRDevice.h")]
    [NativeHeader("Modules/VR/VRModule.h")]
    [NativeHeader("Runtime/GfxDevice/GfxDeviceTypes.h")]
    [NativeConditional("ENABLE_VR")]
    public static class XRSettings
    {
        extern public static bool enabled
        {
            [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
            get;

            set;
        }

        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static GameViewRenderMode gameViewRenderMode { get; set; }

        [NativeName("Active")]
        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static bool isDeviceActive { get; }

        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static bool showDeviceView { get; set; }

        [NativeName("RenderScale")]
        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static float eyeTextureResolutionScale { get; set; }

        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static int eyeTextureWidth { get; }

        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static int eyeTextureHeight { get; }

        [NativeName("IntermediateEyeTextureDesc")]
        [NativeConditional("ENABLE_VR", "RenderTextureDesc()")]
        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static UnityEngine.RenderTextureDescriptor eyeTextureDesc { get; }

        [NativeName("DeviceEyeTextureDimension")]
        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static TextureDimension deviceEyeTextureDimension { get; }

        public static float renderViewportScale
        {
            get
            {
                return renderViewportScaleInternal;
            }
            set
            {
                if (value < 0.0f || value > 1.0f)
                    throw new ArgumentOutOfRangeException("value", "Render viewport scale should be between 0 and 1.");
                renderViewportScaleInternal = value;
            }
        }

        [NativeName("RenderViewportScale")]
        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern internal static float renderViewportScaleInternal { get; set; }

        [StaticAccessor("GetIVRDevice()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static float occlusionMaskScale { get; set; }

        [StaticAccessor("GetIVRDevice()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static bool useOcclusionMesh { get; set; }

        [NativeName("DeviceName")]
        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static string loadedDeviceName { get; }

        public static void LoadDeviceByName(string deviceName)
        {
            LoadDeviceByName(new string[] { deviceName });
        }

        extern public static void LoadDeviceByName(string[] prioritizedDeviceNameList);

        extern public static string[] supportedDevices { get; }

        public enum StereoRenderingMode
        {
            MultiPass = 0,
            SinglePass,
            SinglePassInstanced,
            SinglePassMultiview
        }

        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static StereoRenderingMode stereoRenderingMode { get; }
    }

    [Obsolete("This is obsolete, and should no longer be used.  Please use InputTrackingModeFlags.")]
    public enum TrackingSpaceType
    {
        Stationary,
        RoomScale
    }

    [NativeConditional("ENABLE_VR")]
    public static class XRDevice
    {
        [Obsolete("This is obsolete, and should no longer be used. Instead, find the active XRDisplaySubsystem and check that the running property is true (for details, see XRDevice.isPresent documentation).", true)]
        public static bool isPresent { get {throw new NotSupportedException("XRDevice is Obsolete. Instead, find the active XRDisplaySubsystem and check to see if it is running.");} }


        [NativeName("DeviceRefreshRate")]
        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static float refreshRate { get; }

        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static IntPtr GetNativePtr();

        [StaticAccessor("GetIVRDevice()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        [Obsolete("This is obsolete, and should no longer be used.  Please use XRInputSubsystem.GetTrackingOriginMode.")]
        extern public static TrackingSpaceType GetTrackingSpaceType();

        [StaticAccessor("GetIVRDevice()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        [Obsolete("This is obsolete, and should no longer be used.  Please use XRInputSubsystem.TrySetTrackingOriginMode.")]
        extern public static bool SetTrackingSpaceType(TrackingSpaceType trackingSpaceType);

        [NativeName("DisableAutoVRCameraTracking")]
        [StaticAccessor("GetIVRDevice()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static void DisableAutoXRCameraTracking([NotNull] Camera camera, bool disabled);

        [NativeName("UpdateEyeTextureMSAASetting")]
        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static void UpdateEyeTextureMSAASetting();

        extern public static float fovZoomFactor
        {
            get;

            [NativeName("SetProjectionZoomFactor")]
            [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
            set;
        }

        public static event Action<string> deviceLoaded = null;

        [RequiredByNativeCode]
        private static void InvokeDeviceLoaded(string loadedDeviceName)
        {
            if (deviceLoaded != null)
            {
                deviceLoaded(loadedDeviceName);
            }
        }
    }

    [NativeConditional("ENABLE_VR")]
    public static class XRStats
    {
        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static bool TryGetGPUTimeLastFrame(out float gpuTimeLastFrame);

        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static bool TryGetDroppedFrameCount(out int droppedFrameCount);

        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static bool TryGetFramePresentCount(out int framePresentCount);
    }
}
