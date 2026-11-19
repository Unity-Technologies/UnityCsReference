// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using UnityEditor.QuickInstall;

namespace UnityEditor.InAppPurchasing
{
    static partial class InAppPurchasingInstaller
    {
        // Recreated by Initialize() on every code load; the live instance is owned by QuickInstaller's registries.
        [NoAutoStaticsCleanup]
        static QuickInstaller s_Installer;

        [OnCodeLoaded]
        static void Initialize()
        {
            s_Installer = new(new QuickInstallConfig
            (
                packageName: "com.unity.purchasing",
                assembly: "UnityEditor.Purchasing",
                settingsPageConfig: new SettingsPageConfig(
                    body:   "In order to use In-App Purchasing you need to install the In-App " +
                            "Purchasing package. Clicking the button below will install the latest " +
                            "In-App Purchasing package and allow you to configure your project for " +
                            "In-App Purchasing.",
                    settingsPath: "Project/Services/In-App Purchasing",
                    installButton: "Install In-App Purchasing",
                    installing: "Installing In-App Purchasing ...",
                    documentationUrl: "https://docs.unity3d.com/Packages/com.unity.purchasing@latest"),
                menuConfig: new MenuConfig(menuPath: "Services/In-App Purchasing/Install"),
                analyticConfig: new AnalyticConfig(sendAssetInstallAnalytic: false)
            ));
        }
    }
}
