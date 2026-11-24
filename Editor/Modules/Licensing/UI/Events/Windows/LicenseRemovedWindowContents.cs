// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Licensing.UI.Data.Events;
using UnityEditor.Licensing.UI.Events.Buttons;
using UnityEditor.Licensing.UI.Events.Handlers;
using UnityEditor.Licensing.UI.Events.Text;
using UnityEditor.Licensing.UI.Helper;
using UnityEngine;

namespace UnityEditor.Licensing.UI.Events.Windows;

class LicenseRemovedWindowContents : TemplateLicenseEventWindowContents
{
    LicenseUpdateNotification m_Notification;
    bool m_HasUiEntitlement;

    public LicenseRemovedWindowContents(bool hasUiEntitlement, LicenseUpdateNotification notification, IEventsButtonFactory eventsButtonFactory, ILicenseLogger licenseLogger)
        : base(eventsButtonFactory, licenseLogger)
    {
        m_HasUiEntitlement = hasUiEntitlement;
        m_Notification = notification;

        OnEnable();
    }

    void OnEnable()
    {
        m_Description = LicenseUpdateHandler.BuildLicenseRemovedText(m_HasUiEntitlement, m_Notification);

        m_Buttons.Add(m_HasUiEntitlement ? EventsButtonType.OpenUnityHub : EventsButtonType.SaveAndQuit);

        m_LogTag = LicenseTrStrings.LicenseRemovedTag;
        m_LogType = LogType.Error;

        CreateContents();
    }
}
