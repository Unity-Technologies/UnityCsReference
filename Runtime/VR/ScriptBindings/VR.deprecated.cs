// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm = System.ComponentModel;
using uei = UnityEngine.Internal;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute = UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using System.Collections.Generic;

namespace UnityEngine.VR
{
    [System.Obsolete("VRDeviceType is deprecated. Use XRSettings.supportedDevices instead.", true)]
    public enum VRDeviceType
    {
        [Obsolete("Enum member VRDeviceType.Morpheus has been deprecated. Use VRDeviceType.PlayStationVR instead (UnityUpgradable) -> PlayStationVR", true)]
        Morpheus = -1,
        None,
        Stereo,
        Split,
        Oculus,
        PlayStationVR,
        Unknown
    }

    [System.Obsolete("TrackingSpaceType has been moved.  Use UnityEngine.XR.TrackingSpaceType instead (UnityUpgradable) -> UnityEngine.XR.TrackingSpaceType", true)]
    public enum TrackingSpaceType
    {
        Stationary,
        RoomScale
    }

    [System.Obsolete("UserPresenceState has been moved.  Use UnityEngine.XR.UserPresenceState instead (UnityUpgradable) -> UnityEngine.XR.UserPresenceState", true)]
    public enum UserPresenceState
    {
        Unsupported = -1,
        NotPresent = 0,
        Present = 1,
        Unknown = 2,
    }

    [System.Obsolete("VRSettings has been moved and renamed.  Use UnityEngine.XR.XRSettings instead (UnityUpgradable)", true)]
    public static partial class VRSettings
    {
        public static bool enabled
        {
            get
            {
                throw new NotSupportedException("VRSettings has been moved and renamed.  Use UnityEngine.XR.XRSettings instead.");
            }
            set
            {
                throw new NotSupportedException("VRSettings has been moved and renamed.  Use UnityEngine.XR.XRSettings instead.");
            }
        }

        public static bool isDeviceActive
        {
            get
            {
                throw new NotSupportedException("VRSettings has been moved and renamed.  Use UnityEngine.XR.XRSettings instead.");
            }
        }

        public static bool showDeviceView
        {
            get
            {
                throw new NotSupportedException("VRSettings has been moved and renamed.  Use UnityEngine.XR.XRSettings instead.");
            }
            set
            {
                throw new NotSupportedException("VRSettings has been moved and renamed.  Use UnityEngine.XR.XRSettings instead.");
            }
        }

        public static float renderScale
        {
            get
            {
                throw new NotSupportedException("VRSettings.renderScale has been moved and renamed.  Use UnityEngine.XR.XRSettings.eyeTextureResolutionScale instead.");
            }
            set
            {
                throw new NotSupportedException("VRSettings.renderScale has been moved and renamed.  Use UnityEngine.XR.XRSettings.eyeTextureResolutionScale instead.");
            }
        }

        public static int eyeTextureWidth
        {
            get
            {
                throw new NotSupportedException("VRSettings has been moved and renamed.  Use UnityEngine.XR.XRSettings instead.");
            }
        }

        public static int eyeTextureHeight
        {
            get
            {
                throw new NotSupportedException("VRSettings has been moved and renamed.  Use UnityEngine.XR.XRSettings instead.");
            }
        }

        public static float renderViewportScale
        {
            get
            {
                throw new NotSupportedException("VRSettings has been moved and renamed.  Use UnityEngine.XR.XRSettings instead.");
            }
            set
            {
                throw new NotSupportedException("VRSettings has been moved and renamed.  Use UnityEngine.XR.XRSettings instead.");
            }
        }

        public static float occlusionMaskScale
        {
            get
            {
                throw new NotSupportedException("VRSettings has been moved and renamed.  Use UnityEngine.XR.XRSettings instead.");
            }
            set
            {
                throw new NotSupportedException("VRSettings has been moved and renamed.  Use UnityEngine.XR.XRSettings instead.");
            }
        }

        [System.Obsolete("loadedDevice is deprecated.  Use loadedDeviceName and LoadDeviceByName instead.", true)]
        public static VRDeviceType loadedDevice
        {
            get
            {
                throw new NotSupportedException("VRSettings has been moved and renamed.  Use UnityEngine.XR.XRSettings instead.");
            }
            set
            {
                throw new NotSupportedException("VRSettings has been moved and renamed.  Use UnityEngine.XR.XRSettings instead.");
            }
        }


        public static string loadedDeviceName
        {
            get
            {
                throw new NotSupportedException("VRSettings has been moved and renamed.  Use UnityEngine.XR.XRSettings instead.");
            }
        }

        public static void LoadDeviceByName(string deviceName)
        {
            throw new NotSupportedException("VRSettings has been moved and renamed.  Use UnityEngine.XR.XRSettings instead.");
        }

        public static void LoadDeviceByName(string[] prioritizedDeviceNameList)
        {
            throw new NotSupportedException("VRSettings has been moved and renamed.  Use UnityEngine.XR.XRSettings instead.");
        }

        public static string[] supportedDevices
        {
            get
            {
                throw new NotSupportedException("VRSettings has been moved and renamed.  Use UnityEngine.XR.XRSettings instead.");
            }
        }
    }

