// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.SettingsAnalysis
{
    class PhysicsAnalyzer : SettingsModuleAnalyzer
    {
        internal const string PAS0013 = nameof(PAS0013);

        static readonly Descriptor k_DefaultLayerCollisionMatrixDescriptor = new Descriptor(
            PAS0013,
            "Physics: Layer Collision Matrix has all boxes ticked",
            Areas.CPU,
            "In Physics Settings, all of the boxes in the <b>Layer Collision Matrix</b> are ticked. This increases the CPU work required to calculate collision detections.",
            "Un-tick all of the boxes except the ones that represent collisions that should be considered by the Physics system."
        );

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_DefaultLayerCollisionMatrixDescriptor);
        }

        public override IEnumerable<ReportItem> Analyze(SettingsAnalysisContext context)
        {
            if (IsDefaultLayerCollisionMatrix())
            {
                yield return context.CreateIssue(IssueCategory.ProjectSetting, k_DefaultLayerCollisionMatrixDescriptor.Id)
                    .WithLocation("Project/Physics/Settings");
            }
        }

        internal static bool IsDefaultLayerCollisionMatrix()
        {
            const int numLayers = 32;
            for (var i = 0; i < numLayers; ++i)
                for (var j = i; j < numLayers; ++j)
                    if (Physics.GetIgnoreLayerCollision(i, j))
                        return false;
            return true;
        }
    }
}
