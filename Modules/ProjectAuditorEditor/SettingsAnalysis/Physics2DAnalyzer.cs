// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.SettingsAnalysis
{
    class Physics2DAnalyzer : SettingsModuleAnalyzer
    {
        internal const string PAS0015 = nameof(PAS0015);
        internal const string PAS0032 = nameof(PAS0032);

        static readonly Descriptor k_DefaultLayerCollisionMatrixDescriptor = new Descriptor(
            PAS0015,
            "Physics2D: Layer Collision Matrix has all boxes ticked",
            Areas.CPU,
            "In Physics2D Settings, all of the boxes in the <b>Layer Collision Matrix</b> are ticked. This increases the CPU work required to calculate collision detections.",
            "Un-tick all of the boxes except the ones that represent collisions that should be considered by the 2D physics system."
        );

        static readonly Descriptor k_SimulationModeDescriptor = new Descriptor(
            PAS0032,
            "Physics2D: Simulation Mode is set to automatically update",
            Areas.CPU,
            "<b>Simulation Mode</b> in Physics2D Settings is set to either <b>FixedUpdate</b> or <b>Update</b>. As a result, 2D physics simulation is executed on every update which might be expensive for some projects.",
            "Change <b>Project Settings > Physics 2D > Simulation Mode</b> to <b>Script</b> to disable the 2d physics processing each frame. If physics simulation is required for certain special rendering, use <b>Script</b> mode to control <b>Physics2d.Simulate</b> on a per frame basis.")
        {
            MinimumVersion = "2020.2"
        };

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_DefaultLayerCollisionMatrixDescriptor);
            registerDescriptor(k_SimulationModeDescriptor);
        }

        public override IEnumerable<ReportItem> Analyze(SettingsAnalysisContext context)
        {
            if (IsDefaultLayerCollisionMatrix())
            {
                yield return context.CreateIssue(IssueCategory.ProjectSetting, k_DefaultLayerCollisionMatrixDescriptor.Id)
                    .WithLocation("Project/Physics 2D/Layer Collision Matrix");
            }
            // Commented out as per Slack thread https://unity.slack.com/archives/CM6B17X50/p1740487386459879
            /*if (k_SimulationModeDescriptor.IsApplicable(context.Params) && IsNotUsingSimulationModeScript())
            {
                yield return context.CreateIssue(IssueCategory.ProjectSetting, k_SimulationModeDescriptor.Id)
                    .WithLocation("Project/Physics 2D/General");
            }*/
        }

        internal static bool IsDefaultLayerCollisionMatrix()
        {
            const int numLayers = 32;
            for (var i = 0; i < numLayers; ++i)
                for (var j = i; j < numLayers; ++j)
                    if (Physics2D.GetIgnoreLayerCollision(i, j))
                        return false;
            return true;
        }

        static bool IsNotUsingSimulationModeScript()
        {
            return Physics2D.simulationMode != SimulationMode2D.Script;
        }
    }
}
