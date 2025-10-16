// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.Editor.SettingsAnalysis
{
    class EntitiesGraphicsAnalyzer : SettingsModuleAnalyzer
    {
        internal const string PAS1000 = nameof(PAS1000);
        internal const string PAS1013 = nameof(PAS1013);

        // Legacy: The Hybrid Renderer was replaced by Entities Graphics when Entities 0.51 was released in mid-2022.
        static readonly Descriptor k_HybridDescriptor = new Descriptor(
            PAS1000,
            "Player Settings: Static batching is enabled",
            Areas.CPU,
            "<b>Static Batching</b> is enabled in Player Settings and the package com.unity.rendering.hybrid is installed. Static batching is incompatible with the batching techniques used in the Hybrid Renderer and Scriptable Render Pipeline, and will result in poor rendering performance and excessive memory use.",
            "Disable static batching in Player Settings.")
        {
            Fixer = (issue, analysisParams) =>
            {
                PlayerSettingsUtil.SetStaticBatchingEnabled(analysisParams.Platform, false);
            }
        };

        static readonly Descriptor k_EntitiesGraphicsDescriptor = new Descriptor(
            PAS1013,
            "Player Settings: Static batching is enabled",
            Areas.CPU,
            "<b>Static Batching</b> is enabled in Player Settings and the package com.unity.entities.graphics is installed. Static batching is incompatible with the batching techniques used in Entities Graphics and the Scriptable Render Pipeline, and will result in poor rendering performance and excessive memory use.",
            "Disable static batching in Player Settings.")
        {
            Fixer = (issue, analysisParams) =>
            {
                PlayerSettingsUtil.SetStaticBatchingEnabled(analysisParams.Platform, false);
            }
        };

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_HybridDescriptor);
            registerDescriptor(k_EntitiesGraphicsDescriptor);
        }

        public override IEnumerable<ReportItem> Analyze(SettingsAnalysisContext context)
        {
            yield break;
        }
    }
}
