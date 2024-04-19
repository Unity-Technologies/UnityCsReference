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
        const string k_PlayerSettingsHelpBox = "custom-player-settings-info-helpbox";
        const string k_PlayerSettingsHelpBoxButton = "custom-player-settings-info-helpbox-button";
        const string k_PlayerSettingsLabel = "player-settings-label";
        const string k_PlayerSettingsOptions = "player-settings-options";
        const string k_ProjectSettingsPath = "ProjectSettings/ProjectSettings.asset";

        VisualElement m_PlayerSettingsRoot;

        Label m_PlayerSettingsLabel;

        Button m_PlayerSettingsInfoButton;
        Button m_PlayerSettingsOptions;

        HelpBox m_PlayerSettingsHelpBox;

        InspectorElement m_PlayerSettingsInspector;

        PlayerSettingsEditor m_PlayerSettingsEditor;

        BuildProfile m_Profile;
        SerializedObject m_ProfileSerializedObject;

        internal static BuildProfilePlayerSettingsEditor CreatePlayerSettingsUI(Action addProfile, VisualElement root, SerializedObject buildProfileSerializedObject)
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

            if (BuildProfileContext.IsClassicPlatformProfile(buildProfile))
                buildProfilePlayerSettingsEditor.ShowPlayerSettingsHelpBox(addProfile, isClassic: true);
            else if (BuildProfileModuleUtil.HasSerializedPlayerSettings(buildProfilePlayerSettingsEditor.m_Profile))
                buildProfilePlayerSettingsEditor.ShowPlayerSettingsEditor();
            else
                buildProfilePlayerSettingsEditor.ShowPlayerSettingsHelpBox(addProfile, isClassic: false);

            return buildProfilePlayerSettingsEditor;
        }

        void InitializeVisualElements(VisualElement root)
        {
            m_PlayerSettingsRoot = root.Q<VisualElement>(k_PlayerSettingsRoot);
            m_PlayerSettingsHelpBox = root.Q<HelpBox>(k_PlayerSettingsHelpBox);
            m_PlayerSettingsInfoButton = root.Q<Button>(k_PlayerSettingsHelpBoxButton);
            m_PlayerSettingsLabel = root.Q<Label>(k_PlayerSettingsLabel);
            m_PlayerSettingsOptions = root.Q<Button>(k_PlayerSettingsOptions);
        }

        void HidePlayerSettingsUI()
        {
            m_PlayerSettingsLabel.Hide();
            m_PlayerSettingsHelpBox.Hide();
            m_PlayerSettingsInfoButton.Hide();
            HidePlayerSettingsEditor();
        }

        internal void ShowPlayerSettingsEditor()
        {
            m_PlayerSettingsHelpBox.Hide();

            if (m_Profile.playerSettings == null)
            {
                BuildProfileModuleUtil.CreatePlayerSettingsFromGlobal(m_Profile);
                UpdateBuildProfile();
            }

            CreatePlayerSettingsInspector();

            m_PlayerSettingsOptions.clicked += PlayerSettingsOptionMenu;
            m_PlayerSettingsOptions.Show();
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
                m_PlayerSettingsEditor.ConfigurePlayerSettingsForBuildProfile(m_Profile.moduleName, m_Profile.subtarget == StandaloneBuildSubtarget.Server, isActiveProfile);
            }

            if (m_PlayerSettingsInspector == null)
            {
                m_PlayerSettingsInspector = new InspectorElement(m_PlayerSettingsEditor);
                m_PlayerSettingsInspector.style.flexGrow = 1;
                m_PlayerSettingsInspector.TrackSerializedObjectValue(m_PlayerSettingsEditor.serializedObject, OnPlayerSettingsEditorChanged);
                m_PlayerSettingsRoot.Add(m_PlayerSettingsInspector);
            }
        }

        internal void RemovePlayerSettingsInspector()
        {
            if (m_PlayerSettingsEditor != null)
                UnityEngine.Object.DestroyImmediate(m_PlayerSettingsEditor);

            if (m_PlayerSettingsInspector != null && m_PlayerSettingsRoot.Contains(m_PlayerSettingsInspector))
                m_PlayerSettingsRoot.Remove(m_PlayerSettingsInspector);

            m_PlayerSettingsInspector = null;
            m_PlayerSettingsEditor = null;
        }

        void OnPlayerSettingsEditorChanged(SerializedObject playerSettingsSerializedObject)
        {
            playerSettingsSerializedObject.ApplyModifiedProperties();
            UpdateBuildProfile();
        }

        void ShowPlayerSettingsHelpBox(Action addProfile, bool isClassic)
        {
            m_PlayerSettingsOptions.Hide();

            m_PlayerSettingsHelpBox.text = isClassic ? TrText.playerSettingsClassicInfo : TrText.playerSettingsInfo;
            m_PlayerSettingsInfoButton.text = isClassic ? TrText.addBuildProfile : TrText.customizePlayerSettingsButton;

            if (isClassic)
                m_PlayerSettingsInfoButton.clicked += addProfile;
            else
                m_PlayerSettingsInfoButton.clicked += ShowPlayerSettingsEditor;

            m_PlayerSettingsLabel.Show();
            m_PlayerSettingsHelpBox.Show();
            m_PlayerSettingsInfoButton.Show();
        }

        void PlayerSettingsOptionMenu()
        {
            bool isDataSameAsProjSettings = BuildProfileModuleUtil.IsDataEqualToProjectSettings(m_Profile.playerSettings);
            var menu = new GenericMenu();
            menu.AddItem(TrText.playerSetttingsRemove, false, RemovePlayerSettings);
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

            HidePlayerSettingsEditor();
            BuildProfileModuleUtil.RemovePlayerSettings(m_Profile);
            UpdateBuildProfile();
            ShowPlayerSettingsHelpBox(null, isClassic: false);
        }

        void ResetToProjectSettingsValues()
        {
            bool resetPlayerSettings = EditorUtility.DisplayDialog(TrText.resetPlayerSettingsDialogTitle,
                                                                    TrText.resetPlayerSettingsDialogMessage,
                                                                    TrText.playerSettingsContinue,
                                                                    TrText.playerSettingsCancel);

            if (!resetPlayerSettings)
                return;

            var playerSettings = AssetDatabase.LoadAssetAtPath<PlayerSettings>(k_ProjectSettingsPath);
            var preset = new Preset(playerSettings);
            preset.ApplyTo(m_Profile.playerSettings);

            UpdateBuildProfile();

            RemovePlayerSettingsInspector();
            CreatePlayerSettingsInspector();
        }

        void UpdateBuildProfile()
        {
            BuildProfileModuleUtil.SerializePlayerSettings(m_Profile);
            m_ProfileSerializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(m_Profile);
            AssetDatabase.SaveAssetIfDirty(m_Profile);
        }
    }
}
