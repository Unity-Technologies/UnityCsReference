// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor.CrashReporting
{
    [NativeHeaderAttribute("Runtime/UnityConnect/CrashReporting/CrashReportingSettings.h")]
    [StaticAccessorAttribute("GetCrashReportingSettings()")]
    public static partial class CrashReportingSettings
    {
        internal static extern string GetEventUrl();
        internal static extern void SetEventUrl(string eventUrl);
        internal static extern string GetNativeEventUrl();
        internal static extern void SetNativeEventUrl(string eventUrl);
    }
}