    [System.Obsolete("VRDevice has been moved and renamed.  Use UnityEngine.XR.XRDevice instead (UnityUpgradable) -> UnityEngine.XR.XRDevice", true)]
    public static partial class VRDevice
    {
        public static bool isPresent
        {
            get
            {
                throw new NotSupportedException("VRDevice has been moved and renamed.  Use UnityEngine.XR.XRDevice instead.");
            }
        }

        public static UnityEngine.XR.UserPresenceState userPresence
        {
            get
            {
                throw new NotSupportedException("VRDevice has been moved and renamed.  Use UnityEngine.XR.XRDevice instead.");
            }
        }

        [System.Obsolete("family is deprecated.  Use XRSettings.loadedDeviceName instead.", true)]
        public static string family
        {
            get
            {
                throw new NotSupportedException("VRDevice has been moved and renamed.  Use UnityEngine.XR.XRDevice instead.");
            }
        }

        public static string model
        {
            get
            {
                throw new NotSupportedException("VRDevice has been moved and renamed.  Use UnityEngine.XR.XRDevice instead.");
            }
        }

        public static float refreshRate
        {
            get
            {
                throw new NotSupportedException("VRDevice has been moved and renamed.  Use UnityEngine.XR.XRDevice instead.");
            }
        }

        public static UnityEngine.XR.TrackingSpaceType GetTrackingSpaceType()
        {
            throw new NotSupportedException("VRDevice has been moved and renamed.  Use UnityEngine.XR.XRDevice instead.");
        }

        public static bool SetTrackingSpaceType(UnityEngine.XR.TrackingSpaceType trackingSpaceType)
        {
            throw new NotSupportedException("VRDevice has been moved and renamed.  Use UnityEngine.XR.XRDevice instead.");
        }

        public static IntPtr GetNativePtr()
        {
            throw new NotSupportedException("VRDevice has been moved and renamed.  Use UnityEngine.XR.XRDevice instead.");
        }

        [Obsolete("DisableAutoVRCameraTracking has been moved and renamed.  Use UnityEngine.XR.XRDevice.DisableAutoXRCameraTracking instead (UnityUpgradable) -> UnityEngine.XR.XRDevice.DisableAutoXRCameraTracking(*)", true)]
        public static void DisableAutoVRCameraTracking(Camera camera, bool disabled)
        {
            throw new NotSupportedException("VRDevice has been moved and renamed.  Use UnityEngine.XR.XRDevice instead.");
        }
    }

    [System.Obsolete("VRStats has been moved and renamed.  Use UnityEngine.XR.XRStats instead (UnityUpgradable) -> UnityEngine.XR.XRStats", true)]
    public static partial class VRStats
    {
        public static bool TryGetGPUTimeLastFrame(out float gpuTimeLastFrame)
        {
            gpuTimeLastFrame = 0.0f;
            throw new NotSupportedException("VRStats has been moved and renamed.  Use UnityEngine.XR.XRStats instead.");
        }

        public static bool TryGetDroppedFrameCount(out int droppedFrameCount)
        {
            droppedFrameCount = 0;
            throw new NotSupportedException("VRStats has been moved and renamed.  Use UnityEngine.XR.XRStats instead.");
        }

        public static bool TryGetFramePresentCount(out int framePresentCount)
        {
            framePresentCount = 0;
            throw new NotSupportedException("VRStats has been moved and renamed.  Use UnityEngine.XR.XRStats instead.");
        }

        [System.Obsolete("gpuTimeLastFrame is deprecated. Use XRStats.TryGetGPUTimeLastFrame instead.", true)]
        public static float gpuTimeLastFrame
        {
            get
            {
                throw new NotSupportedException("VRStats has been moved and renamed.  Use UnityEngine.XR.XRStats instead.");
            }
        }
    }

    [System.Obsolete("InputTracking has been moved.  Use UnityEngine.XR.InputTracking instead (UnityUpgradable) -> UnityEngine.XR.InputTracking", true)]
    public static partial class InputTracking
    {
        public static Vector3 GetLocalPosition(VRNode node)
        {
            throw new NotSupportedException("InputTracking has been moved.  Use UnityEngine.XR.InputTracking instead.");
        }

        public static void Recenter()
        {
            throw new NotSupportedException("InputTracking has been moved.  Use UnityEngine.XR.InputTracking instead.");
        }

        public static string GetNodeName(ulong uniqueID)
        {
            throw new NotSupportedException("InputTracking has been moved.  Use UnityEngine.XR.InputTracking instead.");
        }

        static public void GetNodeStates(List<VRNodeState> nodeStates)
        {
            throw new NotSupportedException("InputTracking has been moved.  Use UnityEngine.XR.InputTracking instead.");
        }

        public static bool disablePositionalTracking
        {
            get
            {
                throw new NotSupportedException("InputTracking has been moved.  Use UnityEngine.XR.InputTracking instead.");
            }
            set
            {
                throw new NotSupportedException("InputTracking has been moved.  Use UnityEngine.XR.InputTracking instead.");
            }
        }
    }
}
