// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Advertisements;
using UnityEngine.Networking;
using UnityEditorInternal;

namespace UnityEditor.Connect
{
    internal class AdsProjectSettings : ServicesProjectSettings
    {
        const string k_ServiceName = "Ads";

        // Actual states for the Ads state machine
        const string k_StateNameDisabled = "DisabledState";
        const string k_StateNameEnabled = "EnabledState";

        const string k_AdsCommonUxmlPath = "UXML/ServicesWindow/AdsProjectSettings.uxml";
        const string k_AdsDisabledUxmlPath = "UXML/ServicesWindow/AdsProjectSettingsDisabled.uxml";
        const string k_AdsEnabledUxmlPath = "UXML/ServicesWindow/AdsProjectSettingsEnabled.uxml";

        // Elements of the UXML
        const string k_ServiceToggleClassName = "service-toggle";
        const string k_ServiceNameProperty = "serviceName";
        const string k_ServiceScrollContainerClassName = "scroll-container";

        const string k_AdsPermissionMessage = "You do not have sufficient permissions to enable / disable Ads service.";

        // Asset store GUIDs
        const string k_AdvertisingAndroidAdsDllGuid = "cad99f482ce25421196533fe02e6a13e";
        const string k_AdvertisingIosAdsDllGuid = "d6f3e2ade30154a80a137e0079f66a08";
        const string k_AdvertisingEditorDllGuid = "56921141d53fd4a5888445107b1b1286";

        const string k_AdsAssetStorePackageInstalledWarning = "The Asset Store Package is installed.\n Usage of Package Manager is recommended.";
        const string k_AdsPackageName = "Ads Package";

        const string k_GettingStartedLink = "GettingStarted";
        const string k_LearnMoreLink = "LearnMore";
        const string k_GoToDashboardLinkName = "GoToDashboard";
        const string k_ToggleTestModeName = "ToggleTestMode";
        const string k_AppleGameIdName = "AppleGameId";
        const string k_AndroidGameIdName = "AndroidGameId";

        const string k_GameIdApiUrl = "/unity/games";
        const string k_JsonAppleGameId = "iOSGameKey";
        const string k_JsonAndroidGameId = "androidGameKey";

        VisualElement m_GoToDashboard;
        Toggle m_MainServiceToggle;
        bool m_EventHandlerInitialized;
        UnityWebRequest m_CurrentWebRequest;

        string m_AppleGameId;
        string m_AndroidGameId;

        EnabledState m_EnabledState;
        DisabledState m_DisabledState;

        internal enum AdsEvent
        {
            Enabling,
            Disabling,
        }

        [SettingsProvider]
        public static SettingsProvider CreateServicesProvider()
        {
            return new AdsProjectSettings(AdsService.instance.projectSettingsPath, SettingsScope.Project);
        }

        SimpleStateMachine<AdsEvent> m_StateMachine;
        public AdsProjectSettings(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, k_ServiceName, keywords)
        {
            m_StateMachine = new SimpleStateMachine<AdsEvent>();
            m_StateMachine.AddEvent(AdsEvent.Enabling);
            m_StateMachine.AddEvent(AdsEvent.Disabling);
            m_EnabledState = new EnabledState(m_StateMachine, this);
            m_DisabledState = new DisabledState(m_StateMachine, this);
            m_AppleGameId = AdvertisementSettings.GetGameId(RuntimePlatform.IPhonePlayer);
            m_AndroidGameId = AdvertisementSettings.GetGameId(RuntimePlatform.Android);

            m_StateMachine.AddState(m_EnabledState);
            m_StateMachine.AddState(m_DisabledState);
        }

        void OnDestroy()
        {
            UnregisterEvent();
        }

        protected override Notification.Topic[] notificationTopicsToSubscribe => new[]
        {
            Notification.Topic.AdsService,
            Notification.Topic.ProjectBind,
            Notification.Topic.CoppaCompliance
        };
        protected override SingleService serviceInstance => AdsService.instance;
        protected override string serviceUssClassName => "ads";
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
                    singleService.EnableService(evt.newValue);
                    if (m_GoToDashboard != null)
                    {
                        m_GoToDashboard.style.display = (evt.newValue) ? DisplayStyle.Flex : DisplayStyle.None;
                    }

