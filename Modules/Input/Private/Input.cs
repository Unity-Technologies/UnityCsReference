// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting;


namespace UnityEngineInternal.Input
{
    using NativeBeforeUpdateCallback = System.Action<NativeInputUpdateType>;
    using NativeUpdateCallback = System.Action<NativeInputUpdateType, int, IntPtr>;
    using NativeDeviceDiscoveredCallback = System.Action<int, string>;

    // C# doesn't support multi-character literals, so we do it by hand here...
    public enum NativeInputEventType
    {
        DeviceRemoved = 0x4452454D,
        DeviceConfigChanged = 0x44434647,

        Text = 0x54455854,
        State = 0x53544154,
        Delta = 0x444C5441,
    }

    [StructLayout(LayoutKind.Explicit, Size = 20)]
    public struct NativeInputEvent
    {
        [FieldOffset(0)] public NativeInputEventType type;
        [FieldOffset(4)] public ushort sizeInBytes;
        [FieldOffset(6)] public ushort deviceId;
        [FieldOffset(8)] public int eventId;
        [FieldOffset(12)] public double time; // Time on GetTimeSinceStartup() timeline in seconds. *NOT* on Time.time timeline.

        public NativeInputEvent(NativeInputEventType type, int sizeInBytes, int deviceId, double time)
        {
            this.type = type;
            this.sizeInBytes = (ushort)sizeInBytes;
            this.deviceId = (ushort)deviceId;
            this.eventId = 0;
            this.time = time;
        }
    }

    [Flags]
    public enum NativeInputUpdateType
    {
        Dynamic = 1 << 0,
        Fixed = 1 << 1,
        BeforeRender = 1 << 2,
        Editor = 1 << 3,
        IgnoreFocus = 1 << 31,
    }


    ////REVIEW: have a notification where a device can tell the HLAPI that its configuration has changed? (like e.g. the surface of a pointer has changed dimensions)
    public partial class NativeInputSystem
    {
        public static NativeUpdateCallback onUpdate;
        public static NativeBeforeUpdateCallback onBeforeUpdate;

        static NativeDeviceDiscoveredCallback s_OnDeviceDiscoveredCallback;
        public static NativeDeviceDiscoveredCallback onDeviceDiscovered
        {
            get { return s_OnDeviceDiscoveredCallback; }
            set
            {
                s_OnDeviceDiscoveredCallback = value;
                hasDeviceDiscoveredCallback = s_OnDeviceDiscoveredCallback != null;
            }
        }

        static NativeInputSystem()
        {
            // This property is backed by a native field, and so it's state is preserved over domain reload.
            // Reset it on initialization to keep it current.
            hasDeviceDiscoveredCallback = false;
        }

        [RequiredByNativeCode]
        internal static void NotifyBeforeUpdate(NativeInputUpdateType updateType)
        {
            NativeBeforeUpdateCallback callback = onBeforeUpdate;
            if (callback != null)
                callback(updateType);
        }

        [RequiredByNativeCode]
        internal static void NotifyUpdate(NativeInputUpdateType updateType, int eventCount, IntPtr eventData)
        {
            NativeUpdateCallback callback = onUpdate;
            if (callback != null)
                callback(updateType, eventCount, eventData);
        }

        [RequiredByNativeCode]
        internal static void NotifyDeviceDiscovered(int deviceId, string deviceDescriptor)
        {
            NativeDeviceDiscoveredCallback callback = s_OnDeviceDiscoveredCallback;
            if (callback != null)
                callback(deviceId, deviceDescriptor);
        }
    }
}
