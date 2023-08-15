// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Licensing.UI.Data.Events;
using UnityEditor.Licensing.UI.Events.Buttons;
using UnityEditor.Licensing.UI.Events.Handlers;
using UnityEditor.Licensing.UI.Events.Text;
using UnityEditor.Licensing.UI.Helper;
using UnityEngine;

namespace UnityEditor.Licensing.UI.Events.Windows;

class LicenseOfflineValidityEndedWindowContents : TemplateLicenseEventWindowContents
{
    LicenseExpiredNotification m_Notification;

    public LicenseOfflineValidityEndedWindowContents(LicenseExpiredNotification notification, IEventsButtonFactory eventsButtonFactory, ILicenseLogger licenseLogger)
        : base(eventsButtonFactory, licenseLogger)
    {
        m_Notification = notification;

        OnEnable();
    }

    void OnEnable()
    {
        m_Description = LicenseExpiredHandler.BuildLicenseOfflineValidityEndedText(m_Notification);

        m_LogTag = LicenseTrStrings.OfflineValidityEndedWindowTitle;
        m_LogType = LogType.Error;
        m_LogMessage = m_Description;

        m_Buttons.Add(EventsButtonType.SaveAndQuit);
        m_Buttons.Add(EventsButtonType.UpdateLicense);

        CreateContents();
    }
}
