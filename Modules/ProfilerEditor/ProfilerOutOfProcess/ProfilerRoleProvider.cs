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

        static int s_SlaveProcessId = -1;

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
            UmpProfilerPing,                  // Used for regression testing.
            UmpProfilerRequestRecordState,    // Used for regression testing.
        }

        [Serializable]
        struct PlayerConnectionInfo
        {
            public bool recording;
            public bool profileEditor;
        }

        // Represents the Profiler (slave) Out-Of-Process is launched by the MainEditorProcess and connects to its EventServer at startup.
        static class ProfilerProcess
        {
            static bool s_ProfilerDriverSetup = false;
            static ProfilerWindow s_SlaveProfilerWindow;
            static string userPrefProfilerLayoutPath = Path.Combine(WindowLayout.layoutsDefaultModePreferencesPath, "Profiler.dwlt");
            static string systemProfilerLayoutPath = Path.Combine(EditorApplication.applicationContentsPath, "Resources/Layouts/Profiler.dwlt");

            [UsedImplicitly, RoleProvider(k_RoleName, ProcessEvent.Create)]
            static void InitializeProfilerSlaveProcess()
            {
                if (!File.Exists(userPrefProfilerLayoutPath) ||
                    File.GetLastWriteTime(systemProfilerLayoutPath) > File.GetLastWriteTime(userPrefProfilerLayoutPath))
                {
                    var parentDir = Path.GetDirectoryName(userPrefProfilerLayoutPath);
                    if (parentDir != null && !System.IO.Directory.Exists(parentDir))
                        System.IO.Directory.CreateDirectory(parentDir);
                    File.Copy(systemProfilerLayoutPath, userPrefProfilerLayoutPath, true);
                }
                WindowLayout.LoadWindowLayout(userPrefProfilerLayoutPath, false);

                SessionState.SetBool("OOPP.Initialized", true);
                EditorApplication.update -= InitializeProfilerSlaveProcessDomain;
                EditorApplication.update += InitializeProfilerSlaveProcessDomain;

                Console.WriteLine("[UMPE] Initialize Profiler Slave Process");
            }

            [UsedImplicitly, RoleProvider(k_RoleName, ProcessEvent.AfterDomainReload)]
            static void InitializeProfilerSlaveProcessDomain()
            {
                Console.WriteLine("[UMPE] Initialize Profiler Slave Process Domain Triggered");

                if (!SessionState.GetBool("OOPP.Initialized", false))
                    return;

                EditorApplication.update -= InitializeProfilerSlaveProcessDomain;

                s_SlaveProfilerWindow = EditorWindow.GetWindow<ProfilerWindow>();
                SetupProfilerWindow(s_SlaveProfilerWindow);

                EventService.RegisterEventHandler(nameof(EventType.UmpProfilerRecordToggle), HandleToggleRecording);
                EventService.RegisterEventHandler(nameof(EventType.UmpProfilerRequestRecordState), HandleRequestRecordState);
                EventService.RegisterEventHandler(nameof(EventType.UmpProfilerPing), HandlePingEvent);
                EventService.RegisterEventHandler(nameof(EventType.UmpProfilerExit), HandleExitEvent);

                EditorApplication.update -= SetupProfilerDriver;
                EditorApplication.update += SetupProfilerDriver;
                EditorApplication.updateMainWindowTitle -= SetProfilerWindowTitle;
                EditorApplication.updateMainWindowTitle += SetProfilerWindowTitle;
                EditorApplication.quitting -= SaveWindowLayout;
                EditorApplication.quitting += SaveWindowLayout;

                Console.WriteLine("[UMPE] Initialize Profiler Slave Process Domain Completed");
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
                EditorApplication.update -= SetupProfilerDriver;

                if (s_ProfilerDriverSetup)
                    return;

                ProfilerDriver.profileEditor = ProfilerUserSettings.defaultTargetMode == ProfilerEditorTargetMode.Editmode;
                var playerConnectionInfo = new PlayerConnectionInfo
                {
                    recording = ProfilerDriver.enabled,
                    profileEditor = ProfilerDriver.profileEditor
                };
                if (!SessionState.GetBool("OOPP.PlayerConnectionOpened", false))
                    EventService.Request(nameof(EventType.UmpProfilerOpenPlayerConnection), HandlePlayerConnectionOpened, playerConnectionInfo, 5000L);
                s_ProfilerDriverSetup = true;

                ModeService.RefreshMenus();
            }

            static void SetupProfiledConnection(int connId)
            {
                ProfilerDriver.connectedProfiler = ProfilerDriver.GetAvailableProfilers().FirstOrDefault(id => id == connId);
                Menu.SetChecked("Edit/Record", s_SlaveProfilerWindow.IsSetToRecord());
                Menu.SetChecked("Edit/Deep Profiling", ProfilerDriver.deepProfiling);
                EditorApplication.UpdateMainWindowTitle();
                s_SlaveProfilerWindow.Repaint();
            }

            static void HandlePlayerConnectionOpened(Exception err, object[] args)
            {
                if (err != null)
                    throw err;
                var connectionId = Convert.ToInt32(args[0]);
                EditorApplication.delayCall += () => SetupProfiledConnection(connectionId);
                SessionState.SetBool("OOPP.PlayerConnectionOpened", true);
            }

            static object HandleToggleRecording(string eventType, object[] args)
            {
                s_SlaveProfilerWindow.Focus();
                s_SlaveProfilerWindow.SetRecordingEnabled(!ProfilerDriver.enabled);
                InternalEditorUtility.RepaintAllViews();
                return s_SlaveProfilerWindow.IsSetToRecord();
            }

            static void SetProfilerWindowTitle(ApplicationTitleDescriptor desc)
            {
                if (s_SlaveProfilerWindow == null)
                    return;
                if (s_SlaveProfilerWindow.IsRecording())
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
                return s_SlaveProfilerWindow ? "Pong" : "";
            }

            static object HandleExitEvent(string type, object[] args)
            {
                EditorApplication.delayCall += () => EditorApplication.Exit(Convert.ToInt32(args[0]));
                return null;
            }

            static object HandleRequestRecordState(string type, object[] args)
            {
                return ProfilerDriver.enabled;
            }

            [UsedImplicitly, CommandHandler("Profiler/OpenProfileData", CommandHint.Menu)]
            static void OnLoadProfileDataFileCommand(CommandExecuteContext ctx)
            {
                s_SlaveProfilerWindow.LoadProfilingData(false);
            }

            [UsedImplicitly, CommandHandler("Profiler/SaveProfileData", CommandHint.Menu)]
            static void OnSaveProfileDataFileCommand(CommandExecuteContext ctx)
            {
                s_SlaveProfilerWindow.SaveProfilingData();
            }

            [UsedImplicitly, CommandHandler("Profiler/Record", CommandHint.Menu)]
            static void OnRecordCommand(CommandExecuteContext ctx)
            {
                s_SlaveProfilerWindow.SetRecordingEnabled(!s_SlaveProfilerWindow.IsSetToRecord());
                Menu.SetChecked("Edit/Record", s_SlaveProfilerWindow.IsSetToRecord());
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
            [UsedImplicitly, RoleProvider(ProcessLevel.Master, ProcessEvent.AfterDomainReload)]
            static void InitializeProfilerMasterProcess()
            {
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
                if (ProcessService.level == ProcessLevel.Master &&
                    ProcessService.IsChannelServiceStarted() &&
                    ProcessService.GetSlaveProcessState(s_SlaveProcessId) == ProcessState.Running &&
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
            if (s_SlaveProcessId == -1)
                return false;
            return ProcessService.GetSlaveProcessState(s_SlaveProcessId) == ProcessState.Running;
        }

        internal static int LaunchProfilerSlave()
        {
            if (IsRunning())
            {
                Debug.LogWarning($"You've already launched the profiler out-of-process ({s_SlaveProcessId}), please wait a few seconds...");
                return s_SlaveProcessId;
            }

            const string umpCap = "ump-cap";
            const string umpWindowTitleSwitch = "ump-window-title";
            s_SlaveProcessId = ProcessService.LaunchSlave(k_RoleName,
                umpWindowTitleSwitch, "Profiler",
                umpCap, "disable-extra-resources",
                umpCap, "menu_bar",
                "editor-mode", k_RoleName,
                "disableManagedDebugger", "true");
            return s_SlaveProcessId;
        }
    }
}
