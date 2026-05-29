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
        public BuildAnalysisTables Tables = new BuildAnalysisTables();
        public BuildAnalysisMessage[] Messages = Array.Empty<BuildAnalysisMessage>();
        public BuildAnalysisComputed Computed = new BuildAnalysisComputed();
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
        public string[] BuildOptions = Array.Empty<string>();
        public string[] BuildContentOptions = Array.Empty<string>();
    }

    [Serializable]
    internal class BuildAnalysisTables
    {
        public BuildAnalysisStep[] Steps = Array.Empty<BuildAnalysisStep>();
        public BuildAnalysisAsset[] Assets = Array.Empty<BuildAnalysisAsset>();
        public BuildAnalysisImporterType[] ImporterTypes = Array.Empty<BuildAnalysisImporterType>();
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
    internal struct BuildAnalysisAsset
    {
        public int Id;
        public string Path;
        public string GUID;
        public ulong OutputSizeBytes;
        public int ObjectCount;
        public int ResourceCount;
        public int ImporterTypeId;
    }

    [Serializable]
    internal struct BuildAnalysisImporterType
    {
        public int Id;
        public string Name;
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
        public int SceneCount;
        public int ErrorMessageCount;
        public int WarningMessageCount;
        public int InfoMessageCount;
    }
}
