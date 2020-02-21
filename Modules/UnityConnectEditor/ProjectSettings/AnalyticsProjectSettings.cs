// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using Button = UnityEngine.UIElements.Button;

namespace UnityEditor.Connect
{
    class AnalyticsProjectSettings : ServicesProjectSettings
    {
        //Resources
        const string k_ServiceName = "Analytics";
        const string k_ProjectSettingsPath = "Project/Services/Analytics";

        const string k_AnalyticsServicesTemplatePath = "UXML/ServicesWindow/AnalyticsProjectSettings.uxml";

        //State Names
        const string k_StateNameDisabled = "DisabledState";
        const string k_StateNameIntegration = "IntegrationState";
        const string k_StateNameEnabled = "EnabledState";

        //Keywords
        const string k_KeywordAnalytics = "analytics";
        const string k_KeywordInsights = "insights";
        const string k_KeywordEvents = "events";
        const string k_KeywordMonetization = "monetization";
        const string k_KeywordDashboard = "dashboard";
        const string k_KeywordValidator = "validator";

        //WebResponse JSON utils
        const string k_JsonKeyBasic = "basic";
        const string k_JsonKeyCustom = "custom";
        const string k_JsonKeyMonetization = "transactions";
        const string k_JsonKeyAuthSignature = "auth_signature";
        const string k_JsonValueCompleted = "Completed";

        //Notification Strings
        const string k_AuthSignatureExceptionMessage = "Exception occurred trying to obtain authentication signature for project {0} and was not handled. Message: {1}";
        const string k_DataValidationExceptionMessage = "Exception occurred trying to validate analytics data for project {0} and was not handled. Message: {1}";

        const string k_AnalyticsPackageName = "Analytics Package";
        const string k_GoToDashboardLink = "GoToDashboard";

        const string k_ServiceToggleClassName = "service-toggle";
        const string k_ServiceNameProperty = "serviceName";

        const string k_AnalyticsPermissionMessage = "You do not have sufficient permissions to enable / disable Analytics service.";

        bool m_CallbacksInitialized;

        Toggle m_MainServiceToggle;
        VisualElement m_GoToDashboard;

        UnityWebRequest m_AuthSignatureRequest;
        UnityWebRequest m_DataClearRequest;
        bool m_BasicDataValidated;
        bool m_CustomDataIntegrated;
        bool m_MonetizationDataIntegrated;

        Action m_NotifyOnBasicValidate;

        AnalyticsValidationPoller m_ValidationPoller;

        [SettingsProvider]
        public static SettingsProvider CreateServicesProvider()
        {
            return new AnalyticsProjectSettings(k_ProjectSettingsPath, SettingsScope.Project, new List<string>()
            {
                L10n.Tr(k_KeywordAnalytics),
                L10n.Tr(k_KeywordInsights),
                L10n.Tr(k_KeywordEvents),
                L10n.Tr(k_KeywordMonetization),
                L10n.Tr(k_KeywordDashboard),
                L10n.Tr(k_KeywordValidator),
            });
        }

        protected override SingleService serviceInstance
        {
            get { return AnalyticsService.instance; }
        }
        protected override string serviceUssClassName => "analytics";

        SimpleStateMachine<ServiceEvent> m_StateMachine;

        DisabledState m_DisabledState;
        IntegrationState m_IntegrationState;
        EnabledState m_EnabledState;

        public AnalyticsProjectSettings(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, k_ServiceName, keywords)
        {
            m_StateMachine = new SimpleStateMachine<ServiceEvent>();

            m_StateMachine.AddEvent(ServiceEvent.Disabled);
            m_StateMachine.AddEvent(ServiceEvent.Integrating);
            m_StateMachine.AddEvent(ServiceEvent.Enabled);

            m_DisabledState = new DisabledState(m_StateMachine, this);
            m_IntegrationState = new IntegrationState(m_StateMachine, this);
            m_EnabledState = new EnabledState(m_StateMachine, this);

            m_StateMachine.AddState(m_DisabledState);
            m_StateMachine.AddState(m_IntegrationState);
            m_StateMachine.AddState(m_EnabledState);

            m_ValidationPoller = new AnalyticsValidationPoller();
        }

        void OnDestroy()
        {
            FinalizeServiceCallbacks();
        }

