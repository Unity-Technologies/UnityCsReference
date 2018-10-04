// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.XR
{
    [StructLayout(LayoutKind.Sequential)]
    [NativeConditional("ENABLE_VR")]
    public struct HapticCapabilities : IEquatable<HapticCapabilities>
    {
        uint m_NumChannels;
        bool m_SupportsImpulse;
        bool m_SupportsBuffer;
        uint m_BufferFrequencyHz;

        public uint numChannels             { get { return m_NumChannels; }         internal set { m_NumChannels = value; } }
        public bool supportsImpulse         { get { return m_SupportsImpulse; }     internal set { m_SupportsImpulse = value; } }
        public bool supportsBuffer          { get { return m_SupportsBuffer; }      internal set { m_SupportsBuffer = value; } }
        public uint bufferFrequencyHz       { get { return m_BufferFrequencyHz; }   internal set { m_BufferFrequencyHz = value; } }

        public override bool Equals(object obj)
        {
            if (!(obj is HapticCapabilities))
                return false;

            return Equals((HapticCapabilities)obj);
        }

        public bool Equals(HapticCapabilities other)
        {
            return numChannels == other.numChannels &&
                supportsImpulse == other.supportsImpulse &&
                supportsBuffer == other.supportsBuffer &&
                bufferFrequencyHz == other.bufferFrequencyHz;
        }

        public override int GetHashCode()
        {
            return numChannels.GetHashCode() ^
                (supportsImpulse.GetHashCode() << 1) ^
                (supportsBuffer.GetHashCode() >> 1) ^
                (bufferFrequencyHz.GetHashCode() << 2);
        }

        public static bool operator==(HapticCapabilities a, HapticCapabilities b)
        {
            return a.Equals(b);
        }

        public static bool operator!=(HapticCapabilities a, HapticCapabilities b)
        {
            return !(a == b);
        }
    }

    public struct InputUsage<T> : IEquatable<InputUsage<T>>
    {
        public string name { get; set; }

        public InputUsage(string usageName) { name = usageName; }

        public override bool Equals(object obj)
        {
            if (!(obj is InputUsage<T>))
                return false;

            return Equals((InputUsage<T>)obj);
        }

        public bool Equals(InputUsage<T> other)
        {
            return name == other.name;
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }

        public static bool operator==(InputUsage<T> a, InputUsage<T> b)
        {
            return a.Equals(b);
        }

        public static bool operator!=(InputUsage<T> a, InputUsage<T> b)
        {
            return !(a == b);
        }
    }

    public static class CommonUsages
    {
        public static InputUsage<bool> isTracked = new InputUsage<bool>("IsTracked");
        public static InputUsage<bool> primaryButton = new InputUsage<bool>("PrimaryButton");
        public static InputUsage<bool> primaryTouch = new InputUsage<bool>("PrimaryTouch");
        public static InputUsage<bool> secondaryButton = new InputUsage<bool>("SecondaryButton");
        public static InputUsage<bool> secondaryTouch = new InputUsage<bool>("SecondaryTouch");
        public static InputUsage<bool> gripButton = new InputUsage<bool>("GripButton");
        public static InputUsage<bool> triggerButton = new InputUsage<bool>("TriggerButton");
        public static InputUsage<bool> menuButton = new InputUsage<bool>("MenuButton");
        public static InputUsage<bool> primary2DAxisClick = new InputUsage<bool>("Primary2DAxisClick");
        public static InputUsage<bool> primary2DAxisTouch = new InputUsage<bool>("Primary2DAxisTouch");
        public static InputUsage<bool> thumbrest = new InputUsage<bool>("Thumbrest");
        public static InputUsage<bool> indexTouch = new InputUsage<bool>("IndexTouch");
        public static InputUsage<bool> thumbTouch = new InputUsage<bool>("ThumbTouch");

        public static InputUsage<float> batteryLevel = new InputUsage<float>("BatteryLevel");
        public static InputUsage<float> trigger = new InputUsage<float>("Trigger");
        public static InputUsage<float> grip = new InputUsage<float>("Grip");
        public static InputUsage<float> indexFinger = new InputUsage<float>("IndexFinger");
        public static InputUsage<float> middleFinger = new InputUsage<float>("MiddleFinger");
        public static InputUsage<float> ringFinger = new InputUsage<float>("RingFinger");
        public static InputUsage<float> pinkyFinger = new InputUsage<float>("PinkyFinger");
        public static InputUsage<float> combinedTrigger = new InputUsage<float>("CombinedTrigger");

        public static InputUsage<Vector2> primary2DAxis = new InputUsage<Vector2>("Primary2DAxis");
        public static InputUsage<Vector2> dPad = new InputUsage<Vector2>("DPad");
        public static InputUsage<Vector2> secondary2DAxis = new InputUsage<Vector2>("Secondary2DAxis");

        public static InputUsage<Vector3> devicePosition = new InputUsage<Vector3>("DevicePosition");
        public static InputUsage<Vector3> leftEyePosition = new InputUsage<Vector3>("LeftEyePosition");
        public static InputUsage<Vector3> rightEyePosition = new InputUsage<Vector3>("RightEyePosition");
        public static InputUsage<Vector3> centerEyePosition = new InputUsage<Vector3>("CenterEyePosition");
        public static InputUsage<Vector3> colorCameraPosition = new InputUsage<Vector3>("CameraPosition");
        public static InputUsage<Vector3> deviceVelocity = new InputUsage<Vector3>("DeviceVelocity");
        public static InputUsage<Vector3> deviceAngularVelocity = new InputUsage<Vector3>("DeviceAngularVelocity");
        public static InputUsage<Vector3> leftEyeVelocity = new InputUsage<Vector3>("LeftEyeVelocity");
        public static InputUsage<Vector3> leftEyeAngularVelocity = new InputUsage<Vector3>("LeftEyeAngularVelocity");
        public static InputUsage<Vector3> rightEyeVelocity = new InputUsage<Vector3>("RightEyeVelocity");
        public static InputUsage<Vector3> rightEyeAngularVelocity = new InputUsage<Vector3>("RightEyeAngularVelocity");
        public static InputUsage<Vector3> centerEyeVelocity = new InputUsage<Vector3>("CenterEyeVelocity");
        public static InputUsage<Vector3> centerEyeAngularVelocity = new InputUsage<Vector3>("CenterEyeAngularVelocity");
        public static InputUsage<Vector3> colorCameraVelocity = new InputUsage<Vector3>("CameraVelocity");
        public static InputUsage<Vector3> colorCameraAngularVelocity = new InputUsage<Vector3>("CameraAngularVelocity");
        public static InputUsage<Vector3> deviceAcceleration = new InputUsage<Vector3>("DeviceAcceleration");
        public static InputUsage<Vector3> deviceAngularAcceleration = new InputUsage<Vector3>("DeviceAngularAcceleration");
        public static InputUsage<Vector3> leftEyeAcceleration = new InputUsage<Vector3>("LeftEyeAcceleration");
        public static InputUsage<Vector3> leftEyeAngularAcceleration = new InputUsage<Vector3>("LeftEyeAngularAcceleration");
        public static InputUsage<Vector3> rightEyeAcceleration = new InputUsage<Vector3>("RightEyeAcceleration");
        public static InputUsage<Vector3> rightEyeAngularAcceleration = new InputUsage<Vector3>("RightEyeAngularAcceleration");
        public static InputUsage<Vector3> centerEyeAcceleration = new InputUsage<Vector3>("CenterEyeAcceleration");
        public static InputUsage<Vector3> centerEyeAngularAcceleration = new InputUsage<Vector3>("CenterEyeAngularAcceleration");
        public static InputUsage<Vector3> colorCameraAcceleration = new InputUsage<Vector3>("CameraAcceleration");
        public static InputUsage<Vector3> colorCameraAngularAcceleration = new InputUsage<Vector3>("CameraAngularAcceleration");

        public static InputUsage<Quaternion> deviceRotation = new InputUsage<Quaternion>("DeviceRotation");
        public static InputUsage<Quaternion> leftEyeRotation = new InputUsage<Quaternion>("LeftEyeRotation");
        public static InputUsage<Quaternion> rightEyeRotation = new InputUsage<Quaternion>("RightEyeRotation");
        public static InputUsage<Quaternion> centerEyeRotation = new InputUsage<Quaternion>("CenterEyeRotation");
        public static InputUsage<Quaternion> colorCameraRotation = new InputUsage<Quaternion>("CameraRotation");
    };

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeConditional("ENABLE_VR")]
    public struct InputDevice : IEquatable<InputDevice>
    {
        private UInt64 m_DeviceId;
        internal InputDevice(UInt64 deviceId) { m_DeviceId = deviceId; }

        public bool IsValid { get { return InputDevices.IsDeviceValid(m_DeviceId); } }

        // Haptics
        public bool SendHapticImpulse(uint channel, float amplitude, float duration = 1.0f)     { return InputDevices.SendHapticImpulse(m_DeviceId, channel, amplitude, duration); }
        public bool SendHapticBuffer(uint channel, byte[] buffer)                               { return InputDevices.SendHapticBuffer(m_DeviceId, channel, buffer); }
        public bool TryGetHapticCapabilities(out HapticCapabilities capabilities)               { return InputDevices.TryGetHapticCapabilities(m_DeviceId, out capabilities); }
        public void StopHaptics()                                                               { InputDevices.StopHaptics(m_DeviceId); }

        // Features
        public bool TryGetFeatureValue(InputUsage<bool> usage, out bool value)                       { return InputDevices.TryGetFeatureValue_bool(m_DeviceId, usage.name, out value); }
        public bool TryGetFeatureValue(InputUsage<uint> usage, out uint value)                       { return InputDevices.TryGetFeatureValue_UInt32(m_DeviceId, usage.name, out value); }
        public bool TryGetFeatureValue(InputUsage<float> usage, out float value)                     { return InputDevices.TryGetFeatureValue_float(m_DeviceId, usage.name, out value); }
        public bool TryGetFeatureValue(InputUsage<Vector2> usage, out Vector2 value)                 { return InputDevices.TryGetFeatureValue_Vector2f(m_DeviceId, usage.name, out value); }
        public bool TryGetFeatureValue(InputUsage<Vector3> usage, out Vector3 value)                 { return InputDevices.TryGetFeatureValue_Vector3f(m_DeviceId, usage.name, out value); }
        public bool TryGetFeatureValue(InputUsage<Quaternion> usage, out Quaternion value)           { return InputDevices.TryGetFeatureValue_Quaternionf(m_DeviceId, usage.name, out value); }

        public override bool Equals(object obj)
        {
            if (!(obj is InputDevice))
                return false;

            return Equals((InputDevice)obj);
        }

        public bool Equals(InputDevice other)
        {
            return m_DeviceId == other.m_DeviceId;
        }

        public override int GetHashCode()
        {
            return m_DeviceId.GetHashCode();
        }

        public static bool operator==(InputDevice a, InputDevice b)
        {
            return a.Equals(b);
        }

        public static bool operator!=(InputDevice a, InputDevice b)
        {
            return !(a == b);
        }
    }

    [NativeHeader("Modules/XR/Subsystems/Input/Public/XRInputDevices.h")]
    [NativeConditional("ENABLE_VR")]
    [StaticAccessor("XRInputDevices::Get()", StaticAccessorType.Dot)]
    public partial class InputDevices
    {
        [NativeConditional("ENABLE_VR", "InputDevice::Identity")]
        public static InputDevice GetDeviceAtXRNode(XRNode node)
        {
            UInt64 deviceId = InputTracking.GetDeviceIdAtXRNode(node);
            return new InputDevice(deviceId);
        }

        internal static extern bool SendHapticImpulse(UInt64 deviceId, uint channel, float amplitude, float duration);
        internal static extern bool SendHapticBuffer(UInt64 deviceId, uint channel, byte[] buffer);
        internal static extern bool TryGetHapticCapabilities(UInt64 deviceId, out HapticCapabilities capabilities);
        internal static extern void StopHaptics(UInt64 deviceId);

        internal static extern bool TryGetFeatureValue_bool(UInt64 deviceId, string usage, out bool value);
        internal static extern bool TryGetFeatureValue_UInt32(UInt64 deviceId, string usage, out uint value);
        internal static extern bool TryGetFeatureValue_float(UInt64 deviceId, string usage, out float value);
        internal static extern bool TryGetFeatureValue_Vector2f(UInt64 deviceId, string usage, out Vector2 value);
        internal static extern bool TryGetFeatureValue_Vector3f(UInt64 deviceId, string usage, out Vector3 value);
        internal static extern bool TryGetFeatureValue_Quaternionf(UInt64 deviceId, string usage, out Quaternion value);

        internal static extern bool IsDeviceValid(UInt64 deviceId);
    }

    [NativeHeader("Modules/XR/Subsystems/Input/Public/XRInputTrackingFacade.h")]
    [NativeConditional("ENABLE_VR")]
    [StaticAccessor("XRInputTrackingFacade::Get()", StaticAccessorType.Dot)]
    public partial class InputTracking
    {
        [NativeConditional("ENABLE_VR", "Vector3f::zero")]
        extern public static Vector3 GetLocalPosition(XRNode node);

        [NativeConditional("ENABLE_VR", "Quaternionf::identity()")]
        extern public static Quaternion GetLocalRotation(XRNode node);

        [NativeConditional("ENABLE_VR")]
        extern public static void Recenter();

        [NativeConditional("ENABLE_VR")]
        extern public static string GetNodeName(ulong uniqueId);

        public static void GetNodeStates(List<XRNodeState> nodeStates)
        {
            if (null == nodeStates)
            {
                throw new ArgumentNullException("nodeStates");
            }

            nodeStates.Clear();
            GetNodeStates_Internal(nodeStates);
        }

        [NativeConditional("ENABLE_VR && !ENABLE_DOTNET")]
        extern private static void GetNodeStates_Internal(List<XRNodeState> nodeStates);

        [NativeConditional("ENABLE_VR && ENABLE_DOTNET")]
        extern private static XRNodeState[] GetNodeStates_Internal_WinRT();

        [NativeConditional("ENABLE_VR")]
        extern public static bool disablePositionalTracking
        {
            [NativeName("GetPositionalTrackingDisabled")]
            get;
            [NativeName("SetPositionalTrackingDisabled")]
            set;
        }

        [NativeHeader("Modules/XR/Subsystems/Input/Public/XRInputTracking.h")]
        [StaticAccessor("XRInputTracking::Get()", StaticAccessorType.Dot)]
        internal static extern UInt64 GetDeviceIdAtXRNode(XRNode node);
    }
}
