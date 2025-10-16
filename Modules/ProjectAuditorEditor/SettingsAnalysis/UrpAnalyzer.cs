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
    class UrpAnalyzer : SettingsModuleAnalyzer
    {
        internal const string PAS1009 = nameof(PAS1009);
        internal const string PAS1010 = nameof(PAS1010);
        internal const string PAS1011 = nameof(PAS1011);
        internal const string PAS1012 = nameof(PAS1012);

        static readonly Descriptor k_URPAssetDescriptor = new Descriptor(
            PAS1009,
            "URP: URP Asset is not specified",
            Areas.GPU | Areas.Quality,
            "Graphics Settings do not refer to a URP Asset.",
            "Check the settings: Graphics > Scriptable Render Pipeline Settings > Render Pipeline Asset.")
        {
            MessageFormat = "URP: URP Asset is not specified"
        };

        static readonly Descriptor k_HdrSettingDescriptor = new Descriptor(
            PAS1010,
            "URP: HDR is enabled",
            Areas.GPU | Areas.Quality,
            "<b>HDR</b> (High Dynamic Range) is enabled in a URP Asset for mobile platforms. HDR rendering can be very intensive on low-end mobile GPUs.",
            "Disable <b>HDR</b> in the URP Asset.")
        {
            Platforms = new SerializableEnum<BuildTarget>[] { BuildTarget.Android, BuildTarget.iOS, BuildTarget.Switch},
            MessageFormat = "URP: HDR is enabled in {0}.asset in {1}",
            Fixer = FixHdrSetting
        };

        static readonly Descriptor k_MsaaSampleCountSettingDescriptor = new Descriptor(
            PAS1011,
            "URP: MSAA is set to 4x or 8x",
            Areas.GPU | Areas.Quality,
            "<b>Anti Aliasing (MSAA)</b> is set to <b>4x</b> or <b>8x</b> in a URP Asset for mobile platforms. MSAA 4x/8x rendering can be intensive on low-end mobile GPUs.",
            "Decrease <b>Anti Aliasing (MSAA)</b> value to <b>2x</b> in the URP Asset.")
        {
            Platforms = new SerializableEnum<BuildTarget>[] { BuildTarget.Android, BuildTarget.iOS, BuildTarget.Switch},
            MessageFormat = "URP: MSAA is set to 4x or 8x in {0}.asset in {1}",
            Fixer = FixMsaaSampleCountSetting
        };

        static readonly Descriptor k_CameraStopNanDescriptor = new Descriptor(
            PAS1012,
            "URP: Stop NaN property is enabled",
            Areas.GPU,
            "The <b>Stop NaNs</b> property is enabled on a Camera component. This stops certain effects from breaking, but is a resource-intensive process on the GPU. Only enable this feature if you experience NaN issues that you cannot fix.",
            "Disable <b>Stop NaNs</b> on as Camera components as you can."
        )
        {
            Platforms = new SerializableEnum<BuildTarget>[] { BuildTarget.Android, BuildTarget.iOS, BuildTarget.Switch}
        };

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_URPAssetDescriptor);
            registerDescriptor(k_HdrSettingDescriptor);
            registerDescriptor(k_MsaaSampleCountSettingDescriptor);
            registerDescriptor(k_CameraStopNanDescriptor);
        }

        public override IEnumerable<ReportItem> Analyze(SettingsAnalysisContext context)
        {
            yield break;
        }

        private static void FixHdrSetting(ReportItem issue, AnalysisParams analysisParams)
        {
        }

        static void FixMsaaSampleCountSetting(ReportItem issue, AnalysisParams analysisParams)
        {
        }

        IEnumerable<ReportItem> Analyze(SettingsAnalysisContext context, RenderPipelineAsset renderPipeline, int qualityLevel)
        {
            yield break;
        }

    }
}
