// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.Analytics
{

    [NativeHeader("Modules/UnityConnect/PerformanceReporting/PerformanceReportingSettings.h")]
    [StaticAccessor("GetPerformanceReportingSettings()", StaticAccessorType.Dot)]
    public static partial class PerformanceReportingSettings
    {
        [ThreadAndSerializationSafe()]
        public static extern bool enabled { get; set; }
    }

}
