// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Licensing.UI.Data.Events;
using UnityEditor.Licensing.UI.Events.Buttons;
using UnityEditor.Licensing.UI.Events.Text;
using UnityEditor.Licensing.UI.Helper;

namespace UnityEditor.Licensing.UI.Events.Windows;

class LicenseRevokedWindow : TemplateLicenseEventWindow
{
    public static void ShowWindow(INativeApiWrapper nativeApiWrapper, ILicenseLogger licenseLogger, object notification)
    {
        s_NativeApiWrapper = nativeApiWrapper;
        s_LicenseLogger = licenseLogger;
        s_Notification = notification as LicenseUpdateNotification;

        TemplateLicenseEventWindow.ShowWindow<LicenseRevokedWindow>(LicenseTrStrings.RevokedWindowTitle, true);
    }

    public void CreateGUI()
    {
        s_Root = new LicenseRevokedWindowContents(s_NativeApiWrapper.HasUiEntitlement(),
            (LicenseUpdateNotification)s_Notification,
            new EventsButtonFactory(s_NativeApiWrapper, Close),
            s_LicenseLogger);
        rootVisualElement.Add(s_Root);
    }
}
