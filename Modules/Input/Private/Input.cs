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
    }

    // Generic.
    [StructLayout(LayoutKind.Explicit, Size = 36)]
    public struct NativeGenericEvent
    {
        [FieldOffset(0)] public NativeInputEvent baseEvent;
        [FieldOffset(20)] public int controlIndex;
        [FieldOffset(24)] public int rawValue;
        [FieldOffset(28)] public double scaledValue;
    }

    // KeyDown, KeyUp, KeyRepeat.
    [StructLayout(LayoutKind.Explicit, Size = 24)]
    public struct NativeKeyEvent
    {
        [FieldOffset(0)] public NativeInputEvent baseEvent;
        [FieldOffset(20)] public KeyCode key; // This is the raw key without any translation from keyboard layouts.
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
    }

    // Click Events
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public struct NativeClickEvent
    {
        [FieldOffset(0)] public NativeInputEvent baseEvent;
        [FieldOffset(20)] public bool isPressed;
        [FieldOffset(24)] public int controlIndex;
        [FieldOffset(28)] public int clickCount;
    }

    // Text.
    [StructLayout(LayoutKind.Explicit, Size = 24)]
    public struct NativeTextEvent
    {
        [FieldOffset(0)] public NativeInputEvent baseEvent;
        [FieldOffset(20)] public int utf32Character;
    }

    // Tracking.
    [StructLayout(LayoutKind.Explicit, Size = 104)]
    public struct NativeTrackingEvent
    {
        [FieldOffset(0)] public NativeInputEvent baseEvent;
        [FieldOffset(20)] public int nodeId;
        [FieldOffset(24)] public uint availableFields;
        [FieldOffset(28)] public Vector3 localPosition;
        [FieldOffset(40)] public Quaternion localRotation;
        [FieldOffset(56)] public Vector3 velocity;
        [FieldOffset(68)] public Vector3 angularVelocity;
        [FieldOffset(80)] public Vector3 acceleration;
        [FieldOffset(92)] public Vector3 angularAcceleration;
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
        public static NativeDeviceDiscoveredCallback onDeviceDiscovered;


        ////TODO: output events
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
        internal static bool HasDeviceDiscoveredHandler()
        {
            return onDeviceDiscovered != null;
        }

        [RequiredByNativeCode]
        internal static void NotifyDeviceDiscovered(NativeInputDeviceInfo deviceInfo)
        {
            NativeDeviceDiscoveredCallback callback = onDeviceDiscovered;
            if (callback != null)
                callback(deviceInfo);
        }
    }
}
