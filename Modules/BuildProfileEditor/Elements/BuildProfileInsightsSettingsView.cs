// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.InsightsEditor;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile.Elements;

class BuildProfileInsightsSettingsView
{
    internal static void CreateGUI(BuildProfile buildProfile, VisualElement root, bool isClassicPlatformSettingsMode)
    {
        _ = BuildTargetDiscovery.TryGetProperties<IInsightsPlatformProperties>(
            buildProfile.buildTarget,
            out var buildTargetProperties);
        if (buildTargetProperties == null)
        {
            return;
        }

        var buildProfileInsightsSettingsVisualElement = new BuildProfileInsightsSettingsVisualElement
        {
            buildProfileName = buildProfile.name,
            buildProfile = buildProfile
        };

        if (!BuildProfileContext.IsClassicPlatformProfile(buildProfile))
        {
            buildProfileInsightsSettingsVisualElement.RegisterSaveAction(() => EditorUtility.SetDirty(buildProfile));
        }

        var projectSettingsValue = EngineDiagnostics.EngineDiagnosticsSettings.GetEngineDiagnosticsEnabledDefaultBuildValue();
        buildProfileInsightsSettingsVisualElement.UpdateSavedProjectSettingsEngineDiagnosticsEnabledValue(projectSettingsValue);

        InsightsEditorUtils.OnEngineDiagnosticsEnabledChanged += buildProfileInsightsSettingsVisualElement.OnProjectSettingsEngineDiagnosticsEnabledChanged;

        buildProfileInsightsSettingsVisualElement.InitializeGUI();

        var insightsSectionContainer = root.Q<VisualElement>("insights-analytics-base-root");
        insightsSectionContainer.Add(buildProfileInsightsSettingsVisualElement);
    }
}
