// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.SettingsAnalysis
{
    class TargetArchitectureAnalyzer : SettingsModuleAnalyzer
    {
        internal const string PAS0003 = nameof(PAS0003);
        internal const string PAS0004 = nameof(PAS0004);

        static readonly Descriptor k_DescriptorIOS = new Descriptor(
            PAS0003,
            "Player (iOS): Architecture is set to Universal",
            Areas.BuildSize,
            "In the iOS Player Settings, <b>Architecture</b> is set to <b>Universal</b>. This means that the application will be compiled for both 32-bit ARMv7 iOS devices (i.e. up to the iPhone 5 or 5c) and 64-bit ARM64 devices (iPhone 5s onwards), resulting in increased build times and binary size.",
            "If your application isn't intended to support 32-bit iOS devices, change <b>Architecture</b> to <b>ARM64</b>.")
        {
            Platforms = new SerializableEnum<BuildTarget>[] { BuildTarget.iOS }
        };

        static readonly Descriptor k_DescriptorAndroid = new Descriptor(
            PAS0004,
            "Player (Android): Target Architecture set to both ARMv7 and ARM64",
            Areas.BuildSize,
            "In the Android Player Settings, in the <b>Target Architecture</b> section, both the <b>ARMv7</b> and <b>ARM64</b> options are selected. This means that the application will be compiled for both 32-bit ARMv7 Android devices and 64-bit ARM64 devices, resulting in increased build times and binary size.",
            "If your application isn't intended to support 32-bit Android devices, disable the <b>ARMv7</b> option.")
        {
            Platforms = new SerializableEnum<BuildTarget>[] { BuildTarget.Android }
        };

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_DescriptorIOS);
            registerDescriptor(k_DescriptorAndroid);
        }

        public override IEnumerable<ReportItem> Analyze(SettingsAnalysisContext context)
        {
            // PlayerSettings.GetArchitecture returns an integer value associated with the architecture of a BuildTargetPlatformGroup. 0 - None, 1 - ARM64, 2 - Universal.
            if (k_DescriptorIOS.IsApplicable(context.Params) && PlayerSettingsUtil.GetArchitecture(BuildTargetGroup.iOS) == 2)
                yield return context.CreateIssue(IssueCategory.ProjectSetting, k_DescriptorIOS.Id)
                    .WithLocation("Project/Player");

            if (k_DescriptorAndroid.IsApplicable(context.Params) && (PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARMv7) != 0 &&
                (PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) != 0)
                yield return context.CreateIssue(IssueCategory.ProjectSetting, k_DescriptorAndroid.Id)
                    .WithLocation("Project/Player");
        }
    }
}
