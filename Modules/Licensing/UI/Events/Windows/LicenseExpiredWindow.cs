// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: License not yet converted
using UnityEditor.Licensing.UI.Data.Events;
using UnityEditor.Licensing.UI.Events.Buttons;
using UnityEditor.Licensing.UI.Events.Text;
using UnityEditor.Licensing.UI.Helper;
using UnityEngine;

namespace UnityEditor.Licensing.UI.Events.Windows;

class LicenseExpiredWindow : TemplateLicenseEventWindow
{
    #pragma warning disable UAL0015 // this side effect does not outlive the current call (global trigger / lazily-loaded asset re-fetched on next access); a stale reference is harmlessly replaced
    public LicenseExpiredWindow() { }
    #pragma warning restore UAL0015

    public static void ShowWindow(INativeApiWrapper nativeApiWrapper, ILicenseLogger licenseLogger, object notification)
    {
        s_NativeApiWrapper = nativeApiWrapper;
        s_Notification = notification as LicenseExpiredNotification;
        s_LicenseLogger = licenseLogger;

        TemplateLicenseEventWindow.ShowWindow<LicenseExpiredWindow>(LicenseTrStrings.ExpiredWindowTitle, true);
    }

    public void CreateGUI()
    {
        s_Root = new LicenseExpiredWindowContents(s_NativeApiWrapper.HasUiEntitlement(),
            (LicenseExpiredNotification)s_Notification,
            new EventsButtonFactory(s_NativeApiWrapper, Close),
            s_LicenseLogger);
        rootVisualElement.Add(s_Root);
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
