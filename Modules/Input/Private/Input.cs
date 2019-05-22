// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Scripting;

[assembly: InternalsVisibleTo("Unity.InputSystem")]
namespace UnityEngineInternal.Input
{
    using NativeBeforeUpdateCallback = System.Action<NativeInputUpdateType>;
    using NativeDeviceDiscoveredCallback = System.Action<int, string>;
    using NativeShouldRunUpdateCallback = System.Func<NativeInputUpdateType, bool>;
    internal unsafe delegate void NativeUpdateCallback(NativeInputUpdateType updateType, NativeInputEventBuffer* buffer);

    // C# doesn't support multi-character literals, so we do it by hand here...
    internal enum NativeInputEventType
    {
        DeviceRemoved = 0x4452454D,
        DeviceConfigChanged = 0x44434647,

        Text = 0x54455854,
        State = 0x53544154,
        Delta = 0x444C5441,
    }

    // We pass this as a struct to make it less painful to change the OnUpdate() API if need be.
    [StructLayout(LayoutKind.Explicit, Size = structSize, Pack = 1)]
    internal unsafe struct NativeInputEventBuffer
    {
        public const int structSize = 20;
        // NOTE: Keep this as the first field in the struct. This avoids alignment/packing issues
        //       on the C++ side due to the compiler wanting to align the 64bit pointer.
        [FieldOffset(0)] public void* eventBuffer;
        [FieldOffset(8)] public int eventCount;
        [FieldOffset(12)] public int sizeInBytes;
        [FieldOffset(16)] public int capacityInBytes;
    }

    [StructLayout(LayoutKind.Explicit, Size = structSize, Pack = 1)]
    internal struct NativeInputEvent
    {
        public const int structSize = 20;

        [FieldOffset(0)] public NativeInputEventType type;
        [FieldOffset(4)] public ushort sizeInBytes;
        [FieldOffset(6)] public ushort deviceId;
        [FieldOffset(8)] public double time; // Time on GetTimeSinceStartup() timeline in seconds. *NOT* on Time.time timeline.
        [FieldOffset(16)] public int eventId;

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
    internal enum NativeInputUpdateType
    {
        Dynamic = 1 << 0,
        Fixed = 1 << 1,
        BeforeRender = 1 << 2,
        Editor = 1 << 3,
        IgnoreFocus = 1 << 31,
    }


    internal partial class NativeInputSystem
    {
        // This generates warnings that these are not used. They are used, but only by the input system package
        // (which can access these via InternalsVisibleTo). But since that is not available during unity build time,
        // and since these are marked as internal, we'd get the warning. So we suppress these.
        #pragma warning disable 649
        public static NativeUpdateCallback onUpdate;
        public static NativeBeforeUpdateCallback onBeforeUpdate;
        public static NativeShouldRunUpdateCallback onShouldRunUpdate;
        #pragma warning restore 649

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

        [RequiredByNativeCode]
        internal static void ShouldRunUpdate(NativeInputUpdateType updateType, out bool retval)
        {
            NativeShouldRunUpdateCallback callback = onShouldRunUpdate;
            retval = callback != null ? callback(updateType) : false;
        }
    }
}
