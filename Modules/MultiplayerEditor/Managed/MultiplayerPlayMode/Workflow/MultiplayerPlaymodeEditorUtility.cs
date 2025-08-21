// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    static class MultiplayerPlaymodeEditorUtility
    {
        public static bool IsPlayerActivateProhibited => EditorUtility.scriptCompilationFailed;

        public static void RevealInFinder(UnityPlayer player)
        {
            var vpContext = EditorContexts.MainEditorContext;
            var directory = vpContext.VirtualProjectsApi.TryGetFunc(player.TypeDependentPlayerInfo.VirtualProjectIdentifier, out var project, out _)
                ? project.Directory
                : Paths.CurrentProjectVirtualProjectsFolder;
            EditorUtility.RevealInFinder(directory);
        }

        public enum FocusPlayerStatus
        {
            None,
            IsNotReady,
            PlayerTypeCannotBeFocused,
            PlayerNotFound,
        }

        public static int GetProcessID(UnityPlayer player) =>
            player.Type switch
            {
                PlayerType.Main => MainProcessID(),
                _ => CloneProcessID(player)
            };

        static int MainProcessID() => int.TryParse(VirtualProjectsEditor.MainEditorProcessId, out var val) ? val : -1;

        static int CloneProcessID(UnityPlayer player)
        {
            if (player.TypeDependentPlayerInfo.VirtualProjectIdentifier == null) return -1;
            if (!player.m_VirtualProjectsApi.TryGetFunc(player.TypeDependentPlayerInfo.VirtualProjectIdentifier,
                out var foundProject, out var _)) return -1;
            return foundProject.ProcessId;
        }

        public static FocusPlayerStatus FocusPlayerView(PlayerIndex playerIndex)
        {
            if (VirtualProjectsEditor.IsClone)
            {
                EditorContexts.CloneContext.MessagingService.Broadcast(new BroadcastOpenPlayerWindowMessage((int)playerIndex), null, MppmLog.Warning);
            }
            else
            {
                var player = playerIndex switch
                {
                    PlayerIndex.Player1 => MultiplayerPlaymode.PlayerOne,
                    PlayerIndex.Player2 => MultiplayerPlaymode.PlayerTwo,
                    PlayerIndex.Player3 => MultiplayerPlaymode.PlayerThree,
                    PlayerIndex.Player4 => MultiplayerPlaymode.PlayerFour,
                    _ => null,
                };

                if (player == null) return FocusPlayerStatus.PlayerNotFound;
                if (player.PlayerState is not (PlayerState.Launched or PlayerState.Launching)) return FocusPlayerStatus.IsNotReady;
                if (player.Type == PlayerType.Main) return FocusPlayerStatus.PlayerTypeCannotBeFocused;
                if (player.TypeDependentPlayerInfo.VirtualProjectIdentifier == null) return FocusPlayerStatus.PlayerTypeCannotBeFocused;

                EditorContexts.MainEditorContext.MessagingService.Send(new OpenPlayerWindowMessage(), player.TypeDependentPlayerInfo.VirtualProjectIdentifier, null, MppmLog.Warning);
            }

            return FocusPlayerStatus.None;
        }

        internal static void ErrorFirstTestFailure()
        {
            try
            {
                var message = VirtualProjectWorkflow.WorkflowMainEditorContext.TestFailure;
                if (message != null)
                {
                    var hasPlayer = Filters.FindFirstPlayerWithVirtualProjectsIdentifier(VirtualProjectWorkflow.WorkflowMainEditorContext.SystemDataStore.LoadAllPlayerJson(), message.Identifier, out var player);
                    Debug.Assert(hasPlayer, $"Unknown Player with VP ID {message.Identifier}");
                    Debug.LogError($"'{message.ResultMessage}'\n" +
                                   $"File[{message.CallingFilePath}]\n" +
                                   $"Line[{message.LineNumber}]\n" +
                                   $"Player[{player.Name}/{message.Identifier}]");
                }
            }
            finally
            {
                VirtualProjectWorkflow.WorkflowMainEditorContext.TestFailure = null;
            }
        }
    }
}
