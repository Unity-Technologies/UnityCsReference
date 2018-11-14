// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.Scripting
{
    [NativeHeader("Runtime/Scripting/GarbageCollector.h")]
    [VisibleToOtherModules]
    public static class GarbageCollector
    {
        public enum Mode
        {
            Disabled = 0,
            Enabled = 1,
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
        [NativeThrows]
        extern static Mode GetMode();
    }
}
