// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using System.Collections.Generic;
using UnityEngine.Scripting;

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

    // Offsets must match UnityXRTrackingOriginType in XRTypes.h
    public enum TrackingOriginMode
    {
        Device,
        Floor,
        Unknown
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
            [StaticAccessor("GetIVRDevice()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
            get;

            set;
        }

        [StaticAccessor("GetIVRDevice()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static GameViewRenderMode gameViewRenderMode { get; set; }

        [NativeName("Active")]
        [StaticAccessor("GetIVRDevice()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static bool isDeviceActive { get; }

        [StaticAccessor("GetIVRDevice()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static bool showDeviceView { get; set; }

        [Obsolete("renderScale is deprecated, use XRSettings.eyeTextureResolutionScale instead (UnityUpgradable) -> eyeTextureResolutionScale", false)]
        [StaticAccessor("GetIVRDevice()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static float renderScale { get; set; }

        [NativeName("RenderScale")]
        [StaticAccessor("GetIVRDevice()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static float eyeTextureResolutionScale { get; set; }

        [StaticAccessor("GetIVRDevice()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static int eyeTextureWidth { get; }

        [StaticAccessor("GetIVRDevice()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static int eyeTextureHeight { get; }

        [NativeName("IntermediateEyeTextureDesc")]
        [NativeConditional("ENABLE_VR", "RenderTextureDesc()")]
        [StaticAccessor("GetIVRDevice()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static UnityEngine.RenderTextureDescriptor eyeTextureDesc { get; }

        [NativeName("DeviceEyeTextureDimension")]
        [StaticAccessor("GetIVRDevice()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
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
        [StaticAccessor("GetIVRDevice()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern internal static float renderViewportScaleInternal { get; set; }

        [StaticAccessor("GetIVRDevice()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static float occlusionMaskScale { get; set; }

        [StaticAccessor("GetIVRDevice()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static bool useOcclusionMesh { get; set; }

        [NativeName("DeviceName")]
        [StaticAccessor("GetIVRDevice()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
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

        [StaticAccessor("GetIVRDevice()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static StereoRenderingMode stereoRenderingMode { get; }
    }

    public enum UserPresenceState
    {
        Unsupported = -1,
        NotPresent = 0,
        Present = 1,
        Unknown = 2,
    }

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

        extern public static UserPresenceState userPresence { get; }

        [NativeName("DeviceName")]
        [Obsolete("family is deprecated.  Use XRSettings.loadedDeviceName instead.", false)]
        [StaticAccessor("GetIVRDevice()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static string family { get; }

        [NativeName("DeviceModel")]
        [StaticAccessor("GetIVRDevice()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static string model { get; }

        [NativeName("DeviceRefreshRate")]
        [StaticAccessor("GetIVRDevice()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static float refreshRate { get; }

        [StaticAccessor("GetIVRDevice()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static IntPtr GetNativePtr();

        [StaticAccessor("GetIVRDevice()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static TrackingSpaceType GetTrackingSpaceType();

        [StaticAccessor("GetIVRDevice()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static bool SetTrackingSpaceType(TrackingSpaceType trackingSpaceType);

        [NativeName("DisableAutoVRCameraTracking")]
        [StaticAccessor("GetIVRDevice()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static void DisableAutoXRCameraTracking([NotNull] Camera camera, bool disabled);

        [NativeName("UpdateEyeTextureMSAASetting")]
        [StaticAccessor("GetIVRDevice()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static void UpdateEyeTextureMSAASetting();

        extern public static float fovZoomFactor
        {
            get;

            [NativeName("SetProjectionZoomFactor")]
            [StaticAccessor("GetIVRDevice()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
            set;
        }
        extern public static TrackingOriginMode trackingOriginMode
        {
            get;
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
        [StaticAccessor("GetIVRDevice()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static bool TryGetGPUTimeLastFrame(out float gpuTimeLastFrame);

        [StaticAccessor("GetIVRDevice()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static bool TryGetDroppedFrameCount(out int droppedFrameCount);

        [StaticAccessor("GetIVRDevice()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static bool TryGetFramePresentCount(out int framePresentCount);

        [Obsolete("gpuTimeLastFrame is deprecated. Use XRStats.TryGetGPUTimeLastFrame instead.", false)]
        public static float gpuTimeLastFrame
        {
            get
            {
                float result;
                if (TryGetGPUTimeLastFrame(out result))
                    return result;
                return 0.0f;
            }
        }
    }
}

namespace UnityEngine.Experimental.XR
{
    [NativeConditional("ENABLE_VR")]
    public static class Boundary
    {
        public enum Type
        {
            PlayArea,
            TrackedArea
        }

        public static bool TryGetDimensions(out Vector3 dimensionsOut)
        {
            return TryGetDimensions(out dimensionsOut, Type.PlayArea);
        }

        public static bool TryGetDimensions(out Vector3 dimensionsOut, [UnityEngine.Internal.DefaultValue("Type.PlayArea")] Type boundaryType)
        {
            return TryGetDimensionsInternal(out dimensionsOut, boundaryType);
        }

        [NativeName("TryGetBoundaryDimensions")]
        [StaticAccessor("GetIVRDevice()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern private static bool TryGetDimensionsInternal(out Vector3 dimensionsOut, Type boundaryType);

        [NativeName("BoundaryVisible")]
        [StaticAccessor("GetIVRDevice()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static bool visible { get; set; }

        [NativeName("BoundaryConfigured")]
        [StaticAccessor("GetIVRDevice()", StaticAccessorType.ArrowWithDefaultReturnIfNull)]
        extern public static bool configured { get; }

        public static bool TryGetGeometry(List<Vector3> geometry)
        {
            return TryGetGeometry(geometry, Type.PlayArea);
        }

        public static bool TryGetGeometry(List<Vector3> geometry, [UnityEngine.Internal.DefaultValue("Type.PlayArea")] Type boundaryType)
        {
            if (geometry == null)
            {
                throw new ArgumentNullException("geometry");
            }

            geometry.Clear();
            return TryGetGeometryScriptingInternal(geometry, boundaryType);
        }

        extern private static bool TryGetGeometryScriptingInternal(List<Vector3> geometry, Type boundaryType);
    }

}
