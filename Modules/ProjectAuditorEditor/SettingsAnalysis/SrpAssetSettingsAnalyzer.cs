// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using UnityEngine.Rendering;


namespace Unity.ProjectAuditor.Editor.SettingsAnalysis
{
    class SrpAssetSettingsAnalyzer : SettingsModuleAnalyzer
    {
        internal const string PAS1008 = nameof(PAS1008);

        static readonly Descriptor k_SRPBatcherSettingDescriptor = new Descriptor(
            PAS1008,
            "SRP Asset: SRP Batcher is disabled",
            Areas.CPU,
            "<b>SRP Batcher</b> is disabled in a Render Pipeline Asset.",
            "Enable <b>SRP Batcher</b> in Render Pipeline Asset. If the option is hidden, click the vertical ellipsis icon and select <b>Show Additional Properties</b>. Enabling the SRP Batcher will reduce the CPU time Unity requires to prepare and dispatch draw calls for materials that use the same shader variant.")
        {
            MessageFormat = "SRP batcher is disabled in {0}.asset in {1}",
            Fixer = FixSrpBatcherSetting
        };

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_SRPBatcherSettingDescriptor);
        }

        public override IEnumerable<ReportItem> Analyze(SettingsAnalysisContext context)
        {
            return RenderPipelineUtils.AnalyzeAssets(context, Analyze);
        }

        static void FixSrpBatcherSetting(ReportItem issue, AnalysisParams analysisParams)
        {
            RenderPipelineUtils.FixAssetSetting(issue, p => SetSrpBatcherSetting(p, true));
        }

        IEnumerable<ReportItem> Analyze(SettingsAnalysisContext context, RenderPipelineAsset renderPipeline, int qualityLevel)
        {
            bool? srpBatcherSetting = GetSrpBatcherSetting(renderPipeline);
            if (srpBatcherSetting != null && !srpBatcherSetting.Value)
            {
                yield return CreateSrpBatcherIssue(context, qualityLevel, renderPipeline.name);
            }
        }

        static ReportItem CreateSrpBatcherIssue(AnalysisContext context, int qualityLevel, string name)
        {
            return RenderPipelineUtils.CreateAssetSettingIssue(context, qualityLevel, name, k_SRPBatcherSettingDescriptor.Id);
        }

        internal static bool? GetSrpBatcherSetting(RenderPipelineAsset renderPipeline)
        {
            if (renderPipeline == null) return null;
            return null;
        }

        internal static void SetSrpBatcherSetting(RenderPipelineAsset renderPipeline, bool value)
        {
            if (renderPipeline == null) return;
        }

    }
}
