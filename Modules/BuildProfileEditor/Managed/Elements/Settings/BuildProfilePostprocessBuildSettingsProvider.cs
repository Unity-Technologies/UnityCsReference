// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Build.Profile.Elements
{
    /// <summary>
    /// Post-process build settings object stores the per-profile post-build callbacks.
    /// After a build that used the profile, the callbacks are invoked.
    /// </summary>
    class BuildProfilePostprocessBuildSettingsProvider : ScriptableObjectSettingsProvider<BuildProfilePostprocessBuild>
    {
        public BuildProfilePostprocessBuildSettingsProvider() : base(CreateSettingsProvider())
        {
        }

        static BuildProfileSettingsProvider CreateSettingsProvider() => new(TrText.postprocessBuildSettings)
        {
            settingsType = typeof(BuildProfilePostprocessBuild),
            canAddSetting = _ => true,
            hasCustomEditor = true,
            tooltip = TrText.postprocessBuildTooltip
        };
    }
}