        protected override Notification.Topic[] notificationTopicsToSubscribe => new[]
        {
            Notification.Topic.AnalyticsService,
            Notification.Topic.ProjectBind
        };

        protected override void ToggleRestrictedVisualElementsAvailability(bool enable)
        {
            var serviceToggleContainer = rootVisualElement.Q(className: k_ServiceToggleContainerClassName);
            var unityToggle = serviceToggleContainer?.Q(className: k_UnityToggleClassName);
            if (unityToggle != null)
            {
                unityToggle.SetEnabled(enable);
                if (!enable)
                {
                    var notifications = NotificationManager.instance.GetNotificationsForTopics(Notification.Topic.AnalyticsService);
                    if (notifications.Any(notification => notification.rawMessage == k_AnalyticsPermissionMessage))
                    {
                        return;
                    }

                    NotificationManager.instance.Publish(
                        Notification.Topic.AnalyticsService,
                        Notification.Severity.Warning,
                        k_AnalyticsPermissionMessage);
                }
            }
        }

        protected override void ActivateAction(string searchContext)
        {
            //Must reset properties every time this is activated
            var mainTemplate = EditorGUIUtility.Load(k_AnalyticsServicesTemplatePath) as VisualTreeAsset;
            rootVisualElement.Add(mainTemplate.CloneTree().contentContainer);

            RequestDataValidation();

            if (!serviceInstance.IsServiceEnabled())
            {
                m_StateMachine.Initialize(m_DisabledState);
            }
            else if (m_BasicDataValidated)
            {
                m_StateMachine.Initialize(m_EnabledState);
            }
            else
            {
                m_StateMachine.Initialize(m_IntegrationState);
            }

            // Moved the Go to dashboard link to the header title section.
            m_GoToDashboard = rootVisualElement.Q(k_GoToDashboardLink);
            if (m_GoToDashboard != null)
            {
                var clickable = new Clickable(() =>
                {
                    ServicesConfiguration.instance.RequestBaseAnalyticsDashboardUrl(OpenDashboardForProjectGuid);
                });
                m_GoToDashboard.AddManipulator(clickable);
            }

            m_MainServiceToggle = rootVisualElement.Q<Toggle>(className: k_ServiceToggleClassName);
            SetupServiceToggle(AnalyticsService.instance);
            InitializeServiceCallbacks();
        }

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

        protected override void DeactivateAction()
        {
            m_StateMachine.ClearCurrentState();

            FinalizeServiceCallbacks();

            if (m_AuthSignatureRequest != null)
            {
                m_AuthSignatureRequest.Abort();
                m_AuthSignatureRequest.Dispose();
                m_AuthSignatureRequest = null;
            }
            m_NotifyOnBasicValidate = null;

            if (m_DataClearRequest != null)
            {
                m_DataClearRequest.Abort();
                m_DataClearRequest.Dispose();
                m_DataClearRequest = null;
            }

            m_ValidationPoller.Shutdown();

            m_BasicDataValidated = false;
            m_CustomDataIntegrated = false;
            m_MonetizationDataIntegrated = false;
        }

        void InitializeServiceCallbacks()
        {
            if (!m_CallbacksInitialized)
            {
                //Bind states to external changes
                serviceInstance.serviceAfterEnableEvent += EnableOperationCompleted;
                serviceInstance.serviceAfterDisableEvent += DisableOperationCompleted;
                m_CallbacksInitialized = true;
            }
        }

        void FinalizeServiceCallbacks()
        {
            if (m_CallbacksInitialized)
            {
                //Bind states to external changes
                serviceInstance.serviceAfterEnableEvent -= EnableOperationCompleted;
                serviceInstance.serviceAfterDisableEvent -= DisableOperationCompleted;
                m_CallbacksInitialized = false;
            }
        }

        void EnableOperationCompleted(object sender, EventArgs args)
        {
            m_StateMachine.ProcessEvent(m_BasicDataValidated ? ServiceEvent.Enabled : ServiceEvent.Integrating);
        }

        void DisableOperationCompleted(object sender, EventArgs args)
        {
            m_StateMachine.ProcessEvent(ServiceEvent.Disabled);
        }

        void RequestDataValidation()
        {
            if (!m_ValidationPoller.IsReady())
            {
                RequestAuthSignature();
            }
            else
            {
                m_ValidationPoller.Poll();
            }
        }

