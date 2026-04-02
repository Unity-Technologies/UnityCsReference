// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.QuickInstall;

namespace UnityEditor.Vivox
{
    [InitializeOnLoad]
    static class VivoxInstaller
    {
        static readonly QuickInstaller s_Installer = new(new QuickInstallConfig
            (
                packageName: "com.unity.services.vivox",
                assembly: "Unity.Vivox.Editor",
                settingsPageConfig: new SettingsPageConfig(
                    body:   "With over 150 game integrations and 120 million monthly active users, " +
                            "Vivox is the leader in communications for the video game industry. " +
                            "Vivox's managed hosted solution and the Vivox Unity engine plug-in " +
                            "make giving comms to your players easy. Never worry about setting up and  " +
                            "maintaining servers, instability issues, scaling, etc. Vivox will ensure " +
                            "a great voice and text experience for your players so that you can " +
                            "focus on other aspects of your game.",
                    settingsPath: "Project/Services/Vivox",
                    installButton: "Install Vivox",
                    installing: "Installing Vivox ...",
                    showSeparator: true,
                    documentationUrl: "https://docs.unity.com/en-us/vivox/",
                    subtitle: "Add voice and text chat with built-in safety features to your games with Vivox"),
                menuConfig: new MenuConfig(menuPath: "Services/Vivox/Install"),
                analyticConfig: new AnalyticConfig(sendAssetInstallAnalytic: false)
        ));
    }
}
