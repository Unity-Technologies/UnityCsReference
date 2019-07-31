// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.XR;


namespace UnityEngine.Internal.VR
{
    [NativeHeader("Modules/VR/Test/VRTestMock.bindings.h")]
    [StaticAccessor("VRTestMockBindings", StaticAccessorType.DoubleColon)]
    public static class VRTestMock
    {
        public static extern void Reset();
        public static extern void AddTrackedDevice(XRNode nodeType);
        public static extern void UpdateTrackedDevice(XRNode nodeType, Vector3 position, Quaternion rotation);
        public static extern void UpdateLeftEye(Vector3 position, Quaternion rotation);
        public static extern void UpdateRightEye(Vector3 position, Quaternion rotation);
        public static extern void UpdateCenterEye(Vector3 position, Quaternion rotation);
        public static extern void UpdateHead(Vector3 position, Quaternion rotation);
        public static extern void UpdateLeftHand(Vector3 position, Quaternion rotation);
        public static extern void UpdateRightHand(Vector3 position, Quaternion rotation);
        public static extern void AddController(string controllerName);
        public static extern void UpdateControllerAxis(string controllerName, int axis, float value);
        public static extern void UpdateControllerButton(string controllerName, int button, bool pressed);
    }
}

