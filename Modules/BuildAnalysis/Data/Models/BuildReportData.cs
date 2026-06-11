// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.Build.Analysis
{
    [Serializable]
    internal class BuildReportData
    {
        public BuildReportStepData[] Steps = Array.Empty<BuildReportStepData>();
        public BuildReportMessageData[] Messages = Array.Empty<BuildReportMessageData>();
        public BuildReportAssetData[] Assets = Array.Empty<BuildReportAssetData>();
        public long TotalDurationMs;
        public int TotalErrors;
        public int TotalWarnings;
        public float CachedReusePercent = -1f; // < 0 means unavailable
    }

    [Serializable]
    internal struct BuildReportStepData
    {
        public string Name;
        public int Depth;
        public long DurationMs;
    }

    [Serializable]
    internal struct BuildReportMessageData
    {
        public string Severity;
        public int StepIndex;
        public string Content;
    }

    [Serializable]
    internal struct BuildReportAssetData
    {
        public string Path;
        public GUID GUID;
        public ulong OutputSizeBytes;
        public int ObjectCount;
        public int ResourceCount;
        public string ImporterTypeName;
    }
}
