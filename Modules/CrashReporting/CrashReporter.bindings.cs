// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using UnityEngine.Bindings;

[assembly: InternalsVisibleTo("Unity.Services.CloudDiagnostics")]
[assembly: InternalsVisibleTo("Unity.Services.CloudDiagnostics.Tests")]
namespace UnityEngine.CrashReportHandler
{
    [NativeHeader("Modules/CrashReporting/Public/CrashReporter.h")]
    [StaticAccessor("CrashReporting::CrashReporter::Get()", StaticAccessorType.Dot)]
    public partial class CrashReportHandler
    {
        private CrashReportHandler()
        {
        }

        [NativeProperty("Enabled")]
        public static extern bool enableCaptureExceptions { get; set; }

        [NativeThrows]
        public static extern UInt32 logBufferSize { get; set; }

        [NativeThrows]
        internal static extern string installationIdentifier { get; set; }

        [NativeThrows]
        public static extern string GetUserMetadata(string key);

        [NativeThrows]
        public static extern void SetUserMetadata(string key, string value);
    }
}