        void OnGetValidationData(string downloadedData)
        {
            string basicStatus = null;
            string customStatus = null;
            string monetizationStatus = null;

            var jsonParser = new JSONParser(downloadedData);
            try
            {
                var json = jsonParser.Parse();
                basicStatus = json.AsDict()[k_JsonKeyBasic].AsString();
                customStatus = json.AsDict()[k_JsonKeyCustom].AsString();
                monetizationStatus = json.AsDict()[k_JsonKeyMonetization].AsString();
            }
            catch (Exception ex)
            {
                NotificationManager.instance.Publish(serviceInstance.notificationTopic, Notification.Severity.Error,
                    string.Format(L10n.Tr(k_DataValidationExceptionMessage), Connect.UnityConnect.instance.projectInfo.projectName, ex.Message));

                Debug.LogException(ex);
            }

            m_BasicDataValidated = (basicStatus == k_JsonValueCompleted);
            m_CustomDataIntegrated = (customStatus == k_JsonValueCompleted);
            m_MonetizationDataIntegrated = (monetizationStatus == k_JsonValueCompleted);

            if (m_BasicDataValidated && m_NotifyOnBasicValidate != null)
            {
                m_NotifyOnBasicValidate();
                m_NotifyOnBasicValidate = null;
            }
        }

        void RequestAuthSignature()
        {
            if (m_AuthSignatureRequest == null)
            {
                AnalyticsService.instance.RequestAuthSignature(OnGetAuthSignature, out m_AuthSignatureRequest);
            }
        }

        void OnGetAuthSignature(AsyncOperation op)
        {
            if (op.isDone && m_AuthSignatureRequest != null && m_AuthSignatureRequest.downloadHandler.isDone)
            {
                if ((m_AuthSignatureRequest.result != UnityWebRequest.Result.ProtocolError) && (m_AuthSignatureRequest.result != UnityWebRequest.Result.ConnectionError))
                {
                    var jsonParser = new JSONParser(m_AuthSignatureRequest.downloadHandler.text);
                    try
                    {
                        var json = jsonParser.Parse();
                        m_ValidationPoller.Setup(json.AsDict()[k_JsonKeyAuthSignature].AsString(), OnGetValidationData);
                    }
                    catch (Exception ex)
                    {
                        NotificationManager.instance.Publish(serviceInstance.notificationTopic, Notification.Severity.Error,
                            string.Format(L10n.Tr(k_AuthSignatureExceptionMessage), Connect.UnityConnect.instance.projectInfo.projectName, ex.Message));
                    }

                    if (m_ValidationPoller.IsReady())
                    {
                        m_ValidationPoller.Poll();
                    }
                }

                m_AuthSignatureRequest.Dispose();
                m_AuthSignatureRequest = null;
            }
        }

        public void RequestNotifyOnDataValidate(Action onCheck)
        {
            if (!m_BasicDataValidated)
            {
                m_NotifyOnBasicValidate += onCheck;
                RequestDataValidation();
            }
            else
            {
                onCheck();
            }
        }

        internal enum ServiceEvent
        {
            Disabled,    //Service toggle is OFF
            Integrating, //Service toggle is ON, but the dev team has not setup/interacted with the analytics backend
            Enabled,     //Service toggle is ON, and the analytics backend has some basic events to display
        }

        class BaseAnalyticsState : GenericBaseState<AnalyticsProjectSettings, ServiceEvent>
        {
            //Common uss class names
            protected const string k_ScrollContainerClass = "scroll-container";

            protected BaseAnalyticsState(string stateName, SimpleStateMachine<ServiceEvent> stateMachine, AnalyticsProjectSettings provider)
                : base(stateName, stateMachine, provider)
            {
            }

            protected void LoadTemplateIntoScrollContainer(string templatePath)
            {
                var generalTemplate = EditorGUIUtility.Load(templatePath) as VisualTreeAsset;
                var rootElement = provider.rootVisualElement;
                if (rootElement != null)
                {
                    var scrollContainer = provider.rootVisualElement.Q(className: k_ScrollContainerClass);
                    scrollContainer.Clear();
                    scrollContainer.Add(generalTemplate.CloneTree().contentContainer);
                    ServicesUtils.TranslateStringsInTree(provider.rootVisualElement);

                    provider.UpdateServiceToggleAndDashboardLink(provider.serviceInstance.IsServiceEnabled());
                }
            }

