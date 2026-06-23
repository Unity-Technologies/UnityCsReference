// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace Unity.ProjectAuditor.Editor.Build
{
    internal partial class LastBuildReportProvider
    {
        public int callbackOrder => 0;
        [AutoStaticsCleanupOnCodeReload] // Lazy cache of BuildReport (ScriptableObject-derived); must be reset on code reload
        static BuildReport s_LastBuildReport = null;

        public BuildReport GetBuildReport(BuildTarget platform)
        {
            // Cached in memory
            if (s_LastBuildReport != null)
                return s_LastBuildReport;

            // Use BuildHistory API to find the last full player build
            s_LastBuildReport = LoadLastFullPlayerBuild(platform);
            return s_LastBuildReport;
        }

        /// <summary>
        /// Loads the most recent full Player build report for the specified platform using BuildHistory API.
        /// A "full" build has packedAssets.Length > 0 (not an incremental build that skipped asset packing).
        /// </summary>
        /// <remarks>
        /// In all versions of Unity 20xx / 6.X, the "incremental" build pipeline is actually all-or-nothing.
        /// An incremental build in which no assets (or settings that could affect assets) has changed
        /// will skip asset packing and packedAssets.Length will be 0, which means much of the build report
        /// information will be absent.  So rather than pick that build we look for earlier full build.
        /// </remarks>
        BuildReport LoadLastFullPlayerBuild(BuildTarget platform)
        {
            try
            {
                // Get all builds from BuildHistory, sorted by most recent first
                GUID[] builds = BuildHistory.GetAllBuilds();

                foreach (var buildGuid in builds)
                {
                    try
                    {
                        BuildReportSummary summary = BuildHistory.GetBuildSummary(buildGuid);

                        // ProjectAuditor only supports Player build analysis
                        if (summary.BuildType != BuildType.Player)
                            continue;

                        if (summary.Platform != platform)
                            continue;

                        // This build looks promising so load the full BuildReport
                        BuildReport report = BuildHistory.LoadBuildReport(buildGuid);
                        if (report != null && report.packedAssets.Length > 0)
                        {
                            return report;
                        }
                    }
                    catch (ArgumentException)
                    {
                        continue;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to load build report from BuildHistory: {e.Message}");
            }

            return null;
        }
    }
}
