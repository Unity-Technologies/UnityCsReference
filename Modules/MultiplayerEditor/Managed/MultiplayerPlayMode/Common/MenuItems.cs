// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics;
using Unity.PlayMode.Editor;
using UnityEditor;

namespace Unity.Multiplayer.PlayMode.Editor
{
    static class MenuItems
    {
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            Menu.menuChanged += SetupMenus;
        }

        static void SetupMenus()
        {
            Menu.menuChanged -= SetupMenus;

            if (MigrationUtility.ShouldDisableMultiplayerPlayMode())
                return;

            Menu.AddMenuItem("Window/Multiplayer/Play Mode Scenarios", "", false, default, PlayModeConfigurationsWindow.ShowWindow, null);
            Menu.AddMenuItem("Window/Multiplayer/Play Mode Status", "", false, default, PlayModeStatusWindow.OpenWindow, null);
            Menu.AddMenuItem("Window/Multiplayer/Multiplayer Play Mode", "", false, default, MultiplayerWindowController.ShowConfiguration, null);

            if (DebugUtils.IsDebugFlagEnabled(DebugUtils.DebugFlags.MppmAnalysisWindow))
            {
                Menu.AddMenuItem("Window/Analysis/Play Mode Scenarios Analysis", "", false, default, ScenarioStatusWindow.OpenWindow, null);
            }
        }
    }
}
