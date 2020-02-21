// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.CrashReporting;
using UnityEditor.UIElements;

namespace UnityEditor.Connect
{
    internal class CloudDiagProjectSettings : ServicesProjectSettings
    {
        const string k_ServiceName = "Game Performance";

        // Actual states for the Cloud Diag state machine
        const string k_StateNameDisabled = "DisabledState";
        const string k_StateNameEnabled = "EnabledState";

        // Related UXML files
        const string k_CloudDiagCommonUxmlPath = "UXML/ServicesWindow/CloudDiagProjectSettings.uxml";
        const string k_CloudDiagUserReportUxmlPath = "UXML/ServicesWindow/CloudDiagProjectSettingsUserReport.uxml";
        const string k_CloudDiagCrashCommonUxmlPath = "UXML/ServicesWindow/CloudDiagProjectSettingsCrash.uxml";
        const string k_CloudDiagCrashEnabledUxmlPath = "UXML/ServicesWindow/CloudDiagProjectSettingsCrashEnabled.uxml";

        const string k_ServiceNameProperty = "serviceName";

        // Elements of the UXML
        const string k_ServiceToggleClassName = "service-toggle";
        const string k_CloudDiagCrashContainerClassName = "cloud-diag-crash";
        const string k_CloudDiagUserReportContainerClassName = "cloud-diag-user-report";

        const string k_CloudDiagCrashGoToDashboardName = "GoToDashboard";
        const string k_CloudDiagCrashStateName = "CloudDiagCrashStateContainer";
        const string k_LearnMoreLink = "LearnMore";
        const string k_CrashCapturePlayMode = "CrashCapturePlayMode";
        const string k_CrashBufferLogCount = "CrashLogCount";

        // These values are from the old services window min/max implementation.
        const int k_CrashBufferLogMinimum = 0;
        const int k_CrashBufferLogMaximum = 50;

        const string k_UserReportingDownloadSdk = "DownloadButtonSection";

        const string k_CloudDiagPermissionMessage = "You do not have sufficient permissions to enable / disable Cloud Diagnostic Crash service.";

        SimpleStateMachine<CloudDiagCrashEvent> m_CrashStateMachine;
        EnabledState m_EnabledState;
        DisabledState m_DisabledState;

        bool m_EventHandlerInitialized;
        Toggle m_CrashServiceToggle;
        VisualElement m_CrashServiceGoToDashboard;

        internal enum CloudDiagCrashEvent
        {
            Enabling,
            Disabling,
        }

        [SettingsProvider]
        public static SettingsProvider CreateServicesProvider()
        {
            return new CloudDiagProjectSettings(CrashService.instance.projectSettingsPath, SettingsScope.Project);
        }

        public CloudDiagProjectSettings(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, k_ServiceName, keywords)
        {
            m_CrashStateMachine = new SimpleStateMachine<CloudDiagCrashEvent>();
            m_CrashStateMachine.AddEvent(CloudDiagCrashEvent.Enabling);
            m_CrashStateMachine.AddEvent(CloudDiagCrashEvent.Disabling);

            m_EnabledState = new EnabledState(m_CrashStateMachine, this);
            m_DisabledState = new DisabledState(m_CrashStateMachine, this);

            m_CrashStateMachine.AddState(m_EnabledState);
            m_CrashStateMachine.AddState(m_DisabledState);
        }

        void OnDestroy()
        {
            UnregisterEvent();
        }

        protected override Notification.Topic[] notificationTopicsToSubscribe => new[]
        {
            Notification.Topic.CrashService,
            Notification.Topic.ProjectBind
        };
        protected override SingleService serviceInstance => CrashService.instance;
        protected override string serviceUssClassName => "clouddiag";

        void SetupServiceToggle()
        {
            m_CrashServiceToggle.SetProperty(k_ServiceNameProperty, CrashService.instance.name);
            m_CrashServiceToggle.SetEnabled(false);
            UpdateServiceToggleAndDashboardLink(CrashService.instance.IsServiceEnabled());

            m_CrashServiceToggle.RegisterValueChangedCallback(evt =>
            {
                if (currentUserPermission != UserRole.Owner && currentUserPermission != UserRole.Manager)
                {
                    UpdateServiceToggleAndDashboardLink(evt.previousValue);
                    return;
                }
                CrashService.instance.EnableService(evt.newValue);
            });
        }

