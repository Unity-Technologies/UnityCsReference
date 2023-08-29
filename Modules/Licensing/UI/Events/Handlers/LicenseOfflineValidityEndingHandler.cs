// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Licensing.UI.Data.Events;
using UnityEditor.Licensing.UI.Events.Text;
using UnityEditor.Licensing.UI.Helper;
using UnityEngine;

namespace UnityEditor.Licensing.UI.Events.Handlers
{
class LicenseOfflineValidityEndingHandler : INotificationHandler
{
    readonly INativeApiWrapper m_NativeApiWrapper;
    readonly ILicenseLogger m_LicenseLogger;
    readonly IModalWrapper m_ModalWrapper;
    LicenseOfflineValidityEndingNotification m_Notification;

    public LicenseOfflineValidityEndingHandler(INativeApiWrapper nativeApiWrapper, ILicenseLogger licenseLogger, IModalWrapper modalWrapper, string jsonNotification)
    {
        m_NativeApiWrapper = nativeApiWrapper;
        m_LicenseLogger = licenseLogger;
        m_ModalWrapper = modalWrapper;
        m_Notification = m_NativeApiWrapper.CreateObjectFromJson<LicenseOfflineValidityEndingNotification>(jsonNotification);
    }

    public override void HandleUI()
    {
        m_ModalWrapper.ShowLicenseOfflineValidityEndingWindow(m_NativeApiWrapper, m_LicenseLogger, m_Notification);
    }

    public override void HandleBatchmode()
    {
        BuildOfflineValidityEndingText(m_Notification.details, out _, out var description);
        m_LicenseLogger.DebugLogNoStackTrace(description, tag: LicenseTrStrings.LicenseOfflineValidityEndingTag);
    }

    public static void BuildOfflineValidityEndingText(IEnumerable<LicenseOfflineValidityEndingNotificationDetails> details,
        out string shortTitle, out string description)
    {
        var remainingDays = CalculateRemainingDays(details);

        var productNames = new List<string>();
        foreach (var detail in details)
        {
            productNames.Add(detail.productName);
        }

        var oxfordFormattedProductNames = Helper.Utils.GetOxfordCommaString(productNames);

        if (remainingDays > 1)
        {
            shortTitle = string.Format(LicenseTrStrings.OfflineValidityEndingShortTitleManyDays, remainingDays);
            description = string.Format(
                productNames.Count == 1
                    ? LicenseTrStrings.OfflineValidityEndingDescriptionOneLicenseManyDays
                    : LicenseTrStrings.OfflineValidityEndingDescriptionManyLicensesManyDays,
                oxfordFormattedProductNames, remainingDays);
        }
        else
        {
            shortTitle = LicenseTrStrings.OfflineValidityEndingShortTitleOneDay;
            description =
                string.Format(
                    productNames.Count == 1
                        ? LicenseTrStrings.OfflineValidityEndingDescriptionOneLicenseOneDay
                        : LicenseTrStrings.OfflineValidityEndingDescriptionManyLicensesOneDay,
                    oxfordFormattedProductNames, remainingDays);
        }

        shortTitle = string.Format(shortTitle, remainingDays);
        description = string.Format(description, remainingDays);
    }

    static int CalculateRemainingDays(IEnumerable<LicenseOfflineValidityEndingNotificationDetails> details)
    {
        var endDates = new List<DateTime>();

        foreach (var detail in details)
        {
            if (DateTime.TryParse(detail.endDate, out var parsed))
            {
                endDates.Add(parsed.ToUniversalTime());
            }
        }

        if (endDates.Count < 1)
        {
            return -1;
        }

        endDates.Sort();

        // Notification times are not exactly 1, 2,... days ahead the end datetimes,
        // but adjusted to some specific local times. To always get the days number we are interested in,
        // calculation should be made with only Date component and in local time.
        return endDates[0].ToLocalTime().Date.Subtract(DateTime.Now.Date).Days;
    }
}
}
