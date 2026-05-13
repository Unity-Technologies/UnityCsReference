// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Build.Analysis
{
    [Serializable]
    internal class BuildReportData
    {
        public BuildReportStepData[] Steps;
        public BuildReportMessageData[] Messages;
        public long TotalDurationMs;
        public int TotalErrors;
        public int TotalWarnings;
        public int AssetCount;
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
}
