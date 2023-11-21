// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderNotifications : VisualElement
    {
        public struct NotificationData
        {
            public string key;
            public string message;
            public bool showDismissButton;
            public Action onDismissButtonClicked;
            public NotificationType notificationType;
            public string actionButtonText;
            public Action onActionButtonClicked;
        }

        public enum NotificationType
        {
            Info,
            Warning
        }

        VisualTreeAsset m_NotificationEntryVTA;
        private List<VisualElement> m_ChildrenToRemove = new();

        public const string BuilderNotificationMessageName = "message";
        public const string BuilderNotificationActionButtonName = "action-button";
        public const string BuilderNotificationDismissButtonName = "dismiss-button";

        private const string k_BuilderNotificationEntryClassName = "unity-builder-notification-entry";
        private const string k_BuilderInfoNotificationEntryClassName = k_BuilderNotificationEntryClassName + "__info";
        private const string k_BuilderWarningNotificationEntryClassName = k_BuilderNotificationEntryClassName + "__warning";
        private const string k_NotificationEntryVTAPath =
            BuilderConstants.UIBuilderPackagePath + "/BuilderNotificationEntry.uxml";

        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new BuilderNotifications();
        }

        public BuilderNotifications()
        {
            m_NotificationEntryVTA = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(k_NotificationEntryVTAPath);
        }

        public void ClearAllNotifications()
        {
            Clear();
        }

        public void ClearNotifications(string notificationKey)
        {
            var children = Children();
            m_ChildrenToRemove.Clear();

            foreach (var child in children)
            {
                if (child.userData != null && child.userData as string == notificationKey)
                {
                    m_ChildrenToRemove.Add(child);
                }
            }

            foreach (var child in m_ChildrenToRemove)
            {
                child.RemoveFromHierarchy();
            }
        }

        public void AddNotification(NotificationData data)
        {
            var newNotification = m_NotificationEntryVTA.CloneTree();
            newNotification.AddToClassList(k_BuilderNotificationEntryClassName);

            switch (data.notificationType)
            {
                case NotificationType.Warning:
                    newNotification.AddToClassList(k_BuilderWarningNotificationEntryClassName);
                    break;
                default:
                    newNotification.AddToClassList(k_BuilderInfoNotificationEntryClassName);
                    break;
            }

            var messageLabel = newNotification.Q<Label>(BuilderNotificationMessageName);
            messageLabel.text = data.message;

            var actionButton = newNotification.Q<Button>(BuilderNotificationActionButtonName);
            if (data.onActionButtonClicked != null)
            {
                actionButton.text = data.actionButtonText;
                actionButton.clickable.clicked +=
                    () =>
                    {
                        data.onActionButtonClicked.Invoke();
                    };
            }
            actionButton.EnableInClassList(BuilderConstants.HiddenStyleClassName,  data.onActionButtonClicked == null);

            var dismissButton = newNotification.Q<Button>(BuilderNotificationDismissButtonName);
            if (data.showDismissButton)
            {
                dismissButton.clickable.clicked +=
                    () =>
                    {
                        newNotification.RemoveFromHierarchy();
                        data.onDismissButtonClicked?.Invoke();
                    };
            }
            dismissButton.EnableInClassList(BuilderConstants.HiddenStyleClassName,  !data.showDismissButton);

            newNotification.userData = data.key;
            Add(newNotification);
        }
    }
}
