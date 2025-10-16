// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using UnityEditor.Build.Profile;
using UnityEditor.Connect;
using UnityEditor.EngineDiagnostics;
using UnityEditor.InsightsEditor.EditorAnalytics;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace UnityEditor.InsightsEditor
{
    [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
    sealed class BuildProfileInsightsSettingsVisualElement : VisualElement
    {
        // UXML and USS paths
        const string k_UxmlPath = "Insights/UXML/ServicesWindow/InsightsSettings.uxml";

        // UXML elements
        const string k_InsightsNoCloudVisualElementNodeName = "insights-analytics-no-cloud-visualelement";
        internal const string k_DataReportingLevelDropdownName = "DataReportingDropdown";
        const string k_HideElementClassName = "display-none";
        const string k_InsightsProjectSettingsHeaderNodeName = "insights-project-settings-header";
        const string k_InsightsNoCloudLabelNodeName = "insights-analytics-no-cloud-projectsettings-label";
        const string k_InsightsNoCloudInfoLabelTextNodeName = "insights-analytics-no-cloud-text";

        VisualElement m_Root;
        DropdownField m_DataReportingLevelDropdown;
        VisualElement m_InsightsNoCloudVisualElement;

        bool m_ProjectSettingsEngineDiagnosticsEnabled;

        int m_PreviousIndexValue;
        bool m_DropdownInitialConfigDone;

        BuildProfile m_buildProfile;

        event Action m_saveAction;

        internal BuildProfileEngineDiagnosticsState buildProfileEngineDiagnosticsState
        {
            get => m_buildProfile.platformBuildProfile.insightsSettingsContainer.buildProfileEngineDiagnosticsState;

            set
            {
                var insightsSettingsContainer = m_buildProfile.platformBuildProfile.insightsSettingsContainer;
                insightsSettingsContainer.buildProfileEngineDiagnosticsState = value;
                m_buildProfile.platformBuildProfile.insightsSettingsContainer = insightsSettingsContainer;
                m_saveAction?.Invoke();
            }
        }

        internal BuildProfile buildProfile
        {
            [VisibleToOtherModules]
            set => m_buildProfile = value;

            [VisibleToOtherModules]
            get => m_buildProfile;
        }

        internal string platformGuid
        {
            get;

            [VisibleToOtherModules]
            set;
        }

        internal string buildProfileName
        {
            get;

            [VisibleToOtherModules]
            set;
        }

        // Change to ordering in UI warrants mapping
        [VisibleToOtherModules]
        internal static readonly ReadOnlyDictionary<int, BuildProfileEngineDiagnosticsState> k_IndexToBuildProfileEngineDiagnosticsState =
            new(new Dictionary<int, BuildProfileEngineDiagnosticsState>
        {
            { 0, BuildProfileEngineDiagnosticsState.ProjectSettings },
            { 1, BuildProfileEngineDiagnosticsState.Disabled },
            { 2, BuildProfileEngineDiagnosticsState.Enabled }
        });
        [VisibleToOtherModules]
        internal static readonly ReadOnlyDictionary<BuildProfileEngineDiagnosticsState, int> k_BuildProfileEngineDiagnosticsStateToIndex = new(
            new Dictionary<BuildProfileEngineDiagnosticsState, int>
        {
            { BuildProfileEngineDiagnosticsState.ProjectSettings, 0 },
            { BuildProfileEngineDiagnosticsState.Disabled, 1 },
            { BuildProfileEngineDiagnosticsState.Enabled, 2 }
        });
        static ReadOnlyDictionary<bool, string> k_EngineDiagnosticsEnabledToStringMap = new(
            new Dictionary<bool, string>
        {
            { false, TrText.k_EngineDiagnosticsStateDropdownDisabled },
            { true, TrText.k_EngineDiagnosticsDropdownEnabled }
        });

        public void InitializeGUI()
        {
            var root = GetRoot();

            var visualTree = EditorGUIUtility.LoadRequired(k_UxmlPath) as VisualTreeAsset;
            if (visualTree == null)
            {
                throw new FileNotFoundException($"Could not find UXML file at path: {k_UxmlPath}");
            }

            var styleSheet = EditorGUIUtility.LoadRequired(InsightsEditorUtils.k_InsightsStyleSheetPath) as StyleSheet;
            if (styleSheet == null)
            {
                throw new FileNotFoundException($"Could not find USS file at path: {InsightsEditorUtils.k_InsightsStyleSheetPath}");
            }

            visualTree.CloneTree(root);
            root.styleSheets.Add(styleSheet);

            m_Root = root;

            SetupInsightsSection(root);
        }

        private void SetupInsightsSection(VisualElement root)
        {

            m_InsightsNoCloudVisualElement = root.Q<VisualElement>(k_InsightsNoCloudVisualElementNodeName);

            m_DataReportingLevelDropdown = root.Q<DropdownField>(k_DataReportingLevelDropdownName);
            m_DataReportingLevelDropdown.label = TrText.k_DataReportingLevelDropdownLabel;

            var projectSettingsHeader = root.Q<VisualElement>(k_InsightsProjectSettingsHeaderNodeName);
            projectSettingsHeader?.RemoveFromHierarchy();
            var projectSettingsInfoBox = root.Q<VisualElement>(InsightsEditorUtils.k_ProjectSettingsInfoBoxNodeName);
            projectSettingsInfoBox?.RemoveFromHierarchy();

            var noCloudInfoLabel = root.Q<Label>(k_InsightsNoCloudLabelNodeName);
            noCloudInfoLabel.text = TrText.k_DataReportingLevelDropdownLabel;

            var noCloudInfoLabelText = root.Q<Label>(k_InsightsNoCloudInfoLabelTextNodeName);
            noCloudInfoLabelText.text = TrText.k_BuildProfileNoCloudLabel;
            InsightsEditorUtils.RegisterLinkTagEventCallbacks(
                noCloudInfoLabelText, OnLinkClicked, InsightsEditorUtils.OnLabelLinkPointerOver, InsightsEditorUtils.OnLabelLinkPointerOut);

            UpdateDropdownChoiceLabels();

            m_DataReportingLevelDropdown.index = k_BuildProfileEngineDiagnosticsStateToIndex[buildProfileEngineDiagnosticsState];
            UpdateSelectedDropdownValueForProjectSettingsSync();

            m_PreviousIndexValue = m_DataReportingLevelDropdown.index;
            m_DataReportingLevelDropdown.RegisterValueChangedCallback(_ =>
            {
                var previousIndex = m_PreviousIndexValue;
                var selectedIndex = m_DataReportingLevelDropdown.index;
                if(selectedIndex == previousIndex)
                {
                    return;
                }

                m_PreviousIndexValue = selectedIndex;
                buildProfileEngineDiagnosticsState = k_IndexToBuildProfileEngineDiagnosticsState[selectedIndex];

                var previousBuildProfileDataReportingLevel = k_IndexToBuildProfileEngineDiagnosticsState[previousIndex];
                var revertSelectionAndShowDisablementPopup = ShouldShowPopup(
                    previousBuildProfileDataReportingLevel,
                    k_IndexToBuildProfileEngineDiagnosticsState[selectedIndex]);
                if (revertSelectionAndShowDisablementPopup)
                {
                    SetDropdownValueAndUnderlyingStateWithoutNotify(previousIndex);
                    ShowDisabledConfirmationDialog(k_IndexToBuildProfileEngineDiagnosticsState[selectedIndex]);
                    return;
                }

                SendOnDataReportingLevelChangedEditorAnalytics(
                    k_IndexToBuildProfileEngineDiagnosticsState[previousIndex],
                    k_IndexToBuildProfileEngineDiagnosticsState[selectedIndex]);
            });

            ToggleSectionVisibility(!string.IsNullOrEmpty(CloudProjectSettings.projectId));

            UnityConnect.instance.ProjectStateChanged -= OnCloudProjectStateChanged;
            UnityConnect.instance.ProjectStateChanged += OnCloudProjectStateChanged;
        }

        internal void SetDropdownValueAndUnderlyingStateWithoutNotify(int previousIndex)
        {
            buildProfileEngineDiagnosticsState = k_IndexToBuildProfileEngineDiagnosticsState[previousIndex];
            m_DataReportingLevelDropdown.SetIndexWithoutNotify(previousIndex);
            m_PreviousIndexValue = previousIndex;
        }

        private void OnLinkClicked(PointerDownLinkTagEvent evt)
        {
            var linkTag = int.Parse(evt.linkID);
            if (linkTag != TrText.k_InsightsLinkTagNoCloudLinkId)
            {
                return;
            }

            SettingsService.OpenProjectSettings("Project/Services");
        }

        bool ShouldShowPopup(BuildProfileEngineDiagnosticsState previousState, BuildProfileEngineDiagnosticsState newState)
        {
            var enabledToDisabledFlag =
                    previousState == BuildProfileEngineDiagnosticsState.Enabled &&
                    newState == BuildProfileEngineDiagnosticsState.Disabled;
            if (enabledToDisabledFlag)
            {
                return true;
            }

            var enabledToProjectSettingsDisabledFlag =
                    previousState == BuildProfileEngineDiagnosticsState.Enabled &&
                    newState == BuildProfileEngineDiagnosticsState.ProjectSettings &&
                    !m_ProjectSettingsEngineDiagnosticsEnabled;
            if (enabledToProjectSettingsDisabledFlag)
            {
                return true;
            }

            var projectSettingsEnabledToDisabledFlag =
                    previousState == BuildProfileEngineDiagnosticsState.ProjectSettings &&
                    newState == BuildProfileEngineDiagnosticsState.Disabled &&
                    m_ProjectSettingsEngineDiagnosticsEnabled;
            return projectSettingsEnabledToDisabledFlag;
        }

        void ShowDisabledConfirmationDialog(BuildProfileEngineDiagnosticsState targetState)
        {
            DisablementPopup.ShowDisabledConfirmationDialog(
                () => OnAcceptDisabledConfirmationDialog(targetState), OnCancelDisabledConfirmationDialog);
        }

        void OnCloudProjectStateChanged(ProjectInfo _)
        {
            var canActivateInsights = !string.IsNullOrEmpty(CloudProjectSettings.projectId);
            ToggleSectionVisibility(canActivateInsights);
        }

        private void SendOnDataReportingLevelChangedEditorAnalytics(
            BuildProfileEngineDiagnosticsState oldState,
            BuildProfileEngineDiagnosticsState newState)
        {
            InsightsEditorAnalytic.LogAppInsights(new InsightsEditorAnalytic.InsightsEditorAnalyticsEvent
            {
                ActionType = InsightsEditorAnalytic.ActionType.ChangeBuildProfileEngineDiagnosticsState,
                interactionContext = new InsightsEditorAnalytic.InteractionContext
                {
                    platformGuid = platformGuid,
                    profileName = buildProfileName
                },
                buildProfileEngineDiagnosticsStateChange = new InsightsEditorAnalytic.BuildProfileEngineDiagnosticsStateChange
                {
                    ToState = newState,
                    FromState = oldState
                }
            });
        }

        [VisibleToOtherModules]
        internal void RegisterSaveAction(Action saveAction)
        {
            m_saveAction -= saveAction;
            m_saveAction += saveAction;
        }

        public void ToggleSectionVisibility(bool isVisible)
        {
            if (isVisible)
            {
                m_InsightsNoCloudVisualElement.AddToClassList(k_HideElementClassName);
                m_DataReportingLevelDropdown.RemoveFromClassList(k_HideElementClassName);
                return;
            }

            m_InsightsNoCloudVisualElement.RemoveFromClassList(k_HideElementClassName);
            m_DataReportingLevelDropdown.AddToClassList(k_HideElementClassName);

        }

        public void UpdateSavedProjectSettingsEngineDiagnosticsEnabledValue(bool projectSettingsValue)
        {
            m_ProjectSettingsEngineDiagnosticsEnabled = projectSettingsValue;
        }

        public void OnProjectSettingsEngineDiagnosticsEnabledChanged(bool projectSettingsValue)
        {
            UpdateSavedProjectSettingsEngineDiagnosticsEnabledValue(projectSettingsValue);
            UpdateDropdownChoiceLabels();
            UpdateSelectedDropdownValueForProjectSettingsSync();

            m_DataReportingLevelDropdown.MarkDirtyRepaint();
            m_Root.MarkDirtyRepaint();
        }

        void UpdateDropdownChoiceLabels()
        {
            if(m_DataReportingLevelDropdown == null)
            {
                return;
            }

            var choices = m_DataReportingLevelDropdown.choices;

            if (!m_DropdownInitialConfigDone)
            {
                choices[k_BuildProfileEngineDiagnosticsStateToIndex[BuildProfileEngineDiagnosticsState.Disabled]] =
                    TrText.k_EngineDiagnosticsStateDropdownDisabled;
                choices[k_BuildProfileEngineDiagnosticsStateToIndex[BuildProfileEngineDiagnosticsState.Enabled]] =
                    TrText.k_EngineDiagnosticsDropdownEnabled;
                m_DropdownInitialConfigDone = true;
            }

            var useProjectSettingsIndex = k_BuildProfileEngineDiagnosticsStateToIndex[BuildProfileEngineDiagnosticsState.ProjectSettings];
            choices[useProjectSettingsIndex] = $"{k_EngineDiagnosticsEnabledToStringMap[m_ProjectSettingsEngineDiagnosticsEnabled]} {TrText.k_DataReportingLevelDropdownUseProjectSettingsGeneric}";
            m_DataReportingLevelDropdown.choices = choices;
        }

        void UpdateSelectedDropdownValueForProjectSettingsSync()
        {
            if (buildProfileEngineDiagnosticsState != BuildProfileEngineDiagnosticsState.ProjectSettings)
            {
                return;
            }

            var useProjectSettingsIndex = k_BuildProfileEngineDiagnosticsStateToIndex[BuildProfileEngineDiagnosticsState.ProjectSettings];
            m_DataReportingLevelDropdown.value = m_DataReportingLevelDropdown.choices[useProjectSettingsIndex];
            m_DataReportingLevelDropdown.SetValueWithoutNotify(m_DataReportingLevelDropdown.choices[useProjectSettingsIndex]);
        }

        void LogDisablementDialogInteraction(InsightsEditorAnalytic.PopupInteraction interaction)
        {
            InsightsEditorAnalytic.LogAppInsights(new InsightsEditorAnalytic.InsightsEditorAnalyticsEvent
            {
                ActionType = InsightsEditorAnalytic.ActionType.DisablementPopupInteraction,
                disablementPopupInteraction = new InsightsEditorAnalytic.DisablementPopupInteraction
                {
                    PopupInteraction = interaction
                },
                interactionContext = new InsightsEditorAnalytic.InteractionContext
                {
                    platformGuid = platformGuid,
                    profileName = buildProfileName
                }
            });
        }

        void OnCancelDisabledConfirmationDialog() =>
            LogDisablementDialogInteraction(InsightsEditorAnalytic.PopupInteraction.Cancel);

        void OnAcceptDisabledConfirmationDialog(BuildProfileEngineDiagnosticsState targetState)
        {
            ChangeBuildProfileEngineDiagnosticsState(targetState);
            LogDisablementDialogInteraction(InsightsEditorAnalytic.PopupInteraction.Accept);
        }

        void ChangeBuildProfileEngineDiagnosticsState(BuildProfileEngineDiagnosticsState newState)
        {
            var oldLevel = buildProfileEngineDiagnosticsState;
            buildProfileEngineDiagnosticsState = newState;
            m_DataReportingLevelDropdown.SetIndexWithoutNotify(k_BuildProfileEngineDiagnosticsStateToIndex[newState]);
            m_PreviousIndexValue = k_BuildProfileEngineDiagnosticsStateToIndex[newState];

            SendOnDataReportingLevelChangedEditorAnalytics(oldLevel, newState);
        }
    }
}
