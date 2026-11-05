// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
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
        const string k_InsightsNoCloudInfoLabelTextNodeName = "insights-analytics-no-cloud-text";

        const int k_EnabledIndex = 0;
        const int k_DisabledIndex = 1;

        DropdownField m_DataReportingLevelDropdown;
        VisualElement m_InsightsNoCloudVisualElement;

        BuildProfile m_buildProfile;

        event Action m_saveAction;

        bool m_IsCloudProjectLinked;

        internal event Action dataCollectionChanged;

        internal BuildProfileEngineDiagnosticsState buildProfileEngineDiagnosticsState
        {
            get => m_buildProfile.platformBuildProfile.insightsSettingsContainer.buildProfileEngineDiagnosticsState;

            set
            {
                var insightsSettingsContainer = m_buildProfile.platformBuildProfile.insightsSettingsContainer;
                insightsSettingsContainer.buildProfileEngineDiagnosticsState = value;
                m_buildProfile.platformBuildProfile.insightsSettingsContainer = insightsSettingsContainer;
                m_saveAction?.Invoke();
                EngineDiagnosticsSettings.InvalidatePlayerDataCache(m_buildProfile.buildTarget);
                ActiveBuildProfileChangeNotifier.Notify();
                dataCollectionChanged?.Invoke();
            }
        }

        internal bool isDataCollectionEnabled =>
            m_IsCloudProjectLinked && buildProfileEngineDiagnosticsState != BuildProfileEngineDiagnosticsState.Disabled;

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

        // Default is shown as Enabled and is kept until the user picks Disabled
        internal static int StateToIndex(BuildProfileEngineDiagnosticsState state) =>
            state == BuildProfileEngineDiagnosticsState.Disabled ? k_DisabledIndex : k_EnabledIndex;

        internal static BuildProfileEngineDiagnosticsState IndexToState(int index) =>
            index == k_DisabledIndex ? BuildProfileEngineDiagnosticsState.Disabled : BuildProfileEngineDiagnosticsState.Enabled;

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

            SetupInsightsSection(root);
        }

        private void SetupInsightsSection(VisualElement root)
        {

            m_InsightsNoCloudVisualElement = root.Q<VisualElement>(k_InsightsNoCloudVisualElementNodeName);

            m_DataReportingLevelDropdown = root.Q<DropdownField>(k_DataReportingLevelDropdownName);
            m_DataReportingLevelDropdown.label = TrText.k_DataReportingLevelDropdownLabel;
            m_DataReportingLevelDropdown.labelElement.tooltip = TrText.k_DataReportingLevelDropdownTooltip;

            var noCloudInfoLabelText = root.Q<Label>(k_InsightsNoCloudInfoLabelTextNodeName);
            noCloudInfoLabelText.text = TrText.k_BuildProfileNoCloudLabel;
            InsightsEditorUtils.RegisterLinkTagEventCallbacks(
                noCloudInfoLabelText, OnLinkClicked, InsightsEditorUtils.OnLabelLinkPointerOver, InsightsEditorUtils.OnLabelLinkPointerOut);

            m_DataReportingLevelDropdown.choices = new List<string>
            {
                TrText.k_EngineDiagnosticsDropdownEnabled,
                TrText.k_EngineDiagnosticsStateDropdownDisabled,
            };
            m_DataReportingLevelDropdown.SetIndexWithoutNotify(StateToIndex(buildProfileEngineDiagnosticsState));
            m_DataReportingLevelDropdown.RegisterValueChangedCallback(_ => OnDropdownIndexChanged(m_DataReportingLevelDropdown.index));

            ToggleSectionVisibility(!string.IsNullOrEmpty(CloudProjectSettings.projectId));

            UnityConnect.instance.ProjectStateChanged -= OnCloudProjectStateChanged;
            UnityConnect.instance.ProjectStateChanged += OnCloudProjectStateChanged;
        }

        void OnDropdownIndexChanged(int index)
        {
            var previousState = buildProfileEngineDiagnosticsState;
            if (StateToIndex(previousState) == index)
                return;

            var newState = IndexToState(index);
            buildProfileEngineDiagnosticsState = newState;
            SendOnDataReportingLevelChangedEditorAnalytics(previousState, newState);
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
            m_IsCloudProjectLinked = isVisible;
            m_InsightsNoCloudVisualElement.EnableInClassList(k_HideElementClassName, isVisible);
            m_DataReportingLevelDropdown.EnableInClassList(k_HideElementClassName, !isVisible);
            dataCollectionChanged?.Invoke();
        }
    }
}
