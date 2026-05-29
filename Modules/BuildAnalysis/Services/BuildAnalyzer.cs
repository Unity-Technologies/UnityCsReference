// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace UnityEditor.Build.Analysis
{
    internal interface IBuildAnalyzer
    {
        BuildAnalysis Generate(BuildEntry entry);
    }

    internal sealed class BuildAnalyzer : IBuildAnalyzer
    {
        private const int k_SchemaVersion = 1;

        private readonly IBuildReportConverter m_BuildReportConverter;
        private readonly IBuildAnalysisFileSystem m_FileSystem;
        private readonly IBuildHistoryProvider m_BuildHistory;

        public BuildAnalyzer(
            IBuildReportConverter buildReportConverter,
            IBuildAnalysisFileSystem fileSystem,
            IBuildHistoryProvider buildHistory)
        {
            m_BuildReportConverter = buildReportConverter ?? throw new ArgumentNullException(nameof(buildReportConverter));
            m_FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            m_BuildHistory = buildHistory ?? throw new ArgumentNullException(nameof(buildHistory));
        }

        public BuildAnalysis Generate(BuildEntry entry)
        {
            ValidateEntry(entry);

            var reportSummary = m_BuildHistory.GetBuildSummary(entry.BuildSessionGUID);

            if (!m_BuildHistory.TryLoadBuildReport(entry.BuildSessionGUID, out var buildReport))
                throw new InvalidDataException($"Missing build report for build '{entry.BuildSessionGUID}'.");
            var reportData = m_BuildReportConverter.Convert(buildReport);

            if (!m_BuildHistory.TryGetMetadataPath(entry.BuildSessionGUID, out var metadataPath))
                throw new InvalidDataException($"No metadata path available for build '{entry.BuildSessionGUID}'.");

            var analysis = BuildAnalysisFrom(reportSummary, reportData);

            var analysisPath = Path.Combine(metadataPath, BuildAnalysisConstants.k_BuildAnalysisFileName);
            var json = JsonUtility.ToJson(analysis, true);
            m_FileSystem.WriteAllText(analysisPath, json);

            return analysis;
        }

        private static BuildAnalysis BuildAnalysisFrom(BuildReportSummary reportSummary, BuildReportData reportData)
        {
            var stepTable = ConvertSteps(reportData.Steps);
            var analysisMessages = ConvertMessages(reportData.Messages, stepTable.Length);
            ConvertAssets(reportData.Assets, out var assetTable, out var importerTypeTable);
            var computed = BuildComputed(
                assetTable,
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
                    GUID = src.GUID ?? string.Empty,
                    OutputSizeBytes = src.OutputSizeBytes,
                    ObjectCount = src.ObjectCount,
                    ResourceCount = src.ResourceCount,
                    ImporterTypeId = importerId,
                };
            }

            importerTypes = importerList.ToArray();
        }

        private static BuildAnalysisComputed BuildComputed(
            BuildAnalysisAsset[] assets,
            BuildAnalysisMessage[] messages,
            float cacheReusePercent)
        {
            var counts = new BuildAnalysisCounts
            {
                AssetCount = assets.Length,
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

        private static void ValidateEntry(BuildEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));
            if (entry.BuildSessionGUID.Empty())
                throw new InvalidDataException("BuildSessionGUID is required to generate BuildAnalysis.");
        }
    }
}
