// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    class LocalPlayerController : PlayerController<LocalPlayerController, LocalInstanceDescription>
    {
        private bool m_HasEditorInstance;
        internal bool HasEditorInstance
        {
            get => m_HasEditorInstance;
            set => m_HasEditorInstance = value;
        }

        protected internal override void SetupExecutionGraph(ExecutionGraph executionGraph)
        {
            // TODO: We need to share the build nodes between instances that share the same build profile and role.
            // The build nodes can be defined in the base PlayerController class. Move it once the graph builder is ready.
            var buildNode = new EditorBuildNode($"{Settings.Name} - Build");
            executionGraph.AddNode(buildNode, ExecutionStage.Prepare);

            executionGraph.ConnectConstant(buildNode.BuildPath, ScenarioFactory.GenerateBuildPath(Settings.BuildProfile));
            executionGraph.ConnectConstant(buildNode.Profile, Settings.BuildProfile);


            // TODO: UUM-50144 - There is currently a bug in windows dedicated server where screen related
            // arguments cause a crash. As a temporary workaround we detect that case and remove any
            // of those arguments that, in any case, take no effect on that platform.
            var arguments = Settings.GetCurrentModeArguments();
            if (InternalUtilities.IsServerProfile(Settings.BuildProfile))
            {
                arguments = CleanupScreenArguments(arguments);
            }

            if (LocalDeploymentUtility.IsServerProfileOrRole(Settings.BuildProfile))
            {
                arguments = SetupServerCliArguments(arguments, Settings.ServerSettings.CliSettings);
            }

            if (InternalUtilities.IsAndroidBuildTarget(Settings.BuildProfile))
            {
                var deviceRunNode = new LocalDeviceRunNode($"{Settings.Name} - Run");
                executionGraph.AddNode(deviceRunNode, ExecutionStage.Run);
                executionGraph.ConnectConstant(deviceRunNode.Arguments, arguments);
                executionGraph.ConnectConstant(deviceRunNode.StreamLogs, Settings.AdvancedConfiguration.StreamLogsToMainEditor);
                executionGraph.ConnectConstant(deviceRunNode.LogsColor, Settings.AdvancedConfiguration.LogsColor);
                executionGraph.ConnectConstant(deviceRunNode.DeviceName, Settings.AdvancedConfiguration.DeviceID);

                executionGraph.Connect(buildNode.ExecutablePath, deviceRunNode.ExecutablePath);
                executionGraph.Connect(buildNode.BuildReport, deviceRunNode.BuildReport);

                // [TODO]: We need to remove this line, since 1 instance could have multiple nodes
                Settings.CorrespondingNodeId = deviceRunNode.Name;

                Settings.SetCorrespondingNodes(buildNode, deviceRunNode);
                return;
            }

            var localRunNode = new LocalRunNode($"{Settings.Name} - Run");
            executionGraph.AddNode(localRunNode, ExecutionStage.Run);

            executionGraph.ConnectConstant(localRunNode.Arguments, arguments);
            executionGraph.ConnectConstant(localRunNode.StreamLogs, Settings.AdvancedConfiguration.StreamLogsToMainEditor);
            executionGraph.ConnectConstant(localRunNode.LogsColor, Settings.AdvancedConfiguration.LogsColor);
            executionGraph.Connect(buildNode.ExecutablePath, localRunNode.ExecutablePath);

            // [TODO]: We need to remove this line, since 1 instance could have multiple nodes
            Settings.CorrespondingNodeId = localRunNode.Name;

            var nodes = new List<Node>()
            {
                buildNode,
                localRunNode
            };
            nodes.AddRange(LocalDeploymentUtility.SetupExecutionGraph(Settings, executionGraph, m_HasEditorInstance));
            Settings.SetCorrespondingNodes(nodes.ToArray());
        }

        private static string CleanupScreenArguments(string arguments)
        {
            // We need to remove -screen-fullscreen -screen-width and -screen-height arguments
            arguments = Regex.Replace(arguments, @"-screen-fullscreen\s+\d*", "");
            arguments = Regex.Replace(arguments, @"-screen-width\s+\d*", "");
            arguments = Regex.Replace(arguments, @"-screen-height\s+\d*", "");
            return arguments;
        }

        private static string SetupServerCliArguments(string arguments, ServerCliSettings serverCliSettings)
        {
            serverCliSettings.ComputeFinalArguments(out var port, out var queryType, out var queryPort);

            if (!Regex.IsMatch(arguments, @"-port\s+"))
                arguments += $" -port {port}";

            if (!Regex.IsMatch(arguments, @"-querytype\s+"))
                arguments += $" -querytype {GetQueryTypeString(queryType)}";

            if (!Regex.IsMatch(arguments, @"-queryport\s+"))
                arguments += $" -queryport {queryPort}";

            return arguments;
        }

        private static string GetQueryTypeString(SimulatorSettings.ProtocolType queryProtocol)
            => queryProtocol.ToString().ToLowerInvariant();

        /// <summary>
        /// Validates whether the current Unity project is properly set up for running a simulated local instance
        /// This includes:
        /// 1. Checking if the project is linked to Unity Cloud - has a valid cloud project ID
        /// 2. Verifying the existence of a valid Unity Cloud Environment ID - has a valid environment ID
        /// Returns a failed ValidationResult if any step is not properly configured.
        /// </summary>
        protected internal override async Task<Scenario.ValidationResult> ValidateForRunningAsync(CancellationToken cancellationToken)
        {
            if (!LocalDeploymentUtility.IsLocalDeploymentAvailable() || Settings.ServerSettings.DeployMode != ServerSettings.ServerDeployMode.Simulated)
                return new Scenario.ValidationResult(true, string.Empty);

            try
            {
                await IPlayModeServices.Instance.SetupSimEnvironmentAsync(cancellationToken);
            }
            catch (Exception e)
            {
                return new Scenario.ValidationResult(false, e.Message);
            }

            return new Scenario.ValidationResult(true, string.Empty);
        }

        protected internal override VisualElement CreateControllerUI()
        {
            return new CommonInstanceStatusElement(Settings);
        }
    }
}
