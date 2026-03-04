// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.QuickInstall;
using UnityEngine.Analytics;
using System;

namespace UnityEditor.LevelPlay
{
    static class LevelPlayInstaller
    {
        static readonly QuickInstaller s_Installer = new(new QuickInstallConfig
            {
                packageId = "com.unity.services.levelplay",
                alternateInstallAssembly = "Unity.LevelPlay.Editor",
                settingsProviderConfig = new SettingsProviderConfig
                {
                    installationHelpText =  "Drive higher revenue with the leading ads mediation platform from Unity. " +
                                            "LevelPlay gives you access to a unified auction of all the leading SDK " +
                                            "networks and bidders to maximize competition and increase your potential " +
                                            "earnings. Serve a diverse mix of ad formats, get real time monetization " +
                                            "data, A/B test adjustments to your ad strategy and more.",
                    settingsRootTitle = "Project/Services/Ads Mediation (LevelPlay)",
                    documentationUri = new Uri("https://developers.is.com/ironsource-mobile/unity/unity-plugin/"),
                    installButtonText = "Install LevelPlay",
                    downloadingText = "Downloading LevelPlay ...",
                    installingText = "Installing LevelPlay ...",
                    subtitle = "Monetize your game with Unity LevelPlay",
                    showSubtitle = true,
                    showDocumentationButton = true,
                    showSeparator = true,
                },
                menuPath = "Services/Ads Mediation (LevelPlay)/Install",
                analytic = new LevelPlayQuickInstallAnalytic()
            });

        [SettingsProvider]
        internal static SettingsProvider CreateLevelPlayInstallerProvider()
        {
            return s_Installer.CreateSettingsProvider();
        }

        [InitializeOnLoadMethod]
        internal static void InitializeMenuItem()
        {
            s_Installer.SetupMenu();
        }
    }

    // Schema com.unity3d.data.schemas.editor.analytics.levelplay_quickinstall_packageInstalled_v1
    [AnalyticInfo(eventName: "levelplay_quickinstall_packageInstalled", vendorKey: "unity.quickInstall")]
    class LevelPlayQuickInstallAnalytic : QuickInstallAnalytic { }
}
