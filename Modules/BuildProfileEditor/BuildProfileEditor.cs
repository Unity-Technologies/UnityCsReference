// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.AdaptivePerformance.UI.Editor;
using UnityEditor.Modules;
using UnityEditor.Build.Profile.Elements;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Build.Profile.Internal;
using UnityEditor.Build.Profile.Handlers;

namespace UnityEditor.Build.Profile
{
    [CustomEditor(typeof(BuildProfile))]
    class BuildProfileEditor : Editor
    {
        const string k_Uxml = "BuildProfile/UXML/BuildProfileEditor.uxml";
        const string k_PlatformSettingPropertyName = "m_PlatformBuildProfile";
        const string k_AdditionalPlatformSettings = "m_AdditionalPlatformBuildSettings";
        const string k_AdditionalPlatformSettingsPlatformGuid = "platformGuid";
        const string k_AdditionalPlatformSettingsValue = "platformSettings";
        const string k_PlatformWarningHelpBox = "platform-warning-help-box";
        const string k_PlatformSettingsBaseRoot = "platform-settings-base-root";

        const string k_GlobalSceneLabel = "global-scene-label";
        const string k_SharedSettingsInfoHelpbox = "shared-settings-info-helpbox";
        const string k_SharedSettingsInfoHelpboxButton = "shared-settings-info-helpbox-button";
        const string k_SceneListFoldout = "scene-list-foldout";
        const string k_SceneListFoldoutRoot = "scene-list-foldout-root";
        const string k_SceneListFoldoutAddOpenSection = "scene-list-foldout-add-open-section";
        const string k_SceneListFoldoutAddOpenButton = "scene-list-foldout-add-open-button";
        const string k_SceneListFoldoutClassicSection = "scene-list-foldout-classic-section";
        const string k_SceneListFoldoutClassicButton = "scene-list-foldout-classic-button";
        const string k_SceneListGlobalToggle = "scene-list-global-toggle";
        const string k_CompilingWarningHelpBox = "compiling-warning-help-box";
        const string k_VirtualTextureWarningHelpBox = "virtual-texture-warning-help-box";
        const string k_BuildSettingsFoldout = "build-settings-foldout";
        const string k_AdditionalSettingsWrapper = "editor-additional-settings-wrapper";
        const string k_AddSettingsButton = "bp-add-settings-button";
        const string k_SettingsFoldoutRoot = "bp-editor-settings-container";
        const string k_InsightSettingsFoldout = "insights-analytics-foldout";
        const string k_PlatformSelectionContainer = "platform-selection-container";
        const string k_PlatformSelectionDropdown = "platform-selection-dropdown";
        const string k_SwitchProfilePlatformButton = "switch-profile-platform-button";
        bool isClassic = false;
        BuildProfileSceneList m_SceneList;
        HelpBox m_CompilingWarningHelpBox;
        HelpBox m_VirtualTexturingHelpBox;
        Button m_AddSettingsButton;
        VisualElement m_SettingsFoldoutRoot;
        BuildProfile m_Profile;
        AddSettingsDataProvider m_AddSettingsDataSource;
        VisualElement m_PlatformSelectionContainer;
        GUID m_SelectedPlatformGuid;

        public BuildProfileWindow parent { get; set; }

        public BuildProfileWorkflowState parentState { get; set; }

        public BuildProfileWorkflowState editorState { get; set; }

        public BuildProfileWorkflowState platformSettingsState { get; set; }

        internal BuildProfile buildProfile => m_Profile;

        IBuildProfileExtension m_PlatformExtension = null;
        ISDKPlatformExtension m_SDKExtension = null;

