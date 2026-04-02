// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    class LocalPlayerController : PlayerController<LocalPlayerController.InstanceSettings>
    {
        const string k_TempLogsPath = Constants.k_TempRootPath + "ScenariosLogs/";

        [Serializable]
        public struct InstanceSettings
        {
            public BuildProfile BuildProfile;
            public bool StreamLogsToMainEditor;
            public Color LogsColor = new(0.3643f, 0.581f, 0.8679f);
            public string Arguments = "-screen-fullscreen 0 -screen-width 1024 -screen-height 720";

            public InstanceSettings()
            {
            }
        }

        [Serializable]
        internal struct UserSettings
        {
            public string DeviceID;
            public string DeviceName;
        }

        internal override string GetTypeNameForAnalytics() => "Local";

        protected internal override void SetupExecutionGraph(ExecutionGraphBuilder executionGraph)
        {
            var buildPath = ScenarioFactory.GenerateBuildPath(Settings.BuildProfile);
            // TODO: We need to share the build nodes between instances that share the same build profile and role.
            // The build nodes can be defined in the base PlayerController class. Move it once the graph builder is ready.
            var buildNode = executionGraph.AddNode<BuildPlayerNode>(ExecutionStage.Prepare);
            executionGraph.ConnectConstant(buildNode.BuildPath, buildPath);
            executionGraph.ConnectConstant(buildNode.Profile, Settings.BuildProfile);

            if (InternalUtilities.IsAndroidBuildTarget(Settings.BuildProfile))
            {
                SetupExecutionGraphForAdbProcess(executionGraph, buildNode);
            }
            else
            {
                SetupExecutionGraphForLocalProcess(executionGraph, buildNode);
            }
        }

        void SetupExecutionGraphForAdbProcess(ExecutionGraphBuilder executionGraph, BuildPlayerNode buildNode)
        {
            var installAdbNode = executionGraph.AddNode<AdbInstallNode>(ExecutionStage.Deploy);
            executionGraph.Connect(buildNode.ExecutablePath, installAdbNode.ApkPath);
            executionGraph.ConnectConstant(installAdbNode.DeviceName, GetUserSettings<UserSettings>().DeviceID);

            var packageName = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            var startAdbNode = executionGraph.AddNode<AdbStartProcessNode>(ExecutionStage.Start);
            executionGraph.ConnectConstant(startAdbNode.DeviceName, GetUserSettings<UserSettings>().DeviceID);
            executionGraph.ConnectConstant(startAdbNode.PackageName, packageName);
            executionGraph.ConnectConstant(startAdbNode.ActivityName, AdbUtilities.GetActivityName());

            var monitorAdbNode = executionGraph.AddNode<AdbMonitorProcessNode>(ExecutionStage.Run);
            executionGraph.Connect(startAdbNode.ProcessId, monitorAdbNode.ProcessId);
            executionGraph.ConnectConstant(monitorAdbNode.DeviceName,  GetUserSettings<UserSettings>().DeviceID);
            executionGraph.ConnectConstant(monitorAdbNode.PackageName, packageName);

            var stopAdbNode = executionGraph.AddNode<AdbStopProcessNode>(ExecutionStage.Cleanup);
            executionGraph.ConnectConstant(stopAdbNode.PackageName, packageName);
            executionGraph.ConnectConstant(stopAdbNode.DeviceName, GetUserSettings<UserSettings>().DeviceID);

            if (Settings.StreamLogsToMainEditor)
            {
                if (!Directory.Exists(k_TempLogsPath))
                    Directory.CreateDirectory(k_TempLogsPath);

                var logsFilePath = GenerateRandomLogFilePath();

                var logcatNode = executionGraph.AddNode<AdbLogcatNode>(ExecutionStage.Run);
                executionGraph.ConnectConstant(logcatNode.LogPath, logsFilePath);
                executionGraph.ConnectConstant(logcatNode.DeviceName, GetUserSettings<UserSettings>().DeviceID);
                executionGraph.Connect(startAdbNode.ProcessId, logcatNode.DeviceProcessId);

                var streamLogsNode = executionGraph.AddNode<StreamLogsFromFileNode>(ExecutionStage.Run);
                executionGraph.ConnectConstant(streamLogsNode.LogLabel, name);
                executionGraph.ConnectConstant(streamLogsNode.LogPath, logsFilePath);
                executionGraph.ConnectConstant(streamLogsNode.LogColor, Settings.LogsColor);
                executionGraph.Connect(logcatNode.ProcessId, streamLogsNode.ProcessId);

                var stopLogcatNode = executionGraph.AddNode<StopProcessNode>(ExecutionStage.Cleanup);
                executionGraph.Connect(logcatNode.ProcessId, stopLogcatNode.ProcessId);

                var deleteLogsNode = executionGraph.AddNode<DeleteFileNode>(ExecutionStage.Cleanup);
                executionGraph.ConnectConstant(deleteLogsNode.FilePath, logsFilePath);
            }
        }

        void SetupExecutionGraphForLocalProcess(ExecutionGraphBuilder executionGraph, BuildPlayerNode buildNode)
        {
            var arguments = Settings.Arguments ?? string.Empty;
            if (InternalUtilities.IsServerProfile(Settings.BuildProfile))
                arguments = CleanupScreenArguments(arguments);

            var logsFilePath = string.Empty;
            if (Settings.StreamLogsToMainEditor)
            {
                if (!Directory.Exists(k_TempLogsPath))
                    Directory.CreateDirectory(k_TempLogsPath);

                logsFilePath = GenerateRandomLogFilePath();
                arguments = AppendLogsArguments(arguments, logsFilePath);
            }

            var startProcessNode = executionGraph.AddNode<StartProcessNode>(ExecutionStage.Start);
            executionGraph.ConnectConstant(startProcessNode.Arguments, arguments);
            executionGraph.Connect(buildNode.ExecutablePath, startProcessNode.ExecutablePath);

            var monitorProcessNode = executionGraph.AddNode<MonitorProcessNode>(ExecutionStage.Run);
            executionGraph.Connect(startProcessNode.ProcessId, monitorProcessNode.ProcessId);

            var stopProcessNode = executionGraph.AddNode<StopProcessNode>(ExecutionStage.Cleanup);
            executionGraph.Connect(startProcessNode.ProcessId, stopProcessNode.ProcessId);

            if (Settings.StreamLogsToMainEditor)
            {
                var streamLogsNode = executionGraph.AddNode<StreamLogsFromFileNode>(ExecutionStage.Run);
                executionGraph.ConnectConstant(streamLogsNode.LogLabel, name);
                executionGraph.ConnectConstant(streamLogsNode.LogPath, logsFilePath);
                executionGraph.ConnectConstant(streamLogsNode.LogColor, Settings.LogsColor);
                executionGraph.Connect(startProcessNode.ProcessId, streamLogsNode.ProcessId);

                var deleteLogsNode = executionGraph.AddNode<DeleteFileNode>(ExecutionStage.Cleanup);
                executionGraph.ConnectConstant(deleteLogsNode.FilePath, logsFilePath);
            }
        }

        string AppendLogsArguments(string arguments, string logFilePath)
        {
            return $"{arguments} -logFile \"{logFilePath}\"";
        }

        string GenerateRandomLogFilePath()
        {
            // We produce a unique log file name to avoid conflicts between multiple instances of the same node
            var logFileName = $"{name.Replace(" ", "")}_{GenerateRandomString(8)}.log";
            return Path.Combine(Path.GetFullPath(k_TempLogsPath), logFileName);
        }

        static string GenerateRandomString(int count)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new System.Random();
            var randomString = new char[count];
            for (var i = 0; i < count; i++)
            {
                randomString[i] = chars[random.Next(chars.Length)];
            }
            return new string(randomString);
        }

        // TODO: UUM-50144 - There is currently a bug in windows dedicated server where screen related
        // arguments cause a crash. As a temporary workaround we detect that case and remove any
        // of those arguments that, in any case, take no effect on that platform.
        static string CleanupScreenArguments(string arguments)
        {
            // We need to remove -screen-fullscreen -screen-width and -screen-height arguments
            arguments = Regex.Replace(arguments, @"-screen-fullscreen\s+\d*", "");
            arguments = Regex.Replace(arguments, @"-screen-width\s+\d*", "");
            arguments = Regex.Replace(arguments, @"-screen-height\s+\d*", "");
            return arguments;
        }

        protected internal override VisualElement CreateControllerUI(Instance instance)
        {
            return new LocalPlayerInstanceStatusElement(instance, Settings, GetUserSettings<UserSettings>());
        }
    }
}
