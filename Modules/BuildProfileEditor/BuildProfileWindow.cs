// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor.Build.Profile.Elements;
using UnityEditor.Build.Profile.Handlers;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Build Settings window in 'File > Build Settings'.
    /// Handles creating and editing of <see cref="BuildProfile"/> assets.
    /// </summary>
    [EditorWindowTitle(title = "Build Settings")]
    internal class BuildProfileWindow : EditorWindow
    {
        const string k_DevOpsUrl = "https://unity.com/products/unity-devops?utm_medium=desktop-app&utm_source=unity-editor-window-menu&utm_content=buildsettings";
        const string k_Uxml = "BuildProfile/UXML/BuildProfileWindow.uxml";
        const string k_PlayerSettingsWindow = "Project/Player";

        internal const string buildProfileInspectorVisualElement = "build-profile-editor-inspector";

        internal BuildProfileEditor buildProfileEditor;
        BuildProfileWorkflowState m_WindowState;

        bool m_ShouldAskForBuildLocation = true;

        BuildProfileDataSource m_BuildProfileDataSource;
        BuildProfileContextMenu m_BuildProfileContextMenu;
        BuildProfileWindowSelection m_BuildProfileSelection;

        /// <summary>
        /// Left column classic profile list view.
        /// </summary>
        PlatformListView m_ProfileListViews;
        VisualElement m_WelcomeMessageElement;

        VisualElement m_InspectorHeader;

        /// <summary>
        /// Build Profile inspector for the selected classic platform or profile,
        /// repainted on <see cref="m_BuildProfileClassicPlatformListView"/> selection change.
        /// </summary>
        /// <see cref="OnClassicPlatformSelected"/>
        ScrollView m_BuildProfileInspectorElement;

        VisualElement m_AdditionalActionsDropdown;
        DropdownButton m_BuildButton;
        ToolbarButton m_AssetImportButton;
        Button m_BuildAndRunButton;
        Button m_ActivateButton;
        AssetImportOverridesWindow m_AssetImportWindow;
        Background m_WarningIcon;

        [UsedImplicitly, RequiredByNativeCode]
        public static void ShowBuildProfileWindow()
        {
            var window = GetWindow<BuildProfileWindow>(TrText.buildProfilesName);
            window.minSize = new Vector2(725, 400);
        }

        public void CreateGUI()
        {
            m_WindowState = new BuildProfileWorkflowState(OnWorkflowStateChanged);
            var windowUxml = EditorGUIUtility.LoadRequired(k_Uxml) as VisualTreeAsset;
            var windowUss = EditorGUIUtility.LoadRequired(Util.k_StyleSheet) as StyleSheet;
            rootVisualElement.styleSheets.Add(windowUss);
            windowUxml.CloneTree(rootVisualElement);
            var listViewAddProfileButton = rootVisualElement.Q<Button>("fallback-add-profile-button");
            var addBuildProfileButton = rootVisualElement.Q<ToolbarButton>("add-build-profile-button");
            var unityDevOpsButton = rootVisualElement.Q<ToolbarButton>("learn-more-unity-dev-ops-button");
            var playerSettingsButton = rootVisualElement.Q<ToolbarButton>("player-settings-button");
            m_AssetImportButton = rootVisualElement.Q<ToolbarButton>("asset-import-overrides-button");

            // Capture static visual element reference.
            m_AdditionalActionsDropdown = rootVisualElement.Q<VisualElement>("additional-actions-dropdown-button");
            m_BuildButton = CreateBuildDropdownButton();
            rootVisualElement.Q<VisualElement>("build-dropdown-button").Add(m_BuildButton);
            m_BuildProfileInspectorElement = rootVisualElement.Q<ScrollView>(buildProfileInspectorVisualElement);
            m_BuildAndRunButton = rootVisualElement.Q<Button>("build-and-run-button");
            m_ActivateButton = rootVisualElement.Q<Button>("activate-button");
            m_WelcomeMessageElement = rootVisualElement.Q<VisualElement>("fallback-no-custom-build-profiles");
            m_InspectorHeader = rootVisualElement.Q<VisualElement>("build-profile-editor-header");

            // Apply localized text to static elements.
            rootVisualElement.Q<Label>("platforms-label").text = TrText.platforms;
            rootVisualElement.Q<Label>("build-profiles-label").text = TrText.buildProfilesName;
            rootVisualElement.Q<Label>("fallback-welcome-label").text = TrText.buildProfileWelcome;
            addBuildProfileButton.text = TrText.addBuildProfile;
            unityDevOpsButton.text = TrText.learnMoreUnityDevOps;
            playerSettingsButton.text = TrText.playerSettings;
            listViewAddProfileButton.text = TrText.addBuildProfile;
            m_ActivateButton.text = TrText.activate;
            m_BuildAndRunButton.text = TrText.buildAndRun;

            UpdateToolbarButtonState();

            // Build dynamic visual elements.
            if (m_BuildProfileDataSource != null)
                m_BuildProfileDataSource.Dispose();
            m_BuildProfileDataSource = new BuildProfileDataSource(this);
            m_ProfileListViews = new PlatformListView(this, m_BuildProfileDataSource);
            m_BuildProfileSelection = new BuildProfileWindowSelection(rootVisualElement, m_ProfileListViews);
            m_BuildProfileContextMenu = new BuildProfileContextMenu(this, m_BuildProfileSelection, m_BuildProfileDataSource);

            m_ProfileListViews.Create();

            if (m_BuildProfileDataSource.customBuildProfiles.Count > 0)
            {
                m_ProfileListViews.ShowCustomBuildProfiles();
                m_WelcomeMessageElement.Hide();
            }

            // When creating the profile lists, the bind callbacks (which set the active profile index)
            // will be called after this, so we need to find the active profile to select it in here
            m_ProfileListViews.SelectActiveProfile();

            // Set up event handlers.
            m_BuildAndRunButton.clicked += () =>
            {
                OnBuildButtonClicked(BuildOptions.AutoRunPlayer | BuildOptions.StrictMode);
            };
            m_ActivateButton.clicked += OnActivateButtonClicked;
            addBuildProfileButton.clicked += PlatformDiscoveryWindow.ShowWindow;
            listViewAddProfileButton.clicked += PlatformDiscoveryWindow.ShowWindow;
            playerSettingsButton.clicked += () =>
            {
                SettingsService.OpenProjectSettings(k_PlayerSettingsWindow);
            };
            m_AssetImportButton.clicked += () =>
            {
                if (m_AssetImportWindow == null)
                    m_AssetImportWindow = ScriptableObject.CreateInstance<AssetImportOverridesWindow>();

                m_AssetImportWindow.ShowUtilityWindow(UpdateToolbarButtonState);
            };
            unityDevOpsButton.clicked += () =>
            {
                Application.OpenURL(k_DevOpsUrl);
            };

            BuildProfileContext.instance.activeProfileChanged += OnActiveProfileChanged;
        }

        public void OnDisable()
        {
            DestroyImmediate(buildProfileEditor);
            BuildProfileContext.instance.activeProfileChanged -= OnActiveProfileChanged;
            m_BuildProfileDataSource?.Dispose();

            if (m_AssetImportWindow != null)
                m_AssetImportWindow.Close();

            m_ProfileListViews?.Unbind();
            SendWorkflowReport();
        }

        /// <summary>
        /// Handle selection of custom build profile item, creates embedded inspector and clears
        /// adjacent list views.
        /// </summary>
        internal void OnCustomProfileSelected(IEnumerable<object> selectedItems)
        {
            using var item = selectedItems.GetEnumerator();
            if (!item.MoveNext())
                return;

            bool shouldRebuild = m_BuildProfileDataSource.ClearDeletedProfiles();
            var profile = item.Current as BuildProfile;
            if (profile == null)
            {
                RepaintAndClearSelection();
                Debug.LogWarning("BuildProfileWindow: selected item has been deleted?");
                return;
            }

            m_ProfileListViews.ClearPlatformSelection();
            m_InspectorHeader.Show();
            m_BuildProfileSelection.SelectItems(selectedItems);
            m_BuildProfileSelection.UpdateSelectionGUI(profile);

            if (!m_BuildProfileSelection.IsMultipleSelection())
            {
                RebuildBuildProfileEditor(profile);
            }
            else
            {
                m_BuildProfileInspectorElement.Clear();
                UpdateFormButtonState(null);
            }

            if (shouldRebuild)
                RebuildProfileListViews();
        }

        /// <summary>
        /// Handle selection of classic platform profile.
        /// </summary>
        internal void OnClassicPlatformSelected(BuildProfile profile)
        {
            if (profile == null)
            {
                Debug.LogWarning("BuildProfileWindow: selected platform has been deleted?");
                return;
            }

            m_ProfileListViews.ClearProfileSelection();
            m_InspectorHeader.Show();
            m_BuildProfileSelection.SelectItem(profile);
            m_BuildProfileSelection.UpdateSelectionGUI(profile);

            RebuildBuildProfileEditor(profile);

            if (m_BuildProfileDataSource.ClearDeletedProfiles())
                RebuildProfileListViews();
        }

        /// <summary>
        /// Handles selection of unavailable and supported platform.
        /// </summary>
        internal void OnMissingClassicPlatformSelected(string moduleName, StandaloneBuildSubtarget subtarget)
        {
            m_ProfileListViews.ClearProfileSelection();
            UpdateFormButtonState(null);

            // Render platform requirement helpbox instead of default inspector.
            m_BuildProfileInspectorElement.Clear();
            var warningHelpBox = new HelpBox()
            {
                messageType = HelpBoxMessageType.Warning
            };
            warningHelpBox.AddToClassList("mx-medium");
            Util.UpdatePlatformRequirementsWarningHelpBox(warningHelpBox, moduleName, subtarget);
            m_BuildProfileInspectorElement.Add(warningHelpBox);

            // Update details headers.
            m_BuildProfileSelection.MissingPlatformSelected(moduleName, subtarget);
        }

        /// <summary>
        /// Show classic platform UI for globally shared settings.
        /// </summary>
        internal void OnClassicSceneListSelected()
        {
            m_BuildProfileSelection.ClearSelectedProfiles();
            m_ProfileListViews.ClearProfileSelection();

            DestroyImmediate(buildProfileEditor);
            buildProfileEditor = ScriptableObject.CreateInstance<BuildProfileEditor>();
            m_BuildProfileInspectorElement.Clear();
            m_BuildProfileInspectorElement.Add(buildProfileEditor.CreateLegacyGUI());
            m_InspectorHeader.Hide();
        }

        /// <summary>
        /// Duplicates and selects the current classic platform profile.
        /// </summary>
        internal void DuplicateSelectedClassicProfile()
        {
            m_BuildProfileContextMenu.HandleDuplicateSelectedProfiles(true);
        }

        /// <summary>
        /// Repaints the list views and clears the selection.
        /// </summary>
        internal void RepaintAndClearSelection()
        {
            RebuildProfileListViews();
            m_ProfileListViews.SelectActiveProfile();
        }

        /// <summary>
        /// Creates a <see cref="BuildProfileListEditableLabel"/> with the current
        /// <see cref="m_BuildProfileContextMenu"/>.
        /// </summary>
        internal BuildProfileListEditableLabel CreateEditableLabelItem() => new BuildProfileListEditableLabel(
            m_BuildProfileContextMenu.UpdateBuildProfileLabelName,
            m_BuildProfileContextMenu.AddBuildProfileContextMenu());

        /// <summary>
        /// Rebuild the custom profile list view, or show the welcome message if no custom
        /// profiles exist.
        /// </summary>
        internal void RebuildProfileListViews()
        {
            if (m_BuildProfileDataSource.customBuildProfiles.Count == 0)
            {
                m_ProfileListViews.HideCustomBuildProfiles();
                m_WelcomeMessageElement.Show();
            }
            else
            {
                m_ProfileListViews.ShowCustomBuildProfiles();
                m_WelcomeMessageElement.Hide();
            }

            m_ProfileListViews.Rebuild();
        }

        void Update()
        {
            // We need to detect when a build profile asset that is selected in inspector
            // gets deleted. Since we want to avoid using asset post processors for performance
            // reason, we check it in update
            if (buildProfileEditor?.buildProfile == null &&
                m_BuildProfileSelection?.IsMultipleSelection() == false &&
                m_InspectorHeader?.style.display != DisplayStyle.None)
            {
                DestroyImmediate(buildProfileEditor);
                m_BuildProfileDataSource?.DeleteNullProfiles();
                RepaintAndClearSelection();
            }
        }

        /// <summary>
        /// Build Profile Workflow state change callback. Invoked when <see cref="m_WindowState"/> is refreshed
        /// by this class or changes are attempted by an embedded <see cref="BuildProfileEditor"/>.
        /// </summary>
        /// <param name="next"></param>
        void OnWorkflowStateChanged(BuildProfileWorkflowState next)
        {
            if (next == m_WindowState && buildProfileEditor is not null)
            {
                // When refreshing the window state, if there's a build profile selected
                // then the corresponding editor state should be considered.
                next = buildProfileEditor.editorState;
            }

            ReduceWindowState(m_WindowState, next);
        }

        void ReduceWindowState(BuildProfileWorkflowState parent, BuildProfileWorkflowState child)
        {
            // For conflict resolution between the parent window and a child state being applied:
            // - false booleans take precedence
            // - ActionState values take priority in decreasing order: Hidden > Disabled > Enabled
            m_ShouldAskForBuildLocation = parent.askForBuildLocation && child.askForBuildLocation;
            m_ActivateButton.ApplyActionState(BuildProfileWorkflowState.CalculateActionState(
                parent.activateAction, child.activateAction));
            m_BuildButton.ApplyActionState(BuildProfileWorkflowState.CalculateActionState(
                parent.buildAction, child.buildAction));
            m_BuildAndRunButton.ApplyActionState(BuildProfileWorkflowState.CalculateActionState(
                parent.buildAndRunAction, child.buildAndRunAction));

            m_BuildAndRunButton.text = child.buildAndRunButtonDisplayName;
            m_BuildButton.SetText(child.buildButtonDisplayName);

            // Additional actions are always directly applied to the parent window state.
            parent.additionalActions = child.additionalActions;
            RepaintAdditionActionsDropdown();
        }

        void RepaintAdditionActionsDropdown()
        {
            if (m_WindowState.buildAction == ActionState.Hidden || m_WindowState.additionalActions.Count == 0)
            {
                m_AdditionalActionsDropdown.Hide();
                return;
            }

            // Dropdown button default text matches the first enabled action in the list,
            // otherwise defaults to the first action.
            var actions = m_WindowState.additionalActions;
            GenericMenu menu = null;
            int firstEnabledIndex = -1;
            if (actions.Count > 1)
            {
                menu = new GenericMenu();
                for (int i = 0; i < actions.Count; i++)
                {
                    var action = actions[i];
                    if (action.state != ActionState.Enabled)
                        continue;

                    if (firstEnabledIndex < 0)
                        firstEnabledIndex = i;
                    else
                        menu.AddItem(new GUIContent(action.displayName), action.isOn, () => action.callback());
                }
            }

            firstEnabledIndex = Math.Max(firstEnabledIndex, 0);
            var dropdown = new DropdownButton(
                actions[firstEnabledIndex].displayName, actions[firstEnabledIndex].callback, menu);
            if (actions[firstEnabledIndex].state != ActionState.Enabled)
                dropdown.SetEnabled(false);

            m_AdditionalActionsDropdown.Clear();
            m_AdditionalActionsDropdown.Add(dropdown);
            m_AdditionalActionsDropdown.Show();
        }

        void OnActivateButtonClicked()
        {
            if (!m_BuildProfileSelection.HasSelection())
                return;

            BuildProfile activateProfile = m_BuildProfileSelection.Get(0);
            if (activateProfile.IsActiveBuildProfileOrPlatform())
                return;

            // Classic profiles should not be set as active, they are identified
            // by the state of EditorUserBuildSettings active build target.
            BuildProfileContext.instance.activeProfile = !BuildProfileContext.IsClassicPlatformProfile(activateProfile)
                ? activateProfile : null;

            buildProfileEditor.OnActivateClicked();

            BuildProfileModuleUtil.SwitchLegacyActiveFromBuildProfile(activateProfile);

            UpdateFormButtonState(activateProfile);
            RebuildProfileListViews();

            // Apply current asset import overrides if switching profile
            // without applying. It should be called lastly since it can
            // trigger a reimport.
            m_AssetImportWindow?.ApplyCurrentAssetImportOverrides();
            UpdateToolbarButtonState();
        }

        void OnBuildButtonClicked(BuildOptions optionFlags)
        {
            if (!m_BuildProfileSelection.HasSelection())
                return;

            var profile = m_BuildProfileSelection.Get(0);
            if (!profile.IsActiveBuildProfileOrPlatform())
            {
                Debug.LogWarning("[BuildProfile] Attempted to build with a non-active build profile.");
                return;
            }

            BuildProfileModuleUtil.SwitchLegacySelectedBuildTargets(profile);

            BuildProfileModuleUtil.CallInternalBuildMethods(m_ShouldAskForBuildLocation, optionFlags);
        }

        void UpdateFormButtonState(BuildProfile profile)
        {
            if (profile is null)
            {
                m_WindowState.activateAction = ActionState.Hidden;
                m_WindowState.buildAction = ActionState.Hidden;
                m_WindowState.buildAndRunAction = ActionState.Hidden;
                m_WindowState.Refresh();
            }
            else if (profile.IsActiveBuildProfileOrPlatform())
            {
                m_WindowState.activateAction = ActionState.Hidden;
                m_WindowState.buildAction = ActionState.Enabled;
                m_WindowState.buildAndRunAction = ActionState.Enabled;
                m_WindowState.Refresh();
            }
            else
            {
                m_WindowState.activateAction = profile.CanBuildLocally() ? ActionState.Enabled : ActionState.Hidden;
                m_WindowState.buildAction = ActionState.Hidden;
                m_WindowState.buildAndRunAction = ActionState.Hidden;
                m_WindowState.Refresh();
            }
        }

        /// <summary>
        /// Callback invoked when <see cref="BuildProfileContext.activeProfile"/> changes.
        /// </summary>
        void OnActiveProfileChanged(BuildProfile prev, BuildProfile cur)
        {
            RebuildProfileListViews();
            UpdateFormButtonState(m_BuildProfileSelection.Get(0));
        }

        void RebuildBuildProfileEditor(BuildProfile profile)
        {
            // Rebuild the BuildProfile inspector, targeting the newly selected BuildProfile.
            DestroyImmediate(buildProfileEditor);
            buildProfileEditor = (BuildProfileEditor)Editor.CreateEditor(profile, typeof(BuildProfileEditor));
            buildProfileEditor.parentState = m_WindowState;
            buildProfileEditor.parent = this;

            m_BuildProfileInspectorElement.Clear();
            m_BuildProfileInspectorElement.Add(buildProfileEditor.CreateInspectorGUI());

            // Builds can only be made for an active BuildProfile,
            // otherwise allow activating the selected profile.
            UpdateFormButtonState(profile);

            // Editor User Build Settings track 'selected' build target group.
            // This was used by different UX to update default build target group tabs
            // based on legacy Build Setting window state.
            BuildProfileModuleUtil.SwitchLegacySelectedBuildTargets(profile);
        }

        void SendWorkflowReport()
        {
            if (m_BuildProfileDataSource.customBuildProfiles.Count == 0)
            {
                EditorAnalytics.SendAnalytic(new BuildProfileWorkflowReport());
                return;
            }

            Dictionary<(BuildTarget, StandaloneBuildSubtarget), BuildProfileWorkflowReport> modules = new();
            foreach (var profile in m_BuildProfileDataSource.customBuildProfiles)
            {
                if (modules.TryGetValue((profile.buildTarget, profile.subtarget), out var report))
                {
                    report.Increment();
                    continue;
                }

                modules.Add((profile.buildTarget, profile.subtarget),
                    new BuildProfileWorkflowReport(new BuildProfileWorkflowReport.Payload()
                {
                    buildTarget = profile.buildTarget,
                    buildTargetString = profile.buildTarget.ToString(),
                    standaloneSubtarget = profile.subtarget,
                    count = 1
                }));
            }

            foreach (var report in modules.Values)
            {
                EditorAnalytics.SendAnalytic(report);
            }
        }

        DropdownButton CreateBuildDropdownButton()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent(TrText.cleanBuild), false,
                () => OnBuildButtonClicked(BuildOptions.ShowBuiltPlayer | BuildOptions.CleanBuildCache));
            menu.AddItem(new GUIContent(TrText.forceSkipDataBuild), false,
                () => OnBuildButtonClicked(BuildOptions.ShowBuiltPlayer | BuildOptions.BuildScriptsOnly));
            return new DropdownButton(TrText.build,
                () => OnBuildButtonClicked(BuildOptions.ShowBuiltPlayer), menu);
        }

        void UpdateToolbarButtonState()
        {
            if (m_AssetImportButton == null)
                return;

            TryLoadWarningIcon();

            m_AssetImportButton.tooltip = AssetImportOverridesWindow.IsAssetImportOverrideEnabled ? TrText.assetImportOverrideTooltip : string.Empty;
            m_AssetImportButton.iconImage = AssetImportOverridesWindow.IsAssetImportOverrideEnabled ? m_WarningIcon : null;
            m_AssetImportButton.text = AssetImportOverridesWindow.IsAssetImportOverrideEnabled ? $" {TrText.assetImportOverrides}" : TrText.assetImportOverrides;
        }

        void TryLoadWarningIcon()
        {
            if (m_WarningIcon == null)
                m_WarningIcon = Background.FromTexture2D(BuildProfileModuleUtil.GetWarningIcon());
        }
    }
}
