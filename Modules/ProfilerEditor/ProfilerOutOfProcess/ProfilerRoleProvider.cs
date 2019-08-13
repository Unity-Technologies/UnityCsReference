// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.ShortcutManagement;
using UnityEditorInternal;
using UnityEngine;
using Unity.MPE;

namespace UnityEditor
{
    static class ProfilerRoleProvider
    {
        const string k_RoleName = "profiler";

        internal enum EventType
        {
            UmpProfilerOpenPlayerConnection,  // Profiler > Editor: Sent when the profiler OOP is ready and wants the master editor process to enable its profile and player connection
            UmpProfilerRecordingStateChanged, // Profiler > Editor: Sent from the profiler OOP to have the master editor synchronize recording state changes in OOPP.
            UmpProfilerDeepProfileChanged,    // Profiler > Editor: Sent from the profiler OOP to have the master synchronize deep profile state and reload scripts.
            UmpProfilerMemRecordModeChanged,  // Profiler > Editor: Sent from the profiler OOP to have the master synchronize memory record mode.
            UmpProfilerCurrentFrameChanged,   // Profiler > Editor: The OOPP notifies the master editor that the user has selected a specific frame in the profiler chart.
            UmpProfilerRecordToggle,          // Editor > Profiler: The master editor requests the OOPP to start or end recording (i.e. used with F9)
            UmpProfilerSyncPlayState,         // Editor > Profiler: The master editor notifies the OOPP that the edit and play mode has changed.
            UmpProfilerSyncPlayPause,         // Editor > Profiler: The master editor notifies the OOPP that the play pause state has changed (e.g. stops temporarily the recording)
            UmpProfilerAboutToQuit,           // Editor > Profiler: The master editor notifies the OOPP that he needs to quit/exit.
            UmpProfilerPing,                  // Used for regression testing.
            UmpProfilerExit,                  // Used for regression testing.
            UmpProfilerRequestRecordState,    // Used for regression testing.
        }

        struct PlayerConnectionInfo
        {
            public int connectionId;
            public bool recording;
            public bool isPlaying;
        }

        // Represents the Profiler (slave) Out-Of-Process is launched by the MainEditorProcess and connects to its EventServer at startup.
        static class ProfilerProcess
        {
            static ProfilerWindow s_SlaveProfilerWindow;
            static string s_MasterPlayStateHint = string.Empty;
            static readonly Rect k_ProfilerProcessInitialWindowRect = new Rect(300, 90, 800, 600);

            [UsedImplicitly, RoleProvider(k_RoleName, ProcessEvent.UMP_EVENT_CREATE)]
            static void InitializeProfilerSlaveProcess()
            {
                s_SlaveProfilerWindow = ScriptableObject.CreateInstance<ProfilerWindow>();
                SetupProfilerWindow(s_SlaveProfilerWindow);

                var view = ScriptableObject.CreateInstance<HostView>();
                view.SetActualViewInternal(s_SlaveProfilerWindow, true);
                view.autoRepaintOnSceneChange = false;

                var cw = ScriptableObject.CreateInstance<ContainerWindow>();
                cw.m_DontSaveToLayout = true;
                cw.position = k_ProfilerProcessInitialWindowRect;
                cw.rootView = view;
                cw.rootView.position = new Rect(0, 0, cw.position.width, cw.position.height);
                cw.Show(ShowMode.MainWindow, loadPosition: true, displayImmediately: false, setFocus: true);

                EditorApplication.delayCall += SetupProfilerDriver;
            }

