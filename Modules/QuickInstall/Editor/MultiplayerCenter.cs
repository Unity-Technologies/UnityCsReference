// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.QuickInstall;

namespace UnityEditor.MultiplayerCenter
{
    [InitializeOnLoad]
    static class MultiplayerCenterInstaller
    {
        // 3010 is the predefined Window/Multiplayer submenu slot; the stub recreates that submenu when it is empty.
        static readonly QuickInstaller s_Installer = new(new QuickInstallConfig
            (
                packageName: "com.unity.multiplayer.center",
                assembly: "Unity.Multiplayer.Center.Editor",
                menuConfig: new MenuConfig(menuPath: "Window/Multiplayer/Multiplayer Center", priority: 3010),
                analyticConfig: new AnalyticConfig(sendAssetInstallAnalytic: false)
        ));
    }
}
