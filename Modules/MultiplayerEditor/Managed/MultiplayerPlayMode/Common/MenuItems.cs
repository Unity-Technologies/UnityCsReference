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

            Menu.AddMenuItem("Window/Multiplayer/Multiplayer Play Mode", "", false, 3010, MultiplayerWindowController.ShowConfiguration, null);
            Menu.AddMenuItem("Window/Play Mode/Scenarios", "", false, 3011, PlayModeScenariosWindow.ShowWindow, null);
            Menu.AddMenuItem("Window/Play Mode/Active Scenario", "", false, 3011, ActiveScenarioWindow.OpenWindow, null);

            if (DebugUtils.IsDebugFlagEnabled(DebugUtils.DebugFlags.MppmAnalysisWindow))
            {
                Menu.AddMenuItem("Window/Analysis/Play Mode Scenarios", "", false, 1000, ScenarioStatusWindow.OpenWindow, null);
            }
        }
    }
}
