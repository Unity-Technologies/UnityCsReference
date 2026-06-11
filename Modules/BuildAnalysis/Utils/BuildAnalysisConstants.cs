// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Build.Analysis
{
    internal static class BuildAnalysisConstants
    {
        public const string k_BuildAnalysisFileName = "BuildAnalysis.json";
        public const string k_BuildReportSummaryFileName = "BuildReportSummary.json";
        public const string k_ContentLayoutFileName = "ContentLayout.json";
        public const string k_ConsoleLogPrefix = "[Build Analysis]";

        // Subfolder inside a build's metadata folder for regenerable cache files.
        public const string k_BuildHistoryCacheSubfolder = "cache";

        public const string k_BuildAnalysisRelativePath = k_BuildHistoryCacheSubfolder + "/" + k_BuildAnalysisFileName;
    }
}
