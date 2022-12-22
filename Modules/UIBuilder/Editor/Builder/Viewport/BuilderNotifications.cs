// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Unity.UI.Builder
{
    internal class BuilderNotifications : VisualElement
    {
        VisualTreeAsset m_NotificationEntryVTA;
        int m_PendingNotifications;

        public new class UxmlFactory : UxmlFactory<BuilderNotifications, UxmlTraits> {}

        public bool hasPendingNotifications => m_PendingNotifications > 0;

        public BuilderNotifications()
        {
            m_NotificationEntryVTA = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(BuilderConstants.UIBuilderPackagePath + "/BuilderNotificationEntry.uxml");
            CheckNotificationWorthyStates();
        }

        public void ResetNotifications()
        {
            BuilderProjectSettings.hideNotificationAboutMissingUITKPackage = false;

            ClearNotifications();
            CheckNotificationWorthyStates();
        }

        public void ClearNotifications()
        {
            Clear();
        }

        void AddNotification(string message, string detailsURL, Action closeAction)
        {
            var newNotification = m_NotificationEntryVTA.CloneTree();
            newNotification.AddToClassList("unity-builder-notification-entry");

            var icon = newNotification.Q("icon");
            icon.style.backgroundImage = (Texture2D)EditorGUIUtility.IconContent("console.infoicon.sml").image;

            var messageLabel = newNotification.Q<Label>("message");
            messageLabel.style.textOverflow = TextOverflow.Ellipsis;
            messageLabel.text = message;

            newNotification.Q<Button>("details").clickable.clicked +=
                () => Application.OpenURL(detailsURL);

            newNotification.Q<Button>("dismiss").clickable.clicked +=
                () => { newNotification.RemoveFromHierarchy(); closeAction(); };

            Add(newNotification);
        }

        void CheckNotificationWorthyStates()
        {
            m_PendingNotifications = 0;
        }
    }
}
