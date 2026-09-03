// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
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
    public bool shouldShowBuildActions { get; private set; } = true;
    public Type[] requiredComponents { get; private set; } = [];
    public Type[] customFooterActions { get; private set; } = [];
    public PreconfiguredSettingsVariant[] preconfiguredSettingsVariants { get; private set; } = [];

    public Action<BuildProfile, int> onMultiTargetPlatformBuildProfileCreated { get; private set; }
    public Action<BuildProfile, int, Action<BuildProfile, int>> onDerivedPlatformBuildProfileCreated { get; private set; }

    const string k_Guid = "guid";
    const string k_TargetName = "targetName";
    const string k_PlatformDefine = "platformDefine";
    const string k_ShouldShowPlatformSettings = "shouldShowPlatformSettings";
    const string k_ShouldShowAdditionalSettings = "shouldShowAdditionalSettings";
    const string k_ShouldShowAddSettingsButton = "shouldShowAddSettingsButton";
    const string k_ShouldShowBuildActions = "shouldShowBuildActions";
    const string k_RequiredComponents = "requiredComponents";
    const string k_FooterActions = "customFooterActions";
    const string k_PreconfiguredSettingsVariants = "preconfiguredSettingsVariants";
    const string k_OnMultiTargetPlatformBuildProfileCreated = "OnMultiTargetPlatformBuildProfileCreated";
    const string k_OnDerivedPlatformBuildProfileCreated = "OnDerivedPlatformBuildProfileCreated";

    static readonly string k_CompatibilityError = L10n.Tr("{0} is not a compatible platform provider: {1}", null);
    static readonly string k_InvalidPlatformTypeError = L10n.Tr("{0} has an invalid platform type: {1}", null);
    static readonly string k_MultiTargetPlatformCompatibilityError = L10n.Tr("{0} is not compatible as a multi-target platform provider: {1}", null);
    static readonly string k_DerivedPlatformCompatibilityError = L10n.Tr("{0} is not compatible as a derived platform provider: {1}", null);
    static readonly string k_UnrecognizedVersionError = L10n.Tr("unrecognized version {0}.", null);
    static readonly string k_RequiredPropertyError = L10n.Tr("required property '{0}' is missing.", null);
    static readonly string k_PropertyTypeError = L10n.Tr("property '{0}' must be of type {1}.", null);
    static readonly string k_DuplicatePreconfiguredSettingsVariantsError = L10n.Tr(
        "{0} declares preconfigured settings variants while SDK platform '{1}' also declares them in its manifest. " +
        "The manifest variants are used; remove the '{2}' property from the provider.", null);

    SDKPlatformProvider(IPlatformProvider provider, SDKPlatformType platformType)
    {
        this.platformType = platformType;
        FetchProviderProperties(provider);

        switch (platformType)
        {
            case SDKPlatformType.MultiTarget:
                FetchMultiTargetOptionals(provider);
                break;
            case SDKPlatformType.Derived:
                FetchDerivedOptionals(provider);
                break;
        }
    }

    void FetchProviderProperties(IPlatformProvider provider)
    {
        providerType = provider.GetType();

        guid = GetProp(k_Guid, new GUID());
        targetName = GetProp(k_TargetName, string.Empty);
        platformDefine = GetProp(k_PlatformDefine, string.Empty);

        shouldShowPlatformSettings = GetProp(k_ShouldShowPlatformSettings, true);
        shouldShowAdditionalSettings = GetProp(k_ShouldShowAdditionalSettings, true);
        shouldShowAddSettingsButton = GetProp(k_ShouldShowAddSettingsButton, true);
        shouldShowBuildActions = GetProp(k_ShouldShowBuildActions, true);

        requiredComponents = GetProp(k_RequiredComponents, Array.Empty<Type>());
        customFooterActions = GetProp(k_FooterActions, Array.Empty<Type>());

        preconfiguredSettingsVariants = BuildTargetDiscovery.BuildPlatformPreconfiguredSettingsVariants(guid);
        var extractedVariants = GetProp(k_PreconfiguredSettingsVariants, Array.Empty<SDKPreconfiguredSettingsVariant>());
        if (preconfiguredSettingsVariants.Length > 0)
        {
            if (extractedVariants.Length > 0)
                Debug.LogError(string.Format(k_DuplicatePreconfiguredSettingsVariantsError, providerType.FullName, guid, k_PreconfiguredSettingsVariants));
        }
        else
        {
            var variantList = new List<PreconfiguredSettingsVariant>();
            foreach (var variant in extractedVariants)
            {
                if (variant == null)
                    continue;
                var newVariant = new PreconfiguredSettingsVariant(variant.displayName, variant.selectedInitially, variant.description, variant.tooltip, variant.platformGuid);
                variantList.Add(newVariant);
            }
            preconfiguredSettingsVariants = variantList.ToArray();
        }

        T GetProp<T>(string propName, T defaultValue)
        {
            var prop = providerType.GetProperty(propName);
            if (prop == null)
                return defaultValue;
            var val = prop.GetValue(provider);
            return val is T t ? t : defaultValue;
        }
    }

    void FetchMultiTargetOptionals(IPlatformProvider provider)
    {
        var methodWithVariant = providerType.GetMethod(k_OnMultiTargetPlatformBuildProfileCreated, new[] { typeof(BuildProfile), typeof(int) });
        if (methodWithVariant != null && methodWithVariant.ReturnType == typeof(void))
        {
            onMultiTargetPlatformBuildProfileCreated = (Action<BuildProfile, int>)methodWithVariant
                .CreateDelegate(typeof(Action<BuildProfile, int>), provider);
        }
        else
        {
            var methodWithoutVariant = providerType.GetMethod(k_OnMultiTargetPlatformBuildProfileCreated, new[] { typeof(BuildProfile) });
            if (methodWithoutVariant != null && methodWithoutVariant.ReturnType == typeof(void))
            {
                var noVariantAction = (Action<BuildProfile>)methodWithoutVariant
                    .CreateDelegate(typeof(Action<BuildProfile>), provider);
                onMultiTargetPlatformBuildProfileCreated = (profile, _) => noVariantAction(profile);
            }
        }
    }

    void FetchDerivedOptionals(IPlatformProvider provider)
    {
        var method = providerType.GetMethod(k_OnDerivedPlatformBuildProfileCreated, new[] { typeof(BuildProfile), typeof(int), typeof(Action<BuildProfile, int>) });
        if (method != null && method.ReturnType == typeof(void))
            onDerivedPlatformBuildProfileCreated = (Action<BuildProfile, int, Action<BuildProfile, int>>)method
                .CreateDelegate(typeof(Action<BuildProfile, int, Action<BuildProfile, int>>), provider);
    }

    public static bool TryGetProviderGuid(IPlatformProvider provider, out GUID guid)
    {
        var type = provider.GetType();

        if (!HasRequiredProperty(type, k_Guid, typeof(GUID), out var error))
        {
            Debug.LogError(string.Format(k_CompatibilityError, type.FullName, error));
            guid = default;
            return false;
        }
        
        var prop = type.GetProperty(k_Guid);
        guid = (GUID)prop.GetValue(provider);
        return true;
    }

    public static bool TryCreatePlatformProvider(IPlatformProvider provider, SDKPlatformType platformType, out SDKPlatformProvider sdkPlatformProvider)
    {
        sdkPlatformProvider = null;

        if (!IsProviderCompatible(provider, platformType))
            return false;

        sdkPlatformProvider = new SDKPlatformProvider(provider, platformType);
        return true;
    }

    static bool IsProviderCompatible(IPlatformProvider provider, SDKPlatformType platformType)
    {
        var providerTypeName = provider.GetType().FullName;
        switch (platformType)
        {
            case SDKPlatformType.MultiTarget:
            {
                if (IsMultiTargetPlatformCompatible(provider, out var error))
                    return true;

                Debug.LogError(string.Format(k_MultiTargetPlatformCompatibilityError, providerTypeName, error));
                return false;
            }
            case SDKPlatformType.Derived:
            {
                if (IsDerivedPlatformCompatible(provider, out var error))
                    return true;

                Debug.LogError(string.Format(k_DerivedPlatformCompatibilityError, providerTypeName, error));
                return false;
            }
            default:
                Debug.LogError(string.Format(k_InvalidPlatformTypeError, providerTypeName, platformType.ToString()));
                return false;
        }
    }

    static bool IsMultiTargetPlatformCompatible(IPlatformProvider provider, out string error)
    {
        switch (provider.version)
        {
            case 1:
                return HasRequiredCoreProperties(provider.GetType(), out error);
            default:
                error = FormatUnrecognizedVersionError(provider.version);
                return false;
        }
    }

    static bool IsDerivedPlatformCompatible(IPlatformProvider provider, out string error)
    {
        switch (provider.version)
        {
            case 1:
                return HasRequiredCoreProperties(provider.GetType(), out error);
            default:
                error = FormatUnrecognizedVersionError(provider.version);
                return false;
        }
    }

    // guid is excluded here as it is validated separately in TryGetProviderGuid
    static bool HasRequiredCoreProperties(Type providerType, out string error)
    {
        if (!HasRequiredProperty(providerType, k_TargetName, typeof(string), out error))
            return false;

        if (!HasRequiredProperty(providerType, k_PlatformDefine, typeof(string), out error))
            return false;

        return true;
    }

    static bool HasRequiredProperty(Type providerType, string propertyName, Type propertyType, out string error)
    {
        var property = providerType.GetProperty(propertyName);
        if (property == null)
        {
            error = FormatRequiredPropertyError(propertyName);
            return false;
        }

        if (property.PropertyType != propertyType)
        {
            error = FormatPropertyTypeError(propertyName, propertyType);
            return false;
        }

        error = null;
        return true;
    }

    static string FormatUnrecognizedVersionError(int version)
    {
        return string.Format(k_UnrecognizedVersionError, version);
    }

    static string FormatRequiredPropertyError(string propertyName)
    {
        return string.Format(k_RequiredPropertyError, propertyName);
    }

    static string FormatPropertyTypeError(string propertyName, Type expectedType)
    {
        return string.Format(k_PropertyTypeError, propertyName, expectedType.FullName);
    }
}
