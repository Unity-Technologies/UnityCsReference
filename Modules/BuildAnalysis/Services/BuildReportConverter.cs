// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
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
        public BuildReportData Convert(BuildReport buildReport)
        {
            if (buildReport == null)
                throw new ArgumentNullException(nameof(buildReport));

            var steps = buildReport.steps ?? Array.Empty<BuildStep>();
            var parsedSteps = new BuildReportStepData[steps.Length];
            var parsedMessages = new List<BuildReportMessageData>(64);

            for (var i = 0; i < steps.Length; i++)
            {
                var step = steps[i];
                parsedSteps[i] = new BuildReportStepData
                {
                    Name = step.name ?? string.Empty,
                    Depth = step.depth,
                    DurationMs = (long)step.duration.TotalMilliseconds,
                };

                var messages = step.messages;
                if (messages == null)
                    continue;

                foreach (var message in messages)
                {
                    parsedMessages.Add(new BuildReportMessageData
                    {
                        Severity = ToSeverityString(message.type),
                        StepIndex = i,
                        Content = message.content ?? string.Empty,
                    });
                }
            }

            var cachedReusePercent = ComputeCachedReusePercent(buildReport);
            var assets = ExtractAssets(buildReport);

            return new BuildReportData
            {
                Steps = parsedSteps,
                Messages = parsedMessages.ToArray(),
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

            var importerTypes = AssetDatabase.GetImporterTypes(guids);
            Debug.Assert(importerTypes.Length == assetStats.Length);

            var assets = new BuildReportAssetData[assetStats.Length];
            for (var i = 0; i < assetStats.Length; i++)
            {
                var stats = assetStats[i];
                assets[i] = new BuildReportAssetData
                {
                    Path = stats.sourceAssetPath ?? string.Empty,
                    GUID = stats.sourceAssetGUID.ToString(),
                    OutputSizeBytes = stats.size,
                    ObjectCount = stats.objectCount,
                    ResourceCount = stats.resourceCount,
                    ImporterTypeName = importerTypes[i]?.Name,
                };
            }

            return assets;
        }

        private static string ToSeverityString(LogType messageType)
        {
            switch (messageType)
            {
                case LogType.Warning:
                    return BuildMessageSeverity.Warning;
                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception:
                    return BuildMessageSeverity.Error;
                default:
                    return BuildMessageSeverity.Info;
            }
        }
    }
}
