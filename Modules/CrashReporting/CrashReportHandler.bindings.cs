// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.CrashReportHandler
{
    [NativeHeader("Modules/CrashReporting/CrashReportHandler.h")]
    public partial class CrashReportHandler
    {
        private CrashReportHandler()
        {
        }

        public static extern bool enableCaptureExceptions { get; set; }
    }
}
