// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Unity.ProjectAuditor.Editor.Build
{
    internal class LastBuildReportProvider : IPostprocessBuildWithReport
    {
        internal const string k_LastBuildReportPath = "Library/LastBuild.buildreport";
        internal const string k_LastCleanBuildReportPath = "Library/LastCleanBuild.buildreport";

        public int callbackOrder => 0;
        static BuildReport s_LastBuildReport = null;

        public BuildReport GetBuildReport(BuildTarget platform)
        {
            // Cached in memory
            if (s_LastBuildReport != null)
                return s_LastBuildReport;

            // Cached in Library folder
            s_LastBuildReport = LoadBuildReport(platform, k_LastCleanBuildReportPath, false);
            if (s_LastBuildReport != null)
                return s_LastBuildReport;

            // Last resort: Try the standard build report path, see if it was a clean build
            s_LastBuildReport = LoadBuildReport(platform, k_LastBuildReportPath, true);
            return s_LastBuildReport;
        }

        BuildReport LoadBuildReport(BuildTarget platform, string path, bool saveCopy)
        {
            if (!File.Exists(path))
                return null;

            UnityEngine.Object[] objects =
                UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget(path);

            foreach (var obj in objects)
            {
                if (obj is BuildReport)
                {
                    var report = (BuildReport)obj;

                    if (report.summary.platform == platform && report.packedAssets.Length > 0)
                    {
                        if (saveCopy)
                        {
                            File.Copy(k_LastBuildReportPath, k_LastCleanBuildReportPath, true);
                        }
                        return report;
                    }
                }
            }

            return null;
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            // In all versions of Unity 20xx, the "incremental" build pipeline is actually all-or-nothing.
            // An incremental build in which no assets (or settings that could affect assets) has changed will skip asset packing and packedAssets.Length will be 0.
            // In all other cases, packedAssets will include information on all the assets in the build.
            if (report.packedAssets.Length > 0)
            {
                // Cache build report in memory in case some script wants to retrieve it this frame
                s_LastBuildReport = report;

                // Library/LastBuild.buildreport is only created AFTER OnPostprocessBuild so we need to defer the copy of the file
                EditorApplication.update += DelayedBuildReportCopy;
            }
        }

        static void DelayedBuildReportCopy()
        {
            File.Copy(k_LastBuildReportPath, k_LastCleanBuildReportPath, true);
            EditorApplication.update -= DelayedBuildReportCopy;
        }
    }
}
