// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Licensing.UI.Data.Events;
using UnityEditor.Licensing.UI.Events.Buttons;
using UnityEditor.Licensing.UI.Events.Handlers;
using UnityEditor.Licensing.UI.Events.Text;
using UnityEditor.Licensing.UI.Helper;

namespace UnityEditor.Licensing.UI.Events.Windows
{
class LicenseOfflineValidityEndingWindowContents : TemplateLicenseEventWindowContents
{
    LicenseOfflineValidityEndingNotification m_Notification;

    public LicenseOfflineValidityEndingWindowContents(LicenseOfflineValidityEndingNotification notification, IEventsButtonFactory eventsButtonFactory, ILicenseLogger licenseLogger)
        : base(eventsButtonFactory, licenseLogger)
    {
        m_Notification = notification;

        OnEnable();
    }

    void OnEnable()
    {
        LicenseOfflineValidityEndingHandler.BuildOfflineValidityEndingText(m_Notification.details, out var shortTitle, out var description);
        m_Description = description;

        m_LogTag = LicenseTrStrings.OfflineValidityEndingWindowTitle;
        m_LogMessage = shortTitle;

        m_Buttons.Add(EventsButtonType.UpdateLicense);

        CreateContents();
    }
}
}
