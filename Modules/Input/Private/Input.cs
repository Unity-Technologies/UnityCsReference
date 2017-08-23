// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting;


namespace UnityEngineInternal.Input
{
    // C# doesn't support multi-character literals, so we do it by hand here...
    public enum NativeInputEventType
    {
        DeviceConnected = 0x44434F4E,
        DeviceDisconnected = 0x44444953,

        Generic = 0x47454E52,

        KeyDown = 0x4B455944,
        KeyUp = 0x4B455955,

        PointerDown = 0x50545244,
        PointerMove = 0x5054524D,
        PointerUp = 0x50545255,
        PointerCancelled = 0x50545243,

        Click = 0x434C494B,

        Text = 0x54455854,
        Tracking = 0x5452434B,
    }

    [StructLayout(LayoutKind.Explicit, Size = 20)]
    public struct NativeInputEvent
    {
        [FieldOffset(0)] public NativeInputEventType type;
        [FieldOffset(4)] public int sizeInBytes;
        [FieldOffset(8)] public int deviceId;
        [FieldOffset(12)] public double time; // Time on GetTimeSinceStartup() timeline in seconds. *NOT* on Time.time timeline.

        public NativeInputEvent(NativeInputEventType type, int sizeInBytes, int deviceId, double time)
        {
            this.type = type;
            this.sizeInBytes = sizeInBytes;
            this.deviceId = deviceId;
            this.time = time;
        }
    }

    // Generic.
    [StructLayout(LayoutKind.Explicit, Size = 36)]
    public struct NativeGenericEvent
    {
        [FieldOffset(0)] public NativeInputEvent baseEvent;
        [FieldOffset(20)] public int controlIndex;
        [FieldOffset(24)] public int rawValue;
        [FieldOffset(28)] public double scaledValue;

        public static NativeGenericEvent Value(int deviceId, double time, int controlIndex, int rawValue, double scaledValue)
        {
            NativeGenericEvent nativeEvent;
            nativeEvent.baseEvent = new NativeInputEvent(NativeInputEventType.Generic, 36, deviceId, time);
            nativeEvent.controlIndex = controlIndex;
            nativeEvent.rawValue = rawValue;
            nativeEvent.scaledValue = scaledValue;
            return nativeEvent;
        }
    }

    // KeyDown, KeyUp, KeyRepeat.
    [StructLayout(LayoutKind.Explicit, Size = 24)]
    public struct NativeKeyEvent
    {
        [FieldOffset(0)] public NativeInputEvent baseEvent;
        [FieldOffset(20)] public KeyCode key; // This is the raw key without any translation from keyboard layouts.

        public static NativeKeyEvent Down(int deviceId, double time, KeyCode key)
        {
            NativeKeyEvent nativeEvent;
            nativeEvent.baseEvent = new NativeInputEvent(NativeInputEventType.KeyDown, 24, deviceId, time);
            nativeEvent.key = key;
            return nativeEvent;
        }

        public static NativeKeyEvent Up(int deviceId, double time, KeyCode key)
        {
            NativeKeyEvent nativeEvent;
            nativeEvent.baseEvent = new NativeInputEvent(NativeInputEventType.KeyUp, 24, deviceId, time);
            nativeEvent.key = key;
            return nativeEvent;
        }
    }

    // PointerDown, PointerMove, PointerUp.
    [StructLayout(LayoutKind.Explicit, Size = 80)]
    public struct NativePointerEvent
    {
        [FieldOffset(0)] public NativeInputEvent baseEvent;
        [FieldOffset(20)] public int pointerId;
        [FieldOffset(24)] public Vector3 position;
        [FieldOffset(36)] public Vector3 delta;
        [FieldOffset(48)] public float pressure;
        [FieldOffset(52)] public float twist;
        [FieldOffset(56)] public Vector2 tilt;
        [FieldOffset(64)] public Vector3 radius;
        [FieldOffset(76)] public int displayIndex;

        public static NativePointerEvent Down(int deviceId, double time, int pointerId, Vector3 position, Vector3 delta = new Vector3(), float pressure = 1.0f, float twist = 1.0f, Vector2 tilt = new Vector2(), Vector3 radius = new Vector3(), int displayIndex = 0)
        {
            NativePointerEvent nativeEvent;
            nativeEvent.baseEvent = new NativeInputEvent(NativeInputEventType.PointerDown, 80, deviceId, time);
            nativeEvent.pointerId = pointerId;
            nativeEvent.position = position;
            nativeEvent.delta = delta;
            nativeEvent.pressure = pressure;
            nativeEvent.twist = twist;
            nativeEvent.tilt = tilt;
            nativeEvent.radius = radius;
            nativeEvent.displayIndex = displayIndex;
            return nativeEvent;
        }

