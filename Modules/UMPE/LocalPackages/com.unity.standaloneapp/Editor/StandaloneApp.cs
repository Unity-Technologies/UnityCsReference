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

        }
    }
}
