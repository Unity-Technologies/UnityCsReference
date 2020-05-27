// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEngine.Networking;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;

using Button = UnityEngine.UIElements.Button;
using Debug = UnityEngine.Debug;

namespace UnityEditor.Connect
{
    /// <summary>
    /// A settings provider specifically designed for Cloud Services.
    /// Sets up NotificationManager by default, allows usage of RootVisualElement and has a default activateHandler
    /// to set it all up.  Will call ActivateAction when all the initial setup is done within the activateHandler.
    /// Keep in mind that each time ActivateAction is called, the UIElements window is brand new.
    /// Do not cache things related to UIElement state in your extension.
    /// </summary>
    internal abstract class ServicesProjectSettings : SettingsProvider
    {
        internal struct OpenDashboardForService
        {
            public string serviceName;
            public string url;
            public string organizationId;
            public string projectId;
        }
        internal struct ImportPackageInfo
        {
            public string packageName;
            public string version;
        }
        internal struct OpenPackageManager
        {
            public string packageName;
        }


        protected abstract string serviceUssClassName { get; }
        protected VisualElement rootVisualElement { get; private set; }

        protected INotificationSubscriber m_NotificationSubscriber;

        protected abstract Notification.Topic[] notificationTopicsToSubscribe { get; }

        protected abstract SingleService serviceInstance { get; }

        protected virtual bool sendNotificationForNonStandardStates => true;

        const string k_LoggedOutMessage = "You are not currently logged in. To enable this service, please login.";
        const string k_UnboundMessage = "Project does not have a Unity project ID. To enable this service, go to Project Settings, Services and create a new project Id or link to an existing one.";
        const string k_ProjectBindingBrokenMessage = "Unity Project ID no longer exists or you no longer have permission to access the Unity Project ID. " +
            "To enable this service, please create a new project ID or link to an existing project.";
        const string k_CoppaRequiredMessage = "COPPA compliance wasn't defined for this project. To enable this service, go to Project Settings, Services and configure COPPA compliance.";
        const string k_CoppaPermissionMessage = "You do not have sufficient permissions to set COPPA Compliance.";
        const string k_UnknownRoleMessage = "Unknown role: {0}";

        const string k_JsonUsersNodeName = "users";
        const string k_JsonUserIdNodeName = "foreign_key";
        const string k_JsonRoleNodeName = "role";

        protected const string k_ClassNameEditMode = "edit-mode";

        const string k_ToggleOnLabel = "ON";
        const string k_ToggleOffLabel = "OFF";
        protected const string k_ServiceToggleContainerClassName = "service-toggle-container";
        protected const string k_UnityToggleClassName = "unity-toggle";
        static string s_ToggleOnLabelTranslated;
        static string s_ToggleOffLabelTranslated;

        protected static UserRole currentUserPermission { get; private set; }

        SimpleStateMachine<Event> m_StateMachine;
        OfflineState m_OfflineState;
        ConnectingServicesState m_ConnectingServicesState;
        UnboundState m_UnboundState;
        InitialState m_InitialState;
        StandardState m_StandardState;
        BrokenBindingState m_BrokenBindingState;
        CoppaState m_CoppaState;
        LoggedOutState m_LoggedOutState;
        DeactivateState m_DeactivateState;

        const Notification.Topic k_NonStandardStateTopic = Notification.Topic.ProjectBind;
        const Notification.Severity k_NonStandardStateSeverity = Notification.Severity.Warning;

        const string k_StateNameInitial = "InitialState";
        const string k_StateNameStandard = "StandardState";
        const string k_StateNameUnbound = "UnboundState";
        const string k_StateNameCoppa = "CoppaState";
        const string k_StateNameBrokenBinding = "BrokenBindingState";
        const string k_StateNameOffline = "OfflineState";
        const string k_StateNameConnectingServices = "ConnectingServicesState";
        const string k_StateNameLoggedOut = "LoggedOutState";
        const string k_StateNameDeactivate = "DeactivateState";

        const string k_ScrollContainerClassName = "scroll-container";

        const string k_GeneralServicesTemplatePath = "UXML/ServicesWindow/GeneralProjectSettings.uxml";
        protected VisualTreeAsset m_GeneralTemplate;

        internal struct ShowServiceState
        {
            public string service;
            public string page;
            public string referrer;
        }

        String m_SearchContext;
        // In order to minimize the number of calls to ActivateAction / DeactivateAction,
        // we need to only achieve those if the project info really changed, thus keeping a cached copy...
        ProjectInfo m_CachedProjectInfo;
        bool m_CachedLoggedInState;
        bool m_CachedOnlineState;
        bool m_UnityConnectStateChanged;

        void KeepCacheInfo()
        {
            m_CachedProjectInfo = UnityConnect.instance.projectInfo;
            m_CachedLoggedInState = UnityConnect.instance.loggedIn;
            m_CachedOnlineState = UnityConnect.instance.online;
        }

        string GetServiceInstanceName()
        {
            return (serviceInstance != null) ? serviceInstance.name : "General";
        }

