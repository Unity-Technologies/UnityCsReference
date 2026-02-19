// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.SettingsAnalysis
{
    class TimeSettingsAnalyzer : SettingsModuleAnalyzer
    {
        internal const string PAS0016 = nameof(PAS0016);
        internal const string PAS0017 = nameof(PAS0017);

        static readonly Descriptor k_FixedTimestepDescriptor = new Descriptor(
            PAS0016,
            "Time: Fixed Timestep is set to the default value",
            Areas.CPU,
            "In the Time Settings, <b>Fixed Timestep</b> is set to the default value of <b>0.02</b>. This means that Unity will try to ensure that the FixedUpdate() methods of MonoBehaviours, and physics updates will be called 50 times per second. This is appropriate for games running at 60 FPS, but at 30 FPS this would mean that the FixedUpdate step will be called twice during most frames.",
            "We recommend setting <b>Fixed Timestep</b> to 0.04 when running at 30 FPS, in order to call the fixed updates at 25 Hz. The reason for having the fixed update be slightly less than the target frame rate is to avoid the \"spiral of death\", in which if one frame takes longer than 33.3ms, FixedUpdate() happens multiple times on the next frame to catch up, pushing that frame time over as well, and permanently locking the game into a state where it cannot reach the desired frame rate because FixedUpdate() is constantly trying to catch up."
        )
        {
            DocumentationUrl = "https://docs.unity3d.com/Manual/managing-time-and-frame-rate.html"
        };

        static readonly Descriptor k_MaximumAllowedTimestepDescriptor = new Descriptor(
            PAS0017,
            "Time: Maximum Allowed Timestep is set to the default value",
            Areas.CPU,
            "In the Time Settings, <b>Maximum Allowed Timestep</b> is set to the default value of <b>0.1</b>. This means that if the Time Manager is trying to \"catch\" up with previous frames that took longer than <b>Fixed Timestep</b> to process, the project's FixedUpdate() methods could end up being called repeatedly, up to a maximum of 0.1 seconds (100 milliseconds). Spending so long in FixedUpdate() would likely mean that FixedUpdate() must also be called multiple times in the subsequent frames, contributing to the \"spiral of death\".",
            "Consider reducing <b>Maximum Allowed Timestep</b> to a time that can be comfortably accommodated within your project's target frame rate."
        )
        {
            DocumentationUrl = "https://docs.unity3d.com/Manual/managing-time-and-frame-rate.html"
        };

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_FixedTimestepDescriptor);
            registerDescriptor(k_MaximumAllowedTimestepDescriptor);
        }

        public override IEnumerable<ReportItem> Analyze(SettingsAnalysisContext context)
        {
            if (UnityEngine.Time.fixedDeltaTime - 0.02f < Mathf.Epsilon)
                yield return context.CreateIssue(IssueCategory.ProjectSetting, k_FixedTimestepDescriptor.Id)
                    .WithLocation("Project/Time");
            if (UnityEngine.Time.maximumDeltaTime - 0.1f < Mathf.Epsilon)
                yield return context.CreateIssue(IssueCategory.ProjectSetting, k_MaximumAllowedTimestepDescriptor.Id)
                    .WithLocation("Project/Time");
        }
    }
}