        public BuildProfileEditor()
        {
            editorState = new BuildProfileWorkflowState((next) =>
            {
                if (editorState.buildAction == ActionState.Disabled)
                    editorState.buildAndRunAction = ActionState.Disabled;

                if (editorState.additionalActions != next.additionalActions)
                    editorState.additionalActions = next.additionalActions;

                parentState?.Apply(next);
            });

            platformSettingsState = new BuildProfileWorkflowState((next) =>
            {
                if (editorState.buildAction == ActionState.Disabled)
                    next.buildAction = ActionState.Disabled;

                if (editorState.buildAndRunAction == ActionState.Disabled || next.buildAction == ActionState.Disabled)
                    next.buildAndRunAction = ActionState.Disabled;

                editorState?.Apply(next);
            });
        }

        public override VisualElement CreateInspectorGUI()
        {
            if (serializedObject.targetObject is not BuildProfile profile)
            {
                throw new InvalidOperationException("Editor object is not of type BuildProfile.");
            }

            m_Profile = profile;

            if (BuildProfileContext.instance.TryGetInitializationInfo(profile, out var initializationInfo)
                && !initializationInfo.IsDone())
            {
                Action callback = parent != null ? parent.RepaintBuildProfileInspector : null;
                return new BuildProfileBootstrapView(m_Profile, initializationInfo, callback);
            }

            var guid = profile.isMultiTarget ? profile.selectedPlatformGuid : profile.platformGuid;
            m_SelectedPlatformGuid = guid;
            m_PlatformExtension = BuildProfileModuleUtil.GetBuildProfileExtension(guid);
            m_SDKExtension = BuildProfileModuleUtil.GetSDKPlatformExtension(profile.platformGuid);
            m_AddSettingsDataSource = new AddSettingsDataProvider(profile);

            CleanupEventHandlers();

            var root = new VisualElement();
            var visualTree = EditorGUIUtility.LoadRequired(k_Uxml) as VisualTreeAsset;
            var windowUss = EditorGUIUtility.LoadRequired(Util.k_StyleSheet) as StyleSheet;
            visualTree.CloneTree(root);
            root.styleSheets.Add(windowUss);

            var noModuleFoundHelpBox = root.Q<HelpBox>(k_PlatformWarningHelpBox);
            var sharedSettingsInfoHelpBox = root.Q<HelpBox>(k_SharedSettingsInfoHelpbox);
            var additionalSettingsWrapper = root.Q<VisualElement>(k_AdditionalSettingsWrapper);
            var platformSettingsBaseRoot = root.Q<VisualElement>(k_PlatformSettingsBaseRoot);
            var buildSettingsFoldout = root.Q<Foldout>(k_BuildSettingsFoldout);
            m_VirtualTexturingHelpBox = root.Q<HelpBox>(k_VirtualTextureWarningHelpBox);
            m_CompilingWarningHelpBox = root.Q<HelpBox>(k_CompilingWarningHelpBox);
            m_SettingsFoldoutRoot = root.Q<VisualElement>(k_SettingsFoldoutRoot);
            m_AddSettingsButton = root.Q<Button>(k_AddSettingsButton);
            m_AddSettingsButton.text = TrText.addSettings;
            m_AddSettingsButton.clicked += ShowAddSettingsDropdown;

            m_VirtualTexturingHelpBox.text = TrText.invalidVirtualTexturingSettingMessage;
            m_CompilingWarningHelpBox.text = TrText.compilingMessage;

            sharedSettingsInfoHelpBox.text = TrText.sharedSettingsInfo;
            buildSettingsFoldout.text = TrText.GetSettingsSectionName(BuildProfileModuleUtil.GetClassicPlatformDisplayName(guid));

            if (m_PlatformExtension != null)
            {
                string profileInfoMessage = m_PlatformExtension.GetProfileInfoMessage();
                if (!string.IsNullOrEmpty(profileInfoMessage))
                    sharedSettingsInfoHelpBox.text = profileInfoMessage;
            }

            m_PlatformSelectionContainer = root.Q<VisualElement>(k_PlatformSelectionContainer);
            m_PlatformSelectionContainer.Hide();
            if (profile.isMultiTarget && (m_SDKExtension == null || m_SDKExtension.shouldShowPlatformSettings))
                ShowProfilePlatformSelection();

            AddSceneList(root, profile);

            if (m_SDKExtension != null)
            {
                if(!m_SDKExtension.shouldShowAdditionalSettings)
                {
                    ShowRequiredSettingsFoldouts();
                    m_AddSettingsButton.Hide();
                } 
                else
                    ShowSettingsFoldouts();

                if(m_SDKExtension.shouldShowAdditionalSettings && !m_SDKExtension.shouldShowAddSettingsButton)
                    m_AddSettingsButton.Hide();
            }
            else
                ShowSettingsFoldouts();

            BuildProfileModuleUtil.OnUpdateActiveEditors += this.OnUpdateEditorView;

            bool hasErrors = Util.UpdatePlatformRequirementsWarningHelpBox(noModuleFoundHelpBox, profile.platformGuid);
            isClassic = BuildProfileContext.IsClassicPlatformProfile(profile);

            if (!isClassic)
            {
                sharedSettingsInfoHelpBox.Hide();
            }
            else
            {
                m_AddSettingsButton.Hide();
                sharedSettingsInfoHelpBox.buttonText = TrText.addBuildProfile;
                sharedSettingsInfoHelpBox.onButtonClicked += () => PlatformDiscoveryWindow.ShowWindowAndSelectPlatform(profile.platformGuid);
            }

            if (hasErrors)
            {
                // Platform requirement is not met, platform settings cannot be changed.
                buildSettingsFoldout.Hide();
                return root;
            }

            if(m_SDKExtension != null && !m_SDKExtension.shouldShowPlatformSettings)
                buildSettingsFoldout.Hide();
            else
                ShowPlatformSettings(profile, platformSettingsBaseRoot);
            if(m_SDKExtension == null || m_SDKExtension.shouldShowAdditionalSettings)
                ShowInsightsSettings(profile, root, isClassic);

            EditorApplication.update += EditorUpdate;

            root.Bind(serializedObject);
            return root;
        }

