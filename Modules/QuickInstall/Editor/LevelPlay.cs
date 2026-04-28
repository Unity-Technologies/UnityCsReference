// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.QuickInstall;

namespace UnityEditor.LevelPlay
{
    [InitializeOnLoad]
    static class LevelPlayInstaller
    {
        static readonly QuickInstaller s_Installer = new(new QuickInstallConfig
            (
                packageName: "com.unity.services.levelplay",
                assembly: "Unity.LevelPlay.Editor",
                settingsPageConfig: new SettingsPageConfig(
                    body:   "Drive higher revenue with the leading ads mediation platform from Unity. " +
                            "LevelPlay gives you access to a unified auction of all the leading SDK " +
                            "networks and bidders to maximize competition and increase your potential " +
                            "earnings. Serve a diverse mix of ad formats, get real time monetization " +
                            "data, A/B test adjustments to your ad strategy and more.",
                    settingsPath: "Project/Services/Ads Mediation (LevelPlay)",
                    installButton: "Install LevelPlay",
                    installing: "Installing LevelPlay ...",
                    showSeparator: true,
                    documentationUrl: "https://developers.is.com/ironsource-mobile/unity/unity-plugin/",
                    subtitle: "Monetize your game with Unity LevelPlay"),
                menuConfig: new MenuConfig ( menuPath: "Services/Ads Mediation (LevelPlay)/Install"),
                analyticConfig: new AnalyticConfig(sendAssetInstallAnalytic: true)
        ));
    }
}
