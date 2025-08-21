// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class SetupWebSocketNode : MultiplayNode
    {
        [SerializeReference] private NodeInput<string> m_ProjectId;
        [SerializeReference] private NodeInput<string> m_EnvironmentId;
        [SerializeReference] private NodeInput<string> m_AllocationId;
        [SerializeReference] private NodeInput<string> m_AuthToken;
        [SerializeReference] private NodeInput<bool> m_WaitForEditorPlaying;

        [SerializeReference] private NodeInput<string> m_FleetID;
        [SerializeReference] private NodeInput<string> m_ServerRemoteHost;
        [SerializeReference] private NodeInput<int> m_ServerRemotePort;

        public NodeInput<string> ProjectId => m_ProjectId;
        public NodeInput<string> EnvironmentId => m_EnvironmentId;
        public NodeInput<string> AllocationId => m_AllocationId;
        public NodeInput<string> AuthToken => m_AuthToken;
        public NodeInput<bool> WaitForEditorPlaying => m_WaitForEditorPlaying;
        public NodeInput<string> FleetID => m_FleetID;
        public NodeInput<string> ServerRemoteHost => m_ServerRemoteHost;
        public NodeInput<int> ServerRemotePort => m_ServerRemotePort;

        public SetupWebSocketNode(string name) : base(name)
        {
            m_ProjectId = new(this);
            m_EnvironmentId = new(this);
            m_AllocationId = new(this);
            m_AuthToken = new(this);
            m_WaitForEditorPlaying = new(this);
            m_FleetID = new(this);
            m_ServerRemoteHost = new(this);
            m_ServerRemotePort = new(this);
        }

        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            ValidateInputs();
            return Task.CompletedTask;
        }

        protected override async Task MonitorAsync(CancellationToken cancellationToken)
        {
            await IPlayModeServices.Instance.MonitorSimFleetAsync(
                GetInput(WaitForEditorPlaying),
                GetInput(m_ProjectId),
                GetInput(m_EnvironmentId),
                GetInput(m_AuthToken),
                GetInput(m_AllocationId),
                GetInput(m_ServerRemoteHost),
                GetInput(m_ServerRemotePort),
                cancellationToken
            );
        }

        private void ValidateInputs()
        {
            ValidateInputIsSet(ProjectId, nameof(ProjectId));
            ValidateInputIsSet(EnvironmentId, nameof(EnvironmentId));
            ValidateInputIsSet(AuthToken, nameof(AuthToken));
            ValidateInputIsSet(FleetID, nameof(FleetID));
            ValidateInputIsSet(ServerRemoteHost, nameof(ServerRemoteHost));
            ValidateInputIsSet(ServerRemotePort, nameof(ServerRemotePort));
        }
    }
}