        /// <summary>
        /// Create modified GUI for shared setting amongst classic platforms.
        /// </summary>
        public VisualElement CreateLegacyGUI()
        {
            CleanupEventHandlers();

            var root = new VisualElement();
            var visualTree = EditorGUIUtility.LoadRequired(k_Uxml) as VisualTreeAsset;
            var windowUss = EditorGUIUtility.LoadRequired(Util.k_StyleSheet) as StyleSheet;
            visualTree.CloneTree(root);
            root.styleSheets.Add(windowUss);

            // Note: HelpBox now includes a built-in button (UISD-514)
            // The button only appears when configured via code. Since we don't configure it here,
            // it won't be visible - no manual hiding required. So we don't do it below this line
            var sharedSettingsHelpbox = root.Q<HelpBox>(k_SharedSettingsInfoHelpbox);

            root.Q<HelpBox>(k_PlatformWarningHelpBox).Hide();
            root.Q<VisualElement>(k_PlatformSettingsBaseRoot).Hide();
            root.Q<HelpBox>(k_VirtualTextureWarningHelpBox).Hide();
            root.Q<HelpBox>(k_CompilingWarningHelpBox).Hide();
            root.Q<Foldout>(k_BuildSettingsFoldout).Hide();
            root.Q<Button>(k_AddSettingsButton).Hide();
            root.Q<VisualElement>(k_PlatformSelectionContainer).Hide();

            sharedSettingsHelpbox.text = TrText.sharedSettingsSectionInfo;

            var sectionLabel = root.Q<Label>(k_GlobalSceneLabel);
            sectionLabel.text = TrText.sceneList;
            sectionLabel.Show();

            AddSceneList(root);
            return root;
        }

        void OnDisable()
        {
            CleanupEventHandlers();

            if (m_PlatformExtension != null)
                m_PlatformExtension.OnDisable();
        }

        internal void OnActivateClicked()
        {
            editorState.buildAndRunButtonDisplayName = platformSettingsState.buildAndRunButtonDisplayName;
            editorState.buildButtonDisplayName = platformSettingsState.buildButtonDisplayName;
            editorState.UpdateBuildActionStates(ActionState.Disabled, ActionState.Disabled);

            if (isClassic)
                return;

            m_SettingsFoldoutRoot.Clear();
            ShowSettingsFoldouts();
        }

