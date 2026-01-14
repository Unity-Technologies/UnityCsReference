// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    class InitialiseFleetNode : MultiplayNode
    {
        private const string k_DefaultLocalHost = "127.0.0.1";
        private const string k_DefaultPort = "9000";
        private const string k_DefaultQueryPort = "9010";

        [SerializeReference] private NodeInput<string> m_ProjectId;
        [SerializeReference] private NodeInput<string> m_EnvironmentId;
        [SerializeReference] private NodeInput<string> m_AuthToken;
        [SerializeReference] private NodeInput<bool> m_AutoAllocate;
        [SerializeReference] private NodeInput<SimulatorSettings.ProtocolType> m_QueryType;
        [SerializeReference] private NodeInput<string> m_LocalHost;
        [SerializeReference] private NodeInput<int> m_LocalPort;
        [SerializeReference] private NodeInput<int> m_QueryPort;

        public NodeInput<string> ProjectId => m_ProjectId;
        public NodeInput<string> EnvironmentId => m_EnvironmentId;
        public NodeInput<string> AuthToken => m_AuthToken;
        public NodeInput<bool> AutoAllocate => m_AutoAllocate;
        public NodeInput<SimulatorSettings.ProtocolType> QueryType => m_QueryType;
        public NodeInput<string> LocalHost => m_LocalHost;
        public NodeInput<int> LocalPort => m_LocalPort;
        public NodeInput<int> QueryPort => m_QueryPort;

        [SerializeReference] protected NodeOutput<string> m_FleetID;
        [SerializeReference] protected NodeOutput<string> m_FleetName;
        [SerializeReference] protected NodeOutput<string> m_ServerRemoteHost;
        [SerializeReference] protected NodeOutput<int> m_ServerRemotePort;
        [SerializeReference] protected NodeOutput<string> m_ServerState;
        [SerializeReference] protected NodeOutput<string> m_AllocationId;

        public NodeOutput<string> FleetID => m_FleetID;
        public NodeOutput<string> FleetName => m_FleetName;
        public NodeOutput<string> ServerRemoteHost => m_ServerRemoteHost;
        public NodeOutput<int> ServerRemotePort => m_ServerRemotePort;
        public NodeOutput<string> ServerState => m_ServerState;
        public NodeOutput<string> AllocationId => m_AllocationId;

        public InitialiseFleetNode(string name) : base(name)
        {
            m_ProjectId = new(this);
            m_EnvironmentId = new(this);
            m_AuthToken = new(this);
            m_AutoAllocate = new(this);
            m_QueryType = new(this);
            m_LocalHost = new(this);
            m_LocalPort = new(this);
            m_QueryPort = new(this);
            m_FleetID = new(this);
            m_FleetName = new(this);
            m_ServerRemoteHost = new(this);
            m_ServerRemotePort = new(this);
            m_ServerState = new(this);
            m_AllocationId = new(this);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            ValidateInputs();

            var result = await IPlayModeServices.Instance.InitialiseSimFleetAsync(
                GetInput(ProjectId),
                GetInput(EnvironmentId),
                GetInput(AuthToken),
                GetInput(m_AutoAllocate),
                GetInput(m_QueryType).Equals(SimulatorSettings.ProtocolType.A2S) ? "a2s" : "sqp",
                string.IsNullOrEmpty(GetInput(m_LocalHost)) ? k_DefaultLocalHost : GetInput(m_LocalHost),
                GetInput(m_LocalPort) > 0 ? GetInput(m_LocalPort).ToString() : k_DefaultPort,
                cancellationToken);

            await IPlayModeServices.Instance.InitialiseSimServerJsonAsync(
                result.FleetID,
                result.ServerId == null ? "1" : result.ServerId.ToString(),
                result.AllocationId,
                result.ServerLocation,
                string.IsNullOrEmpty(GetInput(m_LocalHost)) ? k_DefaultLocalHost : GetInput(m_LocalHost),
                GetInput(m_LocalPort) > 0 ? GetInput(m_LocalPort).ToString() : k_DefaultPort,
                GetInput(m_QueryPort) > 0 ? GetInput(m_QueryPort).ToString() : k_DefaultQueryPort,
                GetInput(m_QueryType).Equals(SimulatorSettings.ProtocolType.A2S) ? "a2s" : "sqp",
                cancellationToken);

            SetOutput(FleetID, result.FleetID);
            SetOutput(FleetName, result.FleetName);
            SetOutput(ServerRemoteHost, result.ServerRemoteHost);
            SetOutput(ServerRemotePort, result.ServerRemotePort);
            SetOutput(ServerState, result.ServerState);
            SetOutput(AllocationId, result.AllocationId);
        }

        private void ValidateInputs()
        {
            ValidateInputIsSet(ProjectId, nameof(ProjectId));
            ValidateInputIsSet(EnvironmentId, nameof(EnvironmentId));
            ValidateInputIsSet(AuthToken, nameof(AuthToken));
        }

        protected override async Task MonitorAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Expected when task is cancelled. Do nothing.
            }

            IPlayModeServices.Instance.ClearSimFleetAllocationLogs();
        }
    }
}
