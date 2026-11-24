// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.Build.Analysis
{
    [Serializable]
    internal class BuildAnalysis
    {
        public int Version;
        public string GeneratedAtUtc;
        public BuildAnalysisSummary Summary;
        public BuildAnalysisTables Tables = new BuildAnalysisTables();
        public BuildAnalysisComputed Computed = new BuildAnalysisComputed();

        // Where the Assets table came from. Populated when this build recorded no assets of its own
        // (e.g. a scripts-only build) and the data was borrowed from an earlier complete build.
        public BuildAnalysisAssetSource AssetSource = new BuildAnalysisAssetSource();
    }

    [Serializable]
    internal class BuildAnalysisAssetSource
    {
        // The complete build this build reused content from (BuildReportSummary.ContentSourceBuildSessionGUID).
        // Empty for builds that produced their own content; set for asset-less scripts-only / incremental builds.
        public GUID ContentSourceBuildSessionGUID;

        // The content-source build's start time; set only when that build was found and its assets borrowed.
        public string BuildStartedAtUtc = string.Empty;

        // The content-source build was found and its assets borrowed.
        public bool IsBorrowed;

        // A content source was declared but couldn't be resolved (pruned/deleted, or its report missing), so the
        // reused assets are unknown.
        public bool SourceUnavailable => !IsBorrowed && !ContentSourceBuildSessionGUID.Empty();
    }

    [Serializable]
    internal class BuildAnalysisSummary
    {
        public string BuildSessionGUID;
        public string BuildName;
        public string BuildProfilePath;
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
        public BuildAnalysisRootAsset[] RootAssets = Array.Empty<BuildAnalysisRootAsset>();
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
        public GUID GUID;
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
    internal struct BuildAnalysisRootAsset
    {
        public int Id;
        public int AssetId;
        public int DirectAssetCount;
        public ulong DirectSizeBytes;
        public int TotalAssetCount;
        public ulong TotalSizeBytes;
        public int[] ReferencedAssetIds;
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
        public int RootAssetCount;
    }
}
