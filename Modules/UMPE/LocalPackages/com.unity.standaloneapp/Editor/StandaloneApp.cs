// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEditor.MPE;

namespace Unity.MPE
{
    static class StandaloneApp
    {
        const string k_RoleName = "com.unity.standaloneapp";

        enum EventType
        {
            UmpStarted,    // StandaloneApp > Editor : Indicates that the standalone application has started
            UmpHello,      // Editor > StandaloneApp : The editor says hello to the standalone application
            UmpExit,       // Editor > StandaloneApp : Shutdown app
        }

        static class SecondaryProcess
        {
            [RoleProvider(k_RoleName, ProcessEvent.AfterDomainReload)]
            internal static void InitializeSlaveProcessDomain()
            {
                Application.logMessageReceived -= EmitLog;
                Application.logMessageReceived += EmitLog;

                EventService.RegisterEventHandler(nameof(EventType.UmpHello), OnReceivedHello);
                EventService.RegisterEventHandler(nameof(EventType.UmpExit), OnExit);

                EditorApplication.delayCall -= StartApp;
                EditorApplication.delayCall += StartApp;

                //EmitLog("Initialized");
            }

            private static void StartApp()
            {
                //EmitLog("Started");
                EmitEvent(EventType.UmpStarted);

                var args = Environment.GetCommandLineArgs();
                if (Array.IndexOf(args, "-single-window") != -1)
                    CreateMainWindow();
            }

            private static void CreateMainWindow()
            {
                const System.Reflection.BindingFlags PV = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
                const System.Reflection.BindingFlags IV = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

                var HostView = typeof(EditorWindow).Assembly.GetType("UnityEditor.HostView");
                var ContainerWindow = typeof(EditorWindow).Assembly.GetType("UnityEditor.ContainerWindow");

                var view = ScriptableObject.CreateInstance(HostView);
                var window = ScriptableObject.CreateInstance(ContainerWindow);

                HostView.GetMethod("SetActualViewInternal", IV).Invoke(view, new object[] { ScriptableObject.CreateInstance<StandaloneWindow>(), true });

                ContainerWindow.GetField("m_DontSaveToLayout", IV).SetValue(window, true);
                ContainerWindow.GetProperty("position", PV).SetValue(window, new Rect(300, 90, 800, 600));
                ContainerWindow.GetProperty("rootView", PV).SetValue(window, view);
                ContainerWindow.GetProperty("title", PV).SetValue(window, "Standalone Application");
                ContainerWindow.GetMethod("Show", PV).Invoke(window, new object[] { 4, true, false, true });
            }

            private static void EmitLog(string msg, string stacktrace = null, LogType type = LogType.Log)
            {
                if (stacktrace != null)
                    EventService.Log($"[STANDALONE APP] {msg}: {stacktrace}");
                else
                    EventService.Log($"[STANDALONE APP] {type}: {msg}");
            }

            private static void EmitEvent(EventType type, object[] args = null)
            {
                EventService.Emit(type.ToString(), args);
            }

            private static void OnReceivedHello(string eventType, object[] args)
            {
                EmitLog($"Received {args[0]}");
            }

            private static void OnExit(string eventType, object[] args)
            {
                // EmitLog("Closing");

                var exitCode = Convert.ToInt32(args[0]);
                EditorApplication.delayCall += () => EditorApplication.Exit(exitCode);
            }
        }

        static class Master
        {
            static int launchProgressId
            {
                get => SessionState.GetInt(nameof(launchProgressId), -1);
                set => SessionState.SetInt(nameof(launchProgressId), value);
            }

            static int standaloneAppProcessId
            {
                get => SessionState.GetInt(nameof(standaloneAppProcessId), -1);
                set => SessionState.SetInt(nameof(standaloneAppProcessId), value);
            }

            [RoleProvider(ProcessLevel.Main, ProcessEvent.AfterDomainReload)]
            internal static void InitializeMasterProcess()
            {
                EventService.RegisterEventHandler(nameof(EventType.UmpStarted), OnStandaloneAppStarted);
            }

            private static void OnStandaloneAppStarted(string eventType, object[] args)
            {
                Progress.SetDescription(launchProgressId, $"Standalone application successfully launched ({standaloneAppProcessId})");
                Progress.Finish(launchProgressId, Progress.Status.Succeeded);
                launchProgressId = -1;
            }

            [MenuItem("Standalone App/Start (Editor Mode)", validate = true)]
            [MenuItem("Standalone App/Start (Single Window)", validate = true)]
            internal static bool StartStandaloneApplicationValidate()
            {
                return !IsStandaloneAppRunning();
            }

            [MenuItem("Standalone App/Start (Editor Mode)")]
            internal static void StartStandaloneApplication1()
            {
                launchProgressId = Progress.Start("Launching standalone app...", options: Progress.Options.Indefinite | Progress.Options.Sticky);
                standaloneAppProcessId = ProcessService.Launch(k_RoleName,
                    "ump-cap", "main_window",
                    "ump-cap", "menu_bar",
                    "ump-cap", "asset-import",
                    "ump-cap", "disable-extra-resources",
                    "editor-mode", "com.unity.standaloneapp");
            }

            [MenuItem("Standalone App/Start (Single Window)")]
            internal static void StartStandaloneApplication2()
            {
                launchProgressId = Progress.Start("Launching standalone window...", options: Progress.Options.Indefinite | Progress.Options.Sticky);
                standaloneAppProcessId = ProcessService.Launch(k_RoleName,
                    "single-window", "true",
                    "minimal-load", "true",
                    "ump-cap", "disable-extra-resources");
            }

            [MenuItem("Standalone App/Stop", validate = true)]
            [MenuItem("Standalone App/Say Hello", validate = true)]
            private static bool IsStandaloneAppRunning()
            {
                return standaloneAppProcessId != -1 && ProcessService.GetProcessState(standaloneAppProcessId) == ProcessState.Running;
            }

            [MenuItem("Standalone App/Stop")]
            internal static void StopStandaloneApplication()
            {
                EventService.Emit(nameof(EventType.UmpExit), 0);
                standaloneAppProcessId = -1;
            }

            [MenuItem("Standalone App/Say Hello")]
            internal static void SayHelloStandaloneApplication()
            {
                EventService.Emit(nameof(EventType.UmpHello), "Hello Standalone App!");
            }
        }
    }
}
