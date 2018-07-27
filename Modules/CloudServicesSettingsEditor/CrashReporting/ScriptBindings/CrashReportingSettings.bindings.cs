// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.CrashReporting
{

    [NativeHeader("Modules/UnityConnect/CrashReporting/CrashReportingSettings.h")]
    [StaticAccessor("GetCrashReportingSettings()")]
    public static partial class CrashReportingSettings
    {
        [ThreadAndSerializationSafe()]
        public static extern bool enabled { get; set; }

        public static extern bool captureEditorExceptions { get; set; }

        [NativeThrows]
        public static extern UInt32 logBufferSize { get; set; }

        internal static extern void SetEnabledServiceWindow(bool enabled);

        internal static extern string GetEventUrl();
        internal static extern void SetEventUrl(string eventUrl);
    }

}
