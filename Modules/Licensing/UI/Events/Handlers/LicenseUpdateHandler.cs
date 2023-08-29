// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Licensing.UI.Data.Events;
using UnityEditor.Licensing.UI.Data.Events.Base;
using UnityEditor.Licensing.UI.Events.Text;
using UnityEditor.Licensing.UI.Helper;
using UnityEngine;

namespace UnityEditor.Licensing.UI.Events.Handlers
{
class LicenseUpdateHandler : INotificationHandler
{
    readonly INativeApiWrapper m_NativeApiWrapper;
    readonly ILicenseLogger m_LicenseLogger;
    readonly IModalWrapper m_ModalWrapper;
    LicenseUpdateNotification m_Notification;

    public LicenseUpdateHandler(INativeApiWrapper nativeApiWrapper, ILicenseLogger licenseLogger, IModalWrapper modalWrapper, string jsonNotification)
    {
        m_NativeApiWrapper = nativeApiWrapper;
        m_LicenseLogger = licenseLogger;
        m_ModalWrapper = modalWrapper;
        m_Notification = m_NativeApiWrapper.CreateObjectFromJson<LicenseUpdateNotification>(jsonNotification);
    }

    public override void HandleUI()
    {
        // revoke
        if (m_Notification.HasAnyReason(NotificationReasons.k_EntitlementGroupRevoked))
        {
            m_ModalWrapper.ShowLicenseRevokedWindow(m_NativeApiWrapper, m_LicenseLogger, m_Notification);
        }

        // assigned
        if (m_Notification.HasAnyReason(NotificationReasons.k_EntitlementGroupAdded))
        {
            LogMessage(NotificationReasons.k_EntitlementGroupAdded);
        }

        // removed
        if (m_Notification.HasAnyReason(NotificationReasons.k_EntitlementGroupRemoved))
        {
            m_ModalWrapper.ShowLicenseRemovedWindow(m_NativeApiWrapper, m_LicenseLogger, m_Notification);
        }

        // returned
        if (m_Notification.HasAnyReason(NotificationReasons.k_EntitlementGroupReturned))
        {
            m_ModalWrapper.ShowLicenseReturnedWindow(m_NativeApiWrapper, m_LicenseLogger, m_Notification);
        }
    }

    public override void HandleBatchmode()
    {
        // revoke
        if (m_Notification.HasAnyReason(NotificationReasons.k_EntitlementGroupRevoked))
        {
            LogMessage(NotificationReasons.k_EntitlementGroupRevoked);
        }

        // assigned
        if (m_Notification.HasAnyReason(NotificationReasons.k_EntitlementGroupAdded))
        {
            LogMessage(NotificationReasons.k_EntitlementGroupAdded);
        }

        // removed
        if (m_Notification.HasAnyReason(NotificationReasons.k_EntitlementGroupRemoved))
        {
            LogMessage(NotificationReasons.k_EntitlementGroupRemoved);
        }

        // returned
        if (m_Notification.HasAnyReason(NotificationReasons.k_EntitlementGroupReturned))
        {
            LogMessage(NotificationReasons.k_EntitlementGroupReturned);
        }
    }

    void LogMessage(string reason)
    {
        var productNames = m_Notification.GetProductNamesWithReason(reason);

        var message = string.Empty;
        var tag = string.Empty;
        switch (reason)
        {
            case NotificationReasons.k_EntitlementGroupAdded:
                message = Helper.Utils.GetDescriptionMessageForProducts(productNames,
                    LicenseTrStrings.LicenseAddedDescription,
                    LicenseTrStrings.LicensesAddedDescription);
                tag = LicenseTrStrings.LicenseAddedTag;
                break;
            case NotificationReasons.k_EntitlementGroupRemoved:
            {
                message = BuildLicenseRemovedText(m_NativeApiWrapper.HasUiEntitlement(), m_Notification);
                tag = LicenseTrStrings.LicenseRemovedTag;
                break;
            }
            case NotificationReasons.k_EntitlementGroupRevoked:
                message = Helper.Utils.GetDescriptionMessageForProducts(productNames,
                    LicenseTrStrings.LicenseRevokedDescription,
                    LicenseTrStrings.LicensesRevokedDescription);
                tag = LicenseTrStrings.LicenseRevokedTag;
                break;
            case NotificationReasons.k_EntitlementGroupReturned:
                message = BuildLicenseReturnedText(m_NativeApiWrapper.HasUiEntitlement(), m_Notification);
                tag = LicenseTrStrings.LicenseReturnedTag;
                break;
        }

        m_LicenseLogger.DebugLogNoStackTrace(message, tag: tag);
    }

    public static string BuildLicenseRemovedText(bool hasUiEntitlement, LicenseUpdateNotification notification)
    {
        var productNames = notification.GetProductNamesWithReason(NotificationReasons.k_EntitlementGroupRemoved);
        return hasUiEntitlement
            ? Helper.Utils.GetDescriptionMessageForProducts(productNames,
                LicenseTrStrings.RemovedDescriptionOneLicenseWithUiEntitlement,
                LicenseTrStrings.RemovedDescriptionManyLicensesWithUiEntitlement)
            : Helper.Utils.GetDescriptionMessageForProducts(productNames,
                LicenseTrStrings.RemovedDescriptionOneLicenseNoUiEntitlement,
                LicenseTrStrings.RemovedDescriptionManyLicensesNoUiEntitlement);
    }

    public static string BuildLicenseReturnedText(bool hasUiEntitlement, LicenseUpdateNotification notification)
    {
        var productNames = notification.GetProductNamesWithReason(NotificationReasons.k_EntitlementGroupReturned);
        return hasUiEntitlement
            ? Helper.Utils.GetDescriptionMessageForProducts(productNames,
                LicenseTrStrings.ReturnedDescriptionOneLicenseWithUiEntitlement,
                LicenseTrStrings.ReturnedDescriptionManyLicensesWithUiEntitlement)
            : Helper.Utils.GetDescriptionMessageForProducts(productNames,
                LicenseTrStrings.ReturnedDescriptionOneLicenseNoUiEntitlement,
                LicenseTrStrings.ReturnedDescriptionManyLicensesNoUiEntitlement);
    }
}
}