        void UpdateServiceToggleAndDashboardLink(bool isEnabled)
        {
            if (m_CrashServiceGoToDashboard != null)
            {
                m_CrashServiceGoToDashboard.style.display = (isEnabled) ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (m_CrashServiceToggle != null)
            {
                m_CrashServiceToggle.SetValueWithoutNotify(isEnabled);
                SetupServiceToggleLabel(m_CrashServiceToggle, isEnabled);
            }
        }

        protected override void ToggleRestrictedVisualElementsAvailability(bool enable)
        {
            var serviceToggleContainer = rootVisualElement.Q(className: k_ServiceToggleContainerClassName);
            var unityToggle = serviceToggleContainer?.Q(className: k_UnityToggleClassName);
            if (unityToggle != null)
            {
                unityToggle.SetEnabled(enable);
                if (!enable)
                {
                    var notifications = NotificationManager.instance.GetNotificationsForTopics(Notification.Topic.CrashService);
                    if (notifications.Any(notification => notification.rawMessage == k_CloudDiagPermissionMessage))
                    {
                        return;
                    }

                    NotificationManager.instance.Publish(
                        Notification.Topic.CrashService,
                        Notification.Severity.Warning,
                        k_CloudDiagPermissionMessage);
                }
            }
        }

        protected override void ActivateAction(string searchContext)
        {
            // Must reset properties every time this is activated
            var mainTemplate = EditorGUIUtility.Load(k_CloudDiagCommonUxmlPath) as VisualTreeAsset;
            var newVisual = mainTemplate.CloneTree().contentContainer;
            ServicesUtils.TranslateStringsInTree(newVisual);
            rootVisualElement.Add(newVisual);

            // Setup the crash reporting UI
            SetupCrashDiag();
            // User Reporting is handled without specific states...
            SetupUserReport();

            // The Crash Reporting is done with the state machine
            // Make sure to reset the state machine
            m_CrashStateMachine.ClearCurrentState();

            // Make sure to activate the state machine to the current state...
            if (CrashService.instance.IsServiceEnabled())
            {
                m_CrashStateMachine.Initialize(m_EnabledState);
            }
            else
            {
                m_CrashStateMachine.Initialize(m_DisabledState);
            }

            // This is for the learn more button
            var learnMore = rootVisualElement.Q(k_LearnMoreLink);
            if (learnMore != null)
            {
                var clickable = new Clickable(() =>
                {
                    Application.OpenURL(ServicesConfiguration.instance.GetUnityCloudDiagnosticInfoUrl());
                });
                learnMore.AddManipulator(clickable);
            }
        }

        protected override void DeactivateAction()
        {
            UnregisterEvent();
        }

        void RegisterEvent()
        {
            if (!m_EventHandlerInitialized)
            {
                CrashService.instance.serviceAfterEnableEvent += ServiceIsEnablingEvent;
                CrashService.instance.serviceAfterDisableEvent += ServiceIsDisablingEvent;
                m_EventHandlerInitialized = true;
            }
        }

        void UnregisterEvent()
        {
            if (m_EventHandlerInitialized)
            {
                CrashService.instance.serviceAfterEnableEvent -= ServiceIsEnablingEvent;
                CrashService.instance.serviceAfterDisableEvent -= ServiceIsDisablingEvent;
                m_EventHandlerInitialized = false;
            }
        }

        void ServiceIsEnablingEvent(object sender, EventArgs args)
        {
            if (settingsWindow.GetCurrentProvider() == this)
            {
                m_CrashServiceToggle.SetValueWithoutNotify(true);
                m_CrashStateMachine.ProcessEvent(CloudDiagCrashEvent.Enabling);
            }
        }

        void ServiceIsDisablingEvent(object sender, EventArgs args)
        {
            if (settingsWindow.GetCurrentProvider() == this)
            {
                m_CrashServiceToggle.SetValueWithoutNotify(false);
                m_CrashStateMachine.ProcessEvent(CloudDiagCrashEvent.Disabling);
            }
        }

        void SetupCrashDiag()
        {
            var crashDiagContainer = rootVisualElement.Q(className: k_CloudDiagCrashContainerClassName);
            if (crashDiagContainer == null)
            {
                return;
            }

            var generalTemplate = EditorGUIUtility.Load(k_CloudDiagCrashCommonUxmlPath) as VisualTreeAsset;
            if (generalTemplate != null)
            {
                var newVisual = generalTemplate.CloneTree().contentContainer;
                ServicesUtils.TranslateStringsInTree(newVisual);
                crashDiagContainer.Clear();
                crashDiagContainer.Add(newVisual);
                crashDiagContainer.Add(ServicesUtils.SetupSupportedPlatformsBlock(ServicesUtils.GetCloudDiagCrashSupportedPlatforms()));

                m_CrashServiceGoToDashboard = rootVisualElement.Q(k_CloudDiagCrashGoToDashboardName);
                if (m_CrashServiceGoToDashboard != null)
                {
                    var clickable = new Clickable(() =>
                    {
                        ServicesConfiguration.instance.RequestBaseCloudDiagCrashesDashboardUrl(OpenDashboardOrgAndProjectIds);
                    });
                    m_CrashServiceGoToDashboard.AddManipulator(clickable);
                }

                m_CrashServiceToggle = rootVisualElement.Q<Toggle>(className: k_ServiceToggleClassName);
                SetupServiceToggle();
                RegisterEvent();
            }
        }

        void SetupUserReport()
        {
            var userReportContainer = rootVisualElement.Q(className: k_CloudDiagUserReportContainerClassName);
            var generalTemplate = EditorGUIUtility.Load(k_CloudDiagUserReportUxmlPath) as VisualTreeAsset;
            if ((generalTemplate != null) && (userReportContainer != null))
            {
                var newVisual = generalTemplate.CloneTree().contentContainer;
                ServicesUtils.TranslateStringsInTree(newVisual);
                userReportContainer.Clear();
                userReportContainer.Add(newVisual);
                userReportContainer.Add(ServicesUtils.SetupSupportedPlatformsBlock(ServicesUtils.GetCloudDiagUserReportSupportedPlatforms()));

                var downloadSdkButtonLink = rootVisualElement.Q(k_UserReportingDownloadSdk);
                if (downloadSdkButtonLink != null)
                {
                    var clickable = new Clickable(() =>
                    {
                        Application.OpenURL(ServicesConfiguration.instance.GetUnityCloudDiagnosticUserReportingSdkUrl());
                    });
                    downloadSdkButtonLink.AddManipulator(clickable);
                }
            }
        }

        // Disabled state of the service
        sealed class DisabledState : SimpleStateMachine<CloudDiagCrashEvent>.State
        {
            CloudDiagProjectSettings m_Provider;

            public DisabledState(SimpleStateMachine<CloudDiagCrashEvent> simpleStateMachine, CloudDiagProjectSettings provider)
                : base(k_StateNameDisabled, simpleStateMachine)
            {
                m_Provider = provider;
                ModifyActionForEvent(CloudDiagCrashEvent.Enabling, HandleBinding);
            }

            public override void EnterState()
            {
                var crashContainer = m_Provider.rootVisualElement.Q(k_CloudDiagCrashStateName);
                crashContainer?.Clear();
                m_Provider.UpdateServiceToggleAndDashboardLink(CrashService.instance.IsServiceEnabled());

                m_Provider.HandlePermissionRestrictedControls();
            }

            SimpleStateMachine<CloudDiagCrashEvent>.State HandleBinding(CloudDiagCrashEvent raisedEvent)
            {
                return stateMachine.GetStateByName(k_StateNameEnabled);
            }
        }

        // Enabled state of the service
        sealed class EnabledState : SimpleStateMachine<CloudDiagCrashEvent>.State
        {
            CloudDiagProjectSettings m_Provider;

            public EnabledState(SimpleStateMachine<CloudDiagCrashEvent> simpleStateMachine, CloudDiagProjectSettings provider)
                : base(k_StateNameEnabled, simpleStateMachine)
            {
                m_Provider = provider;
                ModifyActionForEvent(CloudDiagCrashEvent.Disabling, HandleUnbinding);
            }

            public override void EnterState()
            {
                //If we haven't received new bound info, fetch them
                var generalTemplate = EditorGUIUtility.Load(k_CloudDiagCrashEnabledUxmlPath) as VisualTreeAsset;
                var crashContainer = m_Provider.rootVisualElement.Q(k_CloudDiagCrashStateName);

                if ((generalTemplate != null) && (crashContainer != null))
                {
                    var newVisual = generalTemplate.CloneTree().contentContainer;
                    ServicesUtils.TranslateStringsInTree(newVisual);
                    crashContainer.Clear();
                    crashContainer.Add(newVisual);

                    var capturePlayMode = crashContainer.Q<Toggle>(k_CrashCapturePlayMode);
                    if (capturePlayMode != null)
                    {
                        capturePlayMode.SetValueWithoutNotify(CrashReportingSettings.captureEditorExceptions);
                        capturePlayMode.RegisterValueChangedCallback(evt =>
                        {
                            CrashReportingSettings.captureEditorExceptions = evt.newValue;
                        });
                    }

                    var logBufferSize = crashContainer.Q<IntegerField>(k_CrashBufferLogCount);
                    if (logBufferSize != null)
                    {
                        logBufferSize.SetValueWithoutNotify((int)CrashReportingSettings.logBufferSize);
                        logBufferSize.RegisterValueChangedCallback(evt =>
                        {
                            var newValue = evt.newValue;
                            var updateValue = false;

                            if (evt.newValue < k_CrashBufferLogMinimum)
                            {
                                newValue = k_CrashBufferLogMinimum;
                                updateValue = true;
                            }
                            else if (evt.newValue > k_CrashBufferLogMaximum)
                            {
                                newValue = k_CrashBufferLogMaximum;
                                updateValue = true;
                            }

                            CrashReportingSettings.logBufferSize = (uint)newValue;
                            if (updateValue)
                            {
                                logBufferSize.SetValueWithoutNotify(newValue);
                            }
                        });
                        m_Provider.UpdateServiceToggleAndDashboardLink(CrashService.instance.IsServiceEnabled());
                    }

                    m_Provider.HandlePermissionRestrictedControls();
                }
            }

            SimpleStateMachine<CloudDiagCrashEvent>.State HandleUnbinding(CloudDiagCrashEvent raisedEvent)
            {
                return stateMachine.GetStateByName(k_StateNameDisabled);
            }
        }
    }
}
