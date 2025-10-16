// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEngine.Rendering;

namespace Unity.ProjectAuditor.Editor.Modules
{
    class ShaderAnalyzer : ShaderModuleAnalyzer
    {
        internal const string PAA2000 = nameof(PAA2000);

        internal static readonly Descriptor k_SrpBatcherDescriptor = new Descriptor(
            PAA2000,
            "Shader: Not compatible with SRP batcher",
            Areas.CPU,
            "The shader is not compatible with SRP Batcher.",
            "Consider adding SRP Batcher compatibility to the shader. This will reduce the CPU time Unity requires to prepare and dispatch draw calls for materials that use the same shader variant."
        )
        {
            MessageFormat = "Shader '{0}' is not compatible with SRP Batcher",
            DocumentationUrl = "https://docs.unity3d.com/Manual/SRPBatcher.html"
        };

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_SrpBatcherDescriptor);
        }

        public override IEnumerable<ReportItem> Analyze(ShaderAnalysisContext context)
        {
            if (!IsSrpBatchingEnabled)
            {
                yield break;
            }

            if (context.Shader.name.StartsWith("Hidden/"))
            {
                yield break;
            }

            var subShaderIndex = ShaderUtilProxy.GetShaderActiveSubshaderIndex(context.Shader);
            var isSrpBatchingCompatible = ShaderUtilProxy.GetSRPBatcherCompatibilityCode(context.Shader, subShaderIndex) == 0;

            if (!isSrpBatchingCompatible && IsSrpBatchingEnabled)
            {
                yield return context.CreateIssue(IssueCategory.AssetIssue, k_SrpBatcherDescriptor.Id, context.Shader.name)
                    .WithLocation(context.AssetPath);
            }
        }

        internal static bool IsSrpBatchingEnabled => GraphicsSettings.defaultRenderPipeline != null &&
        GraphicsSettings.useScriptableRenderPipelineBatching;
    }
}
