// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Build.Analysis
{
    [Serializable]
    internal class BuildAnalysis
    {
        public int Version;
        public string GeneratedAtUtc;
        public BuildAnalysisSummary Summary;
        public BuildAnalysisTables Tables;
        public BuildAnalysisMessage[] Messages;
        public BuildAnalysisComputed Computed;
    }

    [Serializable]
    internal class BuildAnalysisSummary
    {
        public string BuildSessionGUID;
        public string BuildName;
        public string Platform;
        public string BuildResult;
        public string BuildStartedAtUtc;
        public string BuildType;
        public long TotalSizeBytes;
        public long TotalTimeMs;
        public int TotalErrors;
        public int TotalWarnings;
        public string BuildManifestHash;
        public string OutputPath;
        public string[] BuildOptions;
        public string[] BuildContentOptions;
    }

    [Serializable]
    internal class BuildAnalysisTables
    {
        public BuildAnalysisStep[] Steps;
    }

    [Serializable]
    internal struct BuildAnalysisStep
    {
        public int Id;
        public string Name;
        public int Depth;
        public long DurationMs;
    }

    [Serializable]
    internal struct BuildAnalysisMessage
    {
        public string Severity;
        public int StepId;
        public string Text;
    }

    [Serializable]
    internal class BuildAnalysisComputed
    {
        public BuildAnalysisCounts Counts;
        public float CacheReusePercent = -1f; // < 0 means unavailable
    }

    [Serializable]
    internal struct BuildAnalysisCounts
    {
        public int AssetCount;
        public int ErrorMessageCount;
        public int WarningMessageCount;
        public int InfoMessageCount;
    }
}
