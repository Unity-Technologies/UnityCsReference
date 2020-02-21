// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace UnityEditor.Connect
{
    internal class CloudBuildProjectSettings : ServicesProjectSettings
    {
        const string k_ServiceName = "Build";

        // Actual states for the cloud build state machine
        const string k_StateNameDisabled = "DisabledState";
        const string k_StateNameEnabled = "EnabledState";

        const string k_CloudBuildCommonUxmlPath = "UXML/ServicesWindow/CloudBuildProjectSettings.uxml";
        const string k_CloudBuildDisabledUxmlPath = "UXML/ServicesWindow/CloudBuildProjectSettingsStateDisabled.uxml";
        const string k_CloudBuildEnabledUxmlPath = "UXML/ServicesWindow/CloudBuildProjectSettingsStateEnabled.uxml";

        const string k_SubscriptionPersonal = "personal";
        const string k_SubscriptionTeamsBasic = "teams basic";
        //Other subscription possibility is "teams advanced", but we don't need it for now.

        // Elements of the UXML
        const string k_ServiceToggleClassName = "service-toggle";
        const string k_ServiceNameProperty = "serviceName";
        const string k_ServiceScrollContainerClassName = "scroll-container";
        const string k_ServiceTargetContainerClassName = "target-container";
        const string k_ServiceTargetContainerTitleClassName = "target-container-title";
        const string k_ServiceCloudBuildContainerClassName = "cloud-build-container";
        const string k_ServiceCloudProgressClassName = "cloud-progress";

        const string k_LearnMoreLink = "LearnMore";
        const string k_StartUsingCloudBuildLink = "StartUsingCloudBuild";
        const string k_HistoryButtonName = "HistoryButton";
        const string k_UploadButtonName = "UploadButton";
        const string k_ManageTargetButton = "ManageTargetButton";
        const string k_AddTargetButton = "AddTargetButton";
        const string k_NoTargetContainer = "NoTargetContainer";
        const string k_PollFooterSectionName = "PollFooterSection";
        const string k_PollFooterName = "PollFooter";
        const string k_PollToggleName = "PollToggle";
        const string k_GoToDashboardLink = "GoToDashboard";

        VisualElement m_GoToDashboard;
        Toggle m_MainServiceToggle;
        bool m_EventHandlerInitialized;

        UnityWebRequest m_GetProjectRequest;
        UnityWebRequest m_GetProjectBillingPlanRequest;
        UnityWebRequest m_GetProjectBuildTargetsRequest;
        UnityWebRequest m_GetApiStatusRequest;
        List<UnityWebRequest> m_BuildRequests = new List<UnityWebRequest>();

        const string k_ClassNameTargetEntry = "target-entry";
        const string k_ClassNameTitle = "title";
        const string k_ClassNameBuildButton = "build-button";
        const string k_ClassNameSeparator = "separator";

        const string k_JsonNodeNameDisabled = "disabled";
        const string k_JsonNodeNameLinks = "links";
        const string k_JsonNodeNameSelf = "self";
        const string k_JsonNodeNameHref = "href";
        const string k_JsonNodeNameListBuildTargets = "list_buildtargets";
        const string k_JsonNodeNameLatestBuilds = "latest_builds";
        const string k_JsonNodeNameEffective = "effective";
        const string k_JsonNodeNameLabel = "label";
        const string k_JsonNodeNameName = "name";
        const string k_JsonNodeNameBuildTargetId = "buildtargetid";
        const string k_JsonNodeNameBuildTargetName = "buildtargetname";
        const string k_JsonNodeNameEnabled = "enabled";
        const string k_JsonNodeNameStartBuilds = "start_builds";
        const string k_JsonNodeNameText = "text";
        const string k_JsonNodeNameBillingPlan = "billingPlan";
        const string k_JsonNodeNameAlertType = "alertType";
        const string k_JsonNodeNameBuild = "build";
        const string k_JsonNodeNameError = "error";

        const string k_UrlSuffixBillingPlan = "/billingplan";

        const string k_LaunchBuildPayload = "{\"clean\":false}";

        const string k_MessageErrorForProjectData = "An unexpected error occurred while querying Cloud Build for current project data. See the console for more information.";
        const string k_MessageErrorForProjectTeamData = "An unexpected error occurred while querying Cloud Build for current project team data. See the console for more information.";
        const string k_MessageErrorForProjectBuildTargetsData = "An unexpected error occurred while querying Cloud Build for current project configured build targets. See the console for more information.";
        const string k_MessageProjectStateMismatch = "There is a mismatch between local and web configuration for Cloud Build. Please open the Cloud Build web dashboard and enable the current project.";
        const string k_MessageErrorForApiStatusData = "An unexpected error occurred while querying Cloud Build for api status. See the console for more information.";
        const string k_MessageErrorForBuildLaunch = "An unexpected error occurred while launching a build. See the console for more information.";
        const string k_CloudBuildPermissionMessage = "You do not have sufficient permissions to enable / disable Cloud Build service.";

        const string k_MessageLaunchingBuild = "Starting build {0}.";
        const string k_MessageLaunchedBuildSuccess = "Build #{0} {1} added to queue";
        const string k_MessageLaunchedBuildFailedWithMsg = "Build {0} wasn't launched: {1}";
        const string k_MessageLaunchedBuildFailure = "Unable to build project";

        const string k_BuildButtonNamePrefix = "BuildBtn_";
        const string k_LabelBuildButton = "Build";
        const string k_LabelConfiguredTargets = "Build targets";

        const string k_NumberOfBuildsToQuery = "25";
        const long k_HttpResponseCodeAccepted = 202L;

        internal enum CloudBuildEvent
        {
            Enabling,
            Disabling,
        }

        private struct BuildPostInfo
        {
            public string targetName;
        }

        [SettingsProvider]
        public static SettingsProvider CreateServicesProvider()
        {
            return new CloudBuildProjectSettings(BuildService.instance.projectSettingsPath, SettingsScope.Project);
        }

        SimpleStateMachine<CloudBuildEvent> m_StateMachine;
        EnabledState m_EnabledState;
        DisabledState m_DisabledState;

        public CloudBuildProjectSettings(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, k_ServiceName, keywords)
        {
            m_StateMachine = new SimpleStateMachine<CloudBuildEvent>();
            m_StateMachine.AddEvent(CloudBuildEvent.Enabling);
            m_StateMachine.AddEvent(CloudBuildEvent.Disabling);
            m_EnabledState = new EnabledState(m_StateMachine, this);
            m_StateMachine.AddState(m_EnabledState);
            m_DisabledState = new DisabledState(m_StateMachine, this);
            m_StateMachine.AddState(m_DisabledState);
        }

        void OnDestroy()
        {
            DeactivateAction();
        }

        protected override Notification.Topic[] notificationTopicsToSubscribe => new[]
        {
            Notification.Topic.BuildService,
            Notification.Topic.ProjectBind
        };
        protected override SingleService serviceInstance => BuildService.instance;
        protected override string serviceUssClassName => "cloudbuild";

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
                    var notifications = NotificationManager.instance.GetNotificationsForTopics(Notification.Topic.BuildService);
                    if (notifications.Any(notification => notification.rawMessage == k_CloudBuildPermissionMessage))
                    {
                        return;
                    }

                    NotificationManager.instance.Publish(
                        Notification.Topic.BuildService,
                        Notification.Severity.Warning,
                        k_CloudBuildPermissionMessage);
                }
            }
        }

        protected override void ActivateAction(string searchContext)
        {
            // Must reset properties every time this is activated
            var mainTemplate = EditorGUIUtility.Load(k_CloudBuildCommonUxmlPath) as VisualTreeAsset;

            // To allow the save using the view data, we must provide a key on the root element
            rootVisualElement.viewDataKey = "cloud-build-root-data-key";

            var mainTemplateContainer = mainTemplate.CloneTree().contentContainer;
            ServicesUtils.TranslateStringsInTree(mainTemplateContainer);
            rootVisualElement.Add(mainTemplateContainer);

            // Make sure to reset the state machine
            m_StateMachine.ClearCurrentState();

            // Make sure to activate the state machine to the current state...

            if (BuildService.instance.IsServiceEnabled())
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
            m_GoToDashboard = rootVisualElement.Q(k_GoToDashboardLink);
            if (m_GoToDashboard != null)
            {
                var clickable = new Clickable(() =>
                {
                    ServicesConfiguration.instance.RequestBaseCloudBuildDashboardUrl(OpenDashboardOrgAndProjectIds);
                });
                m_GoToDashboard.AddManipulator(clickable);
            }

            m_MainServiceToggle = rootVisualElement.Q<Toggle>(className: k_ServiceToggleClassName);
            SetupServiceToggle(BuildService.instance);

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
            FinalizeServiceCallbacks();
        }

        void FinalizeServiceCallbacks()
        {
            if (m_GetProjectRequest != null)
            {
                m_GetProjectRequest.Abort();
                m_GetProjectRequest.Dispose();
                m_GetProjectRequest = null;
            }
            if (m_GetProjectBillingPlanRequest != null)
            {
                m_GetProjectBillingPlanRequest.Abort();
                m_GetProjectBillingPlanRequest.Dispose();
                m_GetProjectBillingPlanRequest = null;
            }
            if (m_GetProjectBuildTargetsRequest != null)
            {
                m_GetProjectBuildTargetsRequest.Abort();
                m_GetProjectBuildTargetsRequest.Dispose();
                m_GetProjectBuildTargetsRequest = null;
            }
            if (m_GetApiStatusRequest != null)
            {
                m_GetApiStatusRequest.Abort();
                m_GetApiStatusRequest.Dispose();
                m_GetApiStatusRequest = null;
            }

            if (m_BuildRequests != null)
            {
                foreach (var buildRequest in m_BuildRequests)
                {
                    buildRequest.Abort();
                    buildRequest.Dispose();
                }
                m_BuildRequests.Clear();
            }
        }

        void RegisterEvent()
        {
            if (!m_EventHandlerInitialized)
            {
                BuildService.instance.serviceAfterEnableEvent += ServiceIsEnablingEvent;
                BuildService.instance.serviceAfterDisableEvent += ServiceIsDisablingEvent;
                m_EventHandlerInitialized = true;
            }
        }

        void UnregisterEvent()
        {
            if (m_EventHandlerInitialized)
            {
                BuildService.instance.serviceAfterEnableEvent -= ServiceIsEnablingEvent;
                BuildService.instance.serviceAfterDisableEvent -= ServiceIsDisablingEvent;
                m_EventHandlerInitialized = false;
            }
        }

        void ServiceIsEnablingEvent(object sender, EventArgs args)
        {
            if (settingsWindow.GetCurrentProvider() == this)
            {
                m_MainServiceToggle.SetValueWithoutNotify(true);
                m_StateMachine.ProcessEvent(CloudBuildEvent.Enabling);
            }
        }

        void ServiceIsDisablingEvent(object sender, EventArgs args)
        {
            if (settingsWindow.GetCurrentProvider() == this)
            {
                m_MainServiceToggle.SetValueWithoutNotify(false);
                m_StateMachine.ProcessEvent(CloudBuildEvent.Disabling);
            }
        }

        // Disabled state of the service
        sealed class DisabledState : SimpleStateMachine<CloudBuildEvent>.State
        {
            CloudBuildProjectSettings m_Provider;

            public DisabledState(SimpleStateMachine<CloudBuildEvent> simpleStateMachine, CloudBuildProjectSettings provider)
                : base(k_StateNameDisabled, simpleStateMachine)
            {
                m_Provider = provider;
                ModifyActionForEvent(CloudBuildEvent.Enabling, HandleBinding);
            }

            public override void EnterState()
            {
                CloudBuildPoller.instance.Disable(true);
                var generalTemplate = EditorGUIUtility.Load(k_CloudBuildDisabledUxmlPath) as VisualTreeAsset;
                var scrollContainer = m_Provider.rootVisualElement.Q(className: k_ServiceScrollContainerClassName);
                scrollContainer.Clear();
                if (generalTemplate != null)
                {
                    var disabledStateContent = generalTemplate.CloneTree().contentContainer;
                    ServicesUtils.TranslateStringsInTree(disabledStateContent);
                    scrollContainer.Add(disabledStateContent);
                    m_Provider.UpdateServiceToggleAndDashboardLink(m_Provider.serviceInstance.IsServiceEnabled());
                }

                var startUsing = m_Provider.rootVisualElement.Q(k_StartUsingCloudBuildLink);
                if (startUsing != null)
                {
                    var clickable = new Clickable(() =>
                    {
                        Application.OpenURL(ServicesConfiguration.instance.GetCloudBuildTutorialUrl());
                    });
                    startUsing.AddManipulator(clickable);
                }

                m_Provider.HandlePermissionRestrictedControls();
            }

            SimpleStateMachine<CloudBuildEvent>.State HandleBinding(CloudBuildEvent raisedEvent)
            {
                return stateMachine.GetStateByName(k_StateNameEnabled);
            }
        }

        // Enabled state of the service
        sealed class EnabledState : SimpleStateMachine<CloudBuildEvent>.State
        {
            CloudBuildProjectSettings m_Provider;
            string m_CloudBuildApiOrgProjectUrl;
            string m_CloudBuildApiOrgProjectBillingPlanUrl;
            string m_CloudBuildApiOrgProjectBuildTargetsUrl;
            string m_CloudBuildApiOrgLatestBuilds;
            string m_BillingPlanLabel;

            public EnabledState(SimpleStateMachine<CloudBuildEvent> simpleStateMachine, CloudBuildProjectSettings provider)
                : base(k_StateNameEnabled, simpleStateMachine)
            {
                m_Provider = provider;
                ModifyActionForEvent(CloudBuildEvent.Disabling, HandleUnbinding);
            }

            public override void EnterState()
            {
                // If we haven't received new bound info, fetch them
                var generalTemplate = EditorGUIUtility.Load(k_CloudBuildEnabledUxmlPath) as VisualTreeAsset;
                var scrollContainer = m_Provider.rootVisualElement.Q(className: k_ServiceScrollContainerClassName);
                var enabledStateContent = generalTemplate.CloneTree().contentContainer;
                ServicesUtils.TranslateStringsInTree(enabledStateContent);
                scrollContainer.Clear();
                scrollContainer.Add(enabledStateContent);

                m_Provider.rootVisualElement.Q(className: k_ServiceCloudBuildContainerClassName).style.display = DisplayStyle.None;
                m_Provider.rootVisualElement.Q(k_PollFooterSectionName).style.display = DisplayStyle.None;

                var historyButton = m_Provider.rootVisualElement.Q<Button>(k_HistoryButtonName);
                if (historyButton != null)
                {
                    historyButton.clicked += () =>
                    {
                        ServicesConfiguration.instance.RequestCurrentCloudBuildProjectHistoryUrl(currentCloudBuildProjectHistoryUrl =>
                        {
                            Application.OpenURL(currentCloudBuildProjectHistoryUrl);
                        });
                    };
                }

                var uploadButton = m_Provider.rootVisualElement.Q<Button>(k_UploadButtonName);
                if (uploadButton != null)
                {
                    uploadButton.clicked += () =>
                    {
                        ServicesConfiguration.instance.RequestCurrentCloudBuildProjectUploadUrl(currentCloudBuildProjectUploadUrl =>
                        {
                            Application.OpenURL(currentCloudBuildProjectUploadUrl);
                        });
                    };
                }

                var manageTargetButton = m_Provider.rootVisualElement.Q<Button>(k_ManageTargetButton);
                if (manageTargetButton != null)
                {
                    manageTargetButton.clicked += () =>
                    {
                        ServicesConfiguration.instance.RequestCurrentCloudBuildProjectTargetUrl(currentCloudBuildProjectTargetUrl =>
                        {
                            Application.OpenURL(currentCloudBuildProjectTargetUrl);
                        });
                    };
                    manageTargetButton.style.display = DisplayStyle.None;
                }
                var addTargetButton = m_Provider.rootVisualElement.Q<Button>(k_AddTargetButton);
                if (addTargetButton != null)
                {
                    addTargetButton.clicked += () =>
                    {
                        ServicesConfiguration.instance.RequestCloudBuildAddTargetUrl(cloudBuildAddTargetUrl =>
                        {
                            Application.OpenURL(cloudBuildAddTargetUrl);
                        });
                    };
                }
                m_Provider.UpdateServiceToggleAndDashboardLink(m_Provider.serviceInstance.IsServiceEnabled());

                ResetData();
                GetProjectInfo();

                m_Provider.HandlePermissionRestrictedControls();
            }

            void ResetData()
            {
                m_CloudBuildApiOrgProjectUrl = null;
                m_CloudBuildApiOrgProjectBillingPlanUrl = null;
                m_CloudBuildApiOrgProjectBuildTargetsUrl = null;
                m_BillingPlanLabel = string.Empty;
            }

            void GetProjectInfo()
            {
                ServicesConfiguration.instance.RequestCloudBuildApiCurrentProjectUrl(cloudBuildApiCurrentProjectUrl =>
                {
                    var getCurrentProjectRequest = new UnityWebRequest(cloudBuildApiCurrentProjectUrl,
                        UnityWebRequest.kHttpVerbGET) { downloadHandler = new DownloadHandlerBuffer() };
                    getCurrentProjectRequest.suppressErrorsToConsole = true;
                    getCurrentProjectRequest.SetRequestHeader("AUTHORIZATION", $"Bearer {UnityConnect.instance.GetUserInfo().accessToken}");
                    if (m_Provider.m_GetProjectRequest != null)
                    {
                        m_Provider.m_GetProjectRequest.Abort();
                        m_Provider.m_GetProjectRequest.Dispose();
                        m_Provider.m_GetProjectRequest = null;
                    }
                    m_Provider.m_GetProjectRequest = getCurrentProjectRequest;
                    var operation = getCurrentProjectRequest.SendWebRequest();
                    operation.completed += GetProjectInfoRequestOnCompleted;
                });
            }

            void GetProjectInfoRequestOnCompleted(AsyncOperation obj)
            {
                if (m_Provider.m_GetProjectRequest == null)
                {
                    //If we lost our request reference, we can't risk doing anything.
                    return;
                }

                try
                {
                    if (ServicesUtils.IsUnityWebRequestReadyForJsonExtract(m_Provider.m_GetProjectRequest))
                    {
                        try
                        {
                            var jsonParser = new JSONParser(m_Provider.m_GetProjectRequest.downloadHandler.text);
                            var json = jsonParser.Parse();
                            if (json.AsDict()[k_JsonNodeNameDisabled].AsBool())
                            {
                                NotificationManager.instance.Publish(
                                    Notification.Topic.BuildService,
                                    Notification.Severity.Error,
                                    L10n.Tr(k_MessageProjectStateMismatch));
                            }
                            else
                            {
                                var linksDict = json.AsDict()[k_JsonNodeNameLinks].AsDict();
                                ServicesConfiguration.instance.RequestCloudBuildApiUrl(cloudBuildApiUrl =>
                                {
                                    m_CloudBuildApiOrgProjectUrl = cloudBuildApiUrl + linksDict[k_JsonNodeNameSelf].AsDict()[k_JsonNodeNameHref].AsString();
                                    m_CloudBuildApiOrgProjectBillingPlanUrl = m_CloudBuildApiOrgProjectUrl + k_UrlSuffixBillingPlan;
                                    m_CloudBuildApiOrgProjectBuildTargetsUrl = cloudBuildApiUrl + linksDict[k_JsonNodeNameListBuildTargets].AsDict()[k_JsonNodeNameHref].AsString();
                                    m_CloudBuildApiOrgLatestBuilds = cloudBuildApiUrl + linksDict[k_JsonNodeNameLatestBuilds].AsDict()[k_JsonNodeNameHref].AsString();
                                    m_CloudBuildApiOrgLatestBuilds = m_CloudBuildApiOrgLatestBuilds.Remove(m_CloudBuildApiOrgLatestBuilds.Length - 1, 1) + k_NumberOfBuildsToQuery;

                                    GetProjectBillingPlan();
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            NotificationManager.instance.Publish(
                                Notification.Topic.BuildService,
                                Notification.Severity.Error,
                                L10n.Tr(k_MessageErrorForProjectData));
                            Debug.LogException(ex);
                        }
                    }
                }
                finally
                {
                    m_Provider.m_GetProjectRequest.Dispose();
                    m_Provider.m_GetProjectRequest = null;
                }
            }

            void GetProjectBillingPlan()
            {
                var getCurrentProjectBillingPlanRequest = new UnityWebRequest(m_CloudBuildApiOrgProjectBillingPlanUrl,
                    UnityWebRequest.kHttpVerbGET) { downloadHandler = new DownloadHandlerBuffer() };
                getCurrentProjectBillingPlanRequest.suppressErrorsToConsole = true;
                getCurrentProjectBillingPlanRequest.SetRequestHeader("AUTHORIZATION", $"Bearer {UnityConnect.instance.GetUserInfo().accessToken}");
                if (m_Provider.m_GetProjectBillingPlanRequest != null)
                {
                    m_Provider.m_GetProjectBillingPlanRequest.Abort();
                    m_Provider.m_GetProjectBillingPlanRequest.Dispose();
                    m_Provider.m_GetProjectBillingPlanRequest = null;
                }
                m_Provider.m_GetProjectBillingPlanRequest = getCurrentProjectBillingPlanRequest;
                var operation = getCurrentProjectBillingPlanRequest.SendWebRequest();
                operation.completed += GetProjectBillingPlanRequestOnCompleted;
            }

            void GetProjectBillingPlanRequestOnCompleted(AsyncOperation obj)
            {
                if (m_Provider.m_GetProjectBillingPlanRequest == null)
                {
                    //If we lost our request reference, we can't risk doing anything.
                    return;
                }

                try
                {
                    if (ServicesUtils.IsUnityWebRequestReadyForJsonExtract(m_Provider.m_GetProjectBillingPlanRequest))
                    {
                        try
                        {
                            var jsonParser = new JSONParser(m_Provider.m_GetProjectBillingPlanRequest.downloadHandler.text);
                            var json = jsonParser.Parse();
                            m_BillingPlanLabel = json.AsDict()[k_JsonNodeNameEffective].AsDict()[k_JsonNodeNameLabel].AsString();
                            GetProjectBuildTargets();
                            GetApiStatus();
                        }
                        catch (Exception ex)
                        {
                            NotificationManager.instance.Publish(
                                Notification.Topic.BuildService,
                                Notification.Severity.Error,
                                L10n.Tr(k_MessageErrorForProjectTeamData));
                            Debug.LogException(ex);
                        }
                    }
                }
                finally
                {
                    m_Provider.m_GetProjectBillingPlanRequest.Dispose();
                    m_Provider.m_GetProjectBillingPlanRequest = null;
                }
            }

            void GetProjectBuildTargets()
            {
                var getCurrentProjectBuildTargetsRequest = new UnityWebRequest(m_CloudBuildApiOrgProjectBuildTargetsUrl,
                    UnityWebRequest.kHttpVerbGET) { downloadHandler = new DownloadHandlerBuffer() };
                getCurrentProjectBuildTargetsRequest.suppressErrorsToConsole = true;
                getCurrentProjectBuildTargetsRequest.SetRequestHeader("AUTHORIZATION", $"Bearer {UnityConnect.instance.GetUserInfo().accessToken}");
                if (m_Provider.m_GetProjectBuildTargetsRequest != null)
                {
                    m_Provider.m_GetProjectBuildTargetsRequest.Abort();
                    m_Provider.m_GetProjectBuildTargetsRequest.Dispose();
                    m_Provider.m_GetProjectBuildTargetsRequest = null;
                }
                m_Provider.m_GetProjectBuildTargetsRequest = getCurrentProjectBuildTargetsRequest;
                var operation = getCurrentProjectBuildTargetsRequest.SendWebRequest();
                operation.completed += GetBuildTargetsRequestOnCompleted;
            }

            void GetBuildTargetsRequestOnCompleted(AsyncOperation obj)
            {
                if (m_Provider.m_GetProjectBuildTargetsRequest == null)
                {
                    //If we lost our request reference, we can't risk doing anything.
                    return;
                }

                try
                {
                    if (ServicesUtils.IsUnityWebRequestReadyForJsonExtract(m_Provider.m_GetProjectBuildTargetsRequest))
                    {
                        try
                        {
                            var jsonParser = new JSONParser(m_Provider.m_GetProjectBuildTargetsRequest.downloadHandler.text);
                            var json = jsonParser.Parse();
                            var buildEntryList = json.AsList();
                            if (buildEntryList.Count <= 0)
                            {
                                CloudBuildPoller.instance.Disable();
                            }
                            else
                            {
                                m_Provider.rootVisualElement.Q(k_NoTargetContainer).style.display = DisplayStyle.None;
                                m_Provider.rootVisualElement.Q(k_AddTargetButton).style.display = DisplayStyle.None;
                                m_Provider.rootVisualElement.Q(k_ManageTargetButton).style.display = DisplayStyle.Flex;
                                m_Provider.rootVisualElement.Q(k_PollFooterName).style.display = DisplayStyle.Flex;
                                m_Provider.rootVisualElement.Q(k_PollFooterSectionName).style.display = DisplayStyle.Flex;

                                var pollerToggle = m_Provider.rootVisualElement.Q<Toggle>(k_PollToggleName);
                                if (!CloudBuildPoller.instance.enabledOnce)
                                {
                                    CloudBuildPoller.instance.Enable(m_CloudBuildApiOrgLatestBuilds);
                                }
                                pollerToggle.SetValueWithoutNotify(CloudBuildPoller.instance.enabled);
                                pollerToggle.RegisterValueChangedCallback(evt =>
                                {
                                    if (evt.newValue)
                                    {
                                        CloudBuildPoller.instance.Enable(m_CloudBuildApiOrgLatestBuilds);
                                    }
                                    else
                                    {
                                        CloudBuildPoller.instance.Disable();
                                    }
                                });

                                m_Provider.rootVisualElement.Q<TextElement>(className: k_ServiceTargetContainerTitleClassName).text = L10n.Tr(k_LabelConfiguredTargets);
                                var targetsContainer = m_Provider.rootVisualElement.Q(className: k_ServiceTargetContainerClassName);
                                foreach (var jsonBuildEntry in buildEntryList)
                                {
                                    var buildEntry = jsonBuildEntry.AsDict();
                                    AddBuildTarget(targetsContainer, buildEntry);
                                }
                            }
                            m_Provider.rootVisualElement.Q(className: k_ServiceCloudBuildContainerClassName).style.display = DisplayStyle.Flex;
                            m_Provider.rootVisualElement.Q(className: k_ServiceCloudProgressClassName).style.display = DisplayStyle.None;
                        }
                        catch (Exception ex)
                        {
                            NotificationManager.instance.Publish(
                                Notification.Topic.BuildService,
                                Notification.Severity.Error,
                                L10n.Tr(k_MessageErrorForProjectBuildTargetsData));
                            Debug.LogException(ex);
                        }
                    }
                }
                finally
                {
                    m_Provider.m_GetProjectBuildTargetsRequest.Dispose();
                    m_Provider.m_GetProjectBuildTargetsRequest = null;
                }
            }

            void AddBuildTarget(VisualElement targetsContainer, Dictionary<string, JSONValue> buildEntry)
            {
                if (buildEntry[k_JsonNodeNameEnabled].AsBool())
                {
                    ServicesConfiguration.instance.RequestCloudBuildApiUrl(cloudBuildApiUrl =>
                    {
                        var buildTargetName = buildEntry[k_JsonNodeNameName].AsString();
                        var buildTargetId = buildEntry[k_JsonNodeNameBuildTargetId].AsString();
                        var buildTargetUrls = buildEntry[k_JsonNodeNameLinks].AsDict();
                        var startBuildUrl = cloudBuildApiUrl + buildTargetUrls[k_JsonNodeNameStartBuilds].AsDict()[k_JsonNodeNameHref].AsString();

                        var targetContainer = new VisualElement();
                        targetContainer.AddToClassList(k_ClassNameTargetEntry);
                        var buildNameTextElement = new TextElement();
                        buildNameTextElement.AddToClassList(k_ClassNameTitle);
                        buildNameTextElement.text = buildTargetName;
                        targetContainer.Add(buildNameTextElement);
                        var buildButton = new Button();
                        buildButton.name = k_BuildButtonNamePrefix + buildTargetId;
                        buildButton.AddToClassList(k_ClassNameBuildButton);
                        if (m_BillingPlanLabel.ToLower() == k_SubscriptionPersonal
                            || k_SubscriptionTeamsBasic.ToLower() == k_SubscriptionPersonal)
                        {
                            buildButton.SetEnabled(false);
                        }
                        buildButton.text = L10n.Tr(k_LabelBuildButton);
                        buildButton.clicked += () =>
                        {
                            var uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(k_LaunchBuildPayload));
                            var launchBuildPostRequest = new UnityWebRequest(startBuildUrl,
                                UnityWebRequest.kHttpVerbPOST) { downloadHandler = new DownloadHandlerBuffer(), uploadHandler = uploadHandler };
                            launchBuildPostRequest.suppressErrorsToConsole = true;
                            launchBuildPostRequest.SetRequestHeader("AUTHORIZATION", $"Bearer {UnityConnect.instance.GetUserInfo().accessToken}");
                            launchBuildPostRequest.SetRequestHeader("Content-Type", "application/json;charset=utf-8");
                            m_Provider.m_BuildRequests.Add(launchBuildPostRequest);
                            var launchingMessage = string.Format(L10n.Tr(k_MessageLaunchingBuild), buildTargetName);

                            Debug.Log(launchingMessage);
                            NotificationManager.instance.Publish(
                                Notification.Topic.BuildService,
                                Notification.Severity.Info,
                                launchingMessage);

                            EditorAnalytics.SendLaunchCloudBuildEvent(new BuildPostInfo() { targetName = buildTargetName });

                            var operation = launchBuildPostRequest.SendWebRequest();
                            operation.completed += asyncOperation =>
                            {
                                try
                                {
                                    if (ServicesUtils.IsUnityWebRequestReadyForJsonExtract(launchBuildPostRequest))
                                    {
                                        try
                                        {
                                            if (launchBuildPostRequest.responseCode == k_HttpResponseCodeAccepted)
                                            {
                                                var jsonLaunchedBuildParser = new JSONParser(launchBuildPostRequest.downloadHandler.text);
                                                var launchedBuildJson = jsonLaunchedBuildParser.Parse();
                                                var launchedBuilds = launchedBuildJson.AsList();

                                                foreach (var rawLaunchedBuild in launchedBuilds)
                                                {
                                                    var launchedBuild = rawLaunchedBuild.AsDict();
                                                    if (launchedBuild.ContainsKey(k_JsonNodeNameBuild))
                                                    {
                                                        var buildNumber = launchedBuild[k_JsonNodeNameBuild].AsFloat().ToString();
                                                        var message = string.Format(L10n.Tr(k_MessageLaunchedBuildSuccess), buildNumber, buildTargetName);
                                                        Debug.Log(message);
                                                        NotificationManager.instance.Publish(
                                                            Notification.Topic.BuildService,
                                                            Notification.Severity.Info,
                                                            message);
                                                    }
                                                    else if (launchedBuild.ContainsKey(k_JsonNodeNameError))
                                                    {
                                                        var message = string.Format(L10n.Tr(k_MessageLaunchedBuildFailedWithMsg), buildTargetName, launchedBuild[k_JsonNodeNameError].ToString());
                                                        Debug.LogError(message);
                                                        NotificationManager.instance.Publish(
                                                            Notification.Topic.BuildService,
                                                            Notification.Severity.Error,
                                                            message);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                var message = L10n.Tr(k_MessageLaunchedBuildFailure);
                                                Debug.LogError(message);
                                                NotificationManager.instance.Publish(
                                                    Notification.Topic.BuildService,
                                                    Notification.Severity.Error,
                                                    message);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            NotificationManager.instance.Publish(
                                                Notification.Topic.BuildService,
                                                Notification.Severity.Error,
                                                L10n.Tr(k_MessageErrorForBuildLaunch));
                                            Debug.LogException(ex);
                                        }
                                    }
                                }
                                finally
                                {
                                    m_Provider.m_BuildRequests.Remove(launchBuildPostRequest);
                                    launchBuildPostRequest.Dispose();
                                    launchBuildPostRequest = null;
                                }
                            };
                        };
                        targetContainer.Add(buildButton);
                        targetsContainer.Add(targetContainer);

                        var separator = new VisualElement();
                        separator.AddToClassList(k_ClassNameSeparator);
                        targetsContainer.Add(separator);
                    });
                }
            }

            void GetApiStatus()
            {
                ServicesConfiguration.instance.RequestCloudBuildApiStatusUrl(cloudBuildApiStatusUrl =>
                {
                    var getCurrentProjectStatusRequest = new UnityWebRequest(cloudBuildApiStatusUrl,
                        UnityWebRequest.kHttpVerbGET) { downloadHandler = new DownloadHandlerBuffer() };
                    getCurrentProjectStatusRequest.suppressErrorsToConsole = true;
                    getCurrentProjectStatusRequest.SetRequestHeader("AUTHORIZATION", $"Bearer {UnityConnect.instance.GetUserInfo().accessToken}");
                    if (m_Provider.m_GetApiStatusRequest != null)
                    {
                        m_Provider.m_GetApiStatusRequest.Abort();
                        m_Provider.m_GetApiStatusRequest.Dispose();
                        m_Provider.m_GetApiStatusRequest = null;
                    }
                    m_Provider.m_GetApiStatusRequest = getCurrentProjectStatusRequest;
                    var operation = getCurrentProjectStatusRequest.SendWebRequest();
                    operation.completed += GetApiStatusRequestOnCompleted;
                });
            }

            void GetApiStatusRequestOnCompleted(AsyncOperation obj)
            {
                if (m_Provider.m_GetApiStatusRequest == null)
                {
                    //If we lost our request reference, we can't risk doing anything.
                    return;
                }

                try
                {
                    if (ServicesUtils.IsUnityWebRequestReadyForJsonExtract(m_Provider.m_GetApiStatusRequest))
                    {
                        try
                        {
                            var jsonParser = new JSONParser(m_Provider.m_GetApiStatusRequest.downloadHandler.text);
                            var json = jsonParser.Parse();
                            var notificationEntryList = json.AsList();
                            if (notificationEntryList.Count > 0)
                            {
                                foreach (var jsonNotificationEntry in notificationEntryList)
                                {
                                    var notificationEntry = jsonNotificationEntry.AsDict();
                                    var notificationText = notificationEntry[k_JsonNodeNameText].AsString();
                                    var billingPlan = string.Empty;
                                    if (notificationEntry.ContainsKey(k_JsonNodeNameBillingPlan))
                                    {
                                        billingPlan = notificationEntry[k_JsonNodeNameBillingPlan].AsString();
                                    }

                                    var notificationAlertType = notificationEntry[k_JsonNodeNameAlertType].AsString().ToLower();

                                    if (string.IsNullOrEmpty(billingPlan) || m_BillingPlanLabel.ToLower().Equals(billingPlan.ToLower()))
                                    {
                                        var severity = Notification.Severity.Error;
                                        if (notificationAlertType.Equals(Notification.Severity.Warning.ToString().ToLower()))
                                        {
                                            severity = Notification.Severity.Warning;
                                        }
                                        else if (notificationAlertType.Equals(Notification.Severity.Info.ToString().ToLower()))
                                        {
                                            severity = Notification.Severity.Info;
                                        }

                                        NotificationManager.instance.Publish(Notification.Topic.BuildService, severity, notificationText);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            NotificationManager.instance.Publish(
                                Notification.Topic.BuildService,
                                Notification.Severity.Error,
                                L10n.Tr(k_MessageErrorForApiStatusData));
                            Debug.LogException(ex);
                        }
                    }
                }
                finally
                {
                    m_Provider.m_GetApiStatusRequest.Dispose();
                    m_Provider.m_GetApiStatusRequest = null;
                }
            }

            SimpleStateMachine<CloudBuildEvent>.State HandleUnbinding(CloudBuildEvent raisedEvent)
            {
                return stateMachine.GetStateByName(k_StateNameDisabled);
            }
        }
    }
}
