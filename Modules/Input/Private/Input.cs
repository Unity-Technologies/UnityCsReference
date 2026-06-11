// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Scripting;
using Unity.Scripting.LifecycleManagement;

using NativeBeforeUpdateCallback = System.Action<UnityEngineInternal.Input.NativeInputUpdateType>;
using NativeDeviceDiscoveredCallback = System.Action<int, string>;
using NativeShouldRunUpdateCallback = System.Func<UnityEngineInternal.Input.NativeInputUpdateType, bool>;

namespace UnityEngineInternal.Input
{
    internal unsafe delegate void NativeUpdateCallback(NativeInputUpdateType updateType, NativeInputEventBuffer* buffer);

    // C# doesn't support multi-character literals, so we do it by hand here...
    internal enum NativeInputEventType
    {
        DeviceRemoved = 0x4452454D,
        DeviceConfigChanged = 0x44434647,

        Text = 0x54455854,
        State = 0x53544154,
        Delta = 0x444C5441,
        Focus = 0x464f4355,
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
        /// <summary>
        /// Defines the packing alignment used when inserting <code>NativeInputEvent</code>
        /// data into a <see cref="NativeInputEventBuffer"/>.
        /// </summary>
        /// <remarks>
        /// Currently this constant is 4 (this may be subject to change), which currently
        /// implies that <see cref="NativeInputEvent.time"/> will not be aligned on an 8-byte
        /// boundary (natural alignment) if <code>NativeInputEvent</code> is referenced from
        /// within a <code>NativeInputEventBuffer</code> via unsafe code.
        /// </remarks>
        public const int alignment = 4;

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
        [AutoStaticsCleanupOnCodeReload]
        public static NativeUpdateCallback onUpdate;
        [AutoStaticsCleanupOnCodeReload]
        public static NativeBeforeUpdateCallback onBeforeUpdate;
        [AutoStaticsCleanupOnCodeReload]
        public static NativeShouldRunUpdateCallback onShouldRunUpdate;
        #pragma warning restore 649

        [AutoStaticsCleanupOnCodeReload]
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

        [OnCodeLoaded]
        static void Clear()
        {
            // This property is backed by a native field, and so its state is preserved over domain reload.
            // Reset it on initialization to keep it current.
            hasDeviceDiscoveredCallback = false;
        }

        [RequiredByNativeCode]
        internal static void NotifyDeviceDiscovered(int deviceId, string deviceDescriptor)
        {
            NativeDeviceDiscoveredCallback callback = s_OnDeviceDiscoveredCallback;
            if (callback != null)
                callback(deviceId, deviceDescriptor);
        }

        // Per-tick gate + before-update dispatch, called from native before acuiring event
        // buffer scope.
        // Checks onShouldRunUpdate first (back-compat gate), then fires onBeforeUpdate.
        // Returns true to continue with the update (snapshot + onUpdate), false to skip.
        // Note that onShouldRunUpdate gate may be transitioned to SetActiveUpdateMask in
        // the future, which gates earlier still (at the PlayerLoop hook in native).
        [RequiredByNativeCode]
        internal static bool NotifyBeforeUpdate(NativeInputUpdateType updateType)
        {
            NativeShouldRunUpdateCallback should = onShouldRunUpdate;
            if (should != null && !should(updateType))
                return false;

            NativeBeforeUpdateCallback beforeCallback = onBeforeUpdate;
            if (beforeCallback != null)
                beforeCallback(updateType);

            return true;
        }

        // Per-tick onUpdate dispatch, called from native after event buffer is acquired.
        // Zeroes buffer when no onUpdate consumer is registered so events don't accumulate.
        [RequiredByNativeCode]
        internal static unsafe void ProcessInputUpdate(NativeInputUpdateType updateType, IntPtr eventBuffer)
        {
            var eventBufferPtr = (NativeInputEventBuffer*)eventBuffer.ToPointer();

            NativeUpdateCallback updateCallback = onUpdate;
            if (updateCallback != null)
                updateCallback(updateType, eventBufferPtr);
            else
            {
                eventBufferPtr->eventCount = 0;
                eventBufferPtr->sizeInBytes = 0;
            }
        }

        internal static void DoSendMouseEvents(bool leftButtonPressed, bool wasPressedThisFrame, float posX, float posY)
        {
            SendMouseEvents.SetMouse(leftButtonPressed, wasPressedThisFrame, posX, posY);
            if (useImplicitMouseEventScriptCallbacks)
                SendMouseEvents.DoSendMouseEvents(1);
        }
    }
}
