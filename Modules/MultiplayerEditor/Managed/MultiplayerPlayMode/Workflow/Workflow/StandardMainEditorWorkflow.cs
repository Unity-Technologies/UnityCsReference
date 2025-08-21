// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Multiplayer.Internal;
using UnityEngine;
using UnityEngine.Multiplayer.Internal;
using UnityEngine.SceneManagement;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class StandardMainEditorWorkflow
    {
        static string s_LastWriteTime;

        /*
         * The main editor keeps its functionality of starting on the scene tab and switching to game view when going into play mode (if OpenGameViewOnPlay is active)
         * However, the clones always run with which ever view was selected from layout (since OpenGameViewOnPlay is always off)
         *
         * Edges cases:
         * This is currently covering the main editor crashing (it uses the non deleted key in the local data store)
         * The clone crashing (it just always runs with OpenGameViewOnPlay off)
         * As well as these crashes happening in either order (main should be able to crash first or last and etc...)
         *
         * See CloneDisablePlayOnGameView for clone behaviour
         */
        static bool OpenWindowOnEnteringPlayMode
        {
            get => EditorPrefs.GetBool(k_OpenGameViewOnPlay, true);
            set
            {
                if (EditorPrefs.GetBool(k_OpenGameViewOnPlay) != value)
                {
                    EditorPrefs.SetBool(k_OpenGameViewOnPlay, value);
                }
            }
        }

        // The following string and property needs to match the functionality
        //  of PlayModeView.openWindowOnEnteringPlayMode currently
        const string k_OpenGameViewOnPlay = "OpenGameViewOnEnteringPlayMode";
        const string k_OpenGameViewOnPlayCache = "OpenGameViewOnEnteringPlayModeCache";

        public void Initialize(WorkflowMainEditorContext mppmContext, MainEditorContext vpContext)
        {
            // If the editor closed unexpectedly we pull the value
            // that was in memory before we went into playmode
            // in case the clones have altered it
            if (EditorPrefs.HasKey(k_OpenGameViewOnPlayCache))
            {
                OpenWindowOnEnteringPlayMode = EditorPrefs.GetBool(k_OpenGameViewOnPlayCache);
                EditorPrefs.DeleteKey(k_OpenGameViewOnPlayCache);
            }

            EditorMultiplayerManager.activeMultiplayerRoleChanged += () =>
            {
                // Based on other system actually updating the role, we update the JSON, which is the authority of the role in MPPM
                // This is just for this editors role
                foreach (var player in UnityPlayer.GetPlayers(VirtualProjectWorkflow.WorkflowMainEditorContext.SystemDataStore))
                {
                    if (player.Type == PlayerType.Main)
                    {
                        var role = (int)EditorMultiplayerManager.activeMultiplayerRoleMask;
                        if (role != player.MultiplayerRole)
                        {
                            player.MultiplayerRole = role;
                            VirtualProjectWorkflow.WorkflowMainEditorContext.SystemDataStore.SavePlayerJson(player.Index, player);  // updates file on disk
                            break;
                        }
                    }
                }
            };

            mppmContext.MainPlayerSystems.ApplicationEvents.SceneChanged += (_, _) =>
            {
                if (EditorApplication.isPaused)
                {
                    vpContext.MessagingService.Broadcast(new PauseMessage());
                }
            };
            mppmContext.MainPlayerSystems.ApplicationEvents.LogCountsChanged += (identifier, logCounts) =>
            {
                if (!mppmContext.LogsRepository.ContainsKey(identifier))
                {
                    mppmContext.LogsRepository.Create(identifier, new BoxedLogCounts { LogCounts = logCounts, });
                }
                else
                {
                    mppmContext.LogsRepository.Update(identifier, playerLogs => { playerLogs.LogCounts = logCounts; }, out _);
                }
            };

            mppmContext.MainPlayerSystems.ApplicationEvents.UIPollUpdate += () =>
            {
                /*****
                 * Keep the MPPM local cache of the state of the clones up to date
                 * (for the UI and other systems)
                 */

                if(!EditorApplication.isPlaying)
                {
                    if (s_LastWriteTime != SystemDataStore.GetMainLastWriteTime())
                    {
                        s_LastWriteTime = SystemDataStore.GetMainLastWriteTime();
                        ///////////////
                        // We load in a new data store (and not overwrite the object
                        // for now because we don't want to invalidate pointers.
                        // So we just read in the new values if they changed on the object that
                        // already exists in memory (across the whole system)
                        // see :UpdatedDataStore for what this effects
                        var updatedDataStore = SystemDataStore.GetMain();
                        foreach (var player in UnityPlayer.GetPlayers(mppmContext.SystemDataStore))
                        {
                            // Make sure we read and update *every* player
                            var newPlayers = updatedDataStore.LoadAllPlayerJson();
                            var newRole = newPlayers[player.Index].MultiplayerRole;
                            if (newRole != player.MultiplayerRole)
                            {
                                player.MultiplayerRole = newRole;
                                // We don't update file on disk. just read new values from disk
                            }
                            if (player.Type == PlayerType.Main)
                            {
                                EditorMultiplayerManager.activeMultiplayerRoleMask = (MultiplayerRoleFlags)newRole;
                            }
                        }
                    }
                }

                foreach (var player in UnityPlayer.GetPlayers(mppmContext.SystemDataStore))
                {
                    // If a player's editor clone has unexpectedly stopped, prompt for a restart.
                    if (player.Active
                     && player.Type == PlayerType.Clone
                     && player.TypeDependentPlayerInfo.VirtualProjectIdentifier != null
                     && vpContext.VirtualProjectsApi.TryGetFunc(player.TypeDependentPlayerInfo.VirtualProjectIdentifier, out var project, out _)
                     && project.EditorState == EditorState.UnexpectedlyStopped)
                    {
                        var choice = PlayerCrashModal.DisplayPlayerCrashModal(player.Name);
                        if (choice == PlayerCrashModal.Choices.Restart)
                        {
                            var hasProjectState = vpContext.StateRepository.TryGetValue(project.Identifier, out var statePerProcessLifetime);
                            var args = statePerProcessLifetime.LaunchArgs;
                            Debug.Assert(hasProjectState);
                            Debug.Assert(args.Length != 0, $"MPPM has its own arguments that should be in the {nameof(statePerProcessLifetime)}. We should be relaunching with those same arguments (before calling Close which deletes the arguments)");
                            project.Close(out _);   // NOTE: This will kill the state per life time
                            project.Launch(out _, out _, args);
                        }
                        else
                        {
                            project.Close(out _);   // NOTE: This will kill the state per life time
                            player.Active = false;
                            mppmContext.SystemDataStore.SavePlayerJson(player.Index, player);
                        }
                    }
                }
            };

            mppmContext.MainPlayerSystems.ApplicationEvents.PausedOnPlayer += () =>
            {
                if (!EditorApplication.isPaused)
                {
                    EditorApplication.isPaused = true;
                    vpContext.MessagingService.Broadcast(new PauseMessage());
                }
            };

            mppmContext.MainPlayerSystems.PlaymodeEvents.Play += () =>
            {
                if (IsAnyClonesOpen(mppmContext))
                {
                    if (IsAnyDirtySceneOpen(out var openSceneNames))
                    {
                        MppmLog.Warning("Unsaved scene changes in the main editor will not be loaded on other players. " +
                            $"Currently open scenes with unsaved changes include: {openSceneNames}");
                    }
                }

                EditorPrefs.SetBool(k_OpenGameViewOnPlayCache, OpenWindowOnEnteringPlayMode);
                vpContext.MessagingService.Broadcast(new PlayMessage());
            };
            mppmContext.MainPlayerSystems.PlaymodeEvents.Pause += () =>
            {
                vpContext.MessagingService.Broadcast(new PauseMessage());
            };
            mppmContext.MainPlayerSystems.PlaymodeEvents.Step += () =>
            {
                vpContext.MessagingService.Broadcast(new StepMessage());
            };
            mppmContext.MainPlayerSystems.PlaymodeEvents.Unpause += () =>
            {
                vpContext.MessagingService.Broadcast(new UnpauseMessage());
            };
            mppmContext.MainPlayerSystems.PlaymodeEvents.Stop += () =>
            {
                vpContext.MessagingService.Broadcast(new StopMessage());
                OpenWindowOnEnteringPlayMode = EditorPrefs.GetBool(k_OpenGameViewOnPlayCache);
                EditorPrefs.DeleteKey(k_OpenGameViewOnPlayCache);
            };
        }

        static bool IsAnyClonesOpen(WorkflowMainEditorContext context)
        {
            foreach (var player in UnityPlayer.GetPlayers(context.SystemDataStore))
            {
                if (player.Type == PlayerType.Clone && player.Active)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool TryOpenProjectSettingsWindow(string settingPath)
        {
            return WindowLayout.TryOpenProjectSettingsWindow(settingPath);
        }

        static bool IsAnyDirtySceneOpen(out string openSceneNames)
        {
            openSceneNames = string.Empty;

            var openDirtyScenes = new List<Scene>();
            var sceneCount = SceneManager.sceneCount;
            for (var i = 0; i < sceneCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.isDirty)
                {
                    openDirtyScenes.Add(scene);
                }
            }
            if (openDirtyScenes.Count <= 0)
            {
                return false;
            }

            var sceneNames = new List<string>();
            foreach (var scene in openDirtyScenes)
            {
                sceneNames.Add(scene.name);
            }
            openSceneNames = string.Join(", ", sceneNames);
            return true;
        }
    }
}
