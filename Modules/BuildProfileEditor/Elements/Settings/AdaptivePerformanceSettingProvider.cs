// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.AdaptivePerformance.UI.Editor;
using UnityEngine;
using UnityEngine.AdaptivePerformance;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile.AdaptivePerformance;

internal class AdaptivePerformanceSettingProvider : IBuildProfileSettingsProvider
{
    public string GetDisplayName() => BuildProfileAdaptivePerformanceToggle.adaptivePerformanceLabelText;
    public string GetTooltip() => string.Empty;

    public bool CanAddSettings(BuildProfile profile)
    {
        return BuildProfileModuleUtil.IsModuleInstalled(profile.platformGuid) && profile.platformBuildProfile != null && profile.GetComponent<AdaptivePerformanceGeneralSettings>() == null;
    }

    public bool HasSettings(BuildProfile profile)
    {
        return profile.platformBuildProfile?.adaptivePerformanceEnabled == true || profile.GetComponent<AdaptivePerformanceGeneralSettings>() != null;
    }

    public void OnAdd(BuildProfile profile)
    {
        if (profile == null || profile.platformBuildProfile == null) return;
        profile.platformBuildProfile.adaptivePerformanceEnabled = true;
    }

    public void OnRemove(BuildProfile profile)
    {
        if (profile == null || profile.platformBuildProfile == null) return;
        profile.platformBuildProfile.adaptivePerformanceEnabled = false;
        BuildProfileAdaptivePerformanceToggle.RemoveAllSettingsFromBuildProfile(profile);
    }

    public Action<BuildProfile> GetResetAction() => OnReset;

    static void OnReset(BuildProfile profile)
    {
        if (profile == null || profile.platformBuildProfile == null) return;
        profile.platformBuildProfile.adaptivePerformanceEnabled = true; // When AP is disabled and reset, it should be enabled again
        BuildProfileAdaptivePerformanceToggle.RemoveAllSettingsFromBuildProfile(profile);
    }

    public VisualElement CreateInspectorGUI(BuildProfile profile, SerializedObject serializedObject)
    {
        return new BuildProfileAdaptivePerformanceToggle(profile);
    }
}
