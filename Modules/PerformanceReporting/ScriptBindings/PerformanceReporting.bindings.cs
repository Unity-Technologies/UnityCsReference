// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Analytics
{
    [NativeHeader("Modules/PerformanceReporting/PerformanceReportingManager.h")]
    public static class PerformanceReporting
    {
        [ThreadAndSerializationSafe()]
        [StaticAccessor("GetPerformanceReportingManager()", StaticAccessorType.Dot)]
        public extern static bool enabled { get; set; }
    }
}