            [UsedImplicitly, RoleProvider(k_RoleName, ProcessEvent.UMP_EVENT_AFTER_DOMAIN_RELOAD)]
            static void InitializeProfilerSlaveProcessDomain()
            {
                if (EditorWindow.HasOpenInstances<ProfilerWindow>())
                {
                    s_SlaveProfilerWindow = EditorWindow.GetWindow<ProfilerWindow>();
                    SetupProfilerWindow(s_SlaveProfilerWindow);
                }

                EditorApplication.updateMainWindowTitle += SetProfilerWindowTitle;

                EventService.On(nameof(EventType.UmpProfilerRecordToggle), HandleToggleRecording);
                EventService.On(nameof(EventType.UmpProfilerSyncPlayState), HandleSyncMasterPlayModeState);
                EventService.On(nameof(EventType.UmpProfilerSyncPlayPause), HandleSyncMasterPlayPause);
                EventService.On(nameof(EventType.UmpProfilerPing), HandlePingEvent);
                EventService.On(nameof(EventType.UmpProfilerExit), HandleExitEvent);
                EventService.On(nameof(EventType.UmpProfilerRequestRecordState), HandleRequestRecordState);
            }

            static void SetupProfilerWindow(ProfilerWindow profilerWindow)
            {
                profilerWindow.SetRecordingEnabled(false);
                profilerWindow.currentFrameChanged -= OnProfilerCurrentFrameChanged;
                profilerWindow.recordingStateChanged -= OnProfilerWindowRecordingStateChanged;
                profilerWindow.deepProfileChanged -= OnProfilerWindowDeepProfileChanged;
                profilerWindow.memoryRecordingModeChanged -= OnProfilerWindowMemoryRecordModeChanged;

                profilerWindow.currentFrameChanged += OnProfilerCurrentFrameChanged;
                profilerWindow.recordingStateChanged += OnProfilerWindowRecordingStateChanged;
                profilerWindow.deepProfileChanged += OnProfilerWindowDeepProfileChanged;
                profilerWindow.memoryRecordingModeChanged += OnProfilerWindowMemoryRecordModeChanged;
            }

            static void SetupProfilerDriver()
            {
                if (Application.HasARGV("profilestartup"))
                    ProfilerDriver.SaveProfile(Application.dataPath + "/../profiler_start.raw");

                var profileSavePath = Application.GetValueForARGV("profileLoad");
                if (!string.IsNullOrEmpty(profileSavePath))
                {
                    ProfilerDriver.LoadProfile(profileSavePath, false);
                    ProfilerDriver.enabled = false;
                }
                else
                {
                    // Setup the profiler.
                    EventService.Request(5000L, nameof(EventType.UmpProfilerOpenPlayerConnection), HandlePlayerConnectionOpened);
                }
            }

            static void SetupProfiledConnection(int connId, bool recording, bool isPlaying)
            {
                ProfilerDriver.connectedProfiler = ProfilerDriver.GetAvailableProfilers().FirstOrDefault(id => id == connId);
                ProfilerDriver.profileEditor = !isPlaying;
                s_SlaveProfilerWindow.SetRecordingEnabled(recording);
                s_SlaveProfilerWindow.Repaint();
                EditorApplication.UpdateMainWindowTitle();
            }

            static void HandlePlayerConnectionOpened(Exception err, object[] args)
            {
                if (err != null)
                    throw err;
                var msg = args[0] as string;
                var info = JsonUtility.FromJson<PlayerConnectionInfo>(msg);
                EditorApplication.delayCall += () => SetupProfiledConnection(info.connectionId, info.recording, info.isPlaying);
            }

            static object HandleSyncMasterPlayPause(string eventType, object[] args)
            {
                var isPlaying = Convert.ToBoolean(args[0]);
                var paused = (PauseState)Convert.ToInt32(args[1]) == PauseState.Paused;
                if (isPlaying)
                {
                    ProfilerDriver.profileEditor = false;
                    s_SlaveProfilerWindow?.SetRecordingEnabled(!paused);
                }
                return null;
            }

            static object HandleSyncMasterPlayModeState(string eventType, object[] args)
            {
                var playState = (PlayModeStateChange)Convert.ToInt32(args[0]);
                if (playState == PlayModeStateChange.EnteredEditMode)
                {
                    s_MasterPlayStateHint = "EDIT";
                    ProfilerDriver.profileEditor = true;
                }
                else if (playState == PlayModeStateChange.EnteredPlayMode)
                {
                    s_MasterPlayStateHint = "PLAY";
                    ProfilerDriver.profileEditor = false;
                    s_SlaveProfilerWindow?.SetRecordingEnabled(true);
                }
                else if (playState == PlayModeStateChange.ExitingPlayMode)
                {
                    s_SlaveProfilerWindow?.SetRecordingEnabled(false);
                }
                return null;
            }