                    // When turning off the service, the game ids must be put to null and refetched when reenabled, but when entering the enabled state.
                    SetupServiceToggleLabel(m_MainServiceToggle, evt.newValue);
                    if (!evt.newValue)
                    {
                        m_AppleGameId = null;
                        m_AndroidGameId = null;
                        SetUpGameId();
                    }
                });
            }
            else
            {
                m_MainServiceToggle.style.display = DisplayStyle.None;
            }
        }

        void RequestAdsGameIds()
        {
            if (m_CurrentWebRequest == null)
            {
                var bodyContent = "{\"projectGUID\": \"" + UnityConnect.instance.projectInfo.projectGUID + "\",\"projectName\":\"" + UnityConnect.instance.projectInfo.projectName + "\",\"token\":\"" + UnityConnect.instance.GetUserInfo().accessToken + "\"}";
                var uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(bodyContent));

                m_CurrentWebRequest = new UnityWebRequest(ServicesConfiguration.instance.adsOperateApiUrl + k_GameIdApiUrl, UnityWebRequest.kHttpVerbPOST) { downloadHandler = new DownloadHandlerBuffer(), uploadHandler = uploadHandler };
                m_CurrentWebRequest.SetRequestHeader("Content-Type", "application/json;charset=UTF-8");
                var operation = m_CurrentWebRequest.SendWebRequest();
                operation.completed += RequestOperationOnCompleted;
            }
        }

        void RequestOperationOnCompleted(AsyncOperation asyncOperation)
        {
            if (asyncOperation.isDone)
            {
                if ((m_CurrentWebRequest != null) &&
                    (m_CurrentWebRequest.result != UnityWebRequest.Result.ConnectionError) &&
                    (m_CurrentWebRequest.result != UnityWebRequest.Result.ProtocolError) &&
                    (m_CurrentWebRequest.downloadHandler != null))
                {
                    if (m_CurrentWebRequest.downloadHandler.text.Length != 0)
                    {
                        var jsonParser = new JSONParser(m_CurrentWebRequest.downloadHandler.text);
                        try
                        {
                            var json = jsonParser.Parse();
                            var key = k_JsonAppleGameId;
                            if (json.AsDict().ContainsKey(key))
                            {
                                m_AppleGameId = json.AsDict()[key].ToString();
                            }
                            key = k_JsonAndroidGameId;
                            if (json.AsDict().ContainsKey(key))
                            {
                                m_AndroidGameId = json.AsDict()[key].ToString();
                            }
                            SetUpGameId();
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                            NotificationManager.instance.Publish(Notification.Topic.AdsService, Notification.Severity.Error, ex.Message);
                        }
                    }
                }
                m_CurrentWebRequest?.Dispose();
                m_CurrentWebRequest = null;
            }
        }

        void SetUpGameId()
        {
            var unavailableGameId = L10n.Tr("N/A");

            AdvertisementSettings.SetGameId(RuntimePlatform.IPhonePlayer, m_AppleGameId);
            AdvertisementSettings.SetGameId(RuntimePlatform.Android, m_AndroidGameId);
            if (m_EnabledState.m_AndroidGameIdTextField != null)
            {
                m_EnabledState.m_AndroidGameIdTextField.value = m_AndroidGameId ?? unavailableGameId;
            }
            if (m_EnabledState.m_AppleGameIdTextField != null)
            {
                m_EnabledState.m_AppleGameIdTextField.value = m_AppleGameId ?? unavailableGameId;
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
                    var notifications = NotificationManager.instance.GetNotificationsForTopics(Notification.Topic.AdsService);
                    if (notifications.Any(notification => notification.rawMessage == k_AdsPermissionMessage))
                    {
                        return;
                    }

                    NotificationManager.instance.Publish(
                        Notification.Topic.AdsService,
                        Notification.Severity.Warning,
                        k_AdsPermissionMessage);
                }
            }
        }

        protected override void ActivateAction(string searchContext)
        {
            // Must reset properties every time this is activated
            var mainTemplate = EditorGUIUtility.Load(k_AdsCommonUxmlPath) as VisualTreeAsset;
            var newVisual = mainTemplate.CloneTree().contentContainer;
            ServicesUtils.TranslateStringsInTree(newVisual);
            rootVisualElement.Add(newVisual);

            // Make sure to activate the state machine to the current state...
            if (AdsService.instance.IsServiceEnabled())
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
                    OpenDashboardForOrganizationForeignKey(ServicesConfiguration.instance.adsDashboardUrl);
                });
                m_GoToDashboard.AddManipulator(clickable);
            }

            m_MainServiceToggle = rootVisualElement.Q<Toggle>(className: k_ServiceToggleClassName);
            SetupServiceToggle(AdsService.instance);
        }

        protected override void DeactivateAction()
        {
            // Make sure to reset the state machine
            m_StateMachine.ClearCurrentState();

            if (m_CurrentWebRequest != null)
            {
                m_CurrentWebRequest.Abort();
                m_CurrentWebRequest.Dispose();
                m_CurrentWebRequest = null;
            }
            UnregisterEvent();
        }

        void RegisterEvent()
        {
            if (!m_EventHandlerInitialized)
            {
                AdsService.instance.serviceAfterEnableEvent += ServiceIsEnablingEvent;
                AdsService.instance.serviceAfterDisableEvent += ServiceIsDisablingEvent;
                m_EventHandlerInitialized = true;
            }
        }

        void UnregisterEvent()
        {
            if (m_EventHandlerInitialized)
            {
                AdsService.instance.serviceAfterEnableEvent -= ServiceIsEnablingEvent;
                AdsService.instance.serviceAfterDisableEvent -= ServiceIsDisablingEvent;
                m_EventHandlerInitialized = false;
            }
        }

        void ServiceIsEnablingEvent(object sender, EventArgs args)
        {
            if (settingsWindow.GetCurrentProvider() == this)
            {
                m_MainServiceToggle.SetValueWithoutNotify(true);
                m_StateMachine.ProcessEvent(AdsEvent.Enabling);
            }
        }

        void ServiceIsDisablingEvent(object sender, EventArgs args)
        {
            if (settingsWindow.GetCurrentProvider() == this)
            {
                m_MainServiceToggle.SetValueWithoutNotify(false);
                m_StateMachine.ProcessEvent(AdsEvent.Disabling);
            }
        }

        class BaseState : GenericBaseState<AdsProjectSettings, AdsEvent>
        {
            public BaseState(string name, SimpleStateMachine<AdsEvent> simpleStateMachine, AdsProjectSettings provider)
                : base(name, simpleStateMachine, provider)
            {
            }
        }

        // Disabled state of the service
        sealed class DisabledState : BaseState
        {
            public DisabledState(SimpleStateMachine<AdsEvent> simpleStateMachine, AdsProjectSettings provider)
                : base(k_StateNameDisabled, simpleStateMachine, provider)
            {
                ModifyActionForEvent(AdsEvent.Enabling, HandleBinding);
            }

            public override void EnterState()
            {
                var generalTemplate = EditorGUIUtility.Load(k_AdsDisabledUxmlPath) as VisualTreeAsset;
                var scrollContainer = provider.rootVisualElement.Q(null, k_ServiceScrollContainerClassName);
                scrollContainer.Clear();
                if (generalTemplate != null)
                {
                    var newVisual = generalTemplate.CloneTree().contentContainer;
                    ServicesUtils.TranslateStringsInTree(newVisual);
                    scrollContainer.Add(newVisual);

                    var gettingStarted = scrollContainer.Q(k_GettingStartedLink);
                    if (gettingStarted != null)
                    {
                        var clickable = new Clickable(() =>
                        {
                            Application.OpenURL(ServicesConfiguration.instance.adsGettingStartedUrl);
                        });
                        gettingStarted.AddManipulator(clickable);
                    }
                }
                scrollContainer.Add(ServicesUtils.SetupSupportedPlatformsBlock(ServicesUtils.GetAdsSupportedPlatforms()));
                provider.HandlePermissionRestrictedControls();
            }

            SimpleStateMachine<AdsEvent>.State HandleBinding(AdsEvent raisedEvent)
            {
                return stateMachine.GetStateByName(k_StateNameEnabled);
            }
        }

        // Enabled state of the service
        sealed class EnabledState : BaseState
        {
            public TextField m_AppleGameIdTextField;
            public TextField m_AndroidGameIdTextField;

            bool m_AssetStoreWarningHasBeenShown;

            public EnabledState(SimpleStateMachine<AdsEvent> simpleStateMachine, AdsProjectSettings provider)
                : base(k_StateNameEnabled, simpleStateMachine, provider)
            {
                ModifyActionForEvent(AdsEvent.Disabling, HandleUnbinding);
                // Related protected variables
                topicForNotifications = Notification.Topic.AdsService;
                notLatestPackageInstalledInfo = string.Format(k_NotLatestPackageInstalledInfo, k_AdsPackageName);
                packageInstallationHeadsup = string.Format(k_PackageInstallationHeadsup, k_AdsPackageName);;
                duplicateInstallWarning = k_DuplicateInstallWarning;
                packageInstallationDialogTitle = string.Format(k_PackageInstallationDialogTitle, k_AdsPackageName);
            }

            void VerifyAssetStorePackageInstallation()
            {
                var assetStoreAndroidDllPath = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(k_AdvertisingAndroidAdsDllGuid /* UnityEngine.Advertising.Android.dll */)) as PluginImporter;
                var assetStoreIosDllPath = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(k_AdvertisingIosAdsDllGuid /* UnityEngine.Advertising.iOS.dll */)) as PluginImporter;
                var assetStoreEditorDllPath = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(k_AdvertisingEditorDllGuid /* UnityEngine.Advertising.Editor.dll */)) as PluginImporter;

                // Assume it is not installed...
                assetStorePackageInstalled = false;

                if ((assetStoreAndroidDllPath != null) || (assetStoreIosDllPath != null) || (assetStoreEditorDllPath != null))
                {
                    assetStorePackageInstalled = true;
                }
            }

            public override void EnterState()
            {
                // Just get the latest state of the asset store package installation...
                VerifyAssetStorePackageInstallation();

                if (assetStorePackageInstalled && !m_AssetStoreWarningHasBeenShown)
                {
                    NotificationManager.instance.Publish(Notification.Topic.AdsService, Notification.Severity.Warning, L10n.Tr(k_AdsAssetStorePackageInstalledWarning));
                    m_AssetStoreWarningHasBeenShown = true;
                }

                // If we haven't received new bound info, fetch them
                var generalTemplate = EditorGUIUtility.Load(k_AdsEnabledUxmlPath) as VisualTreeAsset;
                var scrollContainer = provider.rootVisualElement.Q(null, k_ServiceScrollContainerClassName);
                if (generalTemplate != null)
                {
                    var newVisual = generalTemplate.CloneTree().contentContainer;
                    ServicesUtils.TranslateStringsInTree(newVisual);
                    scrollContainer.Clear();
                    scrollContainer.Add(newVisual);
                    var learnMore = scrollContainer.Q(k_LearnMoreLink);
                    if (learnMore != null)
                    {
                        var clickable = new Clickable(() =>
                        {
                            Application.OpenURL(ServicesConfiguration.instance.adsLearnMoreUrl);
                        });
                        learnMore.AddManipulator(clickable);
                    }
                    var toggleTestMode = scrollContainer.Q<Toggle>(k_ToggleTestModeName);
                    if (toggleTestMode != null)
                    {
                        toggleTestMode.SetValueWithoutNotify(AdvertisementSettings.testMode);
                        toggleTestMode.RegisterValueChangedCallback(evt =>
                        {
                            AdvertisementSettings.testMode = evt.newValue;
                        });
                    }

                    // Prepare the package section and update the package information
                    PreparePackageSection(scrollContainer);
                    UpdatePackageInformation();

                    // Getting the textfield for updates with the actual GameId values...
                    m_AppleGameIdTextField = scrollContainer.Q<TextField>(k_AppleGameIdName);
                    m_AndroidGameIdTextField = scrollContainer.Q<TextField>(k_AndroidGameIdName);
                    provider.SetUpGameId();
                    scrollContainer.Add(ServicesUtils.SetupSupportedPlatformsBlock(ServicesUtils.GetAdsSupportedPlatforms()));

                    provider.HandlePermissionRestrictedControls();
                }
                // Refresh the game Id when entering the ON state...
                provider.RequestAdsGameIds();
            }

            SimpleStateMachine<AdsEvent>.State HandleUnbinding(AdsEvent raisedEvent)
            {
                return stateMachine.GetStateByName(k_StateNameDisabled);
            }
        }
    }
}
