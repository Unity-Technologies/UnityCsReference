// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    /// <summary>
    /// Represents the player index in the multiplayer play mode.
    /// </summary>
    enum PlayerIndex
    {
        /// <summary>
        /// Index of Player 1.
        /// </summary>
        Player1 = 1,
        /// <summary>
        /// Index of Player 2.
        /// </summary>
        Player2,
        /// <summary>
        /// Index of Player 3.
        /// </summary>
        Player3,
        /// <summary>
        /// Index of Player 4.
        /// </summary>
        Player4
    }

    static class MultiplayerPlaymode
    {
        const string k_EditorModeName = "com.unity.mppm.clone";
        const string k_VPPrefix = "mppm";

        public static bool IsVirtualProjectWorkflowInitialized { get; set; }

        public static UnityPlayer PlayerOne { get; private set; }
        public static UnityPlayer PlayerTwo { get; private set; }
        public static UnityPlayer PlayerThree { get; private set; }
        public static UnityPlayer PlayerFour { get; private set; }

        public static UnityPlayer[] Players { get; private set; }
        public static UnityPlayerTags PlayerTags { get; private set; }

        static MultiplayerPlaymode()
        {
            VirtualProjectWorkflow.OnInitialized += isMainEditor =>
            {
                IsVirtualProjectWorkflowInitialized = true;

                if (!isMainEditor)
                {
                    return;
                }

                Reinitialize();
            };
            VirtualProjectWorkflow.OnDisabled += isMainEditor =>
            {
                if (!IsVirtualProjectWorkflowInitialized)
                {
                    return;
                }

                IsVirtualProjectWorkflowInitialized = false;

                if (!isMainEditor)
                {
                    return;
                }

                var mppmContext = VirtualProjectWorkflow.WorkflowMainEditorContext;
                Debug.Assert(Players != null, nameof(Players) + " was null when deactivating?");
                foreach (var player in Players)
                {
                    player.Deactivate(out _);
                }

                mppmContext.MainPlayerSystems.ApplicationEvents.PlayerCommunicative -= OnApplicationEventsOnPlayerCommunicative;
            };
        }

        internal static void Reinitialize()
        {
            var vpContext = EditorContexts.MainEditorContext;
            var mppmContext = VirtualProjectWorkflow.WorkflowMainEditorContext;

            InternalMultiplayerPlaymodeInitialize(vpContext.VirtualProjectsApi, mppmContext.SystemDataStore, mppmContext.ProjectDataStore);

            mppmContext.MainPlayerSystems.ApplicationEvents.PlayerCommunicative += OnApplicationEventsOnPlayerCommunicative;
        }

        static void OnApplicationEventsOnPlayerCommunicative(PlayerIdentifier identifier)
        {
            for (var index = 0; index < Players.Length; index++)
            {
                var player = Players[index];
                var captureIndex = index;
                if (player.PlayerIdentifier == identifier)
                {
                    player.InvokeOnPlayerCommunicative();
                    _ = MultiplayerPlaymodeEditorUtility.FocusPlayerView((PlayerIndex)captureIndex + 1);
                }
            }
        }

        internal static void InternalMultiplayerPlaymodeInitialize(VirtualProjectsApiDelegates virtualProjectsApi, SystemDataStore systemDataStore, ProjectDataStore projectDataStore)
        {
            var has1 = systemDataStore.TryLoadPlayerJson(1, out var one);
            var has2 = systemDataStore.TryLoadPlayerJson(2, out var two);
            var has3 = systemDataStore.TryLoadPlayerJson(3, out var three);
            var has4 = systemDataStore.TryLoadPlayerJson(4, out var four);

            Debug.Assert(has1);
            Debug.Assert(has2);
            Debug.Assert(has3);
            Debug.Assert(has4);

            PlayerOne = new UnityPlayer(one, systemDataStore, virtualProjectsApi, k_VPPrefix, null);
            PlayerTwo = new UnityPlayer(two, systemDataStore, virtualProjectsApi, k_VPPrefix, DefaultMPPMPlayerLaunchArgs(two.Name));
            PlayerThree = new UnityPlayer(three, systemDataStore, virtualProjectsApi, k_VPPrefix, DefaultMPPMPlayerLaunchArgs(three.Name));
            PlayerFour = new UnityPlayer(four, systemDataStore, virtualProjectsApi, k_VPPrefix, DefaultMPPMPlayerLaunchArgs(four.Name));
            Players = new[]
            {
                PlayerOne, PlayerTwo, PlayerThree, PlayerFour,
            };
            PlayerTags = new UnityPlayerTags(projectDataStore, systemDataStore);
        }

        static string[] DefaultMPPMPlayerLaunchArgs(string projectName)
        {
            var args = new List<string>();
            if (!MultiplayerPlayModeSettings.ForceMainWindow)
            {
                args.Add(CommandLineParameters.BuildEditorModeArgument(k_EditorModeName));
                args.Add(CommandLineParameters.k_NoCloudProjectBindPopup);
            }

            if (!MultiplayerPlayModeSettings.ShowLaunchScreenOnPlayers)
            {
                args.Add(CommandLineParameters.k_NoLaunchScreen);
            }

            args.Add(CommandLineParameters.k_VirtualLibraryFolder);
            args.Add(CommandLineParameters.k_AssetDatabaseReadOnly);
            args.Add(CommandLineParameters.k_NoUMP);
            args.Add(CommandLineParameters.k_UMPRestorePackages);
            args.Add(CommandLineParameters.BuildEditorDebuggingName(projectName));
            return args.ToArray();
        }
    }
}
