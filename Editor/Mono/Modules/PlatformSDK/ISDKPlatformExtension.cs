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

    bool shouldShowPlatformSettings { get; }
    bool shouldShowAdditionalSettings { get; }
    bool shouldShowAddSettingsButton { get; }
    Type[] requiredComponents { get; }

    void OnMultiTargetBuildProfileCreated(BuildProfile buildProfile);
}
