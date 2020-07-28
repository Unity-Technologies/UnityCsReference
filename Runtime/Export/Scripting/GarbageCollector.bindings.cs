// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.Scripting
{
    [NativeHeader("Runtime/Scripting/GarbageCollector.h")]
    public static class GarbageCollector
    {
        public enum Mode
        {
            Disabled = 0,
            Enabled = 1,
            Manual =  2,
        }

        public static event Action<Mode> GCModeChanged;

        public static Mode GCMode
        {
            get
            {
                return GetMode();
            }

            set
            {
                if (value == GetMode())
                    return;

                SetMode(value);

                if (GCModeChanged != null)
                    GCModeChanged(value);
            }
        }

        [NativeThrows]
        extern static void SetMode(Mode mode);
        extern static Mode GetMode();

        public extern static bool isIncremental { [NativeMethod("GetIncrementalEnabled")] get; }

        public extern static ulong incrementalTimeSliceNanoseconds { get; set; }

        [NativeThrows]
        [NativeMethod("CollectIncrementalWrapper")]
        public extern static bool CollectIncremental(ulong nanoseconds = 0);
    }
}
