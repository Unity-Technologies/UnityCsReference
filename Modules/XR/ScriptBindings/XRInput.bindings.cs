// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.XR
{
    ///<summary>Describes the haptic capabilities of the device at an <see cref="XR.XRNode" /> in the XR input subsystem.</summary>
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

        ///<summary>The number of channels that this device plays back haptic data.</summary>
        public uint numChannels { get { return m_NumChannels; } internal set { m_NumChannels = value; } }
        ///<summary>True if this device supports sending a haptic impulse.</summary>
        public bool supportsImpulse { get { return m_SupportsImpulse; } internal set { m_SupportsImpulse = value; } }
        ///<summary>True if this device supports sending a haptic buffer.</summary>
        public bool supportsBuffer { get { return m_SupportsBuffer; } internal set { m_SupportsBuffer = value; } }
        ///<summary>The frequency (in Hz) that this device plays back buffered haptic data.</summary>
        public uint bufferFrequencyHz { get { return m_BufferFrequencyHz; } internal set { m_BufferFrequencyHz = value; } }
        ///<summary>The maximum amount of data that can be sent to an <see cref="InputDevice" /> via <see cref="InputDevice.SendHapticBuffer" />.</summary>
        ///<remarks>If the InputDevice receives an amount of data greater than this, then <see cref="InputDevice.SendHapticBuffer" /> fails and a rumble is not  triggered.
        ///Check that the device supports haptic buffers via <see cref="InputDevice.TryGetHapticCapabilities" /> and <see cref="HapticCapabilities.supportsBuffer" />.
        ///Will be set to 0 if <see cref="HapticCapabilities.supportsBuffer" /> is false.</remarks>
        public uint bufferMaxSize { get { return m_BufferMaxSize; } internal set { m_BufferMaxSize = value; } }
        ///<summary>The optimal buffer size an <see cref="InputDevice" /> expects to be sent via <see cref="InputDevice.SendHapticBuffer" /> in order to provide a continuous rumble between individual frames.</summary>
        ///<remarks>Check that the device supports haptic buffers via <see cref="InputDevice.TryGetHapticCapabilities" /> and <see cref="HapticCapabilities.supportsBuffer" />.
        ///Will be set to 0 if <see cref="HapticCapabilities.supportsBuffer" /> is false.</remarks>
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

        ///<exclude />
        public static bool operator==(HapticCapabilities a, HapticCapabilities b)
        {
            return a.Equals(b);
        }

        ///<exclude />
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

    ///<summary>Enumeration describing the role of a <see cref="XR.InputDevice" /> in providing input.</summary>
    public enum InputDeviceRole : UInt32
    {
        ///<summary>This device does not have a known role.</summary>
        Unknown = 0,
        ///<summary>This device is typically a HMD or Camera.</summary>
        Generic,
        ///<summary>This device is a controller that represents the left hand.</summary>
        LeftHanded,
        ///<summary>This device is a controller that represents the right hand.</summary>
        RightHanded,
        ///<summary>This device is a game controller.</summary>
        GameController,
        ///<summary>This device is a tracking reference used to track other devices in 3D.</summary>
        TrackingReference,
        ///<summary>This device is a hardware tracker.</summary>
        HardwareTracker,
        ///<summary>This device is a legacy controller.</summary>
        LegacyController
    }

    ///<summary>A set of bit flags describing <see cref="XR.InputDevice" /> characteristics.</summary>
    ///<remarks>The XR system combines the **InputDeviceFlags** members into the <see cref="XR.InputDevice.characteristics" /> bitmask to describe the characteristics and capabilities of an input device. You can also pass a bitwise combination of flags from this enumeration to <see cref="XR.InputDevices.GetDevicesWithCharacteristics" /> to get a list of devices with specific characteristics. For example, you could use the following to get the right-hand controller:
    ///
    ///<c>(InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Right)</c>.</remarks>
    [Flags]
    public enum InputDeviceCharacteristics : UInt32
    {
        ///<summary>A default value specifying no flags.</summary>
        None = 0,
        ///<summary>The <see cref="InputDevice" /> is attached to the head.</summary>
        HeadMounted = 1 << 0,
        ///<summary>The <see cref="InputDevice" /> has a camera and associated camera tracking information.</summary>
        Camera = 1 << 1,
        ///<summary>The <see cref="InputDevice" /> is held in the user's hand. Typically, a tracked controller.</summary>
        HeldInHand = 1 << 2,
        ///<summary>The <see cref="InputDevice" /> provides hand tracking information via a <see cref="Hand" /> input feature.</summary>
        HandTracking = 1 << 3,
        ///<summary>The <see cref="InputDevice" /> provides eye tracking information via an <see cref="Eyes" /> input feature.</summary>
        EyeTracking = 1 << 4,
        ///<summary>The <see cref="InputDevice" /> provides 3DOF or 6DOF tracking data.</summary>
        ///<remarks>Devices of this type have their tracking data reported through UnityEngine.XR.InputTracking.</remarks>
        TrackedDevice = 1 << 5,
        ///<summary>The <see cref="InputDevice" /> is a game controller.</summary>
        ///<remarks>Game Controllers have axes and buttons that can be accessed through <see cref="T:UnityEngine.Input" />.</remarks>
        Controller = 1 << 6,
        ///<summary>The <see cref="InputDevice" /> is an unmoving reference object used to locate and track other objects in the world.</summary>
        TrackingReference = 1 << 7,
        ///<summary>The <see cref="InputDevice" /> is associated with the left side of the user.</summary>
        Left = 1 << 8,
        ///<summary>The <see cref="InputDevice" /> is associated with the right side of the user.</summary>
        Right = 1 << 9,
        ///<summary>The <see cref="InputDevice" /> reports software approximated, positional data.</summary>
        ///<remarks>This <see cref="InputDevice" /> can only sense rotation, and its reported positional data is approximated.</remarks>
        Simulated6DOF = 1 << 10
    }

    ///<summary>Represents the values being tracked for this device.</summary>
    [Flags]
    public enum InputTrackingState : UInt32
    {
        ///<summary>Represents no values being tracked for this device.</summary>
        None = 0,
        ///<summary>Represents position being tracked for this device.</summary>
        Position = 1 << 0,
        ///<summary>Represents rotation being tracked for this device.</summary>
        Rotation = 1 << 1,
        ///<summary>Represents velocity being tracked for this device.</summary>
        Velocity = 1 << 2,
        ///<summary>Represents no angular velocity being tracked for this device.</summary>
        AngularVelocity = 1 << 3,
        ///<summary>Represents acceleration being tracked for this device.</summary>
        Acceleration = 1 << 4,
        ///<summary>Represents angular acceleration being tracked for this device.</summary>
        AngularAcceleration = 1 << 5,

        ///<summary>Represents all InputTrackingState values being tracked for this device.</summary>
        All = (1 << 6) - 1 // Keep this as the last entry, if you add an entry, bump this shift up by 1 as well
    }

    ///<summary>Defines a generic usage that maps to an input feature on a device. Use the As method to turn into a generic usage.</summary>
    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeConditional("ENABLE_VR")]
    [NativeHeader("Modules/XR/Subsystems/Input/Public/XRInputDevices.h")]
    public struct InputFeatureUsage : IEquatable<InputFeatureUsage>
    {
        internal string m_Name;
        [NativeName("m_FeatureType")] internal InputFeatureType m_InternalType;
        ///<summary>The string name of this usage feature; used internally to map to an input feature on a device.</summary>
        public string name { get { return m_Name; } internal set { m_Name = value; } }
        internal InputFeatureType internalType { get { return m_InternalType; } set { m_InternalType = value; } }
        ///<summary>The type of this usage feature; used internally to map to an input feature on a device.</summary>
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

        ///<exclude />
        public override bool Equals(object obj)
        {
            if (!(obj is InputFeatureUsage))
                return false;
            return Equals((InputFeatureUsage)obj);
        }

        ///<exclude />
        public bool Equals(InputFeatureUsage other)
        {
            return name == other.name && internalType == other.internalType;
        }

        ///<exclude />
        public override int GetHashCode()
        {
            return name.GetHashCode() ^ (internalType.GetHashCode() << 1);
        }

        ///<exclude />
        public static bool operator==(InputFeatureUsage a, InputFeatureUsage b)
        {
            return a.Equals(b);
        }

        ///<exclude />
        public static bool operator!=(InputFeatureUsage a, InputFeatureUsage b)
        {
            return !(a == b);
        }

        ///<summary>Returns the generic version of this type for retrieving a feature value from a device.</summary>
        public InputFeatureUsage<T> As<T>()
        {
            if (type != typeof(T))
                throw new ArgumentException("InputFeatureUsage type does not match out variable type.");
            return new InputFeatureUsage<T>(this.name);
        }
    }

    ///<summary>Defines a generic usage that maps to an input feature on a device.</summary>
    public struct InputFeatureUsage<T> : IEquatable<InputFeatureUsage<T>>
    {
        ///<summary>The string name of this usage feature; used internally to map to an input feature on a device.</summary>
        public string name { get; set; }
        ///<summary>Construct a usage from a usage name.</summary>
        ///<param name="usageName">The name of the feature usage to query for.</param>
        public InputFeatureUsage(string usageName) { name = usageName; }
        ///<exclude />
        public override bool Equals(object obj)
        {
            if (!(obj is InputFeatureUsage<T>))
                return false;
            return Equals((InputFeatureUsage<T>)obj);
        }

        ///<exclude />
        public bool Equals(InputFeatureUsage<T> other)
        {
            return name == other.name;
        }

        ///<exclude />
        public override int GetHashCode()
        {
            return name.GetHashCode();
        }

        ///<exclude />
        public static bool operator==(InputFeatureUsage<T> a, InputFeatureUsage<T> b)
        {
            return a.Equals(b);
        }

        ///<exclude />
        public static bool operator!=(InputFeatureUsage<T> a, InputFeatureUsage<T> b)
        {
            return !(a == b);
        }

        private Type usageType { get { return typeof(T); } }
        ///<summary>Converts a generic InputFeatureUsage&lt;T&gt; into an InputFeatureUsage.</summary>
        ///<param name="self">The generic <see cref="InputFeatureUsage{T}" /> to convert into an <see cref="InputFeatureUsage" />.</param>
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

    ///<summary>Defines static variables that are used to retrieve input features from XR.InputDevice.TryGetFeatureValue.</summary>
    ///<remarks>Use these static variables to retrieve common feature values by usage for an XR.InputDevice.</remarks>
    public static class CommonUsages
    {
        ///<summary>Informs to the developer whether the device is currently being tracked.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<bool> isTracked = new InputFeatureUsage<bool>("IsTracked");
        ///<summary>The primary face button being pressed on a device, or sole button if only one is available.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<bool> primaryButton = new InputFeatureUsage<bool>("PrimaryButton");
        ///<summary>The primary face button being touched on a device.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<bool> primaryTouch = new InputFeatureUsage<bool>("PrimaryTouch");
        ///<summary>The secondary face button being pressed on a device.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<bool> secondaryButton = new InputFeatureUsage<bool>("SecondaryButton");
        ///<summary>The secondary face button being touched on a device.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<bool> secondaryTouch = new InputFeatureUsage<bool>("SecondaryTouch");
        ///<summary>A binary measure of whether the device is being gripped.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<bool> gripButton = new InputFeatureUsage<bool>("GripButton");
        ///<summary>A binary measure of whether the index finger is activating the trigger.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<bool> triggerButton = new InputFeatureUsage<bool>("TriggerButton");
        ///<summary>Represents a menu button, used to pause, go back, or otherwise exit gameplay.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<bool> menuButton = new InputFeatureUsage<bool>("MenuButton");
        ///<summary>Represents the primary 2D axis being clicked or otherwise depressed.</summary>
        ///<remarks>The primary 2D axis is the <see cref="Vector2" /> input feature tagged with <see cref="CommonUsages.primary2DAxis" />. A click means that the user presses the thumbstick or touchpad inward, toward the body of the controller. Moving the thumbstick or touchpad to one side changes the value of <see cref="CommonUsages.primary2DAxis" />, but doesn't set this feature to true.</remarks>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<bool> primary2DAxisClick = new InputFeatureUsage<bool>("Primary2DAxisClick");
        ///<summary>Represents the primary 2D axis being touched.</summary>
        ///<remarks>The primary 2D axis is the <see cref="Vector2" /> input feature tagged with <see cref="CommonUsages.primary2DAxis" />.</remarks>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<bool> primary2DAxisTouch = new InputFeatureUsage<bool>("Primary2DAxisTouch");
        ///<summary>Represents the secondary 2D axis being clicked or otherwise depressed.</summary>
        ///<remarks>The secondary 2D axis is the <see cref="Vector2" /> input feature tagged with <see cref="CommonUsages.secondary2DAxis" />. A click means that the user presses the joystick or touchpad inward, toward the body of the controller. Moving the joystick or touchpad to one side changes the value of <see cref="CommonUsages.secondary2DAxis" />, but doesn't set this feature to true.</remarks>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<bool> secondary2DAxisClick = new InputFeatureUsage<bool>("Secondary2DAxisClick");
        ///<summary>Represents the secondary 2D axis being touched.</summary>
        ///<remarks>The secondary 2D axis is the <see cref="Vector2" /> input feature tagged with <see cref="CommonUsages.secondary2DAxis" />.</remarks>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<bool> secondary2DAxisTouch = new InputFeatureUsage<bool>("Secondary2DAxisTouch");
        ///<summary>Use this property to test whether the user is currently wearing and/or interacting with the XR device. The exact behavior of this property varies with each type of device: some devices have a sensor specifically to detect user proximity, however you can reasonably infer that a user is present with the device when the property is <c>true</c>.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<bool> userPresence = new InputFeatureUsage<bool>("UserPresence");

        ///<summary>Represents the values being tracked for this device.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<InputTrackingState> trackingState = new InputFeatureUsage<InputTrackingState>("TrackingState");

        ///<summary>Value representing the current battery life of this device.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<float> batteryLevel = new InputFeatureUsage<float>("BatteryLevel");
        ///<summary>A trigger-like control, pressed with the index finger.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<float> trigger = new InputFeatureUsage<float>("Trigger");
        ///<summary>Represents the users grip on the controller.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<float> grip = new InputFeatureUsage<float>("Grip");

        ///<summary>The primary touchpad or joystick on a device.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<Vector2> primary2DAxis = new InputFeatureUsage<Vector2>("Primary2DAxis");
        ///<summary>A secondary touchpad or joystick on a device.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<Vector2> secondary2DAxis = new InputFeatureUsage<Vector2>("Secondary2DAxis");

        ///<summary>The position of the device.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<Vector3> devicePosition = new InputFeatureUsage<Vector3>("DevicePosition");
        ///<summary>The position of the left eye on this device.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<Vector3> leftEyePosition = new InputFeatureUsage<Vector3>("LeftEyePosition");
        ///<summary>The position of the right eye on this device.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<Vector3> rightEyePosition = new InputFeatureUsage<Vector3>("RightEyePosition");
        ///<summary>The position of the center eye on this device.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<Vector3> centerEyePosition = new InputFeatureUsage<Vector3>("CenterEyePosition");
        ///<summary>The position of the color camera on this device.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<Vector3> colorCameraPosition = new InputFeatureUsage<Vector3>("CameraPosition");
        ///<summary>The velocity of the device.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<Vector3> deviceVelocity = new InputFeatureUsage<Vector3>("DeviceVelocity");
        ///<summary>The angular velocity of this device, formatted as euler angles.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<Vector3> deviceAngularVelocity = new InputFeatureUsage<Vector3>("DeviceAngularVelocity");
        ///<summary>The velocity of the left eye on this device.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<Vector3> leftEyeVelocity = new InputFeatureUsage<Vector3>("LeftEyeVelocity");
        ///<summary>The angular velocity of the left eye on this device, formatted as euler angles.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<Vector3> leftEyeAngularVelocity = new InputFeatureUsage<Vector3>("LeftEyeAngularVelocity");
        ///<summary>The velocity of the right eye on this device.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<Vector3> rightEyeVelocity = new InputFeatureUsage<Vector3>("RightEyeVelocity");
        ///<summary>The angular velocity of the right eye on this device, formatted as euler angles.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<Vector3> rightEyeAngularVelocity = new InputFeatureUsage<Vector3>("RightEyeAngularVelocity");
        ///<summary>The velocity of the center eye on this device.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<Vector3> centerEyeVelocity = new InputFeatureUsage<Vector3>("CenterEyeVelocity");
        ///<summary>The angular velocity of the center eye on this device, formatted as euler angles.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<Vector3> centerEyeAngularVelocity = new InputFeatureUsage<Vector3>("CenterEyeAngularVelocity");
        ///<summary>The velocity of the color camera on this device.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<Vector3> colorCameraVelocity = new InputFeatureUsage<Vector3>("CameraVelocity");
        ///<summary>The angular velocity of the color camera on this device, formatted as euler angles.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<Vector3> colorCameraAngularVelocity = new InputFeatureUsage<Vector3>("CameraAngularVelocity");
        ///<summary>The acceleration of the device.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<Vector3> deviceAcceleration = new InputFeatureUsage<Vector3>("DeviceAcceleration");
        ///<summary>The angular acceleration of this device, formatted as euler angles.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<Vector3> deviceAngularAcceleration = new InputFeatureUsage<Vector3>("DeviceAngularAcceleration");
        ///<summary>The acceleration of the left eye on this device.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<Vector3> leftEyeAcceleration = new InputFeatureUsage<Vector3>("LeftEyeAcceleration");
        ///<summary>The angular acceleration of the left eye on this device, formatted as euler angles.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<Vector3> leftEyeAngularAcceleration = new InputFeatureUsage<Vector3>("LeftEyeAngularAcceleration");
        ///<summary>The acceleration of the right eye on this device.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<Vector3> rightEyeAcceleration = new InputFeatureUsage<Vector3>("RightEyeAcceleration");
        ///<summary>The angular acceleration of the right eye on this device, formatted as euler angles.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<Vector3> rightEyeAngularAcceleration = new InputFeatureUsage<Vector3>("RightEyeAngularAcceleration");
        ///<summary>The acceleration of the center eye on this device.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<Vector3> centerEyeAcceleration = new InputFeatureUsage<Vector3>("CenterEyeAcceleration");
        ///<summary>The angular acceleration of the center eye on this device, formatted as euler angles.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<Vector3> centerEyeAngularAcceleration = new InputFeatureUsage<Vector3>("CenterEyeAngularAcceleration");
        ///<summary>The acceleration of the color camera on this device.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<Vector3> colorCameraAcceleration = new InputFeatureUsage<Vector3>("CameraAcceleration");
        ///<summary>The angular acceleration of the color camera on this device, formatted as euler angles.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<Vector3> colorCameraAngularAcceleration = new InputFeatureUsage<Vector3>("CameraAngularAcceleration");

        ///<summary>The rotation of this device.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<Quaternion> deviceRotation = new InputFeatureUsage<Quaternion>("DeviceRotation");
        ///<summary>The rotation of the left eye on this device.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<Quaternion> leftEyeRotation = new InputFeatureUsage<Quaternion>("LeftEyeRotation");
        ///<summary>The rotation of the right eye on this device.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<Quaternion> rightEyeRotation = new InputFeatureUsage<Quaternion>("RightEyeRotation");
        ///<summary>The rotation of the center eye on this device.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<Quaternion> centerEyeRotation = new InputFeatureUsage<Quaternion>("CenterEyeRotation");
        ///<summary>The rotation of the color camera on this device.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<Quaternion> colorCameraRotation = new InputFeatureUsage<Quaternion>("CameraRotation");

        ///<summary>Value representing the hand data for this device.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<Hand> handData = new InputFeatureUsage<Hand>("HandData");
        ///<summary>An Eyes struct containing eye tracking data collected from the device.</summary>
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<Eyes> eyesData = new InputFeatureUsage<Eyes>("EyesData");

        ///<summary>A non-handed 2D axis.</summary>
        [Obsolete("CommonUsages.dPad is not used by any XR platform and will be removed.")]
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<Vector2> dPad = new InputFeatureUsage<Vector2>("DPad");
        ///<summary>Represents the grip pressure or angle of the index finger.</summary>
        [Obsolete("CommonUsages.indexFinger is not used by any XR platform and will be removed.")]
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<float> indexFinger = new InputFeatureUsage<float>("IndexFinger");
        ///<summary>Represents the grip pressure or angle of the middle finger.</summary>
        [Obsolete("CommonUsages.MiddleFinger is not used by any XR platform and will be removed.")]
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<float> middleFinger = new InputFeatureUsage<float>("MiddleFinger");
        ///<summary>Represents the grip pressure or angle of the ring finger.</summary>
        [Obsolete("CommonUsages.RingFinger is not used by any XR platform and will be removed.")]
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<float> ringFinger = new InputFeatureUsage<float>("RingFinger");
        ///<summary>Represents the grip pressure or angle of the pinky finger.</summary>
        [Obsolete("CommonUsages.PinkyFinger is not used by any XR platform and will be removed.")]
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<float> pinkyFinger = new InputFeatureUsage<float>("PinkyFinger");

        // These should go to Oculus SDK
        ///<summary>Represents a thumbrest or light thumb touch.</summary>
        [Obsolete("CommonUsages.thumbrest is Oculus only, and is being moved to their package. Please use OculusUsages.thumbrest. These will still function until removed.")]
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<bool> thumbrest = new InputFeatureUsage<bool>("Thumbrest");
        ///<summary>Represents a touch of the trigger or index finger.</summary>
        [Obsolete("CommonUsages.indexTouch is Oculus only, and is being moved to their package.  Please use OculusUsages.indexTouch. These will still function until removed.")]
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<float> indexTouch = new InputFeatureUsage<float>("IndexTouch");
        ///<summary>Represents the thumb pressing any input or feature.</summary>
        [Obsolete("CommonUsages.thumbTouch is Oculus only, and is being moved to their package.  Please use OculusUsages.thumbTouch. These will still function until removed.")]
        [NoAutoStaticsCleanup]
        public static InputFeatureUsage<float> thumbTouch = new InputFeatureUsage<float>("ThumbTouch");
    }

    ///<summary>Defines an input device in the XR input subsystem.</summary>
    ///<remarks>To retrieve input features or route haptic feedback to XR input devices, specify an <see cref="XR.XRNode" /> as the destination. Use <see cref="XR.XRNode.LeftHand" /> and <see cref="XR.XRNode.RightHand" /> to send haptic data to left or right devices.  You can send haptic data either as an impulse or as a buffer of raw bytes that is played back through the haptic device. You can stop haptic output or query the device for its buffered capabilities at any time.</remarks>
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeConditional("ENABLE_VR")]
    public partial struct InputDevice : IEquatable<InputDevice>
    {
        [AutoStaticsCleanupOnCodeReload] // cleared on code reload to drop stale subsystem refs
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

        ///<summary>Gets the <see cref="XRInputSubsystem" /> that reported this <see cref="InputDevice" />.</summary>
        ///<remarks>Will return null if no <see cref="XRInputSubsystem" /> is associated with this device.</remarks>
        public XRInputSubsystem subsystem
        {
            get
            {
                if (s_InputSubsystemCache == null)
                    s_InputSubsystemCache = new List<XRInputSubsystem>();

                if (m_Initialized)
                {
                    // The DeviceId is cut in two, with the hiword being the subsystem identifier, and the loword being for the specific device.
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
        ///<summary>Read Only. True if the device is currently a valid input device; otherwise false.</summary>
        ///<remarks>Use this property to determine whether this device is still valid.</remarks>
        public bool isValid { get { return IsValidId() && InputDevices.IsDeviceValid(m_DeviceId); } }
        ///<summary>Read Only. The name of the device in the XR system. This is a platform provided unique identifier for the device.</summary>
        public string name { get { return IsValidId() ? InputDevices.GetDeviceName(m_DeviceId) : null; } }
        ///<summary>Read Only. The <see cref="InputDeviceRole" /> of the device in the XR system. This is a platform provided description of how the device is used.</summary>
        [Obsolete("This API has been marked as deprecated and will be removed in future versions. Please use InputDevice.characteristics instead.")]
        public InputDeviceRole role { get { return IsValidId() ? InputDevices.GetDeviceRole(m_DeviceId) : InputDeviceRole.Unknown; } }
        ///<summary>The manufacturer of the connected Input Device.</summary>
        public string manufacturer { get { return IsValidId() ? InputDevices.GetDeviceManufacturer(m_DeviceId) : null; } }
        ///<summary>The serial number of the connected Input Device.  Blank if no serial number is available.</summary>
        public string serialNumber { get { return IsValidId() ? InputDevices.GetDeviceSerialNumber(m_DeviceId) : null; } }
        ///<summary>Read Only. A bitmask of enumerated flags describing the characteristics of this InputDevice.</summary>
        ///<remarks>Use **Characteristics** to determine whether an **InputDevice** has specific features or capabilities. For example, if the set of <see cref="XR.InputDeviceCharacteristics" /> includes both <see cref="XR.InputDeviceCharacteristics.HeldInHand" /> and <see cref="XR.InputDeviceCharacteristics.Left" />, then the device provides tracking data for the left hand.
        ///
        ///**Characteristics** is a bitmask. Use the bitwise operators to test for specific flags. For example, to determine whether an **InputDevice** has a camera, use the conditional:
        ///
        ///<c>(inputDevice.characteristics &amp; InputDeviceCharacteristics.Camera) == InputDeviceCharacteristics.Camera</c>.</remarks>
        public InputDeviceCharacteristics characteristics { get { return IsValidId() ? InputDevices.GetDeviceCharacteristics(m_DeviceId) : InputDeviceCharacteristics.None; } }

        private bool IsValidId() { return deviceId != UInt64.MaxValue; }

        // Haptics
        ///<summary>Sends a haptic impulse to a device.</summary>
        ///<remarks>Sends an impulse (amplitude and frequency) to a device.
        ///
        ///                **Note:** Not all devices support all parameters (OpenVR currently only supports amplitude). To determine whether impulse haptics are supported, call the TryGetHapticCapabilities method and inspect the supportsImpulse property.</remarks>
        ///<param name="channel">The channel to receive the impulse.</param>
        ///<param name="amplitude">The normalized (0.0 to 1.0) amplitude value of the haptic impulse to play on the device.</param>
        ///<param name="duration">The duration in seconds that the haptic impulse will play. Only supported on Oculus.</param>
        ///<returns>Returns true if successful. Returns false otherwise.</returns>
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

        ///<summary>Sends a raw buffer of haptic data to the device.</summary>
        ///<remarks>The buffered data plays at the sampleRate rate, represented by the frequencyHz value returned by a call to the TryGetCapabilities method, until it completes.
        ///
        ///                **Note:** Not all devices support playing haptic buffers. To determine whether buffered haptics are supported, call the <see cref="InputDevice.TryGetHapticCapabilities" /> method and inspect the <see cref="HapticCapabilities.supportsBuffer" /> property. Also, the size of the buffer sent to the <see cref="InputDevice" /> must never be greater than <see cref="HapticCapabilities.bufferMaxSize" />.</remarks>
        ///<param name="channel">The channel to receive the data.</param>
        ///<param name="buffer">A raw byte buffer that contains the haptic data to send to the device.</param>
        ///<returns>Returns true if successful. Returns false otherwise.</returns>
        public bool SendHapticBuffer(uint channel, byte[] buffer)
        {
            if (!IsValidId())
                return false;

            return InputDevices.SendHapticBuffer(m_DeviceId, channel, buffer);
        }

        ///<summary>Gets the haptic capabilities of the device.</summary>
        ///<param name="capabilities">A HapticCapabilities struct to receive the capabilities of this device.</param>
        ///<returns>Returns true if the device supports any form of haptics. Returns false otherwise.</returns>
        public bool TryGetHapticCapabilities(out HapticCapabilities capabilities)
        {
            if (CheckValidAndSetDefault(out capabilities))
                return InputDevices.TryGetHapticCapabilities(m_DeviceId, out capabilities);
            return false;
        }

        ///<summary>Stop all haptic playback for a device.</summary>
        public void StopHaptics()
        {
            if (IsValidId())
                InputDevices.StopHaptics(m_DeviceId);
        }

        // Feature Usages
        ///<summary>Gets a list of all the input feature usages available on this device. For example, "Trigger" or "Device Position".</summary>
        ///<param name="featureUsages">A List of <see cref="InputFeatureUsage" /> structures to receive the available features on this device.</param>
        ///<returns>true if device can be queried; otherwise false.</returns>
        public bool TryGetFeatureUsages(List<InputFeatureUsage> featureUsages)
        {
            if (IsValidId())
                return InputDevices.TryGetFeatureUsages(m_DeviceId, featureUsages);

            return false;
        }

        // Features by Usage
        ///<summary>Retrieves information about the input feature specified by the Usage parameter. Those functions which take a time parameter allow querying for that feature at a particular point in time</summary>
        ///<remarks>See XR.InputDevice.CommonUsages for valid usages that can be used to retrieve input values.  Note: not all of these features will be available on all devices.  If a feature is not available this function will return false.</remarks>
        ///<param name="usage">Usage that describes the feature to retrieve.</param>
        ///<param name="value">A variable of the appropriate type to receive the information about the feature.</param>
        ///<returns>True if the feature information is retrieved; otherwise false.</returns>
        public bool TryGetFeatureValue(InputFeatureUsage<bool> usage, out bool value)
        {
            if (CheckValidAndSetDefault(out value))
                return InputDevices.TryGetFeatureValue_bool(m_DeviceId, usage.name, out value);
            return false;
        }

        ///<summary>Retrieves information about the input feature specified by the Usage parameter. Those functions which take a time parameter allow querying for that feature at a particular point in time</summary>
        ///<remarks>See XR.InputDevice.CommonUsages for valid usages that can be used to retrieve input values.  Note: not all of these features will be available on all devices.  If a feature is not available this function will return false.</remarks>
        ///<param name="usage">Usage that describes the feature to retrieve.</param>
        ///<param name="value">A variable of the appropriate type to receive the information about the feature.</param>
        ///<returns>True if the feature information is retrieved; otherwise false.</returns>
        public bool TryGetFeatureValue(InputFeatureUsage<uint> usage, out uint value)
        {
            if (CheckValidAndSetDefault(out value))
                return InputDevices.TryGetFeatureValue_UInt32(m_DeviceId, usage.name, out value);
            return false;
        }

        ///<summary>Retrieves information about the input feature specified by the Usage parameter. Those functions which take a time parameter allow querying for that feature at a particular point in time</summary>
        ///<remarks>See XR.InputDevice.CommonUsages for valid usages that can be used to retrieve input values.  Note: not all of these features will be available on all devices.  If a feature is not available this function will return false.</remarks>
        ///<param name="usage">Usage that describes the feature to retrieve.</param>
        ///<param name="value">A variable of the appropriate type to receive the information about the feature.</param>
        ///<returns>True if the feature information is retrieved; otherwise false.</returns>
        public bool TryGetFeatureValue(InputFeatureUsage<float> usage, out float value)
        {
            if (CheckValidAndSetDefault(out value))
                return InputDevices.TryGetFeatureValue_float(m_DeviceId, usage.name, out value);
            return false;
        }

        ///<summary>Retrieves information about the input feature specified by the Usage parameter. Those functions which take a time parameter allow querying for that feature at a particular point in time</summary>
        ///<remarks>See XR.InputDevice.CommonUsages for valid usages that can be used to retrieve input values.  Note: not all of these features will be available on all devices.  If a feature is not available this function will return false.</remarks>
        ///<param name="usage">Usage that describes the feature to retrieve.</param>
        ///<param name="value">A variable of the appropriate type to receive the information about the feature.</param>
        ///<returns>True if the feature information is retrieved; otherwise false.</returns>
        public bool TryGetFeatureValue(InputFeatureUsage<Vector2> usage, out Vector2 value)
        {
            if (CheckValidAndSetDefault(out value))
                return InputDevices.TryGetFeatureValue_Vector2f(m_DeviceId, usage.name, out value);
            return false;
        }

        ///<summary>Retrieves information about the input feature specified by the Usage parameter. Those functions which take a time parameter allow querying for that feature at a particular point in time</summary>
        ///<remarks>See XR.InputDevice.CommonUsages for valid usages that can be used to retrieve input values.  Note: not all of these features will be available on all devices.  If a feature is not available this function will return false.</remarks>
        ///<param name="usage">Usage that describes the feature to retrieve.</param>
        ///<param name="value">A variable of the appropriate type to receive the information about the feature.</param>
        ///<returns>True if the feature information is retrieved; otherwise false.</returns>
        public bool TryGetFeatureValue(InputFeatureUsage<Vector3> usage, out Vector3 value)
        {
            if (CheckValidAndSetDefault(out value))
                return InputDevices.TryGetFeatureValue_Vector3f(m_DeviceId, usage.name, out value);
            return false;
        }

        ///<summary>Retrieves information about the input feature specified by the Usage parameter. Those functions which take a time parameter allow querying for that feature at a particular point in time</summary>
        ///<remarks>See XR.InputDevice.CommonUsages for valid usages that can be used to retrieve input values.  Note: not all of these features will be available on all devices.  If a feature is not available this function will return false.</remarks>
        ///<param name="usage">Usage that describes the feature to retrieve.</param>
        ///<param name="value">A variable of the appropriate type to receive the information about the feature.</param>
        ///<returns>True if the feature information is retrieved; otherwise false.</returns>
        public bool TryGetFeatureValue(InputFeatureUsage<Quaternion> usage, out Quaternion value)
        {
            if (CheckValidAndSetDefault(out value))
                return InputDevices.TryGetFeatureValue_Quaternionf(m_DeviceId, usage.name, out value);
            return false;
        }

        ///<summary>Retrieves information about the input feature specified by the Usage parameter. Those functions which take a time parameter allow querying for that feature at a particular point in time</summary>
        ///<remarks>See XR.InputDevice.CommonUsages for valid usages that can be used to retrieve input values.  Note: not all of these features will be available on all devices.  If a feature is not available this function will return false.</remarks>
        ///<param name="usage">Usage that describes the feature to retrieve.</param>
        ///<param name="value">A variable of the appropriate type to receive the information about the feature.</param>
        ///<returns>True if the feature information is retrieved; otherwise false.</returns>
        public bool TryGetFeatureValue(InputFeatureUsage<Hand> usage, out Hand value)
        {
            if (CheckValidAndSetDefault(out value))
                return InputDevices.TryGetFeatureValue_XRHand(m_DeviceId, usage.name, out value);
            return false;
        }

        ///<summary>Retrieves information about the input feature specified by the Usage parameter. Those functions which take a time parameter allow querying for that feature at a particular point in time</summary>
        ///<remarks>See XR.InputDevice.CommonUsages for valid usages that can be used to retrieve input values.  Note: not all of these features will be available on all devices.  If a feature is not available this function will return false.</remarks>
        ///<param name="usage">Usage that describes the feature to retrieve.</param>
        ///<param name="value">A variable of the appropriate type to receive the information about the feature.</param>
        ///<returns>True if the feature information is retrieved; otherwise false.</returns>
        public bool TryGetFeatureValue(InputFeatureUsage<Bone> usage, out Bone value)
        {
            if (CheckValidAndSetDefault(out value))
                return InputDevices.TryGetFeatureValue_XRBone(m_DeviceId, usage.name, out value);
            return false;
        }

        ///<summary>Retrieves information about the input feature specified by the Usage parameter. Those functions which take a time parameter allow querying for that feature at a particular point in time</summary>
        ///<remarks>See XR.InputDevice.CommonUsages for valid usages that can be used to retrieve input values.  Note: not all of these features will be available on all devices.  If a feature is not available this function will return false.</remarks>
        ///<param name="usage">Usage that describes the feature to retrieve.</param>
        ///<param name="value">A variable of the appropriate type to receive the information about the feature.</param>
        ///<returns>True if the feature information is retrieved; otherwise false.</returns>
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

        ///<summary>Retrieves information about the input feature specified by the Usage parameter. Those functions which take a time parameter allow querying for that feature at a particular point in time</summary>
        ///<remarks>See XR.InputDevice.CommonUsages for valid usages that can be used to retrieve input values.  Note: not all of these features will be available on all devices.  If a feature is not available this function will return false.</remarks>
        ///<param name="usage">Usage that describes the feature to retrieve.</param>
        ///<param name="value">A variable of the appropriate type to receive the information about the feature.</param>
        ///<returns>True if the feature information is retrieved; otherwise false.</returns>
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
        ///<summary>Retrieves information about the input feature specified by the Usage parameter. Those functions which take a time parameter allow querying for that feature at a particular point in time</summary>
        ///<remarks>See XR.InputDevice.CommonUsages for valid usages that can be used to retrieve input values.  Note: not all of these features will be available on all devices.  If a feature is not available this function will return false.</remarks>
        ///<param name="usage">Usage that describes the feature to retrieve.</param>
        ///<param name="time">A DateTime struct with the local time at which to query for data.</param>
        ///<param name="value">A variable of the appropriate type to receive the information about the feature.</param>
        ///<returns>True if the feature information is retrieved; otherwise false.</returns>
        public bool TryGetFeatureValue(InputFeatureUsage<bool> usage, DateTime time, out bool value)
        {
            if (CheckValidAndSetDefault(out value))
                return InputDevices.TryGetFeatureValueAtTime_bool(m_DeviceId, usage.name, TimeConverter.LocalDateTimeToUnixTimeMilliseconds(time), out value);
            return false;
        }

        ///<summary>Retrieves information about the input feature specified by the Usage parameter. Those functions which take a time parameter allow querying for that feature at a particular point in time</summary>
        ///<remarks>See XR.InputDevice.CommonUsages for valid usages that can be used to retrieve input values.  Note: not all of these features will be available on all devices.  If a feature is not available this function will return false.</remarks>
        ///<param name="usage">Usage that describes the feature to retrieve.</param>
        ///<param name="time">A DateTime struct with the local time at which to query for data.</param>
        ///<param name="value">A variable of the appropriate type to receive the information about the feature.</param>
        ///<returns>True if the feature information is retrieved; otherwise false.</returns>
        public bool TryGetFeatureValue(InputFeatureUsage<uint> usage, DateTime time, out uint value)
        {
            if (CheckValidAndSetDefault(out value))
                return InputDevices.TryGetFeatureValueAtTime_UInt32(m_DeviceId, usage.name, TimeConverter.LocalDateTimeToUnixTimeMilliseconds(time), out value);
            return false;
        }

        ///<summary>Retrieves information about the input feature specified by the Usage parameter. Those functions which take a time parameter allow querying for that feature at a particular point in time</summary>
        ///<remarks>See XR.InputDevice.CommonUsages for valid usages that can be used to retrieve input values.  Note: not all of these features will be available on all devices.  If a feature is not available this function will return false.</remarks>
        ///<param name="usage">Usage that describes the feature to retrieve.</param>
        ///<param name="time">A DateTime struct with the local time at which to query for data.</param>
        ///<param name="value">A variable of the appropriate type to receive the information about the feature.</param>
        ///<returns>True if the feature information is retrieved; otherwise false.</returns>
        public bool TryGetFeatureValue(InputFeatureUsage<float> usage, DateTime time, out float value)
        {
            if (CheckValidAndSetDefault(out value))
                return InputDevices.TryGetFeatureValueAtTime_float(m_DeviceId, usage.name, TimeConverter.LocalDateTimeToUnixTimeMilliseconds(time), out value);
            return false;
        }

        ///<summary>Retrieves information about the input feature specified by the Usage parameter. Those functions which take a time parameter allow querying for that feature at a particular point in time</summary>
        ///<remarks>See XR.InputDevice.CommonUsages for valid usages that can be used to retrieve input values.  Note: not all of these features will be available on all devices.  If a feature is not available this function will return false.</remarks>
        ///<param name="usage">Usage that describes the feature to retrieve.</param>
        ///<param name="time">A DateTime struct with the local time at which to query for data.</param>
        ///<param name="value">A variable of the appropriate type to receive the information about the feature.</param>
        ///<returns>True if the feature information is retrieved; otherwise false.</returns>
        public bool TryGetFeatureValue(InputFeatureUsage<Vector2> usage, DateTime time, out Vector2 value)
        {
            if (CheckValidAndSetDefault(out value))
                return InputDevices.TryGetFeatureValueAtTime_Vector2f(m_DeviceId, usage.name, TimeConverter.LocalDateTimeToUnixTimeMilliseconds(time), out value);
            return false;
        }

        ///<summary>Retrieves information about the input feature specified by the Usage parameter. Those functions which take a time parameter allow querying for that feature at a particular point in time</summary>
        ///<remarks>See XR.InputDevice.CommonUsages for valid usages that can be used to retrieve input values.  Note: not all of these features will be available on all devices.  If a feature is not available this function will return false.</remarks>
        ///<param name="usage">Usage that describes the feature to retrieve.</param>
        ///<param name="time">A DateTime struct with the local time at which to query for data.</param>
        ///<param name="value">A variable of the appropriate type to receive the information about the feature.</param>
        ///<returns>True if the feature information is retrieved; otherwise false.</returns>
        public bool TryGetFeatureValue(InputFeatureUsage<Vector3> usage, DateTime time, out Vector3 value)
        {
            if (CheckValidAndSetDefault(out value))
                return InputDevices.TryGetFeatureValueAtTime_Vector3f(m_DeviceId, usage.name, TimeConverter.LocalDateTimeToUnixTimeMilliseconds(time), out value);
            return false;
        }

        ///<summary>Retrieves information about the input feature specified by the Usage parameter. Those functions which take a time parameter allow querying for that feature at a particular point in time</summary>
        ///<remarks>See XR.InputDevice.CommonUsages for valid usages that can be used to retrieve input values.  Note: not all of these features will be available on all devices.  If a feature is not available this function will return false.</remarks>
        ///<param name="usage">Usage that describes the feature to retrieve.</param>
        ///<param name="time">A DateTime struct with the local time at which to query for data.</param>
        ///<param name="value">A variable of the appropriate type to receive the information about the feature.</param>
        ///<returns>True if the feature information is retrieved; otherwise false.</returns>
        public bool TryGetFeatureValue(InputFeatureUsage<Quaternion> usage, DateTime time, out Quaternion value)
        {
            if (CheckValidAndSetDefault(out value))
                return InputDevices.TryGetFeatureValueAtTime_Quaternionf(m_DeviceId, usage.name, TimeConverter.LocalDateTimeToUnixTimeMilliseconds(time), out value);
            return false;
        }

        ///<summary>Retrieves information about the input feature specified by the Usage parameter. Those functions which take a time parameter allow querying for that feature at a particular point in time</summary>
        ///<remarks>See XR.InputDevice.CommonUsages for valid usages that can be used to retrieve input values.  Note: not all of these features will be available on all devices.  If a feature is not available this function will return false.</remarks>
        ///<param name="usage">Usage that describes the feature to retrieve.</param>
        ///<param name="time">A DateTime struct with the local time at which to query for data.</param>
        ///<param name="value">A variable of the appropriate type to receive the information about the feature.</param>
        ///<returns>True if the feature information is retrieved; otherwise false.</returns>
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

        ///<exclude />
        public override bool Equals(object obj)
        {
            if (!(obj is InputDevice))
                return false;

            return Equals((InputDevice)obj);
        }

        ///<exclude />
        public bool Equals(InputDevice other)
        {
            return deviceId == other.deviceId;
        }

        ///<exclude />
        public override int GetHashCode()
        {
            return deviceId.GetHashCode();
        }

        ///<exclude />
        public static bool operator==(InputDevice a, InputDevice b)
        {
            return a.Equals(b);
        }

        ///<exclude />
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


    ///<summary>Enumeration describing the AR rendering mode used with <see cref="XR.Hand" />.</summary>
    public enum HandFinger
    {
        ///<summary>Thumb finger on a hand.</summary>
        Thumb,
        ///<summary>Index finger on a hand.</summary>
        Index,
        ///<summary>Middle finger on a hand.</summary>
        Middle,
        ///<summary>Ring finger on a hand.</summary>
        Ring,
        ///<summary>Pinky finger on a hand.</summary>
        Pinky
    }

    ///<summary>A tracked hand on the device at an <see cref="XR.XRNode" /> in the XR input subsystem.</summary>
    ///<remarks>The Hand type represents a body element hierarchy corresponding to a human hand. It is comprised of <see cref="XR.Bone" /> type elements.</remarks>
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

        ///<summary>Gets the root bone for this hand.</summary>
        ///<param name="boneOut">A Bone struct to receive the root bone.</param>
        ///<returns>true if hand can be queried for the root bone; otherwise false.</returns>
        public bool TryGetRootBone(out Bone boneOut)
        {
            return Hand_TryGetRootBone(this, out boneOut);
        }

        private static extern bool Hand_TryGetRootBone(Hand hand, out Bone boneOut);

        ///<summary>Gets a list of the finger bones for a finger on this hand.</summary>
        ///<param name="finger">HandFinger enum value for this finger.</param>
        ///<param name="bonesOut">A list of bones that will be filled out for this finger.</param>
        ///<returns>true if hand can be queried for this finger; otherwise false.</returns>
        public bool TryGetFingerBones(HandFinger finger, List<Bone> bonesOut)
        {
            if (bonesOut == null)
                throw new ArgumentNullException("bonesOut");

            return Hand_TryGetFingerBonesAsList(this, finger, bonesOut);
        }

        private static extern bool Hand_TryGetFingerBonesAsList(Hand hand, HandFinger finger, [NotNull] List<Bone> bonesOut);

        ///<exclude />
        public override bool Equals(object obj)
        {
            if (!(obj is Hand))
                return false;

            return Equals((Hand)obj);
        }

        ///<exclude />
        public bool Equals(Hand other)
        {
            return deviceId == other.deviceId &&
                featureIndex == other.featureIndex;
        }

        ///<exclude />
        public override int GetHashCode()
        {
            return deviceId.GetHashCode() ^ (featureIndex.GetHashCode() << 1);
        }

        ///<exclude />
        public static bool operator==(Hand a, Hand b)
        {
            return a.Equals(b);
        }

        ///<exclude />
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

    ///<summary>Contains eye tracking data from the device at an <see cref="XR.XRNode" /> in the XR input subsystem.</summary>
    ///<remarks>Represents eye tracking data collected by the device. The Eyes type contains eye position, rotation, and data indicating the eye fixation point and blink values for both the left and right eye.  All eye spatial information is in the Unity coordinate space.</remarks>
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

        ///<summary>Gets the Vector3 that describes the position of the left eye.</summary>
        ///<returns>true if eyes can be queried for the left eye position; otherwise false.</returns>
        public bool TryGetLeftEyePosition(out Vector3 position)
        {
            return Eyes_TryGetEyePosition(this, EyeSide.Left, out position);
        }

        ///<summary>Gets the Vector3 that describes the position of the right eye.</summary>
        ///<returns>true if eyes can be queried for the right eye position; otherwise false.</returns>
        public bool TryGetRightEyePosition(out Vector3 position)
        {
            return Eyes_TryGetEyePosition(this, EyeSide.Right, out position);
        }

        ///<summary>Gets the Quaternion that describes the rotation of the left eye.</summary>
        ///<returns>true if eyes can be queried for the left eye rotation; otherwise false.</returns>
        public bool TryGetLeftEyeRotation(out Quaternion rotation)
        {
            return Eyes_TryGetEyeRotation(this, EyeSide.Left, out rotation);
        }

        ///<summary>Gets the Quaternion that describes the rotation of the right eye.</summary>
        ///<returns>true if eyes can be queried for the right eye rotation; otherwise false.</returns>
        public bool TryGetRightEyeRotation(out Quaternion rotation)
        {
            return Eyes_TryGetEyeRotation(this, EyeSide.Right, out rotation);
        }

        private static extern bool Eyes_TryGetEyePosition(Eyes eyes, EyeSide chirality, out Vector3 position);
        private static extern bool Eyes_TryGetEyeRotation(Eyes eyes, EyeSide chirality, out Quaternion rotation);

        ///<summary>Gets the point represents the convergence of the line of sight for both eyes.</summary>
        ///<param name="fixationPoint">A Vector3 struct that is filled in with the fixation position.</param>
        ///<returns>true if eyes can be queried for the fixation point; otherwise false.</returns>
        public bool TryGetFixationPoint(out Vector3 fixationPoint)
        {
            return Eyes_TryGetFixationPoint(this, out fixationPoint);
        }

        private static extern bool Eyes_TryGetFixationPoint(Eyes eyes, out Vector3 fixationPoint);

        ///<summary>Gets a value that represents the how far the left eye is open.</summary>
        ///<returns>true if eyes can be queried for the amount that the left eye is open; otherwise false.</returns>
        public bool TryGetLeftEyeOpenAmount(out float openAmount)
        {
            return Eyes_TryGetEyeOpenAmount(this, EyeSide.Left, out openAmount);
        }

        ///<summary>Gets a value that represents the how far the right eye is open.</summary>
        ///<returns>true if eyes can be queried for the amount that the right eye is open; otherwise false.</returns>
        public bool TryGetRightEyeOpenAmount(out float openAmount)
        {
            return Eyes_TryGetEyeOpenAmount(this, EyeSide.Right, out openAmount);
        }

        private static extern bool Eyes_TryGetEyeOpenAmount(Eyes eyes, EyeSide chirality, out float openAmount);

        ///<exclude />
        public override bool Equals(object obj)
        {
            if (!(obj is Eyes))
                return false;

            return Equals((Eyes)obj);
        }

        ///<exclude />
        public bool Equals(Eyes other)
        {
            return deviceId == other.deviceId &&
                featureIndex == other.featureIndex;
        }

        ///<exclude />
        public override int GetHashCode()
        {
            return deviceId.GetHashCode() ^ (featureIndex.GetHashCode() << 1);
        }

        ///<exclude />
        public static bool operator==(Eyes a, Eyes b)
        {
            return a.Equals(b);
        }

        ///<exclude />
        public static bool operator!=(Eyes a, Eyes b)
        {
            return !(a == b);
        }
    }

    ///<summary>A tracked bone on the device at an <see cref="XR.XRNode" /> in the XR input subsystem.</summary>
    ///<remarks>The Bone type is a general purpose structure that represents a part of a specific body element hierarchy. such as Hand, Eyes, or Skeleton.</remarks>
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

        ///<summary>Gets the world position of the bone</summary>
        ///<param name="position">Vector3 to receive the position of the bone in Unity world space.</param>
        ///<returns>true if the rotation was retrieved, false otherwise.</returns>
        public bool TryGetPosition(out Vector3 position) { return Bone_TryGetPosition(this, out position); }
        private static extern bool Bone_TryGetPosition(Bone bone, out Vector3 position);

        ///<summary>Gets the world rotation of the bone.</summary>
        ///<param name="rotation">Quaternion to receive the rotation of the bone in Unity world space.</param>
        ///<returns>true if the rotation was retrieved, false otherwise.</returns>
        public bool TryGetRotation(out Quaternion rotation) { return Bone_TryGetRotation(this, out rotation); }
        private static extern bool Bone_TryGetRotation(Bone bone, out Quaternion rotation);

        ///<summary>Gets the parent of this bone.</summary>
        ///<param name="parentBone">Bone struct that receives the parent bone of this bone.</param>
        ///<returns>true if the rotation was retrieved, false otherwise.</returns>
        public bool TryGetParentBone(out Bone parentBone) { return Bone_TryGetParentBone(this, out parentBone); }
        private static extern bool Bone_TryGetParentBone(Bone bone, out Bone parentBone);

        ///<summary>Get the child bones of this bone.</summary>
        ///<param name="childBones">A list of bones that will be filled out with the children bones of this bone.</param>
        ///<returns>true if bone can be queried for child bones; otherwise false.</returns>
        public bool TryGetChildBones(List<Bone> childBones) { return Bone_TryGetChildBones(this, childBones); }
        private static extern bool Bone_TryGetChildBones(Bone bone, [NotNull] List<Bone> childBones);

        ///<exclude />
        public override bool Equals(object obj)
        {
            if (!(obj is Bone))
                return false;

            return Equals((Bone)obj);
        }

        ///<exclude />
        public bool Equals(Bone other)
        {
            return deviceId == other.deviceId &&
                featureIndex == other.featureIndex;
        }

        ///<exclude />
        public override int GetHashCode()
        {
            return deviceId.GetHashCode() ^
                (featureIndex.GetHashCode() << 1);
        }

        ///<exclude />
        public static bool operator==(Bone a, Bone b)
        {
            return a.Equals(b);
        }

        ///<exclude />
        public static bool operator!=(Bone a, Bone b)
        {
            return !(a == b);
        }
    }


    ///<summary>An interface for accessing devices in the XR input subsytem.</summary>
    ///<remarks>To route haptic feedback to XR input devices, specify an <see cref="XR.XRNode" /> as the destination. This interface provides access to input devices using an XRNode. For example, use the use <see cref="XR.XRNode.LeftHand" /> and <see cref="XR.XRNode.RightHand" /> to access the left or right devices.</remarks>
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeConditional("ENABLE_VR")]
    [NativeHeader("Modules/XR/Subsystems/Input/Public/XRInputDevices.h")]
    [StaticAccessor("XRInputDevices::Get()", StaticAccessorType.Dot)]
    public partial class InputDevices
    {
        ///<summary>Gets the input device at a given <see cref="XR.XRNode" /> endpoint.</summary>
        ///<remarks>If there is no device at the specified endpoint, the method returns an InputDevice on which a call to InputDevice.IsValid returns false.</remarks>
        ///<param name="node">The XRNode that owns the requested device.</param>
        ///<returns>An <see cref="XR.InputDevice" /> at this [[XR.XRNode].</returns>
        public static InputDevice GetDeviceAtXRNode(XRNode node)
        {
            UInt64 deviceId = InputTracking.GetDeviceIdAtXRNode(node);
            return new InputDevice(deviceId);
        }

        ///<summary>Gets a list of active input devices available to the XR Input Subsystem at a given <see cref="XR.XRNode" /> endpoint.</summary>
        ///<param name="node">The XRNode that owns the requested device.</param>
        ///<param name="inputDevices">A List of type InputDevices to receive the available input devices.</param>
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

        ///<summary>Gets a list of active input devices available to the XR Input Subsystem.</summary>
        ///<param name="inputDevices">A List of type InputDevices to receive the available input devices.</param>
        public static void GetDevices(List<InputDevice> inputDevices)
        {
            if (null == inputDevices)
                throw new ArgumentNullException("inputDevices");

            inputDevices.Clear();
            GetDevices_Internal(inputDevices);
        }

        ///<summary>Gets a list of active input devices available to the XR Input Subsystem that match the specified role.</summary>
        ///<param name="role">
        ///  <see cref="XR.InputDeviceRole" /> that is defined for the devices returned.</param>
        ///<param name="inputDevices">A List of type InputDevices to receive the available input devices.</param>
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
        [AutoStaticsCleanupOnCodeReload] // re-created lazily after reload
        static List<InputDevice> s_InputDeviceList;
        ///<summary>Gets the list of active XR input devices that match the specified <see cref="InputDeviceCharacteristics" />.</summary>
        ///<remarks>This function finds any input devices available to the XR Subsystem that match the specified <see cref="InputDeviceCharacteristics" /> bitmask exactly and inserts them into the <c>inputDevices</c> list. The function does not include devices that only provide some of the desired characteristics or capabilities.
        ///
        ///The inputDevices list is cleared before any new elements are added.
        ///
        ///The characteristics are a bitmask, and so you can use the | operator in order to search for multiple characteristics at once.</remarks>
        ///<param name="desiredCharacteristics">A bitwise combination of the characteristics you are looking for.</param>
        ///<param name="inputDevices">A List&lt;InputDevice&gt; object to receive the available input devices.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.XR;
        ///using System.Collections.Generic;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        InputDeviceCharacteristics leftTrackedControllerFilter = InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Left, leftHandedControllers;
        ///
        ///        List<InputDevice> foundControllers = new List<InputDevice>();
        ///        InputDevices.GetDevicesWithCharacteristics(leftTrackedControllerFilter, foundControllers);
        ///    }
        ///}
        ///]]></code>
        ///</example>
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

        ///<summary>Defines the delegate to use to register events when an <see cref="InputDevice" /> is connected.</summary>
        ///<remarks>This delegate allows you to receive device connection events, so you know when the list of devices changes.</remarks>
        [AutoStaticsCleanupOnCodeReload]
        public static event Action<InputDevice> deviceConnected;
        ///<summary>Defines the delegate to use to register events when an <see cref="InputDevice" /> is disconnected.</summary>
        ///<remarks>This delegate allows you to receive device disconnection events, so you know when the list of devices changes.
        ///
        ///**Note**: <see cref="InputDevice.isValid" /> will be false for the passed-in device, and the only device data available will be <see cref="InputDevice.name" />, <see cref="InputDevice.role" />, and comparison against other InputDevice objects.</remarks>
        [AutoStaticsCleanupOnCodeReload]
        public static event Action<InputDevice> deviceDisconnected;
        ///<summary>Defines the delegate to use to register events when an <see cref="InputDevice" />'s configuration changes.</summary>
        [AutoStaticsCleanupOnCodeReload]
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

        internal static extern bool TryGetFeatureUsages(UInt64 deviceId, [NotNull][Out] List<InputFeatureUsage> featureUsages);

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
        ///<summary>**Note**: This API has been marked as obsolete in code, and is no longer in use. Please use <see cref="InputTracking.GetNodeStates" /> and look for the <see cref="XRNodeState" /> with the corresponding <see cref="XRNode" /> type instead.
        ///Gets the position of a specific node.</summary>
        ///<remarks>This can be used to keep objects at the same position as the given node. For example, if the user picks up an object, you can use this method along with <see cref="InputTracking.GetLocalRotation" /> to ensure the object is correctly positioned and oriented to match the user's hand.
        ///
        ///**Note:** This function doesn't work with the following XRNode types: GameController, TrackingReference, or HardwareTracker. Use the <see cref="InputTracking.GetNodeStates" /> method instead. See <see cref="XR.XRNode" /> for more details.</remarks>
        ///<param name="node">Specifies which node's position should be returned.</param>
        ///<returns>The position of the node in its local tracking space.</returns>
        [NativeConditional("ENABLE_VR", "Vector3f::zero")]
        [Obsolete("This API is obsolete, and should no longer be used. Please use InputDevice.TryGetFeatureValue with the CommonUsages.devicePosition usage instead.")]
        extern public static Vector3 GetLocalPosition(XRNode node);

        ///<summary>**Note**: This API has been marked as obsolete in code, and is no longer in use. Please use <see cref="InputTracking.GetNodeStates" /> and look for the <see cref="XRNodeState" /> with the corresponding <see cref="XRNode" /> type instead.
        ///Gets the rotation of a specific node.</summary>
        ///<remarks>This can be used to keep objects at the same orientation as the given node. For example, if the user picks up an object you can use this method along with <see cref="InputTracking.GetLocalPosition" /> to ensure the object is correctly positioned and oriented to match the user's hand.
        ///
        ///**Note:** This function doesn't work with the following XRNode types: GameController, TrackingReference, or HardwareTracker. Use the <see cref="InputTracking.GetNodeStates" /> method instead. See <see cref="XR.XRNode" /> for more details.</remarks>
        ///<param name="node">Specifies which node's rotation should be returned.</param>
        ///<returns>The rotation of the node in its local tracking space.</returns>
        [NativeConditional("ENABLE_VR", "Quaternionf::identity()")]
        [Obsolete("This API is obsolete, and should no longer be used. Please use InputDevice.TryGetFeatureValue with the CommonUsages.deviceRotation usage instead.")]
        extern public static Quaternion GetLocalRotation(XRNode node);

        ///<summary>Center tracking to the current position and orientation of the HMD.</summary>
        ///<remarks>This only works with seated and standing experiences. Room scale experiences are not affected by Recenter.</remarks>
        [NativeConditional("ENABLE_VR")]
        [Obsolete("This API is obsolete, and should no longer be used. Please use XRInputSubsystem.TryRecenter() instead.")]
        extern public static void Recenter();

        [NativeConditional("ENABLE_VR")]
        [Obsolete("This API is obsolete, and should no longer be used. Please use InputDevice.name with the device associated with that tracking data instead.")]
        extern public static string GetNodeName(ulong uniqueId);

        ///<summary>Describes all currently connected XRNodes and provides available tracking states for each.</summary>
        ///<remarks>Use this method to acquire all the currently available XR input Nodes, as an alternative to handling the node events <see cref="InputTracking.nodeAdded" /> and <see cref="InputTracking.nodeRemoved" />. The contents of <c>nodeStates</c> list will be cleared and replaced with fresh data.
        ///
        ///Not all XR platforms provide complete tracking data. Use the methods <see cref="XR.XRNodeState.TryGetPosition" />, <see cref="XR.XRNodeState.TryGetRotation" />, etc. to read the data if it's available.</remarks>
        ///<param name="nodeStates">A list that is populated with <see cref="XR.XRNodeState" /> objects.</param>
        public static void GetNodeStates(List<XRNodeState> nodeStates)
        {
            if (null == nodeStates)
                throw new ArgumentNullException("nodeStates");

            nodeStates.Clear();
            GetNodeStates_Internal(nodeStates);
        }

        [NativeConditional("ENABLE_VR")]
        extern private static void GetNodeStates_Internal([NotNull] List<XRNodeState> nodeStates);

        ///<summary>Disables positional tracking in XR. This takes effect the next time the head pose is sampled.  If set to true the camera only tracks headset rotation state.</summary>
        ///<remarks>This will disable the neck model in seated XR experiences.  The only positional component remaining is the space between the eyes.
        ///
        ///This functionality is most useful for 360 video use case where you don't want to allow the head to translate at all.</remarks>
        [NativeConditional("ENABLE_VR")]
        [Obsolete("This API is obsolete, and should no longer be used. Please use the TrackedPoseDriver in the Legacy Input Helpers package for controlling a camera in XR.")]
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
