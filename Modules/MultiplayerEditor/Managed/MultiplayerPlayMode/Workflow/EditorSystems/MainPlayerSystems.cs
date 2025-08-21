// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class MainPlayerSystems
    {
        int m_LastFrameStepWasDetected;

        public MainPlayerApplicationEvents ApplicationEvents { get; }
        public PlaymodeEvents PlaymodeEvents { get; }

        internal MainPlayerSystems()
        {
            PlaymodeEvents = new PlaymodeEvents();
            ApplicationEvents = new MainPlayerApplicationEvents();
        }

        internal void Listen(WorkflowMainEditorContext mppmContext, MainEditorContext vpContext)
        {
            /*
             * These system classes are simply an aggregation of logic and other events
             *
             * Its only purpose is to forward events to the Internal Runtimes, Workflows, and MultiplayerPlaymode (UI)
             */

            vpContext.MessagingService.Receive<PlayerInitializedMessage>(message =>
            {
                var hasPlayer = Filters.FindFirstPlayerWithVirtualProjectsIdentifier(VirtualProjectWorkflow.WorkflowMainEditorContext.SystemDataStore.LoadAllPlayerJson(), message.Identifier, out var player);
                Debug.Assert(hasPlayer, $"Unknown Player with VP ID '{message.Identifier}'");
                MppmLog.Debug($"{player.Name} launch complete");
                ApplicationEvents.InvokeEditorCommunicative(player.PlayerIdentifier);
                var endTime = DateTime.UtcNow;
                var players = MultiplayerPlaymode.Players;
                foreach (var virtualPlayer in players)
                {
                    if (virtualPlayer.PlayerIdentifier == player.PlayerIdentifier)
                    {
                        // This is important for filtering out default m_TimeSinceStartingLaunch values and preventing outlier durations, as users might enter play mode while the VP is still launching
                        if (virtualPlayer.m_TimeSinceStartingLaunch == default) return;

                        var duration = (long)Math.Round((endTime - virtualPlayer.m_TimeSinceStartingLaunch).TotalMilliseconds);
                        AnalyticsOnVirtualPlayerActiveEvent.Send(new OnVirtualPlayerActiveData()
                        {
                            LaunchingDurationMs = duration
                        });
                        break;
                    }
                }

                var sceneHierarchy = SceneHierarchy.FromCurrentEditorSceneManager();
                vpContext.MessagingService.Send(
                    new SceneHierarchyChangedMessage(sceneHierarchy),
                    message.Identifier,
                    () =>
                    {
                        MppmLog.Debug($"{player.Name} correctly replicated the scene hierarchy with {sceneHierarchy.LoadedScenes.Count} active scenes, {sceneHierarchy.UnloadedScenes.Count} unloaded scenes and {sceneHierarchy.ActiveScene} as active scene");
                        if (EditorApplication.isPlaying)
                        {
                            PlaymodeEvents.InvokePlay();
                        }
                    },
                    error =>
                    {
                        MppmLog.Debug($"{player.Name} failed to replicate the scene hierarchy [{error}]");
                    });
            });
            vpContext.MessagingService.Receive<UpdateCloneLogCountsMessage>(message =>
            {
                var hasPlayer = Filters.FindFirstPlayerWithVirtualProjectsIdentifier(VirtualProjectWorkflow.WorkflowMainEditorContext.SystemDataStore.LoadAllPlayerJson(), message.Identifier, out var player);
                Debug.Assert(hasPlayer, $"Unknown Player with VP ID {message.Identifier}");
                ApplicationEvents.InvokeLogCountsChanged(player.PlayerIdentifier, message.LogCounts);
            });
            vpContext.MessagingService.Receive<PlayerPausedOnCloneMessage>(_ =>
            {
                ApplicationEvents.InvokePausedOnPlayer();
            });
            vpContext.MessagingService.Receive<TestResultMessage>(message =>
            {
                var hasPlayer = Filters.FindFirstPlayerWithVirtualProjectsIdentifier(VirtualProjectWorkflow.WorkflowMainEditorContext.SystemDataStore.LoadAllPlayerJson(), message.Identifier, out _);
                Debug.Assert(hasPlayer, $"Unknown Player with VP ID {message.Identifier}");
                if (!message.ResultCondition)
                {
                    VirtualProjectWorkflow.WorkflowMainEditorContext.TestFailure = message;
                }
            });
            vpContext.MessagingService.Receive<BroadcastOpenPlayerWindowMessage>(message =>
            {
                var hasPlayer = VirtualProjectWorkflow.WorkflowMainEditorContext.SystemDataStore.TryLoadPlayerJson(message.PlayerIndex, out var player);
                Debug.Assert(hasPlayer, $"Unknown Player with index {(PlayerIndex)message.PlayerIndex}");
                EditorContexts.MainEditorContext.MessagingService.Send(new OpenPlayerWindowMessage(), player.TypeDependentPlayerInfo.VirtualProjectIdentifier, null, MppmLog.Warning);
            });

            vpContext.MessagingService.Receive<LogMessage>(message => ApplicationEvents.InvokeLogMessageReceived(message.Identifier, message.Message, message.StackTrace, message.Type));

            EditorSceneManager.sceneOpened += (scene, _) => { ApplicationEvents.InvokeSceneChanged(scene.path, null); };

            EditorApplication.pauseStateChanged += state =>
            {
                if (state == PauseState.Paused)
                {
                    m_LastFrameStepWasDetected = Time.frameCount;
                }

                if (EditorApplication.isPlaying)
                {
                    if (state == PauseState.Paused)
                    {
                        PlaymodeEvents.InvokePause();
                    }
                    else
                    {
                        PlaymodeEvents.InvokeUnpause();
                    }
                }
            };
            EditorApplication.playModeStateChanged += mode =>
            {
                var isExitingPlaymodeFromChangingScenes = mode == PlayModeStateChange.EnteredEditMode && !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode;   //  This is to handle an edge case that is not really defined clearly in the Unity docs. see https://jira.unity3d.com/browse/MTT-5204
                var playButtonPressedInSession = mode == PlayModeStateChange.ExitingEditMode && EditorApplication.isPlayingOrWillChangePlaymode;
                var stopButtonPressedInSession = mode == PlayModeStateChange.ExitingPlayMode || isExitingPlaymodeFromChangingScenes;
                if (playButtonPressedInSession)
                {
                    PlaymodeEvents.InvokePlay();
                }
                else if (stopButtonPressedInSession)
                {
                    PlaymodeEvents.InvokeStop();
                }
            };

            const int durationUIUpdate5Seconds = 5;
            var startTime = DateTime.UtcNow;
            EditorApplication.update += () =>
            {
                var stepButtonPressedInSession = EditorApplication.isPlaying
                                                 && EditorApplication.isPaused
                                                 && Time.deltaTime > 0f;
                if (stepButtonPressedInSession)
                {
                    if (m_LastFrameStepWasDetected < Time.frameCount)
                    {
                        m_LastFrameStepWasDetected = Time.frameCount;

                        PlaymodeEvents.InvokeStep();
                    }
                }

                var hasExceeded = (DateTime.UtcNow - startTime).TotalSeconds >= (float)durationUIUpdate5Seconds;
                if (hasExceeded)
                {
                    startTime = DateTime.UtcNow;
                    ApplicationEvents.InvokeUIPollUpdate();
                }
            };

            OptionallyInitializePlayerInitialState(mppmContext.SystemDataStore, vpContext.VirtualProjectsApi);
        }

        internal static void OptionallyInitializePlayerInitialState(SystemDataStore systemDataStore, VirtualProjectsApiDelegates virtualProjectsApi)
        {
            for (var i = 1; i <= 4; i++)
            {
                Debug.Assert(i is <= 4 and >= 1, $"index {i} is not a valid player index, must be between 1 and 4.");
                if (systemDataStore.TryLoadPlayerJson(i, out var player))
                {
                    if (player.Type != PlayerType.Main
                        && player.TypeDependentPlayerInfo.VirtualProjectIdentifier != null
                        && virtualProjectsApi.TryGetFunc(player.TypeDependentPlayerInfo.VirtualProjectIdentifier, out var project, out _))
                    {
                        player.Active = project.EditorState != EditorState.NotLaunched;
                    }

                    continue;
                }

                player = i == 1 ? PlayerStateJson.NewMain() : PlayerStateJson.NewClone(i);

                systemDataStore.SavePlayerJson(i, player);
            }
        }
    }
}