        public static NativePointerEvent Move(int deviceId, double time, int pointerId, Vector3 position, Vector3 delta = new Vector3(), float pressure = 1.0f, float twist = 1.0f, Vector2 tilt = new Vector2(), Vector3 radius = new Vector3(), int displayIndex = 0)
        {
            NativePointerEvent nativeEvent;
            nativeEvent.baseEvent = new NativeInputEvent(NativeInputEventType.PointerMove, 80, deviceId, time);
            nativeEvent.pointerId = pointerId;
            nativeEvent.position = position;
            nativeEvent.delta = delta;
            nativeEvent.pressure = pressure;
            nativeEvent.twist = twist;
            nativeEvent.tilt = tilt;
            nativeEvent.radius = radius;
            nativeEvent.displayIndex = displayIndex;
            return nativeEvent;
        }

        public static NativePointerEvent Up(int deviceId, double time, int pointerId, Vector3 position, Vector3 delta = new Vector3(), float pressure = 1.0f, float twist = 1.0f, Vector2 tilt = new Vector2(), Vector3 radius = new Vector3(), int displayIndex = 0)
        {
            NativePointerEvent nativeEvent;
            nativeEvent.baseEvent = new NativeInputEvent(NativeInputEventType.PointerUp, 80, deviceId, time);
            nativeEvent.pointerId = pointerId;
            nativeEvent.position = position;
            nativeEvent.delta = delta;
            nativeEvent.pressure = pressure;
            nativeEvent.twist = twist;
            nativeEvent.tilt = tilt;
            nativeEvent.radius = radius;
            nativeEvent.displayIndex = displayIndex;
            return nativeEvent;
        }

        public static NativePointerEvent Cancelled(int deviceId, double time, int pointerId, Vector3 position, Vector3 delta = new Vector3(), float pressure = 1.0f, float twist = 1.0f, Vector2 tilt = new Vector2(), Vector3 radius = new Vector3(), int displayIndex = 0)
        {
            NativePointerEvent nativeEvent;
            nativeEvent.baseEvent = new NativeInputEvent(NativeInputEventType.PointerCancelled, 80, deviceId, time);
            nativeEvent.pointerId = pointerId;
            nativeEvent.position = position;
            nativeEvent.delta = delta;
            nativeEvent.pressure = pressure;
            nativeEvent.twist = twist;
            nativeEvent.tilt = tilt;
            nativeEvent.radius = radius;
            nativeEvent.displayIndex = displayIndex;
            return nativeEvent;
        }
    }

    // Click Events
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public struct NativeClickEvent
    {
        [FieldOffset(0)] public NativeInputEvent baseEvent;
        [FieldOffset(20)] public bool isPressed;////TODO: remove this and replace with ClickPress and ClickRelease type
        [FieldOffset(24)] public int controlIndex;
        [FieldOffset(28)] public int clickCount;

        public static NativeClickEvent Press(int deviceId, double time, int controlIndex, int clickCount)
        {
            NativeClickEvent nativeEvent;
            nativeEvent.baseEvent = new NativeInputEvent(NativeInputEventType.Click, 32, deviceId, time);
            nativeEvent.isPressed = true;
            nativeEvent.controlIndex = controlIndex;
            nativeEvent.clickCount = clickCount;
            return nativeEvent;
        }

        public static NativeClickEvent Release(int deviceId, double time, int controlIndex, int clickCount)
        {
            NativeClickEvent nativeEvent;
            nativeEvent.baseEvent = new NativeInputEvent(NativeInputEventType.Click, 32, deviceId, time);
            nativeEvent.isPressed = false;
            nativeEvent.controlIndex = controlIndex;
            nativeEvent.clickCount = clickCount;
            return nativeEvent;
        }
    }

    // Text.
    [StructLayout(LayoutKind.Explicit, Size = 24)]
    public struct NativeTextEvent
    {
        [FieldOffset(0)] public NativeInputEvent baseEvent;
        [FieldOffset(20)] public int utf32Character;

