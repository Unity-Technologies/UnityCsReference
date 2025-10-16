// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.SettingsAnalysis
{
    class HdrpAnalyzer : SettingsModuleAnalyzer
    {
        internal const string PAS1001 = nameof(PAS1001);
        internal const string PAS1002 = nameof(PAS1002);

        static readonly Descriptor k_AssetLitShaderModeBothOrMixed = new Descriptor(
            PAS1001,
            "HDRP: Render Pipeline Assets use both Lit Shader Modes",
            Areas.BuildSize | Areas.BuildTime,
            "The <b>Lit Shader Mode</b> option in the HDRP Asset is set to <b>Both</b>. As a result, shaders will be built for both Forward and Deferred rendering. This increases build time and size.",
            "Change <b>Lit Shader Mode</b> to either <b>Forward</b> or <b>Deferred</b>."
        );

        static readonly Descriptor k_CameraLitShaderModeBothOrMixed = new Descriptor(
            PAS1002,
            "HDRP: Cameras mix usage of Lit Shader Modes",
            Areas.BuildSize | Areas.BuildTime,
            "Project contains Multiple HD Cameras, some of which have <b>Lit Shader Mode</b> set to <b>Forward</b>, and some to <b>Deferred</b>. As a result, shaders will be built for both Forward and Deferred rendering. This increases build time and size.",
            "Change the <b>Lit Shader Mode</b> in all HDRP Assets and all Cameras to either <b>Forward</b> or <b>Deferred</b>."
        );

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_AssetLitShaderModeBothOrMixed);
            registerDescriptor(k_CameraLitShaderModeBothOrMixed);
        }

        public override IEnumerable<ReportItem> Analyze(SettingsAnalysisContext context)
        {
            yield break;
        }

    }
}
