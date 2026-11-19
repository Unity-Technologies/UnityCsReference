// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Build;
using System.Collections;
using Unity.Scripting.LifecycleManagement;

namespace Unity.ProjectAuditor.Editor.Modules
{
    class BuildReportModule : Module
    {
        [NoAutoStaticsCleanup] // Stateless provider instance; safe to persist across code reload
        internal static readonly LastBuildReportProvider BuildReportProvider = new LastBuildReportProvider();

        public override string Name => "Build Report";

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => System.Array.Empty<IssueLayout>();

        public override IEnumerator Audit(AnalysisParams analysisParams, IProgress progress)
        {
            analysisParams.OnModuleCompleted?.Invoke(Name, AnalysisResult.Success, 0);
            yield break;
        }
    }
}