        internal void DuplicateSelectedClassicProfile()
        {
            parent.DuplicateSelectedClassicProfile();
        }

        void EditorUpdate()
        {
            UpdateWarningsAndButtonStatesForActiveProfile();
        }

        void RebuildBuildProfileEditor()
        {
            if (parent != null)
            {
                parent.RepaintBuildProfileInspector();
                return;
            }

            // For build profiles in inspector windows without BuildProfileWindow.
            ActiveEditorTracker.sharedTracker.ForceRebuild();
        }

        void ShowRequiredSettingsFoldouts()
        {
            m_SettingsFoldoutRoot.Clear();
            foreach (var settings in m_AddSettingsDataSource.GetSettingsInProfile())
            {
                if (settings.GetIsRequired())
                {
                    m_SettingsFoldoutRoot.Add(new BuildProfileSettingsFoldout(
                    serializedObject,
                    m_Profile,
                    settings));

                    BuildProfileModuleUtil.UpdateActiveEditors(m_Profile);
                }
            }
        }

        void ShowSettingsFoldouts()
        {
            m_SettingsFoldoutRoot.Clear();
            foreach (var settings in m_AddSettingsDataSource.GetSettingsInProfile())
            {
                m_SettingsFoldoutRoot.Add(new BuildProfileSettingsFoldout(
                    serializedObject,
                    m_Profile,
                    settings));
            }

            m_AddSettingsButton.SetEnabled(!m_AddSettingsDataSource.AllProfileSettingsInUse());
        }

        void OnAddSettingsClicked(int key)
        {
            var settingsProvider = m_AddSettingsDataSource.Get(key);
            if (settingsProvider == null)
                return;

            settingsProvider.OnAdd(m_Profile);
            BuildProfileModuleUtil.UpdateActiveEditors(m_Profile);
        }

        void UpdateWarningsAndButtonStatesForActiveProfile()
        {
            if (!m_Profile.IsActiveBuildProfileOrPlatform())
                return;

            bool isVirtualTexturingValid = BuildProfileModuleUtil.IsVirtualTexturingSettingsValid(m_SelectedPlatformGuid);
            bool isCompiling = EditorApplication.isCompiling || EditorApplication.isUpdating;
            UpdateHelpBoxVisibility(m_VirtualTexturingHelpBox, !isVirtualTexturingValid);
            UpdateHelpBoxVisibility(m_CompilingWarningHelpBox, isCompiling);

            if (!isVirtualTexturingValid || isCompiling)
            {
                editorState.UpdateBuildActionStates(ActionState.Disabled, ActionState.Disabled);
            }
            else
            {
                editorState.UpdateBuildActionStates(ActionState.Enabled, ActionState.Enabled);
            }
        }

        void UpdateHelpBoxVisibility(HelpBox helpBox, bool showHelpBox)
        {
            if (showHelpBox && helpBox.resolvedStyle.display == DisplayStyle.None)
            {
                helpBox.Show();
            }
            else if (!showHelpBox && helpBox.resolvedStyle.display != DisplayStyle.None)
            {
                helpBox.Hide();
            }
        }