        public static NativeTextEvent Character(int deviceId, double time, int utf32)
        {
            NativeTextEvent nativeEvent;
            nativeEvent.baseEvent = new NativeInputEvent(NativeInputEventType.Text, 24, deviceId, time);
            nativeEvent.utf32Character = utf32;
            return nativeEvent;
        }
    }

    // Tracking.
    [StructLayout(LayoutKind.Explicit, Size = 104)]
    public struct NativeTrackingEvent
    {
        [Flags]
        public enum Flags : uint
        {
            PositionAvailable = 1 << 0,
            OrientationAvailable = 1 << 1,
            VelocityAvailable = 1 << 2,
            AngularVelocityAvailable = 1 << 3,
            AccelerationAvailable = 1 << 4,
            AngularAccelerationAvailable = 1 << 5,
        }

        [FieldOffset(0)] public NativeInputEvent baseEvent;
        [FieldOffset(20)] public int nodeId;
        [FieldOffset(24)] public Flags availableFields;
        [FieldOffset(28)] public Vector3 localPosition;
        [FieldOffset(40)] public Quaternion localRotation;
        [FieldOffset(56)] public Vector3 velocity;
        [FieldOffset(68)] public Vector3 angularVelocity;
        [FieldOffset(80)] public Vector3 acceleration;
        [FieldOffset(92)] public Vector3 angularAcceleration;

        public static NativeTrackingEvent Create(int deviceId, double time, int nodeId, Vector3 position, Quaternion rotation)
        {
            NativeTrackingEvent nativeEvent = new NativeTrackingEvent(); // Necessary because we don't initialize all fields.
            nativeEvent.baseEvent = new NativeInputEvent(NativeInputEventType.Tracking, 104, deviceId, time);
            nativeEvent.nodeId = nodeId;
            nativeEvent.availableFields = (Flags.PositionAvailable | Flags.OrientationAvailable);
            nativeEvent.localPosition = position;
            nativeEvent.localRotation = rotation;
            return nativeEvent;
        }
    }

    // Keep in sync with InputDeviceInfo in InputDeviceData.h.
    [Serializable]
    public struct NativeInputDeviceInfo
    {
        public int deviceId;
        public string deviceDescriptor;
    }

    public enum NativeInputUpdateType
    {
        BeginFixed = 0,
        EndFixed = 1,
        BeginDynamic = 2,
        EndDynamic = 3,
        BeginBeforeRender = 4,
        EndBeforeRender = 5,
        BeginEditor = 6,
        EndEditor = 7,
    }

    public delegate void NativeUpdateCallback(NativeInputUpdateType updateType);
    public delegate void NativeEventCallback(int eventCount, IntPtr eventData);
    public delegate void NativeDeviceDiscoveredCallback(NativeInputDeviceInfo deviceInfo);

    ////REVIEW: have a notification where a device can tell the HLAPI that its configuration has changed? (like e.g. the surface of a pointer has changed dimensions)
    public partial class NativeInputSystem
    {
        public static NativeUpdateCallback onUpdate;
        public static NativeEventCallback onEvents;

        static NativeDeviceDiscoveredCallback s_OnDeviceDiscoveredCallback;
        public static event NativeDeviceDiscoveredCallback onDeviceDiscovered
        {
            add
            {
                s_OnDeviceDiscoveredCallback += value;
                hasDeviceDiscoveredCallback = s_OnDeviceDiscoveredCallback != null;
            }
            remove
            {
                s_OnDeviceDiscoveredCallback -= value;
                hasDeviceDiscoveredCallback = s_OnDeviceDiscoveredCallback != null;
            }
        }

        static NativeInputSystem()
        {
            // This property is backed by a native field, and so it's state is preserved over domain reload.  Reset it on initialization to keep it current.
            hasDeviceDiscoveredCallback = false;
        }

        [RequiredByNativeCode]
        internal static void NotifyUpdate(NativeInputUpdateType updateType)
        {
            NativeUpdateCallback callback = onUpdate;
            if (callback != null)
                callback(updateType);
        }

        [RequiredByNativeCode]
        internal static void NotifyEvents(int eventCount, IntPtr eventData)
        {
            NativeEventCallback callback = onEvents;
            if (callback != null)
                callback(eventCount, eventData);
        }

        [RequiredByNativeCode]
        internal static void NotifyDeviceDiscovered(NativeInputDeviceInfo deviceInfo)
        {
            NativeDeviceDiscoveredCallback callback = s_OnDeviceDiscoveredCallback;
            if (callback != null)
                callback(deviceInfo);
        }
    }
}
