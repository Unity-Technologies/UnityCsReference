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
    using NativeDeviceDiscoveredCallback = System.Action<int, string>;

    public unsafe delegate void NativeUpdateCallback(NativeInputUpdateType updateType, NativeInputEventBuffer* buffer);

    // C# doesn't support multi-character literals, so we do it by hand here...
    public enum NativeInputEventType
    {
        DeviceRemoved = 0x4452454D,
        DeviceConfigChanged = 0x44434647,

        Text = 0x54455854,
        State = 0x53544154,
        Delta = 0x444C5441,
    }

    // We pass this as a struct to make it less painful to change the OnUpdate() API if need be.
    [StructLayout(LayoutKind.Explicit, Size = 20, Pack = 1)]
    public unsafe struct NativeInputEventBuffer
    {
        // NOTE: Keep this as the first field in the struct. This avoids alignment/packing issues
        //       on the C++ side due to the compiler wanting to align the 64bit pointer.
        [FieldOffset(0)] public void* eventBuffer;
        [FieldOffset(8)] public int eventCount;
        [FieldOffset(12)] public int sizeInBytes;
        [FieldOffset(16)] public int capacityInBytes;
    }

    [StructLayout(LayoutKind.Explicit, Size = 20, Pack = 1)]
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
        internal static unsafe void NotifyUpdate(NativeInputUpdateType updateType, IntPtr eventBuffer)
        {
            NativeUpdateCallback callback = onUpdate;
            var eventBufferPtr = (NativeInputEventBuffer*)eventBuffer.ToPointer();
            if (callback == null)
            {
                eventBufferPtr->eventCount = 0;
                eventBufferPtr->sizeInBytes = 0;
            }
            else
            {
                callback(updateType, eventBufferPtr);
            }
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
