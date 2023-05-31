// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.Licensing.UI.Data.Events;
using UnityEditor.Licensing.UI.Data.Events.Base;
using UnityEditor.Licensing.UI.Events.Buttons;
using UnityEditor.Licensing.UI.Events.Text;
using UnityEditor.Licensing.UI.Helper;
using UnityEngine;

namespace UnityEditor.Licensing.UI.Events.Windows;

class LicenseRevokedWindowContents : TemplateLicenseEventWindowContents
{
    bool m_HasUiEntitlement;
    LicenseUpdateNotification m_Notification;

    public LicenseRevokedWindowContents(bool hasUiEntitlement, LicenseUpdateNotification notification,
        IEventsButtonFactory eventsButtonFactory, ILicenseLogger licenseLogger)
        : base(eventsButtonFactory, licenseLogger)
    {
        m_Notification = notification;
        m_HasUiEntitlement = hasUiEntitlement;
        OnEnable();
    }

    void OnEnable()
    {
        var productNames = m_Notification.GetProductNamesWithReason(NotificationReasons.k_EntitlementGroupRevoked);

        var shortTitle = Helper.Utils.GetDescriptionMessageForProducts(productNames,
            LicenseTrStrings.RevokedShortTitleOneLicense,
            LicenseTrStrings.RevokedShortTitleManyLicenses);

        m_LogTag = LicenseTrStrings.RevokedWindowTitle;
        m_LogType = LogType.Error;

        m_LogMessage = shortTitle;

        if (!m_HasUiEntitlement)
        {
            m_Description = shortTitle + " " + LicenseTrStrings.RevokedDescriptionNoUiEntitlement;
            m_Buttons.Add(EventsButtonType.SaveAndClose);
        }
        else
        {
            m_Description = shortTitle + " " + LicenseTrStrings.RevokedDescriptionWithUiEntitlement;
            m_Buttons.Add(EventsButtonType.Ok);
        }

        CreateContents();
    }
}
