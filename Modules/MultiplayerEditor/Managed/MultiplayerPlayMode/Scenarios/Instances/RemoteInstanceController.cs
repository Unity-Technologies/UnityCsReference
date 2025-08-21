// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Assertions;
using UnityEngine.Multiplayer.Internal;
using static Unity.Multiplayer.PlayMode.Editor.ScenarioFactory;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class RemoteInstanceController : PlayModeController
    {
        private readonly RemoteInstanceDescription m_Settings;

        internal RemoteInstanceController(RemoteInstanceDescription remoteInstanceDescription)
        {
            m_Settings = remoteInstanceDescription;
        }

        protected internal override void SetupExecutionGraph(ExecutionGraph executionGraph)
        {
            var role = ScenarioFactory.GetRoleForInstance(m_Settings);

            // We assume the remote instance is a server.
            Assert.AreEqual(role, MultiplayerRoleFlags.Server);

            var buildPath = ScenarioFactory.GenerateBuildPath(m_Settings.BuildProfile);

            var buildNode = new EditorBuildNode($"{m_Settings.Name} {RemoteNodeConstants.k_BuildNodePostFix}");
            executionGraph.AddNode(buildNode, ExecutionStage.Prepare);
            executionGraph.ConnectConstant(buildNode.BuildPath, buildPath);
            executionGraph.ConnectConstant(buildNode.Profile, m_Settings.BuildProfile);


            var advancedConfiguration = m_Settings.AdvancedConfiguration;
            var multiplayName = RemoteInstanceDescription.ComputeMultiplayName(advancedConfiguration.Identifier);
            var buildName = multiplayName;
            var buildConfigurationName = multiplayName;
            var fleetName = multiplayName;
            // TODO Grab this from m_Settings.BuildProfile once ARM64 support has landed and we're in the engine.
            var architecture = "amd64";

            var deployBuildNode = new DeployBuildNode($"{m_Settings.Name} {RemoteNodeConstants.k_DeployBuildNodePostfix}");
            executionGraph.AddNode(deployBuildNode, ExecutionStage.Deploy);
            executionGraph.ConnectConstant(deployBuildNode.BuildName, buildName);
            executionGraph.Connect(buildNode.OutputPath, deployBuildNode.BuildPath);
            executionGraph.Connect(buildNode.ExecutablePath, deployBuildNode.ExecutablePath);
            executionGraph.Connect(buildNode.BuildHash, deployBuildNode.BuildHash);

            var deployBuildConfigNode = new DeployBuildConfigurationNode($"{m_Settings.Name} {RemoteNodeConstants.k_DeployConfigBuildNodePostfix}");
            executionGraph.AddNode(deployBuildConfigNode, ExecutionStage.Deploy);
            executionGraph.ConnectConstant(deployBuildConfigNode.BuildConfigurationName, buildConfigurationName);
            executionGraph.ConnectConstant(deployBuildConfigNode.BuildName, buildName);
            executionGraph.ConnectConstant(deployBuildConfigNode.Settings, m_Settings.GetBuildConfigurationSettings());
            executionGraph.Connect(deployBuildNode.BuildId, deployBuildConfigNode.BuildId);
            executionGraph.Connect(buildNode.RelativeExecutablePath, deployBuildConfigNode.BinaryPath);

            var deployFleetNode = new DeployFleetNode($"{m_Settings.Name} {RemoteNodeConstants.k_DeployFleetNodePostfix}");
            executionGraph.AddNode(deployFleetNode, ExecutionStage.Deploy);
            executionGraph.ConnectConstant(deployFleetNode.FleetName, fleetName);
            executionGraph.ConnectConstant(deployFleetNode.Region, advancedConfiguration.FleetRegion);
            executionGraph.ConnectConstant(deployFleetNode.Architecture, architecture);
            executionGraph.ConnectConstant(deployFleetNode.BuildConfigurationName, buildConfigurationName);
            executionGraph.Connect(deployBuildConfigNode.BuildConfigurationId, deployFleetNode.BuildConfigurationId);


            var allocateNode = new AllocateNode($"{m_Settings.Name} {RemoteNodeConstants.k_AllocateNodePostfix}");
            executionGraph.AddNode(allocateNode, ExecutionStage.Run);
            executionGraph.ConnectConstant(allocateNode.FleetName, fleetName);
            executionGraph.ConnectConstant(allocateNode.BuildConfigurationName, buildConfigurationName);

            var remoteRunNode = new RunServerNode($"{m_Settings.Name} {RemoteNodeConstants.k_RunNodePostfix}");
            executionGraph.AddNode(remoteRunNode, ExecutionStage.Run);
            executionGraph.ConnectConstant(remoteRunNode.StreamLogs, m_Settings.AdvancedConfiguration.StreamLogsToMainEditor);
            executionGraph.ConnectConstant(remoteRunNode.LogsColor, m_Settings.AdvancedConfiguration.LogsColor);
            executionGraph.Connect(allocateNode.ServerId, remoteRunNode.ServerId);
            executionGraph.Connect(allocateNode.ConnectionDataOut, remoteRunNode.ConnectionData);

            // [TODO]: We need to remove this line, since 1 instance could have multiple nodes
            m_Settings.CorrespondingNodeId = remoteRunNode.Name;

            m_Settings.SetCorrespondingNodes(buildNode, deployBuildNode, deployBuildConfigNode, deployFleetNode, allocateNode, remoteRunNode);
        }
    }
}
