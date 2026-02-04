// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text.RegularExpressions;
using UnityEditor.Build.Profile;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    class LocalPlayerController : PlayerController<LocalPlayerController, LocalPlayerController.InstanceSettings>
    {
        [Serializable]
        public struct InstanceSettings
        {
            public BuildProfile BuildProfile;
            public bool StreamLogsToMainEditor;
            public Color LogsColor = new(0.3643f, 0.581f, 0.8679f);
            public string Arguments = "-screen-fullscreen 0 -screen-width 1024 -screen-height 720";

            // TODO: These values shouldn't be stored here.
            // They are a per-user configuration and should be stored in EditorPrefs or similar.
            public string DeviceID;
            public string DeviceName;

            public InstanceSettings()
            {
            }
        }

        internal override string GetTypeNameForAnalytics() => "Local";

        protected internal override void SetupExecutionGraph(ExecutionGraph executionGraph)
        {
            // TODO: We need to share the build nodes between instances that share the same build profile and role.
            // The build nodes can be defined in the base PlayerController class. Move it once the graph builder is ready.
            var buildNode = new EditorBuildNode($"LocalPlayer - Build");
            executionGraph.AddNode(buildNode, ExecutionStage.Prepare);

            executionGraph.ConnectConstant(buildNode.BuildPath, ScenarioFactory.GenerateBuildPath(Settings.BuildProfile));
            executionGraph.ConnectConstant(buildNode.Profile, Settings.BuildProfile);


            // TODO: UUM-50144 - There is currently a bug in windows dedicated server where screen related
            // arguments cause a crash. As a temporary workaround we detect that case and remove any
            // of those arguments that, in any case, take no effect on that platform.
            var arguments = Settings.Arguments;
            if (InternalUtilities.IsServerProfile(Settings.BuildProfile))
            {
                arguments = CleanupScreenArguments(arguments);
            }

            if (InternalUtilities.IsAndroidBuildTarget(Settings.BuildProfile))
            {
                var deviceRunNode = new LocalDeviceRunNode($"LocalPlayer - Run");
                executionGraph.AddNode(deviceRunNode, ExecutionStage.Run);
                executionGraph.ConnectConstant(deviceRunNode.Arguments, arguments);
                executionGraph.ConnectConstant(deviceRunNode.StreamLogs, Settings.StreamLogsToMainEditor);
                executionGraph.ConnectConstant(deviceRunNode.LogsColor, Settings.LogsColor);
                executionGraph.ConnectConstant(deviceRunNode.DeviceName, Settings.DeviceID);

                executionGraph.Connect(buildNode.ExecutablePath, deviceRunNode.ExecutablePath);
                executionGraph.Connect(buildNode.BuildReport, deviceRunNode.BuildReport);
                return;
            }

            var localRunNode = new LocalRunNode($"LocalPlayer - Run");
            executionGraph.AddNode(localRunNode, ExecutionStage.Run);

            executionGraph.ConnectConstant(localRunNode.Arguments, arguments);
            executionGraph.ConnectConstant(localRunNode.StreamLogs, Settings.StreamLogsToMainEditor);
            executionGraph.ConnectConstant(localRunNode.LogsColor, Settings.LogsColor);
            executionGraph.Connect(buildNode.ExecutablePath, localRunNode.ExecutablePath);
        }

        private static string CleanupScreenArguments(string arguments)
        {
            // We need to remove -screen-fullscreen -screen-width and -screen-height arguments
            arguments = Regex.Replace(arguments, @"-screen-fullscreen\s+\d*", "");
            arguments = Regex.Replace(arguments, @"-screen-width\s+\d*", "");
            arguments = Regex.Replace(arguments, @"-screen-height\s+\d*", "");
            return arguments;
        }

        protected internal override VisualElement CreateControllerUI(Instance instance)
        {
            return new LocalPlayerInstanceStatusElement(instance, Settings);
        }
    }
}
