// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.Multiplayer.Internal;
using UnityEngine;
using UnityEngine.Multiplayer.Internal;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class StandardCloneWorkflow
    {
        enum ScriptChangesDuringPlayOptions
        {
            RecompileAndContinuePlaying = 0,
            RecompileAfterFinishedPlaying = 1,
            StopPlayingAndRecompile = 2,
        }   // From ScriptChangesDuringPlayOptions in EditorApplication.cs

        const string k_ScriptCompilationDuringPlay = "ScriptCompilationDuringPlay";
        const string k_OpenGameViewOnPlay = "OpenGameViewOnEnteringPlayMode";
        const string k_ClonedPlayerStateKey = "Unity.Multiplayer.Playmode.Workflow.Editor.StandardCloneWorkflow_ClonedPlayerState";

        static string s_LastWriteTime;

        private CloneState m_ClonedPlayerState;

        public void Initialize(WorkflowCloneContext mppmContext, CloneContext vpContext)
        {
            Application.logMessageReceivedThreaded += (condition, stackTrace, type) => OnLogMessageReceived(condition, stackTrace, type, vpContext);
            m_ClonedPlayerState = JsonUtility.FromJson<CloneState>(EditorPrefs.GetString(k_ClonedPlayerStateKey, "{}"));
            mppmContext.ClonedPlayerSystems.ClonedPlayerApplicationEvents.SyncStateRequested += SyncState;
            SyncState(m_ClonedPlayerState);

            if (!EditorApplication.isPlaying)
            {
                UpdateMultiplayerRoleMask();
            }

            mppmContext.ClonedPlayerSystems.ClonedPlayerApplicationEvents.UIPollUpdate += () =>
            {
                if(!EditorApplication.isPlaying)
                {
                    if (s_LastWriteTime != SystemDataStore.GetCloneLastWriteTime())
                    {
                        s_LastWriteTime = SystemDataStore.GetCloneLastWriteTime();
                        UpdateMultiplayerRoleMask();
                    }
                }
            };

            vpContext.CloneSystems.AssetImportEvents.RequestImport += (didDomainReload, numAssetsChanged) =>
            {
                // This prevents a race condition where the clone goes into playmode first and the main editor does a domain reload before it goes into playmode
                if (EditorPrefs.GetInt(k_ScriptCompilationDuringPlay) == (int)ScriptChangesDuringPlayOptions.StopPlayingAndRecompile && didDomainReload && numAssetsChanged > 0)
                {
                    MppmLog.Debug("MPPM Clone Stop Playing And Recompile");
                    EditorApplication.ExitPlaymode();
                }
            };
            mppmContext.ClonedPlayerSystems.ClonedPlayerApplicationEvents.PlayerActive += () =>
            {
                // Do not update log counts when message logging is enabled,
                // otherwise logs will continually spam every time the message is sent
                if (MppmLog.AreLogsEnabled())
                    return;

                ConsoleWindowUtility.GetConsoleLogCounts(out var error, out var warning, out var log);
                vpContext.MessagingService.Broadcast(new UpdateCloneLogCountsMessage(VirtualProjectsEditor.CloneIdentifier, new LogCounts{Logs = log, Warnings = warning, Errors = error}));
            };
            mppmContext.ClonedPlayerSystems.ClonedPlayerApplicationEvents.PlayerTitleRename += editorTitleUpdater =>
            {
                const string unknownPlayerWindowTitle = "Unknown Player";
                var dataStore = SystemDataStore.GetClone();
                var hasPlayer = Filters.FindFirstPlayerWithVirtualProjectsIdentifier(dataStore.LoadAllPlayerJson(), VirtualProjectsEditor.CloneIdentifier, out var player);
                if (hasPlayer)
                {
                    var tag = player.Tags.Count <= 0 ? string.Empty : $" [{string.Join('|', player.Tags)}]";
                    var roleText = InternalUtilities.GetMultiplayerRoleDisplayText((MultiplayerRoleFlags)player.MultiplayerRole);
                    editorTitleUpdater.title = EditorMultiplayerManager.enableMultiplayerRoles ? $"{player.Name} ({roleText}){tag}" : $"{player.Name}{tag}";
                }
                else
                {
                    editorTitleUpdater.title = unknownPlayerWindowTitle;
                }
            };
            mppmContext.ClonedPlayerSystems.ClonedPlayerApplicationEvents.ConsoleLogMessagesChanged += () =>
            {
                // Do not update log counts when message logging is enabled,
                // otherwise logs will continually spam every time the message is sent
                if (MppmLog.AreLogsEnabled())
                    return;

                ConsoleWindowUtility.GetConsoleLogCounts(out var error, out var warning, out var log);
                vpContext.MessagingService.Broadcast(new UpdateCloneLogCountsMessage(VirtualProjectsEditor.CloneIdentifier, new LogCounts { Logs = log, Warnings = warning, Errors = error }));
            };

            mppmContext.ClonedPlayerSystems.ClonedPlayerApplicationEvents.PlayerPaused += () => vpContext.MessagingService.Broadcast(new PlayerPausedOnCloneMessage());

            mppmContext.ClonedPlayerSystems.PlaymodeEvents.Play += () =>
            {
                /*
                 * The main editor keeps its functionality of starting on the scene tab and switching to game view when going into play mode (if OpenGameViewOnPlay is active)
                 * However, the clones always run with which ever view was selected from layout (since OpenGameViewOnPlay is always off)
                 *
                 * Edges cases:
                 * This is currently covering the main editor crashing (it uses the non deleted key in the local data store)
                 * The clone crashing (it just always runs with OpenGameViewOnPlay off)
                 * As well as these crashes happening in either order (main should be able to crash first or last and etc...)
                 *
                 * See MainCachePlayOnGameView for main editor behaviour
                 */

                // The following string and property needs to match the functionality
                //  of PlayModeView.openWindowOnEnteringPlayMode currently

                // We are a clone so we always go in as false.
                // The main editor will restore the previous mode
                EditorPrefs.SetBool(k_OpenGameViewOnPlay, false);

                // Save the current view first
                EditorApplication.EnterPlaymode();
            };
            mppmContext.ClonedPlayerSystems.PlaymodeEvents.Pause += () => { EditorApplication.isPaused = true; };
            mppmContext.ClonedPlayerSystems.PlaymodeEvents.Step += () => { EditorApplication.Step(); };
            mppmContext.ClonedPlayerSystems.PlaymodeEvents.Unpause += () => { EditorApplication.isPaused = false; };
            mppmContext.ClonedPlayerSystems.PlaymodeEvents.Stop += () =>
            {
                EditorApplication.ExitPlaymode();
            };

            mppmContext.ClonedPlayerSystems.ClonedPlayerApplicationEvents.FrameAfterPlaymodeMessage += () =>
            {
                EditorModesUtility.SwitchLayoutToMode(EditorApplication.isPlaying);
            };
        }

        private void SyncState(CloneState state)
        {
            m_ClonedPlayerState = state;
            EditorPrefs.SetString(k_ClonedPlayerStateKey, JsonUtility.ToJson(m_ClonedPlayerState));
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type, CloneContext vpContext)
        {
            if (!m_ClonedPlayerState.StreamLogsToMainEditor)
                return;

            vpContext.MessagingService.Broadcast(new LogMessage(VirtualProjectsEditor.CloneIdentifier, condition, stackTrace, type));
        }

        static void UpdateMultiplayerRoleMask()
        {
            ///////////////
            // We load in a new data store (and not overwrite the object)
            // for now because we don't want to invalidate pointers.
            // So we just read in the new values if they changed on the object that
            // already exists in memory (across the whole system)
            // The file could have been updated by MultiplayerWindow or by this clones TopView
            // see :UpdatedDataStore for what this effects
            var updatedDataStore = SystemDataStore.GetClone();
            var previousRole = EditorMultiplayerManager.activeMultiplayerRoleMask;
            foreach (var (_, player) in updatedDataStore.LoadAllPlayerJson())
            {
                if (player.TypeDependentPlayerInfo.VirtualProjectIdentifier == VirtualProjectsEditor.CloneIdentifier)
                {
                    var role = (MultiplayerRoleFlags)player.MultiplayerRole;
                    if (previousRole != role)
                    {
                        EditorMultiplayerManager.activeMultiplayerRoleMask = role;
                        break;
                    }
                }
            }
        }
    }
}
