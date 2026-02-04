// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.ProjectAuditor.Editor.SettingsAnalysis
{
    class BuiltinRenderPipelineAnalyzer : SettingsModuleAnalyzer
    {
        static readonly GraphicsTier[] k_GraphicsTiers = { GraphicsTier.Tier1, GraphicsTier.Tier2, GraphicsTier.Tier3};

        internal const string PAS0022 = nameof(PAS0022);
        internal const string PAS0023 = nameof(PAS0023);
        internal const string PAS0024 = nameof(PAS0024);

        static readonly Descriptor k_ShaderQualityDescriptor = new Descriptor(
            PAS0022,
            "Graphics: Shader Quality uses a mixture of different values",
            Areas.BuildSize,
            "The current build target Graphics Tier Settings use a mixture of different values (Low/Medium/High) for the <b>Standard Shader Quality</b> setting. This will result in a larger number of shader variants being compiled, which will increase build times and your application's download/install size.",
            "Unless you support devices with a very wide range of capabilities for a particular platform, consider editing the platform in Graphics Settings to use the same shader quality setting across all Graphics Tiers.");

        static readonly Descriptor k_ForwardRenderingDescriptor = new Descriptor(
            PAS0023,
            "Graphics: Rendering Path is set to Forward Rendering",
            Areas.GPU,
            "The current build target uses forward rendering, as set in the <b>Rendering Path</b> settings in <b>Project Settings > Graphics > Tier Settings</b>. This can impact GPU performance in projects with nontrivial numbers of dynamic lights.",
            "This rendering path is suitable for games with simple rendering and lighting requirements - for instance, 2D games, or games which mainly use baked lighting. If the project makes use of a more than a few dynamic lights, consider experimenting with changing <b>Rendering Path</b> to Deferred to see whether doing so improves GPU rendering times.");

        static readonly Descriptor k_DeferredRenderingDescriptor = new Descriptor(
            PAS0024,
            "Graphics: Rendering Path is set to Deferred Rendering",
            Areas.GPU,
            "The current build target uses deferred rendering, as set in the <b>Rendering Path</b> settings in <b>Project Settings > Graphics > Tier Settings</b>. This can impact GPU performance in projects with simple rendering requirements.",
            "This rendering path is suitable for games with more complex rendering requirements - for instance, games that make uses of dynamic lighting or certain types of fullscreen post-processing effects. If the project doesn't make use of such rendering techniques, consider experimenting with changing <b>Rendering Path</b> to Forward to see whether doing so improves GPU rendering times.");

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_ShaderQualityDescriptor);
            registerDescriptor(k_ForwardRenderingDescriptor);
            registerDescriptor(k_DeferredRenderingDescriptor);
        }

        public override IEnumerable<ReportItem> Analyze(SettingsAnalysisContext context)
        {
            // Only check for Built-In Rendering Pipeline
            if (IsUsingBuiltinRenderPipeline())
            {
                if (IsMixedStandardShaderQuality(context.Params.Platform))
                {
                    yield return context.CreateIssue(IssueCategory.ProjectSetting, k_ShaderQualityDescriptor.Id)
                        .WithLocation("Project/Graphics");
                }
                if (IsUsingForwardRendering(context.Params.Platform))
                {
                    yield return context.CreateIssue(IssueCategory.ProjectSetting, k_ForwardRenderingDescriptor.Id)
                        .WithLocation("Project/Graphics");
                }
                if (IsUsingDeferredRendering(context.Params.Platform))
                {
                    yield return context.CreateIssue(IssueCategory.ProjectSetting, k_DeferredRenderingDescriptor.Id)
                        .WithLocation("Project/Graphics");
                }
            }
        }

        static bool IsUsingBuiltinRenderPipeline()
        {
            return GraphicsSettings.defaultRenderPipeline == null;
        }

        internal static bool IsMixedStandardShaderQuality(BuildTarget platform)
        {
            var buildGroup = BuildPipeline.GetBuildTargetGroup(platform);
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var standardShaderQualities = k_GraphicsTiers.Select(tier => EditorGraphicsSettings.GetTierSettings(buildGroup, tier).standardShaderQuality);
#pragma warning restore UA2001

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return standardShaderQualities.Distinct().Count() > 1;
#pragma warning restore UA2001
        }

        internal static bool IsUsingForwardRendering(BuildTarget platform)
        {
            var buildGroup = BuildPipeline.GetBuildTargetGroup(platform);
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var renderingPaths = k_GraphicsTiers.Select(tier => EditorGraphicsSettings.GetTierSettings(buildGroup, tier).renderingPath);
#pragma warning restore UA2001

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return renderingPaths.Any(path => path == RenderingPath.Forward);
#pragma warning restore UA2001
        }

        internal static bool IsUsingDeferredRendering(BuildTarget platform)
        {
            var buildGroup = BuildPipeline.GetBuildTargetGroup(platform);
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var renderingPaths = k_GraphicsTiers.Select(tier => EditorGraphicsSettings.GetTierSettings(buildGroup, tier).renderingPath);
#pragma warning restore UA2001

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return renderingPaths.Any(path => path == RenderingPath.DeferredShading);
#pragma warning restore UA2001
        }
    }
}
