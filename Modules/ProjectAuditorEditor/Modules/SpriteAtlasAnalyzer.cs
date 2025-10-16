// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine.U2D;

namespace Unity.ProjectAuditor.Editor.Modules
{
    internal class SpriteAtlasAnalyzer : SpriteAtlasModuleAnalyzer
    {
        internal const string PAA0008 = nameof(PAA0008);

        internal static readonly Descriptor k_PoorUtilizationDescriptor = new Descriptor(
            PAA0008,
            "Sprite Atlas: Too much empty space",
            Areas.Memory,
            "The Sprite Atlas texture contains a lot of empty space. Empty space contributes to texture memory usage.",
            "Consider reorganizing your Sprite Atlas Texture in order to reduce the amount of empty space."
        )
        {
            IsEnabledByDefault = true,
            MessageFormat = "Sprite Atlas '{0}' has too much empty space ({1})"
        };

#pragma warning disable CS0649
        [DiagnosticParameter("SpriteAtlasEmptySpaceLimit","Empty Sprite Atlas use threshold (percentage, set to 100 to disable analysis)", "Warn if the percentage of unused pixels in a Sprite Atlas is greater than this threshold.", 50)]
        int m_EmptySpaceLimit;
#pragma warning restore CS0649

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_PoorUtilizationDescriptor);
        }

        public override IEnumerable<ReportItem> Analyze(SpriteAtlasAnalysisContext context)
        {
            if (context.IsDescriptorEnabled(k_PoorUtilizationDescriptor))
            {
                if (context.EmptySpacePercentage > m_EmptySpaceLimit)
                {
                    yield return context.CreateIssue(IssueCategory.AssetIssue,
                        k_PoorUtilizationDescriptor.Id, context.SpriteAtlas.name, Formatting.FormatPercentage(context.EmptySpacePercentage / 100))
                        .WithLocation(context.AssetPath);
                }
            }
        }
    }
}
