// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Defines the display information and behavior for a custom settings foldout in
    /// the Build Profile window. Use with <see cref="BuildProfileSettingsProviderAttribute"/>
    /// to register a settings foldout for a given <see cref="UnityEngine.ScriptableObject"/> settings type.
    /// </summary>
    public class BuildProfileSettingsProvider
    {
        /// <summary>
        /// Display name for the settings foldout in the build profile editor.
        /// </summary>
        public string displayName { get; set; }

        /// <summary>
        /// Returns the display order for the settings provider.
        /// </summary>
        internal int displayOrder { get; set; } = 0;

        /// <summary>
        /// Settings foldout tooltip text. If empty, no tooltip is displayed.
        /// </summary>
        public string tooltip { get; set; } = string.Empty;

        /// <summary>
        /// When true, Unity uses a custom <see cref="Editor"/> implementation for this settings foldout.
        /// Otherwise, the default inspector for the settings object is displayed.
        /// </summary>
        public bool hasCustomEditor { get; set; } = false;

        /// <summary>
        /// Returns true if the setting is marked as required
        /// by the provider. A required setting will appear in
        /// every build profile it is valid for as  defined by
        /// CanAddSettings.
        /// </summary>
        internal bool isRequired { get; set; } = false;

        /// <summary>
        /// Callback determining if the settings object described by this provider can be
        /// added to the given build profile. Must be set for settings to appear in the
        /// build profile editor; when not set, the settings will not be shown.
        /// </summary>
        public Func<BuildProfile, bool> canAddSetting { get; set; }

        internal Type settingsType { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="BuildProfileSettingsProvider"/> with the specified display name.
        /// </summary>
        /// <param name="displayName">Display name for the settings foldout in the Build Profile window.</param>
        public BuildProfileSettingsProvider(string displayName)
        {
            this.displayName = displayName;
        }
    }
}
