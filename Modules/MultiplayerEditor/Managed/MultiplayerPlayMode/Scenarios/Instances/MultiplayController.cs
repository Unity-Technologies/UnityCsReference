// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Assertions;
using UnityEngine.Multiplayer.Internal;
using UnityEngine.UIElements;
using static Unity.Multiplayer.PlayMode.Editor.ScenarioFactory;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    class MultiplayController : PlayerController<MultiplayController, RemoteInstanceDescription>
    {
        protected internal override void SetupExecutionGraph(ExecutionGraph executionGraph)
        {
            var role = ScenarioFactory.GetRoleForInstance(Settings);

            // We assume the remote instance is a server.
            Assert.AreEqual(role, MultiplayerRoleFlags.Server);

            var buildPath = ScenarioFactory.GenerateBuildPath(Settings.BuildProfile);

            var buildNode = new EditorBuildNode($"{Settings.Name} {RemoteNodeConstants.k_BuildNodePostFix}");
            executionGraph.AddNode(buildNode, ExecutionStage.Prepare);
            executionGraph.ConnectConstant(buildNode.BuildPath, buildPath);
            executionGraph.ConnectConstant(buildNode.Profile, Settings.BuildProfile);


            var advancedConfiguration = Settings.AdvancedConfiguration;
            var multiplayName = RemoteInstanceDescription.ComputeMultiplayName(advancedConfiguration.Identifier);
            var buildName = multiplayName;
            var buildConfigurationName = multiplayName;
            var fleetName = multiplayName;

            var deployBuildNode = new DeployBuildNode($"{Settings.Name} {RemoteNodeConstants.k_DeployBuildNodePostfix}");
            executionGraph.AddNode(deployBuildNode, ExecutionStage.Deploy);
            executionGraph.ConnectConstant(deployBuildNode.BuildName, buildName);
            executionGraph.Connect(buildNode.OutputPath, deployBuildNode.BuildPath);
            executionGraph.Connect(buildNode.ExecutablePath, deployBuildNode.ExecutablePath);
            executionGraph.Connect(buildNode.BuildHash, deployBuildNode.BuildHash);

            var deployBuildConfigNode = new DeployBuildConfigurationNode($"{Settings.Name} {RemoteNodeConstants.k_DeployConfigBuildNodePostfix}");
            executionGraph.AddNode(deployBuildConfigNode, ExecutionStage.Deploy);
            executionGraph.ConnectConstant(deployBuildConfigNode.BuildConfigurationName, buildConfigurationName);
            executionGraph.ConnectConstant(deployBuildConfigNode.BuildName, buildName);
            executionGraph.ConnectConstant(deployBuildConfigNode.Settings, Settings.GetBuildConfigurationSettings());
            executionGraph.Connect(deployBuildNode.BuildId, deployBuildConfigNode.BuildId);
            executionGraph.Connect(buildNode.RelativeExecutablePath, deployBuildConfigNode.BinaryPath);

            var deployFleetNode = new DeployFleetNode($"{Settings.Name} {RemoteNodeConstants.k_DeployFleetNodePostfix}");
            executionGraph.AddNode(deployFleetNode, ExecutionStage.Deploy);
            executionGraph.ConnectConstant(deployFleetNode.FleetName, fleetName);
            executionGraph.ConnectConstant(deployFleetNode.Region, advancedConfiguration.FleetRegion);
            executionGraph.ConnectConstant(deployFleetNode.BuildConfigurationName, buildConfigurationName);
            executionGraph.Connect(deployBuildConfigNode.BuildConfigurationId, deployFleetNode.BuildConfigurationId);
            executionGraph.Connect(buildNode.ExecutablePath, deployFleetNode.BuildExecutablePath);


            var allocateNode = new AllocateNode($"{Settings.Name} {RemoteNodeConstants.k_AllocateNodePostfix}");
            executionGraph.AddNode(allocateNode, ExecutionStage.Run);
            executionGraph.ConnectConstant(allocateNode.FleetName, fleetName);
            executionGraph.ConnectConstant(allocateNode.BuildConfigurationName, buildConfigurationName);

            var remoteRunNode = new RunServerNode($"{Settings.Name} {RemoteNodeConstants.k_RunNodePostfix}");
            executionGraph.AddNode(remoteRunNode, ExecutionStage.Run);
            executionGraph.ConnectConstant(remoteRunNode.StreamLogs, Settings.AdvancedConfiguration.StreamLogsToMainEditor);
            executionGraph.ConnectConstant(remoteRunNode.LogsColor, Settings.AdvancedConfiguration.LogsColor);
            executionGraph.Connect(allocateNode.ServerId, remoteRunNode.ServerId);
            executionGraph.Connect(allocateNode.ConnectionDataOut, remoteRunNode.ConnectionData);

            // [TODO]: We need to remove this line, since 1 instance could have multiple nodes
            Settings.CorrespondingNodeId = remoteRunNode.Name;

            Settings.SetCorrespondingNodes(buildNode, deployBuildNode, deployBuildConfigNode, deployFleetNode, allocateNode, remoteRunNode);
        }

        /// <summary>
        /// Validates whether the current Unity project is properly set up for running a remote instance
        /// This includes:
        /// 1. Checking if the project is linked to Unity Cloud - has a valid cloud project ID
        /// 2. Verifying the existence of a valid Unity Cloud Environment ID
        /// Returns a failed ValidationResult if any step is not properly configured.
        /// </summary>
        protected internal override async Task<Scenario.ValidationResult> ValidateForRunningAsync(CancellationToken cancellationToken)
        {
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
