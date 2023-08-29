// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Licensing.UI.Data.Events;
using UnityEditor.Licensing.UI.Events.Windows;
using UnityEditor.Licensing.UI.Helper;

namespace UnityEditor.Licensing.UI.Events
{
class ModalWrapper : IModalWrapper
{
    public void ShowLicenseExpiredWindow(INativeApiWrapper nativeApiWrapper, ILicenseLogger licenseLogger, LicenseExpiredNotification notification)
    {
        LicenseExpiredWindow.ShowWindow(nativeApiWrapper, licenseLogger, notification);
    }

    public void ShowLicenseRemovedWindow(INativeApiWrapper nativeApiWrapper, ILicenseLogger licenseLogger, LicenseUpdateNotification notification)
    {
        LicenseRemovedWindow.ShowWindow(nativeApiWrapper, licenseLogger, notification);
    }

    public void ShowLicenseReturnedWindow(INativeApiWrapper nativeApiWrapper, ILicenseLogger licenseLogger, LicenseUpdateNotification notification)
    {
        LicenseReturnedWindow.ShowWindow(nativeApiWrapper, licenseLogger, notification);
    }

    public void ShowLicenseRevokedWindow(INativeApiWrapper nativeApiWrapper, ILicenseLogger licenseLogger, LicenseUpdateNotification notification)
    {
        LicenseRevokedWindow.ShowWindow(nativeApiWrapper, licenseLogger, notification);
    }

    public void ShowLicenseOfflineValidityEndingWindow(INativeApiWrapper nativeApiWrapper, ILicenseLogger licenseLogger, LicenseOfflineValidityEndingNotification notification)
    {
        LicenseOfflineValidityEndingWindow.ShowWindow(nativeApiWrapper, licenseLogger, notification);
    }

    public void ShowLicenseOfflineValidityEndedWindow(INativeApiWrapper nativeApiWrapper, ILicenseLogger licenseLogger, LicenseExpiredNotification notification)
    {
        LicenseOfflineValidityEndedWindow.ShowWindow(nativeApiWrapper, licenseLogger, notification);
    }
}
}