            protected void SetupSupportedPlatformsBlock()
            {
                var scrollContainer = provider.rootVisualElement.Q(className: k_ScrollContainerClass);
                scrollContainer.Add(ServicesUtils.SetupSupportedPlatformsBlock(ServicesUtils.GetAnalyticsSupportedPlatforms()));
            }
        }

        class DisabledState : BaseAnalyticsState
        {
            const string k_TemplatePath = "UXML/ServicesWindow/AnalyticsProjectSettingsStateDisabled.uxml";

            //uxml element names
            const string k_EnableAnalyticsBlock = "EnableAnalyticsBlock";
            const string k_LearnMoreLink = "LearnMoreLink";

            public DisabledState(SimpleStateMachine<ServiceEvent> stateMachine, AnalyticsProjectSettings provider)
                : base(k_StateNameDisabled, stateMachine, provider)
            {
                ModifyActionForEvent(ServiceEvent.Integrating, HandleIntegrating);
                ModifyActionForEvent(ServiceEvent.Enabled, HandleEnabling);
            }

            public override void EnterState()
            {
                LoadTemplateIntoScrollContainer(k_TemplatePath);
                SetupEnableBlock();
                SetupSupportedPlatformsBlock();

                provider.m_ValidationPoller.Stop();

                provider.HandlePermissionRestrictedControls();
            }

            void SetupEnableBlock()
            {
                VisualElement enableBlock = provider.rootVisualElement.Q(k_EnableAnalyticsBlock);

                enableBlock.Q<Button>(k_LearnMoreLink).clicked += () =>
                {
                    Application.OpenURL(AnalyticsConfiguration.instance.learnMoreUrl);
                };
            }

            SimpleStateMachine<ServiceEvent>.State HandleIntegrating(ServiceEvent raisedEvent)
            {
                return stateMachine.GetStateByName(k_StateNameIntegration);
            }

            SimpleStateMachine<ServiceEvent>.State HandleEnabling(ServiceEvent raisedEvent)
            {
                return stateMachine.GetStateByName(k_StateNameEnabled);
            }
        }

        class IntegrationState : BaseAnalyticsState
        {
            const string k_TemplatePath = "UXML/ServicesWindow/AnalyticsProjectSettingsStateIntegrate.uxml";

            VisualElement m_TroubleshootingBlock;

            //uxml element names
            const string k_TroubleshootingBlock = "HavingTroubleBlock";
            const string k_TroubleToggle = "TroubleDropdown";
            const string k_TroubleshootingMode = "trouble-mode";
            const string k_AccessAnalyticsDashboardLink = "AccessAnalyticsDashboard";
            const string k_SupportLink = "SupportLink";

            bool m_HavingTrouble;

            public IntegrationState(SimpleStateMachine<ServiceEvent> stateMachine, AnalyticsProjectSettings provider)
                : base(k_StateNameIntegration, stateMachine, provider)
            {
                ModifyActionForEvent(ServiceEvent.Disabled, HandleDisabling);
                ModifyActionForEvent(ServiceEvent.Enabled, HandleEnabling);
            }

            public override void EnterState()
            {
                base.EnterState();

                LoadTemplateIntoScrollContainer(k_TemplatePath);
                SetupTroubleShootingBlock();
                SetupSupportedPlatformsBlock();

                provider.RequestNotifyOnDataValidate(OnIntegrationComplete);
                provider.m_ValidationPoller.Start();

                provider.HandlePermissionRestrictedControls();
            }