        void ShowProfilePlatformSelection()
        {
            if (!m_Profile.TryGetSupportedIBuildTargets(out var installedPlatforms))
            {
                m_PlatformSelectionContainer.Hide();
                return;
            }

            if (m_Profile.selectedPlatformGuid.Empty())
                m_Profile.selectedPlatformGuid = installedPlatforms[0].Guid;

            SetUpPlatformSelectionDropdown();
            SetUpSwitchProfilePlatformButton();
            m_PlatformSelectionContainer.Show();

            void SetUpPlatformSelectionDropdown()
            {
                var platformSelectionDropdown = m_PlatformSelectionContainer.Q<DropdownField>(k_PlatformSelectionDropdown);
                platformSelectionDropdown.label = TrText.platformSelectionDropdown;
                platformSelectionDropdown.choices.Clear();
                var selectedIndex = 0;

                for (var index = 0; index < installedPlatforms.Length; index++)
                {
                    var platform = installedPlatforms[index];
                    var guid = platform.Guid;
                    var displayName = BuildTargetDiscovery.BuildPlatformDisplayName(guid);
                    platformSelectionDropdown.choices.Add(displayName);

                    if (guid == m_Profile.selectedPlatformGuid)
                        selectedIndex = index;
                }

                platformSelectionDropdown.index = selectedIndex;

                platformSelectionDropdown.RegisterValueChangedCallback(_ =>
                {
                    var dropdownIndex = platformSelectionDropdown.index;
                    if (dropdownIndex < 0 || dropdownIndex >= installedPlatforms.Length)
                        return;

                    var selectedGuid = installedPlatforms[dropdownIndex].Guid;
                    if (m_Profile.selectedPlatformGuid != selectedGuid)
                    {
                        m_Profile.selectedPlatformGuid = selectedGuid;
                        m_SelectedPlatformGuid = selectedGuid;
                        BuildProfileModuleUtil.UpdateActiveEditors(m_Profile);
                        RebuildBuildProfileEditor();
                    }
                });
            }

            void SetUpSwitchProfilePlatformButton()
            {
                var switchProfilePlatformButton = m_PlatformSelectionContainer.Q<Button>(k_SwitchProfilePlatformButton);
                switchProfilePlatformButton.text = TrText.switchProfilePlatformButton;
                switchProfilePlatformButton.clicked += () => parent?.OnActivateButtonClicked();

                if (!m_Profile.IsActiveBuildProfileOrPlatform() || parent == null)
                {
                    switchProfilePlatformButton.Hide();
                    return;
                }

                switchProfilePlatformButton.Show();
                if (m_Profile.selectedPlatformGuid == m_Profile.activePlatformGuid)
                    switchProfilePlatformButton.SetEnabled(false);
                else
                    switchProfilePlatformButton.SetEnabled(true);
            }
        }

        void ShowPlatformSettings(BuildProfile profile, VisualElement platformSettingsBaseRoot)
        {
            var platformProperties = GetPlatformSettingsProperty(profile);

            if (m_PlatformExtension == null)
                return;

            var warningContainer = m_PlatformExtension.CreatePlatformBuildWarningsGUI(serializedObject, platformProperties);

            // Build Profile Window reserves space for custom
            // platform GUI outside of the editor scroll view.
            if (parent != null && warningContainer != null)
            {
                parent.AppendInspectorHeaderElement(warningContainer);
            }

            var settings = m_PlatformExtension.CreateSettingsGUI(
                serializedObject, platformProperties, platformSettingsState);
            platformSettingsBaseRoot.Add(settings);
        }

        SerializedProperty GetPlatformSettingsProperty(BuildProfile profile)
        {
            var defaultPlatformSettings = serializedObject.FindProperty(k_PlatformSettingPropertyName);

            if (!profile.isMultiTarget)
                return defaultPlatformSettings;

            var additionalPlatformSettings = serializedObject.FindProperty(k_AdditionalPlatformSettings);
            if (additionalPlatformSettings == null || !additionalPlatformSettings.isArray)
                return defaultPlatformSettings;

            for (int index = 0; index < additionalPlatformSettings.arraySize; index++)
            {
                var entry = additionalPlatformSettings.GetArrayElementAtIndex(index);
                var platformIdProperty = entry.FindPropertyRelative(k_AdditionalPlatformSettingsPlatformGuid);
                if (platformIdProperty == null ||
                    platformIdProperty.propertyType != SerializedPropertyType.GUID ||
                    platformIdProperty.guidValue != profile.selectedPlatformGuid)
                {
                    continue;
                }

                var selectedPlatformSettings = entry.FindPropertyRelative(k_AdditionalPlatformSettingsValue);
                return selectedPlatformSettings ?? defaultPlatformSettings;
            }

            return defaultPlatformSettings;
        }

