// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;
using UnityEngine.Rendering;

namespace Unity.ProjectAuditor.Editor.SettingsAnalysis
{
    class GraphicsApiAnalyzer : SettingsModuleAnalyzer
    {
        const string documentationUrl = "https://docs.unity3d.com/Manual/GraphicsAPIs.html";

        internal const string PAS0005 = nameof(PAS0005);
        internal const string PAS0006 = nameof(PAS0006);
        internal const string PAS0031 = nameof(PAS0031);

        static readonly Descriptor k_MetalDescriptor = new Descriptor(
            PAS0006,
            "Player (iOS): Metal API is not enabled",
            Areas.CPU,
            "In the iOS Player Settings, Metal is not enabled.",
            "Enable Metal graphics API for better CPU Performance.")
        {
            DocumentationUrl = documentationUrl,
            Platforms = [BuildTarget.iOS]
        };

        static readonly Descriptor k_VulkanDescriptor = new Descriptor(
            PAS0031,
            "Player (Android): Vulkan API is not enabled",
            Areas.CPU | Areas.GPU,
            "In the Android Player Settings, Vulkan graphics API is not enabled.",
            "Enable Vulkan graphics API for better CPU Performance.")
        {
            DocumentationUrl = documentationUrl,
            Platforms = [BuildTarget.Android]
        };

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_MetalDescriptor);
            registerDescriptor(k_VulkanDescriptor);
        }

        public override IEnumerable<ReportItem> Analyze(SettingsAnalysisContext context)
        {
            if (k_MetalDescriptor.IsApplicable(context.Params) && IsNotUsingMetal())
                yield return context.CreateIssue(IssueCategory.ProjectSetting, k_MetalDescriptor.Id)
                    .WithLocation("Project/Player");

            if (k_VulkanDescriptor.IsApplicable(context.Params) && IsNotUsingVulkan())
                yield return context.CreateIssue(IssueCategory.ProjectSetting, k_VulkanDescriptor.Id)
                    .WithLocation("Project/Player");
        }

        static bool IsNotUsingMetal()
        {
            var graphicsAPIs = PlayerSettings.GetGraphicsAPIs(BuildTarget.iOS);
            return Array.IndexOf(graphicsAPIs, GraphicsDeviceType.Metal) == -1;
        }

        static bool IsNotUsingVulkan()
        {
            var graphicsAPIs = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            return Array.IndexOf(graphicsAPIs, GraphicsDeviceType.Vulkan) == -1;
        }
    }
}
