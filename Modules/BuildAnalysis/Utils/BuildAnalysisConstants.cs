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
        public const string k_BuildLogFileName = "BuildLog.jsonl";
        public const string k_BuildReportFileExtension = ".buildreport";
        public const string k_DependencyGraphFileName = "DependencyGraph.bin";
        public const string k_ConsoleLogPrefix = "[Build Analysis]";

        // Schema version stamped into BuildAnalysis.json. Bump this whenever the serialized shape changes:
        // BuildAnalysisService regenerates any cached analysis whose version differs, so old caches self-heal.
        // 4: analyses now generate a DependencyGraph.bin sibling, so pre-graph caches regenerate once and write it.
        public const int k_SchemaVersion = 4;

        // The one ContentLayout.json shape the dependency-graph reader understands.
        // Keep in sync with kContentLayoutVersion in Modules/BuildReportingEditor/Managed/ContentLayout.cs.
        public const int k_SupportedContentLayoutVersion = 3;

        // Subfolder inside a build's metadata folder for regenerable cache files.
        public const string k_BuildHistoryCacheSubfolder = "cache";

        public const string k_BuildAnalysisRelativePath = k_BuildHistoryCacheSubfolder + "/" + k_BuildAnalysisFileName;
        public const string k_DependencyGraphRelativePath = k_BuildHistoryCacheSubfolder + "/" + k_DependencyGraphFileName;
    }
}
