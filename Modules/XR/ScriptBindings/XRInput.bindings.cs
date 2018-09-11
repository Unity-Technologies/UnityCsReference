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

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeConditional("ENABLE_VR")]
    [NativeHeader("Modules/XR/Subsystems/Input/Public/XRInputTrackingFacade.h")]
    public struct InputDevice : IEquatable<InputDevice>
    {
        private UInt64 m_DeviceId;

        internal InputDevice(UInt64 deviceId) { m_DeviceId = deviceId; }

        public bool IsValid { get { return InputTracking.IsDeviceValid(m_DeviceId); } }

        // Haptics
        public bool SendHapticImpulse(uint channel, float amplitude, float duration = 1.0f)     { return InputTracking.SendHapticImpulse(m_DeviceId, channel, amplitude, duration); }
        public bool SendHapticBuffer(uint channel, byte[] buffer)                               { return InputTracking.SendHapticBuffer(m_DeviceId, channel, buffer); }
        public bool TryGetHapticCapabilities(out HapticCapabilities capabilities)               { return InputTracking.TryGetHapticCapabilities(m_DeviceId, out capabilities); }
        public void StopHaptics()                                                               { InputTracking.StopHaptics(m_DeviceId); }

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

    [NativeHeader("Modules/XR/Subsystems/Input/Public/XRInputTrackingFacade.h")]
    [NativeConditional("ENABLE_VR")]
    [StaticAccessor("XRInputTrackingFacade::Get()", StaticAccessorType.Dot)]
    public partial class InputDevices
    {
        [NativeConditional("ENABLE_VR", "InputDevice::Identity")]
        public static InputDevice GetDeviceAtXRNode(XRNode node)
        {
            UInt64 deviceId = InputTracking.GetDeviceIdAtXRNode(node);
            return new InputDevice(deviceId);
        }
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

        internal static extern bool SendHapticImpulse(UInt64 deviceId, uint channel, float amplitude, float duration);
        internal static extern bool SendHapticBuffer(UInt64 deviceId, uint channel, byte[] buffer);
        internal static extern bool TryGetHapticCapabilities(UInt64 deviceId, out HapticCapabilities capabilities);
        internal static extern void StopHaptics(UInt64 deviceId);

        internal static extern bool IsDeviceValid(UInt64 deviceId);
        internal static extern UInt64 GetDeviceIdAtXRNode(XRNode node);
    }
}
