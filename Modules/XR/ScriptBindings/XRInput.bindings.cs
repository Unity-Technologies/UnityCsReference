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
    };

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
    };

    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeConditional("ENABLE_VR")]
    [NativeHeader("Modules/XR/Subsystems/Input/Public/XRInputDevices.h")]
    public struct InputFeatureUsage : IEquatable<InputFeatureUsage>
    {
        internal string m_Name;
        [NativeName("m_FeatureType")]  internal InputFeatureType m_InternalType;

        public string name { get { return m_Name; } internal set { m_Name = value; } }
        internal InputFeatureType internalType { get { return m_InternalType; } set { m_InternalType = value; } }
        public Type type
        {
            get
            {
                switch (m_InternalType)
                {
                    case InputFeatureType.Custom:           throw new InvalidCastException("No valid managed type for Custom native types.");
                    case InputFeatureType.Binary:           return typeof(bool);
                    case InputFeatureType.DiscreteStates:   return typeof(uint);
                    case InputFeatureType.Axis1D:           return typeof(float);
                    case InputFeatureType.Axis2D:           return typeof(Vector2);
                    case InputFeatureType.Axis3D:           return typeof(Vector3);
                    case InputFeatureType.Rotation:         return typeof(Quaternion);
                    case InputFeatureType.Hand:             return typeof(Hand);
                    case InputFeatureType.Bone:             return typeof(Bone);
                    case InputFeatureType.Eyes:             return typeof(Eyes);

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
        public static InputFeatureUsage<bool> thumbrest = new InputFeatureUsage<bool>("Thumbrest");
        public static InputFeatureUsage<bool> indexTouch = new InputFeatureUsage<bool>("IndexTouch");
        public static InputFeatureUsage<bool> thumbTouch = new InputFeatureUsage<bool>("ThumbTouch");

        public static InputFeatureUsage<float> batteryLevel = new InputFeatureUsage<float>("BatteryLevel");
        public static InputFeatureUsage<float> trigger = new InputFeatureUsage<float>("Trigger");
        public static InputFeatureUsage<float> grip = new InputFeatureUsage<float>("Grip");
        public static InputFeatureUsage<float> indexFinger = new InputFeatureUsage<float>("IndexFinger");
        public static InputFeatureUsage<float> middleFinger = new InputFeatureUsage<float>("MiddleFinger");
        public static InputFeatureUsage<float> ringFinger = new InputFeatureUsage<float>("RingFinger");
        public static InputFeatureUsage<float> pinkyFinger = new InputFeatureUsage<float>("PinkyFinger");

        public static InputFeatureUsage<Vector2> primary2DAxis = new InputFeatureUsage<Vector2>("Primary2DAxis");
        public static InputFeatureUsage<Vector2> dPad = new InputFeatureUsage<Vector2>("DPad");
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
    };

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeConditional("ENABLE_VR")]
    public struct InputDevice : IEquatable<InputDevice>
    {
        private UInt64 m_DeviceId;
        internal InputDevice(UInt64 deviceId) { m_DeviceId = deviceId; }

        public bool isValid { get { return InputDevices.IsDeviceValid(m_DeviceId); } }
        public string name { get { return InputDevices.GetDeviceName(m_DeviceId); } }
        public InputDeviceRole role { get { return InputDevices.GetDeviceRole(m_DeviceId); } }

        // Haptics
        public bool SendHapticImpulse(uint channel, float amplitude, float duration = 1.0f)     { return InputDevices.SendHapticImpulse(m_DeviceId, channel, amplitude, duration); }
        public bool SendHapticBuffer(uint channel, byte[] buffer)                               { return InputDevices.SendHapticBuffer(m_DeviceId, channel, buffer); }
        public bool TryGetHapticCapabilities(out HapticCapabilities capabilities)               { return InputDevices.TryGetHapticCapabilities(m_DeviceId, out capabilities); }
        public void StopHaptics()                                                               { InputDevices.StopHaptics(m_DeviceId); }

        // Feature Usages
        public bool TryGetFeatureUsages(List<InputFeatureUsage> featureUsages)
        {
            return InputDevices.TryGetFeatureUsages(m_DeviceId, featureUsages);
        }

        // Features
        public bool TryGetFeatureValue(InputFeatureUsage<bool> usage, out bool value)              { return InputDevices.TryGetFeatureValue_bool(m_DeviceId, usage.name, out value); }
        public bool TryGetFeatureValue(InputFeatureUsage<uint> usage, out uint value)              { return InputDevices.TryGetFeatureValue_UInt32(m_DeviceId, usage.name, out value); }
        public bool TryGetFeatureValue(InputFeatureUsage<float> usage, out float value)            { return InputDevices.TryGetFeatureValue_float(m_DeviceId, usage.name, out value); }
        public bool TryGetFeatureValue(InputFeatureUsage<Vector2> usage, out Vector2 value)        { return InputDevices.TryGetFeatureValue_Vector2f(m_DeviceId, usage.name, out value); }
        public bool TryGetFeatureValue(InputFeatureUsage<Vector3> usage, out Vector3 value)        { return InputDevices.TryGetFeatureValue_Vector3f(m_DeviceId, usage.name, out value); }
        public bool TryGetFeatureValue(InputFeatureUsage<Quaternion> usage, out Quaternion value)  { return InputDevices.TryGetFeatureValue_Quaternionf(m_DeviceId, usage.name, out value); }
        public bool TryGetFeatureValue(InputFeatureUsage<Hand> usage, out Hand value)              { return InputDevices.TryGetFeatureValue_XRHand(m_DeviceId, usage.name, out value); }
        public bool TryGetFeatureValue(InputFeatureUsage<Bone> usage, out Bone value)              { return InputDevices.TryGetFeatureValue_XRBone(m_DeviceId, usage.name, out value); }
        public bool TryGetFeatureValue(InputFeatureUsage<Eyes> usage, out Eyes value)              { return InputDevices.TryGetFeatureValue_XREyes(m_DeviceId, usage.name, out value); }

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

    public enum HandFinger
    {
        Thumb,
        Index,
        Middle,
        Ring,
        Pinky
    };

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
    };

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

        public static void GetDevicesWithRole(InputDeviceRole role, List<InputDevice> inputDevices)
        {
            if (null == inputDevices)
                throw new ArgumentNullException("inputDevices");

            List<InputDevice> allDevices = new List<InputDevice>();
            GetDevices_Internal(allDevices);

            inputDevices.Clear();
            foreach (var device in allDevices)
                if (device.role == role)
                    inputDevices.Add(device);
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
        internal static extern bool TryGetFeatureValue_XRHand(UInt64 deviceId, string usage, out Hand value);
        internal static extern bool TryGetFeatureValue_XRBone(UInt64 deviceId, string usage, out Bone value);
        internal static extern bool TryGetFeatureValue_XREyes(UInt64 deviceId, string usage, out Eyes value);

        internal static extern bool IsDeviceValid(UInt64 deviceId);
        internal static extern string GetDeviceName(UInt64 deviceId);
        internal static extern InputDeviceRole GetDeviceRole(UInt64 deviceId);
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
