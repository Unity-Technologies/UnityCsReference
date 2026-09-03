// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.Build.Profile
{
    public partial class BuildProfile
    {
        [Serializable]
        internal class PlayerSettingsYaml
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
                string prevLine = "";
                foreach (var setting in settings)
                {
                    // When the } is on the second line, we should join the two lines.
                    // Otherwise, we will break the serialization for the object by adding
                    // the '-line' in front of the second line that when deserialized, it does not
                    // know how to parse it.
                    if (setting.Contains("{") && !setting.Contains("}"))
                    {
                        prevLine = setting;
                        continue;
                    }

                    if (!string.IsNullOrEmpty(prevLine))
                    {
                        if (setting.Contains("}"))
                        {
                            m_Settings.Add(new YamlSetting(prevLine + setting));
                        }
                        else
                        {
                            Debug.LogWarning("a { has no closing } on the second line. Invalid Serialization.");
                        }
                        prevLine = "";
                        continue;
                    }

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

        [AutoStaticsCleanupOnCodeReload]
        static PlayerSettings s_GlobalPlayerSettings;

        [AutoStaticsCleanupOnCodeReload]
        static readonly List<PlayerSettings> s_LoadedPlayerSettings = new();

        internal void CreatePlayerSettingsFromGlobal()
        {
            if (m_PlayerSettings != null || BuildProfileContext.IsClassicPlatformProfile(this))
                return;

            // Create BuildProfilePlayerSettings subasset with a copy of global settings
            if (buildProfilePlayerSettings == null)
            {
                buildProfilePlayerSettings = new BuildProfilePlayerSettings();
                buildProfilePlayerSettings.name = "Player Settings";
                buildProfilePlayerSettings.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                PlayerSettings.CopySettingsToBuildProfilePlayerSettings(buildProfilePlayerSettings);

                if (EditorUtility.IsPersistent(this))
                {
                    AssetDatabase.AddObjectToAsset(buildProfilePlayerSettings, this);
                    EditorUtility.SetDirty(this);
                }
            }

            // Create PlayerSettings wrapper that proxies to the subasset
            m_PlayerSettings = PlayerSettings.CreateAsBuildProfileOverride(buildProfilePlayerSettings);
            s_LoadedPlayerSettings.Add(m_PlayerSettings);

            // Mark the profile dirty so the serialized YAML is persisted to disk.
            // Without this, the asset is not flagged for save and a subsequent
            // AssetDatabase.CopyAsset (Duplicate) would copy a stale on-disk file
            // that does not include the player settings the user just added.
            EditorUtility.SetDirty(this);

            UpdateGlobalManagerPlayerSettings();
        }

        internal void RemovePlayerSettings(bool clearYaml = false)
        {
            if (BuildProfileContext.IsClassicPlatformProfile(this))
                return;

            UpdateGlobalManagerPlayerSettings(activeWillBeRemoved: true);

            if (m_PlayerSettings != null)
            {
                var toDestroy = m_PlayerSettings;

                m_PlayerSettings = null;

                DestroyImmediate(toDestroy, true);
                s_LoadedPlayerSettings.Remove(toDestroy);

                if (clearYaml)
                    m_PlayerSettingsYaml.Clear();
            }

            BuildProfileModuleUtil.UpdateActiveEditors(this);
        }

        internal void RemoveBuildProfilePlayerSettings()
        {
            RemovePlayerSettings(clearYaml: true);

            if (buildProfilePlayerSettings != null)
            {
                if (AssetDatabase.Contains(buildProfilePlayerSettings))
                    AssetDatabase.RemoveObjectFromAsset(buildProfilePlayerSettings);
                UnityEngine.Object.DestroyImmediate(buildProfilePlayerSettings, true);
                buildProfilePlayerSettings = null;
            }
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

        internal bool HasSerializedPlayerSettings()
        {
            return buildProfilePlayerSettings != null || m_PlayerSettingsYaml.HasSettings();
        }

        internal void UpdateGlobalManagerPlayerSettings(bool activeWillBeRemoved = false)
        {
            if (BuildProfileContext.activeProfile != this)
                return;

            if (HasSerializedPlayerSettings() && !activeWillBeRemoved)
            {
                PlayerSettings.SetOverridePlayerSettingsInternal(m_PlayerSettings);
            }
            else
            {
                TryLoadProjectSettingsAssetPlayerSettings();
                PlayerSettings.SetOverridePlayerSettingsInternal(s_GlobalPlayerSettings);
            }
        }

        internal static void TrySetProjectSettingsAssetAsGlobalManagerPlayerSettings()
        {
            if (BuildProfileContext.activeProfile != null)
                return;

            TryLoadProjectSettingsAssetPlayerSettings();
            if (!PlayerSettings.IsGlobalManagerPlayerSettings(s_GlobalPlayerSettings))
                PlayerSettings.SetOverridePlayerSettingsInternal(s_GlobalPlayerSettings);
        }

        static void TryLoadProjectSettingsAssetPlayerSettings()
        {
            if (s_GlobalPlayerSettings is not null)
                return;

            s_GlobalPlayerSettings= PlayerSettings.GetProjectSettingsPlayerSettings();
            if (s_GlobalPlayerSettings is null)
                Debug.LogError("[BuildProfile] Global Player Settings instance returned null.");
        }

        internal static PlayerSettings GetGlobalPlayerSettings()
        {
            return s_GlobalPlayerSettings;
        }
    }
}