            void SetupTroubleShootingBlock()
            {
                m_TroubleshootingBlock = provider.rootVisualElement.Q(k_TroubleshootingBlock);

                if (m_TroubleshootingBlock != null)
                {
                    m_TroubleshootingBlock.Q<Button>(k_TroubleToggle).clicked += ToggleTroubleshooting;

                    var accessDashboard = m_TroubleshootingBlock.Q(k_AccessAnalyticsDashboardLink);
                    if (accessDashboard != null)
                    {
                        var clickable = new Clickable(() =>
                        {
                            ServicesConfiguration.instance.RequestBaseAnalyticsDashboardUrl(provider.OpenDashboardForProjectGuid);
                        });
                        accessDashboard.AddManipulator(clickable);
                    }
                    var supportLink = m_TroubleshootingBlock.Q(k_SupportLink);
                    if (supportLink != null)
                    {
                        var clickable = new Clickable(() =>
                        {
                            Application.OpenURL(AnalyticsConfiguration.instance.supportUrl);
                        });
                        supportLink.AddManipulator(clickable);
                    }
                    CheckTroubleshootingModeVisibility(m_TroubleshootingBlock);
                }
            }

            void ToggleTroubleshooting()
            {
                m_HavingTrouble = !m_HavingTrouble;
                CheckTroubleshootingModeVisibility(m_TroubleshootingBlock);
            }

            void CheckTroubleshootingModeVisibility(VisualElement fieldBlock)
            {
                if (fieldBlock != null)
                {
                    var troubleshootingMode = fieldBlock.Q(k_TroubleshootingMode);
                    if (troubleshootingMode != null)
                    {
                        troubleshootingMode.style.display = m_HavingTrouble ? DisplayStyle.Flex : DisplayStyle.None;
                    }
                }
            }

            void OnIntegrationComplete()
            {
                if (stateMachine.currentState == this)
                {
                    stateMachine.ProcessEvent(ServiceEvent.Enabled);
                }
            }

            SimpleStateMachine<ServiceEvent>.State HandleDisabling(ServiceEvent raisedEvent)
            {
                return stateMachine.GetStateByName(k_StateNameDisabled);
            }

            SimpleStateMachine<ServiceEvent>.State HandleEnabling(ServiceEvent raisedEvent)
            {
                return stateMachine.GetStateByName(k_StateNameEnabled);
            }
        }

        class EnabledState : BaseAnalyticsState
        {
            const string k_MainTemplatePath = "UXML/ServicesWindow/AnalyticsProjectSettingsStateEnabled.uxml";
            const string k_AdditionalEventsTemplatePath = "UXML/ServicesWindow/AnalyticsProjectSettingsAdditionalEventsTemplate.uxml";

            private class AdditionalEvent
            {
                internal string title { get; set; }
                internal string description { get; set; }
                internal string learnUrl { get; set; }
                internal bool integrated { get; set; }
            };

            Dictionary<string, AdditionalEvent> m_AdditionalEvents;

            //uxml names
            const string k_IapWarningBlock = "IapWarningBlock";
            const string k_WelcomeBlock = "WelcomeBlock";
            const string k_DashboardButton = "DashboardBtn";
            const string k_AdditionalEventsBlock = "AdditionalEvents";
            const string k_AdditionalEventTitle = "TemplateTitle";
            const string k_AdditionalEventDesc = "TemplateDesc";
            const string k_AdditionalEventLearnUrl = "TemplateLearnMoreLink";

            //uxml classes
            const string k_CheckIconClass = "check-icon";
            const string k_PreCheckIconClass = "pre-check-icon";

            //Additional Event Dictionary Content
            const string k_CustomKey = "Custom";
            const string k_CustomTitle = "Custom Events";
            const string k_CustomDesc = "Understand player behavior and usage patterns with event data.";

            const string k_MonetizationTitleAndKey = "Monetization";
            const string k_MonetizationDesc = "Track in-game revenue and monitor fraudulent transactions.";

            public EnabledState(SimpleStateMachine<ServiceEvent> stateMachine, AnalyticsProjectSettings provider)
                : base(k_StateNameEnabled, stateMachine, provider)
            {
                topicForNotifications = Notification.Topic.AnalyticsService;
                notLatestPackageInstalledInfo = string.Format(k_NotLatestPackageInstalledInfo, k_AnalyticsPackageName);
                packageInstallationHeadsup = string.Format(k_PackageInstallationHeadsup, k_AnalyticsPackageName);
                duplicateInstallWarning = null;
                packageInstallationDialogTitle = string.Format(k_PackageInstallationDialogTitle, k_AnalyticsPackageName);

                ModifyActionForEvent(ServiceEvent.Disabled, HandleDisabling);
                ModifyActionForEvent(ServiceEvent.Integrating, HandleIntegrating);

                m_AdditionalEvents = new Dictionary<string, AdditionalEvent>()
                {
                    [k_CustomKey] = new AdditionalEvent() { title = k_CustomTitle, description = k_CustomDesc, learnUrl = AnalyticsConfiguration.instance.customLearnUrl },
                    [k_MonetizationTitleAndKey] = new AdditionalEvent() { title = k_MonetizationTitleAndKey, description = k_MonetizationDesc, learnUrl = AnalyticsConfiguration.instance.monetizationLearnUrl } ,
                };
            }

