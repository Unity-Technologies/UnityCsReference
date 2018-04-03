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
    [StaticAccessor("GetPerformanceReportingManager()", StaticAccessorType.Dot)]
    public static class PerformanceReporting
    {
        [ThreadAndSerializationSafe()]
        public extern static bool enabled { get; set; }

        public extern static long graphicsInitializationFinishTime
        {
            [NativeMethod("GetGfxDoneTime")] get;
        }
    }
}
