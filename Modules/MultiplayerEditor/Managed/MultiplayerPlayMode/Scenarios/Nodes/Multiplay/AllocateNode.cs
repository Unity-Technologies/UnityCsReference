// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class AllocateNode : MultiplayNode
    {
        [SerializeReference] public NodeInput<string> FleetName;
        [SerializeReference] public NodeInput<string> BuildConfigurationName;

        [SerializeReference] public NodeOutput<long> ServerId;
        [SerializeReference] public NodeOutput<ConnectionData> ConnectionDataOut;

        public AllocateNode(string name)
            : base(name)
        {
            FleetName = new(this);
            BuildConfigurationName = new(this);
            ServerId = new(this);
            ConnectionDataOut = new(this);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            ValidateInputs();

            var result = await IPlayModeServices.Instance.CreateAndSyncTestAllocationAsync(
                GetInput(FleetName),
                GetInput(BuildConfigurationName),
                cancellationToken);

            SetOutput(ServerId, result.ServerId);
            SetOutput(ConnectionDataOut, new ConnectionData
            {
                IpAddress = result.Ipv4Address,
                Port = result.GamePort,
            });
        }

        private void ValidateInputs()
        {
            ValidateInputIsSet(FleetName, nameof(FleetName));
            ValidateInputIsSet(BuildConfigurationName, nameof(BuildConfigurationName));
        }
    }
}
