// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Build.Profile;
using UnityEngine;

namespace UnityEditor.Modules;

class SDKPlatformProvider
{
    public SDKPlatformType platformType { get; private set; }

    /// <summary>
    /// The IPlatformProvider type that this SDKPlatformProvider was created from. This is used for 
    /// getting the assembly of the provider, which is needed for checking if it's from a unity
    /// registry package.
    /// </summary>
    internal Type providerType { get; private set; }

    public GUID guid { get; private set; }
    public string targetName { get; private set; }
    public string platformDefine { get; private set; }

    public bool shouldShowPlatformSettings { get; private set; } = true;
    public bool shouldShowAdditionalSettings { get; private set; } = true;
    public bool shouldShowAddSettingsButton { get; private set; } = true;
    public Type[] requiredComponents { get; private set; } = [];

    public Action<BuildProfile> onMultiTargetPlatformBuildProfileCreated { get; private set; }
    public Action<BuildProfile, int, Action<BuildProfile, int>> onDerivedPlatformBuildProfileCreated { get; private set; }

    const string k_Guid = "guid";
    const string k_TargetName = "targetName";
    const string k_PlatformDefine = "platformDefine";
    const string k_ShouldShowPlatformSettings = "shouldShowPlatformSettings";
    const string k_ShouldShowAdditionalSettings = "shouldShowAdditionalSettings";
    const string k_ShouldShowAddSettingsButton = "shouldShowAddSettingsButton";
    const string k_RequiredComponents = "requiredComponents";
    const string k_OnMultiTargetPlatformBuildProfileCreated = "OnMultiTargetPlatformBuildProfileCreated";
    const string k_OnDerivedPlatformBuildProfileCreated = "OnDerivedPlatformBuildProfileCreated";

    SDKPlatformProvider(IPlatformProvider provider, SDKPlatformType platformType)
    {
        this.platformType = platformType;
        providerType = provider.GetType();

        guid = (GUID)providerType.GetProperty(k_Guid).GetValue(provider);
        targetName = providerType.GetProperty(k_TargetName).GetValue(provider) as string ?? string.Empty;
        platformDefine = providerType.GetProperty(k_PlatformDefine).GetValue(provider) as string ?? string.Empty;

        shouldShowPlatformSettings = providerType.GetProperty(k_ShouldShowPlatformSettings)?.GetValue(provider) as bool? ?? true;
        shouldShowAdditionalSettings = providerType.GetProperty(k_ShouldShowAdditionalSettings)?.GetValue(provider) as bool? ?? true;
        shouldShowAddSettingsButton = providerType.GetProperty(k_ShouldShowAddSettingsButton)?.GetValue(provider) as bool? ?? true;
        requiredComponents = providerType.GetProperty(k_RequiredComponents)?.GetValue(provider) as Type[] ?? [];

        if (platformType == SDKPlatformType.MultiTarget)
        {
            onMultiTargetPlatformBuildProfileCreated = (Action<BuildProfile>)providerType
                .GetMethod(k_OnMultiTargetPlatformBuildProfileCreated, new[] { typeof(BuildProfile) })
                .CreateDelegate(typeof(Action<BuildProfile>), provider);
        }
        else if (platformType == SDKPlatformType.Derived)
        {
            onDerivedPlatformBuildProfileCreated = (Action<BuildProfile, int, Action<BuildProfile, int>>)providerType
                .GetMethod(k_OnDerivedPlatformBuildProfileCreated, new[] { typeof(BuildProfile), typeof(int), typeof(Action<BuildProfile, int>) })
                .CreateDelegate(typeof(Action<BuildProfile, int, Action<BuildProfile, int>>), provider);
        }
    }

    public static SDKPlatformProvider TryCreateMultiTargetProvider(IPlatformProvider provider)
    {
        if (!IsMultiTargetCompatible(provider))
            return null;

        return new SDKPlatformProvider(provider, SDKPlatformType.MultiTarget);
    }

    public static SDKPlatformProvider TryCreateDerivedPlatformProvider(IPlatformProvider provider)
    {
        if (!IsDerivedPlatformCompatible(provider))
            return null;

        return new SDKPlatformProvider(provider, SDKPlatformType.Derived);
    }

    static bool IsMultiTargetCompatible(IPlatformProvider provider)
    {
        return provider.version switch
        {
            1 => IsMultiTargetCompatibleV1(provider),
            _ => false
        };
    }

    static bool IsDerivedPlatformCompatible(IPlatformProvider provider)
    {
        return provider.version switch
        {
            1 => IsDerivedPlatformCompatibleV1(provider),
            _ => false
        };
    }

    static bool IsMultiTargetCompatibleV1(IPlatformProvider provider)
    {
        var type = provider.GetType();

        if (type.GetProperty(k_Guid) == null)
            return false;

        if (type.GetProperty(k_TargetName) == null)
            return false;

        if (type.GetProperty(k_PlatformDefine) == null)
            return false;

        var method = type.GetMethod(k_OnMultiTargetPlatformBuildProfileCreated, new[] { typeof(BuildProfile) });
        if (method == null)
            return false;

        if (method.ReturnType != typeof(void))
            return false;

        return true;
    }

    static bool IsDerivedPlatformCompatibleV1(IPlatformProvider provider)
    {
        var type = provider.GetType();

        if (type.GetProperty(k_Guid) == null)
            return false;

        if (type.GetProperty(k_TargetName) == null)
            return false;

        if (type.GetProperty(k_PlatformDefine) == null)
            return false;

        var method = type.GetMethod(k_OnDerivedPlatformBuildProfileCreated,
            new[] { typeof(BuildProfile), typeof(int), typeof(Action<BuildProfile, int>) });
        if (method == null)
            return false;

        if (method.ReturnType != typeof(void))
            return false;

        return true;
    }
}
