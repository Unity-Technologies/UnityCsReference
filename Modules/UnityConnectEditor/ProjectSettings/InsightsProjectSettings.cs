// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.EngineDiagnostics;
using UnityEditor.InsightsEditor;
using UnityEditor.InsightsEditor.EditorAnalytics;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace UnityEditor.Connect
{
    internal class InsightsProjectSettings : ServicesProjectSettings
    {
        const string k_ServiceName = "Diagnostics";

        // Actual states for the InsightsProjectSettings state machine
        const string k_StateNameDisabled = "DisabledState";
        const string k_StateNameEnabled = "EnabledState";

        const string k_ProjectSettingsPath = "Project/Services/Diagnostics";

        const string k_InsightsUxmlPath = "Insights/UXML/ServicesWindow/InsightsProjectSettings.uxml";
        const string k_InsightEnabledsUxmlPath = "Insights/UXML/ServicesWindow/InsightsSettings.uxml";
        const string k_InsightsStyleSheetPath = "Insights/StyleSheets/ServicesWindow/InsightsSettings.uss";

        const string k_InsightsNoCloudNodeName = "insights-analytics-no-cloud-visualelement";
        const string k_InsightsSettingsContainerName = "InsightsContentContainer";
        const string k_DropdownFieldNodeName = "DataReportingDropdown";
        const string k_InsightsBuildProfileHeader = "insights-analytics-label";

        const string k_InsightsSettingsTitleLabelNodeName = "project-settings-title";
        const string k_InsightsSettingsTitleDescriptionLabelNodeName = "project-settings-title-description";
        const string k_InsightsBuildSettingsDefaultTitleNodeName = "project-settings-build-settings-default-title";
        const string k_InsightsBuildSettingsDefaultSummaryNodeName = "project-settings-build-settings-default-summary";

        private static readonly string[] k_EngineDiagnosticsStates =
        {
            TrText.k_EngineDiagnosticsStateDropdownDisabled,
            TrText.k_EngineDiagnosticsDropdownEnabled,
        };

        SimpleStateMachine<InsightsProjectSettingsEvent> m_InsightsStateMachine;

        EnabledState m_EnabledState;
        DisabledState m_DisabledState;

        DropdownField m_EngineDiagnosticsDropdown;
        private HelpBox m_DiagnosticsInfoBox;

        internal enum InsightsProjectSettingsEvent
        {
            Enabling,
            Disabling
        }

        private int EnabledStateToIndex(bool enabled) => enabled ? 1 : 0;

        private bool IndexToEnabledState(int index)
        {
            if (index > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return index == 1;
        }

        public InsightsProjectSettings(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, k_ServiceName, keywords)
        {
            m_InsightsStateMachine = new SimpleStateMachine<InsightsProjectSettingsEvent>();
            m_InsightsStateMachine.AddEvent(InsightsProjectSettingsEvent.Enabling);
            m_InsightsStateMachine.AddEvent(InsightsProjectSettingsEvent.Disabling);

            m_DisabledState = new DisabledState(m_InsightsStateMachine, this);
            m_EnabledState = new EnabledState(m_InsightsStateMachine, this);

            m_InsightsStateMachine.AddState(m_DisabledState);
            m_InsightsStateMachine.AddState(m_EnabledState);
        }

        [SettingsProvider]
        public static SettingsProvider CreateServicesProvider()
        {
            return new InsightsProjectSettings(k_ProjectSettingsPath, SettingsScope.Project,
                new List<string> { "Insights", "Analytics" });
        }

        protected override string serviceUssClassName => "insights";

        protected override Notification.Topic[] notificationTopicsToSubscribe => new[]
        {
            Notification.Topic.InsightsService,
            Notification.Topic.ProjectBind
        };

        protected override SingleService serviceInstance => null;

        protected override void ToggleRestrictedVisualElementsAvailability(bool enable) { }

        protected override void ActivateAction(string searchContext)
        {
            var mainTemplate = EditorGUIUtility.Load(k_InsightsUxmlPath) as VisualTreeAsset;
            if (mainTemplate == null)
            {
                throw new Exception("Can't find UXML template for Insights project settings");
            }

            var styleSheet = EditorGUIUtility.Load(k_InsightsStyleSheetPath) as StyleSheet;
            if (styleSheet == null)
            {
                throw new Exception("Can't find USS for Insights project settings");
            }

            var mainContentContainer = mainTemplate.CloneTree().contentContainer;
            mainContentContainer.styleSheets.Add(styleSheet);
            rootVisualElement.Add(mainContentContainer);

            var mainTitleLabel = mainContentContainer.Q<TextElement>(k_InsightsSettingsTitleLabelNodeName);
            mainTitleLabel.text = TrText.k_ProjectSettingsDiagnosticsMainTitle;
            var mainDescriptionLabel = mainContentContainer.Q<Label>(k_InsightsSettingsTitleDescriptionLabelNodeName);
            mainDescriptionLabel.text = TrText.k_ProjectSettingsDiagnosticsMainDescription;
            InsightsEditorUtils.RegisterLinkTagEventCallbacks(
                mainDescriptionLabel, OnLinkClicked, InsightsEditorUtils.OnLabelLinkPointerOver, InsightsEditorUtils.OnLabelLinkPointerOut);

            m_InsightsStateMachine.ClearCurrentState();
            m_InsightsStateMachine.Initialize(m_EnabledState);
        }

        protected override void DeactivateAction() { }

        void SetupVisualElements()
        {
            // UXML document is shared -> perform small adjustments before usage in ProjectSettings
            var noCloudVisualElement = rootVisualElement.Q<VisualElement>(k_InsightsNoCloudNodeName);
            noCloudVisualElement.RemoveFromHierarchy();

            var buildProfileHeader = rootVisualElement.Q<Label>(k_InsightsBuildProfileHeader);
            buildProfileHeader.RemoveFromHierarchy();

            // UI setup
            var defaultBuildSettingsTitle = rootVisualElement.Q<Label>(k_InsightsBuildSettingsDefaultTitleNodeName);
            defaultBuildSettingsTitle.text = TrText.k_ProjectSettingsDiagnosticsDefaultSettings;

            var defaultBuildSettingsSummary = rootVisualElement.Q<Label>(k_InsightsBuildSettingsDefaultSummaryNodeName);
            defaultBuildSettingsSummary.text = TrText.k_ProjectSettingsDiagnosticsDefaultSummary;
            InsightsEditorUtils.RegisterLinkTagEventCallbacks
                (defaultBuildSettingsSummary, OnLinkClicked, InsightsEditorUtils.OnLabelLinkPointerOver, InsightsEditorUtils.OnLabelLinkPointerOut);

            var engineDiagnosticsDropdown = rootVisualElement.Q<DropdownField>(k_DropdownFieldNodeName);
            engineDiagnosticsDropdown.choices = new List<string>(k_EngineDiagnosticsStates);
            m_EngineDiagnosticsDropdown = engineDiagnosticsDropdown;

            var storedEngineDiagnosticsEnabledValue = EngineDiagnostics.EngineDiagnosticsSettings.GetEngineDiagnosticsEnabledDefaultBuildValue();
            engineDiagnosticsDropdown.SetIndexWithoutNotify(EnabledStateToIndex(storedEngineDiagnosticsEnabledValue));
            engineDiagnosticsDropdown.label = TrText.k_DataReportingLevelDropdownLabel;
            engineDiagnosticsDropdown.RegisterValueChangedCallback(evt =>
            {
                var oldReportingLevel = EngineDiagnostics.EngineDiagnosticsSettings.GetEngineDiagnosticsEnabledDefaultBuildValue();
                var selectedIndex = engineDiagnosticsDropdown.index;
                var engineDiagnosticsEnabled = IndexToEnabledState(selectedIndex);
                if (engineDiagnosticsEnabled == oldReportingLevel)
                {
                    return;
                }

                // If the user chooses to disable data reporting, do not change state
                // right away. Instead, revert to previous selection + show confirmation
                // popup for disablement (value changes on confirm).
                if (!engineDiagnosticsEnabled)
                {
                    engineDiagnosticsDropdown.SetIndexWithoutNotify(EnabledStateToIndex(true));
                    DisablementPopup.ShowDisabledConfirmationDialog(
                        OnDisabledConfirmed, OnDisabledCanceled);
                    return;
                }

                engineDiagnosticsDropdown.SetIndexWithoutNotify(1);
                EngineDiagnostics.EngineDiagnosticsSettings.enabled = true;
                SetInfoBoxText();
                SendDataReportingLevelChangeEditorAnalytics(false, true);
            });

            m_DiagnosticsInfoBox = rootVisualElement.Q<HelpBox>(InsightsEditorUtils.k_ProjectSettingsInfoBoxNodeName);
            InsightsEditorUtils.RegisterLinkTagEventCallbacks(m_DiagnosticsInfoBox,
                OnLinkClicked, InsightsEditorUtils.OnLabelLinkPointerOver, InsightsEditorUtils.OnLabelLinkPointerOut);
            SetInfoBoxText();
        }

        private void SetInfoBoxText()
        {
            var isEnabled = EngineDiagnosticsSettings.GetEngineDiagnosticsEnabledDefaultBuildValue();

            m_DiagnosticsInfoBox.text = isEnabled ?
                TrText.k_ProjectSettingsDiagnosticsInfoTextEnabled :
                TrText.k_ProjectSettingsDiagnosticsInfoTextDisabled;
            m_DiagnosticsInfoBox.messageType = isEnabled ?
                HelpBoxMessageType.Info :
                HelpBoxMessageType.Warning;
        }

        private void OnLinkClicked(PointerDownLinkTagEvent evt)
        {
            var linkTag = int.Parse(evt.linkID);
            switch (linkTag)
            {
                case TrText.k_InsightsLinkTagDefaultSummaryLinkId:
                    Application.OpenURL(TrText.k_ProjectSettingsDiagnosticsDefaultSummaryLink);
                    break;
                case TrText.k_InsightsLinkTagMainDescriptionLinkId:
                    Application.OpenURL(TrText.k_ProjectSettingsDiagnosticsMainDescriptionLink);
                    break;
                case TrText.k_InsightsLinkTagProjectSettingsInfoBoxEnabledLinkId:
                    Application.OpenURL(TrText.k_ProjectSettingsDiagnosticsInfoTextEnabledLink);
                    break;
                case TrText.k_InsightsLinkTagProjectSettingsInfoBoxDisabledLinkId:
                    Application.OpenURL(TrText.k_ProjectSettingsDiagnosticsInfoTextDisabledLink);
                    break;
            }
        }

        private void OnDisabledCanceled()
        {
            SendDisablementPopupInteractionEditorAnalytics(InsightsEditorAnalytic.PopupInteraction.Cancel);
        }

        private void OnDisabledConfirmed()
        {
            var previousIndex = m_EngineDiagnosticsDropdown.index;
            m_EngineDiagnosticsDropdown.SetIndexWithoutNotify(0);
            EngineDiagnostics.EngineDiagnosticsSettings.enabled = false;
            SetInfoBoxText();
            SendDisablementPopupInteractionEditorAnalytics(InsightsEditorAnalytic.PopupInteraction.Accept);
            SendDataReportingLevelChangeEditorAnalytics(true, false);
        }

        void SendDataReportingLevelChangeEditorAnalytics(bool oldValue, bool newValue)
        {
            InsightsEditorAnalytic.LogAppInsights(new InsightsEditorAnalytic.InsightsEditorAnalyticsEvent
            {
                ActionType = InsightsEditorAnalytic.ActionType.ChangeEngineDiagnosticsEnabled,
                projectSettingsEngineDiagnosticsEnabledChange = new InsightsEditorAnalytic.ProjectSettingsEngineDiagnosticsEnabledChange
                {
                    FromValue = oldValue,
                    ToValue = newValue
                }
            });
        }

        void SendDisablementPopupInteractionEditorAnalytics(InsightsEditorAnalytic.PopupInteraction interaction)
        {
            InsightsEditorAnalytic.LogAppInsights(new InsightsEditorAnalytic.InsightsEditorAnalyticsEvent
            {
                ActionType = InsightsEditorAnalytic.ActionType.DisablementPopupInteraction,
                disablementPopupInteraction = new InsightsEditorAnalytic.DisablementPopupInteraction
                {
                    PopupInteraction = interaction
                }
            });
        }

        sealed class DisabledState : SimpleStateMachine<InsightsProjectSettingsEvent>.State
        {
            InsightsProjectSettings m_Provider;

            public DisabledState(SimpleStateMachine<InsightsProjectSettingsEvent> simpleStateMachine, InsightsProjectSettings provider)
                : base(k_StateNameDisabled, simpleStateMachine)
            {
                m_Provider = provider;
                ModifyActionForEvent(InsightsProjectSettingsEvent.Enabling, HandleBinding);
            }

            public override void EnterState()
            {
                var container = m_Provider.rootVisualElement.Q(k_InsightsSettingsContainerName);
                container?.Clear();
            }

            private SimpleStateMachine<InsightsProjectSettingsEvent>.State HandleBinding(InsightsProjectSettingsEvent raisedEvent)
            {
                return stateMachine.GetStateByName(k_StateNameEnabled);
            }
        }

        sealed class EnabledState : SimpleStateMachine<InsightsProjectSettingsEvent>.State
        {
            InsightsProjectSettings m_Provider;

            public EnabledState(SimpleStateMachine<InsightsProjectSettingsEvent> simpleStateMachine, InsightsProjectSettings provider)
                : base(k_StateNameEnabled, simpleStateMachine)
            {
                m_Provider = provider;
                ModifyActionForEvent(InsightsProjectSettingsEvent.Disabling, HandleUnbinding);
            }

            public override void EnterState()
            {
                var generalTemplate = EditorGUIUtility.Load(k_InsightEnabledsUxmlPath) as VisualTreeAsset;
                if(generalTemplate == null)
                {
                    throw new Exception("Can't find UXML template for Insights project settings");
                }

                var container = m_Provider.rootVisualElement.Q(k_InsightsSettingsContainerName);
                if (container == null)
                {
                    throw new Exception("Can't find Insights settings container");
                }

                var contentContainer = generalTemplate.CloneTree().contentContainer;
                ServicesUtils.TranslateStringsInTree(contentContainer);
                container.Clear();
                container.Add(contentContainer);

                m_Provider.SetupVisualElements();
                InsightsEditorAnalytic.LogAppInsights(new InsightsEditorAnalytic.InsightsEditorAnalyticsEvent
                {
                    ActionType = InsightsEditorAnalytic.ActionType.EnterProjectSettingsMenu
                });
            }

            private SimpleStateMachine<InsightsProjectSettingsEvent>.State HandleUnbinding(InsightsProjectSettingsEvent raisedEvent)
            {
                return stateMachine.GetStateByName(k_StateNameDisabled);
            }
        }
    }
}
