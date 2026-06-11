// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UnityEditor.Build.Analysis
{
    internal interface IBuildHistoryProvider
    {
        GUID[] GetAllBuilds();
        BuildReportSummary GetBuildSummary(GUID buildSessionGuid);
        bool TryLoadBuildReport(GUID buildSessionGuid, out BuildReport buildReport);
        bool TryGetFilePath(GUID buildSessionGuid, string filename, out string filePath);
        bool TryGetBuildReportDirectory(GUID buildSessionGuid, out string directory);
        void Refresh();
        int GetRevision();
        int GetBuildHistoryLimit();
        int DeleteHistory(GUID[] buildSessionGuids);
        int DeleteAllHistory();
    }

    internal sealed class BuildHistoryProvider : IBuildHistoryProvider
    {
        public GUID[] GetAllBuilds()
        {
            return BuildHistory.GetAllBuilds();
        }

        public BuildReportSummary GetBuildSummary(GUID buildSessionGuid)
        {
            return BuildHistory.GetBuildSummary(buildSessionGuid);
        }

        public bool TryLoadBuildReport(GUID buildSessionGuid, out BuildReport buildReport)
        {
            buildReport = BuildHistory.LoadBuildReport(buildSessionGuid);
            return buildReport != null;
        }

        public bool TryGetFilePath(GUID buildSessionGuid, string filename, out string filePath)
        {
            return BuildHistory.TryGetFilePath(buildSessionGuid, filename, out filePath);
        }

        public bool TryGetBuildReportDirectory(GUID buildSessionGuid, out string directory)
        {
            return BuildHistory.TryGetBuildReportDirectory(buildSessionGuid, out directory);
        }

        public void Refresh()
        {
            BuildHistory.Refresh();
        }

        public int GetRevision()
        {
            return BuildHistory.GetRevision();
        }

        public int GetBuildHistoryLimit()
        {
            return BuildHistory.BuildHistoryLimit;
        }

        public int DeleteHistory(GUID[] buildSessionGuids)
        {
            return BuildHistory.DeleteHistory(buildSessionGuids);
        }

        public int DeleteAllHistory()
        {
            return BuildHistory.DeleteHistory();
        }
    }
}
