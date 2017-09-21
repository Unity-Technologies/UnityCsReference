// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngineInternal;

namespace UnityEngineInternal.Input
{
    [NativeHeader("Modules/Input/Private/InputModuleBindings.h")]
    [NativeHeader("Modules/Input/Private/InputInternal.h")]
    [NativeConditional("ENABLE_NEW_INPUT_SYSTEM")]
    public partial class NativeInputSystem
    {
        public static extern double zeroEventTime { get; }

        public static extern bool hasDeviceDiscoveredCallback { set; }

        // C# doesn't allow taking the address of a value type because of pinning requirements for the heap.
        // And our bindings generator doesn't support overloading. So ugly code following here...
        public static void SendInput<TInputEvent>(TInputEvent inputEvent)
            where TInputEvent : struct
        {
            SendInput(UnsafeUtility.AddressOf<TInputEvent>(ref inputEvent));
        }

        public static extern void SendInput(IntPtr inputEvent);

        public static bool SendOutput<TOutputEvent>(int deviceId, int type, TOutputEvent outputEvent)
            where TOutputEvent : struct
        {
            return SendOutput(deviceId, type, UnsafeUtility.SizeOf<TOutputEvent>(), UnsafeUtility.AddressOf(ref outputEvent));
        }

        public static extern bool SendOutput(int deviceId, int type, int sizeInBytes, IntPtr data);

        // TODO: This was ported from the old bindings system, but it doesn't look needed.
        public static string GetDeviceConfiguration(int deviceId) { return null; }

        public static extern string GetControlConfiguration(int deviceId, int controlIndex);

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
        static extern void SendEvents();
        static extern void Update(NativeInputUpdateType updateType);
        static extern int ReportNewInputDevice(string descriptor);
        static extern void ReportInputDeviceDisconnect(int nativeDeviceId);
        static extern void ReportInputDeviceReconnect(int nativeDeviceId);
    }
}