            public override void EnterState()
            {
                m_AdditionalEvents[k_CustomKey].integrated = provider.m_CustomDataIntegrated;
                m_AdditionalEvents[k_MonetizationTitleAndKey].integrated = provider.m_MonetizationDataIntegrated;

                LoadTemplateIntoScrollContainer(k_MainTemplatePath);
                SetupIapWarningBlock();
                SetupWelcomeBlock();
                SetupAdditionalEventsBlock();
                SetupSupportedPlatformsBlock();

                provider.m_ValidationPoller.Start();

                provider.HandlePermissionRestrictedControls();

                // Prepare the package section and update the package information
                PreparePackageSection(provider.rootVisualElement);
                UpdatePackageInformation();
            }

            void SetupIapWarningBlock()
            {
                var iapWarningBlock = provider.rootVisualElement.Q(k_IapWarningBlock);
                if (iapWarningBlock != null)
                {
                    iapWarningBlock.style.display = PurchasingService.instance.IsServiceEnabled() ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }

            void SetupWelcomeBlock()
            {
                var welcomeBlock = provider.rootVisualElement.Q(k_WelcomeBlock);
                if (welcomeBlock != null)
                {
                    var goToDashboard = new Clickable(() =>
                    {
                        ServicesConfiguration.instance.RequestBaseAnalyticsDashboardUrl(provider.OpenDashboardForProjectGuid);
                    });

                    welcomeBlock.Q(k_DashboardButton).AddManipulator(goToDashboard);
                }
            }

            void SetupAdditionalEventsBlock()
            {
                var additionalEventsBlock = provider.rootVisualElement.Q(k_AdditionalEventsBlock);
                if (additionalEventsBlock != null)
                {
                    additionalEventsBlock.Clear();

                    var eventTemplate = EditorGUIUtility.Load(k_AdditionalEventsTemplatePath) as VisualTreeAsset;
                    if (eventTemplate != null)
                    {
                        foreach (var additionalEvent in m_AdditionalEvents)
                        {
                            var eventBlock = eventTemplate.CloneTree().contentContainer;
                            ServicesUtils.TranslateStringsInTree(eventBlock);
                            if (eventBlock != null)
                            {
                                //set icon visibility
                                var checkIcon = eventBlock.Q(className: k_CheckIconClass);
                                if (checkIcon != null)
                                {
                                    checkIcon.style.display = additionalEvent.Value.integrated ? DisplayStyle.Flex : DisplayStyle.None;
                                }
                                var preCheckIcon = eventBlock.Q(className: k_PreCheckIconClass);
                                if (preCheckIcon != null)
                                {
                                    preCheckIcon.style.display = !additionalEvent.Value.integrated ? DisplayStyle.Flex : DisplayStyle.None;
                                }

                                //set text
                                eventBlock.Q<Label>(k_AdditionalEventTitle).text = additionalEvent.Value.title;
                                eventBlock.Q<Label>(k_AdditionalEventDesc).text = additionalEvent.Value.description;

                                // Setup link url
                                var learnMore = new Clickable(() =>
                                {
                                    Application.OpenURL(additionalEvent.Value.learnUrl);
                                });
                                eventBlock.Q(k_AdditionalEventLearnUrl).AddManipulator(learnMore);

                                additionalEventsBlock.Add(eventBlock);
                            }
                        }
                    }
                }
            }

            SimpleStateMachine<ServiceEvent>.State HandleDisabling(ServiceEvent raisedEvent)
            {
                return stateMachine.GetStateByName(k_StateNameDisabled);
            }

            SimpleStateMachine<ServiceEvent>.State HandleIntegrating(ServiceEvent raisedEvent)
            {
                return stateMachine.GetStateByName(k_StateNameIntegration);
            }
        }
    }
}