            static object HandleToggleRecording(string eventType, object[] args)
            {
                var isPlaying = Convert.ToBoolean(args[0]);

                s_SlaveProfilerWindow.Focus();
                ProfilerDriver.profileEditor = !isPlaying;
                s_SlaveProfilerWindow.SetRecordingEnabled(!ProfilerDriver.enabled);

                return s_SlaveProfilerWindow.IsRecording();
            }

            static void SetProfilerWindowTitle(ApplicationTitleDescriptor desc)
            {
                if (s_SlaveProfilerWindow == null)
                    return;
                if (s_SlaveProfilerWindow.IsRecording())
                    desc.title = $"Profiler [RECORDING {s_MasterPlayStateHint}] - {desc.projectName} - {desc.unityVersion}";
                else
                    desc.title = $"Profiler - {desc.projectName} - {desc.unityVersion}";
            }

            static void OnProfilerWindowRecordingStateChanged(bool recording)
            {
                EventService.Emit(nameof(EventType.UmpProfilerRecordingStateChanged),
                    recording,
                    ProfilerDriver.profileEditor);

                s_MasterPlayStateHint = !ProfilerDriver.profileEditor ? "PLAY" : "EDIT";
                EditorApplication.delayCall += EditorApplication.UpdateMainWindowTitle;
            }

            static void OnProfilerWindowDeepProfileChanged(bool deepProfiling)
            {
                EventService.Request(nameof(EventType.UmpProfilerDeepProfileChanged), (err, results) =>
                {
                    if (err != null)
                        throw err;
                    var applied = Convert.ToBoolean(results[0]);
                    if (!applied)
                        ProfilerDriver.deepProfiling = !deepProfiling;
                }, deepProfiling);
            }

            static void OnProfilerWindowMemoryRecordModeChanged(ProfilerMemoryRecordMode memRecordMode)
            {
                EventService.Emit(nameof(EventType.UmpProfilerMemRecordModeChanged), (int)memRecordMode);
            }

            static void OnProfilerCurrentFrameChanged(int frame, bool paused)
            {
                if (frame == -1)
                    return;
                EventService.Emit(nameof(EventType.UmpProfilerCurrentFrameChanged), frame, paused);
            }

            // Only used by tests
            static object HandlePingEvent(string type, object[] args)
            {
                return "Pong";
            }

            // Only used by tests
            static object HandleExitEvent(string type, object[] args)
            {
                EditorApplication.delayCall += () => EditorApplication.Exit(Convert.ToInt32(args[0]));
                return null;
            }

            static object HandleRequestRecordState(string type, object[] args)
            {
                return ProfilerDriver.enabled;
            }
        }

        // Represents the code running in the main (master) editor process.
        static class MainEditorProcess
        {
            [UsedImplicitly, RoleProvider(ProcessLevel.UMP_MASTER, ProcessEvent.UMP_EVENT_AFTER_DOMAIN_RELOAD)]
            static void InitializeProfilerMasterProcess()
            {
                EditorApplication.quitting += () =>
                {
                    if (!EventService.IsConnected)
                        return;

                    EventService.Emit(nameof(EventType.UmpProfilerExit), 0);
                    EventService.Tick(); // We really need the message to be sent now.
                };

                EditorApplication.pauseStateChanged += OnMasterPauseStateChanged;
                EditorApplication.playModeStateChanged += HandleMasterPlayModeChanged;

                EventService.On(nameof(EventType.UmpProfilerAboutToQuit), OnProfilerExited);
                EventService.On(nameof(EventType.UmpProfilerOpenPlayerConnection), OnOpenPlayerConnectionRequested);
                EventService.On(nameof(EventType.UmpProfilerCurrentFrameChanged), OnProfilerCurrentFrameChanged);
                EventService.On(nameof(EventType.UmpProfilerRecordingStateChanged), OnProfilerRecordingStateChanged);
                EventService.On(nameof(EventType.UmpProfilerDeepProfileChanged), OnProfilerDeepProfileChanged);
                EventService.On(nameof(EventType.UmpProfilerMemRecordModeChanged), OnProfilerMemoryRecordModeChanged);
            }

