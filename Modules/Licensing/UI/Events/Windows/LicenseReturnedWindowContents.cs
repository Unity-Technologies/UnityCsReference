// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.Licensing.UI.Data.Events;
using UnityEditor.Licensing.UI.Data.Events.Base;
using UnityEditor.Licensing.UI.Events.Buttons;
using UnityEditor.Licensing.UI.Events.Handlers;
using UnityEditor.Licensing.UI.Events.Text;
using UnityEditor.Licensing.UI.Helper;
using UnityEngine;

namespace UnityEditor.Licensing.UI.Events.Windows;

class LicenseReturnedWindowContents : TemplateLicenseEventWindowContents
{
    bool m_HasUiEntitlement;
    LicenseUpdateNotification m_Notification;

    public LicenseReturnedWindowContents(bool hasUiEntitlement, LicenseUpdateNotification notification,
        IEventsButtonFactory eventsButtonFactory, ILicenseLogger licenseLogger)
        : base(eventsButtonFactory, licenseLogger)
    {
        m_Notification = notification;
        m_HasUiEntitlement = hasUiEntitlement;
        OnEnable();
    }

    void OnEnable()
    {
        m_Description = LicenseUpdateHandler.BuildLicenseReturnedText(m_HasUiEntitlement, m_Notification);

        m_Buttons.Add(m_HasUiEntitlement ? EventsButtonType.Ok : EventsButtonType.SaveAndQuit);

        m_LogTag = LicenseTrStrings.LicenseReturnedTag;
        m_LogType = LogType.Error;

        CreateContents();
    }
}
