// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using UnityEditor.Licensing.UI.Data.Events;
using UnityEditor.Licensing.UI.Data.Events.Base;
using UnityEditor.Licensing.UI.Events.Text;
using UnityEditor.Licensing.UI.Helper;
using UnityEngine;

namespace UnityEditor.Licensing.UI.Events.Handlers;

class LicenseExpiredHandler : INotificationHandler
{
    readonly INativeApiWrapper m_NativeApiWrapper;
    readonly ILicenseLogger m_LicenseLogger;
    readonly IModalWrapper m_ModalWrapper;
    LicenseExpiredNotification m_Notification;

    public LicenseExpiredHandler(INativeApiWrapper nativeApiWrapper, ILicenseLogger licenseLogger, IModalWrapper modalWrapper, string jsonNotification)
    {
        m_NativeApiWrapper = nativeApiWrapper;
        m_LicenseLogger = licenseLogger;
        m_ModalWrapper = modalWrapper;
        m_Notification = m_NativeApiWrapper.CreateObjectFromJson<LicenseExpiredNotification>(jsonNotification);
    }

    public void HandleUI()
    {
        // offline validity period expired
        if (m_Notification.HasAnyUpdateDateExpired())
        {
            m_ModalWrapper.ShowLicenseOfflineValidityEndedWindow(m_NativeApiWrapper, m_LicenseLogger, m_Notification);
        }

        // license actually expired
        if (m_Notification.HasAnyEndDateExpired())
        {
            m_ModalWrapper.ShowLicenseExpiredWindow(m_NativeApiWrapper, m_LicenseLogger, m_Notification);
        }
    }

    public void HandleBatchmode()
    {
        // offline validity period expired
        if (m_Notification.HasAnyUpdateDateExpired())
        {
            var log = BuildLicenseOfflineValidityEndedText(m_Notification);
            m_LicenseLogger.DebugLogNoStackTrace(log, LogType.Error, LicenseTrStrings.LicenseUpdateDateExpiredTag);
        }

        if (m_Notification.HasAnyEndDateExpired())
        {
            var log = BuildLicenseExpiredText(m_NativeApiWrapper.HasUiEntitlement(), m_Notification);
            m_LicenseLogger.DebugLogNoStackTrace(log, LogType.Error, LicenseTrStrings.LicenseExpiredTag);
        }
    }

    public static string BuildLicenseOfflineValidityEndedText(LicenseExpiredNotification notification)
    {
        return Helper.Utils.GetDescriptionMessageForProducts(notification.GetProductNamesWithReason(NotificationReasons.k_UpdateDateExpired),
            LicenseTrStrings.OfflineValidityEndedDescriptionOneLicense,
            LicenseTrStrings.OfflineValidityEndedDescriptionManyLicenses);
    }

    public static string BuildLicenseExpiredText(bool hasUiEntitlement, LicenseExpiredNotification notification)
    {
        var strProduct = notification.GetProductNamesWithReason(NotificationReasons.k_EndDateExpired);

        if (hasUiEntitlement)
        {
            return Helper.Utils.GetDescriptionMessageForProducts(strProduct,
                LicenseTrStrings.ExpiredDescriptionOneLicenseWithUiEntitlement,
                LicenseTrStrings.ExpiredDescriptionManyLicensesWithUiEntitlement);
        }

        return Helper.Utils.GetDescriptionMessageForProducts(strProduct,
            LicenseTrStrings.ExpiredDescriptionOneLicenseNoUiEntitlement,
            LicenseTrStrings.ExpiredDescriptionManyLicensesNoUiEntitlement);
    }
}
