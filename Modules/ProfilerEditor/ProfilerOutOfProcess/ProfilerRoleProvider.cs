// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEditorInternal;
using UnityEditor.Profiling;
using UnityEngine;
using UnityEditor.MPE;

namespace UnityEditor
{
    static class ProfilerRoleProvider
    {
        const string k_RoleName = "profiler";

        static int OOPProcessId
        {
            get => SessionState.GetInt(nameof(OOPProcessId), -1);
            set => SessionState.SetInt(nameof(OOPProcessId), value);
        }

        internal enum EventType
        {
            UmpProfilerOpenPlayerConnection,  // Profiler > Editor: Sent when the profiler OOP is ready and wants the master editor process to enable its profile and player connection
            UmpProfilerRecordingStateChanged, // Profiler > Editor: Sent from the profiler OOP to have the master editor synchronize recording state changes in OOPP.
            UmpProfilerDeepProfileChanged,    // Profiler > Editor: Sent from the profiler OOP to have the master synchronize deep profile state and reload scripts.
            UmpProfilerMemRecordModeChanged,  // Profiler > Editor: Sent from the profiler OOP to have the master synchronize memory record mode.
            UmpProfilerCurrentFrameChanged,   // Profiler > Editor: The OOPP notifies the master editor that the user has selected a specific frame in the profiler chart.
            UmpProfilerRecordToggle,          // Editor > Profiler: The master editor requests the OOPP to start or end recording (i.e. used with F9)
            UmpProfilerAboutToQuit,           // Editor > Profiler: The master editor notifies the OOPP that he needs to quit/exit.
            UmpProfilerExit,                  // Editor > Profiler: Request the OOPP to exit (used when the main editor is about to quit)
            UmpProfilerEnteredPlayMode,       // Editor > Profiler: Notification about entering Play Mode (used for ClearOnPlay functionality)
            UmpProfilerPing,                  // Used for regression testing.
            UmpProfilerRequestRecordState,    // Used for regression testing.
            UmpProfilerFocus                  // Used to focus the OOPP when a user tries to open a second instance
        }

        [Serializable]
        struct PlayerConnectionInfo
        {
            public bool recording;
            public bool profileEditor;
        }

        // Represents the Profiler Out-Of-Process is launched by the MainEditorProcess and connects to its EventServer at startup.
        static class ProfilerProcess
        {
            static bool s_ProfilerDriverSetup = false;
            static ProfilerWindow s_OOPProfilerWindow;
            static string userPrefProfilerLayoutPath = Path.Combine(WindowLayout.layoutsDefaultModePreferencesPath, "Profiler.dwlt");
            static string systemProfilerLayoutPath = Path.Combine(EditorApplication.applicationContentsPath, "Resources/Layouts/Profiler.dwlt");

            [UsedImplicitly, RoleProvider(k_RoleName, ProcessEvent.Create)]
            static void InitializeProfilerProcess()
            {
                if (!File.Exists(userPrefProfilerLayoutPath) ||
                    File.GetLastWriteTime(systemProfilerLayoutPath) > File.GetLastWriteTime(userPrefProfilerLayoutPath))
                {
                    var parentDir = Path.GetDirectoryName(userPrefProfilerLayoutPath);
                    if (parentDir != null && !System.IO.Directory.Exists(parentDir))
                        System.IO.Directory.CreateDirectory(parentDir);
                    File.Copy(systemProfilerLayoutPath, userPrefProfilerLayoutPath, true);
                }
                WindowLayout.TryLoadWindowLayout(userPrefProfilerLayoutPath, false);

                SessionState.SetBool("OOPP.Initialized", true);
                EditorApplication.CallDelayed(InitializeProfilerProcessDomain);

                Console.WriteLine("[UMPE] Initialize Profiler Out of Process");
            }

            [UsedImplicitly, RoleProvider(k_RoleName, ProcessEvent.AfterDomainReload)]
            static void InitializeProfilerProcessDomain()
            {
                Console.WriteLine("[UMPE] Initialize Profiler Secondary Process Domain Triggered");

                if (!SessionState.GetBool("OOPP.Initialized", false))
                    return;

                s_OOPProfilerWindow = EditorWindow.GetWindow<ProfilerWindow>();
                SetupProfilerWindow(s_OOPProfilerWindow);

                EventService.RegisterEventHandler(nameof(EventType.UmpProfilerRecordToggle), HandleToggleRecording);
                EventService.RegisterEventHandler(nameof(EventType.UmpProfilerRequestRecordState), HandleRequestRecordState);
                EventService.RegisterEventHandler(nameof(EventType.UmpProfilerPing), HandlePingEvent);
                EventService.RegisterEventHandler(nameof(EventType.UmpProfilerExit), HandleExitEvent);
                EventService.RegisterEventHandler(nameof(EventType.UmpProfilerFocus), HandleFocus);
                EventService.RegisterEventHandler(nameof(EventType.UmpProfilerEnteredPlayMode), HandleEnteredPlayMode);

                EditorApplication.CallDelayed(SetupProfilerDriver);
                EditorApplication.updateMainWindowTitle -= SetProfilerWindowTitle;
                EditorApplication.updateMainWindowTitle += SetProfilerWindowTitle;
                EditorApplication.quitting -= SaveWindowLayout;
                EditorApplication.quitting += SaveWindowLayout;

                Console.WriteLine("[UMPE] Initialize Profiler Secondary Process Domain Completed");
            }

