// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Build.Profile;
using UnityEngine.Bindings;

namespace UnityEditor.Modules;

[VisibleToOtherModules]
internal interface ISDKPlatformExtension
{
    SDKPlatformProvider sdkPlatformProvider { get; }
    ConfigurableBuildTarget sdkPlatformBuildTarget { get; }

    /// <summary>
    /// What this SDK platform adds to its base or selected platform's compilation extension.
    /// </summary>
    ICompilationExtension additionalCompilationExtension { get; }

    /// <summary>
    /// This SDK platform's complete compilation extension. For a multi-target platform the base is the
    /// selected platform's extension, which changes with the selection, so this is computed per
    /// call and must not be stored.
    /// </summary>
    ICompilationExtension CreateCompilationExtension(ICompilationExtension baseCompilationExtension)
        => CompositeCompilationExtension.Combine(baseCompilationExtension, additionalCompilationExtension);

    bool shouldShowPlatformSettings { get; }
    bool shouldShowAdditionalSettings { get; }
    bool shouldShowAddSettingsButton { get; }
    bool shouldShowBuildActions { get; }
    Type[] requiredComponents { get; }
    Type[] customFooterActions { get; }

    void OnMultiTargetBuildProfileCreated(BuildProfile buildProfile, int preconfiguredSettingsVariant);
}
