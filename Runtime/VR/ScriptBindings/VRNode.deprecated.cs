// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.VR
{
    // Matches UnityVRTrackedNodeType in IUnityVR.h
    [System.Obsolete("VRNode has been moved and renamed.  Use UnityEngine.XR.XRNode instead (UnityUpgradable) -> UnityEngine.XR.XRNode", true)]
    public enum VRNode
    {
        LeftEye,
        RightEye,
        CenterEye,
        Head,
        LeftHand,
        RightHand,
        GameController,
        TrackingReference,
        HardwareTracker
    }
}
