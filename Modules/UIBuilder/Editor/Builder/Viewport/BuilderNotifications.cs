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
        public struct NotificationItem
        {
            public string key;
            public string message;
            public VisualElement panel;
        }

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

        public List<NotificationItem> items { get; } = new();

        public BuilderNotifications()
        {
            m_NotificationEntryVTA = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(k_NotificationEntryVTAPath);
        }

        public bool HasNotification(string notificationKey)
        {
            foreach (var notification in items)
            {
                if (notification.key == notificationKey)
                    return true;
            }
            return false;
        }

        public void ClearAllNotifications()
        {
            items.Clear();
            Clear();
        }

        public void ClearNotifications(string notificationKey)
        {
            for (var i = 0; i < items.Count; i++)
            {
                var notification = items[i];
                if (notification.key == notificationKey)
                {
                    notification.panel.RemoveFromHierarchy();
                    items.RemoveAt(i);
                    i--; // Adjust index after removal
                }
            }
        }

        public void AddNotification(NotificationData data)
        {
            var newNotificationPanel = m_NotificationEntryVTA.CloneTree();
            newNotificationPanel.AddToClassList(k_BuilderNotificationEntryClassName);

            switch (data.notificationType)
            {
                case NotificationType.Warning:
                    newNotificationPanel.AddToClassList(k_BuilderWarningNotificationEntryClassName);
                    break;
                default:
                    newNotificationPanel.AddToClassList(k_BuilderInfoNotificationEntryClassName);
                    break;
            }

            var messageLabel = newNotificationPanel.Q<Label>(BuilderNotificationMessageName);
            messageLabel.text = data.message;

            var actionButton = newNotificationPanel.Q<Button>(BuilderNotificationActionButtonName);
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

            var dismissButton = newNotificationPanel.Q<Button>(BuilderNotificationDismissButtonName);
            if (data.showDismissButton)
            {
                dismissButton.clickable.clicked +=
                    () =>
                    {
                        newNotificationPanel.RemoveFromHierarchy();
                        data.onDismissButtonClicked?.Invoke();
                    };
            }
            dismissButton.EnableInClassList(BuilderConstants.HiddenStyleClassName,  !data.showDismissButton);

            newNotificationPanel.userData = data.key;
            Add(newNotificationPanel);
            items.Add(new NotificationItem { key = data.key, message = data.message, panel = newNotificationPanel } );
        }
    }
}
