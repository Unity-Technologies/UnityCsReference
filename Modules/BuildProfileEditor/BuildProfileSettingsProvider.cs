// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Created by occurrences of <see cref="BuildProfileSettingsProviderAttribute"/>, contains
    /// display information for a build profile settings section.
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
        /// Foldout tooltip text. If empty, no tooltip is displayed.
        /// </summary>
        public string tooltip { get; set; } = string.Empty;

        /// <summary>
        /// When true, indicates that a custom implementation of <see cref="Editor"/> should be used.
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
        /// added to the given build profile.
        /// </summary>
        public Func<BuildProfile, bool> canAddSetting { get; set; }

        internal Type settingsType { get; set; }

        public BuildProfileSettingsProvider(string displayName)
        {
            this.displayName = displayName;
        }
    }
}
