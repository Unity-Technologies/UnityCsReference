// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UnityEditor.Build.Analysis
{
    internal interface IBuildReportConverter
    {
        BuildReportData Convert(BuildReport buildReport);
    }

    internal sealed class BuildReportConverter : IBuildReportConverter
    {
        static readonly ProfilerMarker s_ExtractAssetsMarker = new ProfilerMarker("BuildReportConverter.ExtractAssets");
        static readonly ProfilerMarker s_GetImporterTypesMarker = new ProfilerMarker("BuildReportConverter.GetImporterTypes");

        public BuildReportData Convert(BuildReport buildReport)
        {
            if (buildReport == null)
                throw new ArgumentNullException(nameof(buildReport));

            var steps = buildReport.steps ?? Array.Empty<BuildStep>();
            var parsedSteps = new BuildReportStepData[steps.Length];

            for (var i = 0; i < steps.Length; i++)
            {
                var step = steps[i];
                parsedSteps[i] = new BuildReportStepData
                {
                    Name = step.name ?? string.Empty,
                    Depth = step.depth,
                    DurationMs = (long)step.duration.TotalMilliseconds,
                };
            }

            var cachedReusePercent = ComputeCachedReusePercent(buildReport);
            var assets = ExtractAssets(buildReport);

            return new BuildReportData
            {
                Steps = parsedSteps,
                Assets = assets,
                TotalDurationMs = (long)buildReport.summary.totalTime.TotalMilliseconds,
                TotalErrors = buildReport.summary.totalErrors,
                TotalWarnings = buildReport.summary.totalWarnings,
                CachedReusePercent = cachedReusePercent,
            };
        }

        private static float ComputeCachedReusePercent(BuildReport buildReport)
        {
            if (buildReport == null)
                return -1f;

            var contentSummary = buildReport.contentSummary;
            if (contentSummary == null)
                return -1f;

            var serializedFileSize = contentSummary.serializedFileSize;
            var reusedSerializedFileSize = contentSummary.reusedSerializedFileSize;
            if (serializedFileSize == 0 || reusedSerializedFileSize > serializedFileSize)
                return -1f;

            var percent = (float)reusedSerializedFileSize * 100f / serializedFileSize;

            if (percent < 0f)
                percent = 0f;
            else if (percent > 100f)
                percent = 100f;

            return percent;
        }

        private static BuildReportAssetData[] ExtractAssets(BuildReport buildReport)
        {
            using (s_ExtractAssetsMarker.Auto())
            {
                if (buildReport == null)
                    return Array.Empty<BuildReportAssetData>();

                var contentSummary = buildReport.contentSummary;
                if (contentSummary == null)
                    return Array.Empty<BuildReportAssetData>();

                var assetStats = contentSummary.assetStats;
                if (assetStats.Length == 0)
                    return Array.Empty<BuildReportAssetData>();

                var guids = new GUID[assetStats.Length];
                for (var i = 0; i < assetStats.Length; i++)
                    guids[i] = assetStats[i].sourceAssetGUID;

                Type[] importerTypes;
                using (s_GetImporterTypesMarker.Auto())
                    importerTypes = AssetDatabase.GetImporterTypes(guids);
                Debug.Assert(importerTypes.Length == assetStats.Length);

                var assets = new BuildReportAssetData[assetStats.Length];
                for (var i = 0; i < assetStats.Length; i++)
                {
                    var stats = assetStats[i];
                    assets[i] = new BuildReportAssetData
                    {
                        Path = stats.sourceAssetPath ?? string.Empty,
                        GUID = stats.sourceAssetGUID,
                        OutputSizeBytes = stats.size,
                        ObjectCount = stats.objectCount,
                        ResourceCount = stats.resourceCount,
                        ImporterTypeName = importerTypes[i]?.Name,
                    };
                }

                return assets;
            }
        }
    }
}
