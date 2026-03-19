// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Build.Profile;

namespace UnityEditor.Modules;

class ConfigurableSDKPlatformExtension : ISDKPlatformExtension
{
    public SDKPlatformProvider sdkPlatformProvider { get; private set; }
    public ConfigurableBuildTarget sdkPlatformBuildTarget { get; private set; }

    public bool shouldShowPlatformSettings => sdkPlatformProvider.shouldShowPlatformSettings;
    public bool shouldShowAdditionalSettings => sdkPlatformProvider.shouldShowAdditionalSettings;
    public bool shouldShowAddSettingsButton => sdkPlatformProvider.shouldShowAddSettingsButton;
    public Type[] requiredComponents => sdkPlatformProvider.requiredComponents;

    public void OnMultiTargetBuildProfileCreated(BuildProfile buildProfile)
    {
        if (sdkPlatformProvider.platformType == SDKPlatformType.MultiTarget)
            sdkPlatformProvider.onMultiTargetPlatformBuildProfileCreated?.Invoke(buildProfile);
    }

    public ConfigurableSDKPlatformExtension(SDKPlatformProvider provider, ConfigurableBuildTarget buildTarget)
    {
        sdkPlatformProvider = provider;
        sdkPlatformBuildTarget = buildTarget;
    }
}
