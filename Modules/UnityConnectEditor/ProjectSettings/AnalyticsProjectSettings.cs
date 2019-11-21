// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Connect;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using Button = UnityEngine.UIElements.Button;

namespace UnityEditor.Mono.UnityConnect.Services
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
        const string k_JsonKeyBasicEvents = "events";
        const string k_JsonKeyAuthSignature = "auth_signature";
        const string k_JsonValueCompleted = "Completed";

        //Event Types
        const string k_JsonKeyType = "event_type";
        const string k_JsonValueCustomType = "custom";
        const string k_JsonValueDeviceInfoType = "deviceInfo";
        const string k_JsonValueTransactionType = "transaction";

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
        DateTime m_LastEventTime;
        List<AnalyticsValidatorEvent> m_Events;

        Action m_NotifyOnBasicValidate;
        Action m_NotifyOnEventsUpdate;

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

        private struct ClearDataInfo
        {
            public int clearedEvents;
        }

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

            m_Events = new List<AnalyticsValidatorEvent>();

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
            rootVisualElement.AddStyleSheetPath(ServicesUtils.StylesheetPath.servicesWindowCommon);
            rootVisualElement.AddStyleSheetPath(EditorGUIUtility.isProSkin ? ServicesUtils.StylesheetPath.servicesWindowDark : ServicesUtils.StylesheetPath.servicesWindowLight);
            rootVisualElement.AddStyleSheetPath(ServicesUtils.StylesheetPath.servicesCommon);
            rootVisualElement.AddStyleSheetPath(EditorGUIUtility.isProSkin ? ServicesUtils.StylesheetPath.servicesDark : ServicesUtils.StylesheetPath.servicesLight);

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
                    Application.OpenURL(ServicesConfiguration.instance.analyticsDashboardUrl);
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
            m_MainServiceToggle.SetValueWithoutNotify(singleService.IsServiceEnabled());
            SetupServiceToggleLabel(m_MainServiceToggle, singleService.IsServiceEnabled());
            m_MainServiceToggle.SetEnabled(false);
            if (m_GoToDashboard != null)
            {
                m_GoToDashboard.style.display = (singleService.IsServiceEnabled()) ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (singleService.displayToggle)
            {
                m_MainServiceToggle.RegisterValueChangedCallback(evt =>
                {
                    if (currentUserPermission != UserRole.Owner && currentUserPermission != UserRole.Manager)
                    {
                        m_MainServiceToggle.SetValueWithoutNotify(evt.previousValue);
                        SetupServiceToggleLabel(m_MainServiceToggle, evt.previousValue);
                        return;
                    }
                    SetupServiceToggleLabel(m_MainServiceToggle, evt.newValue);
                    singleService.EnableService(evt.newValue);
                    if (m_GoToDashboard != null)
                    {
                        m_GoToDashboard.style.display = (evt.newValue) ? DisplayStyle.Flex : DisplayStyle.None;
                    }
                });
            }
            else
            {
                m_MainServiceToggle.style.display = DisplayStyle.None;
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
            m_LastEventTime = DateTime.MinValue;

            m_ValidationPoller.Shutdown();

            m_BasicDataValidated = false;
            m_CustomDataIntegrated = false;
            m_MonetizationDataIntegrated = false;
            m_Events.Clear();
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

            List<JSONValue> basicEvents = null;
            m_Events.Clear();

            var jsonParser = new JSONParser(downloadedData);
            try
            {
                var json = jsonParser.Parse();
                basicStatus = json.AsDict()[k_JsonKeyBasic].AsString();
                customStatus = json.AsDict()[k_JsonKeyCustom].AsString();
                monetizationStatus = json.AsDict()[k_JsonKeyMonetization].AsString();

                basicEvents = json.AsDict()[k_JsonKeyBasicEvents].AsList();
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

            foreach (var jsonEvent in basicEvents)
            {
                AnalyticsValidatorEvent parsedEvent;

                var type = jsonEvent.AsDict()[k_JsonKeyType].AsString();
                if (type == k_JsonValueCustomType)
                {
                    parsedEvent = new CustomValidatorEvent(jsonEvent);
                }
                else if (type == k_JsonValueDeviceInfoType)
                {
                    parsedEvent = new DeviceInfoValidatorEvent(jsonEvent);
                }
                else if (type == k_JsonValueTransactionType)
                {
                    parsedEvent = new TransactionValidatorEvent(jsonEvent);
                }
                else
                {
                    parsedEvent = new AnalyticsValidatorEvent(jsonEvent);
                }

                if (parsedEvent.GetTimeStamp() > m_LastEventTime)
                {
                    m_Events.Add(parsedEvent);
                }
            }

            if (m_BasicDataValidated && m_NotifyOnBasicValidate != null)
            {
                m_NotifyOnBasicValidate();
                m_NotifyOnBasicValidate = null;
            }

            if (m_Events.Count > 0 && m_NotifyOnEventsUpdate != null)
            {
                m_NotifyOnEventsUpdate();
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

        public void RequestNotifyOnDataUpdate(Action onCheck)
        {
            m_NotifyOnEventsUpdate = onCheck;
        }

        void RequestDataClearing()
        {
            if (m_Events.Count > 0 && m_ValidationPoller.IsReady() && m_DataClearRequest == null)
            {
                m_LastEventTime = m_Events[0].GetTimeStamp();
                AnalyticsService.instance.ClearValidationData(OnClearValidationData, m_ValidationPoller.projectAuthSignature, out m_DataClearRequest);
            }
        }

        void OnClearValidationData(AsyncOperation op)
        {
            if (op.isDone)
            {
                if (m_DataClearRequest != null)
                {
                    if ((m_DataClearRequest.result != UnityWebRequest.Result.ProtocolError) && (m_DataClearRequest.result != UnityWebRequest.Result.ConnectionError))
                    {
                        m_Events.RemoveAll(x => x.GetTimeStamp() <= m_LastEventTime);
                    }

                    m_DataClearRequest.Dispose();
                    m_DataClearRequest = null;
                }
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
            protected const string k_TableClass = "table";

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
                            Application.OpenURL(string.Format(AnalyticsConfiguration.instance.dashboardUrl, Connect.UnityConnect.instance.projectInfo.projectGUID));
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
            const string k_ValidatorEventsTemplatePath = "UXML/ServicesWindow/AnalyticsProjectSettingsValidatorEventsTemplate.uxml";
            const string k_CustomEventsTemplatePath = "UXML/ServicesWindow/AnalyticsProjectSettingsCustomEventsTemplate.uxml";
            const string k_DeviceInfoEventsTemplatePath = "UXML/ServicesWindow/AnalyticsProjectSettingsDeviceInfoEventsTemplate.uxml";
            const string k_TransactionEventsTemplatePath = "UXML/ServicesWindow/AnalyticsProjectSettingsTransactionEventsTemplate.uxml";
            const string k_CustomDataExceptionMessage =  "Exception occurred trying to parse custom data event type {0} and was not handled. Message: {1}";

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
            const string k_ValidatorBlock = "ValidatorBlock";
            const string k_ClearButton = "ClearBtn";

            //Validator uxml names
            const string k_ValidatorUtcTimeLabel = "UTC Time";
            const string k_ValidatorEventTypeLabel = "Event Type";
            const string k_ValidatorPlatformLabel = "Platform";
            const string k_ValidatorSdkVersionLabel = "SDKVersion";
            private const string k_SpecialTemplatePlaceholder = "SpecialTemplatePlaceholder";

            //Custom Validator uxml names
            const string k_CustomName = "EventName";
            const string k_CustomNoParams = "has-no-params";
            const string k_CustomParamBlock = "param-container";

            //Device Info Validator uxml names
            const string k_DeviceInfoType = "DeviceType";
            const string k_DeviceInfoOSVersion = "OSVersion";
            const string k_DeviceInfoAppVersion = "AppVersion";
            const string k_DeviceInfoBundleId = "BundleID";
            const string k_DeviceInfoProcessor = "Processor";
            const string k_DeviceInfoSystemMemory = "SystemMemory";
            const string k_DeviceInfoUnityEngine = "UnityEngine";

            //Transaction Validator uxml names
            const string k_TransactionAmount = "Amount";
            const string k_TransactionProductID = "ProductID";
            const string k_TransactionReceipt = "has-receipt";
            const string k_TransactionNoReceipt = "has-no-receipt";

            //uxml classes
            const string k_CheckIconClass = "check-icon";
            const string k_PreCheckIconClass = "pre-check-icon";
            const string k_TableHeaderClass = "table-header";
            const string k_TableHeaderRowClass = "table-header-row";
            const string k_BulletClass = "bullet-item";
            const string k_TableClassSuffix = "-column";

            //Additional Event Dictionary Content
            const string k_CustomKey = "Custom";
            const string k_CustomTitle = "Custom Events";
            const string k_CustomDesc = "Understand player behavior and usage patterns with event data.";

            const string k_MonetizaionTitleAndKey = "Monetization";
            const string k_MonetizaionDesc = "Track in-game revenue and monitor fraudulent transactions.";

            //formatting
            const string k_CustomDataFormat = "\u2022 {0} > {1}";

            VisualElement m_ValidatorTable;

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
                    [k_MonetizaionTitleAndKey] = new AdditionalEvent() { title = k_MonetizaionTitleAndKey, description = k_MonetizaionDesc, learnUrl = AnalyticsConfiguration.instance.monetizationLearnUrl } ,
                };
            }

            public override void EnterState()
            {
                m_AdditionalEvents[k_CustomKey].integrated = provider.m_CustomDataIntegrated;
                m_AdditionalEvents[k_MonetizaionTitleAndKey].integrated = provider.m_MonetizationDataIntegrated;

                LoadTemplateIntoScrollContainer(k_MainTemplatePath);
                SetupIapWarningBlock();
                SetupWelcomeBlock();
                SetupAdditionalEventsBlock();
                SetupSupportedPlatformsBlock();
                SetupValidatorBlock();

                provider.m_ValidationPoller.Start();
                provider.RequestNotifyOnDataUpdate(OnValidationDataUpdate);

                provider.HandlePermissionRestrictedControls();

                // Prepare the package section and update the package information
                PreparePackageSection(provider.rootVisualElement);
                UpdatePackageInformation();
            }

            void OnValidationDataUpdate()
            {
                if (stateMachine.currentState == this)
                {
                    SetupValidatorBlock();
                }
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
                        Application.OpenURL(string.Format(AnalyticsConfiguration.instance.dashboardUrl, Connect.UnityConnect.instance.projectInfo.projectGUID));
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

            void SetupValidatorBlock()
            {
                var validatorBlock = provider.rootVisualElement.Q(k_ValidatorBlock);
                if (validatorBlock != null)
                {
                    validatorBlock.Q<Button>(k_ClearButton).clicked += ClearValidationEvents;

                    m_ValidatorTable = validatorBlock.Q(className: k_TableClass);
                    if (m_ValidatorTable != null)
                    {
                        ClearValidationTable();
                        PopulateValidationTable();

                        var tableHeaderRow = m_ValidatorTable.Q(className: k_TableHeaderRowClass);
                        if (tableHeaderRow != null)
                        {
                            ServicesUtils.CapitalizeStringsInTree(tableHeaderRow);

                            var headerElements = tableHeaderRow.Query<VisualElement>(className: k_TableHeaderClass);
                            headerElements.ForEach((element) =>
                            {
                                var columnClass = element.classList.Find(s => s.Contains(k_TableClassSuffix));
                                element.RegisterCallback<GeometryChangedEvent>((evt) =>
                                {
                                    float columnWidth = evt.newRect.width;
                                    FormatValidationColumn(columnClass, columnWidth);
                                });
                            });
                        }
                    }
                }
            }

            void PopulateValidationTable()
            {
                if (m_ValidatorTable != null)
                {
                    var scrollContainer = m_ValidatorTable.Q(className: k_ScrollContainerClass);
                    if (scrollContainer != null)
                    {
                        var eventTemplate = EditorGUIUtility.Load(k_ValidatorEventsTemplatePath) as VisualTreeAsset;
                        if (eventTemplate != null)
                        {
                            foreach (var validatorEvent in provider.m_Events)
                            {
                                PopulateAnalyticsEvent(validatorEvent, eventTemplate, scrollContainer);
                            }
                        }
                    }
                }
            }

            void FormatValidationColumn(string columnClass, float columnWidth)
            {
                var columnElements = m_ValidatorTable.Query<VisualElement>(className: columnClass);
                columnElements.ForEach((element) =>
                {
                    element.style.width = columnWidth;
                });
            }

            void PopulateAnalyticsEvent(AnalyticsValidatorEvent validatorEvent, VisualTreeAsset cloneableAsset, VisualElement targetContainer)
            {
                if (validatorEvent.GetTimeStamp() > provider.m_LastEventTime)
                {
                    var eventBlock = cloneableAsset.CloneTree().contentContainer;
                    ServicesUtils.TranslateStringsInTree(eventBlock);
                    if (eventBlock != null)
                    {
                        //set core text
                        eventBlock.Q<Label>(k_ValidatorUtcTimeLabel).text = validatorEvent.GetTimeStampText();
                        eventBlock.Q<Label>(k_ValidatorEventTypeLabel).text = validatorEvent.GetTypeText();
                        eventBlock.Q<Label>(k_ValidatorPlatformLabel).text = validatorEvent.GetPlatformText();
                        eventBlock.Q<Label>(k_ValidatorSdkVersionLabel).text = validatorEvent.GetSdkVersionText();

                        var specialTemplateBlock = eventBlock.Q(k_SpecialTemplatePlaceholder);
                        if (specialTemplateBlock != null)
                        {
                            //special case text
                            if (validatorEvent is CustomValidatorEvent)
                            {
                                var eventTemplate =
                                    EditorGUIUtility.Load(k_CustomEventsTemplatePath) as VisualTreeAsset;
                                if (eventTemplate != null)
                                {
                                    PopulateCustomEvent((CustomValidatorEvent)validatorEvent, eventTemplate,
                                        specialTemplateBlock);
                                }
                            }
                            else if (validatorEvent is DeviceInfoValidatorEvent)
                            {
                                var eventTemplate =
                                    EditorGUIUtility.Load(k_DeviceInfoEventsTemplatePath) as VisualTreeAsset;
                                if (eventTemplate != null)
                                {
                                    PopulateDeviceInfoEvent((DeviceInfoValidatorEvent)validatorEvent, eventTemplate,
                                        specialTemplateBlock);
                                }
                            }
                            else if (validatorEvent is TransactionValidatorEvent)
                            {
                                var eventTemplate =
                                    EditorGUIUtility.Load(k_TransactionEventsTemplatePath) as VisualTreeAsset;
                                if (eventTemplate != null)
                                {
                                    PopulateTransactionEvent((TransactionValidatorEvent)validatorEvent, eventTemplate,
                                        specialTemplateBlock);
                                }
                            }
                            else
                            {
                                specialTemplateBlock.style.display = DisplayStyle.None;
                            }
                        }
                    }

                    targetContainer.Add(eventBlock);
                }
            }

            void PopulateCustomEvent(CustomValidatorEvent validatorEvent, VisualTreeAsset cloneableAsset,
                VisualElement targetContainer)
            {
                var eventBlock = cloneableAsset.CloneTree().contentContainer;
                eventBlock.Q<Label>(k_CustomName).text = validatorEvent.GetNameText();

                try
                {
                    List<string> customStrings = validatorEvent.GetCustomParamsTexts(k_CustomDataFormat);

                    bool hasParams = customStrings.Count > 0;
                    var paramsBlock = eventBlock.Q(k_CustomParamBlock);
                    if (paramsBlock != null)
                    {
                        paramsBlock.style.display = hasParams ? DisplayStyle.Flex : DisplayStyle.None;

                        foreach (var customString in customStrings)
                        {
                            var customLabel = new Label(customString);
                            customLabel.AddToClassList(k_BulletClass);
                            paramsBlock.Add(customLabel);
                        }
                    }

                    var noParamsBlock = eventBlock.Q(k_CustomNoParams);
                    if (noParamsBlock != null)
                    {
                        noParamsBlock.style.display = hasParams ? DisplayStyle.None : DisplayStyle.Flex;
                    }
                }
                catch (Exception ex)
                {
                    NotificationManager.instance.Publish(provider.serviceInstance.notificationTopic, Notification.Severity.Warning,
                        string.Format(L10n.Tr(k_CustomDataExceptionMessage), validatorEvent.GetNameText(), ex.Message));

                    Debug.LogException(ex);
                }

                targetContainer.Add(eventBlock);
            }

            void PopulateDeviceInfoEvent(DeviceInfoValidatorEvent validatorEvent, VisualTreeAsset cloneableAsset,
                VisualElement targetContainer)
            {
                var eventBlock = cloneableAsset.CloneTree().contentContainer;

                eventBlock.Q<Label>(k_DeviceInfoType).text = validatorEvent.GetDeviceTypeText();
                eventBlock.Q<Label>(k_DeviceInfoOSVersion).text = validatorEvent.GetOSVersionText();
                eventBlock.Q<Label>(k_DeviceInfoAppVersion).text = validatorEvent.GetAppVersionText();
                eventBlock.Q<Label>(k_DeviceInfoBundleId).text = validatorEvent.GetBundleIdText();
                eventBlock.Q<Label>(k_DeviceInfoProcessor).text = validatorEvent.GetProcessorText();
                eventBlock.Q<Label>(k_DeviceInfoSystemMemory).text = validatorEvent.GetSystemMemoryText();
                eventBlock.Q<Label>(k_DeviceInfoUnityEngine).text = validatorEvent.GetUnityEngineText();

                targetContainer.Add(eventBlock);
            }

            void PopulateTransactionEvent(TransactionValidatorEvent validatorEvent, VisualTreeAsset cloneableAsset,
                VisualElement targetContainer)
            {
                var eventBlock = cloneableAsset.CloneTree().contentContainer;

                eventBlock.Q<Label>(k_TransactionAmount).text = validatorEvent.GetPriceText() + " " + validatorEvent.GetCurrencyText();
                eventBlock.Q<Label>(k_TransactionProductID).text = validatorEvent.GetProductIdText();

                bool hasReceipt = validatorEvent.HasReceipt();
                var hasReceiptBlock = eventBlock.Q(k_TransactionReceipt);
                if (hasReceiptBlock != null)
                {
                    hasReceiptBlock.style.display = hasReceipt ? DisplayStyle.Flex : DisplayStyle.None;
                }

                var noReceiptBlock = eventBlock.Q(k_TransactionNoReceipt);
                if (noReceiptBlock != null)
                {
                    noReceiptBlock.style.display = hasReceipt ? DisplayStyle.None : DisplayStyle.Flex;
                }

                targetContainer.Add(eventBlock);
            }

            void ClearValidationEvents()
            {
                EditorAnalytics.SendClearAnalyticsDataEvent(new ClearDataInfo() { clearedEvents = provider.m_Events.Count });

                provider.RequestDataClearing();
                ClearValidationTable();
            }

            void ClearValidationTable()
            {
                // Don't move to callback, as we want to hide all current events
                if (m_ValidatorTable != null)
                {
                    var scrollContainer = m_ValidatorTable.Q(className: k_ScrollContainerClass);
                    scrollContainer?.Clear();
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
