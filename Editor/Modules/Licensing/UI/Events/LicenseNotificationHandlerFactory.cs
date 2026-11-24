// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Licensing.UI.Data.Events;
using UnityEditor.Licensing.UI.Events.Handlers;
using UnityEditor.Licensing.UI.Helper;

namespace UnityEditor.Licensing.UI.Events;

class LicenseNotificationHandlerFactory : ILicenseNotificationHandlerFactory
{
    readonly ILicenseLogger m_LicenseLogger;
    readonly INativeApiWrapper m_NativeApiWrapper;
    readonly IModalWrapper m_ModalWrapper;

    public LicenseNotificationHandlerFactory(ILicenseLogger licenseLogger, INativeApiWrapper nativeApiWrapper, IModalWrapper modalWrapper)
    {
        m_LicenseLogger = licenseLogger;
        m_NativeApiWrapper = nativeApiWrapper;
        m_ModalWrapper = modalWrapper;
    }

    public INotificationHandler GetHandler(NotificationType notificationType, string jsonNotification)
    {
        switch (notificationType)
        {
            case NotificationType.LicenseUpdate:
                return new LicenseUpdateHandler(m_NativeApiWrapper, m_LicenseLogger, m_ModalWrapper, jsonNotification);
            case NotificationType.LicenseOfflineValidityEnding:
                return new LicenseOfflineValidityEndingHandler(m_NativeApiWrapper, m_LicenseLogger, m_ModalWrapper, jsonNotification);
            case NotificationType.LicenseExpired:
                return new LicenseExpiredHandler(m_NativeApiWrapper, m_LicenseLogger, m_ModalWrapper, jsonNotification);
            case NotificationType.Unknown:
            default:
                throw new ArgumentException("Cannot create handler for unknown notification type.");
        }
    }
}
