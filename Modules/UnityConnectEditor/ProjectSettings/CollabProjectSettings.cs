// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Collaboration;
using UnityEngine.UIElements;

namespace UnityEditor.Connect
{
    internal class CollabProjectSettings : ServicesProjectSettings
    {
        const string k_ServiceName = "Collaborate";

        // Actual states for the collab state machine
        const string k_StateNameDisabled = "DisabledState";
        const string k_StateNameEnabled = "EnabledState";

        const string k_CollaborateCommonUxmlPath = "UXML/ServicesWindow/CollabProjectSettings.uxml";
        const string k_CollaborateDisabledUxmlPath = "UXML/ServicesWindow/CollabProjectSettingsDisabled.uxml";
        const string k_CollaborateEnabledUxmlPath = "UXML/ServicesWindow/CollabProjectSettingsEnabled.uxml";

        // Elements of the UXML
        const string k_ServiceToggleClassName = "service-toggle";
        const string k_ServiceNameProperty = "serviceName";
        const string k_ServiceScrollContainerClassName = "scroll-container";

        const string k_LearnMoreLink = "LearnMore";
        const string k_GoToWebDashboardLink = "GoToWebDashboard";
        const string k_GoToDashboardLinkName = "GoToDashboard";
        const string k_OpenHistoryLink = "OpenHistory";
        const string k_OpenChangesLink = "OpenChanges";

        const string k_CollabPublishSection = "CollabPublishSection";
        const string k_CollabHistorySection = "CollabHistorySection";

        VisualElement m_GoToDashboard;
        Toggle m_MainServiceToggle;
        bool m_EventHandlerInitialized;

        const string k_CollabPermissionMessage = "You do not have sufficient permissions to enable / disable Collaborate service.";
        const string k_CollabPackageName = "Collab Package";

        internal enum CollabEvent
        {
            Enabling,
            Disabling,
        }

        [SettingsProvider]
        public static SettingsProvider CreateServicesProvider()
        {
            return new CollabProjectSettings(CollabService.instance.projectSettingsPath, SettingsScope.Project);
        }

        SimpleStateMachine<CollabEvent> m_StateMachine;
        EnabledState m_EnabledState;
        DisabledState m_DisabledState;

        public CollabProjectSettings(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, k_ServiceName, keywords)
        {
            m_StateMachine = new SimpleStateMachine<CollabEvent>();
            m_StateMachine.AddEvent(CollabEvent.Enabling);
            m_StateMachine.AddEvent(CollabEvent.Disabling);

            m_EnabledState = new EnabledState(m_StateMachine, this);
            m_DisabledState = new DisabledState(m_StateMachine, this);

            m_StateMachine.AddState(m_EnabledState);
            m_StateMachine.AddState(m_DisabledState);
        }

        void OnDestroy()
        {
            UnregisterEvent();
        }

        protected override Notification.Topic[] notificationTopicsToSubscribe => new[]
        {
            Notification.Topic.CollabService,
            Notification.Topic.ProjectBind
        };
        protected override SingleService serviceInstance => CollabService.instance;
        protected override string serviceUssClassName => "collab";
        void SetupServiceToggle(SingleService singleService)
        {
            m_MainServiceToggle.SetProperty(k_ServiceNameProperty, singleService.name);
            m_MainServiceToggle.SetEnabled(false);
            UpdateServiceToggleAndDashboardLink(singleService.IsServiceEnabled());

            if (singleService.displayToggle)
            {
                m_MainServiceToggle.RegisterValueChangedCallback(evt =>
                {
                    if (currentUserPermission != UserRole.Owner && currentUserPermission != UserRole.Manager)
                    {
                        UpdateServiceToggleAndDashboardLink(evt.previousValue);
                        return;
                    }
                    singleService.EnableService(evt.newValue);
                });
            }
            else
            {
                m_MainServiceToggle.style.display = DisplayStyle.None;
            }
        }

        void UpdateServiceToggleAndDashboardLink(bool isEnabled)
        {
            if (m_GoToDashboard != null)
            {
                m_GoToDashboard.style.display = (isEnabled) ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (m_MainServiceToggle != null)
            {
                m_MainServiceToggle.SetValueWithoutNotify(isEnabled);
                SetupServiceToggleLabel(m_MainServiceToggle, isEnabled);
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
                    var notifications = NotificationManager.instance.GetNotificationsForTopics(Notification.Topic.CollabService);
                    if (notifications.Any(notification => notification.rawMessage == k_CollabPermissionMessage))
                    {
                        return;
                    }

                    NotificationManager.instance.Publish(
                        Notification.Topic.CollabService,
                        Notification.Severity.Warning,
                        k_CollabPermissionMessage);
                }
            }
        }

