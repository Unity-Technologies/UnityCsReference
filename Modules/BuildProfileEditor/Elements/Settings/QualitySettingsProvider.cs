// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile.Elements
{
    /// <summary>
    /// Quality settings object stores a list of quality names specified in Project Settings > Quality.
    /// On build, the settings are baked into the player.
    /// </summary>
    class QualitySettingsProvider : IBuildProfileSettingsProvider
    {
        public string GetDisplayName() => TrText.qualitySettings;

        public string GetTooltip() => string.Empty;

        public bool HasSettings(BuildProfile profile) => profile.qualitySettings != null;

        public void OnAdd(BuildProfile profile)
        {
            BuildProfileModuleUtil.CreateQualitySettings(profile);
            profile.ResetToGlobalQualitySettingsValues();
        }

        public void OnRemove(BuildProfile profile)
        {
            BuildProfileModuleUtil.RemoveQualitySettings(profile);
        }

        public Action<BuildProfile> GetResetAction() => OnReset;

        public VisualElement CreateInspectorGUI(BuildProfile profile, SerializedObject serializedObject)
        {
            return new EditorAsVisualElement(profile.qualitySettings, true);
        }

        void OnReset(BuildProfile profile) => profile.ResetToGlobalQualitySettingsValues();
    }
}
