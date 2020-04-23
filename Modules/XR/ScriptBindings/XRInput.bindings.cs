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
        uint m_BufferMaxSize;
        uint m_BufferOptimalSize;

        public uint numChannels { get { return m_NumChannels; } internal set { m_NumChannels = value; } }
        public bool supportsImpulse { get { return m_SupportsImpulse; } internal set { m_SupportsImpulse = value; } }
        public bool supportsBuffer { get { return m_SupportsBuffer; } internal set { m_SupportsBuffer = value; } }
        public uint bufferFrequencyHz { get { return m_BufferFrequencyHz; } internal set { m_BufferFrequencyHz = value; } }
        public uint bufferMaxSize { get { return m_BufferMaxSize; } internal set { m_BufferMaxSize = value; } }
        public uint bufferOptimalSize { get { return m_BufferOptimalSize; } internal set { m_BufferOptimalSize = value; } }

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
                bufferFrequencyHz == other.bufferFrequencyHz &&
                bufferMaxSize == other.bufferMaxSize &&
                bufferOptimalSize == other.bufferOptimalSize;
        }

        public override int GetHashCode()
        {
            return numChannels.GetHashCode() ^
                (supportsImpulse.GetHashCode() << 1) ^
                (supportsBuffer.GetHashCode() >> 1) ^
                (bufferFrequencyHz.GetHashCode() << 2) ^
                (bufferMaxSize.GetHashCode() >> 2) ^
                (bufferOptimalSize.GetHashCode() << 3);
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

    internal enum InputFeatureType : UInt32
    {
        Custom = 0,
        Binary, /// Boolean
        DiscreteStates, /// Integer
        Axis1D, /// Float
        Axis2D, /// XRVector2
        Axis3D, /// XRVector3
        Rotation, /// XRQuaternion
        Hand, /// XRHand
        Bone, /// XRBone
        Eyes, /// XREyes

        kUnityXRInputFeatureTypeInvalid = UInt32.MaxValue
    }

    internal enum ConnectionChangeType : UInt32
    {
        Connected,
        Disconnected,
        ConfigChange,
    }

    public enum InputDeviceRole : UInt32
    {
        Unknown = 0,
        Generic,
        LeftHanded,
        RightHanded,
        GameController,
        TrackingReference,
        HardwareTracker,
        LegacyController
    }

    [Flags]
    public enum InputDeviceCharacteristics : UInt32
    {
        None = 0,
        HeadMounted = 1 << 0,
        Camera = 1 << 1,
        HeldInHand = 1 << 2,
        HandTracking = 1 << 3,
        EyeTracking = 1 << 4,
        TrackedDevice = 1 << 5,
        Controller = 1 << 6,
        TrackingReference = 1 << 7,
        Left = 1 << 8,
        Right = 1 << 9,
        Simulated6DOF = 1 << 10
    }

    [Flags]
    public enum InputTrackingState : UInt32
    {
        None = 0,
        Position = 1 << 0,
        Rotation = 1 << 1,
        Velocity = 1 << 2,
        AngularVelocity = 1 << 3,
        Acceleration = 1 << 4,
        AngularAcceleration = 1 << 5,

        All = (1 << 6) - 1 // Keep this as the last entry, if you add an entry, bump this shift up by 1 as well
    }

    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeConditional("ENABLE_VR")]
    [NativeHeader("Modules/XR/Subsystems/Input/Public/XRInputDevices.h")]
    public struct InputFeatureUsage : IEquatable<InputFeatureUsage>
    {
        internal string m_Name;
        [NativeName("m_FeatureType")] internal InputFeatureType m_InternalType;
        public string name { get { return m_Name; } internal set { m_Name = value; } }
        internal InputFeatureType internalType { get { return m_InternalType; } set { m_InternalType = value; } }
        public Type type
        {
            get
            {
                switch (m_InternalType)
                {
                    case InputFeatureType.Custom: return typeof(byte[]);
                    case InputFeatureType.Binary: return typeof(bool);
                    case InputFeatureType.DiscreteStates: return typeof(uint);
                    case InputFeatureType.Axis1D: return typeof(float);
                    case InputFeatureType.Axis2D: return typeof(Vector2);
                    case InputFeatureType.Axis3D: return typeof(Vector3);
                    case InputFeatureType.Rotation: return typeof(Quaternion);
                    case InputFeatureType.Hand: return typeof(Hand);
                    case InputFeatureType.Bone: return typeof(Bone);
                    case InputFeatureType.Eyes: return typeof(Eyes);
                    default: throw new InvalidCastException("No valid managed type for unknown native type.");
                }
            }
        }

        internal InputFeatureUsage(string name, InputFeatureType type)
        {
            m_Name = name;
            m_InternalType = type;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is InputFeatureUsage))
                return false;
            return Equals((InputFeatureUsage)obj);
        }

        public bool Equals(InputFeatureUsage other)
        {
            return name == other.name && internalType == other.internalType;
        }

        public override int GetHashCode()
        {
            return name.GetHashCode() ^ (internalType.GetHashCode() << 1);
        }

        public static bool operator==(InputFeatureUsage a, InputFeatureUsage b)
        {
            return a.Equals(b);
        }

        public static bool operator!=(InputFeatureUsage a, InputFeatureUsage b)
        {
            return !(a == b);
        }

        public InputFeatureUsage<T> As<T>()
        {
            if (type != typeof(T))
                throw new ArgumentException("InputFeatureUsage type does not match out variable type.");
            return new InputFeatureUsage<T>(this.name);
        }
    }

    public struct InputFeatureUsage<T> : IEquatable<InputFeatureUsage<T>>
    {
        public string name { get; set; }
        public InputFeatureUsage(string usageName) { name = usageName; }
        public override bool Equals(object obj)
        {
            if (!(obj is InputFeatureUsage<T>))
                return false;
            return Equals((InputFeatureUsage<T>)obj);
        }

        public bool Equals(InputFeatureUsage<T> other)
        {
            return name == other.name;
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }

        public static bool operator==(InputFeatureUsage<T> a, InputFeatureUsage<T> b)
        {
            return a.Equals(b);
        }

        public static bool operator!=(InputFeatureUsage<T> a, InputFeatureUsage<T> b)
        {
            return !(a == b);
        }

        private Type usageType { get { return typeof(T); } }
        public static explicit operator InputFeatureUsage(InputFeatureUsage<T> self)
        {
            InputFeatureType featureType = InputFeatureType.kUnityXRInputFeatureTypeInvalid;
            Type usageType = self.usageType;
            if (usageType == typeof(bool))
                featureType = InputFeatureType.Binary;
            else if (usageType == typeof(uint))
                featureType = InputFeatureType.DiscreteStates;
            else if (usageType == typeof(float))
                featureType = InputFeatureType.Axis1D;
            else if (usageType == typeof(Vector2))
                featureType = InputFeatureType.Axis2D;
            else if (usageType == typeof(Vector3))
                featureType = InputFeatureType.Axis3D;
            else if (usageType == typeof(Quaternion))
                featureType = InputFeatureType.Rotation;
            else if (usageType == typeof(Hand))
                featureType = InputFeatureType.Hand;
            else if (usageType == typeof(Bone))
                featureType = InputFeatureType.Bone;
            else if (usageType == typeof(Eyes))
                featureType = InputFeatureType.Eyes;
            else if (usageType == typeof(byte[]))
                featureType = InputFeatureType.Custom;
            else if (usageType.IsEnum)
                featureType = InputFeatureType.DiscreteStates;
            if (featureType != InputFeatureType.kUnityXRInputFeatureTypeInvalid)
                return new InputFeatureUsage(self.name, featureType);
            throw new InvalidCastException($"No valid InputFeatureType for {self.name}.");
        }
    }

    public static class CommonUsages
    {
        public static InputFeatureUsage<bool> isTracked = new InputFeatureUsage<bool>("IsTracked");
        public static InputFeatureUsage<bool> primaryButton = new InputFeatureUsage<bool>("PrimaryButton");
        public static InputFeatureUsage<bool> primaryTouch = new InputFeatureUsage<bool>("PrimaryTouch");
        public static InputFeatureUsage<bool> secondaryButton = new InputFeatureUsage<bool>("SecondaryButton");
        public static InputFeatureUsage<bool> secondaryTouch = new InputFeatureUsage<bool>("SecondaryTouch");
        public static InputFeatureUsage<bool> gripButton = new InputFeatureUsage<bool>("GripButton");
        public static InputFeatureUsage<bool> triggerButton = new InputFeatureUsage<bool>("TriggerButton");
        public static InputFeatureUsage<bool> menuButton = new InputFeatureUsage<bool>("MenuButton");
        public static InputFeatureUsage<bool> primary2DAxisClick = new InputFeatureUsage<bool>("Primary2DAxisClick");
        public static InputFeatureUsage<bool> primary2DAxisTouch = new InputFeatureUsage<bool>("Primary2DAxisTouch");
        public static InputFeatureUsage<bool> secondary2DAxisClick = new InputFeatureUsage<bool>("Secondary2DAxisClick");
        public static InputFeatureUsage<bool> secondary2DAxisTouch = new InputFeatureUsage<bool>("Secondary2DAxisTouch");
        public static InputFeatureUsage<bool> userPresence = new InputFeatureUsage<bool>("UserPresence");

        public static InputFeatureUsage<InputTrackingState> trackingState = new InputFeatureUsage<InputTrackingState>("TrackingState");

        public static InputFeatureUsage<float> batteryLevel = new InputFeatureUsage<float>("BatteryLevel");
        public static InputFeatureUsage<float> trigger = new InputFeatureUsage<float>("Trigger");
        public static InputFeatureUsage<float> grip = new InputFeatureUsage<float>("Grip");

        public static InputFeatureUsage<Vector2> primary2DAxis = new InputFeatureUsage<Vector2>("Primary2DAxis");
        public static InputFeatureUsage<Vector2> secondary2DAxis = new InputFeatureUsage<Vector2>("Secondary2DAxis");

        public static InputFeatureUsage<Vector3> devicePosition = new InputFeatureUsage<Vector3>("DevicePosition");
        public static InputFeatureUsage<Vector3> leftEyePosition = new InputFeatureUsage<Vector3>("LeftEyePosition");
        public static InputFeatureUsage<Vector3> rightEyePosition = new InputFeatureUsage<Vector3>("RightEyePosition");
        public static InputFeatureUsage<Vector3> centerEyePosition = new InputFeatureUsage<Vector3>("CenterEyePosition");
        public static InputFeatureUsage<Vector3> colorCameraPosition = new InputFeatureUsage<Vector3>("CameraPosition");
        public static InputFeatureUsage<Vector3> deviceVelocity = new InputFeatureUsage<Vector3>("DeviceVelocity");
        public static InputFeatureUsage<Vector3> deviceAngularVelocity = new InputFeatureUsage<Vector3>("DeviceAngularVelocity");
        public static InputFeatureUsage<Vector3> leftEyeVelocity = new InputFeatureUsage<Vector3>("LeftEyeVelocity");
        public static InputFeatureUsage<Vector3> leftEyeAngularVelocity = new InputFeatureUsage<Vector3>("LeftEyeAngularVelocity");
        public static InputFeatureUsage<Vector3> rightEyeVelocity = new InputFeatureUsage<Vector3>("RightEyeVelocity");
        public static InputFeatureUsage<Vector3> rightEyeAngularVelocity = new InputFeatureUsage<Vector3>("RightEyeAngularVelocity");
        public static InputFeatureUsage<Vector3> centerEyeVelocity = new InputFeatureUsage<Vector3>("CenterEyeVelocity");
        public static InputFeatureUsage<Vector3> centerEyeAngularVelocity = new InputFeatureUsage<Vector3>("CenterEyeAngularVelocity");
        public static InputFeatureUsage<Vector3> colorCameraVelocity = new InputFeatureUsage<Vector3>("CameraVelocity");
        public static InputFeatureUsage<Vector3> colorCameraAngularVelocity = new InputFeatureUsage<Vector3>("CameraAngularVelocity");
        public static InputFeatureUsage<Vector3> deviceAcceleration = new InputFeatureUsage<Vector3>("DeviceAcceleration");
        public static InputFeatureUsage<Vector3> deviceAngularAcceleration = new InputFeatureUsage<Vector3>("DeviceAngularAcceleration");
        public static InputFeatureUsage<Vector3> leftEyeAcceleration = new InputFeatureUsage<Vector3>("LeftEyeAcceleration");
        public static InputFeatureUsage<Vector3> leftEyeAngularAcceleration = new InputFeatureUsage<Vector3>("LeftEyeAngularAcceleration");
        public static InputFeatureUsage<Vector3> rightEyeAcceleration = new InputFeatureUsage<Vector3>("RightEyeAcceleration");
        public static InputFeatureUsage<Vector3> rightEyeAngularAcceleration = new InputFeatureUsage<Vector3>("RightEyeAngularAcceleration");
        public static InputFeatureUsage<Vector3> centerEyeAcceleration = new InputFeatureUsage<Vector3>("CenterEyeAcceleration");
        public static InputFeatureUsage<Vector3> centerEyeAngularAcceleration = new InputFeatureUsage<Vector3>("CenterEyeAngularAcceleration");
        public static InputFeatureUsage<Vector3> colorCameraAcceleration = new InputFeatureUsage<Vector3>("CameraAcceleration");
        public static InputFeatureUsage<Vector3> colorCameraAngularAcceleration = new InputFeatureUsage<Vector3>("CameraAngularAcceleration");

        public static InputFeatureUsage<Quaternion> deviceRotation = new InputFeatureUsage<Quaternion>("DeviceRotation");
        public static InputFeatureUsage<Quaternion> leftEyeRotation = new InputFeatureUsage<Quaternion>("LeftEyeRotation");
        public static InputFeatureUsage<Quaternion> rightEyeRotation = new InputFeatureUsage<Quaternion>("RightEyeRotation");
        public static InputFeatureUsage<Quaternion> centerEyeRotation = new InputFeatureUsage<Quaternion>("CenterEyeRotation");
        public static InputFeatureUsage<Quaternion> colorCameraRotation = new InputFeatureUsage<Quaternion>("CameraRotation");

        public static InputFeatureUsage<Hand> handData = new InputFeatureUsage<Hand>("HandData");
        public static InputFeatureUsage<Eyes> eyesData = new InputFeatureUsage<Eyes>("EyesData");

        [Obsolete("CommonUsages.dPad is not used by any XR platform and will be removed.")]
        public static InputFeatureUsage<Vector2> dPad = new InputFeatureUsage<Vector2>("DPad");
        [Obsolete("CommonUsages.indexFinger is not used by any XR platform and will be removed.")]
        public static InputFeatureUsage<float> indexFinger = new InputFeatureUsage<float>("IndexFinger");
        [Obsolete("CommonUsages.MiddleFinger is not used by any XR platform and will be removed.")]
        public static InputFeatureUsage<float> middleFinger = new InputFeatureUsage<float>("MiddleFinger");
        [Obsolete("CommonUsages.RingFinger is not used by any XR platform and will be removed.")]
        public static InputFeatureUsage<float> ringFinger = new InputFeatureUsage<float>("RingFinger");
        [Obsolete("CommonUsages.PinkyFinger is not used by any XR platform and will be removed.")]
        public static InputFeatureUsage<float> pinkyFinger = new InputFeatureUsage<float>("PinkyFinger");

        // These should go to Oculus SDK
        [Obsolete("CommonUsages.thumbrest is Oculus only, and is being moved to their package. Please use OculusUsages.thumbrest. These will still function until removed.")]
        public static InputFeatureUsage<bool> thumbrest = new InputFeatureUsage<bool>("Thumbrest");
        [Obsolete("CommonUsages.indexTouch is Oculus only, and is being moved to their package.  Please use OculusUsages.indexTouch. These will still function until removed.")]
        public static InputFeatureUsage<float> indexTouch = new InputFeatureUsage<float>("IndexTouch");
        [Obsolete("CommonUsages.thumbTouch is Oculus only, and is being moved to their package.  Please use OculusUsages.thumbTouch. These will still function until removed.")]
        public static InputFeatureUsage<float> thumbTouch = new InputFeatureUsage<float>("ThumbTouch");
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeConditional("ENABLE_VR")]
    public struct InputDevice : IEquatable<InputDevice>
    {
        private static List<XRInputSubsystem> s_InputSubsystemCache;

        private UInt64 m_DeviceId;
        private bool m_Initialized;
        internal InputDevice(UInt64 deviceId)
        {
            m_DeviceId = deviceId;
            m_Initialized = true;
        }

        // Use this to compare deviceIds.  It will take care of the default uninitialized case.
        private UInt64 deviceId
        {
            get
            {
                return m_Initialized ? m_DeviceId : UInt64.MaxValue;
            }
        }

        public XRInputSubsystem subsystem
        {
            get
            {
                if (s_InputSubsystemCache == null)
                    s_InputSubsystemCache = new List<XRInputSubsystem>();

                if (m_Initialized)
                {
                    /// The DeviceId is cut in two, with the hiword being the subsystem identifier, and the loword being for the specific device.
                    UInt32 pluginIndex = (UInt32)(m_DeviceId >> 32);
                    SubsystemManager.GetSubsystems(s_InputSubsystemCache);
                    for (int i = 0; i < s_InputSubsystemCache.Count; i++)
                    {
                        if (pluginIndex == s_InputSubsystemCache[i].GetIndex())
                            return s_InputSubsystemCache[i];
                    }
                }

                return null;
            }
        }
        public bool isValid { get { return IsValidId() && InputDevices.IsDeviceValid(m_DeviceId); } }
        public string name { get { return IsValidId() ? InputDevices.GetDeviceName(m_DeviceId) : null; } }
        [Obsolete("This API has been marked as deprecated and will be removed in future versions. Please use InputDevice.characteristics instead.")]
        public InputDeviceRole role { get { return IsValidId() ? InputDevices.GetDeviceRole(m_DeviceId) : InputDeviceRole.Unknown; } }
        public string manufacturer { get { return IsValidId() ? InputDevices.GetDeviceManufacturer(m_DeviceId) : null; } }
        public string serialNumber { get { return IsValidId() ? InputDevices.GetDeviceSerialNumber(m_DeviceId) : null; } }
        public InputDeviceCharacteristics characteristics { get { return IsValidId() ? InputDevices.GetDeviceCharacteristics(m_DeviceId) : InputDeviceCharacteristics.None; } }

        private bool IsValidId() { return deviceId != UInt64.MaxValue; }

        // Haptics
        public bool SendHapticImpulse(uint channel, float amplitude, float duration = 1.0f)
        {
            if (!IsValidId())
                return false;

            if (amplitude < 0.0f)
                throw new ArgumentException("Amplitude of SendHapticImpulse cannot be negative.");
            if (duration < 0.0f)
                throw new ArgumentException("Duration of SendHapticImpulse cannot be negative.");
            return InputDevices.SendHapticImpulse(m_DeviceId, channel, amplitude, duration);
        }

        public bool SendHapticBuffer(uint channel, byte[] buffer)
        {
            if (!IsValidId())
                return false;

            return InputDevices.SendHapticBuffer(m_DeviceId, channel, buffer);
        }

        public bool TryGetHapticCapabilities(out HapticCapabilities capabilities)
        {
            if (CheckValidAndSetDefault(out capabilities))
                return InputDevices.TryGetHapticCapabilities(m_DeviceId, out capabilities);
            return false;
        }

        public void StopHaptics()
        {
            if (IsValidId())
                InputDevices.StopHaptics(m_DeviceId);
        }

        // Feature Usages
        public bool TryGetFeatureUsages(List<InputFeatureUsage> featureUsages)
        {
            if (IsValidId())
                return InputDevices.TryGetFeatureUsages(m_DeviceId, featureUsages);

            return false;
        }

        // Features by Usage
        public bool TryGetFeatureValue(InputFeatureUsage<bool> usage, out bool value)
        {
            if (CheckValidAndSetDefault(out value))
                return InputDevices.TryGetFeatureValue_bool(m_DeviceId, usage.name, out value);
            return false;
        }

        public bool TryGetFeatureValue(InputFeatureUsage<uint> usage, out uint value)
        {
            if (CheckValidAndSetDefault(out value))
                return InputDevices.TryGetFeatureValue_UInt32(m_DeviceId, usage.name, out value);
            return false;
        }

        public bool TryGetFeatureValue(InputFeatureUsage<float> usage, out float value)
        {
            if (CheckValidAndSetDefault(out value))
                return InputDevices.TryGetFeatureValue_float(m_DeviceId, usage.name, out value);
            return false;
        }

        public bool TryGetFeatureValue(InputFeatureUsage<Vector2> usage, out Vector2 value)
        {
            if (CheckValidAndSetDefault(out value))
                return InputDevices.TryGetFeatureValue_Vector2f(m_DeviceId, usage.name, out value);
            return false;
        }

        public bool TryGetFeatureValue(InputFeatureUsage<Vector3> usage, out Vector3 value)
        {
            if (CheckValidAndSetDefault(out value))
                return InputDevices.TryGetFeatureValue_Vector3f(m_DeviceId, usage.name, out value);
            return false;
        }

        public bool TryGetFeatureValue(InputFeatureUsage<Quaternion> usage, out Quaternion value)
        {
            if (CheckValidAndSetDefault(out value))
                return InputDevices.TryGetFeatureValue_Quaternionf(m_DeviceId, usage.name, out value);
            return false;
        }

        public bool TryGetFeatureValue(InputFeatureUsage<Hand> usage, out Hand value)
        {
            if (CheckValidAndSetDefault(out value))
                return InputDevices.TryGetFeatureValue_XRHand(m_DeviceId, usage.name, out value);
            return false;
        }

        public bool TryGetFeatureValue(InputFeatureUsage<Bone> usage, out Bone value)
        {
            if (CheckValidAndSetDefault(out value))
                return InputDevices.TryGetFeatureValue_XRBone(m_DeviceId, usage.name, out value);
            return false;
        }

        public bool TryGetFeatureValue(InputFeatureUsage<Eyes> usage, out Eyes value)
        {
            if (CheckValidAndSetDefault(out value))
                return InputDevices.TryGetFeatureValue_XREyes(m_DeviceId, usage.name, out value);
            return false;
        }

        public bool TryGetFeatureValue(InputFeatureUsage<byte[]> usage, byte[] value)
        {
            if (IsValidId())
                return InputDevices.TryGetFeatureValue_Custom(m_DeviceId, usage.name, value);

            return false;
        }

        public bool TryGetFeatureValue(InputFeatureUsage<InputTrackingState> usage, out InputTrackingState value)
        {
            if (IsValidId())
            {
                uint intValue = 0;
                if (InputDevices.TryGetFeatureValue_UInt32(m_DeviceId, usage.name, out intValue))
                {
                    value = (InputTrackingState)intValue;
                    return true;
                }
            }
            value = InputTrackingState.None;
            return false;
        }

        // Features at time
        public bool TryGetFeatureValue(InputFeatureUsage<bool> usage, DateTime time, out bool value)
        {
            if (CheckValidAndSetDefault(out value))
                return InputDevices.TryGetFeatureValueAtTime_bool(m_DeviceId, usage.name, TimeConverter.LocalDateTimeToUnixTimeMilliseconds(time), out value);
            return false;
        }

        public bool TryGetFeatureValue(InputFeatureUsage<uint> usage, DateTime time, out uint value)
        {
            if (CheckValidAndSetDefault(out value))
                return InputDevices.TryGetFeatureValueAtTime_UInt32(m_DeviceId, usage.name, TimeConverter.LocalDateTimeToUnixTimeMilliseconds(time), out value);
            return false;
        }

        public bool TryGetFeatureValue(InputFeatureUsage<float> usage, DateTime time, out float value)
        {
            if (CheckValidAndSetDefault(out value))
                return InputDevices.TryGetFeatureValueAtTime_float(m_DeviceId, usage.name, TimeConverter.LocalDateTimeToUnixTimeMilliseconds(time), out value);
            return false;
        }

        public bool TryGetFeatureValue(InputFeatureUsage<Vector2> usage, DateTime time, out Vector2 value)
        {
            if (CheckValidAndSetDefault(out value))
                return InputDevices.TryGetFeatureValueAtTime_Vector2f(m_DeviceId, usage.name, TimeConverter.LocalDateTimeToUnixTimeMilliseconds(time), out value);
            return false;
        }

        public bool TryGetFeatureValue(InputFeatureUsage<Vector3> usage, DateTime time, out Vector3 value)
        {
            if (CheckValidAndSetDefault(out value))
                return InputDevices.TryGetFeatureValueAtTime_Vector3f(m_DeviceId, usage.name, TimeConverter.LocalDateTimeToUnixTimeMilliseconds(time), out value);
            return false;
        }

        public bool TryGetFeatureValue(InputFeatureUsage<Quaternion> usage, DateTime time, out Quaternion value)
        {
            if (CheckValidAndSetDefault(out value))
                return InputDevices.TryGetFeatureValueAtTime_Quaternionf(m_DeviceId, usage.name, TimeConverter.LocalDateTimeToUnixTimeMilliseconds(time), out value);
            return false;
        }

        public bool TryGetFeatureValue(InputFeatureUsage<InputTrackingState> usage, DateTime time, out InputTrackingState value)
        {
            if (IsValidId())
            {
                uint intValue = 0;
                if (InputDevices.TryGetFeatureValueAtTime_UInt32(m_DeviceId, usage.name, TimeConverter.LocalDateTimeToUnixTimeMilliseconds(time), out intValue))
                {
                    value = (InputTrackingState)intValue;
                    return true;
                }
            }

            value = InputTrackingState.None;
            return false;
        }

        bool CheckValidAndSetDefault<T>(out T value)
        {
            value = default(T);
            return IsValidId();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is InputDevice))
                return false;

            return Equals((InputDevice)obj);
        }

        public bool Equals(InputDevice other)
        {
            return deviceId == other.deviceId;
        }

        public override int GetHashCode()
        {
            return deviceId.GetHashCode();
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

    internal static class TimeConverter
    {
        static readonly DateTime s_Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static DateTime now
        {
            get { return DateTime.Now; }
        }

        public static long LocalDateTimeToUnixTimeMilliseconds(DateTime date)
        {
            return Convert.ToInt64((date.ToUniversalTime() - s_Epoch).TotalMilliseconds);
        }

        public static DateTime UnixTimeMillisecondsToLocalDateTime(long unixTimeInMilliseconds)
        {
            DateTime dateTime = s_Epoch;
            return dateTime.AddMilliseconds(unixTimeInMilliseconds).ToLocalTime();
        }
    }


    public enum HandFinger
    {
        Thumb,
        Index,
        Middle,
        Ring,
        Pinky
    }

    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeConditional("ENABLE_VR")]
    [NativeHeader("Modules/XR/XRPrefix.h")]
    [NativeHeader("XRScriptingClasses.h")]
    [NativeHeader("Modules/XR/Subsystems/Input/Public/XRInputDevices.h")]
    [StaticAccessor("XRInputDevices::Get()", StaticAccessorType.Dot)]
    public struct Hand : IEquatable<Hand>
    {
        UInt64 m_DeviceId;
        UInt32 m_FeatureIndex;
        internal UInt64 deviceId { get { return m_DeviceId; } }
        internal UInt32 featureIndex { get { return m_FeatureIndex; } }

        public bool TryGetRootBone(out Bone boneOut)
        {
            return Hand_TryGetRootBone(this, out boneOut);
        }

        private static extern bool Hand_TryGetRootBone(Hand hand, out Bone boneOut);

        public bool TryGetFingerBones(HandFinger finger, List<Bone> bonesOut)
        {
            if (bonesOut == null)
                throw new ArgumentNullException("bonesOut");

            return Hand_TryGetFingerBonesAsList(this, finger, bonesOut);
        }

        private static extern bool Hand_TryGetFingerBonesAsList(Hand hand, HandFinger finger, [NotNull] List<Bone> bonesOut);

        public override bool Equals(object obj)
        {
            if (!(obj is Hand))
                return false;

            return Equals((Hand)obj);
        }

        public bool Equals(Hand other)
        {
            return deviceId == other.deviceId &&
                featureIndex == other.featureIndex;
        }

        public override int GetHashCode()
        {
            return deviceId.GetHashCode() ^ (featureIndex.GetHashCode() << 1);
        }

        public static bool operator==(Hand a, Hand b)
        {
            return a.Equals(b);
        }

        public static bool operator!=(Hand a, Hand b)
        {
            return !(a == b);
        }
    }

    internal enum EyeSide
    {
        Left,
        Right
    }

    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeConditional("ENABLE_VR")]
    [NativeHeader("Modules/XR/XRPrefix.h")]
    [NativeHeader("XRScriptingClasses.h")]
    [NativeHeader("Modules/XR/Subsystems/Input/Public/XRInputDevices.h")]
    [StaticAccessor("XRInputDevices::Get()", StaticAccessorType.Dot)]
    public struct Eyes : IEquatable<Eyes>
    {
        UInt64 m_DeviceId;
        UInt32 m_FeatureIndex;
        internal UInt64 deviceId { get { return m_DeviceId; } }
        internal UInt32 featureIndex { get { return m_FeatureIndex; } }

        public bool TryGetLeftEyePosition(out Vector3 position)
        {
            return Eyes_TryGetEyePosition(this, EyeSide.Left, out position);
        }

        public bool TryGetRightEyePosition(out Vector3 position)
        {
            return Eyes_TryGetEyePosition(this, EyeSide.Right, out position);
        }

        public bool TryGetLeftEyeRotation(out Quaternion rotation)
        {
            return Eyes_TryGetEyeRotation(this, EyeSide.Left, out rotation);
        }

        public bool TryGetRightEyeRotation(out Quaternion rotation)
        {
            return Eyes_TryGetEyeRotation(this, EyeSide.Right, out rotation);
        }

        private static extern bool Eyes_TryGetEyePosition(Eyes eyes, EyeSide chirality, out Vector3 position);
        private static extern bool Eyes_TryGetEyeRotation(Eyes eyes, EyeSide chirality, out Quaternion rotation);

        public bool TryGetFixationPoint(out Vector3 fixationPoint)
        {
            return Eyes_TryGetFixationPoint(this, out fixationPoint);
        }

        private static extern bool Eyes_TryGetFixationPoint(Eyes eyes, out Vector3 fixationPoint);

        public bool TryGetLeftEyeOpenAmount(out float openAmount)
        {
            return Eyes_TryGetEyeOpenAmount(this, EyeSide.Left, out openAmount);
        }

        public bool TryGetRightEyeOpenAmount(out float openAmount)
        {
            return Eyes_TryGetEyeOpenAmount(this, EyeSide.Right, out openAmount);
        }

        private static extern bool Eyes_TryGetEyeOpenAmount(Eyes eyes, EyeSide chirality, out float openAmount);

        public override bool Equals(object obj)
        {
            if (!(obj is Eyes))
                return false;

            return Equals((Eyes)obj);
        }

        public bool Equals(Eyes other)
        {
            return deviceId == other.deviceId &&
                featureIndex == other.featureIndex;
        }

        public override int GetHashCode()
        {
            return deviceId.GetHashCode() ^ (featureIndex.GetHashCode() << 1);
        }

        public static bool operator==(Eyes a, Eyes b)
        {
            return a.Equals(b);
        }

        public static bool operator!=(Eyes a, Eyes b)
        {
            return !(a == b);
        }
    }

    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeConditional("ENABLE_VR")]
    [NativeHeader("Modules/XR/XRPrefix.h")]
    [NativeHeader("XRScriptingClasses.h")]
    [NativeHeader("Modules/XR/Subsystems/Input/Public/XRInputDevices.h")]
    [StaticAccessor("XRInputDevices::Get()", StaticAccessorType.Dot)]
    public struct Bone : IEquatable<Bone>
    {
        UInt64 m_DeviceId;
        UInt32 m_FeatureIndex;

        internal UInt64 deviceId { get { return m_DeviceId; } }
        internal UInt32 featureIndex { get { return m_FeatureIndex; } }

        public bool TryGetPosition(out Vector3 position) { return Bone_TryGetPosition(this, out position); }
        private static extern bool Bone_TryGetPosition(Bone bone, out Vector3 position);

        public bool TryGetRotation(out Quaternion rotation) { return Bone_TryGetRotation(this, out rotation); }
        private static extern bool Bone_TryGetRotation(Bone bone, out Quaternion rotation);

        public bool TryGetParentBone(out Bone parentBone) { return Bone_TryGetParentBone(this, out parentBone); }
        private static extern bool Bone_TryGetParentBone(Bone bone, out Bone parentBone);

        public bool TryGetChildBones(List<Bone> childBones) { return Bone_TryGetChildBones(this, childBones); }
        private static extern bool Bone_TryGetChildBones(Bone bone, [NotNull] List<Bone> childBones);

        public override bool Equals(object obj)
        {
            if (!(obj is Bone))
                return false;

            return Equals((Bone)obj);
        }

        public bool Equals(Bone other)
        {
            return deviceId == other.deviceId &&
                featureIndex == other.featureIndex;
        }

        public override int GetHashCode()
        {
            return deviceId.GetHashCode() ^
                (featureIndex.GetHashCode() << 1);
        }

        public static bool operator==(Bone a, Bone b)
        {
            return a.Equals(b);
        }

        public static bool operator!=(Bone a, Bone b)
        {
            return !(a == b);
        }
    }


    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeConditional("ENABLE_VR")]
    [NativeHeader("Modules/XR/Subsystems/Input/Public/XRInputDevices.h")]
    [StaticAccessor("XRInputDevices::Get()", StaticAccessorType.Dot)]
    public partial class InputDevices
    {
        public static InputDevice GetDeviceAtXRNode(XRNode node)
        {
            UInt64 deviceId = InputTracking.GetDeviceIdAtXRNode(node);
            return new InputDevice(deviceId);
        }

        public static void GetDevicesAtXRNode(XRNode node, List<InputDevice> inputDevices)
        {
            if (null == inputDevices)
                throw new ArgumentNullException("inputDevices");

            List<UInt64> deviceIds = new List<UInt64>();
            InputTracking.GetDeviceIdsAtXRNode_Internal(node, deviceIds);

            inputDevices.Clear();
            foreach (var deviceId in deviceIds)
            {
                InputDevice nodeDevice = new InputDevice(deviceId);
                if (nodeDevice.isValid)
                    inputDevices.Add(nodeDevice);
            }
        }

        public static void GetDevices(List<InputDevice> inputDevices)
        {
            if (null == inputDevices)
                throw new ArgumentNullException("inputDevices");

            inputDevices.Clear();
            GetDevices_Internal(inputDevices);
        }

        [Obsolete("This API has been marked as deprecated and will be removed in future versions. Please use InputDevices.GetDevicesWithCharacteristics instead.")]
        public static void GetDevicesWithRole(InputDeviceRole role, List<InputDevice> inputDevices)
        {
            if (null == inputDevices)
                throw new ArgumentNullException("inputDevices");

            if (s_InputDeviceList == null)
                s_InputDeviceList = new List<InputDevice>();
            GetDevices_Internal(s_InputDeviceList);

            inputDevices.Clear();
            foreach (var device in s_InputDeviceList)
                if (device.role == role)
                    inputDevices.Add(device);
        }

        /// Used to avoid creating garbage when getting all devices from native.  Do not use without first calling GetDevices_Internal in order to keep it up to date.
        static List<InputDevice> s_InputDeviceList;
        public static void GetDevicesWithCharacteristics(InputDeviceCharacteristics desiredCharacteristics, List<InputDevice> inputDevices)
        {
            if (null == inputDevices)
                throw new ArgumentNullException("inputDevices");

            if (s_InputDeviceList == null)
                s_InputDeviceList = new List<InputDevice>();
            GetDevices_Internal(s_InputDeviceList);

            inputDevices.Clear();
            foreach (var device in s_InputDeviceList)
                if ((device.characteristics & desiredCharacteristics) == desiredCharacteristics)
                    inputDevices.Add(device);
        }

        public static event Action<InputDevice> deviceConnected;
        public static event Action<InputDevice> deviceDisconnected;
        public static event Action<InputDevice> deviceConfigChanged;

        [RequiredByNativeCode]
        private static void InvokeConnectionEvent(UInt64 deviceId, ConnectionChangeType change)
        {
            switch (change)
            {
                case ConnectionChangeType.Connected:
                {
                    if (deviceConnected != null)
                        deviceConnected(new InputDevice(deviceId));
                    break;
                }
                case ConnectionChangeType.Disconnected:
                {
                    if (deviceDisconnected != null)
                        deviceDisconnected(new InputDevice(deviceId));
                    break;
                }
                case ConnectionChangeType.ConfigChange:
                {
                    if (deviceConfigChanged != null)
                        deviceConfigChanged(new InputDevice(deviceId));
                    break;
                }
            }
        }

        private static extern void GetDevices_Internal([NotNull] List<InputDevice> inputDevices);

        internal static extern bool SendHapticImpulse(UInt64 deviceId, uint channel, float amplitude, float duration);
        internal static extern bool SendHapticBuffer(UInt64 deviceId, uint channel, [NotNull] byte[] buffer);
        internal static extern bool TryGetHapticCapabilities(UInt64 deviceId, out HapticCapabilities capabilities);
        internal static extern void StopHaptics(UInt64 deviceId);

        internal static extern bool TryGetFeatureUsages(UInt64 deviceId, [NotNull] List<InputFeatureUsage> featureUsages);

        internal static extern bool TryGetFeatureValue_bool(UInt64 deviceId, string usage, out bool value);
        internal static extern bool TryGetFeatureValue_UInt32(UInt64 deviceId, string usage, out uint value);
        internal static extern bool TryGetFeatureValue_float(UInt64 deviceId, string usage, out float value);
        internal static extern bool TryGetFeatureValue_Vector2f(UInt64 deviceId, string usage, out Vector2 value);
        internal static extern bool TryGetFeatureValue_Vector3f(UInt64 deviceId, string usage, out Vector3 value);
        internal static extern bool TryGetFeatureValue_Quaternionf(UInt64 deviceId, string usage, out Quaternion value);
        internal static extern bool TryGetFeatureValue_Custom(UInt64 deviceId, string usage, [Out] byte[] value);

        internal static extern bool TryGetFeatureValueAtTime_bool(UInt64 deviceId, string usage, Int64 time, out bool value);
        internal static extern bool TryGetFeatureValueAtTime_UInt32(UInt64 deviceId, string usage, Int64 time, out uint value);
        internal static extern bool TryGetFeatureValueAtTime_float(UInt64 deviceId, string usage, Int64 time, out float value);
        internal static extern bool TryGetFeatureValueAtTime_Vector2f(UInt64 deviceId, string usage, Int64 time, out Vector2 value);
        internal static extern bool TryGetFeatureValueAtTime_Vector3f(UInt64 deviceId, string usage, Int64 time, out Vector3 value);
        internal static extern bool TryGetFeatureValueAtTime_Quaternionf(UInt64 deviceId, string usage, Int64 time, out Quaternion value);

        internal static extern bool TryGetFeatureValue_XRHand(UInt64 deviceId, string usage, out Hand value);
        internal static extern bool TryGetFeatureValue_XRBone(UInt64 deviceId, string usage, out Bone value);
        internal static extern bool TryGetFeatureValue_XREyes(UInt64 deviceId, string usage, out Eyes value);

        internal static extern bool IsDeviceValid(UInt64 deviceId);
        internal static extern string GetDeviceName(UInt64 deviceId);
        internal static extern string GetDeviceManufacturer(UInt64 deviceId);
        internal static extern string GetDeviceSerialNumber(UInt64 deviceId);
        internal static extern InputDeviceCharacteristics GetDeviceCharacteristics(UInt64 deviceId);

        internal static InputDeviceRole GetDeviceRole(UInt64 deviceId)
        {
            InputDeviceCharacteristics flags = GetDeviceCharacteristics(deviceId);

            const InputDeviceCharacteristics genericCharacteristics = InputDeviceCharacteristics.HeadMounted | InputDeviceCharacteristics.TrackedDevice;
            const InputDeviceCharacteristics leftHandedCharacteristics = InputDeviceCharacteristics.Left | InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice;
            const InputDeviceCharacteristics rightHandedCharacteristics = InputDeviceCharacteristics.Right | InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice;
            const InputDeviceCharacteristics trackingReferenceCharacteristics = InputDeviceCharacteristics.TrackingReference | InputDeviceCharacteristics.TrackedDevice;

            if ((flags & genericCharacteristics) == genericCharacteristics)
                return InputDeviceRole.Generic;
            else if ((flags & leftHandedCharacteristics) == leftHandedCharacteristics)
                return InputDeviceRole.LeftHanded;
            else if ((flags & rightHandedCharacteristics) == rightHandedCharacteristics)
                return InputDeviceRole.RightHanded;
            else if ((flags & InputDeviceCharacteristics.Controller) == InputDeviceCharacteristics.Controller)
                return InputDeviceRole.GameController;
            else if ((flags & trackingReferenceCharacteristics) == trackingReferenceCharacteristics)
                return InputDeviceRole.TrackingReference;
            else if ((flags & InputDeviceCharacteristics.TrackedDevice) == InputDeviceCharacteristics.TrackedDevice)
                return InputDeviceRole.HardwareTracker;

            return InputDeviceRole.Unknown;
        }
    }

    [NativeHeader("Modules/XR/Subsystems/Input/Public/XRInputTrackingFacade.h")]
    [NativeConditional("ENABLE_VR")]
    [StaticAccessor("XRInputTrackingFacade::Get()", StaticAccessorType.Dot)]
    public partial class InputTracking
    {
        [NativeConditional("ENABLE_VR", "Vector3f::zero")]
        [Obsolete("This API has been marked as obsolete in code, and is no longer in use. Please use InputTracking.GetNodeStates and look for the XRNodeState with the corresponding XRNode type instead.")]
        extern public static Vector3 GetLocalPosition(XRNode node);

        [NativeConditional("ENABLE_VR", "Quaternionf::identity()")]
        [Obsolete("This API has been marked as obsolete in code, and is no longer in use. Please use InputTracking.GetNodeStates and look for the XRNodeState with the corresponding XRNode type instead.")]
        extern public static Quaternion GetLocalRotation(XRNode node);

        [NativeConditional("ENABLE_VR")]
        extern public static void Recenter();

        [NativeConditional("ENABLE_VR")]
        extern public static string GetNodeName(ulong uniqueId);

        public static void GetNodeStates(List<XRNodeState> nodeStates)
        {
            if (null == nodeStates)
                throw new ArgumentNullException("nodeStates");

            nodeStates.Clear();
            GetNodeStates_Internal(nodeStates);
        }

        [NativeConditional("ENABLE_VR")]
        extern private static void GetNodeStates_Internal([NotNull] List<XRNodeState> nodeStates);

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

        [NativeHeader("Modules/XR/Subsystems/Input/Public/XRInputTracking.h")]
        [StaticAccessor("XRInputTracking::Get()", StaticAccessorType.Dot)]
        internal static extern void GetDeviceIdsAtXRNode_Internal(XRNode node, [NotNull] List<UInt64> deviceIds);
    }
}
