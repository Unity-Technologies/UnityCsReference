// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Connect
{
    /// <summary>
    /// A common system to display messages.
    /// Uses UIElements, expects the presence of an empty VisualElement with class "notification-container"
    /// where the messages will be displayed, usually at the very top of a window.
    /// Don't forget to subscribe to the topics that are of interest to your context and unsubscribed when closing
    /// window, etc.
    /// </summary>
    internal class UIElementsNotificationSubscriber : INotificationSubscriber
    {
        const string k_NotificationTemplateUxmlPath = "UXML/ServicesWindow/NotificationTemplate.uxml";
        const string k_NotificationTemplateCommonUssPath = "StyleSheets/ServicesWindow/NotificationCommon.uss";
        const string k_NotificationTemplateDarkUssPath = "StyleSheets/ServicesWindow/NotificationDark.uss";
        const string k_NotificationTemplateLightUssPath = "StyleSheets/ServicesWindow/NotificationLight.uss";
        const string k_PagerTextTemplate = "{0} of {1}";
        const string k_NullVisualElementExceptionMessage = "rootVisualElement cannot be null. It should be the rootVisualElement of the UI Toolkit layout.";
        const string k_NotificationSeverityParamName = "Notification.Severity";
        const string k_UnknownSeverityExceptionMessage = "Unknown severity: {0}";

        const string k_NotificationContainerClassName = "notification-container";
        const string k_IconClassName = "icon";
        const string k_IconSeverityErrorClassName = "severity-error";
        const string k_IconSeverityWarningClassName = "severity-warning";
        const string k_IconSeverityInfoClassName = "severity-info";
        const string k_MessageContainerClassName = "message-container";
        const string k_DismissButtonClassName = "dismiss-btn";
        const string k_PagerContainerClassName = "pager-container";
        const string k_PreviousButtonClassName = "previous-btn";
        const string k_PageCountClassName = "page-count";
        const string k_NextButtonClassName = "next-btn";
        const string k_PreviousButtonText = "<";
        const string k_NextButtonText = ">";
        const string k_PagerDefaultValue = "0";

        VisualElement m_NotificationRoot;
        Image m_Icon;
        VisualElement m_MessageContainer;
        Button m_DismissButton;
        VisualElement m_PagerContainer;
        Button m_PreviousButton;
        TextElement m_PageCount;
        Button m_NextButton;

        List<Notification> m_Notifications = new List<Notification>();
        int m_CurrentIndex;
        Notification.Severity? m_LastDisplayedSeverity;

        public UIElementsNotificationSubscriber(VisualElement rootVisualElement)
        {
            if (rootVisualElement == null)
            {
                throw new ArgumentException(L10n.Tr(k_NullVisualElementExceptionMessage));
            }

            // This notification subscriber needs a specific container to work.
            // A specific service can create a container with the right class : notification-container
            var notificationContainer = rootVisualElement.Q(null, k_NotificationContainerClassName);
            if (notificationContainer == null)
            {
                // Since the container does not exist, it is created and inserted as the first element...
                notificationContainer = new VisualElement();
                notificationContainer.AddToClassList(k_NotificationContainerClassName);
                rootVisualElement.Insert(0, notificationContainer);
            }
            notificationContainer.Clear();
            var notificationTemplate = EditorGUIUtility.Load(k_NotificationTemplateUxmlPath) as VisualTreeAsset;
            m_NotificationRoot = notificationTemplate.CloneTree().contentContainer;
            ServicesUtils.TranslateStringsInTree(m_NotificationRoot);
            notificationContainer.Add(m_NotificationRoot);

            rootVisualElement.AddStyleSheetPath(k_NotificationTemplateCommonUssPath);
            rootVisualElement.AddStyleSheetPath(EditorGUIUtility.isProSkin
                ? k_NotificationTemplateDarkUssPath : k_NotificationTemplateLightUssPath);

            InitializeNotification();
            InitializePager();
        }

        void InitializeNotification()
        {
            m_Icon = m_NotificationRoot.Q<Image>(null, k_IconClassName);
            m_MessageContainer = m_NotificationRoot.Q(null, k_MessageContainerClassName);
            m_DismissButton = m_NotificationRoot.Q<Button>(null, k_DismissButtonClassName);
            m_DismissButton.clicked += () =>
            {
                NotificationManager.instance.Dismiss(m_Notifications[m_CurrentIndex].id);
            };
            if (m_Notifications.Count == 0)
            {
                m_NotificationRoot.style.display = DisplayStyle.None;
            }
        }

        void InitializePager()
        {
            m_PagerContainer = m_NotificationRoot.Q(null, k_PagerContainerClassName);

            m_PreviousButton = m_NotificationRoot.Q<Button>(null, k_PreviousButtonClassName);
            m_PageCount = m_NotificationRoot.Q<TextElement>(null, k_PageCountClassName);
            m_NextButton = m_NotificationRoot.Q<Button>(null, k_NextButtonClassName);

            m_PreviousButton.text = k_PreviousButtonText;
            m_PageCount.text = string.Format(L10n.Tr(k_PagerTextTemplate), k_PagerDefaultValue, k_PagerDefaultValue);
            m_NextButton.text = k_NextButtonText;

            m_PreviousButton.clicked += () =>
            {
                if (m_CurrentIndex > 0)
                {
                    m_CurrentIndex -= 1;
                    ShowNotification();
                }
            };

            m_NextButton.clicked += () =>
            {
                if (m_CurrentIndex < m_Notifications.Count)
                {
                    m_CurrentIndex += 1;
                    ShowNotification();
                }
            };
        }

        /// <summary>
        /// Receives and displays a notification
        /// </summary>
        /// <param name="notification"></param>
        /// <exception cref="NotImplementedException"></exception>
        public virtual void ReceiveNotification(Notification notification)
        {
            m_Notifications.Add(notification);
            m_NotificationRoot.style.display = DisplayStyle.Flex;
            m_CurrentIndex = m_Notifications.Count - 1;
            ShowNotification();
        }

        /// <summary>
        /// Removes a notification
        /// </summary>
        /// <param name="notificationId"></param>
        /// <exception cref="NotImplementedException"></exception>
        public virtual void DismissNotification(long notificationId)
        {
            Notification dismissedNotification = null;
            foreach (var notification in m_Notifications)
            {
                if (notification.id == notificationId)
                {
                    dismissedNotification = notification;
                    break;
                }
            }

            if (dismissedNotification != null)
            {
                var dismissedNotificationIndex = m_Notifications.IndexOf(dismissedNotification);
                m_Notifications.Remove(dismissedNotification);
                if (m_Notifications.Count == 0)
                {
                    m_NotificationRoot.style.display = DisplayStyle.None;
                    m_CurrentIndex = -1;
                }
                else if (m_CurrentIndex > dismissedNotificationIndex
                         || (m_CurrentIndex == dismissedNotificationIndex && m_CurrentIndex >= m_Notifications.Count))
                {
                    m_CurrentIndex -= 1;
                }

                ShowNotification();
            }
        }

        void ShowNotification()
        {
            ConfigureNotificationSeverity();
            ConfigureNotificationMessage();
            ConfigurePager();
        }

        void ConfigureNotificationMessage()
        {
            m_MessageContainer.Clear();
            if (m_CurrentIndex > -1)
            {
                m_MessageContainer.Add(m_Notifications[m_CurrentIndex].message);
            }
        }

        void ConfigureNotificationSeverity()
        {
            switch (m_LastDisplayedSeverity)
            {
                case Notification.Severity.Error:
                    m_Icon.RemoveFromClassList(k_IconSeverityErrorClassName);
                    break;
                case Notification.Severity.Warning:
                    m_Icon.RemoveFromClassList(k_IconSeverityWarningClassName);
                    break;
                case Notification.Severity.Info:
                    m_Icon.RemoveFromClassList(k_IconSeverityInfoClassName);
                    break;
            }
            m_LastDisplayedSeverity = null;

            if (m_CurrentIndex > -1)
            {
                switch (m_Notifications[m_CurrentIndex].severity)
                {
                    case Notification.Severity.Error:
                        m_Icon.AddToClassList(k_IconSeverityErrorClassName);
                        break;
                    case Notification.Severity.Warning:
                        m_Icon.AddToClassList(k_IconSeverityWarningClassName);
                        break;
                    case Notification.Severity.Info:
                        m_Icon.AddToClassList(k_IconSeverityInfoClassName);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(k_NotificationSeverityParamName,
                            string.Format(L10n.Tr(k_UnknownSeverityExceptionMessage), m_Notifications[m_CurrentIndex].severity));
                }

                m_LastDisplayedSeverity = m_Notifications[m_CurrentIndex].severity;
            }
        }

        void ConfigurePager()
        {
            if (m_Notifications.Count == 0)
            {
                m_PagerContainer.style.display = DisplayStyle.None;
            }
            else
            {
                m_PagerContainer.style.display = DisplayStyle.Flex;
                m_PageCount.text = string.Format(L10n.Tr(k_PagerTextTemplate), m_CurrentIndex + 1, m_Notifications.Count);
                m_PreviousButton.SetEnabled(m_CurrentIndex > 0);
                m_NextButton.SetEnabled(m_CurrentIndex + 1 < m_Notifications.Count);
            }
        }

        /// <summary>
        /// Helper method to simplify subscribing to the NotificationManager
        /// </summary>
        /// <param name="topics"></param>
        public void Subscribe(params Notification.Topic[] topics)
        {
            var notifications = NotificationManager.instance.Subscribe(this, topics);
            foreach (var notification in notifications)
            {
                ReceiveNotification(notification);
            }
        }

        /// <summary>
        /// Helper method to simplify unsubscribe from the NotificationManager
        /// </summary>
        public void UnsubscribeFromAllTopics()
        {
            NotificationManager.instance.UnsubscribeFromAllTopics(this);
        }

        /// <summary>
        /// Helper method to simplify unsubscribe from the NotificationManager
        /// </summary>
        /// <param name="topics"></param>
        public void Unsubscribe(params Notification.Topic[] topics)
        {
            NotificationManager.instance.Unsubscribe(this, topics);
        }
    }
}
