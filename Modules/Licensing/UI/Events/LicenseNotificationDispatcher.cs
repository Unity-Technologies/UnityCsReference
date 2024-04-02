// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Licensing.UI.Data.Events;
using UnityEditor.Licensing.UI.Data.Events.Base;
using UnityEditor.Licensing.UI.Helper;
using UnityEngine;

namespace UnityEditor.Licensing.UI.Events;

class LicenseNotificationDispatcher : INotificationDispatcher
{
    ILicenseNotificationHandlerFactory m_NotificationHandlerFactory;
    ILicenseLogger m_LicenseLogger;
    readonly INativeApiWrapper m_NativeApiWrapper;
    List<NotificationType> m_IgnorableNotifications = new ()
    {
        NotificationType.BorrowFeatureStatus
    };

    public LicenseNotificationDispatcher(ILicenseNotificationHandlerFactory notificationHandlerFactory,
        ILicenseLogger licenseLogger, INativeApiWrapper nativeApiWrapper)
    {
        m_NotificationHandlerFactory = notificationHandlerFactory;
        m_LicenseLogger = licenseLogger;
        m_NativeApiWrapper = nativeApiWrapper;
    }

    public void Dispatch(string jsonNotification)
    {
        var notificationBase = m_NativeApiWrapper.CreateObjectFromJson<Notification>(jsonNotification);

        if (notificationBase.messageType != "Notification")
        {
            m_LicenseLogger.LogError($"Unintended message type: \"{notificationBase.messageType}\" is received!");
            return;
        }

        try
        {
            var notificationType = notificationBase.NotificationTypeAsEnum;

            if (m_IgnorableNotifications.Contains(notificationType))
            {
                return;
            }

            var notificationHandler = m_NotificationHandlerFactory.GetHandler(notificationBase.NotificationTypeAsEnum, jsonNotification);
            notificationHandler.Handle(m_NativeApiWrapper.IsHumanControllingUs());
        }
        catch (ArgumentException)
        {
            // log that received an unknown notification
            m_LicenseLogger.DebugLogNoStackTrace($"There is no handler for: " +
                $"notificationType: \"{notificationBase.notificationType}\", " +
                $"title: \"{notificationBase.title}\", " +
                $"message: \"{notificationBase.message}\", " +
                $"sentDate: \"{notificationBase.sentDate}\""
            );
        }
    }
}
