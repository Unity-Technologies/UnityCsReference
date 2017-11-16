// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.BuildReporting
{
    [NativeType(Header = "Modules/BuildReportingEditor/Managed/BuildSummary.bindings.h", CodegenOptions = CodegenOptions.Custom)]
    public struct BuildSummary
    {
        internal Int64 buildStartTimeTicks;
        public DateTime buildStartedAt { get { return new DateTime(buildStartTimeTicks); } }

        [NativeName("buildGUID")]
        public GUID guid { get; }

        public BuildTarget platform { get; }
        public BuildTargetGroup platformGroup { get; }
        public BuildOptions options { get; }
        internal BuildAssetBundleOptions assetBundleOptions { get; }
        public string outputPath { get; }
        internal uint crc { get; }
        public ulong totalSize { get; }

        internal UInt64 totalTimeTicks;
        public TimeSpan totalTime { get { return new TimeSpan((long)totalTimeTicks); } }

        [NativeName("totalErrors")]
        public int errors { get; }

        [NativeName("totalWarnings")]
        public int warnings { get; }

        [NativeName("buildResult")]
        public BuildResult result { get; }

        internal BuildType buildType { get; }
    }
}
