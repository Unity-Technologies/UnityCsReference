// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Build.Profile.Elements
{
    /// <summary>
    /// Build destination settings object stores a build path.
    /// On build, the settings are used to determine where the build will be placed.
    /// </summary>
    class BuildDestinationSettingsProvider : ScriptableObjectSettingsProvider<BuildDestinationSettings>
    {
        public BuildDestinationSettingsProvider() : base(CreateSettingsProvider())
        {
        }

        static BuildProfileSettingsProvider CreateSettingsProvider() => new(TrText.buildDestinationSettings)
        {
            settingsType = typeof(BuildDestinationSettings),
            canAddSetting = _ => true,
            hasCustomEditor = true,
            tooltip = string.Empty
        };
    }
}
