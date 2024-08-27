// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Presets;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Editor class responsible for visualizing the player settings of a <see cref="BuildProfile"/>.
    /// </summary>
    internal class BuildProfilePlayerSettingsEditor
    {
        const string k_PlayerSettingsRoot = "editor-player-settings";
        const string k_PlayerSettingsFoldout = "player-settings-foldout";
        const string k_PlayerSettingsHelpBox = "custom-player-settings-info-helpbox";
        const string k_PlayerSettingsHelpBoxButton = "custom-player-settings-info-helpbox-button";
        const string k_PlayerSettingsLabel = "player-settings-label";
        const string k_PlayerSettingsOptions = "player-settings-options";
        const string k_ProjectSettingsPath = "ProjectSettings/ProjectSettings.asset";

        Foldout m_PlayerSettingsFoldout;

        Label m_PlayerSettingsLabel;

        Button m_PlayerSettingsInfoButton;
        Button m_PlayerSettingsOptions;

        HelpBox m_PlayerSettingsHelpBox;

        InspectorElement m_PlayerSettingsInspector;

        PlayerSettingsEditor m_PlayerSettingsEditor;

        BuildProfile m_Profile;
        SerializedObject m_ProfileSerializedObject;

        bool m_PlayerSettingsYamlUpdated = false;

        internal static BuildProfilePlayerSettingsEditor CreatePlayerSettingsUI(VisualElement root, SerializedObject buildProfileSerializedObject)
        {
            var buildProfilePlayerSettingsEditor = new BuildProfilePlayerSettingsEditor();
            buildProfilePlayerSettingsEditor.InitializeVisualElements(root);

            if (buildProfileSerializedObject == null)
            {
                buildProfilePlayerSettingsEditor.HidePlayerSettingsUI();
                return buildProfilePlayerSettingsEditor;
            }

            if (buildProfileSerializedObject.targetObject is not BuildProfile buildProfile)
                throw new InvalidOperationException("Editor object is not of type BuildProfile.");

            buildProfilePlayerSettingsEditor.m_Profile = buildProfile;
            buildProfilePlayerSettingsEditor.m_ProfileSerializedObject = buildProfileSerializedObject;
            buildProfilePlayerSettingsEditor.m_PlayerSettingsLabel.text = TrText.playerSettingsLabelText;

            if (BuildProfileModuleUtil.HasSerializedPlayerSettings(buildProfilePlayerSettingsEditor.m_Profile))
                buildProfilePlayerSettingsEditor.ShowPlayerSettingsEditor();
            else
                buildProfilePlayerSettingsEditor.ShowPlayerSettingsHelpBox();

            return buildProfilePlayerSettingsEditor;
        }

        internal void EditorUpdate()
        {
            if (m_PlayerSettingsYamlUpdated)
            {
                RemovePlayerSettingsInspector();

                if (m_Profile.playerSettings == null)
                    ShowPlayerSettingsHelpBox();
                else
                    ShowPlayerSettingsEditor();

                m_PlayerSettingsYamlUpdated = false;
            }
        }

        void InitializeVisualElements(VisualElement root)
        {
            var playerSettingsRoot = root.Q<VisualElement>(k_PlayerSettingsRoot);
            m_PlayerSettingsFoldout = playerSettingsRoot.Q<Foldout>(k_PlayerSettingsFoldout);
            m_PlayerSettingsHelpBox = playerSettingsRoot.Q<HelpBox>(k_PlayerSettingsHelpBox);
            m_PlayerSettingsInfoButton = playerSettingsRoot.Q<Button>(k_PlayerSettingsHelpBoxButton);
            m_PlayerSettingsLabel = playerSettingsRoot.Q<Label>(k_PlayerSettingsLabel);
            m_PlayerSettingsOptions = playerSettingsRoot.Q<Button>(k_PlayerSettingsOptions);

            m_PlayerSettingsFoldout.text = TrText.playerSettings;
            playerSettingsRoot.Show();
        }

        void HidePlayerSettingsUI()
        {
            m_PlayerSettingsFoldout.Hide();
            m_PlayerSettingsLabel.Hide();
            m_PlayerSettingsHelpBox.Hide();
            m_PlayerSettingsInfoButton.Hide();
            HidePlayerSettingsEditor();
        }

        internal void ShowPlayerSettingsEditor()
        {
            m_PlayerSettingsHelpBox.Hide();

            bool createPlayerSettings = m_Profile.playerSettings == null;

            if (createPlayerSettings)
            {
                BuildProfileModuleUtil.CreatePlayerSettingsFromGlobal(m_Profile);
                UpdateBuildProfile();
            }

            CreatePlayerSettingsInspector();

            if (createPlayerSettings && m_PlayerSettingsEditor.CopyProjectSettingsToPlayerSettingsExtension())
                UpdateBuildProfile();

            m_PlayerSettingsOptions.clicked += PlayerSettingsOptionMenu;
            m_PlayerSettingsOptions.Show();
            m_PlayerSettingsFoldout.Show();

            m_Profile.OnPlayerSettingsUpdatedFromYAML -= OnPlayerSettingsUpdatedFromYAML;
            m_Profile.OnPlayerSettingsUpdatedFromYAML += OnPlayerSettingsUpdatedFromYAML;
        }

        void HidePlayerSettingsEditor()
        {
            m_PlayerSettingsOptions.Hide();
            RemovePlayerSettingsInspector();
        }

        void CreatePlayerSettingsInspector()
        {
            if (m_PlayerSettingsEditor == null)
            {
                var isActiveProfile = BuildProfile.GetActiveBuildProfile() == m_Profile;
                m_PlayerSettingsEditor = Editor.CreateEditor(m_Profile.playerSettings) as PlayerSettingsEditor;
                m_PlayerSettingsEditor.ConfigurePlayerSettingsForBuildProfile(m_ProfileSerializedObject, m_Profile.moduleName, m_Profile.subtarget == StandaloneBuildSubtarget.Server, isActiveProfile);
            }

            if (m_PlayerSettingsInspector == null)
            {
                m_PlayerSettingsInspector = new InspectorElement(m_PlayerSettingsEditor);
                m_PlayerSettingsInspector.style.flexGrow = 1;
                m_PlayerSettingsInspector.TrackSerializedObjectValue(m_PlayerSettingsEditor.serializedObject, OnPlayerSettingsEditorChanged);
                m_PlayerSettingsFoldout.Add(m_PlayerSettingsInspector);
            }
        }

        internal void RemovePlayerSettingsInspector()
        {
            if (m_PlayerSettingsEditor != null)
                UnityEngine.Object.DestroyImmediate(m_PlayerSettingsEditor);

            if (m_PlayerSettingsInspector != null && m_PlayerSettingsFoldout.Contains(m_PlayerSettingsInspector))
                m_PlayerSettingsFoldout.Remove(m_PlayerSettingsInspector);

            m_PlayerSettingsInspector = null;
            m_PlayerSettingsEditor = null;
        }

        void OnPlayerSettingsEditorChanged(SerializedObject playerSettingsSerializedObject)
        {
            playerSettingsSerializedObject.ApplyModifiedProperties();
            UpdateBuildProfile();
        }

        void OnPlayerSettingsUpdatedFromYAML()
        {
            m_PlayerSettingsYamlUpdated = true;
        }

        void ShowPlayerSettingsHelpBox()
        {
            m_PlayerSettingsFoldout.Hide();
            m_PlayerSettingsOptions.Hide();
            m_PlayerSettingsHelpBox.text = TrText.playerSettingsInfo;
            m_PlayerSettingsInfoButton.text = TrText.customizePlayerSettingsButton;
            m_PlayerSettingsInfoButton.clicked += ShowPlayerSettingsEditor;
            m_PlayerSettingsLabel.Show();
            m_PlayerSettingsHelpBox.Show();
            m_PlayerSettingsInfoButton.Show();
        }

        void PlayerSettingsOptionMenu()
        {
            bool isDataSameAsProjSettings = BuildProfileModuleUtil.IsDataEqualToProjectSettings(m_Profile.playerSettings);
            isDataSameAsProjSettings = isDataSameAsProjSettings && m_PlayerSettingsEditor.IsPlayerSettingsExtensionDataEqualToProjectSettings();
            var menu = new GenericMenu();
            menu.AddItem(TrText.playerSettingsRemove, false, RemovePlayerSettings);
            menu.AddItem(TrText.playerSettingsReset, false, isDataSameAsProjSettings ? null : ResetToProjectSettingsValues);
            menu.ShowAsContext();
        }

        void RemovePlayerSettings()
        {
            bool removePlayerSettings = EditorUtility.DisplayDialog(TrText.removePlayerSettingsDialogTitle,
                                                                    TrText.removePlayerSettingsDialogMessage,
                                                                    TrText.playerSettingsContinue,
                                                                    TrText.playerSettingsCancel);
            if (!removePlayerSettings)
                return;

            var targetName = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(m_Profile.buildTarget));
            var customScriptingDefines = PlayerSettings.GetScriptingDefineSymbols(targetName);
            var customAdditionalCompilerArguments = PlayerSettings.GetAdditionalCompilerArguments(targetName);

            // we only want to show the restart editor prompt when making changes to an active profile
            // otherwise we continue normally with removing the player settings
            if (m_Profile == BuildProfileContext.instance.activeProfile)
            {
                BuildTarget currentBuildTarget = m_Profile.buildTarget;
                BuildTarget nextBuildTarget = m_Profile.buildTarget;
                // success is we either found no settings to restart or we did and the user agreed to restart the editor
                // failure here is if we found settings requiring restart but the user declined to cancel
                // so we don't continue with the action
                var isSuccess =
                    BuildProfileModuleUtil.HandlePlayerSettingsRequiringRestart(m_Profile, null, currentBuildTarget,
                        nextBuildTarget);
                if (!isSuccess)
                {
                    return;
                }
            }

            HidePlayerSettingsEditor();
            BuildProfileModuleUtil.RemovePlayerSettings(m_Profile);
            UpdateBuildProfile();
            CheckPropertiesThatRequireRecompilation(targetName, customScriptingDefines, customAdditionalCompilerArguments);

        }

        void ResetToProjectSettingsValues()
        {
            bool resetPlayerSettings = EditorUtility.DisplayDialog(TrText.resetPlayerSettingsDialogTitle,
                                                                    TrText.resetPlayerSettingsDialogMessage,
                                                                    TrText.playerSettingsContinue,
                                                                    TrText.playerSettingsCancel);

            if (!resetPlayerSettings)
                return;

            // we only want to show the restart editor prompt when making changes to an active profile
            // otherwise we continue normally with resetting the values to project settings
            if (m_Profile == BuildProfileContext.instance.activeProfile)
            {
                BuildTarget currentBuildTarget = m_Profile.buildTarget;
                BuildTarget nextBuildTarget = m_Profile.buildTarget;

                // we should check if the player setting overrides we're updating differ from the project settings
                // in that case wes should check for any setting that requires an editor restart to take effect
                // if it does, we should a restart prompt. if the user cancels, we cancel the resetting action

                var isSuccess =
                    BuildProfileModuleUtil.HandlePlayerSettingsRequiringRestart(m_Profile, null, currentBuildTarget,
                        nextBuildTarget);
                if (!isSuccess)
                {
                    return;
                }
            }

            var targetName = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(m_Profile.buildTarget));
            var customScriptingDefines = PlayerSettings.GetScriptingDefineSymbols(targetName);
            var customAdditionalCompilerArguments = PlayerSettings.GetAdditionalCompilerArguments(targetName);

            var playerSettings = AssetDatabase.LoadAssetAtPath<PlayerSettings>(k_ProjectSettingsPath);
            var preset = new Preset(playerSettings);
            preset.ApplyTo(m_Profile.playerSettings);
            m_PlayerSettingsEditor.CopyProjectSettingsToPlayerSettingsExtension();

            UpdateBuildProfile();

            RemovePlayerSettingsInspector();
            CreatePlayerSettingsInspector();
            CheckPropertiesThatRequireRecompilation(targetName, customScriptingDefines, customAdditionalCompilerArguments);
        }

        void CheckPropertiesThatRequireRecompilation(NamedBuildTarget targetName, string customScriptingDefines, string[] customAdditionalCompilerArguments)
        {
            if (BuildProfile.GetActiveBuildProfile() != m_Profile)
                return;

            // Check if current global player settings that we switched to has different script defines
            // when compared to the custom player settings
            var additionalCompilerArguments = PlayerSettings.GetAdditionalCompilerArguments(targetName);
            var scriptingDefines = PlayerSettings.GetScriptingDefineSymbols(targetName);
            if (customScriptingDefines != scriptingDefines || !ArrayUtility.ArrayEquals(customAdditionalCompilerArguments, additionalCompilerArguments))
                BuildProfileModuleUtil.RequestScriptCompilation(m_Profile);
        }

        void UpdateBuildProfile()
        {
            BuildProfileModuleUtil.SerializePlayerSettings(m_Profile);
            m_ProfileSerializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(m_Profile);
        }
    }
}