        void OnCollabStateChanged(CollabInfo info)
        {
            // Make sure to update the service state based on the information changed in Collab...
            if (CollabService.instance.IsServiceEnabled() != Collab.instance.IsCollabEnabledForCurrentProject())
            {
                CollabService.instance.EnableService(Collab.instance.IsCollabEnabledForCurrentProject());
            }
        }

        protected override void ActivateAction(string searchContext)
        {
            // Must reset properties every time this is activated
            var mainTemplate = EditorGUIUtility.Load(k_CollaborateCommonUxmlPath) as VisualTreeAsset;
            rootVisualElement.Add(mainTemplate.CloneTree().contentContainer);

            // Make sure to reset the state machine
            m_StateMachine.ClearCurrentState();

            // Make sure to activate the state machine to the current state...
            if (CollabService.instance.IsServiceEnabled())
            {
                m_StateMachine.Initialize(m_EnabledState);
            }
            else
            {
                m_StateMachine.Initialize(m_DisabledState);
            }

            // Register the events for enabling / disabling the service only once.
            RegisterEvent();
            // Moved the Go to dashboard link to the header title section.
            m_GoToDashboard = rootVisualElement.Q(k_GoToDashboardLinkName);
            if (m_GoToDashboard != null)
            {
                var clickable = new Clickable(() =>
                {
                    ServicesConfiguration.instance.RequestBaseCollabDashboardUrl(OpenDashboardOrgAndProjectIds);
                });
                m_GoToDashboard.AddManipulator(clickable);
            }

            m_MainServiceToggle = rootVisualElement.Q<Toggle>(null, k_ServiceToggleClassName);
            SetupServiceToggle(CollabService.instance);

            var learnMore = rootVisualElement.Q(k_LearnMoreLink);
            if (learnMore != null)
            {
                var clickable = new Clickable(() =>
                {
                    Application.OpenURL(ServicesConfiguration.instance.GetUnityTeamInfoUrl());
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
                // Make sure to follow-up the Collab State...
                // Collab is a specific case where the service can be enabled inside another Editor Window by the Collab package code itself.
                //     We need to be informed of that changed when it happens
                Collab.instance.StateChanged += OnCollabStateChanged;

                CollabService.instance.serviceAfterEnableEvent += ServiceIsEnablingEvent;
                CollabService.instance.serviceAfterDisableEvent += ServiceIsDisablingEvent;
                m_EventHandlerInitialized = true;
            }
        }

        void UnregisterEvent()
        {
            if (m_EventHandlerInitialized)
            {
                Collab.instance.StateChanged -= OnCollabStateChanged;
                CollabService.instance.serviceAfterEnableEvent -= ServiceIsEnablingEvent;
                CollabService.instance.serviceAfterDisableEvent -= ServiceIsDisablingEvent;
                m_EventHandlerInitialized = false;
            }
        }

        void ServiceIsEnablingEvent(object sender, EventArgs args)
        {
            if (settingsWindow.GetCurrentProvider() == this)
            {
                m_MainServiceToggle.SetValueWithoutNotify(true);
                m_StateMachine.ProcessEvent(CollabEvent.Enabling);
            }
        }

        void ServiceIsDisablingEvent(object sender, EventArgs args)
        {
            if (settingsWindow.GetCurrentProvider() == this)
            {
                m_MainServiceToggle.SetValueWithoutNotify(false);
                m_StateMachine.ProcessEvent(CollabEvent.Disabling);
            }
        }

        // Disabled state of the service
        sealed class DisabledState : GenericBaseState<CollabProjectSettings, CollabEvent>
        {
            public DisabledState(SimpleStateMachine<CollabEvent> simpleStateMachine, CollabProjectSettings provider)
                : base(k_StateNameDisabled, simpleStateMachine, provider)
            {
                ModifyActionForEvent(CollabEvent.Enabling, HandleBinding);
            }

            public override void EnterState()
            {
                var generalTemplate = EditorGUIUtility.Load(k_CollaborateDisabledUxmlPath) as VisualTreeAsset;
                var scrollContainer = provider.rootVisualElement.Q(null, k_ServiceScrollContainerClassName);
                scrollContainer.Clear();
                if (generalTemplate != null)
                {
                    var newVisual = generalTemplate.CloneTree().contentContainer;
                    ServicesUtils.TranslateStringsInTree(newVisual);
                    scrollContainer.Add(newVisual);

                    provider.UpdateServiceToggleAndDashboardLink(provider.serviceInstance.IsServiceEnabled());
                }

                provider.HandlePermissionRestrictedControls();
            }

            SimpleStateMachine<CollabEvent>.State HandleBinding(CollabEvent raisedEvent)
            {
                return stateMachine.GetStateByName(k_StateNameEnabled);
            }
        }

        // Enabled state of the service
        sealed class EnabledState : GenericBaseState<CollabProjectSettings, CollabEvent>
        {
            VisualElement m_CollabPublishSection;
            VisualElement m_CollabHistorySection;

            public EnabledState(SimpleStateMachine<CollabEvent> simpleStateMachine, CollabProjectSettings provider)
                : base(k_StateNameEnabled, simpleStateMachine, provider)
            {
                ModifyActionForEvent(CollabEvent.Disabling, HandleUnbinding);

                // Related protected variables
                topicForNotifications = Notification.Topic.CollabService;
                notLatestPackageInstalledInfo = string.Format(k_NotLatestPackageInstalledInfo, k_CollabPackageName);
                packageInstallationHeadsup = string.Format(k_PackageInstallationHeadsup, k_CollabPackageName);
                duplicateInstallWarning = null;
                packageInstallationDialogTitle = string.Format(k_PackageInstallationDialogTitle, k_CollabPackageName);
            }

            protected override void PackageInformationUpdated()
            {
                if (packmanPackageInstalled)
                {
                    // Show the Publish and History section
                    if (m_CollabHistorySection != null)
                    {
                        m_CollabHistorySection.style.display = DisplayStyle.Flex;
                    }
                    if (m_CollabPublishSection != null)
                    {
                        m_CollabPublishSection.style.display = DisplayStyle.Flex;
                    }
                }
                else
                {
                    // Don't show the Publish and History section
                    if (m_CollabHistorySection != null)
                    {
                        m_CollabHistorySection.style.display = DisplayStyle.None;
                    }
                    if (m_CollabPublishSection != null)
                    {
                        m_CollabPublishSection.style.display = DisplayStyle.None;
                    }
                }
            }

            public override void EnterState()
            {
                //If we haven't received new bound info, fetch them
                var generalTemplate = EditorGUIUtility.Load(k_CollaborateEnabledUxmlPath) as VisualTreeAsset;
                var scrollContainer = provider.rootVisualElement.Q(null, k_ServiceScrollContainerClassName);
                var newVisual = generalTemplate.CloneTree().contentContainer;
                ServicesUtils.TranslateStringsInTree(newVisual);
                scrollContainer.Clear();
                scrollContainer.Add(newVisual);

                m_CollabPublishSection = scrollContainer.Q(k_CollabPublishSection);
                m_CollabHistorySection = scrollContainer.Q(k_CollabHistorySection);
                // Don't show the Publish and History section by default
                if (m_CollabHistorySection != null)
                {
                    m_CollabHistorySection.style.display = DisplayStyle.None;
                }
                if (m_CollabPublishSection != null)
                {
                    m_CollabPublishSection.style.display = DisplayStyle.None;
                }

                var openHistory = provider.rootVisualElement.Q(k_OpenHistoryLink) as Button;
                if (openHistory != null)
                {
                    openHistory.clicked += () =>
                    {
                        if (Collab.ShowHistoryWindow != null)
                        {
                            Collab.ShowHistoryWindow();
                        }
                    };
                }

                Button openChangesLinkBtn = provider.rootVisualElement.Q(k_OpenChangesLink) as Button;
                if (openChangesLinkBtn != null)
                {
                    openChangesLinkBtn.clicked += () =>
                    {
                        if (Collab.ShowChangesWindow != null)
                        {
                            Collab.ShowChangesWindow();
                        }
                    };
                }

                var gotoWebDashboard = scrollContainer.Q(k_GoToWebDashboardLink);
                if (gotoWebDashboard != null)
                {
                    var clickable = new Clickable(() =>
                    {
                        ServicesConfiguration.instance.RequestBaseCloudUsageDashboardUrl(provider.OpenDashboardOrgAndProjectIds);
                    });
                    gotoWebDashboard.AddManipulator(clickable);
                }
                provider.UpdateServiceToggleAndDashboardLink(provider.serviceInstance.IsServiceEnabled());

                // Prepare the package section and update the package information
                PreparePackageSection(scrollContainer);
                UpdatePackageInformation();

                provider.HandlePermissionRestrictedControls();
            }

            SimpleStateMachine<CollabEvent>.State HandleUnbinding(CollabEvent raisedEvent)
            {
                return stateMachine.GetStateByName(k_StateNameDisabled);
            }
        }
    }
}
