// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Multiplayer.PlayMode.Editor
{
    struct Interval
    {
        public float Duration;
        public DateTime StartTime;

        public static bool HasHitInterval(ref Interval interval)
        {
            if ((DateTime.UtcNow - interval.StartTime).TotalSeconds >= interval.Duration)
            {
                interval.StartTime = DateTime.UtcNow;
                return true;
            }

            return false;
        }
    }


    class ClonedPlayerSystems
    {
        const string k_InitializeMessageSent = "mppm_InitializeMessageSent";

        // This is done because something like a EditorApplication.delayCall does not survive a domain reload
        // that could occur when going in or out of playmode
        const string k_FrameAfterPlaymodeMessage = "frameAfterPlaymodeMessage";

        readonly PlaymodeMessageQueue m_PlaymodeMessageQueue;

        internal PlaymodeEvents PlaymodeEvents { get; }

        internal ClonedPlayerApplicationEvents ClonedPlayerApplicationEvents { get; }

        internal ClonedPlayerSystems()
        {
            ClonedPlayerApplicationEvents = new ClonedPlayerApplicationEvents();
            PlaymodeEvents = new PlaymodeEvents();
            m_PlaymodeMessageQueue = new PlaymodeMessageQueue();
            EditorUtility.audioMasterMute = SystemDataStore.GetClone().GetMutePlayers();

            EditorWindow.windowFocusChanged += OnWindowChangedShouldBlock;
        }

        private void OnWindowChangedShouldBlock()
        {
            var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            foreach (var window in windows)
            {
                BlockWindowIfRequired(window);
            }
        }

        private void BlockWindowIfRequired(EditorWindow window)
        {
            if (window == null)
                return;

            // Compare if the active window is of a type should be allowed (whitelisted), Else close if not.
            var windowType = window.GetType().FullName;
            if (!LayoutFlagsUtil.IsWindowTypeSupported(windowType))
            {
                // Found a window to be Blocked - Close the Window.
                // Downstream Unity window components during an OnWindowChange pass relies on the window to
                // be opened to finalize the event. As such, have an async closure of the windows instead.
                Debug.LogWarning($"Attempted to open unsupported window in cloned player systems: {windowType}." +
                                 $"Please reach out on the forums and contact Unity Admins for more info.");

                EditorApplication.delayCall += window.Close;
            }
        }

        private bool CanClose()
        {
            // Save the current window before a close event occurs
            // (we can't save in time during onClose onFocus changes)
            EditorModesUtility.SaveCurrentWindow();
            return true;
        }

        internal void Listen(WorkflowCloneContext mppmContext, CloneContext vpContext)
        {
            /*
             * These system classes are simply an aggregation of logic and other events
             *
             * Its only purpose is to forward events to the Internal Runtimes, Workflows, and MultiplayerPlaymode (UI)
             */
            var messagingService = vpContext.MessagingService;

            ContainerWindowProxy.SetMppmCanCloseCallback(CanClose);

            EditorApplicationProxy.RegisterUpdateMainWindowTitle(applicationTitleDescriptor =>
            {
                ClonedPlayerApplicationEvents.InvokeEditorStarted(applicationTitleDescriptor);
            });
            EditorApplication.pauseStateChanged += pauseState =>
            {
                if (pauseState == PauseState.Paused && EditorApplication.isPlaying)
                {
                    // Note: Just handle any case (including pause on error)
                    // Where the player ends up paused.
                    // Pause all the other players as well
                    ClonedPlayerApplicationEvents.InvokeClonePlayerPaused();
                }
            };

            EditorApplication.focusChanged += (isFocused) =>
            {
                // On off-focus events, persist cached VP Window configurations
                // and positions across activations should they change.
                if (!isFocused)
                {
                    EditorModesUtility.SaveCurrentWindow();
                }
            };

            messagingService.Receive<SyncStateMessage>(message => ClonedPlayerApplicationEvents.InvokeSyncStateRequested(message.State));
            messagingService.Receive<PauseMessage>(_ => m_PlaymodeMessageQueue.AddEvent(PlayModeMessageTypes.Pause));
            messagingService.Receive<UnpauseMessage>(_ => m_PlaymodeMessageQueue.AddEvent(PlayModeMessageTypes.Unpause));
            messagingService.Receive<StepMessage>(_ => m_PlaymodeMessageQueue.AddEvent(PlayModeMessageTypes.Step));
            messagingService.Receive<PlayMessage>(_ => m_PlaymodeMessageQueue.AddEvent(PlayModeMessageTypes.Play));
            messagingService.Receive<StopMessage>(_ => m_PlaymodeMessageQueue.AddEvent(PlayModeMessageTypes.Stop));
            messagingService.Receive<OpenPlayerWindowMessage>(_ =>
            {
                EditorModesUtility.SwitchLayoutToMode(EditorApplication.isPlaying);
            });

            ConsoleWindowUtility.consoleLogsChanged += ClonedPlayerApplicationEvents.InvokeConsoleLogMessagesChanged;

            // Because we don't want to enter and exit playmode on the same frame (or immediately right after each other)
            // We instead wait on an interval to find a moment when the editor is not busy
            // At that moment we then process events in the order that they were added (with duplicates removed)
            var halfSecondInterval = new Interval { Duration = 0.5f, StartTime = DateTime.UtcNow, };
            var oneHalfSecondInterval = new Interval { Duration = 1.5f, StartTime = DateTime.UtcNow, };
            var initialTime = EditorApplication.timeSinceStartup;
            var systemDataStore = SystemDataStore.GetClone();
            Filters.FindFirstPlayerWithVirtualProjectsIdentifier(systemDataStore.LoadAllPlayerJson(),
                VirtualProjectsEditor.CloneIdentifier, out var player);
            EditorApplication.update += () =>
            {
                if (EditorApplication.timeSinceStartup - initialTime > .05 && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
                {
                    if (!SessionState.GetBool(k_InitializeMessageSent, false))
                    {
                        SessionState.SetBool(k_InitializeMessageSent, true);
                        messagingService.Broadcast(new PlayerInitializedMessage(VirtualProjectsEditor.CloneIdentifier));
                        ClonedPlayerApplicationEvents.InvokePlayerActive();
                    }
                }

                if (SessionState.GetBool(k_FrameAfterPlaymodeMessage, false))
                {
                    SessionState.SetBool(k_FrameAfterPlaymodeMessage, false);
                    ClonedPlayerApplicationEvents.InvokeFrameAfterPlaymodeMessage();
                }

                if (Interval.HasHitInterval(ref halfSecondInterval))
                {
                    ClonedPlayerApplicationEvents.InvokeUIPollUpdate();

                    var isEditorBusy = EditorApplication.isCompiling
                                       || EditorApplication.isUpdating
                                       || EditorApplication.isPlaying != EditorApplication.isPlayingOrWillChangePlaymode; // See https://docs.unity3d.com/ScriptReference/PlayModeStateChange.ExitingEditMode.html and https://docs.unity3d.com/ScriptReference/PlayModeStateChange.ExitingPlayMode.html
                    if (!isEditorBusy && m_PlaymodeMessageQueue.ReadEvent(out var pm))
                    {
                        switch (pm)
                        {
                            case PlayModeMessageTypes.Pause:
                                PlaymodeEvents.InvokePause();
                                break;
                            case PlayModeMessageTypes.Unpause:
                                PlaymodeEvents.InvokeUnpause();
                                break;
                            case PlayModeMessageTypes.Step:
                                PlaymodeEvents.InvokeStep();
                                break;
                            case PlayModeMessageTypes.Play:
                                PlaymodeEvents.InvokePlay();
                                SessionState.SetBool(k_FrameAfterPlaymodeMessage, true);
                                break;
                            case PlayModeMessageTypes.Stop:
                                PlaymodeEvents.InvokeStop();
                                SessionState.SetBool(k_FrameAfterPlaymodeMessage, true);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException($"{nameof(pm)}:{pm}");
                        }
                    }
                }
            };
        }
    }
}
