// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Utils;
using Unity.ProjectAuditor.Editor.Build;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.Collections;

namespace Unity.ProjectAuditor.Editor.Modules
{
    enum BuildReportMetaData
    {
        Value,
        Num
    }

    enum BuildReportFileProperty
    {
        ImporterType = 0,
        RuntimeType,
        Size,
        SizePercent,
        BuildFile,
        Num
    }

    enum BuildReportStepProperty
    {
        Duration = 0,
        Message,
        Depth,
        Num
    }

    class BuildReportModule : Module
    {
        class BuildAnalysisContext : AnalysisContext
        {
            public BuildReport Report;
        }

        const string k_KeyBuildPath = "Path";
        const string k_KeyPlatform = "Platform";
        const string k_KeyResult = "Result";

        const string k_KeyStartTime = "Start Time";
        const string k_KeyEndTime = "End Time";
        const string k_KeyTotalTime = "Total Time";
        const string k_KeyTotalSize = "Total Size";
        const string k_Unknown = "Unknown";

        static readonly IssueLayout k_MetaDataLayout = new IssueLayout
        {
            Category = IssueCategory.BuildSummary,
            Properties =
            [
                new PropertyDefinition { Type = PropertyType.Description, Name = "Key" }
            ]
        };

        static readonly IssueLayout k_FileLayout = new IssueLayout
        {
            Category = IssueCategory.BuildFile,
            Properties =
            [
                new PropertyDefinition { Type = PropertyType.Description, Name = "Source Asset", MaxAutoWidth = 500},
                new PropertyDefinition { Type = PropertyType.FileType, Name = "File Type", LongName = "File Extension"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildReportFileProperty.ImporterType), Format = PropertyFormat.String, Name = "Importer Type"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildReportFileProperty.RuntimeType), Format = PropertyFormat.String, Name = "Runtime Type", IsDefaultGroup = true},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildReportFileProperty.Size), Format = PropertyFormat.Bytes, Name = "Size", LongName = "Size in the Build"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildReportFileProperty.SizePercent), Format = PropertyFormat.Percentage, Name = "Size % (of Data)", LongName = "Percentage of the total data size"},
                new PropertyDefinition { Type = PropertyType.Path, Name = "Path", IsHidden = true},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildReportFileProperty.BuildFile), Format = PropertyFormat.String, Name = "Build File", MaxAutoWidth = 500 }
            ]
        };

        static readonly IssueLayout k_StepLayout = new IssueLayout
        {
            Category = IssueCategory.BuildStep,
            Properties =
            [
                new PropertyDefinition { Type = PropertyType.LogLevel, Name = "Log Level"},
                new PropertyDefinition { Type = PropertyType.Description, Name = "Build Step", MaxAutoWidth = 500 },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildReportStepProperty.Duration), Format = PropertyFormat.String, Name = "Duration"}
            ],
            IsHierarchy = true
        };

        internal static LastBuildReportProvider BuildReportProvider = new LastBuildReportProvider();

        public override string Name => "Build Report";

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts =>
        [
            k_MetaDataLayout,
            k_FileLayout,
            k_StepLayout
        ];

        public override IEnumerator Audit(AnalysisParams analysisParams, IProgress progress)
        {
            var buildReport = BuildReportProvider.GetBuildReport(analysisParams.Platform);
            yield return null;

            if (buildReport != null)
            {
                var context = new BuildAnalysisContext()
                {
                    Report = buildReport
                };

                analysisParams.OnIncomingIssues(
                [
                    NewMetaData(context, k_KeyBuildPath, buildReport.summary.outputPath),
                    NewMetaData(context, k_KeyPlatform, buildReport.summary.platform),
                    NewMetaData(context, k_KeyResult, buildReport.summary.result),
                    NewMetaData(context, k_KeyStartTime, Formatting.FormatDateTime(buildReport.summary.buildStartedAt)),
                    NewMetaData(context, k_KeyEndTime, Formatting.FormatDateTime(buildReport.summary.buildEndedAt)),
                    NewMetaData(context, k_KeyTotalTime, Formatting.FormatDuration(buildReport.summary.totalTime)),
                    NewMetaData(context, k_KeyTotalSize, Formatting.FormatSize(buildReport.summary.totalSize)),
                ]);

                analysisParams.OnIncomingIssues(AnalyzeBuildSteps(context));
                analysisParams.OnIncomingIssues(AnalyzePackedAssets(context));
            }

            analysisParams.OnModuleCompleted?.Invoke(Name, AnalysisResult.Success, 0);
        }

        IEnumerable<ReportItem> AnalyzeBuildSteps(BuildAnalysisContext context)
        {
            var zeroDuration = Formatting.FormatDuration(TimeSpan.Zero);

            foreach (var step in context.Report.steps)
            {
                var depth = step.depth;
                yield return context.CreateInsight(IssueCategory.BuildStep, step.name)
                    .WithCustomProperties(
                    [
                        Formatting.FormatDuration(step.duration),
                        step.name,
                        depth
                    ])
                    .WithSeverity(Severity.Hidden);

                foreach (var message in step.messages)
                {
                    var logMessage = message.content;
                    var description = new StringReader(logMessage).ReadLine(); // only take first line
                    yield return context.CreateInsight(IssueCategory.BuildStep, description)
                        .WithCustomProperties(
                        [
                            zeroDuration,
                            logMessage,
                            depth + 1
                        ])
                        .WithSeverity(CoreUtils.LogTypeToSeverity(message.type));
                }
            }
        }

        IEnumerable<ReportItem> AnalyzePackedAssets(BuildAnalysisContext context)
        {
            ulong dataSize = 0;
            foreach (var packedAsset in context.Report.packedAssets)
            {
                foreach (var assetInfo in packedAsset.contents)
                    dataSize += assetInfo.packedSize;
            }

            foreach (var packedAsset in context.Report.packedAssets)
            {
                // note that there can be several entries for each source asset (for example, a prefab can reference a Texture, a Material and a shader)
                foreach (var content in packedAsset.contents)
                {
                    // sourceAssetPath might contain '|' which is invalid. This is due to compressed texture format names in the asset name such as DXT1|BC1
                    var assetPath = PathUtils.ReplaceInvalidChars(content.sourceAssetPath);
                    var assetImporter = AssetImporter.GetAtPath(assetPath);
                    var description = string.IsNullOrEmpty(assetPath) ? k_Unknown : Path.GetFileNameWithoutExtension(assetPath);

                    yield return context.CreateInsight(IssueCategory.BuildFile, description)
                        .WithLocation(assetPath)
                        .WithCustomProperties(
                        [
                            assetImporter != null ? assetImporter.GetType().Name : k_Unknown,
                            content.type.Name,
                            content.packedSize,
                            Math.Round((double)content.packedSize / dataSize, 4),
                            packedAsset.shortPath
                        ]);
                }
            }
        }

        ReportItem NewMetaData(BuildAnalysisContext context, string key, object value)
        {
            return context.CreateInsight(IssueCategory.BuildSummary, key)
                .WithCustomProperties([value]);
        }
    }
}