        void ShowInsightsSettings(BuildProfile profile, VisualElement rootVisualElement, bool isClassic)
        {
            var isSupported = BuildProfileInsightsSettingsView.CreateGUI(profile, rootVisualElement, isClassic);
            if(!isSupported)
            {
                return;
            }

            var foldout = rootVisualElement.Q<Foldout>(k_InsightSettingsFoldout);
            foldout.text = TrText.diagnostics;
            foldout.Show();
        }

        void AddSceneList(VisualElement root, BuildProfile profile = null)
        {
            // On no build profile show EditorBuildSetting scene list,
            // classic platforms show read-only version.
            bool isGlobalSceneList = profile == null;
            bool isClassicPlatform = !isGlobalSceneList && BuildProfileContext.IsClassicPlatformProfile(profile);
            bool isEnable = isGlobalSceneList || !isClassicPlatform;

            var sceneListFoldout = root.Q<Foldout>(k_SceneListFoldout);
            var globalToggle = root.Q<Toggle>(k_SceneListGlobalToggle);

            // Profile Scene List are added to the settings foldout root.
            if (!isGlobalSceneList && !isClassicPlatform)
            {
                sceneListFoldout.Hide();
                globalToggle.Hide();
                return;
            }

            globalToggle.label = TrText.sceneListOverride;
            sceneListFoldout.text = TrText.sceneList;
            m_SceneList = new BuildProfileSceneList();
            Undo.undoRedoEvent += m_SceneList.OnUndoRedo;
            var container = m_SceneList.GetSceneListGUI();
            container.SetEnabled(isEnable);
            root.Q<VisualElement>(k_SceneListFoldoutRoot).Add(container);

            if (isEnable)
            {
                // Bind Add Open Scenes List button
                root.Q<VisualElement>(k_SceneListFoldoutAddOpenSection).Show();
                var addOpenSceneListButton = root.Q<Button>(k_SceneListFoldoutAddOpenButton);
                addOpenSceneListButton.text = TrText.addOpenScenes;
                addOpenSceneListButton.clicked += () => m_SceneList.AddOpenScenes();
            }

            if (isClassicPlatform)
            {
                // Bind Global Scene List button
                root.Q<VisualElement>(k_SceneListFoldoutClassicSection).Show();
                var globalSceneListButton = root.Q<Button>(k_SceneListFoldoutClassicButton);
                globalSceneListButton.text = TrText.openSceneList;
                globalSceneListButton.clicked += () =>
                {
                    parent.OnClassicSceneListSelected();
                };

                globalToggle.Hide();
                return;
            }

            if (isGlobalSceneList)
            {
                globalToggle.Hide();
                return;
            }
        }

        /// <summary>
        /// Callback for updating all editors of the build profile.
        /// Used for syncing editors between the Build Profile window and the inspector.
        /// </summary>
        void OnUpdateEditorView(BuildProfile profile)
        {
            if (profile != m_Profile)
                return;

            if (profile.isMultiTarget && m_SelectedPlatformGuid != profile.selectedPlatformGuid)
            {
                m_SelectedPlatformGuid = profile.selectedPlatformGuid;
                RebuildBuildProfileEditor();
                return;
            }

            ShowSettingsFoldouts();
        }

        void CleanupEventHandlers()
        {
            if (m_Profile is not null)
            {
                BuildProfileModuleUtil.OnUpdateActiveEditors -= this.OnUpdateEditorView;
                m_Profile.OnPackageAddProgress = null;
                m_Profile.OnPackageAddComplete = null;
            }

            if (m_SceneList is not null)
                Undo.undoRedoEvent -= m_SceneList.OnUndoRedo;

            EditorApplication.update -= EditorUpdate;
        }

        void ShowAddSettingsDropdown()
        {
            AddSettingsDropdownWindow.Show(m_AddSettingsButton.worldBound, OnAddSettingsClicked, m_AddSettingsDataSource);
        }
    }
}
