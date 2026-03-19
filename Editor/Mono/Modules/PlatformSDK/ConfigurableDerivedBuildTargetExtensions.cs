// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Build.Profile;
using UnityEngine.UIElements;
using static UnityEditor.Modules.DerivedBuildTargetExtensionsProvider;

namespace UnityEditor.Modules;

internal class ConfigurableDerivedBuildTargetExtensions : IDerivedBuildTargetExtensions
{
    SDKPlatformProvider m_SDKPlatformProvider;
    CreateBuildProfileExtensionFunction m_CreateBaseBuildProfileExtensionFunction;

    public ICompilationExtension CompilationExtension { get; }
    public IBuildPostprocessor BuildPostprocessor { get; }
    public IDerivedBuildTarget DerivedBuildTarget { get; }

    public IBuildProfileExtension CreateBuildProfileExtension()
    {
        return new ConfigurableBuildProfileExtension(m_CreateBaseBuildProfileExtensionFunction.Invoke(), m_SDKPlatformProvider);
    }

    public ISettingEditorExtension CreateSettingEditorExtension() => null;

    public ConfigurableDerivedBuildTargetExtensions(SDKPlatformProvider sdkPlatformProvider, IDerivedBuildTarget derivedBuildTarget, CreateBuildProfileExtensionFunction createBaseBuildProfileExtensionFunction)
    {
        m_SDKPlatformProvider = sdkPlatformProvider;
        m_CreateBaseBuildProfileExtensionFunction = createBaseBuildProfileExtensionFunction;
        CompilationExtension = new ConfigurableCompilationExtension(sdkPlatformProvider.platformDefine, Array.Empty<string>(), Array.Empty<string>());
        BuildPostprocessor = null;
        DerivedBuildTarget = derivedBuildTarget;
    }
}

internal class ConfigurableCompilationExtension : DefaultCompilationExtension
{
    List<string> m_AdditionalDefines = new();
    List<string> m_AdditionalEditorDefines = new();

    public override string[] GetAdditionalDefines() => m_AdditionalDefines.ToArray();
    public override string[] GetAdditionalEditorDefines() => m_AdditionalEditorDefines.ToArray();

    public ConfigurableCompilationExtension(string platformDefine, string[] additionalDefines, string[] additionalEditorDefines)
    {
        m_AdditionalDefines.Add(platformDefine);
        m_AdditionalDefines.AddRange(additionalDefines);
        m_AdditionalEditorDefines.Add(platformDefine);
        m_AdditionalEditorDefines.AddRange(additionalEditorDefines);
    }
}

internal class ConfigurableBuildProfileExtension : IBuildProfileExtension
{
    IBuildProfileExtension m_BaseBuildProfileExtension;
    SDKPlatformProvider m_SDKPlatformProvider;

    public BuildProfilePlatformSettingsBase CreateBuildProfilePlatformSettings() =>
        m_BaseBuildProfileExtension.CreateBuildProfilePlatformSettings();

    public VisualElement CreateSettingsGUI(SerializedObject serializedObject, SerializedProperty rootProperty, BuildProfileWorkflowState state) =>
        m_BaseBuildProfileExtension.CreateSettingsGUI(serializedObject, rootProperty, state);

    public VisualElement CreatePlatformBuildWarningsGUI(SerializedObject serializedObject, SerializedProperty rootProperty) =>
        m_BaseBuildProfileExtension.CreatePlatformBuildWarningsGUI(serializedObject, rootProperty);

    public void CopyPlatformSettingsToBuildProfile(BuildProfilePlatformSettingsBase platformSettingsBase) =>
        m_BaseBuildProfileExtension.CopyPlatformSettingsToBuildProfile(platformSettingsBase);

    public void CopyPlatformSettingsFromBuildProfile(BuildProfilePlatformSettingsBase platformSettings) =>
        m_BaseBuildProfileExtension.CopyPlatformSettingsFromBuildProfile(platformSettings);

    public string GetProfileInfoMessage() =>
        m_BaseBuildProfileExtension.GetProfileInfoMessage();

    public PreconfiguredSettingsVariant[] GetPreconfiguredSettingsVariants() =>
        m_BaseBuildProfileExtension.GetPreconfiguredSettingsVariants();

    public void OnBuildProfileCreated(BuildProfile buildProfile, int preconfiguredSettingsVariant) =>
        m_SDKPlatformProvider.onDerivedPlatformBuildProfileCreated?.Invoke(buildProfile, preconfiguredSettingsVariant, m_BaseBuildProfileExtension.OnBuildProfileCreated);

    public void OnDisable() =>
        m_BaseBuildProfileExtension.OnDisable();

    public ConfigurableBuildProfileExtension(IBuildProfileExtension baseBuildProfileExtension, SDKPlatformProvider sdkPlatformProvider)
    {
        m_BaseBuildProfileExtension = baseBuildProfileExtension;
        m_SDKPlatformProvider = sdkPlatformProvider;
    }
}
