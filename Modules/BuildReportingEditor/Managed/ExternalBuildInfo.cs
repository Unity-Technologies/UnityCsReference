// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UnityEditor.Build
{
    /// <summary>Describes a build performed by an external build pipeline, for registration in the build history.</summary>
    /// <remarks>
    /// Pass this structure to <see cref="BuildHistory.RegisterExternalBuild"/> to record a build performed outside
    /// the Unity build pipeline. Unity derives the stored <see cref="BuildReportSummary"/> from these values.
    /// </remarks>
    /// <seealso cref="BuildHistory.RegisterExternalBuild"/>
    /// <seealso cref="BuildReportSummary"/>
    public struct ExternalBuildInfo
    {
        /// <summary>A unique identifier for the build session. Required.</summary>
        /// <remarks>For more information, refer to <see cref="BuildReportSummary.BuildSessionGUID"/>.</remarks>
        public GUID BuildSessionGUID;

        /// <summary>The display name of the build.</summary>
        public string BuildName;

        /// <summary>The type of build. Required.</summary>
        public BuildType BuildType;

        /// <summary>An optional display name for the build pipeline that performed the build.
        /// When empty, Unity uses the name of <see cref="BuildType"/>.</summary>
        public string BuildTypeName;

        /// <summary>The outcome of the build. Required.</summary>
        /// <remarks>Must be a final result: <see cref="Build.Reporting.BuildResult.Succeeded"/>,
        /// <see cref="Build.Reporting.BuildResult.Failed"/>, or <see cref="Build.Reporting.BuildResult.Cancelled"/>.</remarks>
        public BuildResult BuildResult;

        /// <summary>The time the build started. Required.</summary>
        public DateTimeOffset BuildStartedAt;

        /// <summary>The platform the build was created for. Required.</summary>
        public BuildTarget Platform;

        /// <summary>The total time taken by the build.</summary>
        public TimeSpan TotalTime;

        /// <summary>The total size of the build output, in bytes.</summary>
        public long TotalSizeBytes;

        /// <summary>The output path of the build.</summary>
        public string OutputPath;

        /// <summary>The name of the package whose build pipeline made this build, for example `com.unity.addressables`. Optional.</summary>
        /// <remarks>Nothing but that package can display the build's report, so the **Build Analysis** window
        /// uses this to offer it when it isn't installed. Leave it empty for a pipeline that doesn't ship as a
        /// package.</remarks>
        public string ProducerPackage;

        /// <summary>The total number of errors recorded during the build.</summary>
        public int TotalErrors;

        /// <summary>The total number of warnings recorded during the build.</summary>
        public int TotalWarnings;
    }
}