            static void SaveWindowLayout()
            {
                WindowLayout.SaveWindowLayout(userPrefProfilerLayoutPath);
            }

            static void SetupProfilerWindow(ProfilerWindow profilerWindow)
            {
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
                if (s_ProfilerDriverSetup)
                    return;

                ProfilerDriver.profileEditor = ProfilerUserSettings.defaultTargetMode == ProfilerEditorTargetMode.Editmode;
                var playerConnectionInfo = new PlayerConnectionInfo
                {
                    recording = ProfilerDriver.enabled,
                    profileEditor = ProfilerDriver.profileEditor
                };
                EventService.Request(nameof(EventType.UmpProfilerOpenPlayerConnection), HandlePlayerConnectionOpened, playerConnectionInfo, 5000L);
                s_ProfilerDriverSetup = true;
            }

            static void SetupProfiledConnection(int connId)
            {
                if (ProfilerDriver.GetAvailableProfilers().Any(id => id == connId))
                {
                    ProfilerDriver.SetRemoteEditorConnection(connId);
                    Menu.SetChecked("Edit/Record", s_OOPProfilerWindow.IsSetToRecord());
                    Menu.SetChecked("Edit/Deep Profiling", ProfilerDriver.deepProfiling);
                    EditorApplication.UpdateMainWindowTitle();
                    InternalEditorUtility.RepaintAllViews();
                }
                else
                    EditorApplication.CallDelayed(() => SetupProfiledConnection(connId), 0.250d);
            }

            static void HandlePlayerConnectionOpened(Exception err, object[] args)
            {
                if (err != null)
                    throw err;
                var connectionId = Convert.ToInt32(args[0]);
                SetupProfiledConnection(connectionId);
            }

            static object HandleToggleRecording(string eventType, object[] args)
            {
                s_OOPProfilerWindow.Focus();
                s_OOPProfilerWindow.SetRecordingEnabled(!ProfilerDriver.enabled);
                InternalEditorUtility.RepaintAllViews();
                return s_OOPProfilerWindow.IsSetToRecord();
            }

            static void SetProfilerWindowTitle(ApplicationTitleDescriptor desc)
            {
                if (s_OOPProfilerWindow == null)
                    return;
                if (s_OOPProfilerWindow.IsRecording())
                {
                    var playStateHint = !ProfilerDriver.profileEditor ? "PLAY" : "EDIT";
                    desc.title = $"Profiler [RECORDING {playStateHint}] - {desc.projectName} - {desc.unityVersion}";
                }
                else
                    desc.title = $"Profiler - {desc.projectName} - {desc.unityVersion}";
            }

            static void OnProfilerWindowRecordingStateChanged(bool recording)
            {
                EventService.Emit(nameof(EventType.UmpProfilerRecordingStateChanged), new object[] { recording, ProfilerDriver.profileEditor });
                EditorApplication.CallDelayed(EditorApplication.UpdateMainWindowTitle);
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

                    Menu.SetChecked("Edit/Deep Profiling", ProfilerDriver.deepProfiling);
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
                EventService.Emit(nameof(EventType.UmpProfilerCurrentFrameChanged), new object[] { frame, paused });
            }

            // Only used by tests
            static object HandlePingEvent(string type, object[] args)
            {
                return s_OOPProfilerWindow ? "Pong" : "";
            }

            static object HandleExitEvent(string type, object[] args)
            {
                EditorApplication.CallDelayed(() => EditorApplication.Exit(Convert.ToInt32(args[0])));
                return null;
            }

            static object HandleRequestRecordState(string type, object[] args)
            {
                return ProfilerDriver.enabled;
            }

            static void HandleFocus(string type, object[] args)
            {
                EditorWindow.FocusWindowIfItsOpen(typeof(ProfilerWindow));
            }

            static void HandleEnteredPlayMode(string arg1, object[] arg2)
            {
                s_OOPProfilerWindow.ClearFramesOnPlayOrPlayerConnectionChange();
            }

            [UsedImplicitly, CommandHandler("Profiler/OpenProfileData", CommandHint.Menu)]
            static void OnLoadProfileDataFileCommand(CommandExecuteContext ctx)
            {
                s_OOPProfilerWindow.LoadProfilingData(false);
            }

