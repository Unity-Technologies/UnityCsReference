// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.Windows
{
    public static class CrashReporting
    {
        public extern static string crashReportFolder
        {
            [ThreadSafe]
            [NativeHeader("PlatformDependent/WinPlayer/Bindings/CrashReportingBindings.h")]
            get;
        }
    }
}
