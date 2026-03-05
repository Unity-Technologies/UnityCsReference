// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.QuickInstall;
using UnityEngine.Analytics;
using System;

namespace UnityEditor.InAppPurchasing
{
    static class InAppPurchasingInstaller
    {
        static readonly QuickInstaller s_Installer = new(new QuickInstallConfig
            {
                packageId = "com.unity.purchasing",
                alternateInstallAssembly = null,
                settingsProviderConfig = new SettingsProviderConfig
                {
                    installationHelpText =  "In order to use In-App Purchasing you need to install the In-App " +
                                            "Purchasing package. Clicking the button below will install the latest " +
                                            "In-App Purchasing package and allow you to configure your project for " +
                                            "In-App Purchasing.",
                    settingsRootTitle = "Project/Services/In-App Purchasing",
                    documentationUri = new Uri("https://docs.unity3d.com/Packages/com.unity.purchasing@latest"),
                    installButtonText = "Install In-App Purchasing",
                    downloadingText = "Downloading In-App Purchasing ...",
                    installingText = "Installing In-App Purchasing ...",
                    subtitle = null,
                    showSubtitle = false,
                    showDocumentationButton = false,
                    showSeparator = false
                },
                menuPath = "Services/In-App Purchasing/Install",
                analytic = new InAppPurchasingQuickInstallAnalytic(),
            });

        [SettingsProvider]
        internal static SettingsProvider CreateInAppPurchasingInstallerProvider()
        {
            return s_Installer.CreateSettingsProvider();
        }

        [InitializeOnLoadMethod]
        internal static void InitializeMenuItem()
        {
            s_Installer.SetupMenu();
        }
    }

    // Schema com.unity3d.data.schemas.editor.analytics.iap_quickinstall_packageInstalled_v1
    [AnalyticInfo(eventName: "iap_quickinstall_packageInstalled", vendorKey: "unity.quickInstall")]
    class InAppPurchasingQuickInstallAnalytic : QuickInstallAnalytic { }
}
