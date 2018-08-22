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
        internal static extern bool hasDeviceDiscoveredCallback { set; }

        public static extern double currentTime { get; }

        public static extern double currentTimeOffsetToRealtimeSinceStartup { get; }

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

        public static extern void SetPollingFrequency(float hertz);

        public static extern void Update(NativeInputUpdateType updateType);

        public static extern void SetUpdateMask(NativeInputUpdateType mask);
    }
}
