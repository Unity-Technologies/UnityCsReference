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

        public const string k_ShowInFileBrowserLabel = "Open Containing Folder";

        // Schema version stamped into BuildAnalysis.json. Bump this whenever the serialized shape changes:
        // BuildAnalysisService regenerates any cached analysis whose version differs, so old caches self-heal.
        // 4: analyses now generate a DependencyGraph.bin sibling, so pre-graph caches regenerate once and write it.
        public const int k_SchemaVersion = 4;

        // The ContentLayout.json versions the dependency-graph reader understands. The newest is the
        // one the editor writes (keep in sync with kContentLayoutVersion in
        // Modules/BuildReportingEditor/Managed/ContentLayout.cs); the oldest is the first whose fields
        // the reader uses are all present. Version 4 only changed what StableId holds, which the
        // reader never reads, so builds in the history from version 3 stay readable. Version 2 has no
        // ArtifactIndex and reads as garbage, so it is refused.
        public const int k_MinContentLayoutVersion = 3;
        public const int k_SupportedContentLayoutVersion = 4;

        public static bool IsSupportedContentLayoutVersion(int version) =>
            version >= k_MinContentLayoutVersion && version <= k_SupportedContentLayoutVersion;

        // Subfolder inside a build's metadata folder for regenerable cache files.
        public const string k_BuildHistoryCacheSubfolder = "cache";

        public const string k_BuildAnalysisRelativePath = k_BuildHistoryCacheSubfolder + "/" + k_BuildAnalysisFileName;
        public const string k_DependencyGraphRelativePath = k_BuildHistoryCacheSubfolder + "/" + k_DependencyGraphFileName;
    }
}
