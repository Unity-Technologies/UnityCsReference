// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.Connect
{
    /// <summary>
    /// Interface that must be implemented to listen in on NotificationManager
    /// </summary>
    internal interface INotificationSubscriber
    {
        /// <summary>
        /// This method will be called by the NotificationManager when new notifications are published for the topics
        /// to which this INotificationSubscriber has subscribed
        /// </summary>
        /// <param name="notification"></param>
        void ReceiveNotification(Notification notification);

        /// <summary>
        /// This method will be called by the NotificationManager when a notification was dismissed
        /// </summary>
        /// <param name="notificationId"></param>
        void DismissNotification(long notificationId);
    }
}
