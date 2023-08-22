// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Licensing.UI.Data.Events;
using UnityEditor.Licensing.UI.Helper;

namespace UnityEditor.Licensing.UI.Events;

interface IModalWrapper
{
    void ShowLicenseExpiredWindow(INativeApiWrapper nativeApiWrapper, ILicenseLogger licenseLogger, LicenseExpiredNotification notification);
    void ShowLicenseRemovedWindow(INativeApiWrapper nativeApiWrapper, ILicenseLogger licenseLogger, LicenseUpdateNotification notification);
    void ShowLicenseReturnedWindow(INativeApiWrapper nativeApiWrapper, ILicenseLogger licenseLogger, LicenseUpdateNotification notification);
    void ShowLicenseRevokedWindow(INativeApiWrapper nativeApiWrapper, ILicenseLogger licenseLogger, LicenseUpdateNotification notification);
    void ShowLicenseOfflineValidityEndingWindow(INativeApiWrapper nativeApiWrapper, ILicenseLogger licenseLogger, LicenseOfflineValidityEndingNotification notification);
    void ShowLicenseOfflineValidityEndedWindow(INativeApiWrapper nativeApiWrapper, ILicenseLogger licenseLogger, LicenseExpiredNotification notification);
}