        protected ServicesProjectSettings(string path, SettingsScope scopes, string label, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {
            m_StateMachine = new SimpleStateMachine<Event>();
            m_StateMachine.AddEvent(Event.Initializing);
            m_StateMachine.AddEvent(Event.Unbinding);
            m_StateMachine.AddEvent(Event.GoingToStandard);
            m_StateMachine.AddEvent(Event.BreakBinding);
            m_StateMachine.AddEvent(Event.SettingCoppa);
            m_StateMachine.AddEvent(Event.GoingToOffline);
            m_StateMachine.AddEvent(Event.GoingToLoggedOut);
            m_StateMachine.AddEvent(Event.GoingToConnectingServices);
            m_StateMachine.AddEvent(Event.GoingToDeactivate);
            m_UnboundState = new UnboundState(m_StateMachine, this);
            m_StateMachine.AddState(m_UnboundState);
            m_InitialState = new InitialState(m_StateMachine, this);
            m_StateMachine.AddState(m_InitialState);
            m_StandardState = new StandardState(m_StateMachine, this);
            m_StateMachine.AddState(m_StandardState);
            m_BrokenBindingState = new BrokenBindingState(m_StateMachine, this);
            m_StateMachine.AddState(m_BrokenBindingState);
            m_CoppaState = new CoppaState(m_StateMachine, this);
            m_StateMachine.AddState(m_CoppaState);
            m_OfflineState = new OfflineState(m_StateMachine, this);
            m_StateMachine.AddState(m_OfflineState);
            m_ConnectingServicesState = new ConnectingServicesState(m_StateMachine, this);
            m_StateMachine.AddState(m_ConnectingServicesState);
            m_LoggedOutState = new LoggedOutState(m_StateMachine, this);
            m_StateMachine.AddState(m_LoggedOutState);
            m_DeactivateState = new DeactivateState(m_StateMachine, this);
            m_StateMachine.AddState(m_DeactivateState);

            activateHandler = (s, element) =>
            {
                // Take a cache copy of the project info...
                KeepCacheInfo();

                // Create a child to make sure all the style sheets are not added to the root.
                rootVisualElement = new ScrollView();
                rootVisualElement.AddToClassList(serviceUssClassName);
                rootVisualElement.AddStyleSheetPath(ServicesUtils.StylesheetPath.servicesWindowCommon);
                rootVisualElement.AddStyleSheetPath(EditorGUIUtility.isProSkin ? ServicesUtils.StylesheetPath.servicesWindowDark : ServicesUtils.StylesheetPath.servicesWindowLight);
                rootVisualElement.AddStyleSheetPath(ServicesUtils.StylesheetPath.servicesCommon);
                rootVisualElement.AddStyleSheetPath(EditorGUIUtility.isProSkin ? ServicesUtils.StylesheetPath.servicesDark : ServicesUtils.StylesheetPath.servicesLight);

                element.Add(rootVisualElement);

                m_SearchContext = s;
                this.label = label;
                m_GeneralTemplate = EditorGUIUtility.Load(k_GeneralServicesTemplatePath) as VisualTreeAsset;

                UnityConnect.instance.ProjectStateChanged += OnRefreshRequired;
                UnityConnect.instance.ProjectRefreshed += OnRefreshRequired;

                ReinitializeSettings();

                UnityConnect.instance.RefreshProject();

                if (settingsWindow.GetCurrentProvider() != this)
                {
                    EditorAnalytics.SendEventShowService(new ShowServiceState() {
                        service = GetServiceInstanceName(),
                        page = "",
                        referrer = "show_service_method"
                    });
                }
            };
            deactivateHandler = () =>
            {
                UnityConnect.instance.ProjectStateChanged -= OnRefreshRequired;
                UnityConnect.instance.ProjectRefreshed -= OnRefreshRequired;

                // Make sure to call the deactivate if needed...
                var stateMachine = GetStateMachine();
                stateMachine?.ProcessEvent(Event.GoingToDeactivate);

                CleanupNotificationSubscriber();

                // Make sure to invalidate the actual root element for this provider since this is not supposed to be the one active
                rootVisualElement = null;
            };
        }

        bool IsProjectInfoChanged()
        {
            // All struct fields are private, we check the public interface...
            // Doing something like: return (m_CachedProjectInfo != UnityConnect.instance.projectInfo); DOES NOT work.
            // Also, we do not consider "projectInfo.valid" as a property to identify if the info changed.
            return
                (m_CachedProjectInfo.buildAllowed != UnityConnect.instance.projectInfo.buildAllowed) ||
                (m_CachedProjectInfo.coppaLock != UnityConnect.instance.projectInfo.coppaLock) ||
                (m_CachedProjectInfo.moveLock != UnityConnect.instance.projectInfo.moveLock) ||
                (m_CachedProjectInfo.projectBound != UnityConnect.instance.projectInfo.projectBound) ||
                (m_CachedProjectInfo.organizationId != UnityConnect.instance.projectInfo.organizationId) ||
                (m_CachedProjectInfo.organizationName != UnityConnect.instance.projectInfo.organizationName) ||
                (m_CachedProjectInfo.organizationForeignKey != UnityConnect.instance.projectInfo.organizationForeignKey) ||
                (m_CachedProjectInfo.projectName != UnityConnect.instance.projectInfo.projectName) ||
                (m_CachedProjectInfo.projectGUID != UnityConnect.instance.projectInfo.projectGUID) ||
                (m_CachedProjectInfo.COPPA != UnityConnect.instance.projectInfo.COPPA);
        }

        SimpleStateMachine<Event> GetStateMachine()
        {
            return m_StateMachine;
        }

        internal void ReinitializeSettings()
        {
            var stateMachine = GetStateMachine();
            if (stateMachine != null)
            {
                if (stateMachine.currentState == null)
                {
                    stateMachine.Initialize(m_InitialState);
                }
                else
                {
                    stateMachine.ProcessEvent(Event.Initializing);
                }
            }
        }

        protected static void SetupServiceToggleLabel(Toggle toggle, bool active)
        {
            if (string.IsNullOrEmpty(s_ToggleOnLabelTranslated)
                || string.IsNullOrEmpty(s_ToggleOffLabelTranslated))
            {
                s_ToggleOnLabelTranslated = L10n.Tr(k_ToggleOnLabel);
                s_ToggleOffLabelTranslated = L10n.Tr(k_ToggleOffLabel);
            }

            toggle.text = active ? s_ToggleOnLabelTranslated : s_ToggleOffLabelTranslated;
        }

        void OnRefreshRequired(ProjectInfo state)
        {
            // The check to reinitialize was not complete, we also have to take care of the online/loggedin states
            // If those states changed when the state of the project info was not valid, we have to take note of that so that the reinitialize is eventually called
            m_UnityConnectStateChanged = m_UnityConnectStateChanged || (m_CachedOnlineState != UnityConnect.instance.online) || (m_CachedLoggedInState != UnityConnect.instance.loggedIn);

            // Before reinitializing, we check that the actual state of the project info is valid and
            //     that it has changed since the ActivateHandler.
            if (state.valid && (IsProjectInfoChanged() || m_UnityConnectStateChanged))
            {
                DismissWarningNotifications();
                ReinitializeSettings();
            }
            m_CachedOnlineState = UnityConnect.instance.online;
            m_CachedLoggedInState = UnityConnect.instance.loggedIn;

            // Must make sure the state changed indicator comes back to false after any valid state...
            if (state.valid)
            {
                m_UnityConnectStateChanged = false;
            }
        }

        void ConfigureNotificationSubscriberForNonStandardStates()
        {
            m_NotificationSubscriber = new UIElementsNotificationSubscriber(rootVisualElement);
            var topics = notificationTopicsToSubscribe;
            if (!topics.Contains(Notification.Topic.ProjectBind))
            {
                topics = new Notification.Topic[notificationTopicsToSubscribe.Length + 1];
                for (var i = 0; i < notificationTopicsToSubscribe.Length; i++)
                {
                    topics[i] = notificationTopicsToSubscribe[i];
                }

                topics[notificationTopicsToSubscribe.Length] = Notification.Topic.ProjectBind;
            }
            var notifications = NotificationManager.instance.Subscribe(m_NotificationSubscriber, topics);
            foreach (var notification in notifications)
            {
                m_NotificationSubscriber.ReceiveNotification(notification);
            }
        }

        protected virtual INotificationSubscriber ConfigureNotificationSubscriber()
        {
            var defaultNotificationSubscriber = new UIElementsNotificationSubscriber(rootVisualElement);
            var notifications = NotificationManager.instance.Subscribe(defaultNotificationSubscriber, notificationTopicsToSubscribe);
            foreach (var notification in notifications)
            {
                defaultNotificationSubscriber.ReceiveNotification(notification);
            }
            return defaultNotificationSubscriber;
        }

        void PublishConfigWarningNotification(string message)
        {
            if (sendNotificationForNonStandardStates)
            {
                NotificationManager.instance.Publish(k_NonStandardStateTopic, k_NonStandardStateSeverity, message);
            }
        }

        static void DismissWarningNotifications()
        {
            var dismissLogNotification = UnityConnect.instance.loggedIn;
            var dismissBrokenBindNotification = UnityConnect.instance.projectInfo.projectBound
                || string.IsNullOrEmpty(UnityConnect.instance.projectInfo.projectGUID);
            var dismissUnboundNotification = UnityConnect.instance.projectInfo.projectBound;
            var dismissCoppaNotification = UnityConnect.instance.projectInfo.COPPA != COPPACompliance.COPPAUndefined;

            var notifications = NotificationManager.instance.GetNotificationsForTopics(Notification.Topic.ProjectBind);
            foreach (var notification in notifications)
            {
                if (notification.severity == k_NonStandardStateSeverity && notification.topic == k_NonStandardStateTopic)
                {
                    if ((notification.rawMessage == k_LoggedOutMessage && dismissLogNotification)
                        || (notification.rawMessage == k_UnboundMessage && dismissUnboundNotification)
                        || (notification.rawMessage == k_ProjectBindingBrokenMessage && dismissBrokenBindNotification)
                        || (notification.rawMessage == k_CoppaRequiredMessage && dismissCoppaNotification))
                    {
                        NotificationManager.instance.Dismiss(notification.id);
                    }
                }
            }
        }

        void CleanupNotificationSubscriber()
        {
            if (m_NotificationSubscriber != null)
            {
                NotificationManager.instance.UnsubscribeFromAllTopics(m_NotificationSubscriber);
                m_NotificationSubscriber = null;
            }
        }

        protected void HandlePermissionRestrictedControls()
        {
            RefreshCurrentUserRole();
        }

        protected void OpenDashboardForOrgKeyAndProjectGuid(string dashboardUrl)
        {
            var orgForeignKey = UnityConnect.instance.GetOrganizationForeignKey();
            var projectGuid = UnityConnect.instance.projectInfo.projectGUID;

            EditorAnalytics.SendOpenDashboardForService(new OpenDashboardForService()
            {
                serviceName = GetServiceInstanceName(),
                url = dashboardUrl,
                organizationId = orgForeignKey,
                projectId = projectGuid
            });
            Application.OpenURL(String.Format(dashboardUrl, orgForeignKey, projectGuid));
        }

        protected void OpenDashboardForProjectGuid(string dashboardUrl)
        {
            var projectGuid = UnityConnect.instance.projectInfo.projectGUID;
            EditorAnalytics.SendOpenDashboardForService(new OpenDashboardForService()
            {
                serviceName = GetServiceInstanceName(),
                url = dashboardUrl,
                projectId = projectGuid
            });
            Application.OpenURL(String.Format(dashboardUrl, projectGuid));
        }

        protected void OpenDashboardOrgAndProjectIds(string dashboardUrl)
        {
            var orgSubUrl = UnityConnect.instance.projectInfo.organizationId;
            var projectSubUrl = UnityConnect.instance.projectInfo.projectId;
            EditorAnalytics.SendOpenDashboardForService(new OpenDashboardForService()
            {
                serviceName = GetServiceInstanceName(),
                url = dashboardUrl,
                organizationId = orgSubUrl,
                projectId = projectSubUrl
            });
            Application.OpenURL(String.Format(dashboardUrl, orgSubUrl, projectSubUrl));
        }

        void RefreshCurrentUserRole()
        {
            ServicesConfiguration.instance.RequestCurrentProjectUsersApiUrl(currentProjectUsersApiUrl =>
            {
                var getProjectUsersRequest = new UnityWebRequest(currentProjectUsersApiUrl,
                    UnityWebRequest.kHttpVerbGET) { downloadHandler = new DownloadHandlerBuffer() };
                getProjectUsersRequest.suppressErrorsToConsole = true;
                getProjectUsersRequest.SetRequestHeader("AUTHORIZATION", $"Bearer {UnityConnect.instance.GetUserInfo().accessToken}");
                var operation = getProjectUsersRequest.SendWebRequest();
                operation.completed += op =>
                {
                    try
                    {
                        if (ServicesUtils.IsUnityWebRequestReadyForJsonExtract(getProjectUsersRequest))
                        {
                            var jsonParser = new JSONParser(getProjectUsersRequest.downloadHandler.text);
                            var json = jsonParser.Parse();
                            try
                            {
                                var currentUserId = UnityConnect.instance.userInfo.userId;
                                var users = json.AsDict()[k_JsonUsersNodeName].AsList();
                                foreach (var rawUser in users)
                                {
                                    var user = rawUser.AsDict();
                                    if (currentUserId.Equals(user[k_JsonUserIdNodeName].AsString()))
                                    {
                                        currentUserPermission = ConvertStringToUserRole(user[k_JsonRoleNodeName].AsString());
                                        InternalToggleRestrictedVisualElementsAvailability(currentUserPermission == UserRole.User);
                                        break;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.LogException(ex);
                            }
                        }
                    }
                    finally
                    {
                        getProjectUsersRequest.Dispose();
                        getProjectUsersRequest = null;
                    }
                };
            });
        }

        UserRole ConvertStringToUserRole(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                throw new ArgumentNullException(nameof(s));
            }
            if (s.ToUpper().Equals(UserRole.Manager.ToString().ToUpper()))
            {
                return UserRole.Manager;
            }
            else if (s.ToUpper().Equals(UserRole.Owner.ToString().ToUpper()))
            {
                return UserRole.Owner;
            }
            else if (s.ToUpper().Equals(UserRole.User.ToString().ToUpper()))
            {
                return UserRole.User;
            }
            else
            {
                throw new ArgumentException(string.Format(k_UnknownRoleMessage, s));
            }
        }

        void InternalToggleRestrictedVisualElementsAvailability(bool isUserAccess)
        {
            if (rootVisualElement == null)
            {
                return;
            }
            var coppaContainer = rootVisualElement.Q(CoppaManager.coppaContainerName);
            var editModeContainer = coppaContainer?.Q(className: k_ClassNameEditMode);
            if (editModeContainer != null)
            {
                editModeContainer.SetEnabled(!isUserAccess);
                if (isUserAccess)
                {
                    NotificationManager.instance.Publish(Notification.Topic.CoppaCompliance, Notification.Severity.Warning, k_CoppaPermissionMessage);
                }
            }
            ToggleRestrictedVisualElementsAvailability(!isUserAccess);
        }

        protected abstract void ToggleRestrictedVisualElementsAvailability(bool enable);

        /// <summary>
        /// This method is called by the activateHandler after rootVisualElement setups is done,
        /// but before NotificationManager setup is done. Load the UXML within ActivateAction.
        /// If you need to setup specific things for Notification Subscriber, override ConfigureNotificationSubscriber()
        /// Keep in mind that each time ActivateAction is called, the UIElements window is brand new.
        /// Do not cache things related to UIElement state in your extension.
        /// </summary>
        /// <param name="searchContext">The search context of the Project Settings window</param>
        protected abstract void ActivateAction(string searchContext);

        /// <summary>
        /// Override and add additional behavior here if required
        /// </summary>
        protected abstract void DeactivateAction();

        enum Event
        {
            Initializing,
            SettingCoppa,
            Unbinding,
            BreakBinding,
            GoingToStandard,
            GoingToOffline,
            GoingToLoggedOut,
            GoingToConnectingServices,
            GoingToDeactivate,
        }

        class BaseState : SimpleStateMachine<Event>.State
        {
            ServicesProjectSettings m_Provider;
            public ServicesProjectSettings provider
            {
                get { return m_Provider; }
                set { m_Provider = value; }
            }

            protected BaseState(SimpleStateMachine<Event> simpleStateMachine, string name, ServicesProjectSettings provider)
                : base(name, simpleStateMachine)
            {
                m_Provider = provider;
            }
        }

        // Empty state when going to deactivation of the actual provider...
        // This is doing nothing.
        sealed class DeactivateState : BaseState
        {
            public DeactivateState(SimpleStateMachine<Event> simpleStateMachine, ServicesProjectSettings provider)
                : base(simpleStateMachine, k_StateNameDeactivate, provider)
            {
                ModifyActionForEvent(Event.Initializing, HandleInitializing);
            }

            SimpleStateMachine<Event>.State HandleInitializing(Event raisedEvent)
            {
                return provider.m_InitialState;
            }
        }

        sealed class InitialState : BaseState
        {
            public InitialState(SimpleStateMachine<Event> simpleStateMachine, ServicesProjectSettings provider)
                : base(simpleStateMachine, k_StateNameInitial, provider)
            {
                ModifyActionForEvent(Event.GoingToStandard, HandleGoingToStandard);
                ModifyActionForEvent(Event.SettingCoppa, HandleSettingCoppa);
                ModifyActionForEvent(Event.Unbinding, HandleUnbinding);
                ModifyActionForEvent(Event.BreakBinding, HandleBreakBinding);
                ModifyActionForEvent(Event.GoingToOffline, HandleGoingToOffline);
                ModifyActionForEvent(Event.GoingToLoggedOut, HandleGoingToLoggedOut);
                ModifyActionForEvent(Event.GoingToConnectingServices, HandleGoingToConnectingServices);
                ModifyActionForEvent(Event.GoingToDeactivate, HandleGoingToDeactivate);
            }

            public override void EnterState()
            {
                provider.rootVisualElement.Clear();
                provider.CleanupNotificationSubscriber();
                if (!UnityConnect.instance.online)
                {
                    stateMachine.ProcessEvent(Event.GoingToOffline);
                    return;
                }

                if (!ServicesConfiguration.instance.pathsReady)
                {
                    stateMachine.ProcessEvent(Event.GoingToConnectingServices);
                    return;
                }

                if (!UnityConnect.instance.loggedIn)
                {
                    provider.PublishConfigWarningNotification(k_LoggedOutMessage);

                    stateMachine.ProcessEvent(Event.GoingToLoggedOut);
                    return;
                }

                if (!UnityConnect.instance.projectInfo.projectBound)
                {
                    if (string.IsNullOrEmpty(UnityConnect.instance.projectInfo.projectGUID))
                    {
                        provider.PublishConfigWarningNotification(k_UnboundMessage);

                        stateMachine.ProcessEvent(Event.Unbinding);
                        return;
                    }

                    provider.PublishConfigWarningNotification(k_ProjectBindingBrokenMessage);
                    stateMachine.ProcessEvent(Event.BreakBinding);
                    return;
                }

                if (UnityConnect.instance.projectInfo.projectBound
                    && provider.serviceInstance != null
                    && provider.serviceInstance.requiresCoppaCompliance
                    && UnityConnect.instance.projectInfo.COPPA == COPPACompliance.COPPAUndefined)
                {
                    provider.PublishConfigWarningNotification(k_CoppaRequiredMessage);
                    stateMachine.ProcessEvent(Event.SettingCoppa);
                    return;
                }

                stateMachine.ProcessEvent(Event.GoingToStandard);
            }

            SimpleStateMachine<Event>.State HandleGoingToLoggedOut(Event arg)
            {
                return provider.m_LoggedOutState;
            }

            SimpleStateMachine<Event>.State HandleGoingToConnectingServices(Event arg)
            {
                return provider.m_ConnectingServicesState;
            }

            SimpleStateMachine<Event>.State HandleGoingToOffline(Event arg)
            {
                return provider.m_OfflineState;
            }

            SimpleStateMachine<Event>.State HandleUnbinding(Event raisedEvent)
            {
                return provider.m_UnboundState;
            }

            SimpleStateMachine<Event>.State HandleGoingToStandard(Event raisedEvent)
            {
                return provider.m_StandardState;
            }

            SimpleStateMachine<Event>.State HandleBreakBinding(Event raisedEvent)
            {
                return provider.m_BrokenBindingState;
            }

            SimpleStateMachine<Event>.State HandleSettingCoppa(Event raisedEvent)
            {
                return provider.m_CoppaState;
            }

            SimpleStateMachine<Event>.State HandleGoingToDeactivate(Event arg)
            {
                return provider.m_DeactivateState;
            }
        }

        sealed class StandardState : BaseState
        {
            public StandardState(SimpleStateMachine<Event> simpleStateMachine, ServicesProjectSettings provider)
                : base(simpleStateMachine, k_StateNameStandard, provider)
            {
                ModifyActionForEvent(Event.Initializing, HandleInitializing);
                ModifyActionForEvent(Event.GoingToDeactivate, HandleGoingToDeactivate);
            }

            public override void EnterState()
            {
                provider.rootVisualElement.Clear();
                provider.ActivateAction(provider.m_SearchContext);
                provider.m_NotificationSubscriber = provider.ConfigureNotificationSubscriber();
            }

            SimpleStateMachine<Event>.State HandleInitializing(Event raisedEvent)
            {
                provider.DeactivateAction();
                return provider.m_InitialState;
            }

            SimpleStateMachine<Event>.State HandleGoingToDeactivate(Event arg)
            {
                provider.DeactivateAction();
                return provider.m_DeactivateState;
            }
        }

        sealed class CoppaState : BaseState
        {
            CoppaManager m_CoppaManager;

            public CoppaState(SimpleStateMachine<Event> simpleStateMachine, ServicesProjectSettings provider)
                : base(simpleStateMachine, k_StateNameCoppa, provider)
            {
                ModifyActionForEvent(Event.Initializing, HandleInitializing);
                ModifyActionForEvent(Event.GoingToDeactivate, HandleGoingToDeactivate);
            }

            public override void EnterState()
            {
                provider.rootVisualElement.Clear();
                var contentContainer = provider.m_GeneralTemplate.CloneTree().contentContainer;
                ServicesUtils.TranslateStringsInTree(contentContainer);
                provider.rootVisualElement.Add(contentContainer);
                contentContainer.AddStyleSheetPath(ServicesUtils.StylesheetPath.servicesWindowCommon);
                contentContainer.AddStyleSheetPath(EditorGUIUtility.isProSkin ? ServicesUtils.StylesheetPath.servicesWindowDark : ServicesUtils.StylesheetPath.servicesWindowLight);
                contentContainer.AddStyleSheetPath(ServicesUtils.StylesheetPath.servicesCommon);
                contentContainer.AddStyleSheetPath(EditorGUIUtility.isProSkin ? ServicesUtils.StylesheetPath.servicesDark : ServicesUtils.StylesheetPath.servicesLight);
                var scrollContainer = provider.rootVisualElement.Q(null, k_ScrollContainerClassName);
                scrollContainer.Clear();
                provider.ConfigureNotificationSubscriberForNonStandardStates();

                m_CoppaManager = new CoppaManager(scrollContainer)
                {
                    saveButtonCallback = compliance =>
                    {
                        stateMachine.ProcessEvent(Event.Initializing);
                    },
                    exceptionCallback = (compliance, exception) =>
                    {
                        NotificationManager.instance.Publish(Notification.Topic.CoppaCompliance, Notification.Severity.Error,
                            L10n.Tr(exception.Message));
                    }
                };
                var coppaContainer = scrollContainer.Q(CoppaManager.coppaContainerName);
                var editModeContainer = coppaContainer?.Q(className: k_ClassNameEditMode);
                if (editModeContainer != null)
                {
                    editModeContainer.SetEnabled(false);
                }

                provider.RefreshCurrentUserRole();
            }

            SimpleStateMachine<Event>.State HandleInitializing(Event raisedEvent)
            {
                return provider.m_InitialState;
            }

            SimpleStateMachine<Event>.State HandleGoingToDeactivate(Event arg)
            {
                return provider.m_DeactivateState;
            }
        }

        sealed class UnboundState : BaseState
        {
            ProjectBindManager m_ProjectBindManager;

            public UnboundState(SimpleStateMachine<Event> simpleStateMachine, ServicesProjectSettings provider)
                : base(simpleStateMachine, k_StateNameUnbound, provider)
            {
                ModifyActionForEvent(Event.Initializing, HandleInitializing);
                ModifyActionForEvent(Event.GoingToDeactivate, HandleGoingToDeactivate);
            }

            public override void EnterState()
            {
                provider.rootVisualElement.Clear();
                var contentContainer = provider.m_GeneralTemplate.CloneTree().contentContainer;
                ServicesUtils.TranslateStringsInTree(contentContainer);
                provider.rootVisualElement.Add(contentContainer);
                contentContainer.AddStyleSheetPath(ServicesUtils.StylesheetPath.servicesWindowCommon);
                contentContainer.AddStyleSheetPath(EditorGUIUtility.isProSkin ? ServicesUtils.StylesheetPath.servicesWindowDark : ServicesUtils.StylesheetPath.servicesWindowLight);
                contentContainer.AddStyleSheetPath(ServicesUtils.StylesheetPath.servicesCommon);
                contentContainer.AddStyleSheetPath(EditorGUIUtility.isProSkin ? ServicesUtils.StylesheetPath.servicesDark : ServicesUtils.StylesheetPath.servicesLight);
                var scrollContainer = provider.rootVisualElement.Q(null, k_ScrollContainerClassName);
                scrollContainer.Clear();
                provider.ConfigureNotificationSubscriberForNonStandardStates();

                m_ProjectBindManager = new ProjectBindManager(scrollContainer)
                {
                    createButtonCallback = projectInfoData =>
                    {
                        stateMachine.ProcessEvent(Event.Initializing);
                    },
                    linkButtonCallback = projectInfoData =>
                    {
                        stateMachine.ProcessEvent(Event.Initializing);
                    },
                    exceptionCallback = (exception) =>
                    {
                        NotificationManager.instance.Publish(Notification.Topic.ProjectBind, Notification.Severity.Error,
                            L10n.Tr(exception.Message));
                    }
                };
            }

            SimpleStateMachine<Event>.State HandleInitializing(Event raisedEvent)
            {
                return provider.m_InitialState;
            }

            SimpleStateMachine<Event>.State HandleGoingToDeactivate(Event arg)
            {
                return provider.m_DeactivateState;
            }
        }

        sealed class BrokenBindingState : BaseState
        {
            const string k_TemplatePath = "UXML/ServicesWindow/BrokenBinding.uxml";

            const string k_ProjectBrokenBindContainerName = "ProjectBrokenBindContainer";
            const string k_NewLinkButtonName = "NewLinkBtn";
            const string k_RefreshAccessButtonName = "RefreshAccessBtn";

            const string k_PermissionRefreshedMessage = "Permission refreshed";

            public BrokenBindingState(SimpleStateMachine<Event> simpleStateMachine, ServicesProjectSettings provider)
                : base(simpleStateMachine, k_StateNameBrokenBinding, provider)
            {
                ModifyActionForEvent(Event.Initializing, HandleInitializing);
                ModifyActionForEvent(Event.Unbinding, HandleUnbinding);
                ModifyActionForEvent(Event.GoingToDeactivate, HandleGoingToDeactivate);
            }

            public override void EnterState()
            {
                provider.rootVisualElement.Clear();
                var contentContainer = provider.m_GeneralTemplate.CloneTree().contentContainer;
                ServicesUtils.TranslateStringsInTree(contentContainer);
                provider.rootVisualElement.Add(contentContainer);
                contentContainer.AddStyleSheetPath(ServicesUtils.StylesheetPath.servicesWindowCommon);
                contentContainer.AddStyleSheetPath(EditorGUIUtility.isProSkin ? ServicesUtils.StylesheetPath.servicesWindowDark : ServicesUtils.StylesheetPath.servicesWindowLight);
                contentContainer.AddStyleSheetPath(ServicesUtils.StylesheetPath.servicesCommon);
                contentContainer.AddStyleSheetPath(EditorGUIUtility.isProSkin ? ServicesUtils.StylesheetPath.servicesDark : ServicesUtils.StylesheetPath.servicesLight);

                var brokenBindingTemplate = EditorGUIUtility.Load(k_TemplatePath) as VisualTreeAsset;
                var scrollContainer = provider.rootVisualElement.Q(null, k_ScrollContainerClassName);
                scrollContainer.Clear();
                provider.ConfigureNotificationSubscriberForNonStandardStates();
                var brokenContentContainer = brokenBindingTemplate.CloneTree().contentContainer;
                ServicesUtils.TranslateStringsInTree(brokenContentContainer);
                scrollContainer.Add(brokenContentContainer);

                var projectBrokenBindContainer = provider.rootVisualElement.Q(k_ProjectBrokenBindContainerName);

                var newLinkButton = projectBrokenBindContainer.Q<Button>(k_NewLinkButtonName);
                if (newLinkButton != null)
                {
                    newLinkButton.clicked += () =>
                    {
                        stateMachine.ProcessEvent(Event.Unbinding);
                    };
                }
                var refreshAccessButton = projectBrokenBindContainer.Q<Button>(k_RefreshAccessButtonName);
                if (refreshAccessButton != null)
                {
                    refreshAccessButton.clicked += () =>
                    {
                        NotificationManager.instance.Publish(Notification.Topic.ProjectBind, Notification.Severity.Info, L10n.Tr(k_PermissionRefreshedMessage));
                        UnityConnect.instance.RefreshProject();
                    };
                }
            }

            SimpleStateMachine<Event>.State HandleInitializing(Event raisedEvent)
            {
                return provider.m_InitialState;
            }

            SimpleStateMachine<Event>.State HandleUnbinding(Event raisedEvent)
            {
                return provider.m_UnboundState;
            }

            SimpleStateMachine<Event>.State HandleGoingToDeactivate(Event arg)
            {
                return provider.m_DeactivateState;
            }
        }

        sealed class OfflineState : BaseState
        {
            const string k_TemplatePath = "UXML/ServicesWindow/Offline.uxml";

            const string k_OfflineContainerName = "OfflineContainer";
            const string k_RefreshButtonName = "RefreshBtn";

            const string k_ConnectionRefreshedMessage = "Connection refresh attempted";

            public OfflineState(SimpleStateMachine<Event> simpleStateMachine, ServicesProjectSettings provider)
                : base(simpleStateMachine, k_StateNameOffline, provider)
            {
                ModifyActionForEvent(Event.Initializing, HandleInitializing);
                ModifyActionForEvent(Event.GoingToDeactivate, HandleGoingToDeactivate);
            }

            public override void EnterState()
            {
                provider.rootVisualElement.Clear();
                var contentContainer = provider.m_GeneralTemplate.CloneTree().contentContainer;
                ServicesUtils.TranslateStringsInTree(contentContainer);
                provider.rootVisualElement.Add(contentContainer);
                contentContainer.AddStyleSheetPath(ServicesUtils.StylesheetPath.servicesWindowCommon);
                contentContainer.AddStyleSheetPath(EditorGUIUtility.isProSkin ? ServicesUtils.StylesheetPath.servicesWindowDark : ServicesUtils.StylesheetPath.servicesWindowLight);
                contentContainer.AddStyleSheetPath(ServicesUtils.StylesheetPath.servicesCommon);
                contentContainer.AddStyleSheetPath(EditorGUIUtility.isProSkin ? ServicesUtils.StylesheetPath.servicesDark : ServicesUtils.StylesheetPath.servicesLight);

                var offlineTemplate = EditorGUIUtility.Load(k_TemplatePath) as VisualTreeAsset;
                var scrollContainer = provider.rootVisualElement.Q(null, k_ScrollContainerClassName);
                scrollContainer.Clear();
                provider.ConfigureNotificationSubscriberForNonStandardStates();
                var offlineTemplateContainer = offlineTemplate.CloneTree().contentContainer;
                ServicesUtils.TranslateStringsInTree(offlineTemplateContainer);
                scrollContainer.Add(offlineTemplateContainer);

                var offlineContainer = provider.rootVisualElement.Q(k_OfflineContainerName);

                var refreshAccessButton = offlineContainer.Q<Button>(k_RefreshButtonName);
                if (refreshAccessButton != null)
                {
                    refreshAccessButton.clicked += () =>
                    {
                        NotificationManager.instance.Publish(Notification.Topic.ProjectBind, Notification.Severity.Info, L10n.Tr(k_ConnectionRefreshedMessage));
                        UnityConnect.instance.RefreshProject();
                    };
                }
            }

            SimpleStateMachine<Event>.State HandleInitializing(Event raisedEvent)
            {
                return provider.m_InitialState;
            }

            SimpleStateMachine<Event>.State HandleGoingToDeactivate(Event arg)
            {
                return provider.m_DeactivateState;
            }
        }

        sealed class ConnectingServicesState : BaseState
        {
            const string k_TemplatePath = "UXML/ServicesWindow/ConnectingServices.uxml";

            const string k_ConnectingServicesContainerName = "ConnectingServicesContainer";
            const string k_RefreshButtonName = "RefreshBtn";

            const string k_ConnectionRefreshedMessage = "Attempting connection refresh.";
            const string k_ConnectionFailedMessage = "Failed to connect to Services. Services are not reachable right now.";
            const short k_MaxVerifyRetries = 50;
            const short k_VerifyDelay = 500;

            IVisualElementScheduledItem m_ScheduledVerify;
            short m_VerifyRetries;

            public ConnectingServicesState(SimpleStateMachine<Event> simpleStateMachine, ServicesProjectSettings provider)
                : base(simpleStateMachine, k_StateNameConnectingServices, provider)
            {
                ModifyActionForEvent(Event.Initializing, HandleInitializing);
                ModifyActionForEvent(Event.GoingToDeactivate, HandleGoingToDeactivate);
            }

            public override void EnterState()
            {
                provider.rootVisualElement.Clear();
                var contentContainer = provider.m_GeneralTemplate.CloneTree().contentContainer;
                ServicesUtils.TranslateStringsInTree(contentContainer);
                provider.rootVisualElement.Add(contentContainer);
                contentContainer.AddStyleSheetPath(ServicesUtils.StylesheetPath.servicesWindowCommon);
                contentContainer.AddStyleSheetPath(EditorGUIUtility.isProSkin ? ServicesUtils.StylesheetPath.servicesWindowDark : ServicesUtils.StylesheetPath.servicesWindowLight);
                contentContainer.AddStyleSheetPath(ServicesUtils.StylesheetPath.servicesCommon);
                contentContainer.AddStyleSheetPath(EditorGUIUtility.isProSkin ? ServicesUtils.StylesheetPath.servicesDark : ServicesUtils.StylesheetPath.servicesLight);

                var connectingServicesTemplate = EditorGUIUtility.Load(k_TemplatePath) as VisualTreeAsset;
                var scrollContainer = provider.rootVisualElement.Q(null, k_ScrollContainerClassName);
                scrollContainer.Clear();
                provider.ConfigureNotificationSubscriberForNonStandardStates();
                var connectingServicesTemplateContainer = connectingServicesTemplate.CloneTree().contentContainer;
                ServicesUtils.TranslateStringsInTree(connectingServicesTemplateContainer);
                scrollContainer.Add(connectingServicesTemplateContainer);

                var connectingServicesContainer = provider.rootVisualElement.Q(k_ConnectingServicesContainerName);

                var refreshAccessButton = connectingServicesContainer.Q<Button>(k_RefreshButtonName);
                if (refreshAccessButton != null)
                {
                    refreshAccessButton.clicked += () =>
                    {
                        NotificationManager.instance.Publish(Notification.Topic.ProjectBind, Notification.Severity.Info, L10n.Tr(k_ConnectionRefreshedMessage));
                        ServicesConfiguration.instance.LoadConfigurations(true);
                        stateMachine.ProcessEvent(Event.Initializing);
                    };
                }

                if (m_ScheduledVerify != null)
                {
                    ClearScheduledVerify();
                }
                //Rerun every half second and delay first call for a second.
                //It will probably never come to this since fetching URLs is very fast, but...
                m_ScheduledVerify = contentContainer.schedule.Execute(VerifyPaths).Every(k_VerifyDelay).StartingIn(k_VerifyDelay);
            }

            void ClearScheduledVerify()
            {
                m_VerifyRetries = 0;
                m_ScheduledVerify.Pause();
                m_ScheduledVerify = null;
            }

            void VerifyPaths()
            {
                ServicesConfiguration.instance.UpdateProgress();
                if (!ServicesConfiguration.instance.pathsReady && !ServicesConfiguration.instance.loadingConfigurations)
                {
                    //Get a path to force load
                    ServicesConfiguration.instance.RequestBaseDashboardUrl(s => {});
                }

                if (ServicesConfiguration.instance.pathsReady)
                {
                    ClearScheduledVerify();
                    stateMachine.ProcessEvent(Event.Initializing);
                    return;
                }

                m_VerifyRetries++;
                if (m_VerifyRetries > k_MaxVerifyRetries)
                {
                    //That is enough, message and stop
                    //Using the refresh btn will relaunch the verif for another k_MaxVerifyRetries attempts
                    NotificationManager.instance.Publish(Notification.Topic.ProjectBind, Notification.Severity.Error, L10n.Tr(k_ConnectionFailedMessage));
                    ClearScheduledVerify();
                }
            }

            SimpleStateMachine<Event>.State HandleInitializing(Event raisedEvent)
            {
                return provider.m_InitialState;
            }

            SimpleStateMachine<Event>.State HandleGoingToDeactivate(Event arg)
            {
                return provider.m_DeactivateState;
            }
        }

        sealed class LoggedOutState : BaseState
        {
            const string k_TemplatePath = "UXML/ServicesWindow/LoggedOut.uxml";

            const string k_LoggedOutContainerName = "LoggedOutContainer";
            const string k_SignInButtonName = "SignInBtn";

            public LoggedOutState(SimpleStateMachine<Event> simpleStateMachine, ServicesProjectSettings provider)
                : base(simpleStateMachine, k_StateNameLoggedOut, provider)
            {
                ModifyActionForEvent(Event.Initializing, HandleInitializing);
                ModifyActionForEvent(Event.GoingToDeactivate, HandleGoingToDeactivate);
            }

            public override void EnterState()
            {
                provider.rootVisualElement.Clear();
                var contentContainer = provider.m_GeneralTemplate.CloneTree().contentContainer;
                ServicesUtils.TranslateStringsInTree(contentContainer);
                provider.rootVisualElement.Add(contentContainer);
                contentContainer.AddStyleSheetPath(ServicesUtils.StylesheetPath.servicesWindowCommon);
                contentContainer.AddStyleSheetPath(EditorGUIUtility.isProSkin ? ServicesUtils.StylesheetPath.servicesWindowDark : ServicesUtils.StylesheetPath.servicesWindowLight);
                contentContainer.AddStyleSheetPath(ServicesUtils.StylesheetPath.servicesCommon);
                contentContainer.AddStyleSheetPath(EditorGUIUtility.isProSkin ? ServicesUtils.StylesheetPath.servicesDark : ServicesUtils.StylesheetPath.servicesLight);

                var loggedOutTemplate = EditorGUIUtility.Load(k_TemplatePath) as VisualTreeAsset;
                var scrollContainer = provider.rootVisualElement.Q(null, k_ScrollContainerClassName);
                scrollContainer.Clear();
                provider.ConfigureNotificationSubscriberForNonStandardStates();
                var loggedOutTemplateContainer = loggedOutTemplate.CloneTree().contentContainer;
                ServicesUtils.TranslateStringsInTree(loggedOutTemplateContainer);
                scrollContainer.Add(loggedOutTemplateContainer);

                var loggedOutContainer = provider.rootVisualElement.Q(k_LoggedOutContainerName);

                var signInButton = loggedOutContainer.Q<Button>(k_SignInButtonName);
                if (signInButton != null)
                {
                    signInButton.clicked += () =>
                    {
                        UnityConnect.instance.ShowLogin();
                    };
                }
            }

            SimpleStateMachine<Event>.State HandleInitializing(Event raisedEvent)
            {
                return provider.m_InitialState;
            }

            SimpleStateMachine<Event>.State HandleGoingToDeactivate(Event arg)
            {
                return provider.m_DeactivateState;
            }
        }

        protected enum UserRole
        {
            User,
            Owner,
            Manager,
        }

        protected class GenericBaseState<T, U> : SimpleStateMachine<U>.State
            where T : ServicesProjectSettings
        {
            public T provider { get; private set; }

            protected const string k_NotLatestPackageInstalledInfo = "A newer version of the {0} exists and can be installed.";
            protected const string k_DuplicateInstallWarning = "Installing both Asset Store and Package Manager packages will generate duplication.\nDo you want to continue?";
            protected const string k_PackageInstallationHeadsup = "You are about to install the latest {0}.\nDo you want to continue?";
            protected const string k_PackageInstallationDialogTitle = "{0} Installation";

            const string k_GoToPackManButton = "GoToPackManButton";
            const string k_CurrentVersionInfo = "CurrentVersionInfo";
            const string k_LatestVersionInfo = "LatestVersionInfo";
            const string k_InstallLatestVersion = "InstallLatestVersion";
            const string k_SameVersionInfo = "SameVersionInfo";
            const string k_ChoiceYes = "Yes";
            const string k_ChoiceNo = "No";
            const string k_NotApplicable = "N/A";

            // UIElements For the related package messages
            Label m_CurrentPackageVersionLabel;
            Label m_LatestPackageVersionLabel;
            Button m_InstallLatestVersion;
            Label m_SameVersionLabel;

            ListRequest m_Request; // To get a list of all packages for the project
            SearchRequest m_SearchRequest; // To get the actual available package
            AddRequest m_AddRequest; // To start the actual update process
            bool m_InstallingNewPackage; // A switch to make sure to only call the installation in a singleton fashion...
            bool m_NotLatestPackageInfoHasBeenShown;

            // Information to be set-up by the actual child classes
            protected Notification.Topic topicForNotifications { get; set; }
            protected string notLatestPackageInstalledInfo { get; set; }
            protected string packageInstallationHeadsup { get; set; }
            protected string duplicateInstallWarning { get; set; }
            protected string packageInstallationDialogTitle { get; set;  }
            protected bool assetStorePackageInstalled { get; set; }
            protected bool packmanPackageInstalled { get; set; }
            protected bool isLatestPackageInstalled { get; set; }

            protected string installPackageVersion { get; set; }

            // String containing the actual text for the version number label
            protected string currentPackageVersion { get; private set; }
            protected string latestPackageVersion { get; private set; }

            protected void UpdatePackageInformation()
            {
                currentPackageVersion = string.Empty;
                latestPackageVersion = string.Empty;

                // List packages installed for the Project
                m_Request = Client.List();
                EditorApplication.update += ListingCurrentPackageProgress;

                // Look for a specific package
                m_SearchRequest = Client.Search(provider.serviceInstance.packageName);
                EditorApplication.update += SearchPackageProgress;
            }

            protected virtual string GetUpdatablePackageVersion()
            {
                return latestPackageVersion;
            }

            protected void UpdatePackageUpdateButton()
            {
                if ((m_InstallLatestVersion == null) || (m_SameVersionLabel == null))
                {
                    return;
                }

                if ((currentPackageVersion != string.Empty) && (GetUpdatablePackageVersion() != string.Empty))
                {
                    if (currentPackageVersion.Equals(GetUpdatablePackageVersion()))
                    {
                        isLatestPackageInstalled = true;
                        m_InstallLatestVersion.style.display = DisplayStyle.None;
                        m_SameVersionLabel.style.display = DisplayStyle.Flex;
                    }
                    else
                    {
                        isLatestPackageInstalled = false;
                        m_InstallLatestVersion.style.display = DisplayStyle.Flex;
                        m_SameVersionLabel.style.display = DisplayStyle.None;

                        // Display an info if a package is installed, but it is not the latest one !
                        if (!m_NotLatestPackageInfoHasBeenShown)
                        {
                            NotificationManager.instance.Publish(topicForNotifications, Notification.Severity.Info, L10n.Tr(notLatestPackageInstalledInfo));
                            m_NotLatestPackageInfoHasBeenShown = true;
                        }
                    }
                }
            }

            protected void AddPackageProgress()
            {
                if (m_AddRequest.IsCompleted)
                {
                    EditorApplication.update -= AddPackageProgress;
                    if (m_AddRequest.Status >= StatusCode.Failure)
                    {
                        Debug.LogError(m_AddRequest.Error.message);
                        NotificationManager.instance.Publish(topicForNotifications, Notification.Severity.Error, m_AddRequest.Error.message);
                    }
                    else
                    {
                        EditorAnalytics.SendImportServicePackageEvent(new ImportPackageInfo() { packageName = provider.serviceInstance.packageName, version = installPackageVersion });
                    }
                    m_InstallingNewPackage = false;
                }
            }

            protected void SearchPackageProgress()
            {
                if (m_SearchRequest.IsCompleted)
                {
                    EditorApplication.update -= SearchPackageProgress;
                    if (m_SearchRequest.Status == StatusCode.Success)
                    {
                        foreach (var package in m_SearchRequest.Result)
                        {
                            if (package.name.Equals(provider.serviceInstance.packageName))
                            {
                                OnSearchPackageFound(package);
                                if (m_LatestPackageVersionLabel != null)
                                {
                                    m_LatestPackageVersionLabel.text = GetUpdatablePackageVersion();
                                }
                                break;
                            }
                        }
                    }
                    else if (m_SearchRequest.Status >= StatusCode.Failure)
                    {
                        Debug.LogError(m_SearchRequest.Error.message);
                        NotificationManager.instance.Publish(topicForNotifications, Notification.Severity.Error, m_SearchRequest.Error.message);
                    }
                    UpdatePackageUpdateButton();
                    PackageInformationUpdated();
                }
            }

            protected virtual void OnSearchPackageFound(PackageManager.PackageInfo package)
            {
                latestPackageVersion = package.version;
            }

            protected void ListingCurrentPackageProgress()
            {
                if (m_Request.IsCompleted)
                {
                    packmanPackageInstalled = false;
                    EditorApplication.update -= ListingCurrentPackageProgress;
                    if (m_Request.Status == StatusCode.Success)
                    {
                        // Make sure the actual version is N/A...
                        currentPackageVersion = L10n.Tr(k_NotApplicable).ToUpper();
                        foreach (var package in m_Request.Result)
                        {
                            if (package.name.Equals(provider.serviceInstance.packageName))
                            {
                                packmanPackageInstalled = true;
                                currentPackageVersion = package.version;
                                break;
                            }
                        }
                        if (m_CurrentPackageVersionLabel != null)
                        {
                            m_CurrentPackageVersionLabel.text = currentPackageVersion;
                        }
                        // Call the update button update independently of the actual presence of the package
                        UpdatePackageUpdateButton();
                        PackageInformationUpdated();
                    }
                    else if (m_Request.Status >= StatusCode.Failure)
                    {
                        Debug.LogError(m_Request.Error.message);
                        NotificationManager.instance.Publish(topicForNotifications, Notification.Severity.Error, m_Request.Error.message);
                    }
                }
            }

            protected virtual void PackageInformationUpdated() {}

            protected void PreparePackageSection(VisualElement sectionRoot)
            {
                var gotoPackManWindow = sectionRoot.Q<Button>(k_GoToPackManButton);
                if (gotoPackManWindow != null)
                {
                    gotoPackManWindow.clicked += () =>
                    {
                        var packageName = provider.serviceInstance.packageName;
                        EditorAnalytics.SendOpenPackManFromServiceSettings(new OpenPackageManager() { packageName = packageName });
                        PackageManagerWindow.OpenPackageManager(packageName);
                    };
                }
                m_CurrentPackageVersionLabel = sectionRoot.Q<Label>(k_CurrentVersionInfo);
                m_LatestPackageVersionLabel = sectionRoot.Q<Label>(k_LatestVersionInfo);
                // Make sure both texts are upper case...
                if (m_CurrentPackageVersionLabel != null)
                {
                    m_CurrentPackageVersionLabel.text = m_CurrentPackageVersionLabel.text.ToUpper();
                }
                if (m_LatestPackageVersionLabel != null)
                {
                    m_LatestPackageVersionLabel.text = m_LatestPackageVersionLabel.text.ToUpper();
                }

                m_SameVersionLabel = sectionRoot.Q<Label>(k_SameVersionInfo);
                if (m_SameVersionLabel != null)
                {
                    m_SameVersionLabel.style.display = DisplayStyle.None;
                }

                m_InstallLatestVersion = sectionRoot.Q<Button>(k_InstallLatestVersion);
                if (m_InstallLatestVersion != null)
                {
                    m_InstallingNewPackage = false;
                    m_InstallLatestVersion.clicked += InstallUpdatePackage;
                    m_InstallLatestVersion.style.display = DisplayStyle.None;
                }
            }

            void InstallUpdatePackage()
            {
                var messageForDialog = L10n.Tr(packageInstallationHeadsup);
                installPackageVersion = GetUpdatablePackageVersion();
                if ((duplicateInstallWarning != null) && assetStorePackageInstalled)
                {
                    messageForDialog = L10n.Tr(duplicateInstallWarning);
                }

                InstallPackage(messageForDialog);
            }

            protected void InstallPackage(string messageForDialog)
            {
                if (!m_InstallingNewPackage)
                {
                    if (EditorUtility.DisplayDialog(L10n.Tr(packageInstallationDialogTitle), messageForDialog,
                        L10n.Tr(k_ChoiceYes), L10n.Tr(k_ChoiceNo)))
                    {
                        m_InstallingNewPackage = true;
                        m_AddRequest = Client.Add(provider.serviceInstance.packageName + "@" + installPackageVersion);
                        EditorApplication.update += AddPackageProgress;
                    }
                }
            }

            public GenericBaseState(string name, SimpleStateMachine<U> simpleStateMachine, T provider)
                : base(name, simpleStateMachine)
            {
                this.provider = provider;
                topicForNotifications = Notification.Topic.NoProject;
                notLatestPackageInstalledInfo = null;
                packageInstallationHeadsup = null;
                duplicateInstallWarning = null;
                packageInstallationDialogTitle = null;
            }
        }
    }
}
