// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Build Profile settings providers map to setting section in
    /// the editor. Stored as a singleton, each provider describes
    /// interaction between the editor and a settings object.
    /// </summary>
    interface IBuildProfileSettingsProvider
    {
        /// <summary>
        /// Translated display name for the settings provider.
        /// </summary>
        public string GetDisplayName();

        /// <summary>
        /// Foldout tooltip text. If empty, no tooltip is displayed.
        /// </summary>
        public string GetTooltip();

        /// <summary>
        /// Returns true if the given profile contains the settings
        /// described by this provider.
        /// </summary>
        public bool HasSettings(BuildProfile profile);

        /// <summary>
        /// Invoked when a new settings section is added to the profile through the UI.
        /// </summary>
        public void OnAdd(BuildProfile profile);

        /// <summary>
        /// Invoked on remove option clicked in the UI.
        /// </summary>
        /// <param name="profile"></param>
        public void OnRemove(BuildProfile profile);

        /// <summary>
        /// Optional reset action invoked when the user clicks the reset button.
        /// </summary>
        public Action<BuildProfile> GetResetAction();

        /// <summary>
        /// Creates the GUI for the settings section; <see cref="BuildProfileSettingsFoldout"/>.
        /// </summary>
        public VisualElement CreateInspectorGUI(BuildProfile profile, SerializedObject serializedObject);
    }
}
