// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Connect
{
    internal class ServicesEditorWindow : EditorWindow
    {
        const string k_PrivacyPolicyUrl = "https://unity3d.com/legal/privacy-policy";
        const string k_ServicesWindowUxmlPath = "UXML/ServicesWindow/ServicesWindow.uxml";
        const string k_ServiceTemplateUxmlPath = "UXML/ServicesWindow/ServiceTemplate.uxml";

        const string k_WindowTitle = "Services";

        const string k_ServiceNameProperty = "serviceName";

        const string k_ScrollContainerClassName = "scroll-container";
        const string k_ServiceTitleClassName = "service-title";
        const string k_ServiceDescriptionClassName = "service-description";
        const string k_ServiceIconClassName = "service-icon";

        const string k_ServiceStatusClassName = "service-status";
        const string k_ServiceStatusCheckedClassName = "checked";

        const string k_ProjectSettingsLinkName = "ProjectSettingsLink";
        const string k_PrivacyPolicyLinkName = "PrivacyPolicyLink";
        const string k_DashboardLinkName = "DashboardLink";
        const string k_FooterName = "Footer";

        const string k_ProjectNotBoundMessage = "Project is not bound. Either create a new project Id or bind your project to an existing project Id for the dashboard link to become available.";

        Dictionary<string, Label> m_StatusLabelByServiceName = new Dictionary<string, Label>();

        static ServicesEditorWindow s_Instance;

        public static ServicesEditorWindow instance => s_Instance;

        public UIElementsNotificationSubscriber notificationSubscriber { get; private set; }

        private struct CloseServiceWindowState
        {
            public int availableServices;
        }

        [MenuItem("Window/General/Services %0", false, 302)]
        internal static void ShowServicesWindow()
        {
            // Opens the window, otherwise focuses it if it’s already open.
            if (s_Instance == null)
            {
                s_Instance = GetWindow<ServicesEditorWindow>(typeof(InspectorWindow));
                s_Instance.titleContent = new GUIContent(L10n.Tr(k_WindowTitle));
                s_Instance.minSize = new Vector2(300, 150);
            }
            else
            {
                GetWindow<ServicesEditorWindow>();
            }
            EditorAnalytics.SendEventShowService(new ServicesProjectSettings.ShowServiceState() { service = k_WindowTitle, page = "", referrer = "window_menu_item"});
        }

        void OnDestroy()
        {
            EditorAnalytics.SendEventCloseServiceWindow(new CloseServiceWindowState() { availableServices = m_StatusLabelByServiceName.Count });
        }

        public void OnEnable()
        {
            if (s_Instance == null)
            {
                s_Instance = this;
            }
            LoadWindow();
        }

        void LoadWindow()
        {
            rootVisualElement.Clear();

            var mainTemplate = EditorGUIUtility.Load(k_ServicesWindowUxmlPath) as VisualTreeAsset;
            var serviceTemplate = EditorGUIUtility.Load(k_ServiceTemplateUxmlPath) as VisualTreeAsset;
            rootVisualElement.AddStyleSheetPath(ServicesUtils.StylesheetPath.servicesWindowCommon);
            rootVisualElement.AddStyleSheetPath(EditorGUIUtility.isProSkin ? ServicesUtils.StylesheetPath.servicesWindowDark : ServicesUtils.StylesheetPath.servicesWindowLight);

            mainTemplate.CloneTree(rootVisualElement, new Dictionary<string, VisualElement>(), null);
            notificationSubscriber = new UIElementsNotificationSubscriber(rootVisualElement);
            notificationSubscriber.Subscribe(
                Notification.Topic.AdsService,
                Notification.Topic.AnalyticsService,
                Notification.Topic.BuildService,
                Notification.Topic.CollabService,
                Notification.Topic.CoppaCompliance,
                Notification.Topic.CrashService,
                Notification.Topic.ProjectBind,
                Notification.Topic.PurchasingService
            );

            var scrollContainer = rootVisualElement.Q(className: k_ScrollContainerClassName);

            var settingsClickable = new Clickable(() =>
            {
                SettingsService.OpenProjectSettings(GeneralProjectSettings.generalProjectSettingsPath);
            });
            rootVisualElement.Q(k_ProjectSettingsLinkName).AddManipulator(settingsClickable);

            var dashboardClickable = new Clickable(() =>
            {
                if (UnityConnect.instance.projectInfo.projectBound)
                {
                    Application.OpenURL(
                        string.Format(ServicesConfiguration.instance.GetCurrentProjectDashboardUrl(),
                            UnityConnect.instance.projectInfo.organizationId,
                            UnityConnect.instance.projectInfo.projectGUID));
                }
                else
                {
                    NotificationManager.instance.Publish(Notification.Topic.ProjectBind, Notification.Severity.Warning, L10n.Tr(k_ProjectNotBoundMessage));
                    SettingsService.OpenProjectSettings(GeneralProjectSettings.generalProjectSettingsPath);
                }
            });
            rootVisualElement.Q(k_DashboardLinkName).AddManipulator(dashboardClickable);

            var sortedServices = new SortedList<string, SingleService>();
            foreach (var service in ServicesRepository.GetServices())
            {
                sortedServices.Add(service.title, service);
            }
            var footer = scrollContainer.Q(k_FooterName);
            scrollContainer.Remove(footer);
            foreach (var singleCloudService in sortedServices.Values)
            {
                SetupService(scrollContainer, serviceTemplate, singleCloudService);
            }
            scrollContainer.Add(footer);

            var privacyClickable = new Clickable(() =>
            {
                Application.OpenURL(k_PrivacyPolicyUrl);
            });
            scrollContainer.Q(k_PrivacyPolicyLinkName).AddManipulator(privacyClickable);
        }

        void SetupService(VisualElement scrollContainer, VisualTreeAsset serviceTemplate, SingleService singleService)
        {
            scrollContainer.Add(serviceTemplate.CloneTree().contentContainer);
            var serviceIconAsset = EditorGUIUtility.Load(singleService.pathTowardIcon) as Texture2D;
            var serviceRoot = scrollContainer[scrollContainer.childCount - 1];
            serviceRoot.name = singleService.name;

            var serviceTitle = serviceRoot.Q<TextElement>(className: k_ServiceTitleClassName);
            serviceTitle.text = singleService.title;
            serviceRoot.Q<TextElement>(className: k_ServiceDescriptionClassName).text = singleService.description;
            serviceRoot.Q(className: k_ServiceIconClassName).style.backgroundImage = serviceIconAsset;

            Action openServiceSettingsLambda = () =>
            {
                SettingsService.OpenProjectSettings(singleService.projectSettingsPath);
            };
            var clickable = new Clickable(openServiceSettingsLambda);
            serviceRoot.AddManipulator(clickable);

            SetupServiceStatusLabel(serviceRoot, singleService);
        }

        void SetupServiceStatusLabel(VisualElement serviceRoot, SingleService singleService)
        {
            var statusText = serviceRoot.Q<Label>(className: k_ServiceStatusClassName);
            m_StatusLabelByServiceName.Add(singleService.name, statusText);
            SetServiceStatusValue(singleService.name, singleService.IsServiceEnabled());

            if (singleService.displayToggle)
            {
                statusText.style.display = DisplayStyle.Flex;
            }
            else
            {
                statusText.style.display = DisplayStyle.None;
            }
        }

        public void SetServiceStatusValue(string serviceName, bool active)
        {
            if (m_StatusLabelByServiceName.ContainsKey(serviceName))
            {
                m_StatusLabelByServiceName[serviceName].text = active ? L10n.Tr("ON") : L10n.Tr("OFF");
                if (active)
                {
                    m_StatusLabelByServiceName[serviceName].AddToClassList(k_ServiceStatusCheckedClassName);
                }
                else
                {
                    m_StatusLabelByServiceName[serviceName].RemoveFromClassList(k_ServiceStatusCheckedClassName);
                }
            }
        }
    }
}
