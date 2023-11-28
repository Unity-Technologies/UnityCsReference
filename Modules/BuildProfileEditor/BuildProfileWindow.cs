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
    ///
    /// TODO EPIC: https://jira.unity3d.com/browse/PLAT-5878
    /// </summary>
    [EditorWindowTitle(title = "Build Settings")]
    internal class BuildProfileWindow : EditorWindow
    {
        const string k_DevOpsUrl = "https://unity.com/products/unity-devops?utm_medium=desktop-app&utm_source=unity-editor-window-menu&utm_content=buildsettings";
        const string k_Uxml = "BuildProfile/UXML/BuildProfileWindow.uxml";

        internal const string buildProfileClassicPlatformVisualElement = "build-profile-classic-platforms";
        internal const string buildProfileClassicPlatformMissingVisualElement = "build-profile-classic-platforms-missing";
        internal const string buildProfileInspectorVisualElement = "build-profile-editor-inspector";
        internal const string buildProfilesVisualElement = "custom-build-profiles";

        internal BuildProfileEditor buildProfileEditor;
        BuildProfileWorkflowState m_WindowState;

        bool m_ShouldAskForBuildLocation = true;
        int m_ActiveProfileListIndex;

        BuildProfileDataSource m_BuildProfileDataSource;

        /// <summary>
        /// Left column classic profile list view.
        /// </summary>
        ListView m_BuildProfileClassicPlatformListView;
        ListView m_MissingClassicPlatformListView;

        /// <summary>
        /// Left column custom build profile list view.
        /// </summary>
        ListView m_BuildProfilesListView;
        VisualElement m_WelcomeMessageElement;

        VisualElement m_InspectorHeader;

        /// <summary>
        /// Build Profile inspector for the selected classic platform or profile,
        /// repainted on <see cref="m_BuildProfileClassicPlatformListView"/> selection change.
        /// </summary>
        /// <see cref="OnClassicPlatformSelected"/>
        ScrollView m_BuildProfileInspectorElement;
        Image m_SelectedProfileImage;
        Label m_SelectedProfileNameLabel;
        Label m_SelectedProfilePlatformLabel;
        BuildProfile m_SelectedBuildProfile;

        VisualElement m_AdditionalActionsDropdown;
        DropdownButton m_BuildButton;
        Button m_BuildAndRunButton;
        Button m_ActivateButton;
        Button m_TempOpenLegacyBuildSettingsButton;
        Button m_ClassicSceneListButton;

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

            // Capture static visual element reference.
            m_AdditionalActionsDropdown = rootVisualElement.Q<VisualElement>("additional-actions-dropdown-button");
            m_BuildButton = CreateBuildDropdownButton();
            rootVisualElement.Q<VisualElement>("build-dropdown-button").Add(m_BuildButton);
            m_BuildProfileInspectorElement = rootVisualElement.Q<ScrollView>(buildProfileInspectorVisualElement);
            m_BuildProfileClassicPlatformListView = rootVisualElement.Q<ListView>(buildProfileClassicPlatformVisualElement);
            m_BuildProfilesListView = rootVisualElement.Q<ListView>(buildProfilesVisualElement);
            m_SelectedProfileImage = rootVisualElement.Q<Image>("selected-profile-image");
            m_SelectedProfileNameLabel = rootVisualElement.Q<Label>("selected-profile-name");
            m_SelectedProfilePlatformLabel = rootVisualElement.Q<Label>("selected-profile-platform");
            m_BuildAndRunButton = rootVisualElement.Q<Button>("build-and-run-button");
            m_ActivateButton = rootVisualElement.Q<Button>("activate-button");
            m_TempOpenLegacyBuildSettingsButton = rootVisualElement.Q<Button>("temp-open-legacy-window-button");
            m_WelcomeMessageElement = rootVisualElement.Q<VisualElement>("fallback-no-custom-build-profiles");
            m_ClassicSceneListButton = rootVisualElement.Q<Button>("classic-scenes-in-build-button");
            m_InspectorHeader = rootVisualElement.Q<VisualElement>("build-profile-editor-header");

            // Apply localized text to static elements.
            rootVisualElement.Q<Label>("platforms-label").text = TrText.platforms;
            rootVisualElement.Q<Label>("build-profiles-label").text = TrText.buildProfilesName;
            rootVisualElement.Q<Label>("fallback-welcome-label").text = TrText.buildProfileWelcome;
            addBuildProfileButton.text = TrText.addBuildProfile;
            unityDevOpsButton.text = TrText.learnMoreUnityDevOps;
            listViewAddProfileButton.text = TrText.addBuildProfile;
            m_ActivateButton.text = TrText.activate;
            m_BuildAndRunButton.text = TrText.buildAndRun;
            m_ClassicSceneListButton.text = TrText.sceneList;

            // Build dynamic visual elements.
            CreateBuildProfileListView(m_BuildProfileClassicPlatformListView, m_BuildProfileDataSource.classicPlatforms, false);
            CreateBuildProfileListView(m_BuildProfilesListView, m_BuildProfileDataSource.customBuildProfiles, true);
            m_BuildProfileClassicPlatformListView.selectionChanged += OnBuildProfileOrClassicPlatformSelected;
            m_BuildProfilesListView.selectionChanged += OnBuildProfileOrClassicPlatformSelected;
            CreateMissingClassicPlatformListView();

            if (m_BuildProfileDataSource.customBuildProfiles.Count > 0)
            {
                m_BuildProfilesListView.Show();
                m_WelcomeMessageElement.Hide();
            }
            SelectActiveProfile();


            // Set up event handlers.
            m_BuildAndRunButton.clicked += () =>
            {
                OnBuildButtonClicked(BuildOptions.AutoRunPlayer | BuildOptions.StrictMode);
            };
            m_ActivateButton.clicked += OnActivateButtonClicked;
            m_ClassicSceneListButton.clicked += OnClassicSceneListSelected;
            addBuildProfileButton.clicked += PlatformDiscoveryWindow.ShowWindow;
            listViewAddProfileButton.clicked += PlatformDiscoveryWindow.ShowWindow;
            unityDevOpsButton.clicked += () =>
            {
                Application.OpenURL(k_DevOpsUrl);
            };

            // TODO: Temporarily allow showing legacy window.
            // jira: https://jira.unity3d.com/browse/PLAT-7025
            m_TempOpenLegacyBuildSettingsButton.clicked += BuildPlayerWindow.ShowBuildPlayerWindow;
            BuildProfileContext.instance.activeProfileChanged += OnActiveProfileChanged;
        }

        void CreateMissingClassicPlatformListView()
        {
            var itemSource = BuildProfileContext.instance.GetMissingKnownPlatformModules();
            m_MissingClassicPlatformListView = rootVisualElement.Q<ListView>(buildProfileClassicPlatformMissingVisualElement);

            if (itemSource.Count == 0)
            {
                m_MissingClassicPlatformListView.Hide();
                return;
            }

            m_MissingClassicPlatformListView.itemsSource = itemSource;
            m_MissingClassicPlatformListView.Q<ScrollView>().verticalScrollerVisibility = ScrollerVisibility.Hidden;
            m_MissingClassicPlatformListView.makeItem = () =>
            {
                var label = new BuildProfileListLabel();
                label.AddToClassList("pl-large");
                label.AddToClassList("lhs-sidebar-item-faded");
                return label;
            };
            m_MissingClassicPlatformListView.bindItem = (VisualElement element, int index) =>
            {
                var (buildTarget, subtarget) = itemSource[index];
                var buildProfileLabel = element as BuildProfileListLabel;
                UnityEngine.Assertions.Assert.IsNotNull(buildProfileLabel, "Build definition label is null");

                var icon = BuildProfileModuleUtil.GetPlatformIcon(buildTarget, subtarget);
                buildProfileLabel.Set(BuildProfileModuleUtil.GetClassicPlatformDisplayName(buildTarget, subtarget), icon);
            };
            m_MissingClassicPlatformListView.selectionChanged += OnMissingClassicPlatformSelected;
        }

        public void OnEnable()
        {
            m_BuildProfileDataSource = new BuildProfileDataSource(this);
        }

        public void OnDisable()
        {
            m_BuildProfileDataSource.Dispose();
            BuildProfileContext.instance.activeProfileChanged -= OnActiveProfileChanged;
        }

        /// <summary>
        /// Rebuild the custom profile list view, or show the welcome message if no custom
        /// profiles exist.
        /// </summary>
        internal void RebuildProfileListViews()
        {
            if (m_BuildProfileDataSource.customBuildProfiles.Count == 0)
            {
                m_BuildProfilesListView.Hide();
                m_WelcomeMessageElement.Show();
            }
            else
            {
                m_BuildProfilesListView.Show();
                m_WelcomeMessageElement.Hide();
            }

            m_BuildProfileClassicPlatformListView.Rebuild();
            m_BuildProfilesListView.Rebuild();
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

                    if(firstEnabledIndex < 0)
                        firstEnabledIndex = i;
                    else
                        menu.AddItem(new GUIContent(action.displayName), false, () => action.callback());
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

        /// <summary>
        /// Show classic platform UI for globally shared settings.
        /// </summary>
        void OnClassicSceneListSelected()
        {
            m_BuildProfileClassicPlatformListView.ClearSelection();
            m_BuildProfilesListView.ClearSelection();

            DestroyImmediate(buildProfileEditor);
            buildProfileEditor = ScriptableObject.CreateInstance<BuildProfileEditor>();
            m_BuildProfileInspectorElement.Clear();
            m_BuildProfileInspectorElement.Add(buildProfileEditor.CreateLegacyGUI());
            m_InspectorHeader.Hide();
        }

        /// <summary>
        /// Handle selection of build profile item, creates embedded inspector and clears
        /// adjacent list views.
        /// </summary>
        void OnBuildProfileOrClassicPlatformSelected(IEnumerable<object> selectedItems)
        {
            using var item = selectedItems.GetEnumerator();
            if (!item.MoveNext())
                return;

            // Clear null profiles in data source.
            bool shouldRepaint = m_BuildProfileDataSource.ClearDeletedProfiles();
            var profile = item.Current as BuildProfile;
            if (profile == null)
            {
                RepaintAndClearSelection(profile);
                Debug.LogWarning("BuildProfileWindow: selected item has been deleted?");
                return;
            }

            m_InspectorHeader.Show();
            m_MissingClassicPlatformListView.ClearSelection();

            // Selected profile could be a custom or classic platform.
            // Classic platforms only display the platform name, while custom show file name and platform name.
            // When a selection is made, we clear the currently selection in the opposite list view.
            m_SelectedBuildProfile = profile;
            m_SelectedProfileImage.image = BuildProfileModuleUtil.GetPlatformIcon(profile.moduleName, profile.subtarget);
            if (BuildProfileContext.IsClassicPlatformProfile(profile))
            {
                m_SelectedProfileNameLabel.text = BuildProfileModuleUtil.GetClassicPlatformDisplayName(
                    profile.moduleName, profile.subtarget);
                m_SelectedProfilePlatformLabel.Hide();
                m_BuildProfilesListView.ClearSelection();
            }
            else
            {
                m_SelectedProfileNameLabel.text = profile.name;
                m_SelectedProfilePlatformLabel.text = BuildProfileModuleUtil.GetClassicPlatformDisplayName(
                    profile.moduleName, profile.subtarget);
                m_SelectedProfilePlatformLabel.Hide();
                m_BuildProfileClassicPlatformListView.ClearSelection();
            }

            // Rebuild the BuildProfile inspector, targeting the newly selected BuildProfile.
            DestroyImmediate(buildProfileEditor);
            buildProfileEditor = (BuildProfileEditor) Editor.CreateEditor(profile, typeof(BuildProfileEditor));
            buildProfileEditor.parentState = m_WindowState;
            m_BuildProfileInspectorElement.Clear();
            m_BuildProfileInspectorElement.Add(buildProfileEditor.CreateInspectorGUI());

            // Builds can only be made for an active BuildProfile,
            // otherwise allow activating the selected profile.
            UpdateFormButtonState(profile);

            // Editor User Build Settings track 'selected' build target group.
            // This was used by different UX to update default build target group tabs
            // based on legacy Build Setting window state.
            BuildProfileModuleUtil.SwitchLegacySelectedBuildTargets(profile);
            if (shouldRepaint)
                RebuildProfileListViews();
        }

        /// <summary>
        /// Handles selection of unavailable and supported platform.
        /// </summary>
        void OnMissingClassicPlatformSelected(IEnumerable<object> selectedItems)
        {
            using var item = selectedItems.GetEnumerator();
            if (!item.MoveNext())
                return;

            if (item.Current is not (string moduleName, StandaloneBuildSubtarget subtarget))
            {
                return;
            }

            m_BuildProfilesListView.ClearSelection();
            m_BuildProfileClassicPlatformListView.ClearSelection();
            UpdateFormButtonState(null);

            // Render platform requirement helpbox instead o default inspector.
            m_BuildProfileInspectorElement.Clear();
            var warningHelpBox = new HelpBox()
            {
                messageType = HelpBoxMessageType.Warning
            };
            warningHelpBox.AddToClassList("mx-medium");
            Util.UpdatePlatformRequirementsWarningHelpBox(warningHelpBox, moduleName, subtarget);
            m_BuildProfileInspectorElement.Add(warningHelpBox);

            // Update details headers.
            m_SelectedBuildProfile = null;
            m_SelectedProfileImage.image = BuildProfileModuleUtil.GetPlatformIcon(moduleName, subtarget);
            m_SelectedProfileNameLabel.text = BuildProfileModuleUtil.GetClassicPlatformDisplayName(moduleName, subtarget);
            m_SelectedProfilePlatformLabel.Hide();
        }

        void OnActivateButtonClicked()
        {
            if (m_SelectedBuildProfile == null)
                return;

            if (IsActiveBuildProfileOrPlatform(m_SelectedBuildProfile))
                return;

            // Classic profiles should not be set as active, they are identified
            // by the state of EditorUserBuildSettings active build target.
            BuildProfileContext.instance.activeProfile = !BuildProfileContext.IsClassicPlatformProfile(m_SelectedBuildProfile)
                ? m_SelectedBuildProfile : null;
            BuildProfileModuleUtil.SwitchLegacyActiveFromBuildProfile(m_SelectedBuildProfile);

            UpdateFormButtonState(m_SelectedBuildProfile);
            RebuildProfileListViews();
        }

        void OnBuildButtonClicked(BuildOptions optionFlags)
        {
            if (m_SelectedBuildProfile == null)
                return;

            if (!IsActiveBuildProfileOrPlatform(m_SelectedBuildProfile))
            {
                Debug.LogWarning("[BuildProfile] Attempted to build with a non-active build profile.");
                return;
            }

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
            else if (IsActiveBuildProfileOrPlatform((profile)))
            {
                m_WindowState.activateAction = ActionState.Hidden;
                m_WindowState.buildAction = ActionState.Enabled;
                m_WindowState.buildAndRunAction = ActionState.Enabled;
                m_WindowState.Refresh();
            }
            else
            {
                m_WindowState.activateAction = (profile.platformBuildProfile != null)
                    ? ActionState.Enabled : ActionState.Hidden;
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
            // Handle selected build profile is deleted.
            if (prev == m_SelectedBuildProfile && cur is null)
            {
                SelectActiveProfile();
            }

            m_BuildProfileClassicPlatformListView.Rebuild();
            RebuildProfileListViews();
            UpdateFormButtonState(m_SelectedBuildProfile);
        }

        /// <summary>
        /// Selects the active build profile. Must be called after the list views have been updated,
        /// as <see cref="m_ActiveProfileListIndex"/> is dependent on latest list view state.
        /// </summary>
        void SelectActiveProfile()
        {
            if (m_ActiveProfileListIndex < 0)
            {
                m_BuildProfileClassicPlatformListView.SetSelection(0);
                Debug.LogWarning("[BuildProfile] Failed to find an active profile.");
                return;
            }

            if (BuildProfileContext.instance.activeProfile is not null
                && m_ActiveProfileListIndex < m_BuildProfilesListView.itemsSource.Count)
            {
                m_BuildProfilesListView.SetSelection(m_ActiveProfileListIndex);
            }
            else if (m_ActiveProfileListIndex < m_BuildProfileClassicPlatformListView.itemsSource.Count)
            {
                m_BuildProfileClassicPlatformListView.SetSelection(m_ActiveProfileListIndex);
            }
            else
            {
                Debug.LogWarning("[BuildProfile] Active profile not found in build profile window data source.");
            }
        }

        /// <summary>
        /// Repaints the list views and clears the selection.
        /// </summary>
        void RepaintAndClearSelection(BuildProfile deletedProfile)
        {
            RebuildProfileListViews();

            // Clear selection as the newly selected profile may occupy the same
            // index as the deleted profile. Causing the list view to miss the update.
            m_BuildProfilesListView.ClearSelection();
            m_BuildProfileClassicPlatformListView.ClearSelection();
            SelectActiveProfile();
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

        void CreateBuildProfileListView(ListView target, IList<BuildProfile> itemSource, bool useNameAsDisplayName)
        {
            target.Q<ScrollView>().verticalScrollerVisibility = ScrollerVisibility.Hidden;
            target.itemsSource = (System.Collections.IList) itemSource;
            target.makeItem = () =>
            {
                var label = new BuildProfileListLabel();
                label.AddToClassList("pl-large");
                return label;
            };
            target.bindItem = (VisualElement element, int index) =>
            {
                var profile = itemSource[index];
                var buildProfileLabel = element as BuildProfileListLabel;
                UnityEngine.Assertions.Assert.IsNotNull(profile, "Build definition is null");
                UnityEngine.Assertions.Assert.IsNotNull(buildProfileLabel, "Build definition label is null");

                string platformDisplayName = useNameAsDisplayName
                    ? profile.name
                    : BuildProfileModuleUtil.GetClassicPlatformDisplayName(profile.moduleName, profile.subtarget);
                var icon = BuildProfileModuleUtil.GetPlatformIcon(profile.moduleName, profile.subtarget);
                buildProfileLabel.Set(platformDisplayName, icon);

                if (IsActiveBuildProfileOrPlatform(profile))
                {
                    buildProfileLabel.SetActiveIndicator(true);
                    m_ActiveProfileListIndex = index;
                }
                else
                    buildProfileLabel.SetActiveIndicator(false);
            };
        }

        /// <summary>
        /// Returns true if the given <see cref="BuildProfile"/> is the active profile or a classic
        /// profile for the EditorUserBuildSettings active build target.
        /// </summary>
        static bool IsActiveBuildProfileOrPlatform(BuildProfile profile)
        {
            if (BuildProfileContext.instance.activeProfile == profile)
                return true;

            if (BuildProfileContext.instance.activeProfile is not null
                || !BuildProfileContext.IsClassicPlatformProfile(profile))
                return false;

            if (!BuildProfileModuleUtil.IsStandalonePlatform(profile.buildTarget))
                return profile.buildTarget == EditorUserBuildSettings.activeBuildTarget;

            return profile.buildTarget == EditorUserBuildSettings.activeBuildTarget &&
                profile.subtarget == EditorUserBuildSettings.standaloneBuildSubtarget;
        }
    }
}
