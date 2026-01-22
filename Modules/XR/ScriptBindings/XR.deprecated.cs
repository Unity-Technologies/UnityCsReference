// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.XR
{
    public static partial class XRSettings
    {
        [Obsolete("XRSettings.LoadDeviceByName is deprecated and should no longer be used. Instead, query subsystem descriptors via the SubsystemManager to create and start the subsystems you need.", true)]
        public static void LoadDeviceByName(string deviceName)
        {
            LoadDeviceByName(new string[] { deviceName });
        }

        [Obsolete("XRSettings.LoadDeviceByName is deprecated and should no longer be used. Instead, query subsystem descriptors via the SubsystemManager to create and start the subsystems you need..", true)]
        public static void LoadDeviceByName(string[] prioritizedDeviceNameList)
        {
            throw new NotSupportedException("XRSettings.LoadDeviceByName is deprecated and no longer supported.");
        }
    }

    [Obsolete("TrackingSpaceType is obsolete, and should no longer be used. Please use TrackingOriginModeFlags.", true)]
    public enum TrackingSpaceType
    {
        Stationary,
        RoomScale
    }

    [NativeConditional("ENABLE_VR")]
    [Obsolete("UnityEngine.VRModule is deprecated and will be removed in a future version. Please use the APIs in the UnityEngine.XRModule instead")]
    public static class XRDevice
    {
        [Obsolete("XRDevice.refreshRate is deprecated. " +
            "Use XRDisplaySubsystem.activeSubsystemOrStub.displayRefreshRate instead. For a more robust alternative, use XRDisplaySubsystem.TryGetDisplayRefreshRate and check the return value.")]
        public static float refreshRate
        {
            get => XRDisplaySubsystem.activeSubsystemOrStub.displayRefreshRate;
        }

        [NativeName("UpdateEyeTextureMSAASetting")]
        [StaticAccessor("GetIVRDeviceScripting()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        [Obsolete("XRDevice.UpdateEyeTextureMSAASetting is deprecated. Update your code to use XRDisplaySubsystem.SetMSAALevel() instead, passing the desired MSAA level.")]
        extern public static void UpdateEyeTextureMSAASetting();

        [Obsolete("XRDevice.fovZoomFactor is deprecated. " + 
            "Use XRDisplaySubsystem.activeSubsystemOrStub.fovZoomFactor instead.")]
        public static float fovZoomFactor
        {
            get => XRDisplaySubsystem.activeSubsystemOrStub.fovZoomFactor;
            set
            {
                var display = XRDisplaySubsystem.activeSubsystem;
                if (display != null)
                    display.fovZoomFactor = value;
            }
        }

        [Obsolete("XRDevice.GetNativePtr is deprecated, and should no longer be used. This API was only supported for legacy VR.", true)]
        public static IntPtr GetNativePtr()
        {
            throw new NotSupportedException("XRDevice.GetNativePtr is deprecated and no longer supported.");
        }

        [Obsolete("XRDevice.GetTrackingSpaceType is deprecated, and should no longer be used. Please use XRInputSubsystem.GetTrackingOriginMode.", true)]
        public static TrackingSpaceType GetTrackingSpaceType()
        {
            throw new NotSupportedException("XRDevice.GetTrackingSpaceType is deprecated. Please use XRInputSubsystem.GetTrackingOriginMode.");
        }

        [Obsolete("XRDevice.SetTrackingSpaceType is deprecated, and should no longer be used. Please use XRInputSubsystem.TrySetTrackingOriginMode.", true)]
        public static bool SetTrackingSpaceType(TrackingSpaceType trackingSpaceType)
        {
            throw new NotSupportedException("XRDevice.SetTrackingSpaceType is deprecated. Please use XRInputSubsystem.TrySetTrackingOriginMode.");
        }

        [Obsolete("XRDevice.DisableAutoXRCameraTracking is deprecated, and should no longer be used. This API was only supported for legacy VR.", true)]
        public static void DisableAutoXRCameraTracking(Camera camera, bool disabled)
        {
            throw new NotSupportedException("XRDevice.DisableAutoXRCameraTracking is deprecated and no longer supported.");
        }

        [Obsolete("XRDevice.deviceLoaded is deprecated, and should no longer be used. This API was only supported for legacy VR.", true)]
        public static event Action<string> deviceLoaded
        {
            add { throw new NotSupportedException("XRDevice.deviceLoaded is deprecated and no longer supported."); }
            remove { throw new NotSupportedException("XRDevice.deviceLoaded is deprecated and no longer supported."); }
        }

    }

    [NativeConditional("ENABLE_VR")]
    [Obsolete("UnityEngine.VRModule is deprecated and will be removed in a future version. Please use the APIs in the UnityEngine.XRModule instead")]
    public static class XRStats
    {
        [Obsolete(
            "XRStats.TryGetGPUTimeLastFrame is deprecated. " +
            "Use XRDisplaySubsystem.activeSubsystemOrStub.TryGetAppGPUTimeLastFrame instead.")]
        public static bool TryGetGPUTimeLastFrame(out float gpuTimeLastFrame)
        {
            return XRDisplaySubsystem.activeSubsystemOrStub.TryGetAppGPUTimeLastFrame(out gpuTimeLastFrame);
        }


        [Obsolete(
            "XRStats.TryGetDroppedFrameCount is deprecated. " +
            "Use XRDisplaySubsystem.activeSubsystemOrStub.TryGetDroppedFrameCount instead.")]
        public static bool TryGetDroppedFrameCount(out int droppedFrameCount)
        {
            return XRDisplaySubsystem.activeSubsystemOrStub.TryGetDroppedFrameCount(out droppedFrameCount);
        }

        [Obsolete(
            "XRStats.TryGetFramePresentCount is deprecated. " +
            "Use XRDisplaySubsystem.activeSubsystemOrStub.TryGetFramePresentCount instead.")]
        public static bool TryGetFramePresentCount(out int framePresentCount)
        {
            return XRDisplaySubsystem.activeSubsystemOrStub.TryGetFramePresentCount(out framePresentCount);
        }
    }

}
