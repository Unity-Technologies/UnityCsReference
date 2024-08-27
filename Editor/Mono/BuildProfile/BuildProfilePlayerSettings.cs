// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace UnityEditor.Build.Profile
{
    public partial class BuildProfile
    {
        [Serializable]
        class PlayerSettingsYaml
        {
            [Serializable]
            class YamlSetting
            {
                public string line;

                public YamlSetting(string newLine)
                {
                    // Prefixing the YAML property value with '|' to escape special characters
                    // and avoid 'Failed to parse' yaml error
                    line = $"{"| "}{newLine}";
                }

                public string GetLine()
                {
                    return line[2..];
                }
            }

            [SerializeField]
            List<YamlSetting> m_Settings = new();

            internal void SetSettingsFromYaml(string yamlStr)
            {
                m_Settings.Clear();

                // Splitting the YAML single string into individual lines to better readability
                // in the asset file
                var settings = yamlStr.Split("\n");
                foreach (var setting in settings)
                {
                    var newSetting = new YamlSetting(setting);
                    m_Settings.Add(newSetting);
                }
            }

            internal string GetYamlString()
            {
                var stringBuilder = new StringBuilder();
                foreach (var setting in m_Settings)
                {
                    stringBuilder.AppendLine(setting.GetLine());
                }
                return stringBuilder.ToString();
            }

            internal bool HasSettings()
            {
                return m_Settings.Count > 0;
            }

            internal void Clear()
            {
                m_Settings.Clear();
            }
        }

        const string k_ProjectSettingsAssetPath = "ProjectSettings/ProjectSettings.asset";

        static PlayerSettings s_GlobalPlayerSettings;

        static readonly List<PlayerSettings> s_LoadedPlayerSettings = new();

        internal void LoadPlayerSettings()
        {
            TryLoadProjectSettingsAssetPlayerSettings();
            DeserializePlayerSettings();
        }

        internal void UpdatePlayerSettingsObjectFromYAML()
        {
            if (!HasSerializedPlayerSettings())
                return;

            PlayerSettings.UpdatePlayerSettingsObjectFromYAML(playerSettings, m_PlayerSettingsYaml.GetYamlString());
            OnPlayerSettingsUpdatedFromYAML?.Invoke();
        }

        internal void CreatePlayerSettingsFromGlobal()
        {
            if (m_PlayerSettings != null || BuildProfileContext.IsClassicPlatformProfile(this))
                return;

            var newPlayerSettings = Instantiate(s_GlobalPlayerSettings);

            var yamlStr = PlayerSettings.SerializeAsYAMLString(newPlayerSettings);
            m_PlayerSettingsYaml.SetSettingsFromYaml(yamlStr);
            m_PlayerSettings = newPlayerSettings;
            s_LoadedPlayerSettings.Add(m_PlayerSettings);

            UpdateGlobalManagerPlayerSettings();
        }

        internal void RemovePlayerSettings(bool clearYaml = false)
        {
            if (BuildProfileContext.IsClassicPlatformProfile(this))
                return;

            UpdateGlobalManagerPlayerSettings(activeWillBeRemoved: true);

            if (m_PlayerSettings != null)
            {
                DestroyImmediate(m_PlayerSettings, true);
                s_LoadedPlayerSettings.Remove(m_PlayerSettings);
                m_PlayerSettings = null;

                if (clearYaml)
                    m_PlayerSettingsYaml.Clear();
            }

            OnPlayerSettingsUpdatedFromYAML?.Invoke();
        }

        internal static void CleanUpPlayerSettingsForDeletedBuildProfiles(IList<BuildProfile> currentBuildProfiles)
        {
            TrySetProjectSettingsAssetAsGlobalManagerPlayerSettings();

            for (int i = s_LoadedPlayerSettings.Count - 1; i >= 0; i--)
            {
                var loadedPlayerSettings = s_LoadedPlayerSettings[i];
                if (loadedPlayerSettings == null)
                {
                    s_LoadedPlayerSettings.RemoveAt(i);
                    continue;
                }

                bool shouldDelete = true;
                foreach (var profile in currentBuildProfiles)
                {
                    if (profile.playerSettings == loadedPlayerSettings)
                    {
                        shouldDelete = false;
                        break;
                    }
                }

                if (shouldDelete)
                {
                    s_LoadedPlayerSettings.RemoveAt(i);
                    DestroyImmediate(loadedPlayerSettings, true);
                }
            }
        }

        internal void SerializePlayerSettings()
        {
            if (m_PlayerSettings == null)
                return;

            var yamlStr = PlayerSettings.SerializeAsYAMLString(m_PlayerSettings);
            m_PlayerSettingsYaml.SetSettingsFromYaml(yamlStr);
        }

        internal void DeserializePlayerSettings()
        {
            if (!HasSerializedPlayerSettings())
                return;

            if (m_PlayerSettings == null)
                m_PlayerSettings = PlayerSettings.DeserializeFromYAMLString(m_PlayerSettingsYaml.GetYamlString());
            else
                UpdatePlayerSettingsObjectFromYAML();
            s_LoadedPlayerSettings.Add(m_PlayerSettings);
            UpdateGlobalManagerPlayerSettings();
        }

        internal bool HasSerializedPlayerSettings()
        {
            return m_PlayerSettingsYaml.HasSettings();
        }

        internal void UpdateGlobalManagerPlayerSettings(bool activeWillBeRemoved = false)
        {
            if (BuildProfileContext.instance.activeProfile != this)
                return;

            var playerSettings = (HasSerializedPlayerSettings() && !activeWillBeRemoved) ? m_PlayerSettings : s_GlobalPlayerSettings;
            PlayerSettings.SetOverridePlayerSettingsInternal(playerSettings);
        }

        internal static void TrySetProjectSettingsAssetAsGlobalManagerPlayerSettings()
        {
            if (BuildProfileContext.instance.activeProfile != null)
                return;

            TryLoadProjectSettingsAssetPlayerSettings();
            if (!PlayerSettings.IsGlobalManagerPlayerSettings(s_GlobalPlayerSettings))
                PlayerSettings.SetOverridePlayerSettingsInternal(s_GlobalPlayerSettings);
        }

        internal static bool IsDataEqualToProjectSettings(PlayerSettings targetPlayerSettings)
        {
            var projectSettingsYaml = PlayerSettings.SerializeAsYAMLString(s_GlobalPlayerSettings);
            var targetSettingsYaml = PlayerSettings.SerializeAsYAMLString(targetPlayerSettings);
            return projectSettingsYaml == targetSettingsYaml;
        }

        static void TryLoadProjectSettingsAssetPlayerSettings()
        {
            if (s_GlobalPlayerSettings == null)
                s_GlobalPlayerSettings = AssetDatabase.LoadAssetAtPath<PlayerSettings>(k_ProjectSettingsAssetPath);
        }

        internal static PlayerSettings GetGlobalPlayerSettings()
        {
            return s_GlobalPlayerSettings;
        }
    }
}