            [Shortcut("Profiling/Profiler/RecordToggle", KeyCode.F9)]
            static void RecordToggle()
            {
                if (ProcessService.level != ProcessLevel.UMP_MASTER ||
                    !ProcessService.IsChannelServiceStarted() ||
                    !EventService.IsConnected)
                    return;

                EventService.Request(250, nameof(EventType.UmpProfilerRecordToggle), (err, args) =>
                {
                    bool recording = false;
                    if (err == null)
                    {
                        // Recording was toggled by profiler OOP
                        recording = (bool)args[0];
                    }
                    else
                    {
                        if (!EditorWindow.HasOpenInstances<ProfilerWindow>())
                            return;

                        // Toggle profiling in-process
                        recording = !ProfilerDriver.enabled;
                        ProfilerDriver.profileEditor = !EditorApplication.isPlaying;
                        var profilerWindow = EditorWindow.GetWindow<ProfilerWindow>();
                        if (profilerWindow)
                            profilerWindow.SetRecordingEnabled(recording);
                        else
                            ProfilerDriver.enabled = recording;
                    }

                    if (recording)
                        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "Recording has started...");
                    else
                        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "Recording has ended.");
                }, EditorApplication.isPlaying);
            }

            static object OnProfilerCurrentFrameChanged(string eventType, object[] args)
            {
                var paused = Convert.ToBoolean(args[1]);
                if (paused)
                    EditorApplication.isPaused = true;
                return EditorApplication.isPaused;
            }

            static object OnProfilerRecordingStateChanged(string eventType, object[] args)
            {
                // Sync some profiler state on the profiled master editor connection.
                ProfilerDriver.profileEditor = Convert.ToBoolean(args[1]);
                ProfilerDriver.enabled = Convert.ToBoolean(args[0]);
                return null;
            }

            static object OnProfilerDeepProfileChanged(string eventType, object[] args)
            {
                var deep = Convert.ToBoolean(args[0]);
                var doApply = ProfilerWindow.SetEditorDeepProfiling(deep);
                return doApply;
            }

            static object OnProfilerMemoryRecordModeChanged(string eventType, object[] args)
            {
                ProfilerDriver.memoryRecordMode = (ProfilerMemoryRecordMode)Convert.ToInt32(args[0]);
                return null;
            }

            static void OnMasterPauseStateChanged(PauseState pauseState)
            {
                EventService.Emit(nameof(EventType.UmpProfilerSyncPlayPause), EditorApplication.isPlaying, (int)pauseState);
            }

            static object OnProfilerExited(string eventType, object[] args)
            {
                ProcessService.DisableProfileConnection();
                return null;
            }

            static object OnOpenPlayerConnectionRequested(string eventType, object[] args)
            {
                var playerConnectionInfo = new PlayerConnectionInfo
                {
                    connectionId = ProcessService.EnableProfileConnection(Application.dataPath),
                    recording = ProfilerDriver.enabled,
                    isPlaying = EditorApplication.isPlaying
                };
                if (EditorApplication.isPlaying)
                {
                    // Start profiling right away if we are in play mode
                    playerConnectionInfo.recording = true;
                }
                return JsonUtility.ToJson(playerConnectionInfo);
            }

            static void HandleMasterPlayModeChanged(PlayModeStateChange playState)
            {
                EventService.Emit(nameof(EventType.UmpProfilerSyncPlayState), (int)playState);
            }
        }

        internal static void LaunchProfilerSlave()
        {
            const string umpCap = "ump-cap";
            const string umpWindowTitleSwitch = "ump-window-title";

            ProcessService.LaunchSlave(k_RoleName,
                umpWindowTitleSwitch, "Profiler",
                umpCap, "minimal-load",             // Will skip some system loading to boot as fast as possible.
                umpCap, "disable-extra-resources");
        }
    }
}
