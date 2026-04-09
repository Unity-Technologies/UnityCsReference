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
    public bool shouldShowBuildActions { get; private set; } = true;
    public Type[] requiredComponents { get; private set; } = [];
    public Type[] customFooterActions { get; private set; } = [];

    public Action<BuildProfile> onMultiTargetPlatformBuildProfileCreated { get; private set; }
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
    const string k_OnMultiTargetPlatformBuildProfileCreated = "OnMultiTargetPlatformBuildProfileCreated";
    const string k_OnDerivedPlatformBuildProfileCreated = "OnDerivedPlatformBuildProfileCreated";

    static readonly string k_MultiTargetPlatformCompatibilityError = L10n.Tr("{0} is not compatible as a multi-target platform provider: {1}");
    static readonly string k_DerivedPlatformCompatibilityError = L10n.Tr("{0} is not compatible as a derived platform provider: {1}");
    static readonly string k_UnrecognizedVersionError = L10n.Tr("unrecognized version {0}.");
    static readonly string k_RequiredPropertyError = L10n.Tr("required property '{0}' is missing.");
    static readonly string k_PropertyTypeError = L10n.Tr("property '{0}' must be of type {1}.");
    static readonly string k_RequiredMethodError = L10n.Tr("required method '{0}' is missing or has an incorrect signature.");
    static readonly string k_MethodReturnTypeError = L10n.Tr("method '{0}' must return {1}.");

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
        shouldShowBuildActions = providerType.GetProperty(k_ShouldShowBuildActions)?.GetValue(provider) as bool? ?? true;
        requiredComponents = providerType.GetProperty(k_RequiredComponents)?.GetValue(provider) as Type[] ?? [];
        customFooterActions = providerType.GetProperty(k_FooterActions)?.GetValue(provider) as Type[] ?? [];

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

    public static SDKPlatformProvider TryCreateMultiTargetPlatformProvider(IPlatformProvider provider)
    {
        if (!IsMultiTargetPlatformCompatible(provider, out var error))
        {
            if (IsDerivedPlatformCompatible(provider, out _))
                return null;
            
            Debug.LogError(string.Format(k_MultiTargetPlatformCompatibilityError, provider.GetType().FullName, error));
            return null;
        }

        return new SDKPlatformProvider(provider, SDKPlatformType.MultiTarget);
    }

    public static SDKPlatformProvider TryCreateDerivedPlatformProvider(IPlatformProvider provider)
    {
        if (!IsDerivedPlatformCompatible(provider, out var error))
        {
            if (IsMultiTargetPlatformCompatible(provider, out _))
                return null;

            Debug.LogError(string.Format(k_DerivedPlatformCompatibilityError, provider.GetType().FullName, error));
            return null;
        }

        return new SDKPlatformProvider(provider, SDKPlatformType.Derived);
    }

    static bool IsMultiTargetPlatformCompatible(IPlatformProvider provider, out string error)
    {
        switch (provider.version)
        {
            case 1:
                return IsMultiTargetPlatformCompatibleV1(provider, out error);
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
                return IsDerivedPlatformCompatibleV1(provider, out error);
            default:
                error = FormatUnrecognizedVersionError(provider.version);
                return false;
        }
    }

    static bool IsMultiTargetPlatformCompatibleV1(IPlatformProvider provider, out string error)
    {
        var type = provider.GetType();

        if (!HasRequiredProperty(type, k_Guid, typeof(GUID), out error))
            return false;

        if (!HasRequiredProperty(type, k_TargetName, typeof(string), out error))
            return false;

        if (!HasRequiredProperty(type, k_PlatformDefine, typeof(string), out error))
            return false;

        if (!HasRequiredMethod(type, k_OnMultiTargetPlatformBuildProfileCreated,
            new[] { typeof(BuildProfile) }, typeof(void), out error))
        {
            return false;
        }

        return true;
    }

    static bool IsDerivedPlatformCompatibleV1(IPlatformProvider provider, out string error)
    {
        var type = provider.GetType();

        if (!HasRequiredProperty(type, k_Guid, typeof(GUID), out error))
            return false;

        if (!HasRequiredProperty(type, k_TargetName, typeof(string), out error))
            return false;

        if (!HasRequiredProperty(type, k_PlatformDefine, typeof(string), out error))
            return false;

        if (!HasRequiredMethod(type, k_OnDerivedPlatformBuildProfileCreated,
            new[] { typeof(BuildProfile), typeof(int), typeof(Action<BuildProfile, int>) },
            typeof(void), out error))
        {
            return false;
        }

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

    static bool HasRequiredMethod(Type type, string methodName, Type[] paramTypes, Type returnType, out string error)
    {
        var method = type.GetMethod(methodName, paramTypes);
        if (method == null)
        {
            error = FormatRequiredMethodError(methodName);
            return false;
        }

        if (method.ReturnType != returnType)
        {
            error = FormatMethodReturnTypeError(methodName, returnType);
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

    static string FormatRequiredMethodError(string methodName)
    {
        return string.Format(k_RequiredMethodError, methodName);
    }

    static string FormatMethodReturnTypeError(string methodName, Type expectedType)
    {
        return string.Format(k_MethodReturnTypeError, methodName, expectedType.FullName);
    }
}