            [UsedImplicitly, CommandHandler("Profiler/SaveProfileData", CommandHint.Menu)]
            static void OnSaveProfileDataFileCommand(CommandExecuteContext ctx)
            {
                s_OOPProfilerWindow.SaveProfilingData();
            }

            [UsedImplicitly, CommandHandler("Profiler/Record", CommandHint.Menu)]
            static void OnRecordCommand(CommandExecuteContext ctx)
            {
                s_OOPProfilerWindow.SetRecordingEnabled(!s_OOPProfilerWindow.IsSetToRecord());
                Menu.SetChecked("Edit/Record", s_OOPProfilerWindow.IsSetToRecord());
            }

            [UsedImplicitly, CommandHandler("Profiler/EnableDeepProfiling", CommandHint.Menu)]
            static void OnEnableDeepProfilingCommand(CommandExecuteContext ctx)
            {
                OnProfilerWindowDeepProfileChanged(ProfilerDriver.deepProfiling);
            }
        }

        // Represents the code running in the main (master) editor process.
        static class MainEditorProcess
        {
            [UsedImplicitly, RoleProvider(ProcessLevel.Main, ProcessEvent.AfterDomainReload)]
            static void InitializeProfilerMasterProcess()
            {
                EditorApplication.playModeStateChanged += (PlayModeStateChange state) =>
                {
                    if (state != PlayModeStateChange.EnteredPlayMode || !EventService.isConnected)
                        return;

                    EventService.Emit(nameof(EventType.UmpProfilerEnteredPlayMode));
                    EventService.Tick(); // We really need the message to be sent now.
                };

                EditorApplication.quitting += () =>
                {
                    if (!EventService.isConnected)
                        return;

                    EventService.Emit(nameof(EventType.UmpProfilerExit), 0);
                    EventService.Tick(); // We really need the message to be sent now.
                };

                EventService.RegisterEventHandler(nameof(EventType.UmpProfilerAboutToQuit), OnProfilerExited);
                EventService.RegisterEventHandler(nameof(EventType.UmpProfilerOpenPlayerConnection), OnOpenPlayerConnectionRequested);
                EventService.RegisterEventHandler(nameof(EventType.UmpProfilerCurrentFrameChanged), OnProfilerCurrentFrameChanged);
                EventService.RegisterEventHandler(nameof(EventType.UmpProfilerRecordingStateChanged), OnProfilerRecordingStateChanged);
                EventService.RegisterEventHandler(nameof(EventType.UmpProfilerDeepProfileChanged), OnProfilerDeepProfileChanged);
                EventService.RegisterEventHandler(nameof(EventType.UmpProfilerMemRecordModeChanged), OnProfilerMemoryRecordModeChanged);
            }

            [UsedImplicitly, CommandHandler("ProfilerRecordToggle", CommandHint.Shortcut)]
            static void RecordToggle(CommandExecuteContext context)
            {
                if (ProcessService.level == ProcessLevel.Main &&
                    ProcessService.IsChannelServiceStarted() &&
                    ProcessService.GetProcessState(OOPProcessId) == ProcessState.Running &&
                    EventService.isConnected)
                {
                    EventService.Request(nameof(EventType.UmpProfilerRecordToggle), (err, args) =>
                    {
                        bool recording = false;
                        if (err == null)
                        {
                            // Recording was toggled by profiler OOP
                            recording = (bool)args[0];
                            ProfilerDriver.enabled = recording;
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
                    }, 250);

                    context.result = true;
                }
                else
                {
                    context.result = false;
                }
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

            static object OnProfilerExited(string eventType, object[] args)
            {
                ProcessService.DisableProfileConnection();
                return null;
            }

            static object OnOpenPlayerConnectionRequested(string eventType, object[] args)
            {
                var info = (PlayerConnectionInfo)args[0];
                var connectionId = ProcessService.EnableProfileConnection(Application.dataPath);
                ProfilerDriver.enabled = info.recording;
                ProfilerDriver.profileEditor = info.profileEditor;
                return connectionId;
            }
        }

        internal static bool IsRunning()
        {
            if (OOPProcessId == -1)
                return false;
            return ProcessService.GetProcessState(OOPProcessId) == ProcessState.Running;
        }

        internal static int LaunchProfilerProcess()
        {
            if (IsRunning())
            {
                Debug.LogWarning("Profiler (Standalone Process) already launched. Focusing window.");
                EventService.Emit(nameof(EventType.UmpProfilerFocus), null);
                return OOPProcessId;
            }

            const string umpCap = "ump-cap";
            const string umpWindowTitleSwitch = "ump-window-title";
            OOPProcessId = ProcessService.Launch(k_RoleName,
                umpWindowTitleSwitch, "Profiler",
                umpCap, "disable-extra-resources",
                umpCap, "menu_bar",
                "editor-mode", k_RoleName,
                "disableManagedDebugger", "true");
            return OOPProcessId;
        }
    }
}
