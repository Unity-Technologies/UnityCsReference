// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace UnityEditor.Build.Analysis
{
    internal static class BuildAnalysisAssembler
    {
        private const int k_SchemaVersion = 1;

        public static BuildAnalysis Assemble(BuildReportSummary reportSummary, BuildReportData reportData, RootAssetStats[] rootStats)
        {
            var stepTable = ConvertSteps(reportData.Steps);
            var analysisMessages = ConvertMessages(reportData.Messages, stepTable.Length);
            ConvertAssets(reportData.Assets, out var assetTable, out var importerTypeTable);
            var rootAssetTable = ConvertRootAssets(rootStats, assetTable);
            var computed = BuildComputed(
                assetTable,
                rootAssetTable,
                analysisMessages,
                reportData.CachedReusePercent);

            var output = new BuildAnalysis
            {
                Version = k_SchemaVersion,
                GeneratedAtUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                Summary = new BuildAnalysisSummary
                {
                    BuildSessionGUID = reportSummary.BuildSessionGUID.ToString(),
                    BuildName = reportSummary.BuildName ?? string.Empty,
                    BuildProfilePath = reportSummary.BuildProfilePath ?? string.Empty,
                    Platform = reportSummary.Platform.ToString(),
                    BuildResult = reportSummary.BuildResult.ToString(),
                    BuildStartedAtUtc = reportSummary.BuildStartedAt ?? string.Empty,
                    BuildType = reportSummary.BuildType.ToString(),
                    TotalSizeBytes = reportSummary.TotalSizeBytes,
                    TotalTimeMs = reportSummary.TotalTimeMs > 0 ? reportSummary.TotalTimeMs : reportData.TotalDurationMs,
                    TotalErrors = reportData.TotalErrors,
                    TotalWarnings = reportData.TotalWarnings,
                    BuildManifestHash = reportSummary.BuildManifestHash ?? string.Empty,
                    OutputPath = reportSummary.OutputPath ?? string.Empty,
                    BuildOptions = reportSummary.BuildOptions ?? Array.Empty<string>(),
                    BuildContentOptions = reportSummary.BuildContentOptions ?? Array.Empty<string>(),
                },
                Tables = new BuildAnalysisTables
                {
                    Steps = stepTable,
                    Assets = assetTable,
                    ImporterTypes = importerTypeTable,
                    RootAssets = rootAssetTable,
                },
                Messages = analysisMessages,
                Computed = computed,
            };

            return output;
        }

        private static BuildAnalysisStep[] ConvertSteps(BuildReportStepData[] steps)
        {
            var result = new BuildAnalysisStep[steps.Length];
            for (var i = 0; i < steps.Length; i++)
            {
                var source = steps[i];
                result[i] = new BuildAnalysisStep
                {
                    Id = i,
                    Name = source.Name ?? string.Empty,
                    Depth = source.Depth,
                    DurationMs = source.DurationMs,
                };
            }

            return result;
        }

        private static BuildAnalysisMessage[] ConvertMessages(BuildReportMessageData[] messages, int maxStepCount)
        {
            var result = new BuildAnalysisMessage[messages.Length];

            for (var i = 0; i < messages.Length; i++)
            {
                var source = messages[i];
                var stepIndex = source.StepIndex;
                if (stepIndex < 0 || stepIndex >= maxStepCount)
                    stepIndex = -1;

                result[i] = new BuildAnalysisMessage
                {
                    Severity = source.Severity ?? string.Empty,
                    StepId = stepIndex,
                    Text = source.Content ?? string.Empty,
                };
            }

            return result;
        }

        private static void ConvertAssets(
            BuildReportAssetData[] sourceAssets,
            out BuildAnalysisAsset[] assets,
            out BuildAnalysisImporterType[] importerTypes)
        {
            if (sourceAssets.Length == 0)
            {
                assets = Array.Empty<BuildAnalysisAsset>();
                importerTypes = Array.Empty<BuildAnalysisImporterType>();
                return;
            }

            var importerIdByName = new Dictionary<string, int>(StringComparer.Ordinal);
            var importerList = new List<BuildAnalysisImporterType>();

            assets = new BuildAnalysisAsset[sourceAssets.Length];
            for (var i = 0; i < sourceAssets.Length; i++)
            {
                var src = sourceAssets[i];
                var importerKey = string.IsNullOrEmpty(src.ImporterTypeName) ? "Unknown" : src.ImporterTypeName;
                if (!importerIdByName.TryGetValue(importerKey, out var importerId))
                {
                    importerId = importerList.Count;
                    importerList.Add(new BuildAnalysisImporterType { Id = importerId, Name = importerKey });
                    importerIdByName[importerKey] = importerId;
                }

                assets[i] = new BuildAnalysisAsset
                {
                    Id = i,
                    Path = src.Path ?? string.Empty,
                    GUID = src.GUID,
                    OutputSizeBytes = src.OutputSizeBytes,
                    ObjectCount = src.ObjectCount,
                    ResourceCount = src.ResourceCount,
                    ImporterTypeId = importerId,
                };
            }

            importerTypes = importerList.ToArray();
        }

        private static BuildAnalysisRootAsset[] ConvertRootAssets(
            RootAssetStats[] rootStats,
            BuildAnalysisAsset[] assets)
        {
            if (rootStats.Length == 0)
                return Array.Empty<BuildAnalysisRootAsset>();

            var pathToAssetId = new Dictionary<string, int>(assets.Length, StringComparer.Ordinal);
            foreach (var a in assets)
            {
                if (!string.IsNullOrEmpty(a.Path))
                    pathToAssetId[a.Path] = a.Id;
            }

            var result = new List<BuildAnalysisRootAsset>(rootStats.Length);
            foreach (var s in rootStats)
            {
                if (string.IsNullOrEmpty(s.AssetPath) || !pathToAssetId.TryGetValue(s.AssetPath, out var assetId))
                {
                    // Root assets are project source assets that should appear in BuildReport.assetStats.
                    // Skip on the rare miss rather than emit a sentinel AssetId.
                    Debug.LogWarning($"{BuildAnalysisConstants.k_ConsoleLogPrefix} Root asset '{s.AssetPath}' not found in Assets table.");
                    continue;
                }
                result.Add(new BuildAnalysisRootAsset
                {
                    Id = result.Count,
                    AssetId = assetId,
                    DirectAssetCount = s.DirectAssets,
                    DirectSizeBytes = s.DirectSize,
                    TotalAssetCount = s.TotalAssets,
                    TotalSizeBytes = s.TotalSize,
                    ReferencedAssetIds = ResolveReferencedAssetIds(s.ReferencedAssetPaths, pathToAssetId),
                });
            }
            return result.ToArray();
        }

        private static int[] ResolveReferencedAssetIds(
            string[] referencedAssetPaths,
            Dictionary<string, int> pathToAssetId)
        {
            if (referencedAssetPaths == null || referencedAssetPaths.Length == 0)
                return Array.Empty<int>();

            var ids = new List<int>(referencedAssetPaths.Length);
            foreach (var path in referencedAssetPaths)
            {
                if (pathToAssetId.TryGetValue(path, out var id))
                    ids.Add(id);
            }
            return ids.Count == 0 ? Array.Empty<int>() : ids.ToArray();
        }

        private static BuildAnalysisComputed BuildComputed(
            BuildAnalysisAsset[] assets,
            BuildAnalysisRootAsset[] rootAssets,
            BuildAnalysisMessage[] messages,
            float cacheReusePercent)
        {
            var counts = new BuildAnalysisCounts
            {
                AssetCount = assets.Length,
                RootAssetCount = rootAssets.Length,
            };

            foreach (var asset in assets)
            {
                if (!string.IsNullOrEmpty(asset.Path)
                    && asset.Path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                {
                    counts.SceneCount++;
                }
            }

            foreach (var t in messages)
            {
                var severity = t.Severity;
                if (string.Equals(severity, BuildMessageSeverity.Error, StringComparison.Ordinal))
                    counts.ErrorMessageCount++;
                else if (string.Equals(severity, BuildMessageSeverity.Warning, StringComparison.Ordinal))
                    counts.WarningMessageCount++;
                else
                    counts.InfoMessageCount++;
            }

            return new BuildAnalysisComputed
            {
                Counts = counts,
                CacheReusePercent = cacheReusePercent,
            };
        }
    }
}
