// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Build.Profile;
using UnityEditor.Multiplayer.Internal;
using UnityEngine.Multiplayer.Internal;

namespace Unity.Multiplayer.PlayMode.Editor
{
    static class LocalDeploymentUtility
    {
        private const string DefaultHost = "127.0.0.1";

        internal static Node[] SetupExecutionGraph(LocalInstanceDescription settings, ExecutionGraph graph, bool hasEditorInstance)
        {
            if (!ShouldEnableLocalDeployment(settings))
                return Array.Empty<Node>();

            var setupEnvironmentNode = new SetupEnvironmentNode($"{settings.Name} - Setup Environment");
            var initialiseFleetNode = new InitialiseFleetNode($"{settings.Name} - Initialise Fleet");
            var setupWebSocketNode = new SetupWebSocketNode($"{settings.Name} - Setup WebSocket");

            graph.AddNode(setupEnvironmentNode, ExecutionStage.Deploy);
            graph.AddNode(initialiseFleetNode, ExecutionStage.Deploy);
            graph.AddNode(setupWebSocketNode, ExecutionStage.Run);

            settings.ServerSettings.CliSettings.ComputeFinalArguments(out var port, out var queryType, out var queryPort);

            graph.ConnectConstant(initialiseFleetNode.AutoAllocate, settings.ServerSettings.SimulatorSettings.AutoAllocate);
            graph.ConnectConstant(initialiseFleetNode.QueryType, queryType);
            graph.ConnectConstant(initialiseFleetNode.QueryPort, queryPort);
            graph.ConnectConstant(initialiseFleetNode.LocalPort, port);
            graph.ConnectConstant(initialiseFleetNode.LocalHost, DefaultHost);
            graph.ConnectConstant(setupWebSocketNode.WaitForEditorPlaying, hasEditorInstance);

            graph.Connect(setupEnvironmentNode.ProjectId, initialiseFleetNode.ProjectId);
            graph.Connect(setupEnvironmentNode.EnvironmentId, initialiseFleetNode.EnvironmentId);
            graph.Connect(setupEnvironmentNode.AuthToken, initialiseFleetNode.AuthToken);

            graph.Connect(initialiseFleetNode.FleetID, setupWebSocketNode.FleetID);
            graph.Connect(initialiseFleetNode.ServerRemoteHost, setupWebSocketNode.ServerRemoteHost);
            graph.Connect(initialiseFleetNode.ServerRemotePort, setupWebSocketNode.ServerRemotePort);

            graph.Connect(setupEnvironmentNode.ProjectId, setupWebSocketNode.ProjectId);
            graph.Connect(setupEnvironmentNode.EnvironmentId, setupWebSocketNode.EnvironmentId);
            graph.Connect(setupEnvironmentNode.AuthToken, setupWebSocketNode.AuthToken);
            graph.Connect(initialiseFleetNode.AllocationId, setupWebSocketNode.AllocationId);

            return new Node[]
            {
                setupEnvironmentNode,
                initialiseFleetNode,
                setupWebSocketNode
            };
        }

        internal static bool ShouldEnableLocalDeployment(LocalInstanceDescription settings)
        {
            return IsServerProfileOrRole(settings.BuildProfile) &&
                   settings.ServerSettings.DeployMode == ServerSettings.ServerDeployMode.Simulated;
        }

        internal static bool IsServerProfileOrRole(BuildProfile buildProfile)
        {
            if (buildProfile == null)
                return false;

            if (MultiplayerRolesSettings.instance.GetMultiplayerRoleForBuildProfile(buildProfile) == MultiplayerRoleFlags.Server)
                return true;

            return false;
        }
    }
}
