// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngineInternal;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngineInternal.Input
{
    [NativeHeader("Modules/Input/Private/InputModuleBindings.h")]
    [NativeHeader("Modules/Input/Private/InputInternal.h")]
    [NativeConditional("ENABLE_NEW_INPUT_SYSTEM")]
    public partial class NativeInputSystem
    {
        public static extern double zeroEventTime { get; }

        public static extern bool hasDeviceDiscoveredCallback { set; }

        [FreeFunction("AllocateInputDeviceId")]
        public static extern int AllocateDeviceId();

        // C# doesn't allow taking the address of a value type because of pinning requirements for the heap.
        // And our bindings generator doesn't support overloading. So ugly code following here...
        public static unsafe void QueueInputEvent<TInputEvent>(ref TInputEvent inputEvent)
            where TInputEvent : struct
        {
            QueueInputEvent((IntPtr)UnsafeUtility.AddressOf<TInputEvent>(ref inputEvent));
        }

        public static extern void QueueInputEvent(IntPtr inputEvent);

        public static extern long IOCTL(int deviceId, int code, IntPtr data, int sizeInBytes);

        public static void SetPollingFrequency(float hertz)
        {
            if (hertz < 1.0f)
                throw new ArgumentException("Polling frequency cannot be less than 1Hz");
            SetPollingFrequencyInternal(hertz);
        }

        private static extern void SetPollingFrequencyInternal(float hertz);

        // The following APIs are not for normal use of the system as they do things
        // that are normally done either by platform-specific backends or by the player
        // loop. However, they can be used, for example, to drive the system from
        // managed code during tests.
        public static extern void Update(NativeInputUpdateType updateType);
        public static extern int ReportNewInputDevice(string descriptor);
        public static extern void ReportInputDeviceRemoved(int id);
    }
}
